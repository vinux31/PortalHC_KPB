# Task 4: KKJ Matrix Page Browser Verification

**Plan:** 93-04  
**Task:** 4 - Smoke test KKJ Matrix page (all roles)  
**Status:** Ready for verification

## Task Objective

Verify KKJ Matrix section filtering and file downloads:
- L1-L4 see all section tabs (RFCC, GAST, NGP, DHT)
- L5-L6 see only own unit tab
- Tab switching works without errors
- File downloads work for all roles

## Test Setup

**Test 1 - Worker (L1-L4):** Standard worker account  
**Test 2 - SectionHead (L5):** Section head with specific unit assignment  
**Starting URL:** `/CMP/Kkj`

## Verification Steps

### Part A: Worker Role (L1-L4)

#### Step 1: Navigate to KKJ Matrix

1. Login as Worker user (L1-L4)
2. Navigate to `/CMP/Kkj`
3. Verify page loads without errors
4. Check browser console

**Expected:** Page loads, all 4 section tabs visible (RFCC, GAST, NGP, DHT)

#### Step 2: Test tab switching (all sections)

1. Click "RFCC" tab → Verify KKJ files load
2. Click "GAST" tab → Verify KKJ files load
3. Click "NGP" tab → Verify KKJ files load
4. Click "DHT" tab → Verify KKJ files load

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

### Part B: SectionHead Role (L5-L6)

#### Step 1: Navigate with SectionHead

1. Login as SectionHead (L5 with specific unit, e.g., "RFCC")
2. Navigate to `/CMP/Kkj`
3. Check which tabs are visible

**Expected:** Only own unit tab visible (e.g., only "RFCC")

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

**Action:** Note if these errors appear on KKJ Matrix page

## Verification Checklist

**Worker Role (L1-L4):**
- [ ] KKJ Matrix page loads
- [ ] All 4 section tabs visible (RFCC, GAST, NGP, DHT)
- [ ] RFCC tab switches without errors
- [ ] GAST tab switches without errors
- [ ] NGP tab switches without errors
- [ ] DHT tab switches without errors
- [ ] File downloads work
- [ ] No null exceptions when switching tabs

**SectionHead Role (L5-L6):**
- [ ] KKJ Matrix page loads
- [ ] Only own unit tab visible
- [ ] Tab switching works without errors
- [ ] File downloads work
- [ ] No JavaScript errors

## Decision Point

**PASS if:**
- Role-based filtering works (L1-L4 see all, L5 see own)
- Tab switching works without errors
- File downloads work for both roles

**FAIL if:**
- Wrong tabs shown for role level
- Tab switching causes errors
- File downloads broken

**Result:** [Awaiting user verification]
