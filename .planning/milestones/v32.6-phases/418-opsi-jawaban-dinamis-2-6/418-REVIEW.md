---
phase: 418-opsi-jawaban-dinamis-2-6
reviewed: 2026-06-24T00:00:00Z
depth: standard
files_reviewed: 17
files_reviewed_list:
  - Models/OptionInput.cs
  - Helpers/OptionShrinkGuard.cs
  - Helpers/QuestionOptionValidator.cs
  - Controllers/AssessmentAdminController.cs
  - Views/Admin/ManagePackageQuestions.cshtml
  - Views/Admin/_InjectQuestionForm.cshtml
  - Views/Admin/InjectAssessment.cshtml
  - Views/CMP/StartExam.cshtml
  - Views/CMP/Results.cshtml
  - Views/CMP/ExamSummary.cshtml
  - Views/Admin/_PreviewQuestion.cshtml
  - Views/Admin/PreviewPackage.cshtml
  - HcPortal.Tests/OptionValidationTests.cs
  - HcPortal.Tests/EditShrinkGuardLogicTests.cs
  - HcPortal.Tests/EditShrinkGuardIntegrationTests.cs
  - tests/e2e/helpers/wizardSelectors.ts
  - tests/e2e/option-dynamic-418.spec.ts
findings:
  critical: 0
  high: 0
  warning: 1
  info: 4
  total: 5
status: issues_found
---

# Phase 418: Laporan Code Review — Opsi Jawaban Dinamis 2–6

**Direview:** 2026-06-24
**Kedalaman:** standard
**File direview:** 17
**Status:** issues_found (0 Critical / 0 High / 1 Warning / 4 Info)

## Ringkasan Verdict

Implementasi Phase 418 **sehat dan siap-ship dari sisi keamanan & integritas binding.** Fokus utama review — kecocokan nama field form ↔ binding controller, dan keamanan (mass-assignment, CSRF, XSS, file upload, FK-Restrict guard) — **semuanya LULUS tanpa temuan**. Build `HcPortal.csproj` bersih (0 warning / 0 error).

Yang ditemukan: **1 Warning** semantik nyata (penghapusan opsi tengah yang sudah dijawab di-rename diam-diam alih-alih diblokir — konsekuensi sah dari upsert posisional, tetapi melemahkan janji guard D-418-02), dan **4 Info** (kosmetik / konsistensi). Tidak ada satupun yang memblokir.

Verifikasi kunci yang dilakukan eksplisit:

