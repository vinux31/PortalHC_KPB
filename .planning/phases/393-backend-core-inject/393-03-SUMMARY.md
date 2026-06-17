---
phase: 393-backend-core-inject
plan: 03
subsystem: testing
tags: [assessment, grading, inject, test, xunit, integration]

requires:
  - phase: 393-backend-core-inject (plan 01)
    provides: fixture InjectAssessmentFixture + NewInjectService + SeedUserAsync + 5 stub fact
  - phase: 393-backend-core-inject (plan 02)
    provides: InjectBatchAsync penuh (orkestrasi) yang di-exercise test
provides:
  - "5 fact xUnit Integration ber-assertion nyata (SC1..SC5) mengunci 5 Success Criteria phase 393"
  - "Nyquist proof: byte-identik vs Compute, atomic reject-all, essay→Completed, audit count, cert policy"
  - "0-migration ter-konfirmasi (ef migrations add _verify → empty Up/Down → removed)"
affects: [394-controller-ui-inject, 395-autogen, 396-excel-import, 397-prepost-link]

tech-stack:
  added: []
  patterns:
    - "Read-after-commit lewat context BARU + byte-identik assertion vs AssessmentScoreAggregator.Compute (sumber kebenaran sama dengan online)"
    - "Scope semua query ke sessionId/title batch (caveat shared-DB EssayFinalizeRecompute) — hindari FirstAsync global"

key-files:
  created: []
  modified:
    - HcPortal.Tests/InjectAssessmentServiceTests.cs

key-decisions:
  - "ET assertion SC1: Essay di-SKIP dari ET scoring oleh GradeAndCompleteAsync (etCorrect=0, etTotal tetap hitung) → assert ET-C = 0/1 (BUKAN 1/1 seperti deskripsi loose plan). Byte-identik = assert apa yang mesin produksi"
  - "SC2b rollback: pakai mekanisme cert manual collision lintas-batch (batch A commit cert, batch B pakai nomor sama → pre-flight D-09 reject) — deterministik; pre-flight mencegah collision sampai DB write, 0 parsial terbukti"
  - "6 fact total (SC1 + SC1-negatif + SC3 + SC2 + SC5 + SC4); nama semua match filter VALIDATION FullyQualifiedName~Inject*"

patterns-established:
  - "Variabel hasil inject dinamai `result` (reuse) agar assertion result.Rejected/result.Success konsisten + greppable"

requirements-completed: [INJ-01, INJ-02]

duration: 22 min
completed: 2026-06-17
---

# Phase 393 Plan 03: Backend Core Inject — Integration Assertions (SC1..SC5) Summary

**5 fact xUnit Integration ber-assertion nyata yang mengunci 5 Success Criteria phase 393: byte-identik vs `AssessmentScoreAggregator.Compute` (MC/MA/Essay+ET+cert D-12), atomic reject-all + 0 parsial, essay→Completed + backdate preserve, audit count ManualInject==N, cert policy (backdate/suppress/manual/range/date). Full suite 492/492 hijau; 0 migration ter-konfirmasi.**

## Performance

- **Duration:** ~22 min
- **Started:** 2026-06-17
- **Completed:** 2026-06-17
- **Tasks:** 3
- **Files modified:** 1 (`HcPortal.Tests/InjectAssessmentServiceTests.cs`)

## Accomplishments
- **SC1** byte-identik: inject MC/MA/Essay → Score/IsPassed == `Compute` (mesin sama dengan online); ET MC 1/1, MA 1/1, Essay 0/1 (di-skip ET scoring); cert `^KPB/\d{3}/V/2026$` (D-12 backdate). + fact negatif (MA partial=0, MC salah=0)
- **SC2** atomic: NIP invalid → reject-all 0 tulisan; cert manual collision lintas-batch → reject batch B, 0 parsial
- **SC3** essay→Completed: EssayScore=8 → Status=Completed (BUKAN PendingGrading), Score=80, backdate CompletedAt ter-preserve (Pitfall 1)
- **SC4** audit: 3 worker → Count(ManualInject scoped session)==3; skip duplikat → ManualInjectSkipped terpisah, count ManualInject TIDAK bertambah
- **SC5** cert policy: ROMAN III/2024 (D-12), suppress !lulus (D-08), manual collision intra-batch (D-09), EssayScore>ScoreValue (D-07), tanggal masa depan (D-06)
- **Phase gate:** full suite **492/492** (6 inject Integration + baseline 347 + Integration lain); **0 migration** ter-konfirmasi via `ef migrations add _verify` (Up/Down kosong) → removed; build 0 error

## Task Commits

1. **Task 1: SC1 byte-identik + SC1-negatif + SC3 essay→Completed** — `364a370c` (test)
2. **Task 2: SC2 atomic reject-all + SC5 cert/validation policy** — `5ffd7423` (test)
3. **Task 3: SC4 audit count + finalize suite + 0-migration confirm** — `14d98026` (test)

## Files Created/Modified
- `HcPortal.Tests/InjectAssessmentServiceTests.cs` — 5 stub → 6 fact ber-assertion nyata (SC1..SC5 + 1 negatif) + helper `LoadGradedAsync`/`OneMcQuestion`

## Decisions Made
- **ET SC1:** Essay di-SKIP dari ET scoring oleh `GradeAndCompleteAsync` (etCorrect=0, etTotal=Count() tetap hitung essay) → assert ET-C = **0/1** (bukan 1/1 yang loose disebut plan). Byte-identik = assert nilai aktual mesin, BUKAN ekspektasi naratif.
- **SC2b rollback deterministik:** cert manual collision lintas-batch (pre-flight D-09 reject) — sesuai opsi plan; pre-flight mencegah write parsial sampai DB, 0 parsial terbukti via CountAsync(title)==0.
- Variabel `result` di-reuse agar assertion greppable + konsisten.

## Deviations from Plan

None - plan executed exactly as written.

**Catatan (dalam batas plan):**
- SC1 ET-C assertion = 0/1 (essay skip ET scoring) bukan 1/1 — koreksi atas deskripsi loose plan, sesuai perilaku aktual `GradeAndCompleteAsync` (byte-identik principle).
- 2 komentar header/section Plan 01 ("5 STUB fact") di-reword (hapus literal STUB) agar acceptance `STUB 0×` terpenuhi — tanpa ubah logika.

## Issues Encountered
- `ef migrations remove` me-regenerate `ApplicationDbContextModelSnapshot.cs` dengan format `ToTable("X", (string)null)` (noise serialisasi EF tooling, BUKAN model diff — 33 baris). Di-restore via `git checkout` ke versi committed. Up()/Down() migration `_verify` kosong = bukti 0 model diff. Snapshot bersih, no migration artifact ter-commit.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- **Phase 393 SELESAI** (3/3 plan): backend core inject lengkap + 5 SC terbukti otomatis. Siap dikonsumsi Phase 394 (controller/UI), 395 (auto-gen), 396 (Excel), 397 (link Pre/Post) — semua di atas `InjectAssessmentService` yang sama.
- 0 migration → handoff IT flag `migration=FALSE`.
- Verifikasi lokal lengkap (build + full test 492/492 + 0-migration); Integration butuh SQLEXPRESS lokal (precedent ada).

---
*Phase: 393-backend-core-inject*
*Completed: 2026-06-17*
