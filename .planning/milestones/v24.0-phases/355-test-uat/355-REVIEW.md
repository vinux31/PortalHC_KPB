---
phase: 355-test-uat
reviewed: 2026-06-09T00:00:00Z
depth: standard
files_reviewed: 4
files_reviewed_list:
  - HcPortal.Tests/PackageImageDeleteTests.cs
  - tests/e2e/helpers/examTypes.ts
  - tests/e2e/helpers/wizardSelectors.ts
  - tests/e2e/image-in-assessment.spec.ts
findings:
  critical: 0
  warning: 2
  info: 4
  total: 6
status: issues_found
---

# Phase 355: Code Review Report

**Reviewed:** 2026-06-09
**Depth:** standard
**Files Reviewed:** 4
**Status:** issues_found

## Summary

Phase 355 adalah fase TEST/UAT untuk milestone v24.0 (gambar di soal assessment) — semua
file yang ditinjau adalah kode test (xUnit + Playwright e2e + helper selektor), tanpa kode
produksi. Penilaian memakai standar test-engineering: determinisme, cleanup state, hindari
positional `.nth()` pada DOM yang di-shuffle, dan wait yang benar.

Verifikasi silang terhadap markup produksi dilakukan dan **lulus**: seluruh selektor baru di
`questionFormSelectors` (`#questionImgField`, `#questionImageAlt`, `#optAImgField` ..
`#optDImgField`, `#optAImageAlt` ..) cocok dengan `Views/Admin/ManagePackageQuestions.cshtml:145-211`;
atribut yang diassert spec (`question-image-zoom`, `data-img-alt`, `img-fluid`, `loading="lazy"`,
`onclick="event.preventDefault()"`, `#imageLightboxModal`, `.btn-close[data-bs-dismiss="modal"]`)
cocok dengan `Views/Shared/_QuestionImage.cshtml` + `_ImageLightboxModal.cshtml`; teks
`#answeredProgress` ("2/2 answered") cocok dengan `StartExam.cshtml:1005-1006`; struktur opsi
`label.list-group-item` membungkus gambar opsi cocok dengan `StartExam.cshtml:135-149/168-182`.
Helper DB (`backup`/`restore`/`queryString` di `tests/helpers/dbSnapshot.ts`) punya guard
localhost-only dan kontrak yang konsisten dengan pemakaian spec.

Penilaian keseluruhan: berkualitas baik, mengikuti pola Phase 315-319 yang sudah terbukti,
SEED_WORKFLOW dipatuhi (backup/restore + cleanup file fisik). Temuan di bawah bersifat
robustness/maintainability — tidak ada bug correctness yang memblokir, sejalan dengan status
"sudah hijau 3/3 live".

## Warnings

### WR-01: Cleanup file fisik upload bisa terlewat saat TEST 1 gagal sebelum package dibuat (state leak parsial)

**File:** `tests/e2e/image-in-assessment.spec.ts:53-69`
**Issue:** `afterAll` membersihkan folder upload hanya bila `createdPackageId != null`. Bila TEST 1
gagal SEBELUM `createDefaultPackage` mengembalikan id (mis. wizard create assessment gagal di
tengah jalan, atau `createDefaultPackage` throw saat ekstraksi `packageId`), `createdPackageId`
tetap `null` → folder `wwwroot/uploads/questions/{id}` tidak akan dibersihkan IF sebagian file
sudah ter-upload. Walau DB RESTORE mengembalikan baris DB, file fisik di disk adalah artefak yang
tidak ter-cover RESTORE (sudah disadari di komentar D-05). Risiko residual: orphan file di
working tree antar-run. Kecil, tapi melanggar prinsip "jangan biarkan seed temporary nempel"
(CLAUDE.md § Seed Data Workflow).
**Fix:** Karena upload baru terjadi SETELAH `createdPackageId` diset (`createDefaultPackage`
mengembalikan id sebelum `addQuestionViaForm`), kasus ini berdampak hanya jika upload gagal
setelah id diperoleh — sudah ter-handle. Untuk benar-benar fail-safe, pertimbangkan capture id
lebih awal atau sapu berdasarkan glob title-marker. Alternatif ringan: log peringatan eksplisit
saat `createdPackageId == null` di `afterAll` agar kebocoran terlihat:
```ts
if (createdPackageId == null) {
  console.warn('[355] createdPackageId null di afterAll — folder upload TIDAK disapu, cek manual wwwroot/uploads/questions');
}
```

### WR-02: Verifikasi guard-toggle membaca `isChecked()` tanpa menunggu efek label-activation tersettle (potensi false-negative pada regresi)

