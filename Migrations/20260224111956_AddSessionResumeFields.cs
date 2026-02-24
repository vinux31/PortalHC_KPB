using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionResumeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SavedQuestionCount",
                table: "UserPackageAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ElapsedSeconds",
                table: "AssessmentSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastActivePage",
                table: "AssessmentSessions",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SavedQuestionCount",
                table: "UserPackageAssignments");

            migrationBuilder.DropColumn(
                name: "ElapsedSeconds",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "LastActivePage",
                table: "AssessmentSessions");
        }
    }
}
