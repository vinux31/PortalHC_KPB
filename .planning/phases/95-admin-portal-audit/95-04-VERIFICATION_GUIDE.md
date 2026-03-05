# Admin Portal Browser Verification Guide

**Plan:** 95-04 - Browser Verification: Smoke Test Admin Flows
**Date:** 2026-03-05
**Status:** Ready for Human Verification
**Auto-advance:** Bypassed - This plan IS the verification

---

## Overview

This guide provides step-by-step instructions for smoke testing all Admin flows to verify bug fixes from plans 95-02 and 95-03. The focus is on quick verification that pages load and obvious bugs are fixed.

**Prerequisites:**
- Application running and accessible
- Test data seeded (from Phase 83 and Phase 85)
- Admin user account available
- HC user account available

**What was fixed:**
- **95-02:** ManageWorkers date localization (JoinDate), CoachCoacheeMapping date localization (StartDate)
- **95-03:** 12 raw exception exposures fixed with generic Indonesian messages, input validation added to AddQuestion

---

## Task 1: Verify ManageWorkers Flow (ADMIN-01)

### Test as Admin User

#### 1.1 Navigate to ManageWorkers
- [ ] Go to `/Admin/Index`
- [ ] Click "Manajemen Pekerja" card (Section A)
- [ ] Verify: Page loads without errors
- [ ] Verify: Worker list displays correctly
- [ ] Screenshot if failed

#### 1.2 Test Filters
- [ ] Search by name (e.g., " Ahmad")
- [ ] Search by email (e.g., "@test.com")
- [ ] Search by NIP (e.g., "12345")
- [ ] Filter by Section dropdown (select "RFCC")
- [ ] Filter by Role dropdown (select "Admin")
- [ ] Verify: All filters work correctly
- [ ] Verify: Results update without errors
- [ ] Screenshot if failed

#### 1.3 Test showInactive Toggle
- [ ] Note current active worker count
- [ ] Click "Tampilkan Nonaktif" toggle
- [ ] Verify: Inactive workers appear in list
- [ ] Click again to hide
- [ ] Verify: Inactive workers hidden
- [ ] Screenshot if failed

#### 1.4 Test Pagination
- [ ] Click page 2 (if available)
- [ ] Verify: Different users shown
- [ ] Click page 1
- [ ] Verify: Back to first page
- [ ] Try edge case: Add `?page=999` to URL
- [ ] Verify: Doesn't crash, shows last or empty page
- [ ] Screenshot if failed

#### 1.5 Test Create Worker (Validation)
- [ ] Click "Tambah Pekerja" button
- [ ] Verify: Form loads at `/Admin/CreateWorker`
- [ ] Submit form with empty fields
- [ ] Verify: Validation error shown via TempData["Error"]
- [ ] Verify: Error message is user-friendly (Indonesian)
- [ ] Screenshot if failed

#### 1.6 Test Create Worker (Success)
- [ ] Fill form with valid data:
  - Nama: "Test Worker"
  - Email: "testworker@test.com"
  - NIP: "99999"
  - Unit: "RFCC"
  - Role: "Worker"
- [ ] Submit form
- [ ] Verify: Worker created successfully
- [ ] Verify: Redirected to ManageWorkers
- [ ] Verify: Success message shown
- [ ] Check JoinDate displays in Indonesian format (e.g., "5 Maret 2026")
- [ ] Screenshot if failed

#### 1.7 Test Edit Worker
- [ ] Click "Edit" on the newly created worker
- [ ] Verify: Form loads with existing data
- [ ] Modify name to "Test Worker Edited"
- [ ] Submit form
- [ ] Verify: Changes saved successfully
- [ ] Verify: Worker name updated in list
- [ ] Screenshot if failed

#### 1.8 Test Deactivate/Reactivate
- [ ] Click "Nonaktifkan" on active worker
- [ ] Verify: Worker marked inactive (disappears from active list)
- [ ] Click "Tampilkan Nonaktif"
- [ ] Verify: Worker appears with "Nonaktif" badge
- [ ] Click "Aktifkan Kembali"
- [ ] Verify: Worker reactivated (appears in active list)
- [ ] Screenshot if failed

**Expected Results:**
- No crashes or null reference exceptions
- All filters work correctly
- Pagination doesn't break on edge cases
- Dates displayed in Indonesian format
- Validation errors show user-friendly Indonesian messages
- No raw exception details exposed

**Status:** _____ PASS / FAIL

---

## Task 2: Verify CoachCoacheeMapping Flow (ADMIN-05)

### Test as Admin User

#### 2.1 Navigate to CoachCoacheeMapping
- [ ] Go to `/Admin/Index`
- [ ] Click "Coach-Coachee Mapping" card (Section C)
- [ ] Verify: Page loads without errors
- [ ] Verify: Coach groups display correctly with coachee lists
- [ ] Screenshot if failed

