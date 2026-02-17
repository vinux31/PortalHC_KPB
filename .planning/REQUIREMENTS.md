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

- [ ] **PROTN-01**: Coach or SrSpv can assign a coachee to a Proton track (Panelman or Operator, Tahun 1/2/3) from Proton Main page
- [ ] **PROTN-02**: Coachee can view their full deliverable list structured by Kompetensi → Sub Kompetensi → Deliverable on the IDP Plan page (read-only — no status, no navigation links)
- [ ] **PROTN-03**: Coachee can only proceed to the next deliverable after the current one is approved by SrSpv or SectionHead
- [ ] **PROTN-04**: Coach can upload evidence files for a deliverable on the Deliverable page
- [ ] **PROTN-05**: Coach can revise evidence and resubmit a rejected deliverable
- [ ] **PROTN-06**: HC receives notification when a coachee completes all deliverables
- [ ] **PROTN-07**: HC can create and assign a final Proton Assessment to a coachee after completing all pending HC approvals
- [ ] **PROTN-08**: Coachee's Proton view shows final assessment status and resulting competency level update

### Development Dashboard

- [ ] **DASH-01**: Supervisor, SrSpv, SectionHead, HC, and Admin can access the development dashboard (coachees have no access)
- [ ] **DASH-02**: Dashboard data is filtered by role scope — Spv sees their unit only; SrSpv and SectionHead see their section; HC and Admin see all sections
- [ ] **DASH-03**: Dashboard shows team members' deliverable progress, pending approvals, and competency status
- [ ] **DASH-04**: Dashboard includes competency progress charts showing level changes over time

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

- **Master deliverable data:** Does the Kompetensi → Sub Kompetensi → Deliverable hierarchy exist in the database? (Investigate in Phase 4 planning — affects whether data import or UI management is needed)
- **CoachCoacheeMapping:** Is there an existing table linking coaches to coachees? (Relevant for Proton assignment)
- **Existing CoachingLog migration:** Current CoachingLog model has broken FK (TrackingItemId) — needs assessment before Phase 4

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| COACH-01 | Phase 4 | Pending |
| COACH-02 | Phase 4 | Pending |
| COACH-03 | Phase 4 | Pending |
| APPRV-01 | Phase 6 | Pending |
| APPRV-02 | Phase 6 | Pending |
| APPRV-03 | Phase 6 | Pending |
| APPRV-04 | Phase 6 | Pending |
| APPRV-05 | Phase 6 | Pending |
| APPRV-06 | Phase 6 | Pending |
| PROTN-01 | Phase 5 | Pending |
| PROTN-02 | Phase 5 | Pending |
| PROTN-03 | Phase 5 | Pending |
| PROTN-04 | Phase 5 | Pending |
| PROTN-05 | Phase 5 | Pending |
| PROTN-06 | Phase 6 | Pending |
| PROTN-07 | Phase 6 | Pending |
| PROTN-08 | Phase 6 | Pending |
| DASH-01 | Phase 7 | Pending |
| DASH-02 | Phase 7 | Pending |
| DASH-03 | Phase 7 | Pending |
| DASH-04 | Phase 7 | Pending |

**Coverage:**
- v1.1 requirements: 21 total
- Mapped to phases: 21
- Unmapped: 0

---
*Requirements defined: 2026-02-17*
*Last updated: 2026-02-17 — revised after clarifying Proton workflow, dashboard access, and assignment*
