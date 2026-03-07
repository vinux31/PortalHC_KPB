---
phase: 114
slug: status-tab
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-07
---

# Phase 114 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser verification (ASP.NET Core MVC, no unit test framework configured) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run`
- **Before `/gsd:verify-work`:** Full build must succeed + manual browser check
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 114-01-01 | 01 | 1 | STAT-01 | build | `dotnet build` | N/A | pending |
| 114-01-02 | 01 | 1 | STAT-02 | manual | Browser: expand tree | N/A | pending |
| 114-01-03 | 01 | 1 | STAT-03 | manual | Browser: check silabus icons | N/A | pending |
| 114-01-04 | 01 | 1 | STAT-04 | manual | Browser: check guidance icons | N/A | pending |

*Status: pending / green / red / flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Status tab is default active tab | STAT-01 | UI state, no test framework | Navigate to ProtonData/Index, verify Status tab is active |
| Tree displays Bagian > Unit > Track | STAT-02 | Visual layout verification | Check indentation levels, bold styling, background colors |
| Green checkmark on complete silabus | STAT-03 | Requires seeded data + visual check | Ensure track with active Kompetensi+SubKompetensi+Deliverable shows green check |
| Green checkmark on guidance exists | STAT-04 | Requires seeded data + visual check | Ensure track with CoachingGuidanceFile shows green check |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
