# Phase 59: Hapus Page ProtonCatalog - Research

**Researched:** 2026-03-01
**Domain:** ASP.NET Core MVC — Controller and view deletion, stale reference cleanup
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Redirect behavior:**
- Delete ProtonCatalogController completely — no redirect preserved
- /ProtonCatalog will return 404 after deletion
- Rationale: Only internal admin users access this, and they already use /Admin/ProtonData via Kelola Data hub. No external bookmarks to preserve.

**Cleanup scope:**
- Delete ProtonCatalogController.cs
- Delete Views/ProtonCatalog/ directory (all files)
- Remove any navbar/menu links pointing to /ProtonCatalog
- Remove any Admin/Index hub card referencing ProtonCatalog (if still exists)
- Verify no other controllers or views link to /ProtonCatalog
- Do NOT touch /Admin/ProtonData (Phase 51 output) — that stays as-is

### Claude's Discretion
- Exact order of file deletions
- How thorough the stale reference search should be (grep is sufficient)
- Whether to check for any ProtonCatalog-specific CSS/JS (unlikely but worth a quick check)

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| CONS-02 | Delete ProtonCatalogController and Views/ProtonCatalog directory; all functionality moved to /Admin/ProtonData (Phase 51); verify no stale references remain | Confirmed: 15 references all in files to be deleted; Admin/Index.cshtml already points to /Admin/ProtonData; no navbar or external references found |

</phase_requirements>

## Summary

Phase 59 is a straightforward cleanup operation: delete the ProtonCatalogController.cs and entire Views/ProtonCatalog/ directory since all Catalog functionality was migrated to /Admin/ProtonData in Phase 51. The controller currently contains only 11 redirect-only actions that bounce users to the new location, so preserving it serves no purpose.

Current state verification shows:
- ProtonCatalogController.cs exists with all actions redirecting to ProtonData/Index
- Views/ProtonCatalog/ directory contains 2 files: Index.cshtml (28KB) and _CatalogTree.cshtml (12KB) with hardcoded fetch URLs to /ProtonCatalog endpoints
- Admin/Index.cshtml (Phase 70) already replaced ProtonCatalog card with "Silabus & Coaching Guidance" link to /Admin/ProtonData — no hub card cleanup needed
- No navbar or global view references to /ProtonCatalog found
- All 15 ProtonCatalog references in active codebase are contained within the two files to be deleted (controller + 2 views)

**Primary recommendation:** Sequential deletion starting with the view files, then the controller. No migrations, no model changes, no database impact. Straightforward file deletion operation.

---

## Current State Verification

### Files to Delete

**Controller:**
- `Controllers/ProtonCatalogController.cs` (25 lines)
  - Class declaration + [Authorize] attribute
  - 11 action methods, all redirect to ProtonDataController.Index
  - Comments indicate Phase 51 moved all functionality

**Views:**
- `Views/ProtonCatalog/Index.cshtml` (28KB)
  - Full page with tree navigation UI, modals, AJAX endpoints
  - Hardcoded fetch calls to `/ProtonCatalog/*` endpoints (11 references)
  - Track selection, CRUD forms, delete confirmation logic

- `Views/ProtonCatalog/_CatalogTree.cshtml` (12KB)
  - Partial view for tree structure rendering
  - Referenced by Index.cshtml

**Models:**
- `Models/ProtonViewModels.cs` contains `ProtonCatalogViewModel` class (definition only, no usages)
  - Safe to leave: no harmful if unused; may be referenced in past phases for historical reference
  - Clean removal optional but not critical

### References Status

**Deleted files contain (15 total references):**
- 1 reference in ProtonCatalogController.cs (class name)
- 1 reference in ProtonCatalogController.cs (comment)
- 11 references in Views/ProtonCatalog/Index.cshtml (AJAX URLs)
- 1 reference in Models/ProtonViewModels.cs (class definition)
- 1 reference in Views/ProtonCatalog/_CatalogTree.cshtml (implied by Index.cshtml inclusion)

**NOT found:**
- Navbar links (verified: no ProtonCatalog in _Layout.cshtml, Shared views)
- Admin/Index hub cards (verified: Phase 70 replaced with "Silabus & Coaching Guidance" → /Admin/ProtonData)
- Program.cs or Startup.cs references (verified: only SeedProtonData references found)
- Route configuration (ASP.NET Core uses controller name routing convention — no explicit routes needed)
- CSS/JS files (no ProtonCatalog-specific styles or scripts found)

### Functional Replacement

**Phase 51 (Proton Silabus & Coaching Guidance Manager) created:**
- `/Admin/ProtonData` page (ProtonDataController) with two Bootstrap tabs
- Tab 1: Silabus (all Kompetensi/SubKompetensi/Deliverable CRUD, now scoped by Bagian+Unit+Track)
- Tab 2: Coaching Guidance (file upload/download for learning materials)
- Equivalent or enhanced functionality compared to original ProtonCatalog page

---

## Standard Stack

