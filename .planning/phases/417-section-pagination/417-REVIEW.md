---
phase: 417-section-pagination
reviewed: 2026-06-24T00:00:00Z
depth: standard
files_reviewed: 6
files_reviewed_list:
  - Helpers/SectionPaginator.cs
  - Models/PackageExamViewModel.cs
  - Controllers/CMPController.cs
  - Views/CMP/StartExam.cshtml
  - HcPortal.Tests/SectionPaginatorTests.cs
  - tests/e2e/section-pagination.spec.ts
findings:
  critical: 0
  high: 0
  warning: 2
  info: 4
  total: 6
status: issues_found
---

# Phase 417: Laporan Code Review (Section Pagination)

**Direview:** 2026-06-24
**Kedalaman:** standard
**File direview:** 6
**Status:** issues_found (2 Warning, 4 Info — TIDAK ada Critical/High)

## Ringkasan / Verdict

Implementasi Phase 417 (PAG-01/02/03) **solid dan well-engineered**. Inti algoritma `SectionPaginator.ComputePages` adalah fungsi murni, deterministik, dan benar untuk semua edge case yang diperiksa (0 soal, 1 section, "Lainnya" null keystone, `StartNewPage` di soal pertama yang di-guard `!firstQuestion`, auto-split tepat di batas 10, mobile perPage=5). Invariant backward-compat (no-Section = pagination flat identik) dijamin oleh dua lapis: golden test `NoSection_IdenticalToFlatBaseline` di unit + branch `hasSections` di view, dan analisis kode mengonfirmasi keduanya benar.

Keamanan XSS nama Section ditangani dengan benar di SEMUA jalur: Razor `@q.SectionName` (header) auto-encode; navigator/indikator/toast via `textContent`/`createTextNode` — TIDAK ada `innerHTML` yang menyentuh data nama Section. Single-source-of-truth anti-drift terjaga: `pageQuestionIds`, `allQuestionsData`, `pageSectionMap`, dan loop render `exam-page` SEMUA berasal dari `q.PageNumber` yang sama (hasil `ComputePages`), tanpa rekomputasi inline Skip/Take yang bisa menyimpang. Resume clamp server-authoritative (`ClampResumePage`) + guard client tambahan (`< TOTAL_PAGES`). Tidak ada pelemahan otorisasi di jalur resume — `ComputePages`/`ClampResumePage` murni komputasi tampilan, ownership session tetap dijaga oleh guard StartExam yang ada.

Temuan utama (WR-01) adalah **regresi laten nyata namun ber-impact rendah**: toast KEGAGALAN resume (`showResumeFailureToast`) masih memakai rumus flat lama `RESUME_PAGE * QUESTIONS_PER_PAGE + 1` untuk menghitung "soal no. X", padahal di bawah pagination section-aware batas halaman tidak lagi sejajar kelipatan `QUESTIONS_PER_PAGE`. Nomor soal yang ditampilkan bisa salah saat ada section ber-`StartNewPage` atau section pendek. Toast INFO baru (D-417-06) sudah benar memakai `pageQuestionIds`; toast FAILURE lama luput dari generalisasi yang sama.

Tidak ada Critical atau High. Dua Warning + empat Info di bawah; semua non-blocking.

## Warnings

### WR-01: Toast kegagalan resume memakai rumus pagination FLAT lama (nomor soal salah di mode section)

**File:** `Views/CMP/StartExam.cshtml:750`
**Issue:** Saat resume tanpa jawaban tersimpan (`IS_RESUME && RESUME_PAGE > 0` dan `SAVED_ANSWERS` kosong), toast kegagalan menghitung nomor soal pertama halaman tujuan dengan rumus flat:

```js
showResumeFailureToast('Gagal memuat jawaban sebelumnya. Lanjutkan dari soal no. '
    + (RESUME_PAGE * QUESTIONS_PER_PAGE + 1) + '.');
```

Di bawah Phase 417, `RESUME_PAGE` adalah indeks halaman **section-aware** — batas halaman tidak lagi sejajar kelipatan `QUESTIONS_PER_PAGE` begitu ada section ber-`StartNewPage` atau section pendek/`(lanjutan)`. Contoh: Section A 3 soal (page 0), Section B `StartNewPage` (page 1) → soal pertama page 1 adalah DisplayNumber 4, tapi rumus menghasilkan `1*10+1 = 11`. Nomor yang ditampilkan ke peserta salah. Ini regresi laten yang di-EXPOSE Phase 417 (bukan diperkenalkan dari nol, tapi jadi salah karena page-index berubah maknanya). Toast INFO baru (`showResumeInfoToast`, line 1326-1328) sudah benar menurunkan nomor dari `pageQuestionIds[currentPage][0]` + `getDisplayNumForQuestion` — generalisasi yang sama luput di jalur FAILURE ini.

