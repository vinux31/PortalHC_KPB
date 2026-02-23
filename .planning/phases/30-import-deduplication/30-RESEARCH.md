# Phase 30: Import Deduplication - Research

**Researched:** 2026-02-23
**Domain:** ASP.NET Core MVC — in-action deduplication for import flow (CMPController)
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Feedback messaging**
- Success banner format: Green success banner always (not downgraded to warning) — "X questions imported. Y duplicates skipped."
- Detail level: Count only. No list of which questions were skipped.
- Path-specific messages:
  - Excel upload: "Imported from file: X added, Y skipped."
  - Paste: "X added, Y skipped."
- All-duplicates edge case: Stay on the import page (no redirect). Show a yellow warning banner: "All questions were already in the package. Nothing was added."
- Empty/malformed import (0 valid rows): Also stay on import page. Show a distinct message — different from the all-duplicates message so HC knows what happened.

**Match precision**
- Case-insensitive: Yes
- Whitespace normalization: Trim leading/trailing spaces AND collapse internal whitespace runs — "Apa  tujuan" equals "Apa tujuan"
- Punctuation: Claude's discretion — choose whatever is most practical given the implementation
- Duplicate definition: BOTH question text AND all answer options must match (case-insensitive + normalized). If question text is the same but any option differs → treated as a different (new) question and gets imported normally.

**Behavior on full-duplicate import**
- All duplicates: Stay on import page with yellow warning banner
- Some duplicates, some new: Redirect to ManagePackages with green success banner showing both counts
- Visible detail: Count in banner is sufficient — no collapsible list of skipped questions

**Scope of check**
- Package scope: Check against questions in the current package only.
- Self-deduplication: The import file itself is also deduplicated — if HC's Excel/paste contains two identical rows, only one is imported (and the skip count reflects in-file duplicates too).
- Preview mode: Claude's discretion — decide whether dedup runs during preview or only on save, based on existing preview implementation complexity.

### Claude's Discretion

- Punctuation normalization approach (strip or ignore)
- Preview mode dedup behavior (preview vs save-only)
- Exact wording for the empty/malformed 0-valid-rows message

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.
</user_constraints>

---

## Summary

Phase 30 modifies a single action — `ImportPackageQuestions` POST in `CMPController` (~line 3257). No new models, no new views, no migrations. This is pure in-action C# logic: load existing questions with options, build a normalized fingerprint set, filter the incoming rows against it, then route the result to the right message and redirect target.

The existing import flow is: parse rows (Excel or paste) → validate each row → `SaveChangesAsync()` per question → set TempData → redirect to ManagePackages. The deduplication change inserts a filter step before the persist loop, modifies TempData message logic, and adds a stay-on-page path for the all-duplicates edge case.

The only non-obvious complication is the duplicate definition: BOTH question text AND all four option texts must match for a row to be skipped. This requires loading `Options` via `ThenInclude` in the POST — currently the POST only does `.Include(p => p.Questions)` with no options loaded.

**Primary recommendation:** In the POST action, change the Include to `ThenInclude(q => q.Options)`, build a `HashSet<string>` of normalized fingerprints from existing questions, filter incoming rows against the set (for both package-dedup and self-dedup), adjust TempData and redirect logic based on outcome, and update the view to render the warning banner for the all-duplicates case.

---

## Standard Stack

### Core

| Component | Version | Purpose | Why Standard |
|-----------|---------|---------|--------------|
| ASP.NET Core MVC | .NET 10 (project target) | Controller action modification | Already the app framework |
| EF Core | (project version) | Loading existing questions with options | Already in use for all DB access |
| ClosedXML | (project version) | Excel parsing (already used) | Already imported in CMPController |

No new NuGet packages are required. This phase is 100% standard C# string manipulation + existing ORM patterns.

### Supporting

