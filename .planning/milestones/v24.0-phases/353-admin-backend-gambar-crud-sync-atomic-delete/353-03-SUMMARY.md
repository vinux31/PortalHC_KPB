---
phase: 353-admin-backend-gambar-crud-sync-atomic-delete
plan: 03
subsystem: assessment-admin-view-delete
tags: [view, multipart, filereader, preview-render, atomic-delete, refcount]
requires: [02]
provides:
  - form multipart + field gambar inline (soal+opsi A-D)
  - FileReader thumbnail instan + prefill edit
  - _PreviewQuestion render img (RND-04)
  - DeleteQuestion + DeletePackage atomic file delete + ref-count (SYN-02/D-11)
affects:
  - Views/Admin/ManagePackageQuestions.cshtml
  - Views/Admin/_PreviewQuestion.cshtml
  - Controllers/AssessmentAdminController.cs
tech-stack:
  added: []
  patterns: [multipart-form, filereader-thumbnail, refcount-atomic-delete, path-collect-before-cascade]
key-files:
  created: []
  modified:
    - Views/Admin/ManagePackageQuestions.cshtml
    - Views/Admin/_PreviewQuestion.cshtml
    - Controllers/AssessmentAdminController.cs
    - HcPortal.Tests/PackageImageDeleteTests.cs
key-decisions:
  - "DeleteQuestion + DeletePackage tetap single-SaveChanges existing (TIDAK bungkus tx baru) + post-save ref-count delete warn-only (C-04 terpenuhi, konsisten pola method)"
  - "Ref-count ditempatkan SETELAH auto-sync (OQ2) di kedua delete — auto-sync rebuild Post share-path"
  - "JS DRY: wireImageField generik 5 field (soal+A-D); konflik D-05 mirror (uncheck+disable checkbox saat file baru) — backend tetap otoritatif"
  - "Field name= Razor cocok persis param Plan 02 (questionImage/optionAImage..D/removeQuestionImage..) — binding hidup via enctype multipart"
requirements-completed: [RND-04, SYN-02]
duration: ~30 min
completed: 2026-06-08
---

# Phase 353 Plan 03: View Upload + Atomic Delete Summary

Menutup sisi VIEW dan atomic delete. Form `#questionForm` kini multipart dengan field gambar inline (soal di bawah textarea + opsi A-D inline) bernama persis sesuai param Plan 02 → binding upload hidup. JS FileReader menampilkan thumbnail instan saat pilih file + prefill thumbnail lama saat edit (D-03/IMG-07). `_PreviewQuestion` render `<img>` soal (240px) + opsi (120px). `DeleteQuestion`/`DeletePackage` mengumpulkan path SEBELUM cascade + ref-count `AnyAsync` → `File.Delete` warn-only SETELAH auto-sync (SYN-02/D-11/OQ2).

## Tasks
1. **Form inline + enctype** — `enctype="multipart/form-data"` + `.img-drop` soal + opsi A-D (name cocok Plan 02). Commit `93d417c5`.
2. **JS FileReader + prefill** — `wireImageField` (readAsDataURL thumbnail + clear + konflik D-05) + `prefillImage` di populateEditForm + reset bersih. Commit `93d417c5`.
3. **_PreviewQuestion `<img>`** — soal 240px + opsi 120px, `img-fluid rounded border loading=lazy`, null→tak render. Commit `93d417c5`.
4. **DeleteQuestion atomic** — path-collect before + ref-count after auto-sync + warn-only. Commit `92a8c258`.
5. **DeletePackage atomic + test** — path-collect union before cascade + ref-count after auto-sync; test `RefCount_DeletePackage_SkipsShared_DeletesOrphan` + existing `DeletePackageImage_CollectsAllNonNullPaths`. Commit `92a8c258` + `2bc5aac7`.

## Catatan wajib (output plan)
- **Tx:** DeleteQuestion + DeletePackage **tetap single-SaveChanges** existing (tidak ada BeginTransactionAsync di method tsb) + post-save ref-count delete warn-only — memenuhi C-04 (delete post-persist + ref-counted), konsisten pola method.
- **OQ2 dikonfirmasi:** ref-count loop ditempatkan SETELAH blok auto-sync di kedua method (auto-sync me-rebuild Post share-path → ref-count post-sync benar).
- **Integrasi Plan 02↔03:** `name=` field (questionImage, optionA..DImage, *Alt, removeQuestionImage, removeOption*Image) cocok param controller Plan 02 → build hijau + Razor compile bersih = binding ter-verifikasi statis.

## Verification
- `dotnet build HcPortal.csproj` → 0 errors (Razor + controller).
- Full suite: **130 passed / 0 failed** (129 + 1 baru).
- grep gate: enctype multipart ✓, name="questionImage/optionAImage/optionDImage/removeQuestionImage" ✓, FileReader/readAsDataURL/data.imagePath ✓, `<img` ×2 + 240px/120px/lazy ✓, imagePathsToDelete + AnyAsync + File.Delete + LogWarning di kedua delete ✓.
- Sisa: Playwright UAT end-to-end (upload→preview→edit→delete file hilang) = **Phase 355** (konsolidasi).

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None. (Stale/busy worktree index.lock dibersihkan beberapa kali saat commit — operasional OneDrive/git, bukan kode.)

## Next Phase Readiness

Phase 353 = 3/3 plan SHIPPED LOCAL. Backend+view gambar admin lengkap (CRUD + sync + atomic delete + ref-count). Next: render gambar 6 layar peserta = **Phase 354**; test/UAT konsolidasi = **Phase 355**.
