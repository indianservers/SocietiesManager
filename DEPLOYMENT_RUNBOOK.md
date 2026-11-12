# Deployment runbook

Status: supplied and locally syntax-reviewed; no staging/production deployment executed. Docker is unavailable on the current Windows verification machine. Deployment requires explicit approval for the reviewed release and host.

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