| Component | Purpose | Notes |
|-----------|---------|-------|
| `System.Text.RegularExpressions` | Collapsing internal whitespace runs | Already in .NET BCL — no import needed; `Regex.Replace(s, @"\s+", " ")` does the job |
| `HashSet<string>` | O(1) fingerprint lookup | BCL collection; ideal for this use case |
| Bootstrap alert classes | `alert-warning` for stay-on-page, `alert-success` for redirect | Already used in `_Layout.cshtml` and `ManagePackages.cshtml` |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `HashSet<string>` fingerprint | LINQ `.Any()` scan per row | HashSet is O(1) vs O(n) per row — matters for large imports |
| `Regex.Replace` for whitespace | Split+Join | Both correct; Regex is a single call |
| Punctuation: strip all punctuation | Leave punctuation as-is | Stripping adds complexity with minimal benefit — recommend: do NOT strip punctuation (keep as-is after trim + whitespace collapse) |

---

## Architecture Patterns

### Affected Files

```
Controllers/
└── CMPController.cs           # ImportPackageQuestions POST (lines ~3257-3392) — only file modified

Views/CMP/
└── ImportPackageQuestions.cshtml  # Add "all-duplicates" warning block + "0-valid-rows" warning block
                                    # (currently only has a Warning banner at line 17)
```

No migrations. No model changes. No new views.

### Pattern 1: Fingerprint-Based Deduplication

**What:** Build a canonical string fingerprint for each question (normalized text + normalized option texts joined with a separator). Store existing package fingerprints in a HashSet. For each incoming row, compute its fingerprint and check membership.

**When to use:** When equality is content-based, case-insensitive, and whitespace-normalized — not ID-based.

**Example:**
```csharp
// Source: Codebase analysis — CMPController existing pattern + BCL
private static string NormalizeText(string s)
{
    // Trim + collapse internal whitespace
    return System.Text.RegularExpressions.Regex.Replace(s.Trim(), @"\s+", " ").ToLowerInvariant();
}

private static string MakeFingerprint(string q, string a, string b, string c, string d)
{
    // Separator unlikely to appear in question text
    return string.Join("|||", new[] { q, a, b, c, d }.Select(NormalizeText));
}
```

### Pattern 2: Building the Existing-Fingerprint HashSet

**What:** After loading `pkg` with `ThenInclude(q => q.Options)`, project each existing question into its fingerprint.

**Critical finding:** The current POST query is `.Include(p => p.Questions)` with NO `ThenInclude`. To compare options, the query must be upgraded to `.ThenInclude(q => q.Options)`.

**Example:**
```csharp
// Change from:
var pkg = await _context.AssessmentPackages
    .Include(p => p.Questions)
    .FirstOrDefaultAsync(p => p.Id == packageId);

// Change to:
var pkg = await _context.AssessmentPackages
    .Include(p => p.Questions)
        .ThenInclude(q => q.Options)
    .FirstOrDefaultAsync(p => p.Id == packageId);

// Then build fingerprint set:
var existingFingerprints = pkg.Questions
    .Select(q =>
    {
        // Sort options by Id (stable insertion order)
        var opts = q.Options.OrderBy(o => o.Id).Select(o => o.OptionText).ToList();
        return MakeFingerprint(q.QuestionText,
            opts.ElementAtOrDefault(0) ?? "",
            opts.ElementAtOrDefault(1) ?? "",
            opts.ElementAtOrDefault(2) ?? "",
            opts.ElementAtOrDefault(3) ?? "");
    })
    .ToHashSet();
```

**Important:** Options have no explicit ordering column in PackageOption model. OrderBy(o => o.Id) is the correct stable sort since options are always inserted A→B→C→D in creation order and Id is auto-increment.

### Pattern 3: Self-Deduplication of Import File

**What:** Track fingerprints of rows already committed in this import run. Skip rows whose fingerprint already exists either in `existingFingerprints` OR in `seenInBatch`.