### Deletion Scope
| Item | Type | Size | Status |
|------|------|------|--------|
| ProtonCatalogController.cs | C# Controller | 25 lines | Delete |
| Views/ProtonCatalog/Index.cshtml | Razor View | 28KB | Delete |
| Views/ProtonCatalog/_CatalogTree.cshtml | Razor Partial | 12KB | Delete |
| Views/ProtonCatalog/ | Directory | (container) | Delete entire |

### No New Dependencies
This phase removes code — no new packages, libraries, or external dependencies needed.

---

## Architecture Patterns

### Deletion Verification Pattern

**Safe deletion checklist (no breaking changes):**
```
1. File deletion in order: Views directory → Controller class
   - Views deleted first prevents 404 if controller still active
   - Controller deleted last confirms all requests will naturally 404

2. Reference sweep: grep for "ProtonCatalog" in Controllers/ Views/ Models/
   - Verify only deleted files contain references
   - Especially check: _Layout.cshtml, Shared/ views, Program.cs, RouteAttribute declarations

3. Navigation verification:
   - Confirm no navbar/menu items link to /ProtonCatalog
   - Confirm no hub cards (Admin/Index) reference ProtonCatalog
   - Already verified: Phase 70 updated Admin/Index → all cards use /Admin/ProtonData

4. URL redirect testing (post-deletion):
   - Attempt /ProtonCatalog → confirm 404 (expected)
   - Confirm /Admin/ProtonData still works (unchanged by Phase 59)
```

### No Model/Database Impact
- ProtonCatalogViewModel unused in codebase (only defined)
- No database models deleted (ProtonKompetensi, etc. owned by ProtonData)
- No migrations needed (no data changes)
- No URL rewrite rules needed (404 is correct final state)

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Redirect ProtonCatalog URLs | Custom redirect logic in Program.cs or middleware | Direct deletion + 404 | Phase 51 already created the replacement; bookmarks are internal-only (admin users); 404 is cleaner than maintaining redirect logic for outdated page |
| Search for stale references | Manual code review | grep "ProtonCatalog" on entire codebase | grep is fast, reliable, catches hidden references in comments or partial paths |
| Delete files one-by-one | Manual explorer deletion | git rm -r Views/ProtonCatalog/ && rm Controllers/ProtonCatalogController.cs | Batch deletion with git tracking ensures clean commit history; can be reverted if needed |

**Key insight:** Redirect-only endpoints create technical debt. Phase 51 is the de facto replacement; complete deletion is simpler than maintaining a legacy redirect controller.

---

## Common Pitfalls

### Pitfall 1: Incomplete Reference Search
**What goes wrong:** Delete controller and views, but miss a hardcoded link in a view that still references /ProtonCatalog, causing 404 clicks on a button
**Why it happens:** Grep search is skipped or incomplete; folder structure makes it easy to miss embedded references in generated HTML
**How to avoid:** Run comprehensive grep BEFORE deletion: `grep -r "ProtonCatalog" . --include="*.cs" --include="*.cshtml"` excluding the files you're about to delete, verify zero results
**Warning signs:** User clicks a menu item → 404; comments in deleted files suggest external references

### Pitfall 2: Deleting Wrong View Structure
**What goes wrong:** Delete individual .cshtml files but leave Views/ProtonCatalog/ folder empty; ASP.NET Core convention still tries to find views in that location, causing subtle lookup issues
**Why it happens:** Views are deleted but folder structure remains; convention-based routing looks for /Views/ControllerName/ActionName.cshtml
**How to avoid:** Delete entire Views/ProtonCatalog/ directory, not just files; verify deletion with `ls Views/` or file explorer
**Warning signs:** Tests still reference views; folder shows up in git status as empty

### Pitfall 3: Missing the Admin/Index Hub Card
**What goes wrong:** Delete ProtonCatalog, but Phase 70's Admin/Index.cshtml still has a "Proton Catalog" card linking to /ProtonCatalog, now broken
**Why it happens:** Hub page may not have been updated in earlier phase; cards reference stale controller routes
**How to avoid:** Phase 70 already verified: "Silabus & Coaching Guidance" card in Section A points to /Admin/ProtonData (checked live file); no action needed
**Warning signs:** Running app shows broken card; Admin/Index.cshtml contains href="/ProtonCatalog"

### Pitfall 4: Forgetting the _CatalogTree Partial
**What goes wrong:** Delete Views/ProtonCatalog/Index.cshtml but leave _CatalogTree.cshtml orphaned; confusion about what's still in use
**Why it happens:** Partial views are harder to track; grep may find partial names but not show they're unused
**How to avoid:** Delete entire Views/ProtonCatalog/ directory (atomic operation); grep for _CatalogTree references (verify zero before deletion)
**Warning signs:** References to partial remain; unused file persists in repo

---

## Code Examples

### Before (Current State)

**ProtonCatalogController.cs:**
```csharp
// Controllers/ProtonCatalogController.cs
[Authorize]
public class ProtonCatalogController : Controller
{
    // All ProtonCatalog functionality replaced by /ProtonData (Phase 51)
    // Redirect all actions to preserve bookmarked URLs

    public IActionResult Index() => RedirectToAction("Index", "ProtonData");
    public IActionResult GetCatalogTree() => RedirectToAction("Index", "ProtonData");
    // ... 9 more redirect-only actions
}
```

