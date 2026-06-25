---
phase: 420-editquestion-identity-based-option-editing
reviewed: 2026-06-25T00:00:00Z
depth: standard
files_reviewed: 6
files_reviewed_list:
  - Models/OptionInput.cs
  - Controllers/AssessmentAdminController.cs
  - Views/Admin/ManagePackageQuestions.cshtml
  - HcPortal.Tests/EditShrinkGuardIntegrationTests.cs
  - Helpers/OptionShrinkGuard.cs
  - Helpers/QuestionOptionValidator.cs
findings:
  critical: 1
  warning: 3
  info: 2
  total: 6
status: issues_found
---

# Phase 420: Code Review Report

**Reviewed:** 2026-06-25
**Depth:** standard
**Files Reviewed:** 6
**Status:** issues_found

## Summary

Phase 420 mengganti upsert posisional EditQuestion menjadi identity-based (match by stable `PackageOption.Id`). Arsitektur keseluruhan sudah benar: anti-tamper (D-01a) dijalankan fail-closed SEBELUM mutasi, kill-drift terpenuhi (satu himpunan `removedOptionIds` dipakai guard + upsert), dan JS hidden-Id clone-reset sudah menghapus value baru (`inp.type === 'hidden'`). Dua dari delapan test baru di `EditShrinkGuardIntegrationTests.cs` sudah diperbarui ke kontrak identity.

**Satu temuan Critical:** Dua test regresi di `SectionFixRegressionTests.cs` memanggil `EditQuestion` dengan semua-`null`-Id pada soal yang mempunyai opsi existing (tapi tanpa response). Dengan algoritma identity baru, semua opsi existing akan DIHAPUS dan diganti opsi baru (Id berubah). Test-nya lulus karena hanya mengecek jumlah dan teks opsi — bukan stabilitas Id — sehingga regresi senyap: kalau di production HC mengedit soal via form lama (bug submit tanpa Id), semua opsi akan ter-recreate. Ini adalah semantic drift yang menyamar sebagai test green.

Tiga Warning: (a) baris Id-non-null + teks KOSONG disingkirkan dari `keptIds` secara diam-diam — opsinya akan dihapus tanpa pesan error; (b) `ResolveCorrectness` memakai posisi dalam daftar `options` bukan Id-match, sehingga `correctIndex` milik baris baru (null-Id) bisa menunjuk Id-existing yang salah jika urutan campuran; (c) dua test lama `SectionFixRegressionTests` mengirim null-Id ke soal dengan existing options — seharusnya ditambahkan `Assert` pada stabilitas Id untuk mengunci perilaku baru.

---

## Critical Issues

### CR-01: SectionFixRegressionTests — null-Id semua pada soal existing → silent mass-recreate (tidak dikunci oleh assertion)

**File:** `HcPortal.Tests/SectionFixRegressionTests.cs:461-484` dan `:517-538`

**Issue:** `Edit6Options_NoResponses_Succeeds_OptionsUpdated` dan `H3_EditQuestionWith4Options_SucceedsNormally` memanggil `EditQuestion` POST dengan `OptionInput` yang semua `Id`-nya `null` (`new OptionInput { Text = "A" }` dst.) pada soal yang sudah punya opsi existing di DB.

Dengan algoritma identity Phase 420:
- `submittedIds` = `[]` (kosong) → anti-tamper lolos (tidak ada Id non-null untuk divalidasi)
- `keptIds` = `[]` (tidak ada baris dengan `Id.HasValue`)
- `newRows` = semua 6 baris (null-Id + teks)
- `removedOptionIds` = `existingIds.Except(keptIds)` = **semua** Id existing
- `answered` = kosong (tidak ada response) → guard lolos
- Upsert: semua opsi existing di-REMOVE, 6 opsi baru di-ADD dengan Id baru

Test lulus karena assertionnya hanya mengecek `Count` dan `OptionText` — tidak mengecek kestabilan Id. Artinya ada **jalan kode produktif yang tidak terkunci**: kalau form submit tanpa hidden Id (misalnya karena JS gagal mengisi `option_A_id`, atau browser-punya bug `populateEditForm`), semua opsi existing akan di-recreate silently. Jawaban peserta yang merujuk Id opsi lama akan menggantung (FK Restrict akan mencegah delete hanya saat sudah ada response — ini scenario tanpa response, sehingga FK tidak memproteksi).

