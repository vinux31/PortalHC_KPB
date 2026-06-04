---
phase: 343-integrasi-app-wide
plan: 04
requirements-completed: [ORG-INTEG-01]
subsystem: views-assessment-upload-account
tags: [razor, org-label, display-swap, assessment, account, ambiguous]
key-files:
  modified:
    - Views/Admin/EditAssessment.cshtml
    - Views/Admin/CreateAssessment.cshtml
    - Views/Admin/CpdpUpload.cshtml
    - Views/Admin/KkjUpload.cshtml
    - Views/Admin/CpdpFiles.cshtml
    - Views/Admin/KkjMatrix.cshtml
    - Views/Account/Profile.cshtml
    - Views/Account/Settings.cshtml
metrics:
  tasks: 2
  commits: 2
  files: 8
  build: pass
---

# Phase 343 Plan 04 — Summary

## Objective
Swap hardcoded display label → `@OrgLabels.GetLabel(N)` di Admin assessment/upload + Account (ORG-INTEG-01 portion). Memegang SEMUA resolusi AMBIGUOUS.

## Commits
| Task | Commit | Description |
|------|--------|-------------|
| 1 | `62b7d75b` | feat(343-04): swap Admin assessment+upload (AMBIGUOUS Lainnya REPLACE) |
| 2 | `da2414ad` | feat(343-04): swap CpdpFiles/KkjMatrix button + Account (JS/toast SKIP) |

## What was built (8 file)
- **EditAssessment**: th L522 + dropdown L598 + AMBIGUOUS L606 `Lainnya (Tanpa @OrgLabels.GetLabel(0))`.
- **CreateAssessment**: dropdown L264 + AMBIGUOUS L272 `Lainnya (Tanpa ...)`.
- **CpdpUpload/KkjUpload**: selector label `Pilih X` L39 + placeholder `-- Pilih X --` L42.
- **CpdpFiles/KkjMatrix** (AMBIGUOUS→REPLACE button text): `Tambah X` L64 + `Hapus X` L87 + empty-state prose `Klik "Tambah X"` L172. **SKIP intact:** JS func `addBagian()`/`deleteBagian()`, toast, `title="Hapus bagian"` lowercase tooltip, `@bagian.Name` data.
- **Account/Profile**: detail field label L78/84 (`<div>` label; value cell `@Model.Section/@Model.Unit` UNtouched).
- **Account/Settings**: field label L96/114 (input value `@Model.Section/@Model.Unit` UNtouched).

## Deviations
- **CpdpFiles/KkjMatrix L172 empty-state prose `Klik "Tambah Bagian"` swapped** (beyond literal REPLACE table L64/L87) — quoted reference names the button directly; swapped for consistency with the now-dynamic button + to keep grep-residual clean. "Belum ada bagian" lowercase generic left literal.
- **Acceptance criterion inaccuracy (non-blocking):** plan asserted `CpdpFiles STILL contains 'Bagian berhasil'` — CpdpFiles never had that literal (uses `showToast(data.message, ...)` server message; the `'Bagian berhasil dihapus.'` literal exists only in KkjMatrix, intact=2). Zero JS edited in either file; addBagian/deleteBagian/showToast all intact. No defect — criterion based on wrong assumption about CpdpFiles content.

## Verification
- `dotnet build` → **0 Errors** (21 pre-existing nullable warnings).
- Residual grep 8 file: `Semua Bagian`/`-- Pilih Bagian --`/`<th>Bagian</th>`/`Lainnya (Tanpa Bagian)`/`>Bagian</div>`/`>Unit</div>`/`>Bagian</label>`/`</i>Tambah Bagian`/`</i>Hapus Bagian`/`Klik "Tambah Bagian"`/`Pilih Bagian <span` = **0**.
- SKIP guards: `addBagian()` STILL present CpdpFiles+KkjMatrix (2 each); KkjMatrix toast `'Bagian berhasil'` STILL present (2); Account value `@Model.Section/@Model.Unit` STILL present Profile (2).

## Self-Check: PASSED
- 8 file assessment/upload/Account label tier dinamis; AMBIGUOUS resolved per AUDIT §3 (CpdpFiles/KkjMatrix button REPLACE + JS/toast SKIP; Lainnya-Tanpa-Bagian REPLACE).
- JS func/toast/property/data/value-cell untouched. Build hijau. ORG-INTEG-01 (assessment/upload/Account portion) terpenuhi.
