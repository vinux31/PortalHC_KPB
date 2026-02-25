# Phase 45: Cross-Package Per-Position Shuffle - Research

**Researched:** 2026-02-25
**Domain:** ASP.NET Core MVC — shuffle algorithm, EF Core migration, exam package system
**Confidence:** HIGH (all findings from direct codebase inspection)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Phase Boundary:** Replace single-package assignment shuffle with per-position cross-package selection. Each question position independently and randomly draws from which package's question to show. Grading, auto-save, session resume, and reshuffle eligibility rules are unchanged. Only the shuffle generation logic changes.

**Question position ordering:**
- Display positions are always sequential: Soal No. 1, 2, 3, ... N (never reordered)
- What is randomized: which PACKAGE each position's question comes from
- When position i picks Package X → take Package X's question at index i (same row, different column)
- Example (3 packages, 3 questions each):
  - Worker Andi: No.1→PackageA, No.2→PackageC, No.3→PackageB
  - Worker Budi: No.1→PackageB, No.2→PackageA, No.3→PackageC
  - Worker Cici: No.1→PackageC, No.2→PackageB, No.3→PackageA
- This is "shuffle menyamping" — same question number slot, different package column

**Cross-package distribution — guaranteed even spread:**
- Distribution is merata (even): 2 packages = 50/50, 3 packages = 33/33/34, 4 packages = 25/25/25/25
- If questions don't divide evenly (e.g. 10 questions, 3 packages = 3+3+4): remainder allocated randomly to one package
- Each worker's distribution is independent — no coordination between workers
- Result: ShuffledQuestionIds JSON contains question IDs from potentially different packages, one per position

**Option shuffle (A/B/C/D):**
- Removed — options are no longer shuffled per worker
- Options displayed in original DB order
- ShuffledOptionIdsPerQuestion field becomes unused/deprecated
- Grading still uses PackageOption.Id (unchanged)

**Package count validation:**
- Validated at import time: when importing questions to Package B, system checks if count matches Package A (and any other existing packages with questions)
- Empty packages (0 questions) are excluded from validation
- If count mismatch detected at import: block with message "Jumlah soal tidak sama dengan paket lain. Paket A: 10 soal. Harap masukkan 10 soal."
- Safety fallback at StartExam: if mismatch still exists at exam start (edge case), use minimum question count across all packages

**Single-package behavior (1 package):**
- 1 package = questions shown in original DB order 1→N (no shuffle at all)
- No cross-package selection to perform, no Fisher-Yates applied
- Workers all see the same question order

**HC visibility on management page:**
- ManagePackages page displays a package summary panel: package name + question count + status (OK / Warning) per package
- Mode indicator shown: "Single Package" or "Multi-Package (N paket)"
- If count mismatch exists: warning shown but HC can still open the exam (system uses minimum count)
- HC monitoring (AssessmentMonitoringDetail) unchanged

**Import workflow:**
- No change to HC workflow — same Excel/text import process
- Validation added at import time: if importing to Package B and count differs from Package A (or any other non-empty package), import is blocked
- Error message: "Jumlah soal tidak sama dengan paket lain. Paket A: 10 soal. Harap masukkan 10 soal."
- Empty packages (0 questions) are excluded from validation

**Migration on deploy:**
- All UserPackageAssignment records deleted on deploy — no exceptions (Completed, InProgress, Not Started all wiped)
- InProgress workers at deploy time will lose progress and restart with a new assignment under the new logic
- Clean break — no backward compatibility with old shuffle format

**Reshuffle behavior:**
- Reshuffle eligibility unchanged: only "Not started" workers can be reshuffled
- On reshuffle: regenerate cross-package selection using new logic (new independent draw per position)
- Single reshuffle and bulk reshuffle both use new logic

### Claude's Discretion
- How to handle `UserPackageAssignment.AssessmentPackageId` field (no longer meaningful — could store first package ID, null, or be left as nullable)
- Algorithm implementation for guaranteed-even distribution (e.g. populate list with N/packages count per package, shuffle the list, assign positions)
- Whether to keep `ShuffledOptionIdsPerQuestion` as empty JSON or remove from new assignments
- Error handling if all packages are empty when exam starts

### Deferred Ideas (OUT OF SCOPE)
- Button "Start assessment" (manual HC trigger to open exam) — new capability, belongs in its own phase
</user_constraints>

---

## Summary

Phase 45 replaces the single-package shuffle (randomly pick one whole package, Fisher-Yates question order + option order) with a per-position cross-package draw (each position independently picks from which package to take its question, with guaranteed even distribution across packages). The change is surgical: only the assignment creation logic inside `StartExam`, `ReshufflePackage`, and `ReshuffleAll` changes. Grading, auto-save, answer review, and session resume are completely unaffected because `PackageUserResponse` stores `(AssessmentSessionId, PackageQuestionId, PackageOptionId)` — cross-package question IDs from different packages are perfectly valid keys.

The existing `ShuffledQuestionIds` JSON field perfectly handles the new format: instead of `[42, 17, 33]` being question IDs from one package, it becomes `[42, 73, 18]` where 42 may be from PackageA, 73 from PackageC, 18 from PackageB. The model change required is: `AssessmentPackageId` becomes meaningless (recommend: store first package ID, keep field to avoid schema churn), and `ShuffledOptionIdsPerQuestion` becomes empty JSON `"{}"` on all new assignments.

The migration is a single `migrationBuilder.Sql("DELETE FROM UserPackageAssignments")` inside a new EF migration. No column drops. No schema changes other than potentially making `AssessmentPackageId` nullable (or leaving it as-is and storing a sentinel). Four code areas change: the `StartExam` package-assignment block, `ReshufflePackage`, `ReshuffleAll`, and `ImportPackageQuestions` (add validation + redirect to ManagePackages with informative TempData). The ManagePackages view gains a summary panel above the package list.

