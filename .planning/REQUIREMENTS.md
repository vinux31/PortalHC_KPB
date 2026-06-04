# Requirements — v22.0 CMP-06 Residual Fix + CMP/Records Enhancement

**Milestone:** v22.0
**Started:** 2026-06-04
**Status:** 🚀 ACTIVE
**Phases:** 345 (CMP06R-01..05) · 346 (REC-01..10) · 347 (POL-01..10)
**Spec 346/347:** `docs/superpowers/specs/2026-06-04-cmp-records-enhancement-design.md` (audit 7-lens, 37 confirmed)

## Goal

Assessment dengan `Status="Completed"` + `IsPassed == null` (essay submit, belum dinilai HC) harus tampil **"Menunggu Penilaian"** di SEMUA surface report — bukan "Fail/Failed/Tidak Lulus". Tutup 3 surface yang kelewat Phase 337 CMP-06 + unify label + perbaiki pass-rate stats.

**Sumber:** verifikasi Playwright + code sweep 2026-06-04 (memory `project_cmp06_residual_recordsworkerdetail`). Phase 337 (v20.0) cuma fix `WorkerDataService.GetUnifiedRecords` + `Records.cshtml`.

**Keputusan terkunci (user 2026-06-04):** label = "Menunggu Penilaian" (unified); passRate exclude pending dari denominator.

## Requirements

- [ ] **CMP06R-01**: `Views/CMP/RecordsWorkerDetail.cshtml:226-231` binary → 3-way (`null→"Menunggu Penilaian"` badge netral). (Phase 345-01)
- [ ] **CMP06R-02**: `UserAssessmentHistory` 3-layer — VM `AssessmentReportItem.IsPassed` `bool`→`bool?` (`Models/ReportsDashboardViewModel.cs`); ctrl `AssessmentAdminController.cs:4737` drop `?? false`; view `UserAssessmentHistory.cshtml:172` 3-way; stats `passedCount`/`passRate` (L4744-4745) exclude pending dari denominator. (Phase 345-02)
- [ ] **CMP06R-03**: PDF `GeneratePerPesertaPdf` (`AssessmentAdminController.cs:4620-4621`, `BulkExportPdf` CIL-06) binary "Lulus"/"Tidak Lulus"+merah → 3-way `null→"Menunggu Penilaian"` + warna netral. (Phase 345-03)
- [ ] **CMP06R-04**: Label unify — `WorkerDataService.GetUnifiedRecords:51` switch `null→"Menunggu Penilaian"` (ganti "Completed") + `Records.cshtml:188` switch tambah case `"Menunggu Penilaian"`. (Phase 345-01)
- [ ] **CMP06R-05**: Regression test — xUnit (VM nullable + passRate exclude-pending) + Playwright UAT 3 surface (RecordsWorkerDetail + UserAssessmentHistory + BulkExportPdf). (Phase 345-04)

### Minor (opportunistic fold-in)
- `CMPController.cs:694` Excel ExportRecords null→"Menunggu Penilaian" (Phase 345-01).
- Grup `PassedCount` (`AssessmentAdminController.cs:2759/2775/2789/2821`) konsisten exclude pending (Phase 345-02).

### Out of scope
- JS SignalR result-cell `AssessmentMonitoringDetail.cshtml:1409` (edge: post-edit sesi pending) — follow-up bila perlu.
- 6 surface verified aman: AssessmentMonitoringDetail status badge (L252-266), _HistoryTab (L95-103), EditPesertaAnswers (L20), CMP/Assessment (L555), CMP/Results (L45), _TrainingRecordsTab (L249).

---

## Requirements — Phase 346 (CMP/Records Detail, Search & Logic Fix)

**Source:** audit 7-lens 2026-06-04 (37 confirmed). **Depends Phase 345** (sequential). No migration.

