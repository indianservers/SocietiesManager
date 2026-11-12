using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class SharedStandardDesignScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql("""

CREATE FUNCTION unit_within(tenant uuid,unit uuid,ancestor uuid) RETURNS boolean LANGUAGE sql STABLE AS $fn$
 WITH RECURSIVE chain AS (
 SELECT u."Id",u."ParentId",ARRAY[u."Id"] path FROM "Units" u WHERE u."TenantId"=$1 AND u."Id"=$2
 UNION ALL SELECT p."Id",p."ParentId",c.path||p."Id" FROM "Units" p JOIN chain c ON c."ParentId"=p."Id" WHERE p."TenantId"=$1 AND NOT p."Id"=ANY(c.path)
 ) SELECT EXISTS(SELECT 1 FROM chain WHERE "Id"=$3)
$fn$;
UPDATE "GovernanceRoles" SET "Permissions"=ARRAY(SELECT DISTINCT unnest("Permissions"||ARRAY['studio.read','service.read','intelligence.read','assistant.use'])) WHERE "Name" LIKE '%Treasurer%';

""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS unit_within(uuid,uuid,uuid);");

        }
    }
}
