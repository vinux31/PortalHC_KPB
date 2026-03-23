# Phase 239: Date Range Filter & Export - Research

**Researched:** 2026-03-23
**Domain:** ASP.NET Core MVC — client-side filter refactor ke server-side AJAX + date range filtering
**Confidence:** HIGH (semua findings dari source code langsung, bukan library eksternal)

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions
- **D-01:** Layout tetap 2 baris. Row 2 berubah dari `Status | Search | Reset` menjadi `Status | Tgl Awal | Tgl Akhir | Reset`. Row 1 tidak berubah (Bagian, Unit, Category, Sub Category).
- **D-02:** Semua filter (termasuk date range) dikirim ke server via AJAX. Setiap perubahan filter apapun → AJAX request dengan semua parameter → server return data ter-filter (HTML partial atau JSON).
- **D-03:** Count Assessment & Training per worker dihitung server-side berdasarkan date range + filter lainnya. Tidak ada client-side re-counting.
- **D-04:** Gunakan native HTML `<input type="date">` — tidak perlu library date picker tambahan.
- **D-05:** Auto-filter: saat user mengisi/mengubah tanggal, langsung trigger AJAX request (debounce jika perlu). Tidak perlu tombol Apply terpisah.

### Claude's Discretion
- Debounce timing untuk AJAX requests
- Response format dari server (JSON + JS rebuild table, atau HTML partial replace)
- Loading indicator saat AJAX request berlangsung
- Error handling jika AJAX gagal

### Deferred Ideas (OUT OF SCOPE)
- Preset date ranges (7 hari, 30 hari, dll)
- Search Nama/NIP (dihapus, diganti date range)
- Date filter di tab My Records
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| FILT-01 | User (roleLevel ≤4) melihat 2 input date (Tanggal Awal & Tanggal Akhir) di filter bar Team View, menggantikan textbox Search Nama/NIP | Layout change di RecordsTeam.cshtml Row 2; hapus `#searchFilter`, tambah `#dateFrom` dan `#dateTo` |
| FILT-02 | Saat tanggal diisi, tabel hanya menampilkan workers yang punya minimal 1 record dalam rentang tanggal | `GetWorkersInSection` perlu parameter `dateFrom`/`dateTo`; filter dilakukan di service layer pada `TrainingRecords` dan `AssessmentSessions` |
| FILT-03 | Count kolom Assessment & Training hanya menghitung records dalam rentang tanggal | `CompletedAssessments` dan `CompletedTrainings` dihitung ulang di service dengan date range filter sebelum diassign ke model |
| FILT-04 | Filter date bisa dikombinasikan dengan filter Bagian, Unit, Category, Sub Category, Status | Parameter `dateFrom`/`dateTo` ditambahkan ke semua call `GetWorkersInSection`, termasuk call yang sudah ada untuk filter lain |
| FILT-05 | Default tanggal kosong = tampilkan semua records | `dateFrom`/`dateTo` adalah nullable — jika null, skip date filter logic |
| FILT-06 | Tombol Reset clear semua filter termasuk date range | `resetTeamFilters()` perlu clear `dateFrom.value` dan `dateTo.value` |
| EXP-01 | Export Assessment menyertakan parameter date range ke server | `ExportRecordsTeamAssessment` perlu tambah param `dateFrom`/`dateTo`; filter pada `assessmentRows` by Date |
| EXP-02 | Export Training menyertakan parameter date range ke server | `ExportRecordsTeamTraining` perlu tambah param `dateFrom`/`dateTo`; filter pada `trainingRows` by Date |
</phase_requirements>

---

## Summary

Phase ini adalah refactor signifikan pada Team View di CMP/Records. Saat ini, `filterTeamTable()` bekerja secara client-side murni — semua data sudah di-render ke HTML dan JS menyembunyikan/menampilkan baris. Kelemahan pendekatan ini: date filtering tidak bisa dilakukan client-side karena date records ada di server (bukan di DOM), dan count per worker harus dihitung ulang server-side.

Perubahan utama: (1) hapus `#searchFilter`, tambah dua `<input type="date">`, (2) ubah `filterTeamTable()` dari DOM manipulation ke AJAX call ke server, (3) server return HTML partial yang langsung mengganti `<tbody>`, dan (4) `GetWorkersInSection` di service menerima `dateFrom`/`dateTo` sebagai parameter baru untuk filter dan count.

**Primary recommendation:** Gunakan HTML partial response (server render `<tbody>` HTML) — lebih sederhana dari JSON rebuild, konsisten dengan pola Razor yang sudah ada, dan count otomatis ikut ter-update di markup yang dikembalikan.

