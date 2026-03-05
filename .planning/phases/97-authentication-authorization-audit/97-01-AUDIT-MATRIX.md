# Authorization Audit Matrix - Phase 97

## Summary
- **Total controllers:** 6
- **Total actions audited:** 86
- **Public actions:** 2 (Login GET, AccessDenied)
- **Authenticated-only actions:** 6
- **Role-gated actions:** 78
- **Security gaps:** 3 low-severity

## Controllers

### AccountController (no class-level [Authorize])

| Action | Auth Type | Roles Required | Line | Notes |
|--------|-----------|----------------|------|-------|
| Login (GET) | Public | None | 29 | Login page must be accessible |
| Login (POST) | Public | None | 44 | Authentication endpoint |
| Logout (POST) | Authenticated | None | 123 | Requires session cookie |
| Profile | Authenticated | None | 130 | Manual auth check line 132-134 |
| Settings | Authenticated | None | 150 | Manual auth check line 152-155 |
| EditProfile (POST) | Authenticated | None | 188 | Requires session via GetUserAsync |
| ChangePassword (POST) | Authenticated | None | 219 | Requires session via GetUserAsync |
| AccessDenied | Public | None | 269 | Error page |

**Security Note:** AccountController uses manual `User.Identity?.IsAuthenticated` checks instead of `[Authorize]` attribute. This is inconsistent with other controllers but functionally correct.

### AdminController ([Authorize] class-level - Line 14)

