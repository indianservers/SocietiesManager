using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class FinancialDeliveryReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FinanceInvoiceId",
                table: "Outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_TenantId_FinanceInvoiceId",
                table: "Outbox",
                columns: new[] { "TenantId", "FinanceInvoiceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Outbox_FinanceInvoice_TenantId_FinanceInvoiceId",
                table: "Outbox",
                columns: new[] { "TenantId", "FinanceInvoiceId" },
                principalTable: "FinanceInvoice",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Outbox_FinanceInvoice_TenantId_FinanceInvoiceId",
                table: "Outbox");

            migrationBuilder.DropIndex(
                name: "IX_Outbox_TenantId_FinanceInvoiceId",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "FinanceInvoiceId",
                table: "Outbox");
        }
    }
}
