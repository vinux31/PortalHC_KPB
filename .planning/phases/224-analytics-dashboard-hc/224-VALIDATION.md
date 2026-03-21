---
phase: 224
slug: analytics-dashboard-hc
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-22
---

# Phase 224 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser verification (ASP.NET Core MVC Razor — no JS test framework) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run` + manual browser check
- **Before `/gsd:verify-work`:** Full suite must compile clean
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 224-01-01 | 01 | 1 | ANLT-01 | build+manual | `dotnet build` | ❌ W0 | ⬜ pending |
| 224-01-02 | 01 | 1 | ANLT-02 | build+manual | `dotnet build` | ❌ W0 | ⬜ pending |
| 224-01-03 | 01 | 1 | ANLT-03 | build+manual | `dotnet build` | ❌ W0 | ⬜ pending |
| 224-01-04 | 01 | 1 | ANLT-04 | build+manual | `dotnet build` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No new test framework needed — this is a server-rendered MVC app verified via `dotnet build` + browser UAT.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Fail rate bar chart renders per section/category | ANLT-01 | Chart.js canvas rendering — no headless JS test | Navigate to /CMP/AnalyticsDashboard, verify bar chart shows data |
| Trend line chart responds to date range | ANLT-02 | Interactive date filter + Chart.js | Change date range, verify line chart updates via AJAX |
| ET score heatmap table with color coding | ANLT-03 | Visual verification of heatmap colors | Check table cells have correct background colors per score |
| Expired certificates list within 30 days | ANLT-04 | Data-dependent query verification | Verify certificates expiring within 30 days appear in table |
| Cascade filter Bagian→Unit, Kategori→SubKategori | D-08 | AJAX cascade behavior | Select Bagian, verify Unit dropdown filters; select Kategori, verify SubKategori filters |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
