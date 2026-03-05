# Phase 95: Admin Portal Audit — Plan Summary

**Phase:** 95 - Admin Portal Audit
**Milestone:** v3.2 Bug Hunting & Quality Audit
**Status:** Ready for execution
**Plans created:** 4

## Overview

Phase 95 systematically audits Kelola Data (Admin Portal) pages for bugs, focusing on pages NOT yet covered in prior audit phases (88, 90). Uses proven Phase 93/94 audit pattern: code review → identify bugs → fix → smoke test verification.

## Phase Goal

**All Admin portal pages work correctly end-to-end:**
- ManageWorkers: filters, pagination, CRUD operations, import/export
- CoachCoacheeMapping: assign, deactivate, reactivate, export
- Cross-cutting: validation error handling, role gates, CSRF protection

**Success Criteria:**
1. All Admin pages load without errors for Admin and HC roles
2. Filters and pagination work correctly
3. File operations (upload, download, archive) work correctly
4. All forms handle validation gracefully (no raw exceptions)
5. Role gates work correctly (HC vs Admin access)

## Requirements Coverage

| Requirement | Plan | Status |
|-------------|------|--------|
| ADMIN-01: ManageWorkers filters and pagination | 95-01, 95-02, 95-04 | Pending |
| ADMIN-02: Manage Silabus KKJ files | Already audited Phase 88 | Complete |
| ADMIN-03: Manage Assessment | Already audited Phase 90 | Complete |
| ADMIN-04: Assessment Monitoring | Already audited Phase 90 | Complete |
| ADMIN-05: CoachCoacheeMapping operations | 95-01, 95-02, 95-04 | Pending |
| ADMIN-06: ProtonData tabs | Already audited Phase 88 | Complete |
| ADMIN-07: Validation error handling | 95-01, 95-03, 95-04 | Pending |
| ADMIN-08: Role gates | 95-01, 95-03, 95-04 | Pending |

**Coverage:** 8/8 requirements mapped (100%)

## Plan Breakdown

### Plan 95-01: Code Review — Admin Controller & Views (Wave 1)
**Goal:** Systematic code review using checklists from Phase 93/94
**Files:** AdminController.cs, ManageWorkers views, CoachCoacheeMapping views
**Tasks:**
1. Audit ManageWorkers actions and views (null safety, localization, validation, authorization)
2. Audit CoachCoacheeMapping actions and views (CSRF, dates, IsActive filters)
3. Audit cross-cutting concerns (validation, role gates)
4. Document findings and prioritize fixes
**Deliverable:** 95-01-BUGS.md with categorized bug list

### Plan 95-02: Fix ManageWorkers & CoachCoacheeMapping Bugs (Wave 2)
**Goal:** Fix bugs in ManageWorkers and CoachCoacheeMapping pages
**Files:** AdminController.cs, ManageWorkers views, CoachCoacheeMapping views
**Commits:** 2 commits (one per page, per CONTEXT.md decision)
**Tasks:**
1. Fix ManageWorkers bugs (null safety, localization, pagination, validation)
2. Fix CoachCoacheeMapping bugs (CSRF, localization, IsActive filters)
3. Add missing using directives (System.Globalization)
4. Verify compilation and basic smoke test

### Plan 95-03: Fix Cross-Cutting Validation & Role Gate Bugs (Wave 3)
**Goal:** Fix validation error handling and role gate inconsistencies
**Files:** AdminController.cs, multiple Admin views
**Commits:** 2 commits (validation + role gates, per CONTEXT.md decision)
**Tasks:**
1. Fix validation error handling inconsistencies (ModelState, TempData, generic errors)
2. Fix role gate inconsistencies (missing attributes, incorrect restrictions)
3. Verify CSRF token consistency across all POST actions
4. Verify compilation and basic smoke test

### Plan 95-04: Browser Verification — Smoke Test Admin Flows (Wave 4)
**Goal:** Verify all bug fixes work correctly in browser
**Files:** None (manual testing)
**Tasks:**
1. Verify ManageWorkers flow (filters, pagination, CRUD)
2. Verify CoachCoacheeMapping flow (assign, deactivate, reactivate, export)
3. Verify validation error handling across Admin forms
4. Verify role gates (HC vs Admin access)
5. Regression check — verify fixes don't break existing functionality
6. Document verification results
**Deliverable:** 95-04-VERIFICATION.md with test results

## Execution Order

```
95-01 (Code Review)
  ↓
95-02 (Fix Page-Specific Bugs)
  ↓
95-03 (Fix Cross-Cutting Bugs)
  ↓
95-04 (Browser Verification)
```

