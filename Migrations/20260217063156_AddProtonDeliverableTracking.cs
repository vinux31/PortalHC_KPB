using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddProtonDeliverableTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProtonKompetensiList",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NamaKompetensi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrackType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TahunKe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Urutan = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtonKompetensiList", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProtonTrackAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoacheeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignedById = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrackType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TahunKe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtonTrackAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProtonSubKompetensiList",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProtonKompetensiId = table.Column<int>(type: "int", nullable: false),
                    NamaSubKompetensi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Urutan = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtonSubKompetensiList", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProtonSubKompetensiList_ProtonKompetensiList_ProtonKompetensiId",
                        column: x => x.ProtonKompetensiId,
                        principalTable: "ProtonKompetensiList",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProtonDeliverableList",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProtonSubKompetensiId = table.Column<int>(type: "int", nullable: false),
                    NamaDeliverable = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Urutan = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtonDeliverableList", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProtonDeliverableList_ProtonSubKompetensiList_ProtonSubKompetensiId",
                        column: x => x.ProtonSubKompetensiId,
                        principalTable: "ProtonSubKompetensiList",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProtonDeliverableProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoacheeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProtonDeliverableId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EvidencePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenceFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtonDeliverableProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProtonDeliverableProgresses_ProtonDeliverableList_ProtonDeliverableId",
                        column: x => x.ProtonDeliverableId,
                        principalTable: "ProtonDeliverableList",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProtonDeliverableList_ProtonSubKompetensiId",
                table: "ProtonDeliverableList",
                column: "ProtonSubKompetensiId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtonDeliverableProgresses_CoacheeId",
                table: "ProtonDeliverableProgresses",
                column: "CoacheeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtonDeliverableProgresses_CoacheeId_ProtonDeliverableId",
                table: "ProtonDeliverableProgresses",
                columns: new[] { "CoacheeId", "ProtonDeliverableId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProtonDeliverableProgresses_ProtonDeliverableId",
                table: "ProtonDeliverableProgresses",
                column: "ProtonDeliverableId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtonDeliverableProgresses_Status",
                table: "ProtonDeliverableProgresses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProtonSubKompetensiList_ProtonKompetensiId",
                table: "ProtonSubKompetensiList",
                column: "ProtonKompetensiId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtonTrackAssignments_CoacheeId",
                table: "ProtonTrackAssignments",
                column: "CoacheeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtonTrackAssignments_CoacheeId_IsActive",
                table: "ProtonTrackAssignments",
                columns: new[] { "CoacheeId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProtonDeliverableProgresses");

            migrationBuilder.DropTable(
                name: "ProtonTrackAssignments");

            migrationBuilder.DropTable(
                name: "ProtonDeliverableList");

            migrationBuilder.DropTable(
                name: "ProtonSubKompetensiList");

            migrationBuilder.DropTable(
                name: "ProtonKompetensiList");
        }
    }
}
