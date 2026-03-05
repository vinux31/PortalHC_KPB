# CMP Bug Inventory - Phase 93

**Created:** 2026-03-05
**Source:** Plan 93-01 Code Review
**Status:** Complete - 15 bugs identified

## Executive Summary

- **Total Bugs Found:** 15
- **Critical:** 3 (Null safety issues that can crash the application)
- **High:** 8 (Localization bugs affecting UX)
- **Medium:** 4 (Validation and error handling)
- **Low:** 0 (Minor code quality issues - deferred)

## Bug Categories

### 1. Null Safety Issues (Critical)

| ID | Location | Issue | Severity | Fix Plan |
|----|----------|-------|----------|----------|
| NS-01 | CMPController.cs:80 | `.First()` without `.Any()` check on availableBagians | Critical | 93-02 |
| NS-02 | CMPController.cs:85 | `.First()` without `.Any()` check on availableBagians (validation path) | Critical | 93-02 |
| NS-03 | CMPController.cs:927 | `.First()` without `.Any()` check on packages | Critical | 93-02 |

**Details:**

**NS-01 & NS-02: Kkj Action - Null Safety**
```csharp
// Line 80
var selectedBagian = availableBagians.FirstOrDefault(b => b.Name == section)
                      ?? availableBagians.First();

// Line 85
if (userLevel >= 5 && section != null && !availableBagians.Any(b => b.Name == section))
{
    selectedBagian = availableBagians.First();
}
```

**Impact:** If no bagians exist in database, both `.First()` calls throw `InvalidOperationException`.

**Fix:** Add guard before line 80:
```csharp
if (!availableBagians.Any())
{
    TempData["Error"] = "No KKJ sections available. Please contact administrator.";
    return RedirectToAction("Index");
}
```

---

**NS-03: StartExam - Null Safety**
```csharp
// Line 927
var sentinelPackage = packages.First();
```

**Impact:** If assessment group has no packages, `.First()` throws exception.

**Fix:**
```csharp
if (!packages.Any())
{
    TempData["Error"] = "No questions available for this assessment. Please contact administrator.";
    return RedirectToAction("Assessment");
}
var sentinelPackage = packages.First();
```

---

### 2. Date Localization Issues (High)

| ID | Location | Issue | Severity | Fix Plan |
|----|----------|-------|----------|----------|
| LOC-01 | Records.cshtml:142 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-03 |
| LOC-02 | Records.cshtml:188 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-03 |
| LOC-03 | Records.cshtml:194 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-03 |
| LOC-04 | Assessment.cshtml:142 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-03 |
| LOC-05 | Assessment.cshtml:146 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-03 |
| LOC-06 | Assessment.cshtml:260 | `.ToString("dd MMM yyyy HH:mm")` without Indonesian culture | High | 93-03 |
| LOC-07 | Assessment.cshtml:308 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-03 |
| LOC-08 | Kkj.cshtml:105 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-03 |

**Details:**

All locations use `.ToString("dd MMM yyyy")` or `.ToString("dd MMM yyyy HH:mm")` without specifying culture, which uses server culture (likely English) instead of Indonesian.

**Fix:** Add CultureInfo parameter:
```cshtml
@item.Date.ToString("dd MMM yyyy", new System.Globalization.CultureInfo("id-ID"))
```

**Better Fix:** Create extension method to avoid repetition:
```csharp
public static class DateTimeExtensions
{
    public static string ToIndonesianDate(this DateTime dt, string format = "dd MMM yyyy")
    {
        return dt.ToString(format, new System.Globalization.CultureInfo("id-ID"));
    }
}
```

Usage in views:
```cshtml
@item.Date.ToIndonesianDate()
```

---

### 3. Validation Issues (Medium)

| ID | Location | Issue | Severity | Fix Plan |
|----|----------|-------|----------|----------|
| VAL-01 | CMPController.cs:228-267 | SaveAnswer: No parameter validation (negative IDs possible) | Medium | 93-04 |
| VAL-02 | CMPController.cs:270-317 | SaveLegacyAnswer: No parameter validation | Medium | 93-04 |
| VAL-03 | CMPController.cs:350-381 | UpdateSessionProgress: No parameter validation | Medium | 93-04 |
| VAL-04 | CMPController.cs:1241 | ExamSummary: Dictionary parameter can be null | Medium | 93-04 |

