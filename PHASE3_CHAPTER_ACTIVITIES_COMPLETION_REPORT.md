# Phase 3 — Chapter Activities and Events Completion Report

Verified locally on 11 November 2026. This extends the existing .NET 10 / React / PostgreSQL application; Phases 1 and 2 were preserved. No production deployment or external messaging campaign was performed.

## Existing-system audit

Reviewed the existing hierarchy, canonical Person/Membership identities, dated Assignments and role permissions, committees, membership approval handlers, audit guards, private object store, outbox, reusable React components, migrations and both completion reports. There was no existing activity/event implementation. The initial regression and subsequent integrated runs exercised real HTTP and PostgreSQL behavior. The implementation reuses Access, organizational descendants, verified Identity sessions, CSRF, canonical memberships, committees, IObjectStore, the outbox, audit history and the persistent application shell. It does not introduce chapter member stores, a parallel hierarchy, independent governance roles or another notification engine.

## Implemented features

- Canonical chapter/region/society activity posts with separate actual author, publishing organization and optional committee identity. Official publishing requires explicit current organizational authority. Categories are configurable society data, with standard activity and event templates.
- A responsive community feed with cursor pagination, virtualized cards, automatic next-page loading, debounced search, category/organization/author/date/event filters, bookmarks, lazy media and optimistic reactions with rollback. Regional scopes use the existing hierarchy; a post is stored once rather than copied to every chapter.
- Compact post drawer with formatting controls and preview, title/text/category, official identity, location, tags, directory-validated member mentions, related event, audience selection, drafts and scheduled publication. Media supports multi-file selection/drop, centered square crop/zoom, reordering, cover selection, video preview and attachments. Browser photographs are converted to PNG before server validation.
- Configurable approval stages, independent author/reviewer separation, distinct reviewers for successive community approval stages, changes/rejection, immutable versions/decisions and renewed review after material edits or media changes. A claimed background publisher releases approved due content and checks the author's current authority before publication.
- Unique per-user reactions, comments/replies, private bookmarks, reports, audited moderation, featuring/unfeaturing APIs, soft archival and policy-controlled blocking. Moderation authority cannot be defeated by blocking the moderator.
- Member-visible chapter profile with permitted member count, scoped current leadership, visible activity/recognition history, event links and photo gallery. The Phase 2 member Activities tab now displays posts visible to the requesting user. Official recognitions are canonical posts marked as achievements, with the same authorization/review history; no certificate engine was added.
- Event drafts, compact six-step editor, duplication, review, scheduled publication, cancellation, co-hosts, IANA time zone, UTC effective timestamps, venue/online/hybrid settings, media, registration windows, capacity, companions, guest policy, audience rules and custom questions. Events with registrations cannot silently change ownership/date/audience or reduce capacity below confirmed seats.
- Bounded month/week/agenda/list calendars and My Events filtering. Queries load at most 93 days and 50 events per page. Week ranges cross month boundaries and month cells align with weekday headers. Event labels and passes use the configured time zone.
- Event audience intersections across authorized organizations, individual memberships, categories, current committees and leadership, with eligibility/exclusion/duplicate counts and communication exclusions. The organizer sees permitted directory identities, not arbitrary private contact values.
- Recipient-bound, expiring/revocable event invitations, in-app invitation history, secure acceptance links, manually initiated WhatsApp sharing and email queueing through the existing outbox. The existing membership communication consent is required for member invitations. Missing/false consent is excluded; duplicate recipient affiliations with an opt-out are conservatively skipped. No SMS delivery is simulated.
- RSVP Going/Maybe/Not Going/No Response with audited changes, independently from registration. Registration supports approval, cancellation, companions, invited verified guests, transactional capacity and ordered waitlists. Cancelling a seat promotes eligible waitlisted registrations while registration remains open.
- Real digital passes: random 32-byte references stored only as hashes; no personal data inside the QR; local QR rendering; mobile display; printable PDF download; reissue and revocation. Pass issuance requires confirmed registration and current eligibility; reissue invalidates older passes.
- Online, authorized camera/manual check-in, registration search, duplicate detection, operator/timestamp/entry point and explicit people-present count. Event-level database locks and unique attendance constraints serialize multiple operators. Corrections void attendance with an audit reason; registration totals never stand in for attendance. Cancelled events reject check-in and remain visible to their original authorized audience.
- Chapter/regional/society intelligence and event analytics computed from PostgreSQL, with grouped queries, date bounds and permission scopes. No impressions, reach, follower counts, payment success or invented engagement values are displayed.

