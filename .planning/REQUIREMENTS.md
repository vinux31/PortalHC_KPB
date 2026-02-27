# Requirements: Portal HC KPB

**Defined:** 2026-02-26
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v2.3 Requirements — Admin Portal

### Category A: Master Data Managers (seed-only → full UI)

- [x] **MDAT-01**: Admin can view, create, edit, and delete KKJ Matrix items (KkjMatrixItem) through a dedicated management page — no database/code change required
- [x] **MDAT-02**: Admin can view, create, edit, and delete CPDP Items (CpdpItem) with section filter through a dedicated management page
- [x] **MDAT-03**: Admin can view, create, edit, and delete Assessment Competency Maps (AssessmentCompetencyMap) — mapping assessment categories to KKJ items

### Category B: Operational Admin (no admin override existed)

- [x] **OPER-01**: Admin can view, create, edit, and delete Coach-Coachee Mappings (CoachCoacheeMapping) — assign and unassign coaches to coachees
- [x] **OPER-02**: Admin can view, create, edit, and delete Proton Track Assignments (ProtonTrackAssignment) — assign workers to Proton tracks and manage active/inactive state
- [x] **OPER-03**: Admin can view and override ProtonDeliverableProgress status — correct stuck or erroneous deliverable records
- [ ] **OPER-04**: Admin can view, approve, reject, and edit ProtonFinalAssessment records — admin-level management of final assessments
- [ ] **OPER-05**: Admin can view all CoachingSession and ActionItem records and perform override edits or deletions

### Category C: CRUD Completions (partial CRUD → full)

- [ ] **CRUD-01**: Admin/HC can edit existing AssessmentQuestion text and options (Edit was missing — only Add/Delete existed)
- [ ] **CRUD-02**: Admin/HC can edit and delete individual PackageQuestion and PackageOption records (currently import-only, no inline edit/delete)
- [ ] **CRUD-03**: Admin can edit and delete ProtonTrack records (Create existed, Edit/Delete were missing)
- [ ] **CRUD-04**: Admin can reset a worker's password from a standalone action without going through the full EditWorker form

## v2.4 Requirements — CDP Progress

### Data Source

- [x] **DATA-01**: Progress page menampilkan data dari ProtonDeliverableProgress + ProtonTrackAssignment dengan konteks track (Panelman/Operator, Tahun 1/2/3), bukan dari IdpItems
- [x] **DATA-02**: Coach melihat daftar coachee asli dari CoachCoacheeMapping, bukan hardcoded mock data
- [x] **DATA-03**: Summary stats (progress %, pending actions, pending approvals) dihitung dari ProtonDeliverableProgress yang benar
- [x] **DATA-04**: Data di Progress page tersinkron otomatis dengan database — perubahan approval/evidence di Deliverable page langsung terlihat di Progress

### Filter

- [ ] **FILT-01**: HC/Admin bisa filter data per Bagian dan Unit, query benar-benar memfilter data dari database
- [ ] **FILT-02**: Coach bisa memilih coachee dari dropdown dan melihat data deliverable spesifik coachee tersebut
- [ ] **FILT-03**: User bisa filter berdasarkan Proton Track (Panelman/Operator) dan Tahun (1/2/3)
- [ ] **FILT-04**: Search box berfungsi memfilter tabel kompetensi secara client-side

### Actions

- [ ] **ACTN-01**: SrSpv/SectionHead bisa approve deliverable dari Progress page, status tersimpan ke ProtonDeliverableProgress di database
- [ ] **ACTN-02**: SrSpv/SectionHead bisa reject deliverable dari Progress page dengan alasan tertulis
- [ ] **ACTN-03**: Coach bisa submit laporan coaching dari modal, tersimpan sebagai CoachingSession record di database
- [ ] **ACTN-04**: Upload evidence dan lihat evidence di Progress page tersambung ke existing Deliverable workflow
- [ ] **ACTN-05**: Export data progress ke Excel (ClosedXML) dan PDF

### UI Polish

- [ ] **UI-01**: HTML selected attribute pada dropdown filter menggunakan conditional rendering yang benar
- [ ] **UI-02**: Tampilkan pesan empty state ketika tidak ada data deliverable
- [ ] **UI-03**: HC/Admin bisa lihat data semua user lintas section, role-scoped (Spv=unit, SrSpv/SectionHead=section, HC/Admin=all)
- [ ] **UI-04**: Tabel data dipaginasi (server-side atau client-side) agar tidak load semua sekaligus

## v2.5 Requirements — User Infrastructure & AD Readiness

### PROF — Profile & Settings

- [ ] **PROF-01**: Profile page menampilkan data real user login (Nama, NIP, Email, Position, Section, Unit, Directorate, Role, JoinDate)
- [ ] **PROF-02**: Field kosong menampilkan placeholder "Belum diisi", bukan blank/error
- [ ] **PROF-03**: Avatar initials dinamis dari FullName user (bukan hardcoded "BS")
- [ ] **PROF-04**: Settings page: Change Password functional via ChangePasswordAsync
- [ ] **PROF-05**: Settings page: User bisa edit FullName dan Position; NIP/Email/Role/Section read-only
- [ ] **PROF-06**: Item non-functional (2FA, Notifications, Language) dihapus atau di-mark "Belum Tersedia" disabled

### AUTH — Authentication

