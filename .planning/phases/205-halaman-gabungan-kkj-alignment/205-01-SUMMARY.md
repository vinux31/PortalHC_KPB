---
phase: 205-halaman-gabungan-kkj-alignment
plan: 01
subsystem: CMP
tags: [kkj, alignment, cpdp, role-filtering, tabs]
dependency_graph:
  requires: []
  provides: [CMP/DokumenKkj endpoint, DokumenKkj view]
  affects: [CMPController, Views/CMP]
tech_stack:
  added: []
  patterns: [ViewBag grouped dictionary, role-based filtering, Bootstrap nav-tabs, stacked sections]
key_files:
  created:
    - Views/CMP/DokumenKkj.cshtml
  modified:
    - Controllers/CMPController.cs
decisions:
  - "ActiveTab di-set server-side via query param sehingga deep-link ?tab=alignment berfungsi tanpa JavaScript tambahan"
  - "Role filtering dilakukan di controller (ViewBag.Bagians sudah filtered), view hanya iterasi list"
  - "Stacked sections (semua bagian vertikal) menggantikan tab-per-bagian dari halaman Kkj dan Mapping lama"
metrics:
  duration: 15 minutes
  completed_date: "2026-03-20"
  tasks_completed: 2
  tasks_total: 2
  files_created: 1
  files_modified: 1
---

# Phase 205 Plan 01: Halaman Gabungan KKJ & Alignment Summary

Halaman `/CMP/DokumenKkj` dengan 2 tab Bootstrap — KKJ (KkjFiles) dan Alignment (CpdpFiles) — menampilkan semua bagian stacked dengan file table per bagian dan role-based filtering L5-L6.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Tambah action DokumenKkj di CMPController | e63c055 | Controllers/CMPController.cs |
| 2 | Buat view DokumenKkj.cshtml dengan 2 tab stacked sections | 6c4ff1b | Views/CMP/DokumenKkj.cshtml |

## Decisions Made

- **ActiveTab server-side:** Query param `?tab=alignment` diproses di controller, set `ViewBag.ActiveTab`, sehingga tab aktif sudah benar saat halaman di-render tanpa perlu JavaScript tambahan.
- **Stacked sections:** Berbeda dari halaman lama (tab-per-bagian), halaman baru menampilkan semua bagian secara vertikal dalam satu tab sehingga user tidak perlu klik tab per bagian.
- **Role filtering di controller:** `ViewBag.Bagians` sudah berisi bagians yang boleh dilihat user. View cukup iterasi list ini, tidak ada kondisi role di view.

## Deviations from Plan

None — plan dieksekusi sesuai spesifikasi.

## Verification

- Build sukses (tidak ada error CS)
- `Controllers/CMPController.cs` berisi `public async Task<IActionResult> DokumenKkj(string? tab)`
- `Controllers/CMPController.cs` berisi `ViewBag.KkjFilesByBagian`, `ViewBag.CpdpFilesByBagian`, `ViewBag.ActiveTab`
- `Views/CMP/DokumenKkj.cshtml` berisi `nav-tabs`, `id="mainTabs"`, `id="tab-kkj"`, `id="tab-alignment"`, `id="pane-kkj"`, `id="pane-alignment"`
- Download button: `btn btn-sm btn-primary` dengan label "Unduh"
- Date format: `dd MMM yyyy` tanpa jam
- Tidak menggunakan `div class="container"`

## Self-Check: PASSED

- Views/CMP/DokumenKkj.cshtml: FOUND
- Commit e63c055: FOUND
- Commit 6c4ff1b: FOUND
