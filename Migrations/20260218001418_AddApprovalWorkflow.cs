using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovedById",
                table: "ProtonDeliverableProgresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HCApprovalStatus",
                table: "ProtonDeliverableProgresses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "HCReviewedAt",
                table: "ProtonDeliverableProgresses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HCReviewedById",
                table: "ProtonDeliverableProgresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ProtonDeliverableProgresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProtonFinalAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoacheeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProtonTrackAssignmentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompetencyLevelGranted = table.Column<int>(type: "int", nullable: false),
                    KkjMatrixItemId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtonFinalAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProtonFinalAssessments_ProtonTrackAssignments_ProtonTrackAssignmentId",
                        column: x => x.ProtonTrackAssignmentId,
                        principalTable: "ProtonTrackAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProtonNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CoacheeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CoacheeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtonNotifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProtonFinalAssessments_CoacheeId",
                table: "ProtonFinalAssessments",
                column: "CoacheeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtonFinalAssessments_CoacheeId_Status",
                table: "ProtonFinalAssessments",
                columns: new[] { "CoacheeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProtonFinalAssessments_ProtonTrackAssignmentId",
                table: "ProtonFinalAssessments",
                column: "ProtonTrackAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtonNotifications_CoacheeId",
                table: "ProtonNotifications",
                column: "CoacheeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtonNotifications_RecipientId",
                table: "ProtonNotifications",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtonNotifications_RecipientId_IsRead",
                table: "ProtonNotifications",
                columns: new[] { "RecipientId", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProtonFinalAssessments");

            migrationBuilder.DropTable(
                name: "ProtonNotifications");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.DropColumn(
                name: "HCApprovalStatus",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.DropColumn(
                name: "HCReviewedAt",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.DropColumn(
                name: "HCReviewedById",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ProtonDeliverableProgresses");
        }
    }
}
