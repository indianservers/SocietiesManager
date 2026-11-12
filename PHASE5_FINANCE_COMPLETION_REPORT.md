# Phase 5 Finance Completion Report

Verified locally on 11 November 2026. Phase 5 extends the existing .NET 10 / PostgreSQL / React modular monolith. It preserves the canonical person, membership, organization, dated governance permission, event-registration and Communication Outbox engines. No production deployment, live provider activation, real payment or external financial notice was performed.

## Implemented functionality

- Explicit legal accounting entities, permanent base currency, explicit organization mappings, fiscal periods, chart of accounts, independent invoice review and high-value approval limits. A chapter does not automatically become a legal entity. Setup creates zero opening balances.
- Configurable joining, life, annual, monthly, quarterly, half-yearly, special and institutional dues. Member and chapter obligations remain distinct. Category eligibility, effective dates, daily proration, percentage discounts, independently approved waivers, grace periods and late-fee terms are captured in assessment snapshots. Historical invoices never recalculate after a plan edit. Late fees are separate authorized invoices.
- Selected assessments and durable cursor-based bulk billing, stable request keys, duplicate and overlapping-period protection, current-authority checks and automatic effective-dated billing through the Finance worker. Progress and failures are persisted.
- Decimal invoice quantity/rate/discount/tax calculations; drafts, independent review, issue, cancellation before issue, credits and historical PDF documents. Invoice, credit, event, donation and sponsorship billing share the same engine.
- Manual cash, bank and cheque collections with independent confirmation/clearance; verified gateway collections; partial and full allocations; overpayments retained as advances; actual numbered receipts only after confirmation. Refunds release allocations and reopen the exact receivable. Chargebacks have separate source journals and tracking.
- Encrypted/write-only provider credentials and bank details, masked bank lists, durable statement imports with row deduplication, partial positive bank-credit matching and explicit gateway-clearing settlement journals.
- Balanced double-entry postings, immutable posted records, source-linked invoice subledgers, independent manual journal review, linked manual reversals, periods that cannot silently reopen, general-ledger cursors, accrual expense/AP postings and separate cash payments.
- Budgets with category caps, independent approval, preserved revisions and commitments; private expense evidence and review history; actual payment references; donations/sponsorship pledges separated from collections and associated with canonical donors/people.
- Independently approved inter-entity fund obligations and separate actual settlements, with due-to/due-from balances in each entity. No second income entry is invented by allocation or settlement. Explicit dated source exchange rates support an operational current-receivable translation; base-currency records stay separate.
- Existing paid event registrations enter Awaiting Payment; an approved price policy creates a central invoice. Only verified allocated funds confirm the original registration, subject to capacity. Refunds revoke passes and reopen payment requirements. Free-event behavior remains available.
- Eight finance templates, private transaction/review notifications, manual sends, deduplicated due-date reminders and dispatch logs reuse the existing consent-aware Outbox. Current sender authority, recipient eligibility, channel preferences and quiet hours remain enforced. Official external channels still require configured, verified providers/templates.
- Own-member invoices, receipts, statements and supported checkout; scoped treasurer, chapter, region and society views. Presidents receive summary/read authority without inheriting ledger or banking powers.
- Twenty report families: Member Dues, Chapter Dues, Outstanding, Ageing, Collections, Invoices, Receipts, Reconciliation, Bank Cash Book, General Ledger, Trial Balance, Income Expenditure, Balance Sheet, Budget Actual, Event Finance, Donations, Inter Entity, Member Statement, Chapter Statement and Audit. CSV, real XLSX and PDF exports run as durable jobs; downloads recheck author and current financial scope.

## Database changes

Six additive Phase 5 migrations bring the repository to 21 migrations. They were applied both to retained local data and a freshly created disposable benchmark database:

