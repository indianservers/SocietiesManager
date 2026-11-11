# Phase 1 Society & Chapter Completion Report

Verified on 11 November 2026 (Asia/Calcutta). This is a working local Phase 1 foundation. Production deployment and release certification have not been performed. Operational checks and the functional limitations below remain explicit release work.

## Repository audit

The supplied workspace was empty, including hidden entries. There were no existing frameworks, organization tables, migrations, authentication modules, reusable components, or baseline tests to preserve. Ancestor instruction-file checks found no applicable AGENTS.md. The implementation therefore starts one modular monolith with the requested stack. No competing society/association authority or separate region/chapter hierarchy was introduced.

## Implemented features

- React/TypeScript/Vite frontend with Tailwind, light enterprise shell, responsive sidebar, workspace switcher, persistent client routing, cached/background-refreshing API queries, debounced directory search, server pagination, sorting, accessible labels, focus-trapped drawers, keyboard dismissal, empty/loading/error states, and notifications.
- ASP.NET Core 10 Web API, EF Core 10, Npgsql, PostgreSQL 17.7, generated OpenAPI, JSON structured logging, container build definition, and CI verification workflow.
- Identity signup, verified email, password login, lockout, recovery/reset, logout, profile, and password change. Cookies are HttpOnly/SameSite Strict, production cookies require HTTPS, and mutations validate anti-forgery tokens. No browser-local authentication-token storage.
- Society application tracking, normalized duplicate detection, authorized approval/rejection, transactional initial administrator assignment, and resumable eight-step setup/activation. No administrator entitlement is created before verification and approval.
- One configurable hierarchy using `Levels` and `Units`. Administrators create/rename levels and configure permitted parents/root eligibility. Region/Chapter are initial templates rather than fixed structural levels; arbitrary names and alternate trees work.
- Unit creation/editing/status changes, parent reassignment, child browsing, organization metadata, optimistic revision checks, database cycle guards, tenant-aware composite foreign keys, tenant transaction locks, and permanent retention of historical units.
- Region and chapter workspaces with search/status filters/sort/pagination, real database counts, recursive descendant chapter counts, and currently effective regional leadership names when assigned. Chapter overview carries location, contact, charter, description, and meeting details.
- Configurable society/national/region/district/zone/chapter governance role templates; permission selection/editing; responsibility text; dated, scoped assignments and revocation. Membership categories remain separate foundational metadata.
- Verified officer invitations with hashed, expiring, single-use tokens. Joining a society creates a link; a separate authorized role assignment grants privileges.
- Multiple society affiliations on one verified account, with workspace selection restricting visible data while independent server checks remain authoritative.
- Society settings, branding color/logo setup, authenticated PNG branding storage through `IObjectStore`, platform approval/lifecycle administration, and append-only audit history.
- Durable PostgreSQL email outbox with development pickup and an SMTP adapter. Redis was not added: measured database permission queries are small, and an extra cache would complicate invalidation without a demonstrated need.

## Database model and migrations

Authoritative business tables are `Tenants` (society aggregate), `Applications`, `Links`, `Levels`, `Units`, `GovernanceRoles`, `Assignments`, `Onboardings`, `Audits`, `Invitations`, `Outbox`, and `Assets`. ASP.NET Identity owns `AspNetUsers`, claims/logins/tokens and its standard supporting tables. Governance authorization uses one application role/permission engine; the unused Identity role tables do not provide a second application entitlement system.

Society/application/profile/settings metadata uses JSONB; organizational/access relationships use UUID keys, tenant-aware constraints, indexed foreign keys, and PostgreSQL timestamptz. There is no duplicate `societies` table competing with `Tenants`. Organization history is represented by immutable unit audit entries containing before/after values, rather than an additional duplicate history model.

Applied successfully to both the local application database and the real PostgreSQL integration database:

- `20261111031228_InitialFoundation`
- `20261111031340_HierarchyIntegrity`
- `20261111031446_SocietyInvitations`
- `20261111033106_BrandingAssets`
- `20261111033306_OrganizationReadModels`

Database guards reject cycles, invalid parent types, cross-tenant references, stale writes, invalid effective-date ranges, audit mutation, and physical unit deletion. Trigram indexes support name/code searches. A stable recursive database function computes descendant chapter counts.

## API inventory

The generated specification is `backend/openapi.json`, also served at `/openapi/v1.json`. Tenant context is carried explicitly in `/api/v1/societies/{tenant}/...`, rather than relying on a mutable client header or trusted workspace selection. Request DTOs and read projections define the public contracts.

