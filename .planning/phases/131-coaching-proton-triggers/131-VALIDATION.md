---
phase: 131
slug: coaching-proton-triggers
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-09
---

# Phase 131 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (no automated test framework) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | Manual walkthrough of all 7 triggers |
| **Estimated runtime** | ~10 minutes (manual) |

---

## Sampling Rate

- **After every task commit:** `dotnet build` + manual smoke test of modified trigger
- **After every plan wave:** Full manual walkthrough of all 7 triggers
- **Before `/gsd:verify-work`:** All 7 triggers verified with correct recipients and messages
- **Max feedback latency:** Build: ~15 seconds; Manual: ~2 minutes per trigger

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 131-01-01 | 01 | 1 | COACH-01 | manual | Browser: assign mapping, check coach bell | N/A | ⬜ pending |
| 131-01-02 | 01 | 1 | COACH-02 | manual | Browser: edit mapping, check coach+coachee bells | N/A | ⬜ pending |
| 131-01-03 | 01 | 1 | COACH-03 | manual | Browser: deactivate mapping, check coach+coachee bells | N/A | ⬜ pending |
| 131-02-01 | 02 | 1 | COACH-04 | manual | Browser: submit evidence, check SrSpv/SH bell | N/A | ⬜ pending |
| 131-02-02 | 02 | 1 | COACH-05 | manual | Browser: approve deliverable, check coach+coachee bells | N/A | ⬜ pending |
| 131-02-03 | 02 | 1 | COACH-06 | manual | Browser: reject deliverable, check coach+coachee bells | N/A | ⬜ pending |
| 131-02-04 | 02 | 1 | COACH-07 | manual | Browser: approve last deliverable, check all HC bells | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.* Phase 130 delivered NotificationService, UserNotification model, and bell UI. No new test framework needed.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Coach notified on assign | COACH-01 | No automated test framework | Assign mapping, login as coach, verify bell shows notification |
| Coach+coachee notified on edit | COACH-02 | No automated test framework | Edit mapping, check both user bells |
| Coach+coachee notified on deactivate | COACH-03 | No automated test framework | Deactivate mapping, check both user bells |
| SrSpv/SH notified on submit | COACH-04 | No automated test framework | Submit evidence, login as section reviewer, verify bell |
| Coach+coachee notified on approve | COACH-05 | No automated test framework | Approve deliverable, check both user bells |
| Coach+coachee notified on reject | COACH-06 | No automated test framework | Reject deliverable, check both user bells |
| HC notified on all-complete | COACH-07 | No automated test framework | Approve last deliverable, check HC user bells |

---

## Validation Sign-Off

- [x] All tasks have manual verify instructions
- [x] Sampling continuity: manual verification per task commit
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 120s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
