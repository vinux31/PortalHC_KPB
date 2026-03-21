# Phase 212: Tipe Filter, Renewal Flow, AddTraining Renewal - Research

**Researched:** 2026-03-21
**Domain:** ASP.NET Core MVC — RenewalCertificate filter, modal renewal flow, AddTraining renewal mode
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions

**Tipe Filter**
- D-01: Dropdown "Tipe" ditempatkan sebelum dropdown "Status" di filter bar (urutan: Bagian > Unit > Kategori > Sub Kategori > Tipe > Status)
- D-02: Opsi dropdown: "Semua Tipe" (default) / "Assessment" / "Training"
- D-03: Filter Tipe mempengaruhi summary cards (count Expired/Akan Expired ikut berubah sesuai filter)

**Renewal Flow Popup**
- D-04: Klik "Renew" pada SEMUA tipe (Assessment maupun Training) menampilkan modal Bootstrap pilihan metode — bukan hanya Training
- D-05: Modal menampilkan: judul sertifikat, nama pekerja, 2 tombol pilihan ("Renew via Assessment" dan "Renew via Training"), tombol Batal
- D-06: Setelah user pilih metode, langsung redirect ke halaman tujuan (CreateAssessment atau AddTraining) tanpa konfirmasi tambahan

**Bulk Renew Mixed-Type**
- D-07: Bulk renew campuran tipe (Assessment + Training) diblokir — tidak diizinkan
- D-08: Pesan error ditampilkan di dalam modal konfirmasi bulkRenew (bukan toast), dengan tombol Lanjutkan di-disable
- D-09: Bulk renew yang semua tipenya sama tetap tampilkan popup pilihan metode (konsisten dengan single renew)

**AddTraining Renewal Mode**
- D-10: Field yang di-prefill: Title (Judul), Category (Kategori), dan Peserta (konsisten dengan CreateAssessment renewal)
- D-11: Parameter renewal dikirim via query string: `/Admin/AddTraining?renewSessionId=X&renewTrainingId=Y`
- D-12: Banner kuning info di atas form saat mode renewal: "Mode Renewal — Training ini akan me-renew sertifikat [Judul] milik [Nama]"
- D-13: Bulk renew Training menggunakan satu form AddTraining multi-peserta dengan RenewsSessionId/RenewsTrainingId di-map per peserta (pola Phase 210 hidden input JSON)

### Claude's Discretion
- Exact modal styling dan animation
- Query string parameter naming
- Validasi edge cases (missing params, invalid IDs)
- Banner styling details

### Deferred Ideas (OUT OF SCOPE)
Tidak ada — diskusi tetap dalam scope phase.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Deskripsi | Research Support |
|----|-----------|-----------------|
| ENH-01 | Filter tipe (Assessment / Training) pada halaman RenewalCertificate | SertifikatRow sudah memiliki `RecordType` enum. FilterRenewalCertificate perlu parameter `tipe` baru. Dropdown di filter bar di-insert sebelum Status. |
| ENH-02 | Renewal flow berdasarkan tipe — modal pilihan metode untuk SEMUA tipe | Tombol Renew di _RenewalGroupTablePartial saat ini langsung link ke CreateAssessment. Perlu diubah jadi trigger JS modal baru `renewMethodModal`. |
| ENH-03 | Bulk renew aware tipe — blokir campuran, popup pilihan metode untuk tipe seragam | `renewGroup()` di RenewalCertificate.cshtml membaca `cb.dataset.recordtype`. Perlu validasi mixed-type sebelum show modal, dan modal konfirmasi diperluas dengan pilihan metode. |
| ENH-04 | AddTraining renewal mode dengan pre-fill data sertifikat asal + set FK | AddTraining GET saat ini tidak mendukung parameter renewal. Perlu parsing `renewSessionId`/`renewTrainingId` query string, prefill ViewBag, dan POST harus set FK di TrainingRecord. |
| FIX-04 | AddTraining POST set RenewsTrainingId/RenewsSessionId sesuai sumber | CreateTrainingRecordViewModel belum punya field FK. Perlu tambah `RenewsTrainingId?` dan `RenewsSessionId?` ke ViewModel, lalu mapping ke TrainingRecord saat save. Untuk bulk: hidden input JSON pattern (Phase 210). |
</phase_requirements>

