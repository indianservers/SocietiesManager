# Six-Phase Development Roadmap

## Phase 1 — Society, Region & Chapter Foundation

Implemented locally: verified identity and sessions; society applications/review/activation; configurable hierarchy; region/chapter lifecycle; scoped dated governance roles; multi-society workspaces; invitations; settings/branding; audit history; OpenAPI; PostgreSQL migrations; real database tests and measured capacity scenario.

The completion report records the verified results and remaining rollout work. Production release remains gated on platform delegation/support design, operational email/storage setup, runtime database privileges, container/TLS verification, broader accessibility/browser testing and concurrent load/security validation. Do not assume a production SLA from local measurements. No production deployment is authorized.

## Phase 2 — Membership & Leadership

Implemented and locally verified: canonical people with multiple permitted affiliations; staged membership applications; recipient-bound invitations and claims; private member CRM and policy-filtered directories; configurable categories and chapter policies; dated officer appointments and handovers using existing roles; committees; reviewed and scheduled transfers; durable CSV/XLSX imports; private evidence documents; saved views, exports and database-backed intelligence. All 67 foundation and 95 membership integration checks pass. Fresh PostgreSQL migrations and 100,000-membership SQL/API benchmarks pass documented local targets. See PHASE2_SOCIETY_MEMBERSHIP_COMPLETION_REPORT.md for measured results, boundaries and operational limits. Production deployment remains pending the rollout work described in both completion reports.

## Phase 3 — Activities & Events

Implemented and locally verified: canonical activity posts and audiences; member/official publishing; configurable approvals and scheduled publication; reactions, comments, replies, mentions, bookmarks, blocking and moderation; private media; chapter profiles and recognition; bounded calendars; event creation/review; recipient-bound invitations using the existing outbox and membership communication consent; separate RSVP, registration and actual attendance; transactional capacity/waitlists; opaque revocable QR passes with printable PDFs; concurrent authorized check-in; attendance corrections and scoped intelligence. All 67 foundation, 95 membership and 112 community checks pass. Large-data API measurements and a rendered/decoded real PDF pass are recorded in PHASE3_CHAPTER_ACTIVITIES_COMPLETION_REPORT.md. Local development API now uses port 5092 because another application occupies 5088. Communication campaigns, finance and ID cards remain future work.

## Phase 4 — Communication

Next phase: add announcements, scoped audiences, email/SMS/provider adapters, channel preferences, consent evidence, unsubscribe controls, delivery status and communication history. Reuse community/event audiences and the existing outbox; correlate invitations with transport attempts and provider receipts. Evolve the outbox into an idempotent, claimed/retryable dispatcher appropriate to deployment scale. Preserve membership communication opt-outs, recipient binding and current organizational authority. Prevent audience expansion beyond sender authority and validate template/content handling. Do not interpret Phase 3 Queued status as delivery.

## Phase 5 — Dues & Finance

Add configurable dues, invoices, receipts, payment-provider integration, reconciliation, refunds, treasury views and financial reporting. Use precise monetary types and currencies, immutable posting/audit trails, idempotent webhooks and explicit approval authority. Obtain provider/compliance/operational requirements before implementing production payment flows.

## Phase 6 — ID Cards, Reports & Release Hardening

Add member identity cards, verification/revocation, templates and exports; permission-filtered report building; operational dashboards; final scale/accessibility/security regression; backup/restore and disaster-recovery validation; observability and release runbooks. Reassess hierarchy/index/caching choices using measured production-shaped workloads. Release only after explicit authorization and acceptance of the deployment plan.

## Rules across all phases

Keep PostgreSQL authoritative and preserve one society aggregate and one hierarchy engine. Extend the modular monolith, use existing roles/scopes/components, keep personal/financial data behind server policies, and never replace real metrics with display constants. Each phase needs migrations, upgrade checks, automated tests, measured performance, security findings and an updated completion report. Future modules stay unavailable until they work end to end.
