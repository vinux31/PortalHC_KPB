using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class MoveSectionHeadToLevel4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data migration: SectionHead users RoleLevel 3 → 4 (section-scoped access)
            migrationBuilder.Sql(@"
                UPDATE Users
                SET RoleLevel = 4
                WHERE Id IN (
                    SELECT u.Id FROM Users u
                    INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                    INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                    WHERE r.Name = 'Section Head'
                )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Users
                SET RoleLevel = 3
                WHERE Id IN (
                    SELECT u.Id FROM Users u
                    INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                    INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                    WHERE r.Name = 'Section Head'
                )
            ");
        }
    }
}
