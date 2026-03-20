# Requirements: Portal HC KPB

**Defined:** 2026-03-20
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v7.8 Requirements

### CMP View

- [x] **CMP-01**: 2 card (KKJ + Alignment) di CMP Index digabung jadi 1 card "Dokumen KKJ & Alignment KKJ/IDP"
- [x] **CMP-02**: Halaman gabungan menampilkan 2 tab utama: "Kebutuhan Kompetensi Jabatan" dan "Alignment KKJ & IDP"
- [x] **CMP-03**: Tab KKJ menampilkan semua bagian beserta file-nya langsung (grouped per bagian, tanpa dropdown)
- [x] **CMP-04**: Tab Alignment menampilkan semua bagian beserta file-nya langsung (grouped per bagian)
- [x] **CMP-05**: Role-based filtering tetap berlaku — L5-L6 hanya lihat bagian sendiri, L1-L4 lihat semua
- [x] **CMP-06**: Action `/CMP/Kkj` dan `/CMP/Mapping` di-redirect ke halaman gabungan (backward compat)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Merge tabel KkjFile + CpdpFile | Cukup gabung di view layer |
| Gabung admin pages (KkjMatrix, CpdpFiles) | User request: admin tetap terpisah |
| Ubah download endpoint | Tetap pakai endpoint existing |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| CMP-01 | Phase 206 | Complete |
| CMP-02 | Phase 205 | Complete |
| CMP-03 | Phase 205 | Complete |
| CMP-04 | Phase 205 | Complete |
| CMP-05 | Phase 205 | Complete |
| CMP-06 | Phase 206 | Complete |

**Coverage:**
- v7.8 requirements: 6 total
- Mapped to phases: 6
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-20*
*Last updated: 2026-03-20 — traceability updated after roadmap creation*
