using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentResultFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowAnswerReview",
                table: "AssessmentSessions",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "AssessmentSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPassed",
                table: "AssessmentSessions",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PassPercentage",
                table: "AssessmentSessions",
                type: "int",
                nullable: false,
                defaultValue: 70);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentSession_PassPercentage",
                table: "AssessmentSessions",
                sql: "[PassPercentage] >= 0 AND [PassPercentage] <= 100");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentSession_PassPercentage",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "AllowAnswerReview",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "IsPassed",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "PassPercentage",
                table: "AssessmentSessions");
        }
    }
}
