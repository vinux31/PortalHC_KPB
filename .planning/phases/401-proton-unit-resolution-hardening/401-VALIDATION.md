---
phase: 401
slug: proton-unit-resolution-hardening
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-18
---

# Phase 401 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source signals: see `401-RESEARCH.md` §"Validation Architecture" (maps each PSU-ID → observable verification signal).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) + EF-InMemory (logic) + SQL-real disposable fixture (single-active smoke) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~Proton|FullyQualifiedName~CoachMapping|FullyQualifiedName~UnitResolution"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~60-120 seconds (full suite) |

---

## Sampling Rate

- **After every task commit:** Run quick run command (filtered PROTON/CoachMapping/UnitResolution tests)
- **After every plan wave:** Run `dotnet test` (full suite — guard no regression in 366 existing)
- **Before `/gsd-verify-work`:** Full suite green + `dotnet build` 0 error + `dotnet run` localhost:5277 smoke
- **Max feedback latency:** ~120 seconds

---

## Per-Task Verification Map

> Filled by gsd-planner / gsd-nyquist-auditor against final task IDs. Each PSU requirement maps to a resolver/validator/no-clobber/reactivation signal (see RESEARCH §Validation Architecture).

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 401-XX-XX | XX | 1 | PSU-01 | — | Resolver returns null/skip when AssignmentUnit empty even if User.Unit set | unit | `dotnet test --filter ~UnitResolution` | ❌ W0 | ⬜ pending |
| 401-XX-XX | XX | 1 | PSU-02 | — | Filter returns coachee at non-primary AssignmentUnit | unit | `dotnet test --filter ~UnitResolution` | ❌ W0 | ⬜ pending |
| 401-XX-XX | XX | 1 | PSU-03 | T-401-validation | AssignmentUnit ∉ coachee.UserUnits rejected; TargetUnit ∈ worker.UserUnits enforced | unit | `dotnet test --filter ~UnitResolution` | ❌ W0 | ⬜ pending |
| 401-XX-XX | XX | 1 | PSU-04 | T-401-dataloss | CleanupCoachCoacheeMappingOrg preserves valid non-primary AssignmentUnit | unit | `dotnet test --filter ~CoachMapping` | ❌ W0 | ⬜ pending |
| 401-XX-XX | XX | 1 | PSU-05 | T-401-silent-primary | Gate-eligibility BLOCK + AuditLog persisted; read-path skip → ILogger only (CapturingLogger) | unit | `dotnet test --filter ~UnitResolution` | ❌ W0 | ⬜ pending |
| 401-XX-XX | XX | 1 | PSU-07 | T-401-reactivate | Reactivate validates AssignmentUnit ∈ coachee.UserUnits active + preserves unit + single-active intact | unit + SQL-real smoke | `dotnet test --filter ~Proton` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Test seam: confirm/extend `public static` testable helpers for resolver + validator (pola 399 `SyncUserUnitsAsync`)
- [ ] `HcPortal.Tests/...UnitResolutionTests.cs` (or equivalent) — stubs for PSU-01/02/03/05 resolver+validator logic (EF-InMemory)
- [ ] Reuse `CapturingLogger.cs` (existing) for PSU-05 channel assertion (ILogger vs AuditLog)
- [ ] Reuse `ProtonCompletionFixture` (SQL-real disposable) for PSU-07 single-active smoke

*Existing infrastructure (xUnit + EF-InMemory + CapturingLogger + ProtonCompletionFixture) covers all phase requirements — 0 new package.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Indikator UI on-demand (D-01) di CoachCoacheeMapping | PSU-05 | Razor render + Bootstrap (lesson Phase 354: grep/build tak cukup) | `dotnet run` localhost:5277 → /Admin/CoachCoacheeMapping → indikator tampil bila ada mapping aktif AssignmentUnit kosong/∉UserUnits (Playwright bila ada) |
| Fixture coachee multi-unit T1@X→T2@Y resolve benar tiap surface | PSU-01/02 | Multi-surface runtime + DB lokal | `dotnet run` + DB lokal seed fixture (snapshot/restore per Seed Workflow) → verifikasi resolve unit benar di BypassList/CDP/gate |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
