# Requirements: Portal HC KPB

**Defined:** 2026-02-17
**Milestone:** v1.1 CDP Coaching Management (Proton)
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v1.1 Requirements

### Coaching Sessions

- [ ] **COACH-01**: Coach can log a coaching session with date, topic, and notes
- [ ] **COACH-02**: Coach can add action items to a coaching session with due dates
- [ ] **COACH-03**: User can view their coaching session history with date and status filtering

### Approval Workflow

- [ ] **APPRV-01**: Coach can submit a deliverable for approval
- [ ] **APPRV-02**: SrSpv can approve or reject a submitted deliverable
- [ ] **APPRV-03**: SectionHead can approve or reject a submitted deliverable (independent of SrSpv — either one approving is sufficient to proceed)
- [ ] **APPRV-04**: HC receives and reviews deliverables (formality — non-blocking per deliverable, but all HC approvals must be complete before final assessment can be created)
- [ ] **APPRV-05**: Approver can reject a deliverable with a written rejection reason
- [ ] **APPRV-06**: Coach and coachee can see rejection status and reason when a deliverable is rejected

### Proton Progress Tracking

- [ ] **PROTN-01**: Coachee can view their assigned deliverables structured by Kompetensi → Sub Kompetensi → Deliverable
- [ ] **PROTN-02**: Coachee can only proceed to the next deliverable after the current one is approved by SrSpv or SectionHead
- [ ] **PROTN-03**: Coach can upload evidence files for a deliverable
- [ ] **PROTN-04**: Coach can revise evidence and resubmit a rejected deliverable
- [ ] **PROTN-05**: HC receives notification when a coachee completes all deliverables
- [ ] **PROTN-06**: HC can create and assign a final Proton Assessment to a coachee after completing all pending HC approvals
- [ ] **PROTN-07**: Coachee's Proton view shows final assessment status and resulting competency level update

### Development Dashboard

- [ ] **DASH-01**: User can view personal dashboard showing competency progress, current deliverable status, and active action items
- [ ] **DASH-02**: Supervisor can view team dashboard showing each team member's progress and pending approvals
- [ ] **DASH-03**: Dashboard includes competency progress charts showing level changes over time

## v2 Requirements

### Notifications

- **NOTF-01**: Coach and coachee receive email notification on deliverable rejection
- **NOTF-02**: HC receives email notification when coachee completes all deliverables
- **NOTF-03**: Approvers receive email reminders for pending approvals

### UX Enhancements

- **UX-01**: Session templates for common coaching scenarios
- **UX-02**: Auto-suggest IDP actions based on competency gaps
- **UX-03**: Mobile-responsive design optimizations

## Out of Scope

| Feature | Reason |
|---------|--------|
| Calendar integration | High complexity OAuth, defer to v2+ |
| AI-generated coaching suggestions | Requires LLM integration, significant stack change |
| Real-time notifications (SignalR) | In-page polling sufficient for v1.1 |
| Mobile app | Web-only |
| Bulk deliverable assignment | Manage individually for v1.1, automate later |

## Open Items (Need Investigation)

- **Master deliverable data:** Does the Kompetensi → Sub Kompetensi → Deliverable hierarchy exist in the database? (Investigate in Phase 1 planning — affects whether data import or UI management is needed)
- **CoachCoacheeMapping:** Is there an existing table linking coaches to coachees? (Relevant for Proton assignment)
- **Existing CoachingLog migration:** Current CoachingLog model has broken FK (TrackingItemId) — needs assessment before Phase 1

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| COACH-01 | — | Pending |
| COACH-02 | — | Pending |
| COACH-03 | — | Pending |
| APPRV-01 | — | Pending |
| APPRV-02 | — | Pending |
| APPRV-03 | — | Pending |
| APPRV-04 | — | Pending |
| APPRV-05 | — | Pending |
| APPRV-06 | — | Pending |
| PROTN-01 | — | Pending |
| PROTN-02 | — | Pending |
| PROTN-03 | — | Pending |
| PROTN-04 | — | Pending |
| PROTN-05 | — | Pending |
| PROTN-06 | — | Pending |
| PROTN-07 | — | Pending |
| DASH-01 | — | Pending |
| DASH-02 | — | Pending |
| DASH-03 | — | Pending |

**Coverage:**
- v1.1 requirements: 19 total
- Mapped to phases: 0 (pending roadmap)
- Unmapped: 19 ⚠️

---
*Requirements defined: 2026-02-17*
*Last updated: 2026-02-17 after initial definition*
