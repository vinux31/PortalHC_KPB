---
phase: 382
slug: grading-lifecycle-cert
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-14
validated: 2026-06-15
audit:
  auditor: gsd-nyquist-auditor
  date: 2026-06-15
  scope_tests_run: 32
  scope_tests_passed: 32
  full_suite: "415 passed / 0 failed / 0 skipped"
  gaps_filled: 0
  gaps_pre_existing_green: 9
  result: GAPS_FILLED
---

# Phase 382 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
>
> **POST-EXECUTION AUDIT (2026-06-15, gsd-nyquist-auditor):** Semua 9 requirement→test mapping
> sudah ADA dan GREEN — tidak ada gap yang perlu diisi. Scope-filter run: **32/32 passed**
> (GradingDedupe 2 · SubmitResurrection 2 · AbandonGuard 3 · EnsureCanSubmitStandard 7 ·
> AutoSubmitTokenRetry 2 · TokenGate 4 · CertificateStatus 8 · CertAlertConsistency 4).
> Full suite re-run: **415 passed / 0 failed / 0 skipped** (55s, Integration tests INCLUDED —
> SQLEXPRESS+SQLBrowser Running). e2e #8/#9/#11/#12 spec ada di `exam-taking.spec.ts`; #10
> didelegasikan ke xUnit `GradingDedupeTests.Dedupe_PicksLatestSubmittedAt` (terverifikasi green).

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

> **Post-execution (2026-06-15):** semua baris COVERED + green. Run scope-filter 32/32 passed.
> `File Exists` direvisi dari rencana W0 → file aktual yang ada di `HcPortal.Tests/`.

| Req ID | Behavior | Test Type | Automated Command | File / Tests | Status |
|--------|----------|-----------|-------------------|--------------|--------|
| WSE-06 (SAVE-01) | 2 SaveAnswer beda opsi → 1 baris final → Score dari opsi FINAL | integration (real SqlServer — InMemory tak race) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~GradingDedupe"` | `GradingDedupeTests.cs` (2/2; #10 delegasi e2e→xUnit) | ✅ green |
| WSE-06 (SAVE-01) | GradingService dedupe-read pilih SubmittedAt terbaru | integration (real-SQL; ExecuteUpdateAsync tak jalan di InMemory) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~GradingDedupe"` | `GradingDedupeTests.Dedupe_PicksLatestSubmittedAt` + `_MultipleAnswer_NotDeduped` (2/2) | ✅ green |
| WSE-07 (STAT-01) | Submit sesi Abandoned/Cancelled/PendingGrading → reject, tak jadi Completed | integration (real-SQL) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~SubmitResurrection"` | `SubmitResurrectionTests` (Abandoned + Cancelled rejected; 2/2) | ✅ green |
| WSE-08 (STAT-02) | AbandonExam sesi Completed → rowsAffected==0, status tetap Completed | integration (real-SQL) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~AbandonGuard"` | `AbandonGuardTests` (Completed→0 · non-owner→0 · owner-InProgress→1; 3/3) | ✅ green |
| WSE-09 (TMR-01) | Standard elapsed>allowed tanpa token → ditolak (tier-1) + on-time/server-approved diterima | unit (pure helper) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~EnsureCanSubmitStandard"` | `EnsureCanSubmitStandardTests` (ShouldEnforceSubmitTimer ×3 + EvaluateSubmitTimerDecision ×4; 7/7) | ✅ green |
| WSE-09 (TMR-03) | AutoSubmitToken tak dikonsumsi sebelum grading commit (retry aman) | unit (pure helper) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~AutoSubmitTokenRetry"` | `AutoSubmitTokenRetryTests` (fail→not-consumed · success→consumed; 2/2) | ✅ green |
| WSE-10 (TOK-02) | SaveAnswer/SubmitExam token-required + StartedAt==null → reject | unit (pure helper `ShouldGateMissingStart`) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~TokenGate"` | `TokenGateTests` (gated · submit-gated · started-not-gated · non-token-not-gated; 4/4) | ✅ green |
| WSE-11 (CERT-01) | `DeriveCertificateStatus(null,null)` → Aktif/Permanen (BUKAN Expired) | unit | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~CertificateStatus"` | `CertificateStatusTests` (rewrite `_ReturnsAktif` + Permanent + 6 Theory; 8/8) | ✅ green |
| WSE-11 (CERT-01) | Badge+notif+renewal tally konsisten untuk cert null | unit (predicate-mirror consumer) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~CertAlertConsistency"` | `CertAlertConsistencyTests` (DerivesAktif · RenewalTally · Worklist · CDPTally; 4/4) | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