- [ ] **AUTH-01**: Config toggle `Authentication:UseActiveDirectory` di appsettings.json (dev=false, prod=true)
- [ ] **AUTH-02**: `IAuthService` interface + `LdapAuthService` menggunakan DirectoryEntry ke `LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com` dengan samaccountname filter
- [ ] **AUTH-03**: `LocalAuthService` implementation wrapping existing PasswordSignInAsync
- [ ] **AUTH-04**: Program.cs register IAuthService berdasarkan config toggle via DI
- [ ] **AUTH-05**: Login page: "Username" + placeholder NIP (AD mode), "Email" + placeholder email (local mode)
- [ ] **AUTH-06**: First-time AD user auto-provisioned di DB lokal: role Coachee, RoleLevel=6, SelectedView="Coachee", AuthSource="AD"
- [ ] **AUTH-07**: Existing AD user: sync FullName/NIP/Position/Section dari AD; Role dan SelectedView TIDAK pernah diubah
- [ ] **AUTH-08**: NuGet package System.DirectoryServices ditambahkan ke csproj

### USR — Admin User Management

- [ ] **USR-01**: ManageWorkers CRUD (list, create, edit, delete, import, export, detail) accessible dari /Admin/ManageWorkers
- [ ] **USR-02**: Old /CMP/ManageWorkers redirect 301 ke /Admin/ManageWorkers
- [ ] **USR-03**: Standalone "Kelola Pekerja" button di navbar dihapus — akses via Kelola Data hub
- [ ] **USR-04**: Kelola Data hub di-reorganize: ManageWorkers card prominent, stale "Segera" items cleaned up

### USTR — User Structure

- [ ] **USTR-01**: ApplicationUser punya field AuthSource ("Local"/"AD") + EF migration
- [ ] **USTR-02**: Role-to-SelectedView mapping di-extract ke shared helper UserRoles.GetDefaultView()

## Future Requirements

*(None captured yet)*

## Out of Scope

| Feature | Reason |
|---------|--------|
| Notifications manager | System-generated, low admin value for now |
| UserCompetencyLevel admin override | System-calculated — manual override risks data integrity |
| AssessmentAttemptHistory admin CRUD | Append-only audit trail by design |
| Role management page (add/remove IdentityRoles) | 9 roles are fixed by design, no need to add/remove |
| User activity summary (last login, sessions) | Not requested, out of scope |
| AuditLog edit/delete | Append-only by design |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| MDAT-01 | Phase 47 | Complete |
| MDAT-02 | Phase 48 | Complete |
| MDAT-03 | Phase 49 | Complete |
| OPER-01 | Phase 50 | Complete |
| OPER-02 | Phase 51 | Complete |
| OPER-03 | Phase 52 | Complete |
| OPER-04 | Phase 53 | Pending |
| OPER-05 | Phase 54 | Pending |
| CRUD-01 | Phase 55 | Pending |
| CRUD-02 | Phase 56 | Pending |
| CRUD-03 | Phase 57 | Pending |
| CRUD-04 | Phase 58 | Pending |
| DATA-01 | Phase 63 | Complete |
| DATA-02 | Phase 63 | Complete |
| DATA-03 | Phase 63 | Complete |
| DATA-04 | Phase 63 | Complete |
| FILT-01 | Phase 64 | Pending |
| FILT-02 | Phase 64 | Pending |
| FILT-03 | Phase 64 | Pending |
| FILT-04 | Phase 64 | Pending |
| ACTN-01 | Phase 65 | Pending |
| ACTN-02 | Phase 65 | Pending |
| ACTN-03 | Phase 65 | Pending |
| ACTN-04 | Phase 65 | Pending |
| ACTN-05 | Phase 65 | Pending |
| UI-01   | Phase 64 | Pending |
| UI-02   | Phase 66 | Pending |
| UI-03   | Phase 64 | Pending |
| UI-04   | Phase 66 | Pending |
| PROF-01 | Phase 67 | Pending |
| PROF-02 | Phase 67 | Pending |
| PROF-03 | Phase 67 | Pending |
| PROF-04 | Phase 68 | Pending |
| PROF-05 | Phase 68 | Pending |
| PROF-06 | Phase 68 | Pending |
| AUTH-01 | Phase 71 | Pending |
| AUTH-02 | Phase 71 | Pending |
| AUTH-03 | Phase 71 | Pending |
| AUTH-04 | Phase 71 | Pending |
| AUTH-05 | Phase 72 | Pending |
| AUTH-06 | Phase 72 | Pending |
| AUTH-07 | Phase 72 | Pending |
| AUTH-08 | Phase 71 | Pending |
| USR-01  | Phase 69 | Pending |
| USR-02  | Phase 69 | Pending |
| USR-03  | Phase 69 | Pending |
| USR-04  | Phase 70 | Pending |
| USTR-01 | Phase 71 | Pending |
| USTR-02 | Phase 69, 73 | Pending |

**v2.3 Coverage:**
- v2.3 requirements: 12 total
- Mapped to phases: 12
- Unmapped: 0 ✓

**v2.4 Coverage:**
- v2.4 requirements: 17 total
- Mapped to phases: 17
- Unmapped: 0 ✓

**v2.5 Coverage:**
- v2.5 requirements: 20 total (PROF: 6, AUTH: 8, USR: 4, USTR: 2)
- Mapped to phases: 20
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-26*
*Last updated: 2026-02-27 after v2.5 User Infrastructure & AD Readiness milestone setup (phases 67-73)*
