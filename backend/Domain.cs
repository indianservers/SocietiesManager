using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
namespace Society.Api;

public class Account : IdentityUser<Guid> { public string FullName { get; set; } = ""; public bool PlatformAdmin { get; set; } }
public class Tenant { public Guid Id { get; set; } = Guid.NewGuid(); public string Name { get; set; } = ""; public string Status { get; set; } = "Setup"; public string Profile { get; set; } = "{}"; public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; }
public class Application { public Guid Id { get; set; } = Guid.NewGuid(); public Guid ApplicantId { get; set; } public string Name { get; set; } = ""; public string Fingerprint { get; set; } = ""; public string Details { get; set; } = "{}"; public string Status { get; set; } = "Pending"; public string? ReviewNote { get; set; } public Guid? TenantId { get; set; } public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; }
public class Link { public Guid TenantId { get; set; } public Guid UserId { get; set; } }
public class Level { public Guid Id { get; set; } = Guid.NewGuid(); public Guid TenantId { get; set; } public string Name { get; set; } = ""; public string Kind { get; set; } = "Other"; public Guid[] AllowedParents { get; set; } = []; public bool AllowRoot { get; set; } }
public class Unit { public Guid Id { get; set; } = Guid.NewGuid(); public Guid TenantId { get; set; } public Guid? ParentId { get; set; } public Guid LevelId { get; set; } public string Name { get; set; } = ""; public string Code { get; set; } = ""; public string Status { get; set; } = "Proposed"; public string Details { get; set; } = "{}"; public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow; public int Revision { get; set; } }
public class GovernanceRole { public Guid Id { get; set; } = Guid.NewGuid(); public Guid TenantId { get; set; } public string Name { get; set; } = ""; public string[] Permissions { get; set; } = []; public string Responsibilities { get; set; } = ""; }
public class Assignment { public DateTimeOffset? RevokedAt { get; set; } public Guid? MembershipId { get; set; } public Guid Id { get; set; } = Guid.NewGuid(); public Guid TenantId { get; set; } public Guid UserId { get; set; } public Guid RoleId { get; set; } public Guid? UnitId { get; set; } public DateTimeOffset StartsAt { get; set; } = DateTimeOffset.UtcNow; public DateTimeOffset? EndsAt { get; set; } }
public class Onboarding { public Guid TenantId { get; set; } public int Step { get; set; } public string Data { get; set; } = "{}"; public bool Complete { get; set; } }
public class Audit { public Guid Id { get; set; } = Guid.NewGuid(); public Guid TenantId { get; set; } public Guid ActorId { get; set; } public string Action { get; set; } = ""; public Guid ResourceId { get; set; } public string Details { get; set; } = "{}"; public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow; }
public class Asset { public Guid Id { get; set; } = Guid.NewGuid(); public Guid TenantId { get; set; } public long Length { get; set; } public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; }
public class Invitation { public Guid Id { get; set; } = Guid.NewGuid(); public Guid TenantId { get; set; } public string Email { get; set; } = ""; public string TokenHash { get; set; } = ""; public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddDays(7); public DateTimeOffset? AcceptedAt { get; set; } }
public class Outbox { public Guid? TenantId {get;set;} public Guid? UnitId {get;set;} public Guid? UserId {get;set;} public Guid? MembershipId {get;set;} public Guid? AccountId {get;set;} public Guid? CampaignId {get;set;} public Guid? ConversationId {get;set;} public Guid? MessageId {get;set;} public string Channel {get;set;}="Email"; public string Status {get;set;}="Queued"; public string? IdempotencyKey {get;set;} public DateTimeOffset NextAttemptAt {get;set;}=DateTimeOffset.UtcNow; public DateTimeOffset? LeaseUntil {get;set;} public string? LeaseOwner {get;set;} public string? ProviderId {get;set;} public string? Error {get;set;} public DateTimeOffset? DeliveredAt {get;set;} public DateTimeOffset? ReadAt {get;set;} public string? UnsubscribeHash {get;set;} public string? TemplateName {get;set;} public string? TemplateLanguage {get;set;} public Guid Id { get; set; } = Guid.NewGuid(); public string Recipient { get; set; } = ""; public string Subject { get; set; } = ""; public string Body { get; set; } = ""; public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; public DateTimeOffset? SentAt { get; set; } public int Attempts { get; set; } }
public partial class AppDb(DbContextOptions<AppDb> options) : IdentityDbContext<Account, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Tenant> Tenants => Set<Tenant>(); public DbSet<Application> Applications => Set<Application>(); public DbSet<Link> Links => Set<Link>(); public DbSet<Level> Levels => Set<Level>(); public DbSet<Unit> Units => Set<Unit>(); public DbSet<GovernanceRole> GovernanceRoles => Set<GovernanceRole>(); public DbSet<Assignment> Assignments => Set<Assignment>(); public DbSet<Onboarding> Onboardings => Set<Onboarding>(); public DbSet<Audit> Audits => Set<Audit>(); public DbSet<Outbox> Outbox => Set<Outbox>();
    [DbFunction("descendant_chapter_count")] public static int ChapterCount(Guid tenant, Guid unit) => throw new NotSupportedException();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        MembershipModel.Configure(b); CommunityModel.Configure(b); CommunicationModel.Configure(b);
        b.Entity<Asset>().HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Invitation>().HasIndex(x => x.TokenHash).IsUnique(); b.Entity<Invitation>().HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Link>().HasKey(x => new { x.TenantId, x.UserId }); b.Entity<Link>().HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict); b.Entity<Link>().HasOne<Account>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Application>().HasIndex(x => x.Fingerprint).IsUnique(); b.Entity<Application>().HasOne<Account>().WithMany().HasForeignKey(x => x.ApplicantId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Level>().HasAlternateKey(x => new { x.TenantId, x.Id }); b.Entity<Level>().HasIndex(x => new { x.TenantId, x.Name }).IsUnique(); b.Entity<Level>().HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Unit>().HasAlternateKey(x => new { x.TenantId, x.Id }); b.Entity<Unit>().HasIndex(x => new { x.TenantId, x.Code }).IsUnique(); b.Entity<Unit>().HasIndex(x => new { x.TenantId, x.ParentId, x.Name }); b.Entity<Unit>().HasOne<Unit>().WithMany().HasForeignKey(x => new { x.TenantId, x.ParentId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); b.Entity<Unit>().HasOne<Level>().WithMany().HasForeignKey(x => new { x.TenantId, x.LevelId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); b.Entity<Unit>().Property(x => x.Revision).IsConcurrencyToken();
        b.Entity<GovernanceRole>().HasAlternateKey(x => new { x.TenantId, x.Id }); b.Entity<GovernanceRole>().HasIndex(x => new { x.TenantId, x.Name }).IsUnique(); b.Entity<GovernanceRole>().HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Assignment>().HasOne<GovernanceRole>().WithMany().HasForeignKey(x => new { x.TenantId, x.RoleId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); b.Entity<Assignment>().HasOne<Unit>().WithMany().HasForeignKey(x => new { x.TenantId, x.UnitId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); b.Entity<Assignment>().HasOne<Link>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId }).OnDelete(DeleteBehavior.Restrict); b.Entity<Assignment>().HasIndex(x => new { x.TenantId, x.UserId, x.UnitId });
        b.Entity<Onboarding>().HasKey(x => x.TenantId); b.Entity<Onboarding>().HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Audit>().HasIndex(x => new { x.TenantId, x.At });
        foreach (var e in b.Model.GetEntityTypes()) foreach (var prop in e.GetProperties()) if (prop.ClrType == typeof(string) && prop.Name is "Details" or "Profile" or "Data") prop.SetColumnType("jsonb");
    }
}
public record RegisterRequest([Required, EmailAddress] string Email, [Required, MinLength(12)] string Password, [Required, MaxLength(160)] string FullName);
public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public record UnitRequest(Guid LevelId, Guid? ParentId, [Required, MaxLength(160)] string Name, [Required, RegularExpression("^[A-Za-z0-9_-]{2,40}$")] string Code, [Required] string Status, [Required] string Details, int Revision = 0);
public record LevelRequest([Required] string Name, [Required] string Kind, [Required] Guid[] AllowedParents, bool AllowRoot);
public record RoleRequest([Required] string Name, [Required] string[] Permissions, [Required(AllowEmptyStrings = true)] string Responsibilities);
public record AssignmentRequest(Guid UserId, Guid RoleId, Guid? UnitId, DateTimeOffset StartsAt, DateTimeOffset? EndsAt);
public record ApplicationRequest(string Name, string Category, string Country, string State, string City, string OfficialEmail, string ContactNumber, string? Website, string Address, string? Reference, string ApplicantMobile);
public record ReviewRequest(bool Approve, string Note);
public record OnboardingRequest(int Step, string Data, bool Activate);
public record ProfileRequest(string FullName);
public record PasswordRequest(string CurrentPassword, string NewPassword);
public record ResetRequest([Required, EmailAddress] string Email, [Required] string Token, [Required] string Password);
