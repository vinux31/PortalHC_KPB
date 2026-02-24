---
phase: 37-drag-drop-reorder
verified: 2026-02-24T14:30:00Z
status: cancelled
score: 0/7 must-haves verified
re_verification: false
gaps:
  - truth: "HC/Admin can drag a Kompetensi row to a new position"
    status: failed
    reason: "Feature completely reverted - all endpoints, UI, and JS functions removed"
    missing:
      - "ReorderKompetensi POST endpoint"
      - "SortableJS CDN"
      - "Grip handle markup and functions"

  - truth: "HC/Admin can drag a SubKompetensi row within its parent"
    status: failed
    reason: "Feature completely reverted"
    missing:
      - "ReorderSubKompetensi POST endpoint"

  - truth: "HC/Admin can drag a Deliverable row within its parent"
    status: failed
    reason: "Feature completely reverted"
    missing:
      - "ReorderDeliverable POST endpoint"

  - truth: "Grip handles appear with correct visibility rules"
    status: failed
    reason: "All grip handle UI removed"
    missing:
      - "Grip handle elements and visibility logic"

  - truth: "In-flight drag lock prevents concurrent saves"
    status: failed
    reason: "Sortable functions removed"
    missing:
      - "disableAllSortables/enableAllSortables functions"

  - truth: "Failed saves show error and restore order"
    status: failed
    reason: "Error handling removed"
    missing:
      - "showReorderError function"

  - truth: "Reorder constrained within same parent"
    status: failed
    reason: "initSortables removed"
    missing:
      - "SortableJS per-tbody initialization"
---

# Phase 37: Drag-and-Drop Reorder Verification Report

**Phase Goal:** HC/Admin can drag Kompetensi, SubKompetensi, or Deliverable rows to reorder within their level; new order persists immediately.

**Verified:** 2026-02-24T14:30:00Z
**Status:** GAPS_FOUND
**Score:** 0/7 must-haves verified

## Critical Finding

The drag-and-drop reorder feature has been **COMPLETELY REMOVED** from the codebase.

### Timeline

- **1bd199e** (feat): Three Reorder POST endpoints added to ProtonCatalogController
- **c0dae6a** (feat): SortableJS CDN and grip handle markup added  
- **7670925** (feat): initSortables() and drag functions added to Index.cshtml
- **6a770c2** (fix): Collapse container repositioning fix
- **e8e19dc** (revert): All 244 lines of frontend code + 77 lines of backend removed
- **2157fda** (docs): Phase completed with note "reorder removed"

### Reason for Removal

The nested-table tree structure proved incompatible with SortableJS. Collapse containers don't move atomically with parent rows, causing visual glitches and unreliable behavior.

## Goal Achievement Analysis

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Can drag Kompetensi rows; order persists | FAILED | ReorderKompetensi endpoint missing; initSortables removed; tree is 3-column |
| 2 | Can drag SubKompetensi within parent | FAILED | ReorderSubKompetensi endpoint missing; same frontend issues |
| 3 | Can drag Deliverable without collapsing | FAILED | ReorderDeliverable endpoint missing; no drag handles |
| 4 | Grip handles appear/disappear correctly | FAILED | No grip handles exist; tree has no 4th column |
| 5 | Concurrent drag prevented by lock | FAILED | disableAllSortables/enableAllSortables removed |
| 6 | Failed saves show error alert | FAILED | showReorderError function removed |
| 7 | Cross-parent drag not possible | FAILED | initSortables initialization removed |

**Score: 0/7 truths verified**

### Artifact Status

| File | Expected | Current | Status |
|------|----------|---------|--------|
| ProtonCatalogController.cs | 3 Reorder POST actions | DeleteCatalogItem only; ends line 409 | MISSING |
| _Layout.cshtml | SortableJS 1.15.7 CDN | No sortablejs tag | MISSING |
| Index.cshtml | 5 drag functions + wiring | Only collapse preservation | MISSING |
| _CatalogTree.cshtml | 4-column layout with handles | 3-column layout, no handles | MISSING |

### Key Links Status

| Link | Status |
|------|--------|
| initSortables() → [data-sortable-level] tbodies | NOT_WIRED |
| handleDropEnd() → /ProtonCatalog/Reorder* | NOT_WIRED |
| initCatalogTree() → initSortables() | NOT_WIRED |

## What Was Shipped Instead

Only collapse-state preservation in reloadTree() (lines 210-228 Index.cshtml):

```javascript
// Remember expanded state before reload, restore after
var openIds = [];
document.querySelectorAll('#treeContainer .collapse.show').forEach(function(el) {
    if (el.id) openIds.push(el.id);
});
// ... restore after tree HTML update
openIds.forEach(function(id) {
    var el = document.getElementById(id);
    if (el) bootstrap.Collapse.getOrCreateInstance(el, { toggle: false }).show();
});
```

**This is NOT drag-and-drop reorder.** It prevents tree collapse when adding items.

## Verification Conclusion

**Phase goal NOT achieved.** The feature was designed and partially implemented but removed due to technical incompatibility with the nested-table structure. All backend endpoints and frontend code have been deleted.

### Options to Achieve Goal

1. **Re-implement with constraint:** Require collapse before drag (simpler, avoids containment issue)
2. **Refactor tree structure:** Use flat list with CSS indentation (major change, eliminates constraint)
3. **Partial implementation:** Support Deliverable drag only (leaf nodes avoid containment)
4. **Accept as deferred:** Leave removed; focus on other priorities
5. **Custom solution:** Replace SortableJS with custom drag handler (time-intensive)

---

_Verified: 2026-02-24_
_Verifier: Claude (gsd-verifier)_
