# Phase 9: Gap Analysis Removal - Research

**Researched:** 2026-02-18
**Domain:** ASP.NET Core 8 MVC — dead-feature removal (controller action, view, nav links, ViewModel)
**Confidence:** HIGH — all findings based on direct file reads of the actual codebase; no inference required

---

## Summary

Phase 9 is a pure deletion phase. There is no new code to write, no new dependency to introduce, and no data migration. The entire surface area has been confirmed by direct codebase audit: five artifacts must be removed and two nav references must be cleaned up in place.

The `CompetencyGap` action (`CMPController.cs` lines 1532–1632) builds a gap analysis ViewModel from `KkjMatrices`, `UserCompetencyLevels`, and `IdpItems`. It is backed by `CompetencyGap.cshtml` and `CompetencyGapViewModel.cs`. The action is linked from two live pages (`CMP/Index.cshtml` and `CMP/CpdpProgress.cshtml`). The private helper `GenerateIdpSuggestion` (lines 1815–1837) is called exclusively from within this action and must be deleted alongside it. The `_Layout.cshtml` top nav has NO reference to this feature — confirmed clean.

The critical execution discipline is deletion order: remove nav links first (so no dead links ever exist in production), then remove the controller action and private helper, then delete the view file, then delete the ViewModel file. This order ensures any missed links surface as compile-time or runtime errors before files are destroyed. The planner should produce one task per deletion step, not a single "delete everything" task.

**Primary recommendation:** Remove nav links in `Index.cshtml` and `CpdpProgress.cshtml` first, then delete `CompetencyGap()` action + `GenerateIdpSuggestion()` helper from the controller, then delete the view file, then delete the ViewModel file. Verify by grepping for `CompetencyGap` after each step.

---

## Standard Stack

### Core (No Changes Required)

| Library | Version | Purpose | Notes |
|---------|---------|---------|-------|
| ASP.NET Core 8 MVC | 8.0.x | Routing, controller actions | Deletion of an action is standard MVC; no config change needed |
| Razor Views | ASP.NET Core 8 | View files (.cshtml) | Deleting a .cshtml file is sufficient; no view registration to undo |
| EF Core | 8.0.0 | Database queries | No schema changes — no migration needed |

### No New Libraries

This phase introduces zero new dependencies. All work is deletion or simplification of existing code.

---

## Architecture Patterns

### Recommended Deletion Order

```
Step 1: Edit Views/CMP/Index.cshtml        — Remove Gap Analysis card block (lines 58–77)
Step 2: Edit Views/CMP/CpdpProgress.cshtml — Remove Gap Analysis nav link (line 19)
Step 3: Edit Controllers/CMPController.cs  — Delete CompetencyGap() action (lines 1532–1632)
                                            — Delete GenerateIdpSuggestion() helper (lines 1815–1837)
Step 4: Delete Views/CMP/CompetencyGap.cshtml
Step 5: Delete Models/Competency/CompetencyGapViewModel.cs
```

**Why this order matters:** Removing nav links first means users can never click a dead link. Removing the controller action second causes an immediate build/runtime error if any link was missed, catching it before the view is gone. Deleting files last means the compiler still has them available during the link-audit window.

### Pattern: Removing a Card Block From the CMP Index Hub

The Gap Analysis card occupies a complete Bootstrap column block in `Views/CMP/Index.cshtml`. The block to delete is the entire `col-12 col-md-6 col-lg-4` div starting at line 58 (comment `<!-- Competency Gap Analysis -->`) through line 77 (closing `</div>`):

```cshtml
<!-- DELETE THIS ENTIRE BLOCK (lines 58-77) -->
<!-- Competency Gap Analysis -->
<div class="col-12 col-md-6 col-lg-4">
    <div class="card border-0 shadow-sm h-100">
        <div class="card-body">
            ...
            <a href="@Url.Action("CompetencyGap", "CMP")" class="btn btn-warning w-100">
                <i class="bi bi-arrow-right-circle me-2"></i>View Gap Analysis
            </a>
        </div>
    </div>
</div>
```

The remaining cards (KKJ Matrix, CPDP Mapping, Assessment Lobby, Training Records, HC Reports) are unaffected. The grid will reflow automatically since Bootstrap uses `col-12 col-md-6 col-lg-4` — no layout fix needed.

