# Roadmap: Portal HC KBP

## Shipped Milestones

<details>
<summary>v1.0 through v2.7 (Phases 1-81) — See milestones/ for details</summary>

- v1.0 CMP Assessment Completion (Phases 1-3, shipped 2026-02-17)
- v1.1 CDP Coaching Management (Phases 4-8, shipped 2026-02-18)
- v1.2 UX Consolidation (Phases 9-12, shipped 2026-02-19)
- v1.3 Assessment Management UX (Phases 13-15, shipped 2026-02-19)
- v1.4 Assessment Monitoring (Phase 16, shipped 2026-02-19)
- v1.5 Question and Exam UX (Phase 17, shipped 2026-02-19)
- v1.6 Training Records Management (Phases 18-20, shipped 2026-02-20)
- v1.7 Assessment System Integrity (Phases 21-26, shipped 2026-02-21)
- v1.8 Assessment Polish (Phases 27-32, shipped 2026-02-23)
- v1.9 Proton Catalog Management (Phases 33-37, shipped 2026-02-24)
- v2.0 Assessment Management & Training History (Phases 38-40, shipped 2026-02-24)
- v2.1 Assessment Resilience & Real-Time Monitoring (Phases 41-45, shipped 2026-02-25)
- v2.2 Attempt History (Phase 46, shipped 2026-02-26)
- v2.3 Admin Portal (Phases 47-53, 59, shipped 2026-03-01)
- v2.4 CDP Progress (Phases 61-64, shipped 2026-03-01)
- v2.5 User Infrastructure & AD Readiness (Phases 65-72, shipped 2026-03-01)
- v2.6 Codebase Cleanup (Phases 73-78, shipped 2026-03-01)
- v2.7 Assessment Monitoring (Phases 79-81, shipped 2026-03-01)

</details>

<details>
<summary>v3.0 through v3.21 (Phases 82-152) — shipped 2026-03-02 to 2026-03-11</summary>

- **v3.0 Full QA & Feature Completion** — Phases 82-91 (shipped 2026-03-05)
- **v3.1 CPDP Mapping File-Based Rewrite** — Phase 88 CPDP (shipped 2026-03-03)
- **v3.2 Bug Hunting & Quality Audit** — Phases 92-98 (shipped 2026-03-05)
- **v3.5 User Guide** — Phases 105-106 (shipped 2026-03-06)
- **v3.6 Histori Proton** — Phases 107-108 (shipped 2026-03-06)
- **v3.7 Role Access & Filter Audit** — Phases 109-111 (shipped 2026-03-07)
- **v3.8 CoachingProton UI Redesign** — Phase 112 (shipped 2026-03-07)
- **v3.9 ProtonData Enhancement** — Phases 113-115 (shipped 2026-03-07)
- **v3.10 Evidence Coaching & Deliverable Redesign** — Phases 116-120 (shipped 2026-03-08)
- **v3.11 CoachCoacheeMapping Overhaul** — Phases 123-125 (shipped 2026-03-08)
- **v3.12 Progress Unit Scoping** — Phases 128-129 (shipped 2026-03-08)
- **v3.13 In-App Notifications** — Phases 130-132 (shipped 2026-03-09)
- **v3.14 Bug Hunting Per Case** — Phases 133-137 (shipped 2026-03-09)
- **v3.15 Assessment Real Time Test** — Phases 138-142 (shipped 2026-03-09)
- **v3.16 Form Coaching GAST Redesign** — Phases 143-144 (shipped 2026-03-09)
- **v3.17 Assessment Sub-Competency Analysis** — Phases 145-147 (shipped 2026-03-10)
- **v3.18 Homepage Minimalist Redesign** — Phases 148-149 (shipped 2026-03-10)
- **v3.19 Assessment Certificate Toggle** — Phase 150 (shipped 2026-03-11)
- **v3.20 Homepage Progress & Events Fix** — Phase 151 (shipped 2026-03-11)
- **v3.21 Account Profile & Settings Cleanup** — Phase 152 (shipped 2026-03-11)

</details>

<details>
<summary>v4.0 E2E Use-Case Audit (Phases 153-158) — shipped 2026-03-12</summary>

- **v4.0 E2E Use-Case Audit** — Phases 153-158 (shipped 2026-03-12)
  - Phase 153: Assessment Flow Audit (4 plans)
  - Phase 154: Coaching Proton Flow Audit (3 plans)
  - Phase 155: Admin Kelola Data Audit (3 plans)
  - Phase 156: PlanIDP & CDP Dashboard Audit (2 plans)
  - Phase 157: Account & Auth Audit (2 plans)
  - Phase 158: Homepage & Navigation Audit (2 plans)

</details>

---

## v4.1 Coaching Proton Deduplication (In Progress)

**Milestone Goal:** Fix duplicate deliverable rows in CoachingProton caused by overly broad reactivate cascade creating multiple active ProtonTrackAssignments per coachee+track. Includes data cleanup, defensive guard, and a new assignment removal feature.

### Phases

- [x] **Phase 159: Deduplication Fix & Guard** - Fix reactivate cascade, add upsert on assign, clean up existing duplicates, add defensive query guard (completed 2026-03-12)
- [x] **Phase 160: Assignment Removal** - Add "Hapus" action on deactivated mappings for permanent deletion with audit logging (completed 2026-03-12)

## Phase Details

### Phase 159: Deduplication Fix & Guard
**Goal**: CoachingProton shows no duplicate deliverable rows for any coachee — reactivate cascade is safe, assign flow is idempotent, existing dirty data is cleaned, and the query itself tolerates any surviving bad data
**Depends on**: Nothing (first phase of milestone)
**Requirements**: FIX-01, FIX-02, CLN-01, DEF-01
**Success Criteria** (what must be TRUE):
  1. Coach views CoachingProton page and sees each deliverable row exactly once per coachee, with no duplicates — even for coachees who were deactivated and reactivated multiple times
  2. Admin deactivates then reactivates a CoachCoacheeMapping; only that mapping's ProtonTrackAssignments are restored (assignments from unrelated prior mappings remain inactive)
  3. HC assigns a coachee to a track they were previously assigned to; no new ProtonTrackAssignment row is created — the existing inactive one is reused
  4. After running the migration/seed cleanup, no coachee+track combination has more than one active ProtonTrackAssignment in the database
**Plans:** 2/2 plans complete
Plans:
- [x] 159-01-PLAN.md -- Add DeactivatedAt field, fix Reactivate cascade and Assign idempotency
- [x] 159-02-PLAN.md -- Data cleanup and defensive query guard

### Phase 160: Assignment Removal
**Goal**: HC and Admin can permanently delete deactivated coach-coachee mappings and all their associated data with a confirmation dialog, and the action is logged
**Depends on**: Phase 159
**Requirements**: RMV-01, RMV-02
**Success Criteria** (what must be TRUE):
  1. CoachCoacheeMapping page shows a "Hapus" button only on deactivated (not active) mappings
  2. Clicking "Hapus" shows a confirmation dialog; confirming permanently deletes the mapping, its ProtonTrackAssignments, and all associated ProtonDeliverableProgress rows
  3. After deletion, the mapping and all its child records no longer appear anywhere in the portal
  4. The deletion action appears in the AuditLog with actor, timestamp, and the deleted mapping's details
**Plans:** 1/1 plans complete
Plans:
- [ ] 160-01-PLAN.md -- Add Hapus button, delete modal, controller actions, and audit logging

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 159. Deduplication Fix & Guard | v4.1 | 2/2 | Complete | 2026-03-12 |
| 160. Assignment Removal | 1/1 | Complete    | 2026-03-12 | - |