**Primary recommendation:** Implement the even-distribution algorithm as a list-build + Fisher-Yates shuffle: build `packageSlots = [A, A, ..., B, B, ..., C, C, ...]` with `K/N` slots per package (remainder assigned to randomly chosen package), Fisher-Yates shuffle the slots list, then for position `i`: take question at `Order=i+1` from the package assigned to slot `i`. This is simple, correct, and reuses the existing `Shuffle<T>` helper.

---

## Architecture Patterns

### Current Flow (to be replaced)

**StartExam package-assignment block (lines 2853-2889 in CMPController.cs):**
```
1. Load packages with Include(p => p.Questions).ThenInclude(q => q.Options)
2. If no existing assignment:
   a. Pick one random package from packages list: packages[rng.Next(packages.Count)]
   b. Take all question IDs from that package, Fisher-Yates shuffle them
   c. For each question, Fisher-Yates shuffle its option IDs
   d. Create UserPackageAssignment with:
      - AssessmentPackageId = selectedPackage.Id
      - ShuffledQuestionIds = JSON([q1, q2, ...]) — all from same package
      - ShuffledOptionIdsPerQuestion = JSON({q1:[o1,o2,o3,o4], ...})
3. Load assignedPackage = packages.First(p => p.Id == assignment.AssessmentPackageId)
4. Stale check: compare assignment.SavedQuestionCount vs assignedPackage.Questions.Count
5. Build ViewModel using shuffledQuestionIds — lookup questions from assignedPackage only
```

**Key constraint in StartExam ViewModel build (lines 2913-2946):**
```csharp
var questionLookup = assignedPackage.Questions.ToDictionary(q => q.Id);
var optionLookup = assignedPackage.Questions.SelectMany(q => q.Options).ToDictionary(o => o.Id);
// Then iterates shuffledQuestionIds looking up in questionLookup
```
This lookup is SINGLE-PACKAGE today. In the new design, questions come from multiple packages, so `questionLookup` and `optionLookup` must be built from ALL packages combined.

### New Algorithm: Even Cross-Package Distribution

