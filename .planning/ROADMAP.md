# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v5.0** - Phases 1-172 (shipped)
- ✅ **v7.1–v7.12** - Phases 176-222 (shipped)
- ✅ **v8.0** - Phases 223-227 (shipped)
- ✅ **v8.1** - Phases 228-232 (shipped)
- ✅ **v8.2** - Phases 233-238 (shipped)
- ✅ **v8.3** - Phase 239 (shipped)
- ✅ **v8.4** - Phase 240 (shipped)
- ✅ **v8.5** - Phases 241-247 (shipped)
- ✅ **v8.6 Codebase Audit & Hardening** - Phases 248-252 (shipped 2026-03-24)
- ✅ **v8.7** - Phase 253 (shipped)
- ⏸️ **v9.0 Pre-deployment Audit & Finalization** - Phases 254-256 (deferred)
- ✅ **v9.1 UAT Coaching Proton End-to-End** - Phases 257-261 (shipped 2026-03-25, partial — Phase 257 only)

## Phases

<details>
<summary>✅ v9.1 UAT Coaching Proton End-to-End (Phases 257-261) — SHIPPED 2026-03-25 (partial)</summary>

- [x] Phase 257: Setup & Mapping (2/2 plans)
- [ ] Phase 258: Silabus & Guidance (skipped)
- [ ] Phase 259: Evidence & Coaching Session (skipped)
- [ ] Phase 260: Approval Chain (skipped)
- [ ] Phase 261: Dashboard, Export & Completion (skipped)

</details>

<details>
<summary>⏸️ v9.0 Pre-deployment Audit & Finalization (Phases 254-256) — DEFERRED</summary>

- [ ] Phase 254: Seed Cleanup & Tech Debt Closure
- [ ] Phase 255: Production Configuration
- [ ] Phase 256: Security Hardening

Backup: `.planning/milestones/v9.0-REQUIREMENTS.md`, `.planning/milestones/v9.0-ROADMAP.md`

</details>

<details>
<summary>✅ v8.5 UAT Assessment System End-to-End (Phases 241-247) — SHIPPED 2026-03-24</summary>

- [x] Phase 241: Seed Data UAT (2/2 plans)
- [x] Phase 242: UAT Setup Flow (2/2 plans)
- [x] Phase 243: UAT Exam Flow (2/2 plans)
- [x] Phase 244: UAT Monitoring & Analytics (2/2 plans)
- [x] Phase 245: UAT Proton Assessment (2/2 plans)
- [x] Phase 246: UAT Edge Cases & Records (2/2 plans)
- [x] Phase 247: Bug Fix Pasca-UAT (2/2 plans)

</details>

<details>
<summary>✅ v8.6 Codebase Audit & Hardening (Phases 248-252) — SHIPPED 2026-03-24</summary>

- [x] Phase 248: UI & Annotations (1/1 plans)
- [x] Phase 249: Null Safety & Input Validation (2/2 plans)
- [x] Phase 250: Security & Performance (1/1 plans)
- [x] Phase 251: Data Integrity & Logic (2/2 plans)
- [x] Phase 252: XSS Escape AJAX Approval Badge (1/1 plans)

</details>

<details>
<summary>✅ v8.7 AddTraining Multi-Select (Phase 253) — SHIPPED 2026-03-25</summary>

- [x] Phase 253: AddTraining multi-select pekerja dan perbaikan form (2/2 plans)

</details>

### Phase 259: Export Categories (Excel & PDF) + Bug Fix Signatory

**Goal:** Fix bug signatory sub-kategori dan tambah export Excel/PDF di ManageCategories
**Requirements**: N/A
**Depends on:** None
**Plans:** 1/1 plans complete

Plans:
- [x] 259-01-PLAN.md — Bug fix signatory + export Excel & PDF + tombol UI

### Phase 1: Tambahkan tombol hapus worker di halaman ManageWorkers

**Goal:** [To be planned]
**Requirements**: TBD
**Depends on:** Phase 0
**Plans:** 0 plans

Plans:
- [ ] TBD (run /gsd:plan-phase 1 to break down)

### Phase 260: Auto-cascade perubahan nama OrganizationUnit ke semua user records dan template

**Goal:** Cascade rename/reparent OrganizationUnit ke semua user records, blokir deactivate jika ada user aktif, dan ubah template import jadi dinamis
**Requirements**: D-01, D-02, D-03, D-05, D-06, D-07, D-09
**Depends on:** Phase 259
**Plans:** 1/1 plans complete

Plans:
- [x] 260-01-PLAN.md — Cascade rename/reparent + blokir deactivate + dynamic template

### Phase 261: Validasi konsistensi field organisasi di CoachCoacheeMapping dan Directorate

**Goal:** One-time cleanup data CoachCoacheeMapping yang Section/Unit invalid + runtime validation di assign/edit/import
**Requirements**: D-01, D-02, D-03, D-04, D-05, D-06, D-07, D-08, D-09
**Depends on:** Phase 260
**Plans:** 1/1 plans complete

Plans:
- [x] 261-01-PLAN.md — Cleanup + runtime validation Section/Unit di CoachCoacheeMapping

### Phase 262: Fix hardcoded URLs in Views for sub-path deployment compatibility

**Goal:** Setup UsePathBase middleware dan fix ~83 hardcoded absolute URL di 25 view files agar kompatibel dengan sub-path deployment /KPB-PortalHC/
**Requirements**: D-01, D-02, D-03, D-04, D-05, D-06
**Depends on:** Phase 261
**Plans:** 1/3 plans executed

Plans:
- [x] 262-01-PLAN.md — Setup UsePathBase di Program.cs + basePath/appUrl di _Layout.cshtml
- [ ] 262-02-PLAN.md — Fix hardcoded URL di 12 file high-volume (66+ fixes)
- [ ] 262-03-PLAN.md — Fix hardcoded URL di 12 file low-volume + final sweep