**Fix:** Tambah assertion stabilitas Id pada kedua test itu agar mengunci bahwa opsi existing di-UPDATE in-place (Id tidak berubah), bukan di-recreate. Jika perilaku yang diinginkan adalah "null-Id = treat semua sebagai newRows ADD", tambahkan catatan eksplisit. Jika perilakunya adalah "null-Id = legacy positional fallback", maka diperlukan cabang kode khusus. Yang paling mudah dan tepat adalah memperbarui test mengikuti kontrak Phase 420 (submit dengan Id seperti test-test baru):

```csharp
// Di SectionFixRegressionTests.cs — Edit6Options_NoResponses_Succeeds_OptionsUpdated
// Ganti null-Id rows dengan Id dari DB:
var optIds = q.Options.OrderBy(o => o.Id).Select(o => o.Id).ToList();
// ...kemudian dalam EditQuestion call:
new List<OptionInput>
{
    new OptionInput { Id = optIds[0], Text = "A" },
    new OptionInput { Id = optIds[1], Text = "B" },
    // dst.
},
// Dan tambah assertion di verify block:
Assert.Equal(optIds[0], q.Options.Single(o => o.OptionText == "A").Id); // Id stabil (UPDATE, bukan recreate)
```

Kalau keputusan desainnya "null-Id semua = hapus + recreate" (skenario CREATE baru lewat EditQuestion), itu harus didokumentasikan eksplisit dan test harus mengkonfirmasi perilaku itu — bukan secara accidental lolos.

---

## Warnings

### WR-01: Opsi Id-non-null + teks kosong dihapus tanpa pesan error (silent delete via blank text)

**File:** `Controllers/AssessmentAdminController.cs:8049-8052`

**Issue:** Sebuah baris submit dengan `Id != null` tapi `Text` kosong/whitespace menghasilkan: Id-nya tidak masuk `keptIds` (ekskludasi `!string.IsNullOrWhiteSpace(o.Text)`) dan juga tidak masuk `newRows` (`!o.Id.HasValue` gagal). Opsinya menjadi kandidat `removedOptionIds`. Kalau opsi itu tidak punya response → dihapus tanpa warning. Kalau punya response → guard memblok dengan pesan "sudah dijawab".

Urutan logika ini BERBEDA dengan cara legacy Phase 418 yang menginterpretasi teks-kosong sebagai "baris diabaikan" (bukan sebagai delete signal). Seorang HC yang tidak sengaja mengosongkan teks opsi (misal: JS bug atau copy-paste clear) akan menyebabkan opsi itu terhapus diam-diam pada submit berikutnya — padahal intent HC bukan menghapus.

Ini bukan blocker (guard masih melindungi saat ada response), tapi perlu dipertimbangkan apakah intent "Id non-null + teks kosong" seharusnya → error ("Teks opsi tidak boleh kosong saat Id disuplai") daripada silent delete.

**Fix yang direkomendasikan:**
```csharp
// Setelah blok anti-tamper (baris ~8046), sebelum keptIds:
var idNonNullBlankText = options.Where(o => o.Id.HasValue && string.IsNullOrWhiteSpace(o.Text)).ToList();
if (idNonNullBlankText.Any())
{
    TempData["Error"] = "Teks opsi tidak boleh kosong.";
    return RedirectToAction("ManagePackageQuestions", new { packageId });
}
```
Alternatif: perlakukan sebagai "keep dengan teks lama" atau tampilkan warning. Yang penting perilakunya tidak silent.

### WR-02: ResolveCorrectness berbasis posisi-dalam-options-list; correctIndex dari baris newRow (null-Id) bisa berimplikasi salah pada upsert campuran

**File:** `Controllers/AssessmentAdminController.cs:7694-7701` (ResolveCorrectness) dan `7942-7943` (pemanggilan)