**Algorithm (Claude's Discretion — recommended approach):**

```csharp
// packages = all packages with Questions loaded and OrderBy(q.Order) applied
// K = minimum question count across all non-empty packages (safety fallback)
// N = packages.Count (non-empty packages only)

int K = packages.Min(p => p.Questions.Count);  // guaranteed even — all should be equal
int N = packages.Count;

// Step 1: Build slot list with even distribution
var packageSlots = new List<AssessmentPackage>();
int baseCount = K / N;
int remainder = K % N;

foreach (var pkg in packages)
{
    for (int i = 0; i < baseCount; i++)
        packageSlots.Add(pkg);
}

// Assign remainder to one random package
if (remainder > 0)
{
    var bonusPkg = packages[rng.Next(N)];
    for (int i = 0; i < remainder; i++)
        packageSlots.Add(bonusPkg);
}

// Step 2: Fisher-Yates shuffle the slot assignment list
Shuffle(packageSlots, rng);  // reuses existing private static void Shuffle<T>

// Step 3: Build ordered question ID list — for slot i, take package[slot[i]] question at Order=i+1
// Questions in each package are pre-sorted by Order ascending
var questionsByPackage = packages.ToDictionary(
    p => p.Id,
    p => p.Questions.OrderBy(q => q.Order).ToList()
);

var crossPackageQuestionIds = new List<int>();
for (int i = 0; i < packageSlots.Count; i++)
{
    var slotPackage = packageSlots[i];
    var pkgQuestions = questionsByPackage[slotPackage.Id];
    // pkgQuestions[i] is the i-th question of that package
    // But i may exceed pkgQuestions.Count if remainder was handled poorly — guard:
    int qIndex = i < pkgQuestions.Count ? i : pkgQuestions.Count - 1; // shouldn't happen
    crossPackageQuestionIds.Add(pkgQuestions[qIndex].Id);
}
```

Wait — the above approach has a subtle issue: when remainder goes to one package, that package contributes more questions but the index must still align. The correct approach is simpler: track per-package consumption index independently.

**Corrected algorithm:**

```csharp
// Build packageSlots list: each element = which package that position draws from
var packageSlots = new List<AssessmentPackage>();
int baseCount = K / N;
int remainder = K % N;

// baseCount slots per package
foreach (var pkg in packages)
    for (int i = 0; i < baseCount; i++)
        packageSlots.Add(pkg);

// remainder slots to a random package
if (remainder > 0)
{
    var bonusPkg = packages[rng.Next(N)];
    for (int i = 0; i < remainder; i++)
        packageSlots.Add(bonusPkg);
}

// Shuffle: which package each display position gets
Shuffle(packageSlots, rng);

// Now build question IDs: for each position, take the NEXT unused question from the assigned package
// Using a per-package queue of question indices (sorted by Order)
var packageQueues = packages.ToDictionary(
    p => p.Id,
    p => new Queue<int>(p.Questions.OrderBy(q => q.Order).Select(q => q.Id))
);

var crossPackageQuestionIds = new List<int>();
foreach (var slotPkg in packageSlots)
{
    crossPackageQuestionIds.Add(packageQueues[slotPkg.Id].Dequeue());
}
```

This is correct: each package's questions are consumed in their original Order, and each display position gets the next available question from its assigned package. Position 1 getting PackageA means it gets PackageA's first (Order=1) question. Position 2 getting PackageC means it gets PackageC's first question. If positions 3 AND 5 both get PackageA, position 3 gets PackageA.Order=2, position 5 gets PackageA.Order=3, etc.

**IMPORTANT: The "same row, different column" semantics** from the CONTEXT.md mean position i always gets package[slot].Question[i], NOT the "next available" question from that package. Let me re-read the decision carefully.

From CONTEXT.md: "When position i picks Package X → take Package X's question at index i (same row, different column)"

This means the question at index i in Package X — the i-th question by Order. This is different from "next available": if both position 1 and position 3 pick PackageA, then position 1 gets PackageA.Order=1, and position 3 gets PackageA.Order=3 (the 3rd question), NOT PackageA.Order=2.

**Final correct algorithm:**

```csharp
// Build packageSlots (length K), shuffle it — slot[i] tells which package display position i uses
// For position i, take package[slot[i]].Questions ordered by Order, take question at index i

var orderedByPkg = packages.ToDictionary(
    p => p.Id,
    p => p.Questions.OrderBy(q => q.Order).ToList()
);

var crossPackageQuestionIds = new List<int>();
for (int i = 0; i < packageSlots.Count; i++)  // packageSlots.Count == K
{
    var pkg = packageSlots[i];
    // Position i → index i in that package's ordered question list
    crossPackageQuestionIds.Add(orderedByPkg[pkg.Id][i].Id);
}
```

This is the true "shuffle menyamping" semantics. Position 1 (index 0) from PackageA gets PackageA[0]; position 1 (index 0) from PackageB would get PackageB[0]. The display position index IS the question index within whatever package is chosen for that slot.

**CRITICAL RISK:** This only works if all packages have exactly K questions. If packages have unequal counts and the remainder is assigned to a bonus package, the bonus package must have enough questions at that index. Since the slot list length = K (min count), and each package guaranteed has >= K questions, `orderedByPkg[pkg.Id][i]` is always valid (index i < K <= pkg.Questions.Count).

### Single-Package Fast Path

```csharp
if (packages.Count == 1)
{
    // No shuffle — questions in original DB order 1→N
    var pkg = packages[0];
    var questionIds = pkg.Questions.OrderBy(q => q.Order).Select(q => q.Id).ToList();
    // AssessmentPackageId = pkg.Id (still meaningful for single-package)
    // ShuffledQuestionIds = JSON(questionIds)
    // ShuffledOptionIdsPerQuestion = "{}"
    // SavedQuestionCount = pkg.Questions.Count
}
```

### ViewModel Build — Multi-Package Requires Combined Lookups

The existing ViewModel build (lines 2912-2946) uses `assignedPackage.Questions` for the lookup. In the new design, question IDs in `ShuffledQuestionIds` may come from any package. The ViewModel build must load questions from ALL packages:

```csharp
// Load questions from all packages into a flat lookup
var allPackageQuestions = packages.SelectMany(p => p.Questions).ToDictionary(q => q.Id);
var allPackageOptions = packages.SelectMany(p => p.Questions).SelectMany(q => q.Options).ToDictionary(o => o.Id);

// Then iterate shuffledQuestionIds as before, using allPackageQuestions lookup
```

This replaces lines 2916-2919 in the current code.

### Stale Question Count — New Semantics

Currently: `assignment.SavedQuestionCount` is compared to `assignedPackage.Questions.Count`.

In the new design, `AssessmentPackageId` may be the first package ID (or a sentinel). The stale check must compare against the minimum question count across all packages (which is what the worker was assigned):

```csharp
// New stale check: compare SavedQuestionCount vs current minimum across all non-empty packages
int currentMinCount = packages.Where(p => p.Questions.Any()).Min(p => p.Questions.Count);
if (assignment.SavedQuestionCount.HasValue && assignment.SavedQuestionCount.Value != currentMinCount)
{
    // Stale — redirect
}
```

### Grading — Unchanged (No Code Changes Required)

The grading path in `SubmitExam` (lines 3362-3466) loads:
```csharp
var packageAssignment = await _context.UserPackageAssignments
    .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);

var packageQuestions = await _context.PackageQuestions
    .Include(q => q.Options)
    .Where(q => q.AssessmentPackageId == packageAssignment.AssessmentPackageId)
    .ToListAsync();
```

**PROBLEM:** This loads questions only from `AssessmentPackageId` — one package. After Phase 45, questions come from multiple packages. Grading will score INCORRECTLY if it only looks at one package.

**Solution:** Grading must load questions from ALL packages (sibling sessions), then look up each submitted question ID. The `PackageUserResponse` records store `(PackageQuestionId, PackageOptionId)` — the question IDs uniquely identify questions across all packages since they're global auto-increment IDs. Grading just needs to load all `PackageQuestions` from all sibling session packages:

```csharp
// Instead of filtering by AssessmentPackageId:
var siblingSessionIds = await _context.AssessmentSessions
    .Where(s => s.Title == assessment.Title && ...)
    .Select(s => s.Id).ToListAsync();
var allPackageIds = await _context.AssessmentPackages
    .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId))
    .Select(p => p.Id).ToListAsync();
var packageQuestions = await _context.PackageQuestions
    .Include(q => q.Options)
    .Where(q => allPackageIds.Contains(q.AssessmentPackageId))
    .ToListAsync();
```

Actually simpler: since `PackageQuestion.Id` is globally unique, just load the questions whose IDs appear in the submitted `answers` dictionary:
```csharp
var submittedQuestionIds = answers.Keys.ToList();
var packageQuestions = await _context.PackageQuestions
    .Include(q => q.Options)
    .Where(q => submittedQuestionIds.Contains(q.Id))
    .ToListAsync();
```

This is zero-join and works for both single-package and multi-package assignments.

**IMPORTANT:** The force-complete path (bulk grading at lines ~864-940) also loads `packages` from sibling session IDs, then resolves `packageMap.TryGetValue(assignment.AssessmentPackageId, ...)`. This will break for multi-package assignments because `assignment.AssessmentPackageId` is only the "first package" sentinel. The force-complete grading must also load all questions from all packages and look up by submitted question ID (from PackageUserResponse records).

### ExamSummary — Also Needs Update

`ExamSummary` (GET, line 3140) loads questions for the summary page:
```csharp
var questions = await _context.PackageQuestions
    .Include(q => q.Options)
    .Where(q => q.AssessmentPackageId == assignment.AssessmentPackageId)  // WRONG after Phase 45
    .ToListAsync();
```
Must be updated to load from all packages (use submitted question IDs from `shuffledQIds`):
```csharp
var questions = await _context.PackageQuestions
    .Include(q => q.Options)
    .Where(q => shuffledQIds.Contains(q.Id))
    .ToListAsync();
```

### Results / Answer Review — Also Needs Update

`Results` action (lines 3690-3758) loads:
```csharp
var packageQuestions = await _context.PackageQuestions
    .Include(q => q.Options)
    .Where(q => q.AssessmentPackageId == packageAssignment.AssessmentPackageId)
    .ToListAsync();
```
Must load from all packages instead.

---

## Exact Code Locations That Change

### 1. StartExam — Assignment Creation Block
**File:** `Controllers/CMPController.cs`
**Lines:** 2853-2889 (assignment creation) + 2893-2910 (stale check) + 2912-2946 (ViewModel build)

**Changes:**
- Replace "randomly pick one package" with even cross-package slot algorithm
- Remove option shuffle (delete lines 2867-2873 — the `optionOrderDict` block)
- Set `AssessmentPackageId` to `packages.First().Id` (first package, as sentinel)
- Set `ShuffledOptionIdsPerQuestion = "{}"`
- Set `SavedQuestionCount = K` (minimum question count across packages)
- Update stale check: compare `SavedQuestionCount` vs current minimum across all non-empty packages
- Update `questionLookup` and `optionLookup` to span all packages (not just `assignedPackage`)
- Update option rendering: use `q.Options.OrderBy(o => o.Id).Select(...)` (no shuffle)

### 2. ReshufflePackage
**File:** `Controllers/CMPController.cs`
**Lines:** 1199-1311

**Changes:**
- Replace "pick a different package" logic (lines 1244-1254) with the even cross-package slot algorithm
- Remove `optionOrderDict` block (lines 1272-1278)
- Set `AssessmentPackageId` to first package ID (sentinel)
- Set `ShuffledOptionIdsPerQuestion = "{}"`
- Update audit log message: no longer references a single "packageName"

### 3. ReshuffleAll
**File:** `Controllers/CMPController.cs`
**Lines:** 1317-1441

**Changes:**
- Same as ReshufflePackage but in the per-session loop
- Remove per-session `optionOrderDict` block (lines 1399-1404)
- Update `AssessmentPackageId` to first package ID
- Update audit log results list

### 4. ImportPackageQuestions (POST)
**File:** `Controllers/CMPController.cs`
**Lines:** 4025-4198

**Where to add:** After the package is loaded (`pkg` variable, line 4028-4032), before processing rows (before line 4034)

**New validation block:**
```csharp
// Cross-package count validation: all non-empty packages must have same question count
var siblingPackages = await _context.AssessmentPackages
    .Include(p => p.Questions)
    .Where(p => p.AssessmentSessionId == pkg.AssessmentSessionId && p.Id != packageId)
    .ToListAsync();

var nonEmptySiblings = siblingPackages.Where(p => p.Questions.Any()).ToList();
if (nonEmptySiblings.Any())
{
    int existingCount = nonEmptySiblings.First().Questions.Count;
    // Current package question count AFTER this import would be:
    // pkg.Questions.Count + rows.Count (after deduplication)
    // But we validate BEFORE import: check if current target count (if non-zero) matches
    if (pkg.Questions.Any() && pkg.Questions.Count != existingCount)
    {
        // Package already has questions but count doesn't match — block
        var mismatchMsg = string.Join(", ", nonEmptySiblings.Select(p => $"{p.PackageName}: {p.Questions.Count} soal"));
        TempData["Error"] = $"Jumlah soal tidak sama dengan paket lain. {mismatchMsg}. Harap masukkan {existingCount} soal.";
        return RedirectToAction("ImportPackageQuestions", new { packageId });
    }
    // If target package is currently empty, no pre-import count check.
    // After import, count check happens via post-import validation (or next import attempt).
}
```

**IMPORTANT NUANCE:** The CONTEXT.md says "block with message" when importing to Package B and count differs. The validation should happen AFTER parsing (so we know how many rows are being added) but BEFORE persisting. The total-after-import check:

```csharp
// Check: after this import, will the count match?
int importingCount = rows.Count - (deduplication skipped count); // approximate — or check pre-import
// Simpler: validate that the package (if non-empty) matches existing, AND that rows being added won't create mismatch
```

The simplest implementation that matches the spec: after parsing rows (after line 4108), if `pkg.Questions.Any()` OR if `nonEmptySiblings.Any()`, check that `pkg.Questions.Count + validRowCount == existingCount`. Block if mismatch.

An even simpler reading: the package STARTS empty, HC imports N questions. Second package must also import N. Validation fires on the second import: if target package is empty, allow import of any count. If target package already has questions and count != existing, block.

The intent is: "when importing to Package B (which is currently empty), check that the number being imported matches Package A's count." This requires knowing `validRowCount` at validation time. Validate AFTER deduplication/parse, BEFORE persist.

### 5. ManagePackages — Controller
**File:** `Controllers/CMPController.cs`
**Lines:** 3867-3893

**Changes:** Pass additional ViewBag data for the summary panel:
```csharp
// Existing: packages, assignmentCounts, AssessmentTitle, AssessmentId

// Add:
bool isMultiPackage = packages.Count > 1;
bool hasMismatch = packages.Where(p => p.Questions.Any())
    .Select(p => p.Questions.Count)
    .Distinct()
    .Count() > 1;
ViewBag.IsMultiPackage = isMultiPackage;
ViewBag.HasCountMismatch = hasMismatch;
```

### 6. ManagePackages — View
**File:** `Views/CMP/ManagePackages.cshtml`
**Location:** Before line 29 (the `<div class="row">` main layout section)

**Add:** A summary panel card showing mode indicator and per-package status.

### 7. SubmitExam Grading
**File:** `Controllers/CMPController.cs`
**Lines:** 3368-3371

**Change:** Load questions from all packages, not just `assignment.AssessmentPackageId`.

### 8. Force-Complete Grading (bulk close)
**File:** `Controllers/CMPController.cs`
**Lines:** ~864-866

**Change:** Package grading in the bulk close loop must load questions from all sibling packages, not use `packageMap.TryGetValue(assignment.AssessmentPackageId, ...)`.

### 9. ExamSummary (GET)
**File:** `Controllers/CMPController.cs`
**Lines:** 3178-3181

**Change:** Load questions by `shuffledQIds.Contains(q.Id)` instead of `q.AssessmentPackageId == assignment.AssessmentPackageId`.

### 10. Results — Answer Review
**File:** `Controllers/CMPController.cs`
**Lines:** 3691-3695

**Change:** Load questions from all sibling packages.

---

## EF Migration Strategy

**Migration name:** `WipeUserPackageAssignmentsForCrossPackage`

**What it does:**
1. Wipes all `UserPackageAssignments` rows (clean break — no backward compatibility)
2. Does NOT drop columns (no schema change required — `ShuffledOptionIdsPerQuestion` stays as unused JSON column)
3. Does NOT add new columns (all existing columns are reused)

**Migration file:**
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Delete all existing assignments — clean break for cross-package shuffle
    // All in-progress workers will restart with new cross-package assignment on next exam load
    migrationBuilder.Sql("DELETE FROM UserPackageAssignments");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // Cannot restore deleted assignments — Down is intentionally empty
    // (this is a data migration, not structural)
}
```

**Why no schema change is needed:**
- `AssessmentPackageId` will store the first package ID as a sentinel — FK constraint still satisfied
- `ShuffledOptionIdsPerQuestion` will be `"{}"` on all new assignments — column stays
- `ShuffledQuestionIds` now stores cross-package question IDs — same JSON format, different values
- `SavedQuestionCount` now stores `K` (min across packages) — same semantics, different calculation

**Recommendation for `AssessmentPackageId`:** Store the first package's ID (lowest `PackageNumber`). Rationale:
- FK constraint `FK_UserPackageAssignments_AssessmentPackages_AssessmentPackageId` must point to a valid package
- The field is `[ForeignKey("AssessmentPackageId")]` — cannot be null without a migration to make it nullable
- Making it nullable requires a migration and changes to `UserPackageAssignment.cs` model
- RECOMMENDATION: Keep it non-nullable, store `packages.OrderBy(p => p.PackageNumber).First().Id`. This avoids a schema migration while satisfying the FK. The field is simply ignored in all new logic.

**Alternative:** Make `AssessmentPackageId` nullable. This requires:
1. Update `UserPackageAssignment.cs`: `public int? AssessmentPackageId { get; set; }`
2. Migration: `migrationBuilder.AlterColumn<int?>(...)` + update FK to allow null
3. All code that uses `assignment.AssessmentPackageId` must handle null
- This is cleaner semantically but adds migration complexity. The wipe migration already breaks all existing records, so the sentinel approach is simpler.

**RECOMMENDATION:** Use sentinel (first package ID). Simpler. No model change needed.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Fisher-Yates shuffle | Custom swap loop | Existing `private static void Shuffle<T>(List<T> list, Random rng)` at line 3079 | Already implemented, correct |
| Even distribution | Complex math | Simple list-build + Shuffle pattern | Obvious and correct; no edge cases |
| EF data wipe | LINQ RemoveRange | `migrationBuilder.Sql("DELETE FROM UserPackageAssignments")` | Direct SQL is the right tool in migrations |
| Cross-package question lookup | Complex join | Load all questions from all packages into a single flat dictionary | EF Include is sufficient |

---

## Common Pitfalls

### Pitfall 1: Grading Only Looks at AssessmentPackageId's Package
**What goes wrong:** `SubmitExam`, `ExamSummary`, `Results`, and force-complete grading all have `Where(q => q.AssessmentPackageId == assignment.AssessmentPackageId)`. After Phase 45, this only loads questions from one package. Questions answered from OTHER packages won't be found → score = 0 for those questions.
**Why it happens:** The grading code was written for single-package assignments.
**How to avoid:** Change all four locations to load from all sibling packages (or filter by submitted question IDs).
**Warning signs:** Test by assigning a 3-package exam; submit answers; verify all N questions score correctly — not just the K/N from the first package.

### Pitfall 2: ViewModel Build Uses Single-Package Lookup
**What goes wrong:** Lines 2916-2919 build `questionLookup` from `assignedPackage.Questions`. If a question ID in `ShuffledQuestionIds` came from PackageB or PackageC, `questionLookup.TryGetValue(qId, out var q)` returns false → that question is skipped → worker sees fewer questions than expected.
**How to avoid:** Build `questionLookup` from `packages.SelectMany(p => p.Questions).ToDictionary(q => q.Id)`.
**Warning signs:** Worker sees fewer questions displayed than expected.

### Pitfall 3: SavedQuestionCount Stale Check Compares Wrong Value
**What goes wrong:** Current stale check: `assignment.SavedQuestionCount.Value != assignedPackage.Questions.Count`. With multi-package, `assignedPackage` is just the sentinel package. If that package's count changes, stale triggers incorrectly. If a DIFFERENT package's count changes, stale doesn't trigger (misses the real stale condition).
**How to avoid:** Stale check must compare `SavedQuestionCount` against `packages.Where(p => p.Questions.Any()).Min(p => p.Questions.Count)`.

### Pitfall 4: ReshuffleAll Still Has "Select Different Package" Logic
**What goes wrong:** Lines 1376-1385 in ReshuffleAll try to pick a different package than the worker's current assignment. After Phase 45, there is no "assigned package" concept. This logic references `existingAssignment.AssessmentPackageId` to exclude — meaningless since the sentinel may equal any package.
**How to avoid:** Remove the "prefer different package" selection logic entirely. Simply run the cross-package slot algorithm for every eligible worker.

### Pitfall 5: Package Count Validation Fires on First Import
**What goes wrong:** If the import validation checks non-empty sibling packages, and the HC imports Package A (the first package), there are NO non-empty siblings — validation passes correctly. But if validation logic is not careful, it might check Package A against Package B even when Package B is empty.
**How to avoid:** Check `nonEmptySiblings.Where(p => p.Questions.Any())` — only compare against non-empty sibling packages.

### Pitfall 6: Import Validation Doesn't Account for Deduplication
**What goes wrong:** If HC imports 10 rows to Package B but 2 are duplicates, only 8 are added. If Package A has 10 questions, the post-import count will be 8 != 10 — warning should fire but didn't fire at import time.
**How to avoid:** Validate the COUNT TO BE ADDED (after deduplication), not just the raw row count. The cleanest approach: run the import, then check if counts match and warn (or post-validate after save). Since the import already has a success redirect to ManagePackages, the summary panel there will show the warning.
**Alternative:** Block the import post-processing with a count check before the final `SaveChangesAsync` but after deduplication. This is complex to implement cleanly with the current row-by-row flush pattern.
**Recommended:** Accept that the per-row deduplication makes pre-import count validation approximate. Add a post-import warning in the ManagePackages view (summary panel shows count mismatch warning). This is consistent with the CONTEXT.md statement: "If count mismatch exists: warning shown but HC can still open the exam".

### Pitfall 7: Force-Complete PackageMap Lookup Fails for Multi-Package
**What goes wrong:** The bulk force-complete grading (around line 864) builds `packageMap.TryGetValue(assignment.AssessmentPackageId, ...)`. If `AssessmentPackageId` is a sentinel = Package A, but the worker's questions come from A, B, and C, then `packageMap[pkgA.Id].Questions` only has Package A's questions. Grading loads wrong questions.
**How to avoid:** In force-complete grading, load all PackageQuestions from all sibling packages, build a global question lookup, and resolve answers against it.

---

## Code Examples

### Existing Shuffle Helper (HIGH confidence — direct read of source)
```csharp
// CMPController.cs, line 3079
private static void Shuffle<T>(List<T> list, Random rng)
{
    for (int i = list.Count - 1; i > 0; i--)
    {
        int j = rng.Next(i + 1);
        (list[i], list[j]) = (list[j], list[i]);
    }
}
```

### New Assignment Creation — Full Cross-Package Block
```csharp
// Replace lines 2853-2889 in StartExam