**Details:**

**VAL-01, VAL-02, VAL-03: POST Actions Missing Parameter Validation**

These actions accept int parameters but don't validate they're positive numbers:
```csharp
public async Task<IActionResult> SaveAnswer(int sessionId, int questionId, int optionId)
public async Task<IActionResult> SaveLegacyAnswer(int sessionId, int questionId, int optionId)
public async Task<IActionResult> UpdateSessionProgress(int sessionId, int elapsedSeconds, int currentPage)
```

**Fix:** Add validation at start of each method:
```csharp
if (sessionId <= 0 || questionId <= 0 || optionId <= 0)
{
    return Json(new { success = false, error = "Invalid parameters" });
}
```

For UpdateSessionProgress:
```csharp
if (sessionId <= 0 || elapsedSeconds < 0 || currentPage < 1)
{
    return Json(new { success = false, error = "Invalid parameters" });
}
```

---

**VAL-04: ExamSummary Dictionary Can Be Null**
```csharp
public async Task<IActionResult> ExamSummary(int id, int? assignmentId, Dictionary<int, int> answers)
{
    // ... code that accesses answers without null check
    TempData["PendingAnswers"] = System.Text.Json.JsonSerializer.Serialize(answers);
```

**Fix:** Add null coalescing:
```csharp
public async Task<IActionResult> ExamSummary(int id, int? assignmentId, Dictionary<int, int> answers)
{
    answers ??= new Dictionary<int, int>();
    // ... rest of method
}
```

---

### 4. Known Gap Issues (Investigation)

| ID | Location | Issue | Severity | Fix Plan |
|----|----------|-------|----------|----------|
| GAP-01 | Results.cshtml:103-122 | PositionTargetHelper missing for competency display (ASSESS-04) | Low | Deferred |

**Details:**

**GAP-01: PositionTargetHelper Missing**

Phase 90 dropped KKJ tables including PositionTargetHelper. Results page shows competencies but without position/role context.

**Current State:**
- Model: `CompetencyGainItem` has `CompetencyName` and `LevelGranted` only
- View: Shows competency name and level, no position context

**Pre-Phase 90 Behavior (expected):**
- Would show "Leadership - Supervisor Level 3" or similar

**Investigation Result:**
- This is a data model limitation from Phase 90 cleanup
- Current functionality works (competencies are shown), just less informative
- Restoring would require:
  1. Adding `PositionTarget` field to `CompetencyGainItem`
  2. Populating it from assessment scoring logic
  3. Updating Results.cshtml to display it

**Recommendation:** Defer to separate enhancement phase. Not a bug per se, just reduced functionality after KKJ table cleanup.

**Complexity:** Medium (requires model changes + scoring logic update)

---

### 5. Code Quality Notes (Informational)

| ID | Location | Issue | Severity | Fix Plan |
|----|----------|-------|----------|----------|
| INFO-01 | CMPController.cs:326, 344 | Comment says "10-second TTL" but cache is 5 seconds | Low | Trivial |
| INFO-02 | CMPController.cs:58 | RoleLevel defaults to 6 (most permissive) when null | Low | Review needed |

**Details:**

**INFO-01: Cache TTL Comment Mismatch**
```csharp
// Comment: (10-second TTL — reduces DB load for concurrent workers)
// Actual: _cache.Set(cacheKey, (isClosed, redirectUrl), TimeSpan.FromSeconds(5));
```

**Fix:** Update comment to "5-second TTL".

**INFO-02: RoleLevel Default Value**
```csharp
var userLevel = currentUser?.RoleLevel ?? 6;
```

Defaulting to Level 6 (highest privilege) when user is null seems backwards. Should default to most restrictive (Level 1). However, this is in a context where user is already authenticated (`[Authorize]` on controller), so `currentUser` should never be null. This may be defensive programming for an edge case that never occurs.

---

## Fix Priority Plan

### Plan 93-02: Critical Null Safety Fixes (NS-01 to NS-03)
- Add `.Any()` guards before all `.First()` calls
- Add empty collection error messages
- **Estimated time:** 15 minutes
- **Risk:** Low (defensive guards only)

