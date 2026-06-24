---
phase: 416
slug: scoped-shuffle-acak-per-section
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-23
validated: 2026-06-23
---

# Phase 416 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) + Playwright (e2e) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~ShuffleEngine\|FullyQualifiedName~SectionScopedShuffle"` |
| **Full suite command** | `dotnet test HcPortal.Tests` |
| **E2E command** | `cd tests && npx playwright test scoped-shuffle.spec.ts --workers=1` (app @5277) |
| **Estimated runtime** | unit ~60-90s · e2e ~2-3 min |

> NOTE: the `~GoldenOrder` filter referenced in an earlier draft matches NO test class — legacy golden-order lives in `ShuffleEngineTests` (Phase 373). Use the combined `~ShuffleEngine|~SectionScopedShuffle` filter (26 tests: 16 legacy + 10 new).

---

## Sampling Rate

- **After every task commit:** Run quick command (`--filter ~ShuffleEngine`)
- **After every plan wave:** Run full suite command
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 90 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Test Method(s) | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|----------------|-------------------|-------------|--------|
| 416-engine | 01 | 1 | SHF-01 | T-416-01 | section-isolation: soal tak bocor antar-Section; "Lainnya" terakhir (D-15) | unit | `ScopedShuffle_NoCrossSectionLeak`, `SectionOrder_LainnyaAlwaysLast` | `dotnet test --filter ~SectionScopedShuffle` | ✅ | ✅ green |
| 416-golden | 01 | 1 | SHF-04 | — | all-null == baseline pra-416 (golden-order `{12,21}`) | unit | `AllNullSection_ProducesIdenticalOrderToLegacyBaseline` + 16 legacy `ShuffleEngineTests` | `dotnet test --filter "~ShuffleEngine\|~SectionScopedShuffle"` | ✅ | ✅ green |
| 416-determinism | 01 | 1 | SHF-04 | — | workerIndex deterministik (fixed seed 42) | unit | `Determinism_WorkerIndexStable` | `dotnet test --filter ~SectionScopedShuffle` | ✅ | ✅ green |
| 416-precedence | 01 | 1 | SHF-02 | — | induk OFF→semua urut (D-14); induk ON→ikut per-Section toggle; opsi di-gate per-Section (D-416-01) | unit | `Precedence_ParentOff_AllOrdered`, `Precedence_ParentOn_PerSectionToggle`, `OptionShuffle_GatedPerSection` | `dotnet test --filter ~SectionScopedShuffle` | ✅ | ✅ green |
| 416-pooling | 01 | 1 | SHF-03 | — | >1 paket: pooling lintas-paket + cakupan ET dalam batas Section | unit | `MultiPackage_EtCoveragePerSection`, `EtSpanningSections_CoveredIndependently` | `dotnet test --filter ~SectionScopedShuffle` | ✅ | ✅ green |
| 416-reshuffle | 02 | 2 | SHF-03/04 | — | re-roll (seed beda) tetap section-isolated | unit | `Reshuffle_SectionIsolation` | `dotnet test --filter ~SectionScopedShuffle` | ✅ | ✅ green |
| 416-et-warning-nonblock | 02 | 2 | SHF-03 | T-416-07 | Section sempit → TIDAK memblokir kelola/simpan/mulai (D-416-03 inti load-bearing) + kontrol negatif no-false-positive | e2e | `scoped-shuffle.spec.ts` S3 + S3b | `npx playwright test scoped-shuffle.spec.ts --workers=1` | ✅ | ✅ green |
| 416-et-warning-render | 02 | 2 | SHF-03 | — | alert ET-coverage **render positif** saat `DistinctEt > K` | manual/deferred | — (DEF-416-01: predikat unreachable, dead nicety) | — | n/a | 🟡 manual-only |
| 416-uat | 03 | 3 | SHF-01..04 | T-416-09 | real-browser acak per-Section (DB `ShuffledQuestionIds` == DOM) + all-null backward-compat + parity peserta | e2e | `scoped-shuffle.spec.ts` S1, S2, S4 | `npx playwright test scoped-shuffle.spec.ts --workers=1` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky · 🟡 manual-only/deferred*

