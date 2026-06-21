---
phase: 409
slug: data-foundation-re-entry-guards-exclude-removed-query
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-21
---

# Phase 409 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET 8) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~ParticipantRemoval"` |
| **Full suite command** | `dotnet test HcPortal.Tests` |
| **Estimated runtime** | ~quick <30s / full per existing suite |

---

## Sampling Rate

- **After every task commit:** Run quick run command (removal guard + exclude tests)
- **After every plan wave:** Run `dotnet build` + full suite
- **Before `/gsd-verify-work`:** `dotnet build` 0 error + full suite green + migration applied DB lokal (sqlcmd verify)
- **Max feedback latency:** ~30 seconds (quick)

---

## Per-Task Verification Map

> Filled by planner per-task and by `/gsd-validate-phase` (Nyquist). Behaviors to validate (Validation Architecture, RESEARCH §):
> - Guard re-entry: `StartExam` blocks `RemovedAt != null` (redirect + message, session NOT marked InProgress)
> - Guard re-entry: `SubmitExam` blocks `RemovedAt != null` before grading (answers discarded)
> - Guard re-entry: `AssessmentHub.JoinBatch` silent-skips `RemovedAt != null`
> - Exclude-removed: admin monitoring/grouping/detail count queries omit `RemovedAt != null`; per-worker `UserAssessmentHistory` UNCHANGED (boundary)

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD | 01 | 1 | PRMV-03 | T-409-01 | removed session cannot Start/Submit/Join | integration | `dotnet test --filter ~ParticipantRemoval` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Test file(s) for PRMV-03 guard block + exclude-removed query (extend existing `AssessmentWindowRemovalTests` real-controller pattern, NON-tautological — see backlog 999.12 lesson)
- [ ] Shared fixture: removed-session seed (`RemovedAt` set) + active-session baseline

*If existing infra covers: note in plan.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Migration applies to local DB; 3 columns present, existing rows NULL | PRMV-03 (foundation) | DDL apply + sqlcmd inspection | `dotnet ef database update` (ASPNETCORE_ENVIRONMENT=Development) + `sqlcmd -C -I` verify columns nullable, all NULL |

*Full e2e (Playwright live Monitoring) deferred to Phase 413.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
