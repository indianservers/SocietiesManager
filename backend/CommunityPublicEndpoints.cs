using Microsoft.EntityFrameworkCore;
namespace Society.Api;
public static class CommunityPublicEndpoints {
 public static void Map(RouteGroupBuilder api){var t=api.MapGroup("/public/societies/{tenant:guid}/community").RequireRateLimiting("community");
 t.MapGet("/posts/{id:guid}",async(Guid tenant,Guid id,AppDb db)=>{if(!await Allowed(db,tenant))return Results.NotFound();var p=await db.Set<ActivityPost>().Where(p=>p.TenantId==tenant&&p.Id==id&&p.Status=="Published"&&p.Visibility=="Public").Select(p=>new{p.Id,p.Title,p.Body,p.PublishedAt,p.Official,organization=db.Units.Where(u=>u.Id==p.UnitId).Select(u=>u.Name).FirstOrDefault()??"Society headquarters",media=db.Set<CommunityMedia>().Where(m=>m.TenantId==tenant&&m.ResourceKind=="Post"&&m.ResourceId==id).OrderBy(m=>m.Sort).Select(m=>new{m.Id,m.ContentType,m.Name}).ToArray()}).SingleOrDefaultAsync();return p==null?Results.NotFound():Results.Ok(p);});
 t.MapGet("/media/{id:guid}",async(Guid tenant,Guid id,AppDb db,IObjectStore storage)=>{if(!await Allowed(db,tenant))return Results.NotFound();var m=await db.Set<CommunityMedia>().SingleOrDefaultAsync(m=>m.TenantId==tenant&&m.Id==id&&m.ResourceKind=="Post"&&db.Set<ActivityPost>().Any(p=>p.TenantId==tenant&&p.Id==m.ResourceId&&p.Status=="Published"&&p.Visibility=="Public"));if(m==null)return Results.NotFound();return m.ContentType=="application/pdf"?Results.File(storage.Open(tenant,id),m.ContentType,"attachment.pdf"):Results.File(storage.Open(tenant,id),m.ContentType,enableRangeProcessing:true);});
 }
 static Task<bool> Allowed(AppDb db,Guid tenant)=>db.Set<CommunityPolicy>().AnyAsync(p=>p.TenantId==tenant&&p.PublicAllowed&&db.Tenants.Any(t=>t.Id==tenant&&t.Status=="Active"));
}