---

## Summary

Phase 212 menambahkan tiga fitur berkaitan ke sistem RenewalCertificate:

**ENH-01 (Tipe Filter):** `SertifikatRow` sudah punya `RecordType` enum (Training/Assessment). Cukup tambah parameter `tipe` ke `FilterRenewalCertificate` dan `FilterRenewalCertificateGroup`, filter rows berdasarkan `RecordType`, tambah dropdown di view, dan update `refreshTable()` JS untuk menyertakan nilai tipe. Tidak ada perubahan model atau DB.

**ENH-02/ENH-03 (Renewal Flow Popup):** Saat ini tombol "Renew" di `_RenewalGroupTablePartial.cshtml` adalah anchor langsung ke CreateAssessment. Perlu diubah ke button yang mentrigger modal Bootstrap baru (`renewMethodModal`) via JS. Modal menampilkan judul + nama pekerja, dua tombol (via Assessment / via Training). Untuk bulk renew (`renewGroup()`), logika validasi mixed-type perlu ditambahkan sebelum show `bulkRenewConfirmModal`, dan `bulkRenewConfirmModal` perlu diubah untuk menampilkan error mixed-type atau pilihan metode tipe seragam.

**ENH-04/FIX-04 (AddTraining Renewal Mode):** `AddTraining` GET perlu menerima `renewSessionId` dan `renewTrainingId` parameter (List, konsisten dengan CreateAssessment), query DB, prefill ViewBag (`RenewalSourceTitle`, `RenewalSourceUserName`, `SelectedUserIds`, `RenewalFkMap`/`RenewalFkMapType`), dan return view dengan banner kuning. `CreateTrainingRecordViewModel` perlu dua field baru: `RenewsTrainingId?` dan `RenewsSessionId?`. POST handler harus membaca parameter ini (atau hidden JSON map untuk bulk) dan menyimpan ke `TrainingRecord`.

**Primary recommendation:** Ikuti pola yang sudah ada sepenuhnya — CreateAssessment renewal (Phase 210) adalah template langsung untuk AddTraining renewal. Tidak ada teknologi baru.

---

## Standard Stack

### Core
| Library/Teknologi | Versi | Tujuan | Catatan |
|-------------------|-------|--------|---------|
| ASP.NET Core MVC | Existing | Controller + View | Tidak berubah |
| Bootstrap 5 | Existing | Modal renewal method | `bootstrap.Modal` API sudah digunakan |
| Vanilla JS (fetch + URLSearchParams) | Existing | Filter AJAX, modal trigger | Tidak perlu framework tambahan |
| EF Core | Existing | Query TrainingRecord/AssessmentSession | Tidak ada migrasi DB |

**Installation:** Tidak ada package baru diperlukan.

---

## Architecture Patterns

### Pattern 1: Tipe Filter — Parameter Tambahan di Controller
**What:** Tambah `string? tipe` ke signature `FilterRenewalCertificate` dan `FilterRenewalCertificateGroup`. Filter rows dengan:
```csharp
// Source: existing filter pattern di AdminController.cs ~line 7035
if (!string.IsNullOrEmpty(tipe) && Enum.TryParse<RecordType>(tipe, out var rt))
    allRows = allRows.Where(r => r.RecordType == rt).ToList();
```
Update `IsFiltered` untuk menyertakan tipe:
```csharp
gvm.IsFiltered = ... || !string.IsNullOrEmpty(tipe);
```

### Pattern 2: Tipe Filter — View dan JS
**What:** Insert dropdown Tipe sebelum dropdown Status di filter bar (line ~86 RenewalCertificate.cshtml):
```html
<div class="col-md-2">
    <label class="form-label small mb-1">Tipe</label>
    <select id="filter-tipe" class="form-select form-select-sm">
        <option value="">Semua Tipe</option>
        <option value="Assessment">Assessment</option>
        <option value="Training">Training</option>
    </select>
</div>
```
Update `refreshTable()` dan `refreshGroupTable()` untuk menyertakan `filter-tipe`:
```javascript
if (tipeEl.value) params.set('tipe', tipeEl.value);
```
Update reset handler untuk clear tipe.

