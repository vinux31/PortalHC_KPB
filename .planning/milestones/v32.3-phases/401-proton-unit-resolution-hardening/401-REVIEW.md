---
phase: 401-proton-unit-resolution-hardening
reviewed: 2026-06-19T00:00:00Z
depth: standard
files_reviewed: 12
files_reviewed_list:
  - Controllers/AssessmentAdminController.cs
  - Controllers/CDPController.cs
  - Controllers/CoachMappingController.cs
  - Controllers/ProtonDataController.cs
  - HcPortal.Tests/AssignmentUnitInUserUnitsTests.cs
  - HcPortal.Tests/CertGateAuditTests.cs
  - HcPortal.Tests/CleanupNoClobberTests.cs
  - HcPortal.Tests/FilterAxisTests.cs
  - HcPortal.Tests/ProtonUnitResolveTests.cs
  - HcPortal.Tests/ReactivateUnitValidationTests.cs
  - HcPortal.Tests/UnitUnresolvedAuditTests.cs
  - Views/Admin/CoachCoacheeMapping.cshtml
findings:
  critical: 0
  warning: 3
  info: 4
  total: 7
status: issues_found
---

# Phase 401: Code Review Report

**Reviewed:** 2026-06-19T00:00:00Z
**Depth:** standard
**Files Reviewed:** 12
**Status:** issues_found

## Summary

Phase 401 drops the legacy primary-Unit fallback and makes `AssignmentUnit` resolution authoritative against the `UserUnits` junction. The core design is sound and the implementation is consistent with the stated invariants:

- The shared helper `ValidateAssignmentUnitInUserUnits` is a single, well-documented, testable static seam that reads only from the junction (`UserUnits`, active rows), uses `Trim()` + `OrdinalIgnoreCase`, and correctly returns `false` for null/whitespace (never silently resolving from primary). All eight call sites (Assign, Edit, Import, Cleanup, Reactivate in `CoachMappingController`; Bypass `TargetUnit` in `ProtonDataController`; cert-gate in `AssessmentAdminController`) route through it.
- The no-clobber preserve-gate in `CleanupCoachCoacheeMappingOrg` is correctly ordered: it preserves a valid non-primary `AssignmentUnit` before the last-resort "reset to user record" path.
- All POST endpoints retain `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]`, and server-authoritative validation runs **before** `BeginTransactionAsync` in every mutation path (Assign, Edit, Reactivate, Bypass). Client-supplied units are validated against both the org-tree (`GetSectionUnitsDictAsync`) and the junction.
- The AF-4 ±5s correlation window (`EF.Functions.DateDiffSecond`) in `CoachCoacheeMappingReactivate` is preserved untouched per D-05.
- The D-01 orphan-unit indicator is computed on-demand in the GET and rendered via auto-encoded Razor (`@orphanCount` — no `Html.Raw`/XSS surface).

No Critical issues. The findings below are correctness/consistency concerns (Warnings) and minor observations (Info). The deliberate helper-level test scoping with explicit `Skip` reasons deferring deep SQL-real assertions to Phase 404 is appropriate and is **not** flagged as a gap.

## Warnings

### WR-01: Filter-axis unit comparison is case/whitespace-sensitive, diverging from the helper's normalization

