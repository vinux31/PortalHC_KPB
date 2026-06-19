---
phase: 402-coaching-cross-unit-mapping
fixed_at: 2026-06-19T00:00:00Z
review_path: .planning/phases/402-coaching-cross-unit-mapping/402-REVIEW.md
iteration: 1
findings_in_scope: 4
fixed: 4
skipped: 0
status: all_fixed
---

# Phase 402: Code Review Fix Report

**Fixed at:** 2026-06-19
**Source review:** .planning/phases/402-coaching-cross-unit-mapping/402-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 4 (Critical + Warning — WR-01, WR-02, WR-03, WR-04)
- Fixed: 4
- Skipped: 0
- The 10 Info findings (IN-01..IN-10) were OUT of scope and were not touched.

**Build verification:** `dotnet build` → 0 Errors, 28 Warnings (all pre-existing; matches the expected baseline — no regressions introduced).

## Fixed Issues

### WR-01: `applyCoachScope()` never runs on modal open — coach-first UI broken on first render

**Files modified:** `Views/Admin/CoachCoacheeMapping.cshtml`
**Commit:** `1667d7b0` (combined view-side commit, see note below)
**Applied fix:** Added a `show.bs.modal` listener for `#assignModal` inside the existing `DOMContentLoaded` handler, calling `applyCoachScope`. This establishes the coach-first initial state on every modal open (prompt shown, coachee items hidden, manual section filter hidden, Bagian Penugasan empty/locked) so the initial state matches the post-coach-selection contract instead of showing every Bagian's coachees as checkable.

### WR-02: No `req.CoacheeIds.Distinct()` — a duplicate coacheeId rolls back the entire batch

**Files modified:** `Controllers/CoachMappingController.cs`
**Commit:** `c2c2010c`
**Applied fix:** Added `req.CoacheeIds = req.CoacheeIds.Distinct().ToList();` immediately after the null/empty guard at the top of `CoachCoacheeMappingAssign`, before all downstream validation/build steps. A duplicate coacheeId in the payload (DOM glitch / crafted) no longer builds two active rows for the same coachee, so it can no longer trip `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` and roll back the whole batch. The deduped list is the single source used by every downstream loop and `newMappings`.

### WR-03: Coach dashboard `scopeLabel` still uses scalar `user.Unit` while the default view is now a UNION

**Files modified:** `Controllers/CDPController.cs`
**Commit:** `acdf4fa2`
**Applied fix:** Inside `BuildProtonProgressSubModelAsync` (Coach branch), replaced the scalar-`user.Unit` label with one that reflects the actual scope: `Unit: {unit}` only when the narrow `unit` filter param is set (non-blank), otherwise `Section: {user.Section}` to represent the union of coachees across all of the coach's units within the Bagian. The `unit` parameter is in scope at this site (method signature `string? unit = null`), so this is the precise narrow-vs-union indicator the environment note specified. No longer claims a single primary unit while listing cross-unit coachees.
**Note:** Verification was Tier 1 (re-read) + Tier 2 (full `dotnet build` passed, 0 errors). This is a label/display change, not a logic/algorithm change, so standard `fixed` status applies.

### WR-04: Zero-unit coachee is checkable but submit dead-ends with a misleading "multi-unit" alert

**Files modified:** `Views/Admin/CoachCoacheeMapping.cshtml`
**Commit:** `1667d7b0` (combined view-side commit, see note below)
**Applied fix:** Two changes in the assign modal view:
1. When a coachee has `cUnits.Count == 0` (no active `UserUnits` row — `Users.Unit` was NULL), the checkbox now renders with `disabled` and a muted hint *"— tidak punya unit aktif, tambahkan unit dulu"*, so the row can never enter the submit dead-end.
2. The `submitAssign` E-3 alert was changed from the misleading *"Unit penugasan wajib dipilih untuk setiap coachee multi-unit."* to the unit-agnostic *"Pilih unit penugasan untuk setiap coachee."*

## Commit-grouping note

WR-01 and WR-04 both modify the same file (`Views/Admin/CoachCoacheeMapping.cshtml`). Because per-finding commits are atomic at the file level, both view-side fixes landed in a single commit (`1667d7b0`) whose message documents both findings. WR-02 (`c2c2010c`) and WR-03 (`acdf4fa2`) are separate per-file commits. No fixes were lost or skipped; the working tree has no uncommitted source changes.

## Skipped Issues

None — all 4 in-scope findings were fixed.

---

_Fixed: 2026-06-19_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
