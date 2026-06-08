---
phase: 353-admin-backend-gambar-crud-sync-atomic-delete
verified: 2026-06-08T06:37:22Z
status: human_needed
score: 9/9 must-haves verified
overrides_applied: 0
deferred:
  - truth: "Playwright UAT end-to-end (upload soal+opsi → simpan → preview render → edit thumbnail prefill → replace/hapus → file fisik hilang dari disk)"
    addressed_in: "Phase 355"
    evidence: "ROADMAP SC #7 'sisa Playwright UAT = Phase 355'; CONTEXT <deferred> 'Test xUnit konsolidasi + Playwright UAT end-to-end → Phase 355 (TST-01/02)'"
human_verification:
  - test: "Upload gambar soal + tiap opsi A-D (JPG/PNG ≤5MB) via form ManagePackageQuestions, simpan, cek file tersimpan di wwwroot/uploads/questions/{packageId}/ + path tercatat DB"
    expected: "File tersimpan; ImagePath/ImageAlt terisi di DB; preview admin render gambar soal (≤240px) + opsi (≤120px)"
    why_human: "Butuh server berjalan (localhost:5277) + interaksi browser + cek filesystem nyata; tidak bisa diverifikasi statis"
  - test: "Edit soal bergambar: cek thumbnail lama prefill; ganti file baru lalu simpan; cek file LAMA hilang dari disk; ulangi dengan checkbox 'Hapus gambar'"
    expected: "Thumbnail prefill muncul; setelah replace/remove file lama terhapus fisik dari wwwroot (non-shared); ImagePath ter-update/null"
    why_human: "File.Delete fisik + ref-count shared-file hanya terbukti saat dijalankan dengan DB+disk nyata; logika source sudah diverifikasi tapi efek I/O perlu run"
  - test: "Skenario shared-file Pre→Post (SamePackage=true): hapus soal/opsi Pre yang gambarnya masih dipakai Post; cek file TIDAK terhapus (ref-count SKIP); lalu hapus Post juga → file terhapus"
    expected: "Tidak ada double-delete; file shared tetap ada selama Post masih merujuk; orphan benar terhapus saat semua referensi hilang"
    why_human: "Memerlukan eksekusi DeleteQuestion/DeletePackage + SyncPackagesToPost berurutan dengan DB+disk nyata"
---

# Phase 353: Admin Backend Gambar (CRUD + Sync + Atomic Delete) Verification Report

**Phase Goal:** Admin dapat mengelola gambar pada soal assessment di Manage Package Questions — upload soal+opsi (IMG-01/02/03), ganti/hapus dengan file lama terhapus disk (IMG-05/06), prefill thumbnail saat edit (IMG-07), preview render gambar (RND-04), sync Pre→Post shared-file (SYN-01), atomic delete + reference-count saat soal/opsi/paket dihapus (SYN-02).
**Verified:** 2026-06-08T06:37:22Z
**Status:** human_needed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| #   | Truth | Status | Evidence |
| --- | ----- | ------ | -------- |
| 1 | SYN-01: SyncPackagesToPost menyalin ImagePath+ImageAlt soal & opsi sebagai shared-file string copy, tanpa file op | ✓ VERIFIED | `AssessmentAdminController.cs` L5379-5380 (soal) + L5385-5386 (opsi); RemoveRange L5348 DB-only tak diubah; tidak ada File.Delete di method (L5337-5395) |
| 2 | IMG-01/02/03: CreateQuestion upload+validasi+simpan gambar soal & opsi A-D + alt | ✓ VERIFIED | CreateQuestion L6101-6253: validate-all L6124-6132, SaveFileAsync soal L6167 + opsi L6197, ImagePath/ImageAlt set, TruncateAlt 255, subfolder `uploads/questions/{packageId}` |
| 3 | IMG-07/D-06: EditQuestion GET JSON membawa imagePath+imageAlt soal & tiap opsi | ✓ VERIFIED | L6286-6287 (soal) + L6288-6293 (opsi, OrderBy o.Id) di blok Json |
| 4 | IMG-05/06/D-05: EditQuestion POST replace/remove, file-baru-menang, ref-count delete | ✓ VERIFIED | Soal L6385-6401; ApplyOptionImageIntent L6526-6545 (file-baru-menang + remove + keep); ref-count L6508-6509 + File.Delete L6514 warn-only |
| 5 | OQ1 (A3 HIGH): EditQuestion POST UPDATE-IN-PLACE; RemoveRange(q.Options) HANYA di cabang Essay | ✓ VERIFIED | Essay branch L6404-6413 (satu-satunya RemoveRange di EditQuestion POST, L6411); MC/MA branch L6414-6453 update-in-place preserve slot.ImagePath |
| 6 | RND-04: _PreviewQuestion render <img> soal (240px) + opsi (120px) loading=lazy | ✓ VERIFIED | `_PreviewQuestion.cshtml` L22-23 (soal max-height:240px) + L71-72 (opsi max-height:120px), keduanya img-fluid rounded border loading="lazy", null→tak render |
| 7 | SYN-02/D-10: DeleteQuestion path-collect-before + ref-count after auto-sync + File.Delete warn-only | ✓ VERIFIED | DeleteQuestion L6558-6561 path-collect before; auto-sync L6605; ref-count AnyAsync x2 L6613-6614; File.Delete warn-only L6619-6623 |
| 8 | SYN-02/D-11: DeletePackage path-collect union + ref-count after auto-sync | ✓ VERIFIED | DeletePackage L5472-5479 path-collect union; auto-sync L5536; ref-count AnyAsync x2 L5543-5544; File.Delete warn-only L5549-5553 |
| 9 | Form multipart + field names cocok param controller + FileReader/prefill JS | ✓ VERIFIED | enctype multipart L122; questionImage L155, removeQuestionImage L162, questionImageAlt L165; opsi A-D via Razor loop L191-210 (`option{letter}Image`/`Alt`/`removeOption{letter}Image`); FileReader/readAsDataURL L544-546, data.imagePath prefill L490 |

