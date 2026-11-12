using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class UnifiedFinancialEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LegalName = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    TaxRegistration = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    FiscalMonth = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    InvoicePrefix = table.Column<string>(type: "text", nullable: false),
                    ReceiptPrefix = table.Column<string>(type: "text", nullable: false),
                    NextInvoice = table.Column<long>(type: "bigint", nullable: false),
                    NextReceipt = table.Column<long>(type: "bigint", nullable: false),
                    NextJournal = table.Column<long>(type: "bigint", nullable: false),
                    RequireInvoiceApproval = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalLimit = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialEntity", x => x.Id);
                    table.UniqueConstraint("AK_FinancialEntity_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinancialEntity_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingPeriod",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Closed = table.Column<bool>(type: "boolean", nullable: false),
                    ClosedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPeriod", x => x.Id);
                    table.UniqueConstraint("AK_AccountingPeriod_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_AccountingPeriod_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_AccountingPeriod_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingPeriod_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingPeriod_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DuesPlan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Target = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Frequency = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    PerMember = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Proration = table.Column<string>(type: "text", nullable: false),
                    GraceDays = table.Column<int>(type: "integer", nullable: false),
                    LateFee = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    IncomeCode = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    AutoBill = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuesPlan", x => x.Id);
                    table.UniqueConstraint("AK_DuesPlan_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_DuesPlan_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DuesPlan_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuesPlan_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuesPlan_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventFinancePolicy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberPrice = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    GuestPrice = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    AllowManualSettlement = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventFinancePolicy", x => x.Id);
                    table.UniqueConstraint("AK_EventFinancePolicy_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_EventFinancePolicy_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_EventFinancePolicy_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventFinancePolicy_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventFinancePolicy_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceBank",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LastFour = table.Column<string>(type: "text", nullable: false),
                    ProtectedDetails = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    AccountCode = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceBank", x => x.Id);
                    table.UniqueConstraint("AK_FinanceBank_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinanceBank_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinanceBank_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceBank_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceBank_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceDonor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Contact = table.Column<string>(type: "text", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceDonor", x => x.Id);
                    table.UniqueConstraint("AK_FinanceDonor_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinanceDonor_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinanceDonor_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceDonor_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceDonor_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceExchangeRate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromCurrency = table.Column<string>(type: "text", nullable: false),
                    ToCurrency = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(24,10)", precision: 24, scale: 10, nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceExchangeRate", x => x.Id);
                    table.UniqueConstraint("AK_FinanceExchangeRate_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinanceExchangeRate_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinanceExchangeRate_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceExchangeRate_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceExchangeRate_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceFile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceFile", x => x.Id);
                    table.UniqueConstraint("AK_FinanceFile_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinanceFile_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinanceFile_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceFile_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceFile_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceJob",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Cursor = table.Column<string>(type: "text", nullable: false),
                    Processed = table.Column<int>(type: "integer", nullable: false),
                    Failed = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Key = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceJob", x => x.Id);
                    table.UniqueConstraint("AK_FinanceJob_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinanceJob_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinanceJob_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceJob_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceJob_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancePayment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    PayerUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    DonorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Payer = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    Method = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Refunded = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    ChargedBack = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    SettledOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderReference = table.Column<string>(type: "text", nullable: true),
                    JournalId = table.Column<Guid>(type: "uuid", nullable: true),
                    BankId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePayment", x => x.Id);
                    table.UniqueConstraint("AK_FinancePayment_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinancePayment_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinancePayment_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancePayment_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancePayment_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancePayment_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceReminder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DaysFromDue = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceReminder", x => x.Id);
                    table.UniqueConstraint("AK_FinanceReminder_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinanceReminder_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinanceReminder_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceReminder_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceReminder_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialUnit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialUnit", x => x.Id);
                    table.UniqueConstraint("AK_FinancialUnit_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinancialUnit_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinancialUnit_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialUnit_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialUnit_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InterEntityObligation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OtherEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    OtherUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Settled = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    JournalId = table.Column<Guid>(type: "uuid", nullable: true),
                    OtherJournalId = table.Column<Guid>(type: "uuid", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterEntityObligation", x => x.Id);
                    table.UniqueConstraint("AK_InterEntityObligation_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_InterEntityObligation_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_InterEntityObligation_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterEntityObligation_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterEntityObligation_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LedgerAccount",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Control = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerAccount", x => x.Id);
                    table.UniqueConstraint("AK_LedgerAccount_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_LedgerAccount_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_LedgerAccount_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LedgerAccount_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LedgerAccount_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentConnection",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    PublicKey = table.Column<string>(type: "text", nullable: false),
                    ProtectedSecrets = table.Column<string>(type: "text", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Verified = table.Column<bool>(type: "boolean", nullable: false),
                    Health = table.Column<string>(type: "text", nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentConnection", x => x.Id);
                    table.UniqueConstraint("AK_PaymentConnection_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_PaymentConnection_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_PaymentConnection_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentConnection_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentConnection_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceBudget",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    CommitteeId = table.Column<Guid>(type: "uuid", nullable: true),
                    PeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Lines = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceBudget", x => x.Id);
                    table.UniqueConstraint("AK_FinanceBudget_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinanceBudget_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinanceBudget_AccountingPeriod_TenantId_EntityId_PeriodId",
                        columns: x => new { x.TenantId, x.EntityId, x.PeriodId },
                        principalTable: "AccountingPeriod",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceBudget_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceBudget_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceBudget_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    SourceKind = table.Column<string>(type: "text", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceKey = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Posted = table.Column<bool>(type: "boolean", nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReversesId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntry", x => x.Id);
                    table.UniqueConstraint("AK_JournalEntry_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_JournalEntry_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_JournalEntry_AccountingPeriod_TenantId_EntityId_PeriodId",
                        columns: x => new { x.TenantId, x.EntityId, x.PeriodId },
                        principalTable: "AccountingPeriod",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntry_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntry_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntry_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DuesExemption",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Percent = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuesExemption", x => x.Id);
                    table.UniqueConstraint("AK_DuesExemption_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_DuesExemption_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DuesExemption_DuesPlan_TenantId_EntityId_PlanId",
                        columns: x => new { x.TenantId, x.EntityId, x.PlanId },
                        principalTable: "DuesPlan",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuesExemption_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuesExemption_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuesExemption_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuesExemption_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankTransaction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BankId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Fingerprint = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankTransaction", x => x.Id);
                    table.UniqueConstraint("AK_BankTransaction_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_BankTransaction_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_BankTransaction_FinanceBank_TenantId_EntityId_BankId",
                        columns: x => new { x.TenantId, x.EntityId, x.BankId },
                        principalTable: "FinanceBank",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankTransaction_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankTransaction_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankTransaction_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceReceipt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Snapshot = table.Column<string>(type: "text", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceReceipt", x => x.Id);
                    table.UniqueConstraint("AK_FinanceReceipt_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinanceReceipt_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinanceReceipt_FinancePayment_TenantId_EntityId_PaymentId",
                        columns: x => new { x.TenantId, x.EntityId, x.PaymentId },
                        principalTable: "FinancePayment",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceReceipt_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceReceipt_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceReceipt_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceRefund",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ProviderReference = table.Column<string>(type: "text", nullable: true),
                    JournalId = table.Column<Guid>(type: "uuid", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceRefund", x => x.Id);
                    table.UniqueConstraint("AK_FinanceRefund_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinanceRefund_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinanceRefund_FinancePayment_TenantId_EntityId_PaymentId",
                        columns: x => new { x.TenantId, x.EntityId, x.PaymentId },
                        principalTable: "FinancePayment",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceRefund_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceRefund_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceRefund_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InterEntitySettlement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObligationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    JournalId = table.Column<Guid>(type: "uuid", nullable: true),
                    OtherJournalId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterEntitySettlement", x => x.Id);
                    table.UniqueConstraint("AK_InterEntitySettlement_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_InterEntitySettlement_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_InterEntitySettlement_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterEntitySettlement_InterEntityObligation_TenantId_Entity~",
                        columns: x => new { x.TenantId, x.EntityId, x.ObligationId },
                        principalTable: "InterEntityObligation",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterEntitySettlement_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterEntitySettlement_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceProviderEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventKey = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceProviderEvent", x => x.Id);
                    table.UniqueConstraint("AK_FinanceProviderEvent_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinanceProviderEvent_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinanceProviderEvent_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceProviderEvent_PaymentConnection_TenantId_EntityId_Co~",
                        columns: x => new { x.TenantId, x.EntityId, x.ConnectionId },
                        principalTable: "PaymentConnection",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceProviderEvent_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceProviderEvent_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetRevision",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Snapshot = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetRevision", x => x.Id);
                    table.UniqueConstraint("AK_BudgetRevision_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_BudgetRevision_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_BudgetRevision_FinanceBudget_TenantId_EntityId_BudgetId",
                        columns: x => new { x.TenantId, x.EntityId, x.BudgetId },
                        principalTable: "FinanceBudget",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetRevision_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetRevision_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetRevision_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceExpense",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    CommitteeId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Vendor = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    AccountCode = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    JournalId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentJournalId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentReference = table.Column<string>(type: "text", nullable: false),
                    BankId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceExpense", x => x.Id);
                    table.UniqueConstraint("AK_FinanceExpense_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinanceExpense_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinanceExpense_FinanceBudget_TenantId_EntityId_BudgetId",
                        columns: x => new { x.TenantId, x.EntityId, x.BudgetId },
                        principalTable: "FinanceBudget",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceExpense_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceExpense_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceExpense_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceInvoice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Number = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    OriginalId = table.Column<Guid>(type: "uuid", nullable: true),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    BilledUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    DonorId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    RegistrationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    CounterpartyName = table.Column<string>(type: "text", nullable: false),
                    CounterpartyAddress = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    DueOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Lines = table.Column<string>(type: "text", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Tax = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Paid = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Credited = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    IssuerSnapshot = table.Column<string>(type: "text", nullable: false),
                    JournalId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceInvoice", x => x.Id);
                    table.UniqueConstraint("AK_FinanceInvoice_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinanceInvoice_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinanceInvoice_FinanceInvoice_TenantId_EntityId_OriginalId",
                        columns: x => new { x.TenantId, x.EntityId, x.OriginalId },
                        principalTable: "FinanceInvoice",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceInvoice_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceInvoice_JournalEntry_TenantId_EntityId_JournalId",
                        columns: x => new { x.TenantId, x.EntityId, x.JournalId },
                        principalTable: "JournalEntry",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceInvoice_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceInvoice_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceInvoice_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Debit = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Memo = table.Column<string>(type: "text", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    CounterpartyUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalLine", x => x.Id);
                    table.UniqueConstraint("AK_JournalLine_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_JournalLine_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_JournalLine_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLine_JournalEntry_TenantId_EntityId_JournalId",
                        columns: x => new { x.TenantId, x.EntityId, x.JournalId },
                        principalTable: "JournalEntry",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLine_LedgerAccount_TenantId_EntityId_AccountId",
                        columns: x => new { x.TenantId, x.EntityId, x.AccountId },
                        principalTable: "LedgerAccount",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLine_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLine_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLine_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationMatch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BankTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationMatch", x => x.Id);
                    table.UniqueConstraint("AK_ReconciliationMatch_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_ReconciliationMatch_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ReconciliationMatch_BankTransaction_TenantId_EntityId_BankT~",
                        columns: x => new { x.TenantId, x.EntityId, x.BankTransactionId },
                        principalTable: "BankTransaction",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReconciliationMatch_FinancePayment_TenantId_EntityId_Paymen~",
                        columns: x => new { x.TenantId, x.EntityId, x.PaymentId },
                        principalTable: "FinancePayment",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReconciliationMatch_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReconciliationMatch_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReconciliationMatch_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DuesAssessment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: true),
                    CounterpartyKey = table.Column<string>(type: "text", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    DueOn = table.Column<DateOnly>(type: "date", nullable: false),
                    MemberCount = table.Column<int>(type: "integer", nullable: false),
                    Snapshot = table.Column<string>(type: "text", nullable: false),
                    BaseAmount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    PerMemberAmount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuesAssessment", x => x.Id);
                    table.UniqueConstraint("AK_DuesAssessment_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_DuesAssessment_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DuesAssessment_DuesPlan_TenantId_EntityId_PlanId",
                        columns: x => new { x.TenantId, x.EntityId, x.PlanId },
                        principalTable: "DuesPlan",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuesAssessment_FinanceInvoice_TenantId_EntityId_InvoiceId",
                        columns: x => new { x.TenantId, x.EntityId, x.InvoiceId },
                        principalTable: "FinanceInvoice",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuesAssessment_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuesAssessment_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuesAssessment_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuesAssessment_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceContribution",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DonorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    Pledged = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    DueOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Restricted = table.Column<bool>(type: "boolean", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceContribution", x => x.Id);
                    table.UniqueConstraint("AK_FinanceContribution_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinanceContribution_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinanceContribution_FinanceDonor_TenantId_EntityId_DonorId",
                        columns: x => new { x.TenantId, x.EntityId, x.DonorId },
                        principalTable: "FinanceDonor",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceContribution_FinanceInvoice_TenantId_EntityId_Invoic~",
                        columns: x => new { x.TenantId, x.EntityId, x.InvoiceId },
                        principalTable: "FinanceInvoice",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceContribution_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceContribution_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceContribution_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceReminderRun",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cycle = table.Column<string>(type: "text", nullable: false),
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceReminderRun", x => x.Id);
                    table.UniqueConstraint("AK_FinanceReminderRun_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_FinanceReminderRun_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinanceReminderRun_FinanceInvoice_TenantId_EntityId_Invoice~",
                        columns: x => new { x.TenantId, x.EntityId, x.InvoiceId },
                        principalTable: "FinanceInvoice",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceReminderRun_FinanceReminder_TenantId_EntityId_RuleId",
                        columns: x => new { x.TenantId, x.EntityId, x.RuleId },
                        principalTable: "FinanceReminder",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceReminderRun_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceReminderRun_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceReminderRun_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    JournalId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocation", x => x.Id);
                    table.UniqueConstraint("AK_PaymentAllocation_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_PaymentAllocation_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_PaymentAllocation_FinanceInvoice_TenantId_EntityId_InvoiceId",
                        columns: x => new { x.TenantId, x.EntityId, x.InvoiceId },
                        principalTable: "FinanceInvoice",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocation_FinancePayment_TenantId_EntityId_PaymentId",
                        columns: x => new { x.TenantId, x.EntityId, x.PaymentId },
                        principalTable: "FinancePayment",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocation_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocation_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocation_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentIntent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    OrderId = table.Column<string>(type: "text", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Key = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentIntent", x => x.Id);
                    table.UniqueConstraint("AK_PaymentIntent_TenantId_EntityId_Id", x => new { x.TenantId, x.EntityId, x.Id });
                    table.UniqueConstraint("AK_PaymentIntent_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_PaymentIntent_FinanceInvoice_TenantId_EntityId_InvoiceId",
                        columns: x => new { x.TenantId, x.EntityId, x.InvoiceId },
                        principalTable: "FinanceInvoice",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentIntent_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentIntent_PaymentConnection_TenantId_EntityId_Connectio~",
                        columns: x => new { x.TenantId, x.EntityId, x.ConnectionId },
                        principalTable: "PaymentConnection",
                        principalColumns: new[] { "TenantId", "EntityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentIntent_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentIntent_Units_TenantId_UnitId",
                        columns: x => new { x.TenantId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriod_TenantId_EntityId_UnitId",
                table: "AccountingPeriod",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriod_TenantId_UnitId",
                table: "AccountingPeriod",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_BankTransaction_TenantId_EntityId_BankId_Fingerprint",
                table: "BankTransaction",
                columns: new[] { "TenantId", "EntityId", "BankId", "Fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankTransaction_TenantId_EntityId_UnitId",
                table: "BankTransaction",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_BankTransaction_TenantId_UnitId",
                table: "BankTransaction",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetRevision_TenantId_EntityId_BudgetId",
                table: "BudgetRevision",
                columns: new[] { "TenantId", "EntityId", "BudgetId" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetRevision_TenantId_EntityId_UnitId",
                table: "BudgetRevision",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetRevision_TenantId_UnitId",
                table: "BudgetRevision",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_DuesAssessment_TenantId_EntityId_InvoiceId",
                table: "DuesAssessment",
                columns: new[] { "TenantId", "EntityId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_DuesAssessment_TenantId_EntityId_PlanId_CounterpartyKey_Sta~",
                table: "DuesAssessment",
                columns: new[] { "TenantId", "EntityId", "PlanId", "CounterpartyKey", "StartsOn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuesAssessment_TenantId_EntityId_UnitId",
                table: "DuesAssessment",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_DuesAssessment_TenantId_MembershipId",
                table: "DuesAssessment",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_DuesAssessment_TenantId_UnitId",
                table: "DuesAssessment",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_DuesExemption_TenantId_EntityId_PlanId",
                table: "DuesExemption",
                columns: new[] { "TenantId", "EntityId", "PlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_DuesExemption_TenantId_EntityId_UnitId",
                table: "DuesExemption",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_DuesExemption_TenantId_MembershipId",
                table: "DuesExemption",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_DuesExemption_TenantId_UnitId",
                table: "DuesExemption",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_DuesPlan_TenantId_EntityId_Code",
                table: "DuesPlan",
                columns: new[] { "TenantId", "EntityId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuesPlan_TenantId_EntityId_UnitId",
                table: "DuesPlan",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_DuesPlan_TenantId_UnitId",
                table: "DuesPlan",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventFinancePolicy_TenantId_EntityId_UnitId",
                table: "EventFinancePolicy",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventFinancePolicy_TenantId_EventId",
                table: "EventFinancePolicy",
                columns: new[] { "TenantId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventFinancePolicy_TenantId_UnitId",
                table: "EventFinancePolicy",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceBank_TenantId_EntityId_UnitId",
                table: "FinanceBank",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceBank_TenantId_UnitId",
                table: "FinanceBank",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceBudget_TenantId_EntityId_PeriodId",
                table: "FinanceBudget",
                columns: new[] { "TenantId", "EntityId", "PeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceBudget_TenantId_EntityId_UnitId",
                table: "FinanceBudget",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceBudget_TenantId_UnitId",
                table: "FinanceBudget",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceContribution_TenantId_EntityId_DonorId",
                table: "FinanceContribution",
                columns: new[] { "TenantId", "EntityId", "DonorId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceContribution_TenantId_EntityId_InvoiceId",
                table: "FinanceContribution",
                columns: new[] { "TenantId", "EntityId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceContribution_TenantId_EntityId_UnitId",
                table: "FinanceContribution",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceContribution_TenantId_UnitId",
                table: "FinanceContribution",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceDonor_TenantId_EntityId_UnitId",
                table: "FinanceDonor",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceDonor_TenantId_UnitId",
                table: "FinanceDonor",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExchangeRate_TenantId_EntityId_FromCurrency_ToCurren~",
                table: "FinanceExchangeRate",
                columns: new[] { "TenantId", "EntityId", "FromCurrency", "ToCurrency", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExchangeRate_TenantId_EntityId_UnitId",
                table: "FinanceExchangeRate",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExchangeRate_TenantId_UnitId",
                table: "FinanceExchangeRate",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExpense_TenantId_EntityId_BudgetId",
                table: "FinanceExpense",
                columns: new[] { "TenantId", "EntityId", "BudgetId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExpense_TenantId_EntityId_UnitId",
                table: "FinanceExpense",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExpense_TenantId_UnitId",
                table: "FinanceExpense",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceFile_TenantId_EntityId_UnitId",
                table: "FinanceFile",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceFile_TenantId_UnitId",
                table: "FinanceFile",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvoice_TenantId_BilledUnitId_Date",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "BilledUnitId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvoice_TenantId_EntityId_JournalId",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "EntityId", "JournalId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvoice_TenantId_EntityId_Number",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "EntityId", "Number" },
                unique: true,
                filter: "\"Number\"<>''");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvoice_TenantId_EntityId_OriginalId",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "EntityId", "OriginalId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvoice_TenantId_EntityId_Status_DueOn",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "EntityId", "Status", "DueOn" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvoice_TenantId_EntityId_UnitId",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvoice_TenantId_MembershipId_Date",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "MembershipId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvoice_TenantId_RegistrationId",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "RegistrationId" },
                unique: true,
                filter: "\"RegistrationId\" IS NOT NULL AND \"Kind\"='Invoice'");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvoice_TenantId_UnitId",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceJob_State_At",
                table: "FinanceJob",
                columns: new[] { "State", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceJob_TenantId_EntityId_Key",
                table: "FinanceJob",
                columns: new[] { "TenantId", "EntityId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceJob_TenantId_EntityId_UnitId",
                table: "FinanceJob",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceJob_TenantId_UnitId",
                table: "FinanceJob",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayment_TenantId_EntityId_Reference",
                table: "FinancePayment",
                columns: new[] { "TenantId", "EntityId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayment_TenantId_EntityId_UnitId",
                table: "FinancePayment",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayment_TenantId_MembershipId",
                table: "FinancePayment",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayment_TenantId_ProviderId_ProviderReference",
                table: "FinancePayment",
                columns: new[] { "TenantId", "ProviderId", "ProviderReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayment_TenantId_UnitId",
                table: "FinancePayment",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceProviderEvent_ConnectionId_EventKey",
                table: "FinanceProviderEvent",
                columns: new[] { "ConnectionId", "EventKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceProviderEvent_TenantId_EntityId_ConnectionId",
                table: "FinanceProviderEvent",
                columns: new[] { "TenantId", "EntityId", "ConnectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceProviderEvent_TenantId_EntityId_UnitId",
                table: "FinanceProviderEvent",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceProviderEvent_TenantId_UnitId",
                table: "FinanceProviderEvent",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReceipt_TenantId_EntityId_Number",
                table: "FinanceReceipt",
                columns: new[] { "TenantId", "EntityId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReceipt_TenantId_EntityId_PaymentId",
                table: "FinanceReceipt",
                columns: new[] { "TenantId", "EntityId", "PaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReceipt_TenantId_EntityId_UnitId",
                table: "FinanceReceipt",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReceipt_TenantId_PaymentId",
                table: "FinanceReceipt",
                columns: new[] { "TenantId", "PaymentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReceipt_TenantId_UnitId",
                table: "FinanceReceipt",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRefund_TenantId_EntityId_PaymentId",
                table: "FinanceRefund",
                columns: new[] { "TenantId", "EntityId", "PaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRefund_TenantId_EntityId_UnitId",
                table: "FinanceRefund",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRefund_TenantId_UnitId",
                table: "FinanceRefund",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReminder_TenantId_EntityId_UnitId",
                table: "FinanceReminder",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReminder_TenantId_UnitId",
                table: "FinanceReminder",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReminderRun_RuleId_InvoiceId_Cycle",
                table: "FinanceReminderRun",
                columns: new[] { "RuleId", "InvoiceId", "Cycle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReminderRun_TenantId_EntityId_InvoiceId",
                table: "FinanceReminderRun",
                columns: new[] { "TenantId", "EntityId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReminderRun_TenantId_EntityId_RuleId",
                table: "FinanceReminderRun",
                columns: new[] { "TenantId", "EntityId", "RuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReminderRun_TenantId_EntityId_UnitId",
                table: "FinanceReminderRun",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReminderRun_TenantId_UnitId",
                table: "FinanceReminderRun",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialUnit_TenantId_EntityId_UnitId",
                table: "FinancialUnit",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialUnit_TenantId_UnitId",
                table: "FinancialUnit",
                columns: new[] { "TenantId", "UnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterEntityObligation_TenantId_EntityId_UnitId",
                table: "InterEntityObligation",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_InterEntityObligation_TenantId_UnitId",
                table: "InterEntityObligation",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_InterEntitySettlement_TenantId_EntityId_ObligationId",
                table: "InterEntitySettlement",
                columns: new[] { "TenantId", "EntityId", "ObligationId" });

            migrationBuilder.CreateIndex(
                name: "IX_InterEntitySettlement_TenantId_EntityId_UnitId",
                table: "InterEntitySettlement",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_InterEntitySettlement_TenantId_UnitId",
                table: "InterEntitySettlement",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_TenantId_EntityId_Date_Id",
                table: "JournalEntry",
                columns: new[] { "TenantId", "EntityId", "Date", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_TenantId_EntityId_PeriodId",
                table: "JournalEntry",
                columns: new[] { "TenantId", "EntityId", "PeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_TenantId_EntityId_SourceKey",
                table: "JournalEntry",
                columns: new[] { "TenantId", "EntityId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_TenantId_EntityId_UnitId",
                table: "JournalEntry",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_TenantId_UnitId",
                table: "JournalEntry",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLine_TenantId_EntityId_AccountId_JournalId",
                table: "JournalLine",
                columns: new[] { "TenantId", "EntityId", "AccountId", "JournalId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLine_TenantId_EntityId_JournalId",
                table: "JournalLine",
                columns: new[] { "TenantId", "EntityId", "JournalId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLine_TenantId_EntityId_UnitId",
                table: "JournalLine",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLine_TenantId_MembershipId",
                table: "JournalLine",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLine_TenantId_UnitId",
                table: "JournalLine",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAccount_TenantId_EntityId_Code",
                table: "LedgerAccount",
                columns: new[] { "TenantId", "EntityId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAccount_TenantId_EntityId_UnitId",
                table: "LedgerAccount",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAccount_TenantId_UnitId",
                table: "LedgerAccount",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocation_TenantId_EntityId_InvoiceId",
                table: "PaymentAllocation",
                columns: new[] { "TenantId", "EntityId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocation_TenantId_EntityId_Key",
                table: "PaymentAllocation",
                columns: new[] { "TenantId", "EntityId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocation_TenantId_EntityId_PaymentId",
                table: "PaymentAllocation",
                columns: new[] { "TenantId", "EntityId", "PaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocation_TenantId_EntityId_UnitId",
                table: "PaymentAllocation",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocation_TenantId_UnitId",
                table: "PaymentAllocation",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConnection_TenantId_EntityId_UnitId",
                table: "PaymentConnection",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConnection_TenantId_UnitId",
                table: "PaymentConnection",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntent_TenantId_ConnectionId_OrderId",
                table: "PaymentIntent",
                columns: new[] { "TenantId", "ConnectionId", "OrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntent_TenantId_EntityId_ConnectionId",
                table: "PaymentIntent",
                columns: new[] { "TenantId", "EntityId", "ConnectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntent_TenantId_EntityId_InvoiceId",
                table: "PaymentIntent",
                columns: new[] { "TenantId", "EntityId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntent_TenantId_EntityId_UnitId",
                table: "PaymentIntent",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntent_TenantId_UnitId",
                table: "PaymentIntent",
                columns: new[] { "TenantId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntent_TenantId_UserId_Key",
                table: "PaymentIntent",
                columns: new[] { "TenantId", "UserId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatch_BankTransactionId",
                table: "ReconciliationMatch",
                column: "BankTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatch_TenantId_EntityId_BankTransactionId",
                table: "ReconciliationMatch",
                columns: new[] { "TenantId", "EntityId", "BankTransactionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatch_TenantId_EntityId_PaymentId",
                table: "ReconciliationMatch",
                columns: new[] { "TenantId", "EntityId", "PaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatch_TenantId_EntityId_UnitId",
                table: "ReconciliationMatch",
                columns: new[] { "TenantId", "EntityId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatch_TenantId_UnitId",
                table: "ReconciliationMatch",
                columns: new[] { "TenantId", "UnitId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetRevision");

            migrationBuilder.DropTable(
                name: "DuesAssessment");

            migrationBuilder.DropTable(
                name: "DuesExemption");

            migrationBuilder.DropTable(
                name: "EventFinancePolicy");

            migrationBuilder.DropTable(
                name: "FinanceContribution");

            migrationBuilder.DropTable(
                name: "FinanceExchangeRate");

            migrationBuilder.DropTable(
                name: "FinanceExpense");

            migrationBuilder.DropTable(
                name: "FinanceFile");

            migrationBuilder.DropTable(
                name: "FinanceJob");

            migrationBuilder.DropTable(
                name: "FinanceProviderEvent");

            migrationBuilder.DropTable(
                name: "FinanceReceipt");

            migrationBuilder.DropTable(
                name: "FinanceRefund");

            migrationBuilder.DropTable(
                name: "FinanceReminderRun");

            migrationBuilder.DropTable(
                name: "FinancialUnit");

            migrationBuilder.DropTable(
                name: "InterEntitySettlement");

            migrationBuilder.DropTable(
                name: "JournalLine");

            migrationBuilder.DropTable(
                name: "PaymentAllocation");

            migrationBuilder.DropTable(
                name: "PaymentIntent");

            migrationBuilder.DropTable(
                name: "ReconciliationMatch");

            migrationBuilder.DropTable(
                name: "DuesPlan");

            migrationBuilder.DropTable(
                name: "FinanceDonor");

            migrationBuilder.DropTable(
                name: "FinanceBudget");

            migrationBuilder.DropTable(
                name: "FinanceReminder");

            migrationBuilder.DropTable(
                name: "InterEntityObligation");

            migrationBuilder.DropTable(
                name: "LedgerAccount");

            migrationBuilder.DropTable(
                name: "FinanceInvoice");

            migrationBuilder.DropTable(
                name: "PaymentConnection");

            migrationBuilder.DropTable(
                name: "BankTransaction");

            migrationBuilder.DropTable(
                name: "FinancePayment");

            migrationBuilder.DropTable(
                name: "JournalEntry");

            migrationBuilder.DropTable(
                name: "FinanceBank");

            migrationBuilder.DropTable(
                name: "AccountingPeriod");

            migrationBuilder.DropTable(
                name: "FinancialEntity");
        }
    }
}
