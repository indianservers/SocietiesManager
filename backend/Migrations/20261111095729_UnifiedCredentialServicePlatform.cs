using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class UnifiedCredentialServicePlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assets_TenantId",
                table: "Assets");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Assets_TenantId_Id",
                table: "Assets",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "AssistantQuery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Intent = table.Column<string>(type: "text", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantQuery", x => x.Id);
                    table.UniqueConstraint("AK_AssistantQuery_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_AssistantQuery_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssistantQuery_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DesignTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Designation = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Design = table.Column<string>(type: "jsonb", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    PublishedVersion = table.Column<int>(type: "integer", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesignTemplate", x => x.Id);
                    table.UniqueConstraint("AK_DesignTemplate_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DesignTemplate_MembershipCategories_TenantId_CategoryId",
                        columns: x => new { x.TenantId, x.CategoryId },
                        principalTable: "MembershipCategories",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DesignTemplate_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DesignTemplate_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImpactMetric",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Aggregation = table.Column<string>(type: "text", nullable: false),
                    EvidenceRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpactMetric", x => x.Id);
                    table.UniqueConstraint("AK_ImpactMetric_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ImpactMetric_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SavedUnifiedReport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Definition = table.Column<string>(type: "jsonb", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedUnifiedReport", x => x.Id);
                    table.UniqueConstraint("AK_SavedUnifiedReport_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_SavedUnifiedReport_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SavedUnifiedReport_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceProject",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Objectives = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    ManagerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommitteeId = table.Column<Guid>(type: "uuid", nullable: true),
                    BudgetId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProject", x => x.Id);
                    table.UniqueConstraint("AK_ServiceProject_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ServiceProject_ActivityPost_TenantId_ActivityId",
                        columns: x => new { x.TenantId, x.ActivityId },
                        principalTable: "ActivityPost",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceProject_Committees_TenantId_CommitteeId",
                        columns: x => new { x.TenantId, x.CommitteeId },
                        principalTable: "Committees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceProject_CommunityEvent_TenantId_EventId",
                        columns: x => new { x.TenantId, x.EventId },
                        principalTable: "CommunityEvent",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceProject_FinanceBudget_TenantId_BudgetId",
                        columns: x => new { x.TenantId, x.BudgetId },
                        principalTable: "FinanceBudget",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceProject_Memberships_TenantId_ManagerId",
                        columns: x => new { x.TenantId, x.ManagerId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceProject_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceProject_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudioAsset",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudioAsset", x => x.Id);
                    table.UniqueConstraint("AK_StudioAsset_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_StudioAsset_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudioAsset_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudioPolicy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterCustomization = table.Column<bool>(type: "boolean", nullable: false),
                    MemberDownloads = table.Column<bool>(type: "boolean", nullable: false),
                    PublicNames = table.Column<bool>(type: "boolean", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudioPolicy", x => x.Id);
                    table.UniqueConstraint("AK_StudioPolicy_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_StudioPolicy_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantBrand",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    PrimaryColor = table.Column<string>(type: "text", nullable: false),
                    LogoId = table.Column<Guid>(type: "uuid", nullable: true),
                    FaviconId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoginTitle = table.Column<string>(type: "text", nullable: false),
                    ChapterTerm = table.Column<string>(type: "text", nullable: false),
                    RegionTerm = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    TimeZone = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Modules = table.Column<string[]>(type: "text[]", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantBrand", x => x.Id);
                    table.UniqueConstraint("AK_TenantBrand_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_TenantBrand_Assets_TenantId_FaviconId",
                        columns: x => new { x.TenantId, x.FaviconId },
                        principalTable: "Assets",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantBrand_Assets_TenantId_LogoId",
                        columns: x => new { x.TenantId, x.LogoId },
                        principalTable: "Assets",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantBrand_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantDomain",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Host = table.Column<string>(type: "text", nullable: false),
                    Challenge = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantDomain", x => x.Id);
                    table.UniqueConstraint("AK_TenantDomain_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_TenantDomain_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantSubscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Plan = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    ChapterLimit = table.Column<int>(type: "integer", nullable: false),
                    MembershipLimit = table.Column<int>(type: "integer", nullable: false),
                    StorageLimit = table.Column<long>(type: "bigint", nullable: false),
                    Features = table.Column<string[]>(type: "text[]", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSubscription", x => x.Id);
                    table.UniqueConstraint("AK_TenantSubscription_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_TenantSubscription_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerProfile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    Skills = table.Column<string>(type: "text", nullable: false),
                    Interests = table.Column<string>(type: "text", nullable: false),
                    Availability = table.Column<string>(type: "text", nullable: false),
                    PreferredTypes = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerProfile", x => x.Id);
                    table.UniqueConstraint("AK_VolunteerProfile_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_VolunteerProfile_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerProfile_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DesignVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Design = table.Column<string>(type: "jsonb", nullable: false),
                    PublisherId = table.Column<Guid>(type: "uuid", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesignVersion", x => x.Id);
                    table.UniqueConstraint("AK_DesignVersion_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DesignVersion_DesignTemplate_TenantId_TemplateId",
                        columns: x => new { x.TenantId, x.TemplateId },
                        principalTable: "DesignTemplate",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DesignVersion_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DesignVersion_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnifiedReportSchedule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Format = table.Column<string>(type: "text", nullable: false),
                    Frequency = table.Column<string>(type: "text", nullable: false),
                    NextRun = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnifiedReportSchedule", x => x.Id);
                    table.UniqueConstraint("AK_UnifiedReportSchedule_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_UnifiedReportSchedule_SavedUnifiedReport_TenantId_ReportId",
                        columns: x => new { x.TenantId, x.ReportId },
                        principalTable: "SavedUnifiedReport",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnifiedReportSchedule_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnifiedReportSchedule_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectChapter",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectChapter", x => x.Id);
                    table.UniqueConstraint("AK_ProjectChapter_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProjectChapter_ServiceProject_TenantId_ProjectId",
                        columns: x => new { x.TenantId, x.ProjectId },
                        principalTable: "ServiceProject",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectChapter_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectChapter_Units_TenantId_ChapterId",
                        columns: x => new { x.TenantId, x.ChapterId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTask",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DueOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTask", x => x.Id);
                    table.UniqueConstraint("AK_ProjectTask_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProjectTask_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTask_ServiceProject_TenantId_ProjectId",
                        columns: x => new { x.TenantId, x.ProjectId },
                        principalTable: "ServiceProject",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTask_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerAssignment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    CoordinatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerAssignment", x => x.Id);
                    table.UniqueConstraint("AK_VolunteerAssignment_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_VolunteerAssignment_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerAssignment_ServiceProject_TenantId_ProjectId",
                        columns: x => new { x.TenantId, x.ProjectId },
                        principalTable: "ServiceProject",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerAssignment_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerShift",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerShift", x => x.Id);
                    table.UniqueConstraint("AK_VolunteerShift_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_VolunteerShift_ServiceProject_TenantId_ProjectId",
                        columns: x => new { x.TenantId, x.ProjectId },
                        principalTable: "ServiceProject",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerShift_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImpactResult",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutcomeKey = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(20,4)", nullable: false),
                    Evidence = table.Column<string>(type: "text", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpactResult", x => x.Id);
                    table.UniqueConstraint("AK_ImpactResult_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ImpactResult_ImpactMetric_TenantId_MetricId",
                        columns: x => new { x.TenantId, x.MetricId },
                        principalTable: "ImpactMetric",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImpactResult_ServiceProject_TenantId_ProjectId",
                        columns: x => new { x.TenantId, x.ProjectId },
                        principalTable: "ServiceProject",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImpactResult_StudioAsset_TenantId_EvidenceId",
                        columns: x => new { x.TenantId, x.EvidenceId },
                        principalTable: "StudioAsset",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImpactResult_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudioJob",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: true),
                    TemplateVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    Total = table.Column<int>(type: "integer", nullable: false),
                    Processed = table.Column<int>(type: "integer", nullable: false),
                    Failed = table.Column<int>(type: "integer", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudioJob", x => x.Id);
                    table.UniqueConstraint("AK_StudioJob_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_StudioJob_DesignVersion_TenantId_TemplateVersionId",
                        columns: x => new { x.TenantId, x.TemplateVersionId },
                        principalTable: "DesignVersion",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudioJob_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudioJob_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerAttendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckedIn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CheckedOut = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Hours = table.Column<decimal>(type: "numeric(20,4)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RecorderId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerAttendance", x => x.Id);
                    table.UniqueConstraint("AK_VolunteerAttendance_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_VolunteerAttendance_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerAttendance_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerAttendance_VolunteerShift_TenantId_ShiftId",
                        columns: x => new { x.TenantId, x.ShiftId },
                        principalTable: "VolunteerShift",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Credential",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreviousId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Serial = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    ProtectedToken = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: true),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PdfId = table.Column<Guid>(type: "uuid", nullable: true),
                    FrontId = table.Column<Guid>(type: "uuid", nullable: true),
                    BackId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credential", x => x.Id);
                    table.UniqueConstraint("AK_Credential_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Credential_CommunityEvent_TenantId_EventId",
                        columns: x => new { x.TenantId, x.EventId },
                        principalTable: "CommunityEvent",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Credential_Credential_TenantId_PreviousId",
                        columns: x => new { x.TenantId, x.PreviousId },
                        principalTable: "Credential",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Credential_DesignVersion_TenantId_TemplateVersionId",
                        columns: x => new { x.TenantId, x.TemplateVersionId },
                        principalTable: "DesignVersion",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Credential_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Credential_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Credential_ServiceProject_TenantId_ProjectId",
                        columns: x => new { x.TenantId, x.ProjectId },
                        principalTable: "ServiceProject",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Credential_StudioJob_TenantId_JobId",
                        columns: x => new { x.TenantId, x.JobId },
                        principalTable: "StudioJob",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Credential_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Credential_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudioDownload",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudioDownload", x => x.Id);
                    table.UniqueConstraint("AK_StudioDownload_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_StudioDownload_StudioJob_TenantId_JobId",
                        columns: x => new { x.TenantId, x.JobId },
                        principalTable: "StudioJob",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudioDownload_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CredentialRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CredentialRequest", x => x.Id);
                    table.UniqueConstraint("AK_CredentialRequest_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CredentialRequest_Credential_TenantId_CredentialId",
                        columns: x => new { x.TenantId, x.CredentialId },
                        principalTable: "Credential",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CredentialRequest_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CredentialRequest_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudioJobItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialId = table.Column<Guid>(type: "uuid", nullable: true),
                    State = table.Column<string>(type: "text", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudioJobItem", x => x.Id);
                    table.UniqueConstraint("AK_StudioJobItem_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_StudioJobItem_Credential_TenantId_CredentialId",
                        columns: x => new { x.TenantId, x.CredentialId },
                        principalTable: "Credential",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudioJobItem_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudioJobItem_StudioJob_TenantId_JobId",
                        columns: x => new { x.TenantId, x.JobId },
                        principalTable: "StudioJob",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudioJobItem_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantQuery_TenantId_At",
                table: "AssistantQuery",
                columns: new[] { "TenantId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantQuery_TenantId_UnitId",
                table: "AssistantQuery",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_Credential_PersonId",
                table: "Credential",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Credential_TenantId_EventId",
                table: "Credential",
                columns: new[] { "TenantId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_Credential_TenantId_JobId",
                table: "Credential",
                columns: new[] { "TenantId", "JobId" });

            migrationBuilder.CreateIndex(
                name: "IX_Credential_TenantId_MembershipId_Kind_Status",
                table: "Credential",
                columns: new[] { "TenantId", "MembershipId", "Kind", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Credential_TenantId_PreviousId",
                table: "Credential",
                columns: new[] { "TenantId", "PreviousId" });

            migrationBuilder.CreateIndex(
                name: "IX_Credential_TenantId_ProjectId",
                table: "Credential",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Credential_TenantId_Serial",
                table: "Credential",
                columns: new[] { "TenantId", "Serial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Credential_TenantId_TemplateVersionId",
                table: "Credential",
                columns: new[] { "TenantId", "TemplateVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Credential_TenantId_UnitId",
                table: "Credential",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_Credential_TokenHash",
                table: "Credential",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CredentialRequest_TenantId_CredentialId",
                table: "CredentialRequest",
                columns: new[] { "TenantId", "CredentialId" });

            migrationBuilder.CreateIndex(
                name: "IX_CredentialRequest_TenantId_UnitId",
                table: "CredentialRequest",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_DesignTemplate_TenantId_CategoryId",
                table: "DesignTemplate",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_DesignTemplate_TenantId_UnitId",
                table: "DesignTemplate",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_DesignVersion_TenantId_TemplateId_Version",
                table: "DesignVersion",
                columns: new[] { "TenantId", "TemplateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DesignVersion_TenantId_UnitId",
                table: "DesignVersion",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_ImpactResult_TenantId_EvidenceId",
                table: "ImpactResult",
                columns: new[] { "TenantId", "EvidenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ImpactResult_TenantId_MetricId_OutcomeKey",
                table: "ImpactResult",
                columns: new[] { "TenantId", "MetricId", "OutcomeKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImpactResult_TenantId_ProjectId",
                table: "ImpactResult",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ImpactResult_TenantId_Status_ProjectId",
                table: "ImpactResult",
                columns: new[] { "TenantId", "Status", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectChapter_TenantId_ChapterId_ProjectId",
                table: "ProjectChapter",
                columns: new[] { "TenantId", "ChapterId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectChapter_TenantId_ProjectId_ChapterId",
                table: "ProjectChapter",
                columns: new[] { "TenantId", "ProjectId", "ChapterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTask_TenantId_MembershipId",
                table: "ProjectTask",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTask_TenantId_ProjectId",
                table: "ProjectTask",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedUnifiedReport_TenantId_UnitId",
                table: "SavedUnifiedReport",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProject_TenantId_ActivityId",
                table: "ServiceProject",
                columns: new[] { "TenantId", "ActivityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProject_TenantId_BudgetId",
                table: "ServiceProject",
                columns: new[] { "TenantId", "BudgetId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProject_TenantId_CommitteeId",
                table: "ServiceProject",
                columns: new[] { "TenantId", "CommitteeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProject_TenantId_EventId",
                table: "ServiceProject",
                columns: new[] { "TenantId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProject_TenantId_ManagerId",
                table: "ServiceProject",
                columns: new[] { "TenantId", "ManagerId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProject_TenantId_UnitId",
                table: "ServiceProject",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudioAsset_TenantId_UnitId",
                table: "StudioAsset",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudioDownload_TenantId_JobId",
                table: "StudioDownload",
                columns: new[] { "TenantId", "JobId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudioDownload_TokenHash",
                table: "StudioDownload",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudioJob_State_At",
                table: "StudioJob",
                columns: new[] { "State", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_StudioJob_TenantId_AuthorId_Key",
                table: "StudioJob",
                columns: new[] { "TenantId", "AuthorId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudioJob_TenantId_TemplateVersionId",
                table: "StudioJob",
                columns: new[] { "TenantId", "TemplateVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudioJob_TenantId_UnitId",
                table: "StudioJob",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudioJobItem_JobId_State_Id",
                table: "StudioJobItem",
                columns: new[] { "JobId", "State", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StudioJobItem_TenantId_CredentialId",
                table: "StudioJobItem",
                columns: new[] { "TenantId", "CredentialId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudioJobItem_TenantId_JobId_MembershipId",
                table: "StudioJobItem",
                columns: new[] { "TenantId", "JobId", "MembershipId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudioJobItem_TenantId_MembershipId",
                table: "StudioJobItem",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudioPolicy_TenantId",
                table: "StudioPolicy",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantBrand_TenantId",
                table: "TenantBrand",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantBrand_TenantId_FaviconId",
                table: "TenantBrand",
                columns: new[] { "TenantId", "FaviconId" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantBrand_TenantId_LogoId",
                table: "TenantBrand",
                columns: new[] { "TenantId", "LogoId" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomain_Host",
                table: "TenantDomain",
                column: "Host",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscription_TenantId",
                table: "TenantSubscription",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnifiedReportSchedule_TenantId_ReportId",
                table: "UnifiedReportSchedule",
                columns: new[] { "TenantId", "ReportId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnifiedReportSchedule_TenantId_UnitId",
                table: "UnifiedReportSchedule",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAssignment_TenantId_MembershipId",
                table: "VolunteerAssignment",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAssignment_TenantId_ProjectId_MembershipId",
                table: "VolunteerAssignment",
                columns: new[] { "TenantId", "ProjectId", "MembershipId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAttendance_TenantId_MembershipId",
                table: "VolunteerAttendance",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAttendance_TenantId_ShiftId_MembershipId",
                table: "VolunteerAttendance",
                columns: new[] { "TenantId", "ShiftId", "MembershipId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerProfile_TenantId_MembershipId",
                table: "VolunteerProfile",
                columns: new[] { "TenantId", "MembershipId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerShift_TenantId_ProjectId",
                table: "VolunteerShift",
                columns: new[] { "TenantId", "ProjectId" });
            migrationBuilder.Sql("""

INSERT INTO "StudioPolicy" ("Id","TenantId","ChapterCustomization","MemberDownloads","PublicNames","Revision") SELECT gen_random_uuid(),"Id",false,true,false,0 FROM "Tenants";
INSERT INTO "TenantBrand" ("Id","TenantId","DisplayName","PrimaryColor","LoginTitle","ChapterTerm","RegionTerm","Language","TimeZone","Currency","Modules","Revision") SELECT gen_random_uuid(),"Id",'', '#117d72','Welcome to your community','Chapter','Region','en','Asia/Kolkata','INR',ARRAY['membership','community','communications','finance','studio','service','intelligence'],0 FROM "Tenants";
INSERT INTO "TenantSubscription" ("Id","TenantId","Plan","State","ChapterLimit","MembershipLimit","StorageLimit","Features","Revision") SELECT gen_random_uuid(),"Id",'Standard','Trial',10000,100000,10737418240,ARRAY['studio','service','intelligence'],0 FROM "Tenants";
UPDATE "GovernanceRoles" SET "Permissions"=ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['studio.read','studio.design','studio.publish','studio.issue','studio.approve','studio.bulk','studio.settings','service.read','service.manage','service.approve','service.coordinate','service.verify','intelligence.read','reports.export','reports.schedule','assistant.use','branding.manage'])) WHERE "Name"='Society Administrator';
UPDATE "GovernanceRoles" SET "Permissions"=ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['studio.read','studio.design','studio.issue','studio.approve','service.read','service.manage','service.approve','service.coordinate','service.verify','intelligence.read','reports.export','assistant.use'])) WHERE "Name" LIKE '%President%' OR "Name" LIKE '%Secretary%' OR "Name" LIKE '%Governor%' OR "Name" LIKE '%Coordinator%' OR "Name" LIKE '%Manager%';

CREATE FUNCTION studio_immutable() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Published design versions and immutable assets cannot be edited or removed' USING ERRCODE='23514'; END $$;
CREATE TRIGGER design_version_frozen BEFORE UPDATE OR DELETE ON "DesignVersion" FOR EACH ROW EXECUTE FUNCTION studio_immutable();
CREATE TRIGGER studio_asset_frozen BEFORE UPDATE OR DELETE ON "StudioAsset" FOR EACH ROW EXECUTE FUNCTION studio_immutable();
CREATE FUNCTION credential_guard() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Credential history cannot be deleted' USING ERRCODE='23514'; END IF;
 IF TG_OP='INSERT' THEN
  IF NOT EXISTS(SELECT 1 FROM "Memberships" m WHERE m."TenantId"=NEW."TenantId" AND m."Id"=NEW."MembershipId" AND m."PersonId"=NEW."PersonId" AND m."ChapterId"=NEW."UnitId") THEN RAISE EXCEPTION 'Credential must reference its canonical membership/person/chapter' USING ERRCODE='23514';END IF;
 ELSE
  IF (to_jsonb(NEW)-ARRAY['Status','PdfId','FrontId','BackId','ApproverId','Reason']) IS DISTINCT FROM (to_jsonb(OLD)-ARRAY['Status','PdfId','FrontId','BackId','ApproverId','Reason']) THEN RAISE EXCEPTION 'Credential snapshots and identity are immutable' USING ERRCODE='23514';END IF;
  IF OLD."PdfId" IS NOT NULL AND (NEW."PdfId" IS DISTINCT FROM OLD."PdfId" OR NEW."FrontId" IS DISTINCT FROM OLD."FrontId" OR NEW."BackId" IS DISTINCT FROM OLD."BackId") THEN RAISE EXCEPTION 'Issued artifact references cannot be replaced' USING ERRCODE='23514';END IF;
  IF OLD."ApproverId" IS NOT NULL AND NEW."ApproverId" IS DISTINCT FROM OLD."ApproverId" THEN RAISE EXCEPTION 'Issuing authority is immutable after approval' USING ERRCODE='23514';END IF;
  IF OLD."Status" IN('Revoked','Replaced','Expired') AND NEW."Status" NOT IN('Revoked','Replaced','Expired') THEN RAISE EXCEPTION 'Invalid credential reactivation' USING ERRCODE='23514';END IF;
 END IF;
 IF NEW."Status"='Issued' AND (NEW."ApproverId" IS NULL OR NEW."AuthorId"=NEW."ApproverId" OR NEW."PdfId" IS NULL OR NEW."FrontId" IS NULL) THEN RAISE EXCEPTION 'Issue requires independent approval and official files' USING ERRCODE='23514';END IF;
 IF NEW."ExpiryDate"<NEW."IssueDate" THEN RAISE EXCEPTION 'Invalid credential dates' USING ERRCODE='23514';END IF;
 RETURN NEW;END $$;
CREATE TRIGGER credential_history_guard BEFORE INSERT OR UPDATE OR DELETE ON "Credential" FOR EACH ROW EXECUTE FUNCTION credential_guard();
ALTER TABLE "ImpactResult" ADD CONSTRAINT impact_positive CHECK("Value">=0 AND ("Status"<>'Verified' OR ("VerifierId" IS NOT NULL AND "VerifierId"<>"AuthorId")));
ALTER TABLE "VolunteerAttendance" ADD CONSTRAINT volunteer_time_integrity CHECK("Hours">=0 AND ("CheckedOut" IS NULL OR "CheckedOut">"CheckedIn") AND ("Status"<>'Verified' OR ("VerifierId" IS NOT NULL AND "VerifierId"<>"RecorderId" AND "CheckedOut" IS NOT NULL)));
ALTER TABLE "ServiceProject" ADD CONSTRAINT project_dates CHECK("EndsOn">="StartsOn" AND ("Status" NOT IN('Approved','Completed') OR ("ApproverId" IS NOT NULL AND "ApproverId"<>"AuthorId")));

""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS credential_guard() CASCADE; DROP FUNCTION IF EXISTS studio_immutable() CASCADE;");
            migrationBuilder.DropTable(
                name: "AssistantQuery");

            migrationBuilder.DropTable(
                name: "CredentialRequest");

            migrationBuilder.DropTable(
                name: "ImpactResult");

            migrationBuilder.DropTable(
                name: "ProjectChapter");

            migrationBuilder.DropTable(
                name: "ProjectTask");

            migrationBuilder.DropTable(
                name: "StudioDownload");

            migrationBuilder.DropTable(
                name: "StudioJobItem");

            migrationBuilder.DropTable(
                name: "StudioPolicy");

            migrationBuilder.DropTable(
                name: "TenantBrand");

            migrationBuilder.DropTable(
                name: "TenantDomain");

            migrationBuilder.DropTable(
                name: "TenantSubscription");

            migrationBuilder.DropTable(
                name: "UnifiedReportSchedule");

            migrationBuilder.DropTable(
                name: "VolunteerAssignment");

            migrationBuilder.DropTable(
                name: "VolunteerAttendance");

            migrationBuilder.DropTable(
                name: "VolunteerProfile");

            migrationBuilder.DropTable(
                name: "ImpactMetric");

            migrationBuilder.DropTable(
                name: "StudioAsset");

            migrationBuilder.DropTable(
                name: "Credential");

            migrationBuilder.DropTable(
                name: "SavedUnifiedReport");

            migrationBuilder.DropTable(
                name: "VolunteerShift");

            migrationBuilder.DropTable(
                name: "StudioJob");

            migrationBuilder.DropTable(
                name: "ServiceProject");

            migrationBuilder.DropTable(
                name: "DesignVersion");

            migrationBuilder.DropTable(
                name: "DesignTemplate");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Assets_TenantId_Id",
                table: "Assets");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_TenantId",
                table: "Assets",
                column: "TenantId");
        }
    }
}
