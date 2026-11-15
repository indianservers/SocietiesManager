# Deployment runbook

Status: the user explicitly authorized and completed the AIMER Society deployment on 13 November 2026. The original container procedure remains available below. This host uses an isolated native .NET/Apache deployment on its additional disk; see the installation record in README.md. Live payment and SMTP activation are deferred at the owner's request.

## Current native installation

Application root: `/mnt/volume_sfo3_1785477034157/aimer-members`. Services: `aimer-members` and `aimer-members-db`. Apache site: `/etc/apache2/sites-available/aimer-members.conf`. Public URL: `https://members.aimersociety.com`. The API listens on 127.0.0.1:5188 and the dedicated PostgreSQL cluster on 127.0.0.1:5544. Existing sites and the database instance on 5432 must not be stopped or migrated for this application.

Secrets and runtime configuration are root-only files beneath the additional-volume `secrets/` directory. Never print them into deployment logs or commit them. The app runs as its own unprivileged system user, and only its private storage, key and temporary directories are writable. Native binaries and static assets are published locally, checksum-verified after upload, placed in an immutable release directory, and selected through the `current` symlink. Check `/health/ready` through both the private listener (with the correct Host header) and public HTTPS before accepting a release. Retain the previous release for application-only rollback; do not downgrade the database automatically.

For a new empty installation, run migrations and `--bootstrap-admin` with protected operator environment variables, then `--initialize-society-launch` with `SOCIETY_LAUNCH_CONFIG` and `SOCIETY_LAUNCH_TENANT`. This creates only the configured chapters, resources, offers and receiving entity. It does not import the optional demonstration members. The initializer and rerun behavior are covered by `tests/launch_initialization.py`.

The initial authenticated backup was restored to a separate scratch database and validated, then the scratch database was removed. The daily backup helper briefly quiesces only the membership API; other services remain running. Certificate renewal and backup timers keep all working files and logs on the additional disk. Back up the encryption key separately from encrypted snapshots. Before enabling real collections or sending email, configure the provider/SMTP settings and perform controlled end-to-end acceptance for those external services.

## Membership import and self-service cards — 14 November 2026

Release `20261114T024000Z` is deployed. The approved workbook import contains 253 individual memberships, 131 institutional memberships, and 378 institutional contacts. CMR Engineering College has one institutional membership with all six source contacts. Existing administrator accounts, settings, and templates were preserved. The production roster was empty before import, so no membership deletion or history-trigger bypass was needed.

All source member numbers, names, and approved expiry dates were reconciled against production with zero mismatches. Month-only validity ends on the last calendar day; the database stores the exclusive next midnight in Asia/Kolkata. INS1003, INS1004, INS1005, INS1007, INS1008, and INS1009 are valid from 1 August 2023 through 31 January 2027.

Members sign in using a verified source email to access imported memberships. Approved claim addresses are stored in immutable import history rather than editable profiles. Membership → My ID cards provides a PDF based on the live membership register, with an opaque protected QR token and current validity verification. This does not fabricate official Studio issuance approvals. Additional officially issued credentials retain the existing independent approval workflow. No account passwords were assigned and no member email campaign was sent.

The AES-GCM pre-import backup `society-20261114T022410Z-c45f21.abk` was restored and verified in an isolated database before import. The first application switch failed because the package was framework-dependent; automatic rollback restored the prior app. The corrected release includes the verified bundled runtime. Public readiness and authenticated administrator directory checks pass. Local membership integration checks pass 81/81, including imported ownership, PDF download, unverified email denial, cross-account denial, and tampered QR rejection.

## Release prerequisites

