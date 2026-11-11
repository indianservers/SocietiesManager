using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class MembershipGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MembershipId",
                table: "Assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevokedAt",
                table: "Assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Committees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Committees", x => x.Id);
                    table.UniqueConstraint("AK_Committees_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Committees_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Committees_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberSavedViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Data = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberSavedViews", x => x.Id);
                    table.UniqueConstraint("AK_MemberSavedViews_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MemberSavedViews_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MembershipCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipCategories", x => x.Id);
                    table.UniqueConstraint("AK_MembershipCategories_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MembershipCategories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MembershipPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllowMultipleChapters = table.Column<bool>(type: "boolean", nullable: false),
                    DirectoryScope = table.Column<string>(type: "text", nullable: false),
                    SelectedChapters = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    ContactsAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    SourceTransferReview = table.Column<bool>(type: "boolean", nullable: false),
                    SocietyTransferReview = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalStages = table.Column<string>(type: "jsonb", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipPolicies", x => x.Id);
                    table.UniqueConstraint("AK_MembershipPolicies_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MembershipPolicies_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipPolicies_Units_TenantId_ChapterId",
                        columns: x => new { x.TenantId, x.ChapterId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfficerPositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllowConcurrent = table.Column<bool>(type: "boolean", nullable: false),
                    Responsibilities = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficerPositions", x => x.Id);
                    table.UniqueConstraint("AK_OfficerPositions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_OfficerPositions_GovernanceRoles_TenantId_RoleId",
                        columns: x => new { x.TenantId, x.RoleId },
                        principalTable: "GovernanceRoles",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficerPositions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    SearchName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Persons_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Total = table.Column<int>(type: "integer", nullable: false),
                    Processed = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberImportJobs", x => x.Id);
                    table.UniqueConstraint("AK_MemberImportJobs_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MemberImportJobs_MembershipCategories_TenantId_CategoryId",
                        columns: x => new { x.TenantId, x.CategoryId },
                        principalTable: "MembershipCategories",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberImportJobs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberImportJobs_Units_TenantId_ChapterId",
                        columns: x => new { x.TenantId, x.ChapterId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    InviterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Limit = table.Column<int>(type: "integer", nullable: false),
                    Uses = table.Column<int>(type: "integer", nullable: false),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberInvitations", x => x.Id);
                    table.UniqueConstraint("AK_MemberInvitations_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MemberInvitations_MembershipCategories_TenantId_CategoryId",
                        columns: x => new { x.TenantId, x.CategoryId },
                        principalTable: "MembershipCategories",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberInvitations_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberInvitations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberInvitations_Units_TenantId_ChapterId",
                        columns: x => new { x.TenantId, x.ChapterId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Memberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Profile = table.Column<string>(type: "jsonb", nullable: false),
                    Privacy = table.Column<string>(type: "jsonb", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memberships", x => x.Id);
                    table.UniqueConstraint("AK_Memberships_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Memberships_MembershipCategories_TenantId_CategoryId",
                        columns: x => new { x.TenantId, x.CategoryId },
                        principalTable: "MembershipCategories",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Memberships_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Memberships_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Memberships_Units_TenantId_ChapterId",
                        columns: x => new { x.TenantId, x.ChapterId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommitteeMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommitteeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Responsibilities = table.Column<string>(type: "text", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeMemberships", x => x.Id);
                    table.UniqueConstraint("AK_CommitteeMemberships_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommitteeMemberships_Committees_TenantId_CommitteeId",
                        columns: x => new { x.TenantId, x.CommitteeId },
                        principalTable: "Committees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommitteeMemberships_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommitteeMemberships_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Profile = table.Column<string>(type: "jsonb", nullable: false),
                    Stages = table.Column<string>(type: "jsonb", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberApplications", x => x.Id);
                    table.UniqueConstraint("AK_MemberApplications_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MemberApplications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberApplications_MembershipCategories_TenantId_CategoryId",
                        columns: x => new { x.TenantId, x.CategoryId },
                        principalTable: "MembershipCategories",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberApplications_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberApplications_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberApplications_Units_TenantId_ChapterId",
                        columns: x => new { x.TenantId, x.ChapterId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberDocuments", x => x.Id);
                    table.UniqueConstraint("AK_MemberDocuments_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MemberDocuments_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberDocuments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberImportRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Row = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<string>(type: "jsonb", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberImportRows", x => x.Id);
                    table.UniqueConstraint("AK_MemberImportRows_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MemberImportRows_MemberImportJobs_TenantId_JobId",
                        columns: x => new { x.TenantId, x.JobId },
                        principalTable: "MemberImportJobs",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberImportRows_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberImportRows_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MembershipHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "jsonb", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipHistories", x => x.Id);
                    table.UniqueConstraint("AK_MembershipHistories_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MembershipHistories_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipHistories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MembershipTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SocietyReview = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipTransfers", x => x.Id);
                    table.UniqueConstraint("AK_MembershipTransfers_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MembershipTransfers_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipTransfers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipTransfers_Units_TenantId_DestinationId",
                        columns: x => new { x.TenantId, x.DestinationId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipTransfers_Units_TenantId_SourceId",
                        columns: x => new { x.TenantId, x.SourceId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfficerAppointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Method = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AuthorityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficerAppointments", x => x.Id);
                    table.UniqueConstraint("AK_OfficerAppointments_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_OfficerAppointments_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficerAppointments_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficerAppointments_OfficerPositions_TenantId_PositionId",
                        columns: x => new { x.TenantId, x.PositionId },
                        principalTable: "OfficerPositions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficerAppointments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficerAppointments_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MembershipDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<string>(type: "text", nullable: false),
                    Decision = table.Column<string>(type: "text", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipDecisions", x => x.Id);
                    table.UniqueConstraint("AK_MembershipDecisions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MembershipDecisions_MemberApplications_TenantId_Application~",
                        columns: x => new { x.TenantId, x.ApplicationId },
                        principalTable: "MemberApplications",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipDecisions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeadershipHandovers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutgoingId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncomingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadershipHandovers", x => x.Id);
                    table.UniqueConstraint("AK_LeadershipHandovers_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_LeadershipHandovers_OfficerAppointments_TenantId_IncomingId",
                        columns: x => new { x.TenantId, x.IncomingId },
                        principalTable: "OfficerAppointments",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeadershipHandovers_OfficerAppointments_TenantId_OutgoingId",
                        columns: x => new { x.TenantId, x.OutgoingId },
                        principalTable: "OfficerAppointments",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeadershipHandovers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TenantId_MembershipId",
                table: "Assignments",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMemberships_TenantId_CommitteeId_MembershipId",
                table: "CommitteeMemberships",
                columns: new[] { "TenantId", "CommitteeId", "MembershipId" },
                unique: true,
                filter: "\"EndsAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMemberships_TenantId_MembershipId",
                table: "CommitteeMemberships",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_Committees_TenantId_UnitId",
                table: "Committees",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadershipHandovers_TenantId_IncomingId",
                table: "LeadershipHandovers",
                columns: new[] { "TenantId", "IncomingId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadershipHandovers_TenantId_OutgoingId",
                table: "LeadershipHandovers",
                columns: new[] { "TenantId", "OutgoingId" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberApplications_TenantId_CategoryId",
                table: "MemberApplications",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberApplications_TenantId_ChapterId_Status_CreatedAt",
                table: "MemberApplications",
                columns: new[] { "TenantId", "ChapterId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberApplications_TenantId_MembershipId",
                table: "MemberApplications",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberApplications_TenantId_UserId_ChapterId",
                table: "MemberApplications",
                columns: new[] { "TenantId", "UserId", "ChapterId" },
                unique: true,
                filter: "\"Status\" NOT IN ('Rejected','Withdrawn')");

            migrationBuilder.CreateIndex(
                name: "IX_MemberApplications_UserId",
                table: "MemberApplications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberDocuments_TenantId_MembershipId",
                table: "MemberDocuments",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberImportJobs_Status_CreatedAt",
                table: "MemberImportJobs",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberImportJobs_TenantId_CategoryId",
                table: "MemberImportJobs",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberImportJobs_TenantId_ChapterId",
                table: "MemberImportJobs",
                columns: new[] { "TenantId", "ChapterId" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberImportRows_TenantId_JobId_Row",
                table: "MemberImportRows",
                columns: new[] { "TenantId", "JobId", "Row" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberImportRows_TenantId_MembershipId",
                table: "MemberImportRows",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberInvitations_PersonId",
                table: "MemberInvitations",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberInvitations_TenantId_CategoryId",
                table: "MemberInvitations",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberInvitations_TenantId_ChapterId",
                table: "MemberInvitations",
                columns: new[] { "TenantId", "ChapterId" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberInvitations_TokenHash",
                table: "MemberInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembershipCategories_TenantId_Name",
                table: "MembershipCategories",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembershipDecisions_TenantId_ApplicationId_Sequence",
                table: "MembershipDecisions",
                columns: new[] { "TenantId", "ApplicationId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembershipHistories_TenantId_MembershipId_At",
                table: "MembershipHistories",
                columns: new[] { "TenantId", "MembershipId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPolicies_TenantId_ChapterId",
                table: "MembershipPolicies",
                columns: new[] { "TenantId", "ChapterId" });

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_PersonId",
                table: "Memberships",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_TenantId_CategoryId",
                table: "Memberships",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_TenantId_ChapterId_Status_JoinedAt",
                table: "Memberships",
                columns: new[] { "TenantId", "ChapterId", "Status", "JoinedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_TenantId_Number",
                table: "Memberships",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_TenantId_PersonId_ChapterId",
                table: "Memberships",
                columns: new[] { "TenantId", "PersonId", "ChapterId" },
                unique: true,
                filter: "\"Status\" NOT IN ('Closed','Transferred')");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipTransfers_TenantId_DestinationId",
                table: "MembershipTransfers",
                columns: new[] { "TenantId", "DestinationId" });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipTransfers_TenantId_MembershipId",
                table: "MembershipTransfers",
                columns: new[] { "TenantId", "MembershipId" },
                unique: true,
                filter: "\"Status\" NOT IN ('Rejected','Effective')");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipTransfers_TenantId_SourceId",
                table: "MembershipTransfers",
                columns: new[] { "TenantId", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficerAppointments_AssignmentId",
                table: "OfficerAppointments",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerAppointments_TenantId_MembershipId",
                table: "OfficerAppointments",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficerAppointments_TenantId_PositionId",
                table: "OfficerAppointments",
                columns: new[] { "TenantId", "PositionId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficerAppointments_TenantId_UnitId_PositionId_StartsAt_End~",
                table: "OfficerAppointments",
                columns: new[] { "TenantId", "UnitId", "PositionId", "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficerPositions_TenantId_Name",
                table: "OfficerPositions",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficerPositions_TenantId_RoleId",
                table: "OfficerPositions",
                columns: new[] { "TenantId", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Persons_UserId",
                table: "Persons",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Memberships_TenantId_MembershipId",
                table: "Assignments",
                columns: new[] { "TenantId", "MembershipId" },
                principalTable: "Memberships",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
CREATE UNIQUE INDEX membership_policy_scope ON "MembershipPolicies" ("TenantId",COALESCE("ChapterId",'00000000-0000-0000-0000-000000000000'::uuid));
CREATE INDEX person_name_trgm ON "Persons" USING gin ("SearchName" gin_trgm_ops);
ALTER TABLE "Memberships" ADD CONSTRAINT membership_dates CHECK ("EndsAt" IS NULL OR "EndsAt">"EffectiveFrom");
ALTER TABLE "OfficerAppointments" ADD CONSTRAINT appointment_dates CHECK ("EndsAt">"StartsAt");
CREATE TRIGGER member_history_immutable BEFORE UPDATE OR DELETE ON "MembershipHistories" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation();
CREATE TRIGGER member_decision_immutable BEFORE UPDATE OR DELETE ON "MembershipDecisions" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation();
CREATE TRIGGER membership_no_delete BEFORE DELETE ON "Memberships" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation();
CREATE FUNCTION guard_officer_term() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
PERFORM pg_advisory_xact_lock(hashtext(NEW."TenantId"::text));
IF NEW."Status" IN ('Pending','Approved') AND EXISTS(SELECT 1 FROM "OfficerPositions" p WHERE p."Id"=NEW."PositionId" AND NOT p."AllowConcurrent") AND EXISTS(SELECT 1 FROM "OfficerAppointments" a WHERE a."TenantId"=NEW."TenantId" AND a."Id"<>NEW."Id" AND a."UnitId" IS NOT DISTINCT FROM NEW."UnitId" AND a."PositionId"=NEW."PositionId" AND a."Status" IN ('Pending','Approved') AND a."StartsAt"<NEW."EndsAt" AND a."EndsAt">NEW."StartsAt") THEN RAISE EXCEPTION 'Overlapping officer term'; END IF;
RETURN NEW; END $$;
CREATE TRIGGER officer_term_integrity BEFORE INSERT OR UPDATE ON "OfficerAppointments" FOR EACH ROW EXECUTE FUNCTION guard_officer_term();INSERT INTO "MembershipCategories" ("Id","TenantId","Name","Description","Active") SELECT gen_random_uuid(),"Id",'Active Member','',true FROM "Tenants";
INSERT INTO "MembershipCategories" ("Id","TenantId","Name","Description","Active") SELECT gen_random_uuid(),"Id",'Associate Member','',true FROM "Tenants";
INSERT INTO "MembershipCategories" ("Id","TenantId","Name","Description","Active") SELECT gen_random_uuid(),"Id",'Honorary Member','',true FROM "Tenants";
INSERT INTO "MembershipCategories" ("Id","TenantId","Name","Description","Active") SELECT gen_random_uuid(),"Id",'Guest','',true FROM "Tenants";
INSERT INTO "MembershipCategories" ("Id","TenantId","Name","Description","Active") SELECT gen_random_uuid(),"Id",'Applicant','',true FROM "Tenants";
INSERT INTO "MembershipPolicies" ("Id","TenantId","ChapterId","AllowMultipleChapters","DirectoryScope","SelectedChapters","ContactsAllowed","SourceTransferReview","SocietyTransferReview","ApprovalStages","Revision") SELECT gen_random_uuid(),"Id",NULL,false,'chapter',ARRAY[]::uuid[],false,true,false,'[{"name":"Chapter review","permission":"members.approve"}]',0 FROM "Tenants";
UPDATE "GovernanceRoles" SET "Permissions"=ARRAY['units.read','units.write','hierarchy.manage','roles.manage','settings.manage','audit.read','directory.chapter','directory.region','directory.society','members.admin','members.read','members.write','members.approve','members.billing','members.export','members.transfer','members.import','membership.settings','governance.manage','committees.manage'] WHERE "Name"='Society Administrator';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read','members.approve','members.transfer','governance.manage','committees.manage']))) WHERE "Name"='Society President';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read','members.write','members.approve','members.transfer','members.import','committees.manage']))) WHERE "Name"='Society Secretary';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.billing']))) WHERE "Name"='Society Treasurer';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read']))) WHERE "Name"='Society Executive Member';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read','members.approve','members.transfer','governance.manage','committees.manage']))) WHERE "Name"='National President';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read','members.write','members.approve','members.transfer','members.import','committees.manage']))) WHERE "Name"='National Secretary';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.billing']))) WHERE "Name"='National Treasurer';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read']))) WHERE "Name"='National Director';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read','members.approve','members.transfer','governance.manage','committees.manage']))) WHERE "Name"='Regional President';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read','members.write','members.approve','members.transfer','members.import','committees.manage']))) WHERE "Name"='Regional Secretary';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.billing']))) WHERE "Name"='Regional Treasurer';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read','members.approve','members.transfer','governance.manage','committees.manage']))) WHERE "Name"='Regional Coordinator';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read','members.approve','members.transfer','governance.manage','committees.manage']))) WHERE "Name"='District Governor';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read','members.write','members.approve','members.transfer','members.import','committees.manage']))) WHERE "Name"='District Secretary';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.billing']))) WHERE "Name"='District Treasurer';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read']))) WHERE "Name"='Zone Chairperson';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read','members.approve','members.transfer','governance.manage','committees.manage']))) WHERE "Name"='Chapter President';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read','members.approve','members.transfer','governance.manage','committees.manage']))) WHERE "Name"='Chapter Vice President';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read','members.write','members.approve','members.transfer','members.import','committees.manage']))) WHERE "Name"='Chapter Secretary';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.billing']))) WHERE "Name"='Chapter Treasurer';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read','members.write','members.approve','members.transfer','members.import','committees.manage']))) WHERE "Name"='Chapter Manager';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['members.read']))) WHERE "Name"='Chapter Committee Chair';
INSERT INTO "GovernanceRoles" ("Id","TenantId","Name","Permissions","Responsibilities") SELECT gen_random_uuid(),"Id",'Member Directory',ARRAY['units.read','directory.chapter'],'Membership and privacy constrained directory' FROM "Tenants";
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Chapter President','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Chapter President' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'President Elect','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='President Elect' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Immediate Past President','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Immediate Past President' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'First Vice President','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='First Vice President' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Second Vice President','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Second Vice President' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Chapter Secretary','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Chapter Secretary' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Joint Secretary','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Joint Secretary' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Chapter Treasurer','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Chapter Treasurer' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Joint Treasurer','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Joint Treasurer' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Membership Director','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Membership Director' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Event Director','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Event Director' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Service Director','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Service Director' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Training Director','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Training Director' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Public Relations Officer','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Public Relations Officer' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Board Director','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Board Director' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Committee Chair','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Committee Chair' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Chapter Manager','Chapter',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Chapter Manager' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Regional President','Region',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Regional President' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Regional Vice President','Region',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Regional Vice President' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Regional Secretary','Region',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Regional Secretary' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Regional Treasurer','Region',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Regional Treasurer' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Regional Coordinator','Region',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Regional Coordinator' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Regional Membership Director','Region',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Regional Membership Director' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Regional Service Director','Region',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Regional Service Director' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Regional Events Director','Region',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Regional Events Director' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Regional Committee Chair','Region',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Regional Committee Chair' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Society President','Society',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Society President' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Society Vice President','Society',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Society Vice President' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Society Secretary','Society',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Society Secretary' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Society Treasurer','Society',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Society Treasurer' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Executive Director','Society',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Executive Director' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Executive Committee Member','Society',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Executive Committee Member' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'National President','Society',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='National President' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'National Secretary','Society',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='National Secretary' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'National Treasurer','Society',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='National Treasurer' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'National Director','Society',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='National Director' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Society Membership Director','Society',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Society Membership Director' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Society Events Director','Society',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Society Events Director' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Society Service Director','Society',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Society Service Director' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;
INSERT INTO "OfficerPositions" ("Id","TenantId","Name","Level","RoleId","AllowConcurrent","Responsibilities","Active") SELECT gen_random_uuid(),t."Id",'Society Training Director','Society',(SELECT r."Id" FROM "GovernanceRoles" r WHERE r."TenantId"=t."Id" AND r."Name"='Society Training Director' LIMIT 1),false,'Configurable governance position',true FROM "Tenants" t;

