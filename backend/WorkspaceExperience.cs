using Microsoft.EntityFrameworkCore;
namespace Society.Api;

public static class WorkspaceExperience
{
    public static void Map(RouteGroupBuilder api)
    {
        api.MapGet("/societies/{tenant:guid}/workspace/attention", async (Guid tenant, Guid? workspace, AppDb db, Access access, MembershipService membership, FinanceService finance, HttpContext context) =>
        {
            var platform = await access.Platform();
            if (!platform) await access.Member(tenant);
            if (!await db.Tenants.AnyAsync(t => t.Id == tenant && t.Status != "Suspended")) throw new AccessDeniedException();
            context.Response.Headers.CacheControl = "no-store";
            var items = new List<object>();
            var reviewUnits = access.InWorkspace(access.Visible(tenant, "members.approve"), tenant, workspace).Select(u => u.Id);
            var writeUnits = access.InWorkspace(access.Visible(tenant, "members.write"), tenant, workspace).Select(u => u.Id);
            // Counts use the same original permissions and chapter boundary as the review queue.
            var applications = await db.MemberApplications.CountAsync(a => a.TenantId == tenant && (reviewUnits.Contains(a.ChapterId) || writeUnits.Contains(a.ChapterId)) && (a.Status == "Submitted" || a.Status == "Under Review"));
            if (applications > 0) items.Add(new { label = applications + " applications awaiting review", text = "Open the current membership application queue", to = "/app/members/operations?view=applications" });
            var canReadMembers = platform || await access.Visible(tenant, "members.read").AnyAsync() || await access.Visible(tenant, "members.admin").AnyAsync();
            if (canReadMembers)
            {
                var members = (await membership.Directory(tenant)).Where(m => !EF.Functions.JsonContains(m.Profile, "{\"adminArchived\":true}") && m.Status == "Active" && !m.PendingPayment);
                if (workspace != null)
                {
                    var branch = access.InWorkspace(db.Units.Where(u => u.TenantId == tenant), tenant, workspace).Select(u => u.Id);
                    members = members.Where(m => branch.Contains(m.ChapterId));
                }
                var now = DateTimeOffset.UtcNow;
                var soon = now.AddDays(30);
                foreach (var kind in new[] { "Individual", "Institutional", "Corporate" })
                {
                    var selected = MembershipIdentity.Filter(db, members, kind);
                    var expired = await selected.CountAsync(m => m.EndsAt != null && m.EndsAt <= now);
                    var expiring = await selected.CountAsync(m => m.EffectiveFrom <= now && m.EndsAt > now && m.EndsAt <= soon);
                    if (expired > 0) items.Add(new { label = expired + " expired " + kind.ToLowerInvariant() + " memberships", text = "Review the expired register", to = "/app/members?view=directory&memberKind=" + kind + "&validity=Expired" });
                    if (expiring > 0) items.Add(new { label = expiring + " " + kind.ToLowerInvariant() + " memberships expire within 30 days", text = "Review upcoming renewals", to = "/app/members?view=directory&memberKind=" + kind + "&validity=Expiring" });
                }
            }
            var overdue = await PlatformPolicy.WorkAllowed(db, tenant, "finance") ? await finance.Scoped<FinanceInvoice>(tenant, workspace: workspace).CountAsync(i => i.Status == "Issued" && i.Kind == "Invoice" && i.DueOn < FinanceService.Today && i.Total > i.Paid + i.Credited) : 0;
            if (overdue > 0) items.Add(new { label = overdue + " overdue invoices", text = "Open outstanding collections", to = "/app/finance/billing?view=invoices&status=Overdue" });
            return Results.Ok(new { items, updatedAt = DateTimeOffset.UtcNow });
        }).RequireAuthorization();
    }
}
