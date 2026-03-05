# Browser Verification Guide - Phase 98 Data Integrity Audit

> Manual browser testing guide for data integrity bug fixes

## Test Data Requirements

### Test Users (1 per role)
- **Admin**: Email=admin@test.com, Role=Admin, RoleLevel=1
- **HC**: Email=hc@test.com, Role=HC, RoleLevel=2
- **Coach**: Email=coach@test.com, Role=Coach, RoleLevel=3
- **Coachee**: Email=coachee@test.com, Role=Coachee, RoleLevel=5

**Note:** Use existing users from database - Phase 87 already has test data

## Test Flows

### Flow 1: Orphan Prevention - CoachCoacheeMapping (DATA-02)

**Purpose:** Verify orphaned mappings hidden when coach/coachee deactivated

**Test Steps:**
1. Login as Admin (admin@test.com)
2. Navigate to `/Admin/CoachCoacheeMapping`
3. Note the current mapping count
4. Navigate to `/Admin/ManageWorkers`
5. Find a coach or coachee that has active mappings
6. Deactivate that user (click "Nonaktifkan" button)
7. Return to `/Admin/CoachCoacheeMapping`
8. Verify the mapping count decreased (orphaned mapping hidden)
9. Verify the deactivated user's mappings no longer appear

**Expected Results:**
- [ ] Mapping list shows only mappings where BOTH coach and coachee are active
- [ ] Deactivating a coach hides all their mappings from the list
- [ ] Deactivating a coachee hides their assignment from coach view
- [ ] No orphaned mappings visible in UI (parent.IsActive filter working)

**Bugs Found:**
- <Document any issues with format: SEVERITY | Description | Steps to reproduce>

---

### Flow 2: Orphan Prevention - ProtonTrackAssignment Display (DATA-02)

**Purpose:** Verify assignments with inactive ProtonKompetensi hidden from mapping display

**Test Steps:**
1. Login as Admin (admin@test.com)
2. Navigate to `/Admin/ProtonData` (Silabus tab)
3. Find an active ProtonKompetensi (Silabus entry) that has track assignments
4. Deactivate that Silabus entry (click "Nonaktifkan")
5. Navigate to `/Admin/CoachCoacheeMapping`
6. Verify that coachees assigned to that track show empty ProtonTrack field

**Expected Results:**
- [ ] Coachee mappings show empty ProtonTrack display when ProtonKompetensi is inactive
- [ ] No "ghost" track assignments from inactive silabus entries
- [ ] Track display filtered by ProtonKompetensi.IsActive (orphan prevention working)

**Bugs Found:**
- <Document any issues>

---

### Flow 3: Orphan Prevention - Coaching Proton Progress (DATA-02)

**Purpose:** Verify orphaned progress hidden when coachee's track assignment inactive

**Test Steps:**
1. Login as HC (hc@test.com)
2. Navigate to `/CDP/CoachingProton`
3. Note the coachee count and progress data
4. Navigate to `/Admin/ManageWorkers`
5. Find a coachee that has active progress entries
6. Deactivate that coachee
7. Return to `/CDP/CoachingProton`
8. Verify the coachee no longer appears in the list
9. Verify their progress data is hidden (orphaned progress filtered out)

**Expected Results:**
- [ ] Coaching Proton page shows only coachees with active ProtonTrackAssignments
- [ ] Deactivating a coachee hides their progress from coach/HC view
- [ ] No orphaned progress records visible in UI (ProtonTrackAssignment.IsActive filter working)

**Bugs Found:**
- <Document any issues>

---

### Flow 4: AuditLog - Delete Question (DATA-03)

**Purpose:** Verify question deletion logged to AuditLog table

**Test Steps:**
1. Login as Admin (admin@test.com)
2. Navigate to `/Admin/ManageAssessment`
3. Select an assessment with existing questions
4. Click "Manage Questions" for that assessment
5. Click "Delete" button for a question
6. Confirm deletion
7. Navigate to `/Admin/AuditLog` (if page exists) OR check database directly
8. Search for AuditLog entry with ActionType="DeleteQuestion"

**Expected Results:**
- [ ] AuditLog entry created for question deletion
- [ ] AuditLog.ActorUserId matches current admin user
- [ ] AuditLog.Description includes question text snippet and assessment ID
- [ ] AuditLog.ActionType is "DeleteQuestion"
- [ ] AuditLog.TargetType is "AssessmentQuestion"
- [ ] AuditLog.TargetId matches deleted question ID

**Bugs Found:**
- <Document any issues>

---

### Flow 5: AuditLog - Import Questions (DATA-03)

**Purpose:** Verify bulk question import logged to AuditLog

