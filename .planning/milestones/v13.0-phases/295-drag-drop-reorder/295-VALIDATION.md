---
phase: 295
slug: drag-drop-reorder
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-03
---

# Phase 295 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (no automated test framework in project) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build` + manual browser check
- **Before `/gsd:verify-work`:** Full manual UAT
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 295-01-01 | 01 | 1 | REORD-01 | build+grep | `dotnet build && grep -c "ReorderBatch" Controllers/OrganizationController.cs` | N/A | pending |
| 295-01-02 | 01 | 1 | REORD-01, REORD-02 | build+grep | `dotnet build && grep -c "initSortable" wwwroot/js/orgTree.js` | N/A | pending |
| 295-01-03 | 01 | 1 | REORD-01 | build+grep | `dotnet build && grep -c "drag-handle" wwwroot/js/orgTree.js` | N/A | pending |

*Status: pending*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Drag node within siblings reorders correctly | REORD-01 | Requires browser drag interaction | Open ManageOrganization, hover node to see grip, drag up/down, verify order persists after refresh |
| Cross-parent drag blocked | REORD-02 | Requires browser drag interaction | Try dragging node to different parent group, verify it snaps back |
| Drag handle appears on hover only | REORD-01 | Visual CSS behavior | Hover over tree row, verify grip icon fades in; move mouse away, verify it fades out |

---

## Validation Sign-Off

- [ ] All tasks have automated verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
