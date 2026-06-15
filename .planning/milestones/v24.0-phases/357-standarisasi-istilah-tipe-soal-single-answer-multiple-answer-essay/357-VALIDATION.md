---
phase: 357
slug: standarisasi-istilah-tipe-soal
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-09
---

# Phase 357 — Validation Strategy

> Per-phase validation contract. Pure label/dead-code phase — logic-bearing surface kecil (helper string).
> Authored manual (research skipped — spec file:line eksak).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (net8.0) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~QuestionTypeLabels"` |
| **Full suite command** | `dotnet test` (~135 baseline) |
| **Estimated runtime** | quick <5s · full ~15-60s |

---

## Sampling Rate

- **After helper edit (Grup A):** `dotnet test --filter "FullyQualifiedName~QuestionTypeLabels"`
- **After each task commit:** `dotnet build HcPortal.csproj -clp:ErrorsOnly`
- **Phase gate:** `dotnet build` 0 error + `dotnet test` full hijau + grep residual + Playwright UAT 5 surface

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| by-plan | W0 | 0 | LBL-02 (helper) | unit | `dotnet test --filter "FullyQualifiedName~QuestionTypeLabels"` | ❌ W0 | ⬜ pending |
| by-plan | — | — | LBL-02 (Grup B/C surface+dead) | build | `dotnet build HcPortal.csproj -clp:ErrorsOnly` | ✅ | ⬜ pending |
| by-plan | — | — | LBL-02 (Grup D docs) | grep | `grep -rIE "Single Choice\|Multiple Answers\|Multiple Choice" wwwroot/documents/guides Services/GuideContentProvider.cs` → 0 | ✅ | ⬜ pending |
| MANUAL | — | — | LBL-02 e2e | Playwright UAT | 5 surface @localhost:5277 | manual | ⬜ pending |
| MANUAL | — | — | no-migration | SQL | `SELECT DISTINCT QuestionType FROM PackageQuestions` = 3 enum | manual | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/QuestionTypeLabelsTests.cs` — [Fact] lock wording baru: `Long("MultipleChoice")` contains "Single Answer", `Long("MultipleAnswer")` contains "Multiple Answer", `Short("MultipleChoice")=="Single Answer"`, `Short("MultipleAnswer")=="Multiple Answer"`, `Short("Essay")=="Essay"`. (Cek dulu apakah sudah ada test yang assert label LAMA — bila ada, update.)
- Framework install: tidak perlu (xUnit terpasang).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| 5 surface wording baru | LBL-02 | Render visual | Playwright @5277: dropdown form Manage · badge tabel Manage · StartExam · ExamSummary · EditPesertaAnswers tampil "Single Answer/Multiple Answer/Essay" |
| Export Excel sel SA/MA | LBL-02 (S1) | File download | Export per-peserta → sel tipe = "SA"/"MA" (bukan "MC"/"MA") |
| Flow ujian no regresi | LBL-02 | Runtime | MC/MA/Essay tanpa gambar tetap normal (value DB tak berubah) |

---

## Validation Sign-Off

- [ ] Helper test locks new wording (Wave 0)
- [ ] grep residual "Single Choice"/"Multiple Answers"/"Multiple Choice"(tipe soal) = 0 non-arsip
- [ ] No watch-mode flags
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
