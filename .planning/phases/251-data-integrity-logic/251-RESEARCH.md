# Phase 251: Data Integrity & Logic - Research

**Researched:** 2026-03-24
**Domain:** ASP.NET Core — DateTime UTC consistency, EF Core migrations, business rule validation, thread-safety
**Confidence:** HIGH

## Summary

Phase 251 adalah kumpulan 6 bug fix bertarget yang sudah sangat jelas dari audit findings. Semua keputusan implementasi sudah terkunci di CONTEXT.md — tidak ada ambiguitas arsitektur. Setiap fix bersifat atomic dan tidak saling bergantung, sehingga bisa dieksekusi sebagai satu wave atau diparalelkan.

Fix terbesar adalah DATA-02 (EF Core migration untuk composite unique index) karena satu-satunya yang menghasilkan schema change dan memerlukan `dotnet ef migrations add`. Lima fix lainnya murni perubahan kode C# di file yang sudah diidentifikasi secara tepat dengan nomor baris.

**Primary recommendation:** Eksekusi semua 6 fix dalam satu plan dengan urutan: DATA-01 dan DATA-05 (kode model/controller ringan) → DATA-03 dan DATA-04 (AdminController logic) → DATA-06 (CDPController refactor) → DATA-02 (migration terakhir, setelah kode stabil).

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions
- **D-01:** Ganti `DateTime.Now` ke `DateTime.UtcNow` di 3 lokasi: `Models/TrainingRecord.cs` line 77, 91 dan `Models/CertificationManagementViewModel.cs` line 59
- **D-02:** Scope hanya 3 lokasi tersebut — tidak perlu audit seluruh codebase untuk DateTime.Now lainnya
- **D-03:** Ubah unique index `OrganizationUnit.Name` (ApplicationDbContext.cs line 532) ke composite `(ParentId, Name)`
- **D-04:** Ubah unique index `AssessmentCategory.Name` (ApplicationDbContext.cs line 559) ke composite `(ParentId, Name)`
- **D-05:** Buat EF Core migration baru dengan nama convention: `ChangeUniqueIndexToComposite`
- **D-06:** Bug isRenewalModePost hanya deteksi single renewal — bulk renewal memakai `RenewalFkMap` parameter
- **D-07:** Fix: tambahkan cek `!string.IsNullOrEmpty(RenewalFkMap)` ke `isRenewalModePost` detection
- **D-08:** Hapus atau relax validasi `model.Schedule < DateTime.Today` di EditAssessment POST line 1727
- **D-09:** Skip past-date check jika assessment sudah ada (edit mode) — existing schedule tidak boleh menghalangi edit
- **D-10:** Ganti bare `catch { /* ignore */ }` di AdminController.cs line 1437 dengan `catch (Exception ex) { _logger.LogWarning(ex, "Failed to deserialize RenewalFkMap"); }`
- **D-11:** `_logger` sudah tersedia di AdminController — tidak perlu inject baru
- **D-12:** Refactor `_lastScopeLabel` dari private instance field ke return value dari `BuildProtonProgressSubModelAsync`
- **D-13:** `BuildProtonProgressSubModelAsync` saat ini set `_lastScopeLabel = scopeLabel` — ubah agar return tuple atau tambah ScopeLabel ke return model
- **D-14:** Caller di `Dashboard()` line 286 ubah dari `model.ScopeLabel = _lastScopeLabel` ke ambil dari return value

### Claude's Discretion
- Exact migration name dan timestamp
- Apakah DATA-04 hapus validasi sepenuhnya atau hanya skip untuk existing assessments
- Apakah DATA-06 pakai tuple return atau tambah property ke existing model

