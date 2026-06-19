---
phase: 402-coaching-cross-unit-mapping
reviewed: 2026-06-19T00:00:00Z
depth: standard
files_reviewed: 8
files_reviewed_list:
  - Controllers/CDPController.cs
  - Controllers/CoachMappingController.cs
  - Models/CDPDashboardViewModel.cs
  - Views/Admin/CoachCoacheeMapping.cshtml
  - Views/CDP/Shared/_CoachingProtonPartial.cshtml
  - HcPortal.Tests/CrossUnitAssignTests.cs
  - HcPortal.Tests/CdpCoachUnionScopeTests.cs
  - tests/e2e/coaching-crossunit-402.spec.ts
findings:
  critical: 0
  warning: 4
  info: 10
  total: 14
status: issues_found
---

# Phase 402: Code Review Report

**Reviewed:** 2026-06-19
**Depth:** xhigh (multi-agent `/code-review`, recall mode — 10 finder angles + sweep + self-verify)
**Files Reviewed:** 8
**Status:** issues_found

## Summary

Phase 402 adds cross-unit coaching: a coachee can belong to >1 Unit within 1 Bagian, `coach.Section` is authoritative, `AssignmentUnit` is resolved per-coachee (`req.AssignmentUnits[cid] ?? req.AssignmentUnit`), and a multi-unit coach sees the UNION of mapped coachees by default (foreign unit coerced to null). The assign modal was redesigned coach-first.

**No Critical findings. No data-corruption or security holes** — the server re-validates `coach.Section`, cross-Bagian membership (`CoacheeSectionMatchesCoach`), unit ∈ org-tree, and unit ∈ coachee `UserUnits` on every path; XSS surface is safe (`System.Text.Json` default encoder escapes `'`/`<`/`>`/`&` so `Html.Raw(JsonSerializer.Serialize(cUnits))` inside the single-quoted attribute cannot break out).

Two candidate findings were **refuted** against the DB schema and are NOT listed: (a) "non-deterministic narrow via GroupBy/FirstOrDefault" — refuted because `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` (filtered `[IsActive]=1`, CoacheeId-only) guarantees one active mapping per coachee; (b) "primary-first ordering lacks tiebreaker changes default unit" — refuted because `IX_UserUnits_UserId_PrimaryUnique` guarantees exactly one `IsPrimary=1` row per user, so the default option is always that single primary.

4 Warnings (worth fixing now) and 10 Info items below. None block phase verification.

## Warnings

### WR-01: `applyCoachScope()` never runs on modal open — coach-first UI broken on first render

**File:** `Views/Admin/CoachCoacheeMapping.cshtml:692` (def) / `:1048` (DOMContentLoaded wires only the import input)
**Issue:** `applyCoachScope()` only fires on the coach `<select>` `onchange` (`:418`). On modal open (no coach picked yet) the "Pilih coach dahulu" prompt shows BUT all eligible coachees (every Bagian) are simultaneously visible + checkable, the manual "Filter Seksi Coachee" group is visible, and "Bagian Penugasan" is empty + enabled — contradicting the coach-first design until the admin touches the coach dropdown. No data corruption (submit still requires a coach, and picking a coach unchecks everything), but the initial state is inconsistent and confusing.
**Fix:** Run `applyCoachScope()` once when the modal is shown so the initial state matches the post-selection contract. Add a `show.bs.modal` listener for `#assignModal` (or call it inside the existing `DOMContentLoaded` at `:1048`):
```javascript
document.getElementById('assignModal')?.addEventListener('show.bs.modal', applyCoachScope);
```
This establishes the hidden/locked initial state (prompt shown, items hidden, manual filter hidden, Bagian Penugasan empty) on every open.

### WR-02: No `req.CoacheeIds.Distinct()` — a duplicate coacheeId rolls back the entire batch

**File:** `Controllers/CoachMappingController.cs:682` (newMappings build) / loop at `:574`
**Issue:** A duplicate coacheeId in the payload (crafted/DOM glitch, e.g. `['c1','c1','c2']`) passes validation (the `resolvedUnits` dict tolerates overwrite) but `newMappings = req.CoacheeIds.Select(...)` builds TWO active rows for `c1`. `AddRange` → `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` violation on commit → the catch block rolls back the WHOLE batch, so valid `c2` also fails.
**Fix:** Deduplicate the incoming list once, early in the action (right after the null/empty guard at `:542`), and use the deduped list everywhere downstream:
```csharp
req.CoacheeIds = req.CoacheeIds.Distinct().ToList();
```
(Or build `newMappings` from `resolvedUnits.Keys`, which is already unique.)

### WR-03: Coach dashboard `scopeLabel` still uses scalar `user.Unit` while the default view is now a UNION