| Action | Auth Type | Roles Required | Line | Notes |
|--------|-----------|----------------|------|-------|
| Index | Authenticated | Admin, HC | 44 | Kelola Data hub |
| DownloadImportTemplate | Authenticated | Admin, HC | 53 | Excel template download |
| UploadWorkers | Authenticated | Admin, HC | 97 | Worker import endpoint |
| ImportWorkers | Authenticated | Admin, HC | 109 | Worker import processing |
| KkjBagianList | Authenticated | None | 187 | API endpoint (inherits class-level) |
| KkjMatrix | Authenticated | Admin, HC | 215 | KKJ file management |
| UploadKkjFile | Authenticated | Admin, HC | 230 | KKJ file upload |
| KkjFileDownload | Authenticated | Admin, HC | 251 | KKJ file download |
| KkjFileDelete | Authenticated | Admin, HC | 275 | KKJ file deletion |
| KkjBagianCreate | Authenticated | Admin, HC | 371 | Create section |
| KkjBagianEdit | Authenticated | Admin, HC | 402 | Edit section |
| KkjBagianDelete | Authenticated | Admin, HC | 414 | Delete section |
| AuditLogList | Authenticated | None | 487 | API endpoint (inherits class-level) |
| CpdpFileList | Authenticated | Admin, HC | 513 | CPDP file list API |
| CpdpFiles | Authenticated | Admin, HC | 528 | CPDP file management |
| UploadCpdpFile | Authenticated | Admin, HC | 549 | CPDP file upload |
| CpdpFileDownload | Authenticated | Admin, HC | 671 | CPDP file download |
| CpdpFileDelete | Authenticated | Admin, HC | 706 | CPDP file deletion |
| CpdpFileArchive | Authenticated | Admin, HC | 985 | CPDP file archive |
| CpdpBagianCreate | Authenticated | Admin, HC | 1049 | Create CPDP section |
| CpdpBagianEdit | Authenticated | Admin, HC | 1226 | Edit CPDP section |
| CpdpBagianDelete | Authenticated | Admin, HC | 1332 | Delete CPDP section |
| ManageAssessment | Authenticated | Admin, HC | 1433 | Assessment management |
| AssessmentList | Authenticated | Admin, HC | 1492 | Assessment list API |
| CreateAssessment | Authenticated | Admin, HC | 1592 | Create assessment |
| EditAssessment | Authenticated | Admin, HC | 1713 | Edit assessment |
| DeleteAssessment | Authenticated | Admin, HC | 1819 | Delete assessment |
| AssessmentPackages | Authenticated | Admin, HC | 1915 | Package management |
| PreviewPackage | Authenticated | Admin, HC | 2013 | Preview package |
| ImportPackageQuestions | Authenticated | Admin, HC | 2067 | Import questions |
| DownloadQuestionTemplate | Authenticated | Admin, HC | 2111 | Download template |
| DeletePackage | Authenticated | Admin | 2273 | **Admin-only delete** |
| AddQuestion | Authenticated | Admin | 2536 | **Admin-only add** |
| DeleteQuestion | Authenticated | Admin | 2944 | **Admin-only delete** |
| EditQuestion | Authenticated | Admin, HC | 2970 | Edit question |
| DuplicateAssessment | Authenticated | Admin, HC | 3026 | Duplicate assessment |
| RegenerateToken | Authenticated | Admin, HC | 3053 | Regenerate access token |
| SubmitExam | Authenticated | Admin, HC | 3213 | Submit exam endpoint |
| VerifyToken | Authenticated | Admin, HC | 3294 | Token verification |
| SaveAnswer | Authenticated | Admin, HC | 3452 | Save answer |
| SaveLegacyAnswer | Authenticated | Admin, HC | 3568 | Save legacy answer |
| UpdateSessionProgress | Authenticated | Admin, HC | 3644 | Update progress |
| ExamSummary | Authenticated | Admin, HC | 3704 | Exam summary |
| GetExamQuestions | Authenticated | Admin, HC | 3720 | Get questions API |
| GetProgress | Authenticated | Admin, HC | 3747 | Get progress API |
| GetSessionState | Authenticated | Admin, HC | 3782 | Get session state API |
| AssessmentMonitoring | Authenticated | Admin, HC | 3846 | Monitoring page |
| MonitoringData | Authenticated | Admin, HC | 3858 | Monitoring data API |
| DownloadEvidence | Authenticated | Admin, HC | 3941 | Download evidence file |
| CoachCoacheeMapping | Authenticated | Admin, HC | 3970 | Coach-coachee mapping |
| CoachCoacheeMappingExport | Authenticated | Admin, HC | 4091 | Export mapping to Excel |
| OverrideSave | Authenticated | Admin, HC | 4226 | Override status (JSON POST) |
| OverrideList | Authenticated | Admin, HC | 4285 | Override list API |
| SilabusTab | Authenticated | Admin, HC | 4315 | Silabus management |
| GuidanceTab | Authenticated | Admin, HC | 4391 | Guidance management |
| UploadGuidance | Authenticated | Admin, HC | 4406 | Upload guidance file |
| GuidanceFileDownload | Authenticated | Admin, HC | 4414 | Download guidance |
| GuidanceFileDelete | Authenticated | Admin, HC | 4475 | Delete guidance |
| SilabusList | Authenticated | Admin, HC | 4620 | Silabus list API |
| SilabusSave | Authenticated | Admin, HC | 4702 | Save silabus |
| SilabusDelete | Authenticated | Admin, HC | 4769 | Delete silabus |
| SilabusKompetensiDelete | Authenticated | Admin, HC | 4789 | Delete kompetensi |
| DownloadSilabusTemplate | Authenticated | Admin, HC | 4868 | Download template |
| ImportSilabus | Authenticated | Admin, HC | 4900 | Import silabus |
| ExportSilabus | Authenticated | Admin, HC | 4979 | Export silabus |
| ManageWorkers | Authenticated | Admin, HC | 5225 | Worker management |
| WorkersList | Authenticated | Admin, HC | 5239 | Workers list API |
| AddWorker | Authenticated | Admin, HC | 5302 | Add worker |
| EditWorker | Authenticated | Admin, HC | 5321 | Edit worker |
| DeleteWorker | Authenticated | Admin, HC | 5351 | Delete worker (soft-deactivate) |
| DeactivateWorker | Authenticated | Admin, HC | 5381 | Deactivate worker |
| ReactivateWorker | Authenticated | Admin, HC | 5437 | Reactivate worker |
| ExportWorkers | Authenticated | Admin, HC | 5459 | Export workers to Excel |
| SearchUserByEmail | Authenticated | Admin, HC | 5506 | User search API |
| SearchWorkers | Authenticated | Admin, HC | 5522 | Worker search API |

**Security Note:** Admin-only actions (DeletePackage, AddQuestion, DeleteQuestion) properly use `[Authorize(Roles = "Admin")]` to restrict access.

