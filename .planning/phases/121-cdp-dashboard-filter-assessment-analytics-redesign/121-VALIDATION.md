---
phase: 121
slug: cdp-dashboard-filter-assessment-analytics-redesign
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-08
---

# Phase 121 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (project convention) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | Manual UAT per test map below |
| **Estimated runtime** | ~5 seconds (build) + manual UAT |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Manual browser testing on all test cases
- **Before `/gsd:verify-work`:** All 8 test cases pass in browser
- **Max feedback latency:** 5 seconds (build)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 121-01-01 | 01 | 1 | N/A-01 | manual | Browser: change Section, verify Unit populates | N/A | ⬜ pending |
| 121-01-02 | 01 | 1 | N/A-02 | manual | Browser: change Section on Analytics, verify cascade | N/A | ⬜ pending |
| 121-01-03 | 01 | 1 | N/A-03 | manual | Login as SectionHead, verify Section locked | N/A | ⬜ pending |
| 121-01-04 | 01 | 1 | N/A-04 | manual | Login as Coach, verify Section+Unit locked | N/A | ⬜ pending |
| 121-01-05 | 01 | 1 | N/A-05 | manual | Change filter, verify KPIs/charts/table update | N/A | ⬜ pending |
| 121-01-06 | 01 | 1 | N/A-06 | manual | On Analytics tab, change filter, verify tab stays | N/A | ⬜ pending |
| 121-01-07 | 01 | 1 | N/A-07 | manual | Set filter, export Excel, verify filtered data | N/A | ⬜ pending |
| 121-01-08 | 01 | 1 | N/A-08 | manual | Set filters, click Clear, verify reset | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Cascade filter populates dependents | N/A-01,02 | UI interaction + AJAX | Change Section dropdown, verify Unit options update |
| Role-based filter locking | N/A-03,04 | Requires role-specific login | Login as Level 4/5 user, verify locked filters |
| AJAX refresh all sections | N/A-05 | Visual verification of KPI/chart/table | Change any filter, verify all page sections refresh |
| Tab persistence | N/A-06 | UI state verification | On Analytics tab, change filter, verify no tab switch |
| Filtered Excel export | N/A-07 | File content verification | Export with filters active, open Excel, verify data matches |
| Clear button reset | N/A-08 | UI state verification | Click Clear, verify all dropdowns reset to "Semua" |

---

## Validation Sign-Off

- [x] All tasks have manual verification mapped
- [x] Sampling continuity: build check after every commit
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 5s (build only)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
