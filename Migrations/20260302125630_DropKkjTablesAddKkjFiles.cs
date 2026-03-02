using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class DropKkjTablesAddKkjFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentCompetencyMaps_KkjMatrices_KkjMatrixItemId",
                table: "AssessmentCompetencyMaps");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCompetencyLevels_KkjMatrices_KkjMatrixItemId",
                table: "UserCompetencyLevels");

            migrationBuilder.DropTable(
                name: "KkjTargetValues");

            migrationBuilder.DropTable(
                name: "PositionColumnMappings");

            migrationBuilder.DropTable(
                name: "KkjMatrices");

            migrationBuilder.DropTable(
                name: "KkjColumns");

            migrationBuilder.DropIndex(
                name: "IX_UserCompetencyLevels_KkjMatrixItemId",
                table: "UserCompetencyLevels");

            migrationBuilder.DropIndex(
                name: "IX_UserCompetencyLevels_UserId_KkjMatrixItemId",
                table: "UserCompetencyLevels");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentCompetencyMaps_KkjMatrixItemId",
                table: "AssessmentCompetencyMaps");

            migrationBuilder.CreateTable(
                name: "KkjFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BagianId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Keterangan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UploaderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KkjFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KkjFiles_KkjBagians_BagianId",
                        column: x => x.BagianId,
                        principalTable: "KkjBagians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCompetencyLevels_UserId",
                table: "UserCompetencyLevels",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_KkjFiles_BagianId",
                table: "KkjFiles",
                column: "BagianId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KkjFiles");

            migrationBuilder.DropIndex(
                name: "IX_UserCompetencyLevels_UserId",
                table: "UserCompetencyLevels");

            migrationBuilder.CreateTable(
                name: "KkjColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BagianId = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KkjColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KkjColumns_KkjBagians_BagianId",
                        column: x => x.BagianId,
                        principalTable: "KkjBagians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KkjMatrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Bagian = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Indeks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kompetensi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    No = table.Column<int>(type: "int", nullable: false),
                    SkillGroup = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubSkillGroup = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KkjMatrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PositionColumnMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KkjColumnId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionColumnMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionColumnMappings_KkjColumns_KkjColumnId",
                        column: x => x.KkjColumnId,
                        principalTable: "KkjColumns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KkjTargetValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KkjColumnId = table.Column<int>(type: "int", nullable: false),
                    KkjMatrixItemId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KkjTargetValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KkjTargetValues_KkjColumns_KkjColumnId",
                        column: x => x.KkjColumnId,
                        principalTable: "KkjColumns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KkjTargetValues_KkjMatrices_KkjMatrixItemId",
                        column: x => x.KkjMatrixItemId,
                        principalTable: "KkjMatrices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCompetencyLevels_KkjMatrixItemId",
                table: "UserCompetencyLevels",
                column: "KkjMatrixItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompetencyLevels_UserId_KkjMatrixItemId",
                table: "UserCompetencyLevels",
                columns: new[] { "UserId", "KkjMatrixItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCompetencyMaps_KkjMatrixItemId",
                table: "AssessmentCompetencyMaps",
                column: "KkjMatrixItemId");

            migrationBuilder.CreateIndex(
                name: "IX_KkjColumns_BagianId_DisplayOrder",
                table: "KkjColumns",
                columns: new[] { "BagianId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_KkjColumns_BagianId_Name",
                table: "KkjColumns",
                columns: new[] { "BagianId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KkjTargetValues_KkjColumnId",
                table: "KkjTargetValues",
                column: "KkjColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_KkjTargetValues_KkjMatrixItemId_KkjColumnId",
                table: "KkjTargetValues",
                columns: new[] { "KkjMatrixItemId", "KkjColumnId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionColumnMappings_KkjColumnId",
                table: "PositionColumnMappings",
                column: "KkjColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionColumnMappings_Position",
                table: "PositionColumnMappings",
                column: "Position");

            migrationBuilder.CreateIndex(
                name: "IX_PositionColumnMappings_Position_KkjColumnId",
                table: "PositionColumnMappings",
                columns: new[] { "Position", "KkjColumnId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentCompetencyMaps_KkjMatrices_KkjMatrixItemId",
                table: "AssessmentCompetencyMaps",
                column: "KkjMatrixItemId",
                principalTable: "KkjMatrices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCompetencyLevels_KkjMatrices_KkjMatrixItemId",
                table: "UserCompetencyLevels",
                column: "KkjMatrixItemId",
                principalTable: "KkjMatrices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
