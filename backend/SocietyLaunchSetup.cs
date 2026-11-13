using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace Society.Api;

// Explicit operator command: never runs at startup and never changes existing
// account passwords, paid records, or owner-edited renewal prices on rerun.
public static class SocietyLaunchSetup
{
    public static async Task Initialize(IServiceProvider services, IConfiguration configuration, string configFile, Guid tenant)
    {
        var launch = JsonNode.Parse(await File.ReadAllTextAsync(configFile))!.AsObject();
        var db = services.GetRequiredService<AppDb>();
        var users = services.GetRequiredService<UserManager<Account>>();
        var owner = await users.FindByEmailAsync(launch["platformOwnerEmail"]!.GetValue<string>()) ?? throw new DomainException("Bootstrap the designated platform owner before initialization");
        if (!owner.PlatformAdmin) throw new DomainException("Initialization requires the designated platform owner account");
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await MembershipService.Lock(db, tenant);
            if (!await db.Tenants.AnyAsync(t => t.Id == tenant))
            {
                var name = launch["name"]!.GetValue<string>();
                db.Tenants.Add(new Tenant { Id = tenant, Name = name, Status = "Active", Profile = new JsonObject { ["displayName"] = name, ["country"] = launch["country"]!.GetValue<string>() }.ToJsonString() });
                db.Links.Add(new Link { TenantId = tenant, UserId = owner.Id });
                var admin = new GovernanceRole { TenantId = tenant, Name = "Society Administrator", Permissions = Access.Permissions };
                db.GovernanceRoles.Add(admin); db.GovernanceRoles.AddRange(RoleTemplates.For(tenant));
                db.Assignments.Add(new Assignment { TenantId = tenant, UserId = owner.Id, RoleId = admin.Id });
                db.Onboardings.Add(new Onboarding { TenantId = tenant, Step = 7, Complete = true });
                var regionLevel = new Level { TenantId = tenant, Name = "Region", Kind = "Region", AllowRoot = true };
                var chapterLevel = new Level { TenantId = tenant, Name = "Chapter", Kind = "Chapter", AllowedParents = [regionLevel.Id] };
                db.Levels.AddRange(regionLevel, chapterLevel);
                var region = new Unit { TenantId = tenant, Name = launch["country"]!.GetValue<string>(), Code = "NATIONAL", LevelId = regionLevel.Id, Status = "Active" };
                db.Units.Add(region);
                var index = 0;
                foreach (var city in launch["chapters"]!.AsArray())
                    db.Units.Add(new Unit { TenantId = tenant, Name = city!.GetValue<string>() + " Chapter", Code = "CH-" + (++index).ToString("D2"), LevelId = chapterLevel.Id, ParentId = region.Id, Status = "Active", Details = new JsonObject { ["city"] = city.GetValue<string>(), ["country"] = launch["country"]!.GetValue<string>() }.ToJsonString() });
                await MembershipSeed.Ensure(db, tenant);
                db.Audits.Add(new Audit { TenantId = tenant, ActorId = owner.Id, Action = "society.launch.initialized", ResourceId = tenant, Details = "{}" });
                await db.SaveChangesAsync();
            }
            await tx.CommitAsync();
        }
        await Configure(services, configuration, configFile, tenant);
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await MembershipService.Lock(db, tenant);
            var entity = await db.Set<FinancialEntity>().FirstOrDefaultAsync(e => e.TenantId == tenant && e.Active && e.Currency == "INR");
            if (entity == null)
            {
                var units = await db.Units.Where(u => u.TenantId == tenant && u.Status == "Active").Select(u => u.Id).ToArrayAsync();
                var region = await db.Units.Where(u => u.TenantId == tenant && u.ParentId == null).Select(u => u.Id).FirstAsync();
                // Explicit operator initialization uses the already-authorized owner
                // context. Web sessions retain all production MFA requirements.
                var context = FinanceWorker.Context(db, owner.Id, configuration);
                entity = await context.service.CreateEntity(tenant, new FinanceEntityWrite(launch["name"]!.GetValue<string>(), launch["name"]!.GetValue<string>(), region, Units: units, RequireInvoiceApproval: false));
            }
            foreach (var offer in await db.Set<MembershipOffer>().Where(o => o.TenantId == tenant && o.EntityId == null && o.Currency == entity.Currency).ToListAsync()) { offer.EntityId = entity.Id; offer.Revision++; }
            await db.SaveChangesAsync(); await tx.CommitAsync();
        }
        Console.WriteLine("Production society initialized with configured chapters, pricing and receiving entity. No synthetic members or financial transactions were created.");
    }

    public static async Task Configure(IServiceProvider services, IConfiguration configuration, string configFile, Guid tenant)
    {
        var launch = JsonNode.Parse(await File.ReadAllTextAsync(configFile))!.AsObject();
        var db = services.GetRequiredService<AppDb>();
        var users = services.GetRequiredService<UserManager<Account>>();
        var society = await db.Tenants.SingleAsync(t => t.Id == tenant);
        var ownerEmail = launch["platformOwnerEmail"]!.GetValue<string>();
        var host = PlatformPolicy.Host(launch["launchDomain"]!.GetValue<string>());
        var owner = await users.FindByEmailAsync(ownerEmail);
        string? password = null;
        if (owner == null)
        {
            password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "!aA1";
            owner = new Account { Id = Guid.NewGuid(), Email = ownerEmail, UserName = ownerEmail, FullName = "Platform Owner", PlatformAdmin = true, SignupTenantId = tenant };
            var created = await users.CreateAsync(owner, password);
            if (!created.Succeeded) throw new DomainException(string.Join("; ", created.Errors.Select(e => e.Description)));
        }
        else if (!owner.PlatformAdmin) { owner.PlatformAdmin = true; var updated = await users.UpdateAsync(owner); if (!updated.Succeeded) throw new DomainException("Platform owner assignment failed"); }
        if (!owner.EmailConfirmed && !await db.Outbox.AnyAsync(o => o.Recipient == ownerEmail && o.Subject == "Verify your platform owner email"))
        {
            var token = await users.GenerateEmailConfirmationTokenAsync(owner);
            db.Outbox.Add(new Outbox { Recipient = ownerEmail, Subject = "Verify your platform owner email", Body = $"{configuration["PublicUrl"]}/verify?userId={owner.Id}&token={Uri.EscapeDataString(token)}" });
        }
        var brand = await db.Set<TenantBrand>().SingleAsync(b => b.TenantId == tenant);
        brand.DisplayName = launch["name"]!.GetValue<string>(); brand.LoginTitle = "Welcome to " + brand.DisplayName; brand.Revision++;
        var profile = MembershipService.Json(society.Profile); profile["displayName"] = brand.DisplayName; society.Profile = profile.ToJsonString();
        foreach (var group in launch["memberships"]!.AsArray())
        {
            var name = group!["category"]!.GetValue<string>(); var kind = group["kind"]!.GetValue<string>();
            if (!MembershipExperience.Kinds.Contains(kind)) throw new DomainException("Unknown launch membership kind");
            var category = await db.MembershipCategories.SingleOrDefaultAsync(c => c.TenantId == tenant && c.Name == name);
            if (category == null) { category = new MembershipCategory { TenantId = tenant, Name = name, Kind = kind }; db.Add(category); }
            if (kind != "Individual" && await db.Memberships.AnyAsync(m => m.TenantId == tenant && m.CategoryId == category.Id && db.Persons.Any(p => p.Id == m.PersonId && p.UserId != null))) throw new DomainException("An existing personal identity cannot be converted into an organization. Create a separate organization membership.");
            category.Kind = kind;
            if (!await db.Set<MembershipOffer>().AnyAsync(o => o.TenantId == tenant && o.CategoryId == category.Id))
            {
                var amount = group["annualPrice"]!.GetValue<decimal>(); FinanceService.Money(amount, "INR");
                db.Add(new MembershipOffer { TenantId = tenant, CategoryId = category.Id, Enabled = true, Price = amount, Currency = "INR", TermMonths = 12, PaymentRequired = true });
            }
            var resourceTitle = kind + " member welcome guide";
            if (!await db.Set<MemberResource>().AnyAsync(r => r.TenantId == tenant && r.Title == resourceTitle))
                db.Add(new MemberResource { TenantId = tenant, Title = resourceTitle, Description = "Get started with your membership, chapter activities and renewal.", Kind = "Guide", Audiences = [kind], Published = true,
                    Content = kind == "Individual" ? "Use Membership & resources to check your validity, access member resources and renew securely. Join chapter activities from Activities. Your membership is personal; approved chapter roles and organization responsibilities appear separately. Switch between your personal and organization memberships without creating another login." : "Use your organization's membership card to review validity and renewal. A verified controller can add organization administrators and coordinators. These responsibilities do not grant society or chapter governance permissions. Your organization may have linked institutional chapters; society officers approve and assign chapter leadership separately. Representatives can retain their own Individual memberships." });
        }
        var domain = await db.Set<TenantDomain>().SingleOrDefaultAsync(d => d.Host == host);
        if (domain != null && domain.TenantId != tenant) throw new DomainException("The launch hostname is claimed by another society");
        if (domain == null) { domain = new TenantDomain { TenantId = tenant, Host = host, Challenge = CommunityService.Token() }; db.Add(domain); }
        db.Audits.Add(new Audit { TenantId = tenant, ActorId = owner.Id, Action = "society.launch.configured", ResourceId = tenant, Details = System.Text.Json.JsonSerializer.Serialize(new { host, ownerEmail, owner.EmailConfirmed }) });
        await db.SaveChangesAsync();
        var output = configuration["Society:LaunchOutput"];
        if (!string.IsNullOrEmpty(output))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            if (password == null && File.Exists(output)) { var prior = JsonNode.Parse(await File.ReadAllTextAsync(output)); if (prior?["ownerEmail"]?.ToString() == ownerEmail) password = prior?["initialPassword"]?.GetValue<string>(); }
            await File.WriteAllTextAsync(output, System.Text.Json.JsonSerializer.Serialize(new { ownerEmail, initialPassword = password, requiresEmailVerification = !owner.EmailConfirmed, tenant, host, txtName = "_assemblia." + host, txtValue = "assemblia-verification=" + domain.Challenge, domain.Status }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        Console.WriteLine("Society launch settings configured. Verify owner email and domain ownership; receiving entity, provider, SMTP, hosting and TLS remain explicit setup steps.");
    }
}