- `GET /health`
- `GET /api/v1/auth/csrf`
- `POST /api/v1/societies/{tenant}/assets`
- `GET /api/v1/societies/{tenant}/assets/{id}`
- `GET /api/v1/users/me`
- `PUT /api/v1/users/me`
- `POST /api/v1/users/me/password`
- `POST /api/v1/societies/applications`
- `GET /api/v1/societies/applications`
- `POST /api/v1/societies/applications/{id}/review`
- `GET /api/v1/workspaces`
- `GET /api/v1/platform/societies`
- `POST /api/v1/platform/societies/{id}/status`
- `POST /api/v1/societies/{tenant}/invitations`
- `POST /api/v1/invitations/accept`
- `POST /api/v1/auth/signup`
- `POST /api/v1/auth/verify`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/logout`
- `POST /api/v1/auth/forgot`
- `POST /api/v1/auth/reset`
- `GET /api/v1/societies/{tenant}/organizations/levels`
- `POST /api/v1/societies/{tenant}/organizations/levels`
- `PUT /api/v1/societies/{tenant}/organizations/levels/{id}`
- `GET /api/v1/societies/{tenant}/organizations/units`
- `POST /api/v1/societies/{tenant}/organizations/units`
- `GET /api/v1/societies/{tenant}/organizations/units/{id}`
- `PUT /api/v1/societies/{tenant}/organizations/units/{id}`
- `GET /api/v1/societies/{tenant}/role-candidates`
- `GET /api/v1/societies/{tenant}/permissions`
- `GET /api/v1/societies/{tenant}/roles`
- `POST /api/v1/societies/{tenant}/roles`
- `PUT /api/v1/societies/{tenant}/roles/{id}`
- `GET /api/v1/societies/{tenant}/role-assignments`
- `POST /api/v1/societies/{tenant}/role-assignments`
- `DELETE /api/v1/societies/{tenant}/role-assignments/{id}`
- `GET /api/v1/societies/{tenant}/dashboard`
- `GET /api/v1/societies/{tenant}/settings`
- `PUT /api/v1/societies/{tenant}/settings`
- `GET /api/v1/societies/{tenant}/onboarding`
- `PUT /api/v1/societies/{tenant}/onboarding`
- `GET /api/v1/societies/{tenant}/audit-logs`

## UI coverage

Functional routes and shared views:

- `/`: public landing; `/signup`, `/login`, `/forgot`, `/reset`, `/verify`: account lifecycle.
- `/app/register-society`, `/app/applications`: registration, status tracking, and authorized approval center.
- `/app/setup`: eight-step resumable setup wizard.
- `/app`: society/role-scope dashboard; active society and organization scope switcher in the shell.
- `/app/hierarchy`: lazy hierarchy explorer and reusable level/unit creation/editing drawers.
- `/app/regions`, `/app/chapters`: searchable/filterable/sortable directories and creation/editing drawers.
- `/app/units/:id`: chapter/unit overview and subordinate units.
- `/app/roles`: role directory, role editor, dated assignment/revocation, invitation flow, permission matrix.
- `/app/accept-invitation`: verified-account acceptance.
- `/app/profile`: profile and account security.
- `/app/settings`, `/app/platform`, `/app/audit`: society settings, platform lifecycle controls, audit log.

Full CRM, leadership roster management, activities/events, communication, dues, ID cards, and report-building modules have not been implemented prematurely. Their phase labels are informational rather than misleading working links. Phase 1 reporting consists of authorized organizational counts and audit history.

## Verification results

- Backend build: passed, zero warnings and zero errors.
- TypeScript/Vite production build: passed. Current output includes a lazy app chunk of approximately 69 kB (19 kB gzip) and a shared runtime/vendor entry around 285 kB (90 kB gzip).
- Real PostgreSQL/HTTP integration suite: **67/67 passed**. Results: `tests/results.json`; runner: `tests/integration.py`.
- Coverage includes signup/verification/login/recovery, CSRF, application/duplicate/approval/rejection/activation/onboarding, region/chapter operations, invalid/cross-tenant parents, cycles, stale edits, role editing/assignment/effective dates, scoped access, one identity with two societies, invitation replay prevention, metadata privacy, PNG validation/redaction/isolation, audit history, and migrations.
- Browser verification: landing, sign-in, database-backed dashboard, chapter directory, actual chapter creation/save, and mobile directory/drawer/navigation at 390×844. Desktop viewport restored. Screenshot: `tests/workspace-preview.jpg`.
- Dependency audits: npm reports zero vulnerabilities; NuGet reports no vulnerable direct/transitive packages using the configured advisory sources.
- CI configuration is supplied but has not been executed on a remote CI provider. Docker execution and real external SMTP delivery are unverified.

## Measured performance

A real PostgreSQL transaction inserted **1,000 societies, 10,000 regions, 100,000 chapters, and 5,000 role assignments**. Fixture insert time: **6809.16 ms**. All benchmark fixtures were rolled back afterward. Existing integration fixtures remain isolated in `assemblia_test`; demonstration records live only in the ignored local development database.

- tenant paginated directory: median **0.109 ms**, p95 **0.308 ms**, 30 iterations.
- lazy children: median **0.058 ms**, p95 **0.148 ms**, 30 iterations.
- subtree count: median **0.071 ms**, p95 **0.398 ms**, 30 iterations.
- permission evaluation: median **0.228 ms**, p95 **0.41 ms**, 30 iterations.
- indexed search: median **0.209 ms**, p95 **0.438 ms**, 30 iterations.

The separate small-fixture authenticated HTTP directory check measured median **5.42 ms** and p95 **5.689 ms** across 20 requests. These include the local HTTP/authentication/query/serialization path, while the capacity dataset timings above are SQL-only. They are warm single-client local measurements, not a throughput, concurrency, production-SLA, or 100,000-unit single-tenant claim.

`tests/performance.json` contains EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) plans, buffer usage, timings and the actual PostgreSQL version. `tests/http-performance.json` records the HTTP measurement.

## Security findings and resolutions

- An integration run found CSRF validation before authentication. Authentication now precedes validation; authenticated CSRF and rejection checks pass.
- Early dependency versions triggered advisories. Compatible patched dependencies replaced them; the final advisory scans are clear.
- Workspace metadata initially risked exposing the application/profile JSON to ordinary linked users. It now projects a branding-only allowlist; the regression test confirms applicant mobile details are absent.
- Platform administrators cannot read private tenant dashboards without a separate tenant link and scoped permission. Verified in tests.
- A scoped chapter officer cannot read other regions, edit another tenant, grant new roles, or gain access through a workspace parameter. Past/future assignments do not grant current authority.
- Logo uploads validate PNG structure/checksums/bounded decoded rows, strip metadata, reject SVG/HTML, use random object IDs outside the web root, and authorize tenant retrieval. Upload support deliberately accepts only 8-bit noninterlaced RGB/RGBA PNG up to 2 MB and 4096×4096 pixels.
- Historical units and audit entries remain protected in PostgreSQL, not just by hidden UI controls. A restricted production runtime-role script is supplied.

## Remaining issues and release dependencies

1. The global platform capability currently supports an explicitly bootstrapped administrator. Separately delegated Platform Super Admin / Platform Administrator / Support Administrator templates and an approved support-access workflow need product/security decisions and implementation before a broadly delegated platform rollout. Support access to private tenant records is currently denied rather than implicitly granted.
2. Generic user-defined resource-condition expressions are not implemented. Current server conditions explicitly enforce membership, tenant, organization descendants, permission and effective dates. Phase 2 must add field-level member privacy policies before exposing personal directory data.
3. Real SMTP credentials/delivery, TLS/reverse-proxy configuration, a restricted runtime database identity, persistent encryption keys, backups/restore, object-storage durability and monitoring require environment configuration and operational verification. The compose owner connection is a local convenience, not a production least-privilege configuration.
4. The durable outbox dispatcher assumes a single API instance and retries at a fixed interval with a bounded attempt count. Multi-instance claiming, exponential backoff, delivery provider idempotency and dead-letter administration remain deployment work. No SMS/provider integration is bundled.
5. The object-store interface has a local-filesystem implementation; an S3/Azure adapter, asset lifecycle cleanup and replicated deployment storage are not included.
6. Concurrent-user load tests, adversarial concurrency tests, deep single-tenant hierarchy stress, complete automated browser regression, broader accessibility audit and container runtime checks remain. The workload measurements do not establish production capacity. PostgreSQL patch selection must be reviewed for the target deployment; local testing used 17.7.
7. Membership CRM, complete officer/member lifecycle, merge workflows beyond a retained Merged status, and advanced report building remain future modules. No production deployment was attempted or authorized.

## Phase 2 dependencies

Preserve the tenant/unit composite-key pattern. Add memberships as separate records from identities and governance assignments, distinguish member categories, implement effective membership state and minimal directory projections, and enforce field-level visibility on the server. Reuse the existing role scopes, invitations, audit infrastructure, assets, shell and paginated API conventions. Resolve the release dependencies above before treating this foundation as production-certified.
