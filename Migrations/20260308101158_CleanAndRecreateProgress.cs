using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class CleanAndRecreateProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step A: Delete all existing progress data (FK order matters)
            // Step B: Recreate progress for active assignments with unit filter
            // Step C: Log summary
            migrationBuilder.Sql(@"
                DECLARE @deletedHistories INT, @deletedSessions INT, @deletedProgress INT, @insertedProgress INT;

                DELETE FROM DeliverableStatusHistories;
                SET @deletedHistories = @@ROWCOUNT;

                DELETE FROM CoachingSessions WHERE ProtonDeliverableProgressId IS NOT NULL;
                SET @deletedSessions = @@ROWCOUNT;

                DELETE FROM ProtonDeliverableProgresses;
                SET @deletedProgress = @@ROWCOUNT;

                INSERT INTO ProtonDeliverableProgresses (CoacheeId, ProtonDeliverableId, ProtonTrackAssignmentId, Status, CreatedAt)
                SELECT
                    pta.CoacheeId,
                    pdl.Id,
                    pta.Id,
                    'Pending',
                    GETUTCDATE()
                FROM ProtonTrackAssignments pta
                INNER JOIN CoachCoacheeMappings ccm
                    ON ccm.CoacheeId = pta.CoacheeId AND ccm.IsActive = 1
                INNER JOIN Users u
                    ON u.Id = pta.CoacheeId
                INNER JOIN ProtonDeliverableList pdl
                    ON pdl.ProtonSubKompetensiId IN (
                        SELECT psk.Id FROM ProtonSubKompetensiList psk
                        INNER JOIN ProtonKompetensiList pk ON psk.ProtonKompetensiId = pk.Id
                        WHERE pk.ProtonTrackId = pta.ProtonTrackId
                          AND LTRIM(RTRIM(pk.Unit)) = LTRIM(RTRIM(COALESCE(NULLIF(ccm.AssignmentUnit, ''), u.Unit)))
                    )
                WHERE pta.IsActive = 1;
                SET @insertedProgress = @@ROWCOUNT;

                PRINT 'CleanAndRecreateProgress migration:';
                PRINT '  Deleted DeliverableStatusHistories: ' + CAST(@deletedHistories AS VARCHAR(10));
                PRINT '  Deleted CoachingSessions: ' + CAST(@deletedSessions AS VARCHAR(10));
                PRINT '  Deleted ProtonDeliverableProgresses: ' + CAST(@deletedProgress AS VARCHAR(10));
                PRINT '  Inserted ProtonDeliverableProgresses: ' + CAST(@insertedProgress AS VARCHAR(10));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data migration is not reversible — existing data was test data.
            // To revert, re-run the old AutoCreateProgressForAssignment without unit filter.
        }
    }
}
