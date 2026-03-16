# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0 – v5.0** - Phases 1–172 (shipped 2026-03-16)
- ⚠️ **v6.0 Deployment Preparation** - Phases 173–174 (closed 2026-03-16, no work executed)
- ✅ **v7.0 Assessment Terminology Fix** - Phase 175 (shipped 2026-03-16)
- 🚧 **v7.1 Export & Import Data** - Phases 176–180 (in progress)

## Phases

<details>
<summary>✅ v1.0–v5.0 (Phases 1–172) - SHIPPED 2026-03-16</summary>

Phases 1–172 shipped across milestones v1.0–v5.0. See MILESTONES.md for details.

</details>

<details>
<summary>⚠️ v6.0 Deployment Preparation (Phases 173–174) - CLOSED 2026-03-16, no work executed</summary>

Phases 173–174 defined but never executed. Deferred indefinitely.

</details>

<details>
<summary>✅ v7.0 Assessment Terminology Fix (Phase 175) - SHIPPED 2026-03-16</summary>

#### Phase 175: Terminology Rename

**Goal**: All user-facing assessment UI shows "Elemen Teknis" instead of "Sub Kompetensi"
**Requirements**: TERM-01, TERM-02, TERM-03, TERM-04, TERM-05, TERM-06, TERM-07
**Plans**: 1 plan (complete)

</details>

### 🚧 v7.1 Export & Import Data (In Progress)

**Milestone Goal:** Add Excel export and import+template features to pages that lack them, enabling bulk data download and bulk input for operational data.

- [x] **Phase 176: Export Records & RecordsTeam** - Export personal and team training records to Excel (completed 2026-03-16)
- [x] **Phase 177: Import CoachCoacheeMapping** - Bulk import coach-coachee mapping via Excel template (completed 2026-03-16)
- [ ] **Phase 178: Export AuditLog** - Export audit trail to Excel with date filter
- [ ] **Phase 179: Export & Import Silabus Proton** - Roundtrip Excel for Proton syllabus data
- [ ] **Phase 180: Import Training & Export HistoriProton** - Bulk import training records and export Proton history

## Phase Details

### Phase 176: Export Records & RecordsTeam
**Goal**: Users can download their training history as Excel; supervisors/HC/Admin can download team training history as Excel
**Depends on**: Nothing (standalone export feature)
**Requirements**: EXP-01, EXP-02
**Success Criteria** (what must be TRUE):
  1. User clicks Export Excel on Records page and receives an .xlsx file containing their personal training history
  2. Atasan/HC/Admin clicks Export Excel on RecordsTeam page and receives an .xlsx file containing team training records
  3. Exported files contain all visible columns (nama pelatihan, tanggal, status, etc.) matching the on-screen table
**Plans**: 1 plan
Plans:
- [x] 176-01-PLAN.md — Export actions + buttons for Records and RecordsTeam

### Phase 177: Import CoachCoacheeMapping
**Goal**: Admin/HC can bulk-create coach-coachee mappings from an Excel file instead of one-by-one
**Depends on**: Nothing (standalone import feature)
**Requirements**: IMP-01, IMP-02
**Success Criteria** (what must be TRUE):
  1. Admin/HC can download a pre-filled Excel template for CoachCoacheeMapping with column headers and example row
  2. Admin/HC can upload a filled template and have valid rows created as new CoachCoacheeMapping records
  3. Invalid rows (missing data, unknown NIP) show clear error messages without crashing the import
**Plans**: 1 plan
Plans:
- [ ] 177-01-PLAN.md — Import actions + template download + modal UI for CoachCoacheeMapping

### Phase 178: Export AuditLog
**Goal**: Admin/HC can download the audit trail as Excel for offline review and compliance
**Depends on**: Nothing (standalone export feature)
**Requirements**: EXP-03
**Success Criteria** (what must be TRUE):
  1. Admin/HC clicks Export Excel on AuditLog page and receives an .xlsx file
  2. Export respects the current date filter (start/end date) so only filtered entries are exported
  3. Exported file contains all AuditLog columns (timestamp, actor, action, detail)
**Plans**: 1 plan
Plans:
- [ ] 178-01-PLAN.md — Export action + button for AuditLog

### Phase 179: Export & Import Silabus Proton
**Goal**: Admin/HC can roundtrip Silabus Proton data via Excel -- export current data and bulk-import new/updated entries
**Depends on**: Nothing (standalone feature on ProtonData page)
**Requirements**: EXP-04, IMP-03, IMP-04
**Success Criteria** (what must be TRUE):
  1. Admin/HC clicks Export Excel on Silabus Proton tab and receives an .xlsx file with all silabus rows
  2. Admin/HC can download a pre-filled Excel template for Silabus Proton with column headers and example row
  3. Admin/HC can upload a filled template and have valid rows created/updated as Silabus records
  4. Invalid rows show clear error messages without crashing the import
**Plans**: 1 plan
Plans:
- [ ] 179-01-PLAN.md — Export + import actions for Silabus Proton

### Phase 180: Import Training & Export HistoriProton
**Goal**: Admin/HC can bulk-import training records and export Proton history for offline review
**Depends on**: Nothing (standalone features)
**Requirements**: IMP-05, IMP-06, EXP-05
**Success Criteria** (what must be TRUE):
  1. Admin/HC can download a pre-filled Excel template for Training records with column headers and example row
  2. Admin/HC can upload a filled template and have valid rows created as Training records
  3. Invalid rows show clear error messages without crashing the import
  4. Coach/HC/Admin clicks Export Excel on Histori Proton page and receives an .xlsx file with history data
**Plans**: 1 plan
Plans:
- [ ] 180-01-PLAN.md — Import training + export Histori Proton

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 176. Export Records & RecordsTeam | 1/1 | Complete    | 2026-03-16 | - |
| 177. Import CoachCoacheeMapping | 1/1 | Complete   | 2026-03-16 | - |
| 178. Export AuditLog | v7.1 | 0/? | Not started | - |
| 179. Export & Import Silabus Proton | v7.1 | 0/? | Not started | - |
| 180. Import Training & Export HistoriProton | v7.1 | 0/? | Not started | - |
