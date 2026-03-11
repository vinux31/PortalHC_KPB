using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddGenerateCertificateToAssessmentSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GenerateCertificate",
                table: "AssessmentSessions",
                type: "bit",
                nullable: false,
                defaultValue: true);  // existing rows default TRUE (backward compatible)
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenerateCertificate",
                table: "AssessmentSessions");
        }
    }
}