**File:** `Controllers/CDPController.cs:503-505`
**Issue:** For a coaching role, the default load (`unit == null`) returns the union of mapped coachees across all the coach's units, but `scopeLabel = $"Unit: {user.Unit}"` still names only the scalar primary unit. The dashboard header claims `Unit: <primary>` while the table lists coachees from the coach's other units too — a misleading scope indicator introduced by the "stop forcing primary" change.
**Fix:** Make the label reflect the union/narrow state. When the coach has >1 active unit and no unit filter is applied, label the Bagian (union) rather than a single unit; when a unit filter is applied, keep the unit label. For example:
```csharp
else // Coach
{
    // ... existing scopedCoacheeIds query ...
    scopeLabel = !string.IsNullOrEmpty(user.Section)
        ? $"Section: {user.Section}"
        : "(unit not set)";
}
```
(If the narrow-by-unit param is available here, prefer `Unit: {unit}` when it is set and the Section/union label when it is not.)

### WR-04: Zero-unit coachee is checkable but submit dead-ends with a misleading "multi-unit" alert

**File:** `Views/Admin/CoachCoacheeMapping.cshtml:623` (alert) / `:458` (no `<select>` rendered when `cUnits.Count <= 1`)
**Issue:** A coachee in the coach's Bagian whose `Users.Unit` is NULL has no `UserUnits` row (the Phase 399 backfill only inserts for non-empty Unit), so `cUnits=[]` → no per-row `<select>` and the row is still visible + checkable. On submit, `units.length === 1` is false → `assignmentUnits[id]` stays undefined → alert *"Unit penugasan wajib dipilih untuk setiap coachee multi-unit."* — but there is no selector to pick from and the wording wrongly says "multi-unit". The batch can never be submitted while that coachee is checked (server also rejects with "Unit penugasan wajib dipilih"). UX dead-end + wrong message.
**Fix:** Render zero-unit coachees as disabled with a hint so they cannot enter this dead-end, and correct the alert wording. In the view, when `cUnits.Count == 0`, add `disabled` to the checkbox and a muted note (e.g. "tidak punya unit aktif — tambahkan unit dulu"). In `submitAssign`, change the alert to a unit-agnostic message, e.g. *"Pilih unit penugasan untuk setiap coachee."*

## Info

### IN-01: Coach with NULL `User.Unit` (no `UserUnits` backfill) gets an enabled-but-empty unit dropdown
**File:** `Controllers/CDPController.cs:684-690`
**Issue:** For coaching roles `availableUnits` is overwritten with the coach's own `UserUnits` and `unitFilterEnabled=true`. A coach whose `User.Unit` was NULL has no `UserUnits` row → empty list → the partial renders an ENABLED dropdown with only "Semua Unit", so the coach cannot narrow at all. Data still unions (no leak); pre-402 they saw all section units. Narrow edge (unit-less coach).
**Fix:** When the coach's active-units list is empty, fall back to `GetUnitsForSectionAsync(user.Section)` (or leave `unitFilterEnabled=false`) so the control is not a dead enabled dropdown.

### IN-02: Per-row unit options are not intersected with the coach's Bagian org-tree → whole-batch server reject with no per-row indicator
**File:** `Views/Admin/CoachCoacheeMapping.cshtml:463` (options from `coachee.UserUnits`) / `Controllers/CoachMappingController.cs:118-124` (dict build)
**Issue:** Per-row options come straight from `coachee.UserUnits` with no intersection against `validUnits = org-tree(coach.Section)`. A coachee with a stale cross-Bagian `UserUnits` row offers that unit; selecting it fails the WHOLE batch server-side ("bukan anak Bagian coach") with no indication which row was the offender. Requires stale/cross-Bagian membership data.
**Fix:** Intersect the rendered options against the coach's section units (client-side in `applyCoachScope`/`submitAssign` using `sectionUnitsMap[coachSection]`, or controller-side against `GetSectionUnitsDictAsync()`), and/or report the offending coachee name (not just the unit) in the server message.

### IN-03: Unique-index violation message says "untuk unit ini" but the index is per-coachee (one active mapping total)
**File:** `Controllers/CoachMappingController.cs:764-765`
**Issue:** `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` is keyed on CoacheeId only. The message "Coachee sudah memiliki coach aktif untuk unit ini" implies a unit-scoped conflict, misleading the operator (in the cross-unit context) into retrying under a different unit, which keeps failing. Pre-existing wording widened by the cross-unit feature.
**Fix:** Drop "untuk unit ini": *"Coachee sudah memiliki coach aktif. Nonaktifkan mapping lama terlebih dahulu."*

### IN-04: `req.AssignmentUnits[cid] ?? req.AssignmentUnit` is a latent cross-wiring path
**File:** `Controllers/CoachMappingController.cs:576`
**Issue:** If any caller sends both a per-coachee map (partial) and a non-null single `req.AssignmentUnit`, a coachee omitted from the map falls back to the single value and may be assigned to a unit the operator never picked for it (as long as the coachee owns it). The current UI sends `AssignmentUnit=undefined`, so it is masked today, but the back-compat fallback is unsafe for other callers.
**Fix:** Either drop the single-field fallback now that the live client no longer sends it, or document it as back-compat-only and reject requests that set both.