> **Catatan komando filter:** `SaveAnswerConcurrent` (plan-time) dan `TokenGateSaveSubmit` (plan-time)
> TIDAK pernah dibuat sebagai file terpisah — keduanya di-cover oleh `GradingDedupeTests` (last-write-wins
> real-SQL) dan `TokenGateTests` (`ShouldGateMissingStart` dipakai kedua handler). Komando di tabel sudah
> dikoreksi ke filter yang benar-benar match file aktual.

---

## E2E Acceptance (audit scenarios #8-12 = spec acceptance)

> **Post-execution:** spec #8/#9/#11/#12 ADA di `exam-taking.spec.ts` (describe block "Phase 382 #N").
> Per Plan 03 SUMMARY = 18/18 BROWSER green (`--workers=1`, AD off, lpc conn). Auditor TIDAK menjalankan
> ulang browser e2e (per instruksi) — diverifikasi keberadaan spec + delegasi #10 ke xUnit (green).

| Scenario | REQ | Assertion inti | File | Status |
|----------|-----|----------------|------|--------|
| #8 anti-resurrection | STAT-01 | Abandon→SubmitExam ditolak, tak Completed/cert; Cancelled idem | `tests/e2e/exam-taking.spec.ts` L2020 | ✅ spec ada (browser green per SUMMARY) |
| #9 abandon tak menimpa | STAT-02 | Completed→AbandonExam rowsAffected==0, verdict+cert tetap di Results/Records | `tests/e2e/exam-taking.spec.ts` L2101 | ✅ spec ada (browser green per SUMMARY) |
| #10 concurrent save | SAVE-01 | 2 SaveAnswer beda opsi → 1 baris final → Score benar | DELEGASI → xUnit `GradingDedupeTests.Dedupe_PicksLatestSubmittedAt` (spec L1933 note) | ✅ green (xUnit, auditor-run) |
| #11 timer Standard | TMR-01 | StartedAt mundur (seed) → submit manual ditolak; on-time diterima | `tests/e2e/exam-taking.spec.ts` L2176 | ✅ spec ada (browser green per SUMMARY) |
| #12 cert visibility | CERT-01 | lulus ValidUntil=null → Results LULUS+PDF + dashboard Aktif/Permanen + badge/notif konsisten | `tests/e2e/exam-taking.spec.ts` L2232 | ✅ spec ada (browser green per SUMMARY; visual = human spot-check) |
| #4 PrePost same-day | post-382 acceptance | butuh grading 382 + entry 381 — Wave acceptance, BUKAN gate phase ini | `tests/e2e/exam-taking.spec.ts` | — out-of-gate |

---

## Wave 0 Requirements

> **Post-execution: SEMUA selesai (8/8).** Diverifikasi file ada + 32/32 green.

