# Task 3: Records Page Browser Verification

**Plan:** 93-04  
**Task:** 3 - Smoke test Records page (Worker role)  
**Status:** Ready for verification

## Task Objective

Verify the Records 2-tab layout (Assessment Online / Training Manual) works correctly:
- Both tabs render without errors
- Pagination works (if data exists)
- Dates formatted in Indonesian locale
- Row clicks navigate to Results page

## Test Setup

**Login:** Worker user (L1-L4 role level)  
**Starting URL:** `/CMP/Records`  
**Test Data:** Need assessment records and training records in database

## Verification Steps

### Step 1: Navigate to Records page

1. Login as Worker user
2. Navigate to `/CMP/Records`
3. Verify page loads without errors
4. Check browser console for errors

**Expected:** Page loads, no 500 errors, tabs visible

### Step 2: Test Assessment Online tab

1. Click "Assessment Online" tab
2. Verify table renders
3. Check date formatting (should be Indonesian locale: "05 Mar 2026")
4. Check for null reference exceptions in table data
5. If pagination exists, click page 2 or "Next"

**Expected:** 
- Tab switches without errors
- Dates show in Indonesian format
- No null exceptions in data display
- Pagination works (if data exists)

### Step 3: Test Training Manual tab

1. Click "Training Manual" tab
2. Verify table renders
3. Check date formatting
4. Check for null reference exceptions
5. If pagination exists, test pagination

**Expected:**
- Tab switches without errors
- Dates show in Indonesian format
- No null exceptions
- Pagination works (if data exists)

### Step 4: Test row navigation

1. In either tab, click an assessment/training row
2. Verify navigation to Results/Detail page
3. Check if Results page loads correctly

**Expected:** Row click navigates to correct detail page

### Step 5: Check for JavaScript errors

1. Open browser DevTools Console
2. Navigate between tabs
3. Test pagination
4. Click rows
5. Monitor for any JavaScript errors

**Expected:** No JavaScript errors during interaction

## Known Issues to Note

From Task 2 verification:
- Console shows: "Uncaught TypeError: Cannot set properties of null (setting 'innerHTML')"
- WebSocket connection failed

**Action:** Note if these errors appear on Records page too

## Verification Checklist

- [ ] Records page loads (Worker role)
- [ ] Assessment Online tab renders without errors
- [ ] Training Manual tab renders without errors
- [ ] Dates formatted in Indonesian locale (both tabs)
- [ ] No null reference exceptions in data display
- [ ] Pagination works (if records exist)
- [ ] Row clicks navigate to Results/Detail page
- [ ] No JavaScript errors during tab switching
- [ ] No JavaScript errors during pagination
- [ ] No JavaScript errors during row clicks

## Decision Point

After verification:

**PASS if:**
- Both tabs load without errors
- Dates formatted correctly
- No null exceptions
- Navigation works

**FAIL if:**
- Tab switching causes errors
- Dates not localized
- Null exceptions crash page
- Navigation broken

**Result:** [Awaiting user verification]
