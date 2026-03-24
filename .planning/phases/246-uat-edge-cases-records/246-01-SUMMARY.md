---
phase: 246-uat-edge-cases-records
plan: 01
subsystem: seed-data
tags: [seed, uat, edge-cases, assessment, token, certificate]
dependency_graph:
  requires: [241-02-SUMMARY.md]
  provides: [token-required-session, expired-cert-session]
  affects: [Data/SeedData.cs, HcPortalDB_Dev]
tech_stack:
  added: []
  patterns: [inner-guard-fallback, idempotent-seed, multi-user-assignment]
key_files:
  created: []
  modified:
    - Data/SeedData.cs
decisions:
  - UserId diperlukan pada AssessmentSession meskipun multi-user pattern — field not nullable, set ke rinoId sebagai session owner
  - ValidUntil = certDate.AddYears(1) dengan certDate = now.AddDays(-400) menghasilkan expired ~35 hari lalu
  - NomorSertifikat hardcoded "KPB/SEED-EXP/01/2024" untuk menghindari sequence conflict dengan CertNumberHelper
metrics:
  duration: 18m
  completed_date: "2026-03-24"
  tasks: 2
  files_modified: 1
---

# Phase 246 Plan 01: UAT Edge Cases Seed Data Summary

Seed 2 sesi baru untuk UAT edge cases: token-required session (EDGE-01/03) dan completed session dengan sertifikat expired (EDGE-04).

## Completed Tasks

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Seed token-required session + expired cert | e1f8dad9 | Data/SeedData.cs |
| 2 | Run seed + verifikasi database | 8210ac22 | Data/SeedData.cs (FK fix) |

## What Was Built

### SeedTokenRequiredSessionAsync
- AssessmentSession "OJT Token Test Q1-2026": Status=Open, IsTokenRequired=true, AccessToken=EDGE-TOKEN-001
- 5 soal (ET: Keselamatan + Proses) dengan 4 opsi masing-masing
- UserPackageAssignment untuk Rino DAN Iwan (sibling multi-user pattern)
- Inner guard di fallback block + call di main flow

### SeedExpiredCertSessionAsync
- AssessmentSession "OJT Expired Cert Q3-2024": Status=Completed, IsPassed=true
- ValidUntil = now.AddDays(-400).AddYears(1) = ~35 hari lalu (expired)
- NomorSertifikat = "KPB/SEED-EXP/01/2024" (hardcoded untuk isolasi dari CertNumberHelper sequence)
- Inner guard di fallback block + call di main flow

## Verification Results

```
OJT Token Test Q1-2026   | IsTokenRequired=1 | Status=Open
OJT Expired Cert Q3-2024 | IsPassed=1        | Status=Completed | ExpiredStatus=EXPIRED
UserPackageAssignments    | COUNT=2 (Rino+Iwan)
```

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] UserId wajib pada AssessmentSession**
- **Found during:** Task 2 (saat run aplikasi)
- **Issue:** `SeedTokenRequiredSessionAsync` tidak set UserId menyebabkan FK constraint violation — field `UserId` not nullable
- **Fix:** Set `UserId = rinoId` pada session creation, Iwan tetap diakomodasi via UserPackageAssignment
- **Files modified:** Data/SeedData.cs
- **Commit:** 8210ac22

## Known Stubs

Tidak ada stub — semua data seed sudah terhubung ke database dan siap untuk browser UAT di Plan 02.

## Self-Check: PASSED

- [x] Data/SeedData.cs contains `private static async Task SeedTokenRequiredSessionAsync(`
- [x] Data/SeedData.cs contains `private static async Task SeedExpiredCertSessionAsync(`
- [x] Data/SeedData.cs contains `IsTokenRequired = true`
- [x] Data/SeedData.cs contains `AccessToken = "EDGE-TOKEN-001"`
- [x] Data/SeedData.cs contains `Title == "OJT Token Test Q1-2026"` (idempotency guard)
- [x] Data/SeedData.cs contains `Title == "OJT Expired Cert Q3-2024"` (idempotency guard)
- [x] Data/SeedData.cs contains `ValidUntil = certDate.AddYears(1)` (certDate = now.AddDays(-400))
- [x] UserPackageAssignment count = 2 untuk token test session
- [x] `dotnet build` sukses dengan 0 errors
- [x] Database: OJT Token Test Q1-2026 | IsTokenRequired=1 | Open
- [x] Database: OJT Expired Cert Q3-2024 | IsPassed=1 | ValidUntil EXPIRED
