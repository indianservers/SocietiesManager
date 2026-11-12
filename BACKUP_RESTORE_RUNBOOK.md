# Backup and restore runbook

Implemented utilities: `scripts/backup_restore.py`, `scripts/backup_loop.py`, `scripts/check_backup_health.py`, `Dockerfile.backup`, `ops/compose.backup.yaml`. Local execution evidence is in `tests/phase6-backup-restore.json`. This is a tested logical backup/isolated restore path, not a certified production RPO/RTO or PITR service.

## Secrets and consistency

Use PostgreSQL 17 client binaries matching the server. Install `tests/requirements.txt` in the verification environment or the dedicated backup image. Provide PGHOST, PGPORT, PGUSER, PGPASSWORD, PGDATABASE and optional PG_BIN through a protected environment. The backup/restore identity needs source read access, pg_dump privileges and permission to create/drop generated verification databases. Do not give these privileges to the API runtime.

Provide an independently generated 32-byte key as base64 in BACKUP_ENCRYPTION_KEY_FILE (preferred secret mount) or BACKUP_ENCRYPTION_KEY. Never store the key in the backup destination. Escrow keys and encrypted Data Protection PFX/private-key credentials separately; losing either may make protected provider/token data unrecoverable. Establish a rotation and old-key retention policy before production.

Quiesce application writes, or use immutable object storage and coordinate uploads/keys while copying. The database dump and expected table counts use the same exported repeatable-read snapshot. Files are copied and SHA-256 manifested separately; this does not make mutable external storage transactionally consistent with PostgreSQL.

## Create and verify

With protected environment set, run:

```text
python scripts/backup_restore.py backup --destination /backups --objects /app/storage --keys /app/keys --retention-days 7
python scripts/backup_restore.py verify --source /backups/<generated-file>.abk --destination /restore-verification
```

The backup contains a custom-format pg_dump, objects, key files and a manifest, encrypted with streaming AES-256-GCM. Retention removes only the tool's own eligible archive names after a new successful backup. Keep a second off-host immutable copy and monitor retention; local disk alone is insufficient.

Verification authenticates ciphertext before extraction; rejects path traversal/links; compares all manifest file hashes; restores into a generated `assemblia_restore_*` database; compares exact table counts from the source snapshot; checks posted journal balances; and drops only its own isolated database after verification. It does not overwrite running data or objects. JSON status intentionally omits secrets.

`python tests/backup_restore_integration.py` executes the local workflow against the disposable test database. It uses an ephemeral test encryption key and a test key-directory marker. It proves archive authentication, database restoration, file integrity and journal checks; it does not prove decryption of deployed Data Protection secrets or recovery from lost production credentials.

## Scheduled operation and monitoring

Opt into the dedicated backup service with the supplied compose overlay/profile after configuring its secrets. It mounts application objects and keys read-only and writes only backup/status/verification locations. The loop runs every 24 hours and verifies the first and every seventh backup by default. Inspect the loop's environment options before adopting the defaults. No background backup service was started by this task.

Use `scripts/check_backup_health.py` against the status directory to fail on stale success or a later failure. Connect this to real alerts and test disk-full, invalid-key, database-unavailable, corrupt-archive and restore-failure scenarios in staging. A green API health endpoint does not establish backup health.

## Incident recovery

1. Declare incident, stop writes and retain current database/objects/logs for investigation. Identify the last verified archive and its key version.
2. Restore into isolated database and storage, validate integrity and migration compatibility, then start an isolated application using the correct encrypted key ring and certificate. Verify representative private document, provider-secret and credential-token decryption.
3. Reconcile finance/control totals, memberships, approvals, queue idempotency and last successful external delivery/payment callbacks. Avoid replaying real messages/payments without reconciliation.
4. Obtain operator approval for cutover, route traffic, run authenticated acceptance checks and retain the original failed system until recovery is accepted.

`ops/postgresql-pitr.conf.example` is an inactive configuration scaffold. WAL archiving, retention, base backups and point-in-time recovery were not enabled or drilled. Define business RPO/RTO, measure an off-host recovery and test PITR before claiming those objectives.

## Latest local execution evidence

The 12 November drill restored all 130 public tables and matched all source snapshot counts, verified 3,232 object-file hashes and one test key-directory marker, and found zero unbalanced posted journals. Backup took 60.972 seconds; isolated restoration/verification took 98.725 seconds on this local machine during other fixture preparation. The archive was 118,333,487 encrypted bytes. These timings are not an off-host recovery SLA.

`python tests/backup_security_integration.py` additionally passed eight adversarial/health checks: exact authenticated round trip, modified-tag rejection, wrong-key rejection, parent traversal rejection, symbolic-link rejection, fresh success health, later failure health and stale success health. Unsafe archives are rejected before any database is created.
