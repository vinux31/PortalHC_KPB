using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddAcuanFieldsToCoachingSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcuanBestPractice",
                table: "CoachingSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcuanDokumen",
                table: "CoachingSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcuanPedoman",
                table: "CoachingSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcuanTko",
                table: "CoachingSessions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcuanBestPractice",
                table: "CoachingSessions");

            migrationBuilder.DropColumn(
                name: "AcuanDokumen",
                table: "CoachingSessions");

            migrationBuilder.DropColumn(
                name: "AcuanPedoman",
                table: "CoachingSessions");

            migrationBuilder.DropColumn(
                name: "AcuanTko",
                table: "CoachingSessions");
        }
    }
}
