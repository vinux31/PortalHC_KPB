# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0 – v5.0** - Phases 1–172 (shipped 2026-03-16)
- ⚠️ **v6.0 Deployment Preparation** - Phases 173–174 (closed 2026-03-16, no work executed)
- ✅ **v7.0 Assessment Terminology Fix** - Phase 175 (shipped 2026-03-16)
- ✅ **v7.1 Export & Import Data** - Phases 176–180 (shipped 2026-03-16)
- ✅ **v7.2 PDF Evidence Report Enhancement** - Phase 181 (shipped 2026-03-17)
- ✅ **v7.2 (loose)** - Phase 182 (shipped 2026-03-17)
- ✅ **v7.3 Elemen Teknis Shuffle & Rename** - Phases 183–184 (shipped 2026-03-17)
- 🚧 **v7.4 Certification Management** - Phases 185–189 (in progress)

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

<details>
<summary>✅ v7.1 Export & Import Data (Phases 176–180) — SHIPPED 2026-03-16</summary>

- [x] Phase 176: Export Records & RecordsTeam (1/1 plans) — completed 2026-03-16
- [x] Phase 177: Import CoachCoacheeMapping (1/1 plans) — completed 2026-03-16
- [x] Phase 178: Export AuditLog (1/1 plans) — completed 2026-03-16
- [x] Phase 179: Export & Import Silabus Proton (1/1 plans) — completed 2026-03-16
- [x] Phase 180: Import Training & Export HistoriProton (1/1 plans) — completed 2026-03-16

</details>

<details>
<summary>✅ v7.2 PDF Evidence Report Enhancement (Phase 181) — SHIPPED 2026-03-17</summary>

#### Phase 181: PDF Header Coachee Info
**Goal**: The PDF Evidence Report header displays coachee identity (Nama, Unit, Track) above Tanggal Coaching
**Requirements**: PDF-01, PDF-02, PDF-03
**Plans**: 1 plan (complete)

</details>

<details>
<summary>✅ v7.2 (loose) — Phase 182 — SHIPPED 2026-03-17</summary>

#### Phase 182: CDP/CoachingProton Evidence Column Clarification

**Goal**: Fix Evidence column to derive display from Status field instead of EvidencePath
**Requirements**: None (loose phase)
**Plans**: 1 plan (complete)

</details>

<details>
<summary>✅ v7.3 Elemen Teknis Shuffle & Rename (Phases 183–184) — SHIPPED 2026-03-17</summary>

#### Phase 183: Internal Rename SubCompetency → ElemenTeknis
**Goal**: All internal C# code, DB column, and ViewModels use ElemenTeknis instead of SubCompetency
**Requirements**: RENAME-01, RENAME-02, RENAME-03
**Plans**: 1 plan (complete)

#### Phase 184: Shuffle Algorithm — Guaranteed Elemen Teknis Distribution
**Goal**: Cross-package and single-package shuffle guarantees at least one question per Elemen Teknis group, and reshuffles preserve that distribution
**Requirements**: SHUF-01, SHUF-02, SHUF-03
**Plans**: 3 plans (complete)

</details>

---

### 🚧 v7.4 Certification Management (In Progress)

**Milestone Goal:** New CDP menu "Certification Management" — unified table of all certificates (TrainingRecord + AssessmentSession) with expiry status, role-scoped views, filters, and Excel export.

## Phase Details

### Phase 185: ViewModel and Data Model Foundation
**Goal**: The SertifikatRow and CertificationManagementViewModel are defined with correct RecordType discriminator, server-side CertificateStatus derivation, and canonical date mapping for both data sources
**Depends on**: Phase 184
**Requirements**: DATA-01, DATA-02
**Success Criteria** (what must be TRUE):
  1. SertifikatRow has a RecordType discriminator that correctly distinguishes TrainingRecord rows from AssessmentSession rows
  2. CertificateStatus is a pre-computed string property on SertifikatRow — Razor views never derive status from raw dates
  3. TrainingRecord rows without ValidUntil display a defined fallback status (not null-crash)
  4. AssessmentSession rows display "Permanent" status without misinterpreting null ValidUntil as missing data
  5. The ViewModel compiles with zero errors and all nullable fields are handled explicitly
**Plans**: TBD

Plans:
- [ ] 185-01-PLAN.md — Define SertifikatRow, CertificationManagementViewModel, CertificateStatus derivation logic

