using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddRenewalChainFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RenewsSessionId",
                table: "TrainingRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RenewsTrainingId",
                table: "TrainingRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RenewsSessionId",
                table: "AssessmentSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RenewsTrainingId",
                table: "AssessmentSessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecords_RenewsSessionId",
                table: "TrainingRecords",
                column: "RenewsSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecords_RenewsTrainingId",
                table: "TrainingRecords",
                column: "RenewsTrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSessions_RenewsSessionId",
                table: "AssessmentSessions",
                column: "RenewsSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSessions_RenewsTrainingId",
                table: "AssessmentSessions",
                column: "RenewsTrainingId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentSessions_AssessmentSessions_RenewsSessionId",
                table: "AssessmentSessions",
                column: "RenewsSessionId",
                principalTable: "AssessmentSessions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentSessions_TrainingRecords_RenewsTrainingId",
                table: "AssessmentSessions",
                column: "RenewsTrainingId",
                principalTable: "TrainingRecords",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingRecords_AssessmentSessions_RenewsSessionId",
                table: "TrainingRecords",
                column: "RenewsSessionId",
                principalTable: "AssessmentSessions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingRecords_TrainingRecords_RenewsTrainingId",
                table: "TrainingRecords",
                column: "RenewsTrainingId",
                principalTable: "TrainingRecords",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentSessions_AssessmentSessions_RenewsSessionId",
                table: "AssessmentSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentSessions_TrainingRecords_RenewsTrainingId",
                table: "AssessmentSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingRecords_AssessmentSessions_RenewsSessionId",
                table: "TrainingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingRecords_TrainingRecords_RenewsTrainingId",
                table: "TrainingRecords");

            migrationBuilder.DropIndex(
                name: "IX_TrainingRecords_RenewsSessionId",
                table: "TrainingRecords");

            migrationBuilder.DropIndex(
                name: "IX_TrainingRecords_RenewsTrainingId",
                table: "TrainingRecords");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentSessions_RenewsSessionId",
                table: "AssessmentSessions");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentSessions_RenewsTrainingId",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "RenewsSessionId",
                table: "TrainingRecords");

            migrationBuilder.DropColumn(
                name: "RenewsTrainingId",
                table: "TrainingRecords");

            migrationBuilder.DropColumn(
                name: "RenewsSessionId",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "RenewsTrainingId",
                table: "AssessmentSessions");
        }
    }
}
