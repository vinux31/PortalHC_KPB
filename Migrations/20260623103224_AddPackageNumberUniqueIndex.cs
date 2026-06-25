using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageNumberUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // STEP 1 — DEDUP/RENUMBER duplikat PackageNumber existing DULU (else CreateIndex unique gagal
            // pada data lama akibat bug count-based existingCount+1 + DeletePackage tak renumber).
            // ROW_NUMBER per session by (PackageNumber, Id) -> assign sequential 1..N gap-free per session.
            // Literal statik, NO user input (idiom AddUserUnitsTable.cs). Idempotent (re-run = numbering sama).
            migrationBuilder.Sql(@"
                WITH Numbered AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY AssessmentSessionId ORDER BY PackageNumber, Id) AS rn
                    FROM AssessmentPackages
                )
                UPDATE p
                SET p.PackageNumber = n.rn
                FROM AssessmentPackages p
                INNER JOIN Numbered n ON p.Id = n.Id;
            ");

            // STEP 2 — plain UNIQUE index (PackageNumber NON-nullable -> NO filter, Pitfall 2).
            migrationBuilder.CreateIndex(
                name: "IX_AssessmentPackages_SessionId_PackageNumber_Unique",
                table: "AssessmentPackages",
                columns: new[] { "AssessmentSessionId", "PackageNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssessmentPackages_SessionId_PackageNumber_Unique",
                table: "AssessmentPackages");
            // Renumber TIDAK di-revert (data-fix permanen, aman).
        }
    }
}
