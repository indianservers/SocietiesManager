using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;
namespace Society.Api;

public static class ClubMembership
{
    public static async Task<bool> CanManage(AppDb db, Access a, Guid tenant, Guid chapter)
    {
        await a.Member(tenant);
        if (await a.Can(tenant, "units.write", chapter)) return true;
        var now = DateTimeOffset.UtcNow;
        var officer = await (from ap in db.OfficerAppointments join p in db.OfficerPositions on ap.PositionId equals p.Id
            join m in db.Memberships on ap.MembershipId equals m.Id join person in db.Persons on m.PersonId equals person.Id
            where ap.TenantId == tenant && ap.UnitId == chapter && ap.Status == "Approved" && ap.StartsAt <= now && ap.EndsAt > now
            && p.Active && (p.Name == "President" || p.Name == "Chapter President" || p.Name == "Club President" || p.Name == "Secretary" || p.Name == "Chapter Secretary" || p.Name == "Club Secretary") && person.UserId == a.UserId
            && m.Status == "Active" && !m.PendingPayment && m.EffectiveFrom <= now && (m.EndsAt == null || m.EndsAt > now) select ap.Id).AnyAsync();
        if (officer) return true;
        var entitled = MembershipExperience.Entitled(db, a.UserId);
        return await db.Set<InstitutionChapter>().AnyAsync(l => l.TenantId == tenant && l.ChapterId == chapter && entitled.Any(m => m.Id == l.MembershipId && m.TenantId == tenant && m.Status == "Active" && !m.PendingPayment && m.EffectiveFrom <= now && (m.EndsAt == null || m.EndsAt > now)));
    }
    public static async Task<bool> CanManageRoster(AppDb db, Access a, Guid tenant, Guid membership)
    {
        var chapters = await db.Set<InstitutionChapter>().Where(l => l.TenantId == tenant && l.MembershipId == membership).Select(l => l.ChapterId).ToListAsync();
        foreach(var chapter in chapters) if(await CanManage(db,a,tenant,chapter)) return true;
        return false;
    }
    public static void Map(RouteGroupBuilder api)
    {
        var t = api.MapGroup("/societies/{tenant:guid}/clubs").RequireAuthorization();
        t.MapGet("", async (Guid tenant, AppDb db, Access a) => {
            await a.Member(tenant);
            var clubs = await db.Units.Where(u => u.TenantId == tenant && db.Levels.Any(l => l.Id == u.LevelId && l.Kind == "Chapter")).OrderBy(u=>u.Name).ToListAsync();
            var result = new List<object>();
            foreach(var club in clubs) if(await CanManage(db,a,tenant,club.Id)) {
                var member = await (from l in db.Set<InstitutionChapter>() join m in db.Memberships on l.MembershipId equals m.Id where l.TenantId == tenant && l.ChapterId == club.Id select m).SingleOrDefaultAsync();
                result.Add(new { club.Id, club.Name, club.Code, club.Details, club.Revision, membershipId = member?.Id, active = member != null && MembershipExperience.Current(member) });
            }
            return Results.Ok(result);
        });
        t.MapPut("/{id:guid}/profile", async(Guid tenant, Guid id, ClubProfileWrite r, AppDb db, Access a) => {
            if(!await CanManage(db,a,tenant,id)) throw new AccessDeniedException();
            var club = await db.Units.SingleOrDefaultAsync(u=>u.TenantId==tenant && u.Id==id && db.Levels.Any(l=>l.Id==u.LevelId && l.Kind=="Chapter")) ?? throw new AccessDeniedException();
            if(club.Revision != r.Revision) return Results.Conflict(new {title="Club changed. Refresh before saving."});
            if(r.Description.Length>10000 || r.LogoUrl.Length>2000 || (r.LogoUrl!="" && !r.LogoUrl.StartsWith($"/api/v1/societies/{tenant}/assets/") && !(Uri.TryCreate(r.LogoUrl,UriKind.Absolute,out var url) && url.Scheme=="https" && string.IsNullOrEmpty(url.UserInfo)))) throw new DomainException("Use a valid logo URL and description");
            var details=JsonNode.Parse(club.Details)!.AsObject(); details["description"]=r.Description; details["logoUrl"]=r.LogoUrl;
            club.Details=details.ToJsonString(); club.Revision++; club.UpdatedAt=DateTimeOffset.UtcNow;
            a.Audit(tenant,"club.profile.updated",id); await db.SaveChangesAsync(); return Results.NoContent();
        });
    }
}
public record ClubProfileWrite(string Description, string LogoUrl, int Revision);
