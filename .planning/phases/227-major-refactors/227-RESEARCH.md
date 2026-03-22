# Phase 227: Major Refactors - Research

**Researched:** 2026-03-22
**Domain:** ASP.NET Core MVC — data migration, schema cleanup, business logic relokasi
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** QBNK-01, QBNK-02, QBNK-03 di-SKIP seluruhnya. Tidak ada halaman Question Bank terpisah.
- **D-02:** Soal tetap dikelola lewat ManagePackages + ImportPackageQuestions yang sudah ada (terikat ke assessment session).
- **D-03:** Strategi CLEN-02: data migration script — convert semua legacy session data (AssessmentQuestion/AssessmentOption/UserResponse) ke PackageQuestion/PackageOption/PackageUserResponse format.
- **D-04:** Perlu cek session legacy yang masih aktif (belum submitted) sebelum migrasi dan handle secara khusus.
- **D-05:** Post-migrasi: drop tabel legacy (AssessmentQuestion, AssessmentOption, UserResponse) setelah data terverifikasi lengkap. Hapus semua legacy code path dari CMPController.
- **D-06:** Drop tabel AssessmentCompetencyMap dan UserCompetencyLevel — tidak ada data aktif, tabel orphan.
- **D-07:** Pindahkan NomorSertifikat generation dari CreateAssessment ke SubmitExam + IsPassed = true.
- **D-08:** Bad data handling: migration script set NomorSertifikat = NULL untuk semua session dengan IsPassed != true.
- **D-09:** Manual entry (AddTraining/ImportTraining) tetap boleh input NomorSertifikat custom. Auto-generate hanya untuk assessment flow.

### Claude's Discretion
- Urutan eksekusi (migrasi dulu vs NomorSertifikat fix dulu)
- Detail migration script (batch size, error handling, rollback strategy)
- Exact verification query sebelum drop tables

### Deferred Ideas (OUT OF SCOPE)
- **Question Bank terpisah (QBNK-01/02/03)** — User memutuskan tidak perlu saat ini. Soal tetap dikelola per assessment session lewat ManagePackages. Bisa jadi phase terpisah di masa depan jika kebutuhan berubah.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CLEN-02 | Legacy question path (AssessmentQuestion/AssessmentOption/UserResponse) deprecated — existing sessions dimigrasi ke package format | Data migration EF Core migration + code removal dari CMPController |
| CLEN-03 | AssessmentCompetencyMap dan UserCompetencyLevel (orphaned tables) dibersihkan dari database | Drop via EF Core migration, remove DbSet + model files + SeedCompetencyMappings call |
| CLEN-04 | NomorSertifikat di-generate saat SubmitExam + IsPassed (bukan saat CreateAssessment) | Relokasi BuildCertNumber call + bad data migration |
</phase_requirements>

---

## Summary

Phase 227 adalah operasi cleanup murni — tidak ada fitur baru. Tiga requirement aktif (CLEN-02, CLEN-03, CLEN-04) semuanya bersifat destructive/refactor: migrasi data legacy, drop tabel orphan, dan relokasi business logic.

**CLEN-02** adalah yang paling kompleks: legacy question path (AssessmentQuestion/AssessmentOption/UserResponse) masih digunakan oleh beberapa session lama. CMPController.TakeExam (line 954-1008) dan SubmitExam (line 1553-1661) keduanya memiliki legacy branch aktif. Migration script harus convert data legacy ke format package, setelah itu code path legacy dihapus sepenuhnya.

**CLEN-03** relatif straightforward: AssessmentCompetencyMap dan UserCompetencyLevel sudah menjadi orphan sejak Phase 90 (KkjMatrices di-drop). SeedCompetencyMappings sudah no-op. Tinggal drop tabel via migration + bersihkan references di ApplicationDbContext dan Program.cs.