---

## Standard Stack

### Core (tidak ada library baru)
| Komponen | Versi | Purpose | Catatan |
|----------|-------|---------|---------|
| ASP.NET Core Razor | Existing | Server-side HTML rendering | Partial view untuk AJAX response |
| Vanilla JS fetch() | Native | AJAX request dari browser | Tidak perlu jQuery/axios |
| `<input type="date">` | HTML5 | Date picker native | Sesuai D-04, no external library |
| ClosedXML / XLWorkbook | Existing | Excel export | Sudah dipakai di ExportRecordsTeamAssessment |

**Installation:** Tidak ada package baru yang diperlukan.

---

## Architecture Patterns

### Arsitektur Saat Ini (AKAN DIUBAH)
```
Browser: DOM sudah berisi semua worker rows
JS filterTeamTable() → sembunyikan/tampilkan rows secara client-side
JS updateExportLinks() → build URL dari filter values
```

### Arsitektur Baru (Target Phase 239)
```
Browser: input date/filter berubah
JS (debounced) → fetch('/CMP/RecordsTeamPartial?section=...&dateFrom=...&dateTo=...')
Server: GetWorkersInSection(section, unit, category, null, statusFilter, dateFrom, dateTo)
Server: render RecordsTeam_Body.cshtml (hanya <tbody> HTML)
Browser: replace innerHTML #workerTableBody
JS updateExportLinks() → tambahkan dateFrom/dateTo ke URL export
```

### Pattern 1: AJAX Partial Replacement

**What:** Server merender hanya `<tbody>` (bukan full page), JS mengganti innerHTML elemen target.

**When to use:** Ketika filter perlu server-side data, tapi tidak ingin full page reload.

**Contoh controller action:**
```csharp
// Source: pattern dari existing code + kebutuhan baru
[HttpGet]
public async Task<IActionResult> RecordsTeamPartial(
    string? section, string? unit, string? category, string? subCategory,
    string? statusFilter, string? dateFrom, string? dateTo)
{
    var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
    if (roleLevel >= 5) return Forbid();

    string? sectionFilter = (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
        ? user.Section : section;

    DateTime? from = string.IsNullOrEmpty(dateFrom) ? null : DateTime.Parse(dateFrom);
    DateTime? to = string.IsNullOrEmpty(dateTo) ? null : DateTime.Parse(dateTo);

    var workerList = await _workerDataService.GetWorkersInSection(
        sectionFilter, unit, category, null, statusFilter, from, to);

    return PartialView("_RecordsTeamBody", workerList);
}
```

**Contoh JS AJAX call:**
```javascript
let debounceTimer;
function filterTeamTable() {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(doFetch, 300);
}

async function doFetch() {
    const params = new URLSearchParams();
    const section = document.getElementById('sectionFilter').value;
    const unit = document.getElementById('unitFilter').value;
    const category = document.getElementById('categoryFilter').value;
    const subCategory = document.getElementById('subCategoryFilter').value;
    const status = document.getElementById('statusFilter').value;
    const dateFrom = document.getElementById('dateFrom').value;
    const dateTo = document.getElementById('dateTo').value;

    if (section) params.set('section', section);
    if (unit) params.set('unit', unit);
    if (category) params.set('category', category);
    if (subCategory) params.set('subCategory', subCategory);
    if (status && status !== 'ALL') params.set('statusFilter', status);
    if (dateFrom) params.set('dateFrom', dateFrom);
    if (dateTo) params.set('dateTo', dateTo);

    // Show loading indicator
    document.getElementById('tableLoadingIndicator').style.display = '';

    try {
        const url = '/CMP/RecordsTeamPartial?' + params.toString();
        const resp = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (!resp.ok) throw new Error('Server error');
        const html = await resp.text();
        document.getElementById('workerTableBody').innerHTML = html;
        // Update counter
        const visible = document.querySelectorAll('.worker-row').length;
        document.getElementById('workerCount').textContent = visible;
    } catch (e) {
        // Error handling: tampilkan pesan
        document.getElementById('workerTableBody').innerHTML =
            '<tr><td colspan="8" class="text-center text-danger p-4">Gagal memuat data. Coba lagi.</td></tr>';
    } finally {
        document.getElementById('tableLoadingIndicator').style.display = 'none';
    }
    updateExportLinks();
}
```

### Pattern 2: Service Layer Date Filter

**What:** `GetWorkersInSection` menerima `dateFrom`/`dateTo` nullable DateTime. Filter diterapkan pada saat hitung count dan saat filter visible workers.

