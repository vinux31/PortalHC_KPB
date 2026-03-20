# Requirements: Portal HC KPB

**Defined:** 2026-03-20
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v7.9 Requirements

### Grouped View

- [ ] **GRP-01**: Tabel RenewalCertificate menampilkan data dikelompokkan per judul sertifikat (bukan flat list per-orang)
- [ ] **GRP-02**: Group header menampilkan judul sertifikat, kategori/sub-kategori, dan badge count (N expired, N akan expired)
- [ ] **GRP-03**: Setiap group bisa di-collapse/expand (default: expanded)
- [ ] **GRP-04**: Tabel per group hanya menampilkan kolom: Checkbox, Nama, Valid Until, Status, Aksi

### Bulk Renew

- [ ] **BULK-01**: Checkbox select-all per group untuk memilih semua pekerja dalam satu sertifikat
- [ ] **BULK-02**: Tombol "Renew N Pekerja" per group muncul saat ada checkbox tercentang

### Filter & Pagination

- [ ] **FILT-01**: Filter Bagian/Unit/Kategori/Sub Kategori/Status tetap berfungsi pada tampilan grouped
- [ ] **FILT-02**: Summary cards (Expired count, Akan Expired count) tetap dipertahankan dan update sesuai filter

## Out of Scope

| Feature | Reason |
|---------|--------|
| Redesign filter bar | Filter bar existing sudah cukup, cukup pastikan tetap berfungsi |
| Card/tile-based layout | Pendekatan grouped table lebih natural untuk bulk action |
| Grouped by pekerja | User memilih grouped by sertifikat |
| Perubahan Certificate History modal | Modal existing sudah berfungsi baik |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| GRP-01 | - | Pending |
| GRP-02 | - | Pending |
| GRP-03 | - | Pending |
| GRP-04 | - | Pending |
| BULK-01 | - | Pending |
| BULK-02 | - | Pending |
| FILT-01 | - | Pending |
| FILT-02 | - | Pending |

**Coverage:**
- v7.9 requirements: 8 total
- Mapped to phases: 0
- Unmapped: 8 ⚠️

---
*Requirements defined: 2026-03-20*
*Last updated: 2026-03-20 after initial definition*
