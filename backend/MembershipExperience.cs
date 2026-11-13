using Microsoft.EntityFrameworkCore;

namespace Society.Api;

public static class MembershipExperience
{
    public static readonly string[] Kinds = ["Individual", "Institutional", "Corporate"];

    public static IQueryable<Membership> Owned(AppDb db, Guid user, bool administratorsOnly = false)
    {
        var now = DateTimeOffset.UtcNow;
        return db.Memberships.Where(m => db.Tenants.Any(t => t.Id == m.TenantId && t.Status != "Suspended") &&
            (db.Persons.Any(p => p.Id == m.PersonId && p.UserId == user) ||
             db.Set<MembershipRepresentative>().Any(r => r.TenantId == m.TenantId && r.MembershipId == m.Id && r.UserId == user && (!administratorsOnly || r.Role != "Coordinator") && r.RevokedAt == null && r.StartsAt <= now && (r.EndsAt == null || r.EndsAt > now))));
    }

    public static IQueryable<Membership> Entitled(AppDb db, Guid user)
    {
        var organizations = db.Set<OrganizationMember>().Where(r => r.RevokedAt == null && r.Email != null &&
            db.Users.Any(u => u.Id == user && u.EmailConfirmed && u.NormalizedEmail == r.Email)).Select(r => r.MembershipId);
        var owned = Owned(db, user).Select(x => x.Id);
        return db.Memberships.Where(m => owned.Contains(m.Id) ||
            organizations.Contains(m.Id) && db.Tenants.Any(t => t.Id == m.TenantId && t.Status != "Suspended"));
    }

    public static bool Current(Membership m) => m.Status == "Active" && !m.PendingPayment && m.EffectiveFrom <= DateTimeOffset.UtcNow && (m.EndsAt == null || m.EndsAt > DateTimeOffset.UtcNow);
    public static string State(Membership m) => m.Status is not ("Active" or "Inactive") ? m.Status : m.PendingPayment ? "Payment required" : m.Status != "Active" ? m.Status : m.EffectiveFrom > DateTimeOffset.UtcNow ? "Upcoming" : m.EndsAt <= DateTimeOffset.UtcNow ? "Expired" : m.EndsAt <= DateTimeOffset.UtcNow.AddDays(30) ? "Expiring soon" : "Active";

    public static async Task Prepare(AppDb db, Membership m)
    {
        var offer = await db.Set<MembershipOffer>().AsNoTracking().SingleOrDefaultAsync(o => o.TenantId == m.TenantId && o.CategoryId == m.CategoryId && o.Enabled);
        if (offer == null) return;
        m.RenewalReminders = MembershipService.Json(m.Profile)["renewalReminders"]?.GetValue<bool>() == true;
        if (m.EndsAt == null) m.EndsAt = m.EffectiveFrom.AddMonths(offer.TermMonths);
        if (offer.PaymentRequired && m.Status == "Active") { m.Status = "Inactive"; m.PendingPayment = true; }
    }

