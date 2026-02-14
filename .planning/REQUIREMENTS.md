# Requirements

**Project:** Portal HC KPB - CMP Assessment Completion
**Scope:** Complete the assessment workflow with results, pass/fail logic, and HC reporting
**Priority:** CMP Module (Competency Management Platform)

## Overview

Complete the existing assessment system so users can see their results after taking an assessment, and HC staff can view/analyze all assessment results across users.

**Current State:**
- ✅ Assessment creation and assignment works
- ✅ Users can take exams
- ✅ Scoring is calculated automatically
- ❌ No results page (users just get redirected to lobby)
- ❌ No pass/fail logic
- ❌ No answer review
- ❌ No HC reports dashboard

## Functional Requirements

### FR1: Assessment Results Page

**User Story:** As a Coachee/Worker, after completing an assessment, I want to immediately see my results so I know how I performed.

**Acceptance Criteria:**
1. After submitting an assessment, user is redirected to Results page (not lobby)
2. Results page displays:
   - Assessment title and category
   - Date/time completed
   - **Score** (percentage and raw score: "85% (17/20 points)")
   - **Pass/Fail status** with visual indicator (green badge for Pass, red for Fail)
   - **Passing threshold** (e.g., "Passing score: 70%")
   - Option to view certificate (if passed)
   - Button to return to Assessment lobby

**Business Rules:**
- Results are only visible after assessment is submitted and marked "Completed"
- User can only view their own results (unless Admin/HC)
- Results page is accessible from Assessment lobby for completed assessments

**UI/UX:**
- Use color coding: Green for pass, red for fail
- Show score prominently with large font
- Include motivational message: "Congratulations! You passed." or "Keep trying. Review the material and try again."

---

### FR2: Answer Review (Conditional)

**User Story:** As a Coachee, after completing an assessment, I want to review which questions I got right or wrong (if the HC allows) so I can learn from my mistakes.

**Acceptance Criteria:**
1. If HC enabled "Allow Answer Review" for this assessment:
   - Results page shows expandable section "Review Answers"
   - For each question, display:
     - Question text
     - All options
     - User's selected answer (highlighted)
     - Correct answer (highlighted in green)
     - Visual indicator: ✓ (correct) or ✗ (incorrect)
2. If HC disabled answer review:
   - No review section shown
   - Message: "Answer review is not available for this assessment."

**Business Rules:**
- Answer review visibility is controlled per-assessment by HC
- Default: Answer review is ENABLED (most assessments allow learning)
- HC can disable for high-stakes exams or certifications

**UI/UX:**
- Use accordion/collapse component for review (not shown by default)
- Color code: Green for correct answers, Red for incorrect
- Use clear icons (✓ ✗) for quick scanning

---

### FR3: Configurable Pass Threshold

**User Story:** As HC Staff, I want to set different passing percentages for different assessment categories so I can maintain appropriate standards.

**Acceptance Criteria:**
1. When creating or editing an assessment, HC can set:
   - **Pass Percentage** (integer, 0-100)
   - Default: 70%
2. Pass percentage is stored in database per assessment
3. Pass/fail logic uses this threshold:
   - If Score >= PassPercentage → Status = "Passed"
   - If Score < PassPercentage → Status = "Failed"
4. Results page displays the pass percentage

**Category-Based Defaults (Suggested):**
- "Assessment OJ" → 70%
- "IHT" → 70%
- "Licencor" → 80% (higher stakes)
- "OTS" → 70%
- "Mandatory HSSE Training" → 100% (safety critical)

**Business Rules:**
- HC can override default per-assessment
- Pass percentage applies to all users taking that specific assessment instance
- Cannot change pass percentage after first user submits (data integrity)

---

### FR4: Assessment Configuration Enhancements

**User Story:** As HC Staff, I want to control answer review and pass thresholds when creating assessments so I can tailor the assessment experience.

**Acceptance Criteria:**
1. **CreateAssessment form** includes:
   - "Pass Percentage" input (number, 0-100, default 70)
   - "Allow Answer Review" checkbox (default: checked)
2. **EditAssessment form** includes same fields
3. Form validation:
   - Pass percentage must be 0-100
   - Cannot change pass percentage if Status = "Completed" and responses exist
4. Database schema updated:
   - `AssessmentSession` table adds:
     - `PassPercentage` (int, default 70)
     - `AllowAnswerReview` (bool, default true)
     - `IsPassed` (bool, nullable) - calculated on submission

**UI/UX:**
- Group these settings under "Assessment Settings" section
- Show helpful tooltip: "Pass Percentage: Minimum score required to pass (e.g., 70 = 70%)"
- Show tooltip for answer review: "Allow users to see which questions they answered correctly"

---

### FR5: Assessment History & Result Access

**User Story:** As a Coachee, I want to access my past assessment results so I can track my progress over time.

**Acceptance Criteria:**
1. In Assessment lobby (personal view):
   - Completed assessments show "View Results" link
   - Clicking opens Results page with full details
2. Results page accessible via URL: `/CMP/AssessmentResult/{id}`
3. Authorization:
   - User can view their own results
   - Admin/HC can view any user's results
   - Other users cannot access
4. Results page includes:
   - All data from FR1 (score, pass/fail, etc.)
   - "Completed on: [date/time]"
   - Time taken (if tracked)

---

### FR6: HC Reports Dashboard (Phase 2 - Future)

