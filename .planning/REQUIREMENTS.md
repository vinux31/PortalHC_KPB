# Requirements: PortalHC KPB — v11.2 Admin Platform Enhancement

**Defined:** 2026-04-01
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v11.2 Requirements

Requirements untuk milestone v11.2. Setiap requirement maps ke roadmap phases.

### System Settings

- [ ] **SETT-01**: Admin dapat melihat halaman System Settings dengan pengaturan dikelompokkan per kategori (General, Assessment, Security, Coaching)
- [ ] **SETT-02**: Admin dapat mengubah nilai setting dan perubahan langsung berlaku tanpa restart aplikasi
- [ ] **SETT-03**: Sistem menyimpan setting di database (key-value) dengan in-memory cache yang otomatis di-invalidate saat update
- [ ] **SETT-04**: Aplikasi menggunakan setting dari database (bukan hardcoded) untuk parameter yang dikonfigurasi (durasi ujian default, passing score, session timeout, dll)
- [ ] **SETT-05**: Setiap perubahan setting tercatat di audit log (siapa, kapan, nilai lama → baru)
- [ ] **SETT-06**: Setting memiliki validasi sesuai tipe data (min/max untuk angka, required untuk wajib, dropdown untuk enum)
- [ ] **SETT-07**: Seed default values tersedia saat migrasi pertama (10-15 setting awal)

### Maintenance Mode

- [ ] **MAINT-01**: Admin dapat mengaktifkan/menonaktifkan maintenance mode dari halaman System Settings
- [ ] **MAINT-02**: Saat maintenance mode aktif, semua user non-admin diarahkan ke halaman maintenance yang informatif (pesan kustom + estimasi waktu selesai)
- [ ] **MAINT-03**: Admin dan HC tetap dapat mengakses semua halaman selama maintenance mode aktif
- [ ] **MAINT-04**: Halaman maintenance menampilkan logo, pesan kustom dari admin, dan estimasi waktu selesai
- [ ] **MAINT-05**: User yang sedang login saat maintenance diaktifkan langsung diarahkan ke halaman maintenance pada request berikutnya

### User Impersonation

- [ ] **IMP-01**: Admin dapat memilih role (HC/User) untuk "View As" dari dropdown di navbar — tampilan berubah sesuai role yang dipilih
- [ ] **IMP-02**: Admin dapat memilih user spesifik untuk di-impersonate dari halaman ManageWorkers atau dropdown khusus
- [ ] **IMP-03**: Saat impersonation aktif, banner warna mencolok muncul di atas halaman dengan info "Anda melihat sebagai [role/nama user]" dan tombol "Kembali ke Admin"
- [ ] **IMP-04**: Impersonation otomatis berakhir setelah 30 menit (auto-expire)
- [ ] **IMP-05**: Semua aksi write/destructive diblokir saat impersonation aktif (read-only mode) — ubah password, hapus data, ubah role tidak bisa dilakukan
- [ ] **IMP-06**: Setiap impersonation tercatat di audit log: siapa yang impersonate, sebagai siapa, kapan mulai/selesai
- [ ] **IMP-07**: Admin tidak dapat impersonate admin lain (hanya role HC dan User)
- [ ] **IMP-08**: Klik "Kembali ke Admin" langsung mengembalikan ke session admin asli tanpa login ulang

### Backup & Restore

- [ ] **BKP-01**: Admin dapat memicu backup database secara manual dari halaman admin (tombol "Backup Now")
- [ ] **BKP-02**: Proses backup berjalan di background (async) — tidak memblokir website, dengan progress indicator
- [ ] **BKP-03**: Admin dapat melihat daftar backup history (tanggal, ukuran file, status berhasil/gagal)
- [ ] **BKP-04**: Admin dapat download file backup ke komputer lokal
- [ ] **BKP-05**: Admin dapat restore database dari file backup yang dipilih, dengan konfirmasi berlapis (tampilkan dampak + ketik "RESTORE" untuk konfirmasi)
- [ ] **BKP-06**: Sistem otomatis membuat backup sebelum menjalankan restore (safety net)
- [ ] **BKP-07**: Maintenance mode otomatis aktif selama proses restore berlangsung
- [ ] **BKP-08**: Backup history memiliki retention policy (auto-delete backup lebih dari 30 hari)

