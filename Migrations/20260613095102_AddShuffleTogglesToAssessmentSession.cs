using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddShuffleTogglesToAssessmentSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShuffleOptions",
                table: "AssessmentSessions",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShuffleQuestions",
                table: "AssessmentSessions",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShuffleOptions",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "ShuffleQuestions",
                table: "AssessmentSessions");
        }
    }
}
