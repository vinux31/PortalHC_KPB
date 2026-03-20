# Requirements: Portal HC KPB

**Defined:** 2026-03-20
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v7.8 Requirements

### CMP View

- [ ] **CMP-01**: 2 card (KKJ + Alignment) di CMP Index digabung jadi 1 card "Dokumen KKJ & Alignment KKJ/IDP"
- [ ] **CMP-02**: Halaman gabungan menampilkan 2 tab utama: "Kebutuhan Kompetensi Jabatan" dan "Alignment KKJ & IDP"
- [ ] **CMP-03**: Tab KKJ menampilkan semua bagian beserta file-nya langsung (grouped per bagian, tanpa dropdown)
- [ ] **CMP-04**: Tab Alignment menampilkan semua bagian beserta file-nya langsung (grouped per bagian)
- [ ] **CMP-05**: Role-based filtering tetap berlaku — L5-L6 hanya lihat bagian sendiri, L1-L4 lihat semua
- [ ] **CMP-06**: Action `/CMP/Kkj` dan `/CMP/Mapping` di-redirect ke halaman gabungan (backward compat)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Merge tabel KkjFile + CpdpFile | Cukup gabung di view layer |
| Gabung admin pages (KkjMatrix, CpdpFiles) | User request: admin tetap terpisah |
| Ubah download endpoint | Tetap pakai endpoint existing |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| CMP-01 | — | Pending |
| CMP-02 | — | Pending |
| CMP-03 | — | Pending |
| CMP-04 | — | Pending |
| CMP-05 | — | Pending |
| CMP-06 | — | Pending |

**Coverage:**
- v7.8 requirements: 6 total
- Mapped to phases: 0
- Unmapped: 6 ⚠️

---
*Requirements defined: 2026-03-20*
*Last updated: 2026-03-20 after initial definition*