#### 2.2 Test Filters
- [ ] Search by coach name
- [ ] Search by NIP
- [ ] Filter by Section dropdown
- [ ] Verify: All filters work correctly
- [ ] Screenshot if failed

#### 2.3 Test Pagination
- [ ] Click page 2 (if available)
- [ ] Verify: Different coaches shown
- [ ] Screenshot if failed

#### 2.4 Test Assign Coach
- [ ] Click "Tambah Mapping" button
- [ ] Verify: Modal appears
- [ ] Select coach from dropdown
- [ ] Select coachees from dropdown (verify inactive users NOT shown)
- [ ] Set StartDate and EndDate
- [ ] Submit form
- [ ] Verify: Assignment succeeds
- [ ] Verify: New mapping appears in list
- [ ] Check StartDate displays in Indonesian format (e.g., "5 Maret 2026")
- [ ] Screenshot if failed

#### 2.5 Test Edit Mapping
- [ ] Click "Edit" on existing mapping
- [ ] Verify: Modal loads with current data
- [ ] Modify coachee list
- [ ] Submit form
- [ ] Verify: Changes saved
- [ ] Screenshot if failed

#### 2.6 Test Deactivate/Reactivate
- [ ] Click "Nonaktifkan" on active mapping
- [ ] Verify: Mapping marked inactive
- [ ] Click "Tampilkan Semua"
- [ ] Verify: Mapping appears
- [ ] Click "Aktifkan Kembali"
- [ ] Verify: Mapping reactivated
- [ ] Screenshot if failed

#### 2.7 Test Export
- [ ] Click "Export Excel" button
- [ ] Verify: File downloads correctly
- [ ] Open Excel file
- [ ] Verify: Contains mapping data
- [ ] Verify: StartDate/EndDate in Indonesian format
- [ ] Screenshot if failed

**Expected Results:**
- No crashes or AJAX errors
- Assign/edit/deactivate/reactivate operations complete successfully
- CSRF validation works (no 403/400 errors)
- Dates (StartDate/EndDate) displayed in Indonesian format
- Dropdowns filter out inactive users

**Status:** _____ PASS / FAIL

---

## Task 3: Verify Validation Error Handling (ADMIN-07)

### Test Invalid Submissions Across Admin Forms

