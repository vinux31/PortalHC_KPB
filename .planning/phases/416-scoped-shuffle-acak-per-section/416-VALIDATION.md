---
phase: 416
slug: scoped-shuffle-acak-per-section
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-23
---

# Phase 416 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~ShuffleEngine"` |
| **Full suite command** | `dotnet test HcPortal.Tests` |
| **Estimated runtime** | ~60-90 seconds |

---

## Sampling Rate

- **After every task commit:** Run quick command (`--filter ~ShuffleEngine`)
- **After every plan wave:** Run full suite command
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 90 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 416-engine | 01 | 1 | SHF-01 | — | section-isolation: soal tak bocor antar-Section | unit | `dotnet test --filter ~ShuffleEngine` | ❌ W0 | ⬜ pending |
| 416-golden | 01 | 1 | SHF-04 | — | all-null == baseline pra-416 (golden-order) | unit | `dotnet test --filter ~GoldenOrder` | ❌ W0 | ⬜ pending |
| 416-determinism | 01 | 1 | SHF-04 | — | workerIndex deterministik (fixed seed) | unit | `dotnet test --filter ~ShuffleEngine` | ❌ W0 | ⬜ pending |
| 416-precedence | 01 | 1 | SHF-02 | — | induk OFF→semua urut; induk ON→ikut per-Section | unit | `dotnet test --filter ~ShuffleEngine` | ❌ W0 | ⬜ pending |
| 416-pooling | 01 | 1 | SHF-03 | — | >1 paket: pooling lintas-paket dalam batas Section | unit | `dotnet test --filter ~ShuffleEngine` | ❌ W0 | ⬜ pending |
| 416-reshuffle | 02 | 2 | SHF-03 | — | ReshufflePackage/All re-roll tetap section-isolated | unit | `dotnet test --filter ~Reshuffle` | ❌ W0 | ⬜ pending |
| 416-et-warning | 02 | 2 | SHF-03 | — | K < distinct-ET → warning (non-blocking) | unit | `dotnet test --filter ~EtCoverage` | ❌ W0 | ⬜ pending |
| 416-uat | 03 | 3 | SHF-01..04 | — | real-browser acak per-Section + all-null identik | e2e | Playwright (manual gate, §5 autopilot) | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/ShuffleEngineScopedTests.cs` — unit stubs: section-isolation, precedence, pooling, ET-warning (SHF-01/02/03)
- [ ] `HcPortal.Tests/ShuffleEngineGoldenOrderTests.cs` — golden-order all-null == baseline + workerIndex determinism (SHF-04)
- [ ] Fixed-seed helper `new Random(42)` pattern for determinism (reuse existing ShuffleEngine test convention if present)

*Engine is pure (no DB) → unit-testable directly. Reshuffle/StartExam wiring covered by integration where a seam exists; otherwise asserted via engine-level isolation tests + Playwright UAT.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Acak per-Section terlihat di layar ujian (Razor render) | SHF-01 | Razor/JS render runtime — lesson 354 (unit tak nangkap render) | Playwright UAT: buat assessment 2 Section, induk ON, ambil ujian, assert urutan soal tak melompat antar-Section |
| All-null = urutan identik perilaku lama (visual) | SHF-04 | Backward-compat visual proof | Playwright UAT: assessment tanpa Section, bandingkan urutan vs baseline |

*Golden-order + determinism + section-isolation + pooling have automated unit verification; only live-render proof is manual (Playwright UAT gate).*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 90s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
