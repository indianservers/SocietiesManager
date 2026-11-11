using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class CommunityIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
CREATE TRIGGER post_no_delete BEFORE DELETE ON "ActivityPost" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation();
CREATE TRIGGER event_no_delete BEFORE DELETE ON "CommunityEvent" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation();
CREATE TRIGGER registration_no_delete BEFORE DELETE ON "EventRegistration" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation();
CREATE TRIGGER attendance_no_delete BEFORE DELETE ON "EventAttendance" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation();
CREATE FUNCTION guard_community_audience() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF EXISTS(SELECT 1 FROM unnest(NEW."Audience") a WHERE NOT EXISTS(SELECT 1 FROM "Units" u WHERE u."Id"=a AND u."TenantId"=NEW."TenantId")) THEN RAISE EXCEPTION 'Audience organization is outside the society'; END IF;
 RETURN NEW; END $$;
CREATE TRIGGER post_audience_integrity BEFORE INSERT OR UPDATE ON "ActivityPost" FOR EACH ROW EXECUTE FUNCTION guard_community_audience();
CREATE TRIGGER event_audience_integrity BEFORE INSERT OR UPDATE ON "CommunityEvent" FOR EACH ROW EXECUTE FUNCTION guard_community_audience();
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER post_no_delete ON \"ActivityPost\"; DROP TRIGGER event_no_delete ON \"CommunityEvent\"; DROP TRIGGER registration_no_delete ON \"EventRegistration\"; DROP TRIGGER attendance_no_delete ON \"EventAttendance\"; DROP TRIGGER post_audience_integrity ON \"ActivityPost\"; DROP TRIGGER event_audience_integrity ON \"CommunityEvent\"; DROP FUNCTION guard_community_audience();");
        }
    }
}
