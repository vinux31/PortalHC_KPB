using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddProtonSilabusAndGuidance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bagian",
                table: "ProtonKompetensiList",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "ProtonKompetensiList",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // Clean old Proton data — structure is being rebuilt with Bagian+Unit scoping
            migrationBuilder.Sql("DELETE FROM ProtonDeliverableProgresses");
            migrationBuilder.Sql("DELETE FROM ProtonDeliverableList");
            migrationBuilder.Sql("DELETE FROM ProtonSubKompetensiList");
            migrationBuilder.Sql("DELETE FROM ProtonKompetensiList");

            migrationBuilder.CreateTable(
                name: "CoachingGuidanceFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Bagian = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProtonTrackId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedById = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachingGuidanceFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachingGuidanceFiles_ProtonTracks_ProtonTrackId",
                        column: x => x.ProtonTrackId,
                        principalTable: "ProtonTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachingGuidanceFiles_Bagian_Unit_ProtonTrackId",
                table: "CoachingGuidanceFiles",
                columns: new[] { "Bagian", "Unit", "ProtonTrackId" });

            migrationBuilder.CreateIndex(
                name: "IX_CoachingGuidanceFiles_ProtonTrackId",
                table: "CoachingGuidanceFiles",
                column: "ProtonTrackId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachingGuidanceFiles");

            migrationBuilder.DropColumn(
                name: "Bagian",
                table: "ProtonKompetensiList");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "ProtonKompetensiList");
        }
    }
}
