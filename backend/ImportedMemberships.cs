using Microsoft.EntityFrameworkCore;
namespace Society.Api;

public static class ImportedMemberships
{
    // Claim addresses live in immutable approved-registration history, never in editable profile fields.
    public static IQueryable<Membership> ForAccount(AppDb db, Guid user) => db.Memberships.FromSqlInterpolated($"""
        SELECT m.* FROM "Memberships" m WHERE EXISTS (
          SELECT 1 FROM "MembershipHistories" h JOIN "AspNetUsers" u ON u."Id"={user} AND u."EmailConfirmed"
          WHERE h."TenantId"=m."TenantId" AND h."MembershipId"=m."Id"
            AND h."Action" IN ('membership.import.approved','membership.manual.claim.approved') AND (h."Details"->'claimEmails') ? u."NormalizedEmail")
        """);

    public static IQueryable<Membership> ForEmail(AppDb db, string normalizedEmail) => db.Memberships.FromSqlInterpolated($"""
        SELECT m.* FROM "Memberships" m WHERE m."Status" <> 'Closed'
          AND EXISTS (SELECT 1 FROM "Tenants" t WHERE t."Id"=m."TenantId" AND t."Status"='Active')
          AND EXISTS (SELECT 1 FROM "MembershipHistories" h
            WHERE h."TenantId"=m."TenantId" AND h."MembershipId"=m."Id"
              AND h."Action" IN ('membership.import.approved','membership.manual.claim.approved')
              AND (h."Details"->'claimEmails') ? {normalizedEmail})
        """);

    public static async Task LinkAccount(AppDb db, Guid user)
    {
        var tenants = await MembershipExperience.Entitled(db, user).Where(m => db.Tenants.Any(t => t.Id == m.TenantId && t.Status == "Active"))
            .Select(m => m.TenantId).Distinct().ToListAsync();
        foreach (var tenant in tenants)
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Links\" (\"TenantId\",\"UserId\") VALUES ({tenant},{user}) ON CONFLICT DO NOTHING");
    }
}