**File:** `Controllers/CDPController.cs:1599`, `Controllers/CDPController.cs:1611`, `Controllers/CDPController.cs:4264`
**Issue:** Three PSU-02 filter-axis queries compare the requested `unit` against the stored `AssignmentUnit` using a raw EF equality (`m.AssignmentUnit == unit`), which translates to a DB-collation comparison with **no `Trim()` and no `OrdinalIgnoreCase`**. The single-source helper (`CoachMappingController.cs:60-61`) and the sibling resolver at `CDPController.cs:498-502` both normalize with `.Trim()` + `StringComparison.OrdinalIgnoreCase`. If a stored `AssignmentUnit` carries leading/trailing whitespace (the very scenario the helper guards against) or differs only by case (under a case-sensitive column collation), a coachee whose mapping is otherwise valid will silently disappear from the unit-filtered PROTON surface — a false-negative that the orphan indicator will *not* flag (the helper would consider that unit valid). This is the same data-loss/visibility axis Phase 401 is hardening.
**Fix:** Normalize both sides consistently. Since these are server-supplied `unit` values matched against trimmed junction units, either pre-trim the parameter and rely on a CI column collation, or resolve via the same normalized dictionary used at `CDPController.cs:494-502`. Example for the level≤3 branch:
```csharp
var u = unit.Trim();
scopedCoacheeIds = await _context.Users
    .Where(x => scopedCoacheeIds.Contains(x.Id) && x.IsActive
             && _context.CoachCoacheeMappings.Any(m => m.IsActive && m.CoacheeId == x.Id
                 && m.AssignmentUnit != null && m.AssignmentUnit.Trim() == u))
    .Select(x => x.Id).ToListAsync();
```
(Confirm the chosen approach matches the column collation; if the DB is already CI + the data is guaranteed trimmed on write, downgrade this to Info. Given the read paths elsewhere defensively trim, the write side is evidently not fully trusted.)

### WR-02: `CoachCoacheeMappingEdit` skips junction validation when the submitted unit is empty, allowing an active mapping to be edited into a blank `AssignmentUnit`

**File:** `Controllers/CoachMappingController.cs:790-792` (guard) / `Controllers/CoachMappingController.cs:812-813` (write)
**Issue:** The PSU-03 junction guard in Edit is gated on `!string.IsNullOrEmpty(unitEdit)`. When the client submits an empty/blank `AssignmentUnit`, the guard is skipped entirely, and the write at line 813 sets `mapping.AssignmentUnit = req.AssignmentUnit?.Trim()` (i.e. `null`/empty). The result is an **active** mapping with a blank `AssignmentUnit` — exactly the orphan state Invariant #4 and the D-01 indicator are meant to prevent. Unlike Assign (which hard-requires a non-empty unit at `CoachMappingController.cs:522-523`), Edit silently permits demoting a valid mapping to unresolvable. The mapping then vanishes from all PROTON read paths (resolvers return/skip on empty) until an admin notices the orphan banner.
**Fix:** Treat empty `AssignmentUnit` on Edit the same as Assign — reject it for active mappings, or at minimum block the empty-unit write. For example, before the transaction:
```csharp
if (mapping.IsActive && string.IsNullOrWhiteSpace(unitEdit))
    return Json(new { success = false, message = "Unit penugasan wajib diisi untuk mapping aktif." });
```
If preserving the existing valid unit on a blank submit is the intended UX, explicitly preserve `mapping.AssignmentUnit` instead of overwriting it with the blank value.

### WR-03: Cleanup preserve-branch can leave `AssignmentSection` invalid when the coachee user record is missing or has a blank Section

**File:** `Controllers/CoachMappingController.cs:955-964`
**Issue:** The PSU-04 preserve branch fires when `AssignmentUnit` is valid (∈ active UserUnits) but the `(Section, Unit)` pair failed the org-tree check at line 950-951 (commonly because `AssignmentSection` is stale/blank). Inside the branch, `AssignmentSection` is repaired **only** if `userDict.TryGetValue(...)` succeeds *and* the user's `Section` is non-empty (line 960). If the coachee is absent from `userDict` (e.g. user row filtered out) or their `Section` is blank, the branch still `continue`s and increments `autoFixed++` (line 962) while leaving the mapping's `AssignmentSection` in its original invalid state. The operator's cleanup report then claims this mapping was "auto-fixed" when its `(Section, Unit)` pair is still org-tree-invalid — a misleading success count, and the mapping remains a candidate the next cleanup run will re-process.
**Fix:** Only count as `autoFixed` when the section was actually corrected (or independently confirmed valid). For example:
```csharp
if (userDict.TryGetValue(m.CoacheeId, out var ciPreserve)
    && !string.IsNullOrEmpty(ciPreserve.Section?.Trim())
    && sectionUnitsDict.TryGetValue(ciPreserve.Section!.Trim(), out var vuPreserve)
    && vuPreserve.Contains(m.AssignmentUnit!.Trim()))
{
    m.AssignmentSection = ciPreserve.Section!.Trim();
    autoFixed++;
    continue;
}
// else: unit preserved but section unresolved → record as unfixable, not autoFixed
unfixable.Add(new { m.Id, m.CoacheeId, m.AssignmentSection, m.AssignmentUnit });
continue;
```

