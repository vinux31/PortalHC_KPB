---
phase: 250-security-performance
plan: "01"
subsystem: security-performance
tags: [security, xss, console-log, memory-cache, performance]
dependency_graph:
  requires: []
  provides: [SEC-01, SEC-02, PERF-01]
  affects: [Views/CMP/Assessment.cshtml, Views/CDP/CoachingProton.cshtml, Controllers/HomeController.cs]
tech_stack:
  added: [Microsoft.Extensions.Caching.Memory]
  patterns: [IMemoryCache throttle guard, WebUtility.HtmlEncode XSS escape]
key_files:
  modified:
    - Views/CMP/Assessment.cshtml
    - Views/CDP/CoachingProton.cshtml
    - Controllers/HomeController.cs
decisions:
  - "Global cache key 'cert-notif-global' dipilih (bukan per-user) karena TriggerCertExpiredNotificationsAsync bersifat global"
  - "AbsoluteExpiration via TimeSpan.FromHours(1), bukan SlidingExpiration, agar TTL konsisten"
  - "Encode setiap variabel input sebelum interpolasi ke tooltipText, bukan encode hasil akhir"
metrics:
  duration: "~5 menit"
  completed: "2026-03-24T02:40:42Z"
  tasks_completed: 3
  files_modified: 3
---

# Phase 250 Plan 01: Security & Performance Hardening Summary

**One-liner:** Hapus 4 console.log sensitif, tutup XSS di tooltip approval badge via HtmlEncode, dan throttle cert notification query 1x/jam via IMemoryCache.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Hapus console.log sensitif di Assessment.cshtml | 876ae91f | Views/CMP/Assessment.cshtml |
| 2 | XSS escape di GetApprovalBadgeWithTooltip | 11f94bac | Views/CDP/CoachingProton.cshtml |
| 3 | Throttle TriggerCertExpiredNotificationsAsync via IMemoryCache | e83d0df4 | Controllers/HomeController.cs |

## What Was Built

### SEC-01: Hapus console.log Sensitif (Assessment.cshtml)
Empat baris `console.log` dihapus dari JavaScript di Assessment.cshtml:
- Log token asli di verifyTokenForAssessment (ekspos credential)
- Log response payload VerifyToken (ekspos server response)
- Log starting standard assessment id
- Log StartAssessment response payload

Dua `console.error` di error handler dipertahankan karena tidak mengekspos data sensitif.

### SEC-02: XSS Escape di GetApprovalBadgeWithTooltip (CoachingProton.cshtml)
Fungsi `GetApprovalBadgeWithTooltip` di `@functions` block dimodifikasi untuk meng-encode input sebelum interpolasi ke HTML attribute `title`:
- `approverName` di-encode via `System.Net.WebUtility.HtmlEncode(approverName ?? "")`
- `approvedAt` di-encode via `System.Net.WebUtility.HtmlEncode(approvedAt ?? "")`
- Null-safe menggunakan `?? ""` operator

### PERF-01: Throttle via IMemoryCache (HomeController.cs)
`IMemoryCache` di-inject ke `HomeController` dan `TriggerCertExpiredNotificationsAsync` di-wrap dengan cache guard:
- Cache key global `"cert-notif-global"` dengan `AbsoluteExpiration` 1 jam
- Fungsi hanya dieksekusi satu kali per jam, bukan setiap page refresh dashboard

## Deviations from Plan

None - plan executed exactly as written.

## Verification Results

- `grep -c "console.log" Views/CMP/Assessment.cshtml` = 0 (PASS)
- `grep -c "console.error" Views/CMP/Assessment.cshtml` = 2 (PASS)
- `grep -c "WebUtility.HtmlEncode" Views/CDP/CoachingProton.cshtml` = 2 (PASS)
- `grep "cert-notif-global" Controllers/HomeController.cs` = matches (PASS)
- `dotnet build` = 0 Errors (PASS)

## Known Stubs

None.

## Self-Check: PASSED

- Views/CMP/Assessment.cshtml: FOUND (modified)
- Views/CDP/CoachingProton.cshtml: FOUND (modified)
- Controllers/HomeController.cs: FOUND (modified)
- Commit 876ae91f: FOUND
- Commit 11f94bac: FOUND
- Commit e83d0df4: FOUND
