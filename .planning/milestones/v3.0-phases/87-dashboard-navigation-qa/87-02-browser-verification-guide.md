# Phase 87-02: Browser Verification Guide

**Status:** Auto-approved (auto_mode = true)
**Action Required:** Manual browser verification recommended

## Test Data Setup

1. Run `/Admin/SeedDashboardTestData` to create test data
2. Log in as each of the 6 roles: Admin, HC, SrSpv, SectionHead, Coach, Coachee
3. Navigate to:
   - Home/Index (`/`)
   - CDP/Dashboard (`/CDP/Dashboard`)

## Verification Checklist (Spot-Check 2-3 Metrics Per Role)

### Home/Index - All Roles

- [ ] **IDP Progress** - Percentage displays correctly (not NaN, not >100%)
- [ ] **Pending Assessment** - Count matches expected number of open assessments
- [ ] **Mandatory Training** - Status shows correct icon/color (valid/expiring/expired)
- [ ] **Recent Activities** - Shows up to 4 items with time-ago formatting
- [ ] **Upcoming Deadlines** - Shows up to 4 items with urgency badges
- [ ] **Quick Access** - Cards link to correct pages

### CDP Dashboard - Coachee Role

- [ ] **CoacheeData tab** - Shows personal deliverable stats
- [ ] **Total/Approved/Active counts** - Match database values
- [ ] **Active deliverables** - Now shows Pending items (fixed bug)

### CDP Dashboard - Non-Coachee Roles (SrSpv, SectionHead, Coach)

- [ ] **ProtonProgressData tab** - Shows scoped coachees:
  - SrSpv/SectionHead: Only coachees in same section
  - Coach: Only coachees in same unit
- [ ] **Pending Approvals** - Count accurate
- [ ] **Pending HC Reviews** - Count accurate
- [ ] **Inactive users excluded** - Confirm inactive coachees don't appear (fixed bug)

### CDP Dashboard - HC/Admin Only

- [ ] **AssessmentAnalyticsData tab** - Renders correctly
- [ ] **Filter controls** - Work (category, date range, section, search)
- [ ] **Export buttons** - Render (click not required for spot-check)

## Expected Test Data After Seed

Based on SeedDashboardTestData implementation:
- **Active users**: Should be at least 2+
- **Assessment sessions**: Open, Upcoming, Completed
- **IDP items**: Various statuses
- **Coaching data**: Mapped coach-coachee pairs
- **Proton assignments**: Track assignments for coachees

## Known Issues Fixed in This Plan

1. **Coachee ActiveDeliverables**: Was checking Status="Active" (doesn't exist), now checks "Pending"
2. **Proton Progress coachee scope**: Now filters out inactive users with IsActive=true

## If Verification Fails

1. Re-check controller queries in HomeController.cs and CDPController.cs
2. Verify test data was created correctly via SQL or Admin panel
3. Check browser console for JavaScript errors
4. Report discrepancies for further investigation
