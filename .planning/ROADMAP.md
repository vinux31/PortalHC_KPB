# Roadmap: Portal HC KPB

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
<summary>v3.0 through v3.13 (Phases 82-132) — shipped 2026-03-02 to 2026-03-09</summary>

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

</details>

---

## v3.14 Bug Hunting Per Case

**Milestone Goal:** Audit menyeluruh per use-case untuk mencari bug, error, dan inkonsistensi di seluruh website — organized by functional case (Assessment, Coaching, IDP, Admin, General).

## Phases

- [ ] **Phase 133: Assessment Lifecycle Audit** - Audit assessment creation, exam flow, results, records, monitoring, and notifications
- [ ] **Phase 134: Coaching Proton Lifecycle Audit** - Audit coaching mapping, evidence upload, approval chain, export, and history
- [ ] **Phase 135: PlanIDP & Deliverable Audit** - Audit PlanIDP tabs, Deliverable progress, and CDP Dashboard
- [ ] **Phase 136: Admin Data Management Audit** - Audit ManageWorkers, ProtonData, and ManageAssessment
- [ ] **Phase 137: General & Cross-cutting Audit** - Audit login, homepage, notifications, profile, and navigation

## Phase Details

### Phase 133: Assessment Lifecycle Audit
**Goal**: Every step of the assessment lifecycle works correctly end-to-end — from admin creating assessments to workers completing exams to HC monitoring
**Depends on**: Nothing (all phases independent)
**Requirements**: ASMT-01, ASMT-02, ASMT-03, ASMT-04, ASMT-05, ASMT-06
**Success Criteria** (what must be TRUE):
  1. Admin can create assessment with question package, assign workers, and set schedule without errors
  2. Worker can start exam, answer questions with working auto-save, and submit successfully
  3. Results page shows correct score, pass/fail status, and competency earned after submission
  4. Records page displays accurate assessment and training history with working filters
  5. HC monitoring shows live progress with functional reset/force close actions, and notifications reach correct users
**Plans**: 3 plans
Plans:
- [ ] 133-01-PLAN.md — Fix 5 diagnosed assessment bugs from debug folder
- [ ] 133-02-PLAN.md — Audit and fix create/assign, exam, and results flows
- [ ] 133-03-PLAN.md — Audit and fix records, monitoring, and notifications

### Phase 134: Coaching Proton Lifecycle Audit
**Goal**: The full coaching workflow operates correctly — from admin mapping coaches to approval chain completion
**Depends on**: Nothing (all phases independent)
**Requirements**: COACH-01, COACH-02, COACH-03, COACH-04, COACH-05
**Success Criteria** (what must be TRUE):
  1. Admin can assign, edit, and deactivate coaching mappings with notifications sent correctly
  2. Coachee can upload evidence and submit deliverable without errors
  3. Approval chain (SrSpv to SectionHead to HC) works with notifications at each step
  4. PDF and Excel exports from CoachingProton page produce correct output
  5. Histori Proton displays accurate timeline per worker with correct data
**Plans**: TBD

### Phase 135: PlanIDP & Deliverable Audit
**Goal**: CDP information pages display correct role-scoped data for planning and progress tracking
**Depends on**: Nothing (all phases independent)
**Requirements**: IDP-01, IDP-02, IDP-03
**Success Criteria** (what must be TRUE):
  1. PlanIDP shows Silabus and Coaching Guidance tabs with correct data per role
  2. Deliverable page shows accurate progress tracking per coachee
  3. CDP Dashboard displays correct Proton Progress and Assessment Analytics per role
**Plans**: TBD

### Phase 136: Admin Data Management Audit
**Goal**: All admin CRUD operations for master data and assessment management work without errors
**Depends on**: Nothing (all phases independent)
**Requirements**: ADM-01, ADM-02, ADM-03
**Success Criteria** (what must be TRUE):
  1. ManageWorkers CRUD, import template download, file import, and export all function correctly
  2. ProtonData Silabus, Coaching Guidance, and Override tabs CRUD operations work without errors
  3. ManageAssessment create, edit, delete and AssessmentMonitoring actions function correctly
**Plans**: TBD

### Phase 137: General & Cross-cutting Audit
**Goal**: Core infrastructure — authentication, navigation, notifications, and user profile — works correctly across all roles
**Depends on**: Nothing (all phases independent)
**Requirements**: GEN-01, GEN-02, GEN-03, GEN-04, GEN-05
**Success Criteria** (what must be TRUE):
  1. Login (local and AD), logout, and inactive user blocking all work correctly
  2. Homepage dashboard shows correct role-scoped data
  3. Notification bell, dropdown, mark-read, and dismiss all function properly
  4. Profile view and settings (edit name, change password) work without errors
  5. Navigation between all menus is consistent with no broken links or unauthorized access
**Plans**: TBD

## Progress

**Execution Order:** All phases are independent and can execute in any order.

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 133. Assessment Lifecycle Audit | 1/3 | In Progress|  |
| 134. Coaching Proton Lifecycle Audit | 0/? | Not started | - |
| 135. PlanIDP & Deliverable Audit | 0/? | Not started | - |
| 136. Admin Data Management Audit | 0/? | Not started | - |
| 137. General & Cross-cutting Audit | 0/? | Not started | - |
