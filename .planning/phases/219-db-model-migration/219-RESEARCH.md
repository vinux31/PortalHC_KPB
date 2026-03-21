# Phase 219: DB Model & Migration - Research

**Researched:** 2026-03-21
**Domain:** EF Core Code-First Migration — Self-referential entity, FK consolidation, table drop dengan data remapping
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions

- **D-01:** Data yang dimigrasikan: 4 Bagian (RFCC, DHT/HMU, NGP, GAST) dan 17 Unit — bukan 19 seperti di requirements
- **D-02:** Seed data via migration SQL (INSERT langsung di file migration), bukan via SeedData class
- **D-03:** Tidak ada unit/bagian baru yang perlu ditambahkan saat migrasi — admin bisa tambah lewat CRUD nanti (Phase 220)
- **D-04:** OrganizationUnit menggunakan 6 kolom: Id, Name, ParentId, Level, DisplayOrder, IsActive
- **D-05:** Level dihitung otomatis dari depth parent-chain (root = 0, child = parent.Level + 1)
- **D-06:** KkjFile dan CpdpFile: kolom BagianId diganti ke OrganizationUnitId dengan FK ke OrganizationUnit
- **D-07:** Migration SQL remap existing BagianId ke OrganizationUnit ID yang sesuai
- **D-08:** Tabel KkjBagians di-DROP dari database via migration
- **D-09:** Entity class KkjBagian dihapus dari codebase

### Claude's Discretion

- Urutan migration steps (create table → seed → remap FK → drop KkjBagian, atau approach lain)
- DisplayOrder assignment untuk seed data
- Handling edge cases (orphaned records, null BagianId)

### Deferred Ideas (OUT OF SCOPE)

Tidak ada — diskusi tetap dalam scope phase.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Deskripsi | Dukungan Research |
|----|-----------|-------------------|
| DB-01 | Entity OrganizationUnit (Id, Name, ParentId, Level, DisplayOrder, IsActive) — self-referential Adjacency List | Pattern self-referential FK sudah ada di AssessmentCategory (Phase 195) — bisa diikuti langsung |
| DB-02 | Migrasi data dari static OrganizationStructure (4 Bagian, 17 Unit) ke tabel OrganizationUnits | Data source sudah terdokumentasi di OrganizationStructure.cs — 4 Bagian, 17 Unit terkonfirmasi |
| DB-03 | KkjFile/CpdpFile FK BagianId → ganti ke OrganizationUnitId, hapus entity KkjBagian | Pattern drop+remap FK sudah ada di migration DropKkjTablesAddKkjFiles (Phase 90) |
</phase_requirements>

---

## Summary

Phase 219 adalah operasi database murni: membuat entity OrganizationUnit dengan self-referential FK (Adjacency List pattern), meng-insert 21 baris seed data (4 Bagian + 17 Unit) langsung di migration SQL, lalu meremap FK KkjFile/CpdpFile dari KkjBagian ke OrganizationUnit dan men-drop tabel KkjBagians.

Semua pola yang diperlukan sudah ada di codebase ini. Self-referential FK ada di `AssessmentCategory` (Phase 195). Drop-tabel-dengan-remap-FK ada di `DropKkjTablesAddKkjFiles` (Phase 90). Migration ini hanya menggabungkan dua pola tersebut dalam satu migration file yang besar.

Risiko utama: urutan operasi SQL di dalam migration harus benar — jika tabel OrganizationUnits belum ada saat INSERT, atau jika FK lama masih aktif saat kolom baru ditambahkan, migration akan gagal. Ordering yang tepat sudah terdokumentasi di bawah.

**Primary recommendation:** Buat satu migration file bernama `AddOrganizationUnitsAndConsolidateKkjBagian` yang mengeksekusi langkah dalam urutan: CREATE TABLE → INSERT seed → ADD COLUMN → UPDATE (remap) → ADD FK → DROP FK lama → DROP TABLE KkjBagians.

---

## Standard Stack

### Core
| Library | Versi | Purpose | Kenapa Digunakan |
|---------|-------|---------|-----------------|
| EF Core (Microsoft.EntityFrameworkCore.SqlServer) | Sudah terinstall di project | Code-First migrations | Standard project stack |
| MigrationBuilder | Bawaan EF Core | DDL + DML dalam migration | Digunakan di semua migration existing |

### Tools
```bash
dotnet ef migrations add AddOrganizationUnitsAndConsolidateKkjBagian
dotnet ef database update
```

