---
phase: 373
slug: shuffle-engine-read-logic-reshuffle
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-13
---

# Phase 373 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source: `373-RESEARCH.md` §Validation Architecture.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (+ Microsoft.NET.Test.Sdk 17.13.0), net8.0 |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (ProjectReference → HcPortal) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~ShuffleEngine"` (core unit, no DB) |
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

*Filled/refined by planner. Core = pure unit (no DB); reshuffle bug-fix = real-SQL or controller-shape. Source: `373-RESEARCH.md` §Validation Architecture.*

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD | TBD | 0/1 | SHUF-04 | — | ON 1 paket acak (seed-stabil) + ON ≥2 sampling K-min ET-balanced (CMPController canonical) | unit (pure) | `dotnet test --filter ~ShuffleEngine` | ❌ W0 | ⬜ pending |
| TBD | TBD | 1 | SHUF-05 | — | OFF 1 paket → urut `q.Order`, identik semua worker (no rng) | unit (pure) | idem | ❌ W0 | ⬜ pending |
| TBD | TBD | 1 | SHUF-06 | DivByZero (paket kosong) | OFF ≥2 → worker[i]=`pkgWithQ[i % count]` utuh urut Order; index stabil; guard paket kosong SEBELUM modulo | unit (pure) | idem | ❌ W0 | ⬜ pending |
| TBD | TBD | 1 | SHUF-07 | — | ON → optionDict non-kosong; OFF → `"{}"`; independen dari ShuffleQuestions | unit (pure) | idem | ❌ W0 | ⬜ pending |
| TBD | TBD | 1 | SHUF-08 | — | OFF≥2 rekomputasi deterministik: core dipanggil 2x input sama → output identik (count stabil → guard tak trigger) | unit (pure) | idem | ❌ W0 | ⬜ pending |
| TBD | TBD | 2 | SHUF-09 | EoP (preserve auth/guard) | Reshuffle delegasi ke core + optDict ≠ `"{}"` saat ShuffleOptions ON (fix bug `:5119`/`:5213`); guard "Not started/Abandoned" + `[Authorize]`+AntiForgery preserved | integration (real-SQL) / controller-shape | `dotnet test` | ❌ W0 | ⬜ pending |
| TBD | TBD | 2 | SHUF-15 | — | Komentar stale `CMPController.cs:1054` dihapus | grep (verifier) | `rg "option shuffle removed" Controllers/CMPController.cs` → 0 | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/ShuffleEngineTests.cs` — pure unit (no DB, no fixture), covers SHUF-04/05/06/07/08 + guard paket kosong + determinisme + independensi. Cetakan `QuestionTypeLabelsTests.cs` (`[Theory]`/`[InlineData]`, panggil static langsung; feed in-memory `AssessmentPackage`). **Tulis DI Phase 373** (saat ekstraksi core = waktu sampling Nyquist paling tepat; 374/375 sentuh controller sama).
- [ ] (Minimal) Wave-0 assertion SHUF-09 reshuffle optDict ≠ `"{}"` saat ShuffleOptions ON — fix BUG existing, butuh bukti regresi tertutup. Mode-matrix penuh = Phase 375.
- [ ] Framework install: tidak perlu — xUnit + fixture sudah ada (Phase 372).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Efek visual urutan soal/opsi ON vs OFF di layar ujian | SHUF-04..07 | Razor runtime — render exam UI; full UAT = Phase 375 | `dotnet run` @5277 smoke: buka StartExam assessment OFF vs ON, cek urutan. UAT Playwright penuh = Phase 375. |

*Catatan: UAT Playwright lengkap (toggle ON/OFF berefek di exam, lock, reminder, warning) = scope Phase 375.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 90s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
