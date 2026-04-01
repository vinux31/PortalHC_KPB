# Phase 276 Plan

## 1. Overview
Phase 276 menambahkan enhancement pada halaman `StartExam` agar panel navigasi soal menampilkan seluruh nomor soal `1..N`, bukan hanya nomor soal pada halaman aktif. Setiap nomor soal harus bisa diklik untuk loncat langsung ke soal target, termasuk jika soal tersebut berada di halaman lain. Perubahan dibatasi pada layer UI/client-side di `Views/CMP/StartExam.cshtml`.

## 2. Goals
- Menampilkan semua nomor soal dalam satu panel navigasi.
- Menggunakan grid 10 kolom agar urutan `1-10`, `11-20`, dan seterusnya konsisten dengan 10 soal per halaman.
- Memberi status visual sederhana:
  - hijau untuk answered
  - abu-abu untuk unanswered
- Memungkinkan klik nomor soal untuk:
  - scroll ke soal di halaman yang sama
  - pindah halaman lalu scroll ke soal di halaman lain
- Menjaga flow existing tetap aman:
  - pagination biasa
  - autosave
  - progress counter
  - review/submit
  - resume modal
- Menjaga panel tetap usable di mobile dengan mode collapsed dan toggle existing.

## 3. Files to Modify/Create
- Modify: `Views/CMP/StartExam.cshtml`
- Create: `.planning/phases/276-navigasi-soal-di-startexam-tampilkan-seluruh-nomor-ujian-dengan-fitur-klik-langsung-ke-lokasi-soal/PLAN.md`

## 4. Step-by-Step Implementation dengan Checklist

### Step 1: Update panel header dan struktur container
- [ ] Ubah teks header panel dari `Questions this page` menjadi `Daftar Soal`.
- [ ] Ubah container `#panelNumbers` agar siap dipakai sebagai grid seluruh nomor soal.
- [ ] Pastikan perubahan markup hanya terbatas pada area panel kanan.

### Step 2: Siapkan data seluruh nomor soal di client-side
- [ ] Bentuk struktur data flat dari `pageQuestionIds`.
- [ ] Simpan `questionId`, `pageNumber`, dan `displayNumber` berdasarkan urutan tampil existing.
- [ ] Pastikan urutan nomor mengikuti urutan soal di halaman, bukan sorting `questionId`.

### Step 3: Refactor render panel
- [ ] Ubah `updatePanel()` agar merender semua nomor soal `1..N`.
- [ ] Tetapkan warna badge berdasarkan `answeredQuestions`.
- [ ] Jaga `updatePanel()` tetap aman dipanggil berulang.
- [ ] Jangan ubah logic `updateAnsweredCount()` selain memanfaatkan `updatePanel()` yang baru.

### Step 4: Tambahkan navigasi klik ke soal
- [ ] Tambahkan helper untuk scroll ke `#qcard_{questionId}`.
- [ ] Tambahkan handler `jumpToQuestion(questionId, targetPage)`.
- [ ] Jika target page sama, scroll langsung ke soal.
- [ ] Jika target page berbeda, pindah halaman dulu lalu scroll ke soal target.
- [ ] Gunakan scroll instant, bukan smooth.

### Step 5: Integrasikan dengan flow page switching existing
- [ ] Hubungkan `jumpToQuestion()` dengan `changePage()` / `performPageSwitch()` secara minimal.
- [ ] Pastikan page switch biasa tetap scroll ke atas seperti sebelumnya.
- [ ] Pastikan jump dari panel hanya override scroll saat memang dipicu dari badge panel.
- [ ] Pastikan resume flow tetap memanggil render panel dengan benar setelah modal resume dikonfirmasi.

### Step 6: Tambahkan behavior panel desktop/mobile
- [ ] Tambahkan CSS grid 10 kolom untuk `#panelNumbers`.
- [ ] Tambahkan styling badge interaktif secukupnya.
- [ ] Di desktop, panel tetap visible.
- [ ] Di mobile, panel default hidden pada fresh load.
- [ ] Toggle existing tetap dipakai untuk buka/tutup panel.
- [ ] Tambahkan `max-height` dan `overflow-y` agar panel mobile tetap usable.

### Step 7: Tambahkan auto-scroll panel
- [ ] Saat halaman berubah, auto-scroll panel agar badge pertama page aktif tetap terlihat.
- [ ] Jangan tambahkan highlight khusus untuk current question.
- [ ] Pastikan auto-scroll panel tidak mengganggu scroll utama halaman soal.

### Step 8: Verifikasi dan regression check
- [x] Jalankan build untuk memastikan tidak ada compile error.
- [x] Lakukan browser UAT pada exam dengan lebih dari 10 soal.
- [x] Verifikasi skenario halaman sama, lintas halaman, resume flow, dan mobile.
- [x] Catat hasil UAT sebelum dianggap selesai.

## 5. Acceptance Criteria per Step

