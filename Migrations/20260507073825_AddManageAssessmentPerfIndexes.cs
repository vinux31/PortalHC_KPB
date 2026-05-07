using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddManageAssessmentPerfIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSessions_ExamWindowCloseDate",
                table: "AssessmentSessions",
                column: "ExamWindowCloseDate");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSessions_LinkedGroupId",
                table: "AssessmentSessions",
                column: "LinkedGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssessmentSessions_ExamWindowCloseDate",
                table: "AssessmentSessions");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentSessions_LinkedGroupId",
                table: "AssessmentSessions");
        }
    }
}
