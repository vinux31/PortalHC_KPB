# Requirements — v22.0 CMP-06 Residual Fix + CMP/Records Enhancement + ManageAssessment/Monitoring Audit

**Milestone:** v22.0
**Started:** 2026-06-04
**Status:** 🚀 ACTIVE
**Phases:** 345 (CMP06R-01..05) · 346 (REC-01..10) · 347 (POL-01..10) · 348 (MAM-01..13) · 349 (MAP-01..23)
**Spec 346/347:** `docs/superpowers/specs/2026-06-04-cmp-records-enhancement-design.md` (audit 7-lens, 37 confirmed)
**Spec 348/349:** `docs/superpowers/specs/2026-06-04-manageassessment-monitoring-audit-design.md` (audit 6×5-lens, 44 confirmed)

## Goal

Assessment dengan `Status="Completed"` + `IsPassed == null` (essay submit, belum dinilai HC) harus tampil **"Menunggu Penilaian"** di SEMUA surface report — bukan "Fail/Failed/Tidak Lulus". Tutup 3 surface yang kelewat Phase 337 CMP-06 + unify label + perbaiki pass-rate stats.

**Sumber:** verifikasi Playwright + code sweep 2026-06-04 (memory `project_cmp06_residual_recordsworkerdetail`). Phase 337 (v20.0) cuma fix `WorkerDataService.GetUnifiedRecords` + `Records.cshtml`.

**Keputusan terkunci (user 2026-06-04):** label = "Menunggu Penilaian" (unified); passRate exclude pending dari denominator.

## Requirements

- [x] **CMP06R-01**: `Views/CMP/RecordsWorkerDetail.cshtml:226-231` binary → 3-way (`null→"Menunggu Penilaian"` badge netral). (Phase 345-01)
- [x] **CMP06R-02**: `UserAssessmentHistory` 3-layer — VM `AssessmentReportItem.IsPassed` `bool`→`bool?` (`Models/ReportsDashboardViewModel.cs`); ctrl `AssessmentAdminController.cs:4737` drop `?? false`; view `UserAssessmentHistory.cshtml:172` 3-way; stats `passedCount`/`passRate` (L4744-4745) exclude pending dari denominator. (Phase 345-02)
- [x] **CMP06R-03**: PDF `GeneratePerPesertaPdf` (`AssessmentAdminController.cs:4620-4621`, `BulkExportPdf` CIL-06) binary "Lulus"/"Tidak Lulus"+merah → 3-way `null→"Menunggu Penilaian"` + warna netral. (Phase 345-03)
- [x] **CMP06R-04**: Label unify — `WorkerDataService.GetUnifiedRecords:51` switch `null→"Menunggu Penilaian"` (ganti "Completed") + `Records.cshtml:188` switch tambah case `"Menunggu Penilaian"`. (Phase 345-01)
- [x] **CMP06R-05**: Regression test — xUnit (VM nullable + passRate exclude-pending) + Playwright UAT 3 surface (RecordsWorkerDetail + UserAssessmentHistory + BulkExportPdf). (Phase 345-04)

### Minor (opportunistic fold-in)
- `CMPController.cs:694` Excel ExportRecords null→"Menunggu Penilaian" (Phase 345-01).
- Grup `PassedCount` (`AssessmentAdminController.cs:2759/2775/2789/2821`) konsisten exclude pending (Phase 345-02).

### Out of scope
- JS SignalR result-cell `AssessmentMonitoringDetail.cshtml:1409` (edge: post-edit sesi pending) — follow-up bila perlu.
- 6 surface verified aman: AssessmentMonitoringDetail status badge (L252-266), _HistoryTab (L95-103), EditPesertaAnswers (L20), CMP/Assessment (L555), CMP/Results (L45), _TrainingRecordsTab (L249).

---

## Requirements — Phase 346 (CMP/Records Detail, Search & Logic Fix)

**Source:** audit 7-lens 2026-06-04 (37 confirmed). **Depends Phase 345** (sequential). No migration.

