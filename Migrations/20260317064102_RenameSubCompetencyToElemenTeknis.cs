using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class RenameSubCompetencyToElemenTeknis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubCompetency",
                table: "PackageQuestions",
                newName: "ElemenTeknis");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ElemenTeknis",
                table: "PackageQuestions",
                newName: "SubCompetency");
        }
    }
}
