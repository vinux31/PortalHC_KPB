using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class LinkProgressToAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Wipe existing test data before schema change
            migrationBuilder.Sql("DELETE FROM DeliverableStatusHistories");
            migrationBuilder.Sql("DELETE FROM CoachingSessions WHERE ProtonDeliverableProgressId IS NOT NULL");
            migrationBuilder.Sql("DELETE FROM ProtonDeliverableProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ProtonDeliverableProgresses_CoacheeId_ProtonDeliverableId",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.AddColumn<int>(
                name: "ProtonTrackAssignmentId",
                table: "ProtonDeliverableProgresses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProtonDeliverableProgresses_ProtonTrackAssignmentId",
                table: "ProtonDeliverableProgresses",
                column: "ProtonTrackAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtonDeliverableProgresses_ProtonTrackAssignmentId_ProtonDeliverableId",
                table: "ProtonDeliverableProgresses",
                columns: new[] { "ProtonTrackAssignmentId", "ProtonDeliverableId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProtonDeliverableProgresses_ProtonTrackAssignments_ProtonTrackAssignmentId",
                table: "ProtonDeliverableProgresses",
                column: "ProtonTrackAssignmentId",
                principalTable: "ProtonTrackAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Re-populate progress records from active assignments
            migrationBuilder.Sql(@"
                INSERT INTO ProtonDeliverableProgresses
                    (CoacheeId, ProtonDeliverableId, ProtonTrackAssignmentId, Status,
                     HCApprovalStatus, SrSpvApprovalStatus, ShApprovalStatus, CreatedAt)
                SELECT
                    a.CoacheeId,
                    d.Id,
                    a.Id,
                    'Pending',
                    'Pending',
                    'Pending',
                    'Pending',
                    GETUTCDATE()
                FROM ProtonTrackAssignments a
                INNER JOIN ProtonKompetensiList k ON k.ProtonTrackId = a.ProtonTrackId
                INNER JOIN ProtonSubKompetensiList sk ON sk.ProtonKompetensiId = k.Id
                INNER JOIN ProtonDeliverableList d ON d.ProtonSubKompetensiId = sk.Id
                WHERE a.IsActive = 1
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProtonDeliverableProgresses_ProtonTrackAssignments_ProtonTrackAssignmentId",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ProtonDeliverableProgresses_ProtonTrackAssignmentId",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ProtonDeliverableProgresses_ProtonTrackAssignmentId_ProtonDeliverableId",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.DropColumn(
                name: "ProtonTrackAssignmentId",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.CreateIndex(
                name: "IX_ProtonDeliverableProgresses_CoacheeId_ProtonDeliverableId",
                table: "ProtonDeliverableProgresses",
                columns: new[] { "CoacheeId", "ProtonDeliverableId" },
                unique: true);
        }
    }
}
