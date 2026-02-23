# ✅ Testing Checklist — PortalHC_KPB

**Diisi oleh:** Dev 2 (Tester)
**Update terakhir:** 23 Feb 2026

**Cara Pakai:**
- `[ ]` = Belum dicek
- `[/]` = Sedang dicek
- `[x]` = Sudah dicek, OK
- `[!]` = Ada bug → catat di `BUG_REPORTS.md`

---

## 👤 A. Testing per Role

Login sebagai **setiap role** dan verifikasi tampilan serta akses sudah benar.

### Role 1: Admin
- [ ] Bisa login
- [ ] Semua menu navbar muncul (CMP, CDP)
- [ ] **View Switcher** muncul dan bisa ganti perspektif ke semua role
- [ ] Bisa akses halaman Manage Workers
- [ ] Bisa akses semua halaman CMP dan CDP

### Role 2: HC (Human Capital)
- [ ] Bisa login
- [ ] "Manage Assessments" muncul di CMP
- [ ] Tab Monitoring di Assessment berfungsi
- [ ] HC Approvals di CDP muncul dan berfungsi
- [ ] Dashboard Analytics muncul
- [ ] Bisa lihat data semua karyawan

### Role 3: Manager / VP / Direktur
- [ ] Bisa login
- [ ] Bisa lihat data karyawan di section-nya
- [ ] Tidak bisa akses fitur admin/HC
- [ ] Dashboard menampilkan data yang sesuai scope

### Role 4: Section Head / Sr. Supervisor
- [ ] Bisa login
- [ ] Akses terbatas pada section-nya saja
- [ ] Bisa lihat Records karyawan di bawahnya

### Role 5: Coach
- [ ] Bisa login
- [ ] Bisa lihat daftar coachee-nya di CDP
- [ ] Bisa buat Coaching Session
- [ ] Bisa buat Action Items
- [ ] Bisa approve/reject Deliverable coachee
- [ ] Tidak bisa lihat data karyawan lain

### Role 6: Coachee
- [ ] Bisa login
- [ ] Hanya bisa lihat data pribadi
- [ ] Bisa mengerjakan Assessment (jika di-assign)
- [ ] Bisa submit Deliverable di Proton
- [ ] Tidak ada tombol admin/manage yang muncul

---

## 🧭 B. Modul Home (Dashboard Utama)

- [ ] Hero section tampil (greeting + nama + posisi + tanggal)
- [ ] Card "My IDP Status" tampil dengan progress bar SVG
- [ ] Card "Pending Assessment" menampilkan jumlah yang benar
- [ ] Card "Mandatory Training" menampilkan status valid/expired
- [ ] Quick Access links (My IDP, Assessment, Library KKJ) bisa diklik
- [ ] Recent Activity menampilkan data dari database (bukan dummy)
- [ ] Upcoming Deadlines tampil dengan countdown hari

---

## 📋 C. Modul CMP (Competency Management Portal)

### C1. Halaman Index CMP
- [ ] Semua menu card muncul (KKJ, CPDP, Assessment, Records)
- [ ] HC/Admin: card "Manage Assessments" muncul
- [ ] Klik setiap card → berhasil masuk ke halaman yang benar

### C2. KKJ Matrix
- [ ] Matrix kompetensi tampil dengan benar
- [ ] Bisa pilih section → data matrix berubah

### C3. CPDP Mapping
- [ ] Tabel mapping KKJ ↔ program pelatihan tampil

### C4. Assessment — Buat & Jalankan Ujian
- [ ] HC/Admin bisa buat Assessment baru
- [ ] Bisa assign ke multiple user
- [ ] Token security: user tidak bisa akses tanpa token (jika aktif)
- [ ] Exam engine: timer berjalan mundur
- [ ] Bisa navigasi antar soal
- [ ] Auto-submit saat waktu habis
- [ ] Exam Summary muncul sebelum submit final
- [ ] Hasil/score tampil setelah submit
- [ ] Certificate bisa dilihat dan di-print

### C5. Manage Packages & Questions
- [ ] Bisa buat package baru
- [ ] Bisa tambah pertanyaan manual
- [ ] Bisa import pertanyaan dari Excel
- [ ] Preview package berfungsi
- [ ] Bisa hapus package/pertanyaan

### C6. Records & Worker Detail
- [ ] Tabel training records tampil
- [ ] Export ke Excel berfungsi
- [ ] Import worker dari Excel berfungsi
- [ ] Klik nama worker → Worker Detail terbuka
- [ ] Worker Detail menampilkan data assessment + training history

---

## 🚀 D. Modul CDP (Career Development Portal)

### D1. Halaman Index CDP
- [ ] Semua menu card muncul (Plan IDP, Coaching, Progress, Dashboard, Proton)

### D2. Plan IDP
- [ ] PDF viewer tampil (dokumen silabus terbuka di halaman)

### D3. Coaching
- [ ] Coach bisa buat session coaching baru
- [ ] Action items bisa ditambahkan
- [ ] History coaching log tampil dengan benar

### D4. Progress IDP
- [ ] Status IDP items tampil per coachee
- [ ] Persentase progress tampil

### D5. Proton (IDP Track System)
- [ ] HC bisa assign track ke coachee
- [ ] Coachee bisa lihat deliverable-nya
- [ ] Coachee bisa upload evidence
- [ ] Status approval berubah: Coach → Supervisor → HC
- [ ] Notifikasi muncul saat ada perubahan status

### D6. Dashboard CDP
- [ ] Charts tampil dengan benar (Chart.js)
- [ ] HC/Admin: assessment analytics muncul
- [ ] Supervisor: ProtonProgress sesuai scope-nya

### D7. HC Approvals
- [ ] HC bisa lihat semua pending approvals
- [ ] Bisa approve / reject dengan komentar

---

## 🎨 E. UI/UX & Umum

### Tampilan
- [ ] Tidak ada teks terpotong (overflow) di semua halaman
- [ ] Responsive di layar laptop (1366px)
- [ ] Font Inter Google Fonts tampil (bukan default serif/sans-serif)
- [ ] Animasi AOS (fade in saat scroll) berfungsi
- [ ] Glassmorphism cards tampil di Home

### Navigasi & Interaksi
- [ ] Semua link di navbar berfungsi
- [ ] Tidak ada tombol "dead" (tidak bereaksi saat diklik)
- [ ] Alert/notifikasi sukses/error muncul setelah aksi
- [ ] Back button browser tidak menyebabkan error
- [ ] Logout berfungsi dan redirect ke halaman login

### Form & Validasi
- [ ] Submit form kosong → muncul pesan error validasi
- [ ] Format email salah → muncul pesan error
- [ ] Input angka di field teks → ditangani dengan benar

### Performance
- [ ] Halaman load dalam waktu wajar (< 5 detik)
- [ ] Tidak ada halaman yang hang/loading selamanya

---

## 📝 Catatan Testing

*(Tulis catatan umum di sini — hal yang perlu dikomunikasikan ke Dev 1 tapi bukan bug formal)*

```
[Tanggal] - [Catatan]
...
```

---

*File ini dikelola oleh Dev 2. Update checklist setiap sesi testing!*
*Bug yang ditemukan → catat di `BUG_REPORTS.md`*
