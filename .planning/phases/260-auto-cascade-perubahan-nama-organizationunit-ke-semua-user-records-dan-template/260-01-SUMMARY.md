---
phase: 260-auto-cascade-perubahan-nama-organizationunit-ke-semua-user-records-dan-template
plan: 01
subsystem: admin-organization
tags: [cascade, denormalization, organization-unit]
dependency_graph:
  requires: []
  provides: [cascade-rename, cascade-reparent, block-deactivate, dynamic-template]
  affects: [ApplicationUser, CoachCoacheeMapping, AdminController]
tech_stack:
  added: []
  patterns: [cascade-update, denormalized-field-sync]
key_files:
  created: []
  modified:
    - Controllers/AdminController.cs
decisions:
  - Cascade logic placed before unit.Name assignment so oldName comparison works correctly
  - Reparent cascade walks ancestor chain to find Level 0 root for Section name
metrics:
  duration: ~2 minutes
  completed: "2026-03-26T03:03:46Z"
---

# Phase 260 Plan 01: Auto-Cascade OrganizationUnit Changes Summary

Cascade rename/reparent OrganizationUnit ke ApplicationUser dan CoachCoacheeMapping fields, block deactivate jika ada user aktif, dynamic section names di import template.

## What Was Done

### Task 1: Cascade rename/reparent + block deactivate
- **Commit:** f13b74be
- Capture `oldName` dan `oldParentId` di awal EditOrganizationUnit
- Rename Bagian (Level 0) cascades ke `User.Section` + `Mapping.AssignmentSection`
- Rename Unit (Level 1+) cascades ke `User.Unit` + `Mapping.AssignmentUnit`
- Reparent cascades `Section` ke nama Bagian root baru (walk ancestor chain)
- Block deactivate di ToggleOrganizationUnitActive jika ada user aktif di unit
- Flash message menampilkan jumlah user dan mapping yang terupdate

### Task 2: Dynamic DownloadImportTemplate
- **Commit:** b97ada36
- Method signature diubah ke `async Task<IActionResult>`
- Hardcoded "RFCC / DHT / HMU / NGP / GAST" diganti `GetAllSectionsAsync()` query
- Section names sekarang dinamis dari database

## Deviations from Plan

None - plan executed exactly as written.

## Verification Results

| Check | Result |
|-------|--------|
| Build (compilation) | PASS (0 CS errors, file copy lock from running app) |
| grep oldName count >= 4 | PASS (8) |
| grep hasActiveUsers >= 2 | PASS (4) |
| grep GetAllSectionsAsync in template | PASS (10) |
| No hardcoded RFCC string | PASS |

## Known Stubs

None.

## Self-Check: PASSED
