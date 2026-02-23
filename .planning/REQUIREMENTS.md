# Requirements: Portal HC KPB

**Defined:** 2026-02-23
**Milestone:** v1.9 Proton Catalog Management
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v1 Requirements

Requirements for v1.9 milestone. Each maps to roadmap phases.

### Schema (SCHEMA)

- [ ] **SCHEMA-01**: `ProtonTrack` entity exists as a dedicated table; `ProtonKompetensi` links to it via FK, replacing the old `TrackType`+`TahunKe` string fields on `ProtonKompetensi`

### Catalog View (CAT)

- [ ] **CAT-01**: HC/Admin sees the full Proton catalog hierarchy for a selected track on one page — track dropdown at top, tree table below showing all Kompetensi → SubKompetensi → Deliverable with expand/collapse toggles
- [ ] **CAT-02**: HC/Admin can create a new Track (TrackType, TahunKe, DisplayName) and see it immediately in the track dropdown

### CRUD Operations (CAT)

- [ ] **CAT-03**: HC/Admin can add a Kompetensi to the selected track inline (no page navigation)
- [ ] **CAT-04**: HC/Admin can add a SubKompetensi under any Kompetensi inline
- [ ] **CAT-05**: HC/Admin can add a Deliverable under any SubKompetensi inline
- [ ] **CAT-06**: HC/Admin can edit the name of any Kompetensi, SubKompetensi, or Deliverable in-place via AJAX
- [ ] **CAT-07**: HC/Admin can delete any catalog item — system shows count of active coachees affected and requires explicit hard confirmation before proceeding; cascades to child items

### Reorder (CAT)

- [ ] **CAT-08**: HC/Admin can reorder Kompetensi, SubKompetensi, and Deliverables within their parent level via drag-and-drop; new order persists immediately via AJAX

### Navigation (CAT)

- [ ] **CAT-09**: Proton Catalog Manager is accessible from HC/Admin navigation

## v2 Requirements

Deferred to v1.10+.

### Proton UX

- **WTRN-01**: Worker can create their own manual training record
- **WTRN-02**: Worker can upload a certificate for their self-created training record
- **UX-01**: Answer review shows point value per question alongside correct/incorrect indicator
- **PERF-01**: Monitoring queries use batch loading to eliminate N+1 patterns

## Out of Scope

| Feature | Reason |
|---------|--------|
| Email notifications for deliverable approval/rejection | No email system in project |
| Coachee self-upload evidence | Separate feature, deferred to future milestone |
| Batch import of catalog from Excel | Manual entry via UI is sufficient for v1.9 |
| Track archiving / soft-delete | IsActive flag on ProtonTrack provides this |
| Audit log for catalog changes | Not required for v1.9 |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SCHEMA-01 | Phase 33 | Pending |
| CAT-01 | Phase 34 | Pending |
| CAT-02 | Phase 34 | Pending |
| CAT-03 | Phase 35 | Pending |
| CAT-04 | Phase 35 | Pending |
| CAT-05 | Phase 35 | Pending |
| CAT-06 | Phase 35 | Pending |
| CAT-07 | Phase 36 | Pending |
| CAT-08 | Phase 37 | Pending |
| CAT-09 | Phase 37 | Pending |

**Coverage:**
- v1 requirements: 9 total
- Mapped to phases: 9
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-23*
*Last updated: 2026-02-23 — initial definition for v1.9*
