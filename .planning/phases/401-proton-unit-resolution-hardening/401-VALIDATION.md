---
phase: 401
slug: proton-unit-resolution-hardening
status: planned
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-18
updated: 2026-06-18
---

# Phase 401 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source signals: see `401-RESEARCH.md` §"Validation Architecture" (maps each PSU-ID → observable verification signal).
> Filled against final task IDs after planning (6 plans / 3 internal waves).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) + EF-InMemory (logic) + SQL-real disposable fixture (single-active smoke = Phase 404) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~ProtonUnitResolve\|FullyQualifiedName~AssignmentUnitInUserUnits\|FullyQualifiedName~CleanupNoClobber\|FullyQualifiedName~UnitUnresolvedAudit\|FullyQualifiedName~ReactivateUnitValidation\|FullyQualifiedName~CertGateAudit\|FullyQualifiedName~FilterAxis"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~60-120 seconds (full suite) |

---

## Sampling Rate

- **After every task commit:** Run the quick run command (filtered PROTON/CoachMapping/UnitResolution tests) + `dotnet build` 0 error
- **After every plan / wave merge:** Run `dotnet test` (full suite — guard no regression in 366 existing)
- **Before `/gsd-verify-work`:** Full suite green + `dotnet build` 0 error + `dotnet run` localhost:5277 smoke + manual D-01 indicator verification (401-03 checkpoint)
- **Max feedback latency:** ~120 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 401-01-1 | 01 | 0 | PSU-03 | T-401-validation | `ValidateAssignmentUnitInUserUnits` helper: true iff AssignmentUnit ∈ active UserUnits; false on empty (never primary) | unit | `dotnet test --filter ~AssignmentUnitInUserUnits` | ❌→✅ W0 | ⬜ pending |
| 401-01-2 | 01 | 0 | PSU-01,03,04 | T-401-silent-primary | RED scaffolds: resolver-skip + ∈UserUnits + cleanup no-clobber contracts committed | unit | `dotnet test --filter ~ProtonUnitResolve` | ❌ W0 | ⬜ pending |
| 401-01-3 | 01 | 0 | PSU-05,07 | T-401-silent-primary | RED scaffolds: audit channel (persisted primitive) + reactivation guard contracts | unit | `dotnet test --filter ~UnitUnresolvedAudit` | ❌ W0 | ⬜ pending |
| 401-02-1 | 02 | 1 | PSU-01,05 | T-401-silent-primary | GetEligibleCoachees resolves AssignmentUnit-only; empty → BLOCK + persisted AuditLog (ProtonUnitUnresolved) + LogWarning | unit | `dotnet test --filter ~UnitUnresolvedAudit` | ✅ | ⬜ pending |
| 401-02-2 | 02 | 1 | PSU-01,05 | T-401-audit-flood | AutoCreateProgress AssignmentUnit-only; empty → LogWarning ONLY (no persisted AuditLog) | unit | `dotnet test --filter ~UnitUnresolvedAudit` | ✅ | ⬜ pending |
| 401-02-3 | 02 | 1 | PSU-01,05 | T-401-silent-primary | Resolver returns null/skip on empty AssignmentUnit even if User.Unit set; channel separation proven | unit | `dotnet test --filter ~ProtonUnitResolve` | ✅ | ⬜ pending |
| 401-04-1 | 04 | 1 | PSU-01,05 | T-401-silent-primary | AssessmentAdmin cert-gate AssignmentUnit-only; empty → BLOCK issuance + persisted AuditLog + LogWarning | unit | `dotnet test --filter ~CertGateAudit` | ✅ | ⬜ pending |
| 401-04-2 | 04 | 1 | PSU-05 | T-401-cert-misissue | Cert-gate persists ProtonUnitUnresolved audit (ActionType/TargetType/"session/cert") | unit | `dotnet test --filter ~CertGateAudit` | ✅ | ⬜ pending |
| 401-05-1 | 05 | 1 | PSU-01,05 | T-401-silent-primary | CDP defensive resolvers drop `?? userUnits129` primary fallback; empty → LogWarning (read-path), excluded | unit/grep | `dotnet test --filter ~FilterAxis` | ✅ | ⬜ pending |
| 401-05-2 | 05 | 1 | PSU-02 | T-401-wrong-unit-visibility | 4 coachee-scope filters use AssignmentUnit axis (batched, no N+1) | unit/grep | `dotnet test --filter ~FilterAxis` | ✅ | ⬜ pending |
| 401-05-3 | 05 | 1 | PSU-02 | T-401-wrong-unit-visibility | Coachee keyed to AssignmentUnit (UnitY), not primary (UnitX) | unit | `dotnet test --filter ~FilterAxis` | ✅ | ⬜ pending |
| 401-06-1 | 06 | 1 | PSU-02 | T-401-wrong-unit-visibility | BypassList filters by AssignmentUnit (active mapping), not scalar User.Unit | unit/grep | `dotnet build` + `dotnet run` smoke | ✅ | ⬜ pending |
| 401-06-2 | 06 | 1 | PSU-03 | T-401-validation | BypassSave rejects TargetUnit ∉ worker.UserUnits active OR ∉ org-tree (not just non-empty) | unit | `dotnet test --filter ~AssignmentUnitInUserUnits` | ✅ | ⬜ pending |
| 401-06-3 | 06 | 1 | PSU-03 | T-401-validation | TargetUnit owned → accept, unowned → reject, empty → reject | unit | `dotnet test --filter ~AssignmentUnitInUserUnits` | ✅ | ⬜ pending |
| 401-03-1 | 03 | 2 | PSU-03 | T-401-validation | Assign(batch)/Edit(pre-tx)/Import(per-row) validate AssignmentUnit ∈ coachee.UserUnits | unit | `dotnet test --filter ~AssignmentUnitInUserUnits` | ✅ | ⬜ pending |
| 401-03-2 | 03 | 2 | PSU-04,07 | T-401-dataloss | Cleanup preserves valid non-primary AssignmentUnit; Reactivate rejects released-unit; AF-4 window untouched | unit/grep | `dotnet test --filter ~CleanupNoClobber\|~ReactivateUnitValidation` | ✅ | ⬜ pending |
| 401-03-3 | 03 | 2 | PSU-04,05 | T-401-dataloss | Import-reactivate preserve (clobber removed); D-01 indicator computed + rendered | unit/grep | `dotnet test` + manual D-01 (checkpoint) | ✅ | ⬜ pending |
| 401-03-4 | 03 | 2 | PSU-05 | T-401-silent-primary | D-01 on-demand orphan indicator renders on /Admin/CoachCoacheeMapping (Razor — manual) | manual | `dotnet run` localhost:5277 + Playwright bila ada | n/a | ⬜ pending |
| 401-03-5 | 03 | 2 | PSU-03,04,07 | T-401-validation | Write-path decision primitives GREEN (preserve/reject/batch) | unit | `dotnet test --filter ~CleanupNoClobber\|~ReactivateUnitValidation\|~AssignmentUnitInUserUnits` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] Test seam: `public static ValidateAssignmentUnitInUserUnits` helper (pola 399 `WorkerController.ValidateUnitsInSection`) — defined in 401-01 Task 1
- [x] `HcPortal.Tests/ProtonUnitResolveTests.cs` (PSU-01) — 401-01 Task 2
- [x] `HcPortal.Tests/AssignmentUnitInUserUnitsTests.cs` (PSU-03) — 401-01 Task 2
- [x] `HcPortal.Tests/CleanupNoClobberTests.cs` (PSU-04) — 401-01 Task 2
- [x] `HcPortal.Tests/UnitUnresolvedAuditTests.cs` (PSU-05) — 401-01 Task 3 (reuse `CapturingLogger.cs`)
- [x] `HcPortal.Tests/ReactivateUnitValidationTests.cs` (PSU-07) — 401-01 Task 3
- [x] `HcPortal.Tests/CertGateAuditTests.cs` (PSU-05 cert-gate) — 401-04 Task 2 (own file, no Wave-1 overlap)
- [x] `HcPortal.Tests/FilterAxisTests.cs` (PSU-02) — 401-05 Task 3 (own file, no Wave-1 overlap)
- [ ] `ProtonCompletionFixture` (SQL-real disposable) for single-active smoke — DEFERRED to Phase 404 (QA-03 deep SQL-real)

*Existing infrastructure (xUnit + EF-InMemory + CapturingLogger + ProtonCompletionFixture) covers all phase requirements — 0 new package. wave_0_complete flips true once 401-01 lands.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| D-01 on-demand indicator on /Admin/CoachCoacheeMapping | PSU-05 | Razor render + Bootstrap (lesson Phase 354: grep/build tak cukup) | 401-03 checkpoint: `dotnet run` localhost:5277 → /Admin/CoachCoacheeMapping → yellow alert appears when an active mapping has AssignmentUnit empty/∉UserUnits, absent when none (Playwright bila ada; DB lokal snapshot/restore) |
| Fixture coachee multi-unit T1@X→T2@Y resolve benar tiap surface | PSU-01/02 | Multi-surface runtime + DB lokal | `dotnet run` + DB lokal seed fixture (snapshot/restore per Seed Workflow) → verify resolve unit benar di BypassList/CDP/gate; deep SQL-real assertion = Phase 404 |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies (401-03-4 = checkpoint manual, paired with 401-03-3/-5 automated)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (5 base test files + helper; CertGate/FilterAxis own-file in W1)
- [x] No watch-mode flags
- [x] Feedback latency < 120s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved (planning) — wave_0_complete flips true after 401-01 executes.
