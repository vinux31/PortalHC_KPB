# Phase 320: Assessment Export Per-Peserta Excel - Context

**Gathered:** 2026-05-21
**Status:** Ready for planning
**Milestone:** v17.0 Assessment Admin Power Tools

<domain>
## Phase Boundary

Extend method `AssessmentAdminController.ExportAssessmentResults` (line 3651) sehingga file Excel berisi:

1. **1 sheet "Summary"** (rename dari "Results" — breaking change) — tabel ringkas existing (Name/NIP/Jumlah Soal/Status/Score/Result/Completed At) + header info assessment.
2. **N sheet per-peserta** untuk session `Status == Completed || Abandoned`, nama sheet format `{NIP}_{FullName}` (31-char truncate, exclude invalid char):
   - **Variant A (Online, `IsManualEntry == false`):** header (Nama+NIP, Started/Completed At, Durasi Aktual, Tipe) + section "Analisis Elemen Teknis" + PNG radar 500×500 render (embed 400×400, skip kalau <3 ET) + section "Detail Jawaban" (MC/MA per soal, "Tidak dijawab" untuk soal tanpa response, Essay skip dengan note).
   - **Variant B (Manual Entry, `IsManualEntry == true`):** header + section "Info Sertifikasi Manual" (Penyelenggara / Kota / SubKategori / CertificateType + hyperlink `ManualSertifikatUrl`). NO ElemenTeknis / Chart / Detail Jawaban.

**Out of scope (other phase):**
- Edit jawaban peserta (Phase 321 EDIT-01..13)
- DB schema change / migration
- UI page baru selain trigger export existing
- Essay grading / preview rubrik

</domain>

<decisions>
## Implementation Decisions

### Sheet Ordering
- **D-01:** Sheet per-peserta urut `OrderBy(s => s.User?.FullName ?? "")` ascending — konsisten dgn research existing + UI ManageAssessment pattern.

### Chart Visual Style
- **D-02:** Warna chart radar pakai biru research RGB (54,162,235) stroke + (54,162,235,96) fill — Chart.js default, kontras tinggi, ready di research code.
- **D-03:** Render canvas 500×500 di `SpiderChartRenderer.RenderRadarPng`, embed di sheet via `WithSize(400, 400)` — kompak (~22 row), sharp downscale.

### Plan Sub-Numbering Strategy
- **D-04:** Pecah 12 task research jadi **3 PLAN file atomic per layer**:
  - `320-01-PLAN.md` **helpers** — Task 1-3 (SkiaSharp PackageReference, `Helpers/SpiderChartRenderer.cs`, `Helpers/SheetNameSanitizer.cs`)
  - `320-02-PLAN.md` **controller** — Task 4-10 (rename Summary, filter eligible sessions, per-peserta loop + header, ElemenTeknis section, chart embed, Detail Jawaban, Manual Entry variant)
  - `320-03-PLAN.md` **perf-uat** — Task 11-12 (Parallel.ForEachAsync PNG pre-compute + Playwright + manual UAT checklist + tag milestone)
- **Rationale:** Paralel-able antar PLAN setelah PLAN 01 selesai (helpers locked → PLAN 02 + benchmark task PLAN 03 bisa pisah review). Atomic per layer mudah revert kalau task spesifik regression.

### Testing Strategy
- **D-05:** Hybrid Playwright + manual UAT — split by automation feasibility.
  - **Playwright (auth + download flow regression):**
    1. Login Admin → trigger Export → assert .xlsx downloaded + `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` + `size > 1KB`.
    2. Login HC → same assertion (REQ EXP-07 verify HC parity).
    3. Login Worker → assert 403/redirect to login (negative permission test).
    4. Benchmark: Export group ≥50 peserta → assert response time `< 30s` (REQ EXP-08).
  - **Manual UAT (sheet content + visual + cross-Excel — Playwright tidak praktis):**
    1. Tab pertama bernama "Summary", filename `*_Summary.xlsx`.
    2. Sheet name format `{NIP}_{FullName}`, truncate tepat 31 char, no collision saat nama panjang.
    3. Section ElemenTeknis tabel isi benar (Elemen / Benar / Total / Persentase).
    4. PNG radar render visual benar (warna biru, 5+ axis, polygon fill semi-transparent).
    5. Detail Jawaban ✓/✗ benar untuk MC + MA, "Tidak dijawab" muncul untuk Abandoned skip soal, Essay note tampil.
    6. Variant B Manual Entry: section + hyperlink `ManualSertifikatUrl` clickable.
    7. Buka file di Excel **dan** LibreOffice — chart render OK keduanya.
    8. Edge case: peserta ET <3 elemen → tabel tampil, chart skip (no broken image placeholder).