**Example:**
```csharp
var seenInBatch = new HashSet<string>();

// Inside the loop, after validation passes:
var fp = MakeFingerprint(q, a, b, c, d);
if (existingFingerprints.Contains(fp) || seenInBatch.Contains(fp))
{
    skipped++;
    continue;
}
seenInBatch.Add(fp);
// ... proceed with save
```

### Pattern 4: TempData Routing Logic

**What:** After the loop, branch on `added` and `skipped` to select the right message and redirect target.

**Example:**
```csharp
// All rows were valid but all were duplicates (added == 0, skipped > 0)
if (added == 0 && skipped > 0)
{
    TempData["Warning"] = "All questions were already in the package. Nothing was added.";
    return RedirectToAction("ImportPackageQuestions", new { packageId });
}

// 0 valid rows (empty/malformed — no added, no skipped; errors only)
if (added == 0 && skipped == 0)
{
    TempData["Warning"] = "No valid questions found in the import. Check the format and try again.";
    return RedirectToAction("ImportPackageQuestions", new { packageId });
}

// Some or all added successfully
string skipNote = skipped > 0 ? $" {skipped} duplicate(s) skipped." : "";
if (excelFile != null)
    TempData["Success"] = $"Imported from file: {added} added, {skipped} skipped.";
else
    TempData["Success"] = $"{added} added, {skipped} skipped.";

return RedirectToAction("ManagePackages", new { assessmentId = pkg.AssessmentSessionId });
```

**Note:** The "0 valid rows" case (empty/malformed) also currently redirects to ImportPackageQuestions with an Error — but the context says it should stay on the import page with a DISTINCT message. The existing `TempData["Error"]` path (line 3330: "Please upload an Excel file or paste question data.") is for when NOTHING was submitted (no file, no paste). The new 0-valid-rows path handles when something was submitted but produced zero parseable rows.

### Pattern 5: View-Side Warning Rendering

**What:** The ImportPackageQuestions view already handles `TempData["Warning"]` at line 17. The existing block covers the all-duplicates warning and the 0-valid-rows warning — NO view changes needed if we use `TempData["Warning"]` for both stay-on-page messages.

**Existing view block (line 17-20):**
```cshtml
@if (TempData["Warning"] != null)
{
    <div class="alert alert-warning alert-dismissible fade show">@TempData["Warning"]<button type="button" class="btn-close" data-bs-dismiss="alert"></button></div>
}
```

This already renders a yellow alert. The _Layout.cshtml also handles `TempData["Warning"]` globally (with "Warning:" prefix and icon). Since ImportPackageQuestions stays on the same page (redirect-then-GET), the warning will render via the view's own block, not the layout block — no conflict.

**Key insight:** Because both stay-on-page cases use `RedirectToAction("ImportPackageQuestions", ...)`, TempData persists across the redirect. The view's existing Warning block at the top of the page will display correctly. No view changes required.

### Anti-Patterns to Avoid

- **Loading options lazily:** Do not rely on lazy loading for `PackageOption`. Always use `ThenInclude`. The app uses eager loading throughout.
- **Comparing options unordered:** Do NOT build a fingerprint from an unordered set of options (e.g., HashSet<string> of option texts). Options are position-significant (A, B, C, D). Always sort by `Id` asc to get stable A→B→C→D order.
- **Calling `SaveChangesAsync()` once globally at the end:** The existing code calls `SaveChangesAsync()` per-question to obtain the question ID before inserting options. Do not change this pattern — options require a valid `PackageQuestionId`.
- **Modifying the `errors` list for duplicates:** Duplicates are SILENTLY skipped — they do not go into the errors/warning list. Only row parse errors (empty question, missing options, bad Correct value) go into errors.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Whitespace normalization | Custom char-by-char loop | `Regex.Replace(s, @"\s+", " ").Trim()` | Handles all Unicode whitespace, single call |
| Case-insensitive set lookup | Custom comparer class | `ToLowerInvariant()` before adding to HashSet | Simple and explicit |
| Option order stability | Tracking insertion index | `OrderBy(o => o.Id)` | Id auto-increment guarantees A→B→C→D insertion order |

