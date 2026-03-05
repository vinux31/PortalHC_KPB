# Task 5: Mapping Page Browser Verification

**Plan:** 93-04  
**Task:** 5 - Smoke test Mapping page (all roles)  
**Status:** Ready for verification

## Task Objective

Verify CPDP Mapping section filtering and file downloads:
- L1-L4 see all section tabs
- L5-L6 see only own unit tab
- Tab switching works without errors
- File downloads work for all roles
- Same pattern as KKJ Matrix

## Test Setup

**Test 1 - Worker (L1-L4):** Standard worker account  
**Test 2 - SectionHead (L5):** Section head with specific unit assignment  
**Starting URL:** `/CMP/Mapping`

## Verification Steps

### Part A: Worker Role (L1-L4)

#### Step 1: Navigate to CPDP Mapping

1. Login as Worker user (L1-L4)
2. Navigate to `/CMP/Mapping`
3. Verify page loads without errors
4. Check browser console

**Expected:** Page loads, all 4 section tabs visible (RFCC, GAST, NGP, DHT)

#### Step 2: Test tab switching (all sections)

1. Click "RFCC" tab → Verify CPDP files load
2. Click "GAST" tab → Verify CPDP files load
3. Click "NGP" tab → Verify CPDP files load
4. Click "DHT" tab → Verify CPDP files load

**Expected:**
- All tabs visible
- Tab switching works without errors
- Files load for each section
- No null exceptions

#### Step 3: Test file download

1. Select any section with files
2. Click a download link
3. Verify file downloads

**Expected:** File downloads successfully

#### Step 4: Check date formatting (if any dates shown)

1. Look for any date columns (upload date, etc.)
2. Verify dates show in Indonesian locale

**Expected:** Dates formatted as "05 Mar 2026" if displayed

### Part B: SectionHead Role (L5-L6)

#### Step 1: Navigate with SectionHead

1. Login as SectionHead (L5 with specific unit, e.g., "GAST")
2. Navigate to `/CMP/Mapping`
3. Check which tabs are visible

**Expected:** Only own unit tab visible (e.g., only "GAST")

#### Step 2: Test file download

1. Click the visible tab
2. Click a download link
3. Verify file downloads

**Expected:** File downloads work for SectionHead too

#### Step 3: Check for errors

1. Open browser DevTools Console
2. Switch tabs
3. Test downloads
4. Monitor for JavaScript errors

**Expected:** No errors during interaction

## Known Issues to Note

From Task 2 verification:
- Console shows: "Uncaught TypeError: Cannot set properties of null (setting 'innerHTML')"
- WebSocket connection failed

**Action:** Note if these errors appear on Mapping page

## Verification Checklist

**Worker Role (L1-L4):**
- [ ] CPDP Mapping page loads
- [ ] All 4 section tabs visible (RFCC, GAST, NGP, DHT)
- [ ] RFCC tab switches without errors
- [ ] GAST tab switches without errors
- [ ] NGP tab switches without errors
- [ ] DHT tab switches without errors
- [ ] File downloads work
- [ ] Dates formatted in Indonesian (if shown)
- [ ] No null exceptions when switching tabs

**SectionHead Role (L5-L6):**
- [ ] CPDP Mapping page loads
- [ ] Only own unit tab visible
- [ ] Tab switching works without errors
- [ ] File downloads work
- [ ] No JavaScript errors

## Decision Point

**PASS if:**
- Role-based filtering works (L1-L4 see all, L5 see own)
- Tab switching works without errors
- File downloads work for both roles
- Dates formatted correctly (if shown)

**FAIL if:**
- Wrong tabs shown for role level
- Tab switching causes errors
- File downloads broken

**Result:** [Awaiting user verification]