    public static async Task<Membership> Approve(AppDb db, Access access, MembershipService service, MemberApplication application)
    {
        var category = await db.MembershipCategories.SingleAsync(c => c.TenantId == application.TenantId && c.Id == application.CategoryId);
        Person person;
        if (category.Kind == "Individual") person = await service.PersonForAccount(application.UserId);
        else
        {
            var name = MembershipService.Json(application.Profile)["organizationName"]?.GetValue<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 160) throw new DomainException("Enter the institution or company name");
            person = new Person { FullName = name, SearchName = name.ToLowerInvariant() };
            db.Persons.Add(person);
        }
        var m = await service.Create(application.TenantId, person, application.ChapterId, application.CategoryId, null, application.Profile, access.UserId);
        if (category.Kind != "Individual") await AddRepresentative(db, m, application.UserId, "Controller");
        return m;
    }

    public static async Task AddRepresentative(AppDb db, Membership membership, Guid user, string role, DateTimeOffset? endsAt = null)
    {
        if (!await db.Links.AnyAsync(l => l.TenantId == membership.TenantId && l.UserId == user) && !db.Links.Local.Any(l => l.TenantId == membership.TenantId && l.UserId == user))
            db.Links.Add(new Link { TenantId = membership.TenantId, UserId = user });
        db.Add(new MembershipRepresentative { TenantId = membership.TenantId, MembershipId = membership.Id, UserId = user, Role = role, EndsAt = endsAt });
    }

    // Called in the existing tenant-serialized finance transaction after allocation.
    // Browser checkout callbacks never call this method or grant membership time.
    public static async Task ReconcileInvoice(AppDb db, FinanceInvoice invoice, Guid actor)
    {
        var renewal = await db.Set<MembershipRenewal>().SingleOrDefaultAsync(r => r.TenantId == invoice.TenantId && r.InvoiceId == invoice.Id);
        if (renewal == null) return;
        var member = await db.Memberships.SingleAsync(m => m.TenantId == renewal.TenantId && m.Id == renewal.MembershipId);
        var paid = invoice.Status == "Issued" && invoice.Total > 0 && invoice.Paid >= invoice.Total;
        if (paid && renewal.State is "Pending" or "Payment Reversed")
        {
            if (member.Status is not ("Active" or "Inactive") || member.Status == "Inactive" && !member.PendingPayment)
            {
                // A valid collection must still be posted even when an officer has
                // suspended membership. Payment cannot silently clear that hold.
                db.Add(new MembershipHistory { TenantId = member.TenantId, MembershipId = member.Id, ActorId = actor, Action = "membership.payment.review.required", Details = System.Text.Json.JsonSerializer.Serialize(new { renewal.InvoiceId, member.Status }) });
                return;
            }
            if (renewal.AppliedAt == null)
            {
                var now = DateTimeOffset.UtcNow;
                // Initial payment replaces the provisional unpaid term; early renewal
                // extends the existing paid term, while expired membership starts now.
                var start = member.PendingPayment ? now : member.EndsAt > now ? member.EndsAt.Value : now;
                renewal.StartsAt = start;
                renewal.EndsAt = start.AddMonths(renewal.TermMonths);
                renewal.AppliedAt = now;
                member.EndsAt = renewal.EndsAt;
                if (member.PendingPayment || member.EffectiveFrom > now) member.EffectiveFrom = now;
            }
            member.Status = "Active"; member.PendingPayment = false; member.Revision++;
            renewal.State = "Applied";
            await SyncTerm(db, member);
            db.Add(new MembershipHistory { TenantId = member.TenantId, MembershipId = member.Id, ActorId = actor, Action = "membership.payment.renewed", Details = System.Text.Json.JsonSerializer.Serialize(new { renewal.Id, renewal.InvoiceId, renewal.StartsAt, renewal.EndsAt }) });
        }
        else if (!paid && renewal.State == "Applied")
        {
            // Preserve every historical term and payment. A refund/chargeback blocks
            // the benefit until that invoice is paid again or staff reviews the hold.
            renewal.State = "Payment Reversed";
            member.PendingPayment = true; member.Revision++;
            if (member.Status == "Active") member.Status = "Inactive";
            db.Add(new MembershipHistory { TenantId = member.TenantId, MembershipId = member.Id, ActorId = actor, Action = "membership.payment.reversed", Details = System.Text.Json.JsonSerializer.Serialize(new { renewal.InvoiceId }) });
        }
    }

    public static async Task ReconcileApprovedMember(AppDb db, Membership member, Guid actor)
    {
        if (member.Status != "Active") return;
        var invoices = await (from r in db.Set<MembershipRenewal>() join i in db.Set<FinanceInvoice>() on r.InvoiceId equals i.Id
            where r.TenantId == member.TenantId && r.MembershipId == member.Id && (r.State == "Pending" || r.State == "Payment Reversed") && i.Status == "Issued" && i.Paid >= i.Total && i.Total > 0 select i).ToListAsync();
        foreach (var invoice in invoices) await ReconcileInvoice(db, invoice, actor);
    }

    static async Task SyncTerm(AppDb db, Membership member)
    {
        var assignments = await (from a in db.Assignments join role in db.GovernanceRoles on a.RoleId equals role.Id
            where a.TenantId == member.TenantId && a.MembershipId == member.Id && role.Name == "Member Directory" select a).ToListAsync();
        foreach (var assignment in assignments) { assignment.StartsAt = member.EffectiveFrom; assignment.EndsAt = member.EndsAt; }
    }

    public static async Task<MembershipRenewal> RequestRenewal(AppDb db, Access access, FinanceService finance, Guid tenant, Guid id)
    {
        var member = await db.Memberships.SingleOrDefaultAsync(m => m.TenantId == tenant && m.Id == id) ?? throw new AccessDeniedException();
        if (!await Owned(db, access.UserId, true).AnyAsync(m => m.Id == id && m.TenantId == tenant)) await access.Require(tenant, "members.billing", member.ChapterId);
        if (member.Status is not ("Active" or "Inactive") || member.Status == "Inactive" && !member.PendingPayment) throw new DomainException("This membership needs an officer's review before payment renewal");
        var pending = await db.Set<MembershipRenewal>().SingleOrDefaultAsync(r => r.TenantId == tenant && r.MembershipId == id && r.State == "Pending");
        if (pending != null)
        {
            var previous = await db.Set<FinanceInvoice>().SingleAsync(i => i.TenantId == tenant && i.Id == pending.InvoiceId);
            if (previous.Status is "Draft" or "Pending Approval" or "Issued") return pending;
            pending.State = "Cancelled";
            await db.SaveChangesAsync();
        }
        var reversed = await db.Set<MembershipRenewal>().Where(r => r.TenantId == tenant && r.MembershipId == id && r.State == "Payment Reversed").OrderByDescending(r => r.RequestedAt).FirstOrDefaultAsync();
        if (reversed != null) return reversed;
        var offer = await db.Set<MembershipOffer>().SingleOrDefaultAsync(o => o.TenantId == tenant && o.CategoryId == member.CategoryId && o.Enabled) ?? throw new DomainException("Renewal pricing is not configured");
        if (offer.Price == null || offer.Price <= 0 || offer.EntityId == null) throw new DomainException("The society must configure its renewal price and receiving entity before taking payment");
        var entity = await db.Set<FinancialEntity>().SingleOrDefaultAsync(e => e.TenantId == tenant && e.Id == offer.EntityId && e.Active && e.Currency == offer.Currency) ?? throw new DomainException("Renewal receiving entity is unavailable");
        if (!await db.Set<FinancialUnit>().AnyAsync(u => u.TenantId == tenant && u.EntityId == entity.Id && u.UnitId == member.ChapterId)) throw new DomainException("The receiving entity is not configured for this chapter");
        var invoice = await finance.CreateInvoice(tenant, new InvoiceWrite(entity.Id, member.ChapterId, "Membership renewal", FinanceService.Today, FinanceService.Today.AddDays(offer.ReminderDays),
            [new InvoiceLineInput($"Membership · {offer.TermMonths} months", 1, offer.Price.Value, AccountCode: offer.IncomeCode)], MembershipId: member.Id, Purpose: "Membership renewal", Submit: entity.RequireInvoiceApproval), true);
        var renewal = new MembershipRenewal { TenantId = tenant, MembershipId = id, InvoiceId = invoice.Id, RequestedBy = access.UserId, TermMonths = offer.TermMonths, Amount = offer.Price.Value, Currency = offer.Currency };
        db.Add(renewal);
        await db.SaveChangesAsync();
        if (!entity.RequireInvoiceApproval) await finance.Issue(invoice, entity);
        access.Audit(tenant, "membership.renewal.requested", renewal.Id, new { invoice.Id, offer.TermMonths, offer.Price });
        return renewal;
    }
}