**Key insight:** This phase is string normalization + HashSet membership. The BCL handles all of it — no custom infrastructure needed.

---

## Common Pitfalls

### Pitfall 1: Missing ThenInclude for Options

**What goes wrong:** Fingerprint comparison for the "all options must match" rule always fails (no options loaded) — every question with same text but any option gets through, or worse, fingerprint comparison throws NullReferenceException.
**Why it happens:** Current POST query only does `.Include(p => p.Questions)` — options are not loaded.
**How to avoid:** Add `.ThenInclude(q => q.Options)` to the POST query.
**Warning signs:** Tests where identical questions are not skipped, or NullReferenceExceptions on `q.Options`.

### Pitfall 2: Order-Dependent Option Fingerprint

**What goes wrong:** Two questions with the same 4 options in different arrangements are treated as identical, or vice versa — the same question appears not to match because option Ids happen to be non-consecutive.
**Why it happens:** Options are position-significant (A=0, B=1, C=2, D=3). If you sort by `OptionText` alphabetically or don't sort at all, the fingerprint may be unstable.
**How to avoid:** Sort options by `o.Id` ascending (auto-increment = insertion order = A→B→C→D).
**Warning signs:** Deduplication works in dev (small Id gaps) but fails after a few imports (larger Id gaps).

### Pitfall 3: Skipped Count Contaminating Error Display

**What goes wrong:** Duplicates appear as errors in the Warning message ("Imported X with Y error(s): Row Z: duplicate..."), alarming HC unnecessarily.
**Why it happens:** Reusing the existing `errors` list for duplicates.
**How to avoid:** Maintain a separate `int skipped` counter; never add duplicate rows to `errors`.
**Warning signs:** Warning banners listing row numbers for skips.

### Pitfall 4: All-Duplicates Path Still Redirects to ManagePackages

**What goes wrong:** HC uploads the wrong file, all rows are duplicates, nothing is added, but they get redirected away from the import page and see "0 questions imported" on ManagePackages.
**Why it happens:** Forgetting to branch on `added == 0 && skipped > 0` before the existing redirect.
**How to avoid:** Check for the all-duplicates case BEFORE setting TempData["Success"] and redirecting to ManagePackages.
**Warning signs:** All-duplicates scenario sends HC to ManagePackages with confusing message.

### Pitfall 5: 0-Valid-Rows vs All-Duplicates Message Confusion

**What goes wrong:** HC pastes malformed data, gets the "all duplicates" message — thinks their package already has those questions when really nothing was parseable.
**Why it happens:** Using the same message for both `skipped > 0, added == 0` and `skipped == 0, added == 0` cases.
**How to avoid:** Branch explicitly: check `skipped > 0` first (all-duplicates), then `skipped == 0, added == 0` (nothing valid), then the success path.
**Warning signs:** Both edge cases show the same yellow banner text.

### Pitfall 6: Punctuation Strip Adds Complexity for Little Gain

**What goes wrong:** Stripping punctuation causes "Apa tujuan?" to match "Apa tujuan" — but this may cause false deduplication if HC intentionally has similar questions with different punctuation.
**Why it happens:** Over-normalization.
**How to avoid:** Do NOT strip punctuation. Apply only: trim + collapse internal whitespace + toLowerInvariant.
**Warning signs:** HC reports that a question "should have been added" was skipped.

---

## Code Examples

### Full Normalize Helper

```csharp
// Source: BCL Regex + standard .NET pattern (verified in codebase — Regex.Replace not yet used in CMPController)
private static string NormalizeText(string s)
    => System.Text.RegularExpressions.Regex.Replace(s.Trim(), @"\s+", " ").ToLowerInvariant();

private static string MakeFingerprint(string q, string a, string b, string c, string d)
    => string.Join("|||", new[] { q, a, b, c, d }.Select(NormalizeText));
```