**Key logic:**
```csharp
// Filter training records dalam date range
var trainingInRange = trainingRecords.Where(tr =>
    (from == null || (tr.TanggalMulai ?? tr.Tanggal) >= from) &&
    (to == null   || (tr.TanggalMulai ?? tr.Tanggal) <= to.Value.Date.AddDays(1).AddSeconds(-1))
).ToList();

// Filter assessment sessions dalam date range
var assessmentsInRange = sessions.Where(a =>
    (from == null || (a.CompletedAt ?? a.Schedule) >= from) &&
    (to == null   || (a.CompletedAt ?? a.Schedule) <= to.Value.Date.AddDays(1).AddSeconds(-1))
).ToList();

// Worker visible jika punya minimal 1 record dalam range (FILT-02)
bool hasRecordInRange = trainingInRange.Any() || assessmentsInRange.Any(a => a.Status == "Completed");
// Jika date filter aktif DAN tidak ada record dalam range → skip worker ini
if ((from != null || to != null) && !hasRecordInRange) continue;

// Count hanya dari records dalam range (FILT-03)
int completedTrainings = trainingInRange.Count(tr =>
    tr.Status == "Passed" || tr.Status == "Valid" || tr.Status == "Permanent");
int completedAssessments = assessmentsInRange.Count(a => a.IsPassed == true);
```

### Anti-Patterns yang Harus Dihindari

- **Jangan client-side date filter:** Data records tidak ada di DOM (hanya count yang di-render), tidak bisa filter date tanpa server data.
- **Jangan reload full page per filter change:** Performance buruk; gunakan partial AJAX sesuai D-02.
- **Jangan gunakan `DateTime.Parse` tanpa null check:** `dateFrom`/`dateTo` dari query string bisa kosong string — harus null check sebelum parse.
- **Jangan ubah Status filter logic yang ada:** Filter Status (Sudah/Belum) tetap dikombinasikan dengan date range, bukan menggantikan.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Date picker UI | Custom calendar widget | Native `<input type="date">` | Sudah cross-browser (Chrome, Edge, Firefox modern); sesuai D-04 |
| Debounce function | Custom setTimeout logic | Inline closure (lihat contoh atas) | Cukup sederhana, tidak perlu library |
| AJAX request | Custom XMLHttpRequest wrapper | `fetch()` native | Sudah tersedia, cleaner API |
| Excel date filtering | Custom Excel manipulation | Filter data sebelum masuk XLWorkbook | ClosedXML tidak perlu diubah, cukup filter rows sebelum ditulis |

---

## Common Pitfalls

### Pitfall 1: Date Range End-of-Day Boundary
**What goes wrong:** Filter `dateTo = 2025-01-31` tapi record dengan tanggal `2025-01-31 14:30:00` tidak ter-include karena `DateTime.Parse("2025-01-31")` = `2025-01-31 00:00:00`.
**Why it happens:** Input date HTML mengembalikan string `yyyy-MM-dd` tanpa waktu; DateTime comparison default ke 00:00:00.
**How to avoid:** Gunakan `to.Value.Date.AddDays(1).AddTicks(-1)` atau `record.Date.Date <= to.Value.Date` untuk membandingkan tanggal saja.
**Warning signs:** Record tanggal 31 Januari tidak muncul saat filter sampai 31 Januari.

### Pitfall 2: TrainingRecord Date Field Ganda
**What goes wrong:** Filtering training hanya oleh `Tanggal` tapi record tertentu punya `TanggalMulai` yang berbeda.
**Why it happens:** `TrainingRecord` punya dua field tanggal: `TanggalMulai` (nullable) dan `Tanggal`. `GetAllWorkersHistory` sudah menggunakan `t.TanggalMulai ?? t.Tanggal` — harus konsisten.
**How to avoid:** Selalu gunakan `tr.TanggalMulai ?? tr.Tanggal` sebagai effective date untuk training records. Ini sudah terdokumentasi di komentar `WorkerDataService.cs:158`.

### Pitfall 3: AssessmentSession Date Field
**What goes wrong:** Filter assessment berdasarkan `Schedule` saja, tapi session bisa punya `CompletedAt` berbeda dari jadwal.
**Why it happens:** Assessment bisa selesai di hari berbeda dari jadwal awal.
**How to avoid:** Gunakan `a.CompletedAt ?? a.Schedule` — sama dengan pola di `GetAllWorkersHistory`.

