---
phase: 240-alarm-sertifikat-expired
plan: 02
status: complete
started: 2026-03-23
completed: 2026-03-23
---

## Summary

Implementasi bell notification CERT_EXPIRED yang di-trigger on page load Home/Index untuk HC/Admin.

## What Was Built

- **HomeController DI**: Inject `INotificationService` dan `ILogger<HomeController>`
- **TriggerCertExpiredNotificationsAsync**: Query sertifikat expired (TrainingRecords + AssessmentSessions), kirim notifikasi CERT_EXPIRED ke semua user HC/Admin dengan deduplication berdasarkan Type+Message
- Format: "Sertifikat [Judul] milik [Nama Pekerja] telah expired"
- ActionUrl: `/Admin/RenewalCertificate`
- Pre-fetch existing notifications + HashSet untuk O(1) dedup lookup

## Key Files

### Modified
- `Controllers/HomeController.cs`

## Deviations

None.

## Self-Check: PASSED
