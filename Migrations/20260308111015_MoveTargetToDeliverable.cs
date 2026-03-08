using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class MoveTargetToDeliverable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add Target column to ProtonDeliverableList
            migrationBuilder.AddColumn<string>(
                name: "Target",
                table: "ProtonDeliverableList",
                type: "nvarchar(max)",
                nullable: true);

            // 2. Copy Target from SubKompetensi to all child Deliverables
            migrationBuilder.Sql(@"
                UPDATE d
                SET d.Target = s.Target
                FROM ProtonDeliverableList d
                INNER JOIN ProtonSubKompetensiList s ON d.ProtonSubKompetensiId = s.Id
                WHERE s.Target IS NOT NULL;
            ");

            // 3. Drop Target from SubKompetensi
            migrationBuilder.DropColumn(
                name: "Target",
                table: "ProtonSubKompetensiList");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Re-add Target to SubKompetensi
            migrationBuilder.AddColumn<string>(
                name: "Target",
                table: "ProtonSubKompetensiList",
                type: "nvarchar(max)",
                nullable: true);

            // 2. Copy first deliverable's Target back to SubKompetensi
            migrationBuilder.Sql(@"
                UPDATE s
                SET s.Target = (SELECT TOP 1 d.Target FROM ProtonDeliverableList d WHERE d.ProtonSubKompetensiId = s.Id AND d.Target IS NOT NULL)
                FROM ProtonSubKompetensiList s;
            ");

            // 3. Drop Target from Deliverable
            migrationBuilder.DropColumn(
                name: "Target",
                table: "ProtonDeliverableList");
        }
    }
}