- [x] **REC-01**: My Records tambah kolom "Aksi" — Assessment→tombol `Lihat Hasil`→`/CMP/Results`, Training→tombol `Detail`→modal; row tetap clickable. **PITFALL:** colspan empty-state 6→7 (`Records.cshtml:227` + JS L381). (FUG-001/FEAT-1A)
- [x] **REC-02**: My Records modal detail training (port dari RecordsWorkerDetail + field Kategori/SubKategori/Status/ValidUntil/CertType + tombol PDF). Data dari `UnifiedTrainingRecord` (no controller change). (FUG-001/FEAT-1A, D-04)
- [x] **REC-03**: Worker Detail row Assessment tambah tombol `Lihat Hasil`→`/CMP/Results` (+returnUrl). (FUG-002/FEAT-1B)
- [x] **REC-04** 🔐: Extend authz `Results`(2169)+`Certificate`(1815)+`CertificatePdf`(1926): `owner ∥ roleLevel≤3 ∥ (roleLevel==4 && Section non-null && assessment.User.Section==user.Section)`. **PITFALL:** Certificate+CertificatePdf wajib `.Include(a=>a.User)`; ketiga panggil GetCurrentUserRoleLevelAsync. Sekalian fix AUTHZ-01 (tombol Sertifikat dead L3/L4). (D-01/D-06)
- [x] **REC-05**: Worker Detail modal training tambah row Kategori + SubKategori. (FEAT-1B training info)
- [x] **REC-06**: Team View search — 1 input + selektor scope (Nama/Training/Keduanya, default Keduanya), server-side. Wire `RecordsTeamPartial`(753)+Export(652/704)+`GetWorkersInSection`(242, tambah `searchScope`; Training=join `TrainingRecords.Judul`; Keduanya=union). updateExportLinks ikut param. (FUG-003/FILTER-001/FILTER-002/FEAT-2, D-05)
- [x] **REC-07**: Include PendingGrading di `GetUnifiedRecords`(31)+`GetAllWorkersHistory`(136). **PITFALL:** pakai `AssessmentConstants.AssessmentStatus.PendingGrading` (bukan literal). **Depends Phase 345** (label "Menunggu Penilaian"). (CMP-LOGIC-02/CMP-FILTER-02)
- [x] **REC-08**: Team View validasi date range — `dateFrom>dateTo`→warning (extend updateDateHint). (FILTER-006)
- [x] **REC-09**: Perjelas makna badge "Assessment" (header/tooltip "Assessment Lulus"). **JANGAN rename** field `CompletedAssessments` (cross-3-file, value LOW). (CMP-FILTER-01, dilunakkan)
- [ ] **REC-10** ~~Worker Detail category filter server-side~~ — **DROP** (over-eng, data 1 pekerja tak paginated). (FILTER-005)

## Requirements — Phase 347 (CMP/Records i18n + a11y Polish)

**Source:** audit 7-lens 2026-06-04 (15 LOW finding). **Depends Phase 346** (sequential); koordinasi POL-01 dgn Phase 345. No migration.

- [x] **POL-01**: Badge `Passed/Failed`→`Lulus/Tidak Lulus` (case true/false; null tetap "Menunggu Penilaian" dari Phase 345). (I18N-002)
- [x] **POL-02**: Header `Score`→`Nilai` (`Records.cshtml:154`). (I18N-003)
- [x] **POL-03**: `Position`→`Jabatan`, `Section`→`@OrgLabels.GetLabel(0)` (RecordsWorkerDetail:66/70 + RecordsTeam:134). (I18N-004/I18N-006)
- [x] **POL-04**: `All Categories/Sub/Types`→`Semua ...` + label opsi tipe konsisten (RecordsWorkerDetail+RecordsTeam). (I18N-001/I18N-008)
- [x] **POL-05**: Subtitle Inggris RecordsWorkerDetail:38 → Indonesia. (I18N-005)
- [x] **POL-06**: a11y modal `aria-labelledby`+`role=dialog`+btn-close `aria-label`. (UI-001/UI-002/UI-012)
- [x] **POL-07**: Label `for=` semua filter (RecordsTeam+RecordsWorkerDetail) + My Records search visible label. (UI-003/UI-005/UI-006/UI-014)
- [x] **POL-08**: DRY — ekstrak `<style>` duplikat 3 view → `wwwroot/css/records.css`. (UI-004)
- [x] **POL-09**: Mobile grid filter responsif RecordsWorkerDetail. (UI-009)
- [x] **POL-10**: `type="button"` reset + label tombol konsisten Lihat/Sertifikat + pagination `aria-current`. (UI-007/UI-010/UI-011/I18N-009)