## Database changes

Seventeen new tables: CommunityPolicy, ActivityCategory, ActivityPost, CommunityVersion, CommunityDecision, ActivityReaction, ActivityComment, ActivityBookmark, ActivityReport, CommunityBlock, CommunityMedia, CommunityEvent, EventInvitation, EventRsvp, EventRegistration, EventPass and EventAttendance.

Three Phase 3 migrations extend the ten previous migrations:

1. `20261111052753_ChapterActivitiesEvents`: tables, typed tenant-scoped foreign keys, resource/date indexes, GIN audiences, unique reactions/bookmarks/RSVP/registrations/attendance, policy/category seeds and explicit existing-role permission upgrades. Adds immutable community review/version guards, event date/capacity checks and pass/attendance registration-event consistency triggers.
2. `20261111054922_ActualAttendeePartyCount`: actual people-present count, distinct from registered party size, with a positive-count constraint.
3. `20261111060822_CommunityIntegrity`: prevents physical deletion of canonical posts/events/registrations/attendance and validates audience organization tenant ownership in PostgreSQL.

The development database applies all thirteen migrations. The capacity script creates a new isolated database, migrates from empty, seeds synthetic fixtures, executes measurements, stops its API and removes that database. `dotnet ef migrations has-pending-model-changes --project backend --no-build` reports no pending changes. Historical content is archived or corrected rather than physically deleted through application endpoints.

## APIs and screen routes

APIs follow `/api/v1/societies/{tenant}/community`. Exact verbs, parameters and schemas are in refreshed `backend/openapi.json` and the endpoint files.

- Configuration: `/setup`, `/policy`, `/categories`, `/categories/{id}` and `/authors`.
- Activity: `/feed`, `/posts`, `/posts/{id}`, post `/decision`, `/history`, `/reaction`, `/comments`, `/bookmark`, `/report`, `/moderate`; `/comments/{id}/hide`, `/reports` and `/blocks/{user}`.
- Private media: `/media`, `/media/{id}`. Every fetch resolves and authorizes its canonical parent; raw object-store paths are not exposed.
- Profiles/reporting: `/chapters/{id}`, `/members/{id}/activity`, `/intelligence`.
- Events: `/events`, `/events/{id}`, event `/decision`, `/cancel`, `/duplicate`, `/audience`, `/invitations`, `/rsvp`, `/registrations`, registration `/decision`, `/passes`, pass `/revoke`, `/checkins`, `/attendance`, attendance `/correct` and `/analytics`.
- Personal: `/my/events`, `/invitations`, `/invitations/accept`, `/invitations/{id}/revoke`.
- Anonymous public post/media reads use `/api/v1/public/societies/{tenant}/community/posts/{id}` and `/media/{id}`. They require an active society, enabled public policy and a currently published explicitly public post. Private IDs return no public record. Turning public policy off closes those endpoints.

UI surfaces share routes, drawers and tabs: `/app/community` feed/composer; `/app/community/posts/:id` detail/conversation/history; `/app/community/review` approvals and recoverable drafts; `/app/community/moderation`; `/app/community/settings`; `/app/community/chapters/:id` chapter overview/activity/events/members/leadership/achievements/gallery; `/app/community/events` directory/calendar/My Events and creation wizard; `/app/community/events/:id` overview, invitations, registration desk, pass, check-in, attendance and analytics; `/app/community/invitations`; `/app/community/intelligence`. Public shared posts use `/public/community/:tenant/posts/:id`. Membership profile activity history reuses the existing Phase 2 route.

## Permission and security validation

