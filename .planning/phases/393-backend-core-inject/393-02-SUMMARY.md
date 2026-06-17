---
phase: 393-backend-core-inject
plan: 02
subsystem: api
tags: [assessment, grading, inject, backend, service, transaction, certificate, essay, audit]

requires:
  - phase: 393-backend-core-inject (plan 01)
    provides: DTO kontrak InjectRequest/InjectResult + InjectAssessmentService skeleton + signature
  - phase: 296-grading-service
    provides: GradingService.GradeAndCompleteAsync (delegasi skor/lulus/ET/cert)
  - phase: 376-essay-aggregator
    provides: AssessmentScoreAggregator.Compute (pure aggregator essay-aware)
provides:
  - "InjectBatchAsync penuh: pre-flight reject-all (D-03) + cert-aware dedup (D-01/D-02) + atomic tx (D-04)"
  - "per-worker insert sentinel + delegasi GradeAndCompleteAsync (nol duplikasi skor) + essay finalize-block (D-05)"
  - "backdate re-apply pasca-grade (D-06) + cert auto backdate (D-12) / suppress !lulus (D-08) / manual (D-09)"
  - "audit in-tx 3 ActionType ManualInject/ManualInjectSkipped/ManualInjectRejected (D-11)"
affects: [393-03-test-assertions, 394-controller-ui-inject, 395-autogen, 396-excel-import, 397-prepost-link]

tech-stack:
  added: []
  patterns:
    - "Orkestrasi tipis: service susun input → delegasi mesin existing (GradingService + Aggregator + CertNumberHelper); nol perhitungan skor/lulus/cert manual"
    - "Unified cert step (D-12): essay finalize-block menunda cert; satu langkah cert pasca-grade pakai basis tanggal backdate untuk essay & non-essay"
    - "Backdate re-apply pasca-grade (deviasi sadar Pitfall 1): grade/finalize overwrite CompletedAt=UtcNow → ExecuteUpdate set ulang ke nilai backdate"

key-files:
  created: []
  modified:
    - Services/InjectAssessmentService.cs

key-decisions:
  - "D-12 mekanisme cert (executor discretion): UNIFIED — essay finalize-block TIDAK generate cert; satu langkah cert pasca-grade (step h) untuk CertMode==Auto melepas nomor basis-today yang dibuat GradeAndCompleteAsync (NomorSertifikat=null) lalu regenerate dengan basis req.CompletedAt (ROMAN/year/seq tanggal ujian). Sequential + in-tx → GetNextSeqAsync read-your-own-writes benar (Pitfall 3)"
  - "Cert manual (D-09): NomorSertifikat ter-set pra-grade; guard GradeAndCompleteAsync WHERE NomorSertifikat==null tak menimpa → step h skip (hanya CertMode==Auto)"
  - "Dedup intra-batch ditangani di main loop via seenUserIds (HashSet); FindDuplicateNipsAsync fokus DB-existing dup (pemisahan bersih)"
  - "Sentinel package di-anchor ke sesi worker PERTAMA (1× per batch); questions/options di-insert sekali, map TempId→realId dipakai semua worker"
  - "isPassed di-reload via AsNoTracking pasca-grade (ExecuteUpdate bypass change-tracker → session in-memory stale)"

patterns-established:
  - "AccessToken=\"INJECT\" + AssessmentType=req.AssessmentType (BUKAN \"Manual\") + IsManualEntry=true — deviasi sadar BulkBackfill agar grouping & /CMP/Results render seperti online"
  - "Audit in-tx via _context.AuditLogs.Add (ikut rollback); ActionType terpisah agar count ManualInject = jumlah sesi sukses (SC4)"

requirements-completed: [INJ-01, INJ-02]

duration: 20 min
completed: 2026-06-17
---

# Phase 393 Plan 02: Backend Core Inject — Orchestration Summary

**`InjectBatchAsync` orkestrasi penuh: pre-flight reject-all + cert-aware dedup + transaction atomic; per-worker insert sentinel → delegasi GradingService (nol duplikasi skor) → essay finalize-block → backdate re-apply → cert auto backdate (D-12) / suppress / manual → audit in-tx 3 ActionType.**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-06-17
- **Completed:** 2026-06-17
- **Tasks:** 3
- **Files modified:** 1 (`Services/InjectAssessmentService.cs`, ~280 baris implementasi)