### Pattern 3: Single Renew — Modal Pilihan Metode
**What:** Ganti anchor `<a href="/Admin/CreateAssessment?...">` di `_RenewalGroupTablePartial.cshtml` menjadi button yang trigger modal via JS. Data row (sourceId, recordType, judul, namaWorker) disimpan di data-attributes button.

```html
<!-- _RenewalGroupTablePartial.cshtml — tombol Renew baru -->
<button type="button"
        class="btn btn-sm btn-warning btn-renew-single"
        data-sourceid="@row.SourceId"
        data-recordtype="@row.RecordType"
        data-judul="@row.Judul"
        data-namaworker="@row.NamaWorker"
        aria-label="Renew sertifikat @row.Judul untuk @row.NamaWorker">
    <i class="bi bi-arrow-repeat me-1"></i>Renew
</button>
```

Modal baru `renewMethodModal` di RenewalCertificate.cshtml:
```html
<div class="modal fade" id="renewMethodModal" tabindex="-1">
  <div class="modal-dialog">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title">Pilih Metode Renewal</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body">
        <p>Sertifikat: <strong id="renew-method-judul"></strong></p>
        <p>Pekerja: <strong id="renew-method-nama"></strong></p>
      </div>
      <div class="modal-footer justify-content-between">
        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Batal</button>
        <div>
          <button type="button" class="btn btn-outline-primary me-2" id="btn-renew-via-assessment">
            <i class="bi bi-clipboard-check me-1"></i>Renew via Assessment
          </button>
          <button type="button" class="btn btn-warning" id="btn-renew-via-training">
            <i class="bi bi-journal-plus me-1"></i>Renew via Training
          </button>
        </div>
      </div>
    </div>
  </div>
</div>
```

JS handler (inside IIFE di RenewalCertificate.cshtml):
```javascript
document.addEventListener('click', function(e) {
    var btn = e.target.closest('.btn-renew-single');
    if (!btn) return;
    var sourceId = btn.dataset.sourceid;
    var recordType = btn.dataset.recordtype; // 'Assessment' atau 'Training'
    var judul = btn.dataset.judul;
    var namaWorker = btn.dataset.namaworker;

    document.getElementById('renew-method-judul').textContent = judul;
    document.getElementById('renew-method-nama').textContent = namaWorker;

    // Set href saat tombol di-click di modal
    document.getElementById('btn-renew-via-assessment').onclick = function() {
        var param = recordType === 'Assessment' ? 'renewSessionId' : 'renewTrainingId';
        window.location.href = '/Admin/CreateAssessment?' + param + '=' + sourceId;
    };
    document.getElementById('btn-renew-via-training').onclick = function() {
        var param = recordType === 'Assessment' ? 'renewSessionId' : 'renewTrainingId';
        window.location.href = '/Admin/AddTraining?' + param + '=' + sourceId;
    };

    new bootstrap.Modal(document.getElementById('renewMethodModal')).show();
});
```

### Pattern 4: Bulk Renew — Validasi Mixed-Type + Modal Pilihan Metode
**What:** Modifikasi fungsi `renewGroup()` di RenewalCertificate.cshtml.

Validasi mixed-type:
```javascript
function renewGroup(groupKey, judul) {
    var checked = container.querySelectorAll('.cb-select[data-group-key="' + groupKey + '"]:checked');
    if (checked.length === 0) return;

    // Cek apakah semua tipe sama
    var types = Array.from(checked).map(function(cb) { return cb.dataset.recordtype; });
    var uniqueTypes = [...new Set(types)];

    var params = new URLSearchParams();
    checked.forEach(function (cb) {
        if (cb.dataset.recordtype === 'Assessment') {
            params.append('renewSessionId', cb.dataset.sourceid);
        } else {
            params.append('renewTrainingId', cb.dataset.sourceid);
        }
    });
    pendingRenewParams = params.toString();
    pendingRenewType = uniqueTypes.length === 1 ? uniqueTypes[0] : 'Mixed';

    document.getElementById('bulk-renew-count').textContent = checked.length;
    document.getElementById('bulk-renew-judul').textContent = judul;

    // Tampilkan modal dengan state sesuai
    var modal = new bootstrap.Modal(document.getElementById('bulkRenewConfirmModal'));
    modal.show();
    updateBulkModalState();
}
```