1. `20261111075755_UnifiedFinancialEngine`
2. `20261111081643_FinancialIntegrityAndSources`
3. `20261111084000_FinancialPaymentRecovery`
4. `20261111085247_FinancialDeliveryReferences`
5. `20261111085518_InvoiceSubledgerReferences`
6. `20261111091909_FinancialReferenceConstraints`

There are 33 finance entity types, including legal entities/mappings, periods/accounts/journals, dues plans/exemptions/assessments, invoices/payments/allocations/receipts/refunds, provider connections/intents/events, banks/statement rows/matches, budgets/revisions/expenses/private files, donors/contributions, inter-entity obligations/settlements, rates/jobs/reminders and event pricing. Money uses PostgreSQL NUMERIC(20,4) and .NET decimal; rates use NUMERIC(24,10). Currency-specific minor units and rounding are validated. Composite tenant/entity relationships and source keys prevent cross-entity references and duplicates.

PostgreSQL triggers enforce positive/balanced posted journals, open-period posting, immutable posted money, issued document content, receipt snapshots and accounting history. A narrowly source-validated null-to-invoice subledger metadata backfill preserves old monetary values during upgrade. Trigger update-only validation was moved into a nested update branch after large-data testing revealed unnecessary insert work. Query statistics are refreshed during benchmark fixture preparation.

`dotnet ef migrations has-pending-model-changes --project backend --no-build` reports no pending model changes. The local development database reports no outstanding migrations.

## UI screens

`/app/finance` contains the treasury overview, member dues, chapter obligations, regional intelligence, society finance, dues master, assessment batches, invoices/credits, payments, receipts, reconciliation, accounts, journal requests/reviews, ledger, budgets, expenses/evidence, donations/sponsors, event finance, inter-entity funds, reminders, reports/statements, audit, own-member portal and settings.

Browser verification covered first-entity setup, explicit organization selection, honest zero treasury, empty dues register, plan editor and report/export controls. Desktop and 390×844 mobile layouts were inspected; mobile document width equalled its client width (375 CSS pixels after the scrollbar), with no page-level horizontal overflow. The horizontal finance navigation remains scrollable. No browser console errors were observed. A fictional legal-entity metadata record was created only in the existing marked demo society; no financial transactions were seeded into that preview.

Evidence: `tests/phase5-workspace-preview.jpg` and `tests/phase5-mobile-preview.jpg`. Local preview: http://127.0.0.1:5174/app/finance (API port 5092).

## Accounting verification

- Issue: Dr receivables / Cr income and output tax. Credits reverse the appropriate original income/tax and reduce the original debt.
- Confirm collection: Dr cash/bank/gateway clearing / Cr unapplied advances. Allocation: Dr advances / Cr receivables. An unallocated receipt does not falsely settle an invoice.
- Refund/chargeback: release applicable allocations, reopen AR and reverse the actual available collection through source-linked journals.
- Expense approval: Dr expense / Cr AP. Actual payment: Dr AP / Cr cash/bank. Budget approval alone never creates spending or cash.
- Inter-entity approved fund allocation uses fund and due-to/due-from accounts; a separate actual settlement clears those obligations against each entity's bank account.
- Trial balances reconcile independently by legal entity/currency. Statements use posted AR opening + debits − credits = closing. Historical ageing uses invoice-linked postings as of the requested date, excluding later allocations. Balance sheets reconcile assets = liabilities + funds + current surplus. Operational organization segments are labelled distinctly from legal-entity financial statements.

Integration assertions verify these flows, direct PostgreSQL unbalanced-post rejection, line immutability and closed-period rejection. PDFs were downloaded from actual authorized synthetic test transactions, rendered and visually checked: one invoice page and one receipt page, with no clipping. PDFsharp 6.2.4 and embedded licensed Vera fonts produce server-side documents. XLSX was downloaded, ZIP/XML checked and verified formula-free; CSV includes opening/closing summaries and neutralizes formula-leading text. Artifacts are test fixtures, never actual society receipts.

