using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentPackageSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SectionId",
                table: "PackageQuestions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssessmentPackageSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentPackageId = table.Column<int>(type: "int", nullable: false),
                    SectionNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartNewPage = table.Column<bool>(type: "bit", nullable: false),
                    ShuffleEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentPackageSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentPackageSections_AssessmentPackages_AssessmentPackageId",
                        column: x => x.AssessmentPackageId,
                        principalTable: "AssessmentPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageQuestions_SectionId",
                table: "PackageQuestions",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentPackageSections_AssessmentPackageId_SectionNumber",
                table: "AssessmentPackageSections",
                columns: new[] { "AssessmentPackageId", "SectionNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PackageQuestions_AssessmentPackageSections_SectionId",
                table: "PackageQuestions",
                column: "SectionId",
                principalTable: "AssessmentPackageSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PackageQuestions_AssessmentPackageSections_SectionId",
                table: "PackageQuestions");

            migrationBuilder.DropTable(
                name: "AssessmentPackageSections");

            migrationBuilder.DropIndex(
                name: "IX_PackageQuestions_SectionId",
                table: "PackageQuestions");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "PackageQuestions");
        }
    }
}
