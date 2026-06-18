using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddUserUnitsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserUnits_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserUnits_UserId_PrimaryUnique",
                table: "UserUnits",
                column: "UserId",
                unique: true,
                filter: "[IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_UserUnits_UserId_Unit_Unique",
                table: "UserUnits",
                columns: new[] { "UserId", "Unit" },
                unique: true);

            // BACKFILL idempotent (Claude's Discretion -> lean Up). 1 primary-row per pekerja dgn Unit non-null.
            // WHERE NOT EXISTS = idempotent (re-run tidak dobel). Pekerja Unit null/empty -> 0 baris.
            // Literal statik, no user input (T-399-01-01 mitigated).
            migrationBuilder.Sql(@"
                INSERT INTO UserUnits (UserId, Unit, IsPrimary, IsActive)
                SELECT u.Id, u.Unit, 1, 1
                FROM Users u
                WHERE u.Unit IS NOT NULL AND u.Unit <> ''
                  AND NOT EXISTS (SELECT 1 FROM UserUnits uu WHERE uu.UserId = u.Id)
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserUnits");
        }
    }
}
