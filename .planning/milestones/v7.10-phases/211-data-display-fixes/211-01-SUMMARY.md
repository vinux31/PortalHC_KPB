---
phase: 211-data-display-fixes
plan: "01"
subsystem: RenewalCertificate
tags: [bug-fix, renewal, grouping, status, prefill]
dependency_graph:
  requires: []
  provides: [FIX-05, FIX-06, FIX-07, FIX-08, FIX-09, FIX-10]
  affects: [RenewalCertificate, CreateAssessment]
tech_stack:
  added: []
  patterns: [StringComparer.OrdinalIgnoreCase, Uri.UnescapeDataString]
key_files:
  modified:
    - Models/CertificationManagementViewModel.cs
    - Controllers/AdminController.cs
    - Views/Admin/CreateAssessment.cshtml
decisions:
  - "DeriveCertificateStatus pisahkan cek Permanent dan ValidUntil=null agar non-Permanent dengan null expiry → Expired"
  - "MapKategori memakai ToUpperInvariant() untuk case-insensitive input matching"
  - "GroupBy dan Where filter pakai OrdinalIgnoreCase agar MIGAS dan Migas masuk group sama"
  - "Uri.UnescapeDataString di awal FilterRenewalCertificateGroup agar karakter / & # aman"
metrics:
  duration_minutes: 5
  completed_date: "2026-03-21T04:38:05Z"
  tasks_completed: 2
  files_modified: 3
---

# Phase 211 Plan 01: Data Display Fixes Summary

**One-liner:** Perbaikan 6 bug RenewalCertificate — ValidUntil=null status fix, category prefill dari TrainingRecord, MapKategori case-insensitive, GroupBy OrdinalIgnoreCase, URL-safe karakter khusus, dan warning informatif saat expiry kosong.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Fix DeriveCertificateStatus dan ValidUntil=null warning (FIX-05, FIX-10) | 6e969bb | CertificationManagementViewModel.cs, AdminController.cs, CreateAssessment.cshtml |
| 2 | Fix Category prefill, MapKategori, case-insensitive grouping, URL-safe grouping (FIX-06, FIX-07, FIX-08, FIX-09) | 6e969bb | AdminController.cs |

## Changes Made

### FIX-05: DeriveCertificateStatus
- File: `Models/CertificationManagementViewModel.cs` line 55-57
- Sebelum: `if (certificateType == "Permanent" || validUntil == null)` → Permanent
- Sesudah: Cek Permanent terpisah. ValidUntil=null non-Permanent → Expired
- Dampak: TrainingRecord dengan ValidUntil=null dan CertificateType bukan Permanent muncul di renewal list

### FIX-06: Category prefill dari TrainingRecord
- File: `Controllers/AdminController.cs` block renewTrainingId
- Tambah: `model.Category = MapKategori(sourceTraining.Kategori);`
- Dampak: Saat renew dari TrainingRecord, Category ter-prefill sesuai mapping

### FIX-07: MapKategori case-insensitive
- File: `Controllers/AdminController.cs` MapKategori method
- Ubah: `raw switch` → `raw?.Trim().ToUpperInvariant() switch`
- Dampak: Input "mandatory", "Mandatory", "MANDATORY" semua cocok ke "Mandatory HSSE Training"

### FIX-08: GroupBy dan Where case-insensitive
- File: `Controllers/AdminController.cs`
- GroupBy: tambah `StringComparer.OrdinalIgnoreCase`
- Where di FilterRenewalCertificate: pakai `string.Equals(..., OrdinalIgnoreCase)`
- Where di FilterRenewalCertificateGroup: pakai `string.Equals(..., OrdinalIgnoreCase)`
- Dampak: "MIGAS" dan "Migas" masuk ke group yang sama

### FIX-09: URL-safe karakter khusus
- File: `Controllers/AdminController.cs` FilterRenewalCertificateGroup
- Tambah: `judul = Uri.UnescapeDataString(judul ?? "");` di awal method
- Dampak: Judul dengan `/`, `&`, `#` tidak menyebabkan mismatch saat URL query string

### FIX-10: ValidUntil=null warning di CreateAssessment
- File: `Controllers/AdminController.cs` dan `Views/Admin/CreateAssessment.cshtml`
- Tambah else clause pada kedua block (renewSessionId dan renewTrainingId) yang set `ViewBag.RenewalValidUntilWarning`
- Tambah Bootstrap alert di view sebelum ValidUntil input field
- Dampak: User melihat pesan informatif saat sumber sertifikat tidak punya tanggal expired

## Deviations from Plan

None — plan executed exactly as written.

## Verification

- `dotnet build` sukses dengan 0 error (72 warnings — pre-existing CA1416 warnings, tidak relevan)
- Grep confirm: `if (certificateType == "Permanent")` sebagai cek terpisah
- Grep confirm: `return CertificateStatus.Expired; // non-Permanent` ada
- Grep confirm: `StringComparer.OrdinalIgnoreCase` ada di GroupBy
- Grep confirm: `model.Category = MapKategori` ada di renewTrainingId block
- Grep confirm: `Uri.UnescapeDataString` ada di FilterRenewalCertificateGroup

## Self-Check: PASSED
