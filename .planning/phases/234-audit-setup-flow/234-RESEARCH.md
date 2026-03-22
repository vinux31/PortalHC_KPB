# Phase 234: Audit Setup Flow - Research

**Researched:** 2026-03-22
**Domain:** ASP.NET Core MVC — Proton Coaching Setup Integrity (silabus, guidance files, coach-coachee mapping, track assignment, import/export)
**Confidence:** HIGH — berdasarkan audit langsung kode existing

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions
- **D-01:** Hard delete diblokir jika ada progress aktif — hanya soft delete (IsActive=false) yang diizinkan. Hard delete hanya untuk silabus tanpa progress.
- **D-02:** Modal warning menampilkan summary impact count: "Deliverable ini digunakan oleh X coachee dengan Y progress aktif"
- **D-03:** Safety check di semua level: Deliverable, SubKompetensi, dan Kompetensi — setiap level cek apakah ada progress aktif di bawahnya
- **D-04:** Auto orphan cleanup setelah delete: hapus SubKompetensi kosong, lalu Kompetensi kosong — dalam transaction
- **D-05:** CoachCoacheeMappingDeactivate dibungkus dalam explicit DB transaction (BeginTransactionAsync) — rollback semua jika cascade gagal
- **D-06:** SilabusDelete cascade (progress → sessions → action items → orphan cleanup) dibungkus dalam transaction
- **D-07:** GuidanceReplace: upload file baru dulu, update DB, baru delete file lama — jika upload gagal, file lama tetap ada
- **D-08:** CoachCoacheeMappingReactivate: Claude decides — assess risk timestamp matching ±5 detik dan tentukan apakah perlu transaction + matching improvement
- **D-09:** Warning only, bukan block — HC/Admin tetap bisa assign Tahun 2/3 langsung setelah konfirmasi warning
- **D-10:** Definisi "selesai": semua ProtonDeliverableProgress di track tersebut status Approved (semua level approval selesai)
- **D-11:** Reactivated coachee boleh langsung assign Tahun 2/3 tanpa harus selesaikan tahun sebelumnya
- **D-12:** Validasi warning diterapkan di assign action + import — kedua jalur menampilkan warning
- **D-13:** All-or-nothing rollback — jika ada 1 baris error, rollback semua. Tidak ada data yang masuk sampai semua baris valid
- **D-14:** Error reporting: per-row status table (No Baris | Status | Pesan Error) tampil di halaman setelah import
- **D-15:** Duplikasi detection: skip + report — jika data sudah ada (active), skip dan report sebagai "Skipped: sudah ada"
- **D-16:** Template validation: server-side cek header kolom Excel cocok dengan template — reject jika kolom salah/kurang

### Claude's Discretion
- Implementasi detail modal warning UI (styling, animation)
- CoachCoacheeMappingReactivate transaction + matching strategy (D-08)
- Exact validation messages dan error wording
- Guidance file type whitelist details (server-side)
- How to surface progression warning di UI (inline alert vs modal)

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SETUP-01 | Audit Silabus delete — tambah impact count warning sebelum hard delete, soft delete jika ada progress aktif | Bug ditemukan: SilabusDelete (L506) tidak cek progress aktif sama sekali; DeleteKompetensi (L1278) ada transaction tapi tidak ada pre-check impact |
| SETUP-02 | Audit Coaching Guidance — file management integrity (upload/replace/delete), validasi tipe file | Bug ditemukan: GuidanceReplace (L963) menghapus file lama SEBELUM upload baru — berlawanan dengan D-07 |
| SETUP-03 | Audit Coach-Coachee Mapping — tambah explicit DB transaction pada cascade deactivation, validasi duplikasi | Bug ditemukan: CoachCoacheeMappingDeactivate (L4230) tidak ada transaction — jika cascade gagal, mapping sudah terdeaktivasi tapi assignments belum |
| SETUP-04 | Audit Track Assignment — progression validation Tahun 1→2→3, seed ProtonDeliverableProgress correctness | Gap ditemukan: tidak ada progression validation warning di CoachCoacheeMappingAssign — bisa langsung assign Tahun 2/3 tanpa warning |
| SETUP-05 | Audit Import/Export Silabus dan Mapping — validasi data, error handling, template accuracy | Bug ditemukan: ImportSilabus tidak all-or-nothing (partial commit per baris); tidak ada header validation; ImportCoachCoacheeMapping sudah partial-safe tapi juga tidak atomic |
</phase_requirements>

