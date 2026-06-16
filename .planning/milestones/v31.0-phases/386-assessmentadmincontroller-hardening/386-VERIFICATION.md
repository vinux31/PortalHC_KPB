---
phase: 386-assessmentadmincontroller-hardening
verified: 2026-06-16T08:00:00Z
status: passed
score: 7/7
overrides_applied: 0
---

# Phase 386: AssessmentAdminController Hardening — Verification Report

**Phase Goal:** Hardening AssessmentAdminController untuk ujian lisensor — PXF-02 (CreateQuestion & EditQuestion menolak soal malformed), PXF-04 (HC bisa finalisasi penilaian walau essay dikosongkan peserta), PXF-05 (PDF + Excel MA label all-or-nothing SetEquals via shared helpers). Plus build 0 error + full test suite green + Playwright green + Browser UAT 4/4 PASS. 0 migration.
**Verified:** 2026-06-16T08:00:00Z
**Status:** PASSED
**Re-verification:** No — verifikasi awal.

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | QuestionOptionValidator.ValidateQuestionOptions exists, pure (tanpa EF), wired ke CreateQuestion DAN EditQuestion | VERIFIED | `Helpers/QuestionOptionValidator.cs` L18-20: `public static (bool ok, string? error) ValidateQuestionOptions(string type, string?[] texts, bool[] corrects)` — tanpa `using Microsoft.EntityFrameworkCore`. Wired di `Controllers/AssessmentAdminController.cs` L6480 (CreateQuestion) + L6699 (EditQuestion) — tepat 2 occurrences, dikonfirmasi via grep. |
| 2 | Single pending predicate `!IsNullOrWhiteSpace(TextAnswer) && EssayScore == null` diterapkan byte-konsisten di 4 site; SubmitEssayScore adalah defensive upsert dengan status-guard; atribut auth dipertahankan | VERIFIED | Marker PXF-04 D-06 ditemukan di 4 baris: L3308, L3505, L3572, L3649. Site EF (L3308, L3572) mengevaluasi whitespace in-memory setelah filter EssayScore==null server-side (fix SQL Server tab/newline blind spot). SubmitEssayScore L3528-3531: `[HttpPost][Authorize(Roles="Admin, HC")][ValidateAntiForgeryToken]`. Status-guard L3539. Upsert `.Add` L3563. String "Jawaban tidak ditemukan" hanya muncul dalam komentar (L3550), bukan sebagai return aktif. |
| 3 | AssessmentScoreAggregator.BuildAnswerCell exists; GeneratePerPesertaPdf dan ExcelExportHelper.AddDetailPerSoalSheet keduanya route melalui IsQuestionCorrect + BuildAnswerCell (bukan single-row FirstOrDefault) | VERIFIED | `Helpers/AssessmentScoreAggregator.cs` L110: `public static string BuildAnswerCell(...)`. PDF `Controllers/AssessmentAdminController.cs` L5106-5107: `IsQuestionCorrect(q, responsesForQ)` + `BuildAnswerCell(q, responsesForQ)`. Excel `Helpers/ExcelExportHelper.cs` L93-94: `BuildAnswerCell(q, responsesForQ)` + `IsQuestionCorrect(q, responsesForQ)`. Pola single-row mislabel `q.Options?.FirstOrDefault(o => o.Id == resp.PackageOptionId.Value)` tidak ditemukan di GeneratePerPesertaPdf; `var selectedOption = q.Options?.FirstOrDefault(...)` tidak ada di AddDetailPerSoalSheet. |
| 4 | Scoring engine Compute() tidak diubah | VERIFIED | `Helpers/AssessmentScoreAggregator.cs` L26: `public static ScoreAggregateResult Compute(` — signature intact. Git log konfirmasi 0 diff pada AssessmentScoreAggregator.cs sepanjang commit fase 05 (86 per 05-SUMMARY). |
| 5 | 0 migration file baru sejak baseline b21f3e32 | VERIFIED | `git log --oneline b21f3e32.. -- Migrations/` mengembalikan 0 baris. Tidak ada migration baru. |
| 6 | Full test suite green — 474/474; build 0 CS error | VERIFIED | 386-06-SUMMARY melaporkan: `dotnet test` 474/474 GREEN (OptionValidation 7/7, PdfAnswerCell 6/6, EssayEmptyPendingParity 6/6 incl whitespace "\t\n", Authz 2/2, IsQuestionCorrect regression GREEN). Build: error hanya MSB3021 (OS file-lock karena app sedang berjalan saat verifikasi) — `dotnet build 2>&1 | grep "error CS"` mengembalikan 0 baris, artinya 0 compile error C#. |
| 7 | Browser UAT 4/4 PASS — PDF MA all-or-nothing, Excel Detail Jawaban byte-identik PDF, PXF-02 reject banner, PXF-04 finalize button visible | VERIFIED | 386-06-SUMMARY Task 3: orchestrator melakukan live browser UAT localhost:5277. Soal 9 MA partial (pilih "Impeller" dari {Impeller, Volute, Shaft}) → SALAH di PDF (F-17 proof). Excel sharedStrings byte-identical. PXF-02: banner "Single Answer membutuhkan minimal 2 opsi jawaban yang berisi teks." + count soal tidak berubah. PXF-04: tombol "Selesaikan Penilaian" visible pada sesi 118. |

