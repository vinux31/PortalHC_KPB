# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v5.0** - Phases 1-172 (shipped)
- ✅ **v7.1–v7.12** - Phases 176-222 (shipped)
- ✅ **v8.0–v8.7** - Phases 223-253 (shipped)
- ⏸️ **v9.0 Pre-deployment Audit & Finalization** - Phases 254-256 (deferred)
- ✅ **v9.1 UAT Coaching Proton End-to-End** - Phases 257-261 (shipped 2026-03-25, partial)
- ✅ **Phases 262-263** - Sub-path deployment fixes (shipped 2026-03-27)
- ✅ **v10.0 UAT Assessment OJT di Server Development** - Phases 264-280 (shipped)
- 🚧 **v11.2 Admin Platform Enhancement** - Phases 281-284 (in progress)

## Phases

<details>
<summary>✅ Previous milestones (v1.0–v9.1, Phases 1-263) — SHIPPED</summary>

See .planning/MILESTONES.md for full history.

</details>

<details>
<summary>✅ v10.0 UAT Assessment OJT di Server Development (Phases 264-280) — SHIPPED</summary>

- [x] **Phase 264: Admin Setup Assessment OJT** - Admin buat assessment, upload soal, assign worker
- [x] **Phase 265: Worker Exam Flow** - Worker mulai ujian, jawab soal, navigasi halaman
- [x] **Phase 266: Review, Submit & Hasil** - Review jawaban, submit, grading, sertifikat
- [x] **Phase 267: Resilience & Edge Cases** - Offline, resume, refresh, timeout behavior
- [x] **Phase 268: Monitoring Dashboard** - Admin/HC pantau progress real-time
- [x] **Phase 269: Loading overlay SignalR** - Loading overlay saat koneksi belum ready
- [x] **Phase 270: Perbaiki resume exam** - Sederhanakan modal resume + redirect ke page 0
- [x] **Phase 271: Fix timer ujian** - Server-authoritative timer dengan wall-clock cross-check
- [x] **Phase 272: Block submit jika belum semua soal terisi** - Frontend disable + backend guard
- [x] **Phase 274: Hilangkan score di sertifikat** - Remove skor dari sertifikat
- [x] **Phase 275: Warning create assessment** - Pre test tidak bisa create certificate
- [x] **Phase 276: Navigasi soal di StartExam** - Tampilkan seluruh nomor soal dengan fitur klik langsung
- [x] **Phase 277: Delete Peserta Assessment di EditAssessment** - Hapus peserta assessment
- [x] **Phase 278: Cari Bug, Block, Error, Miss** - Audit assessment/exam dan admin area
- [x] **Phase 279: Tambah komponen waktu ExamWindowCloseDate** - Date+time combiner
- [x] **Phase 280: Anti-copy protection StartExam** - CSS anti-select + JS event blocking

</details>

### 🚧 v11.2 Admin Platform Enhancement

**Milestone Goal:** Memperkaya fitur admin PortalHC KPB dengan 4 kapabilitas baru — system settings, maintenance mode, user impersonation, dan backup/restore.

- [ ] **Phase 281: System Settings** - Admin dapat mengelola konfigurasi aplikasi dari UI dengan cache dan audit trail
- [ ] **Phase 282: Maintenance Mode** - Admin dapat mengaktifkan mode pemeliharaan yang memblokir akses non-admin
- [ ] **Phase 283: User Impersonation** - Admin dapat melihat aplikasi dari perspektif role/user lain secara read-only
- [ ] **Phase 284: Backup & Restore** - Admin dapat backup dan restore database dengan safeguard berlapis

## Phase Details

### Phase 281: System Settings
**Goal**: Admin dapat mengelola konfigurasi aplikasi secara dinamis tanpa restart, dengan validasi dan audit trail
**Depends on**: Nothing (first phase v11.2)
**Requirements**: SETT-01, SETT-02, SETT-03, SETT-04, SETT-05, SETT-06, SETT-07
**Success Criteria** (what must be TRUE):
  1. Admin membuka halaman System Settings dan melihat setting dikelompokkan per kategori (General, Assessment, Security, Coaching)
  2. Admin mengubah nilai setting (misal durasi ujian default) dan perubahan langsung berlaku tanpa restart aplikasi
  3. Admin mengubah setting, lalu di audit log terlihat record perubahan (siapa, kapan, nilai lama ke baru)
  4. Setting dengan tipe angka menolak input di luar range min/max; setting required menolak nilai kosong
  5. Setelah migrasi pertama, 10-15 setting default sudah terisi dan aplikasi berjalan normal menggunakan nilai dari database
