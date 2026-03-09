# Requirements: Portal HC KPB — v3.15 Assessment Real Time Test

**Defined:** 2026-03-09
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.15 Requirements

Requirements for Assessment Real Time Test simulation. Each maps to roadmap phases.

### Assessment Setup

- [ ] **SETUP-01**: HC can create assessment with title, category, schedule, duration, and pass percentage without errors
- [ ] **SETUP-02**: HC can import question package with correct question/option mapping
- [ ] **SETUP-03**: HC can assign workers to assessment and workers appear in monitoring list
- [ ] **SETUP-04**: Assessment Monitoring group list shows accurate aggregate stats (participant count, completed, passed, status badge)

### Worker Exam

- [ ] **EXAM-01**: Worker can verify token and start exam successfully (both Package and Legacy paths)
- [ ] **EXAM-02**: Auto-save triggers on each answer selection and persists correctly (PackageUserResponse / UserResponse)
- [ ] **EXAM-03**: Exam Summary page shows all answered/unanswered questions before final submit
- [ ] **EXAM-04**: Worker can submit exam and receive correct score, pass/fail status
- [ ] **EXAM-05**: Session resume works on page reload — ElapsedSeconds, LastActivePage, and answers restored
- [ ] **EXAM-06**: Countdown timer displays accurately and syncs with server every 10 seconds

### HC Monitoring

- [ ] **MON-01**: HC monitoring detail shows live progress (answered/total), status, score, remaining time per worker with 10s polling
- [ ] **MON-02**: Reset action clears worker data, archives attempt history, and worker can restart exam
- [ ] **MON-03**: Force Close action marks session as Completed with score 0 and fail status
- [ ] **MON-04**: Close Early action auto-scores current answers and completes all InProgress sessions
- [ ] **MON-05**: Force Close All action bulk-closes all Open/InProgress sessions in group
- [ ] **MON-06**: Regenerate Token generates new token for all sibling sessions

### Post-Exam

- [ ] **POST-01**: Results page shows correct score, pass/fail, and competency earned after submission
- [ ] **POST-02**: Competency auto-update fires correctly after passing assessment (AssessmentCompetencyMap)
- [ ] **POST-03**: Notifications sent to correct users after assessment completion
- [ ] **POST-04**: Assessment Records (Riwayat Assessment tab) shows completed assessment with accurate data
- [ ] **POST-05**: Attempt History preserves previous attempts after Reset with correct Attempt # numbering

### Edge Cases

- [ ] **EDGE-01**: Server-side timer enforcement rejects submission after DurationMinutes + 2min grace period
- [ ] **EDGE-02**: Exam window close date blocks exam entry when expired
- [ ] **EDGE-03**: Stale question detection clears progress when question count changed mid-exam
- [ ] **EDGE-04**: All HC actions (Reset, ForceClose, CloseEarly, RegenerateToken) logged in AuditLog with actor, timestamp, details
- [ ] **EDGE-05**: Worker redirect works when HC triggers CloseEarly (CheckExamStatus detects closed state)

## Out of Scope

| Feature | Reason |
|---------|--------|
| SignalR/WebSocket upgrade | This milestone tests existing polling system, not upgrading it |
| New assessment features | QA simulation only, no new functionality |
| Load/stress testing | Focus on functional correctness, not performance |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SETUP-01 | Phase 138 | Pending |
| SETUP-02 | Phase 138 | Pending |
| SETUP-03 | Phase 138 | Pending |
| SETUP-04 | Phase 138 | Pending |
| EXAM-01 | Phase 139 | Pending |
| EXAM-02 | Phase 139 | Pending |
| EXAM-03 | Phase 139 | Pending |
| EXAM-04 | Phase 139 | Pending |
| EXAM-05 | Phase 139 | Pending |
| EXAM-06 | Phase 139 | Pending |
| MON-01 | Phase 140 | Pending |
| MON-02 | Phase 140 | Pending |
| MON-03 | Phase 140 | Pending |
| MON-04 | Phase 140 | Pending |
| MON-05 | Phase 140 | Pending |
| MON-06 | Phase 140 | Pending |
| POST-01 | Phase 141 | Pending |
| POST-02 | Phase 141 | Pending |
| POST-03 | Phase 141 | Pending |
| POST-04 | Phase 141 | Pending |
| POST-05 | Phase 141 | Pending |
| EDGE-01 | Phase 142 | Pending |
| EDGE-02 | Phase 142 | Pending |
| EDGE-03 | Phase 142 | Pending |
| EDGE-04 | Phase 142 | Pending |
| EDGE-05 | Phase 142 | Pending |

**Coverage:**
- v3.15 requirements: 26 total
- Mapped to phases: 26
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-09*
*Last updated: 2026-03-09 after initial definition*
