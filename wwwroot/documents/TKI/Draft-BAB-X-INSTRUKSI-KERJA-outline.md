# DRAFT OUTLINE — BAB X & BAB XI (Revisi 2 — APPROVED)

**Dokumen:** Draft TKI Penggunaan HC PRIME untuk Pengelolaan & Pengembangan Kompetensi Pekerja
**Status:** APPROVED — siap masuk Fase 2 (screenshot + kompilasi .docx)
**Tanggal:** 24 April 2026

**Ringkasan keputusan:**
- A. Akses → 1 prosedur (Login). LDAP, Login lokal, Reset password, Edit profil dihapus.
- B. CMP → 7 prosedur. Budget Training dipindah ke modul HC.
- C. CDP → 5 prosedur. IDP = view only (disusun HC via PROTON Data). Konfirmasi Coaching dihapus. Deliverable = detail progress (Coach upload → Reviewer approve → HC review). Renewal dipindah ke modul HC.
- D. Modul HC → 10 prosedur (rename dari "Admin"; Admin = back-up teknis, tidak dibahas).
- E. Sistem & Monitoring → 4 prosedur **dipertahankan** dengan label 🏷️ "TIDAK DISARANKAN DIMASUKKAN KE TKI FINAL".

---

## Konvensi Penulisan

- **Gaya bahasa:** imperatif (perintah langsung).
- **Callout peran** (diletakkan di awal tiap langkah):
  - `[USER]` — seluruh pekerja (pembaca umum TKI).
  - `[COACHEE]` — pekerja yang sedang mengikuti program pengembangan.
  - `[COACH]` — pekerja senior yang mendampingi coachee.
  - `[ATASAN]` — Sr Supervisor, Section Head, Manager, VP, Direktur (lingkup hirarki).
  - `[HC]` — fungsi Human Capital.
  - **Kombinasi** (dipisah dengan `/`): `[ATASAN/HC]`, `[SEMUA]` = seluruh role yang punya akses ke halaman tersebut.
- **Nomor langkah:** `X.` → `A.` → `1.` → `a.` → `1)` → `a)`.
- **Path menu:** ditulis dengan `→` (misal: `CMP → Assessment`).
- **Placeholder screenshot:** `[SCREENSHOT: deskripsi]` (akan diisi file PNG hasil Playwright di Fase 2).

---

# X. INSTRUKSI KERJA

## A. AKSES

### 1. Login ke HC Prime
- a. `[USER]` Buka URL `http://10.55.3.3/KPB-PortalHC` melalui browser (Chrome/Edge/Firefox). `[SCREENSHOT: halaman login]`
- b. `[USER]` Masukkan email Pertamina (`nama.lengkap@pertamina.com`) dan password.
- c. `[USER]` Klik tombol **"Login"**.
- d. `[USER]` Bila kredensial valid, sistem mengarahkan ke halaman **Beranda** sesuai peran pengguna. `[SCREENSHOT: halaman beranda]`

---

## B. MODUL CMP — COMPETENCY MANAGEMENT

### 1. Melihat KKJ Pribadi (Kebutuhan Kompetensi Jabatan)
- a. `[USER]` Pada halaman Beranda, klik menu **CMP**. `[SCREENSHOT: dashboard CMP]`
- b. `[USER]` Klik kartu **"Dokumen KKJ"**. Sistem menampilkan daftar KKJ sesuai jabatan pengguna.
- c. `[USER]` Klik ikon unduh untuk mendapatkan dokumen matriks KKJ (PDF/Excel). `[SCREENSHOT: halaman Dokumen KKJ]`

### 2. Melihat Matriks KKJ Bagian (Atasan)
- a. `[ATASAN]` Buka menu **CMP → Dokumen KKJ**.
- b. `[ATASAN]` Pilih filter bagian (NGP / GAST / RFCC / DHT & HMU / Utilities II). Sistem menampilkan matriks kompetensi seluruh pekerja di bagian tersebut.
- c. `[ATASAN]` Klik salah satu baris untuk melihat detail gap kompetensi per pekerja.

