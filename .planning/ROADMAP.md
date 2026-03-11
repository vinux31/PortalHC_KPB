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

---

## v4.0 E2E Use-Case Audit (In Progress)

**Milestone Goal:** Comprehensive audit of the entire portal — code review per use-case flow, identify bugs/edge cases/security issues, then user verifies in browser. Each phase covers one complete use-case flow end-to-end.

**Audit Format:** Hybrid (Code Review + Browser UAT per phase)

### Phases

- [ ] **Phase 153: Assessment Flow Audit** - Audit full assessment lifecycle from creation to certificate
- [ ] **Phase 154: Coaching Proton Flow Audit** - Audit full coaching flow from mapping to histori
- [ ] **Phase 155: Admin Kelola Data Audit** - Audit all admin data management operations
- [ ] **Phase 156: PlanIDP & CDP Dashboard Audit** - Audit silabus browsing and dashboard metrics
- [ ] **Phase 157: Account & Auth Audit** - Audit login, profile, settings, and access control
- [ ] **Phase 158: Homepage & Navigation Audit** - Audit dashboard, guide, and nav correctness

## Phase Details

### Phase 153: Assessment Flow Audit
**Goal**: The full assessment lifecycle works correctly end-to-end for all roles with no bugs or security gaps
**Depends on**: Nothing (first phase of milestone)
**Requirements**: ASSESS-01, ASSESS-02, ASSESS-03, ASSESS-04, ASSESS-05, ASSESS-06, ASSESS-07, ASSESS-08
**Success Criteria** (what must be TRUE):
  1. HC can create an assessment with all fields (title, category, schedule, duration, pass%, packages, users, token, certificate toggle) and no validation gaps exist
  2. HC can import questions via Excel template and the import handles edge cases (empty rows, duplicate questions, wrong column count) without crashing
  3. Worker sees only assessments filtered by correct status (Open/Upcoming/Completed) and cannot access closed or unassigned assessments via direct URL
  4. Worker can start exam with token, answer questions with auto-save, resume after disconnect, submit, and view results — all without data loss
  5. Worker can download certificate only when GenerateCertificate=true and IsPassed=true; HC can monitor live, reset, force-close, and regenerate token; completed assessment creates TrainingRecord and updates competency level
**Plans**: 3 plans
Plans:
- [ ] 153-01-PLAN.md — Audit HC assessment creation and question import (ASSESS-01, ASSESS-02)
- [ ] 153-02-PLAN.md — Audit worker exam lifecycle (ASSESS-03, ASSESS-04, ASSESS-05)
- [ ] 153-03-PLAN.md — Audit certificate, monitoring, and training records (ASSESS-06, ASSESS-07, ASSESS-08)

### Phase 154: Coaching Proton Flow Audit
**Goal**: The full coaching Proton workflow works correctly for all roles with no bugs or authorization gaps
**Depends on**: Nothing (parallel with other audit phases)
**Requirements**: PROTON-01, PROTON-02, PROTON-03, PROTON-04, PROTON-05, PROTON-06, PROTON-07
**Success Criteria** (what must be TRUE):
  1. HC can create a coach-coachee mapping with correct section/unit assignment and coachee sees their assigned deliverables immediately
  2. Coachee can upload evidence for a deliverable and the upload is visible to coach and approvers with correct status
  3. Coach can create a coaching session with notes, conclusion, and action items linked to the correct coachee and deliverable
  4. SrSpv and SectionHead each see only their scoped deliverables for approval and can approve or reject with a reason that is visible to the coachee
  5. HC can create Assessment Proton (Tahun 1-2 online, Tahun 3 interview) and Histori Proton timeline shows the complete coachee journey in correct order
**Plans**: TBD

### Phase 155: Admin Kelola Data Audit
**Goal**: All admin data management operations work correctly with proper role gates and audit logging
**Depends on**: Nothing (parallel with other audit phases)
**Requirements**: ADMIN-01, ADMIN-02, ADMIN-03, ADMIN-04, ADMIN-05, ADMIN-06
**Success Criteria** (what must be TRUE):
  1. HC/Admin can create, edit, deactivate, and delete workers with cascade that removes related records without orphan leaks
  2. HC/Admin can bulk import workers via Excel template with validation that rejects malformed rows and reports errors clearly
  3. HC/Admin can upload, download, and archive KKJ and CPDP files per bagian with version history preserved
  4. HC/Admin can manage Proton Data (silabus CRUD, guidance file upload, override status) and changes take effect immediately for coachees
  5. Every admin action (create, edit, delete, import, override) is recorded in AuditLog with the correct actor NIP, timestamp, and action detail
**Plans**: TBD

### Phase 156: PlanIDP & CDP Dashboard Audit
**Goal**: Silabus browsing, coaching guidance access, and CDP dashboard show correct data for all roles
**Depends on**: Nothing (parallel with other audit phases)
**Requirements**: CDP-01, CDP-02, CDP-03, CDP-04
**Success Criteria** (what must be TRUE):
  1. Coachee sees their assigned track silabus with deliverable targets and the list matches their active ProtonTrackAssignment
  2. HC/Admin can browse any section/unit/track silabus without errors and coaching guidance files are downloadable per bagian/unit/track
  3. CDP Dashboard shows role-scoped progress metrics — coachee sees own data, coach sees their coachees, HC/Admin see all — with no cross-role data leakage
**Plans**: TBD

### Phase 157: Account & Auth Audit
**Goal**: Login, profile, settings, and authorization enforcement work correctly for all roles and edge cases
**Depends on**: Nothing (parallel with other audit phases)
**Requirements**: AUTH-01, AUTH-02, AUTH-03, AUTH-04
**Success Criteria** (what must be TRUE):
  1. User can log in via local auth (and AD if configured) and an inactive user is blocked at login with a clear error message
  2. Profile page shows correct role, section, unit, and position data for the logged-in user with no ViewBag null-reference errors
  3. User can change password and edit profile fields (FullName, Position) and changes persist after re-login
  4. Accessing a role-restricted URL without the required role redirects to the AccessDenied page, not a 500 error
**Plans**: TBD

### Phase 158: Homepage & Navigation Audit
**Goal**: The homepage dashboard, guide pages, and all navigation links are correct and role-appropriate for every role
**Depends on**: Nothing (parallel with other audit phases)
**Requirements**: NAV-01, NAV-02, NAV-03, NAV-04
**Success Criteria** (what must be TRUE):
  1. Homepage shows personalized progress bars (Proton, Coaching Sessions) and upcoming events that match the logged-in user's real data
  2. Navbar shows exactly the correct menu items for each role — no extra items visible, no missing items — including Kelola Data visibility for HC
  3. Guide pages are accessible for the appropriate roles and display role-relevant content without broken links or 404s
  4. Every navbar link and hub card link resolves to a working page with no dead links or redirect loops
**Plans**: TBD

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 153. Assessment Flow Audit | v4.0 | 0/TBD | Not started | - |
| 154. Coaching Proton Flow Audit | v4.0 | 0/TBD | Not started | - |
| 155. Admin Kelola Data Audit | v4.0 | 0/TBD | Not started | - |
| 156. PlanIDP & CDP Dashboard Audit | v4.0 | 0/TBD | Not started | - |
| 157. Account & Auth Audit | v4.0 | 0/TBD | Not started | - |
| 158. Homepage & Navigation Audit | v4.0 | 0/TBD | Not started | - |
