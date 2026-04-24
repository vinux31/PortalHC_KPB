# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v5.0** — Phases 1-172 (shipped)
- ✅ **v7.1–v7.12** — Phases 176-222 (shipped)
- ✅ **v8.0–v8.7** — Phases 223-253 (shipped)
- ⏸️ **v9.0 Pre-deployment Audit & Finalization** — Phases 254-256 (deferred)
- ✅ **v9.1 UAT Coaching Proton End-to-End** — Phases 257-261 (shipped 2026-03-25, partial)
- ✅ **Phases 262-263** — Sub-path deployment fixes (shipped 2026-03-27)
- ✅ **v10.0 UAT Assessment OJT di Server Development** — Phases 264-280 (shipped)
- ⏸️ **v11.2 Admin Platform Enhancement** — Phases 281-285 (paused — closed early)
- ✅ **v12.0 Controller Refactoring** — Phases 286-291 (shipped 2026-04-02)
- ✅ **v13.0 Redesign Struktur Organisasi** — Phases 292-295 (shipped 2026-04-06)
- ✅ **v14.0 Assessment Enhancement** — Phases 296-303 (shipped 2026-04-24) — [archive](milestones/v14.0-ROADMAP.md)
- 📋 **v15.0** — TBD (next milestone)

## Phases

<details>
<summary>✅ Previous milestones (v1.0–v12.0, Phases 1-291) — SHIPPED</summary>

See .planning/MILESTONES.md for full history.

</details>

<details>
<summary>⏸️ v11.2 Admin Platform Enhancement (Phases 281-285) — PAUSED</summary>

- [ ] **Phase 281: System Settings** — Admin dapat mengelola konfigurasi aplikasi dari UI
- [x] **Phase 282: Maintenance Mode** — Admin dapat mengaktifkan mode pemeliharaan
- [x] **Phase 283: User Impersonation** — Admin dapat melihat aplikasi dari perspektif role/user lain
- [ ] **Phase 285: Dedicated Impersonation Page** — Halaman admin tersendiri untuk impersonation

</details>

<details>
<summary>✅ v13.0 Redesign Struktur Organisasi (Phases 292-295) — SHIPPED 2026-04-06</summary>

- [x] **Phase 292: Backend AJAX Endpoints** — GetOrganizationTree JSON + dual-response pada CRUD actions + CSRF utility
- [x] **Phase 293: View Shell & Tree Rendering** — Ganti 520-baris view dengan ~130-baris shell + recursive tree dari JSON
- [x] **Phase 294: AJAX CRUD Lengkap** — Modal add/edit, toggle, delete, action dropdown via orgTree.js tanpa page reload
- [x] **Phase 295: Drag-drop Reorder** — SortableJS reorder sibling-only, cross-parent diblokir

</details>

<details>
<summary>✅ v14.0 Assessment Enhancement (Phases 296-303) — SHIPPED 2026-04-24</summary>

- [x] **Phase 296: Data Foundation + GradingService Extraction** — Migrasi DB backward-compatible + GradingService single source of truth (2026-04-06)
- [x] **Phase 297: Admin Pre-Post Test** — HC membuat, mengelola, memonitor assessment Pre-Post Test (2026-04-07)
- [x] **Phase 298: Question Types** — 4 tipe soal baru (TF/MA/Essay/FiB) dengan auto/manual grading (2026-04-07)
- [x] **Phase 299: Worker Pre-Post Test + Comparison** — Pekerja mengerjakan Pre-Post Test + melihat gain score (2026-04-07)
- [x] **Phase 300: Mobile Optimization** — Exam UI responsif mobile untuk pekerja lapangan (2026-04-07)
- [x] **Phase 301: Advanced Reporting** — Item analysis, gain score report, Excel export (2026-04-07)
- [x] **Phase 302: Accessibility WCAG Quick Wins** — Keyboard nav, skip link, extra time via SignalR (2026-04-07)
- [x] **Phase 303: Rasio Coach-Coachee + Balanced Mapping** — Coach Workload dashboard + saran reassign + auto-suggest (shipped 2026-04-24, UAT deferred)

Full details: [milestones/v14.0-ROADMAP.md](milestones/v14.0-ROADMAP.md) • Requirements: [milestones/v14.0-REQUIREMENTS.md](milestones/v14.0-REQUIREMENTS.md)

</details>

### 📋 v15.0 (Planning)

Milestone berikutnya belum ditentukan. Kandidat fokus:

- Lanjut analisis performa (deep check) — rencana di `.planning/research/performance/deep-wobbling-taco.md`
- Tutup pending UAT Phase 235, 247, 303 secara batch via `/gsd-audit-uat`
- Reaktivasi Phase 281 (System Settings) & Phase 285 (Dedicated Impersonation) dari v11.2 paused
- Resolve research gaps Phase 297 (Renewal) & Phase 298 (essay char limit) yang ter-defer

Jalankan `/gsd-new-milestone` untuk memulai siklus milestone baru.

---

*Roadmap updated: 2026-04-24 (post v14.0 close)*