**CLEN-04** membutuhkan: (1) hapus NomorSertifikat assignment dari CreateAssessment loop di AdminController, (2) tambah NomorSertifikat generation di CMPController.SubmitExam setelah IsPassed diketahui, (3) migration script untuk NULL-kan NomorSertifikat pada session yang belum lulus. Tantangan utama: BuildCertNumber adalah private static di AdminController — perlu di-extract ke helper yang bisa dipanggil dari CMPController.

**Primary recommendation:** Eksekusi urutan: CLEN-04 data fix dulu (aman, tidak destructive terhadap data soal) → CLEN-03 drop orphan (tidak ada dependency) → CLEN-02 migration + code removal (paling berisiko, taruh di akhir).

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| EF Core Migrations | Sesuai project (.NET 8) | Schema changes (drop tables) | Established pattern di codebase ini |
| Raw SQL via `migrationBuilder.Sql()` | N/A | Data migration dalam migration file | Satu-satunya cara jalankan DML di EF migration |

### Pola yang Sudah Ada di Codebase
| Pattern | Lokasi | Relevansi |
|---------|--------|-----------|
| EF Core Migration dengan Sql() | `20260301021015_DeleteLegacyProtonFinalAssessmentData.cs` | Referensi untuk data migration script |
| Drop table migration | `20260302125630_DropKkjTablesAddKkjFiles.cs` | Pattern untuk CLEN-03 |
| AuditLog | `AdminController._auditLog.LogAsync()` | Migration actions harus di-log |

---

## Architecture Patterns

### Urutan Eksekusi yang Direkomendasikan

```
Wave 1: CLEN-04 setup
  ├── Extract BuildCertNumber ke shared helper (static class CertNumberHelper)
  ├── Hapus NomorSertifikat = BuildCertNumber(...) dari CreateAssessment loop
  ├── Tambah NomorSertifikat generation di CMPController.SubmitExam (package path + legacy path)
  └── EF Migration: NULL-kan NomorSertifikat WHERE IsPassed IS NULL OR IsPassed = 0

Wave 2: CLEN-03 cleanup
  ├── EF Migration: DROP TABLE AssessmentCompetencyMaps, UserCompetencyLevels
  ├── Hapus DbSet di ApplicationDbContext
  ├── Hapus model files: AssessmentCompetencyMap.cs, UserCompetencyLevel.cs
  └── Hapus SeedCompetencyMappings.SeedAsync() call dari Program.cs

Wave 3: CLEN-02 migration + removal
  ├── EF Migration data: Convert AssessmentQuestion → PackageQuestion (via raw SQL)
  ├── EF Migration schema: DROP TABLE AssessmentQuestions, AssessmentOptions, UserResponses
  ├── Hapus code: CMPController.SaveLegacyAnswer action (line 324-370)
  ├── Hapus code: CMPController.TakeExam legacy branch (line 952-1008)
  ├── Hapus code: CMPController.SubmitExam legacy branch (line 1551-1661)
  ├── Hapus DbSet di ApplicationDbContext (AssessmentQuestions, AssessmentOptions, UserResponses)
  ├── Hapus model files: AssessmentQuestion.cs (termasuk AssessmentOption), UserResponse.cs
  └── Hapus navigation property di AssessmentSession.cs (Questions collection, line 125)
```

### Pattern: EF Migration Data + Schema Drop