### 3. Mengikuti Assessment Online
- a. `[USER]` Setelah mendapat notifikasi penugasan assessment, klik menu **CMP → Assessment**. `[SCREENSHOT: daftar assessment]`
- b. `[USER]` Sistem menampilkan daftar paket assessment yang ditugaskan dengan status **Open**.
- c. `[USER]` Klik tombol **"Mulai"** pada paket yang hendak dikerjakan.
- d. `[USER]` Baca instruksi dan ketentuan, lalu klik **"Setuju & Mulai"**. `[SCREENSHOT: modal instruksi]`
- e. `[USER]` Kerjakan soal satu per satu. Timer berjalan di pojok kanan atas. `[SCREENSHOT: halaman StartExam]`
- f. `[USER]` Tipe soal:
  - 1) Pilihan ganda (single answer)
  - 2) Multiple answer (centang beberapa jawaban)
  - 3) Essay (isi text area)
  - 4) Mix (kombinasi)
- g. `[USER]` Gunakan tombol **"Sebelumnya" / "Selanjutnya"** untuk navigasi antar soal.
- h. `[USER]` Bila seluruh soal telah dijawab, klik **"Submit"**. Sistem meminta konfirmasi.
- i. `[USER]` Sistem menyimpan jawaban, melakukan auto-grading untuk tipe non-essay, lalu menampilkan halaman **Ringkasan Ujian**. `[SCREENSHOT: ExamSummary]`
- j. `[USER]` Untuk soal Essay: skor **pending** sampai HC melakukan grading manual.

### 4. Melihat Rekam Jejak Assessment Pribadi (Records)
- a. `[USER]` Klik menu **CMP → Records**. `[SCREENSHOT: halaman Records]`
- b. `[USER]` Sistem menampilkan tabel seluruh riwayat assessment pribadi (kategori, nama paket, tanggal, skor, status).
- c. `[USER]` Klik ikon mata pada salah satu baris untuk melihat detail hasil per soal.
- d. `[USER]` Gunakan filter tanggal / kategori untuk mempersempit tampilan.

### 5. Melihat Rekam Jejak Tim (Atasan/HC)
- a. `[ATASAN/HC]` Klik menu **CMP → Records Team**. `[SCREENSHOT: Records Team]`
- b. `[ATASAN/HC]` Sistem menampilkan daftar bawahan sesuai hierarki organisasi.
- c. `[ATASAN/HC]` Klik salah satu pekerja untuk melihat detail rekam jejak assessment per individu.

### 6. Download Sertifikat Kompetensi
- a. `[USER]` Klik menu **CMP → Certificate**. `[SCREENSHOT: halaman Certificate]`
- b. `[USER]` Sistem menampilkan daftar sertifikat yang telah diterbitkan (lulus assessment, complete training).
- c. `[USER]` Klik ikon **"Download PDF"** untuk mengunduh sertifikat.
- d. `[USER]` Sertifikat yang mendekati expired ditandai badge kuning; yang sudah expired badge merah.

### 7. Analytics Dashboard (HC)
- a. `[HC]` Klik menu **CMP → Analytics Dashboard**. `[SCREENSHOT: Analytics Dashboard]`
- b. `[HC]` Sistem menampilkan visualisasi: heatmap gap kompetensi, progress assessment per bagian, coaching completion rate, training adoption, dan lainnya.
- c. `[HC]` Gunakan filter periode & bagian untuk analisis spesifik.
- d. `[HC]` Ekspor data ke Excel/PDF untuk laporan manajemen.

---

## C. MODUL CDP — CAREER DEVELOPMENT

### 1. Melihat Individual Development Plan (IDP)
> **Catatan:** Data IDP (track kompetensi, sub-kompetensi, deliverable) disusun dan di-upload oleh HC melalui menu **HC → PROTON Data**. Role Coach, Coachee, dan Atasan **hanya dapat melihat** IDP sesuai lingkup akses masing-masing.

