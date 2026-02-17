using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCoachingSessionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "CoachingSessions");

            migrationBuilder.RenameColumn(
                name: "Topic",
                table: "CoachingSessions",
                newName: "SubKompetensi");

            migrationBuilder.AddColumn<string>(
                name: "CatatanCoach",
                table: "CoachingSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CoacheeCompetencies",
                table: "CoachingSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Deliverable",
                table: "CoachingSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Kesimpulan",
                table: "CoachingSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Kompetensi",
                table: "CoachingSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "CoachingSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CatatanCoach",
                table: "CoachingSessions");

            migrationBuilder.DropColumn(
                name: "CoacheeCompetencies",
                table: "CoachingSessions");

            migrationBuilder.DropColumn(
                name: "Deliverable",
                table: "CoachingSessions");

            migrationBuilder.DropColumn(
                name: "Kesimpulan",
                table: "CoachingSessions");

            migrationBuilder.DropColumn(
                name: "Kompetensi",
                table: "CoachingSessions");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "CoachingSessions");

            migrationBuilder.RenameColumn(
                name: "SubKompetensi",
                table: "CoachingSessions",
                newName: "Topic");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "CoachingSessions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
