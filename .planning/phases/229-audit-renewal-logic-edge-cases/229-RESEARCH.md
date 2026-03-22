# Phase 229: Audit Renewal Logic & Edge Cases - Research

**Researched:** 2026-03-22
**Domain:** ASP.NET Core MVC — renewal certificate backend logic, FK chain, badge sync, status derivation, grouping, edge cases
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Fix kode agar 4 kombinasi FK renewal (AS→AS, AS→TR, TR→TR, TR→AS) selalu ter-set dengan benar
- **D-02:** Generate HTML audit report untuk data existing yang FK-nya bermasalah (format seperti audit report v7.7)
- **D-03:** Tidak melakukan data migration — hanya fix kode ke depan + dokumentasi data lama
- **D-05:** Refactor semua tempat yang hitung badge count ke single source `BuildRenewalRowsAsync` — hapus counting duplicate di tempat lain
- **D-06:** `DeriveCertificateStatus`: null ValidUntil + non-Permanent = Expired — perilaku saat ini sudah benar, keep as-is
- **D-07:** Audit apakah AssessmentSession perlu field CertificateType (saat ini dipanggil dengan null). Jika assessment memang seharusnya bisa Permanent, tambahkan field
- **D-08:** Fix MapKategori di Phase 229 — sinkronkan dengan AssessmentCategories database (bukan hardcode). AssessmentCategories.Name di DB adalah canonical source
- **D-09:** Verifikasi semua tempat yang decode GroupKey pakai logika encoding/decoding yang konsisten dengan Base64 URL-safe di line 7089-7090
- **D-10:** Verifikasi server-side check pada action RenewCertificate — pastikan bukan hanya UI filter (!IsRenewed) tapi juga ada server-side guard
- **D-11:** Tolak mixed batch (campuran Assessment + Training dalam satu bulk renew). Tampilkan error — user harus renew per tipe
- **D-12:** Tampilkan pesan informatif sederhana "Tidak ada sertifikat yang perlu di-renew saat ini" dengan icon checkmark saat list kosong

### Claude's Discretion
- Level validasi FK constraint (D-04) — analisa dan pilih level validasi terbaik (controller-level, model IValidatableObject, atau DB constraint) untuk enforce "hanya 1 dari RenewsTrainingId/RenewsSessionId boleh diisi"
- Pendekatan terbaik untuk sinkronisasi MapKategori dengan DB (lookup vs join vs cache)
- Detail implementasi HTML audit report

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| LDAT-01 | Renewal chain FK 4 kombinasi (AS→AS, AS→TR, TR→TR, TR→AS) semua set dengan benar saat renew | Audit kode AddTraining (bulk + single) dan CreateAssessment — temuan: bulk path TR sudah benar, tapi perlu verifikasi CreateAssessment path untuk AS→AS dan AS→TR |
| LDAT-02 | Badge count Admin/Index sinkron dengan BuildRenewalRowsAsync (single source of truth) | Admin/Index sudah pakai BuildRenewalRowsAsync — LDAT-02 sudah terpenuhi, perlu verifikasi tidak ada tempat lain yang hitung renewal count secara independen |
| LDAT-03 | DeriveCertificateStatus handle semua edge case (null ValidUntil, Permanent, expired, akan expired) | CertificationManagementViewModel.cs:53 — kode sudah handle semua kasus, keep as-is (D-06) |
| LDAT-04 | Grouping by Judul case-insensitive dan karakter khusus URL-safe | AdminController:7086 sudah pakai StringComparer.OrdinalIgnoreCase + Base64 URL-safe — perlu verifikasi decode consistency di semua consumer |
| LDAT-05 | MapKategori konsisten dengan AssessmentCategories naming | MapKategori hardcode — perlu refactor ke DB lookup |
| EDGE-01 | Bulk renew mixed-type (campuran Assessment + Training) validasi dan flow benar | Belum ada mixed-type guard di bulk renew path — perlu ditambahkan |
| EDGE-02 | Double renewal prevention — sertifikat yang sudah di-renew tidak bisa di-renew lagi | UI filter `!IsRenewed` ada, server-side guard belum terverifikasi |
| EDGE-03 | Empty state handling saat tidak ada sertifikat yang perlu di-renew | View partial belum diverifikasi memiliki empty state message informatif |
</phase_requirements>

---

## Summary

