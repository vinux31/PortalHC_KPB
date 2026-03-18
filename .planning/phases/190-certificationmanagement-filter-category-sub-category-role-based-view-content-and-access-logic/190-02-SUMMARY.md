---
phase: 190-certificationmanagement-filter-category-sub-category-role-based-view-content-and-access-logic
plan: 02
subsystem: ui
tags: [razor, cshtml, ajax, cascade-filter, role-based-view]

requires:
  - phase: 190-01
    provides: GetSubCategories endpoint, FilterCertificationManagement with category/subCategory params, ViewBag.AllCategories + ViewBag.UserBagian

provides:
  - Filter Kategori/Sub Kategori dropdown dengan AJAX cascade di halaman CertificationManagement
  - Summary cards conditional (hanya L1-4)
  - Filter Bagian disabled+pre-fill untuk L4, disabled untuk L5/L6
  - Filter Unit disabled untuk L5/L6; auto-load units untuk L4
  - Kolom Sub Kategori di tabel (_CertificationManagementTablePartial)
  - Kolom Nama/Bagian/Unit hidden untuk L5/L6
  - colspan empty state dinamis (13 L1-4, 10 L5/L6)
  - category dan subCategory dikirim ke AJAX refresh dan exportExcel

affects:
  - CertificationManagement page testing
  - ExportSertifikatExcel (now receives category/subCategory params)

tech-stack:
  added: []
  patterns:
    - "Role-based conditional rendering via @if (Model.RoleLevel <= 4) in Razor views"
    - "AJAX cascade fetch pattern: Category -> Sub-Category via GetSubCategories endpoint"
    - "Reset handler preserves L4 Bagian pre-fill (does not re-enable disabled selects)"

key-files:
  created: []
  modified:
    - Views/CDP/CertificationManagement.cshtml
    - Views/CDP/Shared/_CertificationManagementTablePartial.cshtml
    - Controllers/CDPController.cs

key-decisions:
  - "Filter Bagian: L4 disabled+pre-fill, L5/L6 disabled; Unit: L5/L6 always disabled, L4 auto-loaded on page load"
  - "colCount dinamis: 13 untuk L1-4 (dengan Nama/Bagian/Unit), 10 untuk L5/L6"
  - "Category/SubKategori resolved dari AssessmentCategories hierarchy di BuildSertifikatRowsAsync — jika AssessmentSession.Category adalah child category, maka parent jadi Kategori dan child jadi SubKategori"

patterns-established:
  - "RoleLevel-gated Razor: wrap seluruh blok HTML dengan @if (Model.RoleLevel <= 4) / >= 5"

requirements-completed: [ROLE-VIEW, FILTER-CASCADE, SUBCATEGORY-DISPLAY]

duration: 8min
completed: 2026-03-18
---

# Phase 190 Plan 02: CertificationManagement Frontend — Filter Category/Sub-Category, Role-Based View Summary

**Filter cascade Category -> Sub-Category via AJAX, role-based summary cards dan kolom tabel visibility, filter disabled state per RoleLevel, kolom Sub Kategori ditambahkan ke tabel**

## Performance

- **Duration:** 8 min
- **Started:** 2026-03-18T~11:00Z
- **Completed:** 2026-03-18T~11:08Z
- **Tasks:** 2 (+ 1 checkpoint:human-verify)
- **Files modified:** 2

## Accomplishments
- Filter Kategori dan Sub Kategori tampil di filter bar dengan cascade AJAX (fetch /CDP/GetSubCategories)
- Summary cards di-wrap `@if (Model.RoleLevel <= 4)` — L5/L6 tidak melihat cards
- Filter Bagian: L4 disabled + pre-fill dari `ViewBag.UserBagian`; L5/L6 disabled
- Filter Unit: L5/L6 selalu disabled; L4 auto-load units saat page load berdasarkan bagian pre-filled
- Reset handler hanya mereset Bagian untuk L1-3; L4 hanya reset Unit value
- Kolom Sub Kategori ditambahkan setelah Kategori di thead dan tbody
- Kolom Nama/Bagian/Unit di tabel hanya tampil untuk `Model.RoleLevel <= 4`
- colspan empty state row dinamis: 13 (L1-4) / 10 (L5/L6)
- category dan subCategory params dikirim ke FilterCertificationManagement dan ExportSertifikatExcel

## Task Commits

1. **Task 1 + Task 2: Filter Category/Sub-Category cascade, role-based rendering, kolom Sub Kategori** - `358f714` (feat)
2. **Task 3: Bug fix — Category/SubKategori column swap di BuildSertifikatRowsAsync** - `59ac342` (fix)

## Files Created/Modified
- `Views/CDP/CertificationManagement.cshtml` - Filter bar + summary cards + JS cascade + role-based disabled state
- `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` - Kolom Sub Kategori + role-based column visibility + dinamis colspan
- `Controllers/CDPController.cs` - BuildSertifikatRowsAsync: load AssessmentCategories hierarchy, resolve child→parent mapping untuk Kategori/SubKategori

## Decisions Made
- Dua task frontend digabung dalam satu commit karena keduanya merupakan view-only changes tanpa dependency antar commit
- Category hierarchy lookup menggunakan in-memory dictionary setelah single ToListAsync — lebih efisien daripada per-row DB query

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed Gas Tester tampil di kolom Kategori alih-alih Sub Kategori**
- **Found during:** Task 3 (Verifikasi visual — user approval)
- **Issue:** AssessmentSession.Category menyimpan child category name (mis. "Gas Tester"), tapi BuildSertifikatRowsAsync langsung assign ke Kategori field tanpa resolusi hierarchy — sehingga "Gas Tester" muncul di kolom Kategori bukan Sub Kategori
- **Fix:** Load AssessmentCategories dengan ParentId, build lookup dictionary (child name → parent name), lalu resolve: jika Category adalah child, parent jadi Kategori dan Category jadi SubKategori
- **Files modified:** Controllers/CDPController.cs
- **Verification:** User mengkonfirmasi "Gas Tester" pindah ke kolom Sub Kategori saat verifikasi visual
- **Committed in:** `59ac342`

---

**Total deviations:** 1 auto-fixed (Rule 1 — bug fix)
**Impact on plan:** Fix diperlukan untuk correctness tampilan kolom. Tidak ada scope creep.

## Issues Encountered
- Build menghasilkan file lock error (MSB3027) karena aplikasi sedang berjalan — bukan error kompilasi, hanya exe tidak bisa di-copy. Kode Razor/C# tidak ada error.

## User Setup Required
None - tidak ada konfigurasi eksternal diperlukan.

## Next Phase Readiness
- Halaman CertificationManagement sekarang siap untuk verifikasi visual manual (Task 3 checkpoint)
- Verifikasi: login sebagai berbagai role level, cek filter cascade, summary cards, kolom tabel, export Excel

---
*Phase: 190-certificationmanagement-filter-category-sub-category-role-based-view-content-and-access-logic*
*Completed: 2026-03-18*
