---
phase: 417-section-pagination
fixed_at: 2026-06-24T00:00:00Z
review_path: .planning/phases/417-section-pagination/417-REVIEW.md
iteration: 1
findings_in_scope: 2
fixed: 2
skipped: 0
status: all_fixed
---

# Phase 417: Laporan Perbaikan Code Review (Section Pagination)

**Diperbaiki:** 2026-06-24
**Sumber review:** `.planning/phases/417-section-pagination/417-REVIEW.md`
**Iterasi:** 1

**Ringkasan:**
- Temuan dalam scope (Warning): 2
- Diperbaiki: 2
- Dilewati: 0
- Info (di luar scope wajib): 4 — semua dievaluasi; 1 (IN-04 e2e tightening) ditolak dengan alasan; 3 diterima by-design.

**Verifikasi gabungan:**
- `dotnet build` → **0 Error** (27 warning pre-existing nullability/xUnit-analyzer di file lain, tidak ada error baru; Razor compile StartExam.cshtml PASS).
- `dotnet test HcPortal.Tests --filter "FullyQualifiedName~SectionPaginator"` → **8/8 PASS** (termasuk assert boundary baru WR-02).
- migration=FALSE dijaga: tidak menyentuh `Migrations/` maupun `Data/`.

## Masalah yang Diperbaiki

### WR-01: Toast kegagalan resume memakai rumus pagination FLAT lama (nomor soal salah di mode section)

**File dimodifikasi:** `Views/CMP/StartExam.cshtml`
**Commit:** `8988b173`
**Catatan:** logic-adjacent (teks UI), tetapi murni komputasi tampilan — tidak menyentuh grading/auth. Diverifikasi via Razor compile (build hijau) + paritas dengan toast INFO yang sudah teruji e2e S5.

**Fix yang diterapkan:**
Mengganti rumus flat lama pada jalur toast KEGAGALAN resume (`prePopulateAnswers`, blok `if (RESUME_PAGE > 0)`):

```js
// SEBELUM (salah di mode section-aware):
showResumeFailureToast('... soal no. ' + (RESUME_PAGE * QUESTIONS_PER_PAGE + 1) + '.');

// SESUDAH (section-aware, identik sumber dengan toast INFO D-417-06):
var firstQid = pageQuestionIds[RESUME_PAGE] && pageQuestionIds[RESUME_PAGE][0];
var num = firstQid ? getDisplayNumForQuestion(firstQid) : (RESUME_PAGE * QUESTIONS_PER_PAGE + 1);
showResumeFailureToast('... soal no. ' + num + '.');
```

Nomor soal kini diturunkan dari `pageQuestionIds[RESUME_PAGE][0]` lalu `getDisplayNumForQuestion(...)` — sumber yang persis sama dipakai toast INFO (`showResumeInfoToast`, line 1326-1328). Fallback ke rumus flat lama dipertahankan hanya bila `firstQid` tidak tersedia (defensif; jalur FAILURE hanya men-cek `RESUME_PAGE > 0`, belum tentu `< TOTAL_PAGES`).

**Keamanan/konsistensi:**
- `pageQuestionIds` (const, line 482) sudah terdeklarasi sebelum `prePopulateAnswers()` dipanggil (line 1270) → tidak ada TDZ/undefined.
- `getDisplayNumForQuestion` membaca `.innerText` dari badge DOM (bukan `innerHTML` pada data nama Section) → tetap XSS-safe sesuai T-417-03.
- Kedua jalur toast (INFO + FAILURE) kini konsisten; regresi laten yang di-expose Phase 417 ditutup.

### WR-02: Tidak ada test yang mem-pin boundary `RESUME_PAGE == maxPage` (halaman terakhir valid)

**File dimodifikasi:** `HcPortal.Tests/SectionPaginatorTests.cs`
**Commit:** `b7a52f22`

**Fix yang diterapkan:**
Menambahkan satu assert boundary di `Resume_ClampsToValidRange`:

```csharp
// WR-02 boundary: requested == maxPage (halaman terakhir) HARUS lolos apa adanya.
Assert.Equal(4, SectionPaginator.ClampResumePage(4, 4));
```

Memin perilaku batas atas tepat (server guard `> maxPage` inklusif maxPage; client guard `< TOTAL_PAGES` == `<= maxPage`). Tanpa assert ini, regresi seperti mengubah `>` jadi `>=` di `ClampResumePage` tidak akan tertangkap. Test PASS (8/8 SectionPaginator hijau).

## Masalah Info (Diterima / Ditolak — di luar scope wajib critical_warning)

### IN-04: E2E memakai assert `count() >= N` (bukan exact) untuk header/label Section — DITOLAK (tidak diubah)

**File:** `tests/e2e/section-pagination.spec.ts:238,242,249,283`
**Keputusan:** Tidak diubah; diterima as-is dengan alasan.
**Alasan:**
- Instruksi eksplisit: perketat ke exact HANYA bila benar-benar deterministik, jika tidak biarkan + catat; jangan introduce flakiness.
- Selektor header `div.text-primary.fw-semibold` (line 236) adalah selektor CSS generik yang berpotensi menangkap elemen lain ber-class sama di luar header Section → count exact (`toBe(3)`) berisiko false-positive/flaky jika render minor berubah.
- Count exact bergantung pada perPage=10 (hasil UA detection), bukan invariant murni — pengetatan menambah kerapuhan tanpa nilai keamanan/grading.
- Memverifikasi exact-count dengan aman menuntut menjalankan e2e penuh (butuh app live + DB seed), yang eksplisit di luar scope tugas ini.
- Review sendiri mengklasifikasikan IN-04 sebagai Info non-blocking & opsional ("kualitas test sudah baik, real assertions bukan tautologi").

### IN-01: `ComputePages` mengandalkan urutan input section-monotonic — DITERIMA by-design

Precondition dijamin kontrak upstream (`GetShuffledQuestionIds`, Phase 416) + di-cover xUnit Phase 416. Bukan bug 417. Tidak ada perubahan kode.

### IN-02: `getDisplayNumForQuestion` fallback mengembalikan `qId` mentah bila qcard tak ditemukan — DITERIMA (pre-existing)

Pre-existing (bukan diperkenalkan 417). Pada jalur happy-path resume, `pageQuestionIds[currentPage][0]` valid → qcard pasti ada di DOM → fallback praktis tidak terpicu. Pada fix WR-01, fallback rumus flat lama bahkan mendahului bila `firstQid` falsy, sehingga `qId` mentah tidak akan dikirim ke toast FAILURE. Non-blocking, tidak diubah.

### IN-03: Branch legacy path StartExam + pemindahan mobile UA detection — DITERIMA (verifikasi PASS)

Konteks saja; bukan kode 417. Review mengonfirmasi pemindahan UA detection bersih (tidak ada double-set `ViewBag.QuestionsPerPage`). Tidak ada temuan, tidak ada perubahan.

---

_Diperbaiki: 2026-06-24_
_Fixer: Claude (gsd-code-fixer)_
_Iterasi: 1_
