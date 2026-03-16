---
gsd_state_version: 1.0
milestone: v6.0
milestone_name: Deployment Preparation
status: defining_requirements
stopped_at: null
last_updated: "2026-03-16"
last_activity: "2026-03-16 — Milestone v6.0 started"
progress:
  total_phases: 0
  completed_phases: 0
  total_plans: 0
  completed_plans: 0
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-16)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** v6.0 Deployment Preparation

## Current Position

Phase: Not started (defining requirements)
Plan: —
Status: Defining requirements
Last activity: 2026-03-16 — Milestone v6.0 started

## Accumulated Context

### Decisions

(Carried forward)
- SignalR Hub methods handle group join/leave only — no DB writes inside Hub methods ever
- DB write always happens before SignalR push; SignalR is notifications-only, not state source
- Silent catch blocks must log at Warning level — bare catch without logging is forbidden in all controllers
- Json.Serialize() is the canonical pattern for JS string contexts (not Html.Raw with Replace)
- All file uploads must have extension allowlists and size limits
- guide-role-badge is the canonical role badge class for Guide system
- step-variant-blue replaces step-variant-pink for CMP/admin module steps

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-03-16
Stopped at: v5.0 completed and archived
Resume file: None