**Score:** 9/9 truths verified

### Deferred Items

| # | Item | Addressed In | Evidence |
|---|------|-------------|----------|
| 1 | Playwright UAT end-to-end (upload→preview→edit→replace/delete→file hilang disk) | Phase 355 | ROADMAP SC #7 "sisa Playwright UAT = Phase 355"; CONTEXT `<deferred>` TST-01/02 |

### Required Artifacts

| Artifact | Expected | Status | Details |
| -------- | -------- | ------ | ------- |
| `Controllers/AssessmentAdminController.cs` | CreateQuestion/EditQuestion GET+POST/DeleteQuestion/DeletePackage/SyncPackagesToPost image binding+atomic delete | ✓ VERIFIED | Semua method ada, substantive, wired; gsd verify artifacts all_passed=true |
| `Views/Admin/ManagePackageQuestions.cshtml` | enctype multipart + field gambar inline + FileReader/prefill | ✓ VERIFIED | enctype + 5 file field (soal+A-D) + JS FileReader + prefillImage |
| `Views/Admin/_PreviewQuestion.cshtml` | render <img> soal+opsi (RND-04) | ✓ VERIFIED | 2x `<img>` dengan cap 240/120px + lazy |
| `HcPortal.Tests/PackageImageSyncTests.cs` | test SYN-01 sync copy | ✓ VERIFIED | 4 [Fact]: SyncCopiesQuestionImagePath, SyncCopiesOptionImageAlt, SyncSharesSamePath, SyncHandlesNull |
| `HcPortal.Tests/PackageImageDeleteTests.cs` | ref-count + replace + OQ1 + path-collect | ✓ VERIFIED | 6 [Fact]: RefCount x2, DeletePackageImage_CollectsAllNonNullPaths, RefCount_DeletePackage, ReplaceConflict, OptionPreserve |

### Key Link Verification