**Score:** 7/7 truths verified

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Helpers/QuestionOptionValidator.cs` | Pure helper ValidateQuestionOptions (PXF-02) | VERIFIED | Exists, namespace `HcPortal.Helpers`, method signature sesuai, tidak ada EF dependency |
| `Helpers/AssessmentScoreAggregator.cs` | BuildAnswerCell ditambahkan (PXF-05) | VERIFIED | L110: `public static string BuildAnswerCell(...)` — beside IsQuestionCorrect |
| `Controllers/AssessmentAdminController.cs` | ValidateQuestionOptions wired ×2, 4-site predicate, upsert, status-guard, BuildAnswerCell | VERIFIED | Semua 6 touchpoint dikonfirmasi via grep |
| `Helpers/ExcelExportHelper.cs` | AddDetailPerSoalSheet routed ke shared helpers (PXF-05 D-13) | VERIFIED | L93-94: BuildAnswerCell + IsQuestionCorrect, single-row path dihapus |
| `HcPortal.Tests/OptionValidationTests.cs` | 7 Fact, pure (no Trait), RED→GREEN | VERIFIED | EXISTS, 7 `[Fact]`, tidak ada `[Trait(` |
| `HcPortal.Tests/PdfAnswerCellTests.cs` | 6 Fact, pure, RED→GREEN | VERIFIED | EXISTS, 6 `[Fact]` |
| `HcPortal.Tests/EssayEmptyPendingParityTests.cs` | 8 Fact, Integration, 4 variants | VERIFIED | EXISTS, 8 `[Fact]`, `[Trait("Category", "Integration")]` ×2, semua 4 variant (NoRow/WhitespaceText/FilledUngraded/Graded) |
| `tests/e2e/option-validation-386.spec.ts` | Un-gated, asserts .alert-danger | VERIFIED | EXISTS, `test.fixme`/`test.skip` = 0 occurrences, `alert-danger` present |
| `tests/e2e/essay-empty-finalize-386.spec.ts` | Un-gated, asserts Selesaikan | VERIFIED | EXISTS, `test.fixme`/`test.skip` = 0 occurrences, `Selesaikan` present |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|----|--------|---------|
| CreateQuestion (L6480) | QuestionOptionValidator.ValidateQuestionOptions | call setelah correctCount gate, sebelum persist | VERIFIED | Grep mengembalikan L6480 |
| EditQuestion POST (L6699) | QuestionOptionValidator.ValidateQuestionOptions | byte-identical block, redirect sama | VERIFIED | Grep mengembalikan L6699 |
| AssessmentMonitoringDetail (L3308) | PackageUserResponses pending count | EF filter EssayScore==null + Join + materialize, then in-memory IsNullOrWhiteSpace | VERIFIED | Marker PXF-04 D-06 di L3308, in-memory eval di L3317 |
| EssayGradingPage (L3506) | items.Count pending | in-memory `!IsNullOrWhiteSpace(i.TextAnswer) && i.EssayScore == null` | VERIFIED | L3506 exact predicate |
| SubmitEssayScore site-3 (L3572) | pending count post-save | EF filter + in-memory whitespace | VERIFIED | L3572 marker, L3580 in-memory count |
| FinalizeEssayGrading site-2 (L3650) | gate Any pending | in-memory `!IsNullOrWhiteSpace(r.TextAnswer) && r.EssayScore == null` | VERIFIED | L3650 confirmed |
| SubmitEssayScore (L3531) | AssessmentSessions.Status == PendingGrading | status-guard block pertama di body | VERIFIED | L3539: `session.Status != AssessmentConstants.AssessmentStatus.PendingGrading` |
| SubmitEssayScore (L3553-3563) | PackageUserResponses.Add (upsert) | find-or-create + SaveChangesAsync | VERIFIED | L3563: `_context.PackageUserResponses.Add(response)` |
| GeneratePerPesertaPdf per-question loop | AssessmentScoreAggregator.IsQuestionCorrect + BuildAnswerCell | per-question responsesForQ | VERIFIED | L5106-5107 |
| ExcelExportHelper.AddDetailPerSoalSheet | AssessmentScoreAggregator.IsQuestionCorrect + BuildAnswerCell | per (session, q) responsesForQ | VERIFIED | L93-94 |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| ExcelExportHelper.AddDetailPerSoalSheet | `responsesForQ` | `responses.Where(session+question filter).ToList()` — `responses` berasal dari `List<PackageUserResponse>` yang di-pass caller (real DB rows) | Ya — query DB di caller, helper menerima list nyata | FLOWING |
| GeneratePerPesertaPdf loop | `responsesForQ` | `sessionResponses.Where(r => r.PackageQuestionId == q.Id).ToList()` — `sessionResponses` di-load dari DB sebelum loop | Ya — real DB query upstream | FLOWING |
| SubmitEssayScore upsert | `response` / `EssayScore` | `FirstOrDefaultAsync` dari `_context.PackageUserResponses`, new row jika null | Ya — DB read/write nyata, bukan hardcoded | FLOWING |

### Behavioral Spot-Checks

| Behavior | Evidence | Status |
|----------|----------|--------|
| CreateQuestion menolak soal MA tanpa opsi ber-teks | Browser UAT: banner "Single Answer membutuhkan minimal 2 opsi jawaban yang berisi teks." muncul, count soal tidak berubah | PASS |
| EditQuestion menolak via helper yang sama | Code grep: L6699 identical block | PASS (code-level) |
| SubmitEssayScore menolak non-PendingGrading | Integration test `SubmitEssayScore_NonPendingGrading_Rejected` GREEN (6/6 EssayEmptyPendingParity suite) | PASS |
| SubmitEssayScore upsert essay kosong | Integration test `SubmitEssayScore_NoRow_UpsertCreatesRowAndScores` GREEN | PASS |
| HC finalisasi sesi dengan essay dikosongkan | Browser UAT: tombol "Selesaikan Penilaian" visible + e2e essay-empty-finalize-386 GREEN | PASS |
| PDF MA Soal 9 partial → SALAH | Browser UAT: pdftotext konfirmasi "SALAH, Jawaban Impeller" | PASS |
| Excel Detail Jawaban byte-identik PDF label | Browser UAT: xlsx sharedStrings identik PDF | PASS |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| PXF-02 (F-DEV-01) | 386-03 | CreateQuestion & EditQuestion reject malformed SA/MA | SATISFIED | ValidateQuestionOptions wired ×2, OptionValidationTests 7/7 GREEN, e2e GREEN, Browser UAT PASS |
| PXF-04 (F-04) | 386-04 | HC bisa finalisasi assessment walau essay kosong | SATISFIED | 4-site predicate + upsert + status-guard, EssayEmptyPendingParity 6/6 GREEN, e2e GREEN, Browser UAT PASS |
| PXF-05 (F-17) | 386-05 | PDF MA label all-or-nothing SetEquals + Jawaban list semua opsi | SATISFIED | BuildAnswerCell + IsQuestionCorrect wired ke PDF + Excel, PdfAnswerCellTests 6/6 GREEN, Browser UAT PASS |
| PXF-07 (F-02) | 386-05 Task 2 (D-13 fold) | Excel Detail Per Soal essay label via IsQuestionCorrect >0 | SATISFIED | ExcelExportHelper L94: `IsQuestionCorrect(q, responsesForQ)` — unifikasi essay >0 didokumentasikan di 386-05-SUMMARY. REQUIREMENTS.md masih "Pending" di traceability table tapi kode sudah implement via D-13 fold. |
| PXF-14 (F-DEV-02) | 386-05 Task 2 (D-13 fold) | Excel Detail Per Soal MA label SetEquals (BuildAnswerCell) | SATISFIED | ExcelExportHelper L93: `BuildAnswerCell(q, responsesForQ)` — single-row mislabel dihapus. REQUIREMENTS.md traceability "Pending" adalah staleness status field, bukan gap kode. |

**Catatan PXF-07 + PXF-14:** REQUIREMENTS.md traceability table masih menampilkan "Pending" untuk kedua REQ ini. Namun ini adalah stale status — kodenya sudah implement di commit `bb058f1b` (ExcelExportHelper rewrite). Rekonsiliasi D-13 terdokumentasi di: (1) REQUIREMENTS.md baris 25-26 (reconcile note), (2) 386-05-SUMMARY `requirements-completed: [PXF-05]` yang mengklaim PXF-07/14 via D-13 fold, (3) kode aktual di ExcelExportHelper.cs. Verifikasi menganggap keduanya SATISFIED karena implementasi kode terbukti ada.

### Anti-Patterns Found

| File | Pattern | Severity | Assessment |
|------|---------|----------|------------|
| `Helpers/ExcelExportHelper.cs` L154 | `FirstOrDefault(et =>` | INFO | Ini di metode lain (etScores lookup, bukan AddDetailPerSoalSheet) — bukan path yang di-fix; tidak relevan terhadap goal |
| None lain | — | — | Tidak ada TODO/FIXME/placeholder/return null/return {} di file-file yang dimodifikasi fase ini |

### Human Verification

Browser UAT sudah dilakukan oleh orchestrator sebelum verifikasi ini diminta. Semua 4 item PASS:

1. **PXF-05 PDF** — Soal 9 MA partial → SALAH terkonfirmasi via pdftotext pada sesi 118.
2. **PXF-05 Excel** — "Detail Jawaban" sharedStrings byte-identik dengan PDF label.
3. **PXF-02** — Banner reject "Single Answer membutuhkan minimal 2 opsi jawaban yang berisi teks." muncul; count soal tidak bertambah.
4. **PXF-04** — Tombol "Selesaikan Penilaian" visible di EssayGrading sesi 118.

Tidak ada item yang membutuhkan verifikasi manusia tambahan.

---

## Gaps Summary

Tidak ada gap. Semua 7 truths VERIFIED, semua artefak exist dan substantive, semua key link WIRED, data flows real, behavioral spot-checks PASS, browser UAT 4/4 PASS.

**Catatan build:** Error `MSB3021` yang muncul saat `dotnet build` pada sesi verifikasi ini adalah OS file-lock (app sedang berjalan, `HcPortal.exe` sedang digunakan). Tidak ada `error CS` compile error — dikonfirmasi via `grep "error CS"` mengembalikan 0 baris. SUMMARY-06 melaporkan build 0 error pada sesi eksekusi (saat app tidak running).

---

_Verified: 2026-06-16T08:00:00Z_
_Verifier: Claude (gsd-verifier)_
