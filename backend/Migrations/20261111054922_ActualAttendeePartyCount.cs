using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class ActualAttendeePartyCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PeopleCount",
                table: "EventAttendance",
                type: "integer",
                nullable: false,
                defaultValue: 1);
            migrationBuilder.Sql("ALTER TABLE \"EventAttendance\" ADD CONSTRAINT attendance_people_positive CHECK (\"PeopleCount\">0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PeopleCount",
                table: "EventAttendance");
        }
    }
}
