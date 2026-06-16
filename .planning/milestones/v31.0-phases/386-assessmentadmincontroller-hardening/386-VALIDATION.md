---
phase: 386
slug: assessmentadmincontroller-hardening
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-15
---

# Phase 386 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution. Source: 386-RESEARCH.md §Validation Architecture (all test patterns verified against existing repo test files).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework (unit/integration)** | xUnit 2.9.3 (`HcPortal.Tests/`) |
| **Framework (e2e)** | Playwright @playwright/test ^1.58.2 (`tests/`) |
| **Config file (unit)** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Config file (e2e)** | `tests/playwright.config.ts` (baseURL `localhost:5277`, `fullyParallel:false`) |
| **Quick run command** | `dotnet test --filter "Category!=Integration"` (pure unit, no SQL) |
| **Full suite command** | `dotnet test` (unit + integration real-SQL) |
| **E2E run command** | `cd tests; npx playwright test <spec> --workers=1` |
| **Estimated runtime** | quick ~30s · full ~2-4min (real-SQL disposable DB) |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (0 error) + `dotnet test --filter "Category!=Integration"` (pure unit, <30s)
- **After every plan wave:** `dotnet test` (full, incl. integration real-SQL)
- **Before `/gsd-verify-work`:** `dotnet test` full green + `dotnet run` (localhost:5277, `Authentication__UseActiveDirectory=false`) manual golden+edge + Playwright spec green `--workers=1`
- **Max feedback latency:** ~30 seconds (quick unit)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 386-W0 | 00 | 0 | infra | — | N/A | scaffold | (create test files RED) | ❌ W0 | ⬜ pending |
| PXF-02-u | — | 1 | PXF-02 | — | Soal MC/MA 0-opsi & opsi-benar-tanpa-teks DITOLAK; ≥2 ber-teks diterima | unit | `dotnet test --filter "FullyQualifiedName~OptionValidation"` | ❌ W0 `OptionValidationTests.cs` | ⬜ pending |
| PXF-02-e | — | 1 | PXF-02 | — | Admin simpan Single 0-opsi → ditolak `.alert-danger`, soal tak tersimpan | e2e | `cd tests; npx playwright test option-validation-386 --workers=1` | ❌ W0 `option-validation-386.spec.ts` | ⬜ pending |
| PXF-04-p | — | 2 | PXF-04 | T-386-AUTHZ | COUNT pending identik 4 surface × 4 fixture (no-row, whitespace, filled-ungraded, graded) | integration | `dotnet test --filter "FullyQualifiedName~EssayEmptyPendingParity"` | ❌ W0 `EssayEmptyPendingParityTests.cs` | ⬜ pending |
| PXF-04-g | — | 2 | PXF-04 | T-386-AUTHZ | SubmitEssayScore upsert (no-row→create) + status-guard reject non-PendingGrading | integration | (same file) | ❌ W0 | ⬜ pending |
| PXF-04-e | — | 2 | PXF-04 | — | HC finalize sesi ≥1 essay kosong → tombol "Selesaikan" muncul → round-trip, essay kosong=0 | e2e | `cd tests; npx playwright test essay-empty-finalize-386 --workers=1` | ❌ W0 `essay-empty-finalize-386.spec.ts` | ⬜ pending |
| PXF-05-u | — | 3 | PXF-05 | — | MA benar={A,C,D} ⇒ "Benar" + Jawaban=semua opsi; partial/superset ⇒ "Salah"; MC byte-identik | unit | `dotnet test --filter "FullyQualifiedName~BuildAnswerCell"` | ❌ W0 `PdfAnswerCellTests.cs` | ⬜ pending |
| 386-authz | — | * | PXF-04 | T-386-AUTHZ | `[Authorize(Roles="Admin, HC")]` tetap di SubmitEssayScore/FinalizeEssayGrading | unit (reflection) | `dotnet test --filter "FullyQualifiedName~Authz"` | ⚠️ extend `EssaySubmitFinalizeAuthzTests` | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/OptionValidationTests.cs` — PXF-02. Pure if validasi diekstrak ke helper `ValidateQuestionOptions(type, texts, corrects)`; else mirror-data. (pola `IsQuestionCorrectTests.cs`)
- [ ] `HcPortal.Tests/EssayEmptyPendingParityTests.cs` — PXF-04 count-parity 4 fixture + upsert + status-guard. `[Trait("Category","Integration")]`, reuse + extend `EssayFinalizeRecomputeTests.SeedEssayOnlyAsync` (`:71-99`) + mirror builders (`MirrorSubmitEssayScoreAsync:155-171`, `MirrorFinalizeWriteAsync:295-311`).
- [ ] `HcPortal.Tests/PdfAnswerCellTests.cs` — PXF-05 `BuildAnswerCell` MA join + `IsQuestionCorrect` MA label. Pure (pola `IsQuestionCorrectTests.cs`).
- [ ] `tests/e2e/option-validation-386.spec.ts` — PXF-02 reject path (selectors `#QuestionType`, `#option_A..D`, `#correct_A..D`, submit; assert `.alert-danger` + soal absent di list).
- [ ] `tests/e2e/essay-empty-finalize-386.spec.ts` — PXF-04 finalize round-trip (reuse helpers `tests/e2e/helpers/examTypes.ts`).
- [ ] Extend `HcPortal.Tests/...AuthzTests` (reflection) — assert `[Authorize(Roles="Admin, HC")]` retained after D-08 status-guard edit.
- Framework install: existing (`dotnet restore`; e2e `cd tests; npm install; npx playwright install chromium` if needed).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| PDF per-peserta MA label + Jawaban visual benar di file PDF nyata | PXF-05 | QuestPDF byte output — unit covers helper; visual layout is human-judged | `dotnet run` → admin BulkExportPdf sesi dengan soal MA → buka PDF → cek label "Benar/Salah" + kolom Jawaban list semua opsi MA |
| UAT final di Dev `http://10.55.3.3/KPB-PortalHC` pasca re-deploy | PXF-02/04/05 | Tak boleh edit Dev; verifikasi end-to-end env nyata = tanggung jawab user/IT | Setelah IT re-deploy: ulangi golden+edge tiap REQ di Dev |

---

## Validation Sign-Off

- [ ] All tasks have automated verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (6 files above)
- [ ] No watch-mode flags (Playwright `--workers=1`, no `--watch`)
- [ ] Feedback latency < 30s (quick unit)
- [ ] `nyquist_compliant: true` set in frontmatter (after planner wires tasks)

**Approval:** pending
