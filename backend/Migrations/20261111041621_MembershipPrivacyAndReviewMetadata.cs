using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class MembershipPrivacyAndReviewMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DirectoryLevelId",
                table: "MembershipPolicies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "MembershipDecisions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ReviewerRoles",
                table: "MembershipDecisions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "MemberDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "MemberDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPolicies_TenantId_DirectoryLevelId",
                table: "MembershipPolicies",
                columns: new[] { "TenantId", "DirectoryLevelId" });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipDecisions_TenantId_OrganizationId",
                table: "MembershipDecisions",
                columns: new[] { "TenantId", "OrganizationId" });

            migrationBuilder.Sql("""
ALTER TABLE "MembershipDecisions" DISABLE TRIGGER member_decision_immutable;
UPDATE "MembershipDecisions" d SET "OrganizationId"=a."ChapterId","ReviewerRoles"='Legacy: permission recorded' FROM "MemberApplications" a WHERE a."Id"=d."ApplicationId" AND a."TenantId"=d."TenantId";
ALTER TABLE "MembershipDecisions" ENABLE TRIGGER member_decision_immutable;
UPDATE "MemberDocuments" SET "ContentType"='image/png',"Kind"='Supporting' WHERE "ContentType"='';
""");
            migrationBuilder.AddForeignKey(
                name: "FK_MembershipDecisions_Units_TenantId_OrganizationId",
                table: "MembershipDecisions",
                columns: new[] { "TenantId", "OrganizationId" },
                principalTable: "Units",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MembershipPolicies_Levels_TenantId_DirectoryLevelId",
                table: "MembershipPolicies",
                columns: new[] { "TenantId", "DirectoryLevelId" },
                principalTable: "Levels",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MembershipDecisions_Units_TenantId_OrganizationId",
                table: "MembershipDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_MembershipPolicies_Levels_TenantId_DirectoryLevelId",
                table: "MembershipPolicies");

            migrationBuilder.DropIndex(
                name: "IX_MembershipPolicies_TenantId_DirectoryLevelId",
                table: "MembershipPolicies");

            migrationBuilder.DropIndex(
                name: "IX_MembershipDecisions_TenantId_OrganizationId",
                table: "MembershipDecisions");

            migrationBuilder.DropColumn(
                name: "DirectoryLevelId",
                table: "MembershipPolicies");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "MembershipDecisions");

            migrationBuilder.DropColumn(
                name: "ReviewerRoles",
                table: "MembershipDecisions");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "MemberDocuments");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "MemberDocuments");
        }
    }
}
