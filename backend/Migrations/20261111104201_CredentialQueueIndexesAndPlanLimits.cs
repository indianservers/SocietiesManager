using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class CredentialQueueIndexesAndPlanLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VolunteerAttendance_TenantId_MembershipId",
                table: "VolunteerAttendance");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAttendance_TenantId_MembershipId_Status_CheckedIn",
                table: "VolunteerAttendance",
                columns: new[] { "TenantId", "MembershipId", "Status", "CheckedIn" });

            migrationBuilder.CreateIndex(
                name: "IX_StudioJob_Kind_State_At",
                table: "StudioJob",
                columns: new[] { "Kind", "State", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_Credential_Status_At",
                table: "Credential",
                columns: new[] { "Status", "At" },
                filter: "\"Status\"='Approved'");
            migrationBuilder.Sql("""

CREATE FUNCTION membership_plan_quota() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE tenant uuid; quota int; added int; removed int; BEGIN
 FOR tenant IN SELECT DISTINCT "TenantId" FROM membership_new WHERE "Status"='Active' LOOP
 SELECT "MembershipLimit" INTO quota FROM "TenantSubscription" WHERE "TenantId"=tenant;
 IF quota IS NULL THEN CONTINUE;END IF;
 SELECT count(*) INTO added FROM membership_new WHERE "TenantId"=tenant AND "Status"='Active';removed:=0;
 IF TG_OP='UPDATE' THEN SELECT count(*) INTO removed FROM membership_old WHERE "TenantId"=tenant AND "Status"='Active';END IF;
 IF added<=removed THEN CONTINUE;END IF;
 PERFORM pg_advisory_xact_lock(hashtextextended(tenant::text,626));
 IF (SELECT count(*) FROM "Memberships" WHERE "TenantId"=tenant AND "Status"='Active')>quota THEN RAISE EXCEPTION 'Active-status membership plan allowance exceeded' USING ERRCODE='23514';END IF;
 END LOOP;RETURN NULL;END $$;
CREATE TRIGGER membership_plan_insert AFTER INSERT ON "Memberships" REFERENCING NEW TABLE AS membership_new FOR EACH STATEMENT EXECUTE FUNCTION membership_plan_quota();
CREATE TRIGGER membership_plan_update AFTER UPDATE ON "Memberships" REFERENCING NEW TABLE AS membership_new OLD TABLE AS membership_old FOR EACH STATEMENT EXECUTE FUNCTION membership_plan_quota();
CREATE FUNCTION chapter_plan_quota() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE tenant uuid; quota int; added int; removed int; BEGIN
 FOR tenant IN SELECT DISTINCT "TenantId" FROM units_new LOOP
 SELECT "ChapterLimit" INTO quota FROM "TenantSubscription" WHERE "TenantId"=tenant;
 IF quota IS NULL THEN CONTINUE;END IF;
 SELECT count(*) INTO added FROM units_new u WHERE u."TenantId"=tenant AND u."Status" NOT IN('Closed','Archived') AND EXISTS(SELECT 1 FROM "Levels" l WHERE l."Id"=u."LevelId" AND l."Kind"='Chapter');removed:=0;
 IF TG_OP='UPDATE' THEN SELECT count(*) INTO removed FROM units_old u WHERE u."TenantId"=tenant AND u."Status" NOT IN('Closed','Archived') AND EXISTS(SELECT 1 FROM "Levels" l WHERE l."Id"=u."LevelId" AND l."Kind"='Chapter');END IF;
 IF added<=removed THEN CONTINUE;END IF;
 PERFORM pg_advisory_xact_lock(hashtextextended(tenant::text,626));
 IF (SELECT count(*) FROM "Units" u WHERE u."TenantId"=tenant AND u."Status" NOT IN('Closed','Archived') AND EXISTS(SELECT 1 FROM "Levels" l WHERE l."Id"=u."LevelId" AND l."Kind"='Chapter'))>quota THEN RAISE EXCEPTION 'Chapter plan allowance exceeded' USING ERRCODE='23514';END IF;
 END LOOP;RETURN NULL;END $$;
CREATE TRIGGER chapter_plan_insert AFTER INSERT ON "Units" REFERENCING NEW TABLE AS units_new FOR EACH STATEMENT EXECUTE FUNCTION chapter_plan_quota();
CREATE TRIGGER chapter_plan_update AFTER UPDATE ON "Units" REFERENCING NEW TABLE AS units_new OLD TABLE AS units_old FOR EACH STATEMENT EXECUTE FUNCTION chapter_plan_quota();

""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS chapter_plan_quota() CASCADE; DROP FUNCTION IF EXISTS membership_plan_quota() CASCADE;");
            migrationBuilder.DropIndex(
                name: "IX_VolunteerAttendance_TenantId_MembershipId_Status_CheckedIn",
                table: "VolunteerAttendance");

            migrationBuilder.DropIndex(
                name: "IX_StudioJob_Kind_State_At",
                table: "StudioJob");

            migrationBuilder.DropIndex(
                name: "IX_Credential_Status_At",
                table: "Credential");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAttendance_TenantId_MembershipId",
                table: "VolunteerAttendance",
                columns: new[] { "TenantId", "MembershipId" });
        }
    }
}