`bulkRenewConfirmModal` body perlu diperluas dengan:
1. Area error untuk mixed-type (visible hanya saat `pendingRenewType === 'Mixed'`)
2. Pilihan metode (visible hanya saat tipe seragam)
3. Disable tombol Lanjutkan saat mixed-type

### Pattern 5: AddTraining GET — Renewal Mode
**What:** Modifikasi `AddTraining` GET di AdminController.cs mengikuti pola CreateAssessment persis.

Parameter signature:
```csharp
public async Task<IActionResult> AddTraining(
    [FromQuery] List<int>? renewSessionId = null,
    [FromQuery] List<int>? renewTrainingId = null)
```

Logic prefill (analog dengan CreateAssessment, line ~992):
```csharp
bool isRenewalMode = false;
if (renewTrainingId != null && renewTrainingId.Count > 0)
{
    if (renewTrainingId.Count == 1)
    {
        var src = await _context.TrainingRecords.Include(t=>t.User)
            .FirstOrDefaultAsync(t => t.Id == renewTrainingId[0]);
        if (src == null) { TempData["Warning"] = "..."; return RedirectToAction("AddTraining"); }
        isRenewalMode = true;
        ViewBag.RenewalSourceTitle = src.Judul ?? "";
        ViewBag.RenewalSourceUserName = src.User?.FullName ?? "";
        ViewBag.SelectedUserId = src.UserId;
        ViewBag.PrefillJudul = src.Judul;
        ViewBag.PrefillKategori = src.Kategori;
        ViewBag.RenewsTrainingId = src.Id;
    }
    else
    {
        // Bulk — Phase 210 hidden JSON pattern
        var srcs = await _context.TrainingRecords.Include(t=>t.User)
            .Where(t => renewTrainingId.Contains(t.Id)).ToListAsync();
        isRenewalMode = true;
        var first = srcs[0];
        ViewBag.RenewalSourceTitle = first.Judul ?? "";
        ViewBag.PrefillJudul = first.Judul;
        ViewBag.PrefillKategori = first.Kategori;
        ViewBag.RenewalFkMap = JsonSerializer.Serialize(srcs.ToDictionary(t => t.UserId ?? "", t => t.Id));
        ViewBag.RenewalFkMapType = "training";
        ViewBag.SelectedUserIds = srcs.Select(t => t.UserId).ToList();
    }
}
else if (renewSessionId != null && renewSessionId.Count > 0)
{
    // Analog untuk sumber Assessment
    // ...single: ViewBag.RenewsSessionId = ...
    // ...bulk: ViewBag.RenewalFkMap + RenewalFkMapType = "session"
}
ViewBag.IsRenewalMode = isRenewalMode;
```

### Pattern 6: AddTraining View — Banner dan Prefill
**What:** Tambah di AddTraining.cshtml, setelah breadcrumb:
```html
@if (ViewBag.IsRenewalMode == true)
{
    <div class="alert alert-warning d-flex align-items-center mb-3">
        <i class="bi bi-arrow-repeat me-2 fs-5"></i>
        <span>Mode Renewal — Training ini akan me-renew sertifikat
            <strong>@ViewBag.RenewalSourceTitle</strong>
            milik <strong>@ViewBag.RenewalSourceUserName</strong>
        </span>
    </div>
}
```

Untuk prefill Judul dan Kategori, gunakan `value` attribute dengan ViewBag:
```html
<input asp-for="Judul" class="form-control"
       value="@(ViewBag.PrefillJudul ?? "")" />
```

Untuk prefill Peserta (single): select dropdown di-set via JS `document.getElementById('UserId').value = '@ViewBag.SelectedUserId'`.
Untuk bulk: multiple select atau hidden JSON map.

Hidden inputs untuk FK (analog CreateAssessment):
```html
@if (ViewBag.RenewsTrainingId != null)
{
    <input type="hidden" name="RenewsTrainingId" value="@ViewBag.RenewsTrainingId" />
}
@if (ViewBag.RenewsSessionId != null)
{
    <input type="hidden" name="RenewsSessionId" value="@ViewBag.RenewsSessionId" />
}
@if (ViewBag.RenewalFkMap != null)
{
    <input type="hidden" id="renewal-fk-map" value='@ViewBag.RenewalFkMap' />
    <input type="hidden" id="renewal-fk-map-type" value="@ViewBag.RenewalFkMapType" />
}
```

