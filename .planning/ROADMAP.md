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
<summary>v3.0 through v3.18 (Phases 82-149) — shipped 2026-03-02 to 2026-03-10</summary>

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

</details>

### Phase 151: Homepage Progress Overview and Upcoming Events Fix

**Goal:** Check dan perbaiki logic Progress Overview dan Upcoming Events di homepage
**Requirements**: None
**Depends on:** Phase 150
**Plans:** 1 plan
**Notes:**
- Upcoming Events seharusnya hanya menunjukkan event hari ini atau besok (bukan semua upcoming events)

Plans:
- [ ] 151-01-PLAN.md — Fix today/tomorrow filter for events + add Coaching progress bar

---

## v3.19 Assessment Certificate Toggle

**Milestone Goal:** Tambah toggle on/off sertifikat saat HC membuat assessment — agar assessment/training yang tidak butuh sertifikat tidak menampilkan tombol "View Certificate".

## Phases

- [x] **Phase 150: Certificate Toggle Implementation** - Add GenerateCertificate field, migration, UI toggle in Create/Edit forms, and guard in Results/Certificate actions (completed 2026-03-11)

## Phase Details

### Phase 150: Certificate Toggle Implementation
**Goal**: HC can control whether an assessment generates certificates via a toggle; Results and Certificate pages respect this flag
**Depends on**: Nothing (single phase)
**Requirements**: CERT-01, CERT-02, CERT-03, CERT-04, CERT-05
**Success Criteria** (what must be TRUE):
  1. AssessmentSession has GenerateCertificate bool field (default true)
  2. CreateAssessment form shows "Terbitkan Sertifikat" toggle switch (default ON)
  3. EditAssessment form shows the same toggle with current value
  4. Results page "View Certificate" button only appears when GenerateCertificate is true AND IsPassed is true
  5. Certificate action returns 404 when GenerateCertificate is false
  6. Existing assessments have GenerateCertificate = true after migration
**Plans**: 1 plan

Plans:
- [ ] 150-01-PLAN.md — Model + migration + Create/Edit forms + Results/Certificate guards

## Progress

**Execution Order:** 150

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 150. Certificate Toggle Implementation | 1/1 | Complete    | 2026-03-11 |
