using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class Phase236_UniqueAssignment_CompletedMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProtonFinalAssessments_ProtonTrackAssignmentId",
                table: "ProtonFinalAssessments");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "CoachCoacheeMappings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "CoachCoacheeMappings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ProtonFinalAssessments_ProtonTrackAssignmentId",
                table: "ProtonFinalAssessments",
                column: "ProtonTrackAssignmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProtonFinalAssessments_ProtonTrackAssignmentId",
                table: "ProtonFinalAssessments");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "CoachCoacheeMappings");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "CoachCoacheeMappings");

            migrationBuilder.CreateIndex(
                name: "IX_ProtonFinalAssessments_ProtonTrackAssignmentId",
                table: "ProtonFinalAssessments",
                column: "ProtonTrackAssignmentId");
        }
    }
}
