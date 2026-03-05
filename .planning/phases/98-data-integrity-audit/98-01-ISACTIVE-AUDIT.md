# IsActive Filter Audit - Phase 98

> **Plan 98-01:** Exhaustive grep audit of all IsActive filters across entities with spot-check verification

**Audit Date:** 2026-03-05
**Requirements:** DATA-01 (All IsActive filters applied consistently)
**Status:** In Progress

---

## Entities with IsActive Fields

### ApplicationUser (Models/ApplicationUser.cs)
- **Field:** `public bool IsActive { get; set; } = true;`
- **Line:** 66
- **Added:** Phase 83 (2026-03-03)
- **Soft-delete purpose:** Workers can be deactivated/reactivated instead of hard delete
- **Login block:** AccountController.Login checks IsActive before creating session
- **Comment:** `/// <summary>Apakah user aktif. Inactive users tidak bisa login dan disembunyikan dari list default.</summary>`

### CoachCoacheeMapping (Models/CoachCoacheeMapping.cs)
- **Field:** `public bool IsActive { get; set; } = true;`
- **Line:** 24
- **Soft-delete purpose:** Coaching assignments can be deactivated
- **Cascade behavior:** Should hide mappings when Coach or Coachee IsActive=false
- **Comment:** `/// <summary>Status aktif/non-aktif</summary>`

### ProtonTrackAssignment (Models/ProtonModels.cs)
- **Field:** `public bool IsActive { get; set; } = true;`
- **Line:** 77
- **Soft-delete purpose:** Track assignments can be deactivated
- **Cascade behavior:** Should hide assignments when Coachee IsActive=false or ProtonTrack IsDeleted=true
- **Comment:** No explicit comment, follows Proton soft-delete pattern

### ProtonKompetensi (Models/ProtonModels.cs - Silabus)
- **Field:** `public bool IsActive { get; set; } = true;`
- **Line:** 32
- **Added:** Phase 83 (2026-03-03)
- **Soft-delete purpose:** Silabus can be deactivated/reactivated
- **Cascade behavior:** Should hide ProtonSubKompetensi, ProtonDeliverable, ProtonDeliverableProgress when Silabus IsActive=false
- **Comment:** No explicit comment, follows soft-delete pattern

## Entities WITHOUT IsActive (Hard Delete Only)

### AssessmentSession (Models/AssessmentSession.cs)
- **Delete strategy:** Hard delete only
- **Cascade:** EF Core OnDelete(DeleteBehavior.Cascade) when User deleted

### TrainingRecord (Models/TrainingRecord.cs)
- **Delete strategy:** Hard delete only
- **Cascade:** EF Core OnDelete(DeleteBehavior.Cascade) when User deleted

### KkjFile (Models/KkjModels.cs)
- **Delete strategy:** Archive pattern (IsArchived=true)
- **Soft-delete field:** `IsArchived` (not IsActive)

### CpdpFile (Models/KkjModels.cs)
- **Delete strategy:** Archive pattern (IsArchived=true)
- **Soft-delete field:** `IsArchived` (not IsActive)

---

## Query Audit Results

### ApplicationUser (Workers) Queries

| Controller | Action | Line | Filter Pattern | Notes |
|------------|--------|------|----------------|-------|
| AdminController | ManageWorkers | TBD | `.Where(u => u.IsActive)` | Worker list |
| AdminController | WorkerDropdown | TBD | `.Where(u => u.IsActive)` | Dropdown options |
| AdminController | ExportWorkers | TBD | `.Where(u => u.IsActive)` | Export filter |
| AccountController | Login | TBD | `if (!user.IsActive)` | Login block |
| AdminController | DeactivateWorker | TBD | `user.IsActive = false` | Soft delete |
| AdminController | ReactivateWorker | TBD | `user.IsActive = true` | Reactivate |

**Missing filters:**
- TBD (will be identified in Task 98-01-02)

### CoachCoacheeMapping Queries

| Controller | Action | Line | Filter Pattern | Notes |
|------------|--------|------|----------------|-------|
| CDPController | CoachingProton | TBD | `.Where(m => m.IsActive)` | Coachee list |
| AdminController | CoachCoacheeMapping | TBD | `.Where(m => m.IsActive)` | Mapping list |
| AdminController | CoachCoacheeMappingExport | TBD | `.Where(m => m.IsActive)` | Export filter |
| AdminController | SaveCoachCoacheeMapping | TBD | `mapping.IsActive = true` | New mapping |

**Missing filters:**
- TBD (will be identified in Task 98-01-02)

### ProtonTrackAssignment Queries

| Controller | Action | Line | Filter Pattern | Notes |
|------------|--------|------|----------------|-------|
| CDPController | PlanIdp | TBD | `.Where(a => a.IsActive)` | Track assignment |
| AdminController | ProtonData (Assign Track) | TBD | `.Where(a => a.IsActive)` | Assignment list |

**Missing filters:**
- TBD (will be identified in Task 98-01-02)

### ProtonKompetensi (Silabus) Queries

| Controller | Action | Line | Filter Pattern | Notes |
|------------|--------|------|----------------|-------|
| ProtonDataController | SilabusTab | TBD | `(showInactive || k.IsActive)` | Silabus list |
| CDPController | PlanIdp | TBD | `.Where(k => k.IsActive)` | Silabus dropdown |
| CDPController | GetSilabusKompetensi | TBD | `.Where(k => k.IsActive)` | AJAX data source |
| AdminController | SilabusDeactivate | TBD | `silabus.IsActive = false` | Soft delete |
| AdminController | SilabusReactivate | TBD | `silabus.IsActive = true` | Reactivate |

