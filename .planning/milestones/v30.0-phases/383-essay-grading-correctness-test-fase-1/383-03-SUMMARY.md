---
phase: 383-essay-grading-correctness-test-fase-1
plan: 03
subsystem: assessment-grading
tags: [essay-grading, pdf-export, correctness, kill-drift, ECG-05, D-03]
requirements: [ECG-05]
dependency_graph:
  requires:
    - "AssessmentScoreAggregator.IsQuestionCorrect (Plan 01)"
  provides:
    - "PDF export per-peserta essay correctness via IsQuestionCorrect (essay > 0, null = pending)"
  affects:
    - "GeneratePerPesertaPdf (AssessmentAdminController) — render badge Benar/Salah/Pending soal essay di PDF"
tech_stack:
  added: []
  patterns:
    - "Kill-drift single-source correctness (Phase 363/365/376) — PDF & web Results satu helper"
key_files:
  created: []
  modified:
    - "Controllers/AssessmentAdminController.cs (GeneratePerPesertaPdf, L5014-5018 branch essay)"
decisions:
  - "[383-03 / ECG-05 / D-03]: PDF export essay correctness di GeneratePerPesertaPdf di-unify ke AssessmentScoreAggregator.IsQuestionCorrect (essay > 0 → Benar, == 0 → Salah, null → Pending). Threshold lama EssayScore >= ScoreValue/2 dihapus. Perubahan behavior PDF DISENGAJA agar PDF & web Results tak bisa lagi divergen (kill-drift)."
metrics:
  duration_min: 2
  tasks: 1
  files: 1
  completed: "2026-06-15"
---

# Phase 383 Plan 03: PDF Essay Correctness Unify Summary

PDF export per-peserta (`AssessmentAdminController.GeneratePerPesertaPdf`) kini menandai soal essay Benar/Salah/Pending lewat helper `AssessmentScoreAggregator.IsQuestionCorrect` (essay `> 0`), menggantikan threshold lama `EssayScore >= ScoreValue/2` — satu aturan correctness dengan web Results (D-03 unify, ECG-05).

## What Changed

Satu perubahan kecil (2 insert / 1 delete) di `Controllers/AssessmentAdminController.cs`, branch essay loop render PDF (L5014-5018):

**Sebelum:**
```csharp
else if (!string.IsNullOrEmpty(resp.TextAnswer))
{
    jawaban = resp.TextAnswer.Length > 300 ? resp.TextAnswer.Substring(0, 300) + "..." : resp.TextAnswer;
    correct = resp.EssayScore.HasValue ? (bool?)(resp.EssayScore.Value >= (q.ScoreValue / 2)) : null;
}
```

**Sesudah:**
```csharp
else if (!string.IsNullOrEmpty(resp.TextAnswer))
{
    jawaban = resp.TextAnswer.Length > 300 ? resp.TextAnswer.Substring(0, 300) + "..." : resp.TextAnswer;
    // Phase 383 ECG-05/D-03 unify: PDF essay correctness via IsQuestionCorrect (essay > 0; null = pending) — sama dengan web Results.
    correct = AssessmentScoreAggregator.IsQuestionCorrect(q, sessionResponses.Where(r => r.PackageQuestionId == q.Id));
}
```

- Hanya baris perhitungan `correct` yang diganti. Baris `jawaban = ...` (truncate 300 char) dipertahankan apa adanya.
- `statusColor`/`statusText` mapping (L5021-5024: Green "✓ Benar" / Red "✗ Salah" / Grey "— Pending") TIDAK diubah — `bool?` cocok langsung.
- `using HcPortal.Helpers;` sudah ada (L13) — tak perlu tambah.
- `SubmitEssayScore` (`response.EssayScore = score;` L3476) & `FinalizeEssayGrading` (`ExecuteUpdateAsync` L3595/3640) TIDAK disentuh (D-05 lock-with-tests, Plan 04).

## Behavior Change (Disengaja)

| Skenario essay graded | Aturan lama PDF (`>= ScoreValue/2`) | Aturan baru (`> 0`, = web Results) |
|-----------------------|--------------------------------------|------------------------------------|
| EssayScore = 0        | Salah                                | Salah                              |
| 0 < EssayScore < ScoreValue/2 | Salah                        | **Benar** (ikut web Results)       |
| EssayScore >= ScoreValue/2 | Benar                           | Benar                              |
| EssayScore = null     | Pending                              | Pending                            |

Ini menutup drift PDF vs web Results untuk skor parsial. Caveat D-02 (partial-credit essay tampil Benar di count biner) diterima.

## Tasks Completed

| Task | Name                                                  | Commit    | Files                                  |
| ---- | ----------------------------------------------------- | --------- | -------------------------------------- |
| 1    | Ganti essay correctness PDF ke IsQuestionCorrect      | 145f08fe  | Controllers/AssessmentAdminController.cs |

## Verification

- `dotnet build` → **Build succeeded, 0 Warning(s), 0 Error(s)**.
- `dotnet test --filter "Category!=Integration"` → **Passed! Failed: 0, Passed: 314, Skipped: 0** (sama dengan baseline Plan 01 = no regression).
- grep POSITIF: `correct = AssessmentScoreAggregator.IsQuestionCorrect(q, sessionResponses.Where(r => r.PackageQuestionId == q.Id))` match di L5018. ✓
- grep NEGATIF: `resp.EssayScore.Value >= (q.ScoreValue / 2)` — **No matches** (threshold lama dihapus). ✓
- grep: `jawaban = resp.TextAnswer.Length > 300` masih ada di L5016 (truncate dipertahankan). ✓
- grep NEGATIF (Submit/Finalize utuh): `response.EssayScore = score;` (L3476) + `ExecuteUpdateAsync` (L3595/3640) masih ada. ✓

### PDF Render (env-blocked, code-verify cukup)

Render QuestPDF/SkiaSharp sebenarnya bisa env-blocked lokal (known issue Phase 327). Verifikasi visual export PDF = manual-only (VALIDATION manual). Aturan correctness essay yang dipakai PDF sudah dikunci unit test Plan 01 (`IsQuestionCorrect` essay `> 0` → 11 unit test hijau), jadi code-verify (build + grep + test suite) sudah cukup untuk plan ini.

## Deviations from Plan

None - plan executed exactly as written (1 task, 1 file, single-line correctness change).

> Catatan scope: `docs/SEED_JOURNAL.md` (modified) dan `docs/ringkasan-v25-v29/` (untracked) sudah ada di working tree sebelum plan ini dan TIDAK terkait task — tidak disentuh / tidak di-commit (scope boundary).

## Known Stubs

None.

## Self-Check: PASSED

- FOUND: Controllers/AssessmentAdminController.cs (modified, L5018 helper call)
- FOUND: commit 145f08fe
- FOUND: .planning/phases/383-essay-grading-correctness-test-fase-1/383-03-SUMMARY.md
