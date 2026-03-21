---
phase: 210
plan: 02
title: Fix Bulk Renew FK Per-User Mapping
subsystem: AdminController / CreateAssessment
tags: [bulk-renew, fk-mapping, assessment]
requires: [210-01]
provides: [correct-bulk-renew-fk]
affects: [RenewalCertificate, CreateAssessment]
tech_added: []
tech_patterns: [per-user-fk-map-via-json-hidden-input]
key_files_created: []
key_files_modified:
  - Controllers/AdminController.cs
  - Views/Admin/CreateAssessment.cshtml
decisions:
  - "Per-user FK map dikirim via hidden input JSON untuk menghindari perubahan model binding"
  - "Backward compat: single-renew path tetap menggunakan int? single value"
duration: 15m
completed_date: "2026-03-21"
---

# Phase 210 Plan 02: Fix Bulk Renew FK Per-User Mapping Summary

**One-liner:** Bulk renew sekarang assign RenewsTrainingId/RenewsSessionId per-user via JSON dictionary hidden input, bukan single value dari user pertama.

## What Was Built

GET handler `CreateAssessment` diubah menerima `List<int>?` untuk `renewSessionId` dan `renewTrainingId`. Pada bulk path (>1 ID), dibangun dictionary `{UserId -> SourceId}` dan diserialisasi sebagai JSON ke `ViewBag.RenewalFkMap`. View meneruskannya sebagai hidden input. POST handler mendeserialize dictionary dan meresolve FK per userId di loop pembuatan session.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Fix GET handler dan POST loop untuk per-user FK mapping | 180f198 | AdminController.cs, CreateAssessment.cshtml |

## Deviations from Plan

None - plan executed exactly as written.

## Verification

- Build C# 0 compile errors (MSB3027 hanya karena app sedang berjalan, bukan error kompilasi)
- Backward compat: single renew path tidak berubah
- Bulk renew: setiap session di loop mendapat FK dari fkMap[userId]

## Self-Check: PASSED

- Controllers/AdminController.cs: modified (confirmed)
- Views/Admin/CreateAssessment.cshtml: modified (confirmed)
- Commit 180f198: exists
