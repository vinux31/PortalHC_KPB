---
phase: 405
slug: backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-21
---

# Phase 405 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) — `HcPortal.Tests/` |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (existing) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~RetakeRules\|FullyQualifiedName~RetakeArchiveBuilder"` |
| **Full suite command** | `dotnet test` |
| **Integration (SQL-real)** | `dotnet test --filter "Category=Integration"` (disposable-DB `MigrateAsync`, SQLEXPRESS-gated; migration WAJIB di chain dulu) |
| **Estimated runtime** | ~quick <15s unit · full suite ~beberapa menit |

---

## Sampling Rate

- **After every task commit:** Run quick command (pure-helper unit tests).
- **After every plan wave:** Run `dotnet test` full suite (0 regresi baseline).
- **Before `/gsd-verify-work`:** `dotnet build` 0 error + `dotnet ef database update` applied + full suite green + integration (claim-atomic + counting) green.
- **Max feedback latency:** ~15s (unit) / suite end-of-wave.

---

## Per-Task Verification Map

> Diisi/diperhalus oleh planner + `/gsd-validate-phase`. Seed per-RTK:

| RTK | Requirement | Test Type | Automated Command | Status |
|-----|-------------|-----------|-------------------|--------|
| RTK-03 | `RetakeRules.CanRetake` semua cabang (AllowRetake/PreTest/IsManualEntry/Status/IsPassed/cap/cooldown) + `ShouldHideRetakeToggle` | unit (pure) | `dotnet test --filter FullyQualifiedName~RetakeRules` | ⬜ pending |
| RTK-13 | Guards exclude PreTest/IsManualEntry/PendingGrading(null)/non-Completed; counting `(UserId,Title,Category)` anti-konflasi | unit (pure) | `dotnet test --filter FullyQualifiedName~RetakeRules` | ⬜ pending |
| RTK-02 | `RetakeArchiveBuilder.Build` beku verdict via IsQuestionCorrect + jawaban (essay full-text) sebelum delete | unit (pure) | `dotnet test --filter FullyQualifiedName~RetakeArchiveBuilder` | ⬜ pending |
| RTK-07 | `RetakeService.ExecuteAsync` claim-atomik anti double-archive + clear token + audit + AttemptNumber=eraRetake+1 | integration (SQL-real) | `dotnet test --filter Category=Integration` | ⬜ pending |
| RTK-07/D-01 | Legacy AttemptHistory (tanpa snapshot) TIDAK menghitung cap (era-retake counting) | integration (SQL-real) | `dotnet test --filter Category=Integration` | ⬜ pending |
| RTK-06 | `ResetAssessment` HC delegasi service (bypassGuards); `ResetGuardTests` regresi hijau | unit + integration | `dotnet test --filter FullyQualifiedName~ResetGuard` | ⬜ pending |
| RTK-01 | 3 kolom + EF default semua jalur create + explicit copy standard add-users; tabel + FK cascade + index | build + migration | `dotnet build; dotnet ef database update; sqlcmd -C -I -Q "..."` | ⬜ pending |
| RTK-04 | `UpdateRetakeSettings` RBAC+AntiForgery+sibling propagation+audit+clamp | integration/controller | `dotnet test` | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/RetakeRulesTests.cs` — stubs semua cabang RTK-03/13 (mirror `ShuffleToggleRulesTests.cs`)
- [ ] `HcPortal.Tests/RetakeArchiveBuilderTests.cs` — stubs RTK-02 (verdict freeze + essay full-text)
- [ ] `HcPortal.Tests/RetakeServiceTests.cs` (+ integration fixture) — stubs RTK-07/06/D-01 (claim-atomic, clear-token, bypass HC, cooldown boundary, legacy-no-count)
- [ ] Migration `AddRetakeColumnsAndArchive` di chain SEBELUM integration jalan (disposable-DB MigrateAsync)

*Framework xUnit sudah ada — tidak perlu install.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Migration applied kolom+tabel di DB lokal | RTK-01 | DB state, bukan code-path | `dotnet ef database update` lalu `sqlcmd -C -I -Q "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID('AssessmentSessions') AND name IN ('AllowRetake','MaxAttempts','RetakeCooldownHours'); SELECT OBJECT_ID('AssessmentAttemptResponseArchive')"` |

*Sisanya otomatis.*

---

## Validation Sign-Off

- [ ] All tasks have automated verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s (unit)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
