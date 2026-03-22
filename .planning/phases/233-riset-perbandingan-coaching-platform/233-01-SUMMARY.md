---
phase: 233-riset-perbandingan-coaching-platform
plan: 01
subsystem: documentation
tags: [coaching, research, html, proton, gap-analysis, 360learning, betterup, coachhub]

# Dependency graph
requires: []
provides:
  - "docs/coaching-platform-research-v8.2.html — dokumen riset HTML lengkap dengan 7 section"
  - "Gap analysis per 4 area Proton (Setup, Execution, Monitoring, Completion) vs 3 platform enterprise"
  - "20 rekomendasi 3-tier (Must-fix/Should-improve/Nice-to-have) di-map ke Phase 234-237"
  - "Validasi DIFF-01/02/03 sebagai best practice enterprise yang ada di BetterUp/CoachHub"
affects:
  - 234-audit-setup-flow
  - 235-audit-execution-flow
  - 236-audit-monitoring-dashboard
  - 237-audit-completion-differentiator

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Self-contained HTML dokumen riset dengan sidebar sticky navigation dan anchor links"
    - "Badge tier (badge-critical/badge-medium/badge-low) dan badge-blue untuk target phase"
    - "Gap tabel per area dengan kolom: Aspek | Portal KPB | Best Practice | Gap | Tier | Target Phase"

key-files:
  created:
    - docs/coaching-platform-research-v8.2.html
  modified: []

key-decisions:
  - "3 platform yang diriset: 360Learning, BetterUp, CoachHub — tidak ada substitusi (D-01/D-02)"
  - "Struktur per area Proton (Setup/Execution/Monitoring/Completion) paralel dengan Phase 234-237 (D-03)"
  - "Perbandingan posisikan sebagai pendekatan berbeda untuk tujuan sama, bukan fitur identik"
  - "Differentiator DIFF-01/02/03 divalidasi sebagai best practice — bukan over-engineering"

patterns-established:
  - "Dokumen riset fase: 1 HTML self-contained di docs/ dengan sidebar + section per area + gap tabel"
  - "Rekomendasi selalu memiliki tier + target phase + ref requirement ID untuk traceability"

requirements-completed: [RSCH-01, RSCH-02, RSCH-03]

# Metrics
duration: 8min
completed: 2026-03-22
---

# Phase 233 Plan 01: Riset Perbandingan Coaching Platform Summary

**Dokumen HTML riset perbandingan 360Learning vs BetterUp vs CoachHub vs Portal KPB per 4 area Proton dengan 20 rekomendasi 3-tier di-map ke Phase 234-237 dan validasi DIFF-01/02/03 sebagai best practice enterprise**

## Performance

- **Duration:** 8 min
- **Started:** 2026-03-22T12:55:53Z
- **Completed:** 2026-03-22T13:04:00Z
- **Tasks:** 2 of 2 (Task 1: buat HTML — commit f2af2fd; Task 2: verifikasi browser — disetujui user)
- **Files modified:** 1

## Accomplishments
- Dokumen HTML self-contained `docs/coaching-platform-research-v8.2.html` dengan 7 section lengkap
- Profil dan flow UX per area dari 360Learning, BetterUp, CoachHub berdasarkan dokumentasi resmi
- Gap analysis konkret per 4 area Proton: 13 Must-fix, 5 Should-improve, 2 Nice-to-have
- Validasi DIFF-01 (BetterUp workload visibility), DIFF-02 (CoachHub+360Learning bulk action), DIFF-03 (CoachHub Insights bottleneck)
- Tabel rekomendasi 20 item dengan mapping eksplisit ke Phase 234 (Setup), 235 (Execution), 236 (Monitoring), 237 (Completion+Differentiator)

## Task Commits

1. **Task 1: Buat dokumen HTML riset perbandingan coaching platform** - `f2af2fd` (feat)
2. **Task 2: Verifikasi dokumen riset di browser** - Disetujui user (checkpoint:human-verify, no commit)

## Files Created/Modified
- `docs/coaching-platform-research-v8.2.html` — dokumen riset HTML 916 baris, 7 section, sidebar navigasi, gap tabel per area dengan badge tier dan phase

## Decisions Made
- Tidak menggunakan screenshot aktual platform (perlu registrasi enterprise) — narasi step-by-step berdasarkan dokumentasi resmi sebagai pengganti; sesuai Pitfall 1 dari RESEARCH.md
- Rekomendasi diurutkan Must-fix → Should-improve → Nice-to-have dalam satu tabel, bukan tabel terpisah per tier, untuk kemudahan scanning planner Phase 234-237

## Deviations from Plan

### Scope Expansion (Post-Approval Discovery)

**1. Codebase audit menemukan 24 bug tambahan di luar 20 rekomendasi riset**

- **Found during:** Setelah Task 2 disetujui user
- **Issue:** Dokumen riset menghasilkan 20 rekomendasi berbasis gap analysis platform comparison. Audit kode sumber lebih dalam menemukan 24 bug konkret yang tidak teridentifikasi hanya dari perbandingan fitur level UX.
- **Fix:** Scope Phase 234-237 direvisi untuk mencakup kedua set item:
  - Phase 234 (Setup): 12 item = 4 High bug + 4 Medium + 4 Low
  - Phase 235 (Execution): 10 item = 3 High bug + 4 Medium + 3 Low
  - Phase 236 (Monitoring): 9 item = 2 High bug + 4 Medium + 3 Low
  - Phase 237 (Completion): 6 item = 1 High fix + 2 Medium + 3 evaluasi
- **Total revised scope:** 37 item (20 rekomendasi riset + 24 bug + 3 evaluasi, beberapa overlap)
- **Impact:** Dokumen riset v8.2.html tetap valid dan disetujui. Scope expansion adalah informasi tambahan untuk planning Phase 234-237.

---

**Total deviations:** 1 scope expansion (post-approval, tidak mengubah output Plan 01)
**Impact on plan:** Output Plan 01 tidak berubah. Scope Phase 234-237 diperluas secara signifikan.

## Issues Encountered
None.

## User Setup Required
None — output adalah file HTML statis, tidak ada konfigurasi service eksternal.

## Next Phase Readiness

- `docs/coaching-platform-research-v8.2.html` telah disetujui user — siap digunakan sebagai lens Phase 234-237
- Phase 234 (Setup): 12 item — baca section #setup dan R-01/R-02/R-03/R-14
- Phase 235 (Execution): 10 item — baca section #execution dan R-04 s/d R-07; termasuk tech debt v4.0 (evidence download auth, sequential lock server-side, ExportProgressExcel role attr)
- Phase 236 (Monitoring): 9 item — baca section #monitoring dan R-08/R-16/R-20
- Phase 237 (Completion + Differentiators): 6 item — baca section #completion + #differentiators dan R-09 s/d R-13/R-17/R-18; Workload indicator (DIFF-01), Batch approval HC (DIFF-02), Bottleneck analysis (DIFF-03)

---
*Phase: 233-riset-perbandingan-coaching-platform*
*Completed: 2026-03-22*
