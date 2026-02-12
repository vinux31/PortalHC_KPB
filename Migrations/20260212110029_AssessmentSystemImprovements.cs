using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AssessmentSystemImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserResponses_AssessmentOptions_SelectedOptionId",
                table: "UserResponses");

            migrationBuilder.DropIndex(
                name: "IX_UserResponses_AssessmentSessionId",
                table: "UserResponses");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "AssessmentSessions");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AssessmentSessions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AccessToken",
                table: "AssessmentSessions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AssessmentSessions",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AssessmentSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AssessmentSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserResponses_AssessmentSessionId_AssessmentQuestionId",
                table: "UserResponses",
                columns: new[] { "AssessmentSessionId", "AssessmentQuestionId" });

            // Fix: Update all AccessTokens to be unique (prefix with ID to ensure uniqueness)
            migrationBuilder.Sql(@"
                UPDATE AssessmentSessions
                SET AccessToken = 'T' + CAST(Id AS VARCHAR(10)) + '-' +
                    CASE
                        WHEN LEN(AccessToken) > 0 THEN AccessToken
                        ELSE UPPER(LEFT(NEWID(), 6))
                    END
            ");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSessions_AccessToken",
                table: "AssessmentSessions",
                column: "AccessToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSessions_UserId_Status",
                table: "AssessmentSessions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentSession_DurationMinutes",
                table: "AssessmentSessions",
                sql: "[DurationMinutes] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentSession_Progress",
                table: "AssessmentSessions",
                sql: "[Progress] >= 0 AND [Progress] <= 100");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentQuestions_Order",
                table: "AssessmentQuestions",
                column: "Order");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentQuestion_ScoreValue",
                table: "AssessmentQuestions",
                sql: "[ScoreValue] > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_UserResponses_AssessmentOptions_SelectedOptionId",
                table: "UserResponses",
                column: "SelectedOptionId",
                principalTable: "AssessmentOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserResponses_AssessmentOptions_SelectedOptionId",
                table: "UserResponses");

            migrationBuilder.DropIndex(
                name: "IX_UserResponses_AssessmentSessionId_AssessmentQuestionId",
                table: "UserResponses");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentSessions_AccessToken",
                table: "AssessmentSessions");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentSessions_UserId_Status",
                table: "AssessmentSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentSession_DurationMinutes",
                table: "AssessmentSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentSession_Progress",
                table: "AssessmentSessions");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentQuestions_Order",
                table: "AssessmentQuestions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentQuestion_ScoreValue",
                table: "AssessmentQuestions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssessmentSessions");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AssessmentSessions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "AccessToken",
                table: "AssessmentSessions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "AssessmentSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserResponses_AssessmentSessionId",
                table: "UserResponses",
                column: "AssessmentSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserResponses_AssessmentOptions_SelectedOptionId",
                table: "UserResponses",
                column: "SelectedOptionId",
                principalTable: "AssessmentOptions",
                principalColumn: "Id");
        }
    }
}
