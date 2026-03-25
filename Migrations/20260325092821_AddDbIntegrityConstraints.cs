using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddDbIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_TrainingRecord_RenewalChain",
                table: "TrainingRecords",
                sql: "[RenewsTrainingId] IS NULL OR [RenewsSessionId] IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentSession_RenewalChain",
                table: "AssessmentSessions",
                sql: "[RenewsSessionId] IS NULL OR [RenewsTrainingId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TrainingRecord_RenewalChain",
                table: "TrainingRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentSession_RenewalChain",
                table: "AssessmentSessions");
        }
    }
}