**Fix:** Turunkan nomor soal dari sumber yang sama dengan toast INFO, bukan rumus flat:
```js
if (RESUME_PAGE > 0) {
    var firstQid = pageQuestionIds[RESUME_PAGE] && pageQuestionIds[RESUME_PAGE][0];
    var num = firstQid ? getDisplayNumForQuestion(firstQid) : (RESUME_PAGE * QUESTIONS_PER_PAGE + 1);
    showResumeFailureToast('Gagal memuat jawaban sebelumnya. Lanjutkan dari soal no. ' + num + '.');
}
```
Catatan: rumus flat ini juga ada di copy "Resume FAILURE toast" di UI-SPEC (baris 117) — selaraskan keduanya. Impact rendah (hanya teks toast, tidak memengaruhi halaman yang benar-benar ditampilkan maupun grading), karena itu Warning bukan High.

### WR-02: Tidak ada test (unit/e2e) yang mem-pin perilaku resume KE halaman bila `RESUME_PAGE === TOTAL_PAGES - 1` atau boundary `> maxPage`

**File:** `HcPortal.Tests/SectionPaginatorTests.cs:129-141`, `Views/CMP/StartExam.cshtml:1313`
**Issue:** `ClampResumePage` diuji untuk `requested=2/maxPage=4` (valid tengah), `0`, `99` (over), `-1` (negatif) — bagus. Namun **batas atas tepat** (`requested == maxPage`) tidak diuji secara eksplisit, padahal guard client di view memakai perbandingan berbeda (`RESUME_PAGE < TOTAL_PAGES`, line 1313) sedangkan server memakai `requested > maxPage` (line 58, inklusif maxPage = halaman terakhir valid). Keduanya konsisten (server: `maxPage` lolos; client: `< TOTAL_PAGES` = `< maxPage+1` = `<= maxPage` lolos), tapi tanpa test boundary `requested == maxPage` regresi pada salah satunya (mis. seseorang mengubah `>` jadi `>=`) tidak akan tertangkap. Halaman terakhir adalah target resume yang sangat plausibel (peserta hampir selesai lalu keluar).

**Fix:** Tambah satu assert boundary di `Resume_ClampsToValidRange`:
```csharp
Assert.Equal(4, SectionPaginator.ClampResumePage(4, 4)); // batas atas tepat = halaman terakhir, valid
```
Opsional: e2e S5 sudah meng-cover resume ke page>0 nyata (bagus) — cukup pin boundary di unit.

## Info

### IN-01: `ComputePages` mengandalkan urutan input section-monotonic; tidak ada guard bila urutan tercampur

**File:** `Helpers/SectionPaginator.cs:33-52`
**Issue:** Algoritma mengasumsikan `ordered` sudah urut Section 1→2→…→Lainnya (dari `GetShuffledQuestionIds`, Phase 416). Deteksi section berubah memakai `!Equals(q.SectionNumber, prevSection)` — hanya membandingkan dengan section SEBELUMNYA, bukan riwayat. Bila urutan input tercampur (mis. S1, S2, S1 lagi karena bug upstream), `ComputePages` akan memperlakukan kemunculan S1 kedua sebagai "section start" baru dan me-render header duplikat tanpa error. Ini benar-benar dependen pada kontrak Phase 416. Bukan bug di 417 (input dijamin terurut), tapi dokumentasinya bisa lebih eksplisit sebagai precondition.
**Fix:** Tidak wajib. Bila ingin defensif, tambahkan komentar precondition tegas atau (di mode debug) `Debug.Assert` bahwa SectionNumber non-decreasing dengan Lainnya terakhir. Saat ini di-cover oleh kontrak upstream + xUnit Phase 416 — terima by-design.

### IN-02: `getDisplayNumForQuestion` fallback mengembalikan `qId` (bukan DisplayNumber) bila qcard tak ditemukan

**File:** `Views/CMP/StartExam.cshtml:603-610`
**Issue:** Fungsi mengambil DisplayNumber dari teks badge DOM; bila `qcard_<id>` tidak ada di DOM, ia mengembalikan `qId` mentah (Question primary key) sebagai "nomor soal". Untuk toast resume (line 1328) bila `pageQuestionIds[currentPage][0]` valid maka qcard pasti ada, jadi praktis aman. Tapi mengembalikan PK database sebagai "soal no." adalah fallback yang membingungkan bila pernah terpicu (mis. id=173 ditampilkan sebagai "Lanjut dari soal no. 173"). Pre-existing (bukan diperkenalkan 417) tapi kini dipakai oleh fitur 417.
**Fix:** Fallback ke string kosong atau angka aman, mis. `return '';` lalu toast guard bila kosong. Non-blocking; jalur happy-path benar.

