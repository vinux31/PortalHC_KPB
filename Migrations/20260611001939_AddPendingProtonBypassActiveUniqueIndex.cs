using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingProtonBypassActiveUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PendingProtonBypasses_CoacheeId_ActiveUnique",
                table: "PendingProtonBypasses",
                column: "CoacheeId",
                unique: true,
                filter: "[Status] IN (N'Menunggu', N'Siap')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PendingProtonBypasses_CoacheeId_ActiveUnique",
                table: "PendingProtonBypasses");
        }
    }
}
