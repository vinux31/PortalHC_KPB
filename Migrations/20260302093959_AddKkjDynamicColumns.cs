using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddKkjDynamicColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Target_HSE",
                table: "KkjMatrices");

            migrationBuilder.DropColumn(
                name: "Target_JrAnalyst",
                table: "KkjMatrices");

            migrationBuilder.DropColumn(
                name: "Target_Operator_ARU_12_13",
                table: "KkjMatrices");

            migrationBuilder.DropColumn(
                name: "Target_Operator_ARU_8_11",
                table: "KkjMatrices");

            migrationBuilder.DropColumn(
                name: "Target_Operator_GSH_12_13",
                table: "KkjMatrices");

            migrationBuilder.DropColumn(
                name: "Target_Operator_GSH_8_11",
                table: "KkjMatrices");

            migrationBuilder.DropColumn(
                name: "Target_Panelman_ARU_12_13",
                table: "KkjMatrices");

            migrationBuilder.DropColumn(
                name: "Target_Panelman_ARU_14",
                table: "KkjMatrices");

            migrationBuilder.DropColumn(
                name: "Target_Panelman_GSH_12_13",
                table: "KkjMatrices");

            migrationBuilder.DropColumn(
                name: "Target_Panelman_GSH_14",
                table: "KkjMatrices");

            migrationBuilder.DropColumn(
                name: "Target_SectionHead",
                table: "KkjMatrices");

            migrationBuilder.DropColumn(
                name: "Target_ShiftSpv_ARU",
                table: "KkjMatrices");

            migrationBuilder.DropColumn(
                name: "Target_ShiftSpv_GSH",
                table: "KkjMatrices");

            migrationBuilder.DropColumn(
                name: "Target_SrSpv_Facility",
                table: "KkjMatrices");

            migrationBuilder.DropColumn(
                name: "Target_SrSpv_GSH",
                table: "KkjMatrices");

            migrationBuilder.DropColumn(
                name: "Label_HSE",
                table: "KkjBagians");

            migrationBuilder.DropColumn(
                name: "Label_JrAnalyst",
                table: "KkjBagians");

            migrationBuilder.DropColumn(
                name: "Label_Operator_ARU_12_13",
                table: "KkjBagians");

            migrationBuilder.DropColumn(
                name: "Label_Operator_ARU_8_11",
                table: "KkjBagians");

            migrationBuilder.DropColumn(
                name: "Label_Operator_GSH_12_13",
                table: "KkjBagians");

            migrationBuilder.DropColumn(
                name: "Label_Operator_GSH_8_11",
                table: "KkjBagians");

            migrationBuilder.DropColumn(
                name: "Label_Panelman_ARU_12_13",
                table: "KkjBagians");

            migrationBuilder.DropColumn(
                name: "Label_Panelman_ARU_14",
                table: "KkjBagians");

            migrationBuilder.DropColumn(
                name: "Label_Panelman_GSH_12_13",
                table: "KkjBagians");

            migrationBuilder.DropColumn(
                name: "Label_Panelman_GSH_14",
                table: "KkjBagians");

            migrationBuilder.DropColumn(
                name: "Label_SectionHead",
                table: "KkjBagians");

            migrationBuilder.DropColumn(
                name: "Label_ShiftSpv_ARU",
                table: "KkjBagians");

            migrationBuilder.DropColumn(
                name: "Label_ShiftSpv_GSH",
                table: "KkjBagians");

            migrationBuilder.DropColumn(
                name: "Label_SrSpv_Facility",
                table: "KkjBagians");

            migrationBuilder.DropColumn(
                name: "Label_SrSpv_GSH",
                table: "KkjBagians");

            migrationBuilder.CreateTable(
                name: "KkjColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BagianId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
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
                name: "KkjTargetValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KkjMatrixItemId = table.Column<int>(type: "int", nullable: false),
                    KkjColumnId = table.Column<int>(type: "int", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "PositionColumnMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Position = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    KkjColumnId = table.Column<int>(type: "int", nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KkjTargetValues");

            migrationBuilder.DropTable(
                name: "PositionColumnMappings");

            migrationBuilder.DropTable(
                name: "KkjColumns");

            migrationBuilder.AddColumn<string>(
                name: "Target_HSE",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Target_JrAnalyst",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Target_Operator_ARU_12_13",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Target_Operator_ARU_8_11",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Target_Operator_GSH_12_13",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Target_Operator_GSH_8_11",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Target_Panelman_ARU_12_13",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Target_Panelman_ARU_14",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Target_Panelman_GSH_12_13",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Target_Panelman_GSH_14",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Target_SectionHead",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Target_ShiftSpv_ARU",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Target_ShiftSpv_GSH",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Target_SrSpv_Facility",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Target_SrSpv_GSH",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label_HSE",
                table: "KkjBagians",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label_JrAnalyst",
                table: "KkjBagians",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label_Operator_ARU_12_13",
                table: "KkjBagians",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label_Operator_ARU_8_11",
                table: "KkjBagians",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label_Operator_GSH_12_13",
                table: "KkjBagians",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label_Operator_GSH_8_11",
                table: "KkjBagians",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label_Panelman_ARU_12_13",
                table: "KkjBagians",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label_Panelman_ARU_14",
                table: "KkjBagians",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label_Panelman_GSH_12_13",
                table: "KkjBagians",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label_Panelman_GSH_14",
                table: "KkjBagians",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label_SectionHead",
                table: "KkjBagians",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label_ShiftSpv_ARU",
                table: "KkjBagians",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label_ShiftSpv_GSH",
                table: "KkjBagians",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label_SrSpv_Facility",
                table: "KkjBagians",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label_SrSpv_GSH",
                table: "KkjBagians",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