## Payment integration status

Razorpay has a concrete server adapter for authenticated order creation and refund dispatch plus signed raw-body webhooks. Checkout browser redirects/callbacks never confirm money. The server verifies the HMAC, event deduplication, connection/order identity, exact minor-unit amount, currency and captured state. Duplicate captures and delayed authorization events cannot post twice or downgrade confirmed funds. Signed lost-dispute events post chargebacks once.

The Development-only Test adapter was used for automated verification. No Razorpay test/live credentials were supplied, so no external sandbox checkout/refund/webhook/settlement cycle was verified. The configuration verification operation checks API authentication, not merchant legal identity or end-to-end settlement. Unknown external request outcomes remain Unknown for reconciliation rather than being blindly retried or marked successful. Live readiness requires the provider's controlled sandbox cycle and operational setup described below.

## Automated verification

478 checks passed: 67 foundation + 95 membership + 112 activities/events + 98 communications + 106 finance. Run `python tests/finance_integration.py`; it imports the prior four suites and uses real PostgreSQL/HTTP with mock payment/communication adapters. Detailed cases and durations are in `tests/phase5-results.json`; prior-phase result files remain separate. Final run exited successfully.

Finance checks include calculations, snapshots, waiver/proration/once-only rules, chapter/member separation, partial/full/advance allocations, credits/refunds/chargebacks, signature/amount/replay protection, historical statements/ageing, independent journal/budget/expense approvals, actual event payment/pass transitions, bank-import duplicates/partial settlements, protected exports, Treasurer/President boundaries, cross-tenant rejection, real PDF/XLSX/CSV output, due-day Outbox dispatch, automatic billing and period locking.

Eighteen overlapping signed mock capture callbacks on six workers all succeeded in 172.07 ms total; PostgreSQL contained exactly one payment and one receipt for that provider reference. See `tests/phase5-payment-concurrency.json`. This is duplicate-event correctness evidence, not a live gateway or distributed-system throughput guarantee.

Backend build completed with zero warnings/errors; frontend TypeScript/Vite production build passed. NuGet vulnerable/transitive package check and production npm audit reported no known vulnerabilities on the verification date. CI now runs the combined Phase 1–5 suite and the finance benchmark. These checks do not replace an independent security review.

## Performance measurements

The freshly migrated disposable PostgreSQL benchmark asserts actual counts: 100,000 memberships/people, 10,000 chapters, 100,000 issued source-linked invoices, 100,000 balanced posted journals and 200,000 immutable journal lines. Integrity triggers stay enabled. Membership fixture load took 8,247.23 ms; accounting fixture load/commit took 57,473.42 ms. Synthetic SQL loading is explicitly separate from real API billing throughput.

Warm authenticated local API measurements (12 samples each; nearest-rank p95 is the maximum for this sample size), all below the local 2,000 ms target:

- invoice register: median 239.243 ms; p95 276.018 ms.
- chapter filter: median 57.102 ms; p95 66.396 ms.
- dashboard: median 955.755 ms; p95 1183.795 ms.
- ledger cursor: median 461.536 ms; p95 510.430 ms.
- trial balance: median 341.578 ms; p95 394.777 ms.
- ageing: median 440.903 ms; p95 745.035 ms.

A real 1,000-member annual assessment job on that database completed in 21,165.24 ms, or 47.25 pending-review invoices/second. This does not measure issuing/posting, PDF generation or payment throughput for all 100,000 recipients. Six overlapping readers completed 24 requests with zero failures and p95 535.038 ms. EXPLAIN ANALYZE BUFFERS includes the invoice-due and member-statement indexes. Raw evidence and reproducible script: `tests/finance-api-performance.json` and `tests/finance_api_benchmark.py`.

