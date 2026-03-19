---
phase: 203-certificate-history-modal
plan: 01
subsystem: ui
tags: [asp.net, razor, partial-view, ajax, certificate, union-find]

requires:
  - phase: 202-renewal-certificate-table
    provides: BuildRenewalRowsAsync pattern, SertifikatRow model, renewal FK schema

provides:
  - WorkerId property di SertifikatRow
  - CertificateChainGroup ViewModel class
  - CertificateHistory endpoint di AdminController (returns HTML partial via AJAX)
  - _CertificateHistoryModalContent.cshtml partial view dengan grouping by renewal chain

affects:
  - 203-02 (modal integration akan consume endpoint ini)

tech-stack:
  added: []
  patterns:
    - "Union-Find in-memory graph untuk grouping renewal chain certificates"
    - "PartialView return untuk AJAX-consumed endpoint"
    - "Scoped renewal lookup — filter by workerId sebelum batch query"

key-files:
  created:
    - Views/Shared/_CertificateHistoryModalContent.cshtml
  modified:
    - Models/CertificationManagementViewModel.cs
    - Controllers/AdminController.cs
    - Controllers/CDPController.cs

key-decisions:
  - "Union-Find dipakai untuk grouping renewal chain — lebih scalable dari traversal rekursif"
  - "ChainTitle diambil dari oldest cert di chain (SubKategori > Kategori > Judul fallback)"
  - "Renewal lookup di CertificateHistory di-scope ke workerId certs saja — tidak full-table scan"
  - "Tombol Renew menggunakan href ke /Admin/CreateAssessment dengan query param renewSessionId atau renewTrainingId"

patterns-established:
  - "CertificateHistory endpoint: [HttpGet][Authorize(Roles = Admin, HC)] returns PartialView"
  - "SertifikatRow.WorkerId diisi oleh semua builder methods (BuildRenewalRowsAsync, BuildSertifikatRowsAsync, CertificateHistory)"

requirements-completed: [HIST-01, HIST-02]

duration: 25min
completed: 2026-03-19
---

# Phase 203 Plan 01: Certificate History Modal Backend Summary

**CertificateHistory AJAX endpoint + grouped partial view dengan Union-Find renewal chain algorithm, mode renewal/readonly support**

## Performance

- **Duration:** 25 min
- **Started:** 2026-03-19T08:55:00Z
- **Completed:** 2026-03-19T09:20:00Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments

- Tambah `WorkerId` ke `SertifikatRow` dan `CertificateChainGroup` ViewModel baru
- `CertificateHistory` action endpoint yang mengembalikan HTML partial via AJAX, grouped by renewal chain
- `_CertificateHistoryModalContent.cshtml` partial view dengan mode renewal (ada tombol Renew) dan readonly (tanpa tombol)
- Update `BuildRenewalRowsAsync` (AdminController) dan `BuildSertifikatRowsAsync` (CDPController) agar mengisi `WorkerId`

## Task Commits

1. **Task 1: WorkerId + CertificateChainGroup + BuildRenewalRowsAsync update** - `05b036e` (feat)
2. **Task 2: CertificateHistoryAsync action + partial view** - `3c2d578` (feat)

## Files Created/Modified

- `Models/CertificationManagementViewModel.cs` - Tambah WorkerId di SertifikatRow, tambah class CertificateChainGroup
- `Controllers/AdminController.cs` - Tambah CertificateHistory action, update BuildRenewalRowsAsync dengan UserId/WorkerId
- `Controllers/CDPController.cs` - Update BuildSertifikatRowsAsync dengan UserId/WorkerId di training & assessment mapping
- `Views/Shared/_CertificateHistoryModalContent.cshtml` - Partial view baru: grouped table, badge status, badge sumber, tombol Renew, empty state

## Decisions Made

- Union-Find dipakai untuk grouping renewal chain secara in-memory — menghindari rekursi per-cert yang mahal
- ChainTitle fallback: SubKategori > Kategori > Judul dari oldest cert di chain
- Renewal lookup di-scope ke workerId certs saja (bukan full-table scan) untuk efisiensi
- Tombol Renew pakai href dengan query param `renewSessionId` atau `renewTrainingId` sesuai RecordType

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Endpoint `/Admin/CertificateHistory?workerId=X&mode=renewal` siap dikonsumsi AJAX
- Plan 02 dapat langsung mengintegrasikan modal dengan tombol trigger di halaman yang sesuai
- Partial view mendukung kedua mode (renewal/readonly) via ViewBag.Mode

---
*Phase: 203-certificate-history-modal*
*Completed: 2026-03-19*
