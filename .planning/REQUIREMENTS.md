# Requirements: PortalHC KPB

**Defined:** 2026-04-02
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v13.0 Requirements

Requirements untuk milestone v13.0. Redesign halaman ManageOrganization dari tabel flat menjadi tree view modern.

### Tree View & Rendering

- [x] **TREE-01**: Admin/HC dapat melihat struktur organisasi sebagai tree view dengan indentasi visual per level
- [x] **TREE-02**: Admin/HC dapat expand/collapse node individual dan semua node sekaligus
- [x] **TREE-03**: Setiap node menampilkan badge status Aktif/Nonaktif
- [x] **TREE-04**: Tree view mendukung kedalaman unlimited (recursive rendering)

### CRUD & Aksi

- [ ] **CRUD-01**: Admin/HC dapat menambah unit baru via modal (AJAX, tanpa page reload)
- [ ] **CRUD-02**: Admin/HC dapat mengedit nama dan parent unit via modal (AJAX, tanpa page reload)
- [ ] **CRUD-03**: Admin/HC dapat toggle aktif/nonaktif unit via AJAX tanpa page reload
- [ ] **CRUD-04**: Admin/HC dapat menghapus unit via modal konfirmasi (AJAX, tanpa page reload)
- [ ] **CRUD-05**: Setiap node memiliki action dropdown (edit, toggle, hapus) menggantikan tombol inline

### Reorder

- [ ] **REORD-01**: Admin/HC dapat drag-and-drop unit untuk mengubah urutan dalam sibling yang sama
- [ ] **REORD-02**: Drag cross-parent diblokir untuk mencegah bypass cascade logic

## v14.0 Requirements

Requirements untuk milestone v14.0. Data foundation dan GradingService extraction untuk assessment grading.

### Data Foundation (Phase 296)

- [x] **FOUND-01**: GradingService terdaftar di DI container (Program.cs)
- [x] **FOUND-02**: QuestionType enum (MultipleChoice, MultipleAnswer, Essay) di PackageQuestion model
- [x] **FOUND-03**: AssessmentSession.AssessmentType kolom baru via migration
- [x] **FOUND-04**: AssessmentSession.AssessmentPhase, LinkedGroupId, LinkedSessionId kolom baru
- [x] **FOUND-05**: PackageUserResponse.TextAnswer + HasManualGrading field + migration
- [x] **FOUND-06**: GradeAndCompleteAsync dengan switch-case QuestionType (MC implemented, MA/Essay graceful skip)
- [x] **FOUND-07**: AkhiriUjian menggunakan GradingService
- [x] **FOUND-08**: AkhiriSemuaUjian menggunakan GradingService (dengan per-session error handling)
- [x] **FOUND-09**: SubmitExam menggunakan GradingService

### Pre-Post Test Admin (Phase 297)

- [ ] **PPT-01**: HC dapat memilih tipe assessment "Pre-Post Test" saat membuat assessment baru
- [ ] **PPT-02**: HC dapat mengatur jadwal dan durasi berbeda untuk Pre dan Post
- [ ] **PPT-03**: HC dapat mencentang "Gunakan paket soal yang sama" untuk copy paket Pre ke Post
- [ ] **PPT-04**: HC dapat memilih paket soal berbeda untuk Pre dan Post secara independen
- [ ] **PPT-05**: AssessmentMonitoring menampilkan grup Pre-Post Test sebagai satu entri expandable
- [ ] **PPT-06**: Reset Pre-Test TIDAK cascade ke Post-Test; reset Pre diblokir jika Post sudah Completed (per D-16, D-17 dari CONTEXT.md)
- [ ] **PPT-07**: Hapus grup Pre-Post menghapus kedua sesi tanpa orphan record
- [ ] **PPT-08**: Sertifikat hanya digenerate dari hasil Post-Test
- [ ] **PPT-09**: Training Record hanya dari Post-Test
- [ ] **PPT-10**: Pre-Post Test muncul di monitoring dengan status per-phase (Pre/Post)
- [ ] **PPT-11**: Renewal assessment bebas pilih tipe (Standard atau PrePostTest)

### Question Types (Phase 298)

