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
<summary>v3.0 through v3.12 (Phases 82-129) — shipped 2026-03-02 to 2026-03-08</summary>

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

</details>

---

## v3.13 In-App Notifications

**Milestone Goal:** Sistem notifikasi in-app (bell icon di navbar) untuk semua role — coaching proton events dan assessment events, dengan mark as read/dismiss.

## Phases

- [x] **Phase 130: Notification Infrastructure** - Bell icon UI, dropdown, mark read/dismiss, and helper service (completed 2026-03-09)
- [x] **Phase 131: Coaching Proton Triggers** - Notifications for mapping, deliverable, and completion events (completed 2026-03-09)
- [ ] **Phase 132: Assessment Triggers** - Notifications for assessment assignment and group completion

## Phase Details

### Phase 130: Notification Infrastructure
**Goal**: Users can see and interact with in-app notifications via bell icon in navbar
**Depends on**: Nothing (first phase)
**Requirements**: INFRA-01, INFRA-02, INFRA-03, INFRA-04, INFRA-05
**Success Criteria** (what must be TRUE):
  1. Authenticated user sees bell icon in navbar with unread count badge (badge hidden when count is 0)
  2. Clicking bell opens dropdown showing notification title, message, and relative timestamp
  3. User can mark individual notification as read and mark all as read — badge count updates accordingly
  4. User can dismiss/delete a notification from the dropdown list
  5. Any controller can call the notification helper service to create a UserNotification for a target user
**Plans**: 1 plan

Plans:
- [x] 130-01-PLAN.md — Bell icon, dropdown UI, API endpoints, and DeleteAsync service enhancement

### Phase 131: Coaching Proton Triggers
**Goal**: Coaching Proton lifecycle events automatically notify relevant users
**Depends on**: Phase 130
**Requirements**: COACH-01, COACH-02, COACH-03, COACH-04, COACH-05, COACH-06, COACH-07
**Success Criteria** (what must be TRUE):
  1. Coach receives notification when assigned a new coachee, and when mapping is edited or deactivated
  2. Coachee receives notification when their mapping is edited or deactivated
  3. SrSpv/SectionHead receives notification when a deliverable is submitted for review
  4. Coach and coachee receive notification when a deliverable is approved or rejected
  5. All HC users receive notification when all deliverables for a coachee are complete (ProtonNotification migrated to UserNotification)
**Plans**: 2 plans

Plans:
- [x] 131-01-PLAN.md — Mapping triggers (assign, edit, deactivate) — COACH-01, COACH-02, COACH-03
- [x] 131-02-PLAN.md — Deliverable triggers (submit, approve, reject, all complete) — COACH-04, COACH-05, COACH-06, COACH-07

### Phase 132: Assessment Triggers
**Goal**: Assessment lifecycle events automatically notify relevant users
**Depends on**: Phase 130
**Requirements**: ASMT-01, ASMT-02
**Success Criteria** (what must be TRUE):
  1. Worker receives notification when a new assessment is assigned to them
  2. HC/Admin users receive notification when all workers in an assessment group have completed the exam
**Plans**: 1 plan

Plans:
- [ ] 132-01-PLAN.md — Assessment notification triggers (assign and group completion)

## Progress

**Execution Order:**
Phase 130 first, then 131 and 132 can run in parallel (both depend only on 130).

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 130. Notification Infrastructure | 1/1 | Complete    | 2026-03-09 |
| 131. Coaching Proton Triggers | 2/2 | Complete    | 2026-03-09 |
| 132. Assessment Triggers | 0/1 | Not started | - |