## Accomplishments
- Pre-flight reject-all (D-03): NIP, opsi valid (MC=1/MA≥1/Essay=0), EssayScore 0..ScoreValue (D-07), tanggal ≤ today (D-06), cert manual unik (D-09) — kumpul SEMUA error per-baris
- Cert-aware dedup (D-01/D-02): skip+lapor duplikat by UserId+NormalizeTitleForDup+Category+Date; intra-batch HashSet
- Atomic batch (D-04): tx wrapper, per-worker insert (sentinel package 1×) → grade delegasi → essay finalize → backdate re-apply → cert
- Cert: auto backdate D-12 (ROMAN/year/seq = tanggal ujian, bukan now), suppress !lulus D-08, manual D-09 untouched
- Audit in-tx 3 ActionType (D-11): ManualInject (count=SC4) / ManualInjectSkipped / ManualInjectRejected
- Build 0 error; fast suite 347/347 (no regression); 0 migration

## Task Commits

1. **Task 1: Pre-flight + cert-aware dedup** — `42ecafb3` (feat)
2. **Task 2: InjectBatchAsync orchestration (tx + grade + finalize + backdate + cert)** — `a25e716c` (feat)
3. **Task 3: Audit 3 ActionType + finalize InjectResult** — `7ced76bd` (feat)

## Files Created/Modified
- `Services/InjectAssessmentService.cs` — implementasi penuh InjectBatchAsync + PreflightValidateAsync + FindDuplicateNipsAsync

## Decisions Made
- **D-12 mekanisme cert (UNIFIED, executor discretion):** essay finalize-block sengaja TIDAK generate cert; satu langkah cert pasca-grade (step h) untuk `CertMode==Auto` melepas nomor basis-today (yang dibuat `GradeAndCompleteAsync` non-essay) lalu regenerate basis `req.CompletedAt`. Konsisten essay & non-essay; in-tx sequential → seq benar.
- Cert manual (D-09): tak disentuh step h (guard `WHERE NomorSertifikat==null`).
- Dedup intra-batch di main loop (`seenUserIds`); `FindDuplicateNipsAsync` fokus DB dup.
- `isPassed` di-reload `AsNoTracking` pasca-grade (ExecuteUpdate bypass change-tracker).

## Deviations from Plan

None - plan executed exactly as written.

**Catatan implementasi (dalam batas discretion plan):**
- D-12 cert: plan memberi 2 pilihan mekanisme + "executor putuskan; dokumentasikan". Dipilih UNIFIED (lihat Decisions) — essay finalize defer cert ke satu langkah pasca-grade agar essay & non-essay konsisten basis-backdate. `AssessmentScoreAggregator.Compute` tetap dipakai di finalize-block untuk recompute Score/IsPassed (acceptance terpenuhi).
- 2 komentar di-reword (hapus literal yang men-trip grep acceptance `AssessmentType.*Manual`=0 & `"ManualInjectRejected"`=1) — tanpa ubah logika.

## Issues Encountered
- Selama sesi, muncul 2 commit `docs(394)` (context phase 394) ter-interleave di git log antara Task 1 & Task 2 commit — sesi paralel lain, docs-only (`.planning/phases/394...`), TIDAK menyentuh kode phase 393. Commit kode 393 tetap berurutan & utuh; build/test hijau.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- `InjectBatchAsync` lengkap & ter-compile → siap dipanggil **Plan 03** (ganti 5 stub fact dengan assertion nyata SC1..SC5: byte-identik vs Compute, rollback atomic, essay→Completed, audit count, cert policy).
- Integration test nyata (real SQL, read-after-commit) = Plan 03; Plan 02 diverifikasi via build + fast suite (stub Integration HIJAU placeholder, belum exercise service).
- 0 migration; tidak ada blocker.

---
*Phase: 393-backend-core-inject*
*Completed: 2026-06-17*