1. Close PRODUCTION_READINESS_CHECKLIST.md with named owners and attached evidence. Run `.github/workflows/verify.yml` on the exact reviewed commit; its remote status has not been verified here.
2. Provision patched Linux host, PostgreSQL 17, Docker/Compose, private registry access, trusted DNS/TLS proxy, restricted runtime role, separate migration identity and backup/restore identity. Restrict database/network access; never expose the database publicly.
3. Configure protected staging and production GitHub Environments with reviewer rules, registry permissions, SSH private key and pinned known-host key. Never disable SSH host verification. The manual release workflow checks a successful verification workflow on the same SHA before dispatch.
4. Store a root-owned mode-600 `/opt/assemblia/deployment.env`. Keep secrets out of the repository, process arguments, build layers and browser configuration. Use actual values for PUBLIC_URL, registry/image, runtime connection, migration connection and backup credentials. The host script reads the concrete names documented in its header; review `scripts/deploy-reviewed-release` before installation.

## Application configuration

`ConnectionStrings__Database` is the restricted runtime connection. Use migration credentials only for `dotnet Society.Api.dll --migrate`. Apply `scripts/runtime-role.sql` initially through psql with RUNTIME_DATABASE_PASSWORD in the protected environment; reapply `scripts/runtime-grants.sql` after migrations. The runtime must read EF migration history for readiness; it does not create/alter schema.

Set `ASPNETCORE_ENVIRONMENT=Production`, HTTPS PUBLIC_URL, `Storage__Root=/app/storage`, `DataProtection__KeyPath=/app/keys`, persistent object/key volumes and strict file ownership. Set `DataProtection__CertificatePath` and `DataProtection__CertificatePassword` through an operator-managed secret mount/environment to encrypt the key ring; compose's base configuration does not supply a certificate. Escrow certificate/private key and password separately from encrypted backups. Verify restart and decrypt recovery before release.

Use `ReverseProxy__KnownProxies__0` (and subsequent indices if needed) for explicit immediate proxy IPs. The supplied nginx example preserves Host and X-Forwarded-Proto, rejects unknown domains and requires valid TLS. Add only verified tenant domains and provision real certificates; no domain/TLS configuration was performed locally.

SMTP, communication, payment and assistant provider settings are optional until verified for launch. Missing credentials must remain disabled. Assistant external processing requires model/key configuration plus per-query consent. Preferred display currency is not a financial currency conversion.

## Reviewed release procedure

Install the reviewed `scripts/deploy-reviewed-release` as `/opt/assemblia/deploy-reviewed-release`, with executable ownership restricted to deployment operators. Keep compose files, Dockerfile.backup and backup scripts at the expected application path. Review `.github/workflows/release.yml` and configure its required secrets/variables before dispatch.

The manual workflow checks out and builds the reviewed tag, retains a hashed image artifact, pushes its image and invokes the host script. The host script locks deployment, records the previous image, pulls the selected release, builds the reviewed backup helper, quiesces API writes, requires a fresh encrypted backup, runs migrations with separate credentials, reapplies runtime grants, starts the application and polls readiness. It also verifies an anonymous private-account request returns 401. It does not certify authenticated workflows or alter database history for rollback.

After automated smoke checks, sign in through production HTTPS, enroll/verify MFA as a user, and exercise each launch role: scoped member search, independent card approval, download/QR/revocation, private bulk export, service review/hours verification, report export, balanced finance and consent-aware delivery. Verify another tenant cannot access those resources. Check logs and monitoring with no secrets or private document contents. Retain acceptance evidence and release SHA.

## Rollback and recovery

For an application-only failure, the host script can restore the previous application image. Database migrations are not automatically downgraded. Confirm backward compatibility before application rollback. If schema/data recovery is needed, stop writes, preserve failure evidence, restore into an isolated database/objects path, validate counts/hashes/journals/keys and have the operator perform an approved controlled cutover. Do not overwrite a live database with the verification helper. Follow BACKUP_RESTORE_RUNBOOK.md.

Health endpoints: `/health/live` proves process liveness; `/health/ready` checks database connectivity and pending migrations. Neither is a production acceptance certificate. Monitor failed/aging queues, outbox errors, disk usage, database saturation, backup health, HTTPS and error rates. Define alert destinations and test a failure; no live alert destination was configured in this task.

## Platform administrator overview — 14 November 2026

