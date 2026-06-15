---
phase: 387-post-lisensor-assessment-polish
plan: 04
subsystem: test
tags: [xunit, playwright, a11y, integration-test, real-sql-fixture, signalr, certificate, anti-tamper, essay-grading]

# Dependency graph
requires:
  - phase: 387-01
    provides: "SubmitEssayScore WR-01/WR-02 + cert retry (PXF-08) + monitor broadcast (PXF-10) + BulkExport essay cell (PXF-09)"
  - phase: 387-02
    provides: "SubmitExam MC presence-guard (PXF-12) + Hub.SaveTextAnswer timer guard (PXF-13)"
  - phase: 387-03
    provides: "Results.cshtml + ExamSummary.cshtml per-letter AriaContext (PXF-11)"
provides:
  - "xUnit data-level regression tests (disposable real-SQL fixture, [Trait Category=Integration]) for PXF-06 guard + PXF-09 essay cell + PXF-12 no-null-overwrite — 8/8 PASS"
  - "Playwright a11y spec (aria-opsi-387) asserting option-image aria-label letter at runtime on BOTH Results + ExamSummary surfaces (PXF-11) — 3/3 PASS"
  - "Manual browser+SignalR+DB verification of PXF-08 (cert retry/surface), PXF-10 (monitor broadcast), PXF-13 (timer guard) — all PASS"
affects: [387-phase-closure, regression-coverage]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Data-level controller-logic replication in tests: replicate the guard/cell DECISION exactly (sebagaimana controller) against a disposable real-SQL DB, bukan instantiate controller berat"
    - "Disposable HcPortalDB_Test_{guid} fixture (IAsyncLifetime, MigrateAsync→EnsureDeletedAsync) — HcPortalDB_Dev tak pernah disentuh"
    - "Playwright runtime a11y assertion (D-09, lesson Phase 354): aria-label letter pada Razor dinamis WAJIB di-assert runtime, grep+build INSUFFICIENT"

key-files:
  created:
    - "HcPortal.Tests/PostLisensorPolishTests.cs - 8 xUnit Integration facts (PXF-06/09/12 positive+negative)"
    - "tests/e2e/aria-opsi-387.spec.ts - Playwright a11y spec (PXF-11, 2 surfaces)"
  modified:
    - "docs/SEED_JOURNAL.md - entry browser UAT PXF-08/10/13 (temporary+local-only, CLEANED)"

key-decisions:
  - "PXF-06 unit di-redirect mengikuti redirect 387-01: fact menguji guard status (Completed reject / PendingGrading allow) — selaras must_have PXF-06, bukan WR-01/WR-02 (yang sudah ter-cover build+grep di 387-01)."
  - "Tiga LOW item tanpa controller/hub harness (PXF-08/10/13) diverifikasi MANUAL via browser+SignalR+DB per D-09 — bukan automated, sesuai desain checkpoint plan."
  - "Semua mutasi DB browser UAT di-snapshot→RESTORE (C:\\Temp .bak) per CLAUDE.md Seed Workflow; SEED_JOURNAL ditandai CLEANED, 0 residue."

patterns-established:
  - "Proportional verification (D-09): unit untuk logic-bearing fixes, Playwright untuk runtime a11y render, manual untuk SignalR/collision/timer LOW items tanpa harness."

requirements-completed: [PXF-06, PXF-08, PXF-09, PXF-10, PXF-11, PXF-12, PXF-13]

# Metrics
duration: ~25min (Tasks 1-2 + checkpoint approval)
completed: 2026-06-16
---

# Phase 387 Plan 04: Post-Lisensor Polish Verification Summary

**Proportional verification (D-09) untuk Phase 387: 8 xUnit Integration facts (disposable real-SQL) untuk PXF-06/09/12 + 1 Playwright a11y spec PXF-11 (2 surface) + manual browser/SignalR/DB sign-off PXF-08/10/13 — semua PASS, fast suite 347/347 GREEN, build 0 error, 0 migration.**

## Performance

- **Duration:** ~25 min (Tasks 1-2 automated + checkpoint browser approval)
- **Completed:** 2026-06-16
- **Tasks:** 3 (2 auto + 1 human-verify checkpoint)
- **Files created:** 2 (PostLisensorPolishTests.cs + aria-opsi-387.spec.ts)
- **Files modified:** 1 (docs/SEED_JOURNAL.md — cleaned entry)

