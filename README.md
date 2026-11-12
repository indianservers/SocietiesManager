# Assemblia — Society, Region & Chapter Management

Six-phase modular monolith: React/TypeScript/Vite/Tailwind frontend, ASP.NET Core 10 API, EF Core/Npgsql, PostgreSQL. No production deployment has been performed.

## Local workspace

The configured development instance uses PostgreSQL at `127.0.0.1:55439`, API at `127.0.0.1:5092`, and Vite at `http://127.0.0.1:5176`. Other local services were already using the usual ports and were preserved. Vite fails explicitly if its port is occupied; `scripts/dev.ps1 -UiPort <port>` can set a matching public URL for an alternate UI port. Generated passwords, database data, assets, email pickup files, and preview credentials live in ignored `.local/`.

1. Start PostgreSQL if needed: `.local/pgsql/bin/pg_ctl.exe -D .local/pgdata -l .local/postgres.log -o "-h 127.0.0.1 -p 55439" start`.
2. Apply migrations with `./scripts/dev.ps1 -Migrate`.
3. Run `./scripts/dev.ps1` in one terminal.
4. Run `npm run dev --prefix frontend` in another.
5. Open the frontend. Local demo credentials are in `.local/preview-account.json`. The demo society is explicitly fictional; its displayed counts come from database queries. Demo seeding is an optional script and is never part of production startup.

A new user signs up, opens the verification link from `.local/mail/` in development, signs in, and submits a society application. A platform administrator reviews it. Approval transactionally creates the society, user link, initial administrator assignment, role templates, configurable Region/Chapter defaults, and resumable onboarding.

## Fresh setup

### Configurable example society and mobile verification

`python scripts/seed-society-example.py` creates a separate local example tenant from `scripts/society-example.json`: AIMER Society, 15 chapters, 105 institute memberships, 30 individual memberships and 15 corporate memberships. The name, chapters, categories and volumes live only in that optional JSON fixture; application code uses the selected tenant and its branding. Run with `--config <file> --output .local/<account>.json` for another society. Reruns resume existing fixtures without replacing member data. Existing preview tenants are preserved. Example administrator credentials are saved privately in `.local/society-example-account.json`.

Institute/company memberships use configurable categories and holder names, organization registration details and authorized representative fields. Account claims still require a verified human account; organization metadata grants no governance permissions. Existing individual fields, consent, approval, billing, documents and dated scoped roles remain available.

The mobile browser tests use a dedicated Vite instance on `http://127.0.0.1:5176` (start with `npm run dev --prefix frontend -- --host 127.0.0.1 --port 5176 --strictPort`) to avoid another local app occupying 5174. Install Playwright and Chromium (`pip install playwright`, `python -m playwright install chromium`), seed the example, then run `python tests/mobile_roles.py`, `python tests/mobile_workflows.py`, and `python tests/mobile_event_journey.py` in that order. They write JSON evidence and screenshots under `tests/`; synthetic account secrets stay under `.local/`. Role tests use temporary assignments and remove them in `finally`; workflow tests retain clearly marked synthetic events and claim one example individual membership for the synthetic participant. The event journey covers independent approval, RSVP, registration, PDF pass download and duplicate-protected manual check-in. Chromium mobile emulation does not verify physical camera hardware or live payment/messaging providers.

Install .NET SDK 10, Node 24, PostgreSQL 17+, and Python 3.12+. Run `npm ci --prefix frontend`, `dotnet tool restore`, and `pip install -r tests/requirements.txt`. Create `.local/dev.json` with a `connectionString` and PostgreSQL `password`, using port 55439 for the included local scripts. Alternatively configure `ConnectionStrings__Database` directly and run the API using `dotnet run --project backend --no-launch-profile --urls http://127.0.0.1:5092`.

Migrations are explicit: `dotnet run --project backend -- --migrate`. Bootstrap the first platform administrator with environment variables `BOOTSTRAP_EMAIL` and `BOOTSTRAP_PASSWORD`, then run `dotnet run --project backend -- --bootstrap-admin`. The command refuses to run after an administrator exists. No default production password is bundled.

## Verification

- `dotnet build backend`
- `npm run build --prefix frontend`
- `python tests/integration.py` — starts an isolated API on 5089; migrates a dedicated `assemblia_test` database; real HTTP, authentication, authorization, and database guards.
- `python tests/benchmark.py` — inserts 1,000 societies / 10,000 regions / 100,000 chapters / 5,000 assignments in a transaction, collects query latency and EXPLAIN ANALYZE BUFFERS plans, then rolls back.
- `dotnet list backend package --vulnerable --include-transitive`
- `npm audit --prefix frontend`

