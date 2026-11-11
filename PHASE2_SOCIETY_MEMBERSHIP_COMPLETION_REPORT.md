# Phase 2 — Society Membership Completion Report

Verified locally on 11 November 2026. Phase 1 was extended in place; no deployment was performed. Activities/events, communication campaigns, dues/finance and ID-card modules remain future work.

## Implemented software

- One global Person identity with tenant-scoped Membership affiliations, unique society member numbers, effective dates, categories, status and immutable history. Multiple chapter affiliations follow society policy; multiple societies reuse the verified canonical person. Phone numbers never identify or merge people. Explicit recipient-verified account claiming can reconcile a provisional imported person with an existing canonical identity without duplicating that identity.
- Verified self-service enrollment, draft/resume, configurable approval-stage snapshots, changes/resubmission, rejection, withdrawal API and final activation. Decisions retain reviewer identity, role snapshot, organization, timestamp and evidence IDs. Recipient-bound, hashed invitation tokens support expiry, revocation, usage limits and local QR generation; invitations cannot bypass approval.
- Paginated searchable CRM/directory, filters, sorting, selected exports, configurable columns, private saved views, quick view, profile editor, personal/professional metadata, membership/leadership history, documents, photo and privacy preferences. Desktop tables become selectable mobile cards.
- Configurable society defaults and chapter overrides for directory scope, contact consent, simultaneous chapter memberships, approval stages and transfer review. Directory scopes include chapter, district, configured regional ancestor, society, selected chapters and international scope within the same society.
- Officer position templates and custom positions, required-position configuration, explicit mapping to existing governance roles, appointment proposals/approval/revocation, effective terms, exclusive-term overlap prevention, supporting documents and outgoing/incoming/approver handover workflow. Position titles do not confer permissions by themselves.
- Scoped committees with chair/member responsibilities, dated roster entries and retained removal history. Reviewed chapter transfers preserve person, membership number, joining history and transfer history; scheduled transfers are applied by the background worker and revoke authority lost through the move.
- CSV and bounded XLSX preview with column mapping, duplicate/invalid row reporting, durable confirmation, progress, errors and retry. Batches use database claims and current authorization; imports create inactive/unclaimed records and never silently overwrite verified profiles.
- Real membership intelligence: status/category/chapter counts, joining trends, active leadership and configured coverage. Retention is unavailable when dated historical baseline is missing. Vacancies are unavailable until required positions are configured; no fabricated percentages or placeholder counts.

## Database and migrations

Five Phase 2 migrations extend the five Phase 1 migrations:

1. `20261111040200_MembershipGovernance`: canonical people, affiliations, policies/categories, applications/decisions/history, invitations, transfers, positions/appointments/handovers, committees/rosters, documents, import jobs/rows and saved views; existing Assignments gain membership source/revocation fields. Seeds existing societies and extends existing role permissions. Includes immutable-history/no-delete guards and serialized officer-overlap protection.
2. `20261111041621_MembershipPrivacyAndReviewMetadata`: configured directory ancestor level and reviewer organization/role snapshots, including explicit legacy metadata handling.
3. `20261111042717_MembershipEvidenceDocuments`: evidence attached to exactly one membership or application, with tenant-bound foreign keys.
4. `20261111044908_RequiredLeadershipCoverage`: configurable required positions for honest vacancy reporting.
5. `20261111045339_MemberImportLookupIndex`: tenant-scoped normalized email lookup index for import duplicate checks.

Composite foreign keys prevent cross-society references; revisions reject stale mutations. Tenant transaction locks serialize sensitive affiliation/leadership changes. Initial Phase 2 downgrade requires reviewed backup recovery instead of automatically deleting membership data. Development migration check reports up to date; the API benchmark applies all ten migrations to a newly created disposable PostgreSQL database and removes that database afterward.

## API and UI

The checked-in `backend/openapi.json` is refreshed from the running API. All endpoints are under `/api/v1`; authorization and CSRF are enforced on the server.

Public/authenticated enrollment routes: `/membership/catalog`, `/membership/applications`, `/membership/applications/{id}`, `/membership/applications/{id}/withdraw`, `/membership/me`, `/membership/invitations/accept`.

Tenant routes use `/societies/{tenant}/membership` with these resource groups:

- `members`: paginated directory, manual creation, private profile read/update; member `documents` and `photo` upload/retrieval.
- `categories`, `policy`: category maintenance and society/chapter policy overrides.
- `applications`: review queues, decisions, evidence/history and private application documents.
- `invitations`: creation, listing and revocation.
- `positions`, `appointments`: configuration, appointment decisions and revocation.
- `handovers`: creation, incoming review and authorized approval.
- `committees` and committee `members`: committee lifecycle and dated roster maintenance.
- `transfers`: requests and staged decisions.
- `imports`: preview, job detail, confirmation and retry.
- `views`, `export`, `intelligence`: private saved filters, selected neutralized CSV exports and real aggregate reporting.

Exact parameter names, verbs and schemas are authoritative in OpenAPI and the endpoint source files. UI routes are `/app/join`, `/app/members`, `/app/members/directory`, `/app/members/profile/:id`, `/app/members/applications`, `/app/members/leadership`, `/app/members/committees`, `/app/members/transfers`, `/app/members/import`, `/app/members/settings`, `/app/members/self`. Existing workspace selection and Phase 1 hierarchy/settings/role routes are reused.

## Permission and privacy rules