---

## Architecture Patterns

### Pattern 1: Self-Referential Entity (Adjacency List)

**Referensi di codebase:** `Models/AssessmentCategory.cs` + konfigurasi di `ApplicationDbContext.cs` baris 571-582.

**Model class:**
```csharp
// Source: Pattern dari AssessmentCategory.cs (Phase 195)
public class OrganizationUnit
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? ParentId { get; set; }
    public int Level { get; set; } = 0;         // 0 = root/Bagian, 1 = Unit, dst
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public OrganizationUnit? Parent { get; set; }
    public ICollection<OrganizationUnit> Children { get; set; } = new List<OrganizationUnit>();

    // Back-references dari KkjFile dan CpdpFile
    public ICollection<KkjFile> KkjFiles { get; set; } = new List<KkjFile>();
    public ICollection<CpdpFile> CpdpFiles { get; set; } = new List<CpdpFile>();
}
```

**DbContext configuration:**
```csharp
// Source: Pattern dari ApplicationDbContext.cs baris 571-582
builder.Entity<OrganizationUnit>(entity =>
{
    entity.ToTable("OrganizationUnits");
    entity.HasIndex(u => u.Name).IsUnique();
    entity.HasIndex(u => new { u.ParentId, u.DisplayOrder });
    entity.Property(u => u.IsActive).HasDefaultValue(true);

    entity.HasOne(u => u.Parent)
          .WithMany(u => u.Children)
          .HasForeignKey(u => u.ParentId)
          .OnDelete(DeleteBehavior.Restrict);  // Jangan Cascade — parent delete harus manual
});
```

**Catatan:** Gunakan `OnDelete(DeleteBehavior.Restrict)` seperti AssessmentCategory — bukan Cascade. Ini memastikan admin tidak bisa hapus Bagian yang masih punya Unit aktif.

### Pattern 2: FK Consolidation dalam Migration

**Referensi di codebase:** `Migrations/20260302125630_DropKkjTablesAddKkjFiles.cs` — contoh drop tabel dengan FK dependencies.

**Urutan operasi yang benar dalam satu migration:**

```
Step 1: CREATE TABLE OrganizationUnits
Step 2: INSERT seed data (21 baris — 4 Bagian lalu 17 Unit)
Step 3: ADD COLUMN OrganizationUnitId ke KkjFiles (nullable dulu)
Step 4: ADD COLUMN OrganizationUnitId ke CpdpFiles (nullable dulu)
Step 5: UPDATE KkjFiles SET OrganizationUnitId = (mapping dari BagianId)
Step 6: UPDATE CpdpFiles SET OrganizationUnitId = (mapping dari BagianId)
Step 7: ALTER COLUMN OrganizationUnitId jadi NOT NULL (setelah semua rows ter-remap)
Step 8: ADD FK KkjFiles → OrganizationUnits
Step 9: ADD FK CpdpFiles → OrganizationUnits
Step 10: DROP FK KkjFiles_KkjBagians
Step 11: DROP FK CpdpFiles_KkjBagians
Step 12: DROP INDEX yang referensikan BagianId
Step 13: DROP COLUMN BagianId dari KkjFiles
Step 14: DROP COLUMN BagianId dari CpdpFiles
Step 15: DROP TABLE KkjBagians
```

**Kenapa urutan ini?** SQL Server tidak mengizinkan DROP TABLE jika masih ada FK yang referensikan tabel tersebut. FK harus di-drop dulu (step 10-11), baru kolom BagianId (step 13-14), baru tabel (step 15).

### Pattern 3: Seed Data via migrationBuilder.Sql()

**Referensi:** Diputuskan D-02 — INSERT langsung di migration, bukan SeedData class.

```csharp
// Source: EF Core official pattern untuk data seeding via migration
migrationBuilder.Sql(@"
    -- Insert Bagian (Level 0, ParentId NULL)
    INSERT INTO OrganizationUnits (Name, ParentId, Level, DisplayOrder, IsActive)
    VALUES
        ('RFCC',      NULL, 0, 1, 1),
        ('DHT / HMU', NULL, 0, 2, 1),
        ('NGP',       NULL, 0, 3, 1),
        ('GAST',      NULL, 0, 4, 1);

    -- Insert Unit (Level 1, ParentId = ID Bagian parent)
    -- RFCC Units (ParentId = ID 'RFCC' yang baru diinsert)
    INSERT INTO OrganizationUnits (Name, ParentId, Level, DisplayOrder, IsActive)
    VALUES
        ('RFCC LPG Treating Unit (062)',   (SELECT Id FROM OrganizationUnits WHERE Name = 'RFCC'),      1, 1, 1),
        ('Propylene Recovery Unit (063)',  (SELECT Id FROM OrganizationUnits WHERE Name = 'RFCC'),      1, 2, 1),
        ...
");
```

