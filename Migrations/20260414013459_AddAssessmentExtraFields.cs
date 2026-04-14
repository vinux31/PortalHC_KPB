using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentExtraFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificateType",
                table: "AssessmentSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsManualEntry",
                table: "AssessmentSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Kota",
                table: "AssessmentSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManualSertifikatUrl",
                table: "AssessmentSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Penyelenggara",
                table: "AssessmentSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubKategori",
                table: "AssessmentSessions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificateType",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "IsManualEntry",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "Kota",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "ManualSertifikatUrl",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "Penyelenggara",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "SubKategori",
                table: "AssessmentSessions");
        }
    }
}
