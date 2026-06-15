---
phase: 382-grading-lifecycle-cert
plan: 01
subsystem: testing
tags: [grading, lifecycle, dedupe, anti-resurrection, race-safety, ExecuteUpdateAsync, xunit]

# Dependency graph
requires:
  - phase: 381-worker-entry
    provides: "Worker entry guards (same-day Pre/Post pool isolation, StartExam GET no-burn) — Wave B prasyarat sebelum grading/lifecycle"
provides:
  - "AssessmentConstants.AssessmentStatus.Abandoned (single-source label, dipakai Plan 02/03)"
  - "GradingService MC scoring membaca jawaban FINAL per soal (finalByQuestion dedupe last-write-wins by SubmittedAt)"
  - "GradeAndCompleteAsync menolak commit Completed-lulus + cert pada sesi terminal (Abandoned/Cancelled/PendingGrading) di KEDUA branch (non-essay + essay)"
  - "2 file test xUnit (GradingDedupeTests, SubmitResurrectionTests) pola real-SQL disposable fixture"
  - "FakeWorkerDataService test helper (reusable lintas test grading)"
affects: [382-02, 382-03, grading, certificate, lifecycle, CMPController]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Dedupe-read in-memory (finalByQuestion) pada list yang sudah ToListAsync — bukan SQL GroupBy(entity) yang tak diterjemahkan EF8"
    - "Status guard ExecuteUpdateAsync WHERE NOT IN (terminal set) + rowsAffected==0 → false (anti-resurrection)"
    - "Real-SQL disposable fixture (HcPortalDB_Test_{guid}) untuk test GradeAndCompleteAsync karena ExecuteUpdateAsync tak didukung EF8 InMemory"

key-files:
  created:
    - HcPortal.Tests/GradingDedupeTests.cs
    - HcPortal.Tests/SubmitResurrectionTests.cs
    - HcPortal.Tests/FakeWorkerDataService.cs
  modified:
    - Models/AssessmentConstants.cs
    - Services/GradingService.cs

key-decisions:
  - "DEVIATION (Rule 3): pakai real-SQL disposable fixture (Category=Integration), BUKAN InMemory seperti tertulis di plan — ExecuteUpdateAsync tidak didukung EF Core 8 InMemory provider"
  - "MC scoring dedupe by SubmittedAt (last-write-wins); MultipleAnswer TIDAK ter-dedupe (multi-row by design)"
  - "STAT-01 guard pakai konstanta S.* (alias AssessmentStatus), bukan literal — carry-forward v22.0 single-source discipline"

patterns-established:
  - "finalByQuestion: dedupe-read jawaban FINAL per soal in-memory sebelum loop scoring"
  - "Anti-resurrection guard: WHERE Status NOT IN (Completed,Abandoned,Cancelled,PendingGrading) pada commit grading"

requirements-completed: [WSE-06, WSE-07]

# Metrics
duration: 15min
completed: 2026-06-14
---

# Phase 382 Plan 01: Grading Dedupe-Read + Anti-Resurrection Guard Summary

**GradingService kini menilai MC dari jawaban FINAL per soal (dedupe last-write-wins by SubmittedAt) dan menolak meng-commit hasil Completed-lulus + sertifikat pada sesi terminal (Abandoned/Cancelled/PendingGrading) di kedua branch grading — divalidasi 4 fact xUnit RED→GREEN.**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-06-14T17:19:53Z
- **Completed:** 2026-06-14T17:35:23Z
- **Tasks:** 3 (TDD: RED → GREEN ×2)
- **Files modified:** 5 (2 production, 3 test)

## Accomplishments
- **SAVE-01 (WSE-06):** `finalByQuestion` dedupe-read (last-write-wins by `SubmittedAt`) menggantikan `FirstOrDefault` tanpa ORDER BY di dua titik MC scoring (loop utama L97 + ET breakdown L152). Score kini selalu dari opsi FINAL — baris duplikat/basi (race multi-tab) tak lagi memengaruhi nilai. MultipleAnswer tetap dibaca penuh (tidak ter-dedupe).
- **STAT-01 (WSE-07):** Guard `ExecuteUpdateAsync` non-essay diperluas dari `!= "Completed"` saja ke `NOT IN (Completed, Abandoned, Cancelled, PendingGrading)`; branch essay menambah `Abandoned/Cancelled`. `rowsAffected==0 → return false` (branch existing di-reuse). Sesi terminal tak bisa di-resurrect jadi Completed-lulus + cert.
- **Const Abandoned** ditambah di `AssessmentConstants.AssessmentStatus` — single-source label untuk Plan 02/03.
- 4 fact xUnit baru membuktikan kedua bug (RED) lalu fix (GREEN); full suite **395/395** tanpa regresi; **migration=false** dikonfirmasi (0 file Migrations/snapshot tersentuh).

## Task Commits

Each task committed atomically (TDD cycle):

1. **Task 1: Wave 0 RED — Abandoned const + 4 facts** - `049c21bf` (test)
2. **Task 2: SAVE-01 dedupe-read FINAL answer (GREEN)** - `2117a323` (fix)
3. **Task 3: STAT-01 guard expand anti-resurrection (GREEN)** - `8dfb596e` (fix)

**Plan metadata:** (final docs commit — lihat git log)

