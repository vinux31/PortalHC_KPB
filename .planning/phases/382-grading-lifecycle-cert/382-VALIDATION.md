---
phase: 382
slug: grading-lifecycle-cert
status: planned
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-14
---

# Phase 382 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (HcPortal.Tests, net8.0) + Playwright (tests/e2e) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` · `tests/e2e/playwright.config.ts` |
| **Quick run command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --nologo` |
| **Full suite command** | `dotnet test --nologo` then `npx playwright test --workers=1` |
| **Estimated runtime** | ~90s xUnit + ~3-5min e2e (workers=1, shared DB) |

> e2e env (MEMORY reference_local_e2e_sql_env_fix): AD lokal WAJIB `Authentication__UseActiveDirectory=false dotnet run`; combined Playwright WAJIB `--workers=1` (DB isolation); SQLBrowser + `lpc:` shared-memory conn override untuk login 500.

---

## Sampling Rate

- **After every task commit:** Run `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --nologo` (< 30s)
- **After every plan wave:** Run full xUnit + `npx playwright test --workers=1` (relevant e2e)
- **Phase gate (before `/gsd-verify-work`):** Full suite green **AND** `dotnet ef migrations list` confirms NO new migration scaffolded (Migration=false guard — D-01)
- **Max feedback latency:** 30 seconds (xUnit quick run)

---

## Per-Task Verification Map

| Req ID | Behavior | Test Type | Automated Command | File Exists | Status |
|--------|----------|-----------|-------------------|-------------|--------|
| WSE-06 (SAVE-01) | 2 SaveAnswer beda opsi → 1 baris final → Score dari opsi FINAL | integration (real SqlServer — InMemory tak race) | `dotnet test --filter "FullyQualifiedName~SaveAnswerConcurrent"` | ❌ W0 | ⬜ pending |
| WSE-06 (SAVE-01) | GradingService dedupe-read pilih SubmittedAt terbaru | unit | `dotnet test --filter "FullyQualifiedName~GradingDedupe"` | ❌ W0 | ⬜ pending |
| WSE-07 (STAT-01) | Submit sesi Abandoned/Cancelled/PendingGrading → reject, tak jadi Completed | unit/integration | `dotnet test --filter "FullyQualifiedName~SubmitResurrection"` | ❌ W0 | ⬜ pending |
| WSE-08 (STAT-02) | AbandonExam sesi Completed → rowsAffected==0, status tetap Completed | integration | `dotnet test --filter "FullyQualifiedName~AbandonGuard"` | ❌ W0 | ⬜ pending |
| WSE-09 (TMR-01) | Standard elapsed>allowed tanpa token → ditolak + audit SubmitExamBlocked; on-time diterima | unit | `dotnet test --filter "FullyQualifiedName~EnsureCanSubmitStandard"` | ❌ W0 | ⬜ pending |
| WSE-09 (TMR-03) | AutoSubmitToken tak dikonsumsi sebelum grading commit (retry aman) | unit | `dotnet test --filter "FullyQualifiedName~AutoSubmitTokenRetry"` | ❌ W0 | ⬜ pending |
| WSE-10 (TOK-02) | SaveAnswer/SubmitExam token-required + StartedAt==null → reject | unit/integration | `dotnet test --filter "FullyQualifiedName~TokenGateSaveSubmit"` | ❌ W0 | ⬜ pending |
| WSE-11 (CERT-01) | `DeriveCertificateStatus(null,null)` → Aktif/Permanen (BUKAN Expired) | unit | `dotnet test --filter "FullyQualifiedName~CertificateStatus"` | ✅ REWRITE L31-36 | ⬜ pending |
| WSE-11 (CERT-01) | Badge+notif+renewal tally konsisten untuk cert null | unit/integration | `dotnet test --filter "FullyQualifiedName~CertAlertConsistency"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## E2E Acceptance (audit scenarios #8-12 = spec acceptance)

| Scenario | REQ | Assertion inti | File |
|----------|-----|----------------|------|
| #8 anti-resurrection | STAT-01 | Abandon→SubmitExam ditolak, tak Completed/cert; Cancelled idem | `tests/e2e/exam-taking.spec.ts` (extend) |
| #9 abandon tak menimpa | STAT-02 | Completed→AbandonExam rowsAffected==0, verdict+cert tetap di Results/Records | `tests/e2e/exam-taking.spec.ts` (extend) |
| #10 concurrent save | SAVE-01 | 2 SaveAnswer beda opsi → 1 baris final → Score benar | integration (preferred) atau exam-taking.spec.ts |
| #11 timer Standard | TMR-01 | StartedAt mundur (seed) → submit manual ditolak + audit; on-time diterima | `tests/e2e/exam-taking.spec.ts` (extend) |
| #12 cert visibility | CERT-01 | lulus ValidUntil=null → Results LULUS+PDF + dashboard Aktif/Permanen + badge/notif konsisten | `tests/e2e/exam-taking.spec.ts` + dashboard assert |
| #4 PrePost same-day | post-382 acceptance | butuh grading 382 + entry 381 — Wave acceptance, BUKAN gate phase ini | `tests/e2e/exam-taking.spec.ts` |

---

## Wave 0 Requirements

- [ ] Integration fixture real-SqlServer untuk concurrent SaveAnswer (pola `ProtonCompletionFixture` Phase 365) — covers WSE-06
- [ ] Unit `GradingDedupeTests` — covers WSE-06 read-final
- [ ] Unit/integration `SubmitResurrectionTests` — covers WSE-07
- [ ] Integration `AbandonGuardTests` — covers WSE-08
- [ ] Unit `EnsureCanSubmitStandardTests` + `AutoSubmitTokenRetryTests` — covers WSE-09
- [ ] Unit/integration `TokenGateTests` (SaveAnswer/SubmitExam StartedAt-gate) — covers WSE-10
- [ ] REWRITE `HcPortal.Tests/CertificateStatusTests.cs:31-36` (DeriveCertificateStatus null→Aktif) + new `CertAlertConsistencyTests` — covers WSE-11
- [ ] Extend `tests/e2e/exam-taking.spec.ts` scenario #8-12 (helper `examTypes.ts` + `dbSnapshot` sudah ada)

*(Framework sudah terpasang — tidak perlu install.)*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Dashboard CMP/CDP/Renewal label "Aktif/Permanen" untuk cert null tampil benar visual | WSE-11 | Visual cross-surface (3 dashboard) | Browser headed: worker lulus ValidUntil=null → buka CMP cert dashboard + CDP + Renewal worklist → assert tak ada "Expired", badge Home konsisten |

*Sisanya automated.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (8 Wave-0 gaps di atas)
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [x] `nyquist_compliant: true` set in frontmatter
- [ ] Migration=false guard: `dotnet ef migrations list` unchanged after phase

**Approval:** pending