- [ ] **REC-01**: My Records tambah kolom "Aksi" — Assessment→tombol `Lihat Hasil`→`/CMP/Results`, Training→tombol `Detail`→modal; row tetap clickable. **PITFALL:** colspan empty-state 6→7 (`Records.cshtml:227` + JS L381). (FUG-001/FEAT-1A)
- [ ] **REC-02**: My Records modal detail training (port dari RecordsWorkerDetail + field Kategori/SubKategori/Status/ValidUntil/CertType + tombol PDF). Data dari `UnifiedTrainingRecord` (no controller change). (FUG-001/FEAT-1A, D-04)
- [ ] **REC-03**: Worker Detail row Assessment tambah tombol `Lihat Hasil`→`/CMP/Results` (+returnUrl). (FUG-002/FEAT-1B)
- [ ] **REC-04** 🔐: Extend authz `Results`(2169)+`Certificate`(1815)+`CertificatePdf`(1926): `owner ∥ roleLevel≤3 ∥ (roleLevel==4 && Section non-null && assessment.User.Section==user.Section)`. **PITFALL:** Certificate+CertificatePdf wajib `.Include(a=>a.User)`; ketiga panggil GetCurrentUserRoleLevelAsync. Sekalian fix AUTHZ-01 (tombol Sertifikat dead L3/L4). (D-01/D-06)
- [ ] **REC-05**: Worker Detail modal training tambah row Kategori + SubKategori. (FEAT-1B training info)
- [ ] **REC-06**: Team View search — 1 input + selektor scope (Nama/Training/Keduanya, default Keduanya), server-side. Wire `RecordsTeamPartial`(753)+Export(652/704)+`GetWorkersInSection`(242, tambah `searchScope`; Training=join `TrainingRecords.Judul`; Keduanya=union). updateExportLinks ikut param. (FUG-003/FILTER-001/FILTER-002/FEAT-2, D-05)
- [ ] **REC-07**: Include PendingGrading di `GetUnifiedRecords`(31)+`GetAllWorkersHistory`(136). **PITFALL:** pakai `AssessmentConstants.AssessmentStatus.PendingGrading` (bukan literal). **Depends Phase 345** (label "Menunggu Penilaian"). (CMP-LOGIC-02/CMP-FILTER-02)
- [ ] **REC-08**: Team View validasi date range — `dateFrom>dateTo`→warning (extend updateDateHint). (FILTER-006)
- [ ] **REC-09**: Perjelas makna badge "Assessment" (header/tooltip "Assessment Lulus"). **JANGAN rename** field `CompletedAssessments` (cross-3-file, value LOW). (CMP-FILTER-01, dilunakkan)
- [ ] **REC-10** ~~Worker Detail category filter server-side~~ — **DROP** (over-eng, data 1 pekerja tak paginated). (FILTER-005)

## Requirements — Phase 347 (CMP/Records i18n + a11y Polish)

**Source:** audit 7-lens 2026-06-04 (15 LOW finding). **Depends Phase 346** (sequential); koordinasi POL-01 dgn Phase 345. No migration.

- [ ] **POL-01**: Badge `Passed/Failed`→`Lulus/Tidak Lulus` (case true/false; null tetap "Menunggu Penilaian" dari Phase 345). (I18N-002)
- [ ] **POL-02**: Header `Score`→`Nilai` (`Records.cshtml:154`). (I18N-003)
- [ ] **POL-03**: `Position`→`Jabatan`, `Section`→`@OrgLabels.GetLabel(0)` (RecordsWorkerDetail:66/70 + RecordsTeam:134). (I18N-004/I18N-006)
- [ ] **POL-04**: `All Categories/Sub/Types`→`Semua ...` + label opsi tipe konsisten (RecordsWorkerDetail+RecordsTeam). (I18N-001/I18N-008)
- [ ] **POL-05**: Subtitle Inggris RecordsWorkerDetail:38 → Indonesia. (I18N-005)
- [ ] **POL-06**: a11y modal `aria-labelledby`+`role=dialog`+btn-close `aria-label`. (UI-001/UI-002/UI-012)
- [ ] **POL-07**: Label `for=` semua filter (RecordsTeam+RecordsWorkerDetail) + My Records search visible label. (UI-003/UI-005/UI-006/UI-014)
- [ ] **POL-08**: DRY — ekstrak `<style>` duplikat 3 view → `wwwroot/css/records.css`. (UI-004)
- [ ] **POL-09**: Mobile grid filter responsif RecordsWorkerDetail. (UI-009)
- [ ] **POL-10**: `type="button"` reset + label tombol konsisten Lihat/Sertifikat + pagination `aria-current`. (UI-007/UI-010/UI-011/I18N-009)

## Coverage Validation

| REQ | Phase | Status |
|-----|-------|--------|
| CMP06R-01 | 345-01 | Pending |
| CMP06R-02 | 345-02 | Pending |
| CMP06R-03 | 345-03 | Pending |
| CMP06R-04 | 345-01 | Pending |
| CMP06R-05 | 345-04 | Pending |
| REC-01..09 | 346 | Pending (plan belum di-generate) |
| REC-10 | — | DROPPED (over-eng) |
| POL-01..10 | 347 | Pending (plan belum di-generate) |

**Active mapped: 24/24 ✓ (5 CMP06R + 9 REC + 10 POL) — Orphans: 0 — Duplicates: 0 — REC-10 dropped — No migration, no schema change.**
