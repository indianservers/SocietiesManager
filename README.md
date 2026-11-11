# Assemblia — Society, Region & Chapter Management

Phase 1 modular monolith: React/TypeScript/Vite/Tailwind frontend, ASP.NET Core 10 API, EF Core/Npgsql, PostgreSQL. No production deployment has been performed.

## Local workspace

The configured development instance uses PostgreSQL at `127.0.0.1:55439`, API at `127.0.0.1:5092`, and Vite at `http://127.0.0.1:5174`. Other local services were already using the usual ports and were preserved. Generated passwords, database data, assets, email pickup files, and preview credentials live in ignored `.local/`.

1. Start PostgreSQL if needed: `.local/pgsql/bin/pg_ctl.exe -D .local/pgdata -l .local/postgres.log -o "-h 127.0.0.1 -p 55439" start`.
2. Apply migrations with `./scripts/dev.ps1 -Migrate`.
3. Run `./scripts/dev.ps1` in one terminal.
4. Run `npm run dev --prefix frontend -- --host 127.0.0.1 --port 5174` in another.
5. Open the frontend. Local demo credentials are in `.local/preview-account.json`. The demo society is explicitly fictional; its displayed counts come from database queries. Demo seeding is an optional script and is never part of production startup.

A new user signs up, opens the verification link from `.local/mail/` in development, signs in, and submits a society application. A platform administrator reviews it. Approval transactionally creates the society, user link, initial administrator assignment, role templates, configurable Region/Chapter defaults, and resumable onboarding.

## Fresh setup

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

Membership's existing **Receive society communication** consent controls event invitations. Members must explicitly opt in; recipients without consent are skipped. Email invitations queue into the existing outbox, and the UI does not claim provider delivery. SMS and full communication-center features remain Phase 4 work. Public post reading is disabled by default and requires both society policy and explicitly authorized public publishing.

Run `python tests/community_integration.py` for 67 foundation + 95 membership + 112 community checks, including concurrent capacity/check-in races. Run `python tests/community_api_benchmark.py` for a freshly migrated disposable database with 100,000 members, 10,000 chapters, 100,000 posts, 10,000 events, 50,000 invitations/registrations and 25,000 attendance records. Its measured p95 target is 1,000 ms per authenticated endpoint; results and EXPLAIN ANALYZE BUFFERS evidence are saved in `tests/community-api-performance.json`. These local warm single-client results are not a production concurrency SLA.

The development API moved to **5092** because another application occupies 5088. Vite remains at **5174**; scripts and proxy configuration match. `scripts/seed-phase3-demo.py` optionally creates clearly fictional preview content only in the named demo society; it is never run at startup. Demo counts are real queries over those marked fixtures.

See `PHASE3_CHAPTER_ACTIVITIES_COMPLETION_REPORT.md` for API routes, migrations, verification and remaining limits. In particular, production document/video malware scanning, storage durability, SMTP/provider verification, distributed outbox dispatch and broader browser/accessibility/concurrency testing remain release work. No production deployment was performed.