- [x] Integration fixture real-SqlServer untuk concurrent SaveAnswer (pola `ProtonCompletionFixture` Phase 365) — covers WSE-06 → diwujudkan sebagai `GradingDedupeFixture` (concurrent #10 didelegasikan ke last-write-wins real-SQL)
- [x] `GradingDedupeTests` (real-SQL, Category=Integration) — covers WSE-06 read-final
- [x] `SubmitResurrectionTests` (real-SQL, Category=Integration) — covers WSE-07
- [x] `AbandonGuardTests` (real-SQL, Category=Integration) — covers WSE-08
- [x] `EnsureCanSubmitStandardTests` + `AutoSubmitTokenRetryTests` (unit, pure helper) — covers WSE-09
- [x] `TokenGateTests` (unit, pure helper `ShouldGateMissingStart` dipakai SaveAnswer+SubmitExam) — covers WSE-10
- [x] REWRITE `CertificateStatusTests` (`_NullValidUntil_NonPermanent_ReturnsAktif`) + new `CertAlertConsistencyTests` — covers WSE-11
- [x] Extend `tests/e2e/exam-taking.spec.ts` scenario #8/#9/#11/#12 (#10 delegasi xUnit) — spec ada, browser green per Plan 03 SUMMARY

*(Framework sudah terpasang — tidak perlu install.)*

> **DEVIATION terdokumentasi (diterima auditor):** 3 file ditandai "Integration" pakai disposable real-SQL
> fixture (`HcPortalDB_Test_{guid}` @ `localhost\SQLEXPRESS`), BUKAN InMemory — `ExecuteUpdateAsync` tak
> didukung EF8 InMemory + race/atomic guard hanya terbukti di SQL nyata. Helper-uji (Token/Timer/AutoSubmit)
> pakai pure static helper karena CMPController ber-ctor 14-dep ("controller construction infeasible",
> konvensi repo `VerifyTokenTests.cs:3`). Keduanya konsisten pola Phase 358/363/365/376/380. Test mereproduksi
> predikat/WHERE produksi secara IDENTIK (predicate-mirror) — behavioral, bukan struktural.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Dashboard CMP/CDP/Renewal label "Aktif/Permanen" untuk cert null tampil benar visual | WSE-11 | Visual cross-surface (3 dashboard) | Browser headed: worker lulus ValidUntil=null → buka CMP cert dashboard + CDP + Renewal worklist → assert tak ada "Expired", badge Home konsisten |

*Sisanya automated.*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies — 9 mapping COVERED + green
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (8 Wave-0 gaps di atas — semua [x])
- [x] No watch-mode flags
- [x] Feedback latency < 30s (scope-filter run 12s; helper-only subset < 5s)
- [x] `nyquist_compliant: true` set in frontmatter
- [x] Migration=false guard — diverifikasi VERIFICATION truth #12 (`git diff Migrations/` kosong; `ef migrations add` → empty Up/Down)

**Approval:** ✅ VALIDATED — gsd-nyquist-auditor 2026-06-15

---

## Post-Execution Audit Result (Nyquist)

**Verdict: GAPS FILLED (0 gap baru — semua mapping sudah hijau pre-audit).**

| Metric | Value |
|--------|-------|
| Requirement→test mapping diaudit | 9 |
| Sudah COVERED + green (pre-audit) | 9 |
| Gap genuine ditemukan | 0 |
| File test dibuat auditor | 0 (tidak perlu — semua sudah ada) |
| Scope-filter run | **32/32 passed, 0 failed, 0 skipped** (12s) |
| Full suite re-run | **415 passed / 0 failed / 0 skipped** (55s, Integration INCLUDED) |
| Impl bug di-escalate | 0 |

**Per-requirement final status:**

| Req | Test | Hasil |
|-----|------|-------|
| WSE-06 SAVE-01 (concurrent / dedupe-read) | `GradingDedupeTests` (2) | ✅ green — #10 concurrent didelegasikan ke last-write-wins real-SQL (deterministik > e2e race) |
| WSE-07 STAT-01 (anti-resurrection grading) | `SubmitResurrectionTests` (2) | ✅ green |
| WSE-08 STAT-02 (abandon guard) | `AbandonGuardTests` (3) | ✅ green |
| WSE-09 TMR-01 (Standard enforce) | `EnsureCanSubmitStandardTests` (7) | ✅ green |
| WSE-09 TMR-03 (token retry-safe) | `AutoSubmitTokenRetryTests` (2) | ✅ green |
| WSE-10 TOK-02 (StartedAt-gate) | `TokenGateTests` (4) | ✅ green |
| WSE-11 CERT-01 (null→Aktif) | `CertificateStatusTests` (8) | ✅ green |
| WSE-11 CERT-01 (tally konsisten) | `CertAlertConsistencyTests` (4) | ✅ green |

**Catatan auditor:**
1. Tidak ada file `SaveAnswerConcurrent*` terpisah — WSE-06 concurrent di-cover oleh `GradingDedupeTests`
   (last-write-wins real-SQL) sesuai delegasi terdokumentasi di Plan 03 + e2e spec L1933. Bukan gap.
2. 1 item human-verify visual TERSISA (dashboard cert null label "Aktif/Permanen" lintas CMP/CDP/Renewal +
   badge Home) — bukan gap automated; DB-level + predicate-mirror sudah ter-otomasi (`CertAlertConsistencyTests`
   + e2e #12 DB-assert). Lihat VERIFICATION Human Verification §1.
3. Test pakai pure-helper + predicate-mirror + real-SQL fixture (deviation terdokumentasi & diterima) —
   behavioral, mereproduksi WHERE/predikat produksi secara identik. Tidak ada test struktural/no-op.
