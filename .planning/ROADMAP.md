# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v5.0** - Phases 1-172 (shipped)
- ✅ **v7.1–v7.12** - Phases 176-222 (shipped)
- ✅ **v8.0–v8.7** - Phases 223-253 (shipped)
- ⏸️ **v9.0 Pre-deployment Audit & Finalization** - Phases 254-256 (deferred)
- ✅ **v9.1 UAT Coaching Proton End-to-End** - Phases 257-261 (shipped 2026-03-25, partial)
- ✅ **Phases 262-263** - Sub-path deployment fixes (shipped 2026-03-27)
- ✅ **v10.0 UAT Assessment OJT di Server Development** - Phases 264-280 (shipped)
- ⏸️ **v11.2 Admin Platform Enhancement** - Phases 281-285 (paused — closed early)
- 🚧 **v12.0 Controller Refactoring** - Phases 286-290 (in progress)

## Phases

<details>
<summary>✅ Previous milestones (v1.0–v10.0, Phases 1-280) — SHIPPED</summary>

See .planning/MILESTONES.md for full history.

</details>

<details>
<summary>⏸️ v11.2 Admin Platform Enhancement (Phases 281-285) — PAUSED</summary>

- [ ] **Phase 281: System Settings** - Admin dapat mengelola konfigurasi aplikasi dari UI
- [x] **Phase 282: Maintenance Mode** - Admin dapat mengaktifkan mode pemeliharaan
- [x] **Phase 283: User Impersonation** - Admin dapat melihat aplikasi dari perspektif role/user lain
- [ ] **Phase 285: Dedicated Impersonation Page** - Halaman admin tersendiri untuk impersonation

</details>

### 🚧 v12.0 Controller Refactoring

- [ ] **Phase 286: AdminBaseController** - Shared base controller dengan DI dan helper methods
- [ ] **Phase 287: AssessmentAdminController** - Ekstraksi semua action assessment dari AdminController
- [ ] **Phase 288: Worker, Coach & Organization Controllers** - Ekstraksi WorkerController, CoachMappingController, OrganizationController
- [ ] **Phase 289: Document, Training & Renewal Controllers** - Ekstraksi DocumentAdminController, TrainingAdminController, RenewalController
- [ ] **Phase 290: Verification & Cleanup** - Validasi semua URL, authorization, dan build bersih

## Phase Details

### Phase 286: AdminBaseController
**Goal**: Fondasi shared base controller tersedia sehingga semua controller domain bisa mewarisi DI dan helper methods tanpa duplikasi
**Depends on**: Nothing (first phase v12.0)
**Requirements**: BASE-01, BASE-02
**Success Criteria** (what must be TRUE):
  1. AdminBaseController ada dengan constructor yang menerima DbContext, UserManager, SignInManager, dan ILogger sebagai shared DI
  2. Helper methods yang dipakai oleh lebih dari satu domain controller sudah dipindahkan ke base class dan bisa diakses oleh subclass
  3. AdminController yang ada masih berfungsi normal setelah mewarisi AdminBaseController (zero regression)
**Plans**: 1 plan
Plans:
- [ ] 286-01-PLAN.md — Buat AdminBaseController + ubah AdminController inherit base

### Phase 287: AssessmentAdminController
**Goal**: Semua action assessment terisolasi di controller tersendiri dengan URL dan behavior yang identik dengan sebelumnya
**Depends on**: Phase 286
**Requirements**: ASMT-01, ASMT-02, ASMT-03
**Success Criteria** (what must be TRUE):
  1. AssessmentAdminController berisi semua action assessment (ManageAssessment, Create, Edit, Delete, Monitoring, Reshuffle, Package, ExportResults, UserHistory, ActivityLog, Categories) dan tidak ada lagi di AdminController
  2. Semua URL assessment (/Admin/ManageAssessment, /Admin/CreateAssessment, dll) tetap bisa diakses tanpa perubahan
  3. Private/helper methods terkait assessment (BuildCrossPackageAssignment, dsb) sudah ikut pindah dan tidak ada referensi broken
  4. Authorization [Authorize(Roles = "Admin")] tetap sama di setiap action
**Plans**: 1 plan
Plans:
- [ ] 286-01-PLAN.md — Buat AdminBaseController + ubah AdminController inherit base

### Phase 288: Worker, Coach & Organization Controllers
**Goal**: Tiga controller domain people-management (WorkerController, CoachMappingController, OrganizationController) terisolasi dengan URL dan behavior identik
**Depends on**: Phase 286
**Requirements**: WKR-01, WKR-02, CCM-01, CCM-02, ORG-01, ORG-02
**Success Criteria** (what must be TRUE):
  1. WorkerController berisi semua action ManageWorkers dan URL /Admin/ManageWorkers, /Admin/CreateWorker, dll tetap bisa diakses
  2. CoachMappingController berisi semua action coach-coachee dan URL /Admin/CoachCoacheeMapping, dll tetap bisa diakses
  3. OrganizationController berisi semua action organization dan URL /Admin/ManageOrganization, dll tetap bisa diakses
  4. Authorization [Authorize(Roles = "Admin, HC")] pada worker actions dan [Authorize(Roles = "Admin")] pada lainnya tetap sama persis
**Plans**: 1 plan
Plans:
- [ ] 286-01-PLAN.md — Buat AdminBaseController + ubah AdminController inherit base

### Phase 289: Document, Training & Renewal Controllers
**Goal**: Tiga controller domain records-management (DocumentAdminController, TrainingAdminController, RenewalController) terisolasi dengan URL dan behavior identik
**Depends on**: Phase 286
**Requirements**: DOC-01, DOC-02, TRN-01, TRN-02, RNW-01, RNW-02
**Success Criteria** (what must be TRUE):
  1. DocumentAdminController berisi semua action KKJ dan CPDP, URL /Admin/KkjMatrix, /Admin/CpdpFiles, dll tetap bisa diakses
  2. TrainingAdminController berisi semua action training, URL /Admin/AddTraining, dll tetap bisa diakses
  3. RenewalController berisi semua action renewal, URL /Admin/RenewalCertificate, dll tetap bisa diakses
  4. Authorization tetap sama persis di setiap action
**Plans**: 1 plan
Plans:
- [ ] 286-01-PLAN.md — Buat AdminBaseController + ubah AdminController inherit base

### Phase 290: Verification & Cleanup
**Goal**: Konfirmasi bahwa seluruh refactoring tidak mengubah behavior apapun — semua URL, authorization, dan fungsi tetap identik
**Depends on**: Phase 287, Phase 288, Phase 289
**Requirements**: VER-01, VER-02, VER-03
**Success Criteria** (what must be TRUE):
  1. Aplikasi build tanpa error dan warning terkait refactoring
  2. Semua URL yang ada sebelum refactoring tetap bisa diakses dan menghasilkan response yang sama
  3. Authorization (role Admin, HC) pada setiap action tetap sama persis — diverifikasi via audit attribute
  4. AdminController asli sudah kosong atau hanya berisi action yang tidak termasuk domain manapun (Index hub, dll)
**Plans**: 1 plan
Plans:
- [ ] 286-01-PLAN.md — Buat AdminBaseController + ubah AdminController inherit base

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 286. AdminBaseController | 0/? | Not started | - |
| 287. AssessmentAdminController | 0/? | Not started | - |
| 288. Worker, Coach & Organization | 0/? | Not started | - |
| 289. Document, Training & Renewal | 0/? | Not started | - |
| 290. Verification & Cleanup | 0/? | Not started | - |