**File:** `tests/e2e/image-in-assessment.spec.ts:149-151`
**Issue:** Test mengklik `optImg` lalu segera mengassert `optRadio.isChecked() === false`. Jika
regresi bug 926a57e1 muncul kembali (label-activation tidak di-`preventDefault`), toggle radio
terjadi sinkron pada event yang sama, sehingga assert tetap menangkapnya — secara umum aman.
Namun assert dijalankan tepat setelah `expect(#imageLightboxModal).toBeVisible()`; bila browser
mem-batch micro-task berbeda, ada celah kecil di mana toggle radio (jika terjadi) baru ter-commit
setelah `isChecked()` dibaca. Ini bukan flaky pada path hijau (radio memang tidak toggle), tapi
melemahkan keandalan deteksi regresi — tujuan utama test ini.
**Fix:** Perkuat guard dengan menunggu sinyal positif bahwa label-activation TIDAK terjadi, mis.
verifikasi `#answeredProgress` tidak bertambah sebelum jawaban sengaja di-set, atau re-assert
setelah idle singkat:
```ts
await optImg.click();
await expect(page.locator('#imageLightboxModal')).toBeVisible({ timeout: 5_000 });
// re-check setelah event loop settle untuk tangkap toggle yang ter-commit telat
await expect.poll(() => optRadio.isChecked()).toBe(false);
```

## Info

### IN-01: Hard sleep `waitForTimeout(2_000)` adalah anti-pattern (non-deterministic wait)

**File:** `tests/e2e/image-in-assessment.spec.ts:166`
**Issue:** `await page.waitForTimeout(2_000)` adalah fixed delay untuk menunggu SignalR auto-save
settle. Sudah didahului assert `#answeredProgress` "2/2" (line 165) yang lebih deterministik,
tapi sleep tambahan tetap bergantung pada angka magic 2 detik (tied ke debounce 2s SaveTextAnswer).
Pada mesin lambat bisa kurang; pada mesin cepat boros waktu.
**Fix:** Idealnya tunggu sinyal `#saveIndicatorText` bernilai saved (pola `checkMAOptionsForQuestion`
di `examTypes.ts:299-302`) alih-alih sleep buta. Untuk MC radio (HTTP save, bukan SignalR), bisa
juga tunggu network idle pasca-check. Pertahankan jika sudah terbukti stabil, tapi tandai sebagai
hutang teknis.

### IN-02: `optImg` di-reuse untuk dua tujuan dengan resolusi `.first()` yang berbeda dari `optLabel`

**File:** `tests/e2e/image-in-assessment.spec.ts:139, 144-149`
**Issue:** `optImg` didefinisikan via `.first()` dari semua `img.question-image-zoom[data-img-alt=...]`,
sedangkan `optRadio` diturunkan dari `optLabel` (filter `has:`). Keduanya seharusnya menunjuk opsi A
yang sama, namun kedua locator dibangun lewat jalur independen. Karena `data-img-alt` opsi
(`OPT_IMG_ALT`) unik di halaman ini, keduanya konvergen — aman saat ini. Tapi jika kelak ada >1
opsi bergambar dengan alt sama, `optImg.first()` dan `optLabel.input` bisa divergen secara senyap.
**Fix:** Turunkan `optImg` dari `optLabel` agar terikat ke elemen yang sama, menghilangkan asumsi
keunikan alt:
```ts
const optImg = optLabel.locator('img.question-image-zoom');
```

### IN-03: Inline-logic mirror di xUnit menduplikasi logika controller tanpa pengikat anti-drift

**File:** `HcPortal.Tests/PackageImageDeleteTests.cs:30-39, 132-151`
**Issue:** `PathStillReferenced`/`DeleteIfUnreferenced`/`ApplyIntent` adalah salinan manual (mirror)
dari logika `AssessmentAdminController` (ref-count delete + `ApplyOptionImageIntent`). Komentar
sudah menandai "keep in sync", tapi tidak ada pengaman otomatis: bila controller berubah, test
tetap hijau menguji logika lama (false confidence). Ini batasan inheren strategi "inline-logic
murni tanpa DbContext" yang dipilih agar GREEN tanpa Skip — keputusan sah untuk scaffold, namun
nilai pertahanan regresi-nya terbatas.
**Fix:** Tidak ada aksi wajib untuk fase test ini. Jika diinginkan, fase mendatang dapat menaikkan
ke integration test yang memanggil method controller sungguhan (atau ekstrak predikat ref-count
ke helper produksi yang di-share controller + test) agar drift mustahil.

### IN-04: `Assert.DoesNotContain(null, collected)` redundan terhadap filter `Where(!IsNullOrEmpty)`

**File:** `HcPortal.Tests/PackageImageDeleteTests.cs:127`
**Issue:** `collected` dibangun dengan `.Where(p => !string.IsNullOrEmpty(p))` (line 119), sehingga
`null` mustahil ada. Assert `DoesNotContain(null, collected)` selalu trivially true dan tidak
menambah jaminan baru — ia menguji konstruksi list itu sendiri, bukan perilaku sistem. Tidak salah,
hanya noise.
**Fix:** Boleh dihapus, atau diganti assert yang lebih bermakna seperti memastikan opsi `ImagePath=null`
(line 103) memang ter-skip dari hasil:
```csharp
Assert.DoesNotContain(collected, p => string.IsNullOrEmpty(p));
```

---

_Reviewed: 2026-06-09_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