### Pattern 7: CreateTrainingRecordViewModel — Tambah FK Fields
**What:** Tambah dua field nullable ke ViewModel:
```csharp
// In CreateTrainingRecordViewModel.cs
public int? RenewsTrainingId { get; set; }
public int? RenewsSessionId { get; set; }
```

### Pattern 8: AddTraining POST — Set FK di TrainingRecord
**What:** Modifikasi POST handler untuk membaca FK:
```csharp
// Single renew — langsung dari model
var record = new TrainingRecord
{
    ...
    RenewsTrainingId = model.RenewsTrainingId,
    RenewsSessionId = model.RenewsSessionId,
};
```

Untuk bulk renew dengan FK map (analog CreateAssessment POST, Phase 210):
```csharp
// Baca JSON map dari hidden field
var fkMapJson = Request.Form["renewalFkMap"].FirstOrDefault();
var fkMapType = Request.Form["renewalFkMapType"].FirstOrDefault();
Dictionary<string, int>? fkMap = null;
if (!string.IsNullOrEmpty(fkMapJson))
    fkMap = JsonSerializer.Deserialize<Dictionary<string, int>>(fkMapJson);

// Saat buat TrainingRecord per-user:
if (fkMap != null && fkMap.TryGetValue(userId, out var fkId))
{
    if (fkMapType == "training") record.RenewsTrainingId = fkId;
    else record.RenewsSessionId = fkId;
}
```

**Catatan penting:** AddTraining saat ini hanya mendukung satu UserId per submit (bukan multi-peserta). Untuk bulk renewal, dua opsi:
1. Perluas AddTraining ke multi-peserta (scope besar, kemungkinan di luar fase)
2. Untuk bulk, kirim ke satu AddTraining per peserta (redirect loop)

Berdasarkan D-13, user ingin pola Phase 210 (hidden input JSON + multi-peserta dalam satu form). Ini berarti AddTraining POST perlu mendukung `List<string> UserIds` — namun AddTraining saat ini single-user. **Ini adalah titik risiko penting: AddTraining perlu diubah menjadi multi-user HANYA untuk bulk renewal, atau dibuat form terpisah.**

Rekomendasi: Untuk single renew via Training, gunakan AddTraining normal dengan prefill. Untuk bulk renew via Training, arahkan ke AddTraining dengan `userId[]=...` multi-param dan hidden FK map — mirip pola CreateAssessment multi-session.

### Anti-Patterns
- **Jangan ubah `bulkRenewConfirmModal` menjadi dua modal terpisah** — satu modal yang state-aware lebih sederhana
- **Jangan kirim tipe via hidden field dari server** — `data-recordtype` sudah ada di checkbox dari partial, baca client-side
- **Jangan duplikasi BuildRenewalRowsAsync** — cukup tambah filter tipe di FilterRenewalCertificate

---

## Don't Hand-Roll

| Problem | Jangan Buat | Gunakan | Alasan |
|---------|-------------|---------|--------|
| Modal Bootstrap | Custom dialog div | `bootstrap.Modal` API yang sudah ada | Sudah dipakai untuk `bulkRenewConfirmModal` dan `certificateHistoryModal` |
| JSON serialization untuk FK map | Custom format | `System.Text.Json.JsonSerializer` | Sudah dipakai Phase 210 (`ViewBag.RenewalFkMap`) |
| Enum parsing filter | String comparison manual | `Enum.TryParse<RecordType>` | Sudah dipakai untuk `CertificateStatus` filter |

---

## Common Pitfalls

### Pitfall 1: AddTraining Hanya Single-User
**What goes wrong:** AddTraining saat ini menerima satu `UserId` (string). Jika bulk renew via Training dikirim tanpa perubahan arsitektur, hanya peserta pertama yang dibuat.
**Why it happens:** `CreateTrainingRecordViewModel.UserId` adalah scalar, bukan List.
**How to avoid:** Perluas AddTraining POST untuk mendukung multi-user dengan pola yang sama dengan CreateAssessment (terima `List<string> UserIds` dari form, loop buat TrainingRecord per user). Ini wajib untuk FIX-04 bulk.
**Warning signs:** Jika tidak ada `List<string> UserIds` di form POST handler, bulk renew hanya akan membuat record untuk satu user.

