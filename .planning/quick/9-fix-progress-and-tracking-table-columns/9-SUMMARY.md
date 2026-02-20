---
phase: quick-9
plan: 01
subsystem: CDP / Progress & Tracking
tags: [view, table, columns, tracking, deliverable]
dependency_graph:
  requires: []
  provides: [Deliverable column in Progress table]
  affects: [Views/CDP/Progress.cshtml, Models/TrackingModels.cs, Controllers/CDPController.cs]
tech_stack:
  added: []
  patterns: [IdpItem -> TrackingItem mapping, Razor table column layout]
key_files:
  created: []
  modified:
    - Models/TrackingModels.cs
    - Controllers/CDPController.cs
    - Views/CDP/Progress.cshtml
decisions:
  - "Deliverable column inserted between Sub Kompetensi and Evidence at 15% width"
  - "Implementasi column removed entirely — Periode field in TrackingItem retained (unused) for future use"
  - "Kompetensi width reduced from 25% to 20% to accommodate new Deliverable column"
metrics:
  duration: "~2 min"
  completed: "2026-02-20"
  tasks: 2
  files_modified: 3
---

# Quick Task 9: Fix Progress & Tracking Table Columns Summary

**One-liner:** Removed Implementasi column, dropped "Nama" prefix from Kompetensi/Sub Kompetensi headers, and added Deliverable column sourced from IdpItem.Deliverable between Sub Kompetensi and Evidence.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Add Deliverable to TrackingItem model and controller mapping | a6446f4 | Models/TrackingModels.cs, Controllers/CDPController.cs |
| 2 | Update Progress.cshtml table — remove Implementasi, rename headers, add Deliverable column | cb87e68 | Views/CDP/Progress.cshtml |

## What Changed

### Task 1 — Model + Controller

**Models/TrackingModels.cs:** Added `public string Deliverable { get; set; } = "";` after `SubKompetensi` property.

**Controllers/CDPController.cs (Progress action ~line 1495):** Added `Deliverable = idp.Deliverable ?? "",` to the IdpItem -> TrackingItem mapping.

### Task 2 — View

**Views/CDP/Progress.cshtml (thead):**
- "Nama Kompetensi CPDP" (25%) -> "Kompetensi" (20%)
- Removed "Implementasi" th entirely
- "Nama Sub Kompetensi" (20%) -> "Sub Kompetensi" (15%)
- Added "Deliverable" th (15%) after Sub Kompetensi, before Evidence

**Views/CDP/Progress.cshtml (tbody):**
- Removed `<td>@item.Periode</td>` (Implementasi data cell)
- Added `<td class="p-3 text-dark small">@item.Deliverable</td>` after Sub Kompetensi cell

## Final Column Order (8 columns)

| # | Header | Width |
|---|--------|-------|
| 1 | Kompetensi | 20% |
| 2 | Sub Kompetensi | 15% |
| 3 | Deliverable | 15% |
| 4 | Evidence | 10% |
| 5 | Sr. Supervisor | 10% |
| 6 | Section Head | 10% |
| 7 | HC | 10% |
| 8 | Action | 5% |

## Verification

- `dotnet build` — 0 C# compilation errors
- No "Implementasi" text in Progress.cshtml table
- No "Nama" prefix in any column header
- `@item.Deliverable` renders in column 3 (after Sub Kompetensi, before Evidence)
- Deliverable data flows: IdpItem.Deliverable -> TrackingItem.Deliverable -> View

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- Models/TrackingModels.cs — FOUND, Deliverable property added
- Controllers/CDPController.cs — FOUND, Deliverable mapped
- Views/CDP/Progress.cshtml — FOUND, 8-column table with correct headers
- Commit a6446f4 — FOUND
- Commit cb87e68 — FOUND
