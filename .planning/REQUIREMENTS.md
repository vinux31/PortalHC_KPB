# Requirements: Portal HC KPB — v8.6

**Defined:** 2026-03-24
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v8.6 Requirements

Requirements untuk Codebase Audit & Hardening. Setiap bug di-fix dengan atomic commit.

### Null Safety & Input Validation

- [ ] **SAFE-01**: Null check pada `GetCurrentUserRoleLevelAsync()` — return error/redirect jika user null, mencegah crash 5 halaman CMP
- [ ] **SAFE-02**: Ganti `DateTime.Parse()` ke `DateTime.TryParse()` di 3 action CMP (ExportRecordsTeamAssessment, ExportRecordsTeamTraining, RecordsTeamPartial)
- [ ] **SAFE-03**: Guard `ToDictionary` key collision di bulk renewal `sourceSessions` dan `sourceTrainings` (AdminController)
- [ ] **SAFE-04**: Null-safe `Model.FullName` di WorkerDetail.cshtml — tambah `?? ""` fallback
- [ ] **SAFE-05**: Safe cast `ViewBag.UnansweredCount` dan `ViewBag.AssessmentId` di ExamSummary.cshtml — ganti hard cast `(int)` dengan null-safe pattern

### Data Integrity & Logic

- [ ] **DATA-01**: Ganti `DateTime.Now` ke `DateTime.UtcNow` di `TrainingRecord.IsExpiringSoon`, `DaysUntilExpiry`, dan `CertificationManagementViewModel.DeriveCertificateStatus`
- [ ] **DATA-02**: Ubah unique index `OrganizationUnit.Name` ke composite `(ParentId, Name)` dan `AssessmentCategory.Name` ke `(ParentId, Name)` via migration
- [ ] **DATA-03**: Validasi `ValidUntil` wajib diisi untuk bulk renewal — fix `isRenewalModePost` detection di CreateAssessment POST
- [ ] **DATA-04**: Allow edit assessment yang sudah lewat jadwalnya — relax validasi past date di EditAssessment POST
- [ ] **DATA-05**: Log warning pada catch block `RenewalFkMap` deserialize alih-alih silent ignore
- [ ] **DATA-06**: Refactor `_lastScopeLabel` dari instance field ke return value/parameter agar thread-safe

### Security & Performance

- [ ] **SEC-01**: Hapus semua `console.log` yang mengekspos token/response di Assessment.cshtml (4 lokasi)
- [ ] **SEC-02**: Escape `approverName` di `GetApprovalBadgeWithTooltip` CoachingProton.cshtml — ganti `@Html.Raw` dengan HTML-encoded output
- [ ] **SEC-03**: Aktifkan minimal password policy (RequireDigit + RequireUppercase + RequiredLength=8), bungkus policy lemah dalam environment check
- [ ] **PERF-01**: Throttle `TriggerCertExpiredNotificationsAsync` — jalankan maksimal 1x per jam via IMemoryCache, bukan setiap page load dashboard

### UI & Annotations

- [ ] **UI-01**: Definisikan `.bg-purple` di CSS global agar badge Proton tampil benar di AssessmentMonitoring
- [ ] **UI-02**: Tambah `[MaxLength]` pada string fields `TrainingRecord` yang belum punya (Judul, Kategori, Penyelenggara, Status, SertifikatUrl, CertificateType, NomorSertifikat, Kota, SubKategori)
- [ ] **UI-03**: Tambah `[Range(0, 5)]` pada `ProtonFinalAssessment.CompetencyLevelGranted`

## Future Requirements

Tidak ada — milestone ini murni bug fix dari audit.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Fitur baru | Milestone ini hanya bug fix & hardening |
| v8.5 UAT execution | Akan dieksekusi setelah v8.6 selesai |
| Refactor arsitektur besar | Fix minimal & targeted, bukan redesign |
| Password migration user existing | Policy baru hanya berlaku saat change password |
| Low-severity bugs (4 item) | Null-forgiving NotificationController, bare catch audit log, SeedData Console.WriteLine, copyright hardcoded — risiko sangat rendah |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SAFE-01 | — | Pending |
| SAFE-02 | — | Pending |
| SAFE-03 | — | Pending |
| SAFE-04 | — | Pending |
| SAFE-05 | — | Pending |
| DATA-01 | — | Pending |
| DATA-02 | — | Pending |
| DATA-03 | — | Pending |
| DATA-04 | — | Pending |
| DATA-05 | — | Pending |
| DATA-06 | — | Pending |
| SEC-01 | — | Pending |
| SEC-02 | — | Pending |
| SEC-03 | — | Pending |
| PERF-01 | — | Pending |
| UI-01 | — | Pending |
| UI-02 | — | Pending |
| UI-03 | — | Pending |

**Coverage:**
- v8.6 requirements: 18 total
- Mapped to phases: 0
- Unmapped: 18 ⚠️

---
*Requirements defined: 2026-03-24*
*Last updated: 2026-03-24 after initial definition*
