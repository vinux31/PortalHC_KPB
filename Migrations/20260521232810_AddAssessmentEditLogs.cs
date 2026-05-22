using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentEditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssessmentEditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentSessionId = table.Column<int>(type: "int", nullable: false),
                    PackageQuestionId = table.Column<int>(type: "int", nullable: false),
                    QuestionTextSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldAnswerJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldAnswerTextSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewAnswerJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewAnswerTextSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldScore = table.Column<int>(type: "int", nullable: true),
                    NewScore = table.Column<int>(type: "int", nullable: true),
                    OldIsPassed = table.Column<bool>(type: "bit", nullable: true),
                    NewIsPassed = table.Column<bool>(type: "bit", nullable: true),
                    ActorUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActorRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReasonText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentEditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentEditLogs_AssessmentSessions_AssessmentSessionId",
                        column: x => x.AssessmentSessionId,
                        principalTable: "AssessmentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentEditLogs_SessionId_EditedAt",
                table: "AssessmentEditLogs",
                columns: new[] { "AssessmentSessionId", "EditedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentEditLogs");
        }
    }
}