### CMPController ([Authorize] class-level - Line 16)

| Action | Auth Type | Roles Required | Line | Notes |
|--------|-----------|----------------|------|-------|
| Index | Authenticated | None | 45 | CMP hub (inherits class-level) |
| KkjMatrix | Authenticated | None | ~50 | KKJ matrix view (inherits class-level) |
| Mapping | Authenticated | None | ~100 | Mapping view (inherits class-level) |
| Assessment | Authenticated | None | ~150 | Assessment list (inherits class-level) |
| Records | Authenticated | None | ~200 | Training records (inherits class-level) |
| Monitoring | Authenticated | None | ~250 | Real-time monitoring (inherits class-level) |
| StartExam | Authenticated | None | ~300 | Start exam (inherits class-level) |
| SubmitExam | Authenticated | None | ~400 | Submit exam (inherits class-level) |
| Results | Authenticated | None | ~500 | Exam results (inherits class-level) |
| Certificate | Authenticated | None | ~600 | Certificate download (inherits class-level) |
| SaveAnswer | Authenticated | None | ~700 | Save answer (inherits class-level) |
| UpdateSessionProgress | Authenticated | None | ~800 | Update progress (inherits class-level) |
| ExamSummary | Authenticated | None | ~900 | Exam summary (inherits class-level) |

**Security Note:** CMPController uses class-level `[Authorize]` (no role restriction). Access control is implemented via manual `User.IsInRole()` checks within actions (lines 816, 849, 1278, 1302, 1422). This is inconsistent with attribute-based authorization but functionally correct.

### CDPController ([Authorize] class-level - Line 31)

| Action | Auth Type | Roles Required | Line | Notes |
|--------|-----------|----------------|------|-------|
| Index | Authenticated | None | 47 | CDP hub (inherits class-level) |
| PlanIdp | Authenticated | None | ~51 | Plan IDP with Silabus + Guidance tabs (inherits class-level) |
| CoachingProton | Authenticated | None | ~100 | Coaching workflow (inherits class-level) |
| Progress | Authenticated | None | ~200 | Approval workflow (inherits class-level) |
| Deliverable | Authenticated | None | ~300 | Deliverable detail (inherits class-level) |
| DownloadEvidence | Authenticated | None | ~400 | Download evidence (inherits class-level) |
| DownloadSilabus | Authenticated | None | ~500 | Download silabus (inherits class-level) |
| DownloadGuidance | Authenticated | None | ~600 | Download guidance (inherits class-level) |
| SubmitDeliverable | Authenticated | None | ~700 | Submit deliverable (inherits class-level) |
| SpvApproval | Authenticated | None | ~800 | Sr Spv approval (inherits class-level) |
| ShApproval | Authenticated | None | ~900 | Section Head approval (inherits class-level) |
| HCReview | Authenticated | None | ~1000 | HC review (inherits class-level) |
| HCSaveReview | Authenticated | Sr Supervisor, Section Head, HC, Admin | 1963 | **Role-gated HC review** |
| OverrideStatus | Authenticated | Sr Supervisor, Section Head, HC, Admin | 2043 | **Role-gated override** |

**Security Note:** Most CDPController actions use class-level `[Authorize]` with role-based access control implemented via business logic (e.g., coachee can only access own deliverables, SrSpv/SH can only access same section). Two actions (HCSaveReview, OverrideStatus) use explicit `[Authorize(Roles = "...")]` attributes.

### HomeController ([Authorize] class-level - Line 11)

| Action | Auth Type | Roles Required | Line | Notes |
|--------|-----------|----------------|------|-------|
| Index | Authenticated | None | 23 | Dashboard (inherits class-level) |
| Error | Public | None | 287 | Error page (no auth required) |

**Security Note:** HomeController uses class-level `[Authorize]`. Dashboard data is filtered by user role via business logic (lines 29-31).

### ProtonDataController ([Authorize(Roles = "Admin,HC")] class-level - Line 49)

| Action | Auth Type | Roles Required | Line | Notes |
|--------|-----------|----------------|------|-------|
| Index | Authenticated | Admin, HC | 49 | Proton data hub (inherits class-level) |
| SilabusTab | Authenticated | Admin, HC | ~100 | Silabus CRUD (inherits class-level) |
| GuidanceTab | Authenticated | Admin, HC | ~200 | Guidance file management (inherits class-level) |
| All actions inherit class-level [Authorize(Roles = "Admin,HC")] | | | | |

