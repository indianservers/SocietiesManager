# Final system architecture

Status: implemented local six-phase platform; production acceptance remains blocked. Reviewed 12 November 2026.

## One canonical organization and identity model

The React/TypeScript client uses a .NET 10 minimal API and PostgreSQL 17 through EF Core/Npgsql. Tenants are societies. Levels and Units represent configurable regions/chapters in one hierarchy. Persons identify people; Memberships connect people to chapters with effective dates, categories and terms. Links establish account affiliation; scoped Assignments reference GovernanceRoles and optionally an effective membership. There is no second membership or service hierarchy.

Access.Visible and Access.InWorkspace compute current scope. Every operation checks society affiliation and resource authority. A society role does not grant platform administration. Platform support views contain aggregate metadata; they do not impersonate society users. Composite tenant foreign keys prevent cross-society references. Database invariants complement application checks.

## Request and background processing

Browser → trusted TLS reverse proxy → cookie/CSRF/rate-limited API → scoped domain services → PostgreSQL and private object store. SignalR provides communication updates. Hosted workers use durable database queues with row locks and current-authority checks; they do not inherit unrestricted platform access. Stable keys prevent duplicate requests. Failed item reasons are retained and visible.

Phase 1 provides authentication, hierarchy and governance. Phase 2 adds membership, documents, lifecycle and leadership. Phase 3 adds moderated community posts and events, registration and verified attendance. Phase 4 adds conversations, recipients, templates, campaigns, automations and the consent-aware Outbox/provider dispatcher. Phase 5 adds a shared financial engine with legal entities, invoices, receipts, allocations and balanced journals. Phase 6 references these existing records rather than recreating them.

## Shared document engine

Cards and certificates use DesignSchema version 1: typed millimetre coordinates, whitelisted bindings and bounded elements. React Konva edits drafts. Published DesignVersion rows are immutable. Credential requests freeze a member/source/design/image snapshot and require a different current approver. The worker renders actual 300 DPI front/back PNGs and physical-size PDFs using SkiaSharp, embedded Vera fonts, QRCoder and PDFsharp. Issued artifacts and source snapshots remain fixed; expiry/revocation/current membership are checked independently by verification.

Opaque 256-bit verification tokens have indexed hashes and protected originals. Public verification discloses only validity, category, organization and expiry, with names opt-in. No contact data is embedded in the QR. Bulk selection persists up to 100,000 canonical memberships. Downloads are bounded jobs (up to 2,000 credentials per export) with ZIP, combined PDF or calibrated sheet output. Five-minute links require the original user and current bulk authority; exports remain eligible for 24 hours. A 100,000-card rendering campaign is not certified by the 1,000-card benchmark.

## Service, evidence and intelligence

ServiceProject has one lead Unit and a ProjectChapter join for participating canonical chapters. Managers/volunteers reference Memberships; optional committee, event, activity and finance budget links reference existing engines. Shift times generate decimal hours, then another authorized verifier approves them. ImpactMetric defines Sum/Max/Count; only independently verified ImpactResult contributes to totals. A tenant/metric/outcome key prevents counting one outcome again through another chapter. Corrections retain CommunityVersion history and return to review.

UnifiedIntelligence projects existing member, event, activity, service, communication and legal-entity finance records. Seventeen report families share scope checks and private export queues. Scheduled reports recheck the author and source permission. The assistant uses these same evidence projections and private document authorization. Guided mode is deterministic and read-only. Optional external mode uses the OpenAI Responses endpoint with explicit consent, a configured model, store=false and no tools. Provider credentials and live external processing were not exercised.

## Platform and operations

TenantBrand controls selected logos, color, login identity, chapter/region terminology, enabled modules, locale/timezone and preferred currency. Branding does not convert existing financial currencies. TenantDomain requires a DNS TXT challenge and binds verified Host routing to the matching tenant. TenantSubscription holds limits/feature entitlements and SaaS plan metadata, separate from member dues. Local storage checks actual per-tenant bytes under a process-local lock; multi-node deployment needs a distributed object store/quota implementation.

Production high-risk actions require enrolled MFA and a recently MFA-marked session. Identity provides TOTP and recovery codes. Persistent Data Protection keys can be encrypted with an operator-owned PFX. Trust forwarded headers only from explicitly listed proxy IPs. Backups use pg_dump exported snapshots, object/key manifests and AES-256-GCM; verification restores only into a generated isolated database.

## Operational boundaries

A local modular monolith is the verified deployment shape. Docker/Linux native rendering, multi-node operation, external delivery/payment/AI providers, live custom DNS/TLS, production MFA through HTTPS, real key escrow recovery, PITR, monitoring alerts and deployment/rollback need staging evidence. Repository CI and manual release workflows are supplied but were not run remotely. See PRODUCTION_READINESS_CHECKLIST.md for the release decision.