- a. `[COACHEE]` Klik menu **CDP → Plan IDP**. `[SCREENSHOT: halaman PlanIdp]`
- b. `[COACHEE]` Sistem menampilkan IDP sesuai track yang di-assign oleh HC. Data yang ditampilkan: daftar kompetensi, sub-kompetensi, deliverable target, dan progress masing-masing.
- c. `[COACHEE]` Klik salah satu deliverable untuk melihat detail progress (akan mengarah ke halaman Deliverable, lihat C.3).
- d. `[COACH]` Pada peran Coach, halaman PlanIdp menampilkan IDP seluruh coachee yang dipetakan. Gunakan filter **Bagian** / **Unit** / **Track** untuk mempersempit tampilan.
- e. `[ATASAN]` Pada peran Atasan, halaman PlanIdp menampilkan IDP bawahan langsung sesuai struktur organisasi.

### 2. Sesi Coaching PROTON
- a. `[COACH]` Klik menu **CDP → Coaching PROTON**. `[SCREENSHOT: halaman CoachingProton]`
- b. `[COACH]` Sistem menampilkan daftar coachee beserta deliverable yang perlu di-coaching.
- c. `[COACH]` Pilih coachee & deliverable → klik **"Input Sesi Coaching"**.
- d. `[COACH]` Isi form PROTON 5 fase:
  - 1) **P**urpose (tujuan sesi)
  - 2) **R**ealita (kondisi saat ini)
  - 3) **O**ptions (alternatif tindakan)
  - 4) **T**o-Do (rencana aksi)
  - 5) **O**utcome & **N**ext step (hasil & follow-up)
- e. `[COACH]` Upload **bukti coaching** (foto kegiatan, dokumen kerja, hasil OJT) bila ada. `[SCREENSHOT: form upload bukti]`
- f. `[COACH]` Klik **"Simpan Sesi"**. Sistem mencatat sesi ke histori & memicu update progress deliverable.

### 3. Progress Deliverable — Lihat, Upload Evidence, Approval
> **Catatan alur:** Halaman Deliverable adalah **detail progress** per deliverable IDP. Evidence di-upload oleh **Coach** (bukan Coachee), lalu di-**approve/reject** oleh Reviewer (Sr Supervisor / Section Head / HC), dan final review oleh **HC**.

- a. `[SEMUA]` Dari halaman **Plan IDP**, klik nama deliverable → sistem membuka halaman **Deliverable**. `[SCREENSHOT: halaman Deliverable]`
- b. `[SEMUA]` Halaman menampilkan: nama deliverable, target, status progress (**Pending / Approved / Rejected**), histori sesi coaching terkait, histori status, dan tombol aksi sesuai peran.
- c. `[COACH]` Klik **"Upload Evidence"** → pilih file (PDF/JPG/PNG, max 10 MB) → klik **"Simpan"**. `[SCREENSHOT: modal upload evidence]`
- d. `[COACH]` Alternatif: **"Submit Evidence with Coaching"** — upload evidence sekaligus input sesi coaching dalam satu form.
- e. `[ATASAN (Sr Spv / Section Head) / HC]` Pada deliverable yang sudah berisi evidence, klik **"Approve"** bila sesuai, atau **"Reject"** dengan mengisi alasan. `[SCREENSHOT: tombol Approve/Reject]`
- f. `[HC]` Lakukan **HC Review** sebagai approval final setelah reviewer memberikan approval tingkat-bagian.
- g. `[SEMUA]` Histori seluruh aksi (upload, approve, reject, review) tercatat di bagian **Status History** halaman Deliverable.

### 4. Melihat Histori Coaching PROTON
- a. `[USER]` Klik menu **CDP → Histori PROTON**. `[SCREENSHOT: HistoriProton]`
- b. `[USER]` Sistem menampilkan timeline seluruh sesi coaching yang pernah diikuti (sebagai coach maupun coachee).
- c. `[USER]` Klik salah satu entri untuk melihat detail sesi (HistoriProtonDetail). `[SCREENSHOT: HistoriProtonDetail]`
- d. `[USER]` Detail berisi isi PROTON 5 fase, bukti upload, timestamp, dan status.

