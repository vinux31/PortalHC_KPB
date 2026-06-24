---
phase: 417-section-pagination
plan: 02
subsystem: exam-render
tags: [pagination, exam-render, razor, signalr, resume, section]

# Dependency graph
requires:
  - phase: 417-01
    provides: "SectionPaginator.ComputePages/ClampResumePage (fungsi murni) + 6 field section-aware di ExamQuestionItem"
  - phase: 416-scoped-shuffle-acak-per-section
    provides: "Urutan soal section-aware (Section 1→2→…→Lainnya) + .ThenInclude(q.Section) di StartExam"
  - phase: 415-section-foundation-import-excel-diperluas
    provides: "AssessmentPackageSection (SectionNumber/Name/StartNewPage)"
provides:
  - "CMPController.StartExam ter-wire: isi field Section ke ExamQuestionItem + ComputePages(perPage section-aware) + clamp RESUME_PAGE"
  - "Views/CMP/StartExam.cshtml section-aware: render grouping by PageNumber + header Section + (lanjutan) + navigator per-Section + indikator ber-nama-Section + resume RESUME_PAGE + toast info"
affects: [419-export-test-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Single source of truth Razor↔JS: pageQuestionIds/allQuestionsData/pageSectionMap SEMUA dari q.PageNumber (anti-drift Pitfall 1)"
    - "Backward-compat branch hasSections — no-Section render byte-identik (header absen, navigator flat, indikator tanpa label)"
    - "Section label navigator full-width via gridColumn 1/-1 (tak konsumsi sel grid 7-kolom)"
    - "XSS-safe nama Section: Razor @ auto-encode (header) + JS textContent/createTextNode (navigator/indikator/toast)"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/StartExam.cshtml

key-decisions:
  - "Indikator halaman ditempatkan di navigator panel header (desktop) + atas mobile footer (single-instance per surface, bukan per-page yang akan dobel ID)"
  - "Resume handler pakai variabel antara targetPage (guard typeof RESUME_PAGE + range) lalu set currentPage — hindari hardcode 0, sembunyikan page_0 bila resume >0"
  - "mobile UA detection (perPage 5/10) dipindahkan SEBELUM ComputePages di dalam package-path (perPage dibutuhkan saat compute); blok lama dihapus anti double-set"

requirements-completed: [PAG-01, PAG-02, PAG-03]

# Metrics
duration: 9min
completed: 2026-06-24
---

# Phase 417 Plan 02: Section Pagination Controller + View Wiring Summary

**Wiring `SectionPaginator.ComputePages` ke `CMPController.StartExam` + generalisasi `StartExam.cshtml` dari pagination flat ke section-aware (header Section + "(lanjutan)" auto-split + navigator per-Section + indikator ber-nama-Section + resume RESUME_PAGE + toast info), SEMUA dengan invariant no-Section byte-identik via branch `hasSections`.**

## Performance

- **Duration:** ~9 min
- **Started:** 2026-06-24T00:05:06Z
- **Completed:** 2026-06-24T00:14:32Z
- **Tasks:** 3
- **Files modified:** 2

## Accomplishments
- **CMPController.StartExam** (PAG-01/02/03): isi `SectionNumber`/`SectionName`/`SectionStartNewPage` ke `ExamQuestionItem` dari `q.Section` (sudah ter-Include di :1054); pindahkan deteksi mobile UA (perPage 5/10) ke SEBELUM compute lalu panggil `SectionPaginator.ComputePages(examQuestions, questionsPerPage)`; clamp `RESUME_PAGE` via `ClampResumePage(LastActivePage, maxPage)` (D-417-05 fallback page 0). Blok mobile UA lama dihapus (anti double-set `ViewBag.QuestionsPerPage`).
- **StartExam.cshtml render** (PAG-01/02): `totalPages` dari `pageGroups.Count` (Pitfall 2, bukan Ceiling); loop render `exam-page` by `GroupBy(q.PageNumber)` (bukan Skip/Take naif); header Section text-only (`text-primary fw-semibold border-bottom`) + `(lanjutan)` saat `IsSectionContinuation`, di-branch `hasSections`. Kartu soal + page-nav DIPERTAHANKAN VERBATIM.
- **StartExam.cshtml JS page-maps** (Pitfall 1 anti-drift): `pageQuestionIds`/`allQuestionsData`/`pageSectionMap` semua dari `q.PageNumber` yang sama; tambah `HAS_SECTIONS` + `sectionName` per-soal.
- **StartExam.cshtml navigator + indikator + resume** (PAG-03, D-417-03/04/05/06): `renderBadges` section-aware dengan label grup full-width (`gridColumn 1/-1`, XSS-safe `textContent`) untuk `#panelNumbers` + `#drawerNumbers`, flat bila `!HAS_SECTIONS`; `updatePageIndicator` "NamaSection — Halaman n/total" (desktop + mobile, tanpa Section → "Halaman n/total"); resume handler mendarat di `RESUME_PAGE` terhitung + `showResumeInfoToast` "Lanjut dari soal no. X" (text-bg-info, reuse `#resumeToastContainer`) saat resume >0.
- Verifikasi lokal: `dotnet build` 0 error; full xUnit **673/673** hijau (665 baseline + 8 SectionPaginator, 0 regresi); runtime boot `dotnet run` @localhost:5278 → HTTP 200 (no Razor compile/startup error); `git status Migrations/ Data/` kosong (migration=FALSE).

## Task Commits

Each task was committed atomically:

1. **Task 1: CMPController.StartExam — isi field Section + ComputePages + clamp resume + mobile UA** - `26a7c552` (feat)
2. **Task 2: StartExam.cshtml — render grouping + header Section + JS page-maps single-source** - `6695fce8` (feat)
3. **Task 3: StartExam.cshtml — navigator per-Section + indikator + resume RESUME_PAGE + toast** - `7dc53c54` (feat)

## Files Created/Modified
- `Controllers/CMPController.cs` (MODIFIED) - StartExam GET package-path: 3 field Section di object initializer; blok mobile UA + ComputePages setelah loop; clamp RESUME_PAGE di ViewBag block; hapus blok mobile UA lama.
- `Views/CMP/StartExam.cshtml` (MODIFIED) - header `@{}` (pageGroups/totalPages/hasSections); loop render by PageNumber + header Section; indikator span (desktop panel + mobile footer); JS page-maps single-source + HAS_SECTIONS/pageSectionMap; renderBadges/appendSectionLabel/updatePageIndicator; showResumeInfoToast; resume handler RESUME_PAGE.

## Decisions Made
- **Indikator single-instance per surface:** `#pageSectionIndicator` di navigator panel header (desktop) + `#pageSectionIndicatorMobile` di atas mobile footer — bukan di page-nav per-page (akan menghasilkan ID dobel). Keduanya di-update `updatePageIndicator()`.
- **Resume via targetPage antara:** guard `typeof RESUME_PAGE === 'number' && >=0 && < TOTAL_PAGES`; bila resume >0 sembunyikan `page_0` (Razor default `display:block`) sebelum tampilkan halaman tujuan. Mempertahankan intent "currentPage dari RESUME_PAGE terhitung" tanpa hardcode 0.
- **mobile UA dipindah ke dalam package-path** (sebelum ComputePages) — legacy path return early via redirect, jadi hanya package-path yang capai `return View(vm)`; `questionsPerPage` lokal cukup, view tetap baca `ViewBag.QuestionsPerPage`.

## Deviations from Plan

**Penyesuaian kecil (bukan deviasi substantif): bentuk literal resume handler.**
- **Ditemukan saat:** Task 3.
- **Plan menulis:** `currentPage = (typeof RESUME_PAGE === 'number' && RESUME_PAGE >= 0) ? RESUME_PAGE : 0;` langsung.
- **Yang diterapkan:** variabel antara `var targetPage = (typeof RESUME_PAGE === 'number' && RESUME_PAGE >= 0 && RESUME_PAGE < TOTAL_PAGES) ? RESUME_PAGE : 0;` lalu `currentPage = targetPage;` — menambah guard `< TOTAL_PAGES` (defensif client-side, server sudah clamp) dan menyembunyikan `page_0` bila resume >0. Intent dan acceptance ("resume mendarat di RESUME_PAGE bukan hardcode 0", guard `typeof RESUME_PAGE`) terpenuhi.
- **Alasan:** kejelasan + safety net page_0 visibility (Razor render page_0 `display:block` by default). Tidak mengubah grading/auth/skor.
- **Commit:** `7dc53c54`.

Selain itu: plan dieksekusi sesuai tulisan.

## Issues Encountered
None. Build hijau di setiap task; suite 673/673 tanpa regresi; boot bersih HTTP 200.

## Threat Surface (dari threat_model plan)
- **T-417-03 (XSS nama Section):** mitigated — header Razor `@q.SectionName` auto-encode; navigator label `lbl.textContent`, indikator `el.textContent`, toast `document.createTextNode(message)`. Tidak ada `innerHTML` untuk nama Section (grep `.innerHTML` hanya pada skeleton toast/clear container, bukan data Section).
- **T-417-04 (palsu page/LastActivePage):** mitigated — `ComputePages` server-authoritative; `ClampResumePage` server-side; client guard `< TOTAL_PAGES` tambahan.
- **T-417-05/06 (config shift / endpoint baru):** accept by-design — recompute dari config + fallback 0; tak ada endpoint mutasi baru.

Tidak ada permukaan ancaman baru di luar threat_model plan.

## User Setup Required
None - migration=FALSE (field viewmodel render-only tidak persisted; tak ada Migrations/Data diff). Verifikasi lokal selesai; promosi ke Dev = tanggung jawab IT (notify migration=FALSE).

## Next Phase Readiness
- Surface render ujian peserta untuk PAG-01/02/03 SELESAI. Plan 03 (Wave 0 e2e/UAT real-browser, lesson 354) dapat memverifikasi: header on section change, "(lanjutan)" auto-split, StartNewPage page-break, navigator grouping, resume landing page + toast, no-Section flat smoke (backward-compat), mobile 5/halaman.
- Tidak ada blocker. Backward-compat dijamin branch `hasSections` + golden-test Plan 01.

## Self-Check: PASSED

- Files: `Controllers/CMPController.cs`, `Views/CMP/StartExam.cshtml`, `417-02-SUMMARY.md` — all FOUND.
- Commits: `26a7c552`, `6695fce8`, `7dc53c54` — verified in git log.
- Build: 0 error. Suite: 673/673. Runtime: HTTP 200. Migration: none.

---
*Phase: 417-section-pagination*
*Completed: 2026-06-24*
