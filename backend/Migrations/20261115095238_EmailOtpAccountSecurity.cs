using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Society.Api.Migrations
{
    /// <inheritdoc />
    public partial class EmailOtpAccountSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailLoginChallenge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdentifierHash = table.Column<string>(type: "text", nullable: false),
                    ProtectedCode = table.Column<string>(type: "text", nullable: false),
                    SecurityStamp = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLoginChallenge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailLoginChallenge_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLoginChallenge_ExpiresAt",
                table: "EmailLoginChallenge",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLoginChallenge_IdentifierHash_CreatedAt",
                table: "EmailLoginChallenge",
                columns: new[] { "IdentifierHash", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLoginChallenge_UserId",
                table: "EmailLoginChallenge",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailLoginChallenge");
        }
    }
}