- **Binding integrity (prioritas tertinggi):** nama field markup PERSIS cocok dengan binding controller di kedua POST. Markup mengemit `options[i].Text`, `options[i].IsCorrect`, `options[i].Image`, `options[i].ImageAlt`, `options[i].RemoveImage`, dan `correctIndex` (radio MC). JS `reletterRows()` mempertahankan nama-nama itu indeks-konsisten setelah Tambah/Hapus baris. **Tidak ada mismatch yang akan men-drop data secara senyap.**
- **Kill-drift removed-detection (plan-check #4):** aturan "opsi dihapus" di guard edit-shrink (baris 8040–8045) dan di loop upsert (baris 8124–8152) **terbukti identik secara aljabar**. Guard: dihapus bila `i >= keep ATAU !stillFilled` (di mana `stillFilled = i < options.Count && Text non-empty`). Upsert: dihapus bila `slot != null && !hasText` di mana `!hasText = i >= options.Count ATAU Text empty ATAU i >= keep`. Keduanya = `i >= keep ∨ i >= options.Count ∨ Text empty`. **Tidak ada drift.**
- **correctIndex → IsCorrect:** `ResolveCorrectness` set `IsCorrect = (correctIndex == i)` untuk MC (bounds-safe, tidak off-by-one; correctIndex out-of-range → 0 benar → ditolak gate `correctCount != 1`). MA pakai `options[i].IsCorrect` per-baris apa adanya. Benar untuk kedua tipe.
- **Validator min-2/max-6:** ditegakkan server-side di `QuestionOptionValidator` (`filled < 2` dan `filled > 6`), dipanggil oleh Create + Edit lewat satu helper bersama.
- **Backward-compat 4-opsi:** array render diperluas ke superset `{A..F}` index-derived dengan fallback numerik di semua 5 view; bug modulo PreviewPackage diperbaiki (no-wrap). Soal 4-opsi render & dinilai identik.

## Warnings

### WR-01: Hapus opsi-tengah yang sudah dijawab tidak diblokir guard — jawaban peserta di-relabel diam-diam

**File:** `Controllers/AssessmentAdminController.cs:8120-8153` (loop upsert posisional) berinteraksi dengan guard `:8030-8069`
**Issue:**
Loop upsert memetakan input ke opsi existing **berdasarkan posisi** (`existing[i]` urut `OrderBy(o => o.Id)`), mempertahankan `PackageOption.Id` per posisi. Konsekuensinya, saat HC menghapus opsi di TENGAH (mis. soal A,B,C,D, opsi B dihapus → form re-letter mengirim 3 baris "A","C","D"):

- i=0: existing[0]=A ← "A" (UPDATE, tak berubah)
- i=1: existing[1]=**B** ← "C" (UPDATE — record opsi B kini berteks "C")
- i=2: existing[2]=C ← "D" (UPDATE — record opsi C kini berteks "D")
- i=3: existing[3]=D → kosong (REMOVE — hanya opsi TERAKHIR yang dihapus)

`removedOptionIds` guard = hanya `existing[3]` (D). Jadi bila peserta sudah menjawab **opsi B**, guard D-418-02 **TIDAK menyala** (Id B selamat, hanya teksnya berubah dari "B" → "C"). Edit lolos; jawaban tercatat peserta yang dulu menunjuk "B" kini diam-diam menunjuk opsi berteks "C". Janji guard ("opsi yang sudah dijawab tidak bisa dihapus") hanya benar untuk penghapusan dari EKOR; penghapusan tengah me-relabel alih-alih memblok.

Ini **konsekuensi sah dari desain upsert-posisional yang dikunci** (preserve Id by position, RESEARCH Pattern 2) dan **bukan FK-Restrict 500** (hazard 999.14 tetap tertutup — tidak ada crash). Tetapi efek integritas-data (makna jawaban peserta berubah senyap) layak dicatat: grading by `PackageOption.Id` tetap konsisten secara teknis, namun secara bisnis jawaban "B" peserta kini di-skor terhadap teks opsi yang berbeda.

**Fix (opsional — keputusan produk, bukan bug blocking):**
Bila perilaku ini tidak diinginkan, perketat guard agar mendeteksi "opsi yang dijawab teksnya berubah" pada posisi yang ada, bukan hanya yang di-`Remove`. Contoh: bandingkan teks baru per-posisi terhadap opsi yang dijawab dan tolak bila berbeda:
```csharp
// Di blok guard, selain removedOptionIds, deteksi relabel-on-answered:
for (int i = 0; i < existingForGuard.Count && i < keep; i++)
{
    var slot = existingForGuard[i];
    bool textChanged = i < options.Count
        && !string.IsNullOrWhiteSpace(options[i].Text)
        && !string.Equals(slot.OptionText?.Trim(), options[i].Text!.Trim(), StringComparison.Ordinal);
    if (textChanged && answered.Contains(slot.Id))
        // tambahkan slot.Id ke daftar "tidak boleh diubah maknanya"
        ...
}
```
Alternatif lebih murah & sesuai ekspektasi UX: bila Anda menerima perilaku saat ini, **dokumentasikan eksplisit** di copy UI ("Menghapus opsi tengah akan menggeser opsi di bawahnya") sehingga HC sadar opsi tidak dihapus berdasarkan identitas melainkan posisi. Disarankan koordinasi dengan keputusan produk D-418-02 sebelum mengubah; jangan ubah tanpa konfirmasi karena upsert-posisional dikunci spec.

## Info

### IN-01: `.Distinct()` redundan setelah `.Intersect()`

**File:** `Helpers/OptionShrinkGuard.cs:33`
**Issue:** `removedOptionIds.Intersect(answeredOptionIds).Distinct().ToList()` — LINQ `Intersect` sudah mengembalikan elemen distinct (set semantics), jadi `.Distinct()` tidak punya efek. Tidak salah, hanya operasi mubazir.
**Fix:** Hapus `.Distinct()`:
```csharp
=> removedOptionIds.Intersect(answeredOptionIds).ToList();
```
(Kontrak dokumentasi tetap valid — hasil tetap distinct dari `Intersect`.)

### IN-02: `resetForm()` tidak menyusutkan baris ekstra setelah batal edit soal 5–6 opsi

**File:** `Views/Admin/ManagePackageQuestions.cshtml:1007-1028`
**Issue:** `resetForm()` memanggil `form.reset()` (mengosongkan nilai) tetapi TIDAK memanggil `ensureRowCount(4)`/`reletterRows()`. Bila HC mengedit soal 5/6 opsi lalu klik "Batal", form kembali ke mode "Tambah Soal" namun tetap menampilkan 5/6 baris kosong (bukan 4). Tidak merusak data — Create mengabaikan baris kosong (Pitfall 4) — tapi UX tidak konsisten dengan keadaan awal.
**Fix:** Tambahkan reset jumlah baris di `resetForm()`:
```javascript
ensureRowCount(4);   // kembalikan ke 4 baris default A–D
```
(letakkan sebelum `IMAGE_FIELDS.forEach(resetImageField)` agar IMAGE_FIELDS dibangun ulang untuk jumlah baris yang benar).

### IN-03: `#injAuthError` tidak punya `role="alert"` (inkonsisten dengan `#authError` authoring)

**File:** `Views/Admin/_InjectQuestionForm.cshtml:88`
**Issue:** Container error authoring utama `#authError` (ManagePackageQuestions.cshtml:396) sudah punya `role="alert"` (a11y, dinaikkan WAJIB oleh UI-SPEC). Container error form inject `#injAuthError` tidak. Inkonsistensi a11y minor; pesan validasi inject tidak diumumkan oleh screen reader.
**Fix:**
```html
<div id="injAuthError" class="alert alert-danger py-1 px-2 small d-none mb-2" role="alert"></div>
```

### IN-04: Komentar `ensureRowCount` menyebut rentang "(1..6)" padahal di-clamp ke MIN_OPTIONS (2)

**File:** `Views/Admin/ManagePackageQuestions.cshtml:880`
**Issue:** Komentar berbunyi "Pastikan jumlah baris == n (1..6)" namun implementasi `Math.max(MIN_OPTIONS, ...)` meng-clamp ke minimum 2, bukan 1. Komentar menyesatkan (kode benar — minimum 2 sesuai D-418-01). Murni dokumentasi.
**Fix:** Ganti "(1..6)" → "(2..6)" pada komentar.

## Catatan Positif (LULUS — diverifikasi eksplisit)

- **Mass-assignment (T-418-06):** `OptionInput` whitelist eksplisit 5 properti, **tanpa `Id`** dari client. Id PackageOption ditentukan server via `existing[i]`. Aman.
- **CSRF + Authz:** kedua POST `CreateQuestion`/`EditQuestion` punya `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`; `@Html.AntiForgeryToken()` ada di dalam form; manipulasi baris JS di dalam form existing (token tak hilang — Pitfall 7).
- **`TruncateAlt`:** masih dipakai untuk alt soal + alt opsi (truncate 255) di Create & Edit & `ApplyOptionImageIntent` — tidak hilang saat refactor.
- **File upload (V12 ASVS):** validasi gambar fail-fast me-loop SEMUA file (`new[]{questionImage}.Concat(options.Select(o=>o.Image))`) — mencakup E/F; pakai `FileUploadHelper.ValidateImageFile`/`SaveFileAsync` (tidak hand-rolled).
- **XSS:** render opsi pakai Razor auto-encode (`@opt.OptionText`, `@option.OptionText`, `@item.QuestionText`); JS inject pakai `textContent`/`createTextNode`, bukan `innerHTML`, untuk data opsi/judul. Tidak ada `Html.Raw`.
- **IDOR Section (T-415-08):** guard `AssessmentPackageSections.AnyAsync(s => s.Id == sectionId && s.AssessmentPackageId == packageId)` dipertahankan di kedua POST.
- **Render A–F:** StartExam (2 site MA+MC), Results, ExamSummary, _PreviewQuestion semua diperluas ke `{A..F}` index-derived dengan fallback `(oi+1)`. PreviewPackage **bug modulo diperbaiki** (`optIdx < letters.Length ? letters[optIdx] : (optIdx+1)`, no-wrap; opsi ke-6 kini "F" bukan "A").
- **Inject parity:** form inject baris dinamis murni client-side; reader baca `.checked` per-baris DOM aktual (2–6), nol perubahan kontrak server; radio MC share `name="injCorrect"` (native single-select).
- **Kualitas test:** `OptionValidationTests` (10 Fact, termasuk max-6 / 5-6 opsi valid / correct-without-text di E) + `EditShrinkGuardLogicTests` (4 Fact irisan set) + `EditShrinkGuardIntegrationTests` (2 Fact real-SQL yang menggerakkan controller ASLI, membuktikan no-DbUpdateException + state DB benar + opsi B selamat + opsi D terhapus). Assertion nyata & substantif — bukan placeholder.

---

_Reviewed: 2026-06-24_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
