---
phase: 237
slug: audit-monitoring-differentiator-enhancement
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-23
---

# Phase 237 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser verification + grep-based code checks |
| **Config file** | none — ASP.NET MVC project, no unit test project configured |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && grep -based acceptance criteria checks` |
| **Estimated runtime** | ~30 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Build + grep acceptance criteria
- **Before `/gsd:verify-work`:** Full build must be green + all acceptance criteria verified
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 237-01-xx | 01 | 1 | MON-01 | grep + build | `dotnet build` | ✅ | ⬜ pending |
| 237-02-xx | 02 | 1 | MON-02 | grep + build | `dotnet build` | ✅ | ⬜ pending |
| 237-03-xx | 03 | 1 | MON-03 | grep + build | `dotnet build` | ✅ | ⬜ pending |
| 237-04-xx | 04 | 1 | MON-04 | grep + build | `dotnet build` | ✅ | ⬜ pending |
| 237-05-xx | 05 | 2 | DIFF-01 | grep + build | `dotnet build` | ✅ | ⬜ pending |
| 237-06-xx | 06 | 2 | DIFF-02 | grep + build | `dotnet build` | ✅ | ⬜ pending |
| 237-07-xx | 07 | 2 | DIFF-03 | grep + build | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No new test framework needed — verification via `dotnet build` + grep-based acceptance criteria + browser UAT.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Chart.js bottleneck bar chart renders correctly | MON-01, DIFF-03 | Visual chart rendering | Open Dashboard, verify horizontal bar chart shows top pending deliverables |
| Batch approval modal UX | DIFF-02 | Interactive UI flow | Select checkboxes, click Approve Selected, verify modal + processing |
| Filter cascade correctness | MON-02 | Browser-dependent AJAX | Test filter dropdowns cascade correctly per role |
| Export Excel file content | MON-04 | File download verification | Download each export, verify columns and data |
| Role-based column visibility | MON-02 | Role switching | Login as HC, Coach, SrSpv — verify column visibility |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