Phase 229 adalah audit + fix backend logic renewal certificate di Portal HC KPB. Berdasarkan pembacaan kode aktual, sebagian besar infrastruktur sudah solid: `BuildRenewalRowsAsync` sudah menjadi single source of truth untuk badge count, `DeriveCertificateStatus` sudah handle semua edge case, dan grouping sudah case-insensitive + Base64 URL-safe.

Bug nyata yang ditemukan: (1) `MapKategori` masih hardcode 2 mapping saja dan tidak sinkron dengan AssessmentCategories DB, (2) tidak ada server-side guard untuk double renewal pada action RenewCertificate, (3) tidak ada validasi mixed-type pada bulk renew batch, (4) perlu verifikasi apakah semua consumer GroupKey menggunakan decoding yang konsisten. CDPController memiliki `BuildSertifikatRowsAsync` yang merupakan duplicate dari AdminController `BuildRenewalRowsAsync` — keduanya sudah identical dalam renewal chain resolution logic, sehingga tidak ada bug inkonsistensi tapi ada code duplication.

**Primary recommendation:** Fix 4 bug nyata (MapKategori → DB lookup, double renewal server-side guard, mixed-type validation, empty state message), generate audit report data existing, verifikasi D-07 (CertificateType di AssessmentSession).

---

## Standard Stack

### Core (Project Stack)
| Komponen | Detail | Purpose |
|----------|--------|---------|
| ASP.NET Core MVC | .NET (existing) | Controller/View pattern |
| Entity Framework Core | existing | ORM untuk query renewal data |
| `IValidatableObject` | System.ComponentModel.DataAnnotations | Model-level validation (kandidat D-04) |
| C# switch expression | existing | Pattern untuk MapKategori refactor |

### Patterns yang Sudah Ada
| Pattern | Lokasi | Status |
|---------|--------|--------|
| `BuildRenewalRowsAsync` | AdminController:6704 | Single source of truth, sudah benar |
| `DeriveCertificateStatus` | CertificationManagementViewModel:53 | Handle semua edge case, keep as-is |
| Base64 URL-safe GroupKey | AdminController:7089-7090 | Encoding sudah benar, perlu verifikasi decode |
| FK chain 4-set resolution | AdminController:6728-6762 | Logic sudah correct, ada di dua controller |

---

## Architecture Patterns

### Kode yang Perlu Diubah

#### 1. MapKategori — AdminController:6696 (juga ada di CDPController)
```csharp
// CURRENT (hardcode):
private static string MapKategori(string? raw) => raw?.Trim().ToUpperInvariant() switch
{
    null or "" => "-",
    "MANDATORY" => "Mandatory HSSE Training",
    "PROTON"    => "Assessment Proton",
    _           => raw!.Trim()
};

// RECOMMENDED (DB lookup — inject via parameter):
// Refactor menjadi instance method yang menerima Dictionary<string, string> lookup
// Dictionary di-build dari AssessmentCategories (Name, canonical display) saat BuildRenewalRowsAsync dipanggil
// Alternatif: static Dictionary yang di-populate dari DB di awal method
```

Pattern yang paling bersih berdasarkan existing code di AdminController:6784-6791 (sudah ada `categoryNameLookup` dictionary dari DB) adalah **tambahkan lookup untuk TrainingRecord.Kategori** menggunakan AssessmentCategories sebagai canonical source.

Perhatian: TrainingRecord.Kategori berisi raw string (contoh: "MANDATORY", "PROTON", "OJT") sementara AssessmentCategories.Name berisi display names (contoh: "Mandatory HSSE Training", "Assessment Proton"). Mapping ini perlu disimpan di DB atau di-query dengan normalized comparison.

#### 2. Double Renewal Server-Side Guard
```csharp
// Perlu ditambahkan di action yang menerima renewal (AddTraining POST, CreateAssessment POST):
// Check: apakah source record sudah punya renewal yang valid?

// Untuk TR source:
var alreadyRenewed = await _context.TrainingRecords
    .AnyAsync(t => t.RenewsTrainingId == sourceId);
// atau via AssessmentSessions
var alreadyRenewedByAS = await _context.AssessmentSessions
    .AnyAsync(a => a.RenewsTrainingId == sourceId && a.IsPassed == true);

if (alreadyRenewed || alreadyRenewedByAS)
    return BadRequest("Sertifikat ini sudah di-renew.");
```

