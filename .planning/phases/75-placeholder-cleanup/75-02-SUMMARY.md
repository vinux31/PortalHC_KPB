---
phase: 75-placeholder-cleanup
plan: "02"
subsystem: Views
tags: [cleanup, stub-removal, admin-hub, settings]
dependency_graph:
  requires: []
  provides: [STUB-02, STUB-03, STUB-04]
  affects: [Views/Admin/Index.cshtml, Views/Account/Settings.cshtml]
tech_stack:
  added: []
  patterns: [razor-view-cleanup]
key_files:
  created: []
  modified:
    - Views/Admin/Index.cshtml
    - Views/Account/Settings.cshtml
decisions:
  - Deleted stub cards entirely rather than hiding them — dead href="#" cards with "Segera" badges provide no value and mislead users about available features
  - Removed entire Pengaturan Lainnya section including its preceding hr separator — no functional content remained in Section 3
metrics:
  duration: "2 minutes"
  completed: "2026-03-01"
  tasks_completed: 2
  files_modified: 2
---

# Phase 75 Plan 02: Placeholder Cleanup (Views) Summary

Removed two inert stub cards from Admin hub (Coaching Session Override, Final Assessment Manager) and deleted the three disabled "Segera Hadir" rows from Settings page, leaving only functional UI in both views.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Remove two stub cards from Admin hub | 3e6bbdb | Views/Admin/Index.cshtml |
| 2 | Remove disabled placeholder items from Settings page | f063a72 | Views/Account/Settings.cshtml |

## What Was Done

**Task 1 — Admin hub (Views/Admin/Index.cshtml):**
- Deleted "Coaching Session Override" col-md-4 card block from Section B (href="#", opacity-75, "Segera" badge, bi-chat-text icon)
- Deleted "Final Assessment Manager" col-md-4 card block from Section C (href="#", opacity-75, "Segera" badge, bi-clipboard-check icon)
- Section A: 4 cards (unchanged)
- Section B: now 2 cards — Coach-Coachee Mapping, Deliverable Progress Override
- Section C: now 1 card — Manage Assessments

**Task 2 — Settings page (Views/Account/Settings.cshtml):**
- Deleted `<hr class="my-4">` separator preceding Section 3
- Deleted Section 3 header: "Pengaturan Lainnya"
- Deleted Two-Factor Authentication row (disabled toggle, Segera Hadir badge)
- Deleted Notifikasi Email row (disabled toggle, Segera Hadir badge)
- Deleted Bahasa row (disabled select, Segera Hadir badge)
- Section 1 (Edit Profil) and Section 2 (Ubah Password) remain completely untouched

## Verification Results

- `grep "Coaching Session Override" Views/Admin/Index.cshtml` — no output (pass)
- `grep "Final Assessment Manager" Views/Admin/Index.cshtml` — no output (pass)
- `grep "Segera" Views/Admin/Index.cshtml` — no output (pass)
- `grep "Two-Factor|Notifikasi|Bahasa|Segera Hadir" Views/Account/Settings.cshtml` — no output (pass)
- Functional cards confirmed present: Coach-Coachee Mapping, Deliverable Progress Override, Manage Assessments
- Functional forms confirmed present: EditProfile, ChangePassword
- `dotnet build` — 0 errors (pass)

## Requirements Satisfied

- STUB-02: "Coaching Session Override" stub card removed from Admin hub Section B
- STUB-03: "Final Assessment Manager" stub card removed from Admin hub Section C
- STUB-04: Disabled 2FA, Notifikasi Email, and Bahasa items removed from Settings page

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- Views/Admin/Index.cshtml: modified (28 lines deleted, 2 stub card blocks removed)
- Views/Account/Settings.cshtml: modified (41 lines deleted, Section 3 removed)
- Commit 3e6bbdb: confirmed present (Task 1)
- Commit f063a72: confirmed present (Task 2)
- Build: 0 errors confirmed
