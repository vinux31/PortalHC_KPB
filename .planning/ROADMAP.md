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
- ✅ **v13.0 Redesign Struktur Organisasi** - Phases 292-295 (shipped 2026-04-06)

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

<details>
<summary>✅ v13.0 Redesign Struktur Organisasi (Phases 292-295) — SHIPPED 2026-04-06</summary>

- [x] **Phase 292: Backend AJAX Endpoints** - GetOrganizationTree JSON + dual-response pada CRUD actions + CSRF utility (completed 2026-04-02)
- [x] **Phase 293: View Shell & Tree Rendering** - Ganti 520-baris view dengan ~130-baris shell + recursive tree dari JSON (completed 2026-04-02)
- [x] **Phase 294: AJAX CRUD Lengkap** - Modal add/edit, toggle, delete, action dropdown via orgTree.js tanpa page reload (completed 2026-04-03)
- [x] **Phase 295: Drag-drop Reorder** - SortableJS reorder sibling-only, cross-parent diblokir (completed 2026-04-03)

</details>