### Pitfall 2: `data-recordtype` Case Sensitivity
**What goes wrong:** `RecordType` enum di C# adalah `Training`/`Assessment` (PascalCase). Di partial `_RenewalGroupTablePartial.cshtml` ditulis `data-recordtype="@row.RecordType"` yang otomatis jadi `Training` atau `Assessment`. JS harus compare dengan case yang sama.
**How to avoid:** Selalu gunakan `cb.dataset.recordtype === 'Assessment'` (PascalCase) — jangan lowercase.

### Pitfall 3: Tipe Filter Tidak Di-pass ke FilterRenewalCertificateGroup
**What goes wrong:** `refreshGroupTable()` memanggil endpoint `/Admin/FilterRenewalCertificateGroup` tetapi jika parameter `tipe` tidak di-pass, pagination per-group tidak akan menghormati filter tipe.
**How to avoid:** Update `refreshGroupTable()` untuk menyertakan `tipeEl.value` di params, dan tambah parameter `tipe` ke `FilterRenewalCertificateGroup` signature di controller.

### Pitfall 4: AddTraining POST Tidak Ada AntiForgery Token Saat Multi-User
**What goes wrong:** Jika form AddTraining diperluas untuk multi-user, pastikan `@Html.AntiForgeryToken()` tetap ada (sudah ada di view saat ini, tidak boleh dihapus).
**How to avoid:** Verifikasi `[ValidateAntiForgeryToken]` tetap ada di POST action signature.

### Pitfall 5: Banner Kuning Muncul Saat ModelState Invalid di POST
**What goes wrong:** Jika POST gagal validasi dan return View(model), banner renewal perlu tetap muncul. ViewBag hilang saat redirect tetapi tidak saat return View.
**How to avoid:** Pastikan semua ViewBag renewal (IsRenewalMode, RenewalSourceTitle, dll) diisi ulang di POST handler saat return View(model) — sama seperti cara CreateAssessment menanganinya.

---

## Code Examples

### Contoh: FilterRenewalCertificate dengan Tipe Filter
```csharp
// AdminController.cs — FilterRenewalCertificate
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> FilterRenewalCertificate(
    string? bagian = null,
    string? unit = null,
    string? status = null,
    string? category = null,
    string? subCategory = null,
    string? tipe = null,   // NEW
    int page = 1)
{
    var allRows = await BuildRenewalRowsAsync();
    // ... existing filters ...
    if (!string.IsNullOrEmpty(tipe) && Enum.TryParse<RecordType>(tipe, out var rt))
        allRows = allRows.Where(r => r.RecordType == rt).ToList();
    // ...
    gvm.IsFiltered = ... || !string.IsNullOrEmpty(tipe);
}
```

### Contoh: Validasi Mixed-Type di renewGroup()
```javascript
function renewGroup(groupKey, judul) {
    var checked = container.querySelectorAll('.cb-select[data-group-key="' + groupKey + '"]:checked');
    if (checked.length === 0) return;
    var types = Array.from(checked).map(function(cb) { return cb.dataset.recordtype; });
    var uniqueTypes = [...new Set(types)];
    var isMixed = uniqueTypes.length > 1;
    // ... build params ...
    pendingRenewType = isMixed ? 'Mixed' : uniqueTypes[0];
    // show modal, update state
}
```

### Contoh: CreateTrainingRecordViewModel dengan FK
```csharp
// Models/CreateTrainingRecordViewModel.cs — tambah dua field
public int? RenewsTrainingId { get; set; }
public int? RenewsSessionId { get; set; }
```

---

## State of the Art

| Old Approach | Current Approach | Kapan Berubah | Impact |
|--------------|------------------|---------------|--------|
| Single renew langsung link ke CreateAssessment | Modal pilihan metode untuk semua tipe | Phase 212 | Semua renew (Assessment maupun Training) melewati popup pilihan |
| AddTraining tanpa renewal mode | AddTraining dengan prefill + FK dari query string | Phase 212 | Renewal chain TR→TR dan AS→TR dapat direkam |
| Bulk renew tidak validasi tipe | Bulk renew blokir campuran tipe | Phase 212 | User harus filter by tipe sebelum bulk renew lintas tipe |