Server decisions combine tenant membership, effective organizational grants, active source membership and scoped permissions. Existing leadership titles are templates only; permission checks use stored roles and dates. Members publish personally only from an active own affiliation when policy permits. Official identities and expanded audiences require explicit authority. Organization, committee, mention, related-event and co-host references are checked; cross-society audiences are also rejected by database guards.

Feed/detail/media/search projections apply the same visibility predicate. Authors retain access to their own drafts/history; scoped reviewers/moderators have explicit review access. Ordinary members cannot inspect other participants' private registrations or administrative analytics. Public endpoints never fall back to private feed projections. Community responses disable HTTP caching; authentication remains HttpOnly with CSRF on mutations, and community endpoints have per-account rate limits. Restricted rich text is encoded with an attribute-free formatting allowlist; React renders only sanitized stored content. Unsafe uploads and unauthorized media URLs are denied.

Invitations use hashed random references, verified recipient binding and expiry; acceptance does not bypass registration eligibility or approval. Pass validation checks tenant/event, revocation, confirmation, eligibility and a bounded check-in window. Check-in permits a registration party count only up to its reserved size and does not assume every companion arrived.

## Tests and observed browser results

- Backend build: succeeded with zero warnings/errors. Frontend TypeScript and production Vite build: succeeded.
- `python tests/community_integration.py`: **67/67 Phase 1 + 95/95 Phase 2 + 112/112 Phase 3 = 274 passing checks**. Real PostgreSQL and authenticated HTTP are used; routine email delivery is local/test only. Results are in `tests/results.json`, `tests/phase2-results.json` and `tests/phase3-results.json`.
- Coverage includes personal/official posting, impersonation denial, draft/rejection/approval/scheduling, private direct URLs, regional/society scope, XSS encoding, version history, reactions, replies/moderation, bookmarks/reporting/blocking, mentions, chapter profiles, media validation/IDOR, event approval/publishing, expiry, consent, RSVP changes, capacity/waitlists, guest enrollment, questions/companions, opaque passes/reissue/revocation, wrong-event/duplicate/cancelled check-in, attendance corrections, accurate metrics, cursor pagination and database history guards.
- Five simultaneous registration clients competing for one seat produced exactly one confirmation and four waitlist entries. Two independent check-in operators produced exactly one Valid and one Already Checked In result.
- Browser verified draft saving and persisted media controls, feed/calendar/detail navigation, separate RSVP and confirmed registration, real pass issuance/PDF download, and responsive 390×844 feed layout. The downloaded PDF rendered as one clean page with Poppler. An independent ZXing decoder recovered the opaque reference from the rendered PDF and its hash matched the issued database pass. The fictional sample pass was revoked through the API after QA. Evidence: `tests/phase3-browser-results.json`, `tests/phase3-pass-render.png`, `tests/phase3-event-pass-preview.jpg`, `tests/phase3-mobile-preview.jpg`, `tests/phase3-workspace-preview.jpg`.
- `npm audit` reported zero vulnerabilities; NuGet's configured advisory sources reported no vulnerable direct/transitive packages. Camera hardware, real SMTP/SMS/WhatsApp delivery, remote CI, Docker execution, broader accessibility and independent penetration testing were not verified.
- CI now runs the combined community regression and Phase 3 benchmark in addition to earlier builds/benchmarks. No remote CI success is asserted.

## Performance measurements and definitions

`tests/community-api-performance.json` records a fresh isolated PostgreSQL database with **100,000 members, 10,000 chapters, 100,000 posts, 10,000 events, 50,000 invitations, 50,000 registrations and 25,000 attendance records**. Each authenticated endpoint receives three warmups and twenty measured single-client requests. The documented local p95 target is **1,000 ms**; all five targets pass. Final numerical results are recorded below from that machine-readable file. EXPLAIN ANALYZE BUFFERS plans cover feed-cursor and date-range calendar queries. Fixture insertion time excludes some later community seeding and is not an end-to-end import throughput claim.