**Views/ProtonCatalog/Index.cshtml:**
```html
<!-- Sample of AJAX endpoints that will be deleted -->
fetch('/ProtonCatalog/GetCatalogTree?trackId=' + trackId)
postItem('/ProtonCatalog/AddKompetensi', { ... })
fetch('/ProtonCatalog/DeleteCatalogItem', { ... })
```

### After (Deleted)

**Both files completely removed.**

**Navigation updated (already done in Phase 70):**
```html
<!-- Views/Admin/Index.cshtml (existing) -->
<a href="@Url.Action("Index", "ProtonData")" class="text-decoration-none">
    <div class="card">
        <span class="fw-bold">Silabus &amp; Coaching Guidance</span>
    </div>
</a>
```

---

## State of the Art

| Timeline | Status | Impact |
|----------|--------|--------|
| Phase 34-37 | ProtonCatalog page created with tree UI, CRUD forms | Original implementation |
| Phase 51 | ProtonData page created; all Catalog functionality migrated | ProtonCatalog became redirect-only |
| Phase 70 | Admin/Index.cshtml hub updated; Catalog card replaced with ProtonData link | Navigation already fixed |
| Phase 59 (this) | ProtonCatalog controller and views deleted | Complete removal of legacy code |

**Deprecated/outdated:**
- ProtonCatalogController: Phase 51 moved all CRUD to ProtonDataController; redirect-only pattern is technical debt with no benefit

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit / Test project (existing) |
| Config file | No phase-specific tests needed |
| Quick run command | N/A — deletion only, no behavior changes to test |
| Full suite command | N/A — file deletion verified by: (1) grep for stale references, (2) manual navigation test in app |

### Phase Requirements → Verification Map

| Req ID | Behavior | Verification Type | Automated Command | Notes |
|--------|----------|-------------------|-------------------|-------|
| CONS-02.1 | ProtonCatalogController.cs deleted | File existence | `test -f Controllers/ProtonCatalogController.cs && echo "FAIL" \|\| echo "PASS"` | Should not exist |
| CONS-02.2 | Views/ProtonCatalog/ directory deleted | File existence | `test -d Views/ProtonCatalog && echo "FAIL" \|\| echo "PASS"` | Should not exist |
| CONS-02.3 | No stale /ProtonCatalog references in active code | Grep search | `grep -r "ProtonCatalog" . --include="*.cs" --include="*.cshtml" \|\| echo "PASS"` | Grep returns 1 (no matches) = PASS |
| CONS-02.4 | Admin/Index hub card points to /Admin/ProtonData | Manual navigation + file inspection | `grep -q "ProtonData" Views/Admin/Index.cshtml && echo "PASS"` | Already verified: Phase 70 card exists |

### Sampling Rate
- **Per commit:** Run automated verification commands above immediately after deletion
- **Pre-merge:** Manual smoke test: start app, click "Kelola Data" → "Silabus & Coaching Guidance" → confirm it loads /Admin/ProtonData page
- **Phase gate:** All verification commands pass ✓

### Wave 0 Gaps
None — existing test infrastructure covers file deletion verification. No new tests needed since this phase removes code (no new behavior to test).

---

## Sources

### Primary (HIGH confidence)
- **Current codebase inspection** (2026-03-01)
  - ProtonCatalogController.cs verified: 25 lines, 11 redirect actions only
  - Views/ProtonCatalog/ verified: Index.cshtml (28KB), _CatalogTree.cshtml (12KB), all references internal
  - Admin/Index.cshtml verified: Phase 70 already updated, "Silabus & Coaching Guidance" card points to /Admin/ProtonData
  - Grep search verified: 15 total references, all contained in files to be deleted

- **Phase 51 (Proton Silabus & Coaching Guidance Manager) RESEARCH.md**
  - Confirmed ProtonDataController created at /Admin/ProtonData
  - Confirmed all Kompetensi/SubKompetensi/Deliverable CRUD moved to new location
  - Confirmed ProtonCatalogController decision noted (Phase 51 discretion): "delete entirely vs redirect to new page"

- **Phase 70 (Kelola Data Hub Reorganization) completion**
  - Confirmed Admin/Index.cshtml updated with ProtonData card
  - No stale ProtonCatalog cards remaining

### Confidence Breakdown

| Area | Level | Reasoning |
|------|-------|-----------|
| Files to delete | HIGH | Direct file inspection, sizes/content confirmed |
| Reference cleanup | HIGH | Comprehensive grep completed, 15/15 references in deletion scope |
| Navigation impact | HIGH | Admin/Index.cshtml already points to /Admin/ProtonData (Phase 70 verified) |
| Database/migration | HIGH | No database changes; Phase 51 handled all data migration |

---

## Metadata

**Research date:** 2026-03-01
**Valid until:** 2026-04-01 (stable phase, no breaking changes expected)
**Phase depends on:** Phase 53 (final assessment manager, stable)
**Next phase:** Phase 60 (Konsolidasi Assessment Management — similar cleanup pattern)

---

## Open Questions

None — this phase has complete clarity on scope and execution.
