using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddProtonExamFieldsToAssessmentSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InterviewResultsJson",
                table: "AssessmentSessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProtonTrackId",
                table: "AssessmentSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TahunKe",
                table: "AssessmentSessions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterviewResultsJson",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "ProtonTrackId",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "TahunKe",
                table: "AssessmentSessions");
        }
    }
}
