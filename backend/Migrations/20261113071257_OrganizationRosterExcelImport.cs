using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationRosterExcelImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationMember_TenantId_Number",
                table: "OrganizationMember");

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "OrganizationMember",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<string>(
                name: "Mobile",
                table: "OrganizationMember",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumberKey",
                table: "OrganizationMember",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceKey",
                table: "OrganizationMember",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrganizationRosterImport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CommittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Rows = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationRosterImport", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationRosterImport_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationRosterImport_Memberships_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalTable: "Memberships",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            // Preserve old IDs and references. Historical duplicate references remain intact;
            // the import validator checks them without forcing destructive cleanup.
            migrationBuilder.Sql("""
                UPDATE "OrganizationMember" SET "NumberKey" = upper(regexp_replace(normalize("Number", NFKC), '\s+', '', 'g'));
                WITH unique_refs AS (
                    SELECT "TenantId", "MembershipId", upper(regexp_replace(normalize("Reference", NFKC), '\s+', '', 'g')) AS key
                    FROM "OrganizationMember" WHERE "RevokedAt" IS NULL AND btrim("Reference") <> ''
                    GROUP BY "TenantId", "MembershipId", upper(regexp_replace(normalize("Reference", NFKC), '\s+', '', 'g')) HAVING count(*) = 1
                ) UPDATE "OrganizationMember" m SET "ReferenceKey" = r.key FROM unique_refs r
                WHERE m."TenantId" = r."TenantId" AND m."MembershipId" = r."MembershipId" AND m."RevokedAt" IS NULL
                  AND upper(regexp_replace(normalize(m."Reference", NFKC), '\s+', '', 'g')) = r.key;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMember_TenantId_MembershipId_Mobile",
                table: "OrganizationMember",
                columns: new[] { "TenantId", "MembershipId", "Mobile" },
                unique: true,
                filter: "\"RevokedAt\" IS NULL AND \"Mobile\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMember_TenantId_MembershipId_NumberKey",
                table: "OrganizationMember",
                columns: new[] { "TenantId", "MembershipId", "NumberKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMember_TenantId_MembershipId_ReferenceKey",
                table: "OrganizationMember",
                columns: new[] { "TenantId", "MembershipId", "ReferenceKey" },
                unique: true,
                filter: "\"RevokedAt\" IS NULL AND \"ReferenceKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRosterImport_ActorId",
                table: "OrganizationRosterImport",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRosterImport_TenantId_MembershipId",
                table: "OrganizationRosterImport",
                columns: new[] { "TenantId", "MembershipId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationRosterImport");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationMember_TenantId_MembershipId_Mobile",
                table: "OrganizationMember");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationMember_TenantId_MembershipId_NumberKey",
                table: "OrganizationMember");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationMember_TenantId_MembershipId_ReferenceKey",
                table: "OrganizationMember");

            migrationBuilder.DropColumn(
                name: "Mobile",
                table: "OrganizationMember");

            migrationBuilder.DropColumn(
                name: "NumberKey",
                table: "OrganizationMember");

            migrationBuilder.DropColumn(
                name: "ReferenceKey",
                table: "OrganizationMember");

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "OrganizationMember",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMember_TenantId_Number",
                table: "OrganizationMember",
                columns: new[] { "TenantId", "Number" },
                unique: true);
        }
    }
}
