# Phase 4 — Communication Center completion report

Verified locally on 11 November 2026. The existing .NET 10 / React / PostgreSQL application now includes a functional Communication Center. No production deployment, live provider activation or messages to real members were performed. External operation remains conditional on verified operator configuration and live acceptance tests.

## Architecture and delivered behavior

The implementation extends canonical Person/Membership records, the existing Units/Levels hierarchy, dated assignments, community blocking, approval decisions, private object store and Outbox. The previous mail worker is replaced by one durable dispatcher; there are no separate channel queues or duplicate membership engines.

Backend modules: CommunicationDomain, CommunicationService, CommunicationEndpoints, CommunicationCampaignEndpoints, CommunicationOperations, CommunicationProviders, MetaTemplates, CommunicationWebhookEndpoints, CommunicationWorker and CommunicationAutomationRunner. The React module is lazy loaded from `frontend/src/communications.tsx`. SignalR sends personal invalidation events without message bodies; bounded polling provides recovery.

- Private messages: policy-permitted member conversations, canonical thread/client-message deduplication, unread/read receipts, permitted search, mute, report, block and private files. Administrative roles do not grant access to private DMs. Current membership, directory permission, opt-out and block checks apply across endpoints.
- Groups: chapter/committee participants, join/leave history, add/remove and Members/Admins/Officers/Approval posting policies. Removal revokes access. Groups containing a blocked peer are withheld from that user's listing and access.
- Team inbox: explicitly shared member inquiries; status, priority, current authorized assignee, follow-up, tags, resolution and internal staff notes. Notes are excluded from member history, previews and search.
- Templates: sixteen starter InApp templates, typed variables, language metadata, revisions, safe rendering and authorized preview. Values resolve from the actual member, chapter, region, society, sending officer and event context. Event URLs use PublicUrl. Unknown variables and executable expressions are rejected. Local templates do not imply provider approval.
- Audiences: typed AND/OR filtering inside current sender scope; organization branches, chapter, status, category, country/state/language, joined dates, officer position, committee, RSVP, registration and attendance. Preview reports consent/contact/suppression exclusions. Canonical-person deduplication prevents multiple-affiliation duplicates. Phone numbers remain contacts, never identity keys.
- Campaigns/announcements: drafts, independent staged reviews, high-volume permission, schedules, cancellation, duplication, priority, expiry, pinning and acknowledgment. Submission persists recipient/destination/variable snapshots; material edits reset review and preserve snapshot versions. Recipients can open personalized notices without campaign administration access. Separate approvers can review pending campaigns without send permission. Authors cannot self approve.
- Automation: member/application lifecycle, birthdays/anniversaries, event lifecycle/RSVP reminders, officer appointments/expiry, announcements and recurring reminders. Durable source keys, execution logs, stored-author authority and safe test mode are implemented. Live rules create campaigns through the same approval/delivery path. Browsers do not drive scheduling.
- Notifications: one topbar center, preference-aware message/system/review notices, deduplication, mark-read and actual authenticated in-app reads. Unsupported cost/read metrics remain unavailable.

## PostgreSQL and dispatch

Two additive migrations extend the previous thirteen: `20261111070234_UnifiedCommunicationCenter` and `20261111070652_ProviderTemplateBindings`. They add eighteen communication tables, extend Outbox, seed existing tenant policy/templates and upgrade established role permissions. Tenant foreign keys, private history/cursor/search indexes, snapshot keys, notification/provider-event deduplication and immutable attempt/provider-event triggers are included.

Fresh migrations passed in disposable benchmark databases. Development migrations are current, EF reports no pending model changes, and `backend/openapi.json` is refreshed from the running API.

The dispatcher claims jobs with PostgreSQL FOR UPDATE SKIP LOCKED and commits leases before transport. It rechecks membership, destination changes, consent, channel preferences, suppression, sender scope/high-volume authority and verified account/channel. Quiet hours defer durable jobs in the recipient timezone. Account rate limits are serialized in PostgreSQL. Transient rate failures receive bounded exponential retries, then DeadLetter. Authorized officers can retry definitive failures. Expired external leases and uncertain transport outcomes become Unknown for reconciliation, without blind resend. InApp lease recovery uses notification deduplication.

