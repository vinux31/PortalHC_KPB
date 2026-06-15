---
phase: 384-monitoring-essay-grading-ui-refactor-fase-2
plan: 03
subsystem: ui
tags: [razor, monitoring, essay-grading, worker-list, refactor]

requires:
  - phase: 384-monitoring-essay-grading-ui-refactor-fase-2
    provides: "GET /Admin/EssayGrading action + page per-worker (Plan 02)"
provides:
  - "Tabel worker-list ringkas (ganti blok essay inline) di AssessmentMonitoringDetail.cshtml"
  - "Tombol Tinjau Essay -> /Admin/EssayGrading dengan 4 nav param"
affects: [384-04]

tech-stack:
  added: []
  patterns:
    - "Worker-list table (badge 3-state) mirror pola tabel sesi existing (table table-hover align-middle)"

key-files:
  created: []
  modified:
    - Views/Admin/AssessmentMonitoringDetail.cshtml

key-decisions:
  - "showAlert helper DIHAPUS (grep-confirmed hanya dipakai 2 handler essay) bersama 2 handler dead; addExtraTime + handler lain di script block yang sama DIPERTAHANKAN (surgical removal, bukan hapus seluruh <script>)"
  - "Task 1+2 di-commit bersama (1 file AssessmentMonitoringDetail.cshtml, 1 refactor logis: ganti blok + hapus handler dead)"

patterns-established:
  - "badge 3-state D-04: bg-warning text-dark '{N} belum dinilai' / bg-info 'Siap difinalisasi' / bg-success 'Selesai' (reuse gate Phase 310 D-02)"

requirements-completed: [UIG-01]

duration: ~20 min
completed: 2026-06-15
---

# Phase 384 Plan 03: Tabel Worker-List Monitoring Summary

**Blok essay inline (stacked cards per-worker) diganti tabel worker-list ringkas 4-kolom dengan badge status 3-state + tombol "Tinjau Essay" → page per-worker; handler AJAX essay dead dihapus.**

## Performance

- **Duration:** ~20 min
- **Completed:** 2026-06-15
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- Blok `:387-480` (stacked `essay-grading-card` per-worker) → tabel `table table-hover align-middle`: kolom Worker+NIP, Essay Belum Dinilai, badge 3-state (D-04), tombol "Tinjau Essay". Filter `HasManualGrading`, urut `UserNIP`. Guard `essayGradingMap.Any()` + `antiforgeryForm` dipertahankan.
- Tombol "Tinjau Essay" → `@Url.Action("EssayGrading", new { sessionId, title, category, scheduleDate, assessmentType })` (Plan 02). Tetap muncul untuk worker finalized (D-10 buka read-only).
- Handler AJAX essay dead (`.btn-save-essay-score` + `.btn-finalize-grading` + `showAlert`) dihapus surgical dari `@section Scripts`; `addExtraTime` + handler lain di script block sama tetap utuh.

## Task Commits

1. **Task 1: Ganti blok essay inline → tabel worker-list** + **Task 2: Hapus handler AJAX essay dead** - `98b7a1a1` (feat) — 1 file, di-commit bersama

## Files Created/Modified
- `Views/Admin/AssessmentMonitoringDetail.cshtml` - blok essay inline → tabel worker-list; handler essay dead dihapus

## Decisions Made
- Lihat `key-decisions` frontmatter.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None. `dotnet build` 0 error; grep gate: `btn-save-essay-score`/`btn-finalize-grading`/`essay-grading-card` = **0 match**; `Tinjau Essay` + `Url.Action("EssayGrading"` + `antiforgeryForm` + `addExtraTime` ada. (Catatan: script block essay-handlers ternyata juga memuat `addExtraTime` non-essay → hapus surgical, bukan hapus seluruh `<script>`.)

## Next Phase Readiness
- UI lengkap (tabel monitoring + page per-worker). Plan 04: hapus `test.fixme`, jalankan e2e FLOW 384 hijau dengan app lokal + UAT manual (checkpoint blocking).

---
*Phase: 384-monitoring-essay-grading-ui-refactor-fase-2*
*Completed: 2026-06-15*
