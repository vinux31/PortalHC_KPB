---
phase: 320-assessment-export-per-peserta-excel
plan: 02
status: complete-uat-passed
completed: 2026-05-21
---

# Plan 320-02 SUMMARY — Controller Refactor

## Commits

| # | Hash | Subject |
|---|------|---------|
| 4 | `c94e645d` | refactor(v17.0-p320)!: rename export sheet Results->Summary |
| 5 | `0b1c018c` | feat(v17.0-p320): filter eligible sessions (UI-match) for per-peserta sheets |
| 6 | `5fdd68cf` | feat(v17.0-p320): per-peserta sheet with session header + info |
| 7 | `5d07c8fa` | feat(v17.0-p320): per-peserta ElemenTeknis section (Variant A) |
| 8 | `08efe756` | feat(v17.0-p320): embed spider chart PNG (>=3 ET, SkiaSharp render) |
| 9 | `c75b891c` | feat(v17.0-p320): per-peserta Detail Jawaban table (MC+MA, Tidak dijawab handling) |
| 10 | `484a731a` | feat(v17.0-p320): per-peserta Variant B (Manual Entry section) |

7 atomic commits total. 4 unrelated commits (proton-video, sosialisasi-v3 docs) sandwiched between task commits — pre-existing background work, no impact.

## Files Touched

| Path | Action | Net LOC |
|------|--------|---------|
| `Controllers/AssessmentAdminController.cs` | modified (refactor + insert per-peserta block + add private method) | ~+250 |

## Build Verification

- `dotnet build` pass — 0 errors, 22 warnings (baseline unchanged, no new warnings)
- `Worksheets.Add` count in file: 3 (Summary tab line 3727, per-peserta loop, unrelated Question Import line ~4545)
- `AdjustToContents` in controller: **0 occurrences** ✓ (PATTERNS.md skip directive verified — delegated to `ExcelExportHelper.ToFileResult`)
- Pre-load batch query: 4 `_context.{table}` queries present BEFORE foreach (PackageUserResponses, SessionElemenTeknisScores, UserPackageAssignments, PackageQuestions.Include(Options))

## Requirements Addressed (EXP-01..07)

- EXP-01 — Per-peserta sheet generation in workbook
- EXP-02 — Sheet name format `{NIP}_{FullName}` 31-char
- EXP-03 — Variant A Online sheet structure (header + ET + chart>=3 + Detail Jawaban; "Tidak dijawab" handling)
- EXP-04 — Variant B Manual Entry sheet (Info Sertifikasi Manual + hyperlink)
- EXP-05 — Filter eligible Completed+Abandoned (UI-match logic — see Deviation below)
- EXP-06 — Excel-invalid char scrubbing via `SheetNameSanitizer.Sanitize`
- EXP-07 — `[Authorize(Roles = "Admin, HC")]` preserved (line 3650 unchanged)

## Threats Mitigated

- T-320-02-01 EoP — Authorize attribute preserved
- T-320-02-02 Sheet inject — `SheetNameSanitizer.Sanitize` called per session
- T-320-02-04 OOM — `using var ms` for PNG buffer (Plan 02 inline; refactor to cache at Plan 03)
- T-320-02-07 N+1 — 4 batch query pre-load before foreach

## Deviations from Plan

### 1. Filter Logic (Task 5)
**Plan literal:** `Status == "Completed" || Status == "Abandoned"`
**Applied:** UI-match logic `Status != "Cancelled" && ((CompletedAt != null || Score != null) || Status == "Abandoned")`

**Reason:** DB Dev distribution check revealed 1 edge-case session with `Status = "Open"` + `CompletedAt != null`. UI grid `ManageAssessment` shows it as "Completed" (derived from CompletedAt), but literal filter would skip → mismatch between Export tab list and grid.

Decision: prioritize UI consistency over plan literal. User confirmed (option A).

### 2. ElapsedSeconds Type (Task 6)
**Plan assumed:** `int?` nullable (`ElapsedSeconds.HasValue`)
**Actual:** `int` non-nullable default 0 (`Models/AssessmentSession.cs:47`)

**Fix:** `int durasi = session.ElapsedSeconds / 60; ws.Cell(...).Value = durasi > 0 ? $"{durasi} menit" : "—";`

### 3. Model File Path (Task 9 read_first)
**Plan reference:** `Models/PackageQuestion.cs` + `Models/PackageOption.cs`
**Actual:** Both classes live in `Models/AssessmentPackage.cs`

Field name verification still valid (`QuestionText`, `QuestionType` string?, `Options` ICollection, `OptionText`, `IsCorrect`).

## UAT Status — PASSED (Variant A) / DEFERRED (Variant B)

Executed via Playwright MCP automation + in-page `fetch()` + XLSX archive introspection. `dotnet run --no-build` background on `localhost:5277`. Login `admin@pertamina.com` / bootstrap password berhasil.

