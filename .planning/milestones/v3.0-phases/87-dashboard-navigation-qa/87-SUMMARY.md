# Phase 87: Dashboard & Navigation QA - Summary

**Phase:** 87 - Dashboard & Navigation QA
**Status:** Ready for execution
**Plans:** 3 plans (87-01, 87-02, 87-03)
**Requirements Coverage:** DASH-01 through DASH-08

## Phase Goal

Verify all dashboards show correct role-scoped data, login and navigation work without errors, and authorization boundaries are enforced across all 6 roles (Admin, HC, SrSpv, SectionHead, Coach, Coachee).

## Plans Overview

### Plan 87-01: Create Dashboard Test Data Seed Action
**Wave:** 1 (can run in parallel with 87-02, 87-03)
**Dependencies:** None
**Autonomous:** Yes (can run without user verification)
**Requirements:** DASH-01, DASH-02, DASH-03

Creates comprehensive test data following the established pattern from Phases 85 and 90:
- `SeedDashboardTestData` action in AdminController
- Idempotent seed data creation for all 6 dashboard data sources
- Test data for Assessment Sessions, IDP Items, Proton Deliverable Progress, Track Assignments, Training Records, and Audit Logs
- Returns JSON summary of created data

### Plan 87-02: QA Home/Index and CDP Dashboard Data Accuracy
**Wave:** 2
**Dependencies:** 87-01
**Autonomous:** No (requires browser verification)
**Requirements:** DASH-01, DASH-02, DASH-03

Deep verification of dashboard metrics through code review and spot-check:
- **HomeController Index:** IDP progress, pending assessments, mandatory training, recent activities, upcoming deadlines
- **CDPController Dashboard:** Coaching Proton tab (role-scoped coachee stats), Assessment Analytics tab (HC/Admin only)
- Fix bugs <100 lines inline, flag larger issues
- Browser spot-check for all 6 roles

### Plan 87-03: QA Login Flow, Navigation, Authorization, and AuditLog
**Wave:** 2 (can run in parallel with 87-02 after 87-01)
**Dependencies:** 87-01
**Autonomous:** No (requires browser verification)
**Requirements:** DASH-04, DASH-05, DASH-06, DASH-07, DASH-08

Verification of authentication and navigation:
- **Login Flow:** Local auth happy path, inactive user block, return URL redirect
- **Navigation Visibility:** Kelola Data menu (Admin/HC only), CMP/CDP (all roles)
- **Section Selectors:** CMP/Mapping (role-based tab filtering), KkjSectionSelect
- **AccessDenied Page:** Renders for unauthorized access
- **AuditLog Page:** Displays and paginates correctly for Admin/HC
- Browser verification for all 6 roles

## Execution Order

```
Wave 1:
  87-01 (autonomous - creates test data)

Wave 2 (parallel after 87-01):
  87-02 (dashboard metrics verification)
  87-03 (auth/nav verification)
```

## Key Decisions from CONTEXT.md

1. **Seed Data Pattern:** Follow Phase 90 (SeedAssessmentTestData) and Phase 85 (SeedCoachingTestData) - idempotent, check existing before insert
2. **Full Role Coverage:** Test all 6 roles for comprehensive verification
3. **Test Data Scope:** Create realistic data across all dashboard data sources
4. **Login Flow:** Local auth tested in browser, AD path code review only (same logic after authenticate)
5. **Dashboard Verification:** Code review + browser spot-check (not exhaustive testing)
6. **Cross-Feature Verification:** Data accuracy only - no round-trip testing to detail pages (already tested in phases 84/85/90/91)
7. **Bug Fix Approach:** Fix bugs <100 lines inline, flag larger issues for discussion
8. **Silent Bugs:** Fix if <20 lines, otherwise log and skip

## Success Criteria

Phase 87 is complete when:
1. ✅ SeedDashboardTestData action creates test data for all 6 dashboard sources
2. ✅ Home/Index metrics verified accurate via code review and browser spot-check
3. ✅ CDP Dashboard metrics verified accurate for all 6 roles
4. ✅ Login flow works (happy path, inactive block, return URL)
5. ✅ Kelola Data navigation visible only to Admin/HC
6. ✅ Section selectors work correctly (Admin/HC all tabs, L5-L6 filtered tabs)
7. ✅ AccessDenied page renders correctly
8. ✅ AuditLog page displays correctly for Admin/HC
9. ✅ All bugs <100 lines fixed, larger bugs flagged
10. ✅ No cross-role data leakage or authorization bypasses

## Files Modified

**Plan 87-01:**
- `Controllers/AdminController.cs` (add SeedDashboardTestData action)

**Plan 87-02:**
- `Controllers/HomeController.cs` (review and potential bug fixes)
- `Controllers/CDPController.cs` (review and potential bug fixes)
- `Views/Home/Index.cshtml` (spot-check only, no changes expected)
- `Views/CDP/Dashboard.cshtml` (spot-check only, no changes expected)

**Plan 87-03:**
- `Controllers/AccountController.cs` (review and potential bug fixes)
- `Views/Shared/_Layout.cshtml` (review and potential bug fixes)
- `Views/CMP/KkjSectionSelect.cshtml` (review only)
- `Views/Account/AccessDenied.cshtml` (review and potential bug fixes)
- `Views/Admin/AuditLog.cshtml` (review and potential bug fixes)
- `Controllers/CMPController.cs` (review Mapping action)

## Requirements Traceability

| Requirement | Plan(s) | Status |
|-------------|---------|--------|
| DASH-01: Home/Index dashboard per role | 87-02 | Pending |
| DASH-02: CDP Dashboard both tabs | 87-02 | Pending |
| DASH-03: Dashboard data accuracy | 87-02 | Pending |
| DASH-04: Login flow | 87-03 | Pending |
| DASH-05: Role-based navigation | 87-03 | Pending |
| DASH-06: Section selectors | 87-03 | Pending |
| DASH-07: AccessDenied page | 87-03 | Pending |
| DASH-08: AuditLog page | 87-03 | Pending |

**Coverage:** 8/8 requirements (100%) mapped to plans

## Notes

- This is the **final phase of v3.0** - completing this phase closes the milestone
- Focus on **verification and bug fixes** - no new features
- Browser verification is **spot-check** (2-3 metrics per role), not exhaustive
- Test data from 87-01 is essential for 87-02 and 87-03
- Account Profile/Settings QA (ACCT-01, ACCT-02) is deferred to v3.1 per REQUIREMENTS.md
- All flows tested in phases 84/85/90/91 are assumed working - Phase 87 focuses on dashboard accuracy and navigation

---

*Phase 87 planning complete - ready for execution via /gsd:execute-phase*