**Penting:** Gunakan subquery `SELECT Id FROM OrganizationUnits WHERE Name = 'RFCC'` untuk ParentId — jangan hardcode ID karena tidak dijamin mulai dari 1 (bisa ada IDENTITY gaps).

### Pattern 4: FK Remapping via migrationBuilder.Sql()

```csharp
// Remap KkjFiles.BagianId ke OrganizationUnitId
// KkjBagian.Id mapping ke OrganizationUnit: harus query KkjBagians join OrganizationUnits by name
migrationBuilder.Sql(@"
    UPDATE kf
    SET kf.OrganizationUnitId = ou.Id
    FROM KkjFiles kf
    INNER JOIN KkjBagians kb ON kf.BagianId = kb.Id
    INNER JOIN OrganizationUnits ou ON ou.Name = kb.Name
        AND ou.ParentId IS NULL  -- Level Bagian saja (Level = 0)
");
```

**Catatan edge case:** Jika ada KkjFile dengan BagianId yang tidak match ke OrganizationUnit manapun (orphaned records), UPDATE tidak akan set OrganizationUnitId mereka. Step 7 (ALTER COLUMN NOT NULL) akan GAGAL jika ada NULL tersisa. Solusi: tambah fallback sebelum Step 7 untuk menangani NULL (pilihan: DELETE orphans, atau assign ke default Bagian).

### Anti-Patterns to Avoid

- **Jangan hardcode ID di seed INSERT** — IDENTITY bisa tidak mulai dari 1, gunakan subquery by Name.
- **Jangan DROP TABLE KkjBagians sebelum FK di-drop** — SQL Server akan error FK constraint violation.
- **Jangan ALTER COLUMN ke NOT NULL sebelum UPDATE remap selesai** — rows NULL akan menyebabkan constraint error.
- **Jangan gunakan HasData() di OnModelCreating** — keputusan D-02 menetapkan migration SQL, bukan HasData(). Selain itu HasData() punya komplikasi dengan identity columns.

---

## Don't Hand-Roll

| Problem | Jangan Buat | Gunakan Ini | Alasan |
|---------|-------------|-------------|--------|
| Level calculation | Computed property C# yang hitung depth runtime | Kolom Level yang di-set saat INSERT (D-05) | Level dihitung sekali saat insert/update, bukan setiap query |
| Mapping BagianId → OrganizationUnitId | Dictionary hardcoded di C# | JOIN query di migration SQL | Database lebih reliable, tidak tergantung ID urutan |
| Validasi circular reference | Logic custom di migration | Constraint `OnDelete(DeleteBehavior.Restrict)` | Phase 220 CRUD yang akan handle validasi circular reference |

---

## Common Pitfalls

### Pitfall 1: Identity ID tidak terprediksi untuk seed data
**Yang terjadi:** Developer hardcode `ParentId = 1` untuk RFCC, tapi IDENTITY table mulai dari nilai lain.
**Kenapa terjadi:** SQL Server IDENTITY bisa mulai dari nilai berbeda jika pernah ada insert+delete sebelumnya.
**Cara menghindari:** Selalu gunakan subquery `SELECT Id FROM OrganizationUnits WHERE Name = '...'` saat INSERT Unit.
**Tanda peringatan:** Seed berjalan tanpa error tapi Unit semua mengarah ke Bagian yang salah.

### Pitfall 2: FK constraint violation saat DROP TABLE
**Yang terjadi:** `Cannot drop the table 'KkjBagians', because it is being referenced by object 'FK_KkjFiles_KkjBagians_BagianId'`.
**Kenapa terjadi:** DROP TABLE sebelum DROP FOREIGN KEY.
**Cara menghindari:** Ikuti urutan 15 step — selalu drop FK dulu (step 10-11) baru drop table (step 15).