### 5. Melihat Daftar Sertifikasi Pribadi (Certification Management)
- a. `[USER]` Klik menu **CDP → Certification Management**. `[SCREENSHOT: CertificationManagement]`
- b. `[USER]` Sistem menampilkan daftar sertifikasi pribadi (mandatory, licensor/vendor, dan lainnya) beserta tanggal terbit, tanggal expired, dan status.
- c. `[USER]` Sertifikat yang mendekati/sudah expired ditandai dengan badge warna.
- d. `[USER]` Klik salah satu baris untuk melihat detail sertifikat & unduh PDF.

---

## D. MODUL HC — PENGELOLAAN DATA & PENGEMBANGAN KOMPETENSI

> **Catatan peran:** Seluruh fitur di modul ini secara teknis dapat diakses oleh role Admin dan HC. Namun secara operasional, pengelolaan dilakukan oleh fungsi **Human Capital (HC)**; peran Admin hanya untuk back-up teknis dan tidak dibahas pada TKI ini.

### 1. Kelola Data Pekerja

#### 1.1 Tambah Pekerja Manual
- a. `[HC]` Klik menu **Kelola Pekerja**. `[SCREENSHOT: ManageWorkers]`
- b. `[HC]` Klik **"Tambah Pekerja"**. `[SCREENSHOT: CreateWorker]`
- c. `[HC]` Isi form: NIP, nama, email, jabatan, bagian/seksi, tanggal masuk, tipe (operator/panelman/supervisor).
- d. `[HC]` Pilih **role** pekerja: HC / Manager / Section Head / Sr Supervisor / Coach / Coachee.
- e. `[HC]` Klik **"Simpan"**.

#### 1.2 Import Pekerja Massal via Excel
- a. `[HC]` Pada halaman Kelola Pekerja, klik **"Import Excel"**.
- b. `[HC]` Download template Excel (lihat Lampiran II).
- c. `[HC]` Isi template sesuai kolom yang disediakan; jangan ubah header.
- d. `[HC]` Upload file Excel → klik **"Preview"**. `[SCREENSHOT: ImportWorkers]`
- e. `[HC]` Review data, cek warning validasi (duplikat NIP, email invalid).
- f. `[HC]` Klik **"Konfirmasi Import"**. Sistem melakukan bulk insert.

#### 1.3 Edit & Detail Pekerja
- a. `[HC]` Pada list pekerja, klik ikon **Edit** / **Detail**. `[SCREENSHOT: EditWorker, WorkerDetail]`
- b. `[HC]` Edit data → klik Simpan. Perubahan role akan me-refresh akses pengguna.

### 2. Struktur Organisasi
- a. `[HC]` Klik menu **Struktur Organisasi**. `[SCREENSHOT: ManageOrganization]`
- b. `[HC]` Sistem menampilkan pohon organisasi (Direktorat → Fungsi → Seksi).
- c. `[HC]` Klik node untuk edit nama, menambah child, atau menghapus.

### 3. Pemetaan Coach ↔ Coachee
- a. `[HC]` Klik menu **Coach-Coachee Mapping**. `[SCREENSHOT: CoachCoacheeMapping]`
- b. `[HC]` Pilih coachee → pilih satu atau lebih coach → klik **"Simpan Mapping"**.
- c. `[HC]` Untuk memonitor beban coach, klik **"Workload Coach"**. `[SCREENSHOT: CoachWorkload]`
- d. `[HC]` Sistem menampilkan jumlah coachee per coach & warning bila melampaui threshold.

### 4. Kelola Kategori & Paket Soal Assessment

#### 4.1 Kelola Kategori
- a. `[HC]` Klik menu **Kelola Kategori**. `[SCREENSHOT: ManageCategories]`
- b. `[HC]` Tambah/edit/hapus kategori (misal: Kompetensi Teknis Operasi, HSSE, Leadership).

#### 4.2 Kelola Paket Assessment
- a. `[HC]` Klik menu **Kelola Paket**. `[SCREENSHOT: ManagePackages]`
- b. `[HC]` Klik **"Buat Paket Baru"** → isi: nama paket, kategori, durasi, passing score, tipe (Online/Manual/PreTest/PostTest), tanggal buka/tutup.
- c. `[HC]` Klik **"Simpan"**.

