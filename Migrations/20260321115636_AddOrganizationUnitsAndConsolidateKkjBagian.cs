using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationUnitsAndConsolidateKkjBagian : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop FKs from KkjFiles/CpdpFiles to KkjBagians (so we can restructure)
            migrationBuilder.DropForeignKey(
                name: "FK_CpdpFiles_KkjBagians_BagianId",
                table: "CpdpFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_KkjFiles_KkjBagians_BagianId",
                table: "KkjFiles");

            // Step 2: Create OrganizationUnits table
            migrationBuilder.CreateTable(
                name: "OrganizationUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationUnits_OrganizationUnits_ParentId",
                        column: x => x.ParentId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnits_Name",
                table: "OrganizationUnits",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnits_ParentId_DisplayOrder",
                table: "OrganizationUnits",
                columns: new[] { "ParentId", "DisplayOrder" });

            // Step 3: Seed 4 Bagian (Level 0) and 17 Unit (Level 1)
            migrationBuilder.Sql(@"
                -- Seed Bagian (Level 0)
                INSERT INTO OrganizationUnits (Name, ParentId, Level, DisplayOrder, IsActive)
                VALUES
                    ('RFCC',      NULL, 0, 1, 1),
                    ('DHT / HMU', NULL, 0, 2, 1),
                    ('NGP',       NULL, 0, 3, 1),
                    ('GAST',      NULL, 0, 4, 1);

                -- Seed Unit RFCC (Level 1)
                INSERT INTO OrganizationUnits (Name, ParentId, Level, DisplayOrder, IsActive)
                VALUES
                    ('RFCC LPG Treating Unit (062)',  (SELECT Id FROM OrganizationUnits WHERE Name = 'RFCC'), 1, 1, 1),
                    ('Propylene Recovery Unit (063)', (SELECT Id FROM OrganizationUnits WHERE Name = 'RFCC'), 1, 2, 1);

                -- Seed Unit DHT / HMU (Level 1)
                INSERT INTO OrganizationUnits (Name, ParentId, Level, DisplayOrder, IsActive)
                VALUES
                    ('Diesel Hydrotreating Unit I & II (054 & 083)', (SELECT Id FROM OrganizationUnits WHERE Name = 'DHT / HMU'), 1, 1, 1),
                    ('Hydrogen Manufacturing Unit (068)',            (SELECT Id FROM OrganizationUnits WHERE Name = 'DHT / HMU'), 1, 2, 1),
                    ('Common DHT H2 Compressor (085)',               (SELECT Id FROM OrganizationUnits WHERE Name = 'DHT / HMU'), 1, 3, 1);

                -- Seed Unit NGP (Level 1)
                INSERT INTO OrganizationUnits (Name, ParentId, Level, DisplayOrder, IsActive)
                VALUES
                    ('Saturated Gas Concentration Unit (060)', (SELECT Id FROM OrganizationUnits WHERE Name = 'NGP'), 1, 1, 1),
                    ('Saturated LPG Treating Unit (064)',      (SELECT Id FROM OrganizationUnits WHERE Name = 'NGP'), 1, 2, 1),
                    ('Isomerization Unit (082)',               (SELECT Id FROM OrganizationUnits WHERE Name = 'NGP'), 1, 3, 1),
                    ('Common Facilities For NLP (160)',        (SELECT Id FROM OrganizationUnits WHERE Name = 'NGP'), 1, 4, 1),
                    ('Naphtha Hydrotreating Unit II (084)',    (SELECT Id FROM OrganizationUnits WHERE Name = 'NGP'), 1, 5, 1);

                -- Seed Unit GAST (Level 1)
                INSERT INTO OrganizationUnits (Name, ParentId, Level, DisplayOrder, IsActive)
                VALUES
                    ('RFCC NHT (053)',                             (SELECT Id FROM OrganizationUnits WHERE Name = 'GAST'), 1, 1, 1),
                    ('Alkylation Unit (065)',                      (SELECT Id FROM OrganizationUnits WHERE Name = 'GAST'), 1, 2, 1),
                    ('Wet Gas Sulfuric Acid Unit (066)',           (SELECT Id FROM OrganizationUnits WHERE Name = 'GAST'), 1, 3, 1),
                    ('SWS RFCC & Non RFCC (067 & 167)',           (SELECT Id FROM OrganizationUnits WHERE Name = 'GAST'), 1, 4, 1),
                    ('Amine Regeneration Unit I & II (069 & 079)', (SELECT Id FROM OrganizationUnits WHERE Name = 'GAST'), 1, 5, 1),
                    ('Flare System (319)',                         (SELECT Id FROM OrganizationUnits WHERE Name = 'GAST'), 1, 6, 1),
                    ('Sulfur Recovery Unit (169)',                 (SELECT Id FROM OrganizationUnits WHERE Name = 'GAST'), 1, 7, 1);
            ");

            // Step 4: Rename BagianId columns to OrganizationUnitId
            migrationBuilder.RenameColumn(
                name: "BagianId",
                table: "KkjFiles",
                newName: "OrganizationUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_KkjFiles_BagianId",
                table: "KkjFiles",
                newName: "IX_KkjFiles_OrganizationUnitId");

            migrationBuilder.RenameColumn(
                name: "BagianId",
                table: "CpdpFiles",
                newName: "OrganizationUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_CpdpFiles_BagianId",
                table: "CpdpFiles",
                newName: "IX_CpdpFiles_OrganizationUnitId");

            // Step 5: Remap KkjFiles.OrganizationUnitId from old KkjBagians.Name to new OrganizationUnits.Id
            // KkjBagians had: RFCC, GAST, NGP, DHT/HMU — map to OrganizationUnits top-level by name match
            migrationBuilder.Sql(@"
                UPDATE kf
                SET kf.OrganizationUnitId = ou.Id
                FROM KkjFiles kf
                INNER JOIN KkjBagians kb ON kf.OrganizationUnitId = kb.Id
                INNER JOIN OrganizationUnits ou ON ou.ParentId IS NULL
                    AND (
                        ou.Name = kb.Name
                        OR (kb.Name = 'DHT/HMU' AND ou.Name = 'DHT / HMU')
                    );

                -- Guard: remove orphan rows that could not be remapped
                DELETE FROM KkjFiles WHERE OrganizationUnitId NOT IN (SELECT Id FROM OrganizationUnits);
            ");

            // Step 6: Remap CpdpFiles.OrganizationUnitId from old KkjBagians.Name to new OrganizationUnits.Id
            migrationBuilder.Sql(@"
                UPDATE cf
                SET cf.OrganizationUnitId = ou.Id
                FROM CpdpFiles cf
                INNER JOIN KkjBagians kb ON cf.OrganizationUnitId = kb.Id
                INNER JOIN OrganizationUnits ou ON ou.ParentId IS NULL
                    AND (
                        ou.Name = kb.Name
                        OR (kb.Name = 'DHT/HMU' AND ou.Name = 'DHT / HMU')
                    );

                -- Guard: remove orphan rows that could not be remapped
                DELETE FROM CpdpFiles WHERE OrganizationUnitId NOT IN (SELECT Id FROM OrganizationUnits);
            ");

            // Step 7: Drop KkjBagians table (data already remapped)
            migrationBuilder.DropTable(
                name: "KkjBagians");

            // Step 8: Add new FKs from KkjFiles/CpdpFiles to OrganizationUnits
            migrationBuilder.AddForeignKey(
                name: "FK_CpdpFiles_OrganizationUnits_OrganizationUnitId",
                table: "CpdpFiles",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KkjFiles_OrganizationUnits_OrganizationUnitId",
                table: "KkjFiles",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: data loss on Down() is expected — seed data and remap are not reversible
            migrationBuilder.DropForeignKey(
                name: "FK_CpdpFiles_OrganizationUnits_OrganizationUnitId",
                table: "CpdpFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_KkjFiles_OrganizationUnits_OrganizationUnitId",
                table: "KkjFiles");

            migrationBuilder.DropTable(
                name: "OrganizationUnits");

            migrationBuilder.RenameColumn(
                name: "OrganizationUnitId",
                table: "KkjFiles",
                newName: "BagianId");

            migrationBuilder.RenameIndex(
                name: "IX_KkjFiles_OrganizationUnitId",
                table: "KkjFiles",
                newName: "IX_KkjFiles_BagianId");

            migrationBuilder.RenameColumn(
                name: "OrganizationUnitId",
                table: "CpdpFiles",
                newName: "BagianId");

            migrationBuilder.RenameIndex(
                name: "IX_CpdpFiles_OrganizationUnitId",
                table: "CpdpFiles",
                newName: "IX_CpdpFiles_BagianId");

            migrationBuilder.CreateTable(
                name: "KkjBagians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KkjBagians", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KkjBagians_DisplayOrder",
                table: "KkjBagians",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_KkjBagians_Name",
                table: "KkjBagians",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CpdpFiles_KkjBagians_BagianId",
                table: "CpdpFiles",
                column: "BagianId",
                principalTable: "KkjBagians",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KkjFiles_KkjBagians_BagianId",
                table: "KkjFiles",
                column: "BagianId",
                principalTable: "KkjBagians",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
