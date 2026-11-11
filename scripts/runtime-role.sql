-- Run with psql as the migration owner after applying migrations.
-- Pass -v runtime_password=<secret> through a secure secret mechanism.
CREATE ROLE assemblia_runtime LOGIN PASSWORD :'runtime_password' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
GRANT CONNECT ON DATABASE assemblia TO assemblia_runtime;
GRANT USAGE ON SCHEMA public TO assemblia_runtime;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO assemblia_runtime;
REVOKE UPDATE, DELETE ON "Audits" FROM assemblia_runtime;
REVOKE DELETE ON "Units" FROM assemblia_runtime;
REVOKE ALL ON "__EFMigrationsHistory" FROM assemblia_runtime;