Every access decision combines verified account, tenant link, effective dated assignment, explicit permission and descendant organization scope. Selected workspace narrows access and never grants authority. Platform administration does not bypass private society data. Chapter Presidents and Secretaries receive only configured scoped permissions; regional officers cover authorized descendants; society administrators cover their own society. Billing-only access cannot reveal private administrative profiles or contacts.

Ordinary directory DTOs omit birth dates, addresses, full profile/privacy JSON, private history and global person IDs. Contacts require both policy and member consent, with explicit self/authorized administrative projections. Photos and documents have independent server checks and private random-ID object storage; branding URLs cannot bypass them. Membership responses disable caching. Export uses the same permission-filtered projection and neutralizes spreadsheet formulas.

Approved officer access is bounded by both appointment and source membership dates/status. Revocation, expiry, inactivity and transfer remove authority; revoked/expired grants disappear from workspace discovery. Self appointment/approval is prohibited; handovers require incoming review and authorized independent approval. All updates use existing role infrastructure; the managed baseline directory role cannot be edited into an administrative role.

## Verification results

- `dotnet build backend --no-restore`: succeeded, zero warnings/errors.
- `dotnet ef migrations has-pending-model-changes --project backend --no-build`: no pending model changes.
- `npm --prefix frontend run build`: TypeScript and production Vite build succeeded.
- `python tests/membership_integration.py`: **67/67 foundation and 95/95 Phase 2 checks passed** using real HTTP APIs and PostgreSQL. Results: `tests/results.json`, `tests/phase2-results.json`.
- Checks cover staged approval/change/rejection, duplicate and stale actions, recipient claiming/replay/expiry, canonical multi-society affiliations, contact consent, private identifiers, chapter/region/society isolation, President/Secretary/Treasurer boundaries, term expiry/revocation/overlap, handovers, committee history, transfers, CSV/XLSX preview/background processing, private photos/documents/application evidence, export injection protection and real aggregates.
- `npm audit`: zero reported vulnerabilities. `dotnet list backend package --vulnerable --include-transitive`: no vulnerable packages reported by configured sources. These scans do not substitute for an independent penetration test.
- Browser verified sign-in, real directory records, manual member creation, policy controls, desktop table and responsive mobile directory. Screenshot: `tests/phase2-workspace-preview.jpg`. Demo records are explicitly fictional and optional; metrics query their stored data.
- CI workflow includes builds, integration and capacity scripts. Remote CI and Docker runtime execution were not performed on this workstation.

## Measured performance

Local PostgreSQL scenario: 100,000 persons/memberships across ten chapters, 30 warm SQL iterations. Target p95 under 100 ms; chapter directory **1.506 ms**, name search **8.570 ms**, chapter active counts **45.313 ms**, privacy-filtered directory **0.534 ms**. All targets passed. Transactional fixtures were rolled back; EXPLAIN ANALYZE BUFFERS evidence is saved in `tests/membership-performance.json`.

Fresh disposable-database authenticated API scenario: 100,000 memberships, three warmups and 20 measured requests per operation. Target p95 under 500 ms; society directory **61.113 ms**, 10,000-member chapter directory **32.914 ms**, name search **366.880 ms**, membership intelligence **202.075 ms**. All targets passed. These timings include session validation, authorization, privacy projection, database and serialization. Results: `tests/membership-api-performance.json`. Small integration-fixture directory p95 was **13.513 ms** (`tests/phase2-http-performance.json`). Measurements are single-client warm local runs, including other local activity; they establish no production concurrency SLA.

## Known limits and release work

- Real SMTP delivery, provider operations, production storage, TLS, backup/restore, monitoring and restricted runtime database identity remain rollout dependencies. Reapply/review table grants after migrations. The inherited mail dispatcher is for a single API instance; import jobs use row claims, but that does not make mail delivery distributed-safe. No deployment or publication occurred.
- Supporting PDFs have bounded header/EOF framing validation and download as untrusted attachments; full parsing, malware scanning and quarantine must be added before production document intake. Photos accept sanitized noninterlaced 8-bit RGB/RGBA PNG up to 2 MB/4096 pixels, not JPEG or arbitrary image types.
- XLSX currently reads `xl/worksheets/sheet1.xml`, rejects formulas and enforces ZIP/XML expansion limits. Imports support up to 10,000 rows and basic name/email/phone/city/number mapping; richer metadata is edited through CRM. Unresolved duplicates remain errors for review. Provisional identities with cross-society affiliations require separate reconciliation rather than automatic merging.
- Email verification is implemented; there is no SMS verification provider. International directory scope remains inside the society. Arbitrary cross-society discovery is deliberately unavailable.
- Withdrawal exists as an API action; the enrollment UI concentrates on saving/resuming/submitting. A saved draft whose chapter falls outside the first catalog page can require chapter search to display that choice. Selected-chapter policy selection is bounded by its current loaded configuration list. These are UI follow-up items, not authorization bypasses.
- Historical retention needs reliable dated baseline data. Required-position coverage measures configured required chapter positions, not an inferred staffing model for every custom hierarchy. Finance/payment-derived renewal and event activity history remain future modules.
- Inherited Phase 1 platform delegation/support design and wider accessibility, concurrent load and independent security testing remain open. No claim of production certification is made.

## Phase 3 dependencies

Reuse canonical Person/Membership and organization IDs for registrations and attendance; evaluate effective membership and scoped officer authority at the action time. Define event participant consent, eligibility, cancellations, capacity, attendance corrections and historical retention. Extend the current private document adapter with production scanning/storage and reusable durable jobs where needed. Add activity records to the member Activities view only when those records exist. Do not introduce duplicate chapter member stores, broad title-based access, finance placeholders or fabricated engagement statistics.