### Updated Include Query

```csharp
// Source: EF Core Include/ThenInclude pattern (used in PreviewPackage at line 3221)
var pkg = await _context.AssessmentPackages
    .Include(p => p.Questions)
        .ThenInclude(q => q.Options)
    .FirstOrDefaultAsync(p => p.Id == packageId);
```

### Building Existing Fingerprints

```csharp
// Source: Codebase analysis — pattern consistent with existing LINQ projections in CMPController
var existingFingerprints = pkg.Questions.Select(q =>
{
    var opts = q.Options.OrderBy(o => o.Id).Select(o => o.OptionText).ToList();
    return MakeFingerprint(
        q.QuestionText,
        opts.ElementAtOrDefault(0) ?? "",
        opts.ElementAtOrDefault(1) ?? "",
        opts.ElementAtOrDefault(2) ?? "",
        opts.ElementAtOrDefault(3) ?? "");
}).ToHashSet();
```

### Dedup Check in Loop

```csharp
// Source: Codebase analysis — inserted after existing validation checks, before SaveChangesAsync
var seenInBatch = new HashSet<string>();

// ... inside the existing validation block, after 'if (!new[] { "A","B","C","D" }.Contains(normalizedCor))' check:

var fp = MakeFingerprint(q, a, b, c, d);
if (existingFingerprints.Contains(fp) || seenInBatch.Contains(fp))
{
    skipped++;
    continue;
}
seenInBatch.Add(fp);
// ... existing SaveChangesAsync block follows unchanged
```

### TempData and Redirect Logic (Replaces Lines 3385-3391)

```csharp
// Replace the existing TempData block at lines 3385-3391

// Edge case: nothing submitted was parseable (0 added, 0 skipped)
if (added == 0 && skipped == 0)
{
    TempData["Warning"] = "No valid questions found in the import. Check the format and try again.";
    return RedirectToAction("ImportPackageQuestions", new { packageId });
}

// Edge case: everything was a duplicate
if (added == 0 && skipped > 0)
{
    TempData["Warning"] = "All questions were already in the package. Nothing was added.";
    return RedirectToAction("ImportPackageQuestions", new { packageId });
}

// Normal success (at least 1 added)
if (excelFile != null && excelFile.Length > 0)
    TempData["Success"] = $"Imported from file: {added} added, {skipped} skipped.";
else
    TempData["Success"] = $"{added} added, {skipped} skipped.";

return RedirectToAction("ManagePackages", new { assessmentId = pkg.AssessmentSessionId });
```

**Note on existing errors:** The existing `if (errors.Any())` block that produced `TempData["Warning"]` with error details is REPLACED by the above logic. Errors from invalid rows (empty text, bad Correct column) are separate from skips. The question is whether to preserve error reporting alongside the success message. Given the context says green banner always (not warning), and errors are malformed rows HC controls, recommend: suppress the existing error-list warning in favor of the clean success message. If errors occur alongside adds, the `added` count accurately reflects what was saved.

---

## State of the Art

| Old Approach | Current Approach | Notes |
|--------------|------------------|-------|
| No deduplication — all rows imported blindly | Fingerprint-based skip with count reporting | Phase 30 adds this |
| Always redirect to ManagePackages | Stay on import page for all-duplicates / 0-valid edge cases | Phase 30 adds this branch |
| TempData["Warning"] for any partial import | TempData["Success"] always for partial success, TempData["Warning"] for zero-add cases | Phase 30 changes messaging |

---

## Open Questions

1. **What happens to the existing `errors` list and error-reporting Warning banner?**
   - What we know: Currently, if any row is skipped for format reasons (empty text, bad Correct), a `TempData["Warning"]` is set instead of `TempData["Success"]`, listing up to 5 errors.
   - What's unclear: The context says green success banner ALWAYS for successful imports. Does this mean suppress error-row reporting entirely?
   - Recommendation: Suppress the error-list warning. The `added` count alone tells HC how many were saved. If HC gets 0 added due to all-format-errors, the new "No valid questions found" message covers it. This keeps the messaging simple and consistent with the context decision.