## Files Created/Modified
- `Models/AssessmentConstants.cs` — tambah `public const string Abandoned = "Abandoned";`
- `Services/GradingService.cs` — `finalByQuestion` dedupe-read + ganti 2 FirstOrDefault MC → TryGetValue; guard expand kedua branch (S.* konstanta) + alias `using S`
- `HcPortal.Tests/GradingDedupeTests.cs` — Test A dedupe-final (RED→GREEN) + Test B MA-not-deduped (green)
- `HcPortal.Tests/SubmitResurrectionTests.cs` — Test C abandoned-rejected + Test D cancelled-rejected (RED→GREEN)
- `HcPortal.Tests/FakeWorkerDataService.cs` — fake `IWorkerDataService` (NotifyIfGroupCompleted no-op) untuk instantiate GradingService tanpa UserManager

## Decisions Made
- **MC dedupe by SubmittedAt** (last-write-wins); MultipleAnswer dibaca penuh (multi-row by design, tak ter-dedupe).
- **Anti-resurrection set** = {Completed, Abandoned, Cancelled, PendingGrading} pada kedua branch; PendingGrading tetap diperbolehkan di-set oleh branch essay untuk sesi yang BUKAN terminal.
- **Konstanta S.*** dipakai menggantikan literal `"Completed"` di guard non-essay (v22.0 single-source discipline). Literal di branch lain (`RegradeAfterEditAsync`) di luar scope plan — tidak disentuh.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Test memakai real-SQL disposable fixture, bukan InMemory**
- **Found during:** Task 1 (penulisan Wave-0 facts)
- **Issue:** Plan instruksi eksplisit "InMemory DbContext". Namun `GradeAndCompleteAsync` memakai `ExecuteUpdateAsync`, yang **tidak didukung EF Core 8 InMemory provider** (`InvalidOperationException` saat dipanggil) — test InMemory akan crash sebelum mencapai logika dedupe/guard yang diuji, sehingga tak bisa membuktikan apapun (RED palsu karena exception, bukan assertion).
- **Fix:** Pakai pola disposable real-SQL fixture yang sudah established di repo (`EssayFinalizeRecomputeFixture` / `ProtonCompletionFixture`): `HcPortalDB_Test_{guid}` @`localhost\SQLEXPRESS`, `MigrateAsync` penuh, drop on dispose, `[Trait("Category","Integration")]`. DB lokal `HcPortalDB_Dev` TAK tersentuh — tidak melanggar CLAUDE.md (no edit DB Dev/Prod; ini DB throwaway lokal).
- **Files modified:** GradingDedupeTests.cs, SubmitResurrectionTests.cs (fixture + Trait Integration)
- **Verification:** RED terbukti via assertion sungguhan (Test A: Expected 100/Actual 0; Test C/D: return true→resurrection). Setelah fix GREEN 4/4. Probe SQLEXPRESS lokal tersedia (`Migration_AddsOriginColumn` jalan 1/1).
- **Committed in:** `049c21bf` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking — pemilihan test backend).
**Impact on plan:** Tidak mengubah scope/behavior produksi maupun must-haves. Hanya memilih backend test yang benar agar fact bisa mengeksekusi kode yang diuji. Tujuan plan (RED→GREEN membuktikan SAVE-01 & STAT-01) tercapai penuh. Test tag `Category=Integration` → bisa di-skip via `--filter "Category!=Integration"` di lingkungan SQL-less (CI), konsisten pola test grading existing (Phase 358/376).

## Issues Encountered
- `PackageOption` tidak punya field `Order` (sempat ditulis di seed awal) — dihapus dari seed sebelum build pertama; build pertama langsung 0 error.

## TDD Gate Compliance
Plan ini `type: execute` dengan task-level `tdd="true"`. Gate sequence terpenuhi di git log:
1. RED gate: `test(382-01)` `049c21bf` — 3 fact FAIL (Test A Expected 100/Actual 0; Test C/D return true) + 1 fact green (MA).
2. GREEN gate: `fix(382-01)` `2117a323` (SAVE-01) + `8dfb596e` (STAT-01) — 4/4 fact GREEN.
3. REFACTOR: tidak diperlukan (kode minimal sudah bersih).

## Verification Results
- `dotnet build`: **0 Error** (24 warning pre-existing, out-of-scope).
- `dotnet test HcPortal.Tests --filter "GradingDedupe|SubmitResurrection"`: **4/4 passed** (dedupe-final, MA-not-deduped, abandoned-rejected, cancelled-rejected).
- Full suite `dotnet test HcPortal.Tests`: **395 passed / 0 failed / 0 skipped**.
- `AssessmentConstants.cs` punya `Abandoned` const. ✅
- Migration: **0 file** `Migrations/` atau `*ModelSnapshot.cs` tersentuh across 3 commit (`git diff HEAD~3 HEAD -- Migrations/` kosong). ✅ migration=false.

## User Setup Required
None - no external service configuration required. (Test Integration butuh `localhost\SQLEXPRESS` + SQLBrowser lokal saat run; sudah terverifikasi tersedia di mesin dev.)

## Next Phase Readiness
- `Abandoned` const siap dikonsumsi Plan 02 (CMPController-side guards: SubmitExam early guard, TOK-02, TMR) dan Plan 03 (CERT-01).
- GradingService-side WSE-06/WSE-07 selesai. Sisi CMPController (SubmitExam exclude Abandoned/Cancelled di entry, bukan hanya grading) = Plan 02.
- NOT PUSHED (sesuai DEV_WORKFLOW: verifikasi lokal dulu; push + notify IT setelah fase tuntas). migration=false → tidak ada flag migration baru untuk plan ini.

## Self-Check: PASSED

Semua 6 file diklaim ada (FOUND) + 3 commit hash ada (049c21bf, 2117a323, 8dfb596e).

---
*Phase: 382-grading-lifecycle-cert*
*Completed: 2026-06-14*
