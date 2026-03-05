# Requirements: Portal HC KPB

**Defined:** 2026-03-05
**Milestone:** v3.3 Basic Notifications
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.3 Requirements

Requirements for basic in-app notification system. Each maps to roadmap phases.

### Notification Infrastructure

- [ ] **INFRA-01**: System stores notifications persistently in database with proper indexing for performance
- [ ] **INFRA-02**: NotificationService follows AuditLogService pattern (async, scoped DI, try-catch wrapped)
- [ ] **INFRA-03**: Notification Center UI displays bell icon in navbar with unread badge counter
- [ ] **INFRA-04**: Notification dropdown shows list of notifications (most recent first, pagination)
- [ ] **INFRA-05**: Users can mark individual notifications as read (click notification → mark read → remove from counter)
- [ ] **INFRA-06**: Users can mark all notifications as read (bulk action button)
- [ ] **INFRA-07**: System tracks notification audit trail (created by, created at, read at, delivery status)
- [ ] **INFRA-08**: Notification templates provide consistent messaging across all triggers
- [ ] **INFRA-09**: Notification failures gracefully degrade (try-catch prevents main workflow crashes)
- [ ] **INFRA-10**: Notifications use deep linking (click notification → navigate to relevant page)

### Assessment Notifications

- [ ] **ASMT-01**: Worker receives notification when assessment is assigned
- [ ] **ASMT-02**: Worker receives notification when assessment results are ready

### Coaching Proton Notifications

- [ ] **COACH-01**: Coachee receives notification when coach is assigned
- [ ] **COACH-02**: SrSpv receives notification when coach uploads evidence for review
- [ ] **COACH-03**: Coach receives notification when evidence is rejected
- [ ] **COACH-04**: SectionHead receives notification when evidence is approved by SrSpv
- [ ] **COACH-05**: HC receives notification when evidence is approved by SectionHead
- [ ] **COACH-06**: Coachee receives notification when coaching session is completed

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Assessment (Deferred)

- **ASMT-03**: Worker receives notification when assessment is submitted (to HC/Admin) — defer to v3.4
- **ASMT-04**: Worker receives deadline reminder (1 day before due date) — defer to v3.4 (requires background job)

### Notification Preferences (Deferred)

- **PREF-01**: Users can configure notification preferences (enable/disable per type) — defer to v3.4
- **PREF-02**: Users can set quiet hours/do not disturb — defer to v3.5

### Real-Time Notifications (Deferred)

- **REAL-01**: Real-time push notifications via SignalR — defer to v3.4
- **REAL-02**: Browser push notifications — defer to v3.5

### Email Notifications (Deferred)

- **EMAIL-01**: Notification delivery via email — defer to v3.5

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| SMS Notifications | Overkill for internal HR portal; costs money |
| Notification Grouping/Batching | Unnecessary with low volume (8 triggers); one notification per event |
| Notification Search | Over-engineering for basic system; pagination + filtering sufficient |
| Rich Media Notifications (Images/Actions) | Text + link is sufficient for v3.3 |
| Notification Snooze | Unnecessary complexity; notifications are time-sensitive |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01 | Phase 99 | Pending |
| INFRA-02 | Phase 99 | Pending |
| INFRA-03 | Phase 100 | Pending |
| INFRA-04 | Phase 100 | Pending |
| INFRA-05 | Phase 100 | Pending |
| INFRA-06 | Phase 100 | Pending |
| INFRA-07 | Phase 99 | Pending |
| INFRA-08 | Phase 99 | Pending |
| INFRA-09 | Phase 99 | Pending |
| INFRA-10 | Phase 100 | Pending |
| ASMT-01 | Phase 101 | Pending |
| ASMT-02 | Phase 101 | Pending |
| COACH-01 | Phase 102 | Pending |
| COACH-02 | Phase 102 | Pending |
| COACH-03 | Phase 102 | Pending |
| COACH-04 | Phase 102 | Pending |
| COACH-05 | Phase 102 | Pending |
| COACH-06 | Phase 102 | Pending |

**Coverage:**
- v3.3 requirements: 18 total
- Mapped to phases: 18
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-05*
*Last updated: 2026-03-05 after requirements definition*