#### 3. Mixed-Type Bulk Validation
```csharp
// Di bulk renew dispatch (modal/form yang mengirim batch ke AddTraining atau CreateAssessment):
// Sebelum submit, validasi: semua items dalam batch harus RecordType yang sama
// Server-side: jika fkMapType campuran atau ada training IDs + session IDs sekaligus → reject

// Pattern: cek di awal action setelah deserialize fkMap
if (hasMixedTypes)
{
    TempData["Error"] = "Bulk renewal tidak dapat mencampur tipe Assessment dan Training. Renew per tipe secara terpisah.";
    return RedirectToAction("RenewalCertificate");
}
```

#### 4. D-04 — FK Mutual Exclusion Validation
**Rekomendasi:** Controller-level validation (bukan IValidatableObject atau DB constraint).

Alasan:
- `IValidatableObject` di Model tidak memiliki akses ke context untuk query FK validity
- DB constraint (CHECK constraint) bisa ditambahkan tapi tidak memberikan user-friendly error message
- Controller-level guard sudah ada pola serupa di codebase (ModelState.AddModelError pattern)

```csharp
// Di AddTraining POST dan CreateAssessment POST — tambahkan guard:
if (model.RenewsTrainingId.HasValue && model.RenewsSessionId.HasValue)
{
    ModelState.AddModelError("", "Renewal FK tidak valid: hanya boleh mengisi salah satu dari RenewsTrainingId atau RenewsSessionId.");
    // return View(model)
}
```

#### 5. D-07 — AssessmentSession CertificateType Audit
`AssessmentSession` saat ini **tidak punya field `CertificateType`**. `DeriveCertificateStatus` dipanggil dengan `certificateType: null` untuk semua assessment rows (AdminController:6834, CDPController:3380). Null → ValidUntil==null → Permanent (per kode baris 57 CertificationManagementViewModel).

Jika assessment seharusnya bisa Permanent: tambahkan field `CertificateType` ke `AssessmentSession` dengan migration. Jika tidak (semua assessment punya expiry), behavior null=Permanent perlu dikaji — assessment yang ValidUntil==null akan tampil sebagai Permanent bukan Expired.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| FK mutual exclusion | Custom DB trigger | Controller-level guard + optional CHECK constraint | Trigger sulit di-debug, controller guard lebih maintainable |
| Category mapping | Second hardcode table | Query AssessmentCategories + build Dictionary in-memory | DB adalah canonical source (D-08) |
| Base64 encode/decode | Custom URL encoding | `Convert.ToBase64String` + `.Replace("+","_").Replace("/","-").Replace("=","")` (sudah ada) | Sudah established di codebase, konsistensi lebih penting |

---

## Common Pitfalls

### Pitfall 1: CDPController Duplicate Logic
**What goes wrong:** CDPController `BuildSertifikatRowsAsync` (baris 3200+) adalah near-duplicate dari AdminController `BuildRenewalRowsAsync`. Jika fix diterapkan di AdminController saja, CDPController akan tetap punya bug.
**Why it happens:** Dua controller memiliki kebutuhan berbeda (scoping per role) sehingga tidak mudah di-share.
**How to avoid:** Setiap fix di AdminController harus di-mirror ke CDPController untuk logika yang identik (renewal chain resolution, MapKategori, DeriveCertificateStatus).
**Warning signs:** CDPController:3312 sudah panggil `MapKategori` — jika MapKategori di-refactor, CDPController juga perlu diupdate.

### Pitfall 2: MapKategori Refactor Breaking Assessment Path
**What goes wrong:** AssessmentSession.Category sudah berisi nama display langsung (contoh: "Assessment Proton", "IHT") — ini tidak melewati `MapKategori`. TrainingRecord.Kategori berisi raw codes ("PROTON", "MANDATORY"). Jika refactor MapKategori salah, Training records akan muncul dengan Kategori yang tidak match Assessment records.
**Why it happens:** Dua model menyimpan kategori dalam format berbeda.
**How to avoid:** MapKategori hanya dipakai untuk TrainingRecord rows. AssessmentSession rows menggunakan DB lookup langsung. Pastikan refactor MapKategori tidak mengubah behavior untuk AssessmentSession path.

