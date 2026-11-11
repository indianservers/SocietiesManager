namespace Society.Api;

public static class GuardSql
{
    public const string Up = """
CREATE FUNCTION guard_unit_hierarchy() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE parents uuid[]; root_allowed boolean; parent_level uuid;
BEGIN
 PERFORM pg_advisory_xact_lock(hashtext(NEW."TenantId"::text));
 SELECT "AllowedParents", "AllowRoot" INTO parents,root_allowed FROM "Levels" WHERE "TenantId"=NEW."TenantId" AND "Id"=NEW."LevelId";
 IF NEW."ParentId" IS NULL AND NOT root_allowed THEN RAISE EXCEPTION 'Parent required'; END IF;
 IF NEW."ParentId" IS NOT NULL THEN
  SELECT "LevelId" INTO parent_level FROM "Units" WHERE "TenantId"=NEW."TenantId" AND "Id"=NEW."ParentId";
  IF parent_level IS NULL OR NOT(parent_level=ANY(parents)) THEN RAISE EXCEPTION 'Parent level is not permitted'; END IF;
  IF EXISTS(WITH RECURSIVE ancestors AS (SELECT "Id","ParentId" FROM "Units" WHERE "TenantId"=NEW."TenantId" AND "Id"=NEW."ParentId" UNION SELECT u."Id",u."ParentId" FROM "Units" u JOIN ancestors a ON u."Id"=a."ParentId" WHERE u."TenantId"=NEW."TenantId") SELECT 1 FROM ancestors WHERE "Id"=NEW."Id") THEN RAISE EXCEPTION 'Hierarchy cycle'; END IF;
 END IF;
 IF TG_OP='UPDATE' AND NEW."TenantId"<>OLD."TenantId" THEN RAISE EXCEPTION 'Tenant is immutable'; END IF;
 IF TG_OP='UPDATE' AND NEW."LevelId"<>OLD."LevelId" AND EXISTS(SELECT 1 FROM "Units" u JOIN "Levels" l ON l."Id"=u."LevelId" AND l."TenantId"=u."TenantId" WHERE u."ParentId"=NEW."Id" AND NOT(NEW."LevelId"=ANY(l."AllowedParents"))) THEN RAISE EXCEPTION 'Children do not permit this parent level'; END IF;
 RETURN NEW;
END $$;
CREATE TRIGGER unit_integrity BEFORE INSERT OR UPDATE ON "Units" FOR EACH ROW EXECUTE FUNCTION guard_unit_hierarchy();
CREATE FUNCTION prevent_history_mutation() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Historical records are append only'; END $$;
CREATE TRIGGER audit_immutable BEFORE UPDATE OR DELETE ON "Audits" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation();
CREATE TRIGGER unit_no_delete BEFORE DELETE ON "Units" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation();
ALTER TABLE "Assignments" ADD CONSTRAINT assignment_dates CHECK ("EndsAt" IS NULL OR "EndsAt">"StartsAt");
ALTER TABLE "Units" ADD CONSTRAINT unit_name_code CHECK(length(trim("Name")) BETWEEN 1 AND 160 AND "Code" ~ '^[A-Z0-9_-]{2,40}$');
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX unit_name_search ON "Units" USING gin (lower("Name") gin_trgm_ops);
CREATE INDEX unit_code_search ON "Units" USING gin (lower("Code") gin_trgm_ops);
""";
    public const string Down = """
DROP INDEX IF EXISTS unit_name_search; DROP INDEX IF EXISTS unit_code_search;
ALTER TABLE "Assignments" DROP CONSTRAINT assignment_dates;
ALTER TABLE "Units" DROP CONSTRAINT unit_name_code;
DROP TRIGGER unit_no_delete ON "Units"; DROP TRIGGER audit_immutable ON "Audits";
DROP FUNCTION prevent_history_mutation(); DROP TRIGGER unit_integrity ON "Units"; DROP FUNCTION guard_unit_hierarchy();
""";
}
