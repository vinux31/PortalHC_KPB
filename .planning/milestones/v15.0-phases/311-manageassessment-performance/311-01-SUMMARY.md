---
plan: 311-01
phase: 311-manageassessment-performance
status: complete
mode: retroactive
completed_at: 2026-05-05
recorded_at: 2026-05-07
commits:
  - sha: a4ce556e
    type: feat
    message: "feat(311-01): add per-segment Stopwatch instrumentation to ManageAssessment (D-11, D-13, D-16)"
superseded_by:
  - "311-02 (HTMX) — D-09 mandates Stopwatch dipindah dari single-action ke per-action context (1 Stopwatch per partial action)"
---

# 311-01 — Per-Segment Stopwatch Baseline (Retroactive Summary)

## Status

**Complete** — committed via `a4ce556e` pada 2026-05-05.

Plan ini dieksekusi pada arah Phase 311 yang **lama** (backend query optimization, measurement-driven gate). Setelah brainstorm 2026-05-07 yang me-reframe Phase 311 ke arsitektur HTMX lazy load (lihat `311-DESIGN.md`), Plan 01 tetap dipertahankan sebagai historical context — instrumentasi yang dibuat akan **dimigrasi** ke per-action context oleh Plan 02 (D-09), bukan dihapus.

## What Was Built

- 5 segment Stopwatch (T1..T5) ditambahkan ke action `ManageAssessment` di `Controllers/AssessmentAdminController.cs`:
  - T1 = Assessment query L66-110 + grouping
  - T2 = `GetWorkersInSection` (L210)
  - T3 = `GetAllWorkersHistory` (L212)
  - T4 = `GetAllSectionsAsync` + `GetUnitsForSectionAsync` (L220-223)
  - T5 = Distinct Categories (L172-176)
- Structured logging via `_logger.LogInformation` dengan template `"ManageAssessment perf breakdown: t1={T1}ms t2={T2}ms t3={T3}ms t4={T4}ms t5={T5}ms total={Total}ms tab={Tab} search_present={SearchPresent} page={Page}"`
- Build hijau, warnings ≤92 (Phase 309 baseline preserved)
- Baseline measurement direncanakan di `311-BASELINE.md` (5x cold runs, median per segment) — proses dipercepat lewat brainstorm session (TTFB measurement Chrome DevTools menjadi datapoint utama, bukan Stopwatch breakdown formal)

## Why It Was Superseded

Baseline measurement membuktikan backend cepat (TTFB 281ms, total 11-27ms warm di Dev DB). User confirmed lag hanya muncul di wifi kantor (>1 menit), tidak di hotspot HP (<10s) atau wifi lain (instan). Diagnosis: bottleneck di proxy Pertamina yang inspect/throttle traffic, bukan di code/server/DB. Strategi optimasi backend (Skenario A/B/C decision gate yang Plan 01 design) tidak relevan untuk masalah network-bound.

**Reframe:** HTMX lazy-load architecture (Opsi 2 dari 6 yang dievaluasi). Lihat `311-DESIGN.md` (approved 2026-05-07).

## Migration Path

D-09 (preserved decision dari CONTEXT revisi) menyatakan instrumentasi Stopwatch **dipindah** dari single-action ke per-partial-action saat Plan 02 jalan:

- 1 Stopwatch per partial action (`ManageAssessmentTab_Assessment` measure T1, `_Training` measure T2+T4, `_History` measure T3)
- Logger format diadaptasi: `"ManageAssessment perf [tab={Tab}]: elapsed={Ms}ms search_present={SearchPresent} page={Page}"`
- Permanent (NOT removed post-validation)

Plan 02 Task 2 menulis ulang struktur ini sebagai bagian dari refactor controller jadi shell + 3 partial actions.

## Files Touched (Historical)

- `Controllers/AssessmentAdminController.cs` — instrumentasi single-action Stopwatch (akan ditulis ulang di Plan 02)
- `.planning/phases/311-manageassessment-performance/311-BASELINE.md` — scaffold (uncommitted, never filled formally — proses cepat lewat brainstorm)

## Notes

Summary ini dibuat retroaktif pada 2026-05-07 untuk men-tandai Plan 01 selesai sehingga `/gsd-execute-phase 311` melompati Plan 01 dan mulai dari Plan 02 Wave 1 (HTMX lazy load).
