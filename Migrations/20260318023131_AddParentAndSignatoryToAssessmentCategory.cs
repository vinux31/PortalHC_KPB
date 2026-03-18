using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddParentAndSignatoryToAssessmentCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "AssessmentCategories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatoryUserId",
                table: "AssessmentCategories",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCategories_ParentId",
                table: "AssessmentCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCategories_SignatoryUserId",
                table: "AssessmentCategories",
                column: "SignatoryUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentCategories_AssessmentCategories_ParentId",
                table: "AssessmentCategories",
                column: "ParentId",
                principalTable: "AssessmentCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentCategories_Users_SignatoryUserId",
                table: "AssessmentCategories",
                column: "SignatoryUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentCategories_AssessmentCategories_ParentId",
                table: "AssessmentCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentCategories_Users_SignatoryUserId",
                table: "AssessmentCategories");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentCategories_ParentId",
                table: "AssessmentCategories");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentCategories_SignatoryUserId",
                table: "AssessmentCategories");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "AssessmentCategories");

            migrationBuilder.DropColumn(
                name: "SignatoryUserId",
                table: "AssessmentCategories");
        }
    }
}
