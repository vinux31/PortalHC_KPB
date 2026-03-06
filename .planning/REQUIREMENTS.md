# Requirements: Portal HC KPB - Histori Proton

**Defined:** 2026-03-06
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.6 Requirements

Requirements for Histori Proton milestone. Each maps to roadmap phases.

### Navigation & Access

- [x] **HIST-01**: CDP navbar memiliki menu item "Histori Proton"
- [x] **HIST-02**: Coachee hanya melihat riwayat diri sendiri (redirect langsung ke timeline)
- [x] **HIST-03**: Coach/SrSupervisor/SectionHead melihat list worker se-section
- [x] **HIST-04**: HC/Admin melihat semua worker

### Halaman List Worker

- [x] **HIST-05**: Halaman list menampilkan worker yang memiliki riwayat Proton (ProtonTrackAssignment)
- [x] **HIST-06**: Search by nama/NIP
- [x] **HIST-07**: Filter by unit/section
- [x] **HIST-08**: Setiap row menampilkan summary: nama, NIP, tahun Proton terakhir, status terakhir

### Halaman Timeline Detail

- [x] **HIST-09**: Vertical timeline dengan node per Proton year
- [x] **HIST-10**: Setiap node menampilkan: Tahun Proton (1/2/3), Unit saat itu
- [x] **HIST-11**: Setiap node menampilkan: Nama Coach
- [x] **HIST-12**: Setiap node menampilkan: Status (Lulus / Dalam Proses / Belum Mulai)
- [x] **HIST-13**: Setiap node menampilkan: Competency Level yang diperoleh (jika lulus)
- [x] **HIST-14**: Setiap node menampilkan: Tanggal mulai (assignment) & tanggal selesai (completion)
- [x] **HIST-15**: Timeline diurutkan kronologis (Tahun 1 -> 2 -> 3)

### Styling & UX

- [x] **HIST-16**: Desain konsisten dengan design system portal (Bootstrap 5, CSS variables)
- [x] **HIST-17**: Responsive mobile design

## Future Requirements

### Enhanced History

- **HIST-F01**: Expandable deliverable detail per tahun
- **HIST-F02**: Export riwayat ke PDF
- **HIST-F03**: Print-friendly view

## Out of Scope

| Feature | Reason |
|---------|--------|
| Deliverable drill-down per tahun | Deferred to future -- summary only for v3.6 |
| Edit/modify history data | History is read-only, sourced from existing records |
| Coaching session log display | Too much detail for timeline view |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| HIST-01 | Phase 107 | Complete |
| HIST-02 | Phase 107 | Complete |
| HIST-03 | Phase 107 | Complete |
| HIST-04 | Phase 107 | Complete |
| HIST-05 | Phase 107 | Complete |
| HIST-06 | Phase 107 | Complete |
| HIST-07 | Phase 107 | Complete |
| HIST-08 | Phase 107 | Complete |
| HIST-09 | Phase 108 | Complete |
| HIST-10 | Phase 108 | Complete |
| HIST-11 | Phase 108 | Complete |
| HIST-12 | Phase 108 | Complete |
| HIST-13 | Phase 108 | Complete |
| HIST-14 | Phase 108 | Complete |
| HIST-15 | Phase 108 | Complete |
| HIST-16 | Phase 108 | Complete |
| HIST-17 | Phase 108 | Complete |

**Coverage:**
- v3.6 requirements: 17 total
- Mapped to phases: 17
- Unmapped: 0

---
*Requirements defined: 2026-03-06*
*Last updated: 2026-03-06 after initial definition*