### Pattern: Removing the Sibling Nav Link From CpdpProgress

`Views/CMP/CpdpProgress.cshtml` line 19 contains a two-button nav row. The Gap Analysis button is the FIRST button; the CPDP Progress button is the second (and is the active page). Remove only the first `<a>` element:

```cshtml
<!-- Navigation Tabs — BEFORE -->
<div class="mb-3">
    <a href="@Url.Action("CompetencyGap", "CMP", new { userId = Model.UserId })" class="btn btn-outline-primary btn-sm">
        <i class="bi bi-bar-chart-steps me-1"></i>Gap Analysis
    </a>
    <a href="@Url.Action("CpdpProgress", "CMP", new { userId = Model.UserId })" class="btn btn-primary btn-sm active">
        <i class="bi bi-journal-check me-1"></i>CPDP Progress
    </a>
</div>

<!-- Navigation Tabs — AFTER -->
<div class="mb-3">
    <a href="@Url.Action("CpdpProgress", "CMP", new { userId = Model.UserId })" class="btn btn-primary btn-sm active">
        <i class="bi bi-journal-check me-1"></i>CPDP Progress
    </a>
</div>
```

The `<div class="mb-3">` wrapper can remain (it holds one link) or be removed entirely — either is correct. Keeping it is lower risk.

### Pattern: Deleting the Controller Action + Private Helper

The `CompetencyGap()` action spans **lines 1532–1632** in `CMPController.cs` (inclusive of the comment header `// --- COMPETENCY GAP ANALYSIS ---`). Delete this entire block.

The `GenerateIdpSuggestion()` private helper method spans **lines 1815–1837**. It is called exclusively from within the `CompetencyGap()` action. Delete this entire method. After deletion, the `#region Helper Methods` block will contain only `GenerateSecureToken()` — the region itself survives.

**Important:** The `using HcPortal.Models.Competency;` directive at line 8 of `CMPController.cs` must NOT be removed. It is also required for:
- `CpdpProgressViewModel` (used by `CpdpProgress()` action)
- `UserCompetencyLevel` (used in `_context.UserCompetencyLevels` queries)
- `AssessmentCompetencyMap` (used elsewhere in the controller)

### Anti-Patterns to Avoid

- **Add a redirect stub:** STACK.md explicitly decided "Hard 404 on CompetencyGap removal, not a redirect." The content was removed, not moved. A 404 is semantically correct. Do NOT add `return RedirectToAction("CpdpProgress")` as a stub.
- **Remove the `using HcPortal.Models.Competency;` directive:** This namespace covers more than `CompetencyGapViewModel`. Removing it causes a compile error.
- **Delete files before removing nav links:** Results in a window where nav links point to a 404.
- **Leave the ViewModel file:** `CompetencyGapViewModel.cs` has no other consumers after the action and view are deleted. Leaving it creates dead code confusion.

---

## Confirmed Surface Area

All five touch points confirmed by direct file read. This is the complete list — nothing else references this feature.

| # | Artifact | Location | What to Do |
|---|----------|----------|------------|
| 1 | Gap Analysis card link | `Views/CMP/Index.cshtml` lines 58–77 | Delete the entire `col-12 col-md-6 col-lg-4` div block |
| 2 | Gap Analysis nav link | `Views/CMP/CpdpProgress.cshtml` line 19 | Delete the first `<a>` tag (the `CompetencyGap` link) only |
| 3 | `CompetencyGap()` action | `Controllers/CMPController.cs` lines 1532–1632 | Delete the entire method including comment header |
| 4 | `GenerateIdpSuggestion()` helper | `Controllers/CMPController.cs` lines 1815–1837 | Delete the entire private method (exclusive to CompetencyGap) |
| 5 | View file | `Views/CMP/CompetencyGap.cshtml` | Delete the file |
| 6 | ViewModel file | `Models/Competency/CompetencyGapViewModel.cs` | Delete the file (contains `CompetencyGapViewModel` and `CompetencyGapItem` classes) |

**Confirmed clean (no action needed):**
- `Views/Shared/_Layout.cshtml` — no CompetencyGap link in top nav (verified by read)
- All other controllers — no reference to `CompetencyGap` action (confirmed by grep across `**/*.cs`)
- No JS string literals for `CompetencyGap` outside of `CompetencyGap.cshtml` itself (which will be deleted)

