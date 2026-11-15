using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;
using System.Net.Mail;
using System.Text.RegularExpressions;
namespace Society.Api;
public record ContactPerson(string Name,string Email,string Mobile,string Designation="",string Department="");
public record ContactEdit(int Revision,ContactPerson[] Contacts);
public static class MembershipUx {
 public static async Task<Membership> Manage(Guid tenant,Guid id,AppDb db,Access a,MembershipService s,bool write=false){
  if(await a.Platform())await PlatformMemberAdmin.Require(a);else await a.Member(tenant);var m=await db.Memberships.SingleOrDefaultAsync(m=>m.TenantId==tenant&&m.Id==id)??throw new AccessDeniedException();
  if(await a.Platform())return m;
  if(!await s.IsSelf(m)&&!(await a.Can(tenant,"members.read",m.ChapterId)&&(!write||await a.Can(tenant,"members.write",m.ChapterId)))&&!await ClubMembership.CanManageRoster(db,a,tenant,id))throw new AccessDeniedException();return m;
 }
 public static void Map(RouteGroupBuilder api){var t=api.MapGroup("/societies/{tenant:guid}/membership").RequireAuthorization();
 t.MapGet("/members/{id:guid}/contact-people",async(Guid tenant,Guid id,AppDb db,Access a,MembershipService s)=>{var m=await Manage(tenant,id,db,a,s);var p=MembershipService.Json(m.Profile);return Results.Ok(new{m.Revision,contacts=p["contacts"]?.DeepClone()??new JsonArray(),canEdit=await a.Platform()||await s.IsSelf(m)||await a.Can(tenant,"members.write",m.ChapterId)||await ClubMembership.CanManageRoster(db,a,tenant,id)});});
 t.MapPut("/members/{id:guid}/contact-people",async(Guid tenant,Guid id,ContactEdit r,AppDb db,Access a,MembershipService s)=>{
  await using var tx=await db.Database.BeginTransactionAsync();await MembershipService.Lock(db,tenant);var m=await Manage(tenant,id,db,a,s,true);
  if(await db.MembershipCategories.AnyAsync(c=>c.Id==m.CategoryId&&c.Kind=="Individual"))throw new DomainException("Edit personal contact fields on the individual profile");
  if(m.Revision!=r.Revision)return Results.Conflict(new{title="Contacts changed. Refresh before saving."});
  if(r.Contacts==null||r.Contacts.Length>100)throw new DomainException("Use up to 100 organization contacts");
  foreach(var c in r.Contacts){if(c==null||c.Email==null||c.Mobile==null||c.Designation==null||c.Department==null)throw new DomainException("Contact fields must be text");if(string.IsNullOrWhiteSpace(c.Name)||c.Name.Length>160||c.Designation.Length>160||c.Department.Length>160)throw new DomainException("Every contact needs a name of up to 160 characters");if(c.Email!=""&&(!MailAddress.TryCreate(c.Email,out _)||c.Email.Length>254))throw new DomainException("Enter a valid contact email");if(c.Mobile!=""&&(!Regex.IsMatch(c.Mobile,@"^\+?[0-9 ()\-.]{7,30}$")||Regex.Replace(c.Mobile,@"\D","").Length is <7 or >15))throw new DomainException("Enter a mobile number with 7–30 digits or phone punctuation");}
  var p=MembershipService.Json(m.Profile);var before=p["contacts"]?.DeepClone();var contacts=new JsonArray(r.Contacts.Select((c,i)=>(JsonNode)new JsonObject{["name"]=c.Name.Trim(),["email"]=c.Email.Trim(),["mobile"]=c.Mobile.Trim(),["designation"]=c.Designation.Trim(),["department"]=c.Department.Trim(),["sourceSlot"]=i+1}).ToArray());
  p["contacts"]=contacts;p["contactName"]=r.Contacts.FirstOrDefault()?.Name.Trim()??"";p["contactEmail"]=r.Contacts.FirstOrDefault()?.Email.Trim()??"";p["contactPhone"]=r.Contacts.FirstOrDefault()?.Mobile.Trim()??"";p["email"]=p["contactEmail"]?.DeepClone();p["phone"]=p["contactPhone"]?.DeepClone();m.Profile=p.ToJsonString();MembershipService.ValidateProfile(m.Profile);m.Revision++;
  var chapters=await db.Set<InstitutionChapter>().Where(l=>l.TenantId==tenant&&l.MembershipId==id).Select(l=>l.ChapterId).ToArrayAsync();foreach(var u in await db.Units.Where(u=>u.TenantId==tenant&&chapters.Contains(u.Id)).ToListAsync()){var d=MembershipService.Json(u.Details);foreach(var k in new[]{"contacts","contactName","contactEmail","contactPhone"})d[k]=p[k]?.DeepClone();u.Details=d.ToJsonString();u.Revision++;u.UpdatedAt=DateTimeOffset.UtcNow;}
  s.History(m,a.UserId,"organization.contacts.updated",new{before,after=contacts});await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(new{m.Revision,savedAt=DateTimeOffset.UtcNow});
 });
 t.MapGet("/members/{id:guid}/changes",async(Guid tenant,Guid id,AppDb db,Access a,MembershipService s)=>{await Manage(tenant,id,db,a,s);return await(from h in db.MembershipHistories join actor in db.Users on h.ActorId equals actor.Id where h.TenantId==tenant&&h.MembershipId==id orderby h.At descending select new{h.Id,h.Action,h.At,h.Details,actor=actor.FullName}).Take(100).ToListAsync();});
 t.MapGet("/duplicates",async(Guid tenant,string? name,string? email,string? number,string? kind,AppDb db,Access a,MembershipService s)=>{await a.Member(tenant);var readable=a.Visible(tenant,"members.read").Select(u=>u.Id);var q=MembershipIdentity.Filter(db,s.Administrative(tenant,"members.write").Where(m=>readable.Contains(m.ChapterId)),kind);var nm=(name??"").Trim().ToLower();var em=(email??"").Trim().ToLower();var num=(number??"").Trim().ToLower();var ids=db.Memberships.FromSqlInterpolated($"SELECT * FROM \"Memberships\" WHERE \"TenantId\"={tenant} AND ({em}<>'' AND lower(\"Profile\"->>'email')={em})").Select(m=>m.Id);return await(from m in q join p in db.Persons on m.PersonId equals p.Id where (nm.Length>=3&&p.SearchName==nm)||(num!=""&&m.Number.ToLower()==num)||ids.Contains(m.Id) select new{m.Id,m.Number,p.FullName}).Take(10).ToListAsync();});
 }
}