Held, Queued, Processing, Retry, Accepted, Delivered, Read, Failed, Unavailable, Unknown, DeadLetter, Suppressed, Cancelled and local Pickup have distinct meanings. Acceptance is not delivery. Immutable attempt records and verified provider events support actual metrics. Unsupported costs remain null. Existing event invitations share the worker and now carry canonical keys correlating worker outcomes with invitation status.

Hourly retention tombstones old message bodies, removes expired notifications and redacts completed old Outbox bodies. Parent-message removal denies attachment downloads. Immutable audit/transport records remain. This is logical application retention, not physical media erasure or legal-hold support.

## Provider and attachment boundaries

- Meta uses the official Graph host, an operator-specified supported API version, phone identity verification, approved-template sync/binding and positional variable rendering. Free-form replies require a verified incoming message within 24 hours. One PNG/PDF can be uploaded and sent inside that window. Account-bound HMAC callbacks and handshake tokens are verified.
- Resend verifies the sender domain and sends safe HTML/attachments with a provider idempotency key. Timestamped Svix signatures protect callbacks. Bounce/complaint and opaque unsubscribe links create tenant/channel suppressions. Configured inbound callbacks fetch plain text from the fixed receiving API and validate the receiving address before routing to the scoped team inbox.
- Twilio implements SMS behind the common interface, verifies sender ownership/capability and validates canonical URL/form signatures. SMS makes no attachment/read-receipt claim. The operator-configured HTTPS Gateway adapter supports another provider without new workflow/queue engines.
- Test is explicitly enabled only in Development. All automated outbound tests use it; production-signature tests submit synthetic payloads without contacting providers.

Credentials are Data Protection encrypted and write-only, absent from browser bundles. Sender/account changes invalidate verification. Fixed provider hosts, disabled redirects, bounded callbacks/uploads, CSRF checks and authenticated private downloads protect these paths. Persist and protect Data Protection keys across deployment instances.

External attachments use a Held draft: create message, upload private sanitized files, finalize the existing delivery. Held jobs cannot be claimed; finalize retries do not duplicate jobs. InApp supports five PNG/PDF files of 2 MB each; WhatsApp supports one outbound file; SMS rejects files. PNG sanitization and PDF framing validation do not replace malware scanning.

## Verification

`python tests/communication_integration.py` runs all four phases against actual PostgreSQL and HTTP. Evidence is in `tests/results.json`, `tests/phase2-results.json`, `tests/phase3-results.json` and `tests/phase4-results.json`. Final totals are recorded below.

Coverage includes private-DM IDOR/admin denial, tenant/chapter scope, blocks/preferences, read state, attachments, group posting/removal, private notes, assignment, consent, typed variables/XSS, snapshots, independent reviews, edit invalidation, cancellation, duplicate drafts, accepted-versus-delivered states, secret redaction, Meta/Twilio/Resend signature verifiers, callback deduplication, bounce suppression, held files, quiet hours, retry/dead-letter behavior, uncertain leases, restart scheduling, automation idempotency and real metrics.

Backend builds with zero warnings/errors; TypeScript/Vite production builds pass. npm production audit reports zero vulnerabilities; NuGet reports none in configured sources. These are dependency checks, not penetration-test certification. CI invokes the combined suite and communication benchmark; remote CI was not run here.

Browser checks used the local demo account and an actual unsent campaign draft, plus desktop/mobile inbox states. No fake inbox conversations were seeded. The draft has zero submitted recipients and sends nothing. At 390 pixels the document had no horizontal overflow. Screenshots: `tests/phase4-workspace-preview.png` and `tests/phase4-mobile-preview.png`. Local preview: http://127.0.0.1:5174/app/communications/inbox; API 5092; PostgreSQL 55439.

## Performance

`tests/communication_api_benchmark.py` creates, migrates and removes its own disposable database. Synthetic fixtures: 100,000 people/memberships, 10,000 chapters, 1,000 team conversations, 100,000 historical messages, a 50,000-recipient snapshot and 25,000 notifications. Twenty warm authenticated requests per route use a local p95 target of 1,000 ms. Final measurements are in `tests/communication-api-performance.json` and summarized below.

