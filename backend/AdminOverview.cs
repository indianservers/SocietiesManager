using Microsoft.EntityFrameworkCore;
namespace Society.Api;

public static class AdminOverview
{
    public static void Map(RouteGroupBuilder api)
    {
        // Aggregate overview only. Member records and administrative actions retain their own authorization.
        api.MapGet("/platform/overview", async (Guid? tenant, string? period, AppDb db, Access a, HttpContext context) => {
            if (!await a.Platform()) throw new AccessDeniedException();
            context.Response.Headers.CacheControl="no-store";
            var units=db.Units.Where(u=>tenant==null || u.TenantId==tenant);
            var memberships=db.Memberships.Where(m=>tenant==null || m.TenantId==tenant);
            var india=TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"));
            var since=period=="all"?DateTimeOffset.MinValue:new DateTimeOffset(india.Year,period=="year"?1:india.Month,1,0,0,0,india.Offset).ToUniversalTime();
            var chapters=await units.Where(u=>db.Levels.Any(l=>l.Id==u.LevelId && l.Kind=="Chapter"))
                .OrderByDescending(u=>memberships.Count(m=>m.ChapterId==u.Id)).ThenBy(u=>u.Name)
                .Select(u=>new {u.Id,u.Name,u.Code,u.Status,members=memberships.Count(m=>m.ChapterId==u.Id&&db.MembershipCategories.Any(c=>c.Id==m.CategoryId&&c.Kind=="Individual")), organizations=memberships.Count(m=>m.ChapterId==u.Id&&db.MembershipCategories.Any(c=>c.Id==m.CategoryId&&c.Kind!="Individual")),
                    activity=db.Set<ActivityPost>().Count(p=>p.TenantId==u.TenantId && p.UnitId==u.Id && p.Status=="Published" && p.CreatedAt>=since)})
                .Take(100).ToListAsync();
            var feed=await db.Audits.Where(x=>x.TenantId!=Guid.Empty && (tenant==null || x.TenantId==tenant) && x.At>=since &&
                (x.Action=="membership.workbooks.imported" || x.Action=="member.approved" || x.Action=="membership.renewed" || x.Action=="activity.published" || x.Action=="unit.created" || x.Action=="club.profile.updated"))
                .OrderByDescending(x=>x.At).Take(12).Select(x=>new {x.Id,x.Action,x.At}).ToListAsync();
            return Results.Ok(new {
                units=await units.CountAsync(),regions=await units.CountAsync(u=>db.Levels.Any(l=>l.Id==u.LevelId && l.Kind=="Region")),
                chapters=await units.CountAsync(u=>db.Levels.Any(l=>l.Id==u.LevelId && l.Kind=="Chapter")),active=await units.CountAsync(u=>u.Status=="Active"),
                memberCount=await memberships.CountAsync(),individualMembers=await MembershipIdentity.Filter(db,memberships,"Individual").CountAsync(),institutionalMembers=await MembershipIdentity.Filter(db,memberships,"Institutional").CountAsync(),corporateMembers=await MembershipIdentity.Filter(db,memberships,"Corporate").CountAsync(),chapterRows=chapters,feed,updatedAt=DateTimeOffset.UtcNow
            });
        }).RequireAuthorization();
    }
}
