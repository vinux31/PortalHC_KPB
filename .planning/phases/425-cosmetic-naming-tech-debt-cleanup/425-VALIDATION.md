---
phase: 425
slug: cosmetic-naming-tech-debt-cleanup
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-24
---

# Phase 425 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (.NET 8) |
| **Config file** | none — existing test project `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~ExamTimeRules\|FullyQualifiedName~ManualEntryRules\|FullyQualifiedName~ControllerGuards"` |
| **Full suite command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| **Baseline (post-424)** | suite 748 pass / 0 fail / 2 skip — 425 jaga 0 regresi (+ test baru parity/cross-validate/shape) |

---

## Sampling Rate

- **After every task commit:** Run quick run command (+ `dotnet build HcPortal.csproj` 0 error)
- **After every plan wave:** Run full suite command (jaga 748/0/2 + test baru hijau)
- **Before `/gsd-verify-work`:** Full suite must be green + `dotnet build` 0 error/0 warning baru + UAT manual @5270 (CLN-02 warning hidup, CLN-01 label tampil)
- **Max feedback latency:** suite runtime (~baseline)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 425-01-01 | 01 | 1 | CLN-03, CLN-01 | T-425-01 | AssessmentPhase RESERVED (kolom tetap, no migration); komentar Status 7-nilai | static/grep | `grep "RESERVED" Models/AssessmentSession.cs` + `dotnet build` | ✅ (grep) | ✅ green |
| 425-01-02 | 01 | 1 | CLN-01 | T-425-02 | ValidUntil [Display] sumber-tunggal; LinkedSessionId XML-doc dikoreksi (no "ON DELETE SET NULL") | static/grep | `grep 'Berlaku Sampai' + ! grep 'ON DELETE SET NULL'` + `dotnet build` | ✅ (grep) | ✅ green |
| 425-01-03 | 01 | 1 | CLN-01 | T-425-03 | AssessmentPackageId sentinel (nama field tak berubah); label cshtml selaras | static/grep | `grep "SENTINEL (PA-05)"` + `dotnet build` | ✅ (grep) | ✅ green |
| 425-02-01 | 02 | 1 (W0) | CLN-04 | T-425-04 | Parity helper == formula lama 4 situs (incl double-site :4661) | unit (pure) | `dotnet test --filter ~ExamTimeRulesTests` | ✅ EXTEND ExamTimeRulesTests.cs | ✅ green |
| 425-02-02 | 02 | 1 | CLN-04 | T-425-04/05/06 | 4 situs → ExamTimeRules; formula inline habis; token gate/StartExam tak disentuh (D-03) | unit + static | `dotnet build` + `dotnet test --filter ~ExamTimeRules` + token-gate grep | ✅ (test+grep) | ✅ green |
| 425-03-01 | 03 | 1 (W0) | CLN-02 | T-425-10 | PassStatusMismatch: mismatch→true / match→false / null→false / boundary→false | unit (pure) | `dotnet test --filter ~ManualEntryRules` | ✅ ManualEntryRules.cs + ManualEntryRulesTests.cs | ✅ green |
| 425-03-02 | 03 | 1 | CLN-02 | T-425-07/08/09 | Warning non-blocking (TETAP simpan, no auto-override); CSRF/authz utuh; XSS-safe (numerik-only) | unit + static | `dotnet build` + `dotnet test --filter ~ManualEntryRules` + authz/CSRF grep | ✅ (test+grep) | ✅ green |
| 425-04-01 | 04 | 1 (W0) | CLN-05 | T-425-13 | JsonFail shape byte-identik {"success":false,"message":"..."} camelCase | unit (pure) | `dotnet test --filter ~ControllerGuards` | ✅ ControllerGuards.cs + ControllerGuardsTests.cs | ✅ green |
| 425-04-02 | 04 | 1 | CLN-05 | T-425-11/12/13/14 | Cluster SubmitEssayScore → JsonFail (pesan identik); call-site lain tak berubah; authz/CSRF + signature utuh | unit + static | `dotnet build` + `dotnet test --filter ~ControllerGuards` + authz/CSRF grep | ✅ (test+grep) | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**Sampling continuity:** Setiap plan punya ≥1 task ber-`<automated>` test (CLN-04/02/05) atau grep-static (CLN-01/03). Tidak ada 3 task beruntun tanpa automated verify.

