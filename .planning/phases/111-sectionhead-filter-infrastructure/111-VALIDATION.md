---
phase: 111
slug: sectionhead-filter-infrastructure
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-07
---

# Phase 111 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (ASP.NET MVC — no unit test framework configured) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` (compilation check)
- **After every plan wave:** Run `dotnet build && dotnet run` + manual browser verification
- **Before `/gsd:verify-work`:** Full build must succeed, manual UAT pass
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 111-01-01 | 01 | 1 | SH-01, SH-02 | build + manual | `dotnet build` | N/A | pending |
| 111-01-02 | 01 | 1 | SH-03 | build + manual | `dotnet build` | N/A | pending |
| 111-02-01 | 02 | 1 | FILT-04, FILT-05 | build + manual | `dotnet build` | N/A | pending |
| 111-02-02 | 02 | 1 | FILT-05 | build + manual | `dotnet build` | N/A | pending |

*Status: pending*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No new test framework needed — verification is compilation + manual browser testing.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| SH level 4 sees same pages as SrSpv | SH-01 | Role-based UI requires browser login | Login as SH level 4, navigate all CMP/CDP pages, compare with SrSpv |
| Navbar visibility for SH level 4 | SH-02 | Layout rendering requires browser | Login as SH level 4, verify menu items match SrSpv |
| Co-sign approval flow | SH-03 | Multi-step approval requires browser interaction | Submit coaching, login as SrSpv approve, login as SH approve (or vice versa) |
| ManageWorkers filter uses OrganizationStructure | FILT-04 | Dropdown data source requires browser | Open ManageWorkers, verify Bagian dropdown matches OrganizationStructure |
| Cascade filter Bagian > Unit | FILT-05 | JS cascade requires browser interaction | Select Bagian, verify Unit dropdown filters to that Bagian's units only |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 15s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
