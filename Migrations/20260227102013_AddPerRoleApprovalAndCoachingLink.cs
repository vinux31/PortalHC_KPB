using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddPerRoleApprovalAndCoachingLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShApprovalStatus",
                table: "ProtonDeliverableProgresses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ShApprovedAt",
                table: "ProtonDeliverableProgresses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShApprovedById",
                table: "ProtonDeliverableProgresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SrSpvApprovalStatus",
                table: "ProtonDeliverableProgresses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "SrSpvApprovedAt",
                table: "ProtonDeliverableProgresses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SrSpvApprovedById",
                table: "ProtonDeliverableProgresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProtonDeliverableProgressId",
                table: "CoachingSessions",
                type: "int",
                nullable: true);

            // Data migration: convert Locked/Active to Pending
            migrationBuilder.Sql("UPDATE [ProtonDeliverableProgresses] SET [Status] = 'Pending' WHERE [Status] = 'Locked'");
            migrationBuilder.Sql("UPDATE [ProtonDeliverableProgresses] SET [Status] = 'Pending' WHERE [Status] = 'Active'");
            // Backfill existing approved records to SrSpv column
            migrationBuilder.Sql("UPDATE [ProtonDeliverableProgresses] SET [SrSpvApprovalStatus] = 'Approved', [SrSpvApprovedById] = [ApprovedById], [SrSpvApprovedAt] = [ApprovedAt] WHERE [Status] = 'Approved' AND [ApprovedById] IS NOT NULL");
            // Ensure new columns have correct default for existing rows
            migrationBuilder.Sql("UPDATE [ProtonDeliverableProgresses] SET [SrSpvApprovalStatus] = 'Pending' WHERE [SrSpvApprovalStatus] = ''");
            migrationBuilder.Sql("UPDATE [ProtonDeliverableProgresses] SET [ShApprovalStatus] = 'Pending' WHERE [ShApprovalStatus] = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShApprovalStatus",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.DropColumn(
                name: "ShApprovedAt",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.DropColumn(
                name: "ShApprovedById",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.DropColumn(
                name: "SrSpvApprovalStatus",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.DropColumn(
                name: "SrSpvApprovedAt",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.DropColumn(
                name: "SrSpvApprovedById",
                table: "ProtonDeliverableProgresses");

            migrationBuilder.DropColumn(
                name: "ProtonDeliverableProgressId",
                table: "CoachingSessions");
        }
    }
}