if (assignment == null)
{
    var rng = new Random();

    if (packages.Count == 1)
    {
        // Single-package: show questions in original DB order (no shuffle)
        var singlePkg = packages[0];
        var questionIds = singlePkg.Questions.OrderBy(q => q.Order).Select(q => q.Id).ToList();

        assignment = new UserPackageAssignment
        {
            AssessmentSessionId = id,
            AssessmentPackageId = singlePkg.Id,
            UserId = user.Id,
            ShuffledQuestionIds = JsonSerializer.Serialize(questionIds),
            ShuffledOptionIdsPerQuestion = "{}",
            SavedQuestionCount = questionIds.Count
        };
    }
    else
    {
        // Multi-package: per-position cross-package draw with even distribution
        var nonEmptyPackages = packages.Where(p => p.Questions.Any()).OrderBy(p => p.PackageNumber).ToList();

        if (!nonEmptyPackages.Any())
        {
            TempData["Error"] = "Ujian belum memiliki soal. Hubungi HC.";
            return RedirectToAction("Assessment");
        }

        int K = nonEmptyPackages.Min(p => p.Questions.Count);  // safety: use minimum
        int N = nonEmptyPackages.Count;
        int baseCount = K / N;
        int remainder = K % N;

        // Build slot list: baseCount slots per package + remainder to one random package
        var packageSlots = new List<AssessmentPackage>();
        foreach (var pkg in nonEmptyPackages)
            for (int i = 0; i < baseCount; i++)
                packageSlots.Add(pkg);

        if (remainder > 0)
        {
            var bonusPkg = nonEmptyPackages[rng.Next(N)];
            for (int i = 0; i < remainder; i++)
                packageSlots.Add(bonusPkg);
        }

        // Shuffle which package each display position uses
        Shuffle(packageSlots, rng);

        // For position i, take question at index i from the assigned package
        var orderedByPkg = nonEmptyPackages.ToDictionary(
            p => p.Id,
            p => p.Questions.OrderBy(q => q.Order).ToList()
        );

        var crossPackageQuestionIds = new List<int>();
        for (int i = 0; i < packageSlots.Count; i++)
        {
            var slotPkg = packageSlots[i];
            crossPackageQuestionIds.Add(orderedByPkg[slotPkg.Id][i].Id);
        }

        var sentinelPkgId = nonEmptyPackages.First().Id;

        assignment = new UserPackageAssignment
        {
            AssessmentSessionId = id,
            AssessmentPackageId = sentinelPkgId,
            UserId = user.Id,
            ShuffledQuestionIds = JsonSerializer.Serialize(crossPackageQuestionIds),
            ShuffledOptionIdsPerQuestion = "{}",
            SavedQuestionCount = K
        };
    }

    _context.UserPackageAssignments.Add(assignment);
    await _context.SaveChangesAsync();
}
```

### Updated ViewModel Build — Multi-Package Lookup
```csharp
// Replace lines 2912-2919 in StartExam
// Build lookups spanning ALL packages (not just assignedPackage)
var questionLookup = packages.SelectMany(p => p.Questions).ToDictionary(q => q.Id);
var optionLookup = packages.SelectMany(p => p.Questions).SelectMany(q => q.Options).ToDictionary(o => o.Id);