### Phase 186: Role-Scoped Data Query Helper
**Goal**: BuildSertifikatRowsAsync private helper enforces role-scoped access for all four role tiers using a single allowed-user-ID set applied consistently to both TrainingRecord and AssessmentSession queries
**Depends on**: Phase 185
**Requirements**: ROLE-01, ROLE-02, ROLE-03
**Success Criteria** (what must be TRUE):
  1. Admin and HC calling the helper receive certificate rows for all workers across all units
  2. SectionHead and Sr. Supervisor calling the helper receive only certificate rows for workers in their own section — no cross-section leakage
  3. Coach and Coachee calling the helper receive only their own certificate rows — no traversal of CoachCoacheeMapping
  4. Failed assessment sessions (IsPassed != true or GenerateCertificate != true) never appear in any role's results
**Plans**: TBD

Plans:
- [ ] 186-01-PLAN.md — BuildSertifikatRowsAsync helper with unified role-scoping and dual-source query merge

### Phase 187: Full-Page Controller Action and Static View
**Goal**: Users can navigate to Certification Management from CDP/Index, see summary cards (Total, Aktif, Akan Expired, Expired), and view a full certificate table with visual status highlighting
**Depends on**: Phase 186
**Requirements**: DASH-01, DASH-02, DASH-03, DASH-04
**Success Criteria** (what must be TRUE):
  1. A new card on CDP/Index links to Certification Management and is visible to all authenticated roles
  2. The Certification Management page displays four summary cards showing correct Total, Aktif, Akan Expired, and Expired counts derived from the same filtered list as the table
  3. The table shows worker name, NIP, certificate name, type, status badge, and expiry date for every certificate the current user is authorized to see
  4. Expired rows are visually highlighted in a distinct color (e.g., red tint), and expiring-soon rows in a different distinct color (e.g., yellow tint)
**Plans**: TBD

Plans:
- [ ] 187-01-PLAN.md — CertificationManagement GET action + CertificationManagement.cshtml static table + summary cards + CDP/Index entry card

### Phase 188: AJAX Filter Bar
**Goal**: Users can filter the certificate table by Bagian/Unit cascade, status, type, and free-text search — all filters update the table and summary cards without full page reload
**Depends on**: Phase 187
**Requirements**: FILT-01, FILT-02, FILT-03, FILT-04
**Success Criteria** (what must be TRUE):
  1. Selecting a Bagian in the cascade dropdown populates the Unit dropdown with only that Bagian's units
  2. Selecting a status filter (Aktif / Akan Expired / Expired / Permanent) shows only matching rows
  3. Selecting a type filter (Annual / 3-Year / Permanent) shows only matching rows
  4. Typing in the search box filters rows by worker name or NIP without submitting a form
  5. Summary cards update to reflect the filtered count after any filter change
**Plans**: TBD

Plans:
- [ ] 188-01-PLAN.md — FilterCertificationManagement AJAX action + _CertificationManagementTablePartial + JS filter wiring

### Phase 189: Certificate Actions and Excel Export
**Goal**: Users can view or download individual certificates, and Admin/HC can export the filtered certificate list to Excel
**Depends on**: Phase 188
**Requirements**: ACT-01, ACT-02, ACT-03
**Success Criteria** (what must be TRUE):
  1. Clicking "View" on an online assessment certificate navigates to the CMP/Certificate page for that session
  2. Clicking "View" on a manual training certificate opens or serves the uploaded certificate file
  3. Clicking "Download" on a manual training certificate downloads the certificate file to the user's machine
  4. Admin and HC see an Export Excel button; clicking it downloads an Excel file reflecting the currently applied filters
  5. Workers, Coach, and Coachee do not see the Export Excel button
**Plans**: TBD

Plans:
- [ ] 189-01-PLAN.md — ACT-01/ACT-02 view and download actions + ExportSertifikatExcel ClosedXML action + Export button (role-gated)

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 181. PDF Header Coachee Info | v7.2 | 1/1 | Complete | 2026-03-17 |
| 182. CDP Evidence Column Clarification | v7.2 (loose) | 1/1 | Complete | 2026-03-17 |
| 183. Internal Rename SubCompetency → ElemenTeknis | v7.3 | 1/1 | Complete | 2026-03-17 |
| 184. Shuffle Algorithm Elemen Teknis Distribution | v7.3 | 3/3 | Complete | 2026-03-17 |
| 185. ViewModel and Data Model Foundation | v7.4 | 0/TBD | Not started | - |
| 186. Role-Scoped Data Query Helper | v7.4 | 0/TBD | Not started | - |
| 187. Full-Page Controller Action and Static View | v7.4 | 0/TBD | Not started | - |
| 188. AJAX Filter Bar | v7.4 | 0/TBD | Not started | - |
| 189. Certificate Actions and Excel Export | v7.4 | 0/TBD | Not started | - |
