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
            // 1. Delete all DeliverableStatusHistory (references progress records)
            migrationBuilder.Sql("DELETE FROM DeliverableStatusHistories");

            // 2. Delete CoachingSessions that reference progress records
            migrationBuilder.Sql("DELETE FROM CoachingSessions WHERE ProtonDeliverableProgressId IS NOT NULL");

            // 3. Delete all ProtonDeliverableProgresses
            migrationBuilder.Sql("DELETE FROM ProtonDeliverableProgresses");

            // 4. Drop old unique index
            migrationBuilder.DropIndex(
                name: "IX_ProtonDeliverableProgresses_CoacheeId_ProtonDeliverableId",
                table: "ProtonDeliverableProgresses");

            // 5. Add ProtonTrackAssignmentId column
            migrationBuilder.AddColumn<int>(
                name: "ProtonTrackAssignmentId",
                table: "ProtonDeliverableProgresses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 6. Create new unique index
            migrationBuilder.CreateIndex(
                name: "IX_ProtonDeliverableProgresses_ProtonTrackAssignmentId_ProtonDeliverableId",
                table: "ProtonDeliverableProgresses",
                columns: new[] { "ProtonTrackAssignmentId", "ProtonDeliverableId" },
                unique: true);

            // 7. Create index on ProtonTrackAssignmentId
            migrationBuilder.CreateIndex(
                name: "IX_ProtonDeliverableProgresses_ProtonTrackAssignmentId",
                table: "ProtonDeliverableProgresses",
                column: "ProtonTrackAssignmentId");

            // 8. Add FK constraint
            migrationBuilder.AddForeignKey(
                name: "FK_ProtonDeliverableProgresses_ProtonTrackAssignments_ProtonTrackAssignmentId",
                table: "ProtonDeliverableProgresses",
                column: "ProtonTrackAssignmentId",
                principalTable: "ProtonTrackAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // 9. Re-populate progress records from active assignments
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
                INNER JOIN ProtonKompetensis k ON k.ProtonTrackId = a.ProtonTrackId
                INNER JOIN ProtonSubKompetensis sk ON sk.ProtonKompetensiId = k.Id
                INNER JOIN ProtonDeliverables d ON d.ProtonSubKompetensiId = sk.Id
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
                name: "IX_ProtonDeliverableProgresses_ProtonTrackAssignmentId_ProtonDeliverableId",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ProtonDeliverableProgresses_ProtonTrackAssignmentId",
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
