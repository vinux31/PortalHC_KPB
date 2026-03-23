# Requirements: Portal HC KPB

**Defined:** 2026-03-23
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v8.3 Requirements

Requirements for milestone v8.3: Date Range Filter Team View Records.

### Filter UI

- [ ] **FILT-01**: User (roleLevel ≤4) melihat 2 input date (Tanggal Awal & Tanggal Akhir) di filter bar Team View, menggantikan textbox Search Nama/NIP
- [ ] **FILT-02**: Saat tanggal diisi, tabel hanya menampilkan workers yang punya minimal 1 record (assessment atau training) di antara rentang tanggal
- [ ] **FILT-03**: Count kolom Assessment & Training di tabel hanya menghitung records yang jatuh di rentang tanggal yang dipilih
- [ ] **FILT-04**: Filter date bisa dikombinasikan dengan filter Bagian, Unit, Category, Sub Category, dan Status secara independen
- [ ] **FILT-05**: Default tanggal kosong = tampilkan semua records (behavior sama seperti sekarang)
- [ ] **FILT-06**: Tombol Reset clear semua filter termasuk date range

### Export

- [ ] **EXP-01**: Export Assessment menyertakan parameter date range ke server
- [ ] **EXP-02**: Export Training menyertakan parameter date range ke server

## Future Requirements

None for this milestone.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Preset date ranges (7 hari, 30 hari, dll) | Scope terlalu kecil untuk preset, native date input cukup |
| Search Nama/NIP | Dihapus sesuai permintaan user — diganti date range |
| Date filter di tab My Records | Scope hanya Team View |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| FILT-01 | Phase 239 | Pending |
| FILT-02 | Phase 239 | Pending |
| FILT-03 | Phase 239 | Pending |
| FILT-04 | Phase 239 | Pending |
| FILT-05 | Phase 239 | Pending |
| FILT-06 | Phase 239 | Pending |
| EXP-01 | Phase 239 | Pending |
| EXP-02 | Phase 239 | Pending |

**Coverage:**
- v8.3 requirements: 8 total
- Mapped to phases: 8
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-23*
*Last updated: 2026-03-23 — traceability mapped after roadmap creation*
