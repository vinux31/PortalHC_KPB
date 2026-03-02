---
phase: 83-master-data-qa
plan: "03"
subsystem: ProtonData / Silabus CRUD
tags: [silabus, proton, orphan-cleanup, crud, qa]
dependency_graph:
  requires: [83-01, 83-02]
  provides: [DATA-03]
  affects: [CDP/CoachingProton, CDP/PlanIdp, Phase 85, Phase 86]
tech_stack:
  added: []
  patterns: [nested-upsert, orphan-cleanup-with-set-tracking]
key_files:
  created: []
  modified:
    - Controllers/ProtonDataController.cs
    - Views/ProtonData/Index.cshtml
decisions:
  - "Orphan cleanup uses explicit HashSet tracking (orphanDelivIdSet, orphanSubIdSet) instead of relying on in-memory nav property counts after RemoveRange"
  - "Stale-ID fallback (FindAsync returning null for ID > 0) handled for all three levels: Kompetensi, SubKompetensi, Deliverable"
metrics:
  duration: "~15 min"
  completed: "2026-03-02"
  tasks_completed: 1
  tasks_total: 2
  files_changed: 2
---

# Phase 83 Plan 03: Silabus CRUD QA Summary

**One-liner:** Fixed three silent bugs in SilabusSave orphan cleanup (stale-ID fallback, nav-property staleness after RemoveRange, missing SubKompetensi validation).

## Bugs Found and Fixed

### 1. [Rule 1 - Bug] Orphan cleanup uses stale in-memory nav property after RemoveRange

**Found during:** Task 1 code review
**File:** `Controllers/ProtonDataController.cs` lines 264–279 (original)
**Issue:** After calling `_context.ProtonDeliverableList.RemoveRange(orphanDelivs)`, the EF change tracker marks those entities Deleted but does NOT clear the navigation collection `s.Deliverables`. The next line checked `!s.Deliverables.Any()` to decide whether to also delete the parent SubKompetensi — but this returned false (items still in nav collection), so SubKompetensi and Kompetensi orphans were silently LEFT IN THE DATABASE when all their child deliverables were removed.
**Fix:** Replaced the inline nav-count checks with two `HashSet<int>` sets (`orphanDelivIdSet`, `orphanSubIdSet`). After collecting orphan deliverables, their IDs are recorded in `orphanDelivIdSet`. SubKompetensi orphan logic then checks `s.Deliverables.All(d => orphanDelivIdSet.Contains(d.Id))`. Same pattern for Kompetensi.
**Files modified:** `Controllers/ProtonDataController.cs`
**Commit:** 64ed713

---

### 2. [Rule 1 - Bug] Stale-ID fallback path creates entity but doesn't protect it from immediate orphan deletion

**Found during:** Task 1 code review
**File:** `Controllers/ProtonDataController.cs` lines 156–172 / 190–203 (original)
**Issue:** When `KompetensiId > 0` (or `SubKompetensiId > 0`) but `FindAsync` returns null (stale ID), the code created a new entity and added it to context — but this new entity's freshly-generated ID was not added to `savedKompIds` (or `savedSubIds`). The orphan cleanup ran immediately after and would have deleted these newly-created entities, causing a no-op save.
**Fix:** Added `staleFallbackKompIds` and `staleFallbackSubIds` lists. After `SaveChangesAsync` in the stale-ID path, the new entity's `Id` is compared to the original stale ID; if different, the new ID is appended to the fallback list. These lists are merged into `savedKompIds`/`savedSubIds` before orphan cleanup runs.
**Files modified:** `Controllers/ProtonDataController.cs`
**Commit:** 64ed713

---

### 3. [Rule 1 - Bug] Stale DeliverableId > 0 with missing entity silently dropped

