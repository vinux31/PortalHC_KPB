using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentFieldsAndUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deactivate duplicate active mappings: keep newest (highest Id), deactivate rest
            migrationBuilder.Sql(@"
                UPDATE m SET m.IsActive = 0, m.EndDate = GETDATE()
                FROM CoachCoacheeMappings m
                INNER JOIN (
                    SELECT CoacheeId, MAX(Id) AS KeepId
                    FROM CoachCoacheeMappings
                    WHERE IsActive = 1
                    GROUP BY CoacheeId
                    HAVING COUNT(*) > 1
                ) dups ON m.CoacheeId = dups.CoacheeId AND m.Id <> dups.KeepId
                WHERE m.IsActive = 1
            ");

            migrationBuilder.DropIndex(
                name: "IX_CoachCoacheeMappings_CoacheeId",
                table: "CoachCoacheeMappings");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentSection",
                table: "CoachCoacheeMappings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignmentUnit",
                table: "CoachCoacheeMappings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachCoacheeMappings_CoacheeId_ActiveUnique",
                table: "CoachCoacheeMappings",
                column: "CoacheeId",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CoachCoacheeMappings_CoacheeId_ActiveUnique",
                table: "CoachCoacheeMappings");

            migrationBuilder.DropColumn(
                name: "AssignmentSection",
                table: "CoachCoacheeMappings");

            migrationBuilder.DropColumn(
                name: "AssignmentUnit",
                table: "CoachCoacheeMappings");

            migrationBuilder.CreateIndex(
                name: "IX_CoachCoacheeMappings_CoacheeId",
                table: "CoachCoacheeMappings",
                column: "CoacheeId");
        }
    }
}
