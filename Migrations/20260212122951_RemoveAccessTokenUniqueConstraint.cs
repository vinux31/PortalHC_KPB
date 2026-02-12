using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAccessTokenUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssessmentSessions_AccessToken",
                table: "AssessmentSessions");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSessions_AccessToken",
                table: "AssessmentSessions",
                column: "AccessToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssessmentSessions_AccessToken",
                table: "AssessmentSessions");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSessions_AccessToken",
                table: "AssessmentSessions",
                column: "AccessToken",
                unique: true);
        }
    }
}
