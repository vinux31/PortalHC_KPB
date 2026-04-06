using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentV14Columns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL with IF NOT EXISTS to handle pre-existing columns
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PackageUserResponses' AND COLUMN_NAME='TextAnswer')
                    ALTER TABLE [PackageUserResponses] ADD [TextAnswer] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PackageQuestions' AND COLUMN_NAME='QuestionType')
                    ALTER TABLE [PackageQuestions] ADD [QuestionType] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssessmentSessions' AND COLUMN_NAME='AssessmentPhase')
                    ALTER TABLE [AssessmentSessions] ADD [AssessmentPhase] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssessmentSessions' AND COLUMN_NAME='AssessmentType')
                    ALTER TABLE [AssessmentSessions] ADD [AssessmentType] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssessmentSessions' AND COLUMN_NAME='HasManualGrading')
                    ALTER TABLE [AssessmentSessions] ADD [HasManualGrading] bit NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssessmentSessions' AND COLUMN_NAME='LinkedGroupId')
                    ALTER TABLE [AssessmentSessions] ADD [LinkedGroupId] int NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssessmentSessions' AND COLUMN_NAME='LinkedSessionId')
                    ALTER TABLE [AssessmentSessions] ADD [LinkedSessionId] int NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TextAnswer",
                table: "PackageUserResponses");

            migrationBuilder.DropColumn(
                name: "QuestionType",
                table: "PackageQuestions");

            migrationBuilder.DropColumn(
                name: "AssessmentPhase",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "AssessmentType",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "HasManualGrading",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "LinkedGroupId",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "LinkedSessionId",
                table: "AssessmentSessions");
        }
    }
}
