using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class CleanupWaitCertificateStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE TrainingRecords SET Status = 'Passed' WHERE Status = 'Wait Certificate';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort: not all Passed records originated from Wait Certificate
            migrationBuilder.Sql("UPDATE TrainingRecords SET Status = 'Wait Certificate' WHERE Status = 'Passed';");
        }
    }
}
