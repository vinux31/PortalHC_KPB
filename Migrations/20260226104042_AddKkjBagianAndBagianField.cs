using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddKkjBagianAndBagianField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bagian",
                table: "KkjMatrices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "KkjBagians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Label_SectionHead = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_SrSpv_GSH = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_ShiftSpv_GSH = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_Panelman_GSH_12_13 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_Panelman_GSH_14 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_Operator_GSH_8_11 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_Operator_GSH_12_13 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_ShiftSpv_ARU = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_Panelman_ARU_12_13 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_Panelman_ARU_14 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_Operator_ARU_8_11 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_Operator_ARU_12_13 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_SrSpv_Facility = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_JrAnalyst = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_HSE = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KkjBagians", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KkjBagians_DisplayOrder",
                table: "KkjBagians",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_KkjBagians_Name",
                table: "KkjBagians",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KkjBagians");

            migrationBuilder.DropColumn(
                name: "Bagian",
                table: "KkjMatrices");
        }
    }
}
