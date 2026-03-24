---
phase: 251-data-integrity-logic
plan: "01"
subsystem: models-controller
tags: [bugfix, data-integrity, timezone, validation, logging]
dependency_graph:
  requires: []
  provides: [UTC-based expiry, bulk-renewal-detection, edit-assessment-past-date, renewal-fkmap-log]
  affects: [CertificationManagement, CreateAssessment, EditAssessment]
tech_stack:
  added: []
  patterns: [DateTime.UtcNow for UTC consistency, LogWarning for observability]
key_files:
  created: []
  modified:
    - Models/TrainingRecord.cs
    - Models/CertificationManagementViewModel.cs
    - Controllers/AdminController.cs
decisions:
  - DateTime.UtcNow digunakan agar kalkulasi expiry konsisten lintas timezone server
  - isRenewalModePost diperluas agar bulk renewal via RenewalFkMap juga wajib isi ValidUntil
  - Past-date check di EditAssessment dihapus karena HC perlu edit jadwal yang sudah lewat
  - Bare catch RenewalFkMap diganti LogWarning untuk observability tanpa mengubah behavior
metrics:
  duration: "5m"
  completed: "2026-03-24"
  tasks: 2
  files: 3
---

# Phase 251 Plan 01: Data Integrity & Logic Bug Fix Summary

UTC-based expiry calculation pada model sertifikat, bulk renewal ValidUntil enforcement, relax past-date EditAssessment, dan LogWarning pada RenewalFkMap deserialize failure.

## Tasks Completed

| Task | Description | Commit | Files |
|------|-------------|--------|-------|
| 1 | DateTime.Now ke DateTime.UtcNow di TrainingRecord dan CertificationManagementViewModel | fc7c391f | Models/TrainingRecord.cs, Models/CertificationManagementViewModel.cs |
| 2 | Fix isRenewalModePost, hapus past-date EditAssessment, log warning bare catch | f05b5dac | Controllers/AdminController.cs |

## Changes Detail

### DATA-01: DateTime.UtcNow (TrainingRecord.cs + CertificationManagementViewModel.cs)

Tiga lokasi `DateTime.Now` diganti `DateTime.UtcNow`:
- `TrainingRecord.IsExpiringSoon` getter
- `TrainingRecord.DaysUntilExpiry` getter
- `SertifikatRow.DeriveCertificateStatus` method

### DATA-03: isRenewalModePost bulk renewal detection (AdminController.cs line 1254)

```csharp
// SEBELUM
bool isRenewalModePost = model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue;
// SESUDAH
bool isRenewalModePost = model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue || !string.IsNullOrEmpty(RenewalFkMap);
```

Memastikan bulk renewal yang mengirim `RenewalFkMap` JSON (tanpa FK individual) tetap divalidasi wajib isi ValidUntil.

### DATA-04: Relax past-date di EditAssessment POST (AdminController.cs)

Dua baris validasi dihapus dari EditAssessment POST:
```csharp
// DIHAPUS:
if (model.Schedule < DateTime.Today)
    editErrors.Add("Schedule date cannot be in the past.");
```

HC sekarang dapat mengedit assessment yang jadwalnya sudah lewat tanpa error validasi.

### DATA-05: Log warning pada bare catch RenewalFkMap (AdminController.cs line 1437)

```csharp
// SEBELUM
catch { /* ignore malformed map — fall back to model value */ }
// SESUDAH
catch (Exception ex)
{
    _logger.LogWarning(ex, "Failed to deserialize RenewalFkMap");
}
```

## Verification

- `dotnet build`: 0 Error, 67 Warning (pre-existing LDAP warnings, tidak baru)
- Tidak ada `DateTime.Now` aktif di TrainingRecord.cs dan CertificationManagementViewModel.cs
- `grep -c "DateTime.UtcNow" Models/TrainingRecord.cs` = 2
- `grep -c "DateTime.UtcNow" Models/CertificationManagementViewModel.cs` = 1
- isRenewalModePost mengandung `!string.IsNullOrEmpty(RenewalFkMap)`
- LogWarning tersedia di catch block RenewalFkMap

## Deviations from Plan

None - plan dieksekusi tepat seperti yang ditulis.

## Self-Check: PASSED

- fc7c391f: fix(251-01): DateTime.Now ke DateTime.UtcNow - FOUND
- f05b5dac: fix(251-01): bulk renewal detection, relax past-date EditAssessment, log bare catch - FOUND
- Models/TrainingRecord.cs - MODIFIED
- Models/CertificationManagementViewModel.cs - MODIFIED
- Controllers/AdminController.cs - MODIFIED
