# Phase 176: Export Records & RecordsTeam - Context

**Gathered:** 2026-03-16
**Status:** Ready for planning

<domain>
## Phase Boundary

Add Excel export to CMP Records (personal) and RecordsTeam (team) pages. Users can download training history as .xlsx files. No new data queries needed — reuses existing `GetUnifiedRecords()` and `GetWorkersInSection()`.

</domain>

<decisions>
## Implementation Decisions

### Sheet structure
- Excel output split into 2 sheets: "Assessment" and "Training"
- Assessment sheet columns: Tanggal, Judul, Skor, Status (Passed/Failed), Sertifikat
- Training sheet columns: Tanggal, Judul, Penyelenggara, Kategori, Kota, Nomor Sertifikat, Valid Until, Status

### Records personal (EXP-01)
- 1 tombol "Export Excel" saja — exports semua data personal ke file dengan 2 sheet
- Tidak perlu ikut filter — export semua riwayat personal

### RecordsTeam (EXP-02)
- 2 tab (Assessment & Training), masing-masing tab punya tombol Export sendiri
- Export mengikuti filter aktif di halaman (section, unit, search, status, category)
- RecordsTeam Assessment tab → Export Assessment Excel (data sesuai filter)
- RecordsTeam Training tab → Export Training Excel (data sesuai filter)
- Role-scoped: HC/Admin semua data, SectionHead hanya section-nya

### Tombol posisi
- Tombol hijau "Export Excel" sejajar filter bar di kanan — pattern sama seperti ExportWorkers di ManageWorkers

### Claude's Discretion
- Exact column widths and header styling
- Filename format (e.g., Records_NamaUser_2026-03-16.xlsx)
- Empty state handling when no data to export

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Export pattern reference
- `Controllers/AdminController.cs` line ~3684 — `ExportWorkers` action (ClosedXML pattern to follow)
- `Controllers/CDPController.cs` line ~2101 — `ExportProgressExcel` action (another ClosedXML reference)

### Data source
- `Controllers/CMPController.cs` line ~653 — `GetUnifiedRecords()` returns `List<UnifiedTrainingRecord>`
- `Controllers/CMPController.cs` line ~397 — `Records` action (personal records page)
- `Controllers/CMPController.cs` line ~426 — `RecordsTeam` action (team records page)
- `Controllers/CMPController.cs` line ~455 — `RecordsWorkerDetail` action (per-worker detail)

### Views
- `Views/CMP/Records.cshtml` — Personal records view (add export button here)
- `Views/CMP/RecordsTeam.cshtml` — Team records view (add export buttons per tab)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `GetUnifiedRecords(userId)` — Already returns all assessment + training data for a user
- `GetWorkersInSection(sectionFilter)` — Already handles role-scoped worker list
- ClosedXML `XLWorkbook` pattern — Used in 6+ existing export actions

### Established Patterns
- Export actions: `public async Task<IActionResult> ExportXxx(filters)` returning `File(stream, contentType, filename)`
- MemoryStream + workbook.SaveAs(stream) + stream.Position = 0
- Green outline button `btn-outline-success` for export actions

### Integration Points
- New export actions added to `CMPController` (same controller as Records/RecordsTeam)
- Export buttons added to existing Records.cshtml and RecordsTeam.cshtml views
- Filter parameters passed via query string to export action

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches following existing export patterns.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 176-export-records-recordsteam*
*Context gathered: 2026-03-16*
