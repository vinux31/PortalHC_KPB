# Requirements: PortalHC KPB — v12.0 Controller Refactoring

**Defined:** 2026-04-02
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v12.0 Requirements

Requirements untuk milestone v12.0. Pure refactoring — zero fitur baru, zero perubahan UI.

### Base Infrastructure

- [ ] **BASE-01**: AdminBaseController dibuat dengan shared DI (ApplicationDbContext, UserManager, AuditLogService, IWebHostEnvironment) — tanpa helper methods (hanya DI)
- [ ] **BASE-02**: Semua controller baru mewarisi AdminBaseController dan bisa mengakses shared dependencies tanpa duplikasi constructor

### Assessment Admin

- [x] **ASMT-01**: AssessmentAdminController berisi semua action assessment (ManageAssessment, Create, Edit, Delete, Monitoring, Reshuffle, Package, ExportResults, UserHistory, ActivityLog, Categories)
- [x] **ASMT-02**: Semua URL assessment tetap sama (/Admin/ManageAssessment, /Admin/CreateAssessment, dll) via [Route] attribute
- [x] **ASMT-03**: Helper methods dan private methods terkait assessment ikut pindah ke AssessmentAdminController

### Worker Management

- [x] **WKR-01**: WorkerController berisi semua action ManageWorkers (list, create, edit, delete, deactivate, reactivate, detail, import, export, download template)
- [x] **WKR-02**: Semua URL worker tetap sama (/Admin/ManageWorkers, /Admin/CreateWorker, dll) via [Route] attribute

### Coach-Coachee Mapping

- [x] **CCM-01**: CoachMappingController berisi semua action coach-coachee (mapping list, assign, edit, delete, import, export, deactivate, reactivate, mark completed, get eligible coachees)
- [x] **CCM-02**: Semua URL mapping tetap sama (/Admin/CoachCoacheeMapping, dll) via [Route] attribute

### Document Management

- [x] **DOC-01**: DocumentAdminController berisi semua action KKJ (KkjMatrix, KkjUpload, KkjFileDownload, KkjFileDelete, KkjFileHistory, KkjBagianAdd, DeleteBagian) dan CPDP (CpdpFiles, CpdpUpload, CpdpFileDownload, CpdpFileArchive, CpdpFileHistory)
- [x] **DOC-02**: Semua URL dokumen tetap sama (/Admin/KkjMatrix, /Admin/CpdpFiles, dll) via [Route] attribute

### Training Records

- [x] **TRN-01**: TrainingAdminController berisi semua action training (AddTraining, EditTraining, DeleteTraining, ImportTraining, DownloadImportTrainingTemplate)
- [x] **TRN-02**: Semua URL training tetap sama (/Admin/AddTraining, dll) via [Route] attribute

### Renewal Certificate

- [x] **RNW-01**: RenewalController berisi semua action renewal (RenewalCertificate, FilterRenewalCertificate, FilterRenewalCertificateGroup, CertificateHistory) dan helper methods terkait
- [x] **RNW-02**: Semua URL renewal tetap sama (/Admin/RenewalCertificate, dll) via [Route] attribute

### Organization Management

- [x] **ORG-01**: OrganizationController berisi semua action organization (ManageOrganization, Add, Edit, Toggle, Delete, Reorder)
- [x] **ORG-02**: Semua URL organization tetap sama (/Admin/ManageOrganization, dll) via [Route] attribute

### Verification

- [ ] **VER-01**: Semua URL yang ada sebelum refactoring tetap bisa diakses tanpa perubahan
- [ ] **VER-02**: Authorization (role Admin, HC) tetap sama persis di setiap action
- [ ] **VER-03**: Aplikasi build tanpa error dan semua halaman berfungsi normal

## Out of Scope

| Feature | Reason |
|---------|--------|
| Fitur baru | Milestone ini pure refactoring |
| Perubahan UI | Tidak ada perubahan tampilan |
| Service layer extraction | Refactoring lebih dalam — ditunda ke milestone berikutnya |
| Pecah CMPController / CDPController | Fokus AdminController dulu yang paling besar |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| BASE-01 | Phase 286 | Pending |
| BASE-02 | Phase 286 | Pending |
| ASMT-01 | Phase 287 | Complete |
| ASMT-02 | Phase 287 | Complete |
| ASMT-03 | Phase 287 | Complete |
| WKR-01 | Phase 288 | Complete |
| WKR-02 | Phase 288 | Complete |
| CCM-01 | Phase 288 | Complete |
| CCM-02 | Phase 288 | Complete |
| ORG-01 | Phase 288 | Complete |
| ORG-02 | Phase 288 | Complete |
| DOC-01 | Phase 289 | Complete |
| DOC-02 | Phase 289 | Complete |
| TRN-01 | Phase 289 | Complete |
| TRN-02 | Phase 289 | Complete |
| RNW-01 | Phase 289 | Complete |
| RNW-02 | Phase 289 | Complete |
| VER-01 | Phase 290 | Pending |
| VER-02 | Phase 290 | Pending |
| VER-03 | Phase 290 | Pending |

---
*Requirements defined: 2026-04-02*
*Last updated: 2026-04-02 after plan-phase revision (BASE-01 DI list aligned with CONTEXT.md D-01)*