### Pitfall 3: NULL OrganizationUnitId tersisa setelah remap
**Yang terjadi:** `ALTER TABLE ALTER COLUMN failed because the column 'OrganizationUnitId' contains null values`.
**Kenapa terjadi:** Ada KkjFile/CpdpFile yang BagianId-nya tidak match nama Bagian di OrganizationUnits (typo nama, deleted Bagian, dsb).
**Cara menghindari:** Tambahkan handler sebelum ALTER COLUMN — misalnya: `DELETE FROM KkjFiles WHERE OrganizationUnitId IS NULL` atau assign ke default Bagian pertama.
**Tanda peringatan:** Migration step ke-7 gagal.

### Pitfall 4: KkjBagian Navigation Property masih direferensikan di model
**Yang terjadi:** Build error setelah hapus entity class KkjBagian — property `KkjFile.Bagian` masih ada di model.
**Kenapa terjadi:** Lupa update model class KkjModels.cs bersamaan dengan hapus DbSet.
**Cara menghindari:** Ganti `public KkjBagian Bagian { get; set; }` dengan `public OrganizationUnit OrganizationUnit { get; set; }` di KkjFile dan CpdpFile sebelum migration di-run.

### Pitfall 5: Migration Down() tidak simetris
**Yang terjadi:** `dotnet ef database update <previous>` gagal atau merusak data.
**Kenapa terjadi:** Down() tidak merestore data yang sudah di-remap.
**Cara menghindari:** Down() untuk fase ini cukup structural (recreate KkjBagians, add BagianId back, drop OrganizationUnitId) — data loss di Down() adalah acceptable tradeoff yang harus didokumentasikan dengan comment.

---

## Seed Data Mapping (terkonfirmasi dari OrganizationStructure.cs)

Data exact yang akan di-seed — **21 baris total** (4 Bagian + 17 Unit):

**Bagian (Level 0):**
| DisplayOrder | Name |
|---|---|
| 1 | RFCC |
| 2 | DHT / HMU |
| 3 | NGP |
| 4 | GAST |

**Unit RFCC (Level 1, 2 unit):**
| DisplayOrder | Name |
|---|---|
| 1 | RFCC LPG Treating Unit (062) |
| 2 | Propylene Recovery Unit (063) |

**Unit DHT / HMU (Level 1, 3 unit):**
| DisplayOrder | Name |
|---|---|
| 1 | Diesel Hydrotreating Unit I & II (054 & 083) |
| 2 | Hydrogen Manufacturing Unit (068) |
| 3 | Common DHT H2 Compressor (085) |

**Unit NGP (Level 1, 5 unit):**
| DisplayOrder | Name |
|---|---|
| 1 | Saturated Gas Concentration Unit (060) |
| 2 | Saturated LPG Treating Unit (064) |
| 3 | Isomerization Unit (082) |
| 4 | Common Facilities For NLP (160) |
| 5 | Naphtha Hydrotreating Unit II (084) |

**Unit GAST (Level 1, 7 unit):**
| DisplayOrder | Name |
|---|---|
| 1 | RFCC NHT (053) |
| 2 | Alkylation Unit (065) |
| 3 | Wet Gas Sulfuric Acid Unit (066) |
| 4 | SWS RFCC & Non RFCC (067 & 167) |
| 5 | Amine Regeneration Unit I & II (069 & 079) |
| 6 | Flare System (319) |
| 7 | Sulfur Recovery Unit (169) |

**Konfirmasi:** 2 + 3 + 5 + 7 = 17 Unit. Sesuai D-01.

---

## Perubahan Kode yang Diperlukan (selain migration)

### 1. Models/KkjModels.cs — Update entitas

```csharp
// SEBELUM:
public class KkjFile
{
    public int BagianId { get; set; }
    public KkjBagian Bagian { get; set; } = null!;
    ...
}

// SESUDAH:
public class KkjFile
{
    public int OrganizationUnitId { get; set; }
    public OrganizationUnit OrganizationUnit { get; set; } = null!;
    ...
}
```

Sama untuk CpdpFile. Entity class `KkjBagian` dihapus seluruhnya.

### 2. Data/ApplicationDbContext.cs — Update DbSets dan konfigurasi

```csharp
// HAPUS:
public DbSet<KkjBagian> KkjBagians { get; set; }

// TAMBAH:
public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
```

Update konfigurasi KkjFile dan CpdpFile: ganti reference `Bagian` ke `OrganizationUnit`.

### 3. Models/ — Tambah file baru OrganizationUnit.cs

