using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class MembershipRenewalResourcesAndRepresentatives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MemberApplications_TenantId_UserId_ChapterId",
                table: "MemberApplications");

            migrationBuilder.AddColumn<bool>(
                name: "PendingPayment",
                table: "Memberships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RenewalReminders",
                table: "Memberships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "MembershipCategories",
                type: "text",
                nullable: false,
                defaultValue: "Individual");

            migrationBuilder.AddColumn<string>(
                name: "PreferredMembershipKind",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "Individual");

            migrationBuilder.AddColumn<Guid>(
                name: "SignupTenantId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InstitutionChapter",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstitutionChapter", x => new { x.TenantId, x.ChapterId });
                    table.ForeignKey(
                        name: "FK_InstitutionChapter_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstitutionChapter_Units_TenantId_ChapterId",
                        columns: x => new { x.TenantId, x.ChapterId },
                        principalTable: "Units",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberResource",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Audiences = table.Column<string[]>(type: "text[]", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Published = table.Column<bool>(type: "boolean", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberResource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberResource_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MembershipOffer",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    TermMonths = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    IncomeCode = table.Column<string>(type: "text", nullable: false),
                    PaymentRequired = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderDays = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipOffer", x => new { x.TenantId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_MembershipOffer_FinancialEntity_TenantId_EntityId",
                        columns: x => new { x.TenantId, x.EntityId },
                        principalTable: "FinancialEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipOffer_MembershipCategories_TenantId_CategoryId",
                        columns: x => new { x.TenantId, x.CategoryId },
                        principalTable: "MembershipCategories",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MembershipRenewal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    TermMonths = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipRenewal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembershipRenewal_FinanceInvoice_TenantId_InvoiceId",
                        columns: x => new { x.TenantId, x.InvoiceId },
                        principalTable: "FinanceInvoice",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipRenewal_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MembershipRenewalNotice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    TermEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipRenewalNotice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembershipRenewalNotice_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MembershipRepresentative",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipRepresentative", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembershipRepresentative_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipRepresentative_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberApplications_TenantId_UserId_ChapterId_CategoryId",
                table: "MemberApplications",
                columns: new[] { "TenantId", "UserId", "ChapterId", "CategoryId" },
                unique: true,
                filter: "\"Status\" NOT IN ('Rejected','Withdrawn')");

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionChapter_TenantId_MembershipId",
                table: "InstitutionChapter",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberResource_TenantId_Published",
                table: "MemberResource",
                columns: new[] { "TenantId", "Published" });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipOffer_TenantId_EntityId",
                table: "MembershipOffer",
                columns: new[] { "TenantId", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipRenewal_InvoiceId",
                table: "MembershipRenewal",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembershipRenewal_TenantId_InvoiceId",
                table: "MembershipRenewal",
                columns: new[] { "TenantId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipRenewal_TenantId_MembershipId",
                table: "MembershipRenewal",
                columns: new[] { "TenantId", "MembershipId" },
                unique: true,
                filter: "\"State\"='Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipRenewalNotice_TenantId_MembershipId_TermEnd",
                table: "MembershipRenewalNotice",
                columns: new[] { "TenantId", "MembershipId", "TermEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembershipRepresentative_TenantId_MembershipId_UserId",
                table: "MembershipRepresentative",
                columns: new[] { "TenantId", "MembershipId", "UserId" },
                unique: true,
                filter: "\"RevokedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipRepresentative_UserId_TenantId",
                table: "MembershipRepresentative",
                columns: new[] { "UserId", "TenantId" });
            migrationBuilder.Sql("""
ALTER TABLE "MembershipCategories" ADD CONSTRAINT "CK_MembershipCategory_Kind" CHECK ("Kind" IN ('Individual','Institutional','Corporate'));
ALTER TABLE "AspNetUsers" ADD CONSTRAINT "CK_Account_MembershipKind" CHECK ("PreferredMembershipKind" IN ('Individual','Institutional','Corporate'));
ALTER TABLE "MembershipOffer" ADD CONSTRAINT "CK_MembershipOffer_Values" CHECK ("TermMonths" BETWEEN 1 AND 120 AND "ReminderDays" BETWEEN 1 AND 90 AND ("Price" IS NULL OR "Price">0));
ALTER TABLE "MembershipRenewal" ADD CONSTRAINT "CK_MembershipRenewal_Values" CHECK ("TermMonths" BETWEEN 1 AND 120 AND "Amount">0 AND "State" IN ('Pending','Applied','Payment Reversed','Cancelled'));
ALTER TABLE "MembershipRepresentative" ADD CONSTRAINT "CK_MembershipRepresentative_Role" CHECK ("Role" IN ('Controller','Admin','Coordinator') AND ("EndsAt" IS NULL OR "EndsAt">"StartsAt"));
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"MembershipCategories\" DROP CONSTRAINT \"CK_MembershipCategory_Kind\"; ALTER TABLE \"AspNetUsers\" DROP CONSTRAINT \"CK_Account_MembershipKind\";");
            migrationBuilder.DropTable(
                name: "InstitutionChapter");

            migrationBuilder.DropTable(
                name: "MemberResource");

            migrationBuilder.DropTable(
                name: "MembershipOffer");

            migrationBuilder.DropTable(
                name: "MembershipRenewal");

            migrationBuilder.DropTable(
                name: "MembershipRenewalNotice");

            migrationBuilder.DropTable(
                name: "MembershipRepresentative");

            migrationBuilder.DropIndex(
                name: "IX_MemberApplications_TenantId_UserId_ChapterId_CategoryId",
                table: "MemberApplications");

            migrationBuilder.DropColumn(
                name: "PendingPayment",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "RenewalReminders",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "MembershipCategories");

            migrationBuilder.DropColumn(
                name: "PreferredMembershipKind",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SignupTenantId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_MemberApplications_TenantId_UserId_ChapterId",
                table: "MemberApplications",
                columns: new[] { "TenantId", "UserId", "ChapterId" },
                unique: true,
                filter: "\"Status\" NOT IN ('Rejected','Withdrawn')");
        }
    }
}
