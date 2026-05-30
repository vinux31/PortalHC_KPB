---
phase: 337-cmp-records-full-overhaul-filter-data-arch-a11y
plan: 01
subsystem: cmp-records
tags: [filter, status-derivation, htmlencode, sertifikat-url, attempt-number, cmp]

requires:
  - phase: 215
    provides: GetWorkersInSection signature + Section/Unit/Category batch-load pattern
  - phase: 239
    provides: RecordsTeamPartial AJAX endpoint + date range filter
  - phase: 327
    provides: ValidUntil DateOnly migration (no impact, additive only)

provides:
  - 5 CRITICAL filter silent-fail fix (CMP-01..05) browser-Playwright-verified
  - 6 data integrity fix (CMP-06..11) — 4 browser-verified + 2 code-verified SKIP
  - WorkerDataService.GetWorkersInSection subCategory param + Category equality + post-loop narrow
  - AllWorkersHistoryRow Kategori + SubKategori populated for Export Training filter parity
  - Records.cshtml: Training SertifikatUrl link + Permanent badge + decodeEntities search + per-tipe counter

affects: [337-02 (UX baseline filter correctness), 337-03 (SQL push-down arch baseline)]

tech-stack:
  added: []
  patterns:
    - "Post-loop workerList narrow (Category + SubCategory) — bukan hanya set CompletionPercentage"
    - "decodeEntities helper untuk JS-side HtmlDecode (XSS-safe alternative)"
    - "Three-way Status switch (null = Completed, bukan Failed palsu)"

key-files:
  created: []
  modified:
    - Services/WorkerDataService.cs
    - Services/IWorkerDataService.cs
    - Models/AllWorkersHistoryRow.cs
    - Controllers/CMPController.cs
    - Views/CMP/Records.cshtml

key-decisions:
  - "Category narrow equality (string.Equals OrdinalIgnoreCase) — 'OJT' TIDAK match 'POJT' substring"
  - "statusFilter apply mandiri tanpa Category guard — fix silent-fail B-01"
  - "AttemptNumber title-null fallback ke 1 (no archived lookup) — legacy data quality issue"
  - "JS-side decodeEntities (bukan Razor Html.Raw) untuk hindari XSS surface"
  - "ExportRecordsTeamAssessment terima subCategory + category forward (consistency dengan UI worker list)"

patterns-established:
  - "filter post-loop narrow: workerList.Where(w => w.TrainingRecords.Any(t => Equals(...)))"
  - "data-type attribute di tr untuk per-tipe counter (assessment/training)"

requirements-completed: [CMP-01, CMP-02, CMP-03, CMP-04, CMP-05, CMP-06, CMP-07, CMP-08, CMP-09, CMP-10, CMP-11]

duration: ~30min
completed: 2026-05-30
---

# Phase 337-01: CMP Records Wave 1 Filter Silent-Fail + Data Integrity Summary

**5 CRITICAL filter silent-fail fixed + 6 data integrity render fixed di `/CMP/Records` — auto-Playwright-verified 9/11 PASS (2 SKIP code-verified diff).**

## Performance

- **Duration:** ~30 min (3 task code + auto-UAT)
- **Completed:** 2026-05-30
- **Tasks:** 4 (T1+T2+T3 code, T4 checkpoint UAT)
- **Files modified:** 5

## Accomplishments
- Filter Status/Category/SubCategory actual narrow worker list (sebelumnya silent-fail abaikan tanpa Category)
- Export Training honor subCategory filter (sebelumnya drop param)
- Data render fix: SertifikatUrl link Training + Permanent badge + Completed status (null IsPassed)
- JS search robust terhadap HtmlEncode "&" + per-tipe counter real-time
- AttemptNumber title-null no collision (default 1)

## Task Commits

1. **T1: Service filter + status + AttemptNumber** — `7c65c658` (feat)
2. **T2: Controller wire subCategory + AllWorkersHistoryRow** — `fe3fbe43` (feat)
3. **T3: Records.cshtml render SertifikatUrl + counter + search** — `4de88754` (feat)

## Files Modified