### Test Case: "OJT Semarang" 2026-03-25 (2 Completed sessions, 0 Manual Entry)

**Endpoint:** `GET /Admin/ExportAssessmentResults?title=OJT%20Semarang&category=OJT&scheduleDate=2026-03-25`

**Response:** HTTP 200, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, **58325 bytes**

### Verified via XLSX Archive Introspection

| Check | Expected | Actual | Result |
|-------|----------|--------|--------|
| Filename suffix | `_Summary.xlsx` | `OJT_Semarang_20260325_Summary.xlsx` | ✅ |
| Tab 1 name | `Summary` | `Summary` | ✅ |
| Tab 2 name | `{NIP}_{FullName}` 31-char | `123456_Iwan` | ✅ |
| Tab 3 name | `{NIP}_{FullName}` 31-char | `29007720_Rino` | ✅ |
| OrderBy FullName asc (D-01) | Iwan < Rino | Iwan first (sheet2), Rino second (sheet3) | ✅ |
| Header format | `{FullName} (NIP {NIP})` | `Iwan (NIP 123456)` | ✅ |
| 4 row label header | Started At/Completed At/Durasi Aktual/Tipe Assessment | All 4 present | ✅ |
| Durasi format | `{N} menit` | `7 menit` | ✅ |
| Tipe Assessment | non-empty | `Standard` | ✅ |
| ET section header | `Analisis Elemen Teknis` | present | ✅ |
| ET tabel 4 kolom | Elemen Teknis/Benar/Total/Persentase | All 4 present | ✅ |
| ET data (5 elemen) | ≥3 ET | 5 elemen (Kolom Distilasi, Pengendalian Kualitas, Reaksi Alkilasi, Mitigasi Hf, Heat Exchanger) | ✅ |
| Persentase format | `F1` Indonesian locale | `100,0%` | ✅ |
| Spider chart embed (≥3 ET trigger) | 2 PNG (1 per peserta) | `xl/media/image.png` + `image2.png` + `drawing1.xml` + `drawing2.xml` | ✅ |
| Detail Jawaban section | 6 kolom | No/Soal/Tipe/Jawaban Peserta/Jawaban Benar/Status | ✅ |

### Baseline File Size

**58325 bytes (~57 KB)** untuk 2 peserta Completed + 5 ET + spider chart × 2 + Detail Jawaban. Linear extrapolation (untuk Plan 03 Task 11 benchmark baseline): ~290 KB untuk 10 peserta hipothesis, ~1.45 MB untuk 50 peserta.

### UAT NOT Performed

- **Variant B (IsManualEntry=true)** — zero session di DB Dev. Defer ke Plan 03 (setelah seed temporary per `docs/SEED_WORKFLOW.md`).
- **Edge case `<3 ET`** — semua peserta di test group punya 5 ET. Skip rule sudah codepath-tested di Plan 01 acceptance + present di controller `if (sessionEt.Count >= 3)` guard.
- **Edge case Essay** — test group tidak ada soal Essay. Code branch ada di Task 9 (`if (tipe == "Essay")`), defer Playwright spec di Plan 03.
- **Edge case "Tidak dijawab"** — test group 2 peserta semua Completed (no Abandoned). Defer Playwright.
- **Open + CompletedAt edge case (filter deviation #1)** — 1 session di DB Dev. Tidak masuk test group `OJT Semarang` jadi tidak ter-verify di UAT ini, tapi filter logic ada di code (Task 5).

### Browser Console

`[ERROR] Failed to load resource: 404` untuk `/Identity/Account/Login` — pre-existing (app tidak pakai Identity scaffolded URL, pakai custom login di `/`). Not related Phase 320.

## Signal to Plan 03

Controller refactor full functional. Plan 03 ready untuk:
- **Task 11:** Refactor inline `RenderRadarPng` jadi `Parallel.ForEachAsync` pre-compute + cache lookup (perf SLA 30s)
- **Task 12:** Playwright spec `tests/playwright/specs/export-per-peserta.spec.ts`
- **Task 13:** UAT 8-step manual checklist execution
- **Task 14:** Tag `v17.0-p320-complete`

## DEV_WORKFLOW §5 Pre-commit Checklist

- [x] dotnet build pass per commit (0 new warnings)
- [x] dotnet run + browser UAT (Playwright MCP automated, OJT Semarang 2 peserta Completed)
- [x] Golden path Variant A (Online) fully verified via XLSX introspection
- [ ] Golden path Variant B (Manual Entry) — defer, requires seed (zero IsManualEntry=true di DB Dev)
- [ ] Edge case Essay/Abandoned/<3 ET — defer ke Plan 03 Playwright spec
- [x] No DB migration (Plan 02 tidak touch schema)
- [ ] Playwright spec defer ke Plan 03 Task 12 ✓
- [ ] Notify IT defer ke Plan 03 final commit ✓
