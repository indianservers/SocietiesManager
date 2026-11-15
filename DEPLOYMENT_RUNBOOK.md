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


## 2027 workspace experience and homepage sign-in — 15 November 2026

Release `20261115T063547Z-experience-2027` is live at `https://members.aimersociety.com/`. The homepage directly renders sign-in; `/login` redirects to `/` while preserving query parameters and fragments. Signed-in visitors resume their role-appropriate workspace. Enhancements 1–44 are mapped in `UX_2027_ENHANCEMENTS.md`.

This release builds on `20261115T060714Z-member-security`, preserving its input validation, ownership boundaries and private PDF download protections. The reviewed source snapshot includes working-tree changes on base commit `0d2f22b6f03ef2525e5cbe34540be5b62ef65a36`; source SHA-256 is `d0cf4b12a9d940214584fcb576b6e1795c33044699cea02792008c2b1605816b`. Every application/runtime/static file was checksum-verified before activation. Existing runtime files, secrets, provider settings, private storage and encryption keys were retained. No schema migration was required.

Encrypted pre-release backup `society-20261115T063612Z-3272be.abk` passed an isolated restore and table-count verification. Both services and public readiness passed after activation; the membership register fingerprint remained unchanged. The previous security release remains the application rollback target.

The real attention API applies existing permission and workspace scopes to application, expiry and invoice counts. Expiry links use matching `Active`, `Expired` and `Expiring` register filters; review queries now honor the selected workspace. Local validation covers navigation, authentication/MFA/passkeys, individual/institutional/corporate portals, private downloads, profile CRUD, record search and opt-in draft restoration.

Production acceptance passed the homepage/legacy route, four viewport widths, language selection, signup/recovery screens, authenticated platform shell, favorites, density, command palette, notification center, mobile navigation, reduced motion and exact JavaScript/CSS checksums. Production attention counts matched their filtered membership registers. Authentication navigation regression checks verified that filled login/signup forms and recovery/email-link navigation do not show unsaved-work warnings, including stale auth-form dirty markers. Genuine unsaved workspace edits still warn, and Cancel preserves their input. No uncaught browser errors were observed. SMTP delivery, physical biometric hardware and external-provider consent were not exercised.

## Compact shared page layout — 15 November 2026

Frontend release `20261115T074858Z-compact-layout` reduces public authentication spacing and shared internal-page headers, cards, navigation, tables and portal padding. The workspace topbar is 54px. Short phone screens use a compact social sign-in row while retaining 44px provider buttons. Long content remains scrollable. Mobile footer clearance accounts for the fixed navigation and safe area.

Production deployment copied the previous release, updated only static frontend files, verified SHA-256 checksums for the complete resulting release, and atomically switched the current symlink. Backend binaries and configuration were retained; no database migration was required. Archive SHA-256: `34569ce03a8e5b7349d4dd471bd0780646ffe2e5ff9fbd0b9e36793c026310fe`. Rollback release: `20261115T063547Z-experience-2027`. Both services are active and HTTPS readiness passes. Frontend build and eight navigation tests passed; browser evidence is in `tests/compact-layout-live-results.json` and `tests/compact-login-*.png`.

## Preserve login forms on tab return — 15 November 2026

Release `20261115T075810Z-login-focus` treats an anonymous `/users/me` response (401) as a successful empty session result. Previously, an errored session query returned to pending on a focus refetch and replaced the root authentication form with the session-check placeholder, losing entered fields. Background session checks remain enabled, including discovery of a session established in another tab. Other errors retain their normal handling.

The production frontend was built in an isolated source copy excluding the unrelated, unfinished welcome-email feature. Backend files and database were retained. Complete release checksums and HTTPS readiness passed; rollback is `20261115T074858Z-compact-layout`. Patch archive SHA-256: `c1ff105f972643f16eeec471a11d95ba2d4efc1c2176e91ce96f8fd7d0b4f726`. The browser regression reproduces cleared input with the previous frontend and verifies form identity and email/password retention with the fix. Live checks also verify normal sign-in. Evidence: `tests/login-focus-results.json`, `tests/login-focus-live-results.json`.

## Preserve drafts across internal-page tab switches — 15 November 2026

Release `20261115T082119Z-internal-focus` disables focus- and reconnect-triggered page-data refetches throughout the shared query client. An anonymous sign-in screen can still discover a session opened in another tab. Authenticated workspaces retain their mounted page and drafts; existing SessionRecovery and server authorization continue to enforce access.

