---
phase: 405
slug: backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-21
audited: 2026-06-21
auditor: gsd-validate-phase (Nyquist)
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

| RTK | Requirement | Test Type | Automated Command | Status |
|-----|-------------|-----------|-------------------|--------|
| RTK-03 | `RetakeRules.CanRetake` semua cabang (AllowRetake/PreTest/IsManualEntry/Status/IsPassed/cap/cooldown) + `ShouldHideRetakeToggle` | unit (pure) | `dotnet test --filter FullyQualifiedName~RetakeRulesTests` | ✅ green (16/16) |
| RTK-13 | Guards exclude PreTest/IsManualEntry/PendingGrading(null)/non-Completed; counting `(UserId,Title,Category)` anti-konflasi | unit (pure) + integration | `dotnet test --filter FullyQualifiedName~RetakeRulesTests` + `dotnet test --filter FullyQualifiedName~RetakeServiceTests` | ✅ green (16/16 unit + 7/7 integration) |
| RTK-02 | `RetakeArchiveBuilder.Build` beku verdict via IsQuestionCorrect + jawaban (essay full-text) sebelum delete | unit (pure) | `dotnet test --filter FullyQualifiedName~RetakeArchiveBuilderTests` | ✅ green (4/4) |
| RTK-07 | `RetakeService.ExecuteAsync` claim-atomik anti double-archive + clear token + audit + AttemptNumber=eraRetake+1 | integration (SQL-real) | `dotnet test --filter FullyQualifiedName~RetakeServiceTests` | ✅ green (7/7 incl WR-01/WR-02/WR-03 fixes) |
| RTK-07/D-01 | Legacy AttemptHistory (tanpa snapshot) TIDAK menghitung cap (era-retake counting) | integration (SQL-real) | `dotnet test --filter FullyQualifiedName~RetakeServiceTests` | ✅ green (CanRetake_LegacyArchiveWithoutSnapshot_DoesNotConsumeCap) |
| RTK-06 | `ResetAssessment` HC delegasi service (bypassGuards); `ResetGuardTests` regresi hijau | unit + integration | `dotnet test --filter FullyQualifiedName~ResetGuardTests` | ✅ green (2/2) |
| RTK-01 | 3 kolom + EF default semua jalur create + explicit copy standard add-users; tabel + FK cascade + index | build + migration | `dotnet build; dotnet ef database update; sqlcmd -C -I -Q "..."` | ✅ manual-verified (405-01-SUMMARY sqlcmd) |
| RTK-04 | `UpdateRetakeSettings` sibling propagation (Title/Category/Schedule.Date) + clamp server-side + PreTest/Manual guard | integration (SQL-real) + unit | `dotnet test --filter FullyQualifiedName~RetakeSettingsEndpointTests` | ✅ green (9/9 — NEW, added by Nyquist audit) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `HcPortal.Tests/RetakeRulesTests.cs` — semua cabang RTK-03/13 (16 cases) ✅
- [x] `HcPortal.Tests/RetakeArchiveBuilderTests.cs` — RTK-02 (verdict freeze + essay full-text, 4 cases) ✅
- [x] `HcPortal.Tests/RetakeServiceTests.cs` (+ integration fixture) — RTK-07/06/D-01 (7 cases incl WR fixes) ✅
- [x] `HcPortal.Tests/ResetGuardTests.cs` — RTK-06 regresi (2 cases) ✅
- [x] `HcPortal.Tests/RetakeSettingsEndpointTests.cs` — RTK-04 sibling propagation + clamp + guard (9 cases) ✅
- [x] Migration `AddRetakeColumnsAndArchive` di chain SEBELUM integration jalan (disposable-DB MigrateAsync) ✅

