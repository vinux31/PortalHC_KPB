---
phase: 356
slug: audit-fix-assign-coach-coachee
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-09
---

# Phase 356 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source: 356-RESEARCH.md §Validation Architecture (HIGH confidence).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.13.0 (net8.0) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (no separate runsettings) |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~CoacheeEligibility"` |
| **Full suite command** | `dotnet test` (~131 test hijau baseline per Phase 355) |
| **Estimated runtime** | quick <5s · full ~30-60s |

Integration filter (skip real-SQL bila tak ada SQLEXPRESS): `dotnet test --filter "Category!=Integration"`

---

## Sampling Rate

- **After every task commit:** Run `dotnet test HcPortal.Tests --filter "FullyQualifiedName~CoacheeEligibility"` (AF-1 helper, <5s)
- **After every plan wave:** Run `dotnet test` (full suite, ~131+ test)
- **Before `/gsd-verify-work`:** `dotnet build` 0 error + `dotnet test` hijau + Playwright UAT track id=4 (D-14)
- **Max feedback latency:** <5 seconds (quick), <60s (full)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| by-plan | W0 | 0 | AF-1 | — | unit A 3/3 Approved (expectedCount=3) → eligible | unit (static helper) | `dotnet test --filter "FullyQualifiedName~IsEligiblePerUnit"` | ❌ W0 | ⬜ pending |
| by-plan | W0 | 0 | AF-1 | — | unit B 0/1 (expectedCount=1, 0 progress) → NOT eligible | unit | `dotnet test --filter "FullyQualifiedName~IsEligiblePerUnit"` | ❌ W0 | ⬜ pending |
| by-plan | W0 | 0 | AF-1 | — | expectedCount=0 → NOT eligible via helper (Tahun 3 by-design at call-site, D-02) | unit | `dotnet test --filter "FullyQualifiedName~IsEligiblePerUnit"` | ❌ W0 | ⬜ pending |
| by-plan | W0 | 0 | AF-1 | — | partial 2/3 Approved → NOT eligible | unit | `dotnet test --filter "FullyQualifiedName~IsEligiblePerUnit"` | ❌ W0 | ⬜ pending |
| by-plan | — | — | AF-3 | T-356 (priv data integrity) | MarkCompleted set IsActive=false+IsCompleted=true+EndDate; cascade deactivate assignment; histori progress utuh | integration (InMemory DbContext) | `dotnet test --filter "FullyQualifiedName~MarkMappingCompleted"` | ❌ W0 (opsional) | ⬜ pending |
| by-plan | — | — | AF-7 | — | `incompleteCoachees` identik sebelum/sesudah batch refactor (zero behavior change) | unit (InMemory regresi) | `dotnet test --filter "FullyQualifiedName~ProgressionWarning"` | ❌ W0 (opsional) | ⬜ pending |
| by-plan | — | — | AF-6 | — | pesan spesifik saat race duplicate (DbUpdateException filter) | unit atau manual | — | ❌ W0 (opsional) | ⬜ pending |
| MANUAL | — | — | AF-1 e2e | — | track id=4 → assign → approve → CreateAssessment → coachee muncul | Playwright UAT | localhost:5277 (D-14) | manual | ⬜ pending |
| MANUAL | — | — | AF-2 | — | cross-unit checkbox disabled; clear → re-enable | Playwright UAT | localhost:5277 | manual | ⬜ pending |
| MANUAL | — | — | AF-5 | — | reassign → 3 notif terkirim (coach lama/baru/coachee) | Playwright UAT | localhost:5277 | manual | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Helpers/CoacheeEligibilityCalculator.cs` — static helper baru (ekstrak logic eligibility per-unit dari `GetEligibleCoachees`, D-13). Pure: `IsEligiblePerUnit(IEnumerable<string> statuses, int expectedCount)`.
- [ ] `HcPortal.Tests/CoacheeEligibilityCalculatorTests.cs` — 4 [Fact] minimal: full-approved-eligible · zero-progress-not-eligible · partial-not-eligible · expectedCount-zero-not-eligible (AF-1).
- [ ] (Opsional) `HcPortal.Tests/MarkMappingCompletedTests.cs` — AF-3 cascade + IsActive=false via InMemory DbContext (pola `OrganizationControllerTests.MakeController`).
- [ ] (Opsional) AF-7 regresi test `incompleteCoachees` parity — lock zero-behavior-change otomatis.
- Framework install: **tidak perlu** — xUnit sudah terpasang.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| AF-1 end-to-end eligibility | AF-1 | Butuh seed fixture track multi-unit + render dropdown CreateAssessment | Seed track id=4 (SEED_WORKFLOW snapshot+restore) → assign coachee Alkylation → approve 3 deliverable → buka CreateAssessment kategori Assessment Proton track 4 → coachee muncul. localhost:5277. |
| AF-2 UI guard 1-unit/batch | AF-2 | Interaksi DOM checkbox (disable/enable) di modal existing | Buka modal assign → centang coachee unit X → coachee unit lain ter-disable + hint muncul → uncheck semua → semua re-enable. |
| AF-5 notif reassign | AF-5 | Side-effect notifikasi 3 recipient, tak ada return value | ApproveReassignSuggestion → cek 3 notif (coach lama dilepas / coach baru ditunjuk / coachee dipindah) terkirim. |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify (AF-1 helper covers logic core; AF-2/AF-5 inherently manual UI/notif)
- [ ] Wave 0 covers all MISSING references (CoacheeEligibilityCalculator + tests)
- [ ] No watch-mode flags
- [ ] Feedback latency < 5s (quick)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