### Pitfall 4: Export Actions Tidak Menerima Date Params
**What goes wrong:** Export Excel hasilnya tidak konsisten dengan tabel yang ditampilkan karena export action tidak menerapkan date filter.
**Why it happens:** `ExportRecordsTeamAssessment` dan `ExportRecordsTeamTraining` tidak punya parameter `dateFrom`/`dateTo`.
**How to avoid:** Tambahkan `dateFrom`/`dateTo` ke kedua export actions; filter `assessmentRows` dan `trainingRows` setelah di-fetch dari `GetAllWorkersHistory` berdasarkan date range. Atau: pass date params ke `GetWorkersInSection` untuk filter worker IDs, lalu filter history rows by both worker ID dan date.

### Pitfall 5: workerCount Tidak Akurat Setelah AJAX
**What goes wrong:** `#workerCount` menampilkan jumlah lama setelah AJAX refresh.
**Why it happens:** Counter di-update oleh JS client-side yang menghitung DOM rows lama.
**How to avoid:** Setelah replace innerHTML `#workerTableBody`, hitung ulang `querySelectorAll('.worker-row').length`. Atau: server include count dalam response header / data attribute.

### Pitfall 6: L4 Locked Section Tidak Ter-lock di AJAX
**What goes wrong:** L4 user bisa bypass section lock karena AJAX request mengirim section kosong.
**Why it happens:** Section lock dihandle di view (disabled select), tapi AJAX JS hanya membaca `sectionFilter.value`.
**How to avoid:** Controller `RecordsTeamPartial` harus enforce section lock server-side (sama dengan pola existing di `Records` action dan export actions) — bukan bergantung pada client value.

---

## Code Examples

### Layout Row 2 Baru (RecordsTeam.cshtml)
```html
<!-- Source: existing RecordsTeam.cshtml Row 2, dimodifikasi sesuai D-01 -->
<div class="row g-3">
    <!-- Status -->
    <div class="col-12 col-md-3">
        <label class="form-label small text-muted mb-1">Status</label>
        <select id="statusFilter" class="form-select" onchange="filterTeamTable()">
            <option value="ALL">ALL</option>
            <option value="Sudah">Sudah</option>
            <option value="Belum">Belum</option>
        </select>
    </div>
    <!-- Tanggal Awal (baru, menggantikan Search) -->
    <div class="col-12 col-md-3">
        <label class="form-label small text-muted mb-1">Tanggal Awal</label>
        <input type="date" id="dateFrom" class="form-control" onchange="filterTeamTable()">
    </div>
    <!-- Tanggal Akhir (baru) -->
    <div class="col-12 col-md-3">
        <label class="form-label small text-muted mb-1">Tanggal Akhir</label>
        <input type="date" id="dateTo" class="form-control" onchange="filterTeamTable()">
    </div>
    <!-- Reset -->
    <div class="col-12 col-md-3 d-flex align-items-end">
        <button class="btn btn-outline-secondary w-100" onclick="resetTeamFilters()">
            <i class="bi bi-x-circle me-1"></i>Reset
        </button>
    </div>
</div>
```

### resetTeamFilters() Update
```javascript
// Tambahkan clear dateFrom/dateTo, hapus searchFilter clear
function resetTeamFilters() {
    var sectionFilter = document.getElementById('sectionFilter');
    var isLocked = sectionFilter.disabled;
    if (!isLocked) sectionFilter.value = '';
    document.getElementById('unitFilter').value = '';
    document.getElementById('categoryFilter').value = '';
    document.getElementById('subCategoryFilter').value = '';
    document.getElementById('subCategoryFilter').disabled = true;
    document.getElementById('statusFilter').value = 'ALL';
    document.getElementById('dateFrom').value = '';   // baru
    document.getElementById('dateTo').value = '';      // baru
    // searchFilter dihapus
    if (isLocked) updateUnitOptions();
    filterTeamTable();
    updateExportLinks();
}
```

### updateExportLinks() Update
```javascript
function updateExportLinks() {
    const section = document.getElementById('sectionFilter').value;
    const unit = document.getElementById('unitFilter').value;
    const status = document.getElementById('statusFilter').value;
    const category = document.getElementById('categoryFilter')?.value || '';
    const subCategory = document.getElementById('subCategoryFilter')?.value || '';
    const dateFrom = document.getElementById('dateFrom').value;   // baru
    const dateTo = document.getElementById('dateTo').value;        // baru

    const baseAssessment = '@Url.Action("ExportRecordsTeamAssessment", "CMP")';
    const baseTraining = '@Url.Action("ExportRecordsTeamTraining", "CMP")';

    const params = new URLSearchParams();
    if (section) params.set('section', section);
    if (unit) params.set('unit', unit);
    if (status && status !== 'ALL') params.set('statusFilter', status);
    if (category) params.set('category', category);
    if (subCategory) params.set('subCategory', subCategory);
    if (dateFrom) params.set('dateFrom', dateFrom);   // baru
    if (dateTo) params.set('dateTo', dateTo);          // baru
    // search params DIHAPUS

    const qs = params.toString();
    document.getElementById('btnExportAssessment').href = baseAssessment + (qs ? '?' + qs : '');
    document.getElementById('btnExportTraining').href = baseTraining + (qs ? '?' + qs : '');
}
```

