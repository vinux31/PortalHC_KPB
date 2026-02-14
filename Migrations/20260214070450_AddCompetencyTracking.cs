using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetencyTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssessmentCompetencyMaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KkjMatrixItemId = table.Column<int>(type: "int", nullable: false),
                    AssessmentCategory = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TitlePattern = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LevelGranted = table.Column<int>(type: "int", nullable: false),
                    MinimumScoreRequired = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentCompetencyMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentCompetencyMaps_KkjMatrices_KkjMatrixItemId",
                        column: x => x.KkjMatrixItemId,
                        principalTable: "KkjMatrices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCompetencyLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    KkjMatrixItemId = table.Column<int>(type: "int", nullable: false),
                    CurrentLevel = table.Column<int>(type: "int", nullable: false),
                    TargetLevel = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssessmentSessionId = table.Column<int>(type: "int", nullable: true),
                    AchievedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompetencyLevels", x => x.Id);
                    table.CheckConstraint("CK_UserCompetencyLevel_CurrentLevel", "[CurrentLevel] >= 0 AND [CurrentLevel] <= 5");
                    table.CheckConstraint("CK_UserCompetencyLevel_TargetLevel", "[TargetLevel] >= 0 AND [TargetLevel] <= 5");
                    table.ForeignKey(
                        name: "FK_UserCompetencyLevels_AssessmentSessions_AssessmentSessionId",
                        column: x => x.AssessmentSessionId,
                        principalTable: "AssessmentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserCompetencyLevels_KkjMatrices_KkjMatrixItemId",
                        column: x => x.KkjMatrixItemId,
                        principalTable: "KkjMatrices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserCompetencyLevels_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCompetencyMaps_AssessmentCategory",
                table: "AssessmentCompetencyMaps",
                column: "AssessmentCategory");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCompetencyMaps_AssessmentCategory_TitlePattern",
                table: "AssessmentCompetencyMaps",
                columns: new[] { "AssessmentCategory", "TitlePattern" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCompetencyMaps_KkjMatrixItemId",
                table: "AssessmentCompetencyMaps",
                column: "KkjMatrixItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompetencyLevels_AssessmentSessionId",
                table: "UserCompetencyLevels",
                column: "AssessmentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompetencyLevels_KkjMatrixItemId",
                table: "UserCompetencyLevels",
                column: "KkjMatrixItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompetencyLevels_UserId_KkjMatrixItemId",
                table: "UserCompetencyLevels",
                columns: new[] { "UserId", "KkjMatrixItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentCompetencyMaps");

            migrationBuilder.DropTable(
                name: "UserCompetencyLevels");
        }
    }
}
