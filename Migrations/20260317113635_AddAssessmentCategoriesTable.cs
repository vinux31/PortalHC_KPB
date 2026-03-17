using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentCategoriesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssessmentCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DefaultPassPercentage = table.Column<int>(type: "int", nullable: false, defaultValue: 70),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCategories_Name",
                table: "AssessmentCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCategories_SortOrder",
                table: "AssessmentCategories",
                column: "SortOrder");

            migrationBuilder.Sql(@"
                WITH Expected AS (
                    SELECT 'OJT' AS Name, 70 AS DefaultPassPercentage, 1 AS SortOrder
                    UNION ALL SELECT 'IHT', 70, 2
                    UNION ALL SELECT 'Training Licencor', 80, 3
                    UNION ALL SELECT 'OTS', 70, 4
                    UNION ALL SELECT 'Mandatory HSSE Training', 100, 5
                    UNION ALL SELECT 'Assessment Proton', 70, 6
                )
                MERGE INTO AssessmentCategories ac
                USING Expected e ON ac.Name = e.Name
                WHEN NOT MATCHED THEN
                    INSERT (Name, DefaultPassPercentage, IsActive, SortOrder)
                    VALUES (e.Name, e.DefaultPassPercentage, 1, e.SortOrder);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentCategories");
        }
    }
}