### IN-03: Branch `else` legacy path di StartExam mengembalikan error setelah sebagian ViewBag mungkin ter-set — tidak relevan 417 tapi diintai

**File:** `Controllers/CMPController.cs:1340-1345`
**Issue:** (Konteks saja — bukan kode 417.) Mobile UA detection + `ViewBag.QuestionsPerPage` kini di-set DI DALAM package-path (line 1242-1249) sebelum `ComputePages`, dan legacy path return early via redirect. Pemindahan ini benar dan tidak meninggalkan `ViewBag.QuestionsPerPage` ganda (blok lama dihapus, dikonfirmasi komentar line 1349-1350 + grep). Tidak ada temuan; dicatat hanya untuk menegaskan pemindahan UA detection sudah bersih (sesuai fokus review "mobile UA detection move").
**Fix:** Tidak ada. Verifikasi PASS.

### IN-04: E2E menggunakan assert `count() >= N` (bukan exact) untuk header/label Section — toleran tapi longgar

**File:** `tests/e2e/section-pagination.spec.ts:238,242,249,283`
**Issue:** S1-S4 meng-assert `toBeGreaterThanOrEqual(2/3/1)` untuk jumlah header Section, penanda "(lanjutan)", dan label navigator. Pendekatan ini robust terhadap detail render (mis. header muncul per-halaman page-break), dan alasannya didokumentasikan dengan baik (header di halaman hidden → assert KEHADIRAN bukan visible — keputusan yang benar). Namun assert `>=` tidak akan menangkap regresi "header berlebih" (mis. header duplikat karena bug IN-01). Untuk assessment RICH yang deterministik (12+4 soal, perPage 10) jumlah EKSAK header dapat dihitung (Pompa page0 + Pompa-lanjutan page1 + Valve page2 = tepat 3). Test bukan tautologi — assertion-nya nyata mem-pin perilaku — hanya bisa lebih ketat.
**Fix:** Opsional, perketat ke `toBe(3)` untuk header dan `toBe(1)` untuk "(lanjutan)" pada skenario RICH yang count-nya deterministik, agar regresi over-render tertangkap. Non-blocking; kualitas test sudah baik (real assertions, bukan tautologi).

---

## Catatan verifikasi fokus area (semua PASS)

- **Correctness page-number:** `ComputePages` benar di semua edge — auto-split tepat di `countOnPage >= perPage` (batas 10), `needNewPageForSection` di-guard `!firstQuestion` (section pertama tidak memaksa page-break, di-cover test `StartNewPage_BreaksBeforeSection` + e2e S7 `pageA == 0`), "Lainnya" (null) tidak memaksa break (`SectionStartNewPage` default false, test `LainnyaGroup_NoForcedBreak`).
- **Off-by-one:** `totalPages = pageGroups.Count` (bukan `Ceiling(N/perPage)`) — benar untuk page-break section-aware (Pitfall 2 dihindari). `maxPage417 = Max(PageNumber)` konsisten dengan `pageGroups.Count - 1`.
- **Single-source-of-truth:** `pageQuestionIds`/`allQuestionsData`/`pageSectionMap`/loop render semua dari `q.PageNumber` — TIDAK ada rekomputasi Skip/Take inline yang bisa drift. Dikonfirmasi grep + baca.
- **Backward-compat:** branch `hasSections` (view) + golden test `NoSection_IdenticalToFlatBaseline` (`PageNumber == (DisplayNumber-1)/perPage`) + e2e S6 (0 header, navigator flat, indikator `^Halaman \d+/\d+$`). Output no-Section identik pra-417.
- **XSS:** header `@q.SectionName` (Razor auto-encode); `appendSectionLabel` → `lbl.textContent`; `updatePageIndicator` → `el.textContent`; `showResumeInfoToast` → `createTextNode`. Tidak ada `innerHTML` pada data nama Section (innerHTML hanya pada skeleton toast statik + clear container `''`). PASS.
- **Authz:** `ComputePages`/`ClampResumePage` murni; jalur resume tidak menambah endpoint mutasi; ownership session tetap di guard StartExam existing. Tidak ada pelemahan.
- **Robustness:** null/empty SectionName → `'Lainnya'` fallback di JS (`name || 'Lainnya'`, `pageSectionMap[...] || 'Lainnya'`); `ComputePages` ArgumentNullException pada `ordered==null`, `perPage<1` di-clamp ke 1; 0 soal → `totalPages=1`, `maxPage417=0` (guard `Count > 0`).

---

_Direview: 2026-06-24_
_Reviewer: Claude (gsd-code-reviewer)_
_Kedalaman: standard_
