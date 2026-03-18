---
phase: 198
slug: crud-consolidation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-18
---

# Phase 198 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build + manual verification |
| **Config file** | PortalHC_KPB.csproj |
| **Quick run command** | `dotnet build --no-restore` |
| **Full suite command** | `dotnet build` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build --no-restore`
- **After every plan wave:** Run `dotnet build`
- **Before `/gsd:verify-work`:** Full build must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 198-01-01 | 01 | 1 | CRUD-01 | build + grep | `dotnet build && grep -c "EditTrainingRecord" Controllers/CMPController.cs` | N/A | ⬜ pending |
| 198-01-02 | 01 | 1 | CRUD-01 | build + grep | `dotnet build && grep -c "DeleteTrainingRecord" Controllers/CMPController.cs` | N/A | ⬜ pending |
| 198-02-01 | 02 | 1 | CRUD-02 | build + grep | `dotnet build && grep -c "ImportTraining" Controllers/AdminController.cs` | N/A | ⬜ pending |
| 198-02-02 | 02 | 1 | CRUD-02 | build + grep | `grep -c "ImportTraining" Views/CMP/RecordsTeam.cshtml` | N/A | ⬜ pending |
| 198-03-01 | 03 | 1 | CRUD-03 | manual | Browser verification | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. This is a refactoring phase — build success + grep verification is sufficient.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Worker Detail views distinct | CRUD-03 | Visual content verification | Open Admin/WorkerDetail and CMP/RecordsWorkerDetail for same worker, confirm different content |
| Import Training accessible from Admin | CRUD-02 | Navigation flow | Go to Admin/ManageAssessment?tab=training, verify Import button exists and works |
| No broken user workflows | CRUD-01 | End-to-end flow | Attempt training edit/delete from Admin, verify full workflow |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