### Deferred Ideas (OUT OF SCOPE)
- Tidak ada — discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Deskripsi | Research Support |
|----|-----------|-----------------|
| DATA-01 | Ganti `DateTime.Now` ke `DateTime.UtcNow` di `TrainingRecord.IsExpiringSoon`, `DaysUntilExpiry`, dan `CertificationManagementViewModel.DeriveCertificateStatus` | Lokasi exact sudah diverifikasi dari source code — 3 baris kode, perubahan keyword saja |
| DATA-02 | Ubah unique index `OrganizationUnit.Name` ke composite `(ParentId, Name)` dan `AssessmentCategory.Name` ke `(ParentId, Name)` via migration | Pattern EF Core sudah ada di codebase (SessionElemenTeknisScore line 550-552), migration infra established |
| DATA-03 | Validasi `ValidUntil` wajib diisi untuk bulk renewal — fix `isRenewalModePost` detection | Bug diverifikasi di line 1254: hanya cek model FK, tidak cek `RenewalFkMap` parameter |
| DATA-04 | Allow edit assessment yang sudah lewat jadwalnya — relax validasi past date di EditAssessment POST | Validasi ditemukan di line 1727 — perlu dihapus atau di-guard dengan edit mode check |
| DATA-05 | Log warning pada catch block `RenewalFkMap` deserialize alih-alih silent ignore | Bare catch ditemukan di line 1437, `_logger` tersedia di AdminController |
| DATA-06 | Refactor `_lastScopeLabel` dari instance field ke return value/parameter agar thread-safe | Field ditemukan di line 661, usage di line 286 dan 655 — scope refactor terbatas |
</phase_requirements>

## Standard Stack

### Core
| Library | Versi | Tujuan | Kenapa Standard |
|---------|-------|--------|----------------|
| EF Core | sudah terinstall (project existing) | Database migration untuk composite index | ORM established di project ini |
| ASP.NET Core ILogger | built-in | Logging warning pada catch block | Sudah diinjeksi ke AdminController |
| System.DateTime | built-in | UTC vs local time consistency | BCL standard |

### Supporting
| Library | Versi | Tujuan | Kapan Digunakan |
|---------|-------|--------|----------------|
| dotnet-ef CLI tool | global tool | Generate migration file | DATA-02 saja |

**Tidak ada package baru yang perlu diinstall.** Semua perubahan menggunakan library yang sudah ada di project.

## Architecture Patterns

### Pattern 1: EF Core Composite Unique Index
**Apa:** Mengganti single-column unique index ke multi-column unique index
**Kapan digunakan:** DATA-02 — `OrganizationUnit` dan `AssessmentCategory`

**Contoh existing di codebase (line 550-552 ApplicationDbContext.cs):**
```csharp
// SUDAH ADA — pattern ini sudah dipakai di project
entity.HasIndex(e => new { e.AssessmentSessionId, e.ElemenTeknis })
    .IsUnique()
    .HasDatabaseName("IX_SessionElemenTeknisScores_AssessmentSessionId_ElemenTeknis");
```

**Perubahan untuk DATA-02:**
```csharp
// SEBELUM (line 532):
entity.HasIndex(u => u.Name).IsUnique();

// SESUDAH:
entity.HasIndex(u => new { u.ParentId, u.Name }).IsUnique();

// SEBELUM (line 559):
entity.HasIndex(c => c.Name).IsUnique();

// SESUDAH:
entity.HasIndex(c => new { c.ParentId, c.Name }).IsUnique();
```

### Pattern 2: DateTime UTC Consistency
**Apa:** Ganti `DateTime.Now` (local timezone server) ke `DateTime.UtcNow` (UTC timezone)
**Dampak:** Status sertifikat "Akan Expired" dan "Expired" konsisten di semua timezone

```csharp
// SEBELUM (TrainingRecord.cs line 77):
var daysUntilExpiry = (ValidUntil.Value - DateTime.Now).Days;

// SESUDAH:
var daysUntilExpiry = (ValidUntil.Value - DateTime.UtcNow).Days;
```

**Catatan penting:** `ValidUntil` disimpan sebagai `DateTime?` di database. Jika kolom di database menyimpan nilai tanpa timezone info (unspecified kind), perbandingan dengan `UtcNow` tetap benar secara semantik — karena tanggal expired biasanya adalah tanggal kalender (end-of-day), bukan timestamp presisi.

### Pattern 3: Tuple Return untuk Thread-Safety
**Apa:** Ganti side-effect private field (`_lastScopeLabel = scopeLabel`) ke return value eksplisit
**Kenapa:** Instance field tidak aman di multi-threaded environment (ASP.NET Core controller bisa diinstansiasi per-request, tapi side-effect tetap lebih rawan)

