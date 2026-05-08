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

- [x] **LBL-01**: User-facing label untuk tipe soal MultipleChoice dan MultipleAnswer dirubah agar tidak rancu — "Pilihan Tunggal (1 jawaban benar)" dan "Pilihan Jamak (≥2 jawaban benar)" — di form admin (`ManagePackageQuestions.cshtml`), preview (`_PreviewQuestion.cshtml`), exam (`StartExam.cshtml`), dan summary (`ExamSummary.cshtml`). Internal enum/string DB **tidak** diubah. *(maps Temuan 7)*

### Create Assessment Wizard

- [ ] **WIZ-01**: Admin/HC dapat melihat real-time list nama peserta yang sudah dicentang di Step 2 (Pilih Peserta), dengan badge count + truncate "...dan N lainnya" konsisten dengan Step 4 summary. *(maps Temuan 4)*

- [ ] **WIZ-02**: Setiap input waktu di Step 3 wizard (Tanggal/Waktu Jadwal, Tanggal/Waktu Tutup Ujian, Pre-Test datetime, Post-Test datetime, Batas Waktu Pengerjaan Pre/Post) menampilkan label "(WIB)" eksplisit. *(maps Temuan 5)*

- [ ] **WIZ-03**: Step 4 summary menampilkan suffix " WIB" pada baris "Jam Tutup", konsisten dengan baris "Jam Mulai" yang sudah memiliki suffix WIB. PrePost summary juga konsisten. *(maps Temuan 6)*

- [ ] **WIZ-04**: Admin dapat submit assessment Pre-Post Test tanpa error "Status field is required" yang me-reset wizard ke Step 1. JS handler set value `Status='Upcoming'` saat PrePost mode + conditional `ModelState.Remove("Status")` server-side; switching mode Standard ↔ PrePost tidak meninggalkan stale validation state. *(maps Temuan 11)*

### ManageAssessment Performance

- [ ] **PERF-01**: Halaman `/Admin/ManageAssessment` memuat lebih cepat di wifi kantor (yang dimediasi proxy Pertamina) dengan target end-to-end ≤40 detik (≥50% reduction dari baseline ~1.4 menit). Strategi: HTMX lazy load architecture — refactor `ManageAssessment` action menjadi shell-only (no data fetch kecuali cached Categories) + 3 partial actions (`ManageAssessmentTab_Assessment`, `_Training`, `_History`) yang return PartialView per tab. Shell view `Views/Admin/ManageAssessment.cshtml` pakai HTMX attributes (vendored `wwwroot/lib/htmx/htmx.min.js` v2.0.x, ~14 KB) untuk lazy fetch per tab. Plus opportunistic backend wins di Plan 03 (AsNoTracking + IX_AssessmentSessions_LinkedGroupId + IX_AssessmentSessions_ExamWindowCloseDate + IMemoryCache TTL 5min untuk distinct Categories). Acceptance: initial response document <14 KB, end-to-end load wifi kantor ≤40 detik, tab switching ≤2 detik post-initial, smoke test parity (kolom, row count, ordering identik pre/post per tab), TTFB ≤500ms (no regression backend), filter+pagination+ViewBag contract preserved. Source: `.planning/phases/311-manageassessment-performance/311-DESIGN.md` (approved 2026-05-07). *(maps Temuan 3)*

### Essay Scoring & Certification

- [ ] **ESCG-01**: Admin tidak menerima error "session tidak dalam status menunggu penilaian" saat membuka halaman create sertifikasi pada session mix MC/MA/Essay yang sudah Completed. UI menyembunyikan tombol "Create Sertifikasi" jika `Status == "Completed"` && `NomorSertifikat != null`; OR jika tombol di-klik untuk session terminal, menampilkan pesan ramah no-op (bukan error). Idempotent: klik 2x tidak menduplikasi notification atau audit log. *(maps Temuan 9)*

### Worker Certificate View

- [x] **WCRT-01**: Worker yang sudah lulus assessment dengan `GenerateCertificate=true` dapat membuka halaman sertifikat (`/CMP/Certificate/{id}`) tanpa redirect ke 500. Defensif: try-catch di `Certificate` action mirror pattern `CertificatePdf` (baris 2078–2083), structured `_logger.LogError`, null-safe accessor di `Certificate.cshtml` (`Model.User?.FullName ?? "..."`), specific exception catches (DbException, FormatException, NRE). *(maps Temuan 10)*

## Audit Findings 29 April 2026 (Wave 5 — added 2026-04-29)

Empat temuan audit lapangan tambahan yang dilaporkan operator pada 29 April 2026, ditambahkan ke milestone v15.0 sebagai Wave 5.

