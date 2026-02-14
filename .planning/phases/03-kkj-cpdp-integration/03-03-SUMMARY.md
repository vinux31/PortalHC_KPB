---
phase: 03-kkj-cpdp-integration
plan: 03
subsystem: competency-gap-analysis
tags: [gap-analysis, radar-chart, idp-suggestions, cpdp-integration, user-facing]
dependency_graph:
  requires:
    - 03-01-competency-tracking (UserCompetencyLevel, PositionTargetHelper)
    - 02-03-chart-js (Chart.js integration pattern)
  provides:
    - competency-gap-visualization
    - automatic-idp-suggestions
    - cpdp-driven-recommendations
  affects:
    - CMP/Index (navigation)
    - User experience (competency visibility)
tech_stack:
  added:
    - Chart.js radar charts
    - CPDP-based IDP suggestion engine
  patterns:
    - Gap calculation: target - current level
    - IDP suggestion generation from CPDP syllabus
    - HC/Admin multi-user view selector
key_files:
  created:
    - Views/CMP/CompetencyGap.cshtml
  modified:
    - Controllers/CMPController.cs (CompetencyGap action, GenerateIdpSuggestion helper)
    - Views/CMP/Index.cshtml (Gap Analysis card)
decisions:
  - decision: "Display top 8 gaps in radar chart"
    rationale: "Readability - too many competencies make radar chart cluttered"
    impact: "Users see highest-priority gaps at a glance"
  - decision: "Gap color coding: red (>=3), orange (2), yellow (1), green (0)"
    rationale: "Visual hierarchy for gap severity"
    impact: "Users instantly identify critical gaps"
  - decision: "IDP suggestion matches CPDP via string contains (case-insensitive)"
    rationale: "Flexible matching handles competency name variations"
    impact: "More accurate CPDP-to-competency mapping"
  - decision: "Status badges: Met (green), Gap (warning), Not Started (secondary)"
    rationale: "Bootstrap standard badge colors for status indication"
    impact: "Consistent UI/UX with existing assessment results"
metrics:
  tasks_completed: 2
  tasks_total: 2
  files_created: 1
  files_modified: 2
  duration_minutes: 3
  completed_date: 2026-02-14
---

# Phase 03 Plan 03: Gap Analysis Dashboard Summary

**One-liner:** Radar chart gap analysis dashboard with CPDP-driven IDP suggestions showing current vs target competency levels

## What Was Built

Built the competency gap analysis dashboard that visualizes user competency levels against position targets using a radar chart, calculates gaps, and generates actionable IDP suggestions from CPDP training data.

### Controller Logic (Task 1)

**CompetencyGap Action:**
- Determines target user: HC/Admin can view any user via userId parameter, regular users view themselves
- Filters KKJ competencies to those relevant for user's position (target > 0)
- Queries UserCompetencyLevels to get current achievement
- Checks existing IdpItems to flag competencies with active IDP activities
- Calculates gap: target - current
- Generates status: "Met" (gap <= 0), "Not Started" (current = 0), "Gap" (otherwise)
- Orders by largest gap first for priority visibility
- Provides user list to HC/Admin for dropdown selector

**GenerateIdpSuggestion Helper:**
- Matches competency to CPDP items using case-insensitive contains logic
- Returns specific training suggestion with syllabus and target deliverable if CPDP match found
- Falls back to generic suggestions based on gap size:
  - Gap >= 3: "Recommend structured training program"
  - Gap == 2: "Consider intermediate-level assessment or on-the-job training"
  - Gap == 1: "Schedule next-level assessment to advance"

### View Implementation (Task 2)

**CompetencyGap.cshtml:**

1. **User Selector (HC/Admin only):** Dropdown to select any user, triggers page reload with userId parameter

2. **User Info Card:** Displays selected user's name, position, section

3. **Summary Statistics (4 cards):**
   - Total Competencies: Count of competencies with target > 0 for position
   - Met Target: Count with gap <= 0 (green)
   - Gap Detected: Count with gap > 0 (warning)
   - Overall Progress: Percentage with progress bar

4. **Radar Chart (Chart.js):**
   - Shows top 8 competencies ordered by gap size
   - Two datasets: Current Level (blue, solid) and Target Level (red, dashed)
   - Scale: 0-5 with stepSize 1
   - Responsive with legend at bottom
   - Empty state message if no competencies for position

5. **Gap Details Table:**
   - Columns: Competency, Skill Group, Current, Target, Gap, Status, Last Assessment, Suggested Action
   - Color-coded gap badges: red (>= 3), orange (2), yellow (1), green (0)
   - Status badges: Met (green), Gap (warning), Not Started (secondary)
   - Last Assessment: Shows title and date of most recent assessment that contributed to level
   - Suggested Action: CPDP-based training recommendation with "IDP Active" badge if IdpItem exists
   - Empty state message if no targets defined for position