var shuffledQuestionIds = assignment.GetShuffledQuestionIds();
// Note: GetShuffledOptionIds() is no longer used — options render in DB order

var examQuestions = new List<ExamQuestionItem>();
int displayNum = 1;
foreach (var qId in shuffledQuestionIds)
{
    if (!questionLookup.TryGetValue(qId, out var q)) continue;

    // Options in original DB order (no option shuffle)
    var opts = q.Options.OrderBy(o => o.Id).Select(oid => new ExamOptionItem
    {
        OptionId = oid.Id,
        OptionText = oid.OptionText
    }).ToList();

    examQuestions.Add(new ExamQuestionItem
    {
        QuestionId = q.Id,
        QuestionText = q.QuestionText,
        DisplayNumber = displayNum++,
        Options = opts
    });
}
```

### Updated Stale Check
```csharp
// Replace lines 2897-2909 in StartExam
var nonEmptyPkgs = packages.Where(p => p.Questions.Any()).ToList();
int currentMinCount = nonEmptyPkgs.Any() ? nonEmptyPkgs.Min(p => p.Questions.Count) : 0;

if (assessment.StartedAt != null && assignment.SavedQuestionCount.HasValue &&
    assignment.SavedQuestionCount.Value != currentMinCount)
{
    await _context.AssessmentSessions
        .Where(s => s.Id == id)
        .ExecuteUpdateAsync(s => s
            .SetProperty(r => r.ElapsedSeconds, 0)
            .SetProperty(r => r.LastActivePage, (int?)null)
        );
    TempData["Error"] = "Soal ujian telah berubah. Hubungi HC untuk mengatur ulang ujian Anda.";
    return RedirectToAction("Assessment");
}
```

### Import Validation Block
```csharp
// Add after loading pkg (around line 4032), before the fingerprint/deduplication setup