---

## Wave 0 Requirements

- [x] **CLN-04** — EXTEND `HcPortal.Tests/ExamTimeRulesTests.cs` dengan [Theory] parity 4 situs (incl double-site :4661). File ADA, tambah kasus. (Plan 02 Task 1)
- [x] **CLN-02** — NEW `Helpers/ManualEntryRules.cs` (pure `PassStatusMismatch`) + NEW `HcPortal.Tests/ManualEntryRulesTests.cs` (mismatch→warning / match→no-warning / null-safe / boundary). (Plan 03 Task 1)
- [x] **CLN-05** — NEW `Helpers/ControllerGuards.cs` (`JsonFail`) + NEW `HcPortal.Tests/ControllerGuardsTests.cs` (shape JSON byte-identik). (Plan 04 Task 1)
- *Framework install:* tidak perlu — xUnit 2.9.3 sudah terpasang.

**Wave 0 strategy:** Tiga test artifact (parity / cross-validate / shape) dibuat sebagai task PERTAMA di plannya masing-masing (TDD `tdd="true"` — test sebelum implementasi consumer). Logika testable diekstrak ke pure helper (ManualEntryRules, ControllerGuards) agar bebas-DB. CLN-01/CLN-03 = grep/static-verifiable (bukan Wave-0 test, bukan manual-gate).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Warning kuning tampil pasca-submit mismatch + assessment tetap tersimpan | CLN-02 | Verifikasi UX end-to-end (TempData survive redirect, render alert) butuh browser | @5270 Form AddManualAssessment: Score=60, Pass=70, centang Lulus → submit → cek DB tersimpan + alert kuning muncul "Ditandai Lulus walau Score 60 < Pass 70%..." |
| Label "Berlaku Sampai" tampil konsisten di 3 form | CLN-01 | Render label cshtml visual | @5270 buka Create/Edit/AddManual assessment → label expiry sertifikat = "Berlaku Sampai" |
| Guard SubmitEssayScore respons JSON identik di UI | CLN-05 | Frontend JS membaca data.success/data.message | @5270 EssayGrading → picu guard (skor di luar range) → toast/error muncul identik seperti sebelumnya |

*CLN-04 & CLN-02/CLN-05 logic = automated (pure test). Manual = UX confirm only, bukan gate utama.*

---

## Validation Audit 2026-06-24

| Metric | Count |
|--------|-------|
| Requirements | 5 (CLN-01..05) |
| Gaps found | 0 |
| Resolved | 0 (none needed) |
| Escalated | 0 |

**Verdict: NYQUIST-COMPLIANT (State A audit — no gaps).** Semua requirement testable COVERED + green: CLN-04 `ExamTimeRulesTests` (parity 4 situs incl double-site), CLN-02 `ManualEntryRulesTests` (mismatch/match/null/boundary), CLN-05 `ControllerGuardsTests` (shape byte-identik) → **23/23 green** (`dotnet test --filter ~ExamTimeRules|~ManualEntryRules|~ControllerGuards`). Full suite **768/0/2**, build 0 err. CLN-01/CLN-03 = static/grep-verifiable (RESERVED kolom tetap, label "Berlaku Sampai", no positive "ON DELETE SET NULL", sentinel) — dikonfirmasi gsd-verifier 5/5. No test generation needed.

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify atau grep-static-verifiable atau Wave 0 dependency
- [x] Sampling continuity: no 3 consecutive tasks without automated/static verify
- [x] Wave 0 covers all MISSING references (ManualEntryRulesTests + ControllerGuardsTests NEW; ExamTimeRulesTests EXTEND)
- [x] No watch-mode flags (`dotnet test` one-shot)
- [x] Feedback latency acceptable (~baseline suite)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** validated 2026-06-24 — NYQUIST-COMPLIANT, 0 gaps