**Plans**: 2 plans
Plans:
- [ ] 282-01-PLAN.md — Backend: model, migration, middleware
- [ ] 282-02-PLAN.md — UI: controller actions, views, admin card, banner
**UI hint**: yes

### Phase 282: Maintenance Mode
**Goal**: Admin dapat menempatkan website dalam mode pemeliharaan sehingga non-admin tidak bisa mengakses fitur apapun
**Depends on**: Phase 281
**Requirements**: MAINT-01, MAINT-02, MAINT-03, MAINT-04, MAINT-05
**Success Criteria** (what must be TRUE):
  1. Admin mengaktifkan maintenance mode dari halaman System Settings dan toggle langsung berlaku
  2. User biasa (non-admin/HC) yang mengakses halaman apapun diarahkan ke halaman maintenance dengan pesan kustom dan estimasi waktu selesai
  3. Admin dan HC tetap dapat mengakses semua halaman selama maintenance mode aktif
  4. User yang sedang login saat maintenance diaktifkan langsung diarahkan ke halaman maintenance pada request berikutnya
**Plans**: 2 plans
Plans:
- [ ] 282-01-PLAN.md — Backend: model, migration, middleware
- [ ] 282-02-PLAN.md — UI: controller actions, views, admin card, banner
**UI hint**: yes

### Phase 283: User Impersonation
**Goal**: Admin dapat melihat aplikasi dari perspektif role atau user spesifik untuk troubleshooting, tanpa bisa melakukan aksi write
**Depends on**: Phase 281
**Requirements**: IMP-01, IMP-02, IMP-03, IMP-04, IMP-05, IMP-06, IMP-07, IMP-08
**Success Criteria** (what must be TRUE):
  1. Admin memilih "View As HC" atau "View As User" dari dropdown navbar dan tampilan berubah sesuai role yang dipilih
  2. Admin memilih user spesifik untuk di-impersonate dan melihat aplikasi persis seperti user tersebut
  3. Banner merah muncul di atas setiap halaman dengan info "Anda melihat sebagai [nama/role]" dan tombol "Kembali ke Admin" yang langsung mengembalikan session
  4. Semua aksi write (ubah password, hapus data, ubah role) diblokir saat impersonation aktif — user melihat pesan error jika mencoba
  5. Setiap impersonation tercatat di audit log (siapa, sebagai siapa, kapan mulai/selesai) dan otomatis berakhir setelah 30 menit
**Plans**: 2 plans
Plans:
- [ ] 282-01-PLAN.md — Backend: model, migration, middleware
- [ ] 282-02-PLAN.md — UI: controller actions, views, admin card, banner
**UI hint**: yes

### Phase 284: Backup & Restore
**Goal**: Admin dapat backup database secara manual, download file backup, dan restore dengan konfirmasi berlapis dan safety net otomatis
**Depends on**: Phase 281, Phase 282
**Requirements**: BKP-01, BKP-02, BKP-03, BKP-04, BKP-05, BKP-06, BKP-07, BKP-08
**Success Criteria** (what must be TRUE):
  1. Admin menekan "Backup Now" dan proses berjalan di background tanpa memblokir website, dengan progress indicator yang terlihat
  2. Admin melihat daftar backup history (tanggal, ukuran, status) dan dapat download file backup ke komputer lokal
  3. Admin memilih file backup untuk restore, melihat konfirmasi dampak, mengetik "RESTORE", dan sistem otomatis membuat backup sebelum restore dijalankan
  4. Maintenance mode otomatis aktif selama proses restore berlangsung dan nonaktif setelah selesai
  5. Backup lebih dari 30 hari otomatis terhapus sesuai retention policy
**Plans**: 2 plans
Plans:
- [ ] 282-01-PLAN.md — Backend: model, migration, middleware
- [ ] 282-02-PLAN.md — UI: controller actions, views, admin card, banner
**UI hint**: yes

## Progress

**Execution Order:**
Phases execute in numeric order: 281 → 282 → 283 → 284

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 281. System Settings | 0/TBD | Not started | - |
| 282. Maintenance Mode | 0/TBD | Not started | - |
| 283. User Impersonation | 0/TBD | Not started | - |
| 284. Backup & Restore | 0/TBD | Not started | - |
