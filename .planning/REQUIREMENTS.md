# Requirements: Portal HC KPB — v32.7 Perbaikan Menyeluruh Sistem Pre-Test/Post-Test

**Defined:** 2026-06-22
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Sumber:** Audit `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` (~60 temuan in-scope, 4 High). Keputusan bisnis: (a) Pre WAJIB selesai dulu; (b) SamePackage fleksibel; (c) fasilitas soal Post=Pre sudah ada.

## v1 Requirements

Requirements untuk milestone v32.7. Tiap REQ memetakan satu/lebih temuan audit (ID di kurung) dan dipetakan ke satu fase (420–425).

### FORM — Form Create/Edit: Persistensi Field + UX Pre-Post (Phase 420)

- [x] **FORM-01**: Saat HC menyimpan ulang assessment via form Edit, setelan "Acak Soal" & "Acak Pilihan" tidak ter-reset OFF (tetap sesuai nilai tersimpan). [E-01, HIGH]
- [x] **FORM-02**: Setelan Ujian Ulang (AllowRetake/MaxAttempts/RetakeCooldownHours) yang diisi di form Create tersimpan ke sesi yang dibuat (tidak jatuh ke default). [FLD-5.2-08/RTK-LOGIC-04]
- [x] **FORM-03**: Setelan Ujian Ulang yang diubah di form Edit benar-benar tersimpan (bukan no-op). [E-03]
- [x] **FORM-04**: Tanggal kadaluarsa sertifikat (ValidUntil) yang diubah di form Edit tersimpan di jalur standard. [E-05]
- [x] **FORM-05**: Assessment Pre/Post yang sudah Completed terkunci dari perubahan metadata via form Edit. [E-04]
- [x] **FORM-06**: Membuka Edit untuk sesi entry-manual mengarahkan ke form edit manual (bukan form online). [E-08, E-07]
- [ ] **FORM-07**: Di form Create mode Pre-Post, opsi "Paket soal sama" (SamePackage) ditempatkan di tingkat pasangan Pre↔Post, bukan terkubur di dalam kartu Post. [FORM-PP-01]
- [ ] **FORM-08**: Di form Create mode Pre-Post, tiap setelan ujian/sertifikat menampilkan penanda scope (berlaku Pre / Post / keduanya) atau dikelompokkan eksplisit. [FORM-PP-02]
- [ ] **FORM-09**: Form Create mode Pre-Post tidak mengirim input jadwal/durasi/batas-waktu standard yang tersembunyi (eliminasi field duplikat ter-POST). [FORM-PP-03]
- [ ] **FORM-10**: Penamaan tipe assessment konsisten — input pemilih mode diselaraskan dengan kolom DB AssessmentType; label & XML-doc diperbarui. [FLD-5.2-01, FORM-PP-07]
- [ ] **FORM-11**: Tata-letak Group "Pengaturan Ujian" mode Pre-Post dirapikan — baris Status/PassPercentage tidak timpang; PassPercentage/Retake tidak tampil seakan berlaku ke Pre baseline; token jelas scope-nya. [FORM-PP-05, FORM-PP-06]

### RTH — Retake Lifecycle Hardening (Phase 421)

- [x] **RTH-01**: Ujian ulang ditolak bila ExamWindowCloseDate sudah lewat — sesi live tidak dihapus (tidak ada dead-end destruktif). [RTK-LOGIC-02, HIGH]
- [x] **RTH-02**: Reset assessment oleh HC menghapus NomorSertifikat sehingga sesi non-lulus tidak menyandang nomor sertifikat menggantung. [RTK-LOGIC-01]
- [x] **RTH-03**: Penghitungan jumlah percobaan konsisten antara batas (cap) dan peringatan di ManagePackages. [RTK-LOGIC-03]
- [x] **RTH-04**: Guard hapus peserta menolak/menangani sesi Abandoned atau ber-riwayat percobaan, dan membersihkan arsip respons terkait. [PA-06]
- [x] **RTH-05**: Mengubah MaxAttempts di bawah jumlah percobaan yang sudah terpakai memunculkan peringatan (non-blocking). [VAL-06]

### SHFX — SamePackage & Shuffle Integrity (Phase 422)

