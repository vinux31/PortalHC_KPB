using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetToProtonSubKompetensi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Target",
                table: "ProtonSubKompetensiList",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("UPDATE ProtonSubKompetensiList SET Target = '-' WHERE Target IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Target",
                table: "ProtonSubKompetensiList");
        }
    }
}
