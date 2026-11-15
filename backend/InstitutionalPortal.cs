using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
namespace Society.Api;
public static class InstitutionalPortal {
 public record Request([Required,MaxLength(2000)] string Note,Guid? ChapterId=null,[MaxLength(160)] string? ProposedName=null);
 static async Task<Membership> Own(Guid id,AppDb db,Access a)=>await MembershipExperience.Owned(db,a.UserId).SingleOrDefaultAsync(m=>m.Id==id&&db.MembershipCategories.Any(c=>c.Id==m.CategoryId&&c.Kind!="Individual"))??throw new AccessDeniedException();
 public static void Map(RouteGroupBuilder api){
 var group=api.MapGroup("/membership/institutions/{id:guid}").RequireAuthorization();
 group.MapGet("/chapters",async(Guid id,AppDb db,Access a)=>{var m=await Own(id,db,a);return await db.Units.Where(u=>u.TenantId==m.TenantId&&u.Status=="Active"&&db.Levels.Any(l=>l.Id==u.LevelId&&l.Kind=="Chapter")).OrderBy(u=>u.Name).Select(u=>new{u.Id,u.Name,u.Code}).ToListAsync();});
 group.MapGet("/requests",async(Guid id,AppDb db,Access a)=>{var m=await Own(id,db,a);return await db.MembershipHistories.Where(h=>h.MembershipId==id&&h.TenantId==m.TenantId&&(h.Action=="institution.chapter.affiliation.requested"||h.Action=="institution.name.correction.requested")).OrderByDescending(h=>h.At).Take(50).Select(h=>new{h.Id,h.Action,h.At,h.Details}).ToListAsync();});
 group.MapPost("/requests/{kind}",async(Guid id,string kind,Request r,AppDb db,Access a,MembershipService service)=>{
 await using var tx=await db.Database.BeginTransactionAsync();var m=await Own(id,db,a);await MembershipService.Lock(db,m.TenantId);
 if(string.IsNullOrWhiteSpace(r.Note))throw new DomainException("Include a note for the membership team");
 if(kind=="affiliation"){
 if(r.ChapterId==null||!await db.Units.AnyAsync(u=>u.Id==r.ChapterId&&u.TenantId==m.TenantId&&u.Status=="Active"&&db.Levels.Any(l=>l.Id==u.LevelId&&l.Kind=="Chapter")))throw new DomainException("Choose an active chapter");
 if(await db.Set<InstitutionChapter>().AnyAsync(l=>l.TenantId==m.TenantId&&l.ChapterId==r.ChapterId&&l.MembershipId==id))throw new DomainException("This chapter is already linked");
 var match=System.Text.Json.JsonSerializer.Serialize(new{chapterId=r.ChapterId});
 if(await db.MembershipHistories.AnyAsync(h=>h.MembershipId==id&&h.Action=="institution.chapter.affiliation.requested"&&EF.Functions.JsonContains(h.Details,match)))throw new DomainException("An affiliation request for this chapter is already recorded. Contact AIMER for an update.");
 service.History(m,a.UserId,"institution.chapter.affiliation.requested",new{chapterId=r.ChapterId,note=r.Note.Trim(),status="Pending administrator review"});
 }else if(kind=="name-correction"){
 if(string.IsNullOrWhiteSpace(r.ProposedName))throw new DomainException("Enter the proposed institution name");
 service.History(m,a.UserId,"institution.name.correction.requested",new{proposedName=r.ProposedName.Trim(),note=r.Note.Trim(),status="Pending administrator review"});
 }else throw new DomainException("Choose an affiliation or name correction request");
 await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(new{message="Request recorded for AIMER administrator review. Your institutional record is unchanged."});
 });
 }
}