**Test Steps:**
1. Login as HC (hc@test.com)
2. Navigate to `/Admin/ManageAssessment`
3. Select an assessment and create a package
4. Click "Import Questions" for that package
5. Prepare a small Excel file with 2-3 test questions
6. Upload the file and import
7. Note the success message with import count
8. Navigate to `/Admin/AuditLog` OR check database
9. Search for AuditLog entry with ActionType="ImportQuestions"

**Expected Results:**
- [ ] AuditLog entry created for import operation
- [ ] AuditLog.Description includes import count (e.g., "Imported 3 questions")
- [ ] AuditLog.Description includes package name and ID
- [ ] AuditLog.Description includes source (file name or "pasted text")
- [ ] AuditLog.ActionType is "ImportQuestions"
- [ ] Audit log shows who performed the import

**Bugs Found:**
- <Document any issues>

---

### Flow 6: AuditLog - Delete Training Record (DATA-03)

**Purpose:** Verify training record deletion logged to AuditLog

**Test Steps:**
1. Login as HC (hc@test.com)
2. Navigate to `/CMP/Records`
3. Find a training record for a worker
4. Click "Delete" button for that training record
5. Confirm deletion
6. Navigate to `/Admin/AuditLog` OR check database
7. Search for AuditLog entry with ActionType="DeleteTrainingRecord"

**Expected Results:**
- [ ] AuditLog entry created for training record deletion
- [ ] AuditLog.Description includes training title and worker name
- [ ] AuditLog.ActionType is "DeleteTrainingRecord"
- [ ] AuditLog.TargetType is "TrainingRecord"
- [ ] Audit log shows who performed the deletion

**Bugs Found:**
- <Document any issues>

---

### Flow 7: AuditLog - Archive KKJ File (DATA-03)

**Purpose:** Verify KKJ file archival logged to AuditLog

**Test Steps:**
1. Login as Admin (admin@test.com)
2. Navigate to `/Admin/KkjMatrix`
3. Select a bagian (section) tab with active files
4. Click "Archive" button for a KKJ file
5. Confirm archival
6. Navigate to `/Admin/AuditLog` OR check database
7. Search for AuditLog entry with ActionType="ArchiveKKJFile"

**Expected Results:**
- [ ] AuditLog entry created for KKJ file archival
- [ ] AuditLog.Description includes file name and bagian ID
- [ ] AuditLog.ActionType is "ArchiveKKJFile"
- [ ] AuditLog.TargetType is "KkjFile"
- [ ] Audit log shows who performed the archival

**Bugs Found:**
- <Document any issues>

---

## Bug Reporting Template

For each bug found, document:

### Bug #[N]
- **Severity:** Critical / High / Medium / Low
- **Requirement:** DATA-01 / DATA-02 / DATA-03
- **Flow:** <Flow name where bug found>
- **Description:** <Clear description of unexpected behavior>
- **Expected:** <What should happen>
- **Actual:** <What actually happened>
- **Steps to reproduce:**
  1. <Step 1>
  2. <Step 2>
  3. <Step 3>
- **Evidence:** <Screenshot or console error>

## Testing Notes

- **Browser:** Chrome/Edge recommended (DevTools support)
- **Test environment:** Development or staging (not production)
- **Session management:** Use Incognito/Private mode to isolate tests
- **Database verification:** For AuditLog tests, can directly query database to verify

### Direct Database Verification (Optional)

If AuditLog page is not available, verify via SQL:

```sql
-- Check recent audit entries
SELECT TOP 10 * FROM AuditLogs
ORDER BY Timestamp DESC

-- Check specific action types
SELECT * FROM AuditLogs
WHERE ActionType IN ('DeleteQuestion', 'ImportQuestions', 'DeleteTrainingRecord', 'ArchiveKKJFile')
ORDER BY Timestamp DESC
```

## Completion Checklist

- [ ] Flow 1: Orphan Prevention - CoachCoacheeMapping tested
- [ ] Flow 2: Orphan Prevention - ProtonTrackAssignment Display tested
- [ ] Flow 3: Orphan Prevention - Coaching Proton Progress tested
- [ ] Flow 4: AuditLog - Delete Question tested
- [ ] Flow 5: AuditLog - Import Questions tested
- [ ] Flow 6: AuditLog - Delete Training Record tested
- [ ] Flow 7: AuditLog - Archive KKJ File tested
- [ ] All bugs documented with severity and reproduction steps
- [ ] Results ready for regression testing summary

---

**This verification guide covers all 7 bug fixes from plan 98-04 (3 DATA-02 orphan prevention fixes + 4 DATA-03 AuditLog fixes)**