- [ ] **SHFX-01**: Mengimpor soal via Excel ke paket Pre yang ber-SamePackage memicu sinkronisasi otomatis ke Post. [SHUF-ISS-03, HIGH]
- [ ] **SHFX-02**: HC dapat mengubah setelan SamePackage setelah grup Pre-Post dibuat, dengan sinkron/unsync paket + guard sebelum peserta mulai. [FLOW-07] (keputusan bisnis b)
- [ ] **SHFX-03**: Endpoint POST kelola paket/soal menolak edit pada Post yang terkunci SamePackage (lock server-side, bukan hanya di tampilan). [SHUF-ISS-02]
- [ ] **SHFX-04**: Peserta baru yang ditambahkan ke grup Pre-Post mewarisi setelan SamePackage dari grup. [PA-02]
- [x] **SHFX-05**: Penomoran paket (PackageNumber) tetap unik & terurut deterministik setelah operasi hapus paket. [SHUF-ISS-08]
- [ ] **SHFX-06**: Kunci pasangan Pre/Post untuk lock & simpan setelan shuffle konsisten type-aware (selaras StartExam/Reshuffle). [SHUF-ISS-01]
- [x] **SHFX-07**: Peringatan shuffle lengkap — SamePackage+Acak ON, pemangkasan K=min, dan hitung mismatch dari satu sumber. [SHUF-ISS-04, SHUF-ISS-05, SHUF-ISS-07]

### CERT — Certificate Issuance Consistency (Phase 423)

- [x] **CERT-01**: Penerbitan sertifikat memakai satu helper bersama yang konsisten menolak Pre-Test di SEMUA jalur grading-time. [GRD-01, FLD-5.2-10]
- [x] **CERT-02**: Saat sertifikat diaktifkan untuk sesi non-Pre, ValidUntil wajib diisi atau ditangani eksplisit (cert tidak terbit tanpa masa berlaku secara tak sengaja). [GRD-06]
- [x] **CERT-03**: Nomor urut sertifikat dihasilkan secara atomik tanpa race (tidak gagal terbit saat finalize bersamaan/burst). [GRD-08]
- [x] **CERT-04**: Penomoran sertifikat manual vs auto-generate tidak bentrok — kolisi memunculkan pesan error ramah, namespace dipisah. [FLD-5.2-07, FLD-5.2-02]
- [x] **CERT-05**: Guard server-side mencegah dua sertifikat aktif untuk (peserta, judul) yang sama dan tidak bisa di-bypass via ConfirmDuplicateTitle. [VAL-04, GRD-10]
- [x] **CERT-06**: CertificateType dan ValidUntil konsisten — "Permanent" menolak ValidUntil; "Annual"/"3-Year" menurunkan ValidUntil. [FLD-5.2-09]
- [x] **CERT-07**: Sesi "Menunggu Penilaian" menampilkan umur/penanda agar tidak menggantung tanpa finalize HC (tanpa auto-finalize). [GRD-05]

### GRDF — Grading De-dup + Flow/Linking + Gating Pre→Post (Phase 424)

- [ ] **GRDF-01**: Peserta tidak dapat memulai (StartExam) Post-Test sebelum Pre-Test pasangannya berstatus Completed. [FLOW-04, HIGH] (keputusan bisnis a)
- [ ] **GRDF-02**: Logika penilaian per-soal (MC/MA/Essay) memakai satu fungsi murni bersama dengan strategi dedupe konsisten di semua jalur. [GRD-09, GRD-02, GRD-03]
- [ ] **GRDF-03**: Pemasangan Pre/Post memakai satu sumber kebenaran (tidak tiga jalur divergen; pairing per-peserta terfilter UserId). [FLOW-01]
- [ ] **GRDF-04**: Assessment mode Standard tidak mendapat link Pre/Post semu yang diturunkan dari pola judul. [FLOW-03]
- [ ] **GRDF-05**: Perhitungan durasi aktif (ElapsedSeconds) memperhitungkan ExtraTimeMinutes secara konsisten. [FLOW-02]
- [ ] **GRDF-06**: Manajemen peserta simetris — hapus peserta tersedia/konsisten di Standard & Pre-Post; logika dedup seragam. [PA-07, PA-08]
- [ ] **GRDF-07**: Submit ujian on-time menolak essay kosong di sisi server (validasi tidak hanya client-side). [VAL-03]

### CLN — Cosmetic / Naming / Tech-Debt Cleanup (Phase 425, low-risk)