Test scripts require the local database password in `.local/dev.json`; they never target the application database for integration fixtures. Benchmark numbers are local single-client warm-query measurements, not production capacity claims. Results are saved under `tests/`. CI repeats builds, integration tests, and benchmarks using a PostgreSQL service.

## Architecture and security

- `Tenants` is the authoritative society aggregate. There is no competing association table. Flexible profile/contact/branding metadata is JSONB; hierarchy and access relationships use typed keys and composite foreign keys.
- `Levels` defines names, classification, root eligibility, and permitted parents. `Units` is the single adjacency-list hierarchy. Recursive CTEs fetch permitted descendants; pages return at most 100 rows. Regional chapter counts include descendant chapters through districts/subregions.
- `Access` intersects tenant membership, permission, dated role assignment, descendant scope, and optional selected workspace. Platform administrator capability does not bypass tenant membership. Privilege delegation requires society-wide grant authority and possession of every delegated permission.
- ASP.NET Identity provides password hashing, verified email, reset tokens, lockout, security stamps, and HttpOnly sessions. Mutations require an anti-forgery header. No authentication token goes into local storage. Authentication endpoints are rate-limited.
- Hierarchy mutations are serialized per tenant with transaction advisory locks. Composite keys reject cross-tenant relationships. Both API and database reject cycles. Revision checks reject stale edits. Units cannot be physically deleted; archives retain history. Audit entries are append-only.
- Verification, recovery, and invitation mail uses a durable PostgreSQL outbox. Development writes a private local pickup file. Production requires SMTP configuration (`Smtp__Host`, `Port`, `User`, `Password`, `From`). The included worker is intended for one API instance; distributed claiming/backoff is a rollout dependency.
- `IObjectStore` abstracts assets; the local implementation stores random-ID files outside the public root. Branding upload accepts RGB/RGBA noninterlaced PNG only, <=2 MB and <=4096×4096, validates checksums and bounded decompression, removes metadata, and checks tenant access on retrieval. External image/website references on units require HTTPS. No SVG/HTML uploads are accepted.
- Membership categories are separate from governance templates and application permissions. Phase 2 adds private CRM and permission-filtered directory projections.

## Containers and production preparation

`Dockerfile` builds the UI and API into a single non-root runtime. `compose.yaml` supplies PostgreSQL and persistent volumes for database, encryption keys, and assets. Docker is unavailable on this workstation, so the image has not been executed here.

Provide the compose-required database secret, public HTTPS URL, and SMTP settings outside source control. Run migrations as a separate migration identity, create a restricted runtime role using `scripts/runtime-role.sql`, and configure the API connection string for that role. The compose database-owner connection is a local deployment convenience and must be replaced before production. Terminate TLS at a trusted reverse proxy; production cookies require HTTPS. Persist and protect Data Protection keys and assets. Validate backup/restore, SMTP delivery, concurrency, and operational alerts before release. Publication requires explicit authorization.

See `PHASE1_SOCIETY_CHAPTER_COMPLETION_REPORT.md` for verified coverage and remaining work, and `SIX_PHASE_DEVELOPMENT_ROADMAP.md` for the next phases.

## Phase 2 membership and governance

Open `/app/members` for membership intelligence, directory, applications, leadership, committees, transfers, imports and policies. Verified users can apply at `/app/join` and manage their own affiliations at `/app/members/self`. Contact sharing defaults to private and requires both society policy and member consent. Titles confer authority only through an explicitly configured existing role and an effective, approved term.

Run `python tests/membership_integration.py` for all 67 foundation plus 95 membership checks. Run `python tests/membership_benchmark.py` for transactional 100,000-member SQL measurements and `python tests/membership_api_benchmark.py` for authenticated HTTP measurements against a disposable, freshly migrated database. Results and query plans are saved in `tests/`. These are warm local single-client measurements, not production concurrency guarantees.

CSV/XLSX imports validate first, then create inactive, unclaimed affiliations through durable bounded background batches. They never infer identity from phone numbers. A verified recipient must explicitly claim an imported affiliation. QR invitations render locally. Transfers become effective through the background worker after required approvals and the effective date.

