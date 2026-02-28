using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAuthSourceField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthSource",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthSource",
                table: "Users",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }
    }
}