2. **Preview mode — is it relevant?**
   - What we know: `PreviewPackage` (GET) is a separate action that just reads and displays already-saved questions. It is NOT part of the import flow — there is no "preview before save" step in import.
   - What's unclear: Nothing. Preview mode has no intersection with the import flow.
   - Recommendation: Do not implement dedup in preview. Preview is read-only display of saved data. Dedup runs at save time only (the POST action). This resolves the Claude's Discretion item.

3. **`added == 0 && skipped == 0` AND `errors.Any()` — when does this happen?**
   - What we know: This happens when HC uploads a file or pastes data but every row fails format validation (empty question, missing options, invalid Correct column). `added = 0`, `skipped = 0`, `errors.Count > 0`.
   - Recommendation: This is the "No valid questions found" case. Show the distinct warning message. Optionally, append the first error for context: `"No valid questions found. First error: {errors[0]}"` — but keep it simple, one banner.

---

## Implementation Scope Summary

**Changes required:**

1. **CMPController.cs — POST `ImportPackageQuestions` (~lines 3257-3392):**
   - Add `NormalizeText` and `MakeFingerprint` private static helpers (can add near `ExtractCorrectLetter` at ~line 2369)
   - Change `.Include(p => p.Questions)` to `.Include(p => p.Questions).ThenInclude(q => q.Options)` in the POST query
   - Add `existingFingerprints` HashSet build after pkg load
   - Add `int skipped = 0;` counter alongside `int added = 0;`
   - Add `seenInBatch` HashSet
   - Insert fingerprint check + `seenInBatch.Add` inside validation loop
   - Replace TempData block (lines 3385-3391) with the 3-branch logic above

2. **Views/CMP/ImportPackageQuestions.cshtml:**
   - No changes needed. The existing `TempData["Warning"]` block at line 17 already renders yellow banners for stay-on-page cases.

**Total estimated additions:** ~20-30 lines in CMPController.cs. Zero lines elsewhere.

---

## Sources

### Primary (HIGH confidence)

- Codebase: `Controllers/CMPController.cs` lines 3238-3392 — complete `ImportPackageQuestions` GET and POST implementation, read directly
- Codebase: `Models/AssessmentPackage.cs` — `PackageQuestion` and `PackageOption` model structure verified directly
- Codebase: `Data/ApplicationDbContext.cs` — DbSet names and EF configuration verified directly
- Codebase: `Views/CMP/ImportPackageQuestions.cshtml` — TempData["Warning"] block at line 17 verified directly
- Codebase: `Views/Shared/_Layout.cshtml` lines 192-222 — TempData["Warning"], ["Error"], ["Success"] alert rendering verified; no ["Info"] alert block exists

### Secondary (MEDIUM confidence)

- BCL `System.Text.RegularExpressions.Regex.Replace` with `@"\s+"` pattern — standard .NET BCL behavior, well-established
- EF Core `ThenInclude` pattern — verified by existing usage at CMPController line 3221 (`PreviewPackage` action)

### Tertiary (LOW confidence)

None — all findings are grounded in direct codebase inspection.

---

## Metadata

**Confidence breakdown:**
- Implementation scope: HIGH — read all relevant code directly
- Dedup algorithm: HIGH — straightforward HashSet pattern
- TempData routing: HIGH — all message paths and existing banner blocks verified
- Options ordering: HIGH — OrderBy(Id) is stable; verified no explicit order column exists in PackageOption
- Preview mode: HIGH — confirmed it is separate read-only action, not part of import flow

**Research date:** 2026-02-23
**Valid until:** 2026-03-25 (stable codebase; changes only if CMPController import section is modified by another phase)
