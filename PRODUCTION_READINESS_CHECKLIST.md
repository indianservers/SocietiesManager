# Production readiness checklist

Decision: **BLOCKED — not production-ready.** Local implementation and synthetic verification do not authorize deployment. No production deployment was performed.

## Locally verified

- [x] Existing six-phase source builds; real PostgreSQL migration model is current.
- [x] Phase 1–5 HTTP/PostgreSQL regression suite rerun through the Phase 6 entry point.
- [x] Credential schema, independent review, durable generation, historical snapshots, QR/privacy and holder/bulk authorization exercised.
- [x] Certificates, joint service projects, independent hours/impact verification and outcome deduplication exercised.
- [x] Unified report source permissions, private scheduled jobs, guided assistant isolation, branding and synthetic verified-host routing exercised.
- [x] TOTP enrollment and MFA sign-in exercised on a synthetic account; Production-mode HTTPS tests prove privileged password-only denial, recent-MFA success and one-time recovery-code behavior.
- [x] Authenticated encrypted backup and isolated restore/count/hash/journal verification executed locally.
- [x] Actual physical PDF dimensions and QR decode checked; sample documents retained.
- [x] Deployment, backup, recovery and release workflows documented in repository.

## Must close before release

- [ ] Run protected CI on the exact reviewed commit; retain build/test/SBOM/container scan evidence.
- [ ] Run staging Docker/Linux rendering and recovery with deployed image, real persistent key ring and restricted runtime role.
- [ ] Verify recent-MFA enforcement on privileged production paths through trusted HTTPS proxy, recovery-code handling and emergency access.
- [ ] Independent security assessment and closure of critical/high findings; define log retention, alerting, incident response and owner contacts.
- [ ] Validate all launch providers (mail/WhatsApp/SMS/payments, optionally AI) with real sandbox signatures, delivery/status/refund callbacks, credentials and retry/failure handling. Unused integrations remain disabled.
- [ ] Prove real DNS ownership and TLS routing per custom tenant domain; configure proxy allowlist and trusted IPs.
- [ ] Approve representative load/concurrency/error-rate objectives on launch hardware; local warm sample targets are not production SLAs. Certify a 100,000-document run if launch requires it.
- [ ] Printer proof for A4/A3/custom stock, actual-size duplex calibration, bleed/crop tolerances, readability and all launch languages. Embedded Vera does not certify complex script shaping.
- [ ] Full desktop/mobile/manual keyboard/screen-reader acceptance with actual launch roles; drag/resize/group interactions and low-bandwidth behavior need broader coverage.
- [ ] Independently reconcile launch opening balances and all legal-entity accounts; decide unresolved finance capabilities recorded in PHASE5_FINANCE_COMPLETION_REPORT.md.
- [ ] Off-host immutable encrypted retention, key escrow/rotation, real key-decryption recovery, monitored failure alerts and measured approved RPO/RTO.
- [ ] Configure and drill PITR/WAL recovery if required. Supplied PITR example is not active.
- [ ] Staging migration/rollback rehearsal and authenticated smoke acceptance; app image rollback must not downgrade a changed database.
- [ ] Obtain explicit deployment approval for the identified production host and reviewed release.

## Feature boundaries requiring launch decisions

Bulk exports are paged up to 2,000 credentials. Interactive directories are bounded/paged. Report export caps are 200,000 rows, with smaller PDF limits. Storage is private local disk with process-local quota enforcement; distributed object storage is not implemented. Export expiry controls access; automated physical artifact/orphan deletion and its retention policy are not yet implemented. White-label terminology is applied to selected navigation/profile surfaces, not every legacy sentence. Project evidence upload currently accepts PNG, not arbitrary office/PDF attachments. Full production acceptance is intentionally not checked off.

## Measured performance gate — failed locally

At 300,000 invoices, finance dashboard p95 was 2,451.668 ms and ageing p95 was 2,341.538 ms, exceeding the retained 2,000 ms local objective. All six Phase 6 read targets met their 5,000 ms objective with zero failures. Actual jobs completed 1,000 cards, 100 certificates and a 1,000-card ZIP with zero record failures. The completed benchmark exits nonzero for the two read-target misses; do not treat it as an entirely passing performance run. See PERFORMANCE_BENCHMARK_REPORT.md for exact method, hardware and scope.
