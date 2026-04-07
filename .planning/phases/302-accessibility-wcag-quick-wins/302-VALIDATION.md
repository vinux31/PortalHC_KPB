---
phase: 302
slug: accessibility-wcag-quick-wins
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-07
---

# Phase 302 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual testing (no automated test framework in project) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run`
- **Before `/gsd-verify-work`:** Full build must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD | TBD | TBD | A11Y-01 | — | N/A | manual | `dotnet build` | — | ⬜ pending |
| TBD | TBD | TBD | A11Y-02 | — | N/A | manual | `dotnet build` | — | ⬜ pending |
| TBD | TBD | TBD | A11Y-05 | T-302-01 | Extra time input validated server-side (5-120 min range) | manual | `dotnet build` | — | ⬜ pending |
| TBD | TBD | TBD | A11Y-06 | — | N/A | manual | `dotnet build` | — | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements — manual testing per D-20 in CONTEXT.md.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Skip link visible on Tab, navigates to main content | A11Y-01 | UI interaction | Tab on page load, verify skip link appears, click/Enter jumps to main content |
| Keyboard navigation through questions and options | A11Y-02 | UI interaction | Tab through all questions, Arrow keys for options, verify all reachable |
| Extra time added and timer updated via SignalR | A11Y-05 | End-to-end flow | Add extra time from monitoring, verify worker timer extends in real-time |
| Auto-focus on page switch | A11Y-06 | UI interaction | Click Next/Prev, verify focus moves to first question on new page |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
