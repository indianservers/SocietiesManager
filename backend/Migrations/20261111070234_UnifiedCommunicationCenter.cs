using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class UnifiedCommunicationCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "Outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CampaignId",
                table: "Outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "Outbox",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "Outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeliveredAt",
                table: "Outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "Outbox",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Outbox",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeaseOwner",
                table: "Outbox",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LeaseUntil",
                table: "Outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MembershipId",
                table: "Outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MessageId",
                table: "Outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "Outbox",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "ProviderId",
                table: "Outbox",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReadAt",
                table: "Outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Outbox",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TemplateLanguage",
                table: "Outbox",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateName",
                table: "Outbox",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "Outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnsubscribeHash",
                table: "Outbox",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommunicationAccount",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Sender = table.Column<string>(type: "text", nullable: false),
                    ProviderReference = table.Column<string>(type: "text", nullable: false),
                    ProtectedSecrets = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Verified = table.Column<bool>(type: "boolean", nullable: false),
                    InboundEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Health = table.Column<string>(type: "text", nullable: false),
                    PerMinute = table.Column<int>(type: "integer", nullable: false),
                    WindowStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WindowCount = table.Column<int>(type: "integer", nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationAccount", x => x.Id);
                    table.UniqueConstraint("AK_CommunicationAccount_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunicationAccount_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationAccount_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationAudience",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Rules = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationAudience", x => x.Id);
                    table.UniqueConstraint("AK_CommunicationAudience_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunicationAudience_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationAudience_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationNotification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Link = table.Column<string>(type: "text", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DedupKey = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationNotification", x => x.Id);
                    table.UniqueConstraint("AK_CommunicationNotification_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunicationNotification_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationPolicy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DirectScope = table.Column<string>(type: "text", nullable: false),
                    DirectEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalStages = table.Column<string>(type: "text", nullable: false),
                    HighVolumeThreshold = table.Column<int>(type: "integer", nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationPolicy", x => x.Id);
                    table.UniqueConstraint("AK_CommunicationPolicy_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunicationPolicy_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationPreference",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DirectMessages = table.Column<bool>(type: "boolean", nullable: false),
                    InApp = table.Column<bool>(type: "boolean", nullable: false),
                    Email = table.Column<bool>(type: "boolean", nullable: false),
                    Sms = table.Column<bool>(type: "boolean", nullable: false),
                    WhatsApp = table.Column<bool>(type: "boolean", nullable: false),
                    TimeZone = table.Column<string>(type: "text", nullable: false),
                    QuietStart = table.Column<int>(type: "integer", nullable: false),
                    QuietEnd = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationPreference", x => x.Id);
                    table.UniqueConstraint("AK_CommunicationPreference_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunicationPreference_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationSuppression",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    DestinationHash = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationSuppression", x => x.Id);
                    table.UniqueConstraint("AK_CommunicationSuppression_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunicationSuppression_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ProviderName = table.Column<string>(type: "text", nullable: true),
                    ProviderStatus = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationTemplate", x => x.Id);
                    table.UniqueConstraint("AK_CommunicationTemplate_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunicationTemplate_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationTemplate_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryAttempt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    ProviderId = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric", nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryAttempt", x => x.Id);
                    table.UniqueConstraint("AK_DeliveryAttempt_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DeliveryAttempt_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Conversation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalAddress = table.Column<string>(type: "text", nullable: false),
                    ExternalMembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DirectKey = table.Column<string>(type: "text", nullable: true),
                    CommitteeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Posting = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    AssignedTo = table.Column<Guid>(type: "uuid", nullable: true),
                    FollowUpAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    Resolution = table.Column<string>(type: "text", nullable: false),
                    LastAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversation", x => x.Id);
                    table.UniqueConstraint("AK_Conversation_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Conversation_Committees_TenantId_CommitteeId",
                        columns: x => new { x.TenantId, x.CommitteeId },
                        principalTable: "Committees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversation_CommunicationAccount_TenantId_AccountId",
                        columns: x => new { x.TenantId, x.AccountId },
                        principalTable: "CommunicationAccount",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversation_Memberships_TenantId_ExternalMembershipId",
                        columns: x => new { x.TenantId, x.ExternalMembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversation_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversation_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProviderEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    ProviderId = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    PayloadHash = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderEvent", x => x.Id);
                    table.UniqueConstraint("AK_ProviderEvent_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProviderEvent_CommunicationAccount_TenantId_AccountId",
                        columns: x => new { x.TenantId, x.AccountId },
                        principalTable: "CommunicationAccount",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderEvent_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationAutomation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Trigger = table.Column<string>(type: "text", nullable: false),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    Audience = table.Column<string>(type: "text", nullable: false),
                    OffsetMinutes = table.Column<int>(type: "integer", nullable: false),
                    RepeatMinutes = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    TestMode = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastRun = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationAutomation", x => x.Id);
                    table.UniqueConstraint("AK_CommunicationAutomation_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunicationAutomation_CommunicationAccount_TenantId_Accou~",
                        columns: x => new { x.TenantId, x.AccountId },
                        principalTable: "CommunicationAccount",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationAutomation_CommunicationTemplate_TenantId_Temp~",
                        columns: x => new { x.TenantId, x.TemplateId },
                        principalTable: "CommunicationTemplate",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationAutomation_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationAutomation_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationCampaign",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotVersion = table.Column<int>(type: "integer", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Audience = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Stages = table.Column<string>(type: "text", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    Pinned = table.Column<bool>(type: "boolean", nullable: false),
                    Acknowledge = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SnapshotCount = table.Column<int>(type: "integer", nullable: false),
                    EnqueuedCount = table.Column<int>(type: "integer", nullable: false),
                    SnapshotAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationCampaign", x => x.Id);
                    table.UniqueConstraint("AK_CommunicationCampaign_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunicationCampaign_CommunicationAccount_TenantId_Account~",
                        columns: x => new { x.TenantId, x.AccountId },
                        principalTable: "CommunicationAccount",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationCampaign_CommunicationTemplate_TenantId_Templa~",
                        columns: x => new { x.TenantId, x.TemplateId },
                        principalTable: "CommunicationTemplate",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationCampaign_CommunityEvent_TenantId_EventId",
                        columns: x => new { x.TenantId, x.EventId },
                        principalTable: "CommunityEvent",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationCampaign_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationCampaign_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationMessage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Internal = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ClientKey = table.Column<string>(type: "text", nullable: false),
                    ReplyTo = table.Column<Guid>(type: "uuid", nullable: true),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RemovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationMessage", x => x.Id);
                    table.UniqueConstraint("AK_CommunicationMessage_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunicationMessage_CommunicationMessage_TenantId_ReplyTo",
                        columns: x => new { x.TenantId, x.ReplyTo },
                        principalTable: "CommunicationMessage",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationMessage_Conversation_TenantId_ConversationId",
                        columns: x => new { x.TenantId, x.ConversationId },
                        principalTable: "Conversation",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationMessage_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConversationParticipant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Muted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationParticipant", x => x.Id);
                    table.UniqueConstraint("AK_ConversationParticipant_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ConversationParticipant_Conversation_TenantId_ConversationId",
                        columns: x => new { x.TenantId, x.ConversationId },
                        principalTable: "Conversation",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConversationParticipant_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConversationParticipant_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutomationExecution",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceKey = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: true),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationExecution", x => x.Id);
                    table.UniqueConstraint("AK_AutomationExecution_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_AutomationExecution_CommunicationAutomation_TenantId_RuleId",
                        columns: x => new { x.TenantId, x.RuleId },
                        principalTable: "CommunicationAutomation",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutomationExecution_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampaignRecipient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotVersion = table.Column<int>(type: "integer", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Destination = table.Column<string>(type: "text", nullable: false),
                    Variables = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignRecipient", x => x.Id);
                    table.UniqueConstraint("AK_CampaignRecipient_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CampaignRecipient_CommunicationCampaign_TenantId_CampaignId",
                        columns: x => new { x.TenantId, x.CampaignId },
                        principalTable: "CommunicationCampaign",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignRecipient_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignRecipient_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationReport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationReport", x => x.Id);
                    table.UniqueConstraint("AK_CommunicationReport_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunicationReport_CommunicationMessage_TenantId_MessageId",
                        columns: x => new { x.TenantId, x.MessageId },
                        principalTable: "CommunicationMessage",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationReport_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessageAttachment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageAttachment", x => x.Id);
                    table.UniqueConstraint("AK_MessageAttachment_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MessageAttachment_CommunicationMessage_TenantId_MessageId",
                        columns: x => new { x.TenantId, x.MessageId },
                        principalTable: "CommunicationMessage",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageAttachment_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_IdempotencyKey",
                table: "Outbox",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_Status_NextAttemptAt_LeaseUntil",
                table: "Outbox",
                columns: new[] { "Status", "NextAttemptAt", "LeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_TenantId_AccountId_ProviderId",
                table: "Outbox",
                columns: new[] { "TenantId", "AccountId", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationExecution_TenantId_RuleId_SourceKey",
                table: "AutomationExecution",
                columns: new[] { "TenantId", "RuleId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignRecipient_CampaignId_Status_Id",
                table: "CampaignRecipient",
                columns: new[] { "CampaignId", "Status", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignRecipient_TenantId_CampaignId_SnapshotVersion_Perso~",
                table: "CampaignRecipient",
                columns: new[] { "TenantId", "CampaignId", "SnapshotVersion", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignRecipient_TenantId_MembershipId",
                table: "CampaignRecipient",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationAccount_TenantId_UnitId",
                table: "CommunicationAccount",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationAudience_TenantId_UnitId",
                table: "CommunicationAudience",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationAutomation_TenantId_AccountId",
                table: "CommunicationAutomation",
                columns: new[] { "TenantId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationAutomation_TenantId_TemplateId",
                table: "CommunicationAutomation",
                columns: new[] { "TenantId", "TemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationAutomation_TenantId_UnitId",
                table: "CommunicationAutomation",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationCampaign_Status_ScheduledAt",
                table: "CommunicationCampaign",
                columns: new[] { "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationCampaign_TenantId_AccountId",
                table: "CommunicationCampaign",
                columns: new[] { "TenantId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationCampaign_TenantId_EventId",
                table: "CommunicationCampaign",
                columns: new[] { "TenantId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationCampaign_TenantId_TemplateId",
                table: "CommunicationCampaign",
                columns: new[] { "TenantId", "TemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationCampaign_TenantId_UnitId",
                table: "CommunicationCampaign",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessage_TenantId_ConversationId_At_Id",
                table: "CommunicationMessage",
                columns: new[] { "TenantId", "ConversationId", "At", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessage_TenantId_ConversationId_SenderId_Clien~",
                table: "CommunicationMessage",
                columns: new[] { "TenantId", "ConversationId", "SenderId", "ClientKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessage_TenantId_ReplyTo",
                table: "CommunicationMessage",
                columns: new[] { "TenantId", "ReplyTo" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationNotification_TenantId_UserId_DedupKey",
                table: "CommunicationNotification",
                columns: new[] { "TenantId", "UserId", "DedupKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationNotification_UserId_ReadAt_At",
                table: "CommunicationNotification",
                columns: new[] { "UserId", "ReadAt", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationPolicy_TenantId",
                table: "CommunicationPolicy",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationPreference_TenantId_UserId",
                table: "CommunicationPreference",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationReport_TenantId_MessageId",
                table: "CommunicationReport",
                columns: new[] { "TenantId", "MessageId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationSuppression_TenantId_Channel_DestinationHash",
                table: "CommunicationSuppression",
                columns: new[] { "TenantId", "Channel", "DestinationHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTemplate_TenantId_Name_Language_Channel",
                table: "CommunicationTemplate",
                columns: new[] { "TenantId", "Name", "Language", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTemplate_TenantId_UnitId",
                table: "CommunicationTemplate",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_TenantId_AccountId",
                table: "Conversation",
                columns: new[] { "TenantId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_TenantId_CommitteeId",
                table: "Conversation",
                columns: new[] { "TenantId", "CommitteeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_TenantId_DirectKey",
                table: "Conversation",
                columns: new[] { "TenantId", "DirectKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_TenantId_ExternalMembershipId",
                table: "Conversation",
                columns: new[] { "TenantId", "ExternalMembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_TenantId_UnitId_LastAt",
                table: "Conversation",
                columns: new[] { "TenantId", "UnitId", "LastAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_TenantId_ConversationId_UserId",
                table: "ConversationParticipant",
                columns: new[] { "TenantId", "ConversationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_TenantId_MembershipId",
                table: "ConversationParticipant",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_TenantId_UserId_LeftAt",
                table: "ConversationParticipant",
                columns: new[] { "TenantId", "UserId", "LeftAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageAttachment_TenantId_MessageId",
                table: "MessageAttachment",
                columns: new[] { "TenantId", "MessageId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderEvent_TenantId_AccountId_ExternalId",
                table: "ProviderEvent",
                columns: new[] { "TenantId", "AccountId", "ExternalId" },
                unique: true);
            migrationBuilder.Sql("""
UPDATE "Outbox" SET "Status"=CASE WHEN "SentAt" IS NULL THEN 'Queued' ELSE 'LegacySent' END,"NextAttemptAt"="CreatedAt";
INSERT INTO "CommunicationPolicy" ("Id","TenantId","DirectScope","DirectEnabled","ApprovalStages","HighVolumeThreshold","RetentionDays","Revision") SELECT gen_random_uuid(),"Id",'Directory',true,'["communications.approve"]',1000,365,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'Welcome Member','InApp','Welcome Member','en','Welcome Member','Hello {{member_name}}, Welcome Member from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'Application Approved','InApp','Application Approved','en','Application Approved','Hello {{member_name}}, Application Approved from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'Application Rejected','InApp','Application Rejected','en','Application Rejected','Hello {{member_name}}, Application Rejected from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'Member Activation','InApp','Member Activation','en','Member Activation','Hello {{member_name}}, Member Activation from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'Chapter Announcement','InApp','Chapter Announcement','en','Chapter Announcement','Hello {{member_name}}, Chapter Announcement from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'Regional Announcement','InApp','Regional Announcement','en','Regional Announcement','Hello {{member_name}}, Regional Announcement from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'Society Circular','InApp','Society Circular','en','Society Circular','Hello {{member_name}}, Society Circular from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'Event Invitation','InApp','Event Invitation','en','Event Invitation','Hello {{member_name}}, Event Invitation from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'RSVP Reminder','InApp','RSVP Reminder','en','RSVP Reminder','Hello {{member_name}}, RSVP Reminder from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'Event Registration Confirmation','InApp','Event Registration Confirmation','en','Event Registration Confirmation','Hello {{member_name}}, Event Registration Confirmation from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'Event Cancellation','InApp','Event Cancellation','en','Event Cancellation','Hello {{member_name}}, Event Cancellation from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'Birthday Greeting','InApp','Birthday Greeting','en','Birthday Greeting','Hello {{member_name}}, Birthday Greeting from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'Membership Anniversary','InApp','Membership Anniversary','en','Membership Anniversary','Hello {{member_name}}, Membership Anniversary from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'Officer Appointment','InApp','Officer Appointment','en','Officer Appointment','Hello {{member_name}}, Officer Appointment from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'Leadership Transition','InApp','Leadership Transition','en','Leadership Transition','Hello {{member_name}}, Leadership Transition from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
INSERT INTO "CommunicationTemplate" ("Id","TenantId","UnitId","Name","Channel","Category","Language","Subject","Body","ProviderName","ProviderStatus","Active","Revision") SELECT gen_random_uuid(),"Id",NULL,'Meeting Notice','InApp','Meeting Notice','en','Meeting Notice','Hello {{member_name}}, Meeting Notice from {{society_name}}.',NULL,'Not submitted',true,0 FROM "Tenants";
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions" || ARRAY['communications.send','communications.approve','communications.inbox','communications.groups','communications.moderate','communications.templates','communications.accounts','communications.automations','communications.analytics','communications.settings','communications.highvolume']::text[]))) WHERE "Name"='Society Administrator';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions" || ARRAY['communications.send','communications.approve','communications.inbox','communications.groups','communications.moderate','communications.templates','communications.automations','communications.analytics']::text[]))) WHERE "Name" ~ '(President|Secretary|Coordinator|Governor|Manager)';
CREATE TRIGGER communication_attempt_immutable BEFORE UPDATE OR DELETE ON "DeliveryAttempt" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation();
CREATE TRIGGER provider_event_immutable BEFORE UPDATE OR DELETE ON "ProviderEvent" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation();
ALTER TABLE "ConversationParticipant" ADD CONSTRAINT participant_user_tenant FOREIGN KEY ("TenantId","UserId") REFERENCES "Links"("TenantId","UserId") ON DELETE RESTRICT;
ALTER TABLE "CommunicationPreference" ADD CONSTRAINT preference_user_tenant FOREIGN KEY ("TenantId","UserId") REFERENCES "Links"("TenantId","UserId") ON DELETE RESTRICT;
CREATE INDEX communication_message_search ON "CommunicationMessage" USING gin ("Body" gin_trgm_ops);
CREATE FUNCTION guard_communication_message() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
IF NEW."Internal" AND NOT EXISTS(SELECT 1 FROM "Conversation" c WHERE c."Id"=NEW."ConversationId" AND c."TenantId"=NEW."TenantId" AND c."Kind"='Team') THEN RAISE EXCEPTION 'Internal notes require an explicitly shared team inbox'; END IF;
IF NEW."ReplyTo" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM "CommunicationMessage" m WHERE m."Id"=NEW."ReplyTo" AND m."TenantId"=NEW."TenantId" AND m."ConversationId"=NEW."ConversationId") THEN RAISE EXCEPTION 'Reply conversation mismatch'; END IF;
RETURN NEW; END $$;
CREATE TRIGGER communication_message_integrity BEFORE INSERT OR UPDATE ON "CommunicationMessage" FOR EACH ROW EXECUTE FUNCTION guard_communication_message();
CREATE FUNCTION guard_communication_outbox() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
IF NEW."AccountId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM "CommunicationAccount" a WHERE a."Id"=NEW."AccountId" AND a."TenantId"=NEW."TenantId") THEN RAISE EXCEPTION 'Account tenant mismatch'; END IF;
IF NEW."CampaignId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM "CommunicationCampaign" c WHERE c."Id"=NEW."CampaignId" AND c."TenantId"=NEW."TenantId") THEN RAISE EXCEPTION 'Campaign tenant mismatch'; END IF;
RETURN NEW; END $$;
CREATE TRIGGER communication_outbox_integrity BEFORE INSERT OR UPDATE ON "Outbox" FOR EACH ROW EXECUTE FUNCTION guard_communication_outbox();
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER communication_outbox_integrity ON \"Outbox\"; DROP FUNCTION guard_communication_outbox(); DROP TRIGGER communication_message_integrity ON \"CommunicationMessage\"; DROP FUNCTION guard_communication_message();");

            migrationBuilder.DropTable(
                name: "AutomationExecution");

            migrationBuilder.DropTable(
                name: "CampaignRecipient");

            migrationBuilder.DropTable(
                name: "CommunicationAudience");

            migrationBuilder.DropTable(
                name: "CommunicationNotification");

            migrationBuilder.DropTable(
                name: "CommunicationPolicy");

            migrationBuilder.DropTable(
                name: "CommunicationPreference");

            migrationBuilder.DropTable(
                name: "CommunicationReport");

            migrationBuilder.DropTable(
                name: "CommunicationSuppression");

            migrationBuilder.DropTable(
                name: "ConversationParticipant");

            migrationBuilder.DropTable(
                name: "DeliveryAttempt");

            migrationBuilder.DropTable(
                name: "MessageAttachment");

            migrationBuilder.DropTable(
                name: "ProviderEvent");

            migrationBuilder.DropTable(
                name: "CommunicationAutomation");

            migrationBuilder.DropTable(
                name: "CommunicationCampaign");

            migrationBuilder.DropTable(
                name: "CommunicationMessage");

            migrationBuilder.DropTable(
                name: "CommunicationTemplate");

            migrationBuilder.DropTable(
                name: "Conversation");

            migrationBuilder.DropTable(
                name: "CommunicationAccount");

            migrationBuilder.DropIndex(
                name: "IX_Outbox_IdempotencyKey",
                table: "Outbox");

            migrationBuilder.DropIndex(
                name: "IX_Outbox_Status_NextAttemptAt_LeaseUntil",
                table: "Outbox");

            migrationBuilder.DropIndex(
                name: "IX_Outbox_TenantId_AccountId_ProviderId",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "Error",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "LeaseOwner",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "LeaseUntil",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "MembershipId",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "TemplateLanguage",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "TemplateName",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "UnsubscribeHash",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Outbox");
        }
    }
}
