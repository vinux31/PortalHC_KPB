# Requirements: Portal HC KPB

**Defined:** 2026-03-13
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v4.2 Requirements

Requirements for Real-time Assessment milestone. Each maps to roadmap phases.

### Close Action Simplification

- [x] **CLOSE-01**: "Akhiri Ujian" (individual) auto-grades saved answers and sets real score — not hardcoded 0
- [x] **CLOSE-02**: "Akhiri Semua" (group) auto-grades all InProgress workers from saved answers
- [x] **CLOSE-03**: Workers who haven't started get status "Cancelled" (not "Completed" or "Abandoned") on Akhiri Semua
- [x] **CLOSE-04**: Old ForceClose, ForceCloseAll, CloseEarly actions removed — replaced by 2 new actions

### Infrastructure

- [x] **INFRA-01**: SignalR Hub class registered with `AddSignalR()` and `MapHub` in Program.cs
- [x] **INFRA-02**: `@microsoft/signalr@8.x` JS client library installed in wwwroot
- [x] **INFRA-03**: Cookie auth configured to return 401 (not 302 redirect) on SignalR negotiate endpoint
- [x] **INFRA-04**: SQLite WAL mode enabled on application startup to prevent concurrent write locks
- [x] **INFRA-05**: Client-side reconnect handling re-joins SignalR groups after connection restore

### HC → Worker Push

- [x] **PUSH-01**: Worker exam page updates instantly when HC resets their session
- [x] **PUSH-02**: Worker exam page redirects to results when HC closes their session (Akhiri Ujian)
- [x] **PUSH-03**: All workers in a batch are notified when HC closes all sessions (Akhiri Semua)

### Worker → HC Push + Cleanup

- [x] **MONITOR-01**: HC monitoring page shows real-time answer progress without polling
- [x] **MONITOR-02**: HC monitoring page instantly shows when a worker completes their exam
- [x] **MONITOR-03**: HC monitoring page instantly shows when a worker starts their exam
- [ ] **CLEAN-01**: Polling code (setInterval) removed from monitoring and exam pages after SignalR proven working

### Activity Log (Opsional)

- [x] **LOG-01**: Per-worker activity timeline stored server-side (question opens, answers, page nav, disconnect/reconnect)
- [x] **LOG-02**: HC can view activity log from monitoring detail page

### Item Analysis (Opsional)

- [ ] **STATS-01**: Per-question difficulty percentage and answer distribution displayed after assessment completes
- [ ] **STATS-02**: Questions ranked by difficulty for HC to identify problematic questions

## Future Requirements

### Notifications
- **NOTIF-01**: In-app notification bell with real-time push via SignalR
- **NOTIF-02**: Coaching Proton approval notifications

## Out of Scope

| Feature | Reason |
|---------|--------|
| Per-answer content push | Sends answer data over WebSocket — security risk, no value to HC |
| Real-time chat | High complexity, not core to assessment |
| Proctoring/screen monitoring | Out of scope for this portal |
| OS/browser notifications | Requires extra permission UX, SignalR in-page push sufficient |
| Replacing auto-save with SignalR | HTTP auto-save is reliable and proven; SignalR is for notifications only |
| SignalR for non-assessment features | Scope limited to assessment/exam flow per user decision |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| CLOSE-01 | Phase 162 | Complete |
| CLOSE-02 | Phase 162 | Complete |
| CLOSE-03 | Phase 162 | Complete |
| CLOSE-04 | Phase 162 | Complete |
| INFRA-01 | Phase 163 | Complete |
| INFRA-02 | Phase 163 | Complete |
| INFRA-03 | Phase 163 | Complete |
| INFRA-04 | Phase 163 | Complete |
| INFRA-05 | Phase 163 | Complete |
| PUSH-01 | Phase 164 | Complete |
| PUSH-02 | Phase 164 | Complete |
| PUSH-03 | Phase 164 | Complete |
| MONITOR-01 | Phase 165 | Complete |
| MONITOR-02 | Phase 165 | Complete |
| MONITOR-03 | Phase 165 | Complete |
| CLEAN-01 | Phase 165 | Pending |
| LOG-01 | Phase 166 | Complete |
| LOG-02 | Phase 166 | Complete |
| STATS-01 | Phase 167 | Pending |
| STATS-02 | Phase 167 | Pending |

**Coverage:**
- v4.2 requirements: 20 total (16 core + 4 opsional)
- Mapped to phases: 20
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-13*
*Last updated: 2026-03-13 — revised with close action simplification, activity log, and item analysis*