These are local machine measurements with small warm samples, not production p95/SLA claims. No distributed multi-worker failover or full 100,000-recipient annual write campaign was run. An audit of the reused Phase 4 fixture found that its chapter generator created 10 chapters despite a descriptive 10,000-chapter label. The Phase 5 generator corrects this and asserts 10,000 actual rows; historical Phase 4 timings must not be interpreted as 10,000-chapter evidence.

## Security validation

Tenant membership is checked on the finance route group. Every source workflow checks entity and current dated unit authority; bank/provider/legal-owner operations require their distinct permissions. Independent reviewers cannot approve their own money requests; high-value decisions require the elevated permission. PostgreSQL same-entity foreign keys, advisory serialization and source/event/reference uniqueness supplement application policy. Members read their own canonical membership/event financial documents. Presidents cannot acquire finance posting permissions through community approval templates.

Credentials are Data Protection encrypted and write-only. Bank metadata is masked and permission filtered. Files, reports and historical documents recheck current authorization; generic file paths cannot bypass report ownership or expose expense evidence to a read-only President. Responses use no-store. Mutations use CSRF, except the dedicated cryptographically verified webhook routes. CSV/XLSX avoid executable formulas. External HTTP endpoints are fixed and redirects disabled. Live card data is handled by official checkout, not stored in the ERP.

## Known limits and Phase 6 dependencies

- External sandbox/provider operation remains unverified without supplied credentials. Before live use, verify merchant/legal ownership, signed receiving URLs, capture/refund/chargeback/settlement cycles, account-specific fees and error recovery. Externally initiated refunds and dispute restoration need operator reconciliation; the system does not infer successful money movement.
- Bank matching currently covers positive collection credits and gateway clearing. Negative expense/refund debits and net-of-fee settlement matching are retained as unmatched rows; an expense-debit/fee reconciliation extension is still required for a complete bank reconciliation service.
- Restricted donation purpose is recorded, but separate restricted-fund release/accounting and jurisdiction-specific donation/tax certificates are not implemented. No tax-deductibility or statutory compliance claim is made.
- Exchange translation covers current receivables with an explicitly recorded source rate. Foreign-currency transaction posting, realized/unrealized FX and statutory cross-entity consolidation are outside this implementation.
- Due-date reminders run on the exact configured offset; missed past dates need an authorized manual send. The periodic sweep is bounded to 100 enabled plans/rules and 100 invoices per rule; larger installations need paged scheduling/fairness and catch-up controls. Billing jobs process 100 records per transaction. Automatic billing requires a currently authorized source-plan author.
- Interactive registers are paginated and selectors bounded (organization setup picker: 1,000 options). Reports show bounded interactive rows; exports support at most 200,000 source rows, PDFs at most 5,000 rows and statements 1,000 postings, with explicit errors/truncation signals rather than silent full-report claims. Larger jobs need streaming/object-storage delivery. Fine-grained category/year/date filter parity across all dues screens, specialist comparison visualizations and expense-only event summaries remain improvements.
- Budget API supports multiple category lines; the compact UI editor submits one category per request. Invoice/receipt PDFs use embedded Vera fonts; full complex-script shaping and custom branding/logo templates need additional work.
- Persist encryption keys and private object storage, introduce runtime least-privilege database credentials, malware scanning for private PDFs, monitoring/alerts, retention policies, backup/restore rehearsals, distributed-worker failure drills and independent security/accessibility/load review before production. A database owner can override application triggers; use an appropriately restricted runtime role.

The core acceptance workflows are implemented and locally verified. The limits above are explicit rollout/extension work; this report does not certify a fully reconciled production treasury, statutory accounting package or successful live gateway integration. Phase 6 must integrate identity cards/report building with these existing scopes and records, resolve the remaining operational gaps and repeat the full regression suite before any separately authorized release.

## API route inventory

Unless otherwise noted, paths are relative to `/api/v1/societies/{tenant}/finance`. All use existing authentication and scoped authority. The public signed callback is `/api/v1/finance/webhooks/{connectionId}`.

