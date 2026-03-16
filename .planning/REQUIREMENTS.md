# Requirements: Portal HC KPB

**Defined:** 2026-03-16
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v7.0 Requirements

### Terminology

- [x] **TERM-01**: Assessment Results page shows "Analisis Elemen Teknis" as section title (was "Analisis Sub Kompetensi")
- [x] **TERM-02**: Assessment Results table header shows "Elemen Teknis" (was "Sub Kompetensi")
- [x] **TERM-03**: Import template Excel header shows "Elemen Teknis" (was "Sub Kompetensi")
- [x] **TERM-04**: Import template example row shows "Elemen Teknis x.x" (was "Sub Kompetensi x.x")
- [x] **TERM-05**: Import template help text shows "Kolom Elemen Teknis" (was "Kolom Sub Kompetensi")
- [x] **TERM-06**: Import page hint shows "Elemen Teknis (opsional)" (was "Sub Kompetensi (opsional)")
- [x] **TERM-07**: Cross-package warning message shows "Elemen Teknis" (was "Sub Kompetensi")

## Out of Scope

| Feature | Reason |
|---------|--------|
| Rename Proton/Silabus "Sub Kompetensi" | Different domain context (CDP coaching hierarchy) |
| Rename DB column `SubCompetency` | Internal, not user-facing, would require migration |
| Rename C# class/variable names | Internal code, not user-facing |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| TERM-01 | Phase 175 | Complete |
| TERM-02 | Phase 175 | Complete |
| TERM-03 | Phase 175 | Complete |
| TERM-04 | Phase 175 | Complete |
| TERM-05 | Phase 175 | Complete |
| TERM-06 | Phase 175 | Complete |
| TERM-07 | Phase 175 | Complete |

**Coverage:**
- v7.0 requirements: 7 total
- Mapped to phases: 7
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-16*
*Last updated: 2026-03-16 — traceability mapped to Phase 175*