### Plan 93-03: Localization Fixes (LOC-01 to LOC-08)
- Add Indonesian culture to all date formatting
- Create extension method for reusability
- **Estimated time:** 20 minutes
- **Risk:** Low (display only, no logic changes)

### Plan 93-04: Validation & Error Handling (VAL-01 to VAL-04)
- Add parameter validation to POST actions
- Add null checks for nullable parameters
- **Estimated time:** 15 minutes
- **Risk:** Low (input sanitization only)

### Deferred (Future Enhancement)
- GAP-01: Restore PositionTargetHelper (separate enhancement phase)
- INFO-01: Update comment (trivial, can be done anytime)
- INFO-02: Review RoleLevel default (requires business logic review)

---

## Files Requiring Changes

### Controllers
- `Controllers/CMPController.cs` (11 fixes: 3 NS, 4 VAL, 2 INFO, 2 cache-related)

### Views
- `Views/CMP/Records.cshtml` (3 fixes: LOC-01 to LOC-03)
- `Views/CMP/Assessment.cshtml` (4 fixes: LOC-04 to LOC-07)
- `Views/CMP/Kkj.cshtml` (1 fix: LOC-08)

### Models (Deferred)
- `Models/AssessmentResultsViewModel.cs` (GAP-01 enhancement)

### New Files (Plan 93-03)
- `Extensions/DateTimeExtensions.cs` (helper for Indonesian date formatting)

---

## Testing Strategy

### Smoke Tests After Plan 93-02 (Null Safety)

1. **Empty KKJ Sections Test:**
   - Prerequisite: Truncate KkjBagians table
   - Action: Navigate to CMP/Kkj
   - Expected: Error message "No KKJ sections available"
   - Actual (before fix): 500 error

2. **Empty Packages Test:**
   - Prerequisite: Create assessment group with no packages
   - Action: Click "Start Exam"
   - Expected: Error message "No questions available"
   - Actual (before fix): 500 error

### Smoke Tests After Plan 93-03 (Localization)

1. **Records Page Dates:**
   - Action: Navigate to CMP/Records
   - Expected: All dates in Indonesian format (Mar, Apr, etc.)
   - Check: Training date, Valid Until date

2. **Assessment Page Dates:**
   - Action: Navigate to CMP/Assessment
   - Expected: All schedule dates in Indonesian format
   - Check: Schedule column, Opens tooltip

3. **KKJ File Dates:**
   - Action: Navigate to CMP/Kkj
   - Expected: File upload dates in Indonesian format

### Smoke Tests After Plan 93-04 (Validation)

1. **Negative ID Test:**
   - Action: Send POST to SaveAnswer with sessionId=-1
   - Expected: JSON error "Invalid parameters"

2. **Null Dictionary Test:**
   - Action: Send POST to ExamSummary with answers=null
   - Expected: Handles gracefully, uses empty dictionary

---

## Verification Checklist

After completing all fix plans:

- [ ] All `.First()` calls have `.Any()` guards (NS-01 to NS-03)
- [ ] All date formatting uses Indonesian culture (LOC-01 to LOC-08)
- [ ] All POST actions validate input parameters (VAL-01 to VAL-04)
- [ ] All nullable dictionary parameters have null coalescing (VAL-04)
- [ ] Smoke tests pass for null safety scenarios
- [ ] Smoke tests pass for localization scenarios
- [ ] Smoke tests pass for validation scenarios
- [ ] No regression in existing CMP functionality

---

## Requirements Coverage

This bug inventory addresses the following requirements from Phase 93:

- **CMP-01:** Assessment page loads without errors → NS-03 fixes StartExam crash
- **CMP-02:** Assessment monitoring detail page shows real-time data correctly → No issues found (cache working correctly)
- **CMP-03:** Records page displays assessment history with correct pagination → LOC-01 to LOC-03 fix date display
- **CMP-04:** KKJ Matrix page loads correctly → NS-01 to NS-02 fix Kkj crash, LOC-08 fixes date display
- **CMP-05:** All CMP forms handle validation errors gracefully → VAL-01 to VAL-04 add validation
- **CMP-06:** CMP navigation flows work correctly → No navigation issues found

---

**Status:** Ready for fix plans 93-02, 93-03, 93-04

*Last updated: 2026-03-05*
