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
<summary>v3.0 through v3.20 (Phases 82-151) — shipped 2026-03-02 to 2026-03-11</summary>

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

</details>

---

## v3.21 Account Profile & Settings Cleanup

**Milestone Goal:** Merapikan dan fix bug di halaman Account — Profile dan Settings. Perbaiki authorization pattern, validation, UI consistency, dan code quality.

## Phases

- [ ] **Phase 152: Account Cleanup** - Fix authorization pattern, client-side validation, phone regex, ViewModel refactor, button label, and UI consistency on Profile and Settings pages

## Phase Details

### Phase 152: Account Cleanup
**Goal**: Account pages are secure, validated, and consistent — authorization is explicit, forms validate client-side, phone accepts international formats, Profile uses ViewModel properly, and Profile/Settings pages look visually consistent
**Depends on**: Nothing (single phase)
**Requirements**: SEC-01, VAL-01, VAL-02, CODE-01, UI-01, UI-02
**Success Criteria** (what must be TRUE):
  1. Unauthenticated users visiting /Account/Profile or /Account/Settings are redirected to login; Login and AccessDenied pages remain accessible without login
  2. Settings page validates form fields client-side (error messages appear immediately without a round-trip) when invalid data is submitted
  3. Phone number field on Settings page accepts formats like +62 812-3456-7890 without validation error
  4. Profile page displays the user's role without relying on ViewBag (role comes from the ViewModel)
  5. The button on Profile page that navigates to Settings is labeled accurately (not "Edit Profile")
  6. Profile and Settings pages have consistent row spacing and padding so they look like a cohesive pair
**Plans**: TBD

Plans:
- [ ] 152-01-PLAN.md — AccountController auth pattern + ValidationScripts + phone regex + ViewModel + button label + UI spacing

## Progress

**Execution Order:** 152

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 152. Account Cleanup | 0/1 | Not started | - |
