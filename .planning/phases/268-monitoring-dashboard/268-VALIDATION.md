---
phase: 268
slug: monitoring-dashboard
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-28
---

# Phase 268 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual UAT — no automated test framework for real-time SignalR |
| **Config file** | none |
| **Quick run command** | Manual browser verification |
| **Full suite command** | Manual browser verification (two browsers simultaneously) |
| **Estimated runtime** | ~15 minutes |

---

## Sampling Rate

- **After every task commit:** Manual browser verification
- **After every plan wave:** Full manual UAT pass
- **Before `/gsd:verify-work`:** All MON requirements verified in browser
- **Max feedback latency:** N/A (manual-only phase)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 268-01-01 | 01 | 1 | MON-04 | manual | — | N/A | ⬜ pending |
| 268-01-02 | 01 | 1 | MON-01, MON-02, MON-03 | manual | — | N/A | ⬜ pending |
| 268-01-03 | 01 | 1 | MON-02, MON-04 | manual | — | N/A | ⬜ pending |
| 268-01-04 | 01 | 1 | ALL | manual | — | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No test setup needed.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Progress "x/total" updates without refresh | MON-01 | Requires two simultaneous authenticated browsers with SignalR push | Worker answers questions → Admin verifies counter updates |
| Status lifecycle Open→InProgress→Completed | MON-02 | Requires real-time worker action triggering status change | Worker starts exam → Admin sees InProgress; Worker submits → Admin sees Completed |
| Timer/elapsed displays and ticks accurately | MON-03 | Visual verification of countdown timer | Admin opens monitoring → timer visible, counting down, not zero/negative |
| Score and pass/fail after submit | MON-04 | Requires complete submit workflow from worker | Worker submits → Admin sees Score% and Pass/Fail without refresh |

---

## Validation Sign-Off

- [x] All tasks have manual verification instructions
- [x] Sampling continuity: every task has verification step
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency: N/A (manual-only)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
