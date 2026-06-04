# Phase 345: assessment-pending-grade-display-fix - Context

**Gathered:** 2026-06-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Sesi assessment dengan `Status="Completed"` + `IsPassed == null` (essay submit, bagian auto-graded selesai, HC belum nilai) harus tampil **"Menunggu Penilaian"** — bukan "Fail/Failed/Tidak Lulus" — di 3 surface yang kelewat Phase 337 CMP-06: `/CMP/RecordsWorkerDetail`, `/Admin/UserAssessmentHistory`, dan PDF `BulkExportPdf`. Plus unify label via `GetUnifiedRecords` + `Records.cshtml`, dan perbaiki `passRate` stats (exclude pending dari denominator). Tambah regression test (xUnit + Playwright). **No migration.**

Phase ini koreksi tampilan/perhitungan — BUKAN menambah kapabilitas baru.
</domain>

<decisions>
## Implementation Decisions

### Badge / Visual
- **D-01:** Badge "Menunggu Penilaian" = **AMBER**. Web: `bg-warning text-dark`. PDF (QuestPDF): `Colors.Orange.Darken2`. Konsisten di SEMUA 4 surface (RecordsWorkerDetail, Records, UserAssessmentHistory view, BulkExportPdf). Beda jelas dari Failed (merah/`bg-danger`) & dash kosong (`text-muted` abu). *(User: "sesuai reko kamu".)*

### Label / Konstanta
- **D-02:** Gunakan konstanta `AssessmentConstants.AssessmentStatus.PendingGrading` (= "Menunggu Penilaian", `Models/AssessmentConstants.cs:18`) di SEMUA surface C# (controller/service/PDF) — JANGAN literal string. Selaras pitfall Phase 346 (REC-07 wajib pakai konstanta). Di Razor `.cshtml`: utamakan referensi konstanta (mis. `@HcPortal.Models.AssessmentConstants.AssessmentStatus.PendingGrading`); literal boleh dengan komentar referensi bila tidak praktis — mekanisme final diserahkan ke planner.

### 3-way Mapping
- **D-03:** Mapping berbasis `IsPassed` (`bool?`): `true`→"Passed" hijau, `false`→"Failed" merah, `null`→"Menunggu Penilaian" amber. Target = sesi `Status="Completed"` + `IsPassed==null`. **CATATAN:** sesi ber-`Status="Menunggu Penilaian"` (PendingGrading murni) saat ini di-exclude oleh filter `Status=="Completed"` di `GetUnifiedRecords` — itu cakupan **Phase 346 REC-07**, BUKAN 345. Template 3-way sudah ada di `Records.cshtml:182-192` → pakai sebagai pola untuk `RecordsWorkerDetail.cshtml:226-231`.

### Stats UserAssessmentHistory (Claude's Discretion — user "kamu putuskan")
- **D-04:** `passedCount` = `IsPassed == true`; `passRate` denominator = **graded only** (`IsPassed != null`), bukan total. *(Terkunci: passRate exclude pending.)*
- **D-05:** All-pending edge (gradedCount == 0) → `passRate` tampil "—" / "Belum ada penilaian" (hindari "0%" yang menyesatkan).
- **D-06:** Surface pending: tampilkan indikator ringan "Menunggu Penilaian: N" di area kartu stat (reuse styling kartu/badge existing — bukan kartu besar baru) agar HC sadar ada yang belum dinilai.
- **D-07 (discretion, planner konfirmasi):** `averageScore` — rekomendasi exclude pending juga (skor belum final); default aman tetap atas graded sessions. Planner putuskan saat plan 345-02.
- **D-08:** VM ripple — `AssessmentReportItem.IsPassed` `bool`→`bool?` (`Models/ReportsDashboardViewModel.cs`); ctrl drop `?? false` (`AssessmentAdminController.cs:4737`); view 3-way (`UserAssessmentHistory.cshtml:172`). Build harus 0 error setelah nullable ripple.

### Fold-in Scope (user: "ikutkan semua sekarang")
- **D-09:** Include Excel `CMPController.cs:694` ExportRecords `null`→"Menunggu Penilaian".
- **D-10:** Include konsistensi grup `PassedCount` (`AssessmentAdminController.cs:2759/2775/2789/2821`) — pastikan exclude pending. **CATATAN scout:** `MenungguPenilaianCount` SUDAH ada untuk standard group (L2825) via computed `IsMenungguPenilaian` (L2714); PrePost sub-rows belum punya. Researcher cek apakah `a.IsPassed` di grup = computed `bool` (sudah exclude `null`) atau perlu guard. **Jangan tambah kapabilitas baru** — hanya konsistensi hitung. Bila menambah `MenungguPenilaianCount` ke PrePost rows dianggap melebar, defer ke Phase 348 (MAM).

### Test (CMP06R-05)
- **D-11:** xUnit — (a) VM nullable mapping (`null`→pending, tidak jadi Failed), (b) passRate exclude-pending math (termasuk all-pending → 0/—). Playwright UAT 3 surface (RecordsWorkerDetail + UserAssessmentHistory + BulkExportPdf). **SEED_WORKFLOW:** snapshot DB lokal sebelum seed sesi `Completed`+`IsPassed==null`, restore sesudah (sukses atau gagal), tandai journal `cleaned`.

