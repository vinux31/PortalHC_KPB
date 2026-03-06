---
phase: 107
slug: backend-worker-list-page
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-06
---

# Phase 107 — Validation Strategy

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

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run` + manual browser check
- **Before `/gsd:verify-work`:** Full build must succeed + manual verification
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 107-01-01 | 01 | 1 | HIST-01 | manual | `dotnet build` | N/A | ⬜ pending |
| 107-01-02 | 01 | 1 | HIST-02 | manual | `dotnet build` | N/A | ⬜ pending |
| 107-01-03 | 01 | 1 | HIST-03 | manual | `dotnet build` | N/A | ⬜ pending |
| 107-01-04 | 01 | 1 | HIST-04 | manual | `dotnet build` | N/A | ⬜ pending |
| 107-02-01 | 02 | 1 | HIST-05 | manual | `dotnet build` | N/A | ⬜ pending |
| 107-02-02 | 02 | 1 | HIST-06 | manual | `dotnet build` | N/A | ⬜ pending |
| 107-02-03 | 02 | 1 | HIST-07 | manual | `dotnet build` | N/A | ⬜ pending |
| 107-02-04 | 02 | 1 | HIST-08 | manual | `dotnet build` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No test framework setup needed — this is a UI/controller phase verified through build success and manual browser testing.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| CDP Hub shows Histori Proton card | HIST-01 | UI rendering | Navigate to /CDP, verify card visible |
| Coachee redirects to own detail | HIST-02 | Role-scoped redirect | Login as Coachee, navigate to Histori Proton |
| Coach sees mapped coachees | HIST-03 | Role-scoped data | Login as Coach, verify list shows only mapped workers |
| HC/Admin sees all workers | HIST-04 | Role-scoped data | Login as HC, verify all workers visible |
| List shows workers with Proton history | HIST-05 | Data query | Verify only workers with ProtonTrackAssignment appear |
| Search by nama/NIP | HIST-06 | Client-side JS | Type in search box, verify filtering |
| Filter by unit/section/jalur/status | HIST-07 | Client-side JS | Select filter options, verify filtering |
| Row shows summary info | HIST-08 | UI rendering | Verify columns: Nama, NIP, Unit, Jalur, Progress, Status |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