**Opsi A — Tuple return (rekomendasi):**
```csharp
// Method signature baru:
private async Task<(ProtonProgressSubModel subModel, string scopeLabel)> BuildProtonProgressSubModelAsync(...)

// Caller di Dashboard():
var (progressData, scopeLabel) = await BuildProtonProgressSubModelAsync(user, userRole);
model.ProtonProgressData = progressData;
model.ScopeLabel = scopeLabel;
```

**Opsi B — Property di return model:**
Tambah `ScopeLabel` property ke `ProtonProgressSubModel` — lebih banyak perubahan, tidak lebih jelas.

**Rekomendasi:** Opsi A (tuple), karena minimal perubahan dan idiomatik C#.

### Pattern 4: isRenewalModePost Detection Fix
**Bug saat ini (line 1254):**
```csharp
bool isRenewalModePost = model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue;
```
Bulk renewal mengirim `RenewalFkMap` JSON string, bukan model FK fields — sehingga kondisi di atas `false` untuk bulk renewal.

**Fix (D-07):**
```csharp
bool isRenewalModePost = model.RenewsSessionId.HasValue
    || model.RenewsTrainingId.HasValue
    || !string.IsNullOrEmpty(RenewalFkMap);
```

### Pattern 5: Relax Past-Date Validation di Edit Mode
**Bug saat ini (line 1727):**
```csharp
if (model.Schedule < DateTime.Today)
    editErrors.Add("Schedule date cannot be in the past.");
```
HC tidak bisa mengedit assessment yang jadwalnya sudah lewat sama sekali.

**Fix (D-08/D-09) — skip validasi past-date di edit mode:**
```csharp
// Option A: hapus baris 1727-1728 sepenuhnya
// (HC masih bisa edit tanpa batasan schedule ke masa lalu)

// Option B: hanya blokir jika schedule jauh ke belakang (lebih dari N hari)
// Tidak diperlukan — D-09 cukup: "skip past-date check jika edit mode"
```

Karena ini adalah EditAssessment POST (bukan Create), validasi past-date tidak relevan — assessment sudah terjadwal, HC hanya mengedit field lain.

## Don't Hand-Roll

| Problem | Jangan Buat | Gunakan | Kenapa |
|---------|-------------|---------|--------|
| Schema migration | SQL manual ALTER TABLE | `dotnet ef migrations add` | EF Core handle index naming, rollback, tracking otomatis |
| Thread-safe state propagation | Lock/Mutex/ConcurrentDictionary | Return value/tuple langsung | Simplest concurrency solution — tidak perlu shared state sama sekali |

## Common Pitfalls

### Pitfall 1: Migration Conflict Jika Ada Data Existing yang Melanggar Constraint Baru
**Yang bisa salah:** Jika di database production sudah ada dua `OrganizationUnit` dengan nama sama dan `ParentId` sama, migration akan gagal saat apply.
**Kenapa terjadi:** Composite unique index baru lebih longgar dari single-column (lebih banyak kombinasi diijinkan), tapi jika data existing sudah punya duplikat di kolom Name dengan ParentId sama, tetap conflict.
**Cara menghindari:** Jalankan query cek sebelum apply migration:
```sql
-- Cek OrganizationUnits
SELECT ParentId, Name, COUNT(*) FROM OrganizationUnits
GROUP BY ParentId, Name HAVING COUNT(*) > 1;

-- Cek AssessmentCategories
SELECT ParentId, Name, COUNT(*) FROM AssessmentCategories
GROUP BY ParentId, Name HAVING COUNT(*) > 1;
```
**Warning signs:** Migration apply gagal dengan error UNIQUE constraint violation.

