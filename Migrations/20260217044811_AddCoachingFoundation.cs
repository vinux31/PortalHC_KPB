using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrackingItemId",
                table: "CoachingLogs");

            migrationBuilder.CreateTable(
                name: "CoachCoacheeMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CoacheeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachCoacheeMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CoachingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CoacheeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachingSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachingSessionId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionItems_CoachingSessions_CoachingSessionId",
                        column: x => x.CoachingSessionId,
                        principalTable: "CoachingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_CoachingSessionId",
                table: "ActionItems",
                column: "CoachingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_Status",
                table: "ActionItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CoachCoacheeMappings_CoacheeId",
                table: "CoachCoacheeMappings",
                column: "CoacheeId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachCoacheeMappings_CoachId",
                table: "CoachCoacheeMappings",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachCoacheeMappings_CoachId_CoacheeId",
                table: "CoachCoacheeMappings",
                columns: new[] { "CoachId", "CoacheeId" });

            migrationBuilder.CreateIndex(
                name: "IX_CoachingSessions_CoacheeId",
                table: "CoachingSessions",
                column: "CoacheeId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachingSessions_CoacheeId_Date",
                table: "CoachingSessions",
                columns: new[] { "CoacheeId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_CoachingSessions_CoachId",
                table: "CoachingSessions",
                column: "CoachId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionItems");

            migrationBuilder.DropTable(
                name: "CoachCoacheeMappings");

            migrationBuilder.DropTable(
                name: "CoachingSessions");

            migrationBuilder.AddColumn<int>(
                name: "TrackingItemId",
                table: "CoachingLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