---

## Open Questions

1. **AddTraining Multi-User untuk Bulk Renewal**
   - Yang diketahui: `CreateTrainingRecordViewModel.UserId` adalah scalar. CreateAssessment sudah multi-user via `UserIds` list.
   - Yang belum jelas: Apakah AddTraining perlu diperluas ke multi-user untuk bulk renewal via Training, atau bulk renew via Training cukup dibatasi menampilkan satu AddTraining per user (flow lebih kompleks)?
   - Rekomendasi: Perluas AddTraining ke multi-user mengikuti pola CreateAssessment (lebih bersih). Planner perlu memutuskan scope task ini.

2. **Tipe Badge di Grouped Header**
   - Yang diketahui: `RenewalGroup` tidak punya informasi tipe mix (bisa campuran Assessment dan Training dalam satu judul)
   - Yang belum jelas: Apakah grouped header perlu menampilkan badge tipe? (Misal "Assessment + Training" jika mixed)
   - Rekomendasi: Tidak perlu untuk Phase 212 — tidak ada decision yang meminta ini. Abaikan.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (proyek tidak memiliki automated test suite) |
| Config file | none |
| Quick run command | Manual: buka `/Admin/RenewalCertificate` di browser |
| Full suite command | Manual: ikuti use-case flows di bawah |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ENH-01 | Dropdown Tipe memfilter tabel dan summary cards | manual | n/a — browser test | ❌ Wave 0 |
| ENH-02 | Klik Renew single row → modal popup 2 pilihan | manual | n/a — browser test | ❌ Wave 0 |
| ENH-03 | Bulk renew mixed-type → error di modal, tombol disable | manual | n/a — browser test | ❌ Wave 0 |
| ENH-03 | Bulk renew tipe seragam → modal pilihan metode | manual | n/a — browser test | ❌ Wave 0 |
| ENH-04 | Buka `/Admin/AddTraining?renewTrainingId=X` → banner + prefill | manual | n/a — browser test | ❌ Wave 0 |
| FIX-04 | Submit AddTraining renewal → TrainingRecord.RenewsTrainingId terisi | manual + DB check | n/a — DB inspection | ❌ Wave 0 |

### Sampling Rate
- Per task commit: Jalankan browser test untuk fitur yang baru diimplementasi
- Per wave merge: Full flow test semua 4 requirements
- Phase gate: Semua 4 use-case flows hijau sebelum `/gsd:verify-work`

### Wave 0 Gaps
- Tidak ada file test otomatis — proyek menggunakan manual browser testing

---

## Sources

### Primary (HIGH confidence)
- Source code langsung: `AdminController.cs` §FilterRenewalCertificate (~line 7025), §CreateAssessment GET (~line 960), §AddTraining (~line 5392)
- Source code langsung: `Views/Admin/RenewalCertificate.cshtml` — fungsi `renewGroup()`, `refreshTable()`, modal HTML
- Source code langsung: `Views/Admin/Shared/_RenewalGroupTablePartial.cshtml` — tombol Renew existing
- Source code langsung: `Models/CertificationManagementViewModel.cs` — `SertifikatRow`, `RecordType` enum, `RenewalGroup`
- Source code langsung: `Models/CreateTrainingRecordViewModel.cs` — field ViewModel existing
- Source code langsung: `Models/TrainingRecord.cs` — FK fields `RenewsTrainingId`, `RenewsSessionId`

### Secondary (MEDIUM confidence)
- Pola Phase 210 (commit 180f198): hidden input JSON untuk per-user FK mapping

---

## Metadata

**Confidence breakdown:**
- Standard Stack: HIGH — tidak ada library baru, semua pola dari existing code
- Architecture: HIGH — pola CreateAssessment renewal (Phase 210) dapat diikuti langsung untuk AddTraining
- Pitfalls: HIGH — ditemukan dari analisis langsung kode existing (AddTraining single-user, data-recordtype case, parameter propagation ke group pagination)

**Research date:** 2026-03-21
**Valid until:** 2026-04-21 (stable codebase, tidak ada dependency external)
