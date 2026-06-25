using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddRetakeColumnsAndArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowRetake",
                table: "AssessmentSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttempts",
                table: "AssessmentSessions",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "RetakeCooldownHours",
                table: "AssessmentSessions",
                type: "int",
                nullable: false,
                defaultValue: 24);

            migrationBuilder.CreateTable(
                name: "AssessmentAttemptResponseArchives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttemptHistoryId = table.Column<int>(type: "int", nullable: false),
                    PackageQuestionId = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true),
                    AwardedScore = table.Column<int>(type: "int", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentAttemptResponseArchives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentAttemptResponseArchives_AssessmentAttemptHistory_AttemptHistoryId",
                        column: x => x.AttemptHistoryId,
                        principalTable: "AssessmentAttemptHistory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttemptResponseArchives_AttemptHistoryId",
                table: "AssessmentAttemptResponseArchives",
                column: "AttemptHistoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentAttemptResponseArchives");

            migrationBuilder.DropColumn(
                name: "AllowRetake",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "MaxAttempts",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "RetakeCooldownHours",
                table: "AssessmentSessions");
        }
    }
}
