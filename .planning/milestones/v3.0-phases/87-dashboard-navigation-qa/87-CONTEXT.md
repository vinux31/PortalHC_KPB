# Phase 87: Dashboard & Navigation QA - Context

**Gathered:** 2026-03-05
**Status:** Ready for planning

<domain>
## Phase Boundary

QA semua dashboard (Home/Index, CDP Dashboard kedua tab), flow login, dan navigasi — verifikasi data role-scoped akurat, batas otorisasi enforced, dan operasi bebas bug.

Controllers in scope: HomeController (Index), AccountController (Login, Logout, AccessDenied), CDPController (Dashboard kedua tab), AdminController (AuditLog), section selectors (KkjSectionSelect, CMP/Mapping).

</domain>

<decisions>
## Implementation Decisions

### Test Data & Role Coverage
- Buat seed data baru: Create `SeedDashboardTestData` action di AdminController (seperti pattern SeedAssessmentTestData Phase 90, SeedCoachingTestData Phase 85)
- Idempotent: check existing data sebelum insert, safe to run multiple times
- Full role coverage: Test semua 6 roles — Admin, HC, SrSpv, SectionHead, Coach, Coachee
- Seed data harus mencakup:
  - Users untuk semua 6 roles dengan assignment yang sesuai
  - Assessment sessions di berbagai status (Open, Upcoming, Completed)
  - IDP items di berbagai status (Pending, In Progress, Completed)
  - Proton deliverable progress untuk Coaching Proton stats
  - Training records untuk Assessment Analytics
  - Audit log entries untuk AuditLog page

### Login Flow Testing
- Local auth + inactive block: Test local auth login (email/password) dengan coverage:
  - Happy path login → redirect ke Home
  - Inactive user block (IsActive check di AccountController.Login line 72-76)
  - Return URL redirect after login
- AD path: Code review saja — AD menggunakan IAuthService interface yang sama, logic setelah authenticate identik dengan local path

### Dashboard Data Accuracy
- Deep verification: Trace setiap dashboard stat ke controller query
- Code review + browser spot-check: Verifikasi logic dan role scoping via code review, konfirmasi di browser
- Verify semua metrics di:
  - Home/Index: IDP progress, pending assessments, mandatory training status, recent activities, upcoming deadlines
  - CDP Dashboard Coaching Proton tab: Progress stats per bagian/unit
  - CDP Dashboard Assessment Analytics tab: Assessment dan training data + export
- Role scoping: Verify data filter berdasarkan role dengan benar

### Cross-Feature Verification
- Data accuracy only: Fokus verifikasi akurasi data dashboard
- Tidak perlu round-trip testing ke detail pages — flows tersebut sudah di-test dan lulus di Phases 84/85/90/91
- Dashboard stats yang ditampilkan harus akurat berdasarkan query di controller
- Code review: Trace setiap dashboard metric ke controller query untuk verify role scoping correct

### Bug Fix Approach (sama dengan Phase 83-85)
- Code review dulu → fix bug inline → commit → user verify di browser
- Fix bug <100 baris langsung
- Bug besar (>100 baris): Flag untuk diskusi dulu
- Silent bugs (tidak visible ke user): Fix jika mudah (<20 baris), otherwise log dan skip

### Navigation & Authorization Testing
- Kelola Data menu: Verify hanya visible ke Admin dan HC (guna `User.IsInRole()` di _Layout.cshtml line 64-71)
- Section selectors: Test KkjSectionSelect dan CMP/Mapping dengan section param works untuk Admin/HC
  - KkjSectionSelect: Verify 4 department cards render dan link ke CMP/Kkj?section=
  - CMP/Mapping: Verify section param filters CPDP files correctly
  - Code review untuk verify role gating logic
- AccessDenied page: Verify renders ketika unauthorized user attempt restricted action
- Role-based navigation: Verify menu items yang sesuai visible/hidden per role (CMP, CDP, Kelola Data)

### Claude's Discretion
- Urutan dan grouping dari QA plans
- Skenario testing spesifik dalam setiap flow
- Apakah bug cukup localized untuk fix inline vs flag
- Berapa banyak spot-check metrics yang cukup untuk "deep verification"

</decisions>

<code_context>
## Existing Code Insights

### Key Controllers
- `HomeController.cs`: Index action (line 23-78) builds DashboardHomeViewModel dengan IDP stats, pending assessments, mandatory training, recent activities, upcoming deadlines
- `AccountController.cs`: Login action (line 42-118) dengan IAuthService, inactive user block (line 72-76), profile sync untuk AD mode (line 79-107)
- `CDPController.cs`: Dashboard action dengan 2-tab layout (Coaching Proton + Assessment Analytics)
- `AdminController.cs`: AuditLog action dengan pagination

### Key Views
- `Views/Home/Index.cshtml`: Home dashboard dengan stats cards, activities, deadlines
- `Views/CDP/Dashboard.cshtml`: 2-tab dashboard dengan Coaching Proton dan Assessment Analytics
- `Views/Admin/AuditLog.cshtml`: Paginated audit trail table
- `Views/Account/AccessDenied.cshtml`: Simple error page
- `Views/Shared/_Layout.cshtml`: Navigation dengan Kelola Data gated to Admin/HC (line 64-71)
- `Views/CMP/KkjSectionSelect.cshtml`: Section selector untuk KKJ Matrix

### Established Patterns
- QA pattern dari Phase 83/84/85: Code review → fix bugs → browser verification
- Seed data pattern: `SeedAssessmentTestData` (Phase 90), `SeedCoachingTestData` (Phase 85) — idempotent, check existing sebelum insert
- Role gating: `[Authorize(Roles = "Admin, HC")]` di controller + `User.IsInRole()` di views
- Inactive user block: `IsActive` check di AccountController.Login line 72-76
- Dashboard stats: Server-side query di controller, pass ke view via ViewModel

### Integration Points
- Home/Index dashboard meng-query AssessmentSessions, IdpItems, TrainingRecords, CoachingLogs
- CDP Dashboard meng-query ProtonDeliverableProgress, ProtonTrackAssignment, AssessmentSessions, TrainingRecords
- AuditLog meng-read AuditLog table yang populated oleh admin actions
- Section selectors: KkjSectionSelect untuk KKJ, CMP/Mapping?section= untuk CPDP files

</code_context>

<specifics>
## Specific Ideas

- Seed data creation: Build `SeedDashboardTestData` action following pattern from Phase 90 (SeedAssessmentTestData) dan Phase 85 (SeedCoachingTestData)
- Full role coverage: Test semua 6 roles untuk memastikan dashboard behavior correct di setiap role level
- Deep verification: Trace dashboard metrics ke controller queries untuk verify role scoping correct
- Phases 84/85/90/91 already verified end-to-end flows — Phase 87 fokus pada dashboard accuracy dan navigation, bukan re-test flows yang sudah lulus

</specifics>

<deferred>
## Deferred Ideas

- Account Profile/Settings QA (ACCT-01, ACCT-02) — deferred to v3.1 per REQUIREMENTS.md
- Mobile-responsive testing — future phase
- Performance/load testing — future phase

</deferred>

---

*Phase: 87-dashboard-navigation-qa*
*Context gathered: 2026-03-05*