**Parallelism:**
- Wave 1: Plan 95-01 (independent)
- Wave 2: Plan 95-02 (depends on 95-01)
- Wave 3: Plan 95-03 (depends on 95-02)
- Wave 4: Plan 95-04 (depends on 95-03, requires manual testing)

## Commit Strategy

Per CONTEXT.md decision (Audit Organization):
- **Per-page commits:** ManageWorkers fixes → commit 1, CoachCoacheeMapping fixes → commit 2
- **Cross-cutting commits:** Validation fixes → commit 3, Role gate fixes → commit 4
- **Total expected:** 4-5 commits

This keeps changes organized by feature area and makes tracking easier.

## Test Data Approach

Per CONTEXT.md decision (Test Data Approach):
- **Use existing seed data:** Workers from Phase 83 (SeedMasterData.cs), Coach-coachee mappings from Phase 85 (SeedCoachingTestData)
- **Add test data only when needed:** During code review, if specific worker role or mapping status required, then add
- **Import Workers template:** Use existing DownloadImportTemplate, fill with sample data
- **Pragmatic approach:** Only add test data actually required

**No test data seeding plans** — rely on existing comprehensive seed data.

## Testing Approach

Per CONTEXT.md decision (Testing Approach):
- **Smoke test only:** Quick verification that pages load and obvious bugs are fixed
- **Don't test every role combination:** Focus on Admin and HC roles only
- **Pattern:** Code review → identify bugs → fix → browser verify
- **Focus on specific bug fixed:** Verify the fix, not comprehensive testing
- **Browser testing when needed:** Code review unclear or requires runtime verification

## Role Testing Coverage

Per CONTEXT.md decision (Role Testing Coverage):
- **HC & Admin roles only:** Two roles with Admin page access
- **Verify role gates via code review:** Check [Authorize] attributes in controller
- **Don't test all intermediate roles:** Skip Coach, Spv, SectionHead to save time
- **Test role-based filtering if in code:** If .Where(u => u.Unit == user.Unit) found, test it
- **Smoke test level:** Verify role gates exist via code review, not exhaustive permission testing

## Validation Depth

Per CONTEXT.md decision (Validation Depth):
- **All Admin forms:** Check validation error handling on all Admin CRUD forms
- **ManageWorkers:** Create, Edit forms
- **CoachCoacheeMapping:** Assign form
- **Import form:** File upload validation
- **Check:** Required fields, data type validation, error messages via TempData (not raw exceptions)

## Import/Export Depth

Per CONTEXT.md decision (Import/Export Depth):
- **Smoke test Import Workers:** Upload valid file → verify processed → check data in DB
- **Export:** Claude determines based on code review complexity
- **Smoke test validation:** Test one invalid file type to verify validation exists
- **Focus:** Verify basic functionality works, edge cases only if code review reveals issues

## Bug Priority

Per CONTEXT.md decision (Bug Priority):
- **Claude's discretion:** Prioritize based on severity and user impact
- **Critical:** Crashes, null references, raw exceptions shown to users
- **High:** Broken flows, incorrect data displayed, navigation failures
- **Medium:** UX issues (unclear text, missing links, confusing UI)
- **Low:** Cosmetic issues, typos, minor inconsistencies

## Common Bug Patterns

Based on Phase 93/94 audit findings, expect these bugs:

### Null Safety
- ApplicationUser navigation properties (Coach, Coachee) accessed without null checks
- **Fix:** Use null-conditional operator (?.) or null-coalescing operator (??)

### Date Localization
- DateTime.ToString() without culture parameter shows English format
- **Fix:** Use CultureInfo.GetCultureInfo("id-ID") for Indonesian formatting

### Missing IsActive Filters
- Queries missing .Where(u => u.IsActive) filter
- **Fix:** Add IsActive=true filter to user-facing queries

### Missing CSRF Tokens
- AJAX POST requests missing X-Request-Verification-Token header
- **Fix:** Add CSRF token to fetch headers

### Missing Parameter Validation
- POST actions missing ModelState.IsValid or null checks
- **Fix:** Add validation with TempData["Error"] messages

### Pagination Edge Cases
- Page parameter exceeds totalPages without clamping
- **Fix:** Add page clamping logic (if page < 1 page = 1; if page > totalPages page = totalPages)

## Files Modified

### Controllers
- `Controllers/AdminController.cs` (5729 lines) — All Admin actions

### Views
- `Views/Admin/ManageWorkers.cshtml` — Worker list with filters/pagination
- `Views/Admin/CreateWorker.cshtml` — Worker creation form
- `Views/Admin/EditWorker.cshtml` — Worker edit form
- `Views/Admin/ImportWorkers.cshtml` — Excel bulk import
- `Views/Admin/CoachCoacheeMapping.cshtml` — Coach-coachee assignment interface

