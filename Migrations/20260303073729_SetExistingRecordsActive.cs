using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class SetExistingRecordsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set all existing records to active — new column defaults to 0 but semantics require true
            migrationBuilder.Sql("UPDATE Users SET IsActive = 1");
            migrationBuilder.Sql("UPDATE ProtonKompetensiList SET IsActive = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Users SET IsActive = 0");
            migrationBuilder.Sql("UPDATE ProtonKompetensiList SET IsActive = 0");
        }
    }
}