```csharp
// Contoh pola dari 20260301021015_DeleteLegacyProtonFinalAssessmentData.cs
// (referensi — baca file ini sebelum implementasi)
public partial class MigrateLegacyQuestionsToPackages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Step 1: Buat AssessmentPackage rows untuk setiap session yang punya legacy questions
        migrationBuilder.Sql(@"
            INSERT INTO AssessmentPackages (AssessmentSessionId, PackageName, PackageNumber, CreatedAt)
            SELECT DISTINCT aq.AssessmentSessionId, 'Paket A', 1, GETUTCDATE()
            FROM AssessmentQuestions aq
            WHERE NOT EXISTS (
                SELECT 1 FROM AssessmentPackages ap
                WHERE ap.AssessmentSessionId = aq.AssessmentSessionId
            )
        ");

        // Step 2: Migrate AssessmentQuestion → PackageQuestion
        migrationBuilder.Sql(@"
            INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, ElemenTeknis)
            SELECT ap.Id, aq.QuestionText, aq.[Order], aq.ScoreValue, NULL
            FROM AssessmentQuestions aq
            JOIN AssessmentPackages ap ON ap.AssessmentSessionId = aq.AssessmentSessionId
        ");

        // Step 3: Migrate AssessmentOption → PackageOption
        migrationBuilder.Sql(@"
            INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
            SELECT pq.Id, ao.OptionText, ao.IsCorrect
            FROM AssessmentOptions ao
            JOIN AssessmentQuestions aq ON aq.Id = ao.AssessmentQuestionId
            JOIN AssessmentPackages ap ON ap.AssessmentSessionId = aq.AssessmentSessionId
            JOIN PackageQuestions pq ON pq.AssessmentPackageId = ap.Id
                AND pq.[Order] = aq.[Order]
        ");

        // Step 4: Create UserPackageAssignment untuk legacy sessions
        // ... (handle via application code or separate script)

        // Step 5: Migrate UserResponse → PackageUserResponse (untuk completed sessions)
        // ... (link via question order matching)

        // Step 6: Drop legacy tables
        migrationBuilder.DropTable("UserResponses");
        migrationBuilder.DropTable("AssessmentOptions");
        migrationBuilder.DropTable("AssessmentQuestions");
    }
}
```

### Pattern: Extract BuildCertNumber ke Shared Helper

```csharp
// File baru: Helpers/CertNumberHelper.cs
namespace HcPortal.Helpers
{
    public static class CertNumberHelper
    {
        private static string ToRomanMonth(int month) => month switch
        {
            1 => "I", 2 => "II", 3 => "III", 4 => "IV",
            5 => "V", 6 => "VI", 7 => "VII", 8 => "VIII",
            9 => "IX", 10 => "X", 11 => "XI", 12 => "XII",
            _ => throw new ArgumentOutOfRangeException(nameof(month))
        };

        public static string Build(int seq, DateTime date)
            => $"KPB/{seq:D3}/{ToRomanMonth(date.Month)}/{date.Year}";

        // Hitung nextSeq berdasarkan tahun berjalan
        public static async Task<int> GetNextSeqAsync(ApplicationDbContext context, int year)
        {
            var existing = await context.AssessmentSessions
                .Where(s => s.NomorSertifikat != null && s.NomorSertifikat.EndsWith($"/{year}"))
                .Select(s => s.NomorSertifikat!)
                .ToListAsync();

            return existing.Count == 0 ? 1 :
                existing.Select(n => {
                    var parts = n.Split('/');
                    return parts.Length > 1 && int.TryParse(parts[1], out int v) ? v : 0;
                }).Max() + 1;
        }
    }
}
```

### Pattern: NomorSertifikat Generation di SubmitExam

```csharp
// Di CMPController.SubmitExam — SETELAH rowsAffected > 0 dikonfirmasi (status claim berhasil)
// Target: hanya package path (legacy path akan dihapus)
if (rowsAffected > 0 && assessment.GenerateCertificate)
{
    bool isPassed = finalPercentage >= assessment.PassPercentage;
    if (isPassed)
    {
        var now = DateTime.Now;
        int year = now.Year;
        var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, year);
        await _context.AssessmentSessions
            .Where(s => s.Id == id && s.NomorSertifikat == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, now))
            );
    }
}
```

