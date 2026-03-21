---
gsd_state_version: 1.0
milestone: v7.12
milestone_name: Struktur Organisasi CRUD
status: Roadmap defined, ready for Phase 219
stopped_at: Phase 219 context gathered
last_updated: "2026-03-21T11:36:48.291Z"
last_activity: 2026-03-21 — Roadmap v7.12 created
progress:
  total_phases: 10
  completed_phases: 5
  total_plans: 7
  completed_plans: 7
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-21)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** v7.12 — Struktur Organisasi CRUD

## Current Position

Phase: 219 (not started)
Plan: —
Status: Roadmap defined, ready for Phase 219
Last activity: 2026-03-21 — Roadmap v7.12 created

## Performance Metrics

**Velocity (v7.11 reference):**

- Total plans completed: 9
- Phases completed: 5 (of 6 planned; Phase 216 deferred)

**By Phase:**

| Phase | Plans | Status |
|-------|-------|--------|
| 219. DB Model & Migration | TBD | Not started |
| 220. CRUD Page Kelola Data | TBD | Not started |
| 221. Integrasi Codebase | TBD | Not started |
| 222. Cleanup & Finalisasi | TBD | Not started |

## Accumulated Context

### Architecture Decisions (relevant to v7.12)

- [v3.1]: KkjFile/CpdpFile dibuat dengan FK BagianId ke entitas KkjBagian — Phase 219 mengganti ini ke OrganizationUnitId
- [v2.5]: ApplicationUser.Section dan ApplicationUser.Unit disimpan sebagai string, bukan FK — keputusan ini dipertahankan di v7.12 (validasi string saja, tidak full FK migration)
- [v7.11 Phase 217]: Category dropdown sudah berhasil dipindahkan ke master table pattern — pola serupa akan diterapkan untuk OrganizationUnit

### Known Static Class Usage

OrganizationStructure.cs saat ini digunakan di:

- AdminController (filter dropdown Bagian/Unit, worker create/edit)
- CDPController (filter section locking L4/L5)
- CMPController (filter dropdown)
- ProtonDataController (Bagian/Unit dropdown untuk ProtonKompetensi dan CoachingGuidanceFile)
- 15+ views dengan hardcoded dropdown population

### Phase Design Rationale

- Phase 219 dan 220 dipisah agar DB model bisa di-verify independen sebelum CRUD UI dibangun
- Phase 221 mencakup semua integrasi controller/view sekaligus untuk menghindari partial-broken state
- Phase 222 adalah cleanup aman — static class hanya boleh dihapus setelah semua referensi sudah diganti (Phase 221)
- KkjBagian consolidation masuk Phase 219 (bukan Phase 221) karena ini model/migration concern, bukan view concern

### Pending Todos

None.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-03-21T11:36:48.287Z
Stopped at: Phase 219 context gathered
Resume file: .planning/phases/219-db-model-migration/219-CONTEXT.md
