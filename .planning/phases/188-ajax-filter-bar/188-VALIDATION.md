---
phase: 188
slug: ajax-filter-bar
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-18
---

# Phase 188 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser verification (ASP.NET MVC + vanilla JS) |
| **Config file** | none — no automated test framework in project |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run`
- **Before `/gsd:verify-work`:** Full build must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 188-01-01 | 01 | 1 | FILT-01 | build+manual | `dotnet build` | ✅ | ⬜ pending |
| 188-01-02 | 01 | 1 | FILT-02 | build+manual | `dotnet build` | ✅ | ⬜ pending |
| 188-01-03 | 01 | 1 | FILT-03 | build+manual | `dotnet build` | ✅ | ⬜ pending |
| 188-01-04 | 01 | 1 | FILT-04 | build+manual | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Bagian/Unit cascade filter updates table via AJAX | FILT-01 | Browser DOM interaction | Select Bagian → verify Unit dropdown populates → verify table updates |
| Status filter updates table + summary cards | FILT-02 | Browser DOM interaction | Select status → verify table shows only matching rows → verify summary card counts |
| Tipe filter (Training/Assessment) | FILT-03 | Browser DOM interaction | Select tipe → verify table shows only matching record type |
| Free-text search with debounce | FILT-04 | Browser DOM interaction + timing | Type search text → verify 300ms debounce → verify table updates |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