*Framework xUnit sudah ada — tidak perlu install.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Migration applied kolom+tabel di DB lokal | RTK-01 | DB state, bukan code-path | `dotnet ef database update` lalu `sqlcmd -C -I -Q "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID('AssessmentSessions') AND name IN ('AllowRetake','MaxAttempts','RetakeCooldownHours'); SELECT OBJECT_ID('AssessmentAttemptResponseArchive')"` |
| `UpdateRetakeSettings` RBAC + AntiForgery + PRG | RTK-04 (HTTP-layer) | Controller HTTP atribut — tidak ada WebApplicationFactory di project | Grep: `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` + `return RedirectToAction("ManagePackages"...)` di `Controllers/AssessmentAdminController.cs:5564-5566,5609` (verified 405-04-SUMMARY). No HTTP integration test tearup justified (atribut di-check compile-time + confirmed di SUMMARY). |
| Standard add-users bulk-add mewarisi 3 kolom | RTK-01 | Controller path + EF save tidak covered oleh unit test | Grep `AllowRetake = savedAssessment.AllowRetake` di `Controllers/AssessmentAdminController.cs` (verified 405-04-SUMMARY — confirmed 1 occurrence) |

*Sisanya otomatis.*

---

## Validation Sign-Off

- [x] All tasks have automated verify or justified manual-only
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 15s (unit)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** NYQUIST-COMPLIANT — all RTK covered (automated or justified manual-only)

---

## Validation Audit Trail

### 2026-06-21 — Nyquist Audit by gsd-validate-phase

**Suite baseline at audit start:** 587/0/2 (per 405-VERIFICATION.md) → at audit end: 598/0/2 (+9 new RTK-04 tests + 2 WR-fix tests from review already in suite).

**Gap analysis per RTK:**

| RTK | Pre-audit status | Finding | Action |
|-----|-----------------|---------|--------|
| RTK-03 | 16 tests confirmed | COVERED (RetakeRulesTests 16/16 green) | No action |
| RTK-13 | 16 unit + 5 integration tests | COVERED (RetakeRulesTests guards + RetakeServiceTests counting) | No action |
| RTK-02 | 4 tests confirmed | COVERED (RetakeArchiveBuilderTests 4/4 green, incl essay full-text) | No action |
| RTK-07 | 7 integration tests (incl 2 WR-fixes) | COVERED (RetakeServiceTests 7/7 green) | No action |
| RTK-06 | 2 tests confirmed | COVERED (ResetGuardTests 2/2 green) | No action |
| RTK-01 | manual-only (migration) | COVERED — manual sqlcmd verified in 405-01-SUMMARY; EF + DB state not testable via unit/integration | Documented as justified manual-only |
| RTK-04 | NO automated test | **MISSING** — sibling propagation + clamp extractable behavior (pola ShuffleUpdateEndpointTests) | **FILLED** — created `HcPortal.Tests/RetakeSettingsEndpointTests.cs` (9 cases: 2 integration + 1 Theory×3 clamp + 1 Theory×4 guard) |

**RTK-04 test detail (new file):**
- `UpdateRetakeSettings_PropagatesToAllSiblings` — integration: 3 sibling sessions, replika endpoint body, verify all 3 updated (AllowRetake/MaxAttempts/Cooldown)
- `Clamp_MaxAttempts_And_Cooldown_ServerSide` (Theory×3) — unit: Math.Clamp(0→1, 99→5, 3→3) + cooldown (0→0, 999→168, 72→72)
- `UpdateRetakeSettings_DoesNotCrossCategory` — integration: same Title+Schedule, different Category → Category="OtherCat" NOT updated
- `UpdateRetakeSettings_Guard_BlocksPreTestAndManual` (Theory×4) — unit: ShouldHideRetakeToggle guard scenarios (replicates controller guard call)

**HTTP-layer behaviors (RBAC, AntiForgery, PRG redirect) documented as justified manual-only:** No WebApplicationFactory in project; verified via code review (405-04-SUMMARY grep evidence).

**Commands run:**
```
dotnet test --filter "FullyQualifiedName~RetakeSettingsEndpointTests" → Passed! 9/9
dotnet test (full suite) → Passed! 598/0/2
```