## Future Requirements (v12+)

### System Settings
- **SETT-F01**: Feature flags (toggle fitur on/off dari admin panel)
- **SETT-F02**: Password policy (min length, complexity, expiry)
- **SETT-F03**: SMTP/Email configuration dari admin panel

### Maintenance Mode
- **MAINT-F01**: Scheduled maintenance (set waktu mulai & selesai otomatis)
- **MAINT-F02**: Partial maintenance per modul (CMP saja, CDP saja)

### Dashboard Statistik Admin
- **DASH-F01**: Dashboard KPI overview (pekerja, assessment, sertifikat, coaching)
- **DASH-F02**: Trend chart, comparison antar unit, export

### User Impersonation
- **IMP-F01**: Read/Write mode terpisah (admin bisa melakukan aksi atas nama user)

### Backup & Restore
- **BKP-F01**: Scheduled auto-backup (harian/mingguan)
- **BKP-F02**: Backup uploaded files (sertifikat, evidence, KKJ, CPDP)
- **BKP-F03**: Backup validation (checksum, test restore)

### Notification Enhancement
- **NOTIF-F01**: Trigger tambahan: sertifikat expiring, maintenance announcement

### Announcement / Broadcast
- **ANN-F01**: CRUD announcement dengan target audience
- **ANN-F02**: Banner di dashboard + mark as read

## Out of Scope

| Feature | Reason |
|---------|--------|
| Announcement / Broadcast | Di-drop oleh user — belum prioritas untuk milestone ini |
| Dashboard Statistik Admin | Di-drop — user masih mempertimbangkan fungsinya, ditunda ke milestone berikutnya |
| In-App Notification Enhancement | Sistem notifikasi sudah lengkap (Phase 99) — bell icon, dropdown, mark read, templates |
| Soft Delete / Recycle Bin | Di-drop dari scope awal (8 → 7 fitur, lalu 7 → 5) |
| Email blast / SMTP integration | Butuh infrastruktur email terpisah yang belum tersedia |
| Custom report builder (drag & drop) | Over-engineering — export Excel sudah cukup |
| Auto-restore database tanpa konfirmasi | Risiko data loss terlalu tinggi |
| Impersonation tanpa audit trail | Security risk fatal |
| Push notification browser (Web Push API) | Overkill untuk internal portal |
| Multi-tenant settings | Single tenant, tidak relevan |
| Theme customization | Overkill untuk internal portal |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SETT-01 | TBD | Pending |
| SETT-02 | TBD | Pending |
| SETT-03 | TBD | Pending |
| SETT-04 | TBD | Pending |
| SETT-05 | TBD | Pending |
| SETT-06 | TBD | Pending |
| SETT-07 | TBD | Pending |
| MAINT-01 | TBD | Pending |
| MAINT-02 | TBD | Pending |
| MAINT-03 | TBD | Pending |
| MAINT-04 | TBD | Pending |
| MAINT-05 | TBD | Pending |
| IMP-01 | TBD | Pending |
| IMP-02 | TBD | Pending |
| IMP-03 | TBD | Pending |
| IMP-04 | TBD | Pending |
| IMP-05 | TBD | Pending |
| IMP-06 | TBD | Pending |
| IMP-07 | TBD | Pending |
| IMP-08 | TBD | Pending |
| BKP-01 | TBD | Pending |
| BKP-02 | TBD | Pending |
| BKP-03 | TBD | Pending |
| BKP-04 | TBD | Pending |
| BKP-05 | TBD | Pending |
| BKP-06 | TBD | Pending |
| BKP-07 | TBD | Pending |
| BKP-08 | TBD | Pending |

**Coverage:**
- v11.2 requirements: 28 total
- Mapped to phases: 0 (pending roadmap)
- Unmapped: 28 ⚠️

---
*Requirements defined: 2026-04-01*
*Last updated: 2026-04-01 after milestone v11.2 definition*