### Service Signature Baru
```csharp
// IWorkerDataService.cs — tambah parameter dateFrom/dateTo
Task<List<WorkerTrainingStatus>> GetWorkersInSection(
    string? section,
    string? unitFilter = null,
    string? category = null,
    string? search = null,
    string? statusFilter = null,
    DateTime? dateFrom = null,    // baru
    DateTime? dateTo = null       // baru
);
```

### Export Date Filter Pattern
```csharp
// Di ExportRecordsTeamAssessment — filter assessmentRows by date
DateTime? from = string.IsNullOrEmpty(dateFrom) ? null : DateTime.Parse(dateFrom);
DateTime? to = string.IsNullOrEmpty(dateTo) ? null : DateTime.Parse(dateTo);

var filtered = assessmentRows
    .Where(r => filteredIds.Contains(r.WorkerId))
    .Where(r => from == null || r.Date.Date >= from.Value.Date)
    .Where(r => to == null   || r.Date.Date <= to.Value.Date)
    .ToList();
```

---

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| Client-side DOM filter (filterTeamTable) | Server-side AJAX partial | Count per worker jadi akurat karena dihitung di DB query |
| Search Nama/NIP | Date range filter | Search dihapus sepenuhnya dari filter bar |
| Export link static | Export link + dateFrom/dateTo params | Export konsisten dengan tampilan tabel |

---

## Scope & Batasan Penting

1. **File yang berubah:** `RecordsTeam.cshtml`, `CMPController.cs`, `WorkerDataService.cs`, `IWorkerDataService.cs`
2. **File baru yang dibuat:** `_RecordsTeamBody.cshtml` (partial view untuk TBODY content), `RecordsTeamPartial` action di CMPController
3. **Records.cshtml:** Tidak berubah (parent view)
4. **WorkerTrainingStatus model:** Tidak perlu perubahan — field `CompletedAssessments`, `CompletedTrainings` tetap ada; nilainya yang berubah berdasarkan date filter

---

## Open Questions

1. **Response format: full tbody HTML vs JSON?**
   - Rekomendasi: HTML partial (`_RecordsTeamBody.cshtml`) — server merender baris termasuk data-attribute dan badges, konsisten dengan pola Razor existing. JSON memerlukan JS rebuild table yang lebih kompleks.

2. **Debounce timing?**
   - Rekomendasi: 300ms untuk date input. Filter lain (select) bisa 0ms (langsung) karena tidak ada typing.

3. **Loading indicator design?**
   - Rekomendasi: Spinner kecil di atas table (`<div id="tableLoadingIndicator">`) dengan Bootstrap spinner, tersembunyi by default.

4. **subCategory parameter di export?**
   - Saat ini `ExportRecordsTeamAssessment` tidak menerima `subCategory`. Perlu tambah parameter ini juga (konsisten dengan tabel view). Ini terkait EXP-01/EXP-02 — konsistensi tabel vs export.

---

## Sources

### Primary (HIGH confidence)
- `Views/CMP/RecordsTeam.cshtml` — Full view source, dibaca langsung
- `Controllers/CMPController.cs` lines 368-607 — Records, ExportRecordsTeamAssessment, ExportRecordsTeamTraining
- `Services/WorkerDataService.cs` lines 83-268 — GetWorkersInSection, GetAllWorkersHistory
- `Services/IWorkerDataService.cs` — Interface definitions
- `Models/WorkerTrainingStatus.cs` — ViewModel structure

### Secondary (MEDIUM confidence)
- `.planning/phases/239-date-range-filter-export/239-CONTEXT.md` — User decisions D-01 hingga D-05

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — tidak ada library baru, semua existing
- Architecture: HIGH — dibaca langsung dari source code, patterns jelas
- Pitfalls: HIGH — diidentifikasi dari analisis kode aktual (date field ganda, L4 lock, end-of-day boundary)

**Research date:** 2026-03-23
**Valid until:** Sampai ada perubahan pada RecordsTeam.cshtml atau WorkerDataService (estimasi stabil 90+ hari)