File model baru yang berisi class `OrganizationUnit`.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual verification (tidak ada unit test framework terdeteksi) |
| Config file | N/A |
| Quick run command | `dotnet build` |
| Full suite command | `dotnet run` + manual smoke test |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| DB-01 | Tabel OrganizationUnits ada dengan 6 kolom yang benar | SQL smoke | `dotnet ef database update` tanpa error | ❌ Wave 0 |
| DB-02 | 21 baris seed data ter-insert dengan hierarki parent-child yang benar | SQL smoke | Query manual: `SELECT * FROM OrganizationUnits ORDER BY Level, DisplayOrder` | ❌ Wave 0 |
| DB-03 | KkjFile/CpdpFile punya OrganizationUnitId yang benar, KkjBagians ter-drop | SQL smoke | `dotnet build` + `dotnet ef database update` tanpa error | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet build` — pastikan tidak ada compile error
- **Per wave merge:** `dotnet ef database update` + query verifikasi manual
- **Phase gate:** `dotnet run` berjalan tanpa exception setelah migration

### Wave 0 Gaps
- [ ] Migration file belum ada — dibuat di task pertama
- [ ] `Models/OrganizationUnit.cs` belum ada — dibuat di task pertama
- [ ] Update `KkjModels.cs` untuk hapus KkjBagian dan update FK — task terpisah
- [ ] Update `ApplicationDbContext.cs` — task terpisah

---

## State of the Art

| Old Approach | Current Approach | Keterangan |
|---|---|---|
| KkjBagian (4 baris hardcoded) | OrganizationUnit self-referential tree | Phase 219 |
| Static class OrganizationStructure.cs | Database-driven (Phase 221 akan ganti semua referensi) | Phase 219 hanya buat model, static class BELUM dihapus |
| BagianId FK ke KkjBagians | OrganizationUnitId FK ke OrganizationUnits | Phase 219 |

**Penting:** Phase 219 TIDAK menghapus `OrganizationStructure.cs` — itu Phase 222. Phase ini hanya buat foundation DB.

---

## Open Questions

1. **Nama Bagian "DHT / HMU" — konsistensi dengan KkjBagian existing**
   - Yang kita tahu: Static class menggunakan `"DHT / HMU"` (dengan spasi di sekitar slash)
   - Yang perlu dicek: Apakah nilai di tabel KkjBagians existing persis sama?
   - Rekomendasi: Verifikasi dengan `SELECT Name FROM KkjBagians` sebelum migration di-apply — remap JOIN menggunakan `ou.Name = kb.Name` sehingga typo sekecil apapun akan menyebabkan remap gagal

2. **Orphaned KkjFile / CpdpFile records**
   - Yang kita tahu: Belum ada cara mengetahui apakah ada records dengan BagianId yang tidak valid
   - Yang perlu dicek: `SELECT COUNT(*) FROM KkjFiles kf LEFT JOIN KkjBagians kb ON kf.BagianId = kb.Id WHERE kb.Id IS NULL`
   - Rekomendasi: Tambahkan guard `DELETE orphans` di migration sebelum ALTER COLUMN NOT NULL

---

## Sources

### Primary (HIGH confidence)
- `Models/AssessmentCategory.cs` + `Data/ApplicationDbContext.cs` baris 571-582 — self-referential FK pattern yang sudah terbukti di project ini
- `Migrations/20260302125630_DropKkjTablesAddKkjFiles.cs` — pattern drop table dengan FK dependencies
- `Migrations/20260318023131_AddParentAndSignatoryToAssessmentCategory.cs` — pattern AddForeignKey self-referential
- `Models/OrganizationStructure.cs` — sumber data seed yang terkonfirmasi (4 Bagian, 17 Unit)
- `Models/KkjModels.cs` — entity yang akan dimodifikasi (KkjFile, CpdpFile, KkjBagian)

### Secondary (MEDIUM confidence)
- EF Core documentation pattern untuk `migrationBuilder.Sql()` dengan data seeding

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — EF Core sudah digunakan di seluruh project
- Architecture: HIGH — pola self-referential FK dan drop-table sudah ada di codebase ini
- Pitfalls: HIGH — berdasarkan kode existing dan SQL Server constraint behavior yang deterministik
- Seed data: HIGH — terkonfirmasi langsung dari OrganizationStructure.cs

**Research date:** 2026-03-21
**Valid until:** Stabil — tidak bergantung pada library eksternal yang berubah cepat