## Requirements — Phase 348 (ManageAssessment + Monitoring MED Correctness Fix)

**Source:** audit 6×5-lens 2026-06-04 (44 confirmed, 0 HIGH/15 MED/29 LOW). **Depends Phase 347** (sequential; pakai label/konstanta PendingGrading dari 345). No migration. M4 (Tab3 History PendingGrading) **dicakup REC-07/346** — tak diduplikat.

- [x] **MAM-01**: RegenerateToken match by `LinkedGroupId` untuk Pre-Post (`AssessmentAdminController.cs:2616`) → PostTest token ikut regenerate. (logic)
- [x] **MAM-02**: Link Monitoring/Export Pre-Post sadar LinkedGroupId / pecah per-half (`_AssessmentGroupsTab.cshtml:261-285`) → PostTest tak silently di-miss. (logic)
- [x] **MAM-03**: `MenungguPenilaianCount = postSubs.Count(IsMenungguPenilaian)` untuk Pre-Post group di AssessmentMonitoring (`AssessmentAdminController.cs:2749-2796`). (logic; gabung finding list+cross-cut)
- [x] **MAM-04**: Status derivation Detail cek `PendingGrading` SEBELUM `CompletedAt` (`AssessmentAdminController.cs:3229`) → essay-pending tak salah "Completed", CompletedCount/passRate benar. (logic)
- [x] **MAM-05**: SignalR `workerSubmitted` jangan push "Completed"+Pass/Fail prematur untuk essay (`CMPController.cs:1767`). (logic)
- [x] **MAM-06**: `isInitialState` diturunkan dari absennya filter (`AssessmentAdminController.cs:251`) → empty-state hidup, skip full-roster query. **Koord 322-UAT.** (filter)
- [x] **MAM-07**: Tab2 pagination (Skip/Take + kontrol) atau drop param mati `page/pageSize` (`AssessmentAdminController.cs:245`). (filter)
- [x] **MAM-08**: Delete Training/ManualAsm preserve filter context — `hx-post` re-swap (`_TrainingRecordsTab.cshtml:327-349`). (function)
- [x] **MAM-09**: Filter Status Tab2 relabel "Status Training" / lipat passed assessment (`_TrainingRecordsTab.cshtml:107-125` + `WorkerDataService.cs:390`). (ui-design)
- [x] **MAM-10**: Badge status baris Tab1 bind `@group.GroupStatus` + case "Closed" (`_AssessmentGroupsTab.cshtml:195-221`). (logic; gabung MED cross-cut + LOW twin)
- [x] **MAM-11**: Dropdown Kategori Monitoring data-driven dari `AssessmentCategories` + buang "Proton" phantom (`AssessmentMonitoring.cshtml:125-148`). (filter)
- [x] **MAM-12**: Tooltip Closed jujur — buang "lokasi" (`AssessmentMonitoring.cshtml:169`). (content)
- [x] **MAM-13**: Selector tombol Reshuffle scoped, jangan bentrok `<tr>` (`AssessmentMonitoringDetail.cshtml:739`). (function)

## Requirements — Phase 349 (ManageAssessment + Monitoring LOW Polish)

**Source:** audit 6×5-lens 2026-06-04 (29 LOW). **Depends Phase 348** (sequential, file sama). D-02: SEMUA 29 LOW masuk. No migration.