Evidence uploads accept bounded sanitized RGB/RGBA PNG or PDF attachments; PDF validation checks file framing and does not replace production malware scanning. XLSX import currently reads `xl/worksheets/sheet1.xml`, rejects formulas, and supports up to 10,000 rows with basic name/contact/city/number mapping. Configure real SMTP, production storage, backup/restore, restricted runtime database grants for newly migrated tables, TLS and monitoring before release. Docker execution, real mail delivery and remote CI were not verified here. See `PHASE2_SOCIETY_MEMBERSHIP_COMPLETION_REPORT.md` for full coverage and limits.


## Phase 3 activities and events

Open `/app/community` for the authorized activity feed, `/app/community/events` for calendars and events, and `/app/community/review` for approval queues and resumable drafts. Chapter profiles are at `/app/community/chapters/:id`; membership profiles now link to visible activity history. Post and event creation use compact drawers, with private media, audience rules and approval snapshots.

RSVP records intention; registration reserves or waitlists capacity; check-in alone records attendance. Passes contain random opaque references, render QR locally, download printable PDFs and revoke previous passes on reissue. Check-in is online and permission protected, with manual search and camera scanning support. Camera hardware was not available for verification; a downloaded PDF was rendered and its QR independently decoded and matched to the issued database pass.

Membership's existing **Receive society communication** consent controls event invitations. Members must explicitly opt in; recipients without consent are skipped. Email invitations queue into the existing outbox, and the UI does not claim provider delivery. Phase 4 adds the unified communication workspace described below. Public post reading is disabled by default and requires both society policy and explicitly authorized public publishing.

Run `python tests/community_integration.py` for 67 foundation + 95 membership + 112 community checks, including concurrent capacity/check-in races. Run `python tests/community_api_benchmark.py` for a freshly migrated disposable database with 100,000 members, 10,000 chapters, 100,000 posts, 10,000 events, 50,000 invitations/registrations and 25,000 attendance records. Its measured p95 target is 1,000 ms per authenticated endpoint; results and EXPLAIN ANALYZE BUFFERS evidence are saved in `tests/community-api-performance.json`. These local warm single-client results are not a production concurrency SLA.

The development API moved to **5092** because another application occupies 5088. Vite remains at **5174**; scripts and proxy configuration match. `scripts/seed-phase3-demo.py` optionally creates clearly fictional preview content only in the named demo society; it is never run at startup. Demo counts are real queries over those marked fixtures.

See `PHASE3_CHAPTER_ACTIVITIES_COMPLETION_REPORT.md` for API routes, migrations, verification and remaining limits. In particular, production document/video malware scanning, storage durability, SMTP/provider verification, distributed outbox dispatch and broader browser/accessibility/concurrency testing remain release work. No production deployment was performed.


## Phase 4 communication center

Open `/app/communications/inbox` for private member messages, chapter groups, shared team inquiries and private staff notes. Campaigns, announcements, approvals, audience previews, typed multilingual templates, durable schedules/automations, quiet-hour preferences, provider accounts, delivery tracking and notifications reuse membership, hierarchy and Outbox infrastructure. Credentials are encrypted/write-only. Provider verification is required; acceptance is not delivery.

Run `python tests/communication_integration.py` for all four phases. Run `python tests/communication_api_benchmark.py` for an isolated migrated large-data benchmark, overlapping readers, signed callback burst and mock worker throughput. Tests never contact real recipients. See `PHASE4_COMMUNICATION_COMPLETION_REPORT.md` for measured results and limitations.

Configure PublicUrl and canonical Communications:WebhookBaseUrl for deployment, persist Data Protection keys, and configure verified credentials through authorized account settings. Meta needs an explicitly supported Graph API version. GatewayBaseUrl is server-operator configuration. AllowTestProvider is Development-only and disabled in the retained preview. No live provider activation or production deployment was performed.


## Phase 5 finance workspace

Open `/app/finance` for explicit legal entities, precise dues and billing, verified collections, immutable accounting, budgets/expenses, donation/event finance and protected reports. Finance extends the existing membership, organization, registration and Communication Outbox engines. Setup starts with zero balances; receipts require confirmed collections. The retained demo contains fictional entity metadata and no seeded financial transactions.

Run `python tests/finance_integration.py` for all 478 Phase 1–5 integration checks. Run `python tests/finance_api_benchmark.py` for a disposable freshly migrated PostgreSQL database with asserted 100,000 memberships, 10,000 chapters, 100,000 invoices and 200,000 journal lines plus a real 1,000-member assessment job. See `PHASE5_FINANCE_COMPLETION_REPORT.md` for routes, measured results, controls and known limits.

