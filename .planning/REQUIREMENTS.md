# Requirements: Portal HC KPB

**Defined:** 2026-03-17
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v7.3 Requirements

Requirements for Elemen Teknis Shuffle & Rename.

### Shuffle Algorithm

- [ ] **SHUF-01**: Cross-package shuffle guarantees at least 1 question per Elemen Teknis group in the shuffled result
- [ ] **SHUF-02**: Single-package shuffle guarantees at least 1 question per Elemen Teknis group in the shuffled result
- [ ] **SHUF-03**: Reshuffle (single + bulk) preserves Elemen Teknis distribution same as initial shuffle

### Internal Rename

- [ ] **RENAME-01**: PackageQuestion.SubCompetency DB column renamed to ElemenTeknis (with EF Core migration)
- [ ] **RENAME-02**: All C# model properties, variables, and method names use ElemenTeknis instead of SubCompetency
- [ ] **RENAME-03**: ViewModel class SubCompetencyScore renamed to ElemenTeknisScore

## Future Requirements

None.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Change spider web minimum threshold | Not needed — fix shuffle ensures all Elemen Teknis are represented |
| Rename ProtonSubKompetensi model/table | Different domain (Proton catalog), not related to assessment Elemen Teknis |
| UI label changes | Already done in v7.0 (Phase 175) |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| RENAME-01 | Phase 183 | Pending |
| RENAME-02 | Phase 183 | Pending |
| RENAME-03 | Phase 183 | Pending |
| SHUF-01 | Phase 184 | Pending |
| SHUF-02 | Phase 184 | Pending |
| SHUF-03 | Phase 184 | Pending |

**Coverage:**
- v7.3 requirements: 6 total
- Mapped to phases: 6
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-17*
*Last updated: 2026-03-17 — traceability mapped after roadmap creation*