**Issue:** `ResolveCorrectness` meng-set `options[i].IsCorrect = (correctIndex == i)` berdasarkan posisi dalam list `options` yang dikirim form. Ini benar untuk MC biasa. Namun, bila submit berisi campuran Id-existing + baris-null (ADD), `correctIndex` merujuk posisi dalam list SUBMIT, bukan posisi dalam DB. Misalnya:
- Opsi existing: A(id=10), B(id=11), C(id=12) 
- Submit: `[{id=10,"A"}, {id=11,"B"}, {null,"Baru"}]`, `correctIndex=2`
- `ResolveCorrectness` set `options[2].IsCorrect=true` → baris null "Baru" ditandai benar → baris null masuk `newRows` → `PackageOption` baru dibuat dengan `IsCorrect=true`

Ini adalah perilaku yang benar secara teknis (correctIndex=2 = baris ke-3 = "Baru"). Tidak ada bug di sini per se — tapi risikonya muncul jika form mengirim urutan yang berbeda dari tampilan (misal: JS reorder atau server-side reletter berbeda dari submit order). Tidak ada test yang memverifikasi skenario "add new option AND set it as correct".

**Fix:** Tidak ada perubahan kode yang diperlukan sekarang — ini adalah design observation. Namun test #5 di `EditShrinkGuardIntegrationTests.cs` (IdentityEdit_AddOption_NullId_Adds_NotOverwriteExisting) menggunakan `correctIndex=0` (opsi A/existing yang benar), bukan opsi baru. Tambahkan test kasus "add new option AND set it correct (correctIndex pointing at null-Id row)".

### WR-03: populateEditForm mengisi ulang semua baris termasuk padding, tapi padding di luar opts.length tidak di-clear Id-nya secara eksplisit

**File:** `Views/Admin/ManagePackageQuestions.cshtml:712-725`

**Issue:** `ensureRowCount(opts.length || MIN_OPTIONS)` memastikan jumlah baris == jumlah opsi. Baris padding TIDAK ada karena `ensureRowCount` di-trim ke `opts.length`. Ini benar untuk kasus normal (soal 4 opsi → 4 baris, tidak ada padding).

Namun `MIN_OPTIONS = 2`: bila soal mempunyai hanya 1 opsi (edge case — bisa saja dari import rusak), `ensureRowCount(1)` akan di-clamp ke 2, menghasilkan 2 baris. Baris ke-2 adalah hasil `addOptionRow()` (clone baris pertama yang sudah di-clear Id). Id baris ke-2 akan kosong. Ini behavior yang benar (opsi baru jika disubmit).

Tapi ada risiko berbeda: `opts.forEach` hanya iterasi atas `opts` (1 item). Baris ke-2 (index=1, letter B) tidak diisi Id karena tidak ada `opts[1]`. Id-nya sudah dikosongkan oleh `addOptionRow`. Aman.

Skenario yang lebih berisiko: bila `opts.length` lebih besar dari `MIN_OPTIONS` tapi `ensureRowCount` menghapus baris-baris dari atas (bukan bawah) ketika shrink — tapi `ensureRowCount` shrink dari `last` → aman.

Tidak ada bug konkret, tapi `populateEditForm` tidak secara eksplisit zero-fill Id pada baris yang bukan dari `opts` (ada komentar "Baris padded di luar opts.length → Id kosong (baru)"). Ini hanya perlu ditambah komentar atau assertion test.

**Rekomendasi:** Tambahkan zero-fill eksplisit setelah `opts.forEach` untuk baris di luar `opts.length` (defensive):
```js
// Zero-fill Id baris padding (di luar opts.length) — defensive untuk kasus ensureRowCount MIN_OPTIONS > opts.length
var allRows = optionRowEls();
for (var j = opts.length; j < allRows.length; j++) {
    var padId = allRows[j].querySelector('.opt-id-input');
    if (padId) padId.value = '';
}
```

---

## Info

### IN-01: Comment stale di TEST 1 menyebutkan mekanika posisional lama

**File:** `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs:183-188`

**Issue:** Komentar header TEST 1 masih menyebut "Kita kirim 4 OptionInput dengan posisi-B (index 1) ber-teks KOSONG" dan "guard memakai aturan index-aligned". Padahal kode test sudah diperbarui ke kontrak identity (omit baris B, submit A/C/D dengan Id). Komentar menyesatkan bila dibaca oleh reviewer berikutnya.