### Anti-Patterns to Avoid
- **Jangan jalankan data migration di application startup** — gunakan EF Core Migration (idempotent, auditable).
- **Jangan hapus DbSet sebelum migration dijalankan** — EF tidak bisa generate SQL untuk tabel yang sudah tidak ada di model.
- **Jangan drop tabel legacy sebelum verifikasi migrasi** — jalankan verification query dulu (lihat bagian Common Pitfalls).
- **Jangan duplicate NomorSertifikat** — unique constraint `IX_AssessmentSessions_NomorSertifikat` ada, IsDuplicateKeyException handler perlu dipertahankan di SubmitExam path baru.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Schema drop | Manual SQL script terpisah | EF Core Migration | Versioned, rollbackable, tracked di git |
| Data migration dalam migration file | Application-level migration service | `migrationBuilder.Sql()` | Runs at deploy time, guaranteed order |
| Duplicate cert number handling | Distributed lock | Re-query + retry pattern (sudah ada di AdminController) | Proven pattern, cukup untuk single-node |

---

## Runtime State Inventory

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data (legacy questions) | AssessmentQuestions, AssessmentOptions, UserResponses tabel — mungkin ada data aktif dari session lama | Data migration script dalam EF Migration |
| Stored data (NomorSertifikat bad data) | AssessmentSessions rows yang punya NomorSertifikat tapi IsPassed IS NULL atau false | Migration: SET NomorSertifikat = NULL WHERE IsPassed != 1 AND GenerateCertificate = 1 |
| Stored data (orphan tables) | AssessmentCompetencyMaps dan UserCompetencyLevels — data orphan tanpa FK valid sejak Phase 90 | DROP TABLE via EF Migration (verifikasi kosong dulu) |
| Live service config | None — tidak ada external service config | N/A |
| OS-registered state | None | N/A |
| Secrets/env vars | None | N/A |
| Build artifacts | None | N/A |

**Catatan penting:** Sebelum menjalankan CLEN-02 migration, WAJIB cek apakah ada session legacy yang status = 'InProgress' atau 'Open'. Session semacam ini tidak boleh di-migrate karena pengguna mungkin sedang mengerjakan ujian. Opsi: (1) force-complete / abandon dulu, atau (2) exclude dari migration dan handle manual.

---

## Common Pitfalls

### Pitfall 1: Option Linking Error saat Migration
**What goes wrong:** Saat migrating AssessmentOption → PackageOption, join via `aq.Order = pq.Order` bisa gagal jika ada soal dengan Order yang sama dalam satu session.
**Why it happens:** AssessmentQuestion.Order tidak digaransi unique per session di model definition.
**How to avoid:** Join lebih eksplisit: gunakan ROW_NUMBER() atau temporary ID mapping table dalam migration SQL. Alternatif: migrate satu session sekaligus dengan cursor-like approach.
**Warning signs:** PackageOptions.Count tidak sama dengan AssessmentOptions.Count setelah migration.

### Pitfall 2: UserPackageAssignment Hilang untuk Session Legacy
**What goes wrong:** Setelah migration, session yang dulunya legacy tidak punya UserPackageAssignment row — SubmitExam akan masuk ke legacy branch (yang sudah dihapus) dan throw error.
**Why it happens:** UserPackageAssignment dibuat oleh TakeExam saat user pertama kali start exam; legacy sessions tidak memilikinya.
**How to avoid:** Untuk session Completed, tidak perlu — mereka tidak akan di-submit lagi. Untuk session belum selesai: buat UserPackageAssignment sebagai bagian dari migration, atau abandon session tersebut.
**Warning signs:** Session dengan status != 'Completed' tapi tidak punya UserPackageAssignment setelah migration.

### Pitfall 3: NomorSertifikat Unique Constraint Conflict
**What goes wrong:** Saat menambah NomorSertifikat generation di SubmitExam, dua concurrent submissions bisa mendapat seq yang sama.
**Why it happens:** GetNextSeq + Build bukan atomic operation.
**How to avoid:** Salin pola retry dari AdminController.CreateAssessment (try/catch DbUpdateException, re-query seq, retry). IsDuplicateKeyException helper sudah ada di AdminController — pindahkan ke CertNumberHelper atau extract ke shared location.
**Warning signs:** Exception `IX_AssessmentSessions_NomorSertifikat` di logs.

