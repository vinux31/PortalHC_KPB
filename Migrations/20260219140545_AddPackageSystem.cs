using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssessmentPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentSessionId = table.Column<int>(type: "int", nullable: false),
                    PackageName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PackageNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentPackages_AssessmentSessions_AssessmentSessionId",
                        column: x => x.AssessmentSessionId,
                        principalTable: "AssessmentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentPackageId = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    ScoreValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageQuestions_AssessmentPackages_AssessmentPackageId",
                        column: x => x.AssessmentPackageId,
                        principalTable: "AssessmentPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPackageAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentSessionId = table.Column<int>(type: "int", nullable: false),
                    AssessmentPackageId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ShuffledQuestionIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShuffledOptionIdsPerQuestion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPackageAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPackageAssignments_AssessmentPackages_AssessmentPackageId",
                        column: x => x.AssessmentPackageId,
                        principalTable: "AssessmentPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserPackageAssignments_AssessmentSessions_AssessmentSessionId",
                        column: x => x.AssessmentSessionId,
                        principalTable: "AssessmentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageQuestionId = table.Column<int>(type: "int", nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageOptions_PackageQuestions_PackageQuestionId",
                        column: x => x.PackageQuestionId,
                        principalTable: "PackageQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentPackages_AssessmentSessionId",
                table: "AssessmentPackages",
                column: "AssessmentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageOptions_PackageQuestionId",
                table: "PackageOptions",
                column: "PackageQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageQuestions_AssessmentPackageId",
                table: "PackageQuestions",
                column: "AssessmentPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageQuestions_Order",
                table: "PackageQuestions",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_UserPackageAssignments_AssessmentPackageId",
                table: "UserPackageAssignments",
                column: "AssessmentPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPackageAssignments_AssessmentSessionId_UserId",
                table: "UserPackageAssignments",
                columns: new[] { "AssessmentSessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPackageAssignments_UserId",
                table: "UserPackageAssignments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageOptions");

            migrationBuilder.DropTable(
                name: "UserPackageAssignments");

            migrationBuilder.DropTable(
                name: "PackageQuestions");

            migrationBuilder.DropTable(
                name: "AssessmentPackages");
        }
    }
}