Release `20261114T030115Z-dashboard` adds the reference-matched purple administrator shell and overview. Platform administrators land at `/app` after login; member login behavior remains unchanged. The overview uses `/api/v1/platform/overview`, an authenticated platform-only aggregate endpoint. Detailed records and actions retain their existing permissions. Calendar-period boundaries are calculated in Asia/Kolkata and converted to UTC for PostgreSQL.

Desktop and 390px mobile checks passed against the live site, including real login redirect, create menu, participation chart, year/all-time date filters, and horizontal overflow checks. Local integration checks passed 84/84 and navigation checks 8/8. Membership records and society configuration were unchanged by this release. A pre-update encrypted backup is retained on the additional volume. The stable pre-dashboard fallback is `20261114T023716Z-theme`.

## Approved application design — 14 November 2026

Release `20261114T033925Z-approved-ui` implements the approved two-panel login mockup with a separate translucent brain/chip illustration, compact navigation, icon-led fields, remember-me control, four benefits and understated legal footer. Existing Google/Microsoft options remain visible when configured. Signup, verification, recovery and MFA use the same design without changing their destinations.

The reusable design system is bundled through `design-tokens.css`, `app-ui.css`, and `ui.tsx`; `main.tsx` no longer loads the old static `society-theme.css` entry. Compatibility selectors and the new component contract live together in the bundled application theme. Existing module classes, permissions and routes are preserved. Auth content can still use other organizations' configured branding. Shared controls, panels, tables, pagination, status badges, drawers, empty/error/loading states, typography and spacing apply across membership, chapters, community, finance, communications, documents, service, intelligence, settings, platform administration and member account views. The illustration is used only on authentication screens.

Remember me changes cookie persistence, retaining the established eight-hour cookie lifetime. Password and MFA sign-in share the optional setting; an unchecked control keeps a session-only cookie. Automated authentication checks verified persistent/session-only cookies, registration, verification, login/logout, password recovery/reset, authenticator and recovery-code sign-in. External provider consent screens and actual production email delivery were not triggered.

Verification: 509 rendered screen checks across 26 distinct roles, desktop/tablet/mobile widths, nested sections and chapter details; zero browser page errors or error alerts. Authentication 27/27, social-authentication integration 27/27, and navigation 8/8 passed. Live checks verified loaded branding/artwork, three signup categories, accessible policy dialogs, recovery form, real platform administrator login, current dashboard counts and mobile layout. All 384 imported membership names and expiry dates matched their approved source values with zero mismatches. No production member, role or financial data was changed by the redesign.


## Platform manual membership administration — 14 November 2026

Platform administrators can select any society membership workspace, create and edit membership holders, IDs, categories, chapters, status, contact details, and joining/validity dates. Expiry dates are inclusive in Asia/Kolkata. Changes require an administration reason, optimistic revision checks, and retained history; manual entitlements do not manufacture payments. Delete archives with typed-ID confirmation; restore preserves the original membership identity. Individual and organization identity boundaries remain enforced for linked records.

Platform membership access uses a role-only membership authorization helper. Existing strong MFA checks for platform configuration, pricing, roles, and financial operations remain in place. Individual card PDFs can be downloaded from profiles; the directory offers all or selected individual PDFs as a ZIP, excluding archived records. ZIP batches are limited to 2,000 records; larger societies can use chapter or selected-member batches. New manual individual records can optionally grant access by an explicitly approved verified personal email; editing a contact address does not transfer account ownership.

Verification: 157 API integration checks and eight desktop/mobile browser workflows passed, covering dates, categories, duplicate IDs, stale writes, authorization, archival/restore, approved email ownership, and individual PDF/ZIP downloads. Production source audits confirmed 253 individuals, 131 institutions, 378 contacts, six CMR contacts, and zero source-field mismatches.

Production release `20261114T083536Z-platform-members` passed seven live browser checks with zero page errors. All 253 individual PDF cards were downloaded and validated in a 68.3-second ZIP operation. Existing member records remained unchanged.


## Individual member login and self-service ID download — 14 November 2026

