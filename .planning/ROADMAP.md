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

---

## v4.2 Real-time Assessment (In Progress)

**Milestone Goal:** Simplify assessment close actions (3→2 with auto-grading), add SignalR-based real-time communication so HC monitoring updates live and HC actions push instantly to workers, then add activity logging and item analysis for post-assessment insights.

### Phases

- [x] **Phase 162: Simplifikasi Action Close + Auto-Grade** - Merge 3 close actions into 2 consistent actions that auto-grade saved answers (completed 2026-03-13)
- [x] **Phase 163: Hub Infrastructure & Safety Foundations** - SignalR Hub, JS client, auth config, SQLite WAL mode, race condition guards, reconnect handling (completed 2026-03-13)
- [x] **Phase 164: HC-to-Worker Push Events** - Reset and Akhiri Ujian push instantly to targeted worker; Akhiri Semua broadcasts to all workers in batch (completed 2026-03-13)
- [ ] **Phase 165: Worker-to-HC Progress Push + Polling Removal** - HC monitoring receives real-time progress events; remove legacy setInterval polling after confirmed working
- [ ] **Phase 166: Activity Log Per-Worker** - Track per-question timestamps, page navigation, disconnect events for HC audit trail (opsional)
- [ ] **Phase 167: Item Analysis / Statistik Per-Soal** - Difficulty index, discrimination index, answer distribution per question (opsional)

## Phase Details

### Phase 162: Simplifikasi Action Close + Auto-Grade
**Goal**: Replace 3 inconsistent close actions (ForceClose=score 0, ForceCloseAll=Abandoned, CloseEarly=partial grade) with 2 consistent actions that always auto-grade saved answers — matching industry standard behavior (Exam.net, Canvas)
**Depends on**: Phase 161
**Requirements**: CLOSE-01, CLOSE-02, CLOSE-03, CLOSE-04
**Success Criteria** (what must be TRUE):
  1. HC clicks "Akhiri Ujian" on an InProgress worker; the worker's saved answers are graded and a real score/pass-fail is assigned — not hardcoded 0
  2. HC clicks "Akhiri Semua" on a group; all InProgress workers get auto-graded from saved answers, all Open/Not-started workers get status "Cancelled" (not "Completed" or "Abandoned")
  3. The old ForceClose, ForceCloseAll, and CloseEarly actions no longer exist in the codebase — replaced by the 2 new actions
  4. Worker polling (CheckExamStatus) still detects the new close actions and redirects correctly
**Plans:** 2/2 plans complete
Plans:
- [ ] 162-01-PLAN.md — Backend: extract shared grading, create AkhiriUjian + AkhiriSemuaUjian, remove old actions, handle Cancelled status
- [ ] 162-02-PLAN.md — Frontend: update monitoring detail buttons/modals, add worker close notification modal

### Phase 163: Hub Infrastructure & Safety Foundations
**Goal**: A working, authenticated SignalR endpoint at `/hubs/assessment` with SQLite concurrency protection and reconnect-safe group membership — the prerequisite for all real-time features
**Depends on**: Phase 162
**Requirements**: INFRA-01, INFRA-02, INFRA-03, INFRA-04, INFRA-05
**Success Criteria** (what must be TRUE):
  1. `/hubs/assessment/negotiate` returns a WebSocket upgrade (101) or JSON token response (200), not a 302 redirect or HTML login page, when checked in browser DevTools Network tab while logged in
  2. `PRAGMA journal_mode;` query on the running database returns `wal`, confirming SQLite WAL mode is active
  3. Simulating browser disconnect then reconnect (DevTools offline/online toggle) causes the JS client to re-join its SignalR group automatically — no manual page reload required
  4. `AkhiriUjian` and `SubmitExam` both use status-guarded `ExecuteUpdateAsync` so a worker who submits in the same moment HC closes does not have their score silently overwritten
  5. No `Dictionary<userId, connectionId>` exists in the codebase — per-worker targeting uses `Clients.User()` or named groups only
**Plans:** 2/2 plans complete
Plans:
- [ ] 163-01-PLAN.md — SignalR Hub + Program.cs config + vendored JS client + assessment-hub.js reconnect module + page wiring
- [ ] 163-02-PLAN.md — Race condition guards on AkhiriUjian, SubmitExam, ResetAssessment, SaveAnswer