### Step 1 Acceptance Criteria
- Header panel tampil sebagai `Daftar Soal`.
- Struktur HTML panel tetap valid dan tidak merusak layout existing.

### Step 2 Acceptance Criteria
- Data client-side merepresentasikan seluruh soal secara lengkap.
- Nomor soal yang dirender cocok dengan urutan soal yang terlihat di content area.

### Step 3 Acceptance Criteria
- Panel menampilkan seluruh nomor soal, bukan hanya nomor pada page aktif.
- Badge answered/unanswered selalu sinkron dengan jawaban yang tersimpan di page.

### Step 4 Acceptance Criteria
- Klik nomor pada page yang sama membawa user ke soal yang benar.
- Klik nomor pada page berbeda memindahkan page lalu menampilkan soal target.

### Step 5 Acceptance Criteria
- Pagination existing tetap bekerja normal.
- Resume flow tetap aman dan panel tampil benar setelah resume.
- Tidak ada regresi pada autosave, progress counter, dan submit flow.

### Step 6 Acceptance Criteria
- Desktop: panel selalu terlihat.
- Mobile fresh load: panel hidden.
- Mobile setelah toggle: panel muncul dan bisa discroll.

### Step 7 Acceptance Criteria
- Saat user pindah page, panel menyesuaikan posisi sehingga nomor page aktif tetap terlihat.
- Tidak ada current-question marker baru di UI.

### Step 8 Acceptance Criteria
- `dotnet build` sukses tanpa error baru.
- UAT browser lulus untuk:
  - render semua nomor soal
  - klik badge halaman sama
  - klik badge lintas halaman
  - mobile collapse/toggle
  - resume flow

## 6. Dependencies
- `Views/CMP/StartExam.cshtml` sebagai file implementasi utama.
- Data client-side existing:
  - `pageQuestionIds`
  - `answeredQuestions`
  - `currentPage`
- Fungsi existing:
  - `changePage()`
  - `performPageSwitch()`
  - `updateAnsweredCount()`
- Struktur DOM existing:
  - `#panelNumbers`
  - `#questionPanel`
  - `#questionPanelWrapper`
  - `#qcard_{questionId}`
- Browser UAT terhadap app lokal yang berjalan di `http://localhost:5277`

## 7. Verification Results

### Build Verification
**Status**: ✅ PASS
- `dotnet build` sukses tanpa compilation error
- Hanya warnings yang sudah ada sebelumnya (nullability warnings, obsolete API warnings)
- Commit: `27dcd669` (fix grid alignment) → `3b58c3e3` (merge to main)

### UAT Browser Testing
**Status**: ✅ 8/8 PASS
**Tanggal**: 2026-04-01
**Environment**: Local development server (http://localhost:5277)

#### Test Cases:

1. **Panel Display & Layout** ✅ PASS
   - Panel menampilkan SELURUH nomor soal (1-N)
   - Grid 7 kolom ter-align rapi
   - Badge hijau (answered), abu-abu (unanswered)

2. **Navigation - Same Page** ✅ PASS
   - Klik nomor di halaman sama → scroll langsung ke soal
   - Position scroll pas (tidak tertutup header, offset 80px)

3. **Navigation - Cross Page** ✅ PASS
   - Klik nomor di halaman berbeda → pindah halaman + scroll ke soal
   - Page switch dan scroll coordination bekerja smooth

4. **Auto-scroll Panel** ✅ PASS
   - Saat page berubah, panel auto-scroll menunjukkan badge pertama page aktif
   - Tidak mengganggu scroll utama halaman soal

5. **Mobile - Fresh Load** ✅ PASS
   - Panel ter-hidden pada fresh load di mobile
   - Toggle button berfungsi buka/tutup panel

6. **Mobile - Panel Scroll** ✅ PASS
   - Panel bisa di-scroll jika konten banyak
   - max-height 300px dengan overflow-y

7. **Existing Features** ✅ PASS
   - Pagination (Previous/Next) tetap jalan normal
   - Autosave tetap bekerja
   - Progress counter update accurate
   - Resume flow tetap aman

8. **Grid Alignment Fix** ✅ PASS
   - Fixed width (36px) × height (32px) dengan flex centering
   - justify-items: center pada grid container
   - Perfect alignment dalam 7-column grid

### Regression Check
**Status**: ✅ NO REGRESSION
- Semua existing features berfungsi normal
- Tidak ada bug baru ditemukan
- Autosave, pagination, timer, resume semua bekerja

### Deployment
- ✅ Merged to main branch (commit: 3b58c3e3)
- ✅ Pushed to origin/main
- ✅ ROADMAP.md updated
- ✅ Ready untuk production deployment berikutnya

### Notes
- Grid column diubah dari 10 ke 7 sesuai user request
- Scroll position diperbaiki dengan 80px offset untuk menghindari header overlap
- Fixed dimensions untuk memastikan alignment konsisten