### Admin Authorization

- [ ] **DEL-01**: Akun **Admin** dapat melakukan full-delete assessment room termasuk yang berstatus `Completed` dan/atau yang sudah ada response peserta. Role **HC** dilarang menghapus assessment Completed atau yang sudah punya response peserta. Implementasi: role tier guard di body method `DeleteAssessment()` & `DeleteAssessmentGroup()` (Authorize attribute existing sudah `Admin, HC`); UI tombol Hapus di `ManageAssessment.cshtml` di-render conditional sesuai role. AuditLog entry sertakan Status & ResponseCount. *(maps Audit-29Apr T1)*

### Exam Integrity

- [x] **TMR-01**: Server **menolak submit manual** worker pada assessment ber-timer (Online, PreTest, PostTest) saat `elapsed > DurationMinutes + ExtraTimeMinutes`; hanya jalur **auto-submit** (`isAutoSubmit=true`) yang sah setelah waktu habis, dengan grace 2 menit untuk network latency. Implementasi: modify LIFE-03 enforcement di `CMPController.SubmitExam()` (line ~1618–1631) jadi 2-tier branching (manual reject tanpa grace; auto reject setelah grace). Frontend `StartExam.cshtml` disable tombol Submit saat countdown=0. Tipe `Manual` exclude. *(maps Audit-29Apr T2)*

### Submitted Status Handling

- [x] **SUB-01**: Status `"Menunggu Penilaian"` (assessment ber-essay yang di-set oleh `GradingService` line 189–227) diperlakukan sebagai status submit yang sah di endpoint `Results()`, `Certificate()`, dan `CertificatePdf()` (`CMPController.cs` line 1792, 1858, 2105). Helper baru `IsAssessmentSubmitted(string status)` di `AssessmentConstants.cs` returns true untuk Completed dan Menunggu Penilaian. Untuk Certificate/CertificatePdf: branch khusus `Menunggu Penilaian` tampilkan TempData Info (bukan Error) dengan pesan ramah. *(maps Audit-29Apr T3, bundled ke Phase 309)*

### Token Regeneration Bug

- [ ] **TKN-01**: `AssessmentAdminController.RegenerateToken()` (line 2232–2280) berhasil meregenerasi token untuk assessment status `Upcoming` dengan `IsTokenRequired=true` dan **0 worker yang sudah masuk ujian**. Investigative: repro bug → identify root cause (hipotesis: NRE pada `Schedule.Date`, AuditLog FK, concurrency, atau frontend response handler) → patch minimal → frontend propagasi error message detail (bukan generik). *(maps Audit-29Apr T4)*

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
| LBL-01 | Phase 305 (Question Type Naming Clarity) | Complete |
| QSCR-01 | Phase 306 (Score Editable per Question Type) | Pending |
| WIZ-01 | Phase 307 (Selected Participants Inline View) | In Progress (Wave 0 scaffold complete, Wave 1 pending) |
| WIZ-04 | Phase 308 (PrePost Wizard Validation Fix) | Pending |
| WCRT-01 | Phase 309 (Worker Certificate Defensive Fix + Submitted Status Handling) | Complete (2026-05-01) |
| ESCG-01 | Phase 310 (Essay Finalize Idempotency) | Pending |
| PERF-01 | Phase 311 (ManageAssessment Performance) | Pending |
| EPRV-01 | DEFERRED (due 2026-05-12) | Pending klarifikasi user |
| DEL-01 | Phase 312 (Admin Full-Delete Assessment Room) | Pending |
| TMR-01 | Phase 313 (Block Manual Submit Saat Waktu Habis) | Complete |
| SUB-01 | Phase 309 (bundled — Submitted Status Handling) | Complete (2026-05-01) |
| TKN-01 | Phase 314 (Fix Regenerate Token Upcoming) | Pending |

**Coverage:**
- v15.0 requirements: 15 total (14 active + 1 deferred) — 11 dari audit 27 April + 4 dari audit 29 April
- Active mapped to phases: **14/14 ✓** (Phase 304-314)
- Deferred (tracked, not phase-mapped): 1 (EPRV-01, due 2026-05-12)
- Coverage 15 temuan audit: **15/15 (100%)**
- Orphans: 0 | Duplicates: 0

---
*Requirements defined: 2026-04-28*
*Source audit: 27 April 2026 (T1–T11) + 29 April 2026 (Wave 5: T1–T4)*
*Last updated: 2026-05-01 (Phase 309 complete — WCRT-01 + SUB-01 marked complete)*
