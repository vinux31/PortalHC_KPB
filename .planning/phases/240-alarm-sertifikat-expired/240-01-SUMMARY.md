---
phase: 240-alarm-sertifikat-expired
plan: 01
status: complete
started: 2026-03-23
completed: 2026-03-23
---

## Summary

Implementasi banner alert sertifikat expired di dashboard Home/Index untuk HC/Admin.

## What Was Built

- **DashboardHomeViewModel**: Tambah `ExpiredCount` dan `AkanExpiredCount` properties
- **HomeController.GetCertAlertCountsAsync**: Query count sertifikat expired dan akan expired (30 hari) dari TrainingRecords dan AssessmentSessions, dengan renewal chain exclusion
- **_CertAlertBanner.cshtml**: Partial view banner dua baris — merah (expired) dan kuning (akan expired) dengan link ke RenewalCertificate
- **Index.cshtml**: Wire banner setelah hero section, hanya render jika ada sertifikat bermasalah

## Key Files

### Created
- `Views/Home/_CertAlertBanner.cshtml`

### Modified
- `Models/DashboardHomeViewModel.cs`
- `Controllers/HomeController.cs`
- `Views/Home/Index.cshtml`

## Deviations

None.

## Self-Check: PASSED