Additional samples cover eight overlapping readers using one identity, 200 mock transport jobs and thirty concurrent signed callbacks. The 50,000-recipient fixture measures bounded snapshot retrieval, not a fully delivered 50,000-job campaign. Callback bursts use unmatched synthetic references and measure verified ingestion/storage, not carrier delivery. These are local scenarios, not production SLAs or multi-identity real-time load certification.

## Known limits and release work

No live provider/SMTP credentials were supplied or activated. Live sender/domain approval, DNS, receiving routes, supported Meta version, permissions, quotas and end-to-end delivery need operator-controlled recipients before production use. No real bulk campaign was sent.

Incoming WhatsApp/SMS text and configured Resend plain text are supported. Automatic inbound media ingestion, HTML-only email rendering, MIME threading/quoted-reply parsing, campaign attachments and media-bearing Meta template headers are not implemented. Unknown external addresses stay unlinked inquiries; outbound replies require an authorized consenting membership binding. Rich email uses the conservative existing renderer rather than a full email designer.

Automation audit scans consider at most 100 recent sources and 50 executions per pass; birthday scans cover 10,000 active memberships and anniversary scans 1,000 matches. Dates currently use UTC. Large-scale catch-up, fair tenant scheduling, broader backfills, parallel transport and multi-instance fault injection remain release work. Review notices scan 100 recent pending campaigns and 500 candidate officers. Revisit these bounds before a large rollout.

Legacy authentication/member/event invitation SMTP jobs share this worker but remain unscoped and excluded from campaign analytics. Event invitation outcomes are correlated; migrating all legacy invitation/guest flows to explicitly selected scoped provider accounts needs separate compatibility work.

Physical object deletion, backup expiry, legal holds, malware scanning, at-rest policy and provider retention remain deployment work. Costs stay unavailable unless an adapter supplies verified cost; billing reconciliation belongs to finance. Unknown transports require provider/operator reconciliation and deliberately have no one-click retry. No Docker runtime, production deployment, external penetration test, disaster-recovery or complete multi-identity SignalR load test was performed. Broader accessibility/device testing remains release work.

## Phase 5 dependencies

Finance should reuse canonical membership, effective authority, approvals, idempotent callbacks, evidence storage, notifications and this Outbox. Add invoice/payment/receipt templates and typed triggers without another delivery engine. Monetary types, immutable ledger/reconciliation records and treasury authority remain separate Phase 5 work. Message counts are not verified provider costs. No payment flow is activated by Phase 4.

## Official provider references

[Twilio request validation](https://www.twilio.com/docs/usage/security), [Resend webhook signatures](https://resend.com/docs/webhooks/verify-webhooks-requests), [Resend received email API](https://resend.com/docs/api-reference/emails/retrieve-received-email), and official Meta WhatsApp Business Platform API/template documentation informed the adapters. No unofficial WhatsApp automation is used.

## Final measured results

All **372 / 372** integration checks pass: foundation 67, membership 95, community 112 and communication 98. The final suite includes positive regional sender/team scope checks and confirms that these permissions still cannot reveal private DMs.

- large_team_inbox: median 640.051 ms; p95 705.114 ms; 20 samples; target met.
- conversation_history: median 20.266 ms; p95 51.008 ms; 20 samples; target met.
- notification_center: median 10.732 ms; p95 11.959 ms; 20 samples; target met.
- campaign_list: median 4.63 ms; p95 5.492 ms; 20 samples; target met.
- recipient_snapshot_page: median 5.136 ms; p95 5.874 ms; 20 samples; target met.
- analytics: median 8.792 ms; p95 10.17 ms; 20 samples; target met.

Eight overlapping readers: 40 requests, 0 failures, p95 976.281 ms, 8.62 requests/second.
Mock worker: 200/200 accepted in 1618.33 ms, 123.58 jobs/second. This is mock acceptance, not delivery.
Signed callback burst: 30 events, 0 failures, 30 unique stored events, p95 120.556 ms.

An earlier repeated inbox run exceeded the one-second target (p95 1,239.108 ms). The permission query was simplified to resolve current authorized unit IDs once per request instead of expanding repeated hierarchy queries. The final full functional suite and isolated benchmark pass after that change. No privacy predicate was removed. Representative cursor EXPLAIN ANALYZE BUFFERS plans are included in the final JSON.
