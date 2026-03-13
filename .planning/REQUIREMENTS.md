# Requirements: Portal HC KPB

**Defined:** 2026-03-13
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v4.2 Requirements

Requirements for Real-time Assessment milestone. Each maps to roadmap phases.

### Infrastructure

- [ ] **INFRA-01**: SignalR Hub class registered with `AddSignalR()` and `MapHub` in Program.cs
- [ ] **INFRA-02**: `@microsoft/signalr@8.x` JS client library installed in wwwroot
- [ ] **INFRA-03**: Cookie auth configured to return 401 (not 302 redirect) on SignalR negotiate endpoint
- [ ] **INFRA-04**: SQLite WAL mode enabled on application startup to prevent concurrent write locks
- [ ] **INFRA-05**: Client-side reconnect handling re-joins SignalR groups after connection restore

### HC → Worker Push

- [ ] **PUSH-01**: Worker exam page updates instantly when HC resets their session
- [ ] **PUSH-02**: Worker exam page redirects to results when HC force-closes their session
- [ ] **PUSH-03**: All workers in a batch are notified when HC closes the exam early (CloseEarly)
- [ ] **PUSH-05**: All workers in a batch are notified when HC force-closes all sessions (ForceCloseAll)

### Worker → HC Push

- [ ] **MONITOR-01**: HC monitoring page shows real-time answer progress without polling
- [ ] **MONITOR-02**: HC monitoring page instantly shows when a worker completes their exam
- [ ] **MONITOR-03**: HC monitoring page instantly shows when a worker starts their exam

### Cleanup

- [ ] **CLEAN-01**: Polling code (setInterval) removed from monitoring and exam pages after SignalR proven working

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
| INFRA-01 | Pending | Pending |
| INFRA-02 | Pending | Pending |
| INFRA-03 | Pending | Pending |
| INFRA-04 | Pending | Pending |
| INFRA-05 | Pending | Pending |
| PUSH-01 | Pending | Pending |
| PUSH-02 | Pending | Pending |
| PUSH-03 | Pending | Pending |
| PUSH-05 | Pending | Pending |
| MONITOR-01 | Pending | Pending |
| MONITOR-02 | Pending | Pending |
| MONITOR-03 | Pending | Pending |
| CLEAN-01 | Pending | Pending |

**Coverage:**
- v4.2 requirements: 13 total
- Mapped to phases: 0
- Unmapped: 13 ⚠️

---
*Requirements defined: 2026-03-13*
*Last updated: 2026-03-13 after initial definition*
