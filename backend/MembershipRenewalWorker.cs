using Microsoft.EntityFrameworkCore;

namespace Society.Api;

public class MembershipRenewalWorker(IServiceScopeFactory factory, ILogger<MembershipRenewalWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            try
            {
                using var scope = factory.CreateScope();
                await Sweep(scope.ServiceProvider.GetRequiredService<AppDb>(), scope.ServiceProvider.GetRequiredService<IConfiguration>(), stop);
            }
            catch (Exception ex) when (!stop.IsCancellationRequested) { logger.LogWarning("Membership renewal reminder pass failed: {Type}", ex.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromMinutes(5), stop); } catch (OperationCanceledException) { break; }
        }
    }
    public static async Task Sweep(AppDb db, IConfiguration config, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var candidates = await db.Memberships.AsNoTracking().Where(m => m.RenewalReminders && m.EndsAt != null && m.Status == "Active" && !m.PendingPayment && m.EndsAt <= now.AddDays(90) && m.EndsAt > now.AddDays(-30) && db.Tenants.Any(t => t.Id == m.TenantId && t.Status == "Active") &&
            !db.Set<MembershipRenewalNotice>().Any(n => n.TenantId == m.TenantId && n.MembershipId == m.Id && n.TermEnd == m.EndsAt)).OrderBy(m => m.EndsAt).Take(100).ToListAsync(ct);
        foreach (var candidate in candidates)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct); await MembershipService.Lock(db, candidate.TenantId);
            var m = await db.Memberships.SingleAsync(x => x.Id == candidate.Id, ct);
            var offer = await db.Set<MembershipOffer>().AsNoTracking().SingleOrDefaultAsync(o => o.TenantId == m.TenantId && o.CategoryId == m.CategoryId && o.Enabled, ct);
            if (!m.RenewalReminders || m.EndsAt == null || m.Status != "Active" || m.PendingPayment || m.EndsAt > now.AddDays(offer?.ReminderDays ?? 30) || await db.Set<MembershipRenewalNotice>().AnyAsync(n => n.TenantId == m.TenantId && n.MembershipId == m.Id && n.TermEnd == m.EndsAt, ct)) continue;
            var recipients = await db.Users.Where(u => u.EmailConfirmed && (db.Persons.Any(p => p.Id == m.PersonId && p.UserId == u.Id) || db.Set<MembershipRepresentative>().Any(r => r.TenantId == m.TenantId && r.MembershipId == m.Id && r.UserId == u.Id && r.Role != "Coordinator" && r.RevokedAt == null && r.StartsAt <= now && (r.EndsAt == null || r.EndsAt > now)))).Select(u => new { u.Id, u.Email }).ToListAsync(ct);
            if (recipients.Count == 0) continue;
            var society = await db.Tenants.Where(t => t.Id == m.TenantId).Select(t => t.Name).SingleAsync(ct);
            var notice = new MembershipRenewalNotice { TenantId = m.TenantId, MembershipId = m.Id, TermEnd = m.EndsAt.Value };
            foreach (var recipient in recipients) db.Outbox.Add(new Outbox { TenantId = m.TenantId, MembershipId = m.Id, UserId = recipient.Id, IdempotencyKey = "membership-renewal:" + notice.Id + ":" + recipient.Id, Recipient = recipient.Email!, Subject = society + " membership renewal reminder", Body = $"Your membership {m.Number} expires {m.EndsAt:yyyy-MM-dd}. Renew securely at {config["PublicUrl"]}/app/membership. Membership extends only after payment is confirmed. No automatic debit will be made." });
            db.Add(notice);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
    }
}