Loaded DataState content and the staff member editor remain mounted after transient background errors. A loaded form shows a retry notice rather than being replaced by an error screen. Initial failures and explicit 401/403 access denials still block content. Existing polling, explicit retries and save-triggered invalidations remain available.

The final frontend was built from an isolated source copy excluding an unrelated unfinished individual-portal module. The release retained the existing backend and configuration, verified every release-file checksum and passed public readiness. Archive SHA-256: `7cfd0bcd58549753e07360e12fda8626f7408aa55bb8776d982a4c8df7ebd969`. Rollback release: `20261115T081419Z-internal-focus`; the latest welcome-email backend was preserved. Thirteen rendering/navigation checks passed, including denial handling. Browser evidence is in `tests/internal-focus-live-results.json` and `tests/login-focus-results.json`. Browser edits are discarded without saving production records.

### Authentication recovery and linked sign-in methods — 15 November 2026

Release `20261115T101617Z-unified-auth-security` adds the `EmailOtpAccountSecurity` migration. The new challenge table was created through the existing PostgreSQL administrator connection; the runtime role retains DML permissions only, copied from the existing login-link table. Runtime schema-creation permissions were not expanded.

- Password reset emails use a URL fragment for the recovery token, a 30-minute token lifetime, per-address request limits, and generic eligibility responses. Successful resets revoke active sessions, email OTPs and magic links. Tokens cannot be reused after a successful reset.
- Email login OTPs expire after five minutes, are consumed once, and are invalidated after five failed attempts. Requests have a one-minute cooldown and a six-per-hour limit. Stored challenge codes are protected with ASP.NET Data Protection. Challenges older than one day are purged on the request path. Existing MFA remains mandatory.
- Google, Microsoft and Firebase-verified mobile identities connect to an existing application account after recent authentication (including MFA when enabled). Explicit linking supports a different verified provider email. Public provider sign-in no longer silently registers an unknown identity; creation requires the signup flow. Already-connected identities cannot be reassigned across accounts by the linking endpoint.
- Mobile SMS support is enabled by `Authentication__Firebase__PhoneEnabled=true`; upstream Firebase Phone Authentication, SMS billing, authorized domains and regional SMS settings must be configured. Production reCAPTCHA is required and is never disabled for tests. Profile phone edits are separate from the verified sign-in number.
- Reauthentication for open work retains the expected-account check. A different account requires an explicit action that discards the open workspace and reloads the sign-in page. Public sign-in clears old account-scoped query caches.
- Persistent Data Protection uses the stable application name `AIMER.MembershipPortal` (override through `DataProtection:ApplicationName` only when intentionally changing the cryptographic boundary). This avoids release-folder-dependent session invalidation; the initial change requires a fresh sign-in.

Validation: isolated PostgreSQL/Identity/browser tests cover OTP expiry, replay and concurrent consumption, guessing limits, password reset and revocation, provider and verified-mobile account identity, MFA, and explicit workspace switching. Nineteen upstream-token verifier checks cover issuer, audience, revocation, malformed tokens and verified phone matching. A two-release-content-root test confirms valid session continuity. Production reset and OTP messages to the user-authorized test recipient were accepted by SMTP without errors. The live SMS request to the user-authorized number succeeded after the user completed Google's CAPTCHA. The SMS code was not consumed by the test. No CAPTCHA or ownership verification was bypassed.

### Persistent sessions and public membership payments — 15 November 2026
- Live release: `20261115T104706Z-persistent-login-membership-payments`.
- All member sign-in methods issue persistent cookies with a renewable 365-day lifetime. Valid older short cookies upgrade on authenticated requests; shared Data Protection keys survive release changes. Logout, revoked sessions, password resets and security-stamp checks remain enforced.
- Official Individual payment URL: https://rzp.io/l/AIMERSIndividualFee ; Institutional payment URL: https://rzp.io/l/AIMERSEduMembershipFee . Both support new membership / renewal entry points on public portal login/signup, main website Home and Register; authenticated membership pages show the matching kind and renewal buttons open that payment URL.
- External payment navigation does not create a membership, mark invoices paid or update validity automatically. Payment confirmation and AIMER review remain required.
- Main WordPress update is idempotent in `ops/update-public-membership-links.php`, including Elementor content/cache. Initial page backup: remote `backups/wordpress-membership-links-20261115-104207.json`; follow-up backup: `backups/wordpress-membership-links-20261115-104810.json` under the app base.
- Validation: frontend build and Linux backend publish passed; `tests/persistent_session.py` passed persistent lifetime, release restart and logout; public Home/Register browser checks passed desktop/mobile; live existing Individual/Institutional accounts confirmed correct links, persistent cookie/browser restart, unchanged portfolio and logout.