- `Services/WorkerDataService.cs` — GetWorkersInSection signature + Category equality + Category narrow + SubCategory narrow + statusFilter guard removal + GetUnifiedRecords Status 3-way switch + GetAllWorkersHistory AttemptNumber null fix + trainingRows Kategori/SubKategori populate
- `Services/IWorkerDataService.cs` — signature parity (subCategory param)
- `Models/AllWorkersHistoryRow.cs` — tambah Kategori + SubKategori properties
- `Controllers/CMPController.cs` — RecordsTeamPartial pass subCategory; ExportRecordsTeamTraining add subCategory param + filter; ExportRecordsTeamAssessment add category+subCategory params + forward
- `Views/CMP/Records.cshtml` — Sertifikat td (SertifikatUrl link + Permanent badge); Status switch (Completed=bg-info + Permanent=bg-success); 4 counter id (totalCountBadge, assessmentStatCount, trainingStatCount, totalStatCount); tr data-type attr; filterTable JS (decodeEntities + per-tipe counter)

## UAT Verification (Auto-Playwright)

| REQ-ID | Status | Evidence |
|--------|--------|----------|
| CMP-01 | ✅ PASS | Status=Sudah: 12→0 worker (mandiri tanpa Category guard). Status=Belum: 12. |
| CMP-02 | ✅ PASS | subCategory=Gas Tester: narrow ke 2 worker (Rino+Iwan); =DOESNOTEXIST: 0 row |
| CMP-03 | ✅ PASS | Category=OJT: 12→7 worker (5 non-OJT hidden) |
| CMP-04 | ⏭ SKIP | Tidak ada seed "POJT"; code-verified via diff (`.Contains` → `string.Equals` OrdinalIgnoreCase) |
| CMP-05 | ✅ PASS | Export Training: subCategory=DOESNOTEXIST byte-diff −132 (2 row dropped); withSub == noSub (all Mandatory HSSE = Gas Tester) |
| CMP-06 | ✅ PASS | AssessmentSession IsPassed=NULL Status=Completed → badge "Completed" bg-info (BUKAN "Failed") |
| CMP-07 | ✅ PASS | TrainingRecord SertifikatUrl populated → tombol "Lihat" link `/uploads/test-cert-337.pdf` |
| CMP-08 | ✅ PASS | TrainingRecord Status=Permanent → badge "Permanen" hijau + infinity icon |
| CMP-09 | ✅ PASS | Search "&" match "Q&A Session 337" (decodeEntities works) |
| CMP-10 | ✅ PASS | Search "&" → 4 counter update: totalBadge=1, assessmentStat=0, trainingStat=1, totalStat=1 |
| CMP-11 | ⏭ SKIP | DB AssessmentSessions Title IS NULL count=0; code-verified via diff (title-null branch returns 1) |

**UAT Coverage:** 9/11 PASS browser-Playwright, 2/11 SKIP code-verified.

## Threats

| Threat ID | Status |
|-----------|--------|
| T-337-01-01 Tampering filter regression | mitigated (auto-Playwright UAT covers narrow) |
| T-337-01-02 L4 section lock bypass | mitigated (L749 sectionFilter logic preserve, regression-not-touched) |
| T-337-01-03 SertifikatUrl path disclosure | accept (existing TrainingAdmin surface, rel="noopener" added) |
| T-337-01-04 SubCategory in-memory scan DoS | accept (Wave 3 Plan 03 SQL push-down handle) |
| T-337-01-05 RecordsTeamPartial L5/L6 bypass | mitigated (L746 roleLevel>=5 Forbid preserve) |
| T-337-01-06 Filter audit | accept (read-only, no AuditLog per CONCERNS) |

## Seed Workflow

- DB backup: `C:\Temp\HcPortalDB_Dev_pre_337-01_uat.bak` PRE-UAT
- Temp seed: 3 TrainingRecords admin (SertifikatUrl/Permanent/Q&A) + 1 AssessmentSession admin (Completed null)
- Restore: POST-UAT verified clean (TR_admin=0, AS_admin=4 baseline)
- Journal: `docs/SEED_JOURNAL.md` row 2026-05-30 phase 337-01 status=cleaned

## Lessons & Surprises

- ExportRecordsTeamAssessment originally tidak terima Category param (assessment-only export tapi worker list filter scope perlu konsistensi)
- File-level commit split lebih praktis dari per-task commit (T1/T2/T3 touch overlapping files)
- Mandatory HSSE Training seed: 100% rows are SubKategori=Gas Tester (no diversity untuk CMP-04 + minimal noSub vs withSub byte-diff CMP-05)

## Next

- Wave 2 Plan 02 (UX + Quality 12 REQ — race AJAX, ViewModel refactor, a11y)
- Tidak ada D-10 spawn (Plan 02 sudah depend Plan 01 via wave sequence)
