---
phase: 253
plan: 01
status: complete
started: 2026-03-25
completed: 2026-03-25
---

## Summary

Backend AddTraining diperbarui untuk mendukung multi-select pekerja dengan per-worker file upload.

## What Was Built

1. **PerWorkerCertData ViewModel** — nested class dengan UserId, CertificateFile, NomorSertifikat; WorkerCerts list property di CreateTrainingRecordViewModel
2. **Kategori Dedup** — SetTrainingCategoryViewBag menggunakan GroupBy untuk menghilangkan duplikasi kategori
3. **POST Handler Multi-Select** — validasi manual (min 1, max 20 pekerja), all-or-nothing file validation, dan save path baru yang membuat N TrainingRecord per pekerja dengan file masing-masing

## Key Files

### Modified
- `Models/CreateTrainingRecordViewModel.cs` — PerWorkerCertData class + WorkerCerts property, removed [Required] from UserId
- `Controllers/AdminController.cs` — SetTrainingCategoryViewBag dedup + POST handler multi-select path

## Commits
- `ebec9124` — feat(253): add PerWorkerCertData ViewModel + dedup kategori dropdown
- `515591c4` — feat(253): POST handler multi-select with per-worker file upload

## Deviations
- Variable `actor` renamed to `multiActor` in multi-select block to avoid scope conflict with existing variable

## Self-Check: PASSED
- [x] PerWorkerCertData class defined
- [x] WorkerCerts property added
- [x] [Required] removed from UserId
- [x] GroupBy dedup in SetTrainingCategoryViewBag
- [x] POST handler has 3 paths: WorkerCerts, bulk renewal, single user
- [x] dotnet build succeeds with 0 errors