### Pitfall 3: GroupKey Decoding Inconsistency
**What goes wrong:** GroupKey di-encode dengan Base64 URL-safe (replace `+`→`_`, `/`→`-`, `=`→`""`). Decoding harus reverse persis: replace `_`→`+`, `-`→`/`, pad dengan `=` sesuai panjang. Jika ada consumer yang tidak reverse dengan benar, group tidak akan ditemukan.
**Why it happens:** Base64 URL-safe encoding manual tanpa helper class.
**How to avoid:** Cari semua tempat yang decode GroupKey — FilterRenewalCertificateGroup menggunakan `judul` parameter langsung (tidak decode GroupKey), tapi modal/JS mungkin decode. Verifikasi setiap path.
**Warning signs:** `Uri.UnescapeDataString` di baris 7138 untuk `judul` parameter — ini bukan Base64 decode, ini URL decode. Jika ada tempat yang mengirim GroupKey sebagai judul tanpa decode, akan salah.

### Pitfall 4: TR Bulk Renewal — RenewsTrainingId/RenewsSessionId Tidak Di-set Saat Kategori Kosong
**What goes wrong:** Di AddTraining bulk path (baris 5607-5611), jika `fkMap` null atau uid tidak ada di fkMap, FK tidak di-set sama sekali. Record ter-create tanpa renewal chain.
**Why it happens:** fkMap bisa null jika request tidak mengirimkan `renewalFkMap` form field.
**How to avoid:** Validasi bahwa untuk renewal mode, fkMap tidak boleh null dan semua userId harus ada di map.

---

## Code Examples

### DeriveCertificateStatus — Status quo (keep as-is per D-06)
```csharp
// Source: Models/CertificationManagementViewModel.cs:53
public static CertificateStatus DeriveCertificateStatus(DateTime? validUntil, string? certificateType)
{
    if (certificateType == "Permanent")
        return CertificateStatus.Permanent;
    if (validUntil == null)
        return CertificateStatus.Expired; // non-Permanent dengan null ValidUntil = perlu renewal
    var days = (validUntil.Value - DateTime.Now).Days;
    if (days < 0) return CertificateStatus.Expired;
    if (days <= 30) return CertificateStatus.AkanExpired;
    return CertificateStatus.Aktif;
}
// CATATAN: AssessmentSession tidak punya CertificateType — dipanggil dengan null
// → ValidUntil==null pada AssessmentSession = Permanent (bukan Expired)
// D-07: perlu audit apakah ini benar atau perlu CertificateType field di AssessmentSession
```

### Base64 URL-safe GroupKey Encoding (existing)
```csharp
// Source: AdminController.cs:7089-7090
GroupKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(g.Key))
                 .Replace("+", "_").Replace("/", "-").Replace("=", ""),
// Decoding (untuk consumer yang perlu):
// var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(
//     groupKey.Replace("_", "+").Replace("-", "/") + new string('=', (4 - groupKey.Length % 4) % 4)));
```

### Renewal Chain Resolution (existing, sudah benar)
```csharp
// Source: AdminController.cs:6728-6762
// 4 set query:
// Set 1: AS IDs renewed by AS (IsPassed==true)
// Set 2: AS IDs renewed by TR
// Set 3: TR IDs renewed by AS (IsPassed==true)
// Set 4: TR IDs renewed by TR
// Merge ke 2 HashSet: renewedAssessmentSessionIds, renewedTrainingRecordIds
// IsRenewed = HashSet.Contains(SourceId)
```

### HTML Audit Report Pattern (referensi v7.7)
Format audit report yang diharapkan: HTML standalone dengan tabel berisi data dari DB query yang menunjukkan records dengan FK bermasalah (RenewsTrainingId/RenewsSessionId null padahal dibuat dalam context renewal).

---

## State of the Art

| Area | Status Saat Ini | Target Phase 229 |
|------|----------------|------------------|
| MapKategori | Hardcode 2 mapping | DB lookup dari AssessmentCategories |
| Badge count | Single source (sudah benar) | Keep + verifikasi tidak ada duplicate |
| DeriveCertificateStatus | Handle semua kasus | Keep as-is + audit CertificateType di AssessmentSession |
| GroupKey encoding | Base64 URL-safe (sudah benar) | Verifikasi decode consistency |
| Double renewal | UI filter saja | Tambahkan server-side guard |
| Mixed-type bulk | Tidak ada validasi | Tambahkan rejection + error message |
| Empty state | Belum diverifikasi | Pesan informatif + icon checkmark |
| FK mutual exclusion | Tidak ada validation | Controller-level guard |

---

## Open Questions

