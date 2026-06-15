---
phase: 387-post-lisensor-assessment-polish
plan: 01
subsystem: api
tags: [aspnet, ef-core, signalr, closedxml, essay-grading, certificate, anti-tamper]

# Dependency graph
requires:
  - phase: 386-assessmentadmincontroller-hardening
    provides: "SubmitEssayScore status-guard D-08 (Status != PendingGrading) + upsert; FinalizeEssayGrading idempotency; shared display helpers"
provides:
  - "SubmitEssayScore WR-01 (Essay type guard) + WR-02 (cross-session ownership guard) — tutup celah hardening tertunda 386-REVIEW"
  - "FinalizeEssayGrading cert-number retry 3x + LogError + certError surface (PXF-08)"
  - "FinalizeEssayGrading workerSubmitted broadcast ke grup monitor (PXF-10)"
  - "Excel BulkExport 'Detail Jawaban' essay cell tampil TextAnswer + EssayScore, bukan '—' (PXF-09)"
affects: [387-04, monitoring-tab, essay-grading, certificate-issuance]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Server-authoritative write guard: validasi tipe + kepemilikan questionId sebelum upsert (mirror FinalizeEssayGrading shuffledIds gate)"
    - "Cert-number collision retry-loop 3x + LogError on persistent failure (port verbatim dari GradingService:287-318)"
    - "SignalR monitor-{batchKey} fire-and-forget broadcast untuk transisi Completed (workerSubmitted)"

key-files:
  created: []
  modified:
    - "Controllers/AssessmentAdminController.cs - SubmitEssayScore WR-01/WR-02 guards; FinalizeEssayGrading cert-retry + certError + monitor broadcast; BulkExport essay cell"

key-decisions:
  - "PXF-06 Completed-guard plan REDUNDANT — guard 386 D-08 (Status != PendingGrading di L3539) sudah menolak edit pasca-finalize. Task 1 di-redirect (orchestrator-directed) ke penutupan 2 gap hardening tertunda 386-REVIEW WR-01 + WR-02."
  - "WR-02 ownership via rantai navigasi EF PackageQuestion.AssessmentPackage.AssessmentSessionId == sessionId (AnyAsync), bukan UserPackageAssignment.GetShuffledQuestionIds() — lebih langsung & terverifikasi terhadap model."
  - "PXF-08 cert-retry pakai session.Id (session sudah di-load di FinalizeEssayGrading; session.Id == sessionId)."
  - "PXF-09 SURFACE GUARD D-06 dihormati: edit 'Detail Jawaban' di AssessmentAdminController.cs (~L4912), ExcelExportHelper.cs TIDAK tersentuh."

patterns-established:
  - "Anti-tamper essay grading: status-guard (386) + type-guard (WR-01) + ownership-guard (WR-02) berlapis sebelum upsert."
  - "Cert generation tidak pernah silent-pass: retry 3x → LogError → surface certError flag ke HC."

requirements-completed: [PXF-06, PXF-08, PXF-09, PXF-10]

# Metrics
duration: 22min
completed: 2026-06-16
---

# Phase 387 Plan 01: Post-Lisensor AssessmentAdminController Polish Summary

**SubmitEssayScore type+ownership hardening (WR-01/WR-02), cert-number retry 3x + certError surface (PXF-08), monitor workerSubmitted broadcast (PXF-10), dan Excel "Detail Jawaban" essay cell tampil jawaban+skor nyata (PXF-09) — semua in-place edit ke AssessmentAdminController.cs, 0 migration.**

## Performance

- **Duration:** ~22 min
- **Started:** 2026-06-16 (sesi eksekusi)
- **Completed:** 2026-06-16
- **Tasks:** 3
- **Files modified:** 1 (Controllers/AssessmentAdminController.cs)

## Accomplishments
- **Task 1 (PXF-06 redirected):** Tutup 2 celah hardening tertunda di `SubmitEssayScore` yang diperkenalkan upsert 386 D-08 — WR-01 (questionId wajib tipe Essay) + WR-02 (questionId wajib milik sessionId). Guard `Status == Completed` plan dibuktikan redundant karena guard 386 `Status != PendingGrading` (L3539) sudah menolak edit pasca-finalize — must_have PXF-06 ("edit essay pasca-finalize ditolak") tetap terpenuhi.
- **Task 2 (PXF-08 + PXF-10):** Cert-number generation retry 3x saat collision + `_logger.LogError` pada kegagalan persisten (ganti silent `catch (DbUpdateException) {}`) + surface `certError` flag ke HC; broadcast `workerSubmitted` ke grup `monitor-{batchKey}` agar tab Monitoring update tanpa refresh.
- **Task 3 (PXF-09):** Excel BulkExport "Detail Jawaban" essay branch tampil jawaban teks peserta (kol 4) + skor essay "Skor: X/Y"/"Belum dinilai" (kol 6), ganti placeholder "—".

## Task Commits

Each task was committed atomically:

1. **Task 1: PXF-06 redirect — WR-01 type + WR-02 ownership guards** - `9ccd9a17` (feat)
2. **Task 2: PXF-08 cert retry/log/surface + PXF-10 monitor broadcast** - `3b4ce043` (feat)
3. **Task 3: PXF-09 Excel "Detail Jawaban" essay cell** - `c459c047` (feat)

_Note: plan menandai task `tdd="true"` namun unit test eksplisit PXF-06/PXF-09 (real-SQL fixture, `[Trait Category=Integration]`) di-defer ke Plan 04 sesuai `<done>` plan. Verifikasi plan ini: build 0 error + grep acceptance + fast suite 347/347 GREEN._

