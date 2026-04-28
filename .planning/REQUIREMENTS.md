# Requirements: PortalHC KPB — v15.0 Audit Findings 27 April 2026

**Defined:** 2026-04-28
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Milestone Goal:** Tindak lanjut 11 temuan audit pada flow assessment & login PortalHC_KPB — bug-fix + UX enhancements + 1 perf improvement, tanpa migrasi DB.

## v15.0 Requirements

Requirements untuk milestone v15.0 (audit fixes). Setiap REQ memetakan 1 temuan audit ke 1 capability yang testable.

### Authentication UX

- [ ] **AUTH-01**: User dapat toggle visibility password di halaman login via tombol eye-icon (Show/Hide), keyboard accessible, tidak men-trigger form submit. *(maps Temuan 1)*

### Question Management

- [ ] **QSCR-01**: Admin/HC dapat menyimpan skor 1–100 untuk soal MultipleChoice, MultipleAnswer, dan Essay. Override server-side `scoreValue=10` di `CreateQuestion` (line 4681) dan `EditQuestion` (line 4822) dihapus; input view enabled untuk semua tipe. *(maps Temuan 2)*

### Question Type Labels

- [ ] **LBL-01**: User-facing label untuk tipe soal MultipleChoice dan MultipleAnswer dirubah agar tidak rancu — "Pilihan Tunggal (1 jawaban benar)" dan "Pilihan Jamak (≥2 jawaban benar)" — di form admin (`ManagePackageQuestions.cshtml`), preview (`_PreviewQuestion.cshtml`), exam (`StartExam.cshtml`), dan summary (`ExamSummary.cshtml`). Internal enum/string DB **tidak** diubah. *(maps Temuan 7)*

### Create Assessment Wizard

- [ ] **WIZ-01**: Admin/HC dapat melihat real-time list nama peserta yang sudah dicentang di Step 2 (Pilih Peserta), dengan badge count + truncate "...dan N lainnya" konsisten dengan Step 4 summary. *(maps Temuan 4)*

- [ ] **WIZ-02**: Setiap input waktu di Step 3 wizard (Tanggal/Waktu Jadwal, Tanggal/Waktu Tutup Ujian, Pre-Test datetime, Post-Test datetime, Batas Waktu Pengerjaan Pre/Post) menampilkan label "(WIB)" eksplisit. *(maps Temuan 5)*

- [ ] **WIZ-03**: Step 4 summary menampilkan suffix " WIB" pada baris "Jam Tutup", konsisten dengan baris "Jam Mulai" yang sudah memiliki suffix WIB. PrePost summary juga konsisten. *(maps Temuan 6)*

- [ ] **WIZ-04**: Admin dapat submit assessment Pre-Post Test tanpa error "Status field is required" yang me-reset wizard ke Step 1. JS handler set value `Status='Upcoming'` saat PrePost mode + conditional `ModelState.Remove("Status")` server-side; switching mode Standard ↔ PrePost tidak meninggalkan stale validation state. *(maps Temuan 11)*

### ManageAssessment Performance

- [ ] **PERF-01**: Halaman `/Admin/ManageAssessment` memuat ≥30% lebih cepat dari baseline pada dataset produksi (target p95 ≤ baseline × 0.7). Strategi: `AsNoTracking()` di chain query baris 66, hapus redundant `.Include(a => a.User)` baris 88, tambah DB index `IX_AssessmentSessions_Schedule_ExamWindowCloseDate` & `IX_LinkedGroupId` jika belum ada, `IMemoryCache` (TTL 5 menit) untuk distinct Categories. *(maps Temuan 3)*

### Essay Scoring & Certification

- [ ] **ESCG-01**: Admin tidak menerima error "session tidak dalam status menunggu penilaian" saat membuka halaman create sertifikasi pada session mix MC/MA/Essay yang sudah Completed. UI menyembunyikan tombol "Create Sertifikasi" jika `Status == "Completed"` && `NomorSertifikat != null`; OR jika tombol di-klik untuk session terminal, menampilkan pesan ramah no-op (bukan error). Idempotent: klik 2x tidak menduplikasi notification atau audit log. *(maps Temuan 9)*

### Worker Certificate View

