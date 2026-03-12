# 156-01 Audit Report: PlanIdp and GuidanceDownload (CDP-01, CDP-02, CDP-03)

**Date:** 2026-03-12
**Auditor:** Claude (automated code review)
**Files reviewed:** `Controllers/CDPController.cs` (lines 55–235), `Views/CDP/PlanIdp.cshtml`

---

## Summary

| Requirement | Description | Result | Findings |
|---|---|---|---|
| CDP-01 | Coachee silabus view — lock-in and edge cases | PASS (after fix) | 1 bug fixed, 1 edge-case noted |
| CDP-02 | HC/Admin browse any section/unit/track | PASS | 0 findings |
| CDP-03 | Coaching guidance file download | PASS | 0 findings |

**Total findings:** 2 (1 bug fixed, 1 edge-case informational)

---

## CDP-01: Coachee Silabus View

### Finding 1 — BUG (FIXED): Coachee URL Parameter Manipulation for unit and trackId

**Severity:** Bug / Security
**File:Line:** `Controllers/CDPController.cs:82-83`

**Description:**
The coachee branch used null-coalescing assignment (`??=`) for `unit` and `trackId`, meaning if a coachee passed URL params `?unit=OtherUnit&trackId=999`, those values would NOT be overridden — only set if not already provided. Only `bagian` was properly force-overridden (using `=`). A coachee could view silabus for a different unit or arbitrary track by crafting a URL.

**Before:**
```csharp
bagian = coacheeBagian;
unit ??= firstKomp.Unit;
trackId ??= assignment.ProtonTrackId;
```

**After (fixed):**
```csharp
bagian = coacheeBagian;
unit = firstKomp.Unit;
trackId = assignment.ProtonTrackId;
```

**Impact:** Coachee could browse silabus outside their assigned track by URL manipulation.
**Fix:** Changed `??=` to `=` for both `unit` and `trackId`, forcing assignment to the coachee's actual values regardless of URL params.
**Commit:** Applied in task 1.

---

### Finding 2 — Edge-Case (Informational): Coachee With Assignment But No Active Kompetensi

**Severity:** Edge-case (informational — not a crash)
**File:Line:** `Controllers/CDPController.cs:72-86`

**Description:**
If a coachee has an active `ProtonTrackAssignment` but the assigned track has zero active `ProtonKompetensiList` records (e.g., all deactivated), `firstKomp` is null. The code sets `ViewBag.HasAssignment = true` but leaves `bagian`/`unit`/`trackId` unset. The result: the coachee sees the full filter form (not the "no assignment" message) but with empty silabus rows after submitting. This is not a crash but slightly misleading UX.

**Action:** No code change — the empty silabus alert in the view ("Tidak ada data silabus untuk filter ini") handles this gracefully. Tracked as informational.

---

### CDP-01 Checklist

| Check | Result |
|---|---|
| Server-side bagian override for coachee | PASS (line 81) |
| Server-side unit override for coachee | PASS (fixed — was bug) |
| Server-side trackId override for coachee | PASS (fixed — was bug) |
| Coachee with no assignment: graceful message | PASS (ViewBag.HasAssignment=false + view alert at line 28-35) |
| Coachee with assignment: silabus matches assigned track (IsActive filter) | PASS (line 119: k.IsActive filter applied) |
| No crash on null navigation properties | PASS (null checks at lines 72-86) |

---

## CDP-02: HC/Admin Browse Silabus

### Analysis

The HC/Admin (and Coach, SrSpv roles) path through `PlanIdp()` skips all coachee lock-in logic. No bagian/unit/trackId overrides are applied for these roles (except L4/SectionHead which locks to their section — correct behavior). HC and Admin can freely pass any bagian/unit/trackId combination.

**Cascade dropdown:** Bagian dropdown populated from `OrganizationStructure.SectionUnits.Keys`. Unit dropdown cascades client-side via JS (line 157-171 of view). Track dropdown from `allTracks` loaded from DB.

**Nonexistent trackId:** If a nonexistent trackId is passed, the `kompetensiList` query returns empty results — `silabusRows` is empty — view shows "Tidak ada data silabus untuk filter ini." No crash.

**Empty unit (no tracks):** If a unit has no matching kompetensi for a given track, same result — graceful empty state.

### CDP-02 Checklist

| Check | Result |
|---|---|
| HC/Admin not locked to any bagian | PASS |
| HC/Admin can browse any combination | PASS |
| L4 (SectionHead) locked to own section | PASS (line 101-105) |
| Nonexistent trackId: no crash | PASS (empty query → empty state) |
| Empty unit: no crash | PASS |
| Cascade dropdown: bagian → unit | PASS (JS + orgStructure data embedded) |

---

## CDP-03: Coaching Guidance Download

### Analysis

`GuidanceDownload(int id)` at lines 211-235:

1. **Record not found:** Returns `NotFound()` (line 214) — correct.
2. **Physical file missing:** `File.Exists(fullPath)` checked (line 221), returns `NotFound()` — correct.
3. **Path traversal prevention:** `Path.GetFullPath(physicalPath)` compared against `_env.WebRootPath` (lines 218-220) — correct. Uses `StringComparison.OrdinalIgnoreCase` — correct for Windows.
4. **Content-type:** Switch expression maps 7 common extensions correctly, with `application/octet-stream` fallback — correct.
5. **Authorization:** `[Authorize]` class-level covers all authenticated users. No per-role scoping for downloads — guidance files are reference material, open to all authenticated users (consistent with context decision: "open access is harmless for reference data").

**Guidance file scoping in PlanIdp:** Coachee sees only their own bagian's guidance files (line 149-150). L4 sees only their section (line 151-152). HC/Admin sees all.

### CDP-03 Checklist

| Check | Result |
|---|---|
| Missing DB record: NotFound (not 500) | PASS |
| Missing physical file: NotFound (not 500) | PASS |
| Path traversal prevention | PASS |
| Content-type mapping | PASS |
| Coachee guidance scoped to own bagian | PASS |
| L4 guidance scoped to own section | PASS |

---

## View Audit (PlanIdp.cshtml)

| Check | Result |
|---|---|
| No CSRF token needed (GET-only form) | N/A — filter form uses GET, no state mutation |
| Null-reference guards on ViewBag | PASS (all `?? false`, `?? ""`, `?? new List<>()` guards present) |
| Role-gated UI: coachee sees locked bagian field | PASS (disabled input + hidden input for coachee) |
| JSON injection (silabusRowsJson/guidanceGroupedJson via Html.Raw) | LOW RISK — data comes from DB strings (NamaKompetensi etc.), not user-controlled free text. No additional escaping needed. |
| XSS in silabus table JS renderer | PASS — `escHtml()` helper used for all user data in table cells |

---

## Fixes Applied

| # | Severity | Finding | File | Fix |
|---|---|---|---|---|
| 1 | Bug | Coachee unit/trackId URL override missing | CDPController.cs:82-83 | Changed `??=` to `=` |

---

## Deferred (No Action Required)

| # | Severity | Finding | Reason |
|---|---|---|---|
| 1 | Edge-case | Coachee with assignment but 0 active kompetensi sees empty form | Graceful empty state shown; not a crash |