**Clarification on milestone notes:** The milestone context mentioned "One JS string literal in CmdpProgress.cshtml line 19" — this is a terminology mismatch. `CpdpProgress.cshtml` line 19 uses `@Url.Action("CompetencyGap", ...)` inside an anchor href attribute — not a raw JS string. The actual JS string literal (`window.location.href='/CMP/CompetencyGap?userId=...'`) is at line 36 of `CompetencyGap.cshtml` itself, inside the `onchange` handler of the user-select dropdown. Since `CompetencyGap.cshtml` will be deleted entirely, this JS string is moot — no separate action is needed.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead |
|---------|-------------|-------------|
| Protecting against broken links | A redirect stub | Correct deletion order: nav links first, then action |
| Finding all references before deletion | Manual code reading | `grep -r "CompetencyGap" --include="*.cs" --include="*.cshtml"` |

---

## Common Pitfalls

### Pitfall 1: Removing Action Before Removing Nav Links
**What goes wrong:** The action is deleted first. The two nav links in `Index.cshtml` and `CpdpProgress.cshtml` remain live and now generate URLs that return 404. Users click them and see error pages.
**Why it happens:** Developers think "delete the source first, then clean up references." In a compiled language this works because the compiler catches dangling references. In Razor views, `Url.Action("CompetencyGap", "CMP")` is a runtime call — it generates a URL string regardless of whether the action exists.
**How to avoid:** Always remove inbound links before removing the target. Step 1 and Step 2 must complete before Step 3.
**Warning signs:** Any 404 in the application during the removal window indicates a nav link was not cleaned up before the action was removed.

### Pitfall 2: Removing the `using HcPortal.Models.Competency;` Directive
**What goes wrong:** Developer sees that `CompetencyGapViewModel` is being deleted and removes the `using` directive at line 8 of `CMPController.cs`. The build fails because `CpdpProgressViewModel`, `UserCompetencyLevel`, and `AssessmentCompetencyMap` are all in the same namespace.
**Why it happens:** The `using` statement is associated with the file being deleted, so it looks like dead code.
**How to avoid:** Verify all usages of the namespace before removing the `using` directive. In this case: the directive must stay. Do not touch it.
**Warning signs:** Build error `The type or namespace name 'CpdpProgressViewModel' could not be found`.

### Pitfall 3: Partial Block Deletion in Index.cshtml
**What goes wrong:** Only the `<a>` link inside the card is removed but the surrounding card `div` and column `div` remain. The CMP Index hub now shows an empty card with a heading "Gap Analysis" and no button.
**Why it happens:** Developer removes the link element but forgets to remove the full enclosing Bootstrap column block.
**How to avoid:** Delete the entire `col-12 col-md-6 col-lg-4` wrapper div (lines 58–77), not just the `<a>` tag inside it.
**Warning signs:** CMP Index page renders with an empty grey card where the Gap Analysis card used to be.

### Pitfall 4: Missing the `GenerateIdpSuggestion` Private Helper
**What goes wrong:** The `CompetencyGap()` action is deleted but `GenerateIdpSuggestion()` is left in the `#region Helper Methods` block. The application compiles fine (unused private method), but dead code remains that can confuse future developers.
**Why it happens:** The helper is physically far from the action in the file (action at line 1532; helper at line 1815). Easy to miss in a large file.
**How to avoid:** The pre-deletion grep for `GenerateIdpSuggestion` confirms it has exactly two occurrences: the call site (inside CompetencyGap action) and the declaration. Both disappear when action and helper are deleted.
**Warning signs:** Build succeeds but grep for `GenerateIdpSuggestion` in `CMPController.cs` returns one hit instead of zero.

---

## Code Examples

### Pre-Deletion Verification Grep (run these before starting)

```bash
# Find all references to CompetencyGap across the codebase
grep -rn "CompetencyGap" --include="*.cs" --include="*.cshtml" .

# Find all references to GenerateIdpSuggestion
grep -rn "GenerateIdpSuggestion" --include="*.cs" .
```