### IN-05: `CoacheeSectionMatchesCoach` mislabels a missing/forged coacheeId as "cross-Bagian ditolak"
**File:** `Controllers/CoachMappingController.cs:98-104` (helper) / `:562` (call)
**Issue:** A non-existent/deactivated coacheeId → `FirstOrDefaultAsync` null → empty section → `false` → counted in `crossBagian` and surfaced as "bukan anggota Bagian coach (cross-Bagian ditolak)". There is also no eligible-set membership check on the posted IDs. Stale-modal scenario yields a misleading message.
**Fix:** Distinguish "coachee not found / not in eligible set" (refresh-needed) from genuine cross-Bagian in the validation, and surface the appropriate message.

### IN-06: New tests assert on inline reimplementations, not production seams; CXU-05 + CXU-01 effectively unverified
**File:** `HcPortal.Tests/CdpCoachUnionScopeTests.cs:247` / `HcPortal.Tests/CrossUnitAssignTests.cs:377` / `tests/e2e/coaching-crossunit-402.spec.ts:841`
**Issue:** All three `CdpCoachUnionScopeTests` cases query `CoachCoacheeMappings` inline and assert on their own LINQ/ternary (the coercion test even hardcodes `contains ? unit : null`), never calling `BuildProtonProgressSubModelAsync`/`FilterCoachingProton`. The CXU-01 `Eligible_set_aware` test queries `Users` directly instead of the controller's eligible-set build. The e2e CXU-05 is `test.skip(true)`. If the production coercion at `CDPController:310-321` had a bug (missing `Trim`, `==` vs `OrdinalIgnoreCase`), every test still passes — false-confidence green. (The CXU-02/CXU-03 seam tests DO call production `CoacheeSectionMatchesCoach` / `ValidateAssignmentUnitInUserUnits` — those are fine.)
**Fix:** Add at least one test that invokes the real coercion/scope path (or a thin extracted helper — see IN-07), and either un-skip the CXU-05 e2e with a multi-unit fixture or track it explicitly as deferred to Phase 404 QA.

### IN-07: Coach unit-scope block duplicated verbatim; coach-`UserUnits` query written 3×
**File:** `Controllers/CDPController.cs:305-318` and `:339-352` (identical block incl. comments); `:311`, `:345`, `:686` (UserUnits query, two with `.Distinct()`, one without)
**Issue:** The security-relevant "foreign unit → null = union" coercion exists as two byte-identical copies (FilterCoachingProton + ExportDashboardProgress). The next edit will fix one and miss the other, silently diverging export scope from dashboard scope. The third UserUnits query adds a `.Distinct()` the other two lack — a latent inconsistency.
**Fix:** Extract one private helper (e.g. `ResolveCoachUnitScopeAsync(user, roleLevel, section, unit)` returning `(section, unit)`) and `GetActiveUnitsAsync(userId)`, and call from all sites.

### IN-08: Assign issues ~2N sequential awaited DB round-trips before the transaction opens
**File:** `Controllers/CoachMappingController.cs:560-563` (cross-Bagian loop) + `:573-584` (per-coachee unit loop)
**Issue:** `CoacheeSectionMatchesCoach` (1 query/coachee) then `ValidateAssignmentUnitInUserUnits` (1 query/coachee), all sequential, plus `GetSectionUnitsDictAsync`. Whole-Bagian set-aware selection makes large batches easy (e.g. 40 coachees → ~80+ round-trips) before the tx; on the Dev SQL box this is a multi-second hang and `submitAssign` has no spinner/disable, inviting double-submit.
**Fix:** Batch the section/unit checks into 1–2 `IN` queries (load all coachee sections and active UserUnits for `req.CoacheeIds` once, validate in memory), and disable the submit button while the request is in flight.

### IN-09: `availableUnits` computed via `GetUnitsForSectionAsync` then immediately overwritten for coaching roles
**File:** `Controllers/CDPController.cs:677-679` then `:686-688`
**Issue:** For coaching roles the section-units query result is discarded and replaced by the coach's `UserUnits` — a wasted DB round-trip on every coaching dashboard load, and a misleading read.
**Fix:** Guard `GetUnitsForSectionAsync` behind `!IsCoachingRole(roleLevel)`, or compute `availableUnits` in a single if/else.

### IN-10: Dead state — `lockedUnit` always null; `ConfirmProgressionWarning` write-never-read
**File:** `Controllers/CDPController.cs:671,713` (lockedUnit) / `Controllers/CoachMappingController.cs:1922` (DTO `ConfirmProgressionWarning`)
**Issue:** `lockedUnit` is declared, never assigned (the coaching branch dropped it in 402, HasSectionAccess never set it), then `LockedUnit = lockedUnit`. `ConfirmProgressionWarning` has no client writer since the assign warning re-POST flow was deleted (assign is hard-block). Both are vestigial.
**Fix:** Assign `LockedUnit = null` directly and drop the dead local; remove `ConfirmProgressionWarning` from the assign DTO (or comment it as deliberately retained). Note the view still reads `Model.LockedUnit ?? Model.FilterUnit`, so keep the model property.

---

_Reviewed: 2026-06-19_
_Reviewer: Claude (`/code-review` xhigh, transcribed to gsd format for fix dispatch)_
_Depth: xhigh (multi-agent)_