### Pitfall 2: ValidUntil DateTime Kind Mismatch
**Yang bisa salah:** Setelah ganti ke `DateTime.UtcNow`, jika `ValidUntil` tersimpan sebagai `DateTimeKind.Unspecified` di database, perbandingan bisa menghasilkan hasil berbeda.
**Kenapa terjadi:** SQL Server tidak menyimpan timezone info — semua DateTime disimpan sebagai unspecified kind oleh EF Core.
**Cara menghindari:** Untuk field tanggal kalender (bukan timestamp presisi), perbedaan `DateTime.Now` vs `DateTime.UtcNow` hanya signifikan jika server berjalan di timezone bukan UTC dan waktu sekarang dekat dengan midnight. Ini adalah fix yang benar secara prinsip — bug report dari CONTEXT.md mengkonfirmasi ini adalah masalah nyata.

### Pitfall 3: Tuple Deconstruction Breaking Change
**Yang bisa salah:** Jika `BuildProtonProgressSubModelAsync` dipanggil dari lokasi lain selain `Dashboard()`, refactor tuple akan compile error.
**Cara menghindari:** Verifikasi dengan search bahwa method hanya dipanggil dari satu tempat.
**Status:** CONTEXT.md menyatakan "dipanggil hanya dari `Dashboard()` (line 286)" — verifikasi saat implementasi.

### Pitfall 4: isRenewalModePost — RenewalFkMap Bisa Kosong tapi Valid
**Yang bisa salah:** Jika `RenewalFkMap` adalah `"null"` atau `"{}"` (string JSON valid tapi kosong), kondisi `!string.IsNullOrEmpty(RenewalFkMap)` akan `true` tapi tidak benar-benar bulk renewal.
**Cara menghindari:** Fix D-07 cukup — jika `RenewalFkMap` berisi JSON tapi map kosong, ValidUntil wajib diisi tetap merupakan perilaku yang benar (sudah dalam renewal context).

## Code Examples

### EF Core Migration — Generate
```bash
# Di root project
dotnet ef migrations add ChangeUniqueIndexToComposite
dotnet ef database update
```

### LogWarning Pattern (DATA-05)
```csharp
// SEBELUM (line 1437):
catch { /* ignore malformed map — fall back to model value */ }

// SESUDAH:
catch (Exception ex)
{
    _logger.LogWarning(ex, "Failed to deserialize RenewalFkMap");
}
```

### Tuple Return Async Method
```csharp
// Method signature:
private async Task<(ProtonProgressSubModel, string scopeLabel)> BuildProtonProgressSubModelAsync(
    ApplicationUser user, string? userRole)
{
    // ... existing logic ...
    // HAPUS: _lastScopeLabel = scopeLabel;
    return (subModel, scopeLabel);  // return tuple
}

// Caller (Dashboard(), line 285-286):
var (progressData, scopeLabel) = await BuildProtonProgressSubModelAsync(user, userRole);
model.ProtonProgressData = progressData;
model.ScopeLabel = scopeLabel;
// HAPUS: private string _lastScopeLabel = "";
```

## State of the Art

| Lama | Sekarang | Kapan Berubah | Dampak |
|------|----------|---------------|--------|
| `DateTime.Now` untuk expiry calc | `DateTime.UtcNow` | Phase 251 | Status sertifikat konsisten di semua server timezone |
| Single-column unique index pada Name | Composite `(ParentId, Name)` | Phase 251 via migration | Sub-unit/sub-kategori berbeda parent bisa pakai nama sama |
| Bare catch (silent ignore) | LogWarning | Phase 251 | Deserialization error jadi observable di logs |
| Private instance field `_lastScopeLabel` | Return value/tuple | Phase 251 | Thread-safe, tidak ada shared mutable state |

## Open Questions

1. **Apakah `ValidUntil` di database disimpan sebagai UTC atau local?**
   - Yang diketahui: EF Core menyimpan DateTime tanpa timezone info (Unspecified kind)
   - Yang belum jelas: Apakah input form ValidUntil (dari date picker HTML) sudah dikonversi ke UTC saat binding, atau masih local
   - Rekomendasi: Untuk phase ini, fix DATA-01 cukup mengubah sisi "now" menjadi UTC — konsistensi penuh antara stored value dan "now" akan dibahas di milestone berikutnya jika masih ada masalah

2. **Apakah `BuildProtonProgressSubModelAsync` benar-benar hanya dipanggil dari `Dashboard()`?**
   - Yang diketahui: CONTEXT.md menyatakan demikian
   - Rekomendasi: Implementor wajib grep sebelum refactor untuk konfirmasi tidak ada caller tersembunyi