---

## Summary

Audit kode existing menemukan beberapa gap nyata yang sesuai dengan kekhawatiran CONTEXT.md. Setiap requirement SETUP-01 s/d SETUP-05 memiliki bug atau gap yang bisa menyebabkan data corrupt di execution flow berikutnya.

Bug paling kritis adalah **GuidanceReplace** yang menghapus file lama sebelum upload baru berhasil (SETUP-02) — jika upload gagal, file lama hilang permanen. Yang kedua adalah **ImportSilabus** yang melakukan `SaveChangesAsync()` per baris di dalam loop — jika baris 50 error, baris 1-49 sudah commit ke database (SETUP-05).

**Primary recommendation:** Setiap task di phase ini adalah FIX bukan feature baru — fokus pada koreksi logika yang sudah ada, bukan penulisan ulang besar-besaran. Gunakan pattern transaction yang sudah ada di `DeleteKompetensi` (L1278) sebagai template.

---

## Standard Stack

### Core (tidak berubah — stack existing)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| EF Core | Existing | DB access + transactions | `BeginTransactionAsync` sudah dipakai di DeleteKompetensi |
| ClosedXML (XLWorkbook) | Existing | Excel import/export | Sudah dipakai di semua import/export action |
| ASP.NET Core MVC | Existing | Controller layer | Project framework |

**Tidak perlu library baru.** Semua fix menggunakan API yang sudah ada.

---

## Architecture Patterns

### Pattern 1: Transaction Template dari DeleteKompetensi (L1278)
**Apa:** `BeginTransactionAsync` → operasi → `CommitAsync`, dengan `catch` yang memanggil `RollbackAsync`
**Kapan pakai:** SilabusDelete (L506) dan CoachCoacheeMappingDeactivate (L4230) harus mengadopsi pola ini
**Contoh dari kode existing:**
```csharp
// ProtonDataController.cs L1278 — template transaction yang benar
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // ... operasi cascade ...
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### Pattern 2: Impact Count Check sebelum Hard Delete
**Apa:** GET endpoint terpisah yang menghitung jumlah coachee dan progress aktif untuk ditampilkan di modal
**Kapan pakai:** Sebelum SilabusDelete di semua level (Deliverable, SubKompetensi, Kompetensi)
**Pola yang sudah ada:** `CoachCoacheeMappingDeletePreview` (L4365) — endpoint GET yang return JSON count, dipanggil dari JS modal. Adoptasi pola ini.

```csharp
// Pola untuk impact check endpoint
[HttpGet]
public async Task<IActionResult> SilabusDeletePreview(int deliverableId)
{
    var progressCount = await _context.ProtonDeliverableProgresses
        .Where(p => p.ProtonDeliverableId == deliverableId)
        .CountAsync();
    var coacheeCount = await _context.ProtonDeliverableProgresses
        .Where(p => p.ProtonDeliverableId == deliverableId)
        .Select(p => p.CoacheeId).Distinct().CountAsync();
    var hasActive = await _context.ProtonDeliverableProgresses
        .AnyAsync(p => p.ProtonDeliverableId == deliverableId
                    && p.Status != "Approved");
    return Json(new { progressCount, coacheeCount, hasActive });
}
```

### Pattern 3: GuidanceReplace — upload-first, delete-old-last
**Apa:** Upload file baru ke disk → update DB → BARU hapus file lama. Jika upload gagal (exception), file lama masih ada.
**Bug saat ini (L982-1005):** Delete file lama dilakukan SEBELUM upload baru (L983 `File.Delete` sebelum `CopyToAsync` L994).
**Fix:**
```csharp
// BENAR: upload dulu
var safeFileName = $"...";
var newFilePath = Path.Combine(uploadDir, safeFileName);
using (var stream = new FileStream(newFilePath, FileMode.Create))
    await file.CopyToAsync(stream); // jika ini gagal, file lama belum disentuh

// Baru hapus yang lama
var oldPath = ...;
if (System.IO.File.Exists(oldPath))
    System.IO.File.Delete(oldPath);