**User Story:** As HC Staff, I want to see a dashboard of all assessment results so I can analyze performance and identify training needs.

**Scope:** NOT in Phase 1 (Results Page). This will be Phase 2.

**Future Requirements:**
- List all assessments with summary stats (total assigned, completed, pass rate)
- Filter by category, date range, section, user
- Export to Excel/PDF
- Individual user assessment history
- Performance analytics (charts, trends)

---

## Non-Functional Requirements

### NFR1: Performance
- Results page loads in < 2 seconds
- Answer review loads all questions in single query (no N+1)

### NFR2: Security
- Results are only accessible to:
  - Assessment owner (UserId matches)
  - Admin role
  - HC role
- Unauthorized access returns 403 Forbidden

### NFR3: Data Integrity
- Pass/fail status calculated atomically on submission
- Cannot change pass percentage after users have submitted

### NFR4: Browser Compatibility
- Works in Edge, Chrome, Firefox (latest 2 versions)
- Mobile responsive (Bootstrap)

## Database Schema Changes

**AssessmentSession table - Add columns:**

```sql
ALTER TABLE AssessmentSessions ADD PassPercentage INT DEFAULT 70;
ALTER TABLE AssessmentSessions ADD AllowAnswerReview BIT DEFAULT 1;
ALTER TABLE AssessmentSessions ADD IsPassed BIT NULL;
ALTER TABLE AssessmentSessions ADD CompletedAt DATETIME NULL;
```

**Migration Name:** `AddAssessmentResultFields`

## User Interface

### New Pages

1. **AssessmentResult.cshtml** - Results page
   - Route: `/CMP/AssessmentResult/{id}`
   - Controller action: `CMPController.AssessmentResult(int id)`

### Modified Pages

1. **CreateAssessment.cshtml** - Add pass percentage and answer review fields
2. **EditAssessment.cshtml** - Add pass percentage and answer review fields
3. **Assessment.cshtml** (lobby) - Add "View Results" link for completed assessments

## API/Controller Changes

**CMPController.cs:**

1. **New Action:** `AssessmentResult(int id)` [HttpGet]
   - Load assessment with questions, options, user responses
   - Calculate pass/fail
   - Return view with result model

2. **Modified Action:** `SubmitExam(int id, Dictionary<int, int> answers)` [HttpPost]
   - After calculating score, also:
     - Set `IsPassed = (Score >= PassPercentage)`
     - Set `CompletedAt = DateTime.UtcNow`
   - Redirect to `AssessmentResult` instead of `Assessment`

3. **Modified Action:** `CreateAssessment(AssessmentSession model, List<string> UserIds)` [HttpPost]
   - Accept `PassPercentage` and `AllowAnswerReview` from form
   - Validate pass percentage (0-100)

4. **Modified Action:** `EditAssessment(int id, AssessmentSession model)` [HttpPost]
   - Accept `PassPercentage` and `AllowAnswerReview`
   - Prevent changing pass percentage if responses exist

## Test Scenarios

### Happy Path
1. HC creates assessment with 70% pass threshold, answer review enabled
2. User takes assessment, scores 85%
3. Results page shows: "You passed! 85% (17/20 points)"
4. User expands answer review, sees which questions were correct/incorrect
5. User can view certificate

### Edge Cases
1. User scores exactly at threshold (70%) → Should show "Passed"
2. User scores 69% → Should show "Failed"
3. Assessment has 0 questions → Should not crash (show message)
4. Answer review disabled → Review section hidden
5. User tries to view another user's results → 403 Forbidden

### Security Tests
1. Non-owner tries to access results URL → Blocked
2. Unauthenticated user → Redirected to login
3. Admin can view any results → Allowed
4. HC can view any results → Allowed

## Success Metrics

**Phase 1 Complete When:**
- ✅ Users see results immediately after assessment submission
- ✅ Pass/fail status displayed correctly
- ✅ Answer review works (if enabled)
- ✅ HC can configure pass threshold per assessment
- ✅ Completed assessments accessible from lobby
- ✅ All test scenarios pass

**User Satisfaction:**
- Users no longer confused after completing assessments
- HC can validate competency achievement
- Clear feedback loop for learning

## Out of Scope (Phase 1)

❌ HC Reports Dashboard (Phase 2)
❌ Export to Excel/PDF (Phase 2)
❌ Time tracking during exam (future)
❌ Email notifications (future)
❌ Performance analytics charts (Phase 2)
❌ Bulk result management (Phase 2)
❌ Integration with KKJ/CPDP (Phase 3)

## Dependencies

**Requires:**
- Existing AssessmentSession, AssessmentQuestion, UserResponse models
- CMPController with SubmitExam action
- Bootstrap 5 for UI components
- Entity Framework Core for database

**No external dependencies needed.**

## Notes

- Keep consistent with existing ASP.NET MVC patterns
- Use Razor views (no JavaScript frameworks)
- Follow Bootstrap styling conventions
- Maintain role-based authorization
- Use Entity Framework migrations for schema changes

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| FR1 | Phase 1 | ✓ Complete |
| FR2 | Phase 1 | ✓ Complete |
| FR3 | Phase 1 | ✓ Complete |
| FR4 | Phase 1 | ✓ Complete |
| FR5 | Phase 1 | ✓ Complete |
| FR6 | Phase 2 | Pending |