### Carrying Forward (Prior Phase Patterns)
- **D-06:** Commit per task (12 commit) — konsisten dgn pattern Phase 312/313/314 milestone v16.0. Pre-commit checklist `docs/DEV_WORKFLOW.md §5` wajib per commit.
- **D-07:** No DB schema change → no migration → no IT promo blocker untuk DB Dev/Prod. Notifikasi IT cukup commit hash + flag "No migration" via channel komunikasi biasa.
- **D-08:** Project test infra = `dotnet build` + browser UAT (per CLAUDE.md + research §infra). Tidak buat xUnit/NUnit test project baru untuk phase ini.

### Claude's Discretion
- Naming PNG cache variable + lock strategy (`Dictionary<int, byte[]>` + `lock` per research, atau `ConcurrentDictionary` setara) — Claude pick efficient.
- Field name verification `Penyelenggara / Kota / SubKategori / CertificateType / ManualSertifikatUrl` sudah verified codebase (Models/AssessmentSession.cs:130-147) — no further check needed.
- Helper auto-fit `ws.Columns().AdjustToContents()` placement (sesuai research Task 9 step 2).
- Worker filter via `_context.PackageUserResponses + UserPackageAssignments + PackageQuestions.Include(Options)` pre-load (avoid N+1) — Claude execute persis research.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Research (codebase-verified, 12-task breakdown)
- `.planning/phases/320-assessment-export-per-peserta-excel/320-RESEARCH.md` — Full task breakdown dengan code blocks, file structure map, spec coverage matrix.

### Project Spec
- `docs/superpowers/specs/2026-05-20-assessment-admin-power-tools-design.md` §3 (commit `c37e55ef`) — Design spec original, Section 3 Export, Section 5.10 UAT checklist, Section 5.12 lib deps.

### Source Files (target untuk modifikasi/create)
- `Controllers/AssessmentAdminController.cs:3651` — method `ExportAssessmentResults` (modify)
- `Models/AssessmentSession.cs:130-147` — verified fields `IsManualEntry`, `Penyelenggara`, `Kota`, `SubKategori`, `CertificateType`, `ManualSertifikatUrl`
- `HcPortal.csproj` — tambah `SkiaSharp 3.116.1` + `SkiaSharp.NativeAssets.Win32 3.116.1`
- `Helpers/SpiderChartRenderer.cs` — create (PNG radar via SkiaSharp)
- `Helpers/SheetNameSanitizer.cs` — create (`{NIP}_{FullName}` 31-char + invalid char scrub)

### Codebase Maps
- `.planning/codebase/ARCHITECTURE.md` — overall layering
- `.planning/codebase/STACK.md` — .NET 8 + ClosedXML 0.105.0 + Bootstrap 5
- `.planning/codebase/TESTING.md` — confirm no test infra (manual UAT pattern)
- `.planning/codebase/CONVENTIONS.md` — commit message format, helper naming

### Workflow Refs
- `docs/DEV_WORKFLOW.md §5` — pre-commit checklist (build + run + browser UAT + notify IT)
- `CLAUDE.md` — language (Bahasa Indonesia), Develop Workflow, Seed Data Workflow