## Environment Availability

Step 2.6: SKIPPED — fase ini murni perubahan kode C# + satu migration. Tidak ada dependency eksternal baru. `dotnet ef` CLI diasumsikan sudah tersedia karena project ini sudah memiliki existing migrations.

## Validation Architecture

### Test Framework
| Property | Nilai |
|----------|-------|
| Framework | Manual browser testing (project tidak menggunakan automated unit test framework) |
| Config file | none |
| Quick run command | Jalankan aplikasi dan verifikasi di browser |
| Full suite command | N/A |

### Phase Requirements → Test Map
| Req ID | Perilaku | Tipe Test | Command | File Ada? |
|--------|----------|-----------|---------|-----------|
| DATA-01 | Status sertifikat tidak berubah secara keliru karena offset timezone server | Manual — cek CertificationManagement page, verifikasi status badge | N/A (browser) | N/A |
| DATA-02 | Dua OrganizationUnit nama sama di bawah parent berbeda bisa dibuat | Manual — CRUD OrganizationUnit di Kelola Data | N/A (browser) | N/A |
| DATA-03 | Bulk renewal tanpa ValidUntil ditolak dengan pesan error | Manual — coba submit bulk renewal tanpa isi ValidUntil | N/A (browser) | N/A |
| DATA-04 | HC bisa edit assessment jadwal lewat tanpa error "past date" | Manual — buka EditAssessment untuk session jadwal kemarin, edit title, submit | N/A (browser) | N/A |
| DATA-05 | Log warning muncul di console jika RenewalFkMap malformed | Manual — inject malformed JSON, cek log output | N/A (browser/console) | N/A |
| DATA-06 | Dashboard CDP tidak menampilkan scopeLabel yang salah jika concurrent requests | Unit test tidak tersedia — code review bahwa field dihapus cukup sebagai verifikasi | N/A | N/A |

### Sampling Rate
- **Per task commit:** Build project — pastikan tidak ada compile error
- **Per wave merge:** Jalankan `dotnet build` + browser smoke test
- **Phase gate:** Semua 6 kriteria sukses terverifikasi manual sebelum `/gsd:verify-work`

### Wave 0 Gaps
- Tidak ada test infrastructure yang perlu dibuat — project menggunakan manual testing

## Project Constraints (dari CLAUDE.md)

- Semua respons dalam Bahasa Indonesia
- Tidak ada constraint teknis khusus dari CLAUDE.md di luar bahasa

## Sources

### Primary (HIGH confidence)
- Source code langsung dibaca dari disk:
  - `Models/TrainingRecord.cs` line 70-97 — konfirmasi `DateTime.Now` di 2 property
  - `Models/CertificationManagementViewModel.cs` line 53-63 — konfirmasi `DateTime.Now` di `DeriveCertificateStatus`
  - `Data/ApplicationDbContext.cs` line 529-563 — konfirmasi single-column unique index pada Name
  - `Controllers/AdminController.cs` line 1179, 1254-1259, 1430-1438, 1720-1730 — konfirmasi semua 3 bug AdminController
  - `Controllers/CDPController.cs` line 280-286, 650-661 — konfirmasi `_lastScopeLabel` pattern
- `251-CONTEXT.md` — keputusan terkunci dari /gsd:discuss-phase
- `REQUIREMENTS.md` — DATA-01 hingga DATA-06 definitions

### Secondary (MEDIUM confidence)
- Tidak ada source eksternal yang diperlukan — semua informasi dari kode existing

### Tertiary (LOW confidence)
- Tidak ada

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — hanya library existing, tidak ada yang baru
- Architecture: HIGH — semua perubahan diverifikasi dari source code dengan nomor baris
- Pitfalls: HIGH — derived dari code review langsung, bukan asumsi
- Migration: MEDIUM — perlu verifikasi data existing tidak conflict sebelum apply

**Research date:** 2026-03-24
**Valid until:** 90 hari (stable ASP.NET Core patterns, tidak ada dependency fast-moving)
