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

<details>
<summary>v4.1 Coaching Proton Deduplication (Phases 159-161) — shipped 2026-03-12</summary>

- **v4.1 Coaching Proton Deduplication** — Phases 159-161 (shipped 2026-03-12)
  - Phase 159: Deduplication Fix & Guard (2 plans)
  - Phase 160: Assignment Removal (1 plan)
  - Phase 161: Fix deliverable ordering in CoachingProton table (1 plan)

</details>

<details>
<summary>v4.2 Real-time Assessment (Phases 162-166) — shipped 2026-03-13</summary>

- **v4.2 Real-time Assessment** — Phases 162-166 (shipped 2026-03-13)
  - Phase 162: Simplifikasi Action Close + Auto-Grade (2 plans)
  - Phase 163: Hub Infrastructure & Safety Foundations (2 plans)
  - Phase 164: HC-to-Worker Push Events (2 plans)
  - Phase 165: Worker-to-HC Progress Push + Polling Removal (2 plans)
  - Phase 166: Activity Log Per-Worker (2 plans)

</details>

---

## v4.3 Bug Finder (In Progress)

**Milestone Goal:** Audit menyeluruh seluruh codebase, database, dan file — temukan bug, file tidak terpakai, data tidak penting, dan dead code. Hasilkan portal yang bersih, aman, dan bebas technical debt.

### Phases

- [x] **Phase 168: Code Audit** - Identify and remove dead code, fix logic bugs, clean unused imports, remove orphaned views (completed 2026-03-13)
- [ ] **Phase 169: File & Database Audit** - Remove unused files, orphaned JS/CSS, temp artifacts; clean orphaned DB records, unused tables, stale data, verify integrity
- [ ] **Phase 170: Security Review** - Audit authorization attributes, CSRF protection, input validation gaps, file upload security

## Phase Details

### Phase 168: Code Audit
**Goal**: The codebase contains no dead code, logic bugs, unused imports, or orphaned views — every file and method is reachable and correct
**Depends on**: Nothing (first phase of milestone)
**Requirements**: CODE-01, CODE-02, CODE-03, CODE-04
**Success Criteria** (what must be TRUE):
  1. No controller action, helper method, or class exists that cannot be reached through any route or call chain in the application
  2. All known logic bugs across controllers are identified, documented, and either fixed or explicitly deferred with justification
  3. No unused `using` statements or unnecessary namespace imports remain in any .cs file
  4. No .cshtml view file exists in the Views directory without a corresponding reachable controller action
**Plans:** 3/3 plans complete
Plans:
- [ ] 168-01-PLAN.md — Dead code removal and orphaned views cleanup
- [ ] 168-02-PLAN.md — Logic bug audit and fixes
- [ ] 168-03-PLAN.md — Unused imports cleanup

### Phase 169: File & Database Audit
**Goal**: The file system and database contain no orphaned, duplicate, or stale artifacts — every file is referenced and every DB record is valid
**Depends on**: Phase 168
**Requirements**: FILE-01, FILE-02, FILE-03, FILE-04, DB-01, DB-02, DB-03, DB-04
**Success Criteria** (what must be TRUE):
  1. No .cshtml, .js, or .css file exists in the project that is not referenced by any route, bundle, or layout
  2. No temporary files (screenshots, debug logs, test artifacts) remain in the project directory tree
  3. No duplicate or near-duplicate code blocks exist across views or controllers that could be unified into a shared partial or method
  4. All database records have valid foreign key references — no orphaned rows pointing to deleted or non-existent parents
  5. All seed data and test data that is not required for production operation is identified and removed or clearly marked
**Plans:** 3 plans
Plans:
- [ ] 169-01-PLAN.md — Temp file cleanup and orphaned JS/CSS audit
- [ ] 169-02-PLAN.md — View reachability re-verification and duplicate code audit
- [ ] 169-03-PLAN.md — Database schema, FK integrity, and seed data audit

### Phase 170: Security Review
**Goal**: All controller actions have correct authorization, all forms have CSRF protection, and no input validation gaps or unsafe file upload paths exist
**Depends on**: Phase 169
**Requirements**: SEC-01, SEC-02, SEC-03, SEC-04
**Success Criteria** (what must be TRUE):
  1. Every controller action that modifies data or returns sensitive information has an explicit `[Authorize]` attribute with the correct role scope — no action is accidentally open to unauthenticated or under-privileged users
  2. Every POST action that changes state has `[ValidateAntiForgeryToken]` — no state-changing form can be submitted via cross-site request forgery
  3. No user-supplied string is rendered unescaped in any view, no redirect target accepts unvalidated URL input, and no raw SQL is constructed from user input
  4. All file upload endpoints validate file type (allowlist extension check), enforce a maximum file size, and resolve upload paths server-side — no path traversal is possible
**Plans**: TBD

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 168. Code Audit | 3/3 | Complete    | 2026-03-13 | - |
| 169. File & Database Audit | v4.3 | 0/3 | Not started | - |
| 170. Security Review | v4.3 | 0/TBD | Not started | - |