#### 3.1 ManageWorkers CreateWorker Validation
- [ ] Go to `/Admin/CreateWorker`
- [ ] Submit with empty Nama field
- [ ] Verify: Error message shown (not raw exception)
- [ ] Submit with duplicate email (use existing worker's email)
- [ ] Verify: Specific error message about duplicate email
- [ ] Verify: Error is Indonesian, user-friendly
- [ ] Screenshot if failed

#### 3.2 CoachCoacheeMapping Validation
- [ ] Go to `/Admin/CoachCoacheeMapping`
- [ ] Click "Tambah Mapping"
- [ ] Try to assign coach as own coachee
- [ ] Verify: Error message about self-assignment
- [ ] Try to assign coachee who already has active coach
- [ ] Verify: Error message about existing coach
- [ ] Verify: Errors are Indonesian, user-friendly
- [ ] Screenshot if failed

#### 3.3 Import Workers Validation
- [ ] Go to `/Admin/ImportWorkers`
- [ ] Submit without selecting file
- [ ] Verify: Error message about missing file
- [ ] Try uploading invalid file type (e.g., .pdf or .txt)
- [ ] Verify: Error message about invalid file type
- [ ] Click "Download Template" button
- [ ] Verify: Excel template downloads with correct headers
- [ ] Open template and verify headers: Nama, Email, NIP, Unit, Role, JoinDate
- [ ] Screenshot if failed

**Expected Results:**
- All validation errors show user-friendly TempData["Error"] messages
- No raw exception details exposed to users
- Error messages guide users to fix the issue
- All error messages in Indonesian

**Status:** _____ PASS / FAIL

---

## Task 4: Verify Role Gates (ADMIN-08)

### Test as HC User

#### 4.1 Login as HC User
- [ ] Logout if logged in
- [ ] Login as HC user (check test data for HC credentials)
- [ ] Verify: Login successful

#### 4.2 Test HC Access to Admin Pages
- [ ] Navigate to `/Admin/Index`
- [ ] Verify: Accessible (200 OK)
- [ ] Navigate to `/Admin/ManageWorkers`
- [ ] Verify: Accessible (200 OK)
- [ ] Navigate to `/Admin/CoachCoacheeMapping`
- [ ] Verify: Accessible (200 OK)
- [ ] Navigate to `/Admin/SeedDashboardTestData`
- [ ] Verify: FORBIDDEN (403)
- [ ] Navigate to `/Admin/SeedMasterData`
- [ ] Verify: FORBIDDEN (403)
- [ ] Screenshot if any unexpected result

### Test as Admin User

#### 4.3 Login as Admin User
- [ ] Logout
- [ ] Login as Admin user
- [ ] Verify: Login successful

#### 4.4 Test Admin Access to All Pages
- [ ] Navigate to `/Admin/Index`
- [ ] Verify: Accessible (200 OK)
- [ ] Navigate to `/Admin/ManageWorkers`
- [ ] Verify: Accessible (200 OK)
- [ ] Navigate to `/Admin/CoachCoacheeMapping`
- [ ] Verify: Accessible (200 OK)
- [ ] Navigate to `/Admin/SeedDashboardTestData`
- [ ] Verify: Accessible (200 OK)
- [ ] Navigate to `/Admin/SeedMasterData`
- [ ] Verify: Accessible (200 OK)
- [ ] Screenshot if any unexpected result

**Expected Results:**
- HC can access shared Admin/HC pages (Index, ManageWorkers, CoachCoacheeMapping)
- HC cannot access Admin-only pages (SeedDashboardTestData, SeedMasterData) → 403 Forbidden
- Admin can access all pages

**Status:** _____ PASS / FAIL

---

## Task 5: Regression Check - Previously Audited Pages

### Quick Smoke Tests

#### 5.1 Test Admin Hub Navigation
- [ ] Go to `/Admin/Index`
- [ ] Click each card in Section A (Manajemen Pekerja, Import Workers)
- [ ] Click each card in Section B (Coach-Coachee Mapping)
- [ ] Click each card in Section C (KKJ Matrix)
- [ ] Click each card in Section D (Assessment, Proton Data)
- [ ] Verify: All links work correctly
- [ ] Screenshot if broken link

#### 5.2 Test Previously Audited Pages (Phase 88, 90, 91)
- [ ] `/Admin/KkjMatrix` (Phase 88)
  - [ ] Page loads
  - [ ] Tabs work (RFCC, GAST, NGP, DHT/HMU)
  - [ ] Upload button works
  - [ ] Screenshot if failed

- [ ] `/Admin/ManageAssessment` (Phase 90)
  - [ ] Page loads
  - [ ] Assessment list displays
  - [ ] Filters work
  - [ ] Screenshot if failed

- [ ] `/Admin/AssessmentMonitoring` (Phase 90)
  - [ ] Page loads
  - [ ] Monitoring table displays
  - [ ] Detail links work
  - [ ] Screenshot if failed

- [ ] `/Admin/ProtonData` (Phase 88)
  - [ ] Page loads
  - [ ] Tabs work (Silabus, Guidance)
  - [ ] Screenshot if failed

**Expected Results:**
- No broken links or navigation issues
- Previously audited pages still work correctly
- No new bugs introduced by fixes

**Status:** _____ PASS / FAIL

---

## Task 6: Document Verification Results

### Summary Checklist

After completing all tasks, fill out this summary:

**Task 1: ManageWorkers (ADMIN-01)**
- [ ] PASS - All features work correctly
- [ ] FAIL - Issues found: ___________________

**Task 2: CoachCoacheeMapping (ADMIN-05)**
- [ ] PASS - All features work correctly
- [ ] FAIL - Issues found: ___________________

**Task 3: Validation Error Handling (ADMIN-07)**
- [ ] PASS - All validations work correctly
- [ ] FAIL - Issues found: ___________________

**Task 4: Role Gates (ADMIN-08)**
- [ ] PASS - Role access control works correctly
- [ ] FAIL - Issues found: ___________________

**Task 5: Regression Check**
- [ ] PASS - No regressions found
- [ ] FAIL - Regressions found: ___________________

**Overall Status:** _____ PASS / FAIL

### Additional Bugs Discovered

List any new bugs discovered during testing:

1. ___________________
2. ___________________
3. ___________________

### Screenshots

Attach screenshots for any failures or unexpected behavior.

---

## Test Data Reference

### Admin User
- Email: (check Phase 83 seed data)
- Password: (check Phase 83 seed data)

### HC User
- Email: (check Phase 83 seed data)
- Password: (check Phase 83 seed data)

### Test Workers
- Active workers seeded in Phase 83
- Inactive workers seeded in Phase 83
- Coach-coachee mappings seeded in Phase 85

---

## Notes

- This is a smoke test only - quick verification, not exhaustive testing
- Focus on verifying specific bugs that were fixed in 95-02 and 95-03
- Document any new bugs discovered for potential gap closure plans
- Follow the checkpoint return format after verification complete

---

**Verification Complete?** _____ YES / NO

**Verifier Signature:** ___________________

**Date:** ___________________
