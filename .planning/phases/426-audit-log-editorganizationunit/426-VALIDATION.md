---
phase: 426
slug: audit-log-editorganizationunit
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-24
---

# Phase 426 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (.NET 8) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrganizationControllerTests"` |
| **Full suite command** | `dotnet test HcPortal.Tests --filter "Category!=Integration"` |
| **Estimated runtime** | ~15-30 s (filtered class) |

---

## Sampling Rate

- **After every task commit:** Run quick command (OrganizationControllerTests filter)
- **After every plan wave:** Run full suite (`Category!=Integration`)
- **Before `/gsd-verify-work`:** Full suite green
- **Max feedback latency:** ~30 s

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 426-01-01 | 01 | 1 | AUDIT-01 | — | Audit row written only on actual change; raw parent IDs; single combined row | unit | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrganizationControllerTests"` | ❌ W0 (new tests) | ⬜ pending |
| 426-01-02 | 01 | 1 | AUDIT-01 | — | Audit failure (null userManager) swallowed — edit response not blocked | unit | same | ✅ (reuses existing factory) | ⬜ pending |

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/OrganizationControllerTests.cs` — add `FakeUserStore` + `MakeUserManager` + `MakeControllerWithUser` factory (copy pattern from `RetakeExamEndpointTests.cs:47-99`)
- [ ] T1 `EditOrganizationUnit_RenameLevel1_WritesOneAuditRow` (AUDIT-01 / SC#1)
- [ ] T2 `EditOrganizationUnit_Reparent_WritesParentIdsInDescription` (AUDIT-01 / SC#1+SC#2 raw IDs D-03)
- [ ] T3 `EditOrganizationUnit_RenameAndReparent_WritesExactlyOneRow` (AUDIT-01 / D-02 single row)
- [ ] T4 `EditOrganizationUnit_NoChange_WritesZeroAuditRows` (AUDIT-01 / D-01 only-on-change)
- [ ] T5 `EditOrganizationUnit_AuditFailure_DoesNotBlockEdit` (SC#3 swallow — uses existing null-userManager `MakeController()`)

*Existing tests `EditOrganizationUnit_RenameLevel1_RenamesAllUserUnitsRows`, `...ReparentSingleUnitWorker_Allowed`, `PreviewEditCascade_*` serve as the SC#4 regression guard — must stay green (swallow protects null-userManager harness).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| — | — | All AUDIT-01 behaviors have automated xUnit coverage (backend-only, no UI) | — |

*All phase behaviors have automated verification. No Playwright UAT needed (backend-only, UI hint: no).*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (5 new test cases)
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
