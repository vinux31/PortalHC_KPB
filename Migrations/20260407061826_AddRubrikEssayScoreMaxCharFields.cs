using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddRubrikEssayScoreMaxCharFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EssayScore",
                table: "PackageUserResponses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxCharacters",
                table: "PackageQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Rubrik",
                table: "PackageQuestions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EssayScore",
                table: "PackageUserResponses");

            migrationBuilder.DropColumn(
                name: "MaxCharacters",
                table: "PackageQuestions");

            migrationBuilder.DropColumn(
                name: "Rubrik",
                table: "PackageQuestions");
        }
    }
}
