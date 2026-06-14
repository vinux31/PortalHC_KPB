# Phase 357: Standarisasi Istilah Tipe Soal - Discussion Log

> **Audit trail only.** Do not use as input to planning/research/execution agents.
> Decisions captured in CONTEXT.md — this log preserves alternatives considered.

**Date:** 2026-06-09
**Phase:** 357-standarisasi-istilah-tipe-soal
**Areas discussed:** Docs scope (Grup D), Dropdown binding, Verifikasi depth

**Catatan:** Spec `2026-06-09-question-type-naming-single-answer-design.md` sudah mengunci mayoritas keputusan (1A wording, 2A scope, opsi-i badge, S1 abbrev, mapping, daftar NOT-touched). Hanya 3 micro gray-area tersisa.

---

## Grup D — Docs scope

| Option | Description | Selected |
|--------|-------------|----------|
| HTML served + GuideContentProvider | 6 guide HTML + GuideContentProvider.cs context-aware; TKI .py regen bila perlu; PDF defer user | ✓ |
| + Regen TKI BAB-X dari .py | Plus jalankan generate_bab_x.py otomatis | |
| Kode+helper saja, skip docs | Grup A/B/C only | |

**User's choice:** HTML served + GuideContentProvider (Rekomendasi)
**Notes:** PDF defer manual user (kebijakan Phase 305). grep residual=0 criterion terpenuhi.

---

## ManagePackageQuestions dropdown

| Option | Description | Selected |
|--------|-------------|----------|
| Binding @QuestionTypeLabels.Long() | DRY single-source, value attribute enum tetap | ✓ |
| Static text wording baru | Tulis langsung di <option> | |

**User's choice:** Binding helper (Rekomendasi)
**Notes:** Wujudkan goal "QuestionTypeLabels.cs single-source penuh".

---

## Verifikasi depth

| Option | Description | Selected |
|--------|-------------|----------|
| build+test+grep+Playwright 5 surface | SC penuh + UAT visual via Playwright MCP | ✓ |
| build+test+grep saja | Skip Playwright | |

**User's choice:** build+test+grep+Playwright 5 surface (Rekomendasi)
**Notes:** Claude jalankan Playwright UAT (dropdown/badge/StartExam/ExamSummary/EditPeserta) + Excel SA/MA + SELECT DISTINCT enum.

## Claude's Discretion
- Regen TKI BAB-X mekanisme (.py vs manual).
- Null-guard dropdown Long() binding.

## Deferred Ideas
- PDF panduan regen → manual user (Phase 305 D-14 policy).
