using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class RenameEstimatedEndTimeToScheduledTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EstimatedEndTime",
                table: "MaintenanceModes",
                newName: "ScheduledStartTime");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledEndTime",
                table: "MaintenanceModes",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledEndTime",
                table: "MaintenanceModes");

            migrationBuilder.RenameColumn(
                name: "ScheduledStartTime",
                table: "MaintenanceModes",
                newName: "EstimatedEndTime");
        }
    }
}
