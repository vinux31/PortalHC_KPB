using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingProtonBypassAndAssignmentOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "ProtonTrackAssignments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PendingProtonBypasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoacheeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceProtonTrackId = table.Column<int>(type: "int", nullable: false),
                    TargetProtonTrackId = table.Column<int>(type: "int", nullable: false),
                    TargetUnit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetCoachId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkedAssessmentSessionId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InitiatedById = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingProtonBypasses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingProtonBypasses_CoacheeId_Status",
                table: "PendingProtonBypasses",
                columns: new[] { "CoacheeId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingProtonBypasses");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "ProtonTrackAssignments");
        }
    }
}