## Accomplishments

- **Task 1 (xUnit PXF-06/09/12):** `HcPortal.Tests/PostLisensorPolishTests.cs` baru — disposable `HcPortalDB_Test_{guid}` fixture (`IAsyncLifetime`, `[Trait("Category","Integration")]`, `HcPortalDB_Dev` tak disentuh). 8 facts cover positive+negative tiap REQ: PXF-06 guard status (Completed reject / PendingGrading allow EssayScore update), PXF-09 essay cell (graded→"Skor: X/Y", blank→"Tidak dijawab", null score→"Belum dinilai"), PXF-12 MC upsert (absent question→PackageOptionId UNCHANGED, present→updates). **8/8 PASS.**
- **Task 2 (Playwright PXF-11):** `tests/e2e/aria-opsi-387.spec.ts` baru — assert option-image `aria-label` berisi huruf opsi ("opsi A") di RUNTIME pada KEDUA surface review (Results + ExamSummary). **3/3 PASS** (`--workers=1`). Membuktikan a11y render Razor dinamis (D-09, lesson Phase 354 — grep+build insufficient).
- **Task 3 (Checkpoint human-verify — APPROVED):** PXF-08/10/13 (LOW items tanpa controller/hub harness) diverifikasi manual via browser+SignalR+DB. Semua 3 PASS (detail di bawah).

## Task Commits

1. **Task 1: unit tests PXF-06/09/12 (disposable real-SQL fixture)** — `46bd422d` (test)
2. **Task 2: Playwright a11y PXF-11 aria opsi huruf (Results + ExamSummary)** — `3b4db3a2` (test)
3. **Task 3:** checkpoint human-verify (no code commit) — verifikasi manual, approved.

## Browser / SignalR / DB Verification (Task 3 — PXF-08/10/13)

Dilakukan di localhost:5277 (AD-off, shared-memory SQL). Seed Workflow CLAUDE.md dihormati: snapshot DB → mutasi → RESTORE (`C:\Temp\...bak`), `docs/SEED_JOURNAL.md` ditandai CLEANED, 0 residue.

1. **PXF-08 (cert retry/log/surface) — PASS:** Finalize sesi 169 "TEST E2E Campur 2026-06-15" (essay graded 10/10, `GenerateCertificate=1`, `IsPassed`) via tombol "Selesaikan Penilaian" EssayGrading → `NomorSertifikat` ter-assign = **"KPB/005/VI/2026"** (retry-loop generate + persist nomor cert), session→Completed, tidak ada `certError` saat sukses. Dikonfirmasi DB pada dua finalize terpisah.
2. **PXF-10 (monitor broadcast) — PASS:** Klien SignalR live ke `/hubs/assessment`, `JoinMonitor` dengan batchKey tepat "TEST E2E Campur 2026-06-15|OJT|2026-06-15", lalu finalize → grup monitor MENERIMA event `workerSubmitted` live: `{sessionId:169, workerName:"Admin KPB", score:100, result:"Pass", status:"Completed"}` — tab monitor update tanpa refresh. (Percobaan pertama meleset hanya karena Title assessment mengandung suffix tanggal; re-join group key tepat → tertangkap bersih.)
3. **PXF-13 (timer-expiry guard) — PASS:** A/B test invoke `SaveTextAnswer` pada hub terhadap sesi admin-owned: (A) StartedAt=2020 + Duration=1min (EXPIRED) → tulis DITOLAK, essay `TextAnswer` TAK BERUBAH; (B) StartedAt=now + Duration=60min (NOT expired) → tulis SUKSES (`TextAnswer` ter-update). Membuktikan timer guard menolak tulisan pasca-expiry dan mengizinkan yang valid (mirror verbatim `SaveMultipleAnswer`).

Semua mutasi DB di-revert via RESTORE dari `C:\Temp` backup; sesi 169 + responses kembali ke kondisi awal; SEED_JOURNAL CLEANED.

## Files Created/Modified