Verified individual accounts are matched to immutable approved import/manual registration email claims during password login and authenticated account loading. Members land at `/app/membership`, see their own existing membership, and download the current register ID PDF. Matching does not create another membership, change the membership ID, reset validity, or grant administrative privileges. Login/signup screens explain which email to use. Individual register IDs are downloadable independently of the issued-card download toggle; organizational documents and separately issued credentials retain their configured policies. Ownership checks protect the PDF and the administrator ZIP remains platform-only.

All 253 production individuals have approved claim-email entries. Amazon SES will be provided later by the owner. Production signup records the account and queues verification, but verification and password-reset emails cannot be delivered until email transport is configured. Do not bypass email confirmation or set shared/default passwords for imported members. Test verification links are retrieved only for isolated synthetic accounts; this tests the real token flow, not SMTP transport.

Validation: frontend production build and 160 API checks passed, including individual download with issued-card downloads disabled, organization policy preservation, ownership isolation, and prior administrative workflows. Ten end-to-end local browser checks cover signup, unverified sign-in denial, verification, login redirect, membership matching, PDF download, cross-member and bulk-download denial, mobile, unchanged validity, and logout. Equivalent live browser checks use temporary synthetic records which are removed afterward.


## Professional AIMER individual ID template — 14 November 2026

The published editable template `AIMER • Professional Individual ID` (8685a442-ed2f-49c1-9d04-fcaa8472a837) uses a CR80 front/back layout, the existing site organization logo, full society title, individual name, membership ID, inclusive expiry, mobile number, live verification QR, and a typeset D. Sai Satish / President, AIMER Society authorization block. No handwritten signature is fabricated. The logo is a private society-scoped Studio asset. A `member.phone` typed field is available to templates.

Published versions of this template drive individual register PDF and ZIP downloads. Archived/unpublished absence falls back to the existing starter; organizational documents retain their prior layout, and already-issued credential snapshots are unchanged. `--install-aimer-card` requires an explicit AIMER_CARD_TENANT and an AIMER-named society; repeat installation preserves saved edits and published versions. Local preview artifacts are in artifacts/aimer-professional-card.

163 API checks passed, including branded member PDF rendering, QR decoding, published version installation and repeat-install preservation. Seven production browser checks passed with zero page errors, including template gallery, president/logo content, PDF download, decoded QR resolving to live verification, and mobile layout. Source membership names and expiry values remained unchanged.


## Individual member personal-details CRUD — 14 November 2026

Individual members have an audited editor in their membership portal for contact, address, personal, education, emergency-contact and privacy details. `/membership/my/{id}/profile` supports reading, merging/updating and explicitly clearing optional personal fields. The endpoint limits access to the owning Individual membership, rejects administrative fields, and uses revision checks and society locking. Clearing optional details retains membership identity, entitlement, login access and history. Contact email updates do not change sign-in identity or approved email claims. Archived memberships cannot use this editor.

Registered name and membership ID are read-only for members. The older membership update route also rejects registered Individual name changes without member-administration permission. Membership category, status, chapter, joining/validity and expiry remain under administrator control. Existing organization-controller behavior is retained.

Validation: 174 API integration checks and 16 local browser checks passed, including profile upsert/read/delete, invalid fields/email rejection, stale revisions, ownership isolation, fixed registered identity, login/logout, ID PDF download and mobile layout. Live validation uses a temporary Individual account and two synthetic memberships; cleanup removes these exact fixtures and final source audits verify the approved imports. SES email delivery remains pending configuration; synthetic test verification uses the actual queued confirmation token.


## Private membership portals — 14 November 2026

Accounts without active staff permissions use a separate set of portal routes and navigation. Their membership kinds come from their authorized portfolio, not the signup preference. Individual, institutional and corporate labels select only memberships the verified account owns or represents; institutional/corporate-only accounts show organization documents and team/roster controls without an individual personal editor. Members see their own membership, affiliations/chapters, resources, account and invoices/receipts. Staff and Platform Admin workspaces remain separate. Direct attempts to open administrative directories, organization administration, templates, insights or platform settings redirect ordinary accounts to their private portal, without mounting those admin modules. Backend authorization continues to enforce ownership independently.