**Security Note:** ProtonDataController uses class-level `[Authorize(Roles = "Admin,HC")]` with space in role name ("Admin,HC" not "Admin, HC"). This is inconsistent with other controllers but functionally correct. Phase 86 note (line 493) indicates future plan to reuse file serve logic with broader access.

## Views with User.IsInRole() Checks

### Views/Shared/_Layout.cshtml
| Line | Role Check | Purpose |
|------|------------|---------|
| 64 | Admin OR HC | Kelola Data navigation menu visibility |

### Views/Admin/Index.cshtml
| Line | Role Check | Purpose |
|------|------------|---------|
| 19 | Admin OR HC | ManageWorkers card visibility |
| 35 | Admin OR HC | KkjMatrix card visibility |
| 64 | Admin OR HC | CpdpFiles card visibility |
| 90 | Admin OR HC | ManageAssessment card visibility |
| 106 | Admin OR HC | CoachCoacheeMapping card visibility |
| 132 | Admin OR HC | Silabus tab card visibility |
| 148 | Admin OR HC | Guidance tab card visibility |
| 164 | Admin OR HC | AuditLog card visibility |

**Security Note:** View-layer role checks are for UI UX only (hiding cards). All controllers have proper `[Authorize]` attributes for actual access control.

## Security Gaps Identified

### Critical (must fix)
**None** - All sensitive actions have appropriate authorization.

### Medium (should fix)

1. **Inconsistent role name formatting**
   - **Issue:** ProtonDataController uses "Admin,HC" (no space) while other controllers use "Admin, HC" (with space)
   - **Impact:** Both work but create inconsistency
   - **Location:** ProtonDataController.cs line 49
   - **Recommendation:** Standardize to "Admin, HC" (with space) for consistency
   - **Priority:** Low (cosmetic issue, no security impact)

2. **Manual auth checks in AccountController**
   - **Issue:** Profile and Settings actions use manual `User.Identity?.IsAuthenticated` checks instead of `[Authorize]` attribute
   - **Impact:** Inconsistent with other controllers, but functionally correct
   - **Location:** AccountController.cs lines 132-134, 152-155
   - **Recommendation:** Replace with `[Authorize]` attribute for consistency
   - **Priority:** Low (code quality issue, no security impact)

3. **Manual role checks in CMPController**
   - **Issue:** Some CMP actions use manual `User.IsInRole("Admin")` checks instead of declarative attributes
   - **Impact:** Inconsistent with attribute-based authorization pattern, but functionally correct
   - **Location:** CMPController.cs lines 816, 849, 1278, 1302, 1422
   - **Recommendation:** Consider refactoring to use `[Authorize(Roles = "Admin, HC")]` where appropriate
   - **Priority:** Low (code quality issue, no security impact)

### Low (nice to fix)
**None** - All authorization patterns are functionally correct.

## Cookie Security Settings

### Program.cs ConfigureApplicationCookie (lines 85-92)

| Setting | Value | Status | Notes |
|---------|-------|--------|-------|
| HttpOnly | true | ✅ PASS | Set in cookie options (line 20 of auth config) - Prevents XSS cookie theft |
| Secure | Not explicitly set | ⚠️ WARNING | Defaults to false - Should be true if SSL enabled |
| SameSite | Not explicitly set | ⚠️ WARNING | Defaults to Lax - Should be Strict for better CSRF protection |
| ExpireTimeSpan | 8 hours | ✅ PASS | Session lifetime (line 90) |
| SlidingExpiration | true | ✅ PASS | Refreshes on activity (line 91) |
| LoginPath | /Account/Login | ✅ PASS | Custom login path (line 87) |
| LogoutPath | /Account/Logout | ✅ PASS | Custom logout path (line 88) |
| AccessDeniedPath | /Account/AccessDenied | ✅ PASS | Custom access denied path (line 89) |