- `HcPortal.Tests/PostLisensorPolishTests.cs` (new) — 8 facts, `PostLisensorPolishFixture : IAsyncLifetime` (disposable `HcPortalDB_Test_{guid}` @ `localhost\SQLEXPRESS`), `[Trait("Category","Integration")]`, `using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;`. Logika guard/cell direplikasi di data-level PERSIS seperti controller.
- `tests/e2e/aria-opsi-387.spec.ts` (new) — assert aria-label "opsi A" runtime di Results + ExamSummary; `--workers=1`.
- `docs/SEED_JOURNAL.md` — entry browser UAT PXF-08/10/13 (temporary+local-only), ditandai CLEANED setelah RESTORE.

## Decisions Made

- **PXF-06 fact = guard status**, menyelaraskan dengan redirect 387-01 (WR-01/WR-02 sudah ter-cover build+grep di 387-01; fact unit menguji branch guard status Completed/PendingGrading sesuai must_have PXF-06 plan).
- **PXF-08/10/13 manual, bukan automated** — D-09 proportional: tak ada controller/hub test harness untuk SignalR broadcast/cert collision/timer; checkpoint human-verify = jalur kanonik.
- **DB browser UAT snapshot→RESTORE** — CLAUDE.md Seed Workflow; tak ada seed temporary nempel.

## Deviations from Plan

None — plan dieksekusi sesuai tulisan. Task 1 (xUnit) + Task 2 (Playwright) selesai automated; Task 3 checkpoint human-verify di-approve setelah ketiga manual check PASS. Tidak ada Rule 1-4 deviation.

## Known Stubs

None — semua test assert behavior nyata terhadap data real-SQL (PXF-06/09/12) atau DOM Razor runtime (PXF-11). Tidak ada placeholder/mock data yang mengaliri assertion.

## TDD Gate Compliance

Task 1 ditandai `tdd="true"` dengan instruksi menulis GREEN terhadap fix yang sudah ter-apply (Plans 01-03 sudah shipped). Ini adalah pola verification-after-fix (Wave 2 depends_on 01/02/03), bukan RED→GREEN cycle penuh — by design plan: "write them GREEN against the already-applied fixes; if a test is RED, the corresponding fix is incomplete." Semua 8 facts langsung GREEN → konfirmasi fix Plans 01-03 lengkap. Commit `test(387-04)` (`46bd422d`, `3b4db3a2`) hadir untuk kedua test file.

## Verification Summary

- `dotnet test HcPortal.Tests --filter "FullyQualifiedName~PostLisensorPolish"` → 8/8 PASS.
- `npx playwright test e2e/aria-opsi-387.spec.ts --workers=1` → 3/3 PASS (Results + ExamSummary).
- Fast suite `dotnet test --filter "Category!=Integration"` → 347/347 GREEN (no regression).
- `dotnet build` → 0 error.
- Checkpoint human-verify PXF-08/10/13 → APPROVED (3/3 manual PASS).
- 0 migration. `HcPortalDB_Dev` untouched.

## Phase 387 Closure

Plan 04 menutup Phase 387 (Post-Lisensor Assessment Polish). 7 REQ closed: **PXF-06/08/09/10** (387-01, `AssessmentAdminController.cs`) + **PXF-12/13** (387-02, `CMPController.cs` + `AssessmentHub.cs`) + **PXF-11** (387-03, `Results.cshtml` + `ExamSummary.cshtml`), semua terverifikasi 387-04 (unit/Playwright/manual). 0 migration.

**Handoff (CLAUDE.md Develop Workflow):** Phase 387 = deploy IT KEDUA (pasca-acara), terpisah dari bundle urgent 385+386. Sisa: gabung → push `origin/ITHandoff` (BUKAN sekarang — keputusan developer) → notify IT (commit hash HEAD + flag migration = FALSE). ❌ JANGAN edit kode/DB Dev/Prod.

## User Setup Required

None — no external service configuration required.

## Self-Check: PASSED

- FOUND: `HcPortal.Tests/PostLisensorPolishTests.cs`
- FOUND: `tests/e2e/aria-opsi-387.spec.ts`
- FOUND: `.planning/phases/387-post-lisensor-assessment-polish/387-04-SUMMARY.md`
- FOUND commit `46bd422d` (Task 1 unit tests)
- FOUND commit `3b4db3a2` (Task 2 Playwright)

---
*Phase: 387-post-lisensor-assessment-polish*
*Completed: 2026-06-16*