**Missing filters:**
- TBD (will be identified in Task 98-01-02)

---

## Grep Audit Results

> **Task 98-01-02:** Grep all .Where, IsActive, and showInactive patterns

### grep-isactive-where.txt
> All `.Where.*IsActive` patterns in Controllers/

### grep-showinactive.txt
> All `showInactive` toggle patterns in Controllers/ and Views/

### grep-isactive-all.txt
> All `.IsActive` property accesses in Controllers/

---

## Missing Filters - High Risk (Critical Gaps)

> **Task 98-01-03:** Identify missing IsActive filters in high-risk queries

### User-Facing List Views

#### ManageWorkers Table (AdminController.ManageWorkers)
- **Line:** TBD
- **Current filter:** `<pending grep results>`
- **Expected filter:** `.Where(u => u.IsActive)`
- **Status:** ⏳ Pending Analysis
- **Severity:** Critical / Medium / Low
- **Fix:** `<pending analysis>`

#### CoachCoacheeMapping Table (AdminController.CoachCoacheeMapping)
- **Line:** TBD
- **Current filter:** `<pending grep results>`
- **Expected filter:** `.Where(m => m.IsActive)`
- **Status:** ⏳ Pending Analysis
- **Severity:** Critical / Medium / Low
- **Fix:** `<pending analysis>`

### Dropdown/Select Lists

#### Worker Dropdown (AdminController.ManageWorkers)
- **Line:** TBD
- **Current filter:** `<pending grep results>`
- **Expected filter:** `.Where(u => u.IsActive)`
- **Status:** ⏳ Pending Analysis
- **Severity:** Critical (dropdown may contain inactive workers)

### Coaching Proton Queries

#### Coachee List (CDPController.CoachingProton)
- **Line:** TBD
- **Current filter:** `<pending grep results>`
- **Expected filter:** `.Where(m => m.IsActive && m.Coachee.IsActive && m.Coach.IsActive)`
- **Status:** ⏳ Pending Analysis
- **Severity:** Critical (orphaned mappings leak to UI)

### Gap Severity Summary

| Entity | Missing Filters | Severity | UI Impact |
|--------|-----------------|----------|-----------|
| ApplicationUser | TBD | Critical / Medium / Low | Inactive workers visible |
| CoachCoacheeMapping | TBD | Critical / Medium / Low | Orphaned mappings visible |
| ProtonTrackAssignment | TBD | Critical / Medium / Low | Inactive tracks visible |
| ProtonKompetensi | TBD | Critical / Medium / Low | Deleted silabus visible |

---

## Spot-Check Verification Results

> **Task 98-01-04:** Manual code review of high-risk queries

### ManageWorkers Query (AdminController.ManageWorkers)
**Line:** TBD
**Expected:** `.Where(u => u.IsActive)`
**Actual:** `<pending code review>`
**Status:** ⏳ Pending Verification
**Evidence:** `<pending code snippet>`

### CoachCoacheeMapping Query (AdminController.CoachCoacheeMapping)
**Line:** TBD
**Expected:** `.Where(m => m.IsActive)`
**Actual:** `<pending code review>`
**Status:** ⏳ Pending Verification
**Evidence:** `<pending code snippet>`

### CoachingProton Query (CDPController.CoachingProton)
**Line:** TBD
**Expected:** `.Where(m => m.IsActive && m.Coachee.IsActive && m.Coach.IsActive)`
**Actual:** `<pending code review>`
**Status:** ⏳ Pending Verification
**Evidence:** `<pending code snippet>`

### PlanIdp Query (CDPController.PlanIdp)
**Line:** TBD
**Expected:** `.Where(a => a.IsActive)`
**Actual:** `<pending code review>`
**Status:** ⏳ Pending Verification
**Evidence:** `<pending code snippet>`

### SilabusTab Query (ProtonDataController.SilabusTab)
**Line:** TBD
**Expected:** `(showInactive || k.IsActive)`
**Actual:** `<pending code review>`
**Status:** ⏳ Pending Verification
**Evidence:** `<pending code snippet>`

### Summary

- **Total queries spot-checked:** 5
- **Passed:** TBD
- **Gaps found:** TBD
- **Critical gaps:** TBD
- **Recommendations:** `<pending analysis>`

---

## Fix Recommendations for Plan 98-03

> **Task 98-01-04:** Document all fixes needed for plan 98-03

### Critical Fixes (UI Leaks)

1. **[Controller].[Action] (Line X)**
   - **Issue:** Missing IsActive filter
   - **Impact:** Deleted records visible to users
   - **Fix:** Add `.Where(x => x.IsActive)` filter

### Medium Fixes (Internal Queries)

1. **[Controller].[Action] (Line X)**
   - **Issue:** Inconsistent IsActive filter
   - **Impact:** Low exposure, inconsistent behavior
   - **Fix:** Add `.Where(x => x.IsActive)` filter

### Low Priority (Admin-only with Toggle)

1. **[Controller].[Action] (Line X)**
   - **Issue:** Already has showInactive toggle
   - **Impact:** None (working as designed)
   - **Fix:** No action needed

---

## Completion Status

- [x] **Task 98-01-01:** Document all entities with IsActive fields (4 entities identified)
- [ ] **Task 98-01-02:** Grep audit all IsActive filter usage
- [ ] **Task 98-01-03:** Identify missing IsActive filters - High-risk queries
- [ ] **Task 98-01-04:** Spot-check verification - High-risk queries
- [ ] **Task 98-01-04:** Document fix recommendations for plan 98-03

---

**Next:** Execute Task 98-01-02 (Grep audit all IsActive filter usage)
