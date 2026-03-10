# Requirements: Portal HC KPB — v3.17 Assessment Sub-Competency Analysis

**Defined:** 2026-03-10
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.17 Requirements

### Sub-Competency Tagging

- [x] **SUBTAG-01**: HC dapat import soal dengan kolom opsional "Sub Kompetensi" di template Excel
- [x] **SUBTAG-02**: PackageQuestion menyimpan field SubCompetency (nullable string) via migration
- [x] **SUBTAG-03**: Import logic memparse, menormalisasi (trim/case), dan menyimpan Sub Kompetensi per soal — backward compatible dengan template lama

### Assessment Analysis

- [x] **ANAL-01**: Sistem menghitung skor per sub-competency setelah worker submit exam (GroupBy SubCompetency → % benar)
- [x] **ANAL-02**: Results page menampilkan spider web radar chart (Chart.js) dengan axis per sub-competency
- [x] **ANAL-03**: Results page menampilkan summary tabel (Sub Kompetensi | Benar | Total | %)
- [x] **ANAL-04**: Radar chart dan tabel hanya tampil jika soal memiliki data SubCompetency (graceful hide untuk data lama)

## Future Requirements

None deferred.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Sub-competency master data management | Free-text tags cukup, tidak perlu CRUD terpisah |
| Legacy path (non-package) sub-competency | Hanya package path yang digunakan |
| Bar chart fallback untuk <3 sub-competency | Tabel summary sudah cukup sebagai fallback |
| Pre-compute scoring di tabel terpisah | On-the-fly LINQ GroupBy cukup performant |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SUBTAG-01 | Phase 146 | Complete |
| SUBTAG-02 | Phase 145 | Complete |
| SUBTAG-03 | Phase 146 | Complete |
| ANAL-01 | Phase 147 | Complete |
| ANAL-02 | Phase 147 | Complete |
| ANAL-03 | Phase 147 | Complete |
| ANAL-04 | Phase 147 | Complete |

**Coverage:**
- v3.17 requirements: 7 total
- Mapped to phases: 7
- Unmapped: 0

---
*Requirements defined: 2026-03-10*
*Last updated: 2026-03-10 after roadmap creation*
