---
phase: quick-13
plan: 13
subsystem: CMP
tags: [navigation, ux, bagian-selection, cpdp-mapping]
key-files:
  created:
    - Views/CMP/MappingSectionSelect.cshtml
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/Mapping.cshtml
decisions:
  - "No role-based gating for Mapping section select (all roles can select a bagian, unlike KKJ which uses HasFullAccess check)"
  - "GAST description uses Mapping.cshtml wording (GSH Alkylation & Sour Treating), not KkjSectionSelect wording (Gas Separation & Treatment)"
metrics:
  duration: ~10min
  completed: 2026-02-26
  tasks_completed: 2
  tasks_total: 2
  files_created: 1
  files_modified: 2
---

# Quick Task 13: Add Bagian Selection Page for CMP CPDP Mapping Summary

**One-liner:** Added MappingSectionSelect gateway page with 4 department cards (RFCC, GAST, NGP, DHT) for CMP CPDP Mapping, mirroring the KKJ Matrix selection pattern.

## What Was Built

### Task 1: MappingSectionSelect.cshtml (commit 50d75fc)

Created `Views/CMP/MappingSectionSelect.cshtml` modeled on `KkjSectionSelect.cshtml`:

- 4 department cards: RFCC (blue), GAST (orange), NGP (green), DHT (purple)
- Each card links to `/CMP/Mapping?section={BAGIAN}` via `Url.Action("Mapping", "CMP", new { section = "RFCC" })`
- Button text: "Lihat Mapping" (not "Lihat KKJ")
- Page header: "Mapping KKJ - IDP (CPDP)" with subtitle "Pilih bagian untuk melihat mapping CPDP"
- Back button links to `Url.Action("Index", "CMP")` (CMP portal, not Home)
- GAST description: "GSH Alkylation & Sour Treating" (matches Mapping.cshtml)
- DHT description: "Diesel Hydrotreating/Hydrogen Manufacturing Unit"
- RFCC description: "Residual Fluid Catalytic Cracking"
- NGP description: "Naphtha Gas Processing"
- All CSS/icons identical to KkjSectionSelect pattern

### Task 2: Controller + Mapping.cshtml updates (commit cd7ad04)

Updated `Controllers/CMPController.cs` — `Mapping()` action:

- Added `string? section` parameter
- Returns `View("MappingSectionSelect")` when section is null/empty
- Sets `ViewBag.SelectedSection = section` before returning the main view
- No role-based gating (all users see selection page, all users can view any section's mapping)

Updated `Views/CMP/Mapping.cshtml`:

- Replaced hardcoded subtitle "Unit GSH & Alkylation Level Operator" with dynamic expression based on `ViewBag.SelectedSection`
- RFCC -> "Unit RFCC Level Operator"
- GAST -> "Unit GSH & Alkylation Level Operator"
- NGP -> "Unit Naphtha Gas Processing Level Operator"
- DHT -> "Unit Diesel Hydrotreating/HMU Level Operator"
- Added "Ganti Bagian" back button linking to `/CMP/Mapping` (no section = returns to selection page)

## Verification Results

- `dotnet build`: 0 C# compile errors (MSB3027 file-lock warning is pre-existing from running app process — not a compile error)
- Pre-existing CS8602/CS8604 warnings in CMPController.cs and CDPController.cs unchanged
- `/CMP/Mapping` with no section param will show MappingSectionSelect with 4 cards
- `/CMP/Mapping?section=GAST` shows mapping table with "Unit GSH & Alkylation Level Operator"
- "Ganti Bagian" button navigates back to `/CMP/Mapping` selection page
- KKJ Matrix flow unchanged — `/CMP/Kkj` and `/CMP/Index` unaffected

## Deviations from Plan

None — plan executed exactly as written.

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| Task 1 | 50d75fc | feat(13): create MappingSectionSelect.cshtml with 4 department cards |
| Task 2 | cd7ad04 | feat(13): update Mapping() action with section param and dynamic Mapping.cshtml header |

## Self-Check

- [x] `Views/CMP/MappingSectionSelect.cshtml` exists and contains all 4 section links
- [x] `Controllers/CMPController.cs` updated — Mapping() has `string? section` param
- [x] `Views/CMP/Mapping.cshtml` has dynamic subtitle and Ganti Bagian button
- [x] Commits 50d75fc and cd7ad04 exist in git log
