using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class InvoiceSubledgerReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InvoiceId",
                table: "JournalLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalLine_TenantId_EntityId_InvoiceId",
                table: "JournalLine",
                columns: new[] { "TenantId", "EntityId", "InvoiceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_JournalLine_FinanceInvoice_TenantId_EntityId_InvoiceId",
                table: "JournalLine",
                columns: new[] { "TenantId", "EntityId", "InvoiceId" },
                principalTable: "FinanceInvoice",
                principalColumns: new[] { "TenantId", "EntityId", "Id" },
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""

CREATE OR REPLACE FUNCTION finance_journal_check() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE jid uuid; t uuid; e uuid; posted boolean; dt date; period uuid; curr text; dr numeric; cr numeric; n bigint;
BEGIN
 IF TG_TABLE_NAME='JournalEntry' THEN jid=COALESCE(NEW."Id",OLD."Id"); ELSE jid=COALESCE(NEW."JournalId",OLD."JournalId"); END IF;
 SELECT "TenantId","EntityId","Posted","Date","PeriodId","Currency" INTO t,e,posted,dt,period,curr FROM "JournalEntry" WHERE "Id"=jid;
 IF posted THEN
  SELECT COALESCE(sum("Debit"),0),COALESCE(sum("Credit"),0),count(*) INTO dr,cr,n FROM "JournalLine" WHERE "JournalId"=jid AND "TenantId"=t AND "EntityId"=e;
  IF dr<>cr OR dr<=0 OR n<2 THEN RAISE EXCEPTION 'Posted journal must balance with positive totals and at least two lines'; END IF;
  IF TG_TABLE_NAME='JournalEntry' THEN
  IF NOT EXISTS(SELECT 1 FROM "AccountingPeriod" p WHERE p."Id"=period AND p."TenantId"=t AND p."EntityId"=e AND NOT p."Closed" AND p."StartsOn"<=dt AND p."EndsOn">=dt) THEN RAISE EXCEPTION 'Journal posting requires an open matching period'; END IF;
  END IF;
  IF NOT EXISTS(SELECT 1 FROM "FinancialEntity" f WHERE f."Id"=e AND f."TenantId"=t AND f."Currency"=curr) THEN RAISE EXCEPTION 'Journal currency differs from accounting entity'; END IF;
 END IF; RETURN NULL;
END $$;

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
UPDATE "JournalLine" l SET "InvoiceId"=j."SourceId" FROM "JournalEntry" j WHERE l."JournalId"=j."Id" AND l."InvoiceId" IS NULL AND j."SourceKind"='Invoice';
UPDATE "JournalLine" l SET "InvoiceId"=i."OriginalId" FROM "JournalEntry" j JOIN "FinanceInvoice" i ON i."Id"=j."SourceId" WHERE l."JournalId"=j."Id" AND l."InvoiceId" IS NULL AND j."SourceKind"='Credit';
UPDATE "JournalLine" l SET "InvoiceId"=a."InvoiceId" FROM "JournalEntry" j JOIN "PaymentAllocation" a ON a."Id"=j."SourceId" WHERE l."JournalId"=j."Id" AND l."InvoiceId" IS NULL AND j."SourceKind"='Allocation';
UPDATE "JournalLine" l SET "InvoiceId"=a."InvoiceId" FROM "JournalEntry" j JOIN "PaymentAllocation" a ON j."SourceKey" LIKE '%:'||a."Id"::text WHERE l."JournalId"=j."Id" AND l."InvoiceId" IS NULL AND j."SourceKind"='RefundAllocation';
SET CONSTRAINTS ALL IMMEDIATE;
CREATE INDEX finance_invoice_subledger ON "JournalLine" ("TenantId","EntityId","InvoiceId","JournalId");
SET CONSTRAINTS ALL DEFERRED;

""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS finance_invoice_subledger;");
            migrationBuilder.Sql("""
CREATE OR REPLACE FUNCTION finance_posted_guard() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF TG_TABLE_NAME='JournalEntry' THEN
  IF TG_OP<>'INSERT' AND OLD."Posted" THEN RAISE EXCEPTION 'Posted journals are immutable; use a source correction or reversal'; END IF;
 ELSE
  IF TG_OP<>'INSERT' AND EXISTS(SELECT 1 FROM "JournalEntry" WHERE "Id"=OLD."JournalId" AND "Posted") THEN RAISE EXCEPTION 'Posted journal lines are immutable'; END IF;
  IF TG_OP<>'DELETE' AND EXISTS(SELECT 1 FROM "JournalEntry" WHERE "Id"=NEW."JournalId" AND "Posted") THEN RAISE EXCEPTION 'Cannot append to a posted journal'; END IF;
 END IF;
 IF TG_OP='DELETE' THEN RETURN OLD; END IF;RETURN NEW;
END $$;

""");
            migrationBuilder.DropForeignKey(
                name: "FK_JournalLine_FinanceInvoice_TenantId_EntityId_InvoiceId",
                table: "JournalLine");

            migrationBuilder.DropIndex(
                name: "IX_JournalLine_TenantId_EntityId_InvoiceId",
                table: "JournalLine");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "JournalLine");
        }
    }
}
