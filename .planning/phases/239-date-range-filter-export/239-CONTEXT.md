# Phase 239: Date Range Filter & Export - Context

**Gathered:** 2026-03-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Mengganti textbox Search Nama/NIP di filter bar Team View (CMP/Records) dengan 2 input date (Tanggal Awal & Tanggal Akhir). Tabel, count per worker, dan export ikut ter-filter berdasarkan rentang tanggal yang dipilih.

</domain>

<decisions>
## Implementation Decisions

### Layout Filter Bar
- **D-01:** Layout tetap 2 baris. Row 2 berubah dari `Status | Search | Reset` menjadi `Status | Tgl Awal | Tgl Akhir | Reset`. Row 1 tidak berubah (Bagian, Unit, Category, Sub Category).

### Filtering Approach
- **D-02:** Semua filter (termasuk date range) dikirim ke server via AJAX. Setiap perubahan filter apapun → AJAX request dengan semua parameter → server return data ter-filter (HTML partial atau JSON).
- **D-03:** Count Assessment & Training per worker dihitung server-side berdasarkan date range + filter lainnya. Tidak ada client-side re-counting.

### Date Input Format & Behavior
- **D-04:** Gunakan native HTML `<input type="date">` — tidak perlu library date picker tambahan.
- **D-05:** Auto-filter: saat user mengisi/mengubah tanggal, langsung trigger AJAX request (debounce jika perlu). Tidak perlu tombol Apply terpisah.

### Claude's Discretion
- Debounce timing untuk AJAX requests
- Response format dari server (JSON + JS rebuild table, atau HTML partial replace)
- Loading indicator saat AJAX request berlangsung
- Error handling jika AJAX gagal

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Team View Implementation
- `Views/CMP/RecordsTeam.cshtml` — Partial view Team View dengan filter bar, tabel worker, JS filtering & export logic
- `Views/CMP/Records.cshtml` — Parent view yang host tab My Records & Team View

### Controller & Export
- `Controllers/CMPController.cs` — Action Records (data source), ExportRecordsTeamAssessment, ExportRecordsTeamTraining

### Model
- `Models/WorkerTrainingStatus.cs` — ViewModel untuk Team View worker list

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `RecordsTeam.cshtml` sudah punya fungsi `updateExportLinks()` yang build query string dari semua filter — bisa di-extend untuk date params
- `filterTeamTable()` dan `resetTeamFilters()` sudah handle client-side filtering — akan di-refactor ke server-side AJAX
- Export buttons (`btnExportAssessment`, `btnExportTraining`) sudah wired ke `updateExportLinks()`

### Established Patterns
- Filter bar menggunakan Bootstrap grid `row g-3` dengan `col-12 col-md-N` responsive layout
- `form-select` dan `form-control` classes untuk input styling
- Label menggunakan `form-label small text-muted mb-1`
- Worker counter: `<span id="workerCount">` di-update oleh JS

### Integration Points
- Controller action `Records` di CMPController menyiapkan `ViewData["WorkerList"]` dan ViewBag data (SectionUnitsJson, MasterCategoriesJson, SubCategoryMapJson)
- Export actions menerima query params: section, unit, search, statusFilter, category, subCategory — perlu tambah dateFrom, dateTo
- SectionFilter untuk L4 user di-lock ke section mereka (disabled select)

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 239-date-range-filter-export*
*Context gathered: 2026-03-23*