### Phase 164: HC-to-Worker Push Events
**Goal**: HC Akhiri Ujian and Reset actions reach the worker's exam page in sub-second time, eliminating the 10-second window where workers answer questions against a closed session
**Depends on**: Phase 163
**Requirements**: PUSH-01, PUSH-02, PUSH-03
**Success Criteria** (what must be TRUE):
  1. HC clicks Reset on a worker's session; that worker's exam page shows a "Sesi direset oleh HC" modal within 1 second — without any page polling interval completing
  2. HC clicks "Akhiri Ujian" on a worker's session; that worker's exam page shows a countdown modal and redirects to results within 1 second
  3. HC clicks "Akhiri Semua" on an assessment; all workers currently on StartExam receive the close event and are redirected within 1 second
  4. A connection status badge ("Live" / "Reconnecting...") is visible on the worker exam page and accurately reflects the SignalR connection state
**Plans:** 2/2 plans complete
Plans:
- [ ] 164-01-PLAN.md — Backend: inject IHubContext, add SendAsync to Reset/AkhiriUjian/AkhiriSemuaUjian
- [ ] 164-02-PLAN.md — Frontend: push event handlers, reset modal, dynamic text, connection badges

### Phase 165: Worker-to-HC Progress Push + Polling Removal
**Goal**: The HC monitoring page shows worker exam progress, start events, and submission events in near-real-time via SignalR push — then legacy polling is removed after UAT confirms stability
**Depends on**: Phase 164
**Requirements**: MONITOR-01, MONITOR-02, MONITOR-03, CLEAN-01
**Success Criteria** (what must be TRUE):
  1. When a worker answers questions, the HC monitoring detail page updates that worker's answered-count and progress bar within 1-2 seconds — without a polling interval completing
  2. When a worker submits their exam, the HC monitoring page immediately shows their status as "Selesai" and their score
  3. When a worker opens their exam for the first time, the HC monitoring page immediately shows their status as "Dalam Pengerjaan"
  4. A connection status badge ("Live" / "Reconnecting...") is visible on the HC monitoring page and re-joins the monitor group automatically after reconnect
  5. No `setInterval` calls related to exam status checking or monitoring progress remain in `StartExam.cshtml` or `AssessmentMonitoringDetail.cshtml`
  6. The legacy polling endpoints (`CheckExamStatus`, `GetMonitoringProgress`) are either removed or clearly marked as deprecated with no JS callers
**Plans:** 2 plans
Plans:
- [ ] 165-01-PLAN.md — Backend: IHubContext in CMPController + 3 push calls + frontend event handlers, row flash, toast
- [ ] 165-02-PLAN.md — UAT checkpoint + polling removal (CheckExamStatus, GetMonitoringProgress)

### Phase 166: Activity Log Per-Worker
**Goal**: HC can view a detailed timeline of each worker's exam activity — question opens, answer changes, page navigation, disconnect/reconnect events — for audit and fairness purposes
**Depends on**: Phase 165
**Requirements**: LOG-01, LOG-02
**Success Criteria** (what must be TRUE):
  1. HC clicks a worker row in monitoring detail and sees a chronological activity log with timestamps for each event (start, answer, page change, disconnect, reconnect, submit)
  2. Activity data is stored server-side and survives browser refresh — not client-only
**Plans:** 2 plans
Plans:
- [ ] 164-01-PLAN.md — Backend: inject IHubContext, add SendAsync to Reset/AkhiriUjian/AkhiriSemuaUjian
- [ ] 164-02-PLAN.md — Frontend: push event handlers, reset modal, dynamic text, connection badges

### Phase 167: Item Analysis / Statistik Per-Soal
**Goal**: After an assessment group completes, HC can view per-question statistics — difficulty index, discrimination index, answer distribution — to evaluate question quality
**Depends on**: Phase 165
**Requirements**: STATS-01, STATS-02
**Success Criteria** (what must be TRUE):
  1. HC monitoring detail page shows a "Statistik Soal" section with per-question difficulty percentage and answer distribution chart
  2. Questions are ranked by difficulty so HC can identify problematic questions
**Plans:** 2 plans
Plans:
- [ ] 164-01-PLAN.md — Backend: inject IHubContext, add SendAsync to Reset/AkhiriUjian/AkhiriSemuaUjian
- [ ] 164-02-PLAN.md — Frontend: push event handlers, reset modal, dynamic text, connection badges

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 162. Simplifikasi Action Close + Auto-Grade | 2/2 | Complete    | 2026-03-13 | - |
| 163. Hub Infrastructure & Safety Foundations | 2/2 | Complete    | 2026-03-13 | - |
| 164. HC-to-Worker Push Events | 2/2 | Complete   | 2026-03-13 | - |
| 165. Worker-to-HC Progress Push + Polling Removal | v4.2 | 0/2 | Not started | - |
| 166. Activity Log Per-Worker | v4.2 | 0/TBD | Not started | - |
| 167. Item Analysis / Statistik Per-Soal | v4.2 | 0/TBD | Not started | - |
