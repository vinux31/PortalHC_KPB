---
phase: 132
slug: assessment-triggers
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-09
---

# Phase 132 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (no automated test framework) |
| **Config file** | none |
| **Quick run command** | N/A |
| **Full suite command** | N/A |
| **Estimated runtime** | ~120 seconds (manual) |

---

## Sampling Rate

- **After every task commit:** Manual smoke test of modified trigger
- **After every plan wave:** Both triggers verified with correct recipients and messages
- **Before `/gsd:verify-work`:** Both triggers verified
- **Max feedback latency:** 120 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 132-01-01 | 01 | 1 | ASMT-01 | manual | Browser: create assessment, check worker bell | N/A | ⬜ pending |
| 132-01-02 | 01 | 1 | ASMT-02 | manual | Browser: complete all exams in group, check HC/Admin bell | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Worker notified on assessment assign | ASMT-01 | No automated test framework | Create assessment with workers, check worker bell icon |
| HC/Admin notified when all group workers complete | ASMT-02 | No automated test framework | Complete all exams in a group, check HC/Admin bell icon |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 120s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-03-09
