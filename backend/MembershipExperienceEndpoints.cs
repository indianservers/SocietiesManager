using Microsoft.EntityFrameworkCore;

namespace Society.Api;

public static class MembershipExperienceEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        api.MapGet("/membership/portfolio", async (AppDb db, Access a) =>
        {
            var rows = await (from m in MembershipExperience.Owned(db, a.UserId)
                join c in db.MembershipCategories on m.CategoryId equals c.Id
                join p in db.Persons on m.PersonId equals p.Id
                join tenant in db.Tenants on m.TenantId equals tenant.Id
                join chapter in db.Units on m.ChapterId equals chapter.Id
                select new { member = m, c.Kind, category = c.Name, holder = p.FullName, society = tenant.Name, chapter = chapter.Name,
                    relationship = p.UserId == a.UserId ? "My membership" : db.Set<MembershipRepresentative>().Where(r => r.MembershipId == m.Id && r.UserId == a.UserId && r.RevokedAt == null).Select(r => r.Role).FirstOrDefault(),
                    offer = db.Set<MembershipOffer>().Where(o => o.TenantId == m.TenantId && o.CategoryId == m.CategoryId).SingleOrDefault() }).ToListAsync();
            return Results.Ok(rows.Select(r => new { r.member.Id, r.member.TenantId, r.member.Number, r.member.ChapterId, r.member.CategoryId, r.member.EndsAt, r.member.EffectiveFrom,
                r.member.RenewalReminders, r.Kind, r.category, r.holder, r.society, r.chapter, r.relationship, state = MembershipExperience.State(r.member), active = MembershipExperience.Current(r.member), r.offer }));
        }).RequireAuthorization();

        var t = api.MapGroup("/societies/{tenant:guid}/membership").RequireAuthorization();
        t.MapGet("/offers", async (Guid tenant, AppDb db, Access a) => { await a.Member(tenant); return await db.Set<MembershipOffer>().Where(o => o.TenantId == tenant).ToListAsync(); });
        t.MapPost("/members/{id:guid}/renew", async (Guid tenant, Guid id, AppDb db, Access a, FinanceService finance) =>
        {
            await a.Member(tenant);
            await using var tx = await db.Database.BeginTransactionAsync();
            await MembershipService.Lock(db, tenant);
            var renewal = await MembershipExperience.RequestRenewal(db, a, finance, tenant, id);
            await db.SaveChangesAsync(); await tx.CommitAsync();
            var invoice = await db.Set<FinanceInvoice>().SingleAsync(i => i.TenantId == tenant && i.Id == renewal.InvoiceId);
            return Results.Ok(new { renewal.Id, renewal.InvoiceId, renewal.State, invoiceStatus = invoice.Status, renewal.Amount, renewal.Currency, renewal.TermMonths });
        });
        t.MapPut("/members/{id:guid}/renewal-reminders", async (Guid tenant, Guid id, ReminderWrite r, AppDb db, Access a) =>
        {
            var member = await MembershipExperience.Owned(db, a.UserId, true).SingleOrDefaultAsync(m => m.TenantId == tenant && m.Id == id) ?? throw new AccessDeniedException();
            member.RenewalReminders = r.Enabled; member.Revision++;
            a.Audit(tenant, "membership.renewal.reminders", id, new { r.Enabled }); await db.SaveChangesAsync(); return Results.NoContent();
        });
        t.MapGet("/members/{id:guid}/renewals", async (Guid tenant, Guid id, AppDb db, Access a) =>
        {
            await ManageMember(db, a, tenant, id);
            return await db.Set<MembershipRenewal>().Where(r => r.TenantId == tenant && r.MembershipId == id).OrderByDescending(r => r.RequestedAt).Take(100).ToListAsync();
        });
        t.MapGet("/members/{id:guid}/representatives", async (Guid tenant, Guid id, AppDb db, Access a) =>
        {
            await ManageMember(db, a, tenant, id);
            return await (from r in db.Set<MembershipRepresentative>() join u in db.Users on r.UserId equals u.Id
                where r.TenantId == tenant && r.MembershipId == id && r.RevokedAt == null
                select new { r.Id, r.UserId, u.FullName, u.Email, r.Role, r.StartsAt, r.EndsAt }).ToListAsync();
        });
        t.MapPost("/members/{id:guid}/representatives", async (Guid tenant, Guid id, RepresentativeWrite r, AppDb db, Access a) =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(); await MembershipService.Lock(db, tenant);
            var member = await ManageMember(db, a, tenant, id, controller: true);
            if (!await db.MembershipCategories.AnyAsync(c => c.TenantId == tenant && c.Id == member.CategoryId && c.Kind != "Individual")) throw new DomainException("Representatives belong to institutional or corporate memberships");
            if (r.Role is not ("Controller" or "Admin" or "Coordinator") || r.EndsAt <= DateTimeOffset.UtcNow) throw new DomainException("Choose a valid representative role and future end date");
            if (r.Role == "Controller") await a.Require(tenant, "members.write", member.ChapterId);
            var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == r.Email.Trim().ToUpperInvariant() && u.EmailConfirmed) ?? throw new DomainException("The representative must first register and verify this email");
            if (await db.Set<MembershipRepresentative>().AnyAsync(x => x.TenantId == tenant && x.MembershipId == id && x.UserId == user.Id && x.RevokedAt == null)) throw new DomainException("This account already represents this membership");
            await MembershipExperience.AddRepresentative(db, member, user.Id, r.Role, r.EndsAt);
            a.Audit(tenant, "membership.representative.added", id, new { user.Id, r.Role }); await db.SaveChangesAsync(); await tx.CommitAsync(); return Results.Ok(new { user.Id });
        });
        t.MapDelete("/members/{id:guid}/representatives/{representative:guid}", async (Guid tenant, Guid id, Guid representative, AppDb db, Access a) =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(); await MembershipService.Lock(db, tenant);
            var member = await ManageMember(db, a, tenant, id, controller: true);
            var r = await db.Set<MembershipRepresentative>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.MembershipId == id && x.Id == representative && x.RevokedAt == null) ?? throw new AccessDeniedException();
            if (r.Role == "Controller") await a.Require(tenant, "members.write", member.ChapterId);
            if (r.Role == "Controller" && !await db.Set<MembershipRepresentative>().AnyAsync(x => x.TenantId == tenant && x.MembershipId == id && x.Id != representative && x.Role == "Controller" && x.RevokedAt == null && x.StartsAt <= DateTimeOffset.UtcNow && (x.EndsAt == null || x.EndsAt > DateTimeOffset.UtcNow))) throw new DomainException("Keep at least one current controller");
            r.RevokedAt = DateTimeOffset.UtcNow; a.Audit(tenant, "membership.representative.revoked", representative); await db.SaveChangesAsync(); await tx.CommitAsync(); return Results.NoContent();
        });
        t.MapGet("/members/{id:guid}/chapters", async (Guid tenant, Guid id, AppDb db, Access a) =>
        {
            await ManageMember(db, a, tenant, id);
            return await (from link in db.Set<InstitutionChapter>() join u in db.Units on link.ChapterId equals u.Id where link.TenantId == tenant && link.MembershipId == id select new { u.Id, u.Name, u.Code, u.Status }).ToListAsync();
        });
        t.MapPut("/members/{id:guid}/chapters/{chapter:guid}", async (Guid tenant, Guid id, Guid chapter, AppDb db, Access a) =>
        {
            await a.Require(tenant, "units.write", chapter); await a.Require(tenant, "members.write", chapter);
            var member = await db.Memberships.SingleOrDefaultAsync(m => m.TenantId == tenant && m.Id == id && db.MembershipCategories.Any(c => c.Id == m.CategoryId && c.Kind == "Institutional")) ?? throw new DomainException("Choose an institutional membership in this society");
            await a.Require(tenant, "members.read", member.ChapterId);
            if (!await db.Units.AnyAsync(u => u.TenantId == tenant && u.Id == chapter && db.Levels.Any(l => l.Id == u.LevelId && l.Kind == "Chapter"))) throw new DomainException("Choose a chapter in this society");
            var link = await db.Set<InstitutionChapter>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.ChapterId == chapter);
            if (link == null) db.Add(new InstitutionChapter { TenantId = tenant, ChapterId = chapter, MembershipId = id }); else link.MembershipId = id;
            a.Audit(tenant, "institution.chapter.linked", chapter, new { membershipId = id }); await db.SaveChangesAsync(); return Results.NoContent();
        });

        t.MapGet("/resources", async (Guid tenant, AppDb db, Access a) =>
        {
            await a.Member(tenant);
            var now = DateTimeOffset.UtcNow;
            var kinds = await (from m in MembershipExperience.Owned(db, a.UserId) join c in db.MembershipCategories on m.CategoryId equals c.Id
                where m.TenantId == tenant && m.Status == "Active" && !m.PendingPayment && m.EffectiveFrom <= now && (m.EndsAt == null || m.EndsAt > now) select c.Kind).Distinct().ToArrayAsync();
            return await db.Set<MemberResource>().Where(r => r.TenantId == tenant && r.Published && r.Audiences.Any(k => kinds.Contains(k)))
                .OrderBy(r => r.Title).Select(r => new { r.Id, r.Title, r.Description, r.Kind, r.Audiences }).Take(200).ToListAsync();
        });
        t.MapGet("/resources/{id:guid}", async (Guid tenant, Guid id, AppDb db, Access a) =>
        {
            await a.Member(tenant);
            var resource = await db.Set<MemberResource>().SingleOrDefaultAsync(r => r.TenantId == tenant && r.Id == id && r.Published) ?? throw new AccessDeniedException();
            var now = DateTimeOffset.UtcNow;
            if (!await (from m in MembershipExperience.Owned(db, a.UserId) join c in db.MembershipCategories on m.CategoryId equals c.Id
                where m.TenantId == tenant && m.Status == "Active" && !m.PendingPayment && m.EffectiveFrom <= now && (m.EndsAt == null || m.EndsAt > now) && resource.Audiences.Contains(c.Kind) select m.Id).AnyAsync()) throw new AccessDeniedException();
            return Results.Ok(resource);
        });
        t.MapGet("/resource-library", async (Guid tenant, AppDb db, Access a) => { await a.Require(tenant, "membership.settings"); return await db.Set<MemberResource>().Where(r => r.TenantId == tenant).OrderBy(r => r.Title).Take(200).ToListAsync(); });
        t.MapPost("/resource-library", async (Guid tenant, ResourceWrite r, AppDb db, Access a) =>
        {
            await a.Require(tenant, "membership.settings"); ValidateResource(r);
            var resource = new MemberResource { TenantId = tenant }; SetResource(resource, r); db.Add(resource); a.Audit(tenant, "member.resource.created", resource.Id); await db.SaveChangesAsync(); return Results.Ok(resource);
        });
        t.MapPut("/resource-library/{id:guid}", async (Guid tenant, Guid id, ResourceWrite r, AppDb db, Access a) =>
        {
            await a.Require(tenant, "membership.settings"); ValidateResource(r);
            var resource = await db.Set<MemberResource>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.Id == id) ?? throw new AccessDeniedException();
            if (resource.Revision != r.Revision) return Results.Conflict(); SetResource(resource, r); resource.Revision++; a.Audit(tenant, "member.resource.updated", id); await db.SaveChangesAsync(); return Results.Ok(resource);
        });

        var platform = api.MapGroup("/platform/membership-pricing").RequireAuthorization();
        platform.MapGet("/{tenant:guid}", async (Guid tenant, AppDb db, Access a) =>
        {
            await a.RequirePlatform();
            return Results.Ok(new { categories = await db.MembershipCategories.Where(c => c.TenantId == tenant).OrderBy(c => c.Name).Select(c => new { c.Id, c.Name, c.Kind, c.Active, offer = db.Set<MembershipOffer>().Where(o => o.TenantId == tenant && o.CategoryId == c.Id).SingleOrDefault() }).ToListAsync(),
                entities = await db.Set<FinancialEntity>().Where(e => e.TenantId == tenant && e.Active).Select(e => new { e.Id, e.Name, e.Currency }).ToListAsync() });
        });
        platform.MapPut("/{tenant:guid}/{category:guid}", async (Guid tenant, Guid category, MembershipOffer r, AppDb db, Access a) =>
        {
            await a.RequirePlatform();
            if (r.TermMonths is < 1 or > 120 || r.ReminderDays is < 1 or > 90 || r.IncomeCode.Length is < 1 or > 20 || !FinanceService.Currencies.ContainsKey(r.Currency)) throw new DomainException("Check renewal period, currency, income account and reminder days");
            if (r.Price != null) FinanceService.Money(r.Price.Value, r.Currency);
            if (!await db.MembershipCategories.AnyAsync(c => c.TenantId == tenant && c.Id == category)) throw new DomainException("Choose a category in this society");
            if (r.EntityId != null && !await db.Set<FinancialEntity>().AnyAsync(e => e.TenantId == tenant && e.Id == r.EntityId && e.Active && e.Currency == r.Currency)) throw new DomainException("Receiving entity and currency must belong to this society");
            var offer = await db.Set<MembershipOffer>().SingleOrDefaultAsync(o => o.TenantId == tenant && o.CategoryId == category);
            if (offer == null) { offer = new MembershipOffer { TenantId = tenant, CategoryId = category }; db.Add(offer); }
            else if (offer.Revision != r.Revision) return Results.Conflict();
            offer.Enabled = r.Enabled; offer.TermMonths = r.TermMonths; offer.Price = r.Price; offer.EntityId = r.EntityId; offer.Currency = r.Currency; offer.IncomeCode = r.IncomeCode;
            offer.PaymentRequired = r.PaymentRequired; offer.ReminderDays = r.ReminderDays; offer.Revision++;
            a.Audit(tenant, "membership.pricing.updated", category, new { offer.Price, offer.Currency, offer.TermMonths }); await db.SaveChangesAsync(); return Results.Ok(offer);
        });
    }
    static async Task<Membership> ManageMember(AppDb db, Access a, Guid tenant, Guid id, bool controller = false)
    {
        await a.Member(tenant);
        var member = await db.Memberships.SingleOrDefaultAsync(m => m.TenantId == tenant && m.Id == id) ?? throw new AccessDeniedException();
        if (controller)
        {
            var now = DateTimeOffset.UtcNow;
            if (await db.Set<MembershipRepresentative>().AnyAsync(r => r.TenantId == tenant && r.MembershipId == id && r.UserId == a.UserId && r.Role == "Controller" && r.RevokedAt == null && r.StartsAt <= now && (r.EndsAt == null || r.EndsAt > now))) return member;
            await a.Require(tenant, "members.write", member.ChapterId);
        }
        else if (!await MembershipExperience.Owned(db, a.UserId, true).AnyAsync(m => m.TenantId == tenant && m.Id == id)) await a.Require(tenant, "members.read", member.ChapterId);
        return member;
    }
    static void ValidateResource(ResourceWrite r)
    {
        if (string.IsNullOrWhiteSpace(r.Title) || r.Kind is not ("Guide" or "Video" or "Download" or "Link" or "Tool") || r.Audiences.Length is < 1 or > 3 || r.Audiences.Except(MembershipExperience.Kinds).Any()) throw new DomainException("Choose a title, resource type and at least one membership audience");
        if (r.Url != "" && (!Uri.TryCreate(r.Url, UriKind.Absolute, out var url) || url.Scheme != "https" || !string.IsNullOrEmpty(url.UserInfo))) throw new DomainException("Resource links must use HTTPS without embedded credentials");
        if (r.Content.Trim() == "" && r.Url == "") throw new DomainException("Add resource content or an HTTPS link");
    }
    static void SetResource(MemberResource resource, ResourceWrite r) { resource.Title = r.Title.Trim(); resource.Description = r.Description; resource.Kind = r.Kind; resource.Audiences = r.Audiences.Distinct().ToArray(); resource.Content = r.Content; resource.Url = r.Url; resource.Published = r.Published; }
}
public record ReminderWrite(bool Enabled);
