---
phase: 395
slug: mode-jawaban-input-asli-auto-generate
status: approved
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-18
---

# Phase 395 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (.NET, unit + integration real-SQL) + Playwright (e2e) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` · `tests/playwright.config.ts` |
| **Quick run command** | `dotnet test --filter "Category!=Integration"` (unit pure, no DB) |
| **Full suite command** | `dotnet test` + `cd tests && npx playwright test e2e/inject-assessment-395.spec.ts --workers=1` |
| **Estimated runtime** | unit ~20s · integration ~60-90s (disposable SQL) · e2e ~60s |

---

## Sampling Rate

- **After every task commit:** `dotnet build HcPortal.csproj` + (untuk task ber-test) `dotnet test --filter "Category!=Integration"`
- **After every plan wave:** `dotnet test` (incl. Integration real-SQL preview==commit + skip-omit + TextAnswer)
- **Before `/gsd-verify-work`:** Full suite hijau + `npx playwright test e2e/inject-assessment-395.spec.ts --workers=1` hijau
- **Max feedback latency:** ~90 seconds (full suite incl. integration)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 395-01-01 | 01 | 1 | INJ-09 | T-395-01/02 | Seed deterministik (no GetHashCode); ceiling tak di-cap | unit | `dotnet test --filter "FullyQualifiedName~BuildAutoGenAnswers&Category!=Integration"` | ❌ W0 (dibuat task ini) | ⬜ pending |
| 395-01-02 | 01 | 1 | INJ-08/09 | T-395-01/02/03 | BuildAutoGenAnswers re-cek floor; rule TextAnswer ber-guard mode | unit | `dotnet test --filter "FullyQualifiedName~BuildAutoGenAnswers&Category!=Integration"` | ✅ (dari 395-01-01) | ⬜ pending |
| 395-01-03 | 01 | 1 | INJ-08/09 | — | No regression; 0 migration | unit | `dotnet test --filter "Category!=Integration"` | ✅ | ⬜ pending |
| 395-02-01 | 02 | 2 | INJ-08/09 | — | DTO/VM mode/target TIDAK masuk InjectRequest (D-02) | build | `dotnet build HcPortal.csproj` | ✅ | ⬜ pending |
| 395-02-02 | 02 | 2 | INJ-08/09 | T-395-05..11 | RBAC+antiforgery preview; server-authoritative skor; no cert# preview; commit guard BLOCKING | build | `dotnet build HcPortal.csproj` | ✅ | ⬜ pending |
| 395-02-03 | 02 | 2 | INJ-08/09 | T-395-07 | preview==commit (real-SQL); skip=omit grade 0; TextAnswer-wajib reject | integration | `dotnet test --filter "Category=Integration&FullyQualifiedName~PreviewEqualsCommit"` | ❌ W0 (dibuat task ini) | ⬜ pending |
| 395-03-01 | 03 | 3 | INJ-08/09 | T-395-12/13/14 | XSS .textContent; CSRF token fetch; serialize #AnswersJson | build | `dotnet build HcPortal.csproj` | ✅ | ⬜ pending |
| 395-03-02 | 03 | 3 | INJ-08/09 | T-395-14 | #AnswersJson non-empty; skor di /CMP/Results > 0 (anti silent grade-0) | e2e | `cd tests && npx playwright test e2e/inject-assessment-395.spec.ts --workers=1` | ❌ W0 (dibuat task ini) | ⬜ pending |
| 395-03-03 | 03 | 3 | INJ-08/09 | T-395-15 | Live: preview==commit, BLOCKING, "seakan online" /CMP/Results | manual | checkpoint:human-verify (browser localhost:5277) | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/BuildAutoGenAnswersTests.cs` — unit pure (no DB, no [Trait Integration]): hit-target ≥target & smallest-such (equal-weight), boundary off-by-one (mixed-weight), ceiling-essay `TargetReachable=false`, seed reproducible (sama→sama, beda-room→beda, NIP-saja ditolak), degenerate (all-correct/1-opsi forced-correct), MA-salah. Pola: `AssessmentScoreAggregatorTests.cs` (in-memory builder). Dibuat oleh **395-01 Task 1** (RED-first). Covers INJ-09.
- [ ] `HcPortal.Tests/InjectPreviewEqualsCommitTests.cs` — integration real-SQL ([Trait Category=Integration], IClassFixture InjectAssessmentFixture disposable): preview `Aggregator.Compute` == `InjectBatchAsync` finalize skor; skip=omit grade 0; TextAnswer-wajib reject. Pola + helper VERBATIM: `InjectAssessmentServiceTests.cs:1-96`. Dibuat oleh **395-02 Task 3**. Covers INJ-08/09.
- [ ] `tests/e2e/inject-assessment-395.spec.ts` — Playwright: input-asli → skor benar; auto-gen → Pratinjau → commit → skor di /CMP/Results == preview; `#AnswersJson` terisi (anti silent grade-0). Pola: `inject-assessment-394.spec.ts`. Dibuat oleh **395-03 Task 2**. Covers INJ-08/09.
- [ ] Framework: xUnit + Playwright SUDAH ada — tidak ada install.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Preview skor live + BLOCKING + "seakan online" di /CMP/Results | INJ-08/09 | Wizard Razor+JS runtime + SignalR-free render + commit lifecycle = perlu browser interaktif; e2e meng-cover happy-path tetapi state BLOCKING (essay berat) + roster + overshoot lebih tuntas via mata manusia | 395-03 Task 3 checkpoint:human-verify — localhost:5277 main tree AD-off; lihat how-to-verify Plan 03 |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies (semua task code-producing ber-`<automated>`; checkpoint = manual eksplisit)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify (tiap task ber-build/test; checkpoint didahului e2e otomatis)
- [x] Wave 0 covers all MISSING references (3 file test: BuildAutoGenAnswersTests, InjectPreviewEqualsCommitTests, inject-assessment-395.spec.ts)
- [x] No watch-mode flags (Playwright `--workers=1`, no `--watch`; dotnet test no watch)
- [x] Feedback latency < 90s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-06-18
