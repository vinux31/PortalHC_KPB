using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class DropCpdpItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CpdpItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CpdpItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DetailIndikator = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndikatorPerilaku = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NamaKompetensi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    No = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Section = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Silabus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetDeliverable = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CpdpItems", x => x.Id);
                });
        }
    }
}