### Pitfall 4: Compilation Error setelah Hapus DbSet
**What goes wrong:** Setelah menghapus `DbSet<AssessmentQuestion>` dari ApplicationDbContext, ada references di controller lain yang masih pakai `_context.AssessmentQuestions`.
**Why it happens:** Legacy code tersebar — CMPController bukan satu-satunya yang bisa reference tabel ini.
**How to avoid:** Grep seluruh codebase untuk `AssessmentQuestions`, `AssessmentOptions`, `UserResponses` sebelum hapus DbSet. Hapus semua references dulu.
**Warning signs:** Build error CS1061 atau CS0117.

### Pitfall 5: SeedCompetencyMappings Call Tetap Ada
**What goes wrong:** Program.cs masih memanggil `SeedCompetencyMappings.SeedAsync(context)` setelah class dihapus.
**Why it happens:** File SeedCompetencyMappings.cs ada di Data/ folder dan dipanggil dari Program.cs line 128.
**How to avoid:** Hapus file sekaligus hapus call di Program.cs (bukan hanya salah satunya).
**Warning signs:** Build error "type or namespace not found".

---

## Code Examples

### Verification Queries Sebelum Drop (jalankan manual via SQL)

```sql
-- Verifikasi: berapa session masih pakai legacy path?
SELECT COUNT(*) AS LegacySessionCount
FROM AssessmentSessions s
WHERE EXISTS (SELECT 1 FROM AssessmentQuestions aq WHERE aq.AssessmentSessionId = s.Id)
  AND NOT EXISTS (SELECT 1 FROM AssessmentPackages ap WHERE ap.AssessmentSessionId = s.Id);

-- Verifikasi: ada session legacy yang masih aktif?
SELECT s.Id, s.Title, s.Status, s.UserId
FROM AssessmentSessions s
WHERE EXISTS (SELECT 1 FROM AssessmentQuestions aq WHERE aq.AssessmentSessionId = s.Id)
  AND s.Status IN ('Open', 'InProgress');

-- Verifikasi: NomorSertifikat bad data yang perlu di-NULL-kan
SELECT COUNT(*) AS BadCertCount
FROM AssessmentSessions
WHERE GenerateCertificate = 1
  AND NomorSertifikat IS NOT NULL
  AND (IsPassed IS NULL OR IsPassed = 0);

-- Verifikasi: AssessmentCompetencyMaps row count (harus 0 sebelum drop)
SELECT COUNT(*) FROM AssessmentCompetencyMaps;
SELECT COUNT(*) FROM UserCompetencyLevels;
```

### NomorSertifikat Bad Data Fix (dalam EF Migration)

```csharp
migrationBuilder.Sql(@"
    UPDATE AssessmentSessions
    SET NomorSertifikat = NULL
    WHERE GenerateCertificate = 1
      AND NomorSertifikat IS NOT NULL
      AND (IsPassed IS NULL OR IsPassed = 0)
");
```

### Drop Orphan Tables (dalam EF Migration)

