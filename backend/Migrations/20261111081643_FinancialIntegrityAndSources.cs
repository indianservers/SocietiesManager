using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class FinancialIntegrityAndSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SenderId",
                table: "Outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedBy",
                table: "EventFinancePolicy",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AuthorId",
                table: "EventFinancePolicy",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "AuthorId",
                table: "DuesPlan",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
            migrationBuilder.Sql("""
CREATE OR REPLACE FUNCTION finance_journal_check() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE jid uuid; t uuid; e uuid; posted boolean; dt date; period uuid; curr text; dr numeric; cr numeric; n bigint;
BEGIN
 IF TG_TABLE_NAME='JournalEntry' THEN jid=COALESCE(NEW."Id",OLD."Id"); ELSE jid=COALESCE(NEW."JournalId",OLD."JournalId"); END IF;
 SELECT "TenantId","EntityId","Posted","Date","PeriodId","Currency" INTO t,e,posted,dt,period,curr FROM "JournalEntry" WHERE "Id"=jid;
 IF posted THEN
  SELECT COALESCE(sum("Debit"),0),COALESCE(sum("Credit"),0),count(*) INTO dr,cr,n FROM "JournalLine" WHERE "JournalId"=jid AND "TenantId"=t AND "EntityId"=e;
  IF dr<>cr OR dr<=0 OR n<2 THEN RAISE EXCEPTION 'Posted journal must balance with positive totals and at least two lines'; END IF;
  IF NOT EXISTS(SELECT 1 FROM "AccountingPeriod" p WHERE p."Id"=period AND p."TenantId"=t AND p."EntityId"=e AND NOT p."Closed" AND p."StartsOn"<=dt AND p."EndsOn">=dt) THEN RAISE EXCEPTION 'Journal posting requires an open matching period'; END IF;
  IF NOT EXISTS(SELECT 1 FROM "FinancialEntity" f WHERE f."Id"=e AND f."TenantId"=t AND f."Currency"=curr) THEN RAISE EXCEPTION 'Journal currency differs from accounting entity'; END IF;
 END IF; RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER finance_header_balance AFTER INSERT OR UPDATE ON "JournalEntry" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION finance_journal_check();
CREATE CONSTRAINT TRIGGER finance_line_balance AFTER INSERT OR UPDATE OR DELETE ON "JournalLine" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION finance_journal_check();

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
CREATE TRIGGER finance_posted_header BEFORE UPDATE OR DELETE ON "JournalEntry" FOR EACH ROW EXECUTE FUNCTION finance_posted_guard();
CREATE TRIGGER finance_posted_line BEFORE INSERT OR UPDATE OR DELETE ON "JournalLine" FOR EACH ROW EXECUTE FUNCTION finance_posted_guard();
ALTER TABLE "JournalLine" ADD CONSTRAINT finance_line_sign CHECK (("Debit">0 AND "Credit"=0) OR ("Credit">0 AND "Debit"=0));
ALTER TABLE "FinanceInvoice" ADD CONSTRAINT finance_invoice_amounts CHECK ("Total">=0 AND "Paid">=0 AND "Credited">=0 AND "Paid"+"Credited"<="Total" AND "Subtotal"-"Discount"+"Tax"="Total");
ALTER TABLE "FinancePayment" ADD CONSTRAINT finance_payment_amounts CHECK ("Amount">0 AND "Refunded">=0 AND "ChargedBack">=0 AND "Refunded"+"ChargedBack"<="Amount");
ALTER TABLE "InterEntityObligation" ADD CONSTRAINT finance_obligation_amounts CHECK ("Amount">0 AND "Settled">=0 AND "Settled"<="Amount" AND "EntityId"<>"OtherEntityId");

CREATE OR REPLACE FUNCTION finance_invoice_guard() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF OLD."Status"='Issued' AND ((to_jsonb(OLD)-ARRAY['Paid','Credited','DocumentId','Revision']) IS DISTINCT FROM (to_jsonb(NEW)-ARRAY['Paid','Credited','DocumentId','Revision'])) THEN RAISE EXCEPTION 'Issued financial document content is immutable';END IF;
 IF OLD."DocumentId" IS NOT NULL AND NEW."DocumentId" IS DISTINCT FROM OLD."DocumentId" THEN RAISE EXCEPTION 'Historical financial PDF cannot be replaced';END IF;RETURN NEW;
END $$;
CREATE TRIGGER finance_invoice_history BEFORE UPDATE ON "FinanceInvoice" FOR EACH ROW EXECUTE FUNCTION finance_invoice_guard();
CREATE OR REPLACE FUNCTION finance_receipt_guard() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Financial receipt is immutable';END IF;
 IF (to_jsonb(OLD)-'DocumentId') IS DISTINCT FROM (to_jsonb(NEW)-'DocumentId') OR OLD."DocumentId" IS NOT NULL AND OLD."DocumentId" IS DISTINCT FROM NEW."DocumentId" THEN RAISE EXCEPTION 'Financial receipt content is immutable'; END IF;RETURN NEW;
END $$;
CREATE TRIGGER finance_receipt_history BEFORE UPDATE OR DELETE ON "FinanceReceipt" FOR EACH ROW EXECUTE FUNCTION finance_receipt_guard();
CREATE OR REPLACE FUNCTION finance_frozen_guard() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Financial snapshot or provider evidence is immutable'; END $$;
CREATE TRIGGER finance_assessment_frozen BEFORE UPDATE OR DELETE ON "DuesAssessment" FOR EACH ROW EXECUTE FUNCTION finance_frozen_guard();
CREATE TRIGGER finance_provider_frozen BEFORE UPDATE OR DELETE ON "FinanceProviderEvent" FOR EACH ROW EXECUTE FUNCTION finance_frozen_guard();
CREATE TRIGGER finance_budget_version_frozen BEFORE UPDATE OR DELETE ON "BudgetRevision" FOR EACH ROW EXECUTE FUNCTION finance_frozen_guard();
CREATE TRIGGER finance_rate_frozen BEFORE UPDATE OR DELETE ON "FinanceExchangeRate" FOR EACH ROW EXECUTE FUNCTION finance_frozen_guard();

CREATE OR REPLACE FUNCTION finance_period_guard() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 PERFORM pg_advisory_xact_lock(hashtext(NEW."TenantId"::text));
 IF TG_OP='UPDATE' AND OLD."Closed" THEN RAISE EXCEPTION 'Closed periods cannot be silently reopened or changed'; END IF;
 IF NEW."EndsOn"<NEW."StartsOn" OR EXISTS(SELECT 1 FROM "AccountingPeriod" p WHERE p."TenantId"=NEW."TenantId" AND p."EntityId"=NEW."EntityId" AND p."Id"<>NEW."Id" AND p."StartsOn"<=NEW."EndsOn" AND p."EndsOn">=NEW."StartsOn") THEN RAISE EXCEPTION 'Accounting periods overlap or have invalid dates'; END IF;
 RETURN NEW;
END $$;
CREATE TRIGGER finance_period_dates BEFORE INSERT OR UPDATE ON "AccountingPeriod" FOR EACH ROW EXECUTE FUNCTION finance_period_guard();
CREATE OR REPLACE FUNCTION finance_entity_guard() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW."Currency" IS DISTINCT FROM OLD."Currency" THEN RAISE EXCEPTION 'Accounting entity base currency is immutable';END IF;RETURN NEW; END $$;
CREATE TRIGGER finance_entity_currency BEFORE UPDATE ON "FinancialEntity" FOR EACH ROW EXECUTE FUNCTION finance_entity_guard();

CREATE OR REPLACE FUNCTION finance_currency_guard() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE curr text; base text; digits int; value jsonb; k text;
BEGIN
 SELECT "Currency" INTO base FROM "FinancialEntity" WHERE "TenantId"=NEW."TenantId" AND "Id"=NEW."EntityId";
 curr=to_jsonb(NEW)->>'Currency';
 IF curr IS NOT NULL AND curr<>base THEN RAISE EXCEPTION 'Operational financial documents must use entity base currency';END IF;
 digits=CASE base WHEN 'JPY' THEN 0 WHEN 'KWD' THEN 3 WHEN 'BHD' THEN 3 WHEN 'OMR' THEN 3 ELSE 2 END;
 FOREACH k IN ARRAY ARRAY['Amount','Total','Subtotal','Tax','Discount','Paid','Credited','Refunded','ChargedBack','Debit','Credit','Pledged','Settled','BaseAmount','PerMemberAmount','PerMember','LateFee','MemberPrice','GuestPrice'] LOOP
  value=to_jsonb(NEW)->k;
  IF value IS NOT NULL AND value<>'null'::jsonb AND (value#>>'{}')::numeric<>round((value#>>'{}')::numeric,digits) THEN RAISE EXCEPTION 'Money violates currency minor-unit precision'; END IF;
 END LOOP;RETURN NEW;
END $$;
DO $$ DECLARE tab text; BEGIN FOREACH tab IN ARRAY ARRAY['JournalLine','DuesPlan','DuesAssessment','FinanceInvoice','FinancePayment','PaymentAllocation','FinanceReceipt','FinanceRefund','PaymentIntent','FinanceBudget','FinanceExpense','FinanceContribution','InterEntityObligation','InterEntitySettlement','BankTransaction','ReconciliationMatch','EventFinancePolicy'] LOOP EXECUTE format('CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON %I FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()',tab);END LOOP;END $$;

CREATE UNIQUE INDEX finance_refund_reference ON "FinanceRefund" ("TenantId","EntityId","Reference");
CREATE UNIQUE INDEX finance_single_reversal ON "JournalEntry" ("TenantId","EntityId","ReversesId") WHERE "ReversesId" IS NOT NULL;
CREATE INDEX finance_statement_member ON "JournalLine" ("TenantId","MembershipId","JournalId");
CREATE INDEX finance_statement_chapter ON "JournalLine" ("TenantId","CounterpartyUnitId","JournalId");
CREATE INDEX finance_bank_unmatched ON "BankTransaction" ("TenantId","EntityId","Status","Date");
CREATE INDEX finance_reminder_due ON "FinanceInvoice" ("TenantId","EntityId","UnitId","DueOn") WHERE "Status"='Issued' AND "Kind"='Invoice';

UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['finance.read','finance.configure','finance.dues','finance.invoice','finance.collect','finance.approve','finance.post','finance.periods','finance.bank','finance.reconcile','finance.refund','finance.budget','finance.expense','finance.export','finance.highvalue','finance.providers','finance.interentity']))) WHERE "Name"='Society Administrator';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['finance.read','finance.dues','finance.invoice','finance.collect','finance.approve','finance.post','finance.bank','finance.reconcile','finance.refund','finance.budget','finance.expense','finance.export','finance.interentity','communications.send','communications.templates']))) WHERE "Name" LIKE '%Treasurer%';
UPDATE "GovernanceRoles" SET "Permissions"=(SELECT ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['finance.read']))) WHERE "Name" LIKE '%President%' OR "Name" LIKE '%Governor%' OR "Name" LIKE '%Secretary%' OR "Name" LIKE '%Manager%';

INSERT INTO "CommunicationTemplate" ("Id","TenantId","Name","Channel","Category","Language","Subject","Body","ProviderStatus","Active","Revision","ProviderBody") SELECT gen_random_uuid(),t."Id",n,'InApp','Finance','en',n,'Hello {{member_name}}, invoice {{invoice_number}}, outstanding {{outstanding_amount}}, due {{due_date}}. {{finance_link}}','Not submitted',true,0,'' FROM "Tenants" t CROSS JOIN unnest(ARRAY['Member dues issued','Chapter dues issued','Payment reminder','Overdue notice','Payment received','Receipt available','Refund processed','Expense approval request']) n WHERE NOT EXISTS(SELECT 1 FROM "CommunicationTemplate" x WHERE x."TenantId"=t."Id" AND x."Name"=n AND x."Category"='Finance');
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DROP FUNCTION IF EXISTS finance_journal_check() CASCADE;
DROP FUNCTION IF EXISTS finance_posted_guard() CASCADE;
DROP FUNCTION IF EXISTS finance_invoice_guard() CASCADE;
DROP FUNCTION IF EXISTS finance_receipt_guard() CASCADE;
DROP FUNCTION IF EXISTS finance_frozen_guard() CASCADE;
DROP FUNCTION IF EXISTS finance_period_guard() CASCADE;
DROP FUNCTION IF EXISTS finance_entity_guard() CASCADE;
DROP FUNCTION IF EXISTS finance_currency_guard() CASCADE;
ALTER TABLE "JournalLine" DROP CONSTRAINT IF EXISTS finance_line_sign;
ALTER TABLE "FinanceInvoice" DROP CONSTRAINT IF EXISTS finance_invoice_amounts;
ALTER TABLE "FinancePayment" DROP CONSTRAINT IF EXISTS finance_payment_amounts;
ALTER TABLE "InterEntityObligation" DROP CONSTRAINT IF EXISTS finance_obligation_amounts;
DROP INDEX IF EXISTS finance_refund_reference;
DROP INDEX IF EXISTS finance_single_reversal;
DROP INDEX IF EXISTS finance_statement_member;
DROP INDEX IF EXISTS finance_statement_chapter;
DROP INDEX IF EXISTS finance_bank_unmatched;
DROP INDEX IF EXISTS finance_reminder_due;
""");

            migrationBuilder.DropColumn(
                name: "SenderId",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "EventFinancePolicy");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "EventFinancePolicy");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "DuesPlan");
        }
    }
}
