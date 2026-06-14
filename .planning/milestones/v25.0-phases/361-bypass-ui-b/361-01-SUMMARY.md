---
phase: 361-bypass-ui-b
plan: 01
status: complete
completed: 2026-06-11
commits:
  - 92da2cc8: "feat(361-01): tambah ViewBag.AllCoaches di Override() untuk dropdown coach wizard [D-12]"
  - c4d0abe0: "feat(361-01): extend BypassPendingList select - skor/tanggal/reason/coach [D-18]"
key-files:
  created: []
  modified:
    - Controllers/ProtonDataController.cs
---

# Plan 361-01 Summary — Backend prep Tab2 Bypass

**One-liner:** Override() GET sediakan ViewBag.AllCoaches + BypassPendingList select extended (skor/tanggal/reason/coach) — query-only, tanpa migration.

## What was built

1. **Task 1 (`92da2cc8`):** `Override()` GET (`ProtonDataController.cs:242-248`) — blok `ViewBag.AllCoaches` via `GetUsersInRoleAsync(UserRoles.Coach)` filter `IsActive` order `FullName`, pola verbatim `CoachMappingController.cs:146-149`. ViewBag existing (`AllTracks`, `SectionUnitsJson`) utuh.
2. **Task 2 (`c4d0abe0`):** `BypassPendingList()` (`:1542+`) — LEFT JOIN `Users` on `TargetCoachId` (`DefaultIfEmpty()`) + 5 field baru di projection setelah `createdAt`: `skorExam` (s.Score), `tanggalExam` (s.CompletedAt), `reason` (p.Reason), `targetCoachId`, `targetCoachNama` (c.FullName ?? c.UserName). 9 field existing byte-for-byte utuh.

## Verification

- `dotnet build` Build succeeded, 0 Error — dua kali (per task).
- Grep acceptance: semua hit sesuai; `s.Skor`/`s.Completed` (nama salah) = 0 hit.
- Tidak ada file migration baru.

## Deviations

None — plan diikuti persis.

## Self-Check: PASSED
