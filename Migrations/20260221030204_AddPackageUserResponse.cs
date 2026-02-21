using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageUserResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackageUserResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentSessionId = table.Column<int>(type: "int", nullable: false),
                    PackageQuestionId = table.Column<int>(type: "int", nullable: false),
                    PackageOptionId = table.Column<int>(type: "int", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageUserResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageUserResponses_AssessmentSessions_AssessmentSessionId",
                        column: x => x.AssessmentSessionId,
                        principalTable: "AssessmentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageUserResponses_PackageOptions_PackageOptionId",
                        column: x => x.PackageOptionId,
                        principalTable: "PackageOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageUserResponses_PackageQuestions_PackageQuestionId",
                        column: x => x.PackageQuestionId,
                        principalTable: "PackageQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageUserResponses_AssessmentSessionId_PackageQuestionId",
                table: "PackageUserResponses",
                columns: new[] { "AssessmentSessionId", "PackageQuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageUserResponses_PackageOptionId",
                table: "PackageUserResponses",
                column: "PackageOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageUserResponses_PackageQuestionId",
                table: "PackageUserResponses",
                column: "PackageQuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageUserResponses");
        }
    }
}
