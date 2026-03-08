---
phase: 122
slug: remove-assessment-analytics-tab-from-cdp-dashboard
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-08
---

# Phase 122 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser verification + dotnet build |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run` + browser checks
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 122-01-01 | 01 | 1 | N/A-01 | manual | Browser: /CDP/Dashboard as HC | N/A | ⬜ pending |
| 122-01-02 | 01 | 1 | N/A-02 | manual | Browser: /CDP/Dashboard | N/A | ⬜ pending |
| 122-01-03 | 01 | 1 | N/A-03 | build | `dotnet build` | N/A | ⬜ pending |
| 122-01-04 | 01 | 1 | N/A-04 | manual | Browser: /CDP/Index | N/A | ⬜ pending |
| 122-01-05 | 01 | 1 | N/A-05 | manual | Browser: /CDP/Dashboard?activeTab=analytics | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Analytics tab removed from Dashboard | N/A-01 | UI removal verification | Browse /CDP/Dashboard as HC role, confirm no analytics tab |
| Dashboard shows single Coaching Proton section | N/A-02 | Layout verification | Browse /CDP/Dashboard, confirm single-section layout |
| Hub card text updated | N/A-04 | Text content check | Browse /CDP/Index, confirm card reflects new page name |
| Old analytics URL doesn't error | N/A-05 | Graceful degradation | Browse /CDP/Dashboard?activeTab=analytics, confirm no error |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
