using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChapterActivitiesEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityCategory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityCategory", x => x.Id);
                    table.UniqueConstraint("AK_ActivityCategory_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ActivityCategory_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityBlock",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityBlock", x => x.Id);
                    table.UniqueConstraint("AK_CommunityBlock_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunityBlock_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityDecision",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Decision = table.Column<string>(type: "text", nullable: false),
                    Permission = table.Column<string>(type: "text", nullable: false),
                    Roles = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityDecision", x => x.Id);
                    table.UniqueConstraint("AK_CommunityDecision_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunityDecision_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceKind = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Sort = table.Column<int>(type: "integer", nullable: false),
                    Cover = table.Column<bool>(type: "boolean", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityMedia", x => x.Id);
                    table.UniqueConstraint("AK_CommunityMedia_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunityMedia_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityPolicy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberPosting = table.Column<bool>(type: "boolean", nullable: false),
                    PublicAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    BlockingAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    PostStages = table.Column<string>(type: "text", nullable: false),
                    EventStages = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityPolicy", x => x.Id);
                    table.UniqueConstraint("AK_CommunityPolicy_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunityPolicy_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Snapshot = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityVersion", x => x.Id);
                    table.UniqueConstraint("AK_CommunityVersion_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunityVersion_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoHosts = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Venue = table.Column<string>(type: "text", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    OnlineUrl = table.Column<string>(type: "text", nullable: true),
                    TimeZone = table.Column<string>(type: "text", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RegistrationOpens = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RegistrationCloses = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Visibility = table.Column<string>(type: "text", nullable: false),
                    Audience = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    MemberIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    CategoryIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    CommitteeIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    LeadersOnly = table.Column<bool>(type: "boolean", nullable: false),
                    InvitationOnly = table.Column<bool>(type: "boolean", nullable: false),
                    AllowGuests = table.Column<bool>(type: "boolean", nullable: false),
                    MaxCompanions = table.Column<int>(type: "integer", nullable: false),
                    RegistrationApproval = table.Column<bool>(type: "boolean", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Questions = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Stages = table.Column<string>(type: "text", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityEvent", x => x.Id);
                    table.UniqueConstraint("AK_CommunityEvent_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunityEvent_ActivityCategory_TenantId_CategoryId",
                        columns: x => new { x.TenantId, x.CategoryId },
                        principalTable: "ActivityCategory",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunityEvent_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunityEvent_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityPost",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    CommitteeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Official = table.Column<bool>(type: "boolean", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    Mentions = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    Audience = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    Visibility = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Stages = table.Column<string>(type: "text", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Featured = table.Column<bool>(type: "boolean", nullable: false),
                    Achievement = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityPost", x => x.Id);
                    table.UniqueConstraint("AK_ActivityPost_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ActivityPost_ActivityCategory_TenantId_CategoryId",
                        columns: x => new { x.TenantId, x.CategoryId },
                        principalTable: "ActivityCategory",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityPost_Committees_TenantId_CommitteeId",
                        columns: x => new { x.TenantId, x.CommitteeId },
                        principalTable: "Committees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityPost_CommunityEvent_TenantId_EventId",
                        columns: x => new { x.TenantId, x.EventId },
                        principalTable: "CommunityEvent",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityPost_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityPost_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventInvitation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventInvitation", x => x.Id);
                    table.UniqueConstraint("AK_EventInvitation_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_EventInvitation_CommunityEvent_TenantId_EventId",
                        columns: x => new { x.TenantId, x.EventId },
                        principalTable: "CommunityEvent",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventInvitation_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventInvitation_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventRegistration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Companions = table.Column<int>(type: "integer", nullable: false),
                    Answers = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRegistration", x => x.Id);
                    table.UniqueConstraint("AK_EventRegistration_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_EventRegistration_CommunityEvent_TenantId_EventId",
                        columns: x => new { x.TenantId, x.EventId },
                        principalTable: "CommunityEvent",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventRegistration_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventRegistration_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventRsvp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Response = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRsvp", x => x.Id);
                    table.UniqueConstraint("AK_EventRsvp_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_EventRsvp_CommunityEvent_TenantId_EventId",
                        columns: x => new { x.TenantId, x.EventId },
                        principalTable: "CommunityEvent",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventRsvp_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityBookmark",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityBookmark", x => x.Id);
                    table.UniqueConstraint("AK_ActivityBookmark_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ActivityBookmark_ActivityPost_TenantId_PostId",
                        columns: x => new { x.TenantId, x.PostId },
                        principalTable: "ActivityPost",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityBookmark_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityComment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Hidden = table.Column<bool>(type: "boolean", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityComment", x => x.Id);
                    table.UniqueConstraint("AK_ActivityComment_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ActivityComment_ActivityComment_TenantId_ParentId",
                        columns: x => new { x.TenantId, x.ParentId },
                        principalTable: "ActivityComment",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityComment_ActivityPost_TenantId_PostId",
                        columns: x => new { x.TenantId, x.PostId },
                        principalTable: "ActivityPost",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityComment_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityReaction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityReaction", x => x.Id);
                    table.UniqueConstraint("AK_ActivityReaction_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ActivityReaction_ActivityPost_TenantId_PostId",
                        columns: x => new { x.TenantId, x.PostId },
                        principalTable: "ActivityPost",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityReaction_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityReport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityReport", x => x.Id);
                    table.UniqueConstraint("AK_ActivityReport_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ActivityReport_ActivityPost_TenantId_PostId",
                        columns: x => new { x.TenantId, x.PostId },
                        principalTable: "ActivityPost",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityReport_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventAttendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EntryPoint = table.Column<string>(type: "text", nullable: false),
                    Voided = table.Column<bool>(type: "boolean", nullable: false),
                    Correction = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAttendance", x => x.Id);
                    table.UniqueConstraint("AK_EventAttendance_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_EventAttendance_CommunityEvent_TenantId_EventId",
                        columns: x => new { x.TenantId, x.EventId },
                        principalTable: "CommunityEvent",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventAttendance_EventRegistration_TenantId_RegistrationId",
                        columns: x => new { x.TenantId, x.RegistrationId },
                        principalTable: "EventRegistration",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventAttendance_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventPass",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventPass", x => x.Id);
                    table.UniqueConstraint("AK_EventPass_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_EventPass_CommunityEvent_TenantId_EventId",
                        columns: x => new { x.TenantId, x.EventId },
                        principalTable: "CommunityEvent",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventPass_EventRegistration_TenantId_RegistrationId",
                        columns: x => new { x.TenantId, x.RegistrationId },
                        principalTable: "EventRegistration",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventPass_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityBookmark_TenantId_PostId_UserId",
                table: "ActivityBookmark",
                columns: new[] { "TenantId", "PostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityCategory_TenantId_Kind_Name",
                table: "ActivityCategory",
                columns: new[] { "TenantId", "Kind", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityComment_TenantId_ParentId",
                table: "ActivityComment",
                columns: new[] { "TenantId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityComment_TenantId_PostId_At",
                table: "ActivityComment",
                columns: new[] { "TenantId", "PostId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPost_Audience",
                table: "ActivityPost",
                column: "Audience")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPost_TenantId_CategoryId",
                table: "ActivityPost",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPost_TenantId_CommitteeId",
                table: "ActivityPost",
                columns: new[] { "TenantId", "CommitteeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPost_TenantId_EventId",
                table: "ActivityPost",
                columns: new[] { "TenantId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPost_TenantId_Status_PublishedAt_Id",
                table: "ActivityPost",
                columns: new[] { "TenantId", "Status", "PublishedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPost_TenantId_UnitId_PublishedAt",
                table: "ActivityPost",
                columns: new[] { "TenantId", "UnitId", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityReaction_TenantId_PostId_UserId",
                table: "ActivityReaction",
                columns: new[] { "TenantId", "PostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityReport_TenantId_PostId_UserId",
                table: "ActivityReport",
                columns: new[] { "TenantId", "PostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityBlock_TenantId_UserId_BlockedUserId",
                table: "CommunityBlock",
                columns: new[] { "TenantId", "UserId", "BlockedUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityEvent_Audience",
                table: "CommunityEvent",
                column: "Audience")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityEvent_TenantId_CategoryId",
                table: "CommunityEvent",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityEvent_TenantId_Status_StartsAt",
                table: "CommunityEvent",
                columns: new[] { "TenantId", "Status", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityEvent_TenantId_UnitId",
                table: "CommunityEvent",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPolicy_TenantId",
                table: "CommunityPolicy",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendance_TenantId_EventId_RegistrationId",
                table: "EventAttendance",
                columns: new[] { "TenantId", "EventId", "RegistrationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendance_TenantId_RegistrationId",
                table: "EventAttendance",
                columns: new[] { "TenantId", "RegistrationId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventInvitation_TenantId_EventId_Email",
                table: "EventInvitation",
                columns: new[] { "TenantId", "EventId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventInvitation_TenantId_MembershipId",
                table: "EventInvitation",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventInvitation_TokenHash",
                table: "EventInvitation",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventPass_TenantId_EventId",
                table: "EventPass",
                columns: new[] { "TenantId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventPass_TenantId_RegistrationId",
                table: "EventPass",
                columns: new[] { "TenantId", "RegistrationId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventPass_TokenHash",
                table: "EventPass",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistration_TenantId_EventId_Status_At",
                table: "EventRegistration",
                columns: new[] { "TenantId", "EventId", "Status", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistration_TenantId_EventId_UserId",
                table: "EventRegistration",
                columns: new[] { "TenantId", "EventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistration_TenantId_MembershipId",
                table: "EventRegistration",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventRsvp_TenantId_EventId_UserId",
                table: "EventRsvp",
                columns: new[] { "TenantId", "EventId", "UserId" },
                unique: true);
            migrationBuilder.Sql("""
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Chapter News','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Community Service','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Volunteer Activities','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Meetings','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Training','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Workshops','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Educational Programs','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Networking Events','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Celebrations','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Member Achievements','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Leadership Updates','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Awards','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Environmental Activities','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Health Programs','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Fundraising Updates','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Society Announcements','Activity',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Chapter Meeting','Event',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Committee Meeting','Event',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Training Program','Event',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Workshop','Event',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Community Service','Event',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Networking Event','Event',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Guest Lecture','Event',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Annual Celebration','Event',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Awards Ceremony','Event',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Regional Conference','Event',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'District Convention','Event',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Society Annual General Meeting','Event',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'National Convention','Event',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'International Conference','Event',true FROM "Tenants";
INSERT INTO "ActivityCategory" ("Id","TenantId","Name","Kind","Active") SELECT gen_random_uuid(),"Id",'Custom Event','Event',true FROM "Tenants";
INSERT INTO "CommunityPolicy" ("Id","TenantId","MemberPosting","PublicAllowed","BlockingAllowed","PostStages","EventStages","Revision") SELECT gen_random_uuid(),"Id",true,false,true,'["activities.approve"]','["events.approve"]',0 FROM "Tenants";
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions" || ARRAY['activities.read','activities.official','activities.broadcast','activities.approve','activities.moderate','activities.settings','activities.analytics','events.manage','events.approve','events.invite','events.checkin','events.analytics']::text[]))) WHERE "Name"='Society Administrator';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions" || ARRAY['activities.read','activities.official','activities.broadcast','activities.approve','activities.moderate','activities.analytics','events.manage','events.approve','events.invite','events.checkin','events.analytics']::text[]))) WHERE "Name" ~ '(President|Secretary|Coordinator|Governor|Manager)';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions" || ARRAY['activities.read']::text[]))) WHERE "Name"<>'Member Directory';
CREATE TRIGGER community_version_immutable BEFORE UPDATE OR DELETE ON "CommunityVersion" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation();
CREATE TRIGGER community_decision_immutable BEFORE UPDATE OR DELETE ON "CommunityDecision" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation();
ALTER TABLE "CommunityEvent" ADD CONSTRAINT event_dates_capacity CHECK ("EndsAt">"StartsAt" AND "RegistrationCloses">"RegistrationOpens" AND "Capacity">0 AND "MaxCompanions" BETWEEN 0 AND 10);
ALTER TABLE "EventRegistration" ADD CONSTRAINT companion_count CHECK ("Companions" BETWEEN 0 AND 10);
CREATE FUNCTION guard_event_reference() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
IF NOT EXISTS(SELECT 1 FROM "EventRegistration" r WHERE r."Id"=NEW."RegistrationId" AND r."TenantId"=NEW."TenantId" AND r."EventId"=NEW."EventId") THEN RAISE EXCEPTION 'Registration event mismatch'; END IF; RETURN NEW; END $$;
CREATE TRIGGER event_pass_match BEFORE INSERT OR UPDATE ON "EventPass" FOR EACH ROW EXECUTE FUNCTION guard_event_reference();
CREATE TRIGGER event_attendance_match BEFORE INSERT OR UPDATE ON "EventAttendance" FOR EACH ROW EXECUTE FUNCTION guard_event_reference();
CREATE INDEX activity_search ON "ActivityPost" USING gin ("Title" gin_trgm_ops);
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityBookmark");

            migrationBuilder.DropTable(
                name: "ActivityComment");

            migrationBuilder.DropTable(
                name: "ActivityReaction");

            migrationBuilder.DropTable(
                name: "ActivityReport");

            migrationBuilder.DropTable(
                name: "CommunityBlock");

            migrationBuilder.DropTable(
                name: "CommunityDecision");

            migrationBuilder.DropTable(
                name: "CommunityMedia");

            migrationBuilder.DropTable(
                name: "CommunityPolicy");

            migrationBuilder.DropTable(
                name: "CommunityVersion");

            migrationBuilder.DropTable(
                name: "EventAttendance");

            migrationBuilder.DropTable(
                name: "EventInvitation");

            migrationBuilder.DropTable(
                name: "EventPass");

            migrationBuilder.DropTable(
                name: "EventRsvp");

            migrationBuilder.DropTable(
                name: "ActivityPost");

            migrationBuilder.DropTable(
                name: "EventRegistration");

            migrationBuilder.DropTable(
                name: "CommunityEvent");

            migrationBuilder.DropTable(
                name: "ActivityCategory");
        }
    }
}
