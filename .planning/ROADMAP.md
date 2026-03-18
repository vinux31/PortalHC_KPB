# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0 – v5.0** - Phases 1–172 (shipped 2026-03-16)
- ⚠️ **v6.0 Deployment Preparation** - Phases 173–174 (closed 2026-03-16, no work executed)
- ✅ **v7.0 Assessment Terminology Fix** - Phase 175 (shipped 2026-03-16)
- ✅ **v7.1 Export & Import Data** - Phases 176–180 (shipped 2026-03-16)
- ✅ **v7.2 PDF Evidence Report Enhancement** - Phase 181 (shipped 2026-03-17)
- ✅ **v7.2 (loose)** - Phase 182 (shipped 2026-03-17)
- ✅ **v7.3 Elemen Teknis Shuffle & Rename** - Phases 183–184 (shipped 2026-03-17)
- ✅ **v7.5 Assessment Form Revamp & Certificate Enhancement** - Phases 190–195 (shipped 2026-03-18)
- ✅ **v7.6 Code Deduplication & Shared Services** - Phases 196–199 (shipped 2026-03-18)
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

<details>
<summary>✅ v7.5 Assessment Form Revamp & Certificate Enhancement (Phases 190–195) — SHIPPED 2026-03-18</summary>

- [x] Phase 190: DB Categories Foundation (2/2 plans) — completed 2026-03-17
- [x] Phase 191: Wizard UI (2/2 plans) — completed 2026-03-17
- [x] Phase 192: ValidUntil & NomorSertifikat (1/1 plans) — completed 2026-03-17
- ~~Phase 193: Clone Assessment~~ — Removed
- [x] Phase 194: PDF Certificate Download (1/1 plans) — completed 2026-03-17
- [x] Phase 195: Sub-Categories & Signatory Settings (3/3 plans) — completed 2026-03-18

</details>

<details>
<summary>✅ v7.6 Code Deduplication & Shared Services (Phases 196–199) — SHIPPED 2026-03-18</summary>

- [x] Phase 196: Shared Service Extraction (2/2 plans) — completed 2026-03-18
- [x] Phase 197: Excel Export Helper (1/1 plans) — completed 2026-03-18
- [x] Phase 198: CRUD Consolidation (1/1 plans) — completed 2026-03-18
- [x] Phase 199: Code Pattern Extraction (2/2 plans) — completed 2026-03-18

</details>

---

### 🚧 v7.4 Certification Management (In Progress)

**Milestone Goal:** New CDP menu "Certification Management" — unified table of all certificates (TrainingRecord + AssessmentSession) with expiry status, role-scoped views, filters, and Excel export.

**Updated 2026-03-18:** Disesuaikan setelah v7.5 & v7.6 shipped — memanfaatkan field baru (ValidUntil, NomorSertifikat) dan shared helpers (ExcelExportHelper, PaginationHelper, GetCurrentUserRoleLevelAsync pattern).

### Data Sources

| Source | Certificate Fields | Expiry | Nomor |
|--------|-------------------|--------|-------|
| **TrainingRecord** | SertifikatUrl, ValidUntil, CertificateType | ✅ ValidUntil + CertificateType (Permanent/Annual/3-Year) | NomorSertifikat |
| **AssessmentSession** | GenerateCertificate, ValidUntil | ✅ ValidUntil (nullable = no expiry) | NomorSertifikat (auto KPB/{SEQ}/{ROMAN}/{YEAR}) |

### Shared Infrastructure (dari v7.5/v7.6)

- `Helpers/ExcelExportHelper.cs` — CreateSheet() + ToFileResult() → Phase 189
- `Helpers/PaginationHelper.cs` — Calculate() → Phase 187/188
- `CMPController.GetCurrentUserRoleLevelAsync()` — role-scoping pattern → Phase 186
- `Helpers/FileUploadHelper.cs` — jika perlu upload bukti

## Phase Details