- [x] **CLN-01**: Label & dokumentasi diselaraskan — label ValidUntil, komentar Status 7-nilai, nama field sentinel AssessmentPackageId, doc FK LinkedSessionId. [FLD-5.2-06, FLOW-05, PA-05, PA-04]
- [x] **CLN-02**: Entry manual — Schedule/CompletedAt diselaraskan + validasi silang IsPassed vs Score/PassPercentage (peringatan, tidak auto-override). [FLD-5.2-04, FLD-5.2-05]
- [x] **CLN-03**: Kolom dead-field AssessmentPhase di-drop (migration) atau ditandai RESERVED di XML-doc. [FLOW-06]
- [ ] **CLN-04**: Tech-debt timing — hitung timer satu sumber (helper), token via mekanisme server-authoritative, side-effect write-on-GET StartExam dipindah/diamankan. [FLOW-09, FLOW-08, FLOW-10]
- [ ] **CLN-05**: Konvensi validasi ModelState dirapikan (param scalar → DTO ber-anotasi atau guard helper bersama). [VAL-07]

## v2 Requirements

Diakui tapi ditunda dari v32.7.

### Atribusi & Reporting

- **RPT-01**: Breakdown analitik Pre/Post per-unit akurat (bergantung kolom unit-at-issue, ditunda dari v32.3).

## Out of Scope

| Feature | Reason |
|---------|--------|
| Audit & fix Inject Assessment (VAL-01) | Fitur Inject (v32.2) hidup di branch `main`, TIDAK ada di ITHandoff — audit & perbaikan dijalankan terpisah di main. |
| Section + Scoped Shuffle + Pagination + Opsi Dinamis | Sedang dikerjakan di branch `main` (v32.6, fase 415-419). Overlap dengan FORM/SHFX dicatat untuk rekonsiliasi saat merge, bukan di-duplikasi di sini. |
| Grading method retake selain "attempt terakhir" (highest/avg) | YAGNI — dikonfirmasi out di v32.4, tidak dibuka ulang. |
| Hard-gate remediation/training antara Pre & Post | Di luar scope (a) yang diminta = urutan pelaksanaan, bukan gate konten training. |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| FORM-01 | Phase 420 | Complete |
| FORM-02 | Phase 420 | Complete |
| FORM-03 | Phase 420 | Complete |
| FORM-04 | Phase 420 | Complete |
| FORM-05 | Phase 420 | Complete |
| FORM-06 | Phase 420 | Complete |
| FORM-07 | Phase 420 | Pending |
| FORM-08 | Phase 420 | Pending |
| FORM-09 | Phase 420 | Pending |
| FORM-10 | Phase 420 | Pending |
| FORM-11 | Phase 420 | Pending |
| RTH-01 | Phase 421 | Complete |
| RTH-02 | Phase 421 | Complete |
| RTH-03 | Phase 421 | Complete |
| RTH-04 | Phase 421 | Complete |
| RTH-05 | Phase 421 | Complete |
| SHFX-01 | Phase 422 | Pending |
| SHFX-02 | Phase 422 | Pending |
| SHFX-03 | Phase 422 | Pending |
| SHFX-04 | Phase 422 | Pending |
| SHFX-05 | Phase 422 | Complete |
| SHFX-06 | Phase 422 | Pending |
| SHFX-07 | Phase 422 | Complete |
| CERT-01 | Phase 423 | Complete |
| CERT-02 | Phase 423 | Complete |
| CERT-03 | Phase 423 | Complete |
| CERT-04 | Phase 423 | Complete |
| CERT-05 | Phase 423 | Complete |
| CERT-06 | Phase 423 | Complete |
| CERT-07 | Phase 423 | Complete |
| GRDF-01 | Phase 424 | Pending |
| GRDF-02 | Phase 424 | Pending |
| GRDF-03 | Phase 424 | Pending |
| GRDF-04 | Phase 424 | Pending |
| GRDF-05 | Phase 424 | Pending |
| GRDF-06 | Phase 424 | Pending |
| GRDF-07 | Phase 424 | Pending |
| CLN-01 | Phase 425 | Complete |
| CLN-02 | Phase 425 | Complete |
| CLN-03 | Phase 425 | Complete |
| CLN-04 | Phase 425 | Pending |
| CLN-05 | Phase 425 | Pending |

**Coverage:**
- v1 requirements: 42 total
- Mapped to phases: 42
- Unmapped: 0 ✓

---
*Requirements defined: 2026-06-22*
*Last updated: 2026-06-22 after initial definition (milestone v32.7)*
