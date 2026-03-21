using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionETScoreAndUserResponseSubmittedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "UserResponses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SessionElemenTeknisScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentSessionId = table.Column<int>(type: "int", nullable: false),
                    ElemenTeknis = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    QuestionCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionElemenTeknisScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionElemenTeknisScores_AssessmentSessions_AssessmentSessionId",
                        column: x => x.AssessmentSessionId,
                        principalTable: "AssessmentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionElemenTeknisScores_AssessmentSessionId_ElemenTeknis",
                table: "SessionElemenTeknisScores",
                columns: new[] { "AssessmentSessionId", "ElemenTeknis" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionElemenTeknisScores");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "UserResponses");
        }
    }
}
