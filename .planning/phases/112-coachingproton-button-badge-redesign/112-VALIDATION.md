---
phase: 112
slug: coachingproton-button-badge-redesign
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-07
---

# Phase 112 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing + dotnet build |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` + manual browser verification |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build` + manual browser verification across roles
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 112-01-01 | 01 | 1 | BTN-01 | manual | `dotnet build` | N/A | pending |
| 112-01-02 | 01 | 1 | BTN-02 | manual | `dotnet build` | N/A | pending |
| 112-01-03 | 01 | 1 | BTN-03 | manual | `dotnet build` | N/A | pending |
| 112-01-04 | 01 | 1 | CONS-01 | manual | `dotnet build` | N/A | pending |
| 112-01-05 | 01 | 1 | CONS-02 | manual | `dotnet build` | N/A | pending |
| 112-01-06 | 01 | 1 | CONS-03 | manual | `dotnet build` | N/A | pending |
| 112-01-07 | 01 | 1 | CONS-04 | manual | `dotnet build` | N/A | pending |
| 112-01-08 | 01 | 1 | TECH-01 | manual | `dotnet build` | N/A | pending |
| 112-01-09 | 01 | 1 | TECH-02 | manual | `dotnet build` | N/A | pending |
| 112-01-10 | 01 | 1 | TECH-03 | manual | `dotnet build` | N/A | pending |

*Status: pending / green / red / flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.* No automated test framework needed for pure CSS/HTML changes. `dotnet build` catches Razor compilation errors.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| SrSpv Tinjau renders as button | BTN-01 | Visual UI verification | Login as SrSpv, navigate to CoachingProton, verify Tinjau is a button with hover state |
| SH Tinjau renders as button | BTN-02 | Visual UI verification | Login as SH, navigate to CoachingProton, verify Tinjau is a button with hover state |
| Resolved badges bold+border, no icons | BTN-03 | Visual UI verification | After approval, verify Approved/Rejected badges are bold with border |
| Evidence column consistent styling | CONS-01 | Visual UI verification | Check Sudah Upload vs Belum Upload badge styling |
| Lihat Detail button standout | CONS-02 | Visual UI verification | Verify Lihat Detail uses btn-outline-secondary |
| HC Review consistent across views | CONS-03 | Visual UI verification | Compare HC Review button in table vs Antrian Review panel |
| Export/Reset/Kembali polished | CONS-04 | Visual UI verification | Check all navigation/export buttons for consistent styling |
| JS event handlers work | TECH-01 | Functional UI testing | Click Tinjau, Submit Evidence, HC Review -- all must trigger actions |
| Modal triggers work | TECH-02 | Functional UI testing | Click Tinjau button, verify modal opens |
| AJAX innerHTML uses new classes | TECH-03 | Functional UI testing | Approve/reject via modal, verify badge updates with new styling |

---

## Validation Sign-Off

- [x] All tasks have automated verify or Wave 0 dependencies
- [x] Sampling continuity: dotnet build after every commit
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 15s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
