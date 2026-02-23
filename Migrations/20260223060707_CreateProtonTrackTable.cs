using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class CreateProtonTrackTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1 — Create ProtonTracks table
            migrationBuilder.CreateTable(
                name: "ProtonTracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TahunKe = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Urutan = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtonTracks", x => x.Id);
                    table.UniqueConstraint("AK_ProtonTracks_TrackType_TahunKe", x => new { x.TrackType, x.TahunKe });
                });

            // Step 2 — Add nullable FK columns to both child tables
            migrationBuilder.AddColumn<int>(
                name: "ProtonTrackId",
                table: "ProtonKompetensiList",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProtonTrackId",
                table: "ProtonTrackAssignments",
                type: "int",
                nullable: true);

            // Step 3 — Create indexes for FK columns
            migrationBuilder.CreateIndex(
                name: "IX_ProtonKompetensiList_ProtonTrackId",
                table: "ProtonKompetensiList",
                column: "ProtonTrackId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtonTrackAssignments_ProtonTrackId",
                table: "ProtonTrackAssignments",
                column: "ProtonTrackId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtonTracks_TrackType_TahunKe",
                table: "ProtonTracks",
                columns: new[] { "TrackType", "TahunKe" },
                unique: true);

            // Step 4 — Add FK constraints (while columns are still nullable)
            migrationBuilder.AddForeignKey(
                name: "FK_ProtonKompetensiList_ProtonTracks_ProtonTrackId",
                table: "ProtonKompetensiList",
                column: "ProtonTrackId",
                principalTable: "ProtonTracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProtonTrackAssignments_ProtonTracks_ProtonTrackId",
                table: "ProtonTrackAssignments",
                column: "ProtonTrackId",
                principalTable: "ProtonTracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Step 5 — Seed 6 ProtonTrack rows via defensive MERGE (handles any data drift)
            migrationBuilder.Sql(@"
                WITH ExpectedTracks AS (
                    SELECT 'Panelman' AS TrackType, 'Tahun 1' AS TahunKe, 'Panelman - Tahun 1' AS DisplayName, 1 AS Urutan
                    UNION ALL SELECT 'Panelman', 'Tahun 2', 'Panelman - Tahun 2', 2
                    UNION ALL SELECT 'Panelman', 'Tahun 3', 'Panelman - Tahun 3', 3
                    UNION ALL SELECT 'Operator', 'Tahun 1', 'Operator - Tahun 1', 4
                    UNION ALL SELECT 'Operator', 'Tahun 2', 'Operator - Tahun 2', 5
                    UNION ALL SELECT 'Operator', 'Tahun 3', 'Operator - Tahun 3', 6
                )
                MERGE INTO ProtonTracks pt
                USING ExpectedTracks et ON pt.TrackType = et.TrackType AND pt.TahunKe = et.TahunKe
                WHEN NOT MATCHED THEN
                    INSERT (TrackType, TahunKe, DisplayName, Urutan)
                    VALUES (et.TrackType, et.TahunKe, et.DisplayName, et.Urutan);
            ");

            // Step 6 — Backfill ProtonKompetensiList.ProtonTrackId
            migrationBuilder.Sql(@"
                UPDATE pk
                SET pk.ProtonTrackId = pt.Id
                FROM ProtonKompetensiList pk
                INNER JOIN ProtonTracks pt ON pk.TrackType = pt.TrackType AND pk.TahunKe = pt.TahunKe
                WHERE pk.ProtonTrackId IS NULL;
            ");

            // Step 7 — Backfill ProtonTrackAssignments.ProtonTrackId
            migrationBuilder.Sql(@"
                UPDATE pta
                SET pta.ProtonTrackId = pt.Id
                FROM ProtonTrackAssignments pta
                INNER JOIN ProtonTracks pt ON pta.TrackType = pt.TrackType AND pta.TahunKe = pt.TahunKe
                WHERE pta.ProtonTrackId IS NULL;
            ");

            // Step 8 — Validate no NULLs remain (fail loudly if backfill incomplete)
            migrationBuilder.Sql(@"
                IF (SELECT COUNT(*) FROM ProtonKompetensiList WHERE ProtonTrackId IS NULL) > 0
                    RAISERROR('ProtonKompetensiList has NULL ProtonTrackId after backfill — migration aborted', 16, 1);
                IF (SELECT COUNT(*) FROM ProtonTrackAssignments WHERE ProtonTrackId IS NULL) > 0
                    RAISERROR('ProtonTrackAssignments has NULL ProtonTrackId after backfill — migration aborted', 16, 1);
            ");

            // Step 9 — Make FK columns NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "ProtonTrackId",
                table: "ProtonKompetensiList",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProtonTrackId",
                table: "ProtonTrackAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // Step 10 — Drop old string columns from both tables
            migrationBuilder.DropColumn(
                name: "TrackType",
                table: "ProtonKompetensiList");

            migrationBuilder.DropColumn(
                name: "TahunKe",
                table: "ProtonKompetensiList");

            migrationBuilder.DropColumn(
                name: "TrackType",
                table: "ProtonTrackAssignments");

            migrationBuilder.DropColumn(
                name: "TahunKe",
                table: "ProtonTrackAssignments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore TrackType+TahunKe columns as nullable (data in old string cols is not restored on rollback)
            migrationBuilder.AddColumn<string>(
                name: "TrackType",
                table: "ProtonKompetensiList",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TahunKe",
                table: "ProtonKompetensiList",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackType",
                table: "ProtonTrackAssignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TahunKe",
                table: "ProtonTrackAssignments",
                type: "nvarchar(max)",
                nullable: true);

            // Drop FK constraints
            migrationBuilder.DropForeignKey(
                name: "FK_ProtonKompetensiList_ProtonTracks_ProtonTrackId",
                table: "ProtonKompetensiList");

            migrationBuilder.DropForeignKey(
                name: "FK_ProtonTrackAssignments_ProtonTracks_ProtonTrackId",
                table: "ProtonTrackAssignments");

            // Drop ProtonTrackId indexes
            migrationBuilder.DropIndex(
                name: "IX_ProtonKompetensiList_ProtonTrackId",
                table: "ProtonKompetensiList");

            migrationBuilder.DropIndex(
                name: "IX_ProtonTrackAssignments_ProtonTrackId",
                table: "ProtonTrackAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ProtonTracks_TrackType_TahunKe",
                table: "ProtonTracks");

            // Drop ProtonTrackId columns
            migrationBuilder.DropColumn(
                name: "ProtonTrackId",
                table: "ProtonKompetensiList");

            migrationBuilder.DropColumn(
                name: "ProtonTrackId",
                table: "ProtonTrackAssignments");

            // Drop ProtonTracks table
            migrationBuilder.DropTable(
                name: "ProtonTracks");
        }
    }
}