**Index.cshtml Update:**
- Added "Gap Analysis" card with bi-bar-chart-steps icon
- Warning-themed card (yellow) to indicate gap identification focus
- Links to /CMP/CompetencyGap

## Success Criteria Verification

- [x] User can view current vs target competency levels (Success Criterion 1) — Table shows CurrentLevel / TargetLevel
- [x] Radar chart gap analysis visualization (Success Criterion 2) — Chart.js radar with 8 highest-gap competencies
- [x] Automatic IDP suggestions from CPDP data (Success Criterion 3) — GenerateIdpSuggestion matches CPDP items
- [x] HC can view any user's gap analysis (must-have truth) — User selector dropdown for HC/Admin
- [x] System handles edge cases — Empty state for no position, no competencies, no levels

## Deviations from Plan

None - plan executed exactly as written.

## Key Achievements

1. **Visual Gap Analysis:** Radar chart makes competency profile immediately visible - users see at a glance where they stand vs targets

2. **CPDP Integration:** IDP suggestions pull from actual CPDP training syllabus data, creating a direct link from gap → recommended training → deliverable

3. **Smart Suggestion Fallback:** Even when CPDP doesn't have exact match, generates gap-size-appropriate generic suggestions

4. **IDP Activity Tracking:** Shows "IDP Active" badge when user already has IDP item targeting a gapped competency - prevents duplicate efforts

5. **Multi-user Support:** HC/Admin can view any user's gaps without switching accounts - critical for coaching and gap closure monitoring

## Technical Notes

**Chart.js Integration:**
- Uses same Chart.js pattern from Phase 2 (02-03 Chart.js analytics)
- Radar type chosen for competency profiling (industry standard for skills assessment)
- Top 8 limit prevents chart clutter while showing highest-priority gaps

**Gap Calculation:**
- Formula: `gap = target - current`
- Negative gaps treated as "Met" (exceeded target)
- Zero current treated as "Not Started" vs "Gap" for users with some progress

**CPDP Matching:**
- Bidirectional contains check: `cpdp.NamaKompetensi.Contains(kkj.Kompetensi)` OR `kkj.Kompetensi.Contains(cpdp.NamaKompetensi)`
- Handles competency name variations (e.g., "Process Control" matches "Advanced Process Control")
- Case-insensitive for robustness

**Performance:**
- Filters competencies to relevant position targets first (avoids loading all 60+ KKJ items)
- Uses single query for UserCompetencyLevels with Include for AssessmentSession (prevents N+1)
- Loads all IdpItems and CpdpItems upfront for in-memory matching (faster than per-competency queries)

## Files Modified

**Created:**
- `Views/CMP/CompetencyGap.cshtml` — Gap analysis dashboard with radar chart and table

**Modified:**
- `Controllers/CMPController.cs` — CompetencyGap action, GenerateIdpSuggestion helper, added usings for Models.Competency and Helpers
- `Views/CMP/Index.cshtml` — Added Gap Analysis navigation card

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | 88fa5da | CompetencyGap controller action with gap calculation and IDP suggestions |
| 2 | 0286e7a | CompetencyGap view with radar chart and gap table |

## What's Next

**Plan 03-04 (next):** HC Gap Reports Dashboard - aggregate gap analysis across teams, sections, positions for HC oversight and training planning.

**Integration points:**
- IDP module can consume SuggestedAction data to pre-populate IDP creation form
- Assessment results can link to gap analysis (e.g., "View how this assessment affected your gaps")
- Training records can show competency impact (e.g., "This training closed 3 gaps")

## Self-Check: PASSED

**Files exist:**
- FOUND: Views/CMP/CompetencyGap.cshtml
- FOUND: Controllers/CMPController.cs
- FOUND: Views/CMP/Index.cshtml

**Commits exist:**
- FOUND: 88fa5da (Task 1)
- FOUND: 0286e7a (Task 2)

**Build verification:**
- Build succeeded with 0 errors

**Key patterns present:**
- FOUND: `public async Task<IActionResult> CompetencyGap` in CMPController.cs
- FOUND: `competencyRadarChart` in CompetencyGap.cshtml
- FOUND: `CompetencyGapViewModel` in CMPController.cs
- FOUND: `GenerateIdpSuggestion` in CMPController.cs
- FOUND: `@Url.Action("CompetencyGap", "CMP")` in Index.cshtml