- Society feed: median **277.358 ms**, p95 **313.957 ms**.
- Chapter feed: median **56.268 ms**, p95 **63.794 ms**.
- Calendar: median **13.571 ms**, p95 **14.739 ms**.
- Scoped intelligence: median **356.243 ms**, p95 **380.78 ms**.
- Attendance analytics: median **15.152 ms**, p95 **16.044 ms**.
- Integration-fixture feed: median **12.191 ms**, p95 **13.721 ms** (500 ms target passed).

These are warm local measurements including session validation, authorization, PostgreSQL and serialization, not production concurrency or availability guarantees. The separate integration-fixture feed uses twenty warm requests with a 500 ms p95 target (`tests/phase3-http-performance.json`).

Metrics use precise definitions: published activities in the requested publication interval; distinct author accounts as contributors; chapter owners with published posts as reporting chapters; published events by start date; completed events whose end is past; upcoming events within ninety days; all registration requests separately from currently confirmed registrations/seats; non-voided check-in records as actual attendance; explicitly recorded people-present totals as attended seats; distinct checked-in accounts as participating people; stored reactions and non-hidden comments as engagement. No-shows are unavailable until event end. Regional comparisons follow permitted chapter ancestors classified Region. Results reflect current recorded status and corrections, with audit history retained.

## Known limits and rollout dependencies

- The editor supports safe formatting markup and preview rather than a full document-style WYSIWYG editor. Crop is centered-square/zoom, not arbitrary freeform panning. Converted photographs must satisfy the existing bounded sanitized PNG format. Video validation checks MP4 framing/brand; it does not transcode codecs. PDF validation checks bounded framing, not semantic safety. Production quarantine/malware scanning and media lifecycle/storage durability remain required.
- Camera scanning is implemented with ZXing and an explicit user-started camera request; physical camera behavior was not tested. Check-in requires connectivity; no offline synchronization is implied. One pass represents a registration party; companions are counted, not separate person masters or independently named passes.
- Setup organization pickers are capped at 500; author options at 200; audience preview/member pickers at 100; detail conversations at 100; chapter gallery at 30; attendance detail at 200; chapter comparison display at 100. Feed/event/registration pagination is implemented, but deeper picker/thread/gallery pagination remains follow-up work for large deployments. Invitation API batches accept up to 1,000 members; the UI exposes its bounded preview selection.
- In-app invitations and verified invitation links work. Email is queued into the inherited outbox, with no provider receipt claim or invitation-to-provider reconciliation dashboard. SMS is explicitly unavailable; WhatsApp sharing opens a user-controlled share link and does not assert sending. Fine-grained channel preferences, guest unsubscribe, campaign administration, distributed outbox claiming/backoff/idempotency and delivery reconciliation belong to Phase 4. Member invitations already honor the existing explicit communication consent.
- Public sharing is an explicit post-level opt-in behind society policy. Chapter profiles and full event/registration surfaces remain authenticated; this is not a public event-ticketing storefront. International audience is within the same society; there is no cross-tenant network discovery.
- Events are free-registration workflows. No payment is collected or simulated; paid-event integration, financial posting/refunds and payment-derived confirmation remain Phase 5. Certificates and member ID cards remain Phase 6. There is no popularity ranking or invented reach measurement.
- Existing production dependencies remain: real SMTP verification, persistent Data Protection keys, shared object storage, restricted runtime database grants for newly migrated tables, TLS, backup/restore, operational monitoring and independent/concurrent load/security validation. Earlier platform delegation/support limitations remain in the Phase 1 report.
- Local development now uses API **5092** and Vite **5174** because another application occupies 5088. Other applications were left running. Clearly labeled fictional preview records are optional fixtures and are never seeded by normal startup. Their displayed counts still come from PostgreSQL.

## Phase 4 dependencies

Extend the existing outbox rather than adding another messaging engine. Preserve recipient binding, membership communication consent and organizational audience boundaries. Add per-channel preferences/consent evidence, guest unsubscribe, provider credentials, template safety, delivery attempts/receipts, idempotent multi-worker claims, backoff/dead-letter tools and invitation-to-transport correlation. Re-evaluate eligibility/authority before expanding or dispatching audiences. Keep Queued, accepted by transport and delivered as separate facts. Carry forward the operational storage/scanning/security work before any production release.
