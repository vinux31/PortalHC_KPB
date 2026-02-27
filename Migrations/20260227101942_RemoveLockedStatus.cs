using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLockedStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE ProtonDeliverableProgresses SET Status = 'Active' WHERE Status = 'Locked'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
