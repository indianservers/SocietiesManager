using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationReadModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
CREATE FUNCTION descendant_chapter_count(tenant uuid, root uuid) RETURNS integer LANGUAGE sql STABLE AS $$
WITH RECURSIVE descendants AS (
 SELECT "Id","LevelId" FROM "Units" WHERE "TenantId"=tenant AND "ParentId"=root
 UNION ALL SELECT u."Id",u."LevelId" FROM "Units" u JOIN descendants d ON u."ParentId"=d."Id" WHERE u."TenantId"=tenant
) SELECT count(*)::integer FROM descendants d JOIN "Levels" l ON l."Id"=d."LevelId" AND l."TenantId"=tenant WHERE l."Kind"='Chapter';
$$;
""");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION descendant_chapter_count(uuid,uuid)");

        }
    }
}