### Individual ID front artwork — 15 November 2026
- `AimerCyanCard` publishes a separate versioned front-only Individual template; the previously published professional template and its historical credentials remain preserved.
- Uses the supplied luminous cyan brain and brain-on-chip PNGs unchanged as embedded studio assets. Member name, number, mobile and recorded validity are bound at render time; long names fit their available print box. Physical size is 85.60 × 53.98 mm, 300 DPI.
- Individual portal preview and PDF download use the same server-side template. `/id-card/front.png` requires account ownership or authorized platform membership administration; it refuses institutional memberships. General Studio card issuance also requires an Individual membership.
- Institutional downloads produce a membership certificate, not an Individual ID card. No back design has been added to the new Individual template; downloadable examples contain one front page.
- Live existing-account checks: `tests/cyan_cards_live.py` downloads two Individual fronts as PNG and one-page print-size PDF, verifies portal previews and unchanged membership portfolios, and checks institutional front refusal/certificate format. Example outputs: `output/id-cards/individual-1-front.*` and `individual-2-front.*`.
- Fractional print-space text uses subpixel positioning and linear metrics to avoid Linux renderer character-spacing artifacts.
- Final live release: `20261115T132103Z-individual-cyan-front`; both live account checks and institutional certificate checks passed again after the Linux text metrics correction. Downloaded single-page PDFs were rasterized and visually checked.
- Background refinement: replaced all generated wave/dot geometry with the user's exact transparent PNG (`aimer-cyan-lines.png`), byte-for-byte preserved. The image is the first front layer at 65% opacity; all text and foreground artwork render afterward. Published template version 2 preserves version 1 for history.
- Supplied-background final release: `20261115T133031Z-individual-cyan-front`; template version 2 published successfully with prior version preserved. The generated wave and dot elements are absent, and all text appears above the supplied PNG background.

### Individual ID back — 15 November 2026
- Template version 3 adds the supplied membership-card back layout and exact community, registration and contact wording. The same supplied PNG glow lines sit behind text at low opacity; the back includes the actual member name.
- Front design data is unchanged from approved version 2. Prior published template versions remain preserved.
- Individual PDF downloads now contain front and back pages at identical CR80 dimensions. Authorized `/id-card/back.png` previews share the same ownership checks and Individual-only restriction; the member ID cards tab displays both sides.
- Institutional certificate design remains pending the user's next reference; this update does not revise institutional certificates.
- `tests/cyan_cards_duplex_live.py` checks two existing members, byte-identical front renders, both preview images, two-page PDFs, unchanged portfolios, and institutional restriction.
- Final two-sided card release: `20261115T134305Z-individual-cyan-front`. Version 3 published. Two real existing-account downloads passed unchanged-front byte comparisons, two-page PDF dimensions, both portal previews and membership preservation. Back pages rasterized and visually verified.

### Institutional Certificate of Membership — 15 November 2026
- Separate Institutional-only published template uses the user's exact `Empty letter head.png` and terms from `BVC.pdf`. The president signature is extracted from that supplied certificate. A4 portrait, one page, 300 DPI.
- Institution registered name, membership number, recorded effective-from / expiry dates (Asia/Kolkata), place and current membership status bind at download time; no membership or validity changes are made. Missing place is explicitly shown as not provided.
- `/societies/{tenant}/membership/members/{id}/membership-certificate` checks ownership / authorized platform access and strictly requires Institutional category. Institutional portal downloads use this route; old document route remains compatible. Individual ID designs and two-sided downloads remain unchanged.
- Studio issuance of this specific template also rejects non-Institutional memberships. Published template and source assets are stored through the existing Studio/object-store system, preserving prior designs.
- Live check: `tests/institution_certificates_live.py`; output certificates and record metadata in `output/certificates/`.
- Final live release: `20261115T135517Z-institutional-letterhead-certificate`. Published template version 1. Live Kalasalingam (AIMERS-INS100) and BVC (AIMERS-INS117) downloads passed A4 page size, record preservation and institutional portal link checks; Individual certificate requests and cross-account access were rejected. Approved Individual front bytes and two-sided ID downloads remained unchanged. Both downloaded certificate PDFs were rasterized and visually verified against their record metadata.
