using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class CleanupCertTimingAndOrphanTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CLEN-04: NULL NomorSertifikat for sessions that haven't passed (Phase 227)
            // Cert numbers generated before exam completion are invalid — they should only exist when IsPassed=true
            migrationBuilder.Sql(@"
                UPDATE AssessmentSessions
                SET NomorSertifikat = NULL
                WHERE NomorSertifikat IS NOT NULL
                  AND (IsPassed IS NULL OR IsPassed = 0)
            ");

            migrationBuilder.DropTable(
                name: "AssessmentCompetencyMaps");

            migrationBuilder.DropTable(
                name: "UserCompetencyLevels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssessmentCompetencyMaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentCategory = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    KkjMatrixItemId = table.Column<int>(type: "int", nullable: false),
                    LevelGranted = table.Column<int>(type: "int", nullable: false),
                    MinimumScoreRequired = table.Column<int>(type: "int", nullable: true),
                    TitlePattern = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentCompetencyMaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserCompetencyLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentSessionId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AchievedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentLevel = table.Column<int>(type: "int", nullable: false),
                    KkjMatrixItemId = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetLevel = table.Column<int>(type: "int", nullable: false),
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
                name: "IX_UserCompetencyLevels_AssessmentSessionId",
                table: "UserCompetencyLevels",
                column: "AssessmentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompetencyLevels_UserId",
                table: "UserCompetencyLevels",
                column: "UserId");
        }
    }
}