| From | To | Via | Status |
| ---- | -- | --- | ------ |
| SyncPackagesToPost | PackageQuestion/Option.ImagePath Post | string copy di deep-clone (`ImagePath = q.ImagePath`) | ✓ WIRED (L5379/L5385) |
| Create/EditQuestion POST | FileUploadHelper.ValidateImageFile + SaveFileAsync | panggil sebelum set ImagePath | ✓ WIRED (L6124/L6167; L6331/L6387) |
| EditQuestion POST replace/remove | File.Delete old path | ref-count AnyAsync POST auto-sync → File.Delete warn-only | ✓ WIRED (L6508-6519) |
| Form `name="questionImage"`/`option*Image` | Controller param (Plan 02) | model binding multipart (enctype) | ✓ WIRED (Razor name= cocok param; build Razor compile bersih) |
| DeleteQuestion/DeletePackage path-collect | File.Delete fisik post-commit | ref-count AnyAsync(ImagePath==relUrl) → skip-or-delete | ✓ WIRED (L6613-6619; L5543-5549) |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| -------- | ------- | ------ | ------ |
| Build main project | `dotnet build HcPortal.csproj` | 0 Errors, 22 warnings (pre-existing) | ✓ PASS |
| Full test suite | `dotnet test HcPortal.Tests` | Failed: 0, Passed: 130, Skipped: 0 | ✓ PASS |
| File upload/delete I/O fisik (disk) | (butuh server+browser) | — | ? SKIP → human verification |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| ----------- | ----------- | ----------- | ------ | -------- |
| SYN-01 | 01 | Gambar tersalin shared-file saat sync Pre→Post | ✓ SATISFIED | Truth #1 |
| IMG-01 | 02 | Upload gambar ke soal | ✓ SATISFIED | Truth #2 |
| IMG-02 | 02 | Upload gambar ke tiap opsi MC/MA | ✓ SATISFIED | Truth #2 |
| IMG-03 | 02 | Alt text opsional per gambar | ✓ SATISFIED | Truth #2 (TruncateAlt) |
| IMG-05 | 02 | Ganti gambar, file lama terhapus disk | ✓ SATISFIED | Truth #4 (+ human run-verify) |
| IMG-06 | 02 | Hapus gambar via checkbox | ✓ SATISFIED | Truth #4 |
| IMG-07 | 02 | Prefill thumbnail saat edit | ✓ SATISFIED | Truth #3, #9 |
| SYN-02 | 02,03 | Atomic delete + ref-count saat hapus/replace | ✓ SATISFIED | Truth #4, #7, #8 |
| RND-04 | 03 | Preview admin render gambar soal+opsi | ✓ SATISFIED | Truth #6 |

Semua 9 ID requirement Phase 353 tercakup di plan frontmatter (Plan 01: SYN-01; Plan 02: IMG-01/02/03/05/06/07+SYN-02; Plan 03: RND-04+SYN-02) dan terpetakan ke Phase 353 di REQUIREMENTS.md. Tidak ada orphaned requirement.

### Anti-Patterns Found

Tidak ada blocker. Catatan: test PackageImage*.cs memakai mirror-logic (replika ekspresi controller dengan komentar "keep in sync") alih-alih memanggil method private controller langsung — ini terdokumentasi dan dapat diterima; logika controller sendiri sudah diverifikasi langsung di source. Bukan stub.

### Human Verification Required

1. **Upload gambar soal+opsi end-to-end** — upload via form ManagePackageQuestions, cek file di `wwwroot/uploads/questions/{packageId}/` + path DB + render preview. Why human: butuh server+browser+filesystem nyata.
2. **Replace/remove file lama hilang dari disk** — edit, ganti/hapus, verifikasi File.Delete fisik. Why human: efek I/O hanya terbukti saat run.
3. **Shared-file Pre→Post ref-count (no double-delete)** — hapus Pre saat Post masih pakai → file aman; hapus Post → file terhapus. Why human: butuh eksekusi delete+sync berurutan dengan DB+disk nyata.

(Item ini = SC #7 sisi Playwright yang ROADMAP/CONTEXT defer ke Phase 355.)

### Gaps Summary

Tidak ada gap. Seluruh 9 observable truth VERIFIED di source code; build 0 error; 130/130 test hijau. Risiko A3 HIGH OQ1 regression TIDAK terjadi: RemoveRange(q.Options) di EditQuestion POST hanya muncul di cabang Essay (L6411), sedangkan MC/MA memakai update-in-place yang mempertahankan ImagePath opsi. SYN-01 murni string-copy tanpa file op. Ref-count (AnyAsync x2) ditempatkan SETELAH auto-sync (OQ2) di EditQuestion/DeleteQuestion/DeletePackage. File.Delete warn-only di seluruh jalur.

Status `human_needed` (bukan `passed`) karena efek I/O fisik (file tersimpan/terhapus dari disk) dan render visual hanya dapat dikonfirmasi saat aplikasi dijalankan — sengaja dikonsolidasikan ke Phase 355 (Playwright UAT). Tidak ada blocker untuk lanjut.

---

_Verified: 2026-06-08T06:37:22Z_
_Verifier: Claude (gsd-verifier)_
