---
phase: 403-organizationcontroller-cascade-guard-userunits-aware
reviewed: 2026-06-19T00:00:00Z
depth: standard
files_reviewed: 4
files_reviewed_list:
  - Controllers/OrganizationController.cs
  - HcPortal.Tests/OrganizationControllerTests.cs
  - Views/Admin/ManageOrganization.cshtml
  - wwwroot/js/orgTree.js
findings:
  critical: 0
  warning: 1
  info: 3
  total: 4
status: issues_found
---

# Phase 403: Code Review Report

**Reviewed:** 2026-06-19
**Depth:** standard
**Files Reviewed:** 4
**Status:** issues_found

## Summary

Phase 403 makes `OrganizationController` aware of the `UserUnits` junction table (introduced in Phase 399) for cascade-rename, deactivate/delete guards, and reparent split-detection, plus a preview count + a cascade-confirm modal row.

The implementation is solid and the key concerns from the review brief all check out:

- **Cascade rename parity (PARITY RULE):** Rename of a Level>=1 unit updates `UserUnits.Unit` for ALL matching rows with NO `IsActive` filter (`OrganizationController.cs:230`), and `PreviewEditCascade` counts ALL rows with NO `IsActive` filter (`:369`). The two predicates match exactly, so preview == actual. The `IsPrimary` flag is untouched, keeping the mirror to `Users.Unit` consistent (Invariant #3). Test `PreviewEditCascade_RenameLevel1_UserUnitsCountMatchesActual` proves parity and the discriminating `IsActive=false` row.
- **Guards filter IsActive (correct):** Deactivate guard (`:441`) and Delete guard (`:501`) both scan `UserUnits ... && uu.IsActive`. Split-detection (`:264-273`) filters `IsActive` on both the member-set query and the other-rows query. Correct asymmetry vs. rename.
- **Reparent split-detection logic:** Correct. Member set derived from active rows in the moving unit; `unitToSection` built from `GetSectionUnitsDictAsync()` (active Level-0/Level-1 only, which is exactly where UserUnit names live); the moving unit itself is excluded via `uu.Unit != oldName`; a worker is flagged only when another active membership resolves to a Section != the new target Section. Verified against tests `EditOrganizationUnit_ReparentSplitsWorker_Blocked` and `..._ReparentSingleUnitWorker_Allowed`.
- **Transaction wrapping:** `BeginTransactionAsync` at `:182`, `CommitAsync` at `:308`. All return-early paths after `:182` (split-block at `:283-284`) dispose the `using` tx without commit = rollback. Atomicity preserved.
- **Authz/CSRF:** Unchanged. All POST actions retain `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]`. JS `ajaxPost` still appends `__RequestVerificationToken` + `X-Requested-With`.
- **XSS:** Modal uses `.textContent` for the new `cascadeUserUnits` row (`orgTree.js:365`); counts are integers. No injection surface.

Build: 0 errors (28 pre-existing warnings, none from Phase 403). One Warning and three Info items below; none are blocking.

## Warnings

### WR-01: EditOrganizationUnit cascade is not audited (asymmetric with Delete)

**File:** `Controllers/OrganizationController.cs:306-308`
**Issue:** `DeleteOrganizationUnit` writes an `AuditLogService` entry (`:545`), but `EditOrganizationUnit` — which now performs a far larger denormalized cascade (Users.Section/Unit, CoachCoacheeMappings, ProtonKompetensiList, CoachingGuidanceFiles, and now ALL UserUnits rows including inactive ones) plus reparent and a hard-block decision — writes no audit entry. A rename that silently rewrites the `Unit` field on inactive `UserUnits` rows (intentional per D-04) is exactly the kind of bulk, hard-to-reverse mutation that benefits from an audit trail. This is a pre-existing gap that Phase 403 materially widens (cascade now touches the junction table). Not a correctness bug, but a traceability/operability concern for an admin-only org-structure mutation.
**Fix:** Mirror the Delete pattern after `tx.CommitAsync()`:
```csharp
try
{
    var currentUser = await _userManager.GetUserAsync(User);
    var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
        ? (currentUser?.FullName ?? "Unknown")
        : $"{currentUser.NIP} - {currentUser.FullName}";
    await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "EditOrganizationUnit",
        $"Renamed/reparented unit '{oldName}' -> '{unit.Name}' [ID={unit.Id}] " +
        $"(cascaded {cascadedUsers} users, {cascadedMappings} mappings, {cascadedUserUnits} userUnits)",
        unit.Id, "OrganizationUnit");
}
catch { /* audit failure must not block response */ }
```
If audit-on-edit is intentionally out of scope for this phase, capture it as backlog rather than leaving the asymmetry silent.

## Info

### IN-01: Split-detection silently skips memberships not present in the active Section dictionary

**File:** `Controllers/OrganizationController.cs:271-272`
**Issue:** `otherRows.Where(uu => unitToSection.TryGetValue(uu.Unit, out var sec) && sec != newSectionName)` drops any active `UserUnits` row whose `Unit` name is NOT a key in `GetSectionUnitsDictAsync()` (which only includes units whose parent Bagian AND the unit itself are `IsActive`). For the current data model this is correct — UserUnit names are always active Level-1 units within a Bagian — so the skip never fires in practice. But the behavior is a silent fail-open: a stale/orphaned membership (e.g. pointing to a now-inactive unit) would NOT count toward the split block. Given this is a guard protecting a data-integrity invariant, the silent drop is worth a defensive comment or an explicit decision.
**Fix:** Add a short comment documenting the assumption (membership names are always resolvable active units), e.g. `// memberships always map to active units; unresolved names = stale data, treated as no-split (acceptable, see D-01a)`. Optional: log when `TryGetValue` fails to surface stale data.

### IN-02: Reparent count branch in PreviewEditCascade only fires when name is unchanged

**File:** `Controllers/OrganizationController.cs:358`
**Issue:** The reparent-count block is gated on `parentChanged && unit.Level >= 1 && !nameChanged`. When BOTH name and parent change in one Edit, the reparent contribution to `affectedUsers/Mappings/Kompetensi/Guidance` is omitted from the preview, while the rename block (`:338`) already counted the same `oldName` rows. This is deliberate de-dup (the same rows would otherwise be double-counted, since rename and reparent both target `Unit == oldName`), and `affectedUserUnitsCount` is correctly independent. The actual `EditOrganizationUnit` cascade for a simultaneous rename+reparent re-queries by the (already renamed in-memory) entities, so preview-vs-actual parity for the combined case is not covered by a test. Low risk because the UI typically changes one field at a time, but the combined-edit parity is an untested path.
**Fix:** Add a test exercising simultaneous rename + reparent of a Level>=1 unit and assert preview counts == actual cascade counts, to lock the de-dup logic against future drift.

### IN-03: Toast/modal flow does not surface PreviewEditCascade error payload

**File:** `wwwroot/js/orgTree.js:426-433`
**Issue:** When `PreviewEditCascade` returns `{ error: "Unit tidak ditemukan." }` (unit deleted between page load and submit), all `affected*Count` fields are undefined, `total` evaluates to 0, the confirm modal is skipped, and the flow proceeds straight to `EditOrganizationUnit` — which then returns the real error and shows the toast. Functionally correct (the error still surfaces), but the preview's own error message is silently discarded. Minor UX nicety.
**Fix:** Optionally short-circuit on `pv.error` before computing `total`:
```javascript
if (pv.error) { showToast(pv.error, 'danger'); return; }
```

---

_Reviewed: 2026-06-19_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