#### 4.3 Import Soal via Excel
- a. `[HC]` Masuk ke detail paket → klik **"Import Soal Excel"**. `[SCREENSHOT: ImportPackageQuestions]`
- b. `[HC]` Download template sesuai tipe: Essay / Multiple Answer / Mix (lihat Lampiran I).
- c. `[HC]` Isi template, upload, preview, konfirmasi.
- d. `[HC]` Klik **"Preview Paket"** untuk melihat preview akhir sebelum di-assign. `[SCREENSHOT: PreviewPackage]`

#### 4.4 Penugasan Peserta (Assign Assessment)
- a. `[HC]` Pada detail paket, klik **"Assign Peserta"**.
- b. `[HC]` Pilih peserta via filter (bagian, jabatan) atau multi-select manual.
- c. `[HC]` Klik **"Assign"**. Peserta menerima notifikasi di HC Prime.

### 5. Monitoring Assessment Realtime
- a. `[HC]` Klik menu **Assessment Monitoring**. `[SCREENSHOT: AssessmentMonitoring]`
- b. `[HC]` Sistem menampilkan peserta yang sedang aktif mengerjakan (real-time via SignalR).
- c. `[HC]` Klik salah satu peserta untuk melihat progress: soal ke-n, sisa waktu. `[SCREENSHOT: AssessmentMonitoringDetail]`
- d. `[HC]` Input Manual Assessment (misal: hasil interview): menu **Add Manual Assessment**. `[SCREENSHOT: AddManualAssessment]`

### 6. Kelola Data Training Korporat
- a. `[HC]` Klik menu **Kelola Training**. `[SCREENSHOT: ManageTraining]`
- b. `[HC]` Tambah manual via **"Add Training"** atau import Excel via **"Import Training"**. `[SCREENSHOT: AddTraining, ImportTraining]`
- c. `[HC]` Edit record training peserta → **EditTraining**. Upload sertifikat pendukung.

### 7. Upload Dokumen KKJ & CPDP
- a. `[HC]` Klik menu **Kelola Dokumen KKJ**. `[SCREENSHOT: KkjUpload, KkjFiles]`
- b. `[HC]` Pilih bagian → upload file KKJ (PDF/Excel, max 10 MB).
- c. `[HC]` Sistem menyimpan dengan format `yyyyMMddHHmmssfff_{GUID}_{namafile}`.
- d. `[HC]` Untuk CPDP: menu **Kelola Dokumen CPDP** dengan alur serupa.
- e. `[HC]` Lihat **history versi** via **KkjFileHistory** / **CpdpFileHistory**. `[SCREENSHOT: history]`
- f. `[HC]` Buka **KKJ Matrix** untuk melihat matriks kompetensi jabatan per bagian. `[SCREENSHOT: KkjMatrix]`

### 8. PROTON Data — Setup IDP (Track, Kompetensi, Deliverable)
> **Catatan:** Menu ini adalah **sumber utama data IDP** yang tampil di halaman CDP → Plan IDP untuk coachee. Perubahan di PROTON Data langsung ter-refleksi di IDP coachee.

- a. `[HC]` Klik menu **PROTON Data**. `[SCREENSHOT: ProtonData]`
- b. `[HC]` Klik **"Import Silabus Excel"**.
- c. `[HC]` Download template → isi dengan struktur: **Track → Kompetensi → Sub-Kompetensi → Deliverable**.
- d. `[HC]` Upload → preview → konfirmasi.
- e. `[HC]` Untuk revisi entri tunggal, gunakan **"Override Manual"**.
- f. `[HC]` Setelah data terupload, lakukan **Track Assignment** — memetakan track PROTON ke coachee tertentu.

### 9. Budget Training (Perencanaan Anggaran)
> **Catatan:** Menu Budget Training secara teknis berada di URL `/CMP/BudgetTraining`, namun aksesnya **terbatas pada role HC/Admin** sehingga masuk dalam modul pengelolaan HC.

