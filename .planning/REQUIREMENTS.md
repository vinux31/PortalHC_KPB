# Requirements: Portal HC KPB

**Defined:** 2026-02-24
**Milestone:** v2.1 Assessment Resilience & Real-Time Monitoring
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v2.1 Requirements

Requirements for v2.1 milestone. Each maps to roadmap phases.

### Auto-Save

- [ ] **SAVE-01**: Worker's answer is automatically saved to the server on each radio button selection (no manual save needed)
- [ ] **SAVE-02**: All current-page answers are saved before navigating to a different page (Prev/Next)
- [ ] **SAVE-03**: Worker's answer per question is preserved correctly even under concurrent save requests

### Session Resume

- [ ] **RESUME-01**: Worker reconnecting to an in-progress exam resumes at the last page they were on
- [ ] **RESUME-02**: Worker's remaining exam time is accurately restored on reconnect (server-calculated, not client timer)
- [ ] **RESUME-03**: Previously answered questions are pre-selected when the worker resumes their exam

### Worker Polling

- [ ] **POLL-01**: Exam page automatically detects when HC closes the session early (polls every 10 seconds)
- [ ] **POLL-02**: Worker is auto-redirected to their Results page when session closure is detected

### HC Live Monitoring

- [ ] **MONITOR-01**: HC sees an "Answered / Total" progress count per worker on MonitoringDetail
- [ ] **MONITOR-02**: Worker progress table updates automatically every 5-10 seconds without page reload
- [ ] **MONITOR-03**: NIP column is removed from the MonitoringDetail per-user status table

## Future Requirements

Deferred to v2.2 or later.

### Concurrency Hardening

- **CONCUR-01**: Simultaneous SubmitExam + CloseEarly is handled without data loss (RowVersion / DbUpdateConcurrencyException pattern)

### Worker Self-Service Training Records

- **WTRN-01**: Worker can submit training record for HC approval (carried from v2.0 tech debt)
- **WTRN-02**: Worker receives notification when training record is approved or rejected

## Out of Scope

| Feature | Reason |
|---------|--------|
| WebSockets / SignalR | Polling is sufficient for 100-500 concurrent users; real-time push adds complexity |
| Heartbeat / liveness endpoint | Edge case — browser crash detection deferred to v2.2 |
| Pause/resume with explicit lock | Exams auto-resume on return; no explicit pause UI needed |
| Close Early concurrency hardening | Race condition edge case; deferred to v2.2 (CONCUR-01) |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| SAVE-01 | — | Pending |
| SAVE-02 | — | Pending |
| SAVE-03 | — | Pending |
| RESUME-01 | — | Pending |
| RESUME-02 | — | Pending |
| RESUME-03 | — | Pending |
| POLL-01 | — | Pending |
| POLL-02 | — | Pending |
| MONITOR-01 | — | Pending |
| MONITOR-02 | — | Pending |
| MONITOR-03 | — | Pending |

**Coverage:**
- v2.1 requirements: 11 total
- Mapped to phases: 0 (pre-roadmap)
- Unmapped: 11 ⚠️

---
*Requirements defined: 2026-02-24*
*Last updated: 2026-02-24 after v2.1 milestone initialization*