## Files Created/Modified
- `Controllers/AssessmentAdminController.cs`:
  - `SubmitEssayScore` (~L3549-3562) — guard WR-01 (`question.QuestionType != "Essay"`) + WR-02 (`AnyAsync(q => q.Id == questionId && q.AssessmentPackage.AssessmentSessionId == sessionId)`).
  - `FinalizeEssayGrading` cert block (~L3743-3772) — retry-loop 3x + `IsDuplicateKeyException` filter + `LogError`.
  - `FinalizeEssayGrading` akhir (~L3814-3838) — broadcast `workerSubmitted` ke `monitor-{fbatchKey}` + `certError` flag di JSON.
  - BulkExport "Detail Jawaban" essay branch (~L4912-4919) — `essayResp.TextAnswer` + `EssayScore`.

## Decisions Made
- **Task 1 di-redirect oleh orchestrator** (lihat Deviations) — guard `Status == Completed` plan redundant; ganti dengan penutupan WR-01/WR-02 dari 386-REVIEW.
- **WR-02 via rantai navigasi EF** (`PackageQuestion.AssessmentPackage.AssessmentSessionId`) — terverifikasi terhadap `Models/PackageQuestion.cs` + `Models/AssessmentPackage.cs`. Lebih langsung daripada saran 386-REVIEW (`UserPackageAssignment.GetShuffledQuestionIds()`) namun ekuivalen secara semantik (questionId harus berasal dari paket sesi).
- **PXF-08/10 dieksekusi verbatim** dari PATTERNS §PXF-08/§PXF-10; `session.Id` dipakai di cert block (session di-load di awal method).

## Deviations from Plan

### Orchestrator-directed redirect (Task 1)

**1. [Orchestrator decision] PXF-06 Completed-guard diganti penutupan 386-REVIEW WR-01 + WR-02**
- **Found during:** Pre-execution (arahan orchestrator `<ORCHESTRATOR_DIRECTED_DEVIATION>`)
- **Issue:** Plan Task 1 menyuntik guard `if (session.Status == AssessmentConstants.AssessmentStatus.Completed) return reject`. Phase 386 (sudah shipped) menambahkan guard `if (session.Status != AssessmentConstants.AssessmentStatus.PendingGrading) return reject` di L3539 — yang SUDAH menolak sesi Completed (dan semua non-PendingGrading). Guard plan menjadi dead/redundant code.
- **Fix:** Tidak menyuntik guard `== Completed`. Sebagai gantinya, tutup 2 gap hardening tertunda dari `386-REVIEW.md` (WR-01 + WR-02) di `SubmitEssayScore`, setelah status-guard + range-guard dan sebelum upsert: WR-01 tolak bila `QuestionType != "Essay"`; WR-02 tolak bila questionId bukan milik sessionId. Comment menandai bahwa intent anti-tamper PXF-06 sudah dicakup guard 386 D-08.
- **Files modified:** Controllers/AssessmentAdminController.cs (L3549-3562)
- **Verification:** `dotnet build` 0 error; grep konfirmasi "Soal ini bukan tipe Essay." (L3556) + "Soal bukan milik sesi ini." (L3562) + nav chain (L3560) + status-guard 386 `!= PendingGrading` tetap utuh (L3539); fast suite 347/347 GREEN.
- **Committed in:** `9ccd9a17` (Task 1 commit)

Acceptance_criteria asli Task 1 (grep "Sesi sudah final" / `Status == Completed`) digantikan: grep konfirmasi guard WR-01 + WR-02 hadir, dan must_have PXF-06 (edit pasca-finalize ditolak) tetap berlaku via guard L3539.

---

**Total deviations:** 1 orchestrator-directed redirect (Task 1).
**Impact on plan:** Redirect menghilangkan dead code dan menukar dengan 2 hardening guard yang lebih bernilai (mencegah korupsi data lintas-tipe + lintas-sesi). Task 2 & 3 dieksekusi verbatim tanpa deviasi. No scope creep. 0 migration (sesuai plan).

## Issues Encountered
- Bash tool memakai POSIX shell (bukan PowerShell) → command `Select-String`/`Select-Object` gagal (exit 127). Diselesaikan dengan menjalankan build via `dotnet build ... | grep -Ei "..."` POSIX. Tidak berdampak pada hasil.

## Known Stubs
None — semua perubahan mengaliri data nyata (TextAnswer/EssayScore aktual, guard fungsional). Tidak ada placeholder/TODO/mock baru yang diperkenalkan.

## TDD Gate Compliance
Plan menandai task `tdd="true"`, tetapi unit test eksplisit PXF-06/PXF-09 di-defer ke Plan 04 (per plan `<done>`: "Unit test (Plan 04) confirms both branches" + `<verification>`: "Plan 04 adds the PXF-06 / PXF-09 unit tests"). Plan ini tidak menghasilkan commit `test(...)` RED/GREEN karena gating test berada di Plan 04. Fast suite existing 347/347 GREEN memastikan tidak ada regresi.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Plan 387-01 selesai. Plan 387-02, 387-03 (view/CMPController/AssessmentHub) tidak file-overlap dengan plan ini — siap dieksekusi.
- Plan 387-04 menambahkan unit test PXF-06 (WR-01/WR-02 branches) + PXF-09 (real-SQL fixture, Integration trait).
- 0 migration. Handoff IT KEDUA (pasca-acara) — flag migration = FALSE.

## Self-Check: PASSED

- FOUND: `.planning/phases/387-post-lisensor-assessment-polish/387-01-SUMMARY.md`
- FOUND commit `9ccd9a17` (Task 1 WR-01/WR-02)
- FOUND commit `3b4ce043` (Task 2 PXF-08/PXF-10)
- FOUND commit `c459c047` (Task 3 PXF-09)

---
*Phase: 387-post-lisensor-assessment-polish*
*Completed: 2026-06-16*
