using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class MigrateLegacyQuestionsAndDropTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 0: Abandon any active legacy sessions (per D-04 decision)
            // Sessions that have legacy questions but no package assignment are abandoned so data is safe to migrate.
            migrationBuilder.Sql(@"
                UPDATE AssessmentSessions
                SET Status = 'Abandoned'
                WHERE Status IN ('Open', 'InProgress')
                  AND EXISTS (SELECT 1 FROM AssessmentQuestions aq WHERE aq.AssessmentSessionId = AssessmentSessions.Id)
                  AND NOT EXISTS (SELECT 1 FROM AssessmentPackages ap WHERE ap.AssessmentSessionId = AssessmentSessions.Id)
            ");

            // Step 1: Create AssessmentPackage for each legacy session that doesn't have one
            migrationBuilder.Sql(@"
                INSERT INTO AssessmentPackages (AssessmentSessionId, PackageName, PackageNumber, CreatedAt)
                SELECT DISTINCT aq.AssessmentSessionId, N'Paket A', 1, GETUTCDATE()
                FROM AssessmentQuestions aq
                WHERE NOT EXISTS (
                    SELECT 1 FROM AssessmentPackages ap
                    WHERE ap.AssessmentSessionId = aq.AssessmentSessionId
                )
            ");

            // Step 2: Migrate AssessmentQuestion -> PackageQuestion
            migrationBuilder.Sql(@"
                INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, ElemenTeknis)
                SELECT ap.Id, aq.QuestionText, aq.[Order], aq.ScoreValue, NULL
                FROM AssessmentQuestions aq
                JOIN AssessmentPackages ap ON ap.AssessmentSessionId = aq.AssessmentSessionId
                                           AND ap.PackageName = N'Paket A'
            ");

            // Step 3: Migrate AssessmentOption -> PackageOption
            // Join via AssessmentQuestion.Id -> find matching PackageQuestion by same session + same Order
            migrationBuilder.Sql(@"
                INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
                SELECT pq.Id, ao.OptionText, ao.IsCorrect
                FROM AssessmentOptions ao
                JOIN AssessmentQuestions aq ON aq.Id = ao.AssessmentQuestionId
                JOIN AssessmentPackages ap ON ap.AssessmentSessionId = aq.AssessmentSessionId
                                           AND ap.PackageName = N'Paket A'
                JOIN PackageQuestions pq ON pq.AssessmentPackageId = ap.Id
                                          AND pq.[Order] = aq.[Order]
            ");

            // Step 4: Migrate UserResponse -> PackageUserResponse (for completed sessions only)
            // UserResponse has AssessmentSessionId — get UserId from the AssessmentSession.
            // Create UserPackageAssignment first for sessions that have UserResponse data.
            migrationBuilder.Sql(@"
                INSERT INTO UserPackageAssignments (UserId, AssessmentPackageId, AssignedAt, AssessmentSessionId, IsCompleted, ShuffledQuestionIds, ShuffledOptionIdsPerQuestion)
                SELECT DISTINCT sess.UserId, ap.Id, GETUTCDATE(), sess.Id, 1, N'[]', N'{}'
                FROM UserResponses ur
                JOIN AssessmentSessions sess ON sess.Id = ur.AssessmentSessionId
                JOIN AssessmentQuestions aq ON aq.Id = ur.AssessmentQuestionId
                JOIN AssessmentPackages ap ON ap.AssessmentSessionId = aq.AssessmentSessionId
                                           AND ap.PackageName = N'Paket A'
                WHERE NOT EXISTS (
                    SELECT 1 FROM UserPackageAssignments upa
                    WHERE upa.UserId = sess.UserId AND upa.AssessmentPackageId = ap.Id
                )
            ");

            // Step 5: Migrate UserResponse -> PackageUserResponse
            migrationBuilder.Sql(@"
                INSERT INTO PackageUserResponses (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt)
                SELECT sess.Id, pq.Id, po.Id, ISNULL(ur.SubmittedAt, GETUTCDATE())
                FROM UserResponses ur
                JOIN AssessmentSessions sess ON sess.Id = ur.AssessmentSessionId
                JOIN AssessmentQuestions aq ON aq.Id = ur.AssessmentQuestionId
                JOIN AssessmentPackages ap ON ap.AssessmentSessionId = aq.AssessmentSessionId
                                           AND ap.PackageName = N'Paket A'
                JOIN PackageQuestions pq ON pq.AssessmentPackageId = ap.Id
                                          AND pq.[Order] = aq.[Order]
                JOIN AssessmentOptions ao_selected ON ao_selected.Id = ur.SelectedOptionId
                JOIN PackageOptions po ON po.PackageQuestionId = pq.Id
                                        AND po.OptionText = ao_selected.OptionText
                JOIN UserPackageAssignments upa ON upa.UserId = sess.UserId
                                                AND upa.AssessmentPackageId = ap.Id
                WHERE ur.SelectedOptionId IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM PackageUserResponses pur
                    WHERE pur.AssessmentSessionId = sess.Id AND pur.PackageQuestionId = pq.Id
                  )
            ");

            // Step 6: Drop legacy tables (data migrated above)
            migrationBuilder.DropTable(
                name: "UserResponses");

            migrationBuilder.DropTable(
                name: "AssessmentOptions");

            migrationBuilder.DropTable(
                name: "AssessmentQuestions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssessmentQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentSessionId = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScoreValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentQuestions", x => x.Id);
                    table.CheckConstraint("CK_AssessmentQuestion_ScoreValue", "[ScoreValue] > 0");
                    table.ForeignKey(
                        name: "FK_AssessmentQuestions_AssessmentSessions_AssessmentSessionId",
                        column: x => x.AssessmentSessionId,
                        principalTable: "AssessmentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentQuestionId = table.Column<int>(type: "int", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentOptions_AssessmentQuestions_AssessmentQuestionId",
                        column: x => x.AssessmentQuestionId,
                        principalTable: "AssessmentQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentQuestionId = table.Column<int>(type: "int", nullable: false),
                    AssessmentSessionId = table.Column<int>(type: "int", nullable: false),
                    SelectedOptionId = table.Column<int>(type: "int", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TextAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserResponses_AssessmentOptions_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "AssessmentOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserResponses_AssessmentQuestions_AssessmentQuestionId",
                        column: x => x.AssessmentQuestionId,
                        principalTable: "AssessmentQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserResponses_AssessmentSessions_AssessmentSessionId",
                        column: x => x.AssessmentSessionId,
                        principalTable: "AssessmentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentOptions_AssessmentQuestionId",
                table: "AssessmentOptions",
                column: "AssessmentQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentQuestions_AssessmentSessionId",
                table: "AssessmentQuestions",
                column: "AssessmentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentQuestions_Order",
                table: "AssessmentQuestions",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_UserResponses_AssessmentQuestionId",
                table: "UserResponses",
                column: "AssessmentQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserResponses_AssessmentSessionId_AssessmentQuestionId",
                table: "UserResponses",
                columns: new[] { "AssessmentSessionId", "AssessmentQuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserResponses_SelectedOptionId",
                table: "UserResponses",
                column: "SelectedOptionId");
        }
    }
}
