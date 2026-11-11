using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class MembershipEvidenceDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid[]>(
                name: "DocumentIds",
                table: "OfficerAppointments",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.AddColumn<Guid[]>(
                name: "DocumentIds",
                table: "MembershipDecisions",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.AlterColumn<Guid>(
                name: "MembershipId",
                table: "MemberDocuments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationId",
                table: "MemberDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberDocuments_TenantId_ApplicationId",
                table: "MemberDocuments",
                columns: new[] { "TenantId", "ApplicationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_MemberDocuments_MemberApplications_TenantId_ApplicationId",
                table: "MemberDocuments",
                columns: new[] { "TenantId", "ApplicationId" },
                principalTable: "MemberApplications",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("ALTER TABLE \"MemberDocuments\" ADD CONSTRAINT member_document_owner CHECK ((\"MembershipId\" IS NOT NULL)::int+(\"ApplicationId\" IS NOT NULL)::int=1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberDocuments_MemberApplications_TenantId_ApplicationId",
                table: "MemberDocuments");

            migrationBuilder.DropIndex(
                name: "IX_MemberDocuments_TenantId_ApplicationId",
                table: "MemberDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentIds",
                table: "OfficerAppointments");

            migrationBuilder.DropColumn(
                name: "DocumentIds",
                table: "MembershipDecisions");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "MemberDocuments");

            migrationBuilder.AlterColumn<Guid>(
                name: "MembershipId",
                table: "MemberDocuments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