""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Membership history is retained. Restore a reviewed backup for a destructive rollback.");
#pragma warning disable CS0162
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Memberships_TenantId_MembershipId",
                table: "Assignments");

            migrationBuilder.DropTable(
                name: "CommitteeMemberships");

            migrationBuilder.DropTable(
                name: "LeadershipHandovers");

            migrationBuilder.DropTable(
                name: "MemberDocuments");

            migrationBuilder.DropTable(
                name: "MemberImportRows");

            migrationBuilder.DropTable(
                name: "MemberInvitations");

            migrationBuilder.DropTable(
                name: "MemberSavedViews");

            migrationBuilder.DropTable(
                name: "MembershipDecisions");

            migrationBuilder.DropTable(
                name: "MembershipHistories");

            migrationBuilder.DropTable(
                name: "MembershipPolicies");

            migrationBuilder.DropTable(
                name: "MembershipTransfers");

            migrationBuilder.DropTable(
                name: "Committees");

            migrationBuilder.DropTable(
                name: "OfficerAppointments");

            migrationBuilder.DropTable(
                name: "MemberImportJobs");

            migrationBuilder.DropTable(
                name: "MemberApplications");

            migrationBuilder.DropTable(
                name: "OfficerPositions");

            migrationBuilder.DropTable(
                name: "Memberships");

            migrationBuilder.DropTable(
                name: "MembershipCategories");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_TenantId_MembershipId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "MembershipId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "Assignments");
        }
    }
}
