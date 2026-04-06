using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentV14Columns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TextAnswer",
                table: "PackageUserResponses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionType",
                table: "PackageQuestions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssessmentPhase",
                table: "AssessmentSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssessmentType",
                table: "AssessmentSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasManualGrading",
                table: "AssessmentSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LinkedGroupId",
                table: "AssessmentSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LinkedSessionId",
                table: "AssessmentSessions",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TextAnswer",
                table: "PackageUserResponses");

            migrationBuilder.DropColumn(
                name: "QuestionType",
                table: "PackageQuestions");

            migrationBuilder.DropColumn(
                name: "AssessmentPhase",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "AssessmentType",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "HasManualGrading",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "LinkedGroupId",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "LinkedSessionId",
                table: "AssessmentSessions");
        }
    }
}