**Run evidence (2026-06-23):** `dotnet test --no-build --filter "~ShuffleEngine|~SectionScopedShuffle"` → **26/26 PASS** (16 legacy golden-order + 10 SectionScopedShuffle); full suite **665/665** (orchestrator); e2e `scoped-shuffle.spec.ts` **5/5 PASS @5277** (orchestrator). Implementation verified real (not stub): `ShuffleEngine.cs` carries `BuildSectionQuestionAssignment` / `SlicePackagesBySection` / `BuildSectionAwareOptionShuffle` / `SectionStructureComparer.KeyOf`.

---

## Wave 0 Requirements

- [x] `HcPortal.Tests/SectionScopedShuffleTests.cs` — unit suite (canonical single file per PLAN): section-isolation, golden-order all-null == baseline, precedence D-14, cross-package pooling, workerIndex determinism, reshuffle isolation (SHF-01/02/03/04). **10/10 methods present + green.**
- [x] `tests/e2e/scoped-shuffle.spec.ts` — Playwright e2e: section-isolation (S1), backward-compat all-null (S2), ET-warning non-blocking + negative control (S3/S3b), parity peserta (S4) — DB backup/restore, `--workers=1`. **5/5 green @5277.**
- [x] Fixed-seed helper `new Random(42)` pattern for determinism (reuse existing `ShuffleEngineTests.cs` convention) — confirmed in `SectionScopedShuffleTests.cs`.

*Engine is pure (no DB) → unit-testable directly. Reshuffle/StartExam wiring covered by integration where a seam exists; otherwise asserted via engine-level isolation tests + Playwright UAT.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions / Status |
|----------|-------------|------------|----------------------------|
| Acak per-Section terlihat di layar ujian (Razor render) | SHF-01 | Razor/JS render runtime — lesson 354 (unit tak nangkap render) | **AUTOMATED via Playwright** `scoped-shuffle.spec.ts` S1 (DB `ShuffledQuestionIds` == DOM `qcard_{id}`, blok kontigu). Manual human-eye optional — gate auto-satisfied (autopilot §5). |
| All-null = urutan identik perilaku lama (visual) | SHF-04 | Backward-compat visual proof | **AUTOMATED via Playwright** `scoped-shuffle.spec.ts` S2 (1 kolam global, no error, DOM==DB). Byte-identik proof already unit (`AllNullSection_...Baseline`). |
| **ET-coverage warning POSITIVE render** (`DistinctEt > K` → alert tampil) | SHF-03 | **DEAD PREDICATE — DEF-416-01: not testable.** `K = COUNT(soal Section)`, `DistinctEt = distinct ET soal Section yang sama`; 1 soal = 1 string ET ⇒ `DistinctEt ≤ K` SELALU ⇒ `DistinctEt > K` tak pernah true. Alert efektif dead-code, non-blocking by design. | **DEFERRED — do NOT author a positive-fire test** (would be impossible to make pass without redefining semantics, a design decision). Non-blocking core IS covered (S3) + no-false-positive (S3b). Fix = re-spec `DistinctEt` as cross-sibling-package ET pool vs `K = min count Section antar paket-saudara`. Owner: Plan 02 / re-spec (raise at Phase 419 or backlog). See `deferred-items.md`. |

*Golden-order + determinism + section-isolation + pooling + precedence + option-gate have automated unit verification; section-isolation/backward-compat/ET-non-blocking/parity have automated Playwright e2e. Only un-automated item is the **dead** ET-warning positive render (DEF-416-01) — correctly NOT faked.*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies (only un-automated = DEF-416-01 dead predicate, correctly deferred not faked)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (both Wave-0 files exist + green)
- [x] No watch-mode flags
- [x] Feedback latency < 90s (unit quick-run ~0.2s observed for shuffle filter)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** COMPLIANT — 2026-06-23 (gsd-nyquist-auditor)

**Nyquist verdict:** SHF-01/02/03/04 all have automated behavioral verification (unit + e2e). 9 unit + 5 e2e scenarios, 0 gaps requiring new tests. The single un-automated behavior (ET-warning positive render) is a documented dead predicate (DEF-416-01) — recorded as manual/deferred, intentionally NOT given a fake passing test. No implementation files modified; no implementation bugs found (DEF-416-01 is a non-blocking design nicety, not a behavioral bug).
