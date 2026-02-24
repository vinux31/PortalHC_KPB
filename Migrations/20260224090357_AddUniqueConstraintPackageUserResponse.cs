using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintPackageUserResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PackageUserResponses_AssessmentSessionId_PackageQuestionId",
                table: "PackageUserResponses");

            migrationBuilder.CreateIndex(
                name: "IX_PackageUserResponses_AssessmentSessionId_PackageQuestionId",
                table: "PackageUserResponses",
                columns: new[] { "AssessmentSessionId", "PackageQuestionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PackageUserResponses_AssessmentSessionId_PackageQuestionId",
                table: "PackageUserResponses");

            migrationBuilder.CreateIndex(
                name: "IX_PackageUserResponses_AssessmentSessionId_PackageQuestionId",
                table: "PackageUserResponses",
                columns: new[] { "AssessmentSessionId", "PackageQuestionId" });
        }
    }
}
