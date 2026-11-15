using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace Society.Api;

// Passwordless identities are prepared with memberships; email ownership is proved at first OTP sign-in.
public static class MemberAccountProvisioning
{
 public static string? Email(string? value) => value is not null && value.Trim().Length <= 254 && MailAddress.TryCreate(value.Trim(), out var address) && address.Address.Equals(value.Trim(),StringComparison.OrdinalIgnoreCase) ? address.Address.ToUpperInvariant() : null;
 static string[] Emails(Membership member,string kind){var p=MembershipService.Json(member.Profile);var values=new List<string?>{p["email"]?.ToString()};if(kind!="Individual"){values.Add(p["contactEmail"]?.ToString());if(p["contacts"] is JsonArray contacts)values.AddRange(contacts.Select(c=>c?["email"]?.ToString()));}return values.Select(Email).Where(e=>e!=null).Cast<string>().Distinct().Order().ToArray();}
 public static async Task RecordClaims(AppDb db,Membership member,Guid actor,CancellationToken ct=default){
  var category=db.MembershipCategories.Local.FirstOrDefault(c=>c.Id==member.CategoryId)??await db.MembershipCategories.SingleAsync(c=>c.Id==member.CategoryId,ct);
  var emails=Emails(member,category.Kind);if(emails.Length==0)return;
  var saved=await db.MembershipHistories.Where(h=>h.MembershipId==member.Id&&(h.Action=="membership.import.approved"||h.Action=="membership.manual.claim.approved")).Select(h=>h.Details).ToListAsync(ct);
  saved.AddRange(db.MembershipHistories.Local.Where(h=>h.MembershipId==member.Id&&(h.Action=="membership.import.approved"||h.Action=="membership.manual.claim.approved")).Select(h=>h.Details));
  var known=saved.SelectMany(Claims).ToHashSet();var added=emails.Where(e=>!known.Contains(e)).ToArray();if(added.Length==0)return;
  db.MembershipHistories.Add(new MembershipHistory{TenantId=member.TenantId,MembershipId=member.Id,ActorId=actor,Action="membership.manual.claim.approved",Details=JsonSerializer.Serialize(new{claimEmails=added,reason="Automatic member account provisioning"})});
 }
 static IEnumerable<string> Claims(string details){var p=JsonNode.Parse(details);return p?["claimEmails"] is JsonArray a?a.Select(v=>Email(v?.ToString())).Where(v=>v!=null).Cast<string>():[];}
 public static bool Pending(AppDb db)=>db.ChangeTracker.Entries().Any(e=>e.State==EntityState.Added&&(e.Entity is Membership||e.Entity is OrganizationMember||e.Entity is MembershipHistory h&&(h.Action=="membership.import.approved"||h.Action=="membership.manual.claim.approved")));
 public static async Task Prepare(AppDb db,CancellationToken ct){
  var members=db.ChangeTracker.Entries<Membership>().Where(e=>e.State==EntityState.Added).Select(e=>e.Entity).ToArray();
  foreach(var m in members){var actor=db.MembershipHistories.Local.FirstOrDefault(h=>h.MembershipId==m.Id)?.ActorId??Guid.Empty;await RecordClaims(db,m,actor,ct);}
  var requests=new List<(string Email,string Name,string Kind,Guid Tenant)>();
  foreach(var history in db.ChangeTracker.Entries<MembershipHistory>().Where(e=>e.State==EntityState.Added&&(e.Entity.Action=="membership.import.approved"||e.Entity.Action=="membership.manual.claim.approved")).Select(e=>e.Entity).ToArray()){
   var m=db.Memberships.Local.FirstOrDefault(m=>m.Id==history.MembershipId)??await db.Memberships.SingleAsync(m=>m.Id==history.MembershipId,ct);var person=db.Persons.Local.FirstOrDefault(p=>p.Id==m.PersonId)??await db.Persons.SingleAsync(p=>p.Id==m.PersonId,ct);var kind=db.MembershipCategories.Local.FirstOrDefault(c=>c.Id==m.CategoryId)?.Kind??await db.MembershipCategories.Where(c=>c.Id==m.CategoryId).Select(c=>c.Kind).SingleAsync(ct);
   requests.AddRange(Claims(history.Details).Select(email=>(email,person.FullName,kind,m.TenantId)));
  }
  foreach(var r in db.ChangeTracker.Entries<OrganizationMember>().Where(e=>e.State==EntityState.Added&&e.Entity.RevokedAt==null).Select(e=>e.Entity).ToArray())if(Email(r.Email) is string email)requests.Add((email,r.FullName,"Individual",r.TenantId));
  foreach(var r in requests.OrderBy(r=>r.Email).DistinctBy(r=>r.Email))await Ensure(db,r.Email,r.Name,r.Kind,r.Tenant,ct);
 }
 public static async Task<bool> Ensure(AppDb db,string email,string name,string kind,Guid tenant,CancellationToken ct=default){
  // The caller holds a transaction; serialize with signup, provider login and OTP activation.
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"auth-email:"+email}))",ct);
  if(db.Users.Local.Any(u=>u.NormalizedEmail==email)||await db.Users.AnyAsync(u=>u.NormalizedEmail==email,ct))return false;
  if(db.Users.Local.Any(u=>u.NormalizedUserName==email)||await db.Users.AnyAsync(u=>u.NormalizedUserName==email,ct))throw new DomainException("This member email conflicts with an existing username. Review the account before adding it.");
  var account=new Account{Id=Guid.NewGuid(),Email=email.ToLowerInvariant(),NormalizedEmail=email,UserName=email.ToLowerInvariant(),NormalizedUserName=email,FullName=name,PreferredMembershipKind=kind,SignupTenantId=tenant,SecurityStamp=Guid.NewGuid().ToString("N"),ConcurrencyStamp=Guid.NewGuid().ToString("N"),LockoutEnabled=true};db.Users.Add(account);db.Audits.Add(new Audit{TenantId=tenant,ActorId=Guid.Empty,ResourceId=account.Id,Action="auth.member.account.prepared",Details="{}"});return true;
 }
 public static async Task<object> Backfill(AppDb db,CancellationToken ct=default){
  await using var tx=await db.Database.BeginTransactionAsync(ct);await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext('member-account-backfill'))",ct);
  var members=await db.Memberships.Where(m=>m.Status!="Closed"&&db.Tenants.Any(t=>t.Id==m.TenantId&&t.Status=="Active")).OrderBy(m=>m.Id).ToListAsync(ct);var before=await db.Users.CountAsync(ct);var missing=new List<object>();
  foreach(var m in members){var kind=await db.MembershipCategories.Where(c=>c.Id==m.CategoryId).Select(c=>c.Kind).SingleAsync(ct);if(Emails(m,kind).Length==0)missing.Add(new{m.Id,m.Number,kind});await RecordClaims(db,m,Guid.Empty,ct);}
  await db.SaveChangesAsync(ct);
  // Also repair approved histories whose account was never created, and all chapter/club roster members.
  var histories=await db.MembershipHistories.Where(h=>(h.Action=="membership.import.approved"||h.Action=="membership.manual.claim.approved")&&db.Memberships.Any(m=>m.Id==h.MembershipId&&m.Status!="Closed")).ToListAsync(ct);
  foreach(var h in histories){var m=await db.Memberships.SingleAsync(m=>m.Id==h.MembershipId,ct);var person=await db.Persons.SingleAsync(p=>p.Id==m.PersonId,ct);var kind=await db.MembershipCategories.Where(c=>c.Id==m.CategoryId).Select(c=>c.Kind).SingleAsync(ct);foreach(var email in Claims(h.Details).Order())await Ensure(db,email,person.FullName,kind,m.TenantId,ct);}
  var roster=await db.Set<OrganizationMember>().Where(r=>r.RevokedAt==null).OrderBy(r=>r.Email).ToListAsync(ct);var missingRoster=0;foreach(var r in roster){if(Email(r.Email) is string email)await Ensure(db,email,r.FullName,"Individual",r.TenantId,ct);else missingRoster++;}
  await db.SaveChangesAsync(ct);var after=await db.Users.CountAsync(ct);await tx.CommitAsync(ct);return new{members=members.Count,rosterMembers=roster.Count,accountsCreated=after-before,totalAccounts=after,missingEmail=missing,rosterMissingEmail=missingRoster};
 }
}
