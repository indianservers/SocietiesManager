using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Society.Api;

public class MembershipOffer
{
    public Guid TenantId { get; set; }
    public Guid CategoryId { get; set; }
    public bool Enabled { get; set; }
    public int TermMonths { get; set; } = 12;
    public decimal? Price { get; set; }
    public Guid? EntityId { get; set; }
    public string Currency { get; set; } = "INR";
    public string IncomeCode { get; set; } = "4000";
    public bool PaymentRequired { get; set; } = true;
    public int ReminderDays { get; set; } = 30;
    public int Revision { get; set; }
}
public class MembershipRepresentative
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MembershipId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "Controller";
    public DateTimeOffset StartsAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndsAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
public class MembershipRenewal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MembershipId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid RequestedBy { get; set; }
    public int TermMonths { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string State { get; set; } = "Pending";
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AppliedAt { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
}
public class MemberResource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Kind { get; set; } = "Guide";
    public string[] Audiences { get; set; } = [];
    public string Content { get; set; } = "";
    public string Url { get; set; } = "";
    public bool Published { get; set; }
    public int Revision { get; set; }
}
public class MembershipRenewalNotice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MembershipId { get; set; }
    public DateTimeOffset TermEnd { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public class InstitutionChapter
{
    public Guid TenantId { get; set; }
    public Guid ChapterId { get; set; }
    public Guid MembershipId { get; set; }
}
// Roster membership is a benefit of an organization membership, never an admin role.
public class OrganizationMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MembershipId { get; set; }
    public string Number { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? ReferenceKey { get; set; }
    public string? NumberKey { get; set; }
    public string Role { get; set; } = "Student";
    public string Reference { get; set; } = "";
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
}
public record OrganizationMemberWrite([property: Required, MaxLength(160)] string FullName,
    [property: EmailAddress, MaxLength(254)] string? Email, [property: Required, MaxLength(40)] string Role,
    [property: MaxLength(100)] string Reference = "", [property: MaxLength(30)] string? Mobile = null,
    [property: MaxLength(254)] string? MemberId = null);

public class OrganizationRosterImport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MembershipId { get; set; }
    public Guid ActorId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddHours(24);
    public DateTimeOffset? CommittedAt { get; set; }
    public string Rows { get; set; } = "[]";
}

public record RepresentativeWrite([Required, EmailAddress] string Email, string Role = "Controller", DateTimeOffset? EndsAt = null);
public record ResourceWrite([Required, MaxLength(180)] string Title, [MaxLength(2000)] string Description,
    [Required] string Kind, [Required] string[] Audiences, [Required(AllowEmptyStrings = true), MaxLength(40000)] string Content,
    [Required(AllowEmptyStrings = true), MaxLength(2000)] string Url, bool Published = false, int Revision = 0);

public static class MembershipExperienceModel
{
    public static void Configure(ModelBuilder b)
    {
        b.Entity<OrganizationMember>().HasOne<Membership>().WithMany().HasForeignKey(x => new { x.TenantId, x.MembershipId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrganizationMember>().HasIndex(x => new { x.TenantId, x.MembershipId, x.NumberKey }).IsUnique();
        b.Entity<OrganizationMember>().HasIndex(x => new { x.TenantId, x.MembershipId, x.Mobile }).IsUnique().HasFilter("\"RevokedAt\" IS NULL AND \"Mobile\" IS NOT NULL");
        b.Entity<OrganizationMember>().HasIndex(x => new { x.TenantId, x.MembershipId, x.ReferenceKey }).IsUnique().HasFilter("\"RevokedAt\" IS NULL AND \"ReferenceKey\" IS NOT NULL");
        b.Entity<OrganizationMember>().HasIndex(x => new { x.TenantId, x.MembershipId, x.Email }).IsUnique().HasFilter("\"RevokedAt\" IS NULL AND \"Email\" IS NOT NULL");
        b.Entity<OrganizationMember>().HasIndex(x => x.Email);
        b.Entity<OrganizationMember>().Property(x => x.Number).HasMaxLength(254);
        b.Entity<OrganizationMember>().Property(x => x.NumberKey).HasMaxLength(254);
        b.Entity<OrganizationMember>().Property(x => x.Mobile).HasMaxLength(20);
        b.Entity<OrganizationMember>().Property(x => x.ReferenceKey).HasMaxLength(100);
        b.Entity<OrganizationMember>().Property(x => x.FullName).HasMaxLength(160);
        b.Entity<OrganizationMember>().Property(x => x.Email).HasMaxLength(254);
        b.Entity<OrganizationMember>().Property(x => x.Role).HasMaxLength(40);
        b.Entity<OrganizationMember>().Property(x => x.Reference).HasMaxLength(100);
        b.Entity<OrganizationRosterImport>().HasOne<Membership>().WithMany().HasForeignKey(x => new { x.TenantId, x.MembershipId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrganizationRosterImport>().HasOne<Account>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MembershipOffer>().HasKey(x => new { x.TenantId, x.CategoryId });
        b.Entity<MembershipOffer>().HasOne<MembershipCategory>().WithMany().HasForeignKey(x => new { x.TenantId, x.CategoryId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MembershipOffer>().HasOne<FinancialEntity>().WithMany().HasForeignKey(x => new { x.TenantId, x.EntityId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MembershipOffer>().Property(x => x.Price).HasPrecision(18, 3);
        b.Entity<MembershipOffer>().Property(x => x.Revision).IsConcurrencyToken();
        b.Entity<MembershipRepresentative>().HasOne<Membership>().WithMany().HasForeignKey(x => new { x.TenantId, x.MembershipId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MembershipRepresentative>().HasOne<Account>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MembershipRepresentative>().HasIndex(x => new { x.TenantId, x.MembershipId, x.UserId }).IsUnique().HasFilter("\"RevokedAt\" IS NULL");
        b.Entity<MembershipRepresentative>().HasIndex(x => new { x.UserId, x.TenantId });
        b.Entity<MembershipRenewal>().HasOne<Membership>().WithMany().HasForeignKey(x => new { x.TenantId, x.MembershipId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MembershipRenewal>().HasOne<FinanceInvoice>().WithMany().HasForeignKey(x => new { x.TenantId, x.InvoiceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MembershipRenewal>().HasIndex(x => new { x.TenantId, x.MembershipId }).IsUnique().HasFilter("\"State\"='Pending'");
        b.Entity<MembershipRenewal>().HasIndex(x => x.InvoiceId).IsUnique();
        b.Entity<MembershipRenewal>().Property(x => x.Amount).HasPrecision(18, 3);
        b.Entity<MemberResource>().HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MemberResource>().HasIndex(x => new { x.TenantId, x.Published });
        b.Entity<MemberResource>().Property(x => x.Revision).IsConcurrencyToken();
        b.Entity<MembershipRenewalNotice>().HasOne<Membership>().WithMany().HasForeignKey(x => new { x.TenantId, x.MembershipId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MembershipRenewalNotice>().HasIndex(x => new { x.TenantId, x.MembershipId, x.TermEnd }).IsUnique();
        b.Entity<InstitutionChapter>().HasKey(x => new { x.TenantId, x.ChapterId });
        b.Entity<InstitutionChapter>().HasOne<Membership>().WithMany().HasForeignKey(x => new { x.TenantId, x.MembershipId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<InstitutionChapter>().HasOne<Unit>().WithMany().HasForeignKey(x => new { x.TenantId, x.ChapterId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
