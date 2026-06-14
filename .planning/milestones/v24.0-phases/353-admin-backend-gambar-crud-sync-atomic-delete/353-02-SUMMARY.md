---
phase: 353-admin-backend-gambar-crud-sync-atomic-delete
plan: 02
subsystem: assessment-admin-backend
tags: [image-crud, atomic-delete, refcount, oq1-preserve, shared-file]
requires: [01]
provides:
  - CreateQuestion image upload (soal+opsi)
  - EditQuestion GET JSON image prefill (D-06)
  - EditQuestion POST update-in-place + ref-count atomic delete (OQ1/D-05/SYN-02)
affects:
  - Controllers/AssessmentAdminController.cs
tech-stack:
  added: []
  patterns: [validate-all-fail-fast, update-in-place-option, refcount-atomic-delete, file-baru-menang]
key-files:
  created: []
  modified:
    - Controllers/AssessmentAdminController.cs
    - HcPortal.Tests/PackageImageDeleteTests.cs
key-decisions:
  - "Opsi pakai UPDATE-IN-PLACE (preferred, bukan carry-forward): match posisi A-D via OrderBy(o.Id) == urutan GET JSON; preserve Id+ImagePath"
  - "RemoveRange(q.Options) DISISAKAN HANYA di cabang Essay (opsi dibuang seluruhnya; gambar opsi jadi delete-candidate) — bukan wipe membabi-buta MC/MA"
  - "ApplyOptionImageIntent helper async (D-05 file-baru-menang) dipakai soal-path inline + opsi via helper"
  - "Ref-count (AnyAsync x2) + File.Delete warn-only ditempatkan SETELAH auto-sync (OQ2 ordering) agar Post share-path sudah ter-update sebelum dicek"
requirements-completed: [IMG-01, IMG-02, IMG-03, IMG-05, IMG-06, IMG-07, SYN-02]
duration: ~25 min
completed: 2026-06-08
---

# Phase 353 Plan 02: Image CRUD + Atomic Delete (OQ1 Preserve) Summary

Wiring CRUD gambar penuh di `AssessmentAdminController.cs` — bagian paling berisiko fase (OQ1 A3 HIGH). CreateQuestion menerima/validasi/simpan gambar soal+opsi; EditQuestion GET membawa prefill; EditQuestion POST mengganti strategi opsi dari `RemoveRange`+recreate (wipe ImagePath) menjadi **update-in-place by posisi A-D** sehingga gambar opsi tak hilang, plus file-baru-menang (D-05), checkbox hapus (IMG-06), dan atomic delete ref-counted shared-file (SYN-02/D-10) setelah auto-sync.

## Tasks
1. **CreateQuestion** — +10 param (IFormFile soal+opsi A-D + alt), validate-all fail-fast, `SaveFileAsync` → `uploads/questions/{packageId}`, set ImagePath/ImageAlt + `TruncateAlt`. Commit `18d6693d`.
2. **EditQuestion GET JSON** — +imagePath/imageAlt soal + tiap opsi (OrderBy o.Id). Commit `ccb4385d` (digabung T3).
3. **EditQuestion POST** — signature +14 param (image+alt+remove checkbox). Update-in-place opsi (preserve Id+ImagePath), `ApplyOptionImageIntent` (D-05), ref-count `AnyAsync` → `File.Delete` warn-only setelah auto-sync. Commit `ccb4385d`.
4. **Tests** — `ReplaceConflict_NewFileWins_OverRemoveCheckbox` + `OptionPreserve_KeepsImagePath_WhenOptionUntouched` GREEN. Commit `c04ce54c`.

## Strategi Opsi (catatan wajib SUMMARY)
**UPDATE-IN-PLACE dipilih (bukan carry-forward snapshot).** Untuk MultipleChoice/MultipleAnswer: existing options diurutkan `OrderBy(o.Id)` (== urutan pembuatan == posisi A-D == urutan GET JSON). Per posisi i=0..3: slot+text → update text/correct preserve gambar; slot tanpa text → remove + gambar delete-candidate; tanpa slot + text → add baru. `RemoveRange(q.Options)` **hanya** dipakai di cabang Essay (semua opsi dibuang) — grep akan menemukan 1 match di EditQuestion POST, ini cabang Essay yang terdokumentasi (acceptance fallback clause), bukan wipe MC/MA.

## Verification
- `dotnet build HcPortal.csproj` → 0 errors.
- `dotnet test --filter ~ReplaceConflict|~OptionPreserve` → 2 passed.
- Full suite: **129 passed / 0 failed** (127 + 2 baru).

## Deviations from Plan

None - plan executed exactly as written. (Strategi update-in-place = opsi preferred plan; RemoveRange Essay-branch = fallback clause terdokumentasi acceptance T3.)

## Issues Encountered

None. (Stale worktree index.lock dibersihkan saat commit — operasional, bukan kode.)

## Next Phase Readiness

Ready for 353-03 (VIEW form upload inline + FileReader prefill + _PreviewQuestion `<img>` + DeleteQuestion/DeletePackage atomic delete RND-04/SYN-02 sisa). Backend image binding + ref-count pattern sudah live untuk dipakai/ditiru DeleteQuestion/DeletePackage.
