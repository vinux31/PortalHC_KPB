using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class RestoreUserResponseForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_UserResponses_AssessmentQuestions_AssessmentQuestionId",
                table: "UserResponses",
                column: "AssessmentQuestionId",
                principalTable: "AssessmentQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserResponses_AssessmentSessions_AssessmentSessionId",
                table: "UserResponses",
                column: "AssessmentSessionId",
                principalTable: "AssessmentSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserResponses_AssessmentQuestions_AssessmentQuestionId",
                table: "UserResponses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserResponses_AssessmentSessions_AssessmentSessionId",
                table: "UserResponses");
        }
    }
}