**Found during:** Task 1 code review
**File:** `Controllers/ProtonDataController.cs` line 219–228 (original)
**Issue:** When `DeliverableId > 0` but `FindAsync` returned null (stale deliverable ID), the row was silently skipped (no entity created, no error). Data would be lost on save.
**Fix:** Added an `else` branch in the `DeliverableId > 0` path to create a new entity and track its ID in `newDelivIds`, matching the same pattern as the new-deliverable path.
**Files modified:** `Controllers/ProtonDataController.cs`
**Commit:** 64ed713

---

### 4. [Rule 2 - Missing validation] SubKompetensi not validated client-side before save

**Found during:** Task 1 view review
**File:** `Views/ProtonData/Index.cshtml` `saveAll()` function
**Issue:** Client-side validation checked Kompetensi and Deliverable but not SubKompetensi. An empty SubKompetensi would pass client validation and be sent to the server, creating a SubKompetensi row with an empty name.
**Fix:** Added `if (!r.SubKompetensi || !r.SubKompetensi.trim())` guard in `saveAll()`.
**Files modified:** `Views/ProtonData/Index.cshtml`
**Commit:** 64ed713

---

## Items Confirmed OK (No Fix Needed)

| Check | Finding |
|---|---|
| Orphan cleanup scope (bagian/unit/trackId filter) | Correctly scoped at line 259-262. No global orphan risk. |
| SilabusDelete cascade | Explicitly deletes children (SubKompetensi, Kompetensi) after deliverable removal if they become empty. No cascade config needed. |
| Empty rows guard | Present at line 134-135: `if (rows == null || !rows.Any()) return Json(...)`. |
| CSRF token in all fetch calls | All fetch POSTs read from `input[name="__RequestVerificationToken"]`. Both SilabusSave and SilabusDelete pass it as `RequestVerificationToken` header. Controller uses `[ValidateAntiForgeryToken]`. Correct. |

---

## Cross-Feature Query Pattern (Research)

**How CDPController queries silabus data:**

`CDPController.PlanIdp` (Coaching Proton page for coachees):
```csharp
// Line 66-71 in CDPController.cs
var kompetensiList = await _context.ProtonKompetensiList
    .Include(k => k.SubKompetensiList)
        .ThenInclude(s => s.Deliverables)
    .Where(k => k.ProtonTrackId == assignment.ProtonTrackId)
    .OrderBy(k => k.Urutan)
    .ToListAsync();
```
The coachee's `ProtonTrackAssignment.ProtonTrackId` determines which silabus they see. This means silabus data entered for a specific trackId in ProtonData/Index will appear in the Coaching Proton view for coachees assigned to that track.

**Progress tracking chain:** `ProtonDeliverableProgresses` → includes `ProtonDeliverable` → `ProtonSubKompetensi` → `ProtonKompetensi`. All three levels must exist for progress records to display hierarchy correctly.

**AdminController.cs (line 3800):** Also queries `ProtonKompetensiList` for deliverable ID collection when processing coachee assignments.

---

## Manual Verification Result

**Status: PENDING** — Awaiting checkpoint human-verify (Task 2).

The following flows need browser verification:
1. Filter by Bagian/Unit/Track — editor loads silabus rows
2. Add a Deliverable row — save + reload confirms persistence
3. Edit a row — change persists after reload
4. Delete a row — gone after reload (no ghost record)
5. Cross-track safety — other track rows not affected
6. Cross-feature round-trip — deliverable visible in Coaching Proton

---

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Three orphan cleanup bugs fixed (stale-ID, nav-property staleness, stale deliverable ID)**
- Found during: Task 1
- Files modified: `Controllers/ProtonDataController.cs`
- Commit: 64ed713

**2. [Rule 2 - Missing validation] Added SubKompetensi client-side validation**
- Found during: Task 1
- Files modified: `Views/ProtonData/Index.cshtml`
- Commit: 64ed713

## Self-Check: PASSED
- `Controllers/ProtonDataController.cs` — modified, exists
- `Views/ProtonData/Index.cshtml` — modified, exists
- Commit 64ed713 — exists in git log
