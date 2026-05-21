---
phase: 320-assessment-export-per-peserta-excel
plan: 02
status: complete-pending-uat
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

## UAT Status — PENDING

**Browser UAT belum dijalankan.** Deferred per interactive mode decision (batch UAT di akhir plan).

### UAT Checklist Required (sebelum claim Plan 02 fully complete)

1. **DB lokal snapshot** — `sqlcmd ... BACKUP DATABASE HcPortalDB_Dev` ke folder backup before seed temp
2. **`dotnet run`** — buka `http://localhost:5277`
3. **Login admin** — `admin@pertamina.com` (per MEMORY.md reference_dev_credentials.md)
4. **Trigger Export** — pilih 1 grup assessment yang punya minimal 1 Completed session
5. **Verify file output:**
   - Filename suffix `_Summary.xlsx` ✓
   - Tab 1 "Summary" (renamed dari "Results") ✓
   - Tab 2..N per-peserta dengan nama `{NIP}_{FullName}` 31-char
   - Tab Online: header row 1-5 + ET tabel + Spider Chart 400×400 biru + Detail Jawaban
   - Edge case `Open + CompletedAt!=null` (1 session di DB) muncul sebagai tab Completed ✓ (per deviation #1)
6. **Variant B UAT** — kalau ada session `IsManualEntry=true`:
   - Tab manual entry: Info Sertifikasi Manual 4 row + hyperlink Link Sertifikat clickable
   - Section ET/Chart/Detail Jawaban TIDAK muncul (skip via `continue;`)
   - **Kalau tidak ada di DB lokal:** seed temporary per `docs/SEED_WORKFLOW.md` (UPDATE 1 session existing jadi IsManualEntry=1 + isi 5 field), catat di `docs/SEED_JOURNAL.md`, restore DB setelah UAT
7. **Edge cases:**
   - Peserta `<3 ET` → Spider Chart skip
   - Peserta dengan Essay → "Essay – manual grading (lihat Penilaian Essay)"
   - Peserta Abandoned → soal-soal yang di-skip muncul "Tidak dijawab" + ✗
8. **Baseline file size** — record file size 10-peserta untuk Plan 03 benchmark baseline

## Baseline File Size

Belum diukur — akan dicatat saat UAT.

## Signal to Plan 03

Controller refactor full functional. Plan 03 ready untuk:
- **Task 11:** Refactor inline `RenderRadarPng` jadi `Parallel.ForEachAsync` pre-compute + cache lookup (perf SLA 30s)
- **Task 12:** Playwright spec `tests/playwright/specs/export-per-peserta.spec.ts`
- **Task 13:** UAT 8-step manual checklist execution
- **Task 14:** Tag `v17.0-p320-complete`

## DEV_WORKFLOW §5 Pre-commit Checklist

- [x] dotnet build pass per commit (0 new warnings)
- [ ] dotnet run + browser UAT — **PENDING**
- [ ] Golden path + edge case dicek manual — **PENDING**
- [x] No DB migration (Plan 02 tidak touch schema)
- [ ] Playwright defer ke Plan 03 ✓
- [ ] Notify IT defer ke Plan 03 final ✓
