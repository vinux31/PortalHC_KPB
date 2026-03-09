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
<summary>v3.0 through v3.14 (Phases 82-137) — shipped 2026-03-02 to 2026-03-09</summary>

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
- **v3.14 Bug Hunting Per Case** — Phases 133-137 (in progress)

</details>

---

## v3.15 Assessment Real Time Test

**Milestone Goal:** Simulasi assessment end-to-end secara real untuk memverifikasi seluruh flow berjalan tanpa error — dari sisi HC membuat & memonitor assessment, hingga sisi Worker mengerjakan ujian, dengan validasi real-time monitoring.

## Phases

- [ ] **Phase 138: Assessment Setup & Monitoring Overview** - Verify HC can create, import, assign, and view assessment groups without errors
- [ ] **Phase 139: Worker Exam Lifecycle** - Verify worker exam flow from token entry through submission with auto-save and timer
- [ ] **Phase 140: HC Real-Time Monitoring & Actions** - Verify HC monitoring dashboard and all HC actions (Reset, ForceClose, CloseEarly, RegenerateToken)
- [ ] **Phase 141: Post-Exam & Records Validation** - Verify results display, competency updates, notifications, and attempt history
- [ ] **Phase 142: Edge Cases & Integration** - Verify timer enforcement, window expiry, stale detection, audit logging, and worker redirect

## Phase Details

### Phase 138: Assessment Setup & Monitoring Overview
**Goal**: HC can set up a complete assessment and see accurate monitoring overview
**Depends on**: Nothing (first phase)
**Requirements**: SETUP-01, SETUP-02, SETUP-03, SETUP-04
**Success Criteria** (what must be TRUE):
  1. HC creates assessment with all fields (title, category, schedule, duration, pass%) and it persists correctly
  2. HC imports question package and questions/options map correctly to the assessment
  3. HC assigns workers and they appear in the monitoring participant list
  4. Assessment Monitoring group list shows correct participant count, completed count, passed count, and status badge
**Plans**: TBD

Plans:
- [ ] 138-01: TBD

### Phase 139: Worker Exam Lifecycle
**Goal**: Worker can complete an exam end-to-end with correct auto-save, resume, and timer behavior
**Depends on**: Phase 138
**Requirements**: EXAM-01, EXAM-02, EXAM-03, EXAM-04, EXAM-05, EXAM-06
**Success Criteria** (what must be TRUE):
  1. Worker enters valid token and starts exam successfully on both Package and Legacy paths
  2. Each answer selection triggers auto-save and persists to the correct response table
  3. Exam Summary page accurately reflects answered vs unanswered question counts
  4. After submission, worker sees correct score and pass/fail status matching server calculation
  5. On page reload mid-exam, session resumes with correct elapsed time, page position, and pre-populated answers
**Plans**: TBD

Plans:
- [ ] 139-01: TBD

### Phase 140: HC Real-Time Monitoring & Actions
**Goal**: HC can monitor live exam progress and execute all management actions correctly
**Depends on**: Phase 139
**Requirements**: MON-01, MON-02, MON-03, MON-04, MON-05, MON-06
**Success Criteria** (what must be TRUE):
  1. Monitoring detail page shows live answered/total progress, status, score, and remaining time per worker with 10s polling refresh
  2. Reset clears worker data, archives attempt history, and worker can restart the exam from scratch
  3. Force Close marks session as Completed with score 0 and fail, and Force Close All bulk-closes all Open/InProgress sessions
  4. Close Early auto-scores current answers and completes all InProgress sessions in the group
  5. Regenerate Token generates a new token applied to all sibling sessions in the group
**Plans**: TBD

Plans:
- [ ] 140-01: TBD

### Phase 141: Post-Exam & Records Validation
**Goal**: Post-exam data is accurate across results, competency updates, notifications, and history
**Depends on**: Phase 140
**Requirements**: POST-01, POST-02, POST-03, POST-04, POST-05
**Success Criteria** (what must be TRUE):
  1. Results page displays correct score, pass/fail status, and earned competencies after submission
  2. Passing assessment triggers competency auto-update via AssessmentCompetencyMap with correct level
  3. Notifications are sent to the correct users after assessment completion
  4. Riwayat Assessment tab shows the completed assessment with accurate score, date, and status
  5. After Reset and re-exam, Attempt History shows previous attempts with correct sequential numbering
