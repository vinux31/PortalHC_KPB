---
phase: 220-crud-page-kelola-data
plan: 02
subsystem: ui
tags: [razor, bootstrap5, crud, organization, collapsible-tree]

requires:
  - phase: 220-01
    provides: Controller actions ManageOrganization, AddOrganizationUnit, EditOrganizationUnit, ToggleOrganizationUnitActive, DeleteOrganizationUnit, ReorderOrganizationUnit
  - phase: 219
    provides: OrganizationUnit model dan database seed data

provides:
  - Views/Admin/ManageOrganization.cshtml — halaman CRUD lengkap dengan collapsible tree table
  - UI untuk semua 6 operasi: list, tambah, edit, toggle, hapus, reorder

affects: [221-integrasi-codebase, phase-220-kelola-data]

tech-stack:
  added: []
  patterns:
    - "Collapsible tree table: Bootstrap collapse pada tbody dengan data-bs-toggle per row Bagian"
    - "Reorder buttons: inline form POST per tombol ↑↓ dengan disabled state via Razor kondisional"
    - "Delete modal: set id dan name via JavaScript dari data-* attributes pada tombol trigger"

key-files:
  created:
    - Views/Admin/ManageOrganization.cshtml
  modified: []

key-decisions:
  - "Gunakan multiple tbody per Bagian — satu tbody untuk row header Bagian, satu tbody.collapse untuk children — agar Bootstrap collapse bekerja pada level tbody"
  - "Tombol aksi menggunakan onclick stopPropagation agar klik tombol tidak memicu toggle collapse row"
  - "Grandchildren (Level 2) dirender inline tanpa nested collapse — data saat ini 2 level, view mendukung 3 level"

patterns-established:
  - "Pattern collapsible tree: tr data-bs-toggle collapse ke tbody#children-{id} — valid di Bootstrap 5"
  - "Pattern reorder buttons disabled: gunakan kondisi Razor isFirst/isLast dari IndexOf() bukan JS"

requirements-completed: [CRUD-01, CRUD-02, CRUD-03, CRUD-04, CRUD-05, CRUD-06]

duration: 15min
completed: 2026-03-21
---

# Phase 220 Plan 02: ManageOrganization CRUD View Summary

**Razor view ManageOrganization.cshtml dengan collapsible tree table Bootstrap 5, form tambah/edit inline, tombol reorder ↑↓ per baris, modal hapus permanen, dan full aria-label aksesibilitas**

## Performance

- **Duration:** 15 min
- **Started:** 2026-03-21T12:45:00Z
- **Completed:** 2026-03-21T13:00:00Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments

- View ManageOrganization.cshtml dibuat dari nol (519 baris) dengan semua fitur CRUD
- Collapsible tree table — Bagian (Level 0) bisa di-expand/collapse untuk menampilkan Unit; semua Bagian default collapsed
- Form tambah sebagai collapse panel `#addUnitForm` dengan dropdown parent (indented via `&nbsp;`)
- Form edit inline di atas tabel (muncul saat `ViewBag.EditUnit` tidak null), tombol Batal redirect bersih
- Tombol aksi per baris: Reorder ↑↓ (disabled di posisi pertama/terakhir), Edit (GET link), Toggle (POST form), Hapus (modal trigger, disabled jika ada children aktif)
- Modal konfirmasi hapus permanen dengan header `bg-danger` dan JavaScript set nama/ID dari `data-*` attributes
- Build sukses tanpa error Razor (RZ) atau C# (CS)

## Task Commits

1. **Task 1: Buat ManageOrganization.cshtml** - `8099836` (feat)

## Files Created/Modified

- `Views/Admin/ManageOrganization.cshtml` — halaman admin CRUD OrganizationUnit: breadcrumb, alert, form tambah collapse, form edit inline, collapsible tree table 3 level, tombol aksi per baris, modal hapus

## Decisions Made

- Multiple tbody digunakan (satu per Bagian): tbody root + tbody.collapse untuk children — ini necessary agar Bootstrap collapse bekerja pada level row table
- `onclick="event.stopPropagation()"` ditambahkan pada td aksi agar klik tombol tidak mentrigger collapse row Bagian
- Grandchildren Level 2 dirender inline dalam tbody.collapse Bagian induknya (tidak nested collapse) — cukup untuk data saat ini dan sesuai pattern ManageCategories

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- Build error RZ1010 "Unexpected `{`" — disebabkan oleh `@{ var rootList = ... }` di dalam blok `else {}`. Diperbaiki dengan menghapus `@` prefix karena sudah di dalam code block Razor. Satu iterasi fix, build bersih.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- View ManageOrganization.cshtml siap digunakan
- Controller actions sudah ada dari Plan 01 — halaman fully functional
- Phase 221 (integrasi codebase) dapat langsung dimulai: tinggal mengganti hardcoded OrganizationStructure.cs references di seluruh codebase ke query dari tabel OrganizationUnits

---
*Phase: 220-crud-page-kelola-data*
*Completed: 2026-03-21*