- [ ] **WCRT-01**: Worker yang sudah lulus assessment dengan `GenerateCertificate=true` dapat membuka halaman sertifikat (`/CMP/Certificate/{id}`) tanpa redirect ke 500. Defensif: try-catch di `Certificate` action mirror pattern `CertificatePdf` (baris 2078–2083), structured `_logger.LogError`, null-safe accessor di `Certificate.cshtml` (`Model.User?.FullName ?? "..."`), specific exception catches (DbException, FormatException, NRE). *(maps Temuan 10)*

## Deferred Requirements (menunggu klarifikasi)

- [ ] **EPRV-01**: [DEFERRED] Admin dapat melihat kunci jawaban/rubrik Essay yang ter-load benar saat preview package soal. *(maps Temuan 8)*

  **Status:** Menunggu user konfirmasi setelah verifikasi save/load Rubrik:
  - **Jalur A** — Label fix saja: Rubrik tersimpan & tampil benar; ubah label preview menjadi "Kunci Jawaban / Rubrik (hanya HC):". Tidak ada perubahan schema.
  - **Jalur B** — Field baru: tambah kolom `EssayAnswerKey` ke `PackageQuestion` (butuh migrasi DB), update form input, preview, dan logic penilaian.

  **Action sebelum implementasi:** Smoke test — buat soal Essay dengan rubrik berisi teks → simpan → buka preview. Jika rubrik muncul = Jalur A. Jika rubrik kosong padahal sudah diinput = bug binding di save action (perbaiki dulu, baru tentukan A/B).

## Out of Scope

Explicit exclusions untuk milestone v15.0:

| Feature | Reason |
|---------|--------|
| Migrasi DB / schema change | Goal milestone: tanpa migrasi DB. EPRV-01 Jalur B di-defer ke milestone berikutnya jika user pilih. |
| Rename enum/string internal MultipleChoice/MultipleAnswer | Risiko terlalu besar (58+ file & migrasi DB). Hanya rename label UI saja per LBL-01. |
| Multi-timezone support (WITA/WIT) | Plan eksplisit single-timezone label-only. Catat untuk roadmap v16+ jika kebutuhan muncul. |
| NuGet/JS package additions | Stack existing mencukupi (lihat STACK.md). |
| Service extraction (`AssessmentManagementQueryService`, `EssayGradingService`, dll) | YAGNI — semua fix justified inline. |
| Global Exception Filter | Pola try-catch per-action sudah established di `CertificatePdf`. |
| Auto-revert password ke masked setelah X detik (T1 differentiator) | Differentiator nice-to-have, di luar scope audit fix minimal. |
| Bulk-set score untuk semua soal sekaligus (T2 differentiator) | Differentiator, audit hanya minta editable per-soal. |
| Per-option partial credit MA | Differentiator, audit tidak request. |
| Test wizard return-to-step-1 enhancement (T11 differentiator) | Audit hanya minta tidak reset. Reset behavior tetap untuk error real. |

## Traceability

Mapping requirement ke phase (filled by roadmap 2026-04-28). Phase numbering melanjutkan dari Phase 303 (terakhir di v14.0).

| Requirement | Phase | Status |
|-------------|-------|--------|
| AUTH-01 | Phase 304 (UI Label Polish) | Pending |
| WIZ-02 | Phase 304 (UI Label Polish) | Pending |
| WIZ-03 | Phase 304 (UI Label Polish) | Pending |
| LBL-01 | Phase 305 (Question Type Naming Clarity) | Pending |
| QSCR-01 | Phase 306 (Score Editable per Question Type) | Pending |
| WIZ-01 | Phase 307 (Selected Participants Inline View) | Pending |
| WIZ-04 | Phase 308 (PrePost Wizard Validation Fix) | Pending |
| WCRT-01 | Phase 309 (Worker Certificate Defensive Fix) | Pending |
| ESCG-01 | Phase 310 (Essay Finalize Idempotency) | Pending |
| PERF-01 | Phase 311 (ManageAssessment Performance) | Pending |
| EPRV-01 | DEFERRED (due 2026-05-12) | Pending klarifikasi user |

**Coverage:**
- v15.0 requirements: 11 total (10 active + 1 deferred)
- Active mapped to phases: **10/10 ✓** (Phase 304-311)
- Deferred (tracked, not phase-mapped): 1 (EPRV-01, due 2026-05-12)
- Coverage 11 temuan audit: **11/11 (100%)**
- Orphans: 0 | Duplicates: 0

---
*Requirements defined: 2026-04-28*
*Source audit: 27 April 2026*
*Last updated: 2026-04-28 (initial creation v15.0)*