- [ ] **QTYPE-01**: HC dapat membuat soal True/False (2 opsi radio)
- [ ] **QTYPE-02**: HC dapat membuat soal Multiple Answer (checkbox multi-pilih)
- [ ] **QTYPE-03**: HC dapat membuat soal Essay (rich text editor)
- [ ] **QTYPE-04**: HC dapat membuat soal Fill in the Blank (text input)
- [ ] **QTYPE-05**: Template Excel impor soal memiliki kolom QuestionType
- [ ] **QTYPE-06**: Upload bulk berhasil dengan tipe soal beragam dalam satu file
- [ ] **QTYPE-07**: StartExam menampilkan UI yang sesuai per tipe soal
- [ ] **QTYPE-08**: Multiple Answer scoring all-or-nothing
- [ ] **QTYPE-09**: Essay tidak ter-grading otomatis — status "Menunggu Penilaian"
- [ ] **QTYPE-10**: HC dapat input skor per soal Essay dari AssessmentMonitoringDetail
- [ ] **QTYPE-11**: Sistem menghitung ulang skor total setelah semua Essay dinilai
- [ ] **QTYPE-12**: Fill in the Blank auto-grade exact match case-insensitive
- [ ] **QTYPE-13**: IsPassed tetap null sampai semua Essay dinilai

### Worker Pre-Post Test (Phase 299)

- [ ] **WKPPT-01**: Daftar assessment menampilkan Pre dan Post sebagai 2 card terhubung
- [ ] **WKPPT-02**: Post-Test tidak bisa dimulai sebelum Pre-Test Completed
- [ ] **WKPPT-03**: Post-Test dapat dimulai setelah Pre-Test Completed dan jadwal tiba
- [ ] **WKPPT-04**: Halaman perbandingan Pre vs Post dengan skor side-by-side
- [ ] **WKPPT-05**: Gain score formula: (Post - Pre) / (100 - Pre) x 100
- [ ] **WKPPT-06**: PreScore = 100 → Gain = 100
- [ ] **WKPPT-07**: Gain score per elemen kompetensi

### Mobile Optimization (Phase 300)

- [ ] **MOB-01**: Area sentuh minimal 48x48dp untuk tombol dan opsi jawaban
- [ ] **MOB-02**: Swipe kiri/kanan untuk navigasi antar halaman soal
- [ ] **MOB-03**: Sticky footer (Previous, Next, Submit) + offcanvas drawer navigasi soal
- [ ] **MOB-04**: Timer tetap terlihat di header mobile saat scroll
- [ ] **MOB-05**: Anti-copy (Phase 280) tetap berfungsi dengan touch/swipe events
- [ ] **MOB-06**: Layout responsif tanpa elemen terpotong di layar kecil

### Advanced Reporting (Phase 301)

- [ ] **RPT-01**: Item Analysis — difficulty index (p-value) per soal
- [ ] **RPT-02**: Discrimination index (Kelley upper/lower 27%) dengan warning < 30 responden
- [ ] **RPT-03**: Distractor analysis — persentase per opsi per soal
- [ ] **RPT-04**: Pre-Post Gain Score Report per pekerja dan per elemen kompetensi
- [ ] **RPT-05**: Export Item Analysis dan Gain Score Report ke Excel
- [ ] **RPT-06**: Analytics Dashboard panel tren gain score
- [ ] **RPT-07**: Perbandingan antar kelompok (group comparison)

### Accessibility WCAG Quick Wins (Phase 302)

- [ ] **A11Y-01**: Skip link "Lewati ke konten utama"
- [ ] **A11Y-02**: Keyboard navigation untuk semua soal dan opsi jawaban
- [ ] **A11Y-03**: Screen reader announcement (aria-live) sisa waktu < 5 menit
- [ ] **A11Y-04**: Kontrol ukuran font (A+/A-) dengan localStorage persistence
- [ ] **A11Y-05**: ExtraTimeMinutes per sesi untuk peserta kebutuhan khusus
- [ ] **A11Y-06**: Auto-focus ke soal pertama saat berpindah halaman

## Future Requirements (v15+)

### Enhancements