**Fix:** Update komentar TEST 1 (baris 183-188) untuk mendeskripsikan mekanika identity: "Hapus opsi B (opsi TENGAH) dengan menghilangkan baris B dari submit. A/C/D dikirim dengan Id masing-masing."

### IN-02: Komentar TEST 2 di SectionFixRegressionTests masih menyebut kontrak posisional

**File:** `HcPortal.Tests/SectionFixRegressionTests.cs:258-260` (yang lama, sebelum Phase 420 test tambahan)

**Issue:** Komentar "Kita kirim 3 OptionInput (A,B,C) → keep=3 → posisi index 3 (opsi D, OrderBy Id) di luar keep → removed" masih mendeskripsikan mekanika posisional dari Phase 418, padahal kode test sudah berubah ke identity. Tapi komentar ini berada di test yang sekarang dikirim dengan null-Id (lihat CR-01), sehingga masalah utamanya adalah CR-01, bukan komentar.

**Fix:** Update komentar untuk mencerminkan kontrak identity: "A/B/C dikirim dengan Id masing-masing. D (optionIds[3]) DIOMIT → D masuk removedOptionIds via set-difference → D belum dijawab → dihapus."

---

## Pemeriksaan Tambahan (tidak ada temuan)

**Anti-tamper completeness (D-01a):** Lolos. Validasi berjalan SEBELUM mutasi apapun (sebelum `keptIds`, `newRows`, `removedOptionIds`, guard, SaveChanges). Forged Id dari paket/soal lain tertangkap di `!existingIds.Contains(id)`. Duplicate Id tertangkap di `Count != Distinct().Count()`. Kedua check memakai `existingIds = q.Options.Select(o=>o.Id).ToHashSet()` yang di-load dengan `Include(q => q.Options)` → benar.

**RBAC + Antiforgery:** `[Authorize(Roles = "Admin, HC")]` dan `[ValidateAntiForgeryToken]` ada di GET (7875-7876) dan POST (7924-7926). Tidak berubah. Aman.

**Kill-drift (D-01c):** Lolos. `removedOptionIds` = `existingIds.Except(keptIds)` pada baris 8057-8059. Loop upsert memakai `keptIds.Contains(o.Id)` (baris 8141) untuk cabang UPDATE vs REMOVE — ekuivalen dengan `existingIds.Except(keptIds)` secara set-semantics. Guard juga memakai `removedOptionIds` yang sama. Satu himpunan, tidak ada drift.

**Essay branch:** Lolos. Essay → `removedOptionIds = existingIds.ToList()` (semua). Guard menyala jika ada response ke opsi manapun. Jika guard lolos → `RemoveRange(q.Options)` → semua opsi dihapus. `newRows` tidak dipakai di branch Essay (benar, Essay tidak punya opsi).

**CreateQuestion regression (OPTEDIT-05):** Lolos. `CreateQuestion` tidak membaca `inp.Id` sama sekali — hanya `inp.Text`, `inp.IsCorrect`, `inp.Image`, `inp.ImageAlt`. Penambahan properti `Id` ke `OptionInput` inert di sini.

**Import/Inject path:** `OptionInput` tidak dipakai di `ImportPackageQuestions` (import 415 langsung membuat `PackageOption` dari row Excel). Form Inject membuat soal baru lewat path tersendiri (bukan EditQuestion upsert). Tidak ada regresi.

**JS hidden-Id clone-reset:** Lolos. `addOptionRow()` baris 857 sudah menangani `inp.type === 'hidden'` → `inp.value = ''`. Komentar `// Phase 420 GOTCHA §2c` menandai ini eksplisit.

**JS reletter Id preservation:** Lolos. `reletterRows()` hanya me-rename `name` dan `id` attribute, tidak menghapus `.value`. Id dipreservasi saat reletter.

**XSS di hidden value / pesan error:** Hidden value `option_A_id` adalah integer (server-sourced via JSON `opt.id`), di-set via `.value = String(opt.id)`. Tidak ada interpolasi HTML. Pesan error D-04 diset via `TempData["Error"]` (string server-side) → ditampilkan via Razor `@TempData["Error"]` yang auto-encode. Tidak ada XSS.

---

_Reviewed: 2026-06-25_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
