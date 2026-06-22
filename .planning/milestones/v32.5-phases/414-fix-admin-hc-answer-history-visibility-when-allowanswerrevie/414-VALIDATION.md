---
phase: 414
slug: fix-admin-hc-answer-history-visibility-when-allowanswerrevie
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-22
validated: 2026-06-22
---

# Phase 414 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (net8.0) — project `HcPortal.Tests` |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter FullyQualifiedName~CanReviewAnswers` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~quick <5s helper / full suite ~minutes |

---

## Sampling Rate

- **After every task commit:** Run quick run command
- **After every plan wave:** Run full suite command
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** ~5 seconds (unit helper)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 414-01-04 | 01 | 1 | SC-1/2/3 (helper) | T-414-01 | Helper only loosens review AFTER IsResultsAuthorized passes; owner still gated | unit | `dotnet test --filter ~CanReviewAnswers` | ✅ | ✅ green (4/4) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/CanReviewAnswersTests.cs` — `[Theory]`+`[InlineData]` matrix mirroring `ResultsAuthorizationTests.cs` (4 cases: non-owner+OFF→true, owner+OFF→false, owner+ON→true, non-owner+ON→true)

*Existing xUnit infrastructure (HcPortal.Tests) covers; new test file only.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Section "Tinjauan Jawaban" per-soal render untuk Admin/HC saat AllowAnswerReview=false | SC-1 | Razor runtime — render gate hanya nyata di browser (lesson 354) | Login admin@pertamina.com → buka View Results sesi peserta dgn toggle OFF → section per-soal TAMPIL + nota admin |
| Owner (peserta) tetap diblok saat OFF | SC-2 | Persona owner butuh session worker | Login sebagai worker pemilik sesi → buka hasil sendiri dgn toggle OFF → alert "tidak tersedia" |
| Zero-regression toggle ON | SC-3 | Visual parity | Toggle ON → admin & owner lihat review seperti sebelum fix |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 5s (helper unit ~18ms)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-06-22

---

## Validation Audit 2026-06-22

| Metric | Count |
|--------|-------|
| Requirements/SC audited | SC-1, SC-2, SC-3, SC-4 |
| Gaps found (fillable automated) | 0 |
| Resolved | 0 (decision logic already COVERED by `CanReviewAnswersTests` 4/4) |
| Escalated → manual-only | 0 (render/wiring = manual + Playwright UAT gate by design, lesson 354) |

**Verdict:** NYQUIST-COMPLIANT. SC-1/SC-2/SC-3 decision logic fully covered by the pure-static helper unit test (4 InlineData, no-DB, lesson 999.12 honored). Dynamic Razor render + controller wiring are inherently manual (lesson 354) and verified live in the two-persona Playwright UAT gate (2h) — not a unit-fillable gap. SC-4 (build 0-err / suite 609/609 / migration=FALSE) confirmed independently by verifier. No tautological replica generated.