### Already Audited (Phase 88, 90)
- `Views/Admin/KkjMatrix.cshtml` — KKJ file management (Phase 88)
- `Views/Admin/ManageAssessment.cshtml` — Assessment CRUD (Phase 90)
- `Views/Admin/AssessmentMonitoring.cshtml` — Real-time monitoring (Phase 90)
- `Views/Admin/ProtonData.cshtml` — Silabus/Guidance tabs (Phase 88)

## Verification Criteria

### Phase Gate
- [ ] All ADMIN-01 through ADMIN-08 requirements verified via browser testing
- [ ] No regressions discovered in previously audited pages
- [ ] All critical and high bugs fixed
- [ ] Medium/low bugs documented or fixed at discretion

### Plan-Specific Gates
- **95-01:** Bug list documented with severity ratings and commit strategy
- **95-02:** ManageWorkers and CoachCoacheeMapping bugs fixed, code compiles
- **95-03:** Validation and role gate bugs fixed, code compiles
- **95-04:** All flows smoke-tested, verification document created

## Success Criteria (must_haves)

### Derived from Phase Goal

**If phase is successful, then:**
1. Admin/HC can navigate to all Admin pages without errors
2. ManageWorkers filters, pagination, and CRUD operations work correctly
3. CoachCoacheeMapping assign, deactivate, reactivate, and export work correctly
4. All Admin forms show user-friendly validation errors (no raw exceptions)
5. Role gates prevent unauthorized access (HC blocked from Admin-only pages)
6. Previously audited pages (KKJ, Assessments, Monitoring, ProtonData) still work
7. CSRF protection works on all POST actions
8. Dates display in Indonesian format throughout Admin pages

**Therefore, these must be TRUE:**
- [ ] All Admin pages load without errors for Admin and HC roles
- [ ] ManageWorkers filters and pagination work correctly
- [ ] CoachCoacheeMapping operations complete successfully
- [ ] All forms handle validation errors gracefully
- [ ] Role gates work correctly (HC vs Admin access)
- [ ] No regressions in previously audited pages
- [ ] CSRF protection consistent across all POST actions
- [ ] Date localization applied to all Admin date displays

## Notes

- Follows Phase 93/94 audit pattern proven in CMP and CDP sections
- "Secara menyeluruh dan detail" — thoroughness over speed (from Phase 90)
- Commit style: `fix(admin): [description]` with Co-Authored-By footer
- Use Indonesian culture (id-ID) for all date formatting
- Preserve existing functionality — bug fixes only, no behavior changes
- Focus on pages NOT yet audited: ManageWorkers, CoachCoacheeMapping
- Cross-cutting concerns (validation, role gates) get separate commits

## Dependencies

- **Depends on:** Phase 94 (CDP Section Audit) — ensures all CDP pages working before Admin audit
- **Blocks:** Phase 96 (Account Pages Audit) — Admin audit must complete before Account audit
- **Shared context:** Uses seed data from Phase 83 (Master Data QA) and Phase 85 (Coaching Proton QA)

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| AdminController.cs is large (5729 lines) — audit may take time | Use systematic checklist approach, focus on targeted sections (ManageWorkers, CoachCoacheeMapping) |
| Many Admin pages already audited (Phase 88, 90) — duplication risk | Research document clearly identifies already-audited pages, focus only on remaining pages |
| Cross-cutting bugs may span multiple pages | Use separate commits for cross-cutting concerns (validation, role gates) per CONTEXT.md decision |
| Browser testing may reveal additional bugs not found in code review | Create gap closure plans if needed, prioritize by severity |
| Role testing limited to Admin/HC only — may miss edge cases | Code review verifies role gates, smoke test verifies basic access, comprehensive testing out of scope |

## Next Steps

1. Execute plan 95-01: Code review of AdminController.cs and Admin views
2. Document all bugs found with severity ratings
3. Execute plan 95-02: Fix ManageWorkers and CoachCoacheeMapping bugs
4. Execute plan 95-03: Fix cross-cutting validation and role gate bugs
5. Execute plan 95-04: Browser smoke testing of all Admin flows
6. Run `/gsd:verify-work` to confirm phase completion

**Phase 95 complete when:** All ADMIN-01 through ADMIN-08 requirements verified PASS via browser testing, no regressions discovered, all critical/high bugs fixed.

---

**Planned:** 2026-03-05
**Plans:** 4 (95-01, 95-02, 95-03, 95-04)
**Estimated duration:** 60-90 minutes (code review: 20, fixes: 20, testing: 20-30)
**Confidence:** HIGH (proven audit pattern from Phase 93/94)