### Requirements
- `.planning/REQUIREMENTS.md` EXP-01..08 — 8 acceptance criteria untuk Phase 320

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Controllers/AssessmentAdminController.ExportAssessmentResults` — base method (line 3651), worksheet "Results" creation + Summary tabel data rows existing. Refactor target.
- `ClosedXML.Excel.IXLWorksheet` + `XLHyperlink` + `AddPicture().MoveTo().WithSize()` — Excel API sudah dipakai, pola sama untuk per-peserta loop + chart embed.
- `_context.PackageUserResponses` / `SessionElemenTeknisScores` / `UserPackageAssignments` / `PackageQuestions.Include(Options)` — EF Core query pattern dipakai di controller existing (line ~3700-3789), pre-load untuk avoid N+1.

### Established Patterns
- **No test infra** — manual UAT via `dotnet build && dotnet run` + browser, dokumentasi UAT di task step list. Phase 320 ikut pattern + tambah Playwright auth/download (yang sebelumnya jarang dipakai project).
- **Commit per task** — 1 task research = 1 commit kecil, message format `feat|refactor|perf(v17.0-p320): ...`. Breaking change pakai `!` + paragraf `BREAKING CHANGE:`.
- **DEV_WORKFLOW §5 pre-commit** — pasangan baku: build, run, manual verify, notify IT post-push.
- **Helpers folder** — `Helpers/*.cs` static class pattern, `namespace HcPortal.Helpers`, no DI registration (pure static).

### Integration Points
- Tombol Export di view `Views/AssessmentAdmin/ManageAssessment.cshtml` (no UI change Phase 320 — controller method signature unchanged).
- Download response: `File(content, contentType, fileName)` existing pattern di controller, filename suffix berubah `_Summary.xlsx`.
- SkiaSharp Win32 native asset: extracted ke `bin/.../runtimes/win-x64/native/libSkiaSharp.dll` runtime — verify smoke render di Task 2.

### Creative Options Constrained
- DB schema/EF model change → BUKAN scope phase ini (REQ jelas "No DB schema change"). Kalau muncul kebutuhan field baru di session, defer ke Phase backlog.
- Frontend chart library (Chart.js) → tidak relevan, chart server-side render PNG.

</code_context>

<specifics>
## Specific Ideas

- **Sheet ordering** user prefer FullName asc (sesuai monitoring detail UI) supaya admin/HC familiar lokasi peserta saat klik tab.
- **Chart color biru research** dipertahankan — bukan brand Pertamina. Rationale: chart konteks data assessment internal, tidak brand-facing artifact.
- **3 PLAN split** dipilih untuk balance: helpers (foundation) → controller (bulk logic) → perf+UAT (validation). PLAN 02 + PLAN 03 bisa di-review/execute pisah.
- **Playwright hybrid** baru di project ini — request user "via playwright, dan manual yang gak bisa playwright". Playwright cover yang automatable (auth flow + download trigger + benchmark), manual cover sheet content + visual + cross-Excel verify.

</specifics>

<deferred>
## Deferred Ideas

- **Brand color chart variant** — kalau ada request future konsisten Pertamina identity, bisa parameterize `SpiderChartRenderer.RenderRadarPng(data, size, SKColor strokeColor, SKColor fillColor)`. Untuk Phase 320 hardcode biru research.
- **xlsx content assert di test** — kalau test infra (xUnit project) eventually ditambah, bisa upgrade Playwright dari "download trigger only" → "parse xlsx + assert sheet count / sheet name / cell value". Sekarang manual UAT.
- **Test project setup (xUnit/NUnit)** — project belum punya test infra. Kalau eventually setup, bisa migrate manual UAT step ke automated test. Tracked sebagai future hygiene improvement, bukan Phase 320 scope.
- **EXP variant lain (PDF export, JSON export)** — REQ EXP scope cuma Excel. PDF/JSON variant kalau ada request future = phase terpisah.

### Reviewed Todos (not folded)
- `realtime-assessment.md` (score 0.6, matched on "phase, time, assessment") — bukan scope export, kemungkinan realtime monitoring related (Phase 321 SignalR teritori). Defer review ke /gsd-discuss-phase 321 atau backlog review.

</deferred>

---

*Phase: 320-assessment-export-per-peserta-excel*
*Context gathered: 2026-05-21*
