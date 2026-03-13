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

**Milestone Goal:** Add SignalR-based real-time communication to assessment/exam flow so HC actions (Reset, ForceClose) push instantly to worker exam pages and worker progress pushes to HC monitoring — replacing 10-second polling.

### Phases

- [ ] **Phase 162: Hub Infrastructure & Safety Foundations** - SignalR Hub, JS client, auth config, SQLite WAL mode, race condition guards, reconnect handling
- [ ] **Phase 163: HC-to-Worker Push Events** - Reset and ForceClose push instantly to targeted worker; ForceCloseAll and CloseEarly broadcast to all workers in batch
- [ ] **Phase 164: Worker-to-HC Progress Push** - HC monitoring page receives real-time answer progress, exam start, and exam completion events without polling
- [ ] **Phase 165: Cleanup & Polling Removal** - Remove setInterval polling from both views after SignalR confirmed working in UAT

## Phase Details

### Phase 162: Hub Infrastructure & Safety Foundations
**Goal**: A working, authenticated SignalR endpoint at `/hubs/assessment` with SQLite concurrency protection and reconnect-safe group membership — the prerequisite for all real-time features
**Depends on**: Phase 161
**Requirements**: INFRA-01, INFRA-02, INFRA-03, INFRA-04, INFRA-05
**Success Criteria** (what must be TRUE):
  1. `/hubs/assessment/negotiate` returns a WebSocket upgrade (101) or JSON token response (200), not a 302 redirect or HTML login page, when checked in browser DevTools Network tab while logged in
  2. `PRAGMA journal_mode;` query on the running database returns `wal`, confirming SQLite WAL mode is active
  3. Simulating browser disconnect then reconnect (DevTools offline/online toggle) causes the JS client to re-join its SignalR group automatically — no manual page reload required
  4. `ForceCloseAssessment` and `SubmitExam` both use status-guarded `ExecuteUpdateAsync` so a worker who submits in the same moment HC force-closes does not have their score silently overwritten
  5. No `Dictionary<userId, connectionId>` exists in the codebase — per-worker targeting uses `Clients.User()` or named groups only
**Plans**: TBD

### Phase 163: HC-to-Worker Push Events
**Goal**: HC Reset and ForceClose actions reach the worker's exam page in sub-second time, eliminating the 10-second window where workers answer questions against a locked/zeroed session
**Depends on**: Phase 162
**Requirements**: PUSH-01, PUSH-02, PUSH-03, PUSH-05
**Success Criteria** (what must be TRUE):
  1. HC clicks Reset on a worker's session; that worker's exam page shows a "Sesi direset oleh HC" modal within 1 second — without any page polling interval completing
  2. HC clicks ForceClose on a worker's session; that worker's exam page shows a countdown modal and redirects to results within 1 second of the HC action
  3. HC clicks Close Early on an assessment; all workers currently on StartExam receive the `examWindowClosed` event and are redirected within 1 second
  4. HC clicks ForceClose All; every active worker in the batch receives the force-close event simultaneously within 1 second
  5. A connection status badge ("Live" / "Reconnecting...") is visible on the worker exam page and accurately reflects the SignalR connection state
**Plans**: TBD

### Phase 164: Worker-to-HC Progress Push
**Goal**: The HC monitoring page shows worker exam progress, start events, and submission events in near-real-time without polling — HC situational awareness is continuous, not batched at 10-second intervals
**Depends on**: Phase 163
**Requirements**: MONITOR-01, MONITOR-02, MONITOR-03
**Success Criteria** (what must be TRUE):
  1. When a worker answers questions, the HC monitoring detail page updates that worker's answered-count and progress bar within 1-2 seconds — without a polling interval completing
  2. When a worker submits their exam, the HC monitoring page immediately shows their status as "Selesai" and their score — not up to 10 seconds later
  3. When a worker opens their exam for the first time, the HC monitoring page immediately shows their status as "Dalam Pengerjaan"
  4. A connection status badge ("Live" / "Reconnecting...") is visible on the HC monitoring page and re-joins the monitor group automatically after reconnect
**Plans**: TBD

### Phase 165: Cleanup & Polling Removal
**Goal**: The codebase has one refresh mechanism (SignalR push) for assessment status data — polling setInterval calls are removed after UAT confirms real-time push is stable
**Depends on**: Phase 164
**Requirements**: CLEAN-01
**Success Criteria** (what must be TRUE):
  1. No `setInterval` calls related to exam status checking or monitoring progress remain in `StartExam.cshtml` or `AssessmentMonitoringDetail.cshtml`
  2. The worker exam page and HC monitoring page continue to update correctly in real-time after polling removal — SignalR push is the sole update mechanism
  3. The legacy polling endpoints (`CheckExamStatus`, `GetMonitoringProgress`) are either removed or clearly marked as deprecated with no JS callers in the views
**Plans**: TBD

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 162. Hub Infrastructure & Safety Foundations | v4.2 | 0/TBD | Not started | - |
| 163. HC-to-Worker Push Events | v4.2 | 0/TBD | Not started | - |
| 164. Worker-to-HC Progress Push | v4.2 | 0/TBD | Not started | - |
| 165. Cleanup & Polling Removal | v4.2 | 0/TBD | Not started | - |