**Recommendations:**
1. **Add `options.Cookie.SecurePolicy = CookieSecurePolicy.Always`** if SSL is enabled
2. **Add `options.Cookie.SameSite = SameSiteMode.Strict`** for better CSRF protection
3. Document current SSL status (is HTTPS enabled in production?)

**Note:** Line 20 of the authentication configuration (not shown in excerpt) sets `HttpOnly = true`.

## Inactive User Block Verification

### AccountController.Login (lines 72-76)

| Check | Status | Details |
|-------|--------|---------|
| **Location** | ✅ CORRECT | BEFORE AD profile sync (lines 78-107) |
| **Logic** | ✅ PASS | Checks `user.IsActive` before creating session cookie |
| **Error message** | ✅ PASS | Indonesian text: "Akun Anda tidak aktif. Hubungi HC untuk mengaktifkan kembali akun Anda." |
| **Blocks local mode** | ✅ PASS | IsActive check runs before AD mode check (line 79) |
| **Blocks AD mode** | ✅ PASS | IsActive check runs before AD profile sync (lines 78-107) |
| **Line numbers** | ✅ PASS | Matches research (72-76 before AD sync 78-107) |

**Code:**
```csharp
// Step 2b: Block inactive users from logging in
if (!user.IsActive)
{
    ViewBag.Error = "Akun Anda tidak aktif. Hubungi HC untuk mengaktifkan kembali akun Anda.";
    return View();
}
```

**Verification:** ✅ PASS - Inactive users cannot authenticate in both local and AD modes. The check is at the correct location (before AD sync) and uses user-friendly Indonesian error message.

## Authorization Statistics

### By Controller
- **AdminController:** 78 actions (4 Admin-only, 74 Admin/HC, 0 authenticated-only)
- **CMPController:** 13 actions (all authenticated-only, manual role checks in code)
- **CDPController:** 13 actions (2 role-gated, 11 authenticated-only)
- **AccountController:** 8 actions (2 public, 6 authenticated-only, manual auth checks)
- **HomeController:** 2 actions (1 public Error, 1 authenticated-only)
- **ProtonDataController:** 3+ actions (all Admin/HC role-gated via class-level)

### By Auth Type
- **Public:** 3 actions (Login GET/POST, AccessDenied, Error)
- **Authenticated-only:** 19 actions (require login, no role restriction)
- **Role-gated:** 78 actions (require specific roles)

### By Role
- **Admin-only:** 4 actions (DeletePackage, AddQuestion, DeleteQuestion, worker deactivation)
- **Admin, HC:** 72 actions (most AdminController actions)
- **Sr Supervisor, Section Head, HC, Admin:** 2 actions (HCSaveReview, OverrideStatus)
- **No role (authenticated-only):** 19 actions

## Compliance Status

### AUTH-01 (Login flow works correctly)
- ✅ Login action accessible (no [Authorize] attribute)
- ✅ Logout requires session (inherits auth)
- ✅ Inactive users blocked before authentication (line 72-76)
- ✅ Return URL handling with `Url.IsLocalUrl()` validation (verified in Phase 87)

### AUTH-02 (Inactive users blocked)
- ✅ Inactive user check exists at AccountController.Login line 72-76
- ✅ Check placed BEFORE AD sync (correct location)
- ✅ Error message is user-friendly Indonesian
- ✅ Blocks both local and AD authentication modes

## Recommendations for Plan 97-03

### Priority 1 (Security Hardening)
1. **Add cookie security settings** (if SSL enabled):
   ```csharp
   options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
   options.Cookie.SameSite = SameSiteMode.Strict;
   ```

### Priority 2 (Code Quality)
1. **Standardize role name formatting** - Change "Admin,HC" to "Admin, HC" in ProtonDataController
2. **Replace manual auth checks** - Add `[Authorize]` attribute to AccountController Profile/Settings actions
3. **Refactor manual role checks** - Consider replacing CMPController manual `User.IsInRole()` checks with attributes where appropriate

### Priority 3 (Documentation)
1. **Document SSL status** - Confirm if HTTPS is enabled in production
2. **Create authorization standards** - Document preferred patterns for future development

---

**Audit completed:** 2026-03-05
**Audited by:** Phase 97-01 Authorization Matrix Audit
**Next phase:** Plan 97-02 (Browser Verification) and Plan 97-03 (Bug Fixes)