1. **AssessmentSession.CertificateType (D-07)**
   - What we know: Field tidak ada. `DeriveCertificateStatus` dipanggil dengan `null` untuk semua assessment. ValidUntil==null → Permanent (bukan Expired).
   - What's unclear: Apakah ada assessment yang seharusnya Permanent? Atau semua assessment punya expiry dan `null` ValidUntil seharusnya = Expired?
   - Recommendation: Audit data aktual — query `AssessmentSessions WHERE GenerateCertificate=true AND IsPassed=true AND ValidUntil IS NULL`. Jika ada records, tentukan apakah itu intentional (Permanent) atau data entry error (perlu expiry date).

2. **Double Renewal — Definisi "sudah di-renew"**
   - What we know: `IsRenewed = renewedTrainingRecordIds.Contains(t.Id)` — true jika ada record lain yang menunjuk ke sertifikat ini.
   - What's unclear: Apakah renewal yang failed (AS dengan IsPassed=false) dihitung? Saat ini TIDAK dihitung (Set 1 dan Set 3 filter `IsPassed==true`). Ini sudah benar.
   - Recommendation: Keep behavior saat ini — hanya renewal yang berhasil yang mencegah re-renewal.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (project pattern dari MEMORY.md) |
| Config file | Tidak ada test framework otomatis |
| Quick run command | `dotnet build` — verify compilation |
| Full suite command | Browser smoke test per use-case flow |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| LDAT-01 | FK 4 kombinasi ter-set benar saat renew | Manual browser + DB query | `dotnet build` | N/A |
| LDAT-02 | Badge count = BuildRenewalRowsAsync.Count | Code review | `dotnet build` | N/A |
| LDAT-03 | DeriveCertificateStatus edge cases | Code review (keep as-is) | `dotnet build` | N/A |
| LDAT-04 | GroupKey Base64 decode consistency | Code review + manual test | `dotnet build` | N/A |
| LDAT-05 | MapKategori = AssessmentCategories.Name | Manual browser + DB compare | `dotnet build` | N/A |
| EDGE-01 | Mixed-type bulk → error message | Manual browser test | `dotnet build` | N/A |
| EDGE-02 | Double renewal → blocked server-side | Manual browser test | `dotnet build` | N/A |
| EDGE-03 | Empty state message | Manual browser (kosongkan semua renewal) | `dotnet build` | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build` — pastikan tidak ada compilation error
- **Per wave merge:** Browser smoke test seluruh renewal flow
- **Phase gate:** Semua requirement verified sebelum `/gsd:verify-work`

### Wave 0 Gaps
None — tidak ada test framework baru yang perlu di-setup. Project menggunakan manual browser testing.

---

## Sources

### Primary (HIGH confidence)
- `Controllers/AdminController.cs` baris 55-64, 6696-6853, 7080-7176 — Index badge count, BuildRenewalRowsAsync, grouping logic (dibaca langsung)
- `Controllers/CDPController.cs` baris 3050-3390 — BuildSertifikatRowsAsync duplicate (dibaca langsung)
- `Models/CertificationManagementViewModel.cs` baris 1-122 — SertifikatRow, DeriveCertificateStatus, RenewalGroup (dibaca langsung)
- `Models/TrainingRecord.cs` baris 46-57 — RenewsTrainingId/RenewsSessionId FK fields (dibaca langsung)
- `Models/AssessmentSession.cs` baris 109-122 — RenewsSessionId/RenewsTrainingId FK fields (dibaca langsung)
- `Models/AssessmentCategory.cs` — AssessmentCategory model (dibaca langsung)
- `Controllers/AdminController.cs` baris 5539-5638 — AddTraining bulk renewal path (dibaca langsung)
- `Controllers/AdminController.cs` baris 1075-1124 — CreateAssessment renewal pre-fill + MapKategori usage (dibaca langsung)

### Secondary (MEDIUM confidence)
- CONTEXT.md baris 53-85 — Canonical references dan locked decisions dari discussion phase

### Tertiary (LOW confidence)
- Tidak ada

---

## Metadata

**Confidence breakdown:**
- Bug identification: HIGH — berdasarkan pembacaan kode aktual, bukan asumsi
- Fix approach: HIGH — berdasarkan established patterns di codebase
- D-07 assessment: MEDIUM — perlu query data aktual untuk konfirmasi
- HTML audit report format: MEDIUM — berdasarkan referensi v7.7 (file belum dibaca)

**Research date:** 2026-03-22
**Valid until:** 2026-04-22 (stable codebase, tidak ada dependency external)
