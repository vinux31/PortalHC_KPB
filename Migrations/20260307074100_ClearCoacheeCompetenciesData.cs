using Microsoft.EntityFrameworkCore.Migrations;

namespace HcPortal.Migrations
{
    public partial class ClearCoacheeCompetenciesData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE CoachingSessions SET CoacheeCompetencies = ''");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data clearing is irreversible — no rollback possible
        }
    }
}