// Cross-package count validation: check against existing non-empty sibling packages
var siblingPackages = await _context.AssessmentPackages
    .Include(p => p.Questions)
    .Where(p => p.AssessmentSessionId == pkg.AssessmentSessionId && p.Id != packageId)
    .ToListAsync();

var nonEmptySiblings = siblingPackages.Where(p => p.Questions.Any()).ToList();
// Note: empty packages are excluded — HC can build packages one at a time

// Will add this import count check AFTER parsing rows (post-parse, pre-persist)
// Store for validation
int? referenceCount = nonEmptySiblings.Any() ? nonEmptySiblings.First().Questions.Count : (int?)null;
```

Then AFTER rows are parsed but BEFORE persisting (after line 4108, before line 4116):
```csharp
// Validate that import count matches sibling packages (cross-package parity check)
if (referenceCount.HasValue && pkg.Questions.Count == 0)
{
    // Target package is currently empty. Count rows being imported (excluding errors).
    int validRowCount = rows.Count(r =>
        !string.IsNullOrWhiteSpace(r.Question) &&
        !string.IsNullOrWhiteSpace(r.OptA) &&
        !string.IsNullOrWhiteSpace(r.OptB) &&
        !string.IsNullOrWhiteSpace(r.OptC) &&
        !string.IsNullOrWhiteSpace(r.OptD) &&
        new[] { "A", "B", "C", "D" }.Contains(ExtractCorrectLetter(r.Correct.ToUpper()))
    );

    if (validRowCount != referenceCount.Value)
    {
        var siblingNames = string.Join(", ", nonEmptySiblings.Select(p => $"{p.PackageName}: {p.Questions.Count} soal"));
        TempData["Error"] = $"Jumlah soal tidak sama dengan paket lain. {siblingNames}. Harap masukkan {referenceCount.Value} soal.";
        return RedirectToAction("ImportPackageQuestions", new { packageId });
    }
}
```

### ManagePackages Summary Panel (HTML)
```html
<!-- Add before <div class="row"> in ManagePackages.cshtml -->
@{
    bool isMultiPkg = (bool)(ViewBag.IsMultiPackage ?? false);
    bool hasMismatch = (bool)(ViewBag.HasCountMismatch ?? false);
    var pkgs = ViewBag.Packages as List<HcPortal.Models.AssessmentPackage> ?? new();
}