Razorpay configuration is encrypted/write-only. Browser checkout callbacks do not confirm payment; signed verified server events do. The mock gateway is explicitly Development-only and disabled in the preview. No external sandbox/live gateway cycle or real payment was executed. Bank matching currently handles positive collection credits; negative expense/refund debits, fees and restricted-fund accounting remain extensions. Persist Data Protection keys and private storage and complete controlled provider/operational validation before live use.


## Phase 6 connected platform

Open `/app/studio/templates` for the shared card/certificate visual editor, `/app/studio/bulk` and `/app/studio/downloads` for independently approved durable generation and paged exports, `/app/service` for joint projects and verified hours/impact, and `/app/intelligence` for scoped cross-module reporting and guided assistant. Branding, security and platform operations extend the existing identity/hierarchy/accounting/communication engines. The retained preview has a labelled sample template; its financial records remain unseeded.

Build the API before running tests. `python tests/studio_integration.py` runs all 574 checks. `python tests/runtime_role_integration.py`, `python tests/phase6_pdf_qa.py`, `python tests/phase6_api_benchmark.py` and `python tests/backup_restore_integration.py` verify restricted grants, physical artifacts, a disposable scale scenario and encrypted isolated recovery. Set ASSEMBLIA_API_DLL to an isolated compiled DLL when another process owns the default build. Verification uses synthetic data and never activates real providers.

See PHASE6_FINAL_COMPLETION_REPORT.md, SIX_PHASE_FINAL_STATUS.md and the architecture/schema/API/permission references. Production remains BLOCKED in PRODUCTION_READINESS_CHECKLIST.md. Deployment and recovery scripts are supplied with runbooks; no deployment was executed. Do not configure real external credentials or enable production until the documented gates are accepted.

The completed Phase 6 benchmark retains two failed finance read objectives at 300,000 invoices (dashboard/ageing above 2 s p95), while Phase 6 reads and actual 1,000-card/100-certificate/ZIP jobs complete. Its nonzero target assertion is intentional evidence of an open acceptance gate; see PERFORMANCE_BENCHMARK_REPORT.md.

## Consolidated navigation

The main society menu now has seven areas: **Home, People, Activities, Messages, Finance, Documents and Settings**. My account is the footer account button; platform operators have an additional restricted Platform area. This replaces the original 24-item sidebar and the intermediate 11-item menu.

- Home combines the overview, intelligence, reports and assistant. Overview and reports use short panels rather than stacked sections.
- People combines members, leadership, organization, requests/imports and membership rules. Organization has hierarchy, region and chapter tabs; membership operations retain applications, transfers and imports.
- Activities combines the community feed, events/invitations, service projects, volunteers and results/reviews. The review selector exposes community approvals, moderation and insights. Volunteer directory and attendance/hours have separate tabs.
- Messages has six horizontal tabs: Inbox, Broadcasts, Templates, Automations, Delivery and Settings. Channel and assignment/group options are inbox filters.
- Finance has five horizontal tabs: Overview, Billing, Money & budgets, Accounts, and Reports & settings. A compact tool selector retains invoices, dues, assessments, payments, receipts, bank reconciliation, reminders, personal balances, budgets, expenses, donations, event finance, accounts, journals, ledger, settlements, reports, audit and configuration.
- Documents retains templates/designer, the card/certificate issuance register with printing, and generation/download/request/policy tools.
- Settings combines General, Branding, Permissions, Setup, Audit and Health. Branding separates identity, domains and readiness. General and branding forms use tabs that keep all controls mounted, preserving entered values and complete form submissions.
- My account retains profile, security/password, memberships, documents, achievements, applications and joining a chapter. Administrative areas remain permission-filtered.

Tabs show one selected feature panel at a time. Horizontal tab rows scroll within narrow screens rather than wrapping into long menus. Long tables scroll within their panel. Native form validation reveals the tab containing the first invalid field. Original detail screens, drawers, server actions and permissions are retained.

Legacy URLs redirect to the matching section and retain unrelated query parameters and fragments. Section query keys support bookmarks, reload and browser Back; existing grouped Finance URLs also retain their selected tool. Only the selected authorized feature mounts, while form tabs deliberately keep inactive input controls mounted to preserve values.

Run `npm --prefix frontend run test:navigation` for the eight selection, permission, nested-tab, compact-selector and form-retention checks, and `npm --prefix frontend run build` for TypeScript and production bundling. Current browser evidence is in `tests/simple-menu-results.json` and `tests/simple-menu-*.jpg`; the older `navigation-consolidation` files document the previous iteration. Backend APIs, database structure and authorization were not changed by this navigation consolidation.