- `GET /accounts` — `FinanceEndpoints.cs`
- `POST /accounts` — `FinanceEndpoints.cs`
- `POST /assessments` — `FinanceEndpoints.cs`
- `GET /bank-transactions` — `FinanceOperations.cs`
- `POST /bank-transactions/{id:guid}/match` — `FinanceOperations.cs`
- `GET /banks` — `FinanceOperations.cs`
- `POST /banks` — `FinanceOperations.cs`
- `POST /banks/{id:guid}/import` — `FinanceOperations.cs`
- `GET /budgets` — `FinanceOperations.cs`
- `POST /budgets` — `FinanceOperations.cs`
- `POST /budgets/{id:guid}/decision` — `FinanceOperations.cs`
- `GET /budgets/{id:guid}/history` — `FinanceOperations.cs`
- `POST /budgets/{id:guid}/revision` — `FinanceOperations.cs`
- `GET /contributions` — `FinanceOperations.cs`
- `POST /contributions` — `FinanceOperations.cs`
- `POST /contributions/{id:guid}/invoice` — `FinanceOperations.cs`
- `GET /conversions` — `FinanceReports.cs`
- `GET /dashboard` — `FinanceReports.cs`
- `GET /donors` — `FinanceOperations.cs`
- `POST /donors` — `FinanceOperations.cs`
- `GET /dues-plans` — `FinanceEndpoints.cs`
- `POST /dues-plans` — `FinanceEndpoints.cs`
- `PUT /dues-plans/{id:guid}` — `FinanceEndpoints.cs`
- `GET /entities` — `FinanceEndpoints.cs`
- `POST /entities` — `FinanceEndpoints.cs`
- `PUT /entities/{id:guid}` — `FinanceEndpoints.cs`
- `GET /event-policies` — `FinancePortal.cs`
- `POST /event-policies` — `FinancePortal.cs`
- `POST /event-policies/{id:guid}/decision` — `FinancePortal.cs`
- `GET /events/{id:guid}/pricing` — `FinancePortal.cs`
- `POST /events/{id:guid}/registration-invoice` — `FinancePortal.cs`
- `GET /exchange-rates` — `FinanceReports.cs`
- `POST /exchange-rates` — `FinanceAccountingEndpoints.cs`
- `GET /exemptions` — `FinanceEndpoints.cs`
- `POST /exemptions` — `FinanceEndpoints.cs`
- `POST /exemptions/{id:guid}/decision` — `FinanceEndpoints.cs`
- `GET /expenses` — `FinanceOperations.cs`
- `POST /expenses` — `FinanceOperations.cs`
- `GET /expenses/{id:guid}` — `FinanceOperations.cs`
- `POST /expenses/{id:guid}/attachments` — `FinanceOperations.cs`
- `POST /expenses/{id:guid}/decision` — `FinanceOperations.cs`
- `POST /expenses/{id:guid}/pay` — `FinanceOperations.cs`
- `GET /files/{id:guid}` — `FinanceOperations.cs`
- `POST /finance/webhooks/{id:guid}` — `FinancePayments.cs`
- `GET /inter-entity` — `FinanceAccountingEndpoints.cs`
- `POST /inter-entity` — `FinanceAccountingEndpoints.cs`
- `POST /inter-entity/{id:guid}/decision` — `FinanceAccountingEndpoints.cs`
- `POST /inter-entity/{id:guid}/settlements` — `FinanceAccountingEndpoints.cs`
- `GET /invoices` — `FinanceEndpoints.cs`
- `POST /invoices` — `FinanceEndpoints.cs`
- `GET /invoices/{id:guid}` — `FinanceEndpoints.cs`
- `POST /invoices/{id:guid}/cancel` — `FinanceEndpoints.cs`
- `GET /invoices/{id:guid}/checkout-options` — `FinancePayments.cs`
- `POST /invoices/{id:guid}/credit` — `FinanceEndpoints.cs`
- `POST /invoices/{id:guid}/decision` — `FinanceEndpoints.cs`
- `POST /invoices/{id:guid}/document` — `FinanceDocuments.cs`
- `POST /invoices/{id:guid}/issue` — `FinanceEndpoints.cs`
- `POST /invoices/{id:guid}/late-fee` — `FinanceEndpoints.cs`
- `POST /invoices/{id:guid}/payment-intents` — `FinancePayments.cs`
- `GET /invoices/{id:guid}/pdf` — `FinanceDocuments.cs`
- `POST /invoices/{id:guid}/send` — `FinanceCommunication.cs`
- `POST /invoices/{id:guid}/submit` — `FinanceEndpoints.cs`
- `GET /jobs` — `FinanceEndpoints.cs`
- `GET /jobs/{id:guid}/download` — `FinanceDocuments.cs`
- `GET /journals` — `FinanceAccountingEndpoints.cs`
- `POST /journals` — `FinanceAccountingEndpoints.cs`
- `POST /journals/requests/{id:guid}/decision` — `FinanceAccountingEndpoints.cs`
- `GET /journals/{id:guid}` — `FinanceAccountingEndpoints.cs`
- `POST /journals/{id:guid}/reverse` — `FinanceAccountingEndpoints.cs`
- `GET /ledger` — `FinanceReports.cs`
- `GET /my/invoices` — `FinancePortal.cs`
- `GET /my/payments` — `FinancePortal.cs`
- `GET /my/receipts` — `FinancePortal.cs`
- `GET /my/statements/{member:guid}/export` — `FinancePortal.cs`
- `GET /my/summary` — `FinancePortal.cs`
- `GET /payments` — `FinanceEndpoints.cs`
- `POST /payments` — `FinanceEndpoints.cs`
- `POST /payments/{id:guid}/allocate` — `FinanceEndpoints.cs`
- `POST /payments/{id:guid}/decision` — `FinanceEndpoints.cs`
- `POST /payments/{id:guid}/refunds` — `FinanceEndpoints.cs`
- `GET /periods` — `FinanceEndpoints.cs`
- `POST /periods` — `FinanceEndpoints.cs`
- `POST /periods/{id:guid}/close` — `FinanceEndpoints.cs`
- `GET /providers` — `FinancePayments.cs`
- `POST /providers` — `FinancePayments.cs`
- `POST /providers/{id:guid}/disable` — `FinancePayments.cs`
- `POST /providers/{id:guid}/verify` — `FinancePayments.cs`
- `GET /receipts` — `FinanceEndpoints.cs`
- `POST /receipts/{id:guid}/document` — `FinanceDocuments.cs`
- `GET /receipts/{id:guid}/pdf` — `FinanceDocuments.cs`
- `GET /refunds` — `FinancePayments.cs`
- `POST /refunds/{id:guid}/decision` — `FinancePayments.cs`
- `POST /refunds/{id:guid}/dispatch` — `FinancePayments.cs`
- `GET /reminders` — `FinanceCommunication.cs`
- `POST /reminders` — `FinanceCommunication.cs`
- `GET /reminders/{id:guid}/runs` — `FinanceCommunication.cs`
- `POST /reminders/{id:guid}/toggle` — `FinanceCommunication.cs`
- `GET /reports` — `FinanceReports.cs`
- `GET /reports/catalog` — `FinanceReports.cs`
- `POST /reports/export` — `FinanceDocuments.cs`
- `GET /settlements` — `FinanceAccountingEndpoints.cs`
- `POST /settlements/{id:guid}/decision` — `FinanceAccountingEndpoints.cs`
- `GET /setup` — `FinanceEndpoints.cs`
- `GET /statements/chapter/{id:guid}` — `FinanceReports.cs`
- `GET /statements/member/{id:guid}` — `FinanceReports.cs`
