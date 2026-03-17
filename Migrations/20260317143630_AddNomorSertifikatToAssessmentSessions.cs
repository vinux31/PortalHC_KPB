using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddNomorSertifikatToAssessmentSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NomorSertifikat",
                table: "AssessmentSessions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSessions_NomorSertifikat_Unique",
                table: "AssessmentSessions",
                column: "NomorSertifikat",
                unique: true,
                filter: "[NomorSertifikat] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssessmentSessions_NomorSertifikat_Unique",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "NomorSertifikat",
                table: "AssessmentSessions");
        }
    }
}