<div class="card shadow mb-4">
    <div class="card-header py-3 d-flex justify-content-between align-items-center">
        <h6 class="m-0 fw-bold text-primary">Package Summary</h6>
        <span class="badge @(isMultiPkg ? "bg-info" : "bg-secondary")">
            @(isMultiPkg ? $"Multi-Package ({pkgs.Count} paket)" : "Single Package")
        </span>
    </div>
    <div class="card-body">
        @if (hasMismatch)
        {
            <div class="alert alert-warning mb-3">
                <i class="bi bi-exclamation-triangle me-2"></i>
                Jumlah soal antar paket tidak sama. Ujian akan menggunakan jumlah soal minimum.
            </div>
        }
        <div class="table-responsive">
            <table class="table table-sm">
                <thead><tr><th>Package</th><th>Questions</th><th>Status</th></tr></thead>
                <tbody>
                    @foreach (var p in pkgs)
                    {
                        bool pkgOk = !hasMismatch || (pkgs.Where(x => x.Questions.Any()).Select(x => x.Questions.Count).Distinct().Count() <= 1);
                        <tr>
                            <td>@p.PackageName</td>
                            <td>@p.Questions.Count</td>
                            <td>
                                @if (p.Questions.Count == 0)
                                {
                                    <span class="badge bg-secondary">Empty</span>
                                }
                                else if (hasMismatch)
                                {
                                    <span class="badge bg-warning text-dark"><i class="bi bi-exclamation-triangle"></i> Warning</span>
                                }
                                else
                                {
                                    <span class="badge bg-success"><i class="bi bi-check-circle"></i> OK</span>
                                }
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    </div>
</div>
```

---

## State of the Art (Before vs. After)

| Old Approach | New Approach | Impact |
|--------------|--------------|--------|
| Randomly pick one whole package | Per-position package selection | Each worker gets questions from multiple packages |
| Fisher-Yates shuffle question order | No question order shuffle (positions are sequential) | Position 1 always = Question 1 (from some package) |
| Fisher-Yates shuffle option order per question | No option shuffle — DB order | ShuffledOptionIdsPerQuestion unused, render `q.Options.OrderBy(o => o.Id)` |
| `AssessmentPackageId` = assigned package | `AssessmentPackageId` = sentinel (first package) | Field is maintained for FK validity, ignored in logic |
| Grading loads from `AssessmentPackageId` | Grading loads from all sibling packages or submitted question IDs | Grading finds answers regardless of package origin |
| SavedQuestionCount = one package's count | SavedQuestionCount = min count across all packages | Stale detection accounts for any package count change |

**Deprecated pattern after Phase 45:**
- `GetShuffledOptionIds()` helper method — still compiles but never called in new assignments
- "Prefer different package on reshuffle" logic in ReshufflePackage/ReshuffleAll

---

## Open Questions

1. **Import validation: block on wrong count or warn after import?**
   - What we know: CONTEXT.md says "block with message" at import time
   - What's unclear: The deduplication makes exact pre-import count validation approximate (2 duplicate rows in a 10-row import = 8 actual rows added, not 10)
   - Recommendation: Validate the valid-row count AFTER parse but BEFORE persist. Count valid rows (non-empty question, 4 non-empty options, valid Correct letter). If count != referenceCount, block. This is precise enough for the vast majority of imports.

2. **AssessmentPackageId: sentinel vs nullable**
   - What we know: FK constraint requires a valid package ID; making nullable needs a migration + model change
   - What's unclear: Whether future phases will need a meaningful `AssessmentPackageId`
   - Recommendation: Store sentinel (first package ID). Avoids schema migration. Add a comment in code.

3. **Force-complete grading (bulk close) code location**
   - What we know: Around line 864; loads `packageMap.TryGetValue(assignment.AssessmentPackageId, ...)`
   - What's unclear: The exact breadth of this bulk grading loop — need to verify it reads PackageUserResponse records to compute score
   - Recommendation: The planner should include reading lines 800-950 in CMPController to map the full bulk grading flow before writing tasks.

4. **Error handling when ALL packages are empty at StartExam**
   - What we know: CONTEXT.md marks this as Claude's Discretion
   - Recommendation: Check `nonEmptyPackages.Any()` before the algorithm; if empty, redirect to Assessment with TempData["Error"] = "Ujian belum memiliki soal. Hubungi HC."

5. **Option rendering order: `OrderBy(o => o.Id)` vs `OrderBy(o => o.PackageQuestionId)`**
   - What we know: Options have no explicit `Order` column — they are stored by insertion order, accessible via `Id` (auto-increment)
   - Recommendation: Use `OrderBy(o => o.Id)` — this preserves the original import order (A=first inserted, B=second, C=third, D=fourth)

---

## Sources

### Primary (HIGH confidence — direct codebase inspection)
- `Controllers/CMPController.cs` — StartExam (lines 2759-3043), ReshufflePackage (1199-1311), ReshuffleAll (1317-1441), ImportPackageQuestions (4008-4198), ManagePackages (3867-3893), ExamSummary (3115-3250), SubmitExam (3323-3466), Results (3690-3758), force-complete grading (~864-940), Shuffle helper (3079-3086)
- `Models/UserPackageAssignment.cs` — full model with fields and helper methods
- `Models/AssessmentPackage.cs` — AssessmentPackage, PackageQuestion, PackageOption entities
- `Models/PackageUserResponse.cs` — response storage model
- `Models/AssessmentSession.cs` — session model with ElapsedSeconds, LastActivePage, SavedQuestionCount
- `Views/CMP/ManagePackages.cshtml` — current UI structure
- `Views/CMP/StartExam.cshtml` — option rendering loop (lines 88-106, confirms letters assigned by index)
- `Migrations/20260219140545_AddPackageSystem.cs` — table structure, indexes, FK constraints
- `Migrations/20260224111956_AddSessionResumeFields.cs` — SavedQuestionCount migration pattern
- `Data/ApplicationDbContext.cs` — DbSet declarations

---

## Metadata

**Confidence breakdown:**
- Exact code locations: HIGH — direct file inspection with line numbers
- Algorithm correctness: HIGH — verified against CONTEXT.md spec; "same row, different column" semantics confirmed
- Migration strategy: HIGH — matches established migration pattern in codebase
- Import validation implementation: MEDIUM — the deduplication interaction with count validation has a subtle edge case
- Force-complete grading fix: MEDIUM — lines ~864 confirmed, full flow needs reading 800-950 to plan precisely

**Research date:** 2026-02-25
**Valid until:** 2026-03-27 (30 days — stable codebase, no external dependencies)
