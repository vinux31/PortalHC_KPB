---
phase: 371-sesi-online-tampil-di-tab-input-records-visibility-only
reviewed: 2026-06-12T00:00:00Z
depth: standard
files_reviewed: 1
files_reviewed_list:
  - Views/Admin/Shared/_TrainingRecordsTab.cshtml
findings:
  critical: 0
  warning: 0
  info: 3
  total: 3
status: issues_found
---

# Phase 371: Code Review Report

**Reviewed:** 2026-06-12
**Depth:** standard
**Files Reviewed:** 1
**Status:** issues_found

## Summary

Review terhadap satu file Razor view `Views/Admin/Shared/_TrainingRecordsTab.cshtml`
untuk perubahan Phase 371 (URG-03): melonggarkan filter `IsManualEntry` agar sesi
assessment online ikut tampil di expand per-pekerja pada tab "Input Records",
menambah badge tipe 3-arah (Training Manual / Assessment Manual / Assessment Online),
derivasi status online 6-arah lewat local function inline
(`OnlineLabel`/`OnlineClass`/`ManualStatusClass`), link "Lihat hasil" ke /CMP/Results
yang di-gate, dan copy empty-state baru. Bersifat visibility-only (tanpa edit/hapus
untuk baris online).

Penilaian umum: **perubahan aman dan benar**. Tidak ditemukan isu Critical maupun
Warning. Tiga temuan Info bersifat kosmetik/kebersihan kode, tidak mengubah perilaku
dan tidak wajib diperbaiki di fase ini.

Hasil verifikasi terhadap area fokus yang diminta:

- **Konsistensi shape anon-type (3 operand `.Concat()`)** — AMAN. Ketiga anon-type
  (`trainingRows` L296, `assessmentRows` L299, `onlineRows` L302) memiliki 10 properti
  dengan nama, urutan, dan tipe yang identik:
  `Type:string, Date:DateTime, Title:string, Detail:string, Status:string,
  StatusClass:string, ValidUntil:DateOnly?, Id:int, IsOnline:bool, CanViewResult:bool`.
  `Date` konsisten `DateTime` di ketiganya (`r.Tanggal` non-null; `a.CompletedAt ?? a.Schedule`
  dengan `Schedule` non-null), sehingga `.OrderByDescending(r => r.Date)` dan
  `row.Date.ToString(...)` aman tanpa null. Perbedaan annotation nullable pada `Title`
  (`r.Judul` bertipe `string?` vs `a.Title` bertipe `string`) TIDAK memecah kesetaraan
  anon-type karena keduanya `System.String` pada level type — `.Concat()` tetap kompilasi.

- **Razor auto-encoding / XSS (Title & Detail)** — AMAN. `@row.Title` (L358) dan
  `@row.Detail` (L359) memakai sintaks `@` yang auto-HTML-encode. Tidak ada `Html.Raw`
  pada konten yang berasal dari data (satu-satunya `Html.Raw` di L389/L404 hanya
  menyusun JSON `hx-vals` dari `row.Id` integer + antiforgery token internal, bukan
  input pengguna). `a.Title` di-encode walau berisi karakter HTML.

- **IDOR pada link "Lihat hasil"** — AMAN, server-gated. Endpoint
  `CMPController.Results(int id)` (L2218) memvalidasi: `NotFound` bila sesi tak ada
  (L2224), `Challenge()` bila belum login (L2228), dan `IsResultsAuthorized(...)` →
  `Forbid()` bila tidak berwenang (L2229-2230). View hanya menyembunyikan tautan secara
  kosmetik; otorisasi sebenarnya ada di server. Tidak ada kebocoran otorisasi.

- **Null-safety** — AMAN. Koleksi sumber dijaga `?? new List<...>()` (L295/297/300).
  `AssessmentSession.Status` non-null (default `""`), `CompletedAt`/`StartedAt`/`IsPassed`
  nullable dan dibandingkan eksplisit. `OnlineLabel` urut-cek benar: `PendingGrading`
  diperiksa pertama (essay punya `CompletedAt` terisi bersamaan), sesuai komentar.

- **Cabang HTMX delete (Training/Manual) terjaga verbatim** — TERKONFIRMASI. Diff
  menunjukkan blok `hx-post DeleteTraining` (L387-393) dan `hx-post DeleteManualAssessment`
  (L402-408) tidak berubah; hanya dibungkus ulang dalam `@if (row.IsOnline) {...} else if
  (row.Type == "Training") {...} else {...}`. Token antiforgery di body, `hx-confirm`,
  `hx-swap="none"`, dan trigger `recordDeleted` (L178-183) tetap utuh. Baris online tidak
  punya tombol edit/hapus (sesuai D-02 read-only).

## Info

### IN-01: `OnlineLabel(a)` dipanggil dua kali per baris online

**File:** `Views/Admin/Shared/_TrainingRecordsTab.cshtml:302`
**Issue:** Pada proyeksi `onlineRows`, label dihitung dua kali:
`Status = OnlineLabel(a), StatusClass = OnlineClass(OnlineLabel(a))`. Pemanggilan ganda
ini mengevaluasi rantai if-else `OnlineLabel` dua kali per baris. Tidak ada bug
(hasil deterministik & idempoten), hanya pekerjaan redundan dan sedikit menyulitkan
pembacaan.
**Fix:** Hitung sekali lalu pakai ulang, mis. dengan menambah local function gabungan
atau memproyeksikan ke variabel antara sebelum `.Select` final. Contoh ringkas:
```csharp
.Select(a => {
    var lbl = OnlineLabel(a);
    return new { /* ... */ Status = lbl, StatusClass = OnlineClass(lbl), /* ... */ };
})
```
Opsional; aman dibiarkan.

### IN-02: Label "Abandoned" tidak diterjemahkan, beda gaya dengan label lain

**File:** `Views/Admin/Shared/_TrainingRecordsTab.cshtml:271`
**Issue:** `OnlineLabel` mengembalikan literal Inggris `"Abandoned"` sementara label
lain berbahasa Indonesia ("Lulus", "Tidak Lulus", "Menunggu Penilaian",
"Sedang Dikerjakan", "Dibatalkan", "Belum Mulai"). Inkonsistensi bahasa pada UI badge.
**Fix:** Pertimbangkan menerjemahkan menjadi mis. "Ditinggalkan" agar konsisten dengan
badge lain. Hanya kosmetik; tidak memengaruhi logika derivasi maupun warna
(`OnlineClass` memetakan "Abandoned" -> `bg-dark`, sama dengan "Dibatalkan").

### IN-03: Konstanta `ManualStatusClass` mencakup status yang mungkin tak pernah muncul untuk online

**File:** `Views/Admin/Shared/_TrainingRecordsTab.cshtml:285-293`
**Issue:** `ManualStatusClass` dipakai untuk `trainingRows` dan `assessmentRows`.
Untuk `assessmentRows`, nilai `Status` hanya pernah "Passed"/"Failed"/PendingGrading
(L299), sehingga cabang "Valid"/"Expired" tak terjangkau dari jalur assessment manual —
cabang itu hanya relevan bagi training (`r.Status`). Bukan bug (default `bg-secondary`
menangani sisanya), hanya catatan bahwa helper sedikit lebih luas dari yang dipakai
masing-masing pemanggil.
**Fix:** Tidak perlu tindakan. Jika diinginkan kebersihan, dokumentasikan bahwa
"Valid"/"Expired" berasal dari sisi Training. Aman dibiarkan apa adanya.

---

_Reviewed: 2026-06-12_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
