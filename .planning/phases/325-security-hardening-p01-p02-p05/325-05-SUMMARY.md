# Plan 325-05 SUMMARY — UAT batch (PARTIAL)

**Status:** PARTIAL — SC-1 + SC-6 automated PASS, SC-2..SC-5 pending manual user UAT
**Wave:** 3
**Commits:** `f47ff95a` (SC-1 xUnit extend) + `026126cd` (325-UAT.md skeleton)
**Date:** 2026-05-27

## What Was Done

| Step | Method | Status |
|------|--------|--------|
| SC-1 P01 path traversal | xUnit `SaveFileAsync` 3 test (`StripsToFlatNameNoEscape` + `LogsWarningD10` + `NormalFilename_NoWarning`) | ✅ PASS automated |
| SC-6 unit test foundation | xUnit `ValidateCertificateFile` 7 test (Plan 02 commit) | ✅ PASS automated |
| 325-UAT.md skeleton | SC-2..SC-5 template untuk user fill manual | ✅ created |
| Dotnet run app + login curl | Started/stopped untuk explorasi, replaced dengan xUnit | bg terminated |

**dotnet test final:** Passed: 10, Skipped: 0, Failed: 0, Total: 10, Duration: 476ms.

## What Was Deferred to User Manual UAT

| SC | Description | Why Manual |
|----|-------------|------------|
| SC-2 | `.exe` rename `.pdf` reject di browser AddTraining | Visual ModelState error confirmation + Windows file system check |
| SC-3 | PDF/JPG/PNG asli upload regression | Butuh real file (Word PDF, camera JPG, screenshot PNG) + visual TempData success |
| SC-4 | Referenced delete blocked di UI | DB sqlcmd snapshot + seed + UI click delete + visual TempData error |
| SC-5 | Standalone delete success di UI | Same + visual TempData success + DB restore |

**Replace Postman SC-1 dengan xUnit rationale:** Form `/Admin/AddTraining` pakai dynamic JS `WorkerCerts[i].*` index render → curl POST butuh valid UserId GUID + index manual (scripting kompleks). xUnit cover helper langsung = deterministic, automated, no DB seed, no auth, no app running.

## Phase 325 Overall Status (PHASE_NOT_FULLY_SHIPPED)

| Plan | Status | Commits |
|------|--------|---------|
| 01 xUnit bootstrap | ✅ COMPLETE | `7069ead2` + `3255b9b4` + `93c8ef01` |
| 02 P01+P02+ILogger | ✅ COMPLETE | `524da7eb` + `1920e709` + `0a0f6db5` + `63fe0c78` |
| 03 refactor 3 inline site | ✅ COMPLETE | `1df212c6` + `27dd375f` |
| 04 P05 FK quick patch | ✅ COMPLETE | `bea6cb6e` + `9d2ffe99` + `5275081b` |
| 05 UAT batch | ⏳ PARTIAL | `f47ff95a` + `026126cd` + this SUMMARY |

**Code complete: 4/5 plans + SC-1/SC-6 automated.**
**Block point:** Phase 325 push origin/main butuh user UAT SC-2..SC-5 PASS dulu.

## Next Steps (User)

1. **Resume manual UAT:** Anda jalankan SC-2..SC-5 di lokal:
   - SC-2 + SC-3: browser AddTraining upload `.exe` rename `.pdf` + 3 file asli.
   - SC-4 + SC-5: sqlcmd snapshot + UI seed + UI delete + restore.
   - Fill section di `325-UAT.md` dengan verdict masing-masing.
2. **Update SEED_JOURNAL.md:** kalau lakukan SC-4/SC-5, audit trail seed temp + cleaned flag.
3. **Reply approval:** "SC-2..SC-5 PASS — ready commit final + push" → executor lanjut update STATE.md/ROADMAP.md `[x]` Phase 325 + push gating.

## Threat Mitigation (Code-Level Final)

| Threat | Severity | Code-Level Status | UAT Verified |
|--------|----------|-------------------|--------------|
| T-325-01 path traversal | HIGH | MITIGATED (Plan 02 D-01 + D-10) | ✅ SC-1 xUnit |
| T-325-02 MIME spoof | MED | MITIGATED (Plan 02 D-02/D-03/D-09) | ⏳ SC-2 manual |
| T-325-03 DB 500 leak | MED | MITIGATED (Plan 04 D-04/D-05/D-06) | ⏳ SC-4 manual |
| T-325-04 P02 bypass | HIGH | MITIGATED (Plan 03 refactor 3 site) | ⏳ SC-2/SC-3 manual |
| T-325-05 TOCTOU race | LOW | ACCEPTABLE + safety net | ⏳ implicit SC-4 |
