# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v5.0** - Phases 1-172 (shipped)
- ✅ **v7.1–v7.12** - Phases 176-222 (shipped)
- ✅ **v8.0–v8.7** - Phases 223-253 (shipped)
- ⏸️ **v9.0 Pre-deployment Audit & Finalization** - Phases 254-256 (deferred)
- ✅ **v9.1 UAT Coaching Proton End-to-End** - Phases 257-261 (shipped 2026-03-25, partial)
- ✅ **Phases 262-263** - Sub-path deployment fixes (shipped 2026-03-27)
- ✅ **v10.0 UAT Assessment OJT di Server Development** - Phases 264-280 (shipped)
- ⏸️ **v11.2 Admin Platform Enhancement** - Phases 281-285 (paused — closed early)
- ✅ **v12.0 Controller Refactoring** - Phases 286-291 (shipped 2026-04-02)
- 🚧 **v13.0 Redesign Struktur Organisasi** - Phases 292-295 (in progress)

## Phases

<details>
<summary>✅ Previous milestones (v1.0–v12.0, Phases 1-291) — SHIPPED</summary>

See .planning/MILESTONES.md for full history.

</details>

<details>
<summary>⏸️ v11.2 Admin Platform Enhancement (Phases 281-285) — PAUSED</summary>

- [ ] **Phase 281: System Settings** - Admin dapat mengelola konfigurasi aplikasi dari UI
- [x] **Phase 282: Maintenance Mode** - Admin dapat mengaktifkan mode pemeliharaan
- [x] **Phase 283: User Impersonation** - Admin dapat melihat aplikasi dari perspektif role/user lain
- [ ] **Phase 285: Dedicated Impersonation Page** - Halaman admin tersendiri untuk impersonation

</details>

### 🚧 v13.0 Redesign Struktur Organisasi

- [x] **Phase 292: Backend AJAX Endpoints** - GetOrganizationTree JSON + dual-response pada CRUD actions + CSRF utility (completed 2026-04-02)
- [x] **Phase 293: View Shell & Tree Rendering** - Ganti 520-baris view dengan ~130-baris shell + recursive tree dari JSON (completed 2026-04-02)
- [ ] **Phase 294: AJAX CRUD Lengkap** - Modal add/edit, toggle, delete, action dropdown via orgTree.js tanpa page reload
- [ ] **Phase 295: Drag-drop Reorder** - SortableJS reorder sibling-only, cross-parent diblokir

## Phase Details

### Phase 292: Backend AJAX Endpoints
**Goal**: OrganizationController siap melayani AJAX — endpoint GetOrganizationTree baru tersedia dan semua CRUD action sudah dual-response (JSON jika AJAX, redirect jika form POST)
**Depends on**: Nothing (first phase v13.0)
**Requirements**: TREE-01, TREE-04
**Success Criteria** (what must be TRUE):
  1. GET `/Organization/GetOrganizationTree` mengembalikan flat JSON array semua OrganizationUnit dengan field Id, Name, ParentId, Level, DisplayOrder, IsActive
  2. POST actions (Create, Edit, Toggle, Delete, Reorder) mengembalikan `{success, message}` JSON jika header `X-Requested-With: XMLHttpRequest` ada, tetap redirect jika bukan AJAX
  3. Semua AJAX POST sudah melewati CSRF dengan utility function terpusat `ajaxPost(url, data)` di orgTree.js
  4. Tidak ada regression pada alur PRG yang sudah ada — halaman tetap berfungsi normal jika JS dimatikan
**Plans**: 1 plan
Plans:
- [x] 292-01-PLAN.md — IsAjaxRequest helper + GetOrganizationTree + dual-response + orgTree.js utility
**UI hint**: yes

### Phase 293: View Shell & Tree Rendering
**Goal**: Halaman ManageOrganization ter-render sebagai tree view interaktif dari JSON — user dapat melihat hierarki dengan indentasi, expand/collapse per node dan semua sekaligus, serta badge status
**Depends on**: Phase 292
**Requirements**: TREE-01, TREE-02, TREE-03, TREE-04
**Success Criteria** (what must be TRUE):
  1. Halaman ManageOrganization menampilkan tree view dengan indentasi visual per level (Bagian → Unit → Sub-unit)
  2. User dapat expand/collapse node individual dengan klik panah, dan ada tombol Expand All / Collapse All
  3. Setiap node menampilkan badge Aktif (hijau) atau Nonaktif (merah/abu) yang sesuai dengan status database
  4. Tree mendukung kedalaman unlimited — rendering rekursif berjalan benar untuk node Level 0, 1, 2, dan seterusnya
  5. ManageOrganization.cshtml dikurangi dari ~520 baris menjadi ~130 baris dengan 3 loop Razor dihapus
**Plans**: 1 plan
Plans:
- [x] 293-01-PLAN.md — View shell + orgTree.js tree rendering + expand/collapse
**UI hint**: yes

### Phase 294: AJAX CRUD Lengkap
**Goal**: Admin/HC dapat melakukan seluruh operasi CRUD pada struktur organisasi via modal tanpa page reload — Add, Edit, Toggle, Delete semuanya AJAX dengan feedback toast
**Depends on**: Phase 293
**Requirements**: CRUD-01, CRUD-02, CRUD-03, CRUD-04, CRUD-05
**Success Criteria** (what must be TRUE):
  1. Admin/HC dapat menambah unit baru via modal — form terisi, submit, tree refresh tanpa reload halaman
  2. Admin/HC dapat mengedit nama dan parent unit via modal — perubahan tersimpan dan tree diperbarui tanpa reload
  3. Admin/HC dapat toggle aktif/nonaktif unit — status badge berubah instan tanpa reload
  4. Admin/HC dapat menghapus unit via modal konfirmasi — node hilang dari tree tanpa reload
  5. Setiap node memiliki action dropdown (Edit, Toggle, Hapus) menggantikan tombol inline; setiap operasi menampilkan toast notifikasi sukses/gagal
**Plans**: 1 plan
Plans:
- [ ] 293-01-PLAN.md — View shell + orgTree.js tree rendering + expand/collapse
**UI hint**: yes

### Phase 295: Drag-drop Reorder
**Goal**: Admin/HC dapat mengubah urutan unit dalam sibling yang sama dengan drag-and-drop — cross-parent drag diblokir sepenuhnya
**Depends on**: Phase 294
**Requirements**: REORD-01, REORD-02
**Success Criteria** (what must be TRUE):
  1. Admin/HC dapat drag node ke posisi atas/bawah dalam sibling yang sama — urutan tersimpan ke database via `ReorderOrganizationUnit`
  2. Drag handle visual muncul pada hover node sehingga user tahu bahwa node bisa di-drag
  3. Drag lintas parent (reparent via drag) diblokir secara teknis — SortableJS dikonfigurasi `group: false` sehingga node tidak bisa pindah ke parent lain
**Plans**: 1 plan
Plans:
- [ ] 293-01-PLAN.md — View shell + orgTree.js tree rendering + expand/collapse
**UI hint**: yes

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 292. Backend AJAX Endpoints | 1/1 | Complete   | 2026-04-02 |
| 293. View Shell & Tree Rendering | 1/1 | Complete   | 2026-04-02 |
| 294. AJAX CRUD Lengkap | 0/? | Not started | - |
| 295. Drag-drop Reorder | 0/? | Not started | - |