The self-finance route renders only the member ledger/invoice interface, without accounting setup or staff navigation. Login/account loading also establishes the society link for verified roster affiliations via Entitled(), without assigning an administrative role or organization ownership. Existing approved workbook claim emails cover all 253 individuals and 125 of 131 institutions. INS1001, INS1002, INS1003, INS1004, INS1005 and INS1007 have no registered contact email; authorized emails are requested from the owner. SES delivery remains pending.

Verification: 176 API checks and 45 local browser checks across Individual, Institutional and Corporate accounts passed. Browser checks cover approved organization claims, login, private navigation, direct admin-route redirects, own-document downloads, cross-member access denial, personal-profile separation, own finance, mobile and logout. Production verification uses temporary exact-ID fixtures and then removes them.


## Member login enhancements — 15 November 2026

Release `20261115T050034Z-member-login` is live at `https://members.aimersociety.com`, built from Git commit `c9c4f948b68428afd01ff58685c6d8899a0fc9ff`. The uploaded self-contained Linux package and every manifest file were checksum-verified before activation. Existing Firebase configuration, private storage, encryption keys and service configuration were retained.

Encrypted pre-release backup: `society-20261115T053900Z-11e6f4.abk`. The additive `MemberLoginEnhancements` and `MemberPasskeyStore` migrations were applied using the migration identity, with explicit runtime grants on the four new tables. The membership register fingerprint remained unchanged across migration. No member identities, numbers, validity dates, payment records or administrator assignments were edited by deployment.

An initial readiness failure restored the prior application automatically. The subsequent switch passed private-listener and public HTTPS readiness. The retained application rollback release is `20261114T164441Z-private-portals`; database downgrades are not automatic.

Twenty-six live acceptance checks passed with zero uncaught browser errors. Checks cover the actual release JavaScript checksum, society branding, anonymous access denial, existing Google/Microsoft initialization, Hindi/Telugu labels, four viewport widths, verification resend, magic-link request UI, existing owner authentication, sign-in methods, device sessions, login history, session expiry, onboarding checklist, account security rendering and logout. Public and private readiness report ready, and both dedicated services are active. SMTP remains intentionally unconfigured; these checks did not send a membership email campaign, register a physical passkey, or submit external-provider consent.


## Member input and ID-download hardening — 15 November 2026

Release `20261115T060714Z-member-security`, from code commit `88b4cd840aba3eb1bcefdebb2dc4729ced8b964b`, is live. Ordinary members retain login/account security, their own ID-card downloads and personal contact editing. Registered name, membership number, status, validity, category, roles and other administrative metadata remain protected. The legacy profile route now preserves non-contact metadata instead of accepting arbitrary replacement keys.

The shared request validator enforces constructor-parameter validation attributes on request records, in addition to existing property validation. Authentication/account-security bodies are capped at 64 KiB; member-profile bodies are capped at 512 KiB, with narrower field limits. Missing, expired or mismatched passkey ceremonies return a client error. Malformed JSON responses omit exception details. API responses use no-store and unknown API paths return JSON 404 responses. ID-card and issued-credential downloads are limited to ten requests per authenticated account per minute; account limits are independent.

Verification passed: 103 adversarial API/input/ownership checks in `member_security_test`, 176 membership-experience checks, 52 login/passkey/browser checks, and 31 live production checks with no uncaught browser errors. The live checks confirmed input rejection, no-store, unknown-route handling, existing owner authentication, account-security APIs and the unchanged frontend. Production member records were not edited for verification.

Encrypted pre-update backup: `society-20261115T060820Z-7630c2.abk`. The patch archive and changed-file manifest were checksum-verified, with unchanged files verified against the previous release manifest. Existing frontend assets, dependencies, bundled runtime, protected configuration and keys were retained. No database migration was required. The rollback release is `20261115T050034Z-member-login`; both dedicated services are active and HTTPS readiness passes.