### Phase 185: ViewModel and Data Model Foundation
**Goal**: SertifikatRow dan CertificationManagementViewModel didefinisikan dengan RecordType discriminator (Training/Assessment), CertificateStatus derivation dari ValidUntil, dan mapping canonical dari kedua data source
**Depends on**: Phase 195 (AssessmentSession.ValidUntil & NomorSertifikat sudah ada)
**Requirements**: DATA-01, DATA-02
**Plans**: 1 plan
**Notes**:
- TrainingRecord sudah punya: ValidUntil, NomorSertifikat, CertificateType, SertifikatUrl, IsExpiringSoon (computed)
- AssessmentSession sudah punya: ValidUntil, NomorSertifikat, GenerateCertificate, IsPassed
- CertificateStatus enum: Aktif, AkanExpired (≤30 hari), Expired, TidakAdaExpiry (permanent)
- Tidak perlu migration — semua field sudah ada di DB

Plans:
- [ ] 185-01-PLAN.md — Define SertifikatRow, CertificationManagementViewModel, CertificateStatus derivation logic

### Phase 186: Role-Scoped Data Query Helper
**Goal**: BuildSertifikatRowsAsync helper di CDPController yang menggabungkan TrainingRecord + AssessmentSession dengan role-scoped access mengikuti pattern GetCurrentUserRoleLevelAsync() dari v7.6
**Depends on**: Phase 185
**Requirements**: ROLE-01, ROLE-02, ROLE-03
**Plans**: TBD
**Notes**:
- Ikuti pattern CMPController.GetCurrentUserRoleLevelAsync() (L1-3 full, L4 section, L5 coach assignments, L6 own data)
- Gunakan UserRoles.GetRoleLevel() yang sudah ada
- Query kedua tabel lalu merge ke List<SertifikatRow>

Plans:
- [ ] 186-01-PLAN.md — BuildSertifikatRowsAsync helper with unified role-scoping and dual-source query merge

### Phase 187: Full-Page Controller Action and Static View
**Goal**: User bisa navigasi ke Certification Management dari CDP/Index, melihat summary cards (Total, Aktif, Akan Expired, Expired), dan tabel sertifikat dengan status highlighting + pagination
**Depends on**: Phase 186
**Requirements**: DASH-01, DASH-02, DASH-03, DASH-04
**Plans**: TBD
**Notes**:
- Gunakan PaginationHelper.Calculate() untuk pagination tabel
- Summary cards dihitung dari full dataset sebelum pagination
- Status badge: hijau (Aktif), kuning (Akan Expired), merah (Expired), abu-abu (Permanent)

Plans:
- [ ] 187-01-PLAN.md — CertificationManagement GET action + View + summary cards + CDP/Index entry card + PaginationHelper

### Phase 188: AJAX Filter Bar
**Goal**: Filter tabel sertifikat by Bagian/Unit cascade, status, tipe (Training/Assessment), dan free-text search — semua filter update tabel + summary cards via AJAX tanpa reload
**Depends on**: Phase 187
**Requirements**: FILT-01, FILT-02, FILT-03, FILT-04
**Plans**: TBD
**Notes**:
- Ikuti pattern AJAX yang sudah ada di CDPController (partial view return)
- PaginationHelper tetap dipakai di AJAX response

Plans:
- [ ] 188-01-PLAN.md — FilterCertificationManagement AJAX action + _CertificationManagementTablePartial + JS filter wiring

### Phase 189: Certificate Actions and Excel Export
**Goal**: User bisa lihat/download sertifikat individual, Admin/HC bisa export filtered list ke Excel menggunakan ExcelExportHelper
**Depends on**: Phase 188
**Requirements**: ACT-01, ACT-02, ACT-03
**Plans**: TBD
**Notes**:
- View/download: TrainingRecord → redirect ke SertifikatUrl, AssessmentSession → redirect ke CMP/CertificatePdf
- Export: gunakan ExcelExportHelper.CreateSheet() + ToFileResult() (bukan boilerplate ClosedXML manual)
- Export button hanya tampil untuk Admin/HC (role-gated)

Plans:
- [ ] 189-01-PLAN.md — View/download actions + ExportSertifikatExcel via ExcelExportHelper + Export button (role-gated)

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 185. ViewModel and Data Model Foundation | 1/1 | Complete    | 2026-03-18 | - |
| 186. Role-Scoped Data Query Helper | v7.4 | 0/TBD | Not started | - |
| 187. Full-Page Controller Action and Static View | v7.4 | 0/TBD | Not started | - |
| 188. AJAX Filter Bar | v7.4 | 0/TBD | Not started | - |
| 189. Certificate Actions and Excel Export | v7.4 | 0/TBD | Not started | - |
