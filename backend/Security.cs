using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
namespace Society.Api;

public class Access(AppDb db, IHttpContextAccessor accessor)
{
    public Guid UserId => Guid.Parse(accessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public async Task<bool> Platform() => await db.Users.AnyAsync(x => x.Id == UserId && x.PlatformAdmin);
    public async Task RequirePlatform() { if (!await Platform()) throw new AccessDeniedException(); }
    public async Task Member(Guid tenant) { if (!await db.Links.AnyAsync(x => x.TenantId == tenant && x.UserId == UserId && db.Tenants.Any(t => t.Id == tenant && t.Status != "Suspended"))) throw new AccessDeniedException(); }
    public IQueryable<Unit> Visible(Guid tenant, string permission = "units.read")
    {
        return UnitsFor(db, tenant, UserId, permission);
    }
    public static IQueryable<Unit> UnitsFor(AppDb db, Guid tenant, Guid user, string permission)
    {
        var now = DateTimeOffset.UtcNow;
        return db.Units.FromSqlInterpolated($"""
 WITH RECURSIVE permitted AS (
 SELECT u.* FROM "Units" u JOIN "Assignments" a ON a."TenantId"=u."TenantId" AND (a."UnitId" IS NULL OR a."UnitId"=u."Id") JOIN "GovernanceRoles" r ON r."TenantId"=a."TenantId" AND r."Id"=a."RoleId"
 WHERE u."TenantId"={tenant} AND a."UserId"={user} AND a."RevokedAt" IS NULL AND a."StartsAt"<={now} AND (a."EndsAt" IS NULL OR a."EndsAt">{now}) AND {permission}=ANY(r."Permissions") AND (a."MembershipId" IS NULL OR EXISTS(SELECT 1 FROM "Memberships" m WHERE m."Id"=a."MembershipId" AND m."TenantId"=a."TenantId" AND m."Status"='Active' AND m."EffectiveFrom"<={now} AND (m."EndsAt" IS NULL OR m."EndsAt">{now})))
 UNION SELECT child.* FROM "Units" child JOIN permitted p ON child."ParentId"=p."Id" AND child."TenantId"=p."TenantId"
 ) SELECT * FROM permitted
 """).AsNoTracking();
    }
    public IQueryable<Unit> InWorkspace(IQueryable<Unit> query, Guid tenant, Guid? workspace) { if (workspace == null) return query; var branch = db.Units.FromSqlInterpolated($"""
 WITH RECURSIVE branch AS (SELECT * FROM "Units" WHERE "TenantId"={tenant} AND "Id"={workspace} UNION ALL SELECT u.* FROM "Units" u JOIN branch b ON u."ParentId"=b."Id" WHERE u."TenantId"={tenant}) SELECT * FROM branch
 """).Select(x => x.Id); return query.Where(x => branch.Contains(x.Id)); }
    public async Task<bool> Can(Guid tenant, string permission, Guid? unit = null)
    {
        await Member(tenant); var now = DateTimeOffset.UtcNow;
        var grants = from a in db.Assignments join r in db.GovernanceRoles on a.RoleId equals r.Id where a.TenantId == tenant && a.UserId == UserId && a.RevokedAt == null && a.StartsAt <= now && (a.EndsAt == null || a.EndsAt > now) && r.Permissions.Contains(permission) && (a.MembershipId == null || db.Memberships.Any(m => m.Id == a.MembershipId && m.TenantId == tenant && m.Status == "Active" && m.EffectiveFrom <= now && (m.EndsAt == null || m.EndsAt > now))) select a;
        if (await grants.AnyAsync(a => a.UnitId == null)) return true;
        return unit != null && await Visible(tenant, permission).AnyAsync(x => x.Id == unit);
    }
    public async Task Require(Guid tenant, string permission, Guid? unit = null) { if (!await Can(tenant, permission, unit)) throw new AccessDeniedException(); }
    public void Audit(Guid tenant, string action, Guid resource, object? detail = null) => db.Audits.Add(new Audit { TenantId = tenant, ActorId = UserId, Action = action, ResourceId = resource, Details = System.Text.Json.JsonSerializer.Serialize(detail ?? new { }) });
    public static readonly string[] Permissions = ["units.read", "units.write", "hierarchy.manage", "roles.manage", "settings.manage", "audit.read", "directory.chapter", "directory.region", "directory.society", "members.admin", "members.read", "members.write", "members.approve", "members.billing", "members.export", "members.transfer", "members.import", "membership.settings", "governance.manage", "committees.manage", "activities.read", "activities.official", "activities.broadcast", "activities.approve", "activities.moderate", "activities.settings", "activities.analytics", "events.manage", "events.approve", "events.invite", "events.checkin", "events.analytics"];
}
public class AccessDeniedException : Exception;
public class DomainException(string message) : Exception(message);
