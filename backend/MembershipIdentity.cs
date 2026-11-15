using Microsoft.EntityFrameworkCore;
namespace Society.Api;

public static class MembershipIdentity
{
    public static IQueryable<Membership> Filter(AppDb db, IQueryable<Membership> query, string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind) || kind == "All") return query;
        if (kind == "Organizations") return query.Where(m => db.MembershipCategories.Any(c => c.TenantId == m.TenantId && c.Id == m.CategoryId && c.Kind != "Individual"));
        if (!MembershipExperience.Kinds.Contains(kind)) throw new DomainException("Choose Individual, Institutional or Corporate membership");
        return query.Where(m => db.MembershipCategories.Any(c => c.TenantId == m.TenantId && c.Id == m.CategoryId && c.Kind == kind));
    }
    public static async Task RequireIndividual(AppDb db, Guid tenant, Guid id)
    {
        if (!await Filter(db, db.Memberships.Where(m => m.TenantId == tenant && m.Id == id), "Individual").AnyAsync())
            throw new DomainException("Choose an individual member (a person). Institutional and corporate memberships represent organizations and cannot hold a human role. Organization contacts need their own individual membership.");
    }
}
