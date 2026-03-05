# User Verification Guide - Plan 93-04

**Status:** Ready for your verification  
**Tasks Remaining:** 3, 4, 5, 6  
**Estimated Time:** 20-30 minutes

## What's Been Done

- ✅ Task 1: CMP Hub & Navigation - APPROVED
- ✅ Task 2: Assessment Page (Worker) - APPROVED (with notes)
- 📋 Task 3: Records Page (Worker) - AWAITING YOUR VERIFICATION
- 📋 Task 4: KKJ Matrix (All Roles) - AWAITING YOUR VERIFICATION
- 📋 Task 5: Mapping Page (All Roles) - AWAITING YOUR VERIFICATION
- ⏳ Task 6: Bug Documentation - PENDING

## Your Verification Tasks

### Task 3: Records Page (Worker Role)

**File:** `TASK_3_VERIFICATION.md`

**What to test:**
1. Login as Worker → Go to `/CMP/Records`
2. Test "Assessment Online" tab - check dates, pagination
3. Test "Training Manual" tab - check dates, pagination
4. Click a row → verify Results page loads
5. Check console for errors

**Key checks:**
- Both tabs render without errors
- Dates show in Indonesian format (e.g., "05 Mar 2026")
- No null exceptions
- Row clicks navigate correctly

---

### Task 4: KKJ Matrix (All Roles)

**File:** `TASK_4_VERIFICATION.md`

**What to test (Part A - Worker):**
1. Login as Worker (L1-L4) → Go to `/CMP/Kkj`
2. Verify all 4 tabs visible (RFCC, GAST, NGP, DHT)
3. Click each tab → verify files load
4. Test file download

**What to test (Part B - SectionHead):**
1. Login as SectionHead (L5) → Go to `/CMP/Kkj`
2. Verify only own unit tab visible
3. Test file download

**Key checks:**
- Role-based filtering works (L1-L4 see all, L5 see own)
- Tab switching works without errors
- File downloads work

---

### Task 5: Mapping Page (All Roles)

**File:** `TASK_5_VERIFICATION.md`

**What to test (Part A - Worker):**
1. Login as Worker (L1-L4) → Go to `/CMP/Mapping`
2. Verify all 4 tabs visible
3. Click each tab → verify files load
4. Test file download
5. Check date formatting (if shown)

**What to test (Part B - SectionHead):**
1. Login as SectionHead (L5) → Go to `/CMP/Mapping`
2. Verify only own unit tab visible
3. Test file download

**Key checks:**
- Same pattern as KKJ Matrix
- Role-based filtering works
- File downloads work
- Dates formatted correctly (if shown)

---

### Task 6: Bug Documentation

**File:** `BUG_REPORT_TEMPLATE.md`

**What to do:**
- Document any bugs found during Tasks 3-5
- Use template to record:
  - Severity (Critical/High/Medium/Low)
  - Steps to reproduce
  - Expected vs Actual behavior
  - Decision (fix now vs defer)

**Note:** The innerHTML error from Task 2 is already documented as Low severity, non-blocking.

---

## Quick Reference

### Test Users

You'll need:
- Worker account (L1-L4 role level)
- SectionHead account (L5 with specific unit, e.g., "RFCC" or "GAST")

### Test URLs

- CMP Hub: `/CMP/Index`
- Assessment: `/CMP/Assessment`
- Records: `/CMP/Records`
- KKJ Matrix: `/CMP/Kkj`
- Mapping: `/CMP/Mapping`

### What to Look For

✅ **PASS indicators:**
- Pages load without 500 errors
- Tabs switch smoothly
- Dates show as "05 Mar 2026" (Indonesian format)
- Role-based filtering works correctly
- File downloads work
- No null exceptions

❌ **FAIL indicators:**
- Pages crash or show raw exceptions
- Tabs don't switch
- Dates show wrong format
- Wrong tabs shown for role level
- File downloads broken

---

## After Verification

Once you've tested all remaining tasks, reply with your results:

**Example format:**
```
Task 3: PASS - both tabs work, dates correct
Task 4: PASS - role filtering works, downloads work
Task 5: PASS - same as KKJ, no issues
Task 6: No new bugs found (only the innerHTML error from Task 2)

Overall: APPROVE
```

Or if you find bugs:
```
Task 3: PASS
Task 4: FAIL - SectionHead sees all tabs instead of just own unit
Task 5: NOT TESTED
Task 6: Bug documented in template

Overall: STOP for bug fix
```

---

## Files for Reference

All verification guides are in:
```
.planning/phases/93-cmp-section-audit/
├── TASK_3_VERIFICATION.md
├── TASK_4_VERIFICATION.md
├── TASK_5_VERIFICATION.md
├── BUG_REPORT_TEMPLATE.md
└── VERIFICATION_SUMMARY.md
```
