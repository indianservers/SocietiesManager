using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Society.Api;

public class Person
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string FullName { get; set; } = "";
    public string SearchName { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Revision { get; set; }
}
public class MembershipCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Active { get; set; } = true;
}
public class Membership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PersonId { get; set; }
    public Guid ChapterId { get; set; }
    public Guid CategoryId { get; set; }
    public string Number { get; set; } = "";
    public string Status { get; set; } = "Active";
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset EffectiveFrom { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndsAt { get; set; }
    public string Profile { get; set; } = "{}";
    public string Privacy { get; set; } = "{\"directory\":true,\"photo\":false,\"contacts\":false,\"professional\":false}";
    public int Revision { get; set; }
}
public class MembershipPolicyRecord
{
    public Guid TenantId { get; set; }
    public Guid? ChapterId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool AllowMultipleChapters { get; set; }
    public string DirectoryScope { get; set; } = "chapter";
    public Guid? DirectoryLevelId { get; set; }
    public Guid[] SelectedChapters { get; set; } = [];
    public bool ContactsAllowed { get; set; }
    public bool SourceTransferReview { get; set; } = true;
    public bool SocietyTransferReview { get; set; }
    public string ApprovalStages { get; set; } = "[{\"name\":\"Chapter review\",\"permission\":\"members.approve\"}]";
    public int Revision { get; set; }
}
public class MemberApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid ChapterId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? MembershipId { get; set; }
    public string Status { get; set; } = "Draft";
    public string Profile { get; set; } = "{}";
    public string Stages { get; set; } = "[]";
    public int Stage { get; set; }
    public int Revision { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public class MembershipDecision
{
    public Guid[] DocumentIds { get; set; } = [];
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ApplicationId { get; set; }
    public int Sequence { get; set; }
    public Guid ReviewerId { get; set; }
    public string Permission { get; set; } = "";
    public string ReviewerRoles { get; set; } = "";
    public Guid OrganizationId { get; set; }
    public string Decision { get; set; } = "";
    public string Comment { get; set; } = "";
    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
}
public class MembershipHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MembershipId { get; set; }
    public Guid ActorId { get; set; }
    public string Action { get; set; } = "";
    public string Details { get; set; } = "{}";
    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
}
public class MemberInvitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ChapterId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? PersonId { get; set; }
    public Guid InviterId { get; set; }
    public string Email { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddDays(7);
    public int Limit { get; set; } = 1;
    public int Uses { get; set; }
    public bool Revoked { get; set; }
}
public class MembershipTransfer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MembershipId { get; set; }
    public Guid SourceId { get; set; }
    public Guid DestinationId { get; set; }
    public Guid RequestedBy { get; set; }
    public string Status { get; set; } = "Source Review";
    public bool SocietyReview { get; set; }
    public DateTimeOffset EffectiveAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AppliedAt { get; set; }
    public string Notes { get; set; } = "";
    public int Revision { get; set; }
}
public class OfficerPosition
{
    public bool Required {get;set;}
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Name { get; set; } = "";
    public string Level { get; set; } = "Chapter";
    public Guid? RoleId { get; set; }
    public bool AllowConcurrent { get; set; }
    public string Responsibilities { get; set; } = "";
    public bool Active { get; set; } = true;
}
public class OfficerAppointment
{
    public Guid[] DocumentIds { get; set; } = [];
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MembershipId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid PositionId { get; set; }
    public Guid? AssignmentId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string Method { get; set; } = "Appointed";
    public string Status { get; set; } = "Pending";
    public Guid AuthorityId { get; set; }
    public string Notes { get; set; } = "";
    public int Revision { get; set; }
}
public class LeadershipHandover
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OutgoingId { get; set; }
    public Guid IncomingId { get; set; }
    public string Notes { get; set; } = "";
    public string Status { get; set; } = "Prepared";
    public int Revision { get; set; }
}
public class Committee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? UnitId { get; set; }
    public string Name { get; set; } = "";
    public string Purpose { get; set; } = "";
    public string Status { get; set; } = "Active";
    public DateTimeOffset StartsAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndsAt { get; set; }
    public int Revision { get; set; }
}
public class CommitteeMembership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CommitteeId { get; set; }
    public Guid MembershipId { get; set; }
    public string Role { get; set; } = "Member";
    public string Responsibilities { get; set; } = "";
    public DateTimeOffset StartsAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndsAt { get; set; }
}
public class MemberDocument
{
    public Guid? ApplicationId { get; set; }
    public string Kind { get; set; } = "Supporting";
    public string ContentType { get; set; } = "image/png";
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? MembershipId { get; set; }
    public string Name { get; set; } = "";
    public long Length { get; set; }
    public Guid UploadedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public class MemberImportJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ChapterId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid ActorId { get; set; }
    public string Status { get; set; } = "Preview";
    public int Total { get; set; }
    public int Processed { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public class MemberImportRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid JobId { get; set; }
    public int Row { get; set; }
    public string Data { get; set; } = "{}";
    public string Error { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public Guid? MembershipId { get; set; }
}
public class MemberSavedView
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public string Data { get; set; } = "{}";
}
public partial class AppDb
{
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<MembershipCategory> MembershipCategories => Set<MembershipCategory>();
    public DbSet<MembershipPolicyRecord> MembershipPolicies => Set<MembershipPolicyRecord>();
    public DbSet<MemberApplication> MemberApplications => Set<MemberApplication>();
    public DbSet<MembershipDecision> MembershipDecisions => Set<MembershipDecision>();
    public DbSet<MembershipHistory> MembershipHistories => Set<MembershipHistory>();
    public DbSet<MemberInvitation> MemberInvitations => Set<MemberInvitation>();
    public DbSet<MembershipTransfer> MembershipTransfers => Set<MembershipTransfer>();
    public DbSet<OfficerPosition> OfficerPositions => Set<OfficerPosition>();
    public DbSet<OfficerAppointment> OfficerAppointments => Set<OfficerAppointment>();
    public DbSet<LeadershipHandover> LeadershipHandovers => Set<LeadershipHandover>();
    public DbSet<Committee> Committees => Set<Committee>();
    public DbSet<CommitteeMembership> CommitteeMemberships => Set<CommitteeMembership>();
    public DbSet<MemberDocument> MemberDocuments => Set<MemberDocument>();
    public DbSet<MemberImportJob> MemberImportJobs => Set<MemberImportJob>();
    public DbSet<MemberImportRow> MemberImportRows => Set<MemberImportRow>();
    public DbSet<MemberSavedView> MemberSavedViews => Set<MemberSavedView>();
}
public record MemberWriteRequest([Required, MaxLength(160)] string FullName, Guid ChapterId, Guid CategoryId, Guid? PersonId, string? Number, [Required] string Status, [Required] string Profile, string? Privacy, int Revision = 0, DateTimeOffset? JoinedAt = null, DateTimeOffset? EffectiveFrom = null, DateTimeOffset? EndsAt = null, bool DuplicateReviewed = false);
public record MemberApplyRequest(Guid TenantId, Guid ChapterId, Guid CategoryId, [Required] string Profile, bool Submit, int Revision = 0);
public record MemberDecisionRequest([Required] string Decision, [Required(AllowEmptyStrings=true)] string Comment, int Revision);
public record MemberInviteRequest(Guid ChapterId, Guid CategoryId, [Required, EmailAddress] string Email, Guid? PersonId, int Days = 7, int UsageLimit = 1);
public record MemberCategoryRequest([Required, MaxLength(80)] string Name, string Description, bool Active = true);
public record TransferRequest(Guid MembershipId, Guid DestinationId, DateTimeOffset EffectiveAt, string Notes);
public record OfficerPositionRequest([Required,MaxLength(100)] string Name, [Required,MaxLength(80)] string Level, Guid? RoleId, bool AllowConcurrent, [Required(AllowEmptyStrings=true)] string Responsibilities, bool Active = true,bool Required=false);
public record OfficerAppointmentRequest(Guid MembershipId, Guid? UnitId, Guid PositionId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, [Required,MaxLength(80)] string Method, [Required(AllowEmptyStrings=true)] string Notes, Guid[]? DocumentIds = null);
public record HandoverRequest(Guid OutgoingId, Guid IncomingId, string Notes);
public record CommitteeRequest(Guid? UnitId, [Required,MaxLength(160)] string Name, [Required(AllowEmptyStrings=true)] string Purpose, DateTimeOffset StartsAt, DateTimeOffset? EndsAt, string Status, int Revision = 0);
public record CommitteeMemberRequest(Guid MembershipId, string Role, string Responsibilities, DateTimeOffset StartsAt, DateTimeOffset? EndsAt);
public record ImportRequest(Guid ChapterId, Guid CategoryId, [Required] string Format, [Required] string Content, [Required] Dictionary<string, string> Mapping);
