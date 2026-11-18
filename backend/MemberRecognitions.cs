using Microsoft.EntityFrameworkCore;
using System.Text.Json;
namespace Society.Api;
public record MemberRecognitionWrite(string[] Recognitions,int Revision,string Reason);
public static class MemberRecognitions {
 public const string ManagerEmail="SAISATISH@INDIANSERVERS.COM",HistoryAction="membership.recognitions.updated";
 public static readonly string[] Options=["Fellowship","Board Member"];
 static async Task<bool> Manager(AppDb db,Guid user)=>await db.Users.AnyAsync(u=>u.Id==user&&u.PlatformAdmin&&u.EmailConfirmed&&u.NormalizedEmail==ManagerEmail);
 static async Task<Membership> Member(AppDb db,Guid tenant,Guid id)=>await db.Memberships.SingleOrDefaultAsync(m=>m.Id==id&&m.TenantId==tenant)??throw new AccessDeniedException();
 static string[] Values(string? details){if(details==null)return [];using var doc=JsonDocument.Parse(details);return doc.RootElement.TryGetProperty("recognitions",out var values)?values.EnumerateArray().Select(v=>v.GetString()!).Where(Options.Contains).Distinct().ToArray():[];}
 static async Task<MembershipHistory?> Latest(AppDb db,Guid tenant,Guid id)=>await db.MembershipHistories.AsNoTracking().Where(h=>h.TenantId==tenant&&h.MembershipId==id&&h.Action==HistoryAction).OrderByDescending(h=>h.At).ThenByDescending(h=>h.Id).FirstOrDefaultAsync();
 public static void Map(RouteGroupBuilder api){
  api.MapGet("/societies/{tenant:guid}/membership/members/{id:guid}/recognitions",async(Guid tenant,Guid id,AppDb db,Access a)=>{
   var member=await Member(db,tenant,id);if(!await a.Platform()&&!await MembershipExperience.Owned(db,a.UserId,true).AnyAsync(m=>m.Id==id&&m.TenantId==tenant))await a.Require(tenant,"members.read",member.ChapterId);
   var history=await Latest(db,tenant,id);return Results.Ok(new{options=Options,recognitions=Values(history?.Details),revision=member.Revision,canManage=await Manager(db,a.UserId)&&MembershipService.Json(member.Profile)["adminArchived"]?.GetValue<bool>()!=true,updatedAt=history?.At,updatedBy=history==null?null:await db.Users.Where(u=>u.Id==history.ActorId).Select(u=>u.FullName).FirstOrDefaultAsync()});
  }).RequireAuthorization();
  api.MapPut("/platform/membership/{tenant:guid}/members/{id:guid}/recognitions",async(Guid tenant,Guid id,MemberRecognitionWrite r,AppDb db,Access a,MembershipService service)=>{
   if(!await Manager(db,a.UserId))throw new AccessDeniedException();
   if(r.Recognitions==null||r.Recognitions.Length>Options.Length||r.Recognitions.Any(x=>!Options.Contains(x))||r.Recognitions.Distinct().Count()!=r.Recognitions.Length)throw new DomainException("Choose Fellowship, Board Member, both, or neither");
   if(string.IsNullOrWhiteSpace(r.Reason)||r.Reason.Trim().Length<5||r.Reason.Length>1000)throw new DomainException("Enter a reason of 5–1,000 characters");
   await using var tx=await db.Database.BeginTransactionAsync();await MembershipService.Lock(db,tenant);var member=await Member(db,tenant,id);if(member.Revision!=r.Revision)return Results.Conflict(new{title="Membership changed. Refresh before updating recognitions"});if(MembershipService.Json(member.Profile)["adminArchived"]?.GetValue<bool>()==true)throw new DomainException("Restore this archived membership before editing recognitions");
   var before=Values((await Latest(db,tenant,id))?.Details);var selected=Options.Where(r.Recognitions.Contains).ToArray();
   if(!before.SequenceEqual(selected)){member.Revision++;service.History(member,a.UserId,HistoryAction,new{before,recognitions=selected,reason=r.Reason.Trim()});await db.SaveChangesAsync();}
   await tx.CommitAsync();return Results.Ok(new{recognitions=selected,revision=member.Revision});
  }).RequireAuthorization();
 }
}
