# Security audit report

Review date: 12 November 2026. This records local source review, real PostgreSQL/HTTP checks and package scans; it is not an independent penetration test or production certification.

## Verified controls

The combined suite exercises cross-society and cross-chapter denials, scoped organization visibility, expired/revoked authority, CSRF, file authorization, immutable snapshots, independent approvals and financial balance/source constraints across all phases. Phase 6 adds executable/unknown design-field rejection, approved placeholder restrictions, opaque QR privacy and actual QR decode, user-bound bulk links, report ownership, independently verified hours/impact, impact deduplication, assistant permissions/prompt-injection attempts, tenant branding/host isolation, plan feature denials and TOTP enrollment/sign-in using a newly created fixture account. A separate Production-mode HTTPS fixture verifies password-only high-risk denial, recent-MFA high-risk success and recovery-code replay denial. Eleven shipped runtime-grant checks pass under an ephemeral restricted role. Eight backup security/health checks pass, including wrong key, modified tag, traversal/link rejection and stale/later-failure health.

npm production dependency audit reported zero vulnerabilities. NuGet vulnerable-package scan including transitives reported no vulnerable packages from configured feeds. Scan results are point-in-time; repeat in CI and before release. Backend builds without warnings; frontend lint passes with existing hook/refresh warnings, which are not presented as a clean accessibility or security audit.

Typed template JSON disallows unknown properties, scripts, arbitrary URLs and sensitive bindings. Images are sanitized bounded PNG uploads. Server rendering reads authorized private object IDs rather than fetching URLs. Required unsupported glyphs or overflowing required text fail issuance instead of generating an invalid official credential. A finite embedded font set does not provide arbitrary script shaping.

## Security changes

- Production high-risk access requires enrolled MFA and a recently MFA-authenticated session. Password alone cannot finish sign-in for an MFA account; recovery codes are returned once.
- Private assets/exports use authenticated handlers. Bulk links expire after five minutes, remain bound to requester and require current permission; completed export eligibility is 24 hours.
- Public credential checks rate-limit by trusted client IP and return minimal data. Successful token lookups produce a non-contact verification audit event; raw tokens, names, IP and contact fields are not added to that event.
- External assistant access is fixed to an HTTPS provider endpoint, read-only and explicitly consented. It has no database tools or write actions; audit retains intent/mode/token usage rather than question text.
- DNS ownership checks use a fixed DNS resolver endpoint; arbitrary outbound URL probing is not supported. Verified hosts cannot cross-route tenant APIs.
- Optional key-ring persistence supports PFX encryption. Forwarded headers trust only configured immediate proxies. Runtime database grant scripts separate DML from migrations and revoke selected immutable/audit mutations.
- Membership/chapter plan quotas are enforced by statement-level PostgreSQL transition-table triggers with tenant serialization. Local object storage enforces actual byte limits under a per-process tenant lock.

## Findings resolved locally

Template-gallery nested query projection was materialized after browser testing exposed a runtime failure. Shared society templates now expose the latest published content to scoped readers rather than unpublished edits. Project review uses tracked records so approval persists. Long required names now auto-fit or fail explicitly. Bulk download authorization is rechecked at job/file access. Optional forwarded-header configuration was corrected and rebuilt.

## Open release findings and limits

BLOCKED: deployed trusted-proxy HTTPS/MFA acceptance and a full API run under its production runtime identity (local Production-mode HTTPS and restricted SQL grants were tested), actual encrypted key-ring recovery, independent penetration test, Linux native dependency/container scan, host security patching, production secrets/rotation, real custom-domain DNS/TLS, external provider signature/sandbox acceptance, off-host immutable backup retention and PITR drill. These are unresolved release evidence, not claimed passes.

Local storage quotas are single-process; do not claim distributed race protection. Raster rendering is limited to two concurrent images and 24 million pixels per image; 2,000-file exports may still consume substantial aggregate memory. Stress-test concurrency and add infrastructure memory/resource limits before deployment. Public verification audit retention and abuse alerts require an operator policy. Unsupported scripts and font licensing/branding assets require launch review. Legacy Phase 5 finance limitations remain in its completion report.

No production deployment, real payment, message delivery or external AI transmission was performed. No claim of zero security risk is made.
