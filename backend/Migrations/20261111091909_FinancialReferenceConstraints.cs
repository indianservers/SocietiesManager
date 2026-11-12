using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class FinancialReferenceConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_InterEntityObligation_TenantId_OtherEntityId_OtherJournalId",
                table: "InterEntityObligation",
                columns: new[] { "TenantId", "OtherEntityId", "OtherJournalId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRefund_TenantId_EntityId_JournalId",
                table: "FinanceRefund",
                columns: new[] { "TenantId", "EntityId", "JournalId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReceipt_TenantId_EntityId_DocumentId",
                table: "FinanceReceipt",
                columns: new[] { "TenantId", "EntityId", "DocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayment_TenantId_EntityId_BankId",
                table: "FinancePayment",
                columns: new[] { "TenantId", "EntityId", "BankId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayment_TenantId_EntityId_JournalId",
                table: "FinancePayment",
                columns: new[] { "TenantId", "EntityId", "JournalId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayment_TenantId_EntityId_ProviderId",
                table: "FinancePayment",
                columns: new[] { "TenantId", "EntityId", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvoice_TenantId_EntityId_DocumentId",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "EntityId", "DocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvoice_TenantId_EntityId_DonorId",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "EntityId", "DonorId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvoice_TenantId_EventId",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExpense_TenantId_EntityId_BankId",
                table: "FinanceExpense",
                columns: new[] { "TenantId", "EntityId", "BankId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExpense_TenantId_EntityId_JournalId",
                table: "FinanceExpense",
                columns: new[] { "TenantId", "EntityId", "JournalId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExpense_TenantId_EntityId_PaymentJournalId",
                table: "FinanceExpense",
                columns: new[] { "TenantId", "EntityId", "PaymentJournalId" });

            migrationBuilder.CreateIndex(
                name: "IX_DuesPlan_TenantId_CategoryId",
                table: "DuesPlan",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.AddForeignKey(
                name: "FK_DuesPlan_MembershipCategories_TenantId_CategoryId",
                table: "DuesPlan",
                columns: new[] { "TenantId", "CategoryId" },
                principalTable: "MembershipCategories",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EventFinancePolicy_CommunityEvent_TenantId_EventId",
                table: "EventFinancePolicy",
                columns: new[] { "TenantId", "EventId" },
                principalTable: "CommunityEvent",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceExpense_FinanceBank_TenantId_EntityId_BankId",
                table: "FinanceExpense",
                columns: new[] { "TenantId", "EntityId", "BankId" },
                principalTable: "FinanceBank",
                principalColumns: new[] { "TenantId", "EntityId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceExpense_JournalEntry_TenantId_EntityId_JournalId",
                table: "FinanceExpense",
                columns: new[] { "TenantId", "EntityId", "JournalId" },
                principalTable: "JournalEntry",
                principalColumns: new[] { "TenantId", "EntityId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceExpense_JournalEntry_TenantId_EntityId_PaymentJourna~",
                table: "FinanceExpense",
                columns: new[] { "TenantId", "EntityId", "PaymentJournalId" },
                principalTable: "JournalEntry",
                principalColumns: new[] { "TenantId", "EntityId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceInvoice_CommunityEvent_TenantId_EventId",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "EventId" },
                principalTable: "CommunityEvent",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceInvoice_EventRegistration_TenantId_RegistrationId",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "RegistrationId" },
                principalTable: "EventRegistration",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceInvoice_FinanceDonor_TenantId_EntityId_DonorId",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "EntityId", "DonorId" },
                principalTable: "FinanceDonor",
                principalColumns: new[] { "TenantId", "EntityId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceInvoice_FinanceFile_TenantId_EntityId_DocumentId",
                table: "FinanceInvoice",
                columns: new[] { "TenantId", "EntityId", "DocumentId" },
                principalTable: "FinanceFile",
                principalColumns: new[] { "TenantId", "EntityId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancePayment_FinanceBank_TenantId_EntityId_BankId",
                table: "FinancePayment",
                columns: new[] { "TenantId", "EntityId", "BankId" },
                principalTable: "FinanceBank",
                principalColumns: new[] { "TenantId", "EntityId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancePayment_JournalEntry_TenantId_EntityId_JournalId",
                table: "FinancePayment",
                columns: new[] { "TenantId", "EntityId", "JournalId" },
                principalTable: "JournalEntry",
                principalColumns: new[] { "TenantId", "EntityId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancePayment_PaymentConnection_TenantId_EntityId_Provider~",
                table: "FinancePayment",
                columns: new[] { "TenantId", "EntityId", "ProviderId" },
                principalTable: "PaymentConnection",
                principalColumns: new[] { "TenantId", "EntityId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceReceipt_FinanceFile_TenantId_EntityId_DocumentId",
                table: "FinanceReceipt",
                columns: new[] { "TenantId", "EntityId", "DocumentId" },
                principalTable: "FinanceFile",
                principalColumns: new[] { "TenantId", "EntityId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceRefund_JournalEntry_TenantId_EntityId_JournalId",
                table: "FinanceRefund",
                columns: new[] { "TenantId", "EntityId", "JournalId" },
                principalTable: "JournalEntry",
                principalColumns: new[] { "TenantId", "EntityId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InterEntityObligation_FinancialEntity_TenantId_OtherEntityId",
                table: "InterEntityObligation",
                columns: new[] { "TenantId", "OtherEntityId" },
                principalTable: "FinancialEntity",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InterEntityObligation_JournalEntry_TenantId_OtherEntityId_O~",
                table: "InterEntityObligation",
                columns: new[] { "TenantId", "OtherEntityId", "OtherJournalId" },
                principalTable: "JournalEntry",
                principalColumns: new[] { "TenantId", "EntityId", "Id" },
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
CREATE OR REPLACE FUNCTION finance_posted_guard() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF TG_TABLE_NAME='JournalEntry' THEN
  IF TG_OP<>'INSERT' AND OLD."Posted" THEN RAISE EXCEPTION 'Posted journals are immutable; use a source correction or reversal'; END IF;
 ELSE
  IF TG_OP='UPDATE' THEN
  IF OLD."InvoiceId" IS NULL AND NEW."InvoiceId" IS NOT NULL AND (to_jsonb(OLD)-'InvoiceId')=(to_jsonb(NEW)-'InvoiceId') AND EXISTS(
   SELECT 1 FROM "JournalEntry" j WHERE j."Id"=OLD."JournalId" AND (
    j."SourceKind"='Invoice' AND j."SourceId"=NEW."InvoiceId" OR
    j."SourceKind"='Credit' AND EXISTS(SELECT 1 FROM "FinanceInvoice" i WHERE i."Id"=j."SourceId" AND i."OriginalId"=NEW."InvoiceId") OR
    j."SourceKind"='Allocation' AND EXISTS(SELECT 1 FROM "PaymentAllocation" a WHERE a."Id"=j."SourceId" AND a."InvoiceId"=NEW."InvoiceId") OR
    j."SourceKind"='RefundAllocation' AND EXISTS(SELECT 1 FROM "PaymentAllocation" a WHERE a."InvoiceId"=NEW."InvoiceId" AND j."SourceKey" LIKE '%:'||a."Id"::text)
   )) THEN RETURN NEW; END IF;
  END IF;
  IF TG_OP<>'INSERT' AND EXISTS(SELECT 1 FROM "JournalEntry" WHERE "Id"=OLD."JournalId" AND "Posted") THEN RAISE EXCEPTION 'Posted journal lines are immutable'; END IF;
  IF TG_OP<>'DELETE' AND EXISTS(SELECT 1 FROM "JournalEntry" WHERE "Id"=NEW."JournalId" AND "Posted") THEN RAISE EXCEPTION 'Cannot append to a posted journal'; END IF;
 END IF;
 IF TG_OP='DELETE' THEN RETURN OLD; END IF;RETURN NEW;
END $$;
CREATE OR REPLACE FUNCTION finance_currency_guard() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE curr text; base text; digits int; value jsonb; k text; doc jsonb;
BEGIN
 SELECT "Currency" INTO base FROM "FinancialEntity" WHERE "TenantId"=NEW."TenantId" AND "Id"=NEW."EntityId";
 doc=to_jsonb(NEW);curr=doc->>'Currency';
 IF curr IS NOT NULL AND curr<>base THEN RAISE EXCEPTION 'Operational financial documents must use entity base currency';END IF;
 digits=CASE base WHEN 'JPY' THEN 0 WHEN 'KWD' THEN 3 WHEN 'BHD' THEN 3 WHEN 'OMR' THEN 3 ELSE 2 END;
 FOREACH k IN ARRAY ARRAY['Amount','Total','Subtotal','Tax','Discount','Paid','Credited','Refunded','ChargedBack','Debit','Credit','Pledged','Settled','BaseAmount','PerMemberAmount','PerMember','LateFee','MemberPrice','GuestPrice'] LOOP
  value=doc->k;
  IF value IS NOT NULL AND value<>'null'::jsonb AND (value#>>'{}')::numeric<>round((value#>>'{}')::numeric,digits) THEN RAISE EXCEPTION 'Money violates currency minor-unit precision'; END IF;
 END LOOP;RETURN NEW;
END $$;

""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DuesPlan_MembershipCategories_TenantId_CategoryId",
                table: "DuesPlan");

            migrationBuilder.DropForeignKey(
                name: "FK_EventFinancePolicy_CommunityEvent_TenantId_EventId",
                table: "EventFinancePolicy");

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceExpense_FinanceBank_TenantId_EntityId_BankId",
                table: "FinanceExpense");

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceExpense_JournalEntry_TenantId_EntityId_JournalId",
                table: "FinanceExpense");

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceExpense_JournalEntry_TenantId_EntityId_PaymentJourna~",
                table: "FinanceExpense");

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceInvoice_CommunityEvent_TenantId_EventId",
                table: "FinanceInvoice");

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceInvoice_EventRegistration_TenantId_RegistrationId",
                table: "FinanceInvoice");

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceInvoice_FinanceDonor_TenantId_EntityId_DonorId",
                table: "FinanceInvoice");

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceInvoice_FinanceFile_TenantId_EntityId_DocumentId",
                table: "FinanceInvoice");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancePayment_FinanceBank_TenantId_EntityId_BankId",
                table: "FinancePayment");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancePayment_JournalEntry_TenantId_EntityId_JournalId",
                table: "FinancePayment");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancePayment_PaymentConnection_TenantId_EntityId_Provider~",
                table: "FinancePayment");

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceReceipt_FinanceFile_TenantId_EntityId_DocumentId",
                table: "FinanceReceipt");

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceRefund_JournalEntry_TenantId_EntityId_JournalId",
                table: "FinanceRefund");

            migrationBuilder.DropForeignKey(
                name: "FK_InterEntityObligation_FinancialEntity_TenantId_OtherEntityId",
                table: "InterEntityObligation");

            migrationBuilder.DropForeignKey(
                name: "FK_InterEntityObligation_JournalEntry_TenantId_OtherEntityId_O~",
                table: "InterEntityObligation");

            migrationBuilder.DropIndex(
                name: "IX_InterEntityObligation_TenantId_OtherEntityId_OtherJournalId",
                table: "InterEntityObligation");

            migrationBuilder.DropIndex(
                name: "IX_FinanceRefund_TenantId_EntityId_JournalId",
                table: "FinanceRefund");

            migrationBuilder.DropIndex(
                name: "IX_FinanceReceipt_TenantId_EntityId_DocumentId",
                table: "FinanceReceipt");

            migrationBuilder.DropIndex(
                name: "IX_FinancePayment_TenantId_EntityId_BankId",
                table: "FinancePayment");

            migrationBuilder.DropIndex(
                name: "IX_FinancePayment_TenantId_EntityId_JournalId",
                table: "FinancePayment");

            migrationBuilder.DropIndex(
                name: "IX_FinancePayment_TenantId_EntityId_ProviderId",
                table: "FinancePayment");

            migrationBuilder.DropIndex(
                name: "IX_FinanceInvoice_TenantId_EntityId_DocumentId",
                table: "FinanceInvoice");

            migrationBuilder.DropIndex(
                name: "IX_FinanceInvoice_TenantId_EntityId_DonorId",
                table: "FinanceInvoice");

            migrationBuilder.DropIndex(
                name: "IX_FinanceInvoice_TenantId_EventId",
                table: "FinanceInvoice");

            migrationBuilder.DropIndex(
                name: "IX_FinanceExpense_TenantId_EntityId_BankId",
                table: "FinanceExpense");

            migrationBuilder.DropIndex(
                name: "IX_FinanceExpense_TenantId_EntityId_JournalId",
                table: "FinanceExpense");

            migrationBuilder.DropIndex(
                name: "IX_FinanceExpense_TenantId_EntityId_PaymentJournalId",
                table: "FinanceExpense");

            migrationBuilder.DropIndex(
                name: "IX_DuesPlan_TenantId_CategoryId",
                table: "DuesPlan");
        }
    }
}
