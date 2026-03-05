---
wave: 1
depends_on: []
files_modified:
  - Views/CDP/Index.cshtml
autonomous: true
requirements: []
---

# Phase 99: Remove Deliverable Card from CDP Index

**Phase Goal:** CDP Index page no longer has broken Deliverable card link; users access deliverable details through Coaching Proton page

**User Decision Summary:**
- Remove the "Deliverable & Evidence" card from Views/CDP/Index.cshtml (lines 79-98)
- Do NOT create a replacement page or redirect
- Users access deliverable details via: CDP Index → Coaching Proton → "Lihat Detail" button
- This is UI cleanup only — no workflow, controller, model, or database changes

**Background:**
The Deliverable card links to `/CDP/Deliverable` without the required `id` parameter, causing a 404 error. The Deliverable action requires an `id` parameter because it's a detail view, not a list view. The correct workflow is to navigate through Coaching Proton, which provides the necessary context (ProgressId) for each deliverable.

**Success Criteria (must_haves):**
1. The "Deliverable & Evidence" card (lines 79-98) is completely removed from Views/CDP/Index.cshtml
2. Bootstrap grid auto-adjusts to 3 cards (Plan IDP, Coaching Proton, Dashboard Monitoring)
3. Other 3 cards still navigate correctly to their respective pages
4. Coaching Proton → "Lihat Detail" button still navigates to Deliverable detail page with correct id parameter
5. No CSS cleanup needed — styles are shared across all remaining cards

**Files Modified:**
- `Views/CDP/Index.cshtml` — Remove lines 79-98 (the Deliverable card div block)

---

## Wave 1: Remove Deliverable Card (Single Task)

This wave removes the broken navigation card. Bootstrap's responsive grid will auto-adjust from 4 cards to 3 cards without any explicit layout changes.

### Task 1: Remove Deliverable Card from CDP Index

**Description:** Delete lines 79-98 from Views/CDP/Index.cshtml to remove the "Deliverable & Evidence" navigation card that incorrectly links to `/CDP/Deliverable` without the required `id` parameter.

**Implementation Steps:**

1. **Open Views/CDP/Index.cshtml and identify the Deliverable card**
   - Locate the card with title "Deliverable" (line 88)
   - Verify it has icon `bi-file-earmark-check` (line 85)
   - Verify it has green theme (`bg-success` on line 84)
   - Confirm the entire card block spans lines 79-98

2. **Remove the entire card div block**
   - Delete from opening `<div class="col-12 col-md-6 col-lg-3">` (line 79)
   - To closing `</div>` (line 98)
   - Remove all 20 lines inclusive (79-98)
   - Do NOT remove any other cards — verify by checking adjacent line numbers

3. **Verify the remaining grid structure**
   - Confirm 3 cards remain: Plan IDP (lines 17-35), Coaching Proton (lines 38-56), Dashboard Monitoring (lines 59-77)
   - Confirm `<div class="row g-4">` wrapper still exists (line 15)
   - Confirm container closing div still exists (line 100)

4. **Do NOT modify CSS styles**
   - Lines 105-122 styles are shared across all 3 remaining cards
   - No orphaned CSS classes — all styles still in use

5. **Do NOT modify controller, model, or database**
   - CDPController.Deliverable action remains unchanged
   - No routing changes
   - No database changes

**Acceptance Criteria:**
- Lines 79-98 are completely removed from Views/CDP/Index.cshtml
- File contains exactly 103 lines after removal (was 123, minus 20 lines)
- No syntax errors in Razor view
- Bootstrap grid has 3 cards inside `<div class="row g-4">` wrapper
- Other 3 cards unchanged with correct href attributes

**Verification Commands:**
```bash
# Count lines in modified file (should be 103)
wc -l Views/CDP/Index.cshtml

# Verify "Deliverable" string no longer appears in Index.cshtml (except in comments)
grep -n "Deliverable" Views/CDP/Index.cshtml

# Verify 3 card divs remain with col-lg-3 class
grep -c 'class="col-12 col-md-6 col-lg-3"' Views/CDP/Index.cshtml
```

**Browser Verification Steps:**
1. Navigate to `/CDP/Index`
2. Confirm only 3 cards displayed (Plan IDP, Coaching Proton, Dashboard Monitoring)
3. Confirm "Deliverable" card no longer visible
4. Confirm grid layout adjusts correctly:
   - 3 cards per row on lg screens (≥992px)
   - 2 cards per row on md screens (≥768px) with gap
   - 1 card per row on xs screens
5. Click "Coaching Proton" card → verify navigates to `/CDP/CoachingProton`
6. On Coaching Proton page, click any "Lihat Detail" button → verify navigates to `/CDP/Deliverable?id={x}` with correct id parameter

**Expected Result:** Deliverable card removed, 3 remaining cards display correctly, grid auto-adjusts, Coaching Proton → Deliverable detail flow still works

---

## Post-Execution Verification

### Phase Gate Checklist

After completing Wave 1, verify the following:

**Functional Requirements:**
- [ ] Deliverable card completely removed from Views/CDP/Index.cshtml
- [ ] CDP Index displays exactly 3 navigation cards
- [ ] Bootstrap grid auto-adjusts to 3 cards with proper responsive breakpoints
- [ ] Other 3 cards (Plan IDP, Coaching Proton, Dashboard Monitoring) navigate correctly
- [ ] Coaching Proton → "Lihat Detail" → Deliverable detail flow still works

**Code Quality:**
- [ ] No syntax errors in Views/CDP/Index.cshtml
- [ ] No broken HTML structure (all divs properly closed)
- [ ] No orphaned CSS references (styles 105-122 still used by remaining cards)
- [ ] No controller or model changes (only view modified)

**Workflow Verification:**
- [ ] Users can still access deliverable details via Coaching Proton page
- [ ] No 404 errors when clicking remaining cards
- [ ] No visual layout issues (grid gap acceptable on lg screens)

**Backward Compatibility:**
- [ ] Deliverable detail page (`CDP/Deliverable?id={x}`) still functional
- [ ] No regression to Coaching Proton page functionality
- [ ] No regression to Plan IDP or Dashboard pages

### Regression Test Summary

**What Changed:**
- Removed: Deliverable navigation card from CDP Index (lines 79-98)
- Unchanged: CDPController, routing, models, database, CSS styles, other 3 cards

**What to Test:**
1. CDP Index page loads without errors
2. 3 cards display in responsive grid layout
3. All card links navigate correctly
4. Coaching Proton → Deliverable detail flow works
5. No visual layout issues on different screen sizes

**Rollback Plan:**
If issues arise, restore lines 79-98 from git history:
```bash
git checkout HEAD -- Views/CDP/Index.cshtml
```

---

**Phase 99 Plan Complete**