**Plans**: TBD

Plans:
- [ ] 141-01: TBD

### Phase 142: Edge Cases & Integration
**Goal**: Edge case handling and audit integration work correctly under non-happy-path conditions
**Depends on**: Phase 141
**Requirements**: EDGE-01, EDGE-02, EDGE-03, EDGE-04, EDGE-05
**Success Criteria** (what must be TRUE):
  1. Server rejects exam submission after DurationMinutes + 2min grace period has elapsed
  2. Exam entry is blocked when the assessment window close date has expired
  3. Stale question detection clears worker progress when question count changes mid-exam
  4. All HC actions (Reset, ForceClose, CloseEarly, RegenerateToken) produce AuditLog entries with actor, timestamp, and details
  5. Worker is redirected correctly when HC triggers CloseEarly (CheckExamStatus detects closed state)
**Plans**: TBD

Plans:
- [ ] 142-01: TBD

## Progress

**Execution Order:** 138 → 139 → 140 → 141 → 142

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 138. Assessment Setup & Monitoring Overview | 0/? | Not started | - |
| 139. Worker Exam Lifecycle | 0/? | Not started | - |
| 140. HC Real-Time Monitoring & Actions | 0/? | Not started | - |
| 141. Post-Exam & Records Validation | 0/? | Not started | - |
| 142. Edge Cases & Integration | 0/? | Not started | - |

---

## v3.16 Form Coaching GAST Redesign

**Milestone Goal:** Redesign modal form evidence coaching dan export PDF sesuai Form Coaching GAST Pertamina — tambah field Acuan di form + DB, lalu redesign PDF dengan layout 3-column table, checkbox, TTD Coach, dan branding Pertamina.

## Phases

- [ ] **Phase 143: Modal Form Evidence Acuan** - Tambah bagian Acuan (Pedoman, TKO/TKI/TKPA, Best Practice, Dokumen) ke modal form evidence coaching + DB migration + controller update
- [ ] **Phase 144: Export PDF Form GAST** - Redesign DownloadEvidencePdf dengan 3-column table layout, checkbox Kesimpulan/Result, TTD Coach + Nopeg, branding Pertamina

### Phase 143: Modal Form Evidence Acuan
**Goal**: Modal form evidence coaching memiliki bagian Acuan yang tersimpan ke database
**Depends on**: Nothing
**Requirements**: FORM-01, FORM-02, FORM-03
**Success Criteria** (what must be TRUE):
  1. Modal form menampilkan grouped card "Acuan" dengan 4 textarea (Pedoman, TKO/TKI/TKPA, Best Practice, Dokumen)
  2. CoachingSession model memiliki 4 property baru dan migration berhasil dijalankan
  3. Submit evidence menyimpan data Acuan ke database dan data tampil di Deliverable detail page
**Plans:** 1 plan

Plans:
- [ ] 143-01-PLAN.md — Add Acuan fields to model, migration, controller, modal UI, and detail display

### Phase 144: Export PDF Form GAST
**Goal**: Export PDF evidence coaching menghasilkan dokumen sesuai layout Form Coaching GAST Pertamina
**Depends on**: Phase 143 (butuh data Acuan dari DB)
**Requirements**: PDF-01, PDF-02, PDF-03, PDF-04
**Success Criteria** (what must be TRUE):
  1. PDF menggunakan layout 3-column table (Acuan / Catatan Coach / Kesimpulan dari Coach)
  2. Kesimpulan menampilkan checkbox checked dan Result menampilkan checkbox checked sesuai value
  3. TTD Coach dengan nama dan Nopeg ditampilkan (tanpa TTD Coachee)
  4. Header menampilkan Sub Kompetensi, Deliverable, Tanggal dan footer menampilkan www.pertamina.com, red wave, logo Pertamina
**Plans**: TBD

Plans:
- [ ] 144-01: TBD

## Progress

**Execution Order:** 143 → 144

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 143. Modal Form Evidence Acuan | 0/1 | Not started | - |
| 144. Export PDF Form GAST | 0/? | Not started | - |