// Update DB
record.FilePath = $"/uploads/guidance/{safeFileName}";
await _context.SaveChangesAsync();
```

### Pattern 4: All-or-nothing Import dengan Transaction
**Apa:** Parse semua baris → validasi semua → jika ada error, return tanpa SaveChanges. Jika clean, wrap dalam transaction.
**Bug saat ini ImportSilabus (L720-873):** `SaveChangesAsync()` dipanggil di dalam loop per baris (L813, L831, L858) — partial commit tidak bisa di-rollback.
**Fix approach:** Dua-pass strategy:
1. Pass 1: Validasi semua baris → kumpulkan errors → jika ada errors, return error report tanpa sentuh DB
2. Pass 2 (jika clean): Bungkus semua insert dalam satu transaction
**Catatan:** Header validation juga harus ditambahkan di awal sebelum proses baris:
```csharp
// Validate header baris pertama
var expectedHeaders = new[] { "Bagian", "Unit", "Track", "Kompetensi", "SubKompetensi", "Deliverable", "Target" };
for (int i = 0; i < expectedHeaders.Length; i++)
{
    var actual = ws.Cell(1, i + 1).GetString().Trim();
    if (!actual.Equals(expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
        return error("Header kolom tidak cocok dengan template.");
}
```

### Pattern 5: Progression Warning di Track Assignment
**Apa:** Sebelum assign Tahun 2/3, cek apakah Tahun sebelumnya sudah semua Approved. Jika belum, return warning (bukan block).
**Di mana warning muncul:** Frontend tampilkan confirm modal — user klik "Lanjutkan" untuk tetap assign.
**Definisi selesai (D-10):** Semua ProtonDeliverableProgress untuk assignment Tahun N di coachee tersebut berstatus "Approved".
```csharp
// Cek apakah ada Tahun sebelumnya yang belum selesai
var prevTrack = await _context.ProtonTracks
    .FirstOrDefaultAsync(t => t.TahunKe == prevTahunKe);
if (prevTrack != null)
{
    var prevAssignment = await _context.ProtonTrackAssignments
        .FirstOrDefaultAsync(a => a.CoacheeId == coacheeId
                                && a.ProtonTrackId == prevTrack.Id);
    if (prevAssignment != null)
    {
        var allApproved = !await _context.ProtonDeliverableProgresses
            .AnyAsync(p => p.ProtonTrackAssignmentId == prevAssignment.Id
                        && p.Status != "Approved");
        if (!allApproved)
            return Json(new { success = false, warning = true,
                message = $"Coachee belum menyelesaikan {prevTahunKe}. Tetap lanjutkan?" });
    }
}
```

### Anti-Patterns yang Ditemukan di Kode Existing
- **SilabusDelete tanpa transaction:** L506-565 melakukan cascade delete (sessions → progresses → deliverable → orphan cleanup) dengan multiple `SaveChangesAsync` tanpa transaction — jika crash di tengah, data partial corrupt
- **ImportSilabus commit per baris:** L813/831/858 SaveChangesAsync di dalam loop — jika error di baris 50, baris 1-49 sudah masuk DB
- **GuidanceReplace delete-before-upload:** L983 delete file lama sebelum L994 upload baru — file loss jika upload gagal
- **CoachCoacheeMappingDeactivate tanpa transaction:** L4242-4258 deactivate mapping + cascade assignments dengan single SaveChangesAsync — belum cukup; jika ada exception setelah `mapping.IsActive = false` namun sebelum assignments, state inconsistent

---

## Don't Hand-Roll

| Problem | Jangan Buat | Gunakan |
|---------|-------------|---------|
| DB transaction | Custom flag/compensating action | `_context.Database.BeginTransactionAsync()` — sudah ada di DeleteKompetensi |
| File type validation | Magic byte inspection | Extension check sudah ada di GuidanceUpload — tinggal pastikan GuidanceReplace konsisten |
| Excel parsing | Custom CSV/binary parser | XLWorkbook (ClosedXML) — sudah dipakai |
| Impact count query | In-memory calculation | EF `.CountAsync()` dan `.AnyAsync()` — efisien di DB level |

---

## Temuan Bug per Requirement

### SETUP-01: Silabus Delete Safety

**Gap 1 — SilabusDelete (L506) tidak ada impact check:**
- Tidak ada cek apakah deliverable punya progress aktif
- Langsung hapus progress + sessions + deliverable
- Tidak ada transaction wrapping
- **Fix:** Tambah GET endpoint `SilabusDeletePreview(deliverableId)` → return impact count, dan wrap SilabusDelete dalam transaction

**Gap 2 — DeleteKompetensi (L1278) ada transaction tapi tidak ada pre-check:**
- Ada `BeginTransactionAsync` yang bagus
- Tapi tidak ada cek "apakah ada progress aktif?" sebelum delete
- Menurut D-01: harusnya blokir hard delete jika ada progress aktif, tawarkan soft delete
- **Fix:** Tambah check progress aktif di awal DeleteKompetensi; jika ada, return `{ success: false, hasActiveProgress: true, coacheeCount: X, progressCount: Y }`

**Gap 3 — SilabusDeactivate (L570) hanya deactivate level Kompetensi:**
- Tidak cascade ke SubKompetensi dan Deliverable
- D-03 minta safety check di semua level
- **Fix:** Verifikasi apakah cukup deactivate di level Kompetensi saja (karena query biasanya filter `k.IsActive`) — jika query di bawahnya sudah filter via parent, mungkin tidak perlu cascade IsActive flag, tapi perlu dikonfirmasi dengan audit query di ExportSilabus dan PlanIdp

### SETUP-02: Coaching Guidance File Management

**Bug Kritis — GuidanceReplace urutan operasi salah (L982-1005):**
```
SAAT INI (BUGGY):
1. Hapus file lama dari disk (L983-987)     ← FILE HILANG DULU
2. Upload file baru ke disk (L994-997)       ← baru upload
3. Update DB record (L1000-1005)

SEHARUSNYA (D-07):
1. Upload file baru ke disk
2. Update DB record
3. Hapus file lama (non-critical, wrapped in try-catch)
```

**Gap — GuidanceUpload validasi sudah ada (L903-906):**
- Extension whitelist sudah ada: `.pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx`
- Size limit sudah ada: 10MB
- Tidak ada bug di sini — sudah compliant dengan D-02

**Gap — GuidanceDelete urutan sudah benar (L1027-1038):**
- Hapus DB record dulu → baru hapus file fisik
- File fisik wrapped dalam try-catch dengan logger warning
- Sudah compliant — tidak perlu perubahan

### SETUP-03: Coach-Coachee Mapping — Cascade & Duplikasi

**Bug — CoachCoacheeMappingDeactivate tidak ada explicit transaction (L4230-4283):**
- Saat ini: `mapping.IsActive = false` → set DeactivatedAt di assignments → `SaveChangesAsync()` sekali
- Single `SaveChangesAsync` tidak sama dengan transaction — jika ada EF validation error di baris assignments, mapping sudah di-track sebagai modified tapi belum committed
- **Fix:** Wrap dalam `BeginTransactionAsync` seperti template DeleteKompetensi

**Assessment D-08 — CoachCoacheeMappingReactivate timestamp matching:**
- Kode existing (L4316-4343) sudah ada timestamp matching ±5 detik menggunakan `EF.Functions.DateDiffSecond`
- Ada bug kecil: `mappingEndDate` di L4316-4320 di-read dari DB tapi sudah null karena `mapping.EndDate = null` di L4308 sudah di-track oleh EF — kode sudah workaround ini dengan `entry.OriginalValues["EndDate"]` di L4323
- Workaround ini **fragile** tapi bekerja selama EF tracking aktif
- **Rekomendasi (Claude's discretion D-08):** Simpan `originalEndDate` sebelum mengubah nilai (baca sebelum assignment):
  ```csharp
  var originalEndDate = mapping.EndDate; // capture BEFORE modifying
  mapping.IsActive = true;
  mapping.EndDate = null;
  ```
  Lebih aman dan tidak bergantung pada EF OriginalValues internal API.
- Transaction: tambahkan `BeginTransactionAsync` untuk atomicity mapping + assignments reactivation

**Duplikasi check sudah ada di CoachCoacheeMappingAssign (L3956-3972):**
- Check existing active mapping untuk coachee yang sama
- Sudah return error jika duplikat
- Tidak ada gap di sini

### SETUP-04: Track Assignment Progression Validation

**Gap — Tidak ada progression warning sama sekali:**
- `CoachCoacheeMappingAssign` (L3944) langsung create assignment + auto-create progress tanpa cek Tahun sebelumnya
- D-09 minta warning (bukan block) jika assign Tahun 2/3 sebelum Tahun 1 selesai
- Perlu cek TahunKe dari ProtonTrack yang di-assign vs TahunKe sebelumnya

**Cara cek TahunKe:**
```csharp
// ProtonTrack model memiliki field TahunKe (misal "Tahun 1", "Tahun 2", "Tahun 3")
// dan Urutan (1, 2, 3) untuk ordering
var requestedTrack = await _context.ProtonTracks.FindAsync(req.ProtonTrackId);
// Cari track dengan Urutan lebih kecil dari requested
var prevTracks = await _context.ProtonTracks
    .Where(t => t.Urutan < requestedTrack.Urutan && t.IsActive)
    .OrderByDescending(t => t.Urutan)
    .ToListAsync();
```

**D-11 exception:** Reactivated coachee boleh skip — deteksi via: jika coachee pernah punya inactive assignment di Tahun 2/3, tidak perlu warning.

**Gap di import CoachCoacheeMapping:** Import (L3774) tidak assign track sama sekali — hanya create mapping, tidak ada ProtonTrackId di Excel template. Progression validation hanya relevan di Assign action, bukan import mapping. Namun D-12 menyebut warning juga di import — ini mungkin merujuk ke ImportSilabus (jika ada track progression logic) atau perlu klarifikasi. **Assessment:** D-12 lebih relevan ke ImportSilabus jika ada field track, bukan ImportCoachCoacheeMapping yang hanya NIP-to-NIP.

### SETUP-05: Import/Export Robustness

**Bug Kritis — ImportSilabus partial commit (L720-873):**
- SaveChangesAsync dipanggil di L813 (setelah insert Kompetensi), L831 (setelah SubKompetensi), L858 (setelah semua baris)
- L813 dan L831 ada di dalam loop per-baris — ini untuk mendapatkan FK ID (diperlukan untuk nested insert)
- **Masalah:** Tidak bisa rollback L813 insert jika baris 20 error
- **Fix D-13:** Two-pass approach:
  - Pass 1: Parse + validate semua baris, tidak ada DB write
  - Pass 2: Baru execute semua insert dalam satu transaction
  - Untuk FK ID: pre-assign ID menggunakan in-memory dictionary (karena EF sudah track entitas baru, bisa SaveChanges sekali di akhir dalam transaction)

**Bug — ImportSilabus tidak ada header validation:**
- Tidak ada cek kolom header di baris 1
- User bisa upload file Excel berbeda dan tidak ada error yang informatif
- **Fix D-16:** Cek 7 header kolom di baris 1 sebelum proses data

**Import CoachCoacheeMapping (L3774) — partial safe tapi bukan atomic:**
- Collect semua `newMappings` dan `reactivatedMappings` dalam list
- `AddRange` dan `SaveChangesAsync` dilakukan sekali di L3914-3917
- Reactivated mappings dimodifikasi in-memory selama loop, saved sekali
- **Sudah lebih baik** dari ImportSilabus, tapi tidak ada header validation juga
- **Gap:** Tidak ada explicit transaction untuk `newMappings.AddRange + SaveChanges` — wrap dalam transaction sesuai D-13

**Export CoachCoacheeMapping (L5382):**
- Query load semua mappings tanpa IsActive filter — export semua (active + inactive)
- Mungkin ini by design (full audit trail)
- Tidak ada bug yang obvious — data ditampilkan dengan kolom "Status" (Active/Inactive)

**Export Silabus (L613):**
- Filter `k.IsActive` sudah ada — hanya export active silabus
- Tidak ada bug

---

## Common Pitfalls

### Pitfall 1: EF SaveChangesAsync Multiple Kali Tanpa Transaction
**Yang salah:** Multiple `SaveChangesAsync()` dalam sequence tanpa transaction = partial commit jika crash
**Mengapa terjadi:** Perlu flush untuk mendapatkan auto-generated ID sebelum insert child
**Cara mencegah:** Wrap semua dalam `BeginTransactionAsync`. Untuk mendapatkan ID setelah insert tanpa SaveChanges: tidak bisa. Solusi: SaveChanges di dalam transaction tetap atomic — jika exception, rollback semua termasuk yang sudah SaveChanges.

### Pitfall 2: File-then-DB vs DB-then-File
**Yang salah:** Delete file fisik sebelum DB update (atau sebaliknya — DB update sebelum file ada)
**GuidanceReplace bug:** File dihapus di L983 sebelum upload L994 — file loss jika upload gagal
**Pattern aman:** Operasi yang bisa di-retry (write file baru) dulu, baru operasi yang tidak bisa di-undo (delete file lama)

### Pitfall 3: EF OriginalValues untuk Capture Pre-Modification State
**Konteks:** CoachCoacheeMappingReactivate L4323 menggunakan `entry.OriginalValues["EndDate"]`
**Risiko:** EF OriginalValues bisa berubah jika SaveChanges sudah dipanggil sebelumnya di request yang sama
**Cara mencegah:** Capture nilai sebelum modifikasi: `var originalEndDate = mapping.EndDate;` sebelum `mapping.EndDate = null;`

### Pitfall 4: Import Excel — FK Resolution Dalam Loop
**Masalah:** ImportSilabus butuh FK ID (KompetensiId → SubKompetensiId → DeliverableId) untuk insert nested
**Current approach:** SaveChanges per level untuk dapat ID — menyebabkan partial commit
**Alternatif:** Gunakan in-memory dictionary + satu SaveChanges di akhir. EF sudah track entitas baru — selama reference-nya benar, tidak perlu ID dari DB sampai akhir.

---

## Code Examples

### Delete Safety Check (Deliverable Level)
```csharp
// GET: /ProtonData/SilabusDeletePreview
[HttpGet]
public async Task<IActionResult> SilabusDeletePreview(int deliverableId)
{
    var progressQuery = _context.ProtonDeliverableProgresses
        .Where(p => p.ProtonDeliverableId == deliverableId);

    var hasActiveProgress = await progressQuery
        .AnyAsync(p => p.Status != "Approved");
    var totalProgress = await progressQuery.CountAsync();
    var coacheeCount = await progressQuery
        .Select(p => p.CoacheeId).Distinct().CountAsync();

    return Json(new { hasActiveProgress, totalProgress, coacheeCount });
}
```

### Transaction Pattern (adopsi dari DeleteKompetensi)
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // ... semua operasi cascade ...
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
    return Json(new { success = true });
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    _logger.LogError(ex, "Transaction failed");
    return Json(new { success = false, message = "Operasi gagal. Semua perubahan dibatalkan." });
}
```

### ImportSilabus Two-Pass (All-or-Nothing)
```csharp
// Pass 1: Validasi dan parse (tidak ada DB write)
var rowErrors = new List<ImportSilabusResult>();
var validRows = new List<ParsedSilabusRow>();
for (int rowNum = 2; rowNum <= lastRow; rowNum++)
{
    // ... parse dan validasi ...
    if (hasError) rowErrors.Add(errorResult);
    else validRows.Add(parsed);
}

if (rowErrors.Any())
{
    ViewBag.ImportResults = rowErrors; // tampilkan semua error
    return View();
}

// Pass 2: Semua valid — insert dalam transaction
using var tx = await _context.Database.BeginTransactionAsync();
try
{
    // Insert menggunakan in-memory dict untuk FK resolution
    var kompDict = new Dictionary<string, ProtonKompetensi>();
    foreach (var row in validRows)
    {
        // upsert dengan dict — SaveChanges sekali di akhir
    }
    await _context.SaveChangesAsync();
    await tx.CommitAsync();
}
catch { await tx.RollbackAsync(); throw; }
```

### Progression Warning Check
```csharp
// Dalam CoachCoacheeMappingAssign, sebelum create assignment:
if (req.ProtonTrackId.HasValue)
{
    var requestedTrack = await _context.ProtonTracks.FindAsync(req.ProtonTrackId.Value);
    if (requestedTrack?.Urutan > 1)
    {
        var prevTrack = await _context.ProtonTracks
            .Where(t => t.Urutan == requestedTrack.Urutan - 1 && t.IsActive)
            .FirstOrDefaultAsync();
        if (prevTrack != null)
        {
            var incompleteCoachees = new List<string>();
            foreach (var coacheeId in req.CoacheeIds)
            {
                // D-11: Skip jika pernah punya assignment inactive di track ini (reactivated)
                var hasExistingAssignment = await _context.ProtonTrackAssignments
                    .AnyAsync(a => a.CoacheeId == coacheeId
                               && a.ProtonTrackId == req.ProtonTrackId.Value);
                if (hasExistingAssignment) continue;

                var prevAssignment = await _context.ProtonTrackAssignments
                    .FirstOrDefaultAsync(a => a.CoacheeId == coacheeId
                                           && a.ProtonTrackId == prevTrack.Id);
                if (prevAssignment == null) { incompleteCoachees.Add(coacheeId); continue; }

                var allApproved = !await _context.ProtonDeliverableProgresses
                    .AnyAsync(p => p.ProtonTrackAssignmentId == prevAssignment.Id
                               && p.Status != "Approved");
                if (!allApproved) incompleteCoachees.Add(coacheeId);
            }
            if (incompleteCoachees.Any() && !req.ConfirmProgressionWarning)
                return Json(new { success = false, warning = true,
                    message = $"{incompleteCoachees.Count} coachee belum menyelesaikan {prevTrack.DisplayName}. Tetap lanjutkan?" });
        }
    }
}
```

---

## Open Questions

1. **D-12: Progression warning di import**
   - Yang diketahui: ImportCoachCoacheeMapping tidak mengassign track — hanya buat mapping
   - Yang tidak jelas: D-12 menyebut "validasi warning di assign action + import" — jika import tidak assign track, warning Tahun 1→2→3 tidak applicable
   - Rekomendasi: Asumsikan D-12 hanya berlaku untuk import yang mengandung ProtonTrackId (saat ini tidak ada di ImportCoachCoacheeMapping template). Progression warning cukup di `CoachCoacheeMappingAssign` saja.

2. **SilabusDeactivate cascade ke child (D-03)**
   - Yang diketahui: Saat ini hanya deactivate Kompetensi, bukan SubKompetensi dan Deliverable
   - Yang tidak jelas: Apakah query di PlanIdp dan ExportSilabus sudah filter via parent `IsActive` sehingga cascade flag tidak diperlukan?
   - Rekomendasi: Audit query di CDPController/PlanIdp dan ProtonDataController/ExportSilabus sebelum memutuskan perlu cascade flag atau tidak.

3. **ImportSilabus two-pass dan FK resolution**
   - Yang diketahui: EF Core bisa track entitas baru in-memory sebelum SaveChanges
   - Yang tidak jelas: Apakah SaveChanges sekali di akhir bisa handle nested hierarchy (Kompetensi → SubKompetensi → Deliverable) tanpa FK violation?
   - Rekomendasi: Gunakan `await _context.SaveChangesAsync()` sekali di akhir dalam transaction — EF akan insert dalam urutan dependency yang benar karena relasi sudah tertrack via object reference (bukan FK int).

---

## Sources

### Primary (HIGH confidence)
- Audit langsung `Controllers/ProtonDataController.cs` — baris 200-1350, 880-1045
- Audit langsung `Controllers/AdminController.cs` — baris 3770-4365, 5375-5453
- Audit langsung `Models/ProtonModels.cs` — ProtonTrack, ProtonTrackAssignment, ProtonDeliverableProgress
- `.planning/phases/234-audit-setup-flow/234-CONTEXT.md` — semua decisions D-01 s/d D-16

### Secondary (MEDIUM confidence)
- `.planning/REQUIREMENTS.md` — SETUP-01 s/d SETUP-05 definitions
- `.planning/STATE.md` — project context dan accumulated decisions

---

## Metadata

**Confidence breakdown:**
- Bug identification: HIGH — langsung dari audit kode
- Fix patterns: HIGH — mengadopsi pattern existing (DeleteKompetensi)
- Progression warning logic: MEDIUM — ProtonTrack.Urutan diasumsikan tersedia (perlu verifikasi model)
- Import two-pass EF behavior: MEDIUM — EF Core behavior well-known tapi perlu test

**Research date:** 2026-03-22
**Valid until:** Tidak ada library baru — tidak ada expiry terkait versi
