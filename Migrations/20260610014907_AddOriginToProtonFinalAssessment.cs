using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddOriginToProtonFinalAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "ProtonFinalAssessments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Data-seed: semua penanda lama = interview Tahun 3 (pre-358). PLURAL table name.
            migrationBuilder.Sql("UPDATE ProtonFinalAssessments SET Origin = 'Interview' WHERE Origin IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Origin",
                table: "ProtonFinalAssessments");
        }
    }
}
