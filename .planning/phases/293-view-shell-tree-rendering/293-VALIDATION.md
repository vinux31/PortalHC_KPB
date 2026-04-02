---
phase: 293
slug: view-shell-tree-rendering
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-02
---

# Phase 293 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser verification (ASP.NET MVC — no JS test framework) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && curl -s http://localhost:5277/Admin/ManageOrganization` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build` + browser verify tree renders
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 10 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 293-01-01 | 01 | 1 | TREE-01 | build+manual | `dotnet build` | ✅ | ⬜ pending |
| 293-01-02 | 01 | 1 | TREE-02 | build+manual | `dotnet build` | ✅ | ⬜ pending |
| 293-01-03 | 01 | 1 | TREE-03 | build+manual | `dotnet build` | ✅ | ⬜ pending |
| 293-01-04 | 01 | 1 | TREE-04 | build+manual | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No new test framework needed — this is a frontend rendering phase verified by build success + browser inspection.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Tree indentation visual per level | TREE-01 | Visual rendering in browser | Open ManageOrganization, verify Bagian→Unit→Sub-unit indentation |
| Expand/collapse per node + all | TREE-02 | Interactive JS behavior | Click chevron on node, verify children toggle. Click Expand All/Collapse All |
| Badge Aktif/Nonaktif + dimming | TREE-03 | Visual badge color + opacity | Verify green badge on active, red badge + opacity on inactive |
| Unlimited depth recursive | TREE-04 | Requires Level 2+ data in DB | Add Level 2+ unit, verify tree renders correctly |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
