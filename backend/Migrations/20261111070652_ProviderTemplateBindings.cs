using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class ProviderTemplateBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TemplateParameters",
                table: "Outbox",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderAccountId",
                table: "CommunicationTemplate",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderBody",
                table: "CommunicationTemplate",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTemplate_TenantId_ProviderAccountId",
                table: "CommunicationTemplate",
                columns: new[] { "TenantId", "ProviderAccountId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationTemplate_CommunicationAccount_TenantId_Provide~",
                table: "CommunicationTemplate",
                columns: new[] { "TenantId", "ProviderAccountId" },
                principalTable: "CommunicationAccount",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationTemplate_CommunicationAccount_TenantId_Provide~",
                table: "CommunicationTemplate");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationTemplate_TenantId_ProviderAccountId",
                table: "CommunicationTemplate");

            migrationBuilder.DropColumn(
                name: "TemplateParameters",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "ProviderAccountId",
                table: "CommunicationTemplate");

            migrationBuilder.DropColumn(
                name: "ProviderBody",
                table: "CommunicationTemplate");
        }
    }
}
