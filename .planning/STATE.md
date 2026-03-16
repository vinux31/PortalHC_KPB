---
gsd_state_version: 1.0
milestone: v5.0
milestone_name: milestone
status: not_started
stopped_at: Completed 171-02-PLAN.md
last_updated: "2026-03-16T01:58:34.304Z"
last_activity: 2026-03-16 — Milestone v5.0 started
progress:
  total_phases: 2
  completed_phases: 1
  total_plans: 2
  completed_plans: 2
---

---
gsd_state_version: 1.0
milestone: v5.0
milestone_name: Guide Page Overhaul
status: not_started
stopped_at: Milestone defined, ready to plan Phase 171
last_updated: "2026-03-16"
last_activity: "2026-03-16 — Milestone v5.0 started"
progress:
  total_phases: 4
  completed_phases: 0
  total_plans: 0
  completed_plans: 0
  percent: 0
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-16)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** v5.0 Guide Page Overhaul — Phase 171 next

## Current Position

Phase: Not started (defining requirements)
Plan: —
Status: Defining requirements
Last activity: 2026-03-16 — Milestone v5.0 started

## Accumulated Context

### Decisions

(Carried forward)
- SignalR Hub methods handle group join/leave only — no DB writes inside Hub methods ever
- DB write always happens before SignalR push; SignalR is notifications-only, not state source
- Silent catch blocks must log at Warning level — bare catch without logging is forbidden in all controllers
- Json.Serialize() is the canonical pattern for JS string contexts (not Html.Raw with Replace)
- All file uploads must have extension allowlists and size limits
- [Phase 171-guide-faq-cleanup]: CMP accordion reduced to 4 items (5 for Admin/HC) — assessment/results/certificate items removed as covered by PDF tutorial
- [Phase 171-guide-faq-cleanup]: CDP 5 (Approve/Reject Deliverable) gated to Admin/HC only
- [Phase 171-guide-faq-cleanup]: Tutorial card CSS uses variant modifier pattern (guide-tutorial-card--cmp/cdp/admin) replacing inline styles
- [Phase 171-guide-faq-cleanup]: FAQ category order: Akun & Login, Assessment, CDP & Coaching, Umum, KKJ & CPDP, Admin & Kelola Data — Umum moved before KKJ as more universally applicable
- [Phase 171-guide-faq-cleanup]: Removed step-by-step FAQ items covered by PDF tutorials; kept conceptual/policy FAQ items

### Pre-milestone Work (this session)

- Created 2 HTML tutorial files: Panduan-Lengkap-Assessment.html and Panduan-Lengkap-Coaching-Proton.html in wwwroot/documents/guides/
- Integrated tutorial cards into GuideDetail.cshtml (CMP and CDP modules)

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-03-16T01:58:24.782Z
Stopped at: Completed 171-02-PLAN.md
Resume file: None