- **ENH-01**: Badge jumlah pekerja per unit
- **ENH-02**: Badge jumlah children per node
- **ENH-03**: Search/filter tree
- **ENH-04**: Inline rename (double-click)
- **ENH-05**: Konfirmasi delete dengan detail dampak (jumlah user/file terdampak)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Org chart box-and-line diagram | Anti-feature untuk management UI, tidak scale, butuh library besar |
| Drag cross-parent (reparent via drag) | Bypass cascade rename/reparent logic, risiko data rusak |
| Refactor GetSectionUnitsDictAsync | Status quo 2-level cukup untuk kebutuhan saat ini |
| Backend cascade logic changes | Backend sudah solid, fokus milestone ini pada UI/UX saja |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| TREE-01 | Phase 292, 293 | Complete |
| TREE-02 | Phase 293 | Complete |
| TREE-03 | Phase 293 | Complete |
| TREE-04 | Phase 292, 293 | Complete |
| CRUD-01 | Phase 294 | Pending |
| CRUD-02 | Phase 294 | Pending |
| CRUD-03 | Phase 294 | Pending |
| CRUD-04 | Phase 294 | Pending |
| CRUD-05 | Phase 294 | Pending |
| REORD-01 | Phase 295 | Pending |
| REORD-02 | Phase 295 | Pending |

| FOUND-01 | Phase 296 | Complete |
| FOUND-02 | Phase 296 | Complete |
| FOUND-03 | Phase 296 | Complete |
| FOUND-04 | Phase 296 | Complete |
| FOUND-05 | Phase 296 | Complete |
| FOUND-06 | Phase 296 | Complete |
| FOUND-07 | Phase 296 | Complete |
| FOUND-08 | Phase 296 | Complete |
| FOUND-09 | Phase 296 | Complete |
| PPT-01 | Phase 297 | Pending |
| PPT-02 | Phase 297 | Pending |
| PPT-03 | Phase 297 | Pending |
| PPT-04 | Phase 297 | Pending |
| PPT-05 | Phase 297 | Pending |
| PPT-06 | Phase 297 | Pending |
| PPT-07 | Phase 297 | Pending |
| PPT-08 | Phase 297 | Pending |
| PPT-09 | Phase 297 | Pending |
| PPT-10 | Phase 297 | Pending |
| PPT-11 | Phase 297 | Pending |
| QTYPE-01 | Phase 298 | Pending |
| QTYPE-02 | Phase 298 | Pending |
| QTYPE-03 | Phase 298 | Pending |
| QTYPE-04 | Phase 298 | Pending |
| QTYPE-05 | Phase 298 | Pending |
| QTYPE-06 | Phase 298 | Pending |
| QTYPE-07 | Phase 298 | Pending |
| QTYPE-08 | Phase 298 | Pending |
| QTYPE-09 | Phase 298 | Pending |
| QTYPE-10 | Phase 298 | Pending |
| QTYPE-11 | Phase 298 | Pending |
| QTYPE-12 | Phase 298 | Pending |
| QTYPE-13 | Phase 298 | Pending |
| WKPPT-01 | Phase 299 | Pending |
| WKPPT-02 | Phase 299 | Pending |
| WKPPT-03 | Phase 299 | Pending |
| WKPPT-04 | Phase 299 | Pending |
| WKPPT-05 | Phase 299 | Pending |
| WKPPT-06 | Phase 299 | Pending |
| WKPPT-07 | Phase 299 | Pending |
| MOB-01 | Phase 300 | Pending |
| MOB-02 | Phase 300 | Pending |
| MOB-03 | Phase 300 | Pending |
| MOB-04 | Phase 300 | Pending |
| MOB-05 | Phase 300 | Pending |
| MOB-06 | Phase 300 | Pending |
| RPT-01 | Phase 301 | Pending |
| RPT-02 | Phase 301 | Pending |
| RPT-03 | Phase 301 | Pending |
| RPT-04 | Phase 301 | Pending |
| RPT-05 | Phase 301 | Pending |
| RPT-06 | Phase 301 | Pending |
| RPT-07 | Phase 301 | Pending |
| A11Y-01 | Phase 302 | Pending |
| A11Y-02 | Phase 302 | Pending |
| A11Y-03 | Phase 302 | Pending |
| A11Y-04 | Phase 302 | Pending |
| A11Y-05 | Phase 302 | Pending |
| A11Y-06 | Phase 302 | Pending |

**Coverage:**
- v13.0 requirements: 11 total, mapped: 11
- v14.0 requirements: 52 total, mapped: 52 (9 complete, 43 pending)
- Unmapped: 0

---
*Requirements defined: 2026-04-02*
*Last updated: 2026-04-07 — PPT-06 revised to match D-16/D-17 user decisions (no cascade, block if Post Completed)*