- a. `[HC]` Klik menu **CMP → Budget Training**. `[SCREENSHOT: BudgetTraining]`
- b. `[HC]` Sistem menampilkan daftar anggaran training tahun berjalan beserta realisasi.
- c. `[HC]` Klik **"Tambah Budget"** → isi: nama training, vendor, peserta target, estimasi biaya, tahun anggaran.
- d. `[HC]` Alternatif: **"Import Budget Excel"** untuk input massal. Download template via **"Download Template"**.
- e. `[HC]` Update realisasi biaya via **Quick Update** per baris, atau **Bulk Update Realisasi** via modal.
- f. `[HC]` Ekspor laporan anggaran via **"Export Excel"** untuk pelaporan manajemen.

### 10. Renewal Sertifikat
- a. `[HC]` Klik menu **Renewal Certificate**. `[SCREENSHOT: RenewalCertificate]`
- b. `[HC]` Sistem menampilkan seluruh sertifikat yang akan/sudah expired beserta info pekerja dan jenis sertifikat.
- c. `[HC]` Gunakan filter (bagian, tipe, status expired) untuk mempersempit tampilan.
- d. `[HC]` Tandai sertifikat sebagai **"Renewal Processed"** setelah pekerja mengikuti training renewal.
- e. `[HC]` Ekspor daftar renewal via **"Export Excel"** untuk perencanaan training tahunan.

---

## E. SISTEM & MONITORING

> 🏷️ **LABEL: TIDAK DISARANKAN DIMASUKKAN KE TKI FINAL**
>
> Bagian ini dipertahankan dalam outline sebagai **referensi lengkap fungsionalitas website**, namun seluruh isinya bersifat **operasional teknis** (audit log, maintenance mode, impersonation) yang merupakan ranah **tim IT/Admin sistem**, bukan prosedur kerja pengelolaan kompetensi oleh HC.
>
> **Rekomendasi:** Saat kompilasi TKI Final (.docx), bagian E ini sebaiknya **dipindahkan ke dokumen SOP IT terpisah** atau **dihapus dari TKI**. Isi ditulis lengkap di bawah agar dapat dinilai utuh terlebih dahulu.

### 1. Notifikasi
- a. `[USER]` Pada navbar, ikon lonceng menampilkan jumlah notifikasi unread. `[SCREENSHOT: notification dropdown]`
- b. `[USER]` Klik ikon → dropdown daftar notifikasi terbaru (assessment assigned, coaching baru, deliverable review, sertifikat expired).
- c. `[USER]` Klik salah satu item → sistem mengarah ke halaman terkait & menandai sebagai read.
- d. `[USER]` Klik **"Lihat Semua"** untuk halaman penuh daftar notifikasi.

### 2. Audit Log
- a. `[HC]` Klik menu **Audit Log**. `[SCREENSHOT: AuditLog]`
- b. `[HC]` Sistem menampilkan log seluruh aksi pengguna (login, CRUD, impersonation).
- c. `[HC]` Gunakan filter: tanggal, user, controller/action, status.
- d. `[HC]` Ekspor log ke Excel untuk keperluan audit eksternal.

### 3. Maintenance Mode
- a. `[HC]` Klik menu **Maintenance Mode**. `[SCREENSHOT: Maintenance]`
- b. `[HC]` Aktifkan toggle **"Maintenance ON"** sebelum melakukan patching/migration sistem.
- c. `[HC]` Isi pesan banner (estimasi downtime). Seluruh non-HC/Admin akan diarahkan ke halaman maintenance.
- d. `[HC]` Setelah selesai, toggle **"Maintenance OFF"** untuk mengembalikan akses normal.

### 4. Impersonation (Troubleshooting)
- a. `[HC]` Klik menu **Profil → Impersonate User** (hanya terlihat role HC/Admin). `[SCREENSHOT: Impersonate]`
- b. `[HC]` Pilih user yang akan di-impersonate → klik **"Impersonate"**.
- c. `[HC]` Browser session akan beroperasi sebagai user tersebut (banner merah tampil di top). `[SCREENSHOT: impersonation banner]`
- d. `[HC]` Klik **"Stop Impersonation"** di banner untuk kembali ke sesi HC. **Wajib stop impersonation setelah selesai troubleshoot.**

