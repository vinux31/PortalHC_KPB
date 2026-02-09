using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddAllEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssessmentSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Schedule = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Progress = table.Column<int>(type: "int", nullable: false),
                    BannerColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: true),
                    IsTokenRequired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachingLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackingItemId = table.Column<int>(type: "int", nullable: false),
                    CoachId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CoachName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoachPosition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoacheeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CoacheeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubKompetensi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Deliverables = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tanggal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CoacheeCompetencies = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CatatanCoach = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kesimpulan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachingLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CpdpItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    No = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NamaKompetensi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndikatorPerilaku = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetailIndikator = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Silabus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetDeliverable = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CpdpItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdpItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Kompetensi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubKompetensi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deliverable = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Aktivitas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Evidence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApproveSrSpv = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApproveSectionHead = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApproveHC = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KkjMatrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    No = table.Column<int>(type: "int", nullable: false),
                    SkillGroup = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubSkillGroup = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Indeks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kompetensi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target_SectionHead = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target_SrSpv_GSH = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target_ShiftSpv_GSH = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target_Panelman_GSH_12_13 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target_Panelman_GSH_14 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target_Operator_GSH_8_11 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target_Operator_GSH_12_13 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target_ShiftSpv_ARU = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target_Panelman_ARU_12_13 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target_Panelman_ARU_14 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target_Operator_ARU_8_11 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target_Operator_ARU_12_13 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target_SrSpv_Facility = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target_JrAnalyst = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target_HSE = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KkjMatrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Judul = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kategori = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tanggal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Penyelenggara = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SertifikatUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CertificateType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSessions_UserId",
                table: "AssessmentSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachingLogs_CoacheeId",
                table: "CoachingLogs",
                column: "CoacheeId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachingLogs_CoachId",
                table: "CoachingLogs",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpItems_UserId",
                table: "IdpItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecords_UserId",
                table: "TrainingRecords",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentSessions");

            migrationBuilder.DropTable(
                name: "CoachingLogs");

            migrationBuilder.DropTable(
                name: "CpdpItems");

            migrationBuilder.DropTable(
                name: "IdpItems");

            migrationBuilder.DropTable(
                name: "KkjMatrices");

            migrationBuilder.DropTable(
                name: "TrainingRecords");
        }
    }
}