```csharp
// Cek dulu apakah ada FK constraint ke tabel ini
// UserCompetencyLevel punya FK ke AssessmentSession — drop FK dulu jika ada
migrationBuilder.DropTable(name: "UserCompetencyLevels");
migrationBuilder.DropTable(name: "AssessmentCompetencyMaps");
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| AssessmentQuestion per session | PackageQuestion dalam AssessmentPackage | Phase 17 | Legacy path seharusnya tidak digunakan untuk session baru |
| Competency tracking via KKJ matrix | Dihapus (KkjMatrices drop Phase 90) | Phase 90 | AssessmentCompetencyMap/UserCompetencyLevel menjadi orphan |
| NomorSertifikat dibuat saat CreateAssessment | Target: dibuat saat SubmitExam + IsPassed | Phase 227 (ini) | Session yang gagal tidak lagi punya nomor sertifikat |

---

## Open Questions

1. **Apakah ada data di AssessmentQuestions/AssessmentOptions/UserResponses saat ini?**
   - What we know: Tabel masih ada di schema, model masih didefinisikan
   - What's unclear: Apakah ada row aktual (session production yang pakai legacy path)
   - Recommendation: Jalankan verification query sebelum implement. Jika ada session InProgress legacy, putuskan apakah di-abandon atau di-handle khusus.

2. **Apakah `BuildCertNumber` perlu di-extract ke shared helper atau cukup di-duplicate?**
   - What we know: Logic sangat simple (2 method, ~10 baris). Private static di AdminController.
   - What's unclear: Apakah ada rencana reuse di tempat lain
   - Recommendation: Extract ke `Helpers/CertNumberHelper.cs` — lebih clean, terhindari dari duplicate bug. AdminController update reference ke helper baru.

3. **Apakah NomorSertifikat perlu unique constraint dijaga di SubmitExam path?**
   - What we know: Unique index `IX_AssessmentSessions_NomorSertifikat` sudah ada. AdminController punya retry logic.
   - What's unclear: Berapa frequent concurrent exam submission di production
   - Recommendation: Implementasikan retry logic yang sama (sudah ada pattern-nya di AdminController:1441-1465).

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (tidak ada automated test framework terdeteksi) |
| Config file | N/A |
| Quick run command | Build: `dotnet build` |
| Full suite command | Manual: jalankan exam flow end-to-end di browser |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CLEN-02 | Session yang dulunya legacy sekarang berjalan via package path | Smoke (manual) | `dotnet build` (compile check) | N/A |
| CLEN-02 | Tabel AssessmentQuestions/AssessmentOptions/UserResponses tidak ada | DB verification | SQL query manual | N/A |
| CLEN-03 | Tabel AssessmentCompetencyMaps/UserCompetencyLevels tidak ada | DB verification | SQL query manual | N/A |
| CLEN-04 | Session baru yang belum submit tidak punya NomorSertifikat | DB + UI check | SQL query manual | N/A |
| CLEN-04 | Session passed punya NomorSertifikat setelah submit | End-to-end manual | Browser exam flow | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build` — pastikan tidak ada compile error
- **Per wave merge:** Verification SQL queries + dotnet build
- **Phase gate:** Manual exam flow end-to-end (create session → take exam → submit → cek NomorSertifikat)

---

## Sources

### Primary (HIGH confidence)
- Source code langsung: `Controllers/CMPController.cs` — SaveLegacyAnswer, TakeExam legacy branch, SubmitExam kedua paths
- Source code langsung: `Controllers/AdminController.cs` — CreateAssessment NomorSertifikat generation, BuildCertNumber
- Source code langsung: `Data/ApplicationDbContext.cs` — DbSet registrations
- Source code langsung: `Data/SeedCompetencyMappings.cs` — sudah no-op stub
- Source code langsung: `Program.cs` line 128 — SeedCompetencyMappings.SeedAsync call
- Existing migration: `20260301021015_DeleteLegacyProtonFinalAssessmentData.cs` — pattern referensi

### Secondary (MEDIUM confidence)
- EF Core Migrations documentation pattern (verified dari codebase) — migrationBuilder.Sql() untuk DML

---

## Metadata

**Confidence breakdown:**
- CLEN-02 migration strategy: HIGH — code paths sudah dipetakan secara eksak dengan line numbers
- CLEN-03 cleanup: HIGH — orphan status sudah dikonfirmasi dari model comments dan SeedCompetencyMappings stub
- CLEN-04 relokasi: HIGH — BuildCertNumber, CreateAssessment assignment, SubmitExam target sudah diidentifikasi
- Data risk assessment: MEDIUM — jumlah actual row legacy data di production tidak diketahui tanpa query langsung ke DB

**Research date:** 2026-03-22
**Valid until:** 2026-04-22 (stable domain — codebase tidak berubah cepat)