---

# XI. INDIKATOR & UKURAN KEBERHASILAN

## A. Adopsi Platform
- 1. ≥ 95% pekerja CSU Process (NGP, GAST, RFCC, DHT & HMU, Utilities II) login aktif minimal 1× per bulan.
- 2. 100% pekerja ISBL & OSBL memiliki akun HC Prime terdaftar dan teraktivasi.

## B. Assessment
- 1. ≥ 90% pekerja menyelesaikan assessment KKJ wajib tahunan sesuai jadwal.
- 2. Rata-rata skor assessment ≥ 75% (passing score).
- 3. ≤ 5% peserta mengalami disconnect/drop selama sesi assessment.
- 4. 100% soal Essay ter-grading manual oleh HC ≤ 14 hari setelah submit.

## C. IDP & Coaching PROTON
- 1. 100% pekerja Production/Operations Team memiliki track IDP aktif di platform.
- 2. ≥ 80% deliverable IDP per coachee tercatat progress-nya (upload evidence oleh Coach).
- 3. ≥ 80% sesi coaching PROTON terdokumentasi di platform.
- 4. Rata-rata ≥ 1 sesi coaching per coachee per bulan.
- 5. Gap closure kompetensi teknis ≥ 20% per semester (perbandingan skor assessment sebelum–sesudah).
- 6. Beban coach ≤ 10 coachee per coach.

## D. Sertifikasi & Training
- 1. 0 sertifikat expired tanpa rencana renewal terdaftar di menu Renewal Certificate.
- 2. ≥ 80% training mandatory (Safety for Refinery, Gas Tester, SUPREME, PSAIMS, ERP, Confined Space) ter-complete per pekerja sesuai periode.
- 3. ≥ 90% sertifikat training ter-upload ke platform.

## E. Kualitas Data & Governance
- 1. Matriks KKJ terupdate setiap perubahan organisasi dalam ≤ 30 hari.
- 2. Seluruh dokumen KKJ/CPDP per bagian memiliki history versi yang ter-track di platform.
- 3. ≤ 1% kasus data mismatch antara HC Prime dengan sistem HC Core.

---

# XIV. LAMPIRAN (Usulan)

- **Lampiran I** — Template Excel Import Soal (Essay / Multiple Answer / Mix)
- **Lampiran II** — Template Excel Import Worker
- **Lampiran III** — Template Excel Import Training
- **Lampiran IV** — Template Excel Import Silabus PROTON
- **Lampiran V** — Template Excel Import Budget Training
- **Lampiran VI** — Flowchart Alur Assessment Online
- **Lampiran VII** — Flowchart Alur Coaching PROTON (5 fase) & Deliverable Approval
- **Lampiran VIII** — Matriks Akses per Role (HC / Atasan / Coach / Coachee / User)
- **Lampiran IX** — Daftar URL & Environment

---

## ✅ Status Review

Seluruh butir review telah **di-approve** pada 24 April 2026:

1. ✅ A. Akses → 1 prosedur (Login) saja.
2. ✅ C.1 Plan IDP → "Melihat IDP"; alur penyusunan dipindah ke D.8 PROTON Data.
3. ✅ C.3 Deliverable → halaman detail progress dengan alur Coach upload → Reviewer approve → HC review.
4. ✅ D Modul Admin → rename **HC** dengan catatan Admin = back-up teknis.
5. ✅ E Sistem & Monitoring → **seluruh isi dipertahankan** dengan label 🏷️ "TIDAK DISARANKAN DIMASUKKAN KE TKI FINAL".
6. ✅ Lampiran → ditambah Template PROTON & Budget Training; Lampiran Deliverable dihilangkan (sudah inline).

**Next Step:** Fase 2 — ubah `UseActiveDirectory: false`, batch screenshot via Playwright, kompilasi ke `.docx` + `.html` preview.