- [x] **MAP-01**: i18n Monitoring Detail chrome Inggris→Indonesia (header tabel/kartu/back/export). (`AssessmentMonitoringDetail.cshtml`)
- [x] **MAP-02**: Konsistenkan "NIP" vs "Nopeg" History sub-tab → "NIP". (`_HistoryTab.cshtml`)
- [x] **MAP-03**: Chevron+aria-label toggle collapse Tab1 peserta + Tab2 expand-records.
- [x] **MAP-04**: Tab3 drill-down hilangkan ARIA nested-interactive (pilih 1 affordance). (`_HistoryTab.cshtml:78-118`)
- [x] **MAP-05**: Tab1 empty-state filter-aware (kategori/status aktif → bukan "Buat assessment pertama"). (`_AssessmentGroupsTab.cshtml:133-156`)
- [x] **MAP-06**: Tab1 "Hapus Pencarian" clear-search-only atau relabel. (`_AssessmentGroupsTab.cshtml:143-146`)
- [x] **MAP-07**: Tab3 client-filter 0-match → pesan "Tidak ada hasil" +`aria-live`. (`ManageAssessment.cshtml:316-337`)
- [x] **MAP-08**: Tab3 badge count ikut filter / "Menampilkan X dari Y". (`_HistoryTab.cshtml:13,20`)
- [x] **MAP-09**: Skeleton loader match kolom asli Tab2 + History. (`ManageAssessment.cshtml:146-199`)
- [x] **MAP-10**: Monitor Detail summary tambah "Abandoned" (sum=Total) + sync JS. (`AssessmentMonitoringDetail.cshtml:146-177,1280`)
- [x] **MAP-11**: Monitor Detail "In Progress" pakai `Model.InProgressCount`; dead var `completedPct`/`passRatePct` buang/surface. (`AssessmentMonitoringDetail.cshtml:33-39,161`)
- [x] **MAP-12**: Monitor Detail "Akhiri Semua Ujian" conditional render + modal wording predikat-identik. (`AssessmentMonitoringDetail.cshtml:196,542`)
- [x] **MAP-13**: Monitor list `TotalCount` exclude Cancelled → progress bisa 100%. (`AssessmentAdminController.cs:2819,2757`)
- [x] **MAP-14**: Monitor list subtitle buang "real-time". (`AssessmentMonitoring.cshtml:27`)
- [x] **MAP-15**: Monitor list dropdown Status saat search jangan misrepresent. (`AssessmentMonitoring.cshtml:12,80-87`)
- [x] **MAP-16**: Monitor list buang kategori dobel (subtitle vs badge). (`AssessmentMonitoring.cshtml:257-273`)
- [x] **MAP-17**: Monitor list Pre-Post token-required → render View Detail / Regenerate (koord MAM-01). (`AssessmentMonitoring.cshtml:298-327`)
- [x] **MAP-18**: Tab2 Detail manual-assessment tri-state `null→"Menunggu Penilaian"`. (`_TrainingRecordsTab.cshtml:249`)
- [x] **MAP-19**: Tab2 "Belum ada" badge gated combined count / selalu `CompletionDisplayText`. (`_TrainingRecordsTab.cshtml:222-229`)
- [x] **MAP-20**: Tab3 History cell Pass/Fail badge "Menunggu Penilaian" (depends REC-07/346). (`_HistoryTab.cshtml:102-103`)
- [x] **MAP-21**: Pagination Tab1 drop magic-number `20` → `paging.Take` via ViewBag. (`_AssessmentGroupsTab.cshtml:16,180,354`) (gabung L4+L28)
- [x] **MAP-22**: History/Training drop param mati `pageSize/statusFilter`/no-op `page`. (`AssessmentAdminController.cs:245-305`) (gabung L10+L29)
- [x] **MAP-23**: (opsional) Extend search Monitoring ke Category untuk parity. (`AssessmentAdminController.cs:2685-2689`)

## Coverage Validation

| REQ | Phase | Status |
|-----|-------|--------|
| CMP06R-01 | 345-01 | Complete |
| CMP06R-02 | 345-02 | Complete |
| CMP06R-03 | 345-03 | Complete |
| CMP06R-04 | 345-01 | Complete |
| CMP06R-05 | 345-04 | Complete |
| REC-01..09 | 346 | Pending (plan belum di-generate) |
| REC-10 | — | DROPPED (over-eng) |
| POL-01..10 | 347 | Pending (plan belum di-generate) |
| MAM-01..13 | 348 | Pending (plan belum di-generate) |
| MAP-01..23 | 349 | Pending (plan belum di-generate) |

**Active mapped: 60/60 ✓ (5 CMP06R + 9 REC + 10 POL + 13 MAM + 23 MAP) — Orphans: 0 — Duplicates: 0 — REC-10 dropped — M4 dedup→REC-07/346 — No migration, no schema change.**