Expected output BEFORE deletion:
```
Controllers/CMPController.cs:1533:  public async Task<IActionResult> CompetencyGap(...)
Controllers/CMPController.cs:1584:  return new CompetencyGapItem
Controllers/CMPController.cs:1596:  SuggestedAction = gap > 0 ? GenerateIdpSuggestion(...)
Controllers/CMPController.cs:1603:  var viewModel = new CompetencyGapViewModel
Controllers/CMPController.cs:1815:  private string GenerateIdpSuggestion(...)
Models/Competency/CompetencyGapViewModel.cs:6:  public class CompetencyGapViewModel
Models/Competency/CompetencyGapViewModel.cs:16: public List<CompetencyGapItem> ...
Models/Competency/CompetencyGapViewModel.cs:42: public class CompetencyGapItem
Views/CMP/CompetencyGap.cshtml:1:   @model HcPortal.Models.Competency.CompetencyGapViewModel
Views/CMP/CompetencyGap.cshtml:22:  @Url.Action("CompetencyGap", ...)
Views/CMP/CompetencyGap.cshtml:36:  window.location.href='/CMP/CompetencyGap?userId='
Views/CMP/Index.cshtml:72:          @Url.Action("CompetencyGap", "CMP")
Views/CMP/CpdpProgress.cshtml:19:   @Url.Action("CompetencyGap", "CMP", ...)
```

Expected output AFTER full deletion: zero results.

### Post-Deletion Build Verification

```bash
dotnet build
```

Expected: build succeeds with zero errors and zero warnings related to CompetencyGap.

---

## Open Questions

None. All touch points have been confirmed by direct file read. The surface area is completely known. No architectural decisions remain — the milestone STACK.md and PITFALLS.md have resolved all strategy questions (no redirect stub, hard 404 is correct, deletion order is prescribed).

---

## Sources

### Primary (HIGH confidence — direct file reads, 2026-02-18)

- `Controllers/CMPController.cs` — CompetencyGap action (lines 1532–1632), GenerateIdpSuggestion helper (lines 1815–1837), using directive (line 8), CpdpProgress action (lines 1634+)
- `Views/CMP/Index.cshtml` — Gap Analysis card block (lines 58–77), confirmed as `@Url.Action("CompetencyGap", "CMP")`
- `Views/CMP/CpdpProgress.cshtml` — Gap Analysis nav link (line 19), confirmed as `@Url.Action("CompetencyGap", "CMP", new { userId = Model.UserId })`
- `Views/CMP/CompetencyGap.cshtml` — Full file read; JS string literal at line 36 noted (in file to be deleted)
- `Models/Competency/CompetencyGapViewModel.cs` — Full file read; two classes: `CompetencyGapViewModel` and `CompetencyGapItem`
- `Models/Competency/CpdpProgressViewModel.cs` — Confirmed in same namespace `HcPortal.Models.Competency`
- `Models/Competency/UserCompetencyLevel.cs` — Confirmed in same namespace `HcPortal.Models.Competency`
- `Models/Competency/AssessmentCompetencyMap.cs` — Confirmed in same namespace `HcPortal.Models.Competency`
- `Views/Shared/_Layout.cshtml` — Confirmed: no Gap Analysis link in top nav
- Grep across `**/*.cs` and `**/*.cshtml` — Confirmed: complete list of `CompetencyGap` references above; no additional files

### Secondary (HIGH confidence — direct reads, 2026-02-18)

- `.planning/research/STACK.md` — Goal 3 section: deletion strategy, "Hard 404, not redirect" decision, safe deletion order
- `.planning/research/PITFALLS.md` — Pitfall 7 (Orphaned Links After CompetencyGap Deletion), Pitfall checklist item for this phase

---

## Metadata

**Confidence breakdown:**
- Surface area (all touch points): HIGH — all confirmed by direct file read and grep
- Deletion order strategy: HIGH — derived from STACK.md Goal 3 + PITFALLS.md Pitfall 7
- No-redirect decision: HIGH — explicit in STACK.md Alternatives Considered table
- `using` directive retention: HIGH — confirmed by reading all files in `Models/Competency/`

**Research date:** 2026-02-18
**Valid until:** No expiry — this is a pure deletion; codebase state will not change unless another phase modifies CMPController before Phase 9 executes. Re-verify line numbers if any commits touch `CMPController.cs` between now and execution.