### Plan split (pre-locked di ROADMAP)
- 345-01: CMP06R-01 + CMP06R-04 + MINOR Excel (RecordsWorkerDetail 3-way + GetUnifiedRecords label + Records.cshtml switch + CMPController:694)
- 345-02: CMP06R-02 + grup PassedCount (VM bool? + ctrl drop `?? false` + view 3-way + stats exclude-pending)
- 345-03: CMP06R-03 (GeneratePerPesertaPdf 3-way + warna netral)
- 345-04: CMP06R-05 (xUnit + Playwright UAT)
- **Wave:** 345-01 ∥ 345-02 ∥ 345-03 (region independen) → 345-04 (test, depends all)

### Claude's Discretion
- D-05/D-06/D-07 (tampilan stats + averageScore) — Claude pilih default sesuai pola kartu existing; planner boleh refine.
- Mekanisme akses konstanta di Razor (D-02).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Requirements & Roadmap
- `.planning/REQUIREMENTS.md` — CMP06R-01..05 + Minor fold-in + Out of scope (baris 18-32)
- `.planning/ROADMAP.md` — §Phase 345 (baris 734-753): goal, 5 SC, plan split, files affected, wave

### Kode wajib-reuse
- `Models/AssessmentConstants.cs:18` — `AssessmentStatus.PendingGrading = "Menunggu Penilaian"` (WAJIB pakai konstanta, bukan literal); helper `IsSubmitted` (L77-79)
- `Controllers/AssessmentAdminController.cs:2714` — computed `IsMenungguPenilaian`; L2825 `MenungguPenilaianCount` (pola existing utk standard group)

### Spec sekuens (konteks dependency)
- `docs/superpowers/specs/2026-06-04-cmp-records-enhancement-design.md` — Spec Phase 346/347; REC-07 (include PendingGrading di GetUnifiedRecords) depends label dari 345. Baca untuk hindari konflik baris berdekatan di `Records.cshtml`/`RecordsWorkerDetail.cshtml`.

### Di luar repo (referensi keputusan)
- Memory `project_cmp06_residual_recordsworkerdetail` — detail 3 miss (RecordsWorkerDetail:226-231, UserAssessmentHistory, BulkExportPdf) + 2 minor + 6 surface aman.

**Tidak ada design spec khusus Phase 345** — sumber = verifikasi Playwright + code sweep 2026-06-04. Keputusan terkunci ada di REQUIREMENTS/ROADMAP + CONTEXT ini.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AssessmentConstants.AssessmentStatus.PendingGrading` — konstanta label tunggal; reuse di service/ctrl/PDF.
- `Records.cshtml:182-192` — 3-way switch `IsPassed` (true/false/else→Status switch) SUDAH ada; pola untuk port ke `RecordsWorkerDetail.cshtml:226-231` (yang masih binary).
- Computed `IsMenungguPenilaian` (ctrl L2714) + `MenungguPenilaianCount` (L2825) — pola hitung pending untuk grup.

### Established Patterns
- Web badge: Bootstrap `bg-success`/`bg-danger`/`bg-warning text-dark`/`bg-secondary`/`bg-info`.
- PDF: QuestPDF `Colors.Green.Darken2`/`Colors.Red.Darken2` → tambah `Colors.Orange.Darken2` untuk pending.
- Status switch di `GetUnifiedRecords:52-57` saat ini map `null=>"Completed"` → ganti `null=>PendingGrading`.

### Integration Points
- `WorkerDataService.GetUnifiedRecords` (service) → feed `Records.cshtml` + `RecordsWorkerDetail.cshtml`.
- `UserAssessmentHistory` (controller + `ReportsDashboardViewModel.AssessmentReportItem` VM + `UserAssessmentHistory.cshtml` view) — standalone.
- `BulkExportPdf`/`GeneratePerPesertaPdf` (`AssessmentAdminController.cs:4620-4621`) — PDF QuestPDF.
- `CMPController.cs:694` — Excel ExportRecords.
</code_context>

<specifics>
## Specific Ideas

- Filter `GetUnifiedRecords` saat ini hanya `Status=="Completed"` (L33) → sesi pending masuk via IsPassed==null pada sesi Completed; inklusi sesi ber-Status "Menunggu Penilaian" murni = Phase 346 (jangan kerjakan di 345).
- averageScore atas sesi pending = skor parsial (auto-graded MCQ saja) → bisa skew; itu dasar D-07 rekomendasi exclude.
</specifics>

<deferred>
## Deferred Ideas

- **JS SignalR result-cell** `AssessmentMonitoringDetail.cshtml:1409` (edge post-edit sesi pending) — out of scope, follow-up bila perlu (REQUIREMENTS §Out of scope).
- **Inklusi sesi `Status="Menunggu Penilaian"` murni** di `GetUnifiedRecords`/`GetAllWorkersHistory` → Phase 346 REC-07 (sequential, depends label 345).
- **`MenungguPenilaianCount` untuk PrePost sub-rows** Monitoring — bila dianggap melebar dari "konsistensi hitung", defer ke Phase 348 (MAM).
- 6 surface sudah verified aman (AssessmentMonitoringDetail badge L252-266, _HistoryTab L95-103, EditPesertaAnswers L20, CMP/Assessment L555, CMP/Results L45, _TrainingRecordsTab L249) — tidak disentuh.

</deferred>

---

*Phase: 345-assessment-pending-grade-display-fix*
*Context gathered: 2026-06-04*
