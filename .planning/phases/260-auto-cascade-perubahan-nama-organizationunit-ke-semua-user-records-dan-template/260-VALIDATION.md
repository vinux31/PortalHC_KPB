---
phase: 260
slug: auto-cascade-perubahan-nama-organizationunit-ke-semua-user-records-dan-template
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-26
---

# Phase 260 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (ASP.NET MVC server-side) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build`
- **Before `/gsd:verify-work`:** Full build must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 260-01-01 | 01 | 1 | D-01,D-02,D-03 | build | `dotnet build` | ✅ | ⬜ pending |
| 260-01-02 | 01 | 1 | D-05 | build | `dotnet build` | ✅ | ⬜ pending |
| 260-01-03 | 01 | 1 | D-07 | build | `dotnet build` | ✅ | ⬜ pending |
| 260-01-04 | 01 | 1 | D-09 | build | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Rename Bagian cascades to users | D-01,D-02 | Server-side DB operation needs browser | 1. Go to ManageOrganization, rename a Bagian. 2. Check ManageWorkers — users should show new name |
| Rename cascades to CoachCoacheeMapping | D-03 | Server-side DB operation | 1. Rename Bagian. 2. Check CoachCoacheeMapping — AssignmentSection updated |
| Template import shows dynamic names | D-05 | Excel file download | 1. Download import template. 2. Check guidance row shows current Bagian names |
| Deactivate blocked if users exist | D-07 | UI interaction | 1. Try to deactivate a Bagian with active users. 2. Should show error message |
| Reparent cascades Section | D-09 | UI interaction | 1. Move Unit from Bagian A to B. 2. Check user Section updated |
| Flash message shows count | D-06 | UI interaction | 1. Rename a Bagian. 2. Check TempData shows "X user dan Y mapping terupdate" |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
