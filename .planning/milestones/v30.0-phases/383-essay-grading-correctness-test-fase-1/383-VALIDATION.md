---
phase: 383
slug: essay-grading-correctness-test-fase-1
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-15
---

# Phase 383 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET 8) — `HcPortal.Tests/` |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (existing) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~IsQuestionCorrect"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~quick <30s (pure unit) · full suite minutes (incl. real-SQL fixtures) |

Note: regression tests for `FinalizeEssayGrading` require a disposable real-SQL fixture (`Category=Integration`) — `ExecuteUpdateAsync` is unsupported on EF8 InMemory (Phase 382 lesson; analog `EssayFinalizeRecomputeTests.cs`).

---

## Sampling Rate

- **After every task commit:** Run quick command (pure `IsQuestionCorrect` unit tests)
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite green + `dotnet build` 0 error
- **Max feedback latency:** ~30s (unit), full suite as needed

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 383-01-* | 01 | 1 | ECG-01 | — | N/A (pure helper) | unit | `dotnet test --filter "FullyQualifiedName~IsQuestionCorrect"` | ❌ W0 | ⬜ pending |
| 383-02-* | 02 | 2 | ECG-02 | — | count includes graded essays | unit/integration | `dotnet test --filter "FullyQualifiedName~ResultsCorrectCount"` | ❌ W0 | ⬜ pending |
| 383-02-* | 02 | 2 | ECG-03 | — | ElemenTeknis counts essays | unit/integration | `dotnet test --filter "FullyQualifiedName~ElemenTeknis"` | ❌ W0 | ⬜ pending |
| 383-02-* | 02 | 2 | ECG-04 | — | Tinjauan essay badge Benar/Salah/pending + answer text | manual + integration | Playwright/manual `CMP/Results/{id}` | ❌ W0 | ⬜ pending |
| 383-03-* | 03 | 2 | ECG-05 | — | PDF essay correctness = web (`>0`) | unit (helper reuse) | `dotnet test --filter "FullyQualifiedName~IsQuestionCorrect"` | ❌ W0 | ⬜ pending |
| 383-04-* | 04 | 3 | ECG-06 | — | SubmitEssayScore persist+authz; FinalizeEssayGrading recompute+idempotent | integration (real-SQL) | `dotnet test --filter "FullyQualifiedName~EssayFinalize"` | ✅ analog exists | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/Helpers/IsQuestionCorrectTests.cs` — unit stubs for ECG-01 (MC/MA/Essay matrix)
- [ ] `HcPortal.Tests/.../ResultsEssayCorrectnessTests.cs` — regression for ECG-02/03 (N MC + 2 graded essay → CorrectAnswers == N+2; ET counts essays)
- [ ] Extend `EssayFinalizeRecomputeTests.cs` real-SQL fixture for ECG-06 (SubmitEssayScore + FinalizeEssayGrading lock)

*Existing infrastructure (`HcPortal.Tests` + disposable real-SQL fixture) covers all needs — no new framework.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Tinjauan Jawaban essay badge renders green Benar + essay answer text in browser | ECG-04 | Razor view render (grep+build insufficient — Phase 354 lesson) | `dotnet run` → `http://localhost:5277/CMP/Results/166` → Soal 5/6 show green "Benar" + worker essay text; count "6/6" |
| PDF export essay marked correct matches web | ECG-05 | QuestPDF render (may be env-blocked locally, Phase 327) | Export PDF for graded-essay session → essay correctness matches web Results |

---

## Validation Sign-Off

- [ ] All tasks have automated verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s (unit)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
