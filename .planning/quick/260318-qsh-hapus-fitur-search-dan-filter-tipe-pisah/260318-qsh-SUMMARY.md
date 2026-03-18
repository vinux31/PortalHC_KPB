# Quick Task 260318-qsh: Summary

## Changes Made

### Removed
- **Search input** (filter-search) from CertificationManagement page
- **Tipe filter** (Training/Assessment dropdown) from filter bar
- Related JS event listeners (searchEl, tipeEl, searchTimer, debounce)
- `search` and `tipe` parameters from `FilterCertificationManagement` action
- `search` and `tipe` parameters from `ExportSertifikatExcel` action
- Filter logic for tipe and search in both controller actions
- Removed `RecordType.Assessment` restriction on category/subCategory filters (now applies to all record types)

### Changed
- **Sort order**: from `OrderByDescending(TanggalTerbit)` → `OrderBy(Kategori).ThenBy(SubKategori).ThenBy(Status)`
- Applied to all 3 actions: CertificationManagement, FilterCertificationManagement, ExportSertifikatExcel
- **Filter bar layout**: redistributed columns (col-md-2 each) after removing 2 filters

### Files Modified
- `Views/CDP/CertificationManagement.cshtml` — UI and JS cleanup
- `Controllers/CDPController.cs` — controller params and sort logic
