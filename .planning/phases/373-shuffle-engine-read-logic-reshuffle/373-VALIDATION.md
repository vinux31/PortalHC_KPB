---
phase: 373
slug: shuffle-engine-read-logic-reshuffle
status: planned
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-13
updated: 2026-06-13
---

# Phase 373 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source: `373-RESEARCH.md` §Validation Architecture. Task IDs refined by planner 2026-06-13.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (+ Microsoft.NET.Test.Sdk 17.13.0), net8.0 |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (ProjectReference → HcPortal) |
| **Quick run (core)** | `dotnet test --filter "FullyQualifiedName~ShuffleEngine"` (pure unit, no DB) |
| **Quick run (reshuffle)** | `dotnet test --filter "FullyQualifiedName~ShuffleReshuffle"` (pure unit, no DB) |
| **Full suite (no SQL)** | `dotnet test --filter "Category!=Integration"` |
| **Full suite (real SQL)** | `dotnet test` (Phase 372 integration fixture needs SQL up) |
| **Estimated runtime** | ~5s (core unit) / ~30–90s (full real-SQL) |

---

## Sampling Rate

- **After every task commit:** `dotnet test --filter "FullyQualifiedName~ShuffleEngine"` (< 5s, no DB)
- **After every plan wave:** `dotnet test --filter "Category!=Integration"`
- **Before `/gsd-verify-work`:** `dotnet build` (0 err) + `dotnet test` full (Phase 372 integration stays green = no-regression) + `dotnet run` localhost:5277 smoke
- **Max feedback latency:** ~90 seconds

---

## Per-Task Verification Map

*Refined by planner with real Plan/Task IDs. Core = pure unit (no DB); reshuffle bug-fix = pure engine-level regression assertion (full mode-matrix + Playwright = Phase 375).*

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 373-01-T1 | 01 | 1 | SHUF-04 | T-373-02 | ON 1 paket acak (seed-stabil) + ON ≥2 sampling K-min ET-balanced (CMPController CANONICAL Phase 2 per-ET, moved verbatim) | unit (pure) | `dotnet test --filter ~ShuffleEngine` | ❌→W0 (373-01-T2) | ⬜ pending |
| 373-01-T1 | 01 | 1 | SHUF-05 | — | OFF 1 paket → urut `q.Order`, identik semua worker (no rng) | unit (pure) | idem | ❌→W0 | ⬜ pending |
| 373-01-T1 | 01 | 1 | SHUF-06 | T-373-01 (DivByZero) | OFF ≥2 → worker[i]=`pkgWithQ[i % count]` paket UTUH urut Order; index stabil on append; guard paket kosong SEBELUM modulo | unit (pure) | idem | ❌→W0 | ⬜ pending |
| 373-01-T1 | 01 | 1 | SHUF-07 | — | ON → optionDict non-kosong; OFF → empty (→ `"{}"`); independen dari ShuffleQuestions | unit (pure) | idem | ❌→W0 | ⬜ pending |
| 373-01-T2 | 01 | 1 | SHUF-04/05/06/07/08 | T-373-01 | **Wave-0 test file** `ShuffleEngineTests.cs` — proves all engine modes + determinism (call 2× → identical) + empty-package guard + flag independence | unit (pure) | `dotnet test --filter ~ShuffleEngine` | ✅ created here | ⬜ pending |
| 373-02-T1 | 02 | 2 | SHUF-04/05/06/07/08 | T-373-04/06/07 | StartExam delegates to core gated on both flags; worker index from `OrderBy(Id)`; auth/ownership + stale-count guard preserved | unit (pure, via core) + build + manual smoke | `dotnet test --filter ~ShuffleEngine` + `dotnet build` | reuses 373-01-T2 | ⬜ pending |
| 373-02-T2 | 02 | 2 | SHUF-15 | — | Stale comment `CMPController.cs:1054` removed + local dup methods deleted | grep (verifier) | `rg "option shuffle removed" Controllers/CMPController.cs` → 0 | N/A | ⬜ pending |
| 373-03-T1 | 03 | 2 | SHUF-09 | T-373-08/09/10/12 | Reshuffle (both endpoints) delegate to core, respect both flags, fix `"{}"`; `[Authorize]`+AntiForgery+"Not started/Abandoned" guard + audit-log preserved | build + grep (controls present) | `dotnet build` + `rg` controls | reuses 373-03-T3 | ⬜ pending |
| 373-03-T2 | 03 | 2 | SHUF-09 | — | DIVERGENT `BuildCrossPackageAssignment` (per-package) + local `Shuffle<T>` deleted from AssessmentAdminController | grep (verifier) | `rg "private static List<int> BuildCrossPackageAssignment" Controllers/AssessmentAdminController.cs` → 0 | N/A | ⬜ pending |
| 373-03-T3 | 03 | 2 | SHUF-09 | T-373-11 | **Wave-0 regression** `ShuffleReshuffleTests.cs` — optDict serialize ≠ `"{}"` when ShuffleOptions ON; `== "{}"` when OFF (closes hard-coded `"{}"` bug) | unit (pure) | `dotnet test --filter ~ShuffleReshuffle` | ✅ created here | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `HcPortal.Tests/ShuffleEngineTests.cs` — pure unit (no DB, no fixture), covers SHUF-04/05/06/07/08 + guard paket kosong + determinisme + independensi. Created in **Plan 01 Task 2** (Wave 1, alongside core extraction = correct Nyquist sampling moment). Cetakan `QuestionTypeLabelsTests.cs`.
- [x] `HcPortal.Tests/ShuffleReshuffleTests.cs` — Wave-0 regression assertion SHUF-09 (optDict ≠ `"{}"` when ShuffleOptions ON; `== "{}"` when OFF). Created in **Plan 03 Task 3** (Wave 2). Full reshuffle mode-matrix + Playwright = Phase 375.
- [x] Framework install: not needed — xUnit + fixture already present (Phase 372).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual urutan soal/opsi ON vs OFF di layar ujian | SHUF-04..07 | Razor runtime — render exam UI; full UAT = Phase 375 | `dotnet run` @5277 smoke (Plan 02): open StartExam for an ON assessment, confirm questions render; behavior unchanged. |
| Reshuffle ON assessment → opsi non-kosong di DB | SHUF-09 | Requires HC click + DB inspection; full UAT = Phase 375 | `dotnet run` @5277 smoke (Plan 03): HC reshuffle a Not-started session of a ShuffleOptions=ON assessment → `ShuffledOptionIdsPerQuestion` non-empty in DB. |

*Catatan: UAT Playwright lengkap (toggle ON/OFF berefek di exam, lock, reminder, warning) = scope Phase 375.*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (ShuffleEngineTests + ShuffleReshuffleTests)
- [x] No watch-mode flags
- [x] Feedback latency < 90s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** planner-approved 2026-06-13 (pending execution)