## Info

### IN-01: Import row-validation re-checks the primary unit but never validates non-primary `AssignmentUnit` for new mappings

**File:** `Controllers/CoachMappingController.cs:373-380` / `Controllers/CoachMappingController.cs:418-427`
**Issue:** On Import, new mappings always take `AssignmentUnit = coacheeUser.Unit` (the primary). The PSU-03 check at line 374 validates that primary unit against the junction — correct, but it is effectively a tautology (Invariant #3 guarantees the primary is in `UserUnits`), as the comment itself notes ("primary baru selalu sah ... safety net"). This is fine and defensive; the note is only that Import cannot create a non-primary `AssignmentUnit` mapping (by design — the template has only NIPs). No action needed unless multi-unit import is later added; if so, this path will need a real per-row unit column + validation.
**Fix:** None required. Documented for future multi-unit import work.

### IN-02: Cert-gate and `GetEligibleCoachees` resolve `AssignmentUnit` via `FirstOrDefaultAsync()` on active mappings without an explicit ordering

**File:** `Controllers/AssessmentAdminController.cs:1414-1415`, `Controllers/CoachMappingController.cs:1495-1498`, `Controllers/CDPController.cs:498`
**Issue:** Unit resolution picks the first active mapping's `AssignmentUnit` with no `OrderBy`. The unique index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` enforces at most one active mapping per coachee, so in practice there is exactly one row and `FirstOrDefault` is deterministic. The non-determinism only matters if that invariant is ever violated (e.g. index dropped or multi-unit-active introduced in a later phase). Acceptable for v32.3 single-active design.
**Fix:** None required now. If multi-active mappings become possible (a stated future direction), add an explicit ordering or aggregate across active units.

### IN-03: Filter-axis `EXISTS` subqueries omit `AssignmentUnit != null` guard

**File:** `Controllers/CDPController.cs:1599`, `Controllers/CDPController.cs:1611`, `Controllers/CDPController.cs:4264`
**Issue:** `m.AssignmentUnit == unit` against a nullable column is safe in SQL (NULL never equals a non-null `unit`), so null mappings are correctly excluded. This is only a readability note: the sibling resolver paths make the null handling explicit. Adding an explicit `m.AssignmentUnit != null` clarifies intent and pairs naturally with the WR-01 trim fix.
**Fix:** Optional — fold into the WR-01 normalization fix.

### IN-04: Test files are clearly scoped to helper/decision-primitive level with documented Phase 404 deferrals

**File:** `HcPortal.Tests/ProtonUnitResolveTests.cs:70-74`, `HcPortal.Tests/UnitUnresolvedAuditTests.cs:73-77`
**Issue:** Deep HTTP/SQL-real assertions are `Skip`-ped with explicit reasons pointing to Phase 404 QA-01. Per the review brief these deferrals are intentional and are **not** treated as gaps. The helper-level tests (membership, trim/case, active-only, empty→false, batch reject, preserve precondition, reactivation guard, audit channel separation) provide good coverage of the decision primitives. One minor observation: `FilterAxisTests` and `CleanupNoClobberTests` assert the helper/dictionary primitive rather than the actual controller query at `CDPController.cs:1599` etc. — so the WR-01 case-sensitivity divergence would not be caught by the current suite. Worth a targeted assertion when the Phase 404 integration tests land.
**Fix:** None required for Phase 401. Consider adding a trimmed/case-variant filter-axis assertion alongside the Phase 404 integration smoke.

---

_Reviewed: 2026-06-19T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
