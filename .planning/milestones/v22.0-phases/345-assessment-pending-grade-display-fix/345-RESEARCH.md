# Phase 345: assessment-pending-grade-display-fix - Research

**Researched:** 2026-06-04
**Domain:** ASP.NET Core MVC display-correctness fix (Razor views + controller stats + QuestPDF + xUnit/Playwright)
**Confidence:** HIGH (semua jawaban dari grep/read codebase langsung — file:line citation, bukan asumsi)

## Summary

Phase 345 adalah perbaikan tampilan/perhitungan murni (no migration, no new capability). Bug akar: sesi `Status="Completed"` + `IsPassed==null` (essay submit, MCQ auto-graded, HC belum nilai) di-render sebagai "Fail/Failed/Tidak Lulus" merah di 3 surface yang kelewat Phase 337. Riset ini mengonfirmasi **ke-7 open question dengan citation eksak** dan menemukan **3 koreksi terhadap dokumen upstream** yang WAJIB planner ketahui sebelum menulis plan (lihat `## Critical Corrections` — jangan dilewati).

Tipe data sudah ramah: `AssessmentSession.IsPassed` = `bool?`, `UnifiedTrainingRecord.IsPassed` = `bool?`, `AllWorkersHistoryRow.IsPassed` = `bool?`. Hanya **satu** VM yang masih `bool` non-nullable (`AssessmentReportItem.IsPassed`) → itulah ripple D-08. Konstanta `AssessmentConstants.AssessmentStatus.PendingGrading` langsung accessible di Razor karena `_ViewImports.cshtml` sudah `@using HcPortal.Models`. QuestPDF versi `2026.2.2` → `Colors.Orange.Darken2` valid.

**Primary recommendation:** Plan persis sesuai split yang sudah locked (01∥02∥03 → 04). Surface paling "deep" = 345-02 (nullable ripple `AssessmentReportItem` lintas ctrl+VM+view+stats). Surface paling sepele = Excel (D-09: ubah `""` → konstanta, 1 baris). Untuk test, math statistik harus **diekstrak ke helper static testable** karena saat ini inline di action (tidak unit-testable in-place tanpa spin-up controller penuh).

---

## Critical Corrections (planner WAJIB baca — dokumen upstream salah di 3 titik)

| # | Klaim di CONTEXT/REQUIREMENTS | Realita di kode | Dampak ke plan |
|---|------------------------------|-----------------|----------------|
| **C-1** | D-08 + REQUIREMENTS: `AssessmentReportItem.IsPassed` ada di `Models/ReportsDashboardViewModel.cs` | `AssessmentReportItem` class sebenarnya di **`Models/CDPDashboardViewModel.cs:100-113`** (field `bool IsPassed` di L111). `Models/ReportsDashboardViewModel.cs` HANYA berisi `UserAssessmentHistoryViewModel` (tidak ada `AssessmentReportItem`). [VERIFIED: grep `class AssessmentReportItem`] | Plan 345-02 harus edit `Models/CDPDashboardViewModel.cs:111`, BUKAN `ReportsDashboardViewModel.cs`. Kalau planner tulis path salah, task action gagal cari target. |
| **C-2** | Implikasi "RecordsWorkerDetail & UserAssessmentHistory render pending sebagai Failed" disamakan dengan "Records.cshtml juga salah" | `Records.cshtml:182-192` SUDAH 3-way aman: `null` jatuh ke branch `else if Status` → tampil "Completed" badge `bg-info` (BUKAN "Failed"). [VERIFIED: read Records.cshtml:182-192 + WorkerDataService.cs:52-57 map `null=>"Completed"`] | CMP06R-04 BUKAN bug-fix "Failed→pending"; ini **label-unify** (ganti teks "Completed"→"Menunggu Penilaian" + tambah case switch). Verifikasi 345-01 jangan cari "Failed" di Records.cshtml — cari "Completed"/"bg-info". |
| **C-3** | D-10/Q2: kekhawatiran group `PassedCount` (`a.IsPassed`) menghitung pending sebagai passed | Projection induk di `AssessmentAdminController.cs:2712` sudah `IsPassed = a.IsPassed ?? false` → di grup, `a.IsPassed` adalah **`bool` non-nullable yang sudah collapse null→false**. Jadi `PassedCount = ...Count(a => a.IsPassed)` SUDAH exclude pending (pending dihitung sebagai "tidak passed"). [VERIFIED: read L2697-2716 + L2759/2775/2789/2821] | `PassedCount` **tidak butuh guard** — sudah benar by-construction. "Konsistensi" yang dimaksud D-10 = sekadar verifikasi, bukan perubahan kode. Lihat Q2 untuk detail PrePost vs Standard `MenungguPenilaianCount`. |

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Badge "Menunggu Penilaian" = **AMBER**. Web: `bg-warning text-dark`. PDF (QuestPDF): `Colors.Orange.Darken2`. Konsisten di SEMUA 4 surface (RecordsWorkerDetail, Records, UserAssessmentHistory view, BulkExportPdf). Beda jelas dari Failed (merah/`bg-danger`) & dash kosong (`text-muted` abu).
- **D-02:** Gunakan konstanta `AssessmentConstants.AssessmentStatus.PendingGrading` (= "Menunggu Penilaian", `Models/AssessmentConstants.cs:18`) di SEMUA surface C# (controller/service/PDF) — JANGAN literal string. Di Razor `.cshtml`: utamakan referensi konstanta; mekanisme final diserahkan ke planner (lihat Q5 — jawaban: `@HcPortal.Models.AssessmentConstants.AssessmentStatus.PendingGrading` atau `@AssessmentConstants...` karena `@using HcPortal.Models` sudah ada di `_ViewImports`).
- **D-03:** Mapping berbasis `IsPassed` (`bool?`): `true`→"Passed" hijau, `false`→"Failed" merah, `null`→"Menunggu Penilaian" amber. Target = sesi `Status="Completed"` + `IsPassed==null`. Template 3-way ada di `Records.cshtml:182-192` → pola untuk `RecordsWorkerDetail.cshtml:226-231`.
- **D-04:** `passedCount` = `IsPassed == true`; `passRate` denominator = **graded only** (`IsPassed != null`), bukan total. (Terkunci.)
- **D-05:** All-pending edge (gradedCount == 0) → `passRate` tampil "—" / "Belum ada penilaian" (hindari "0%" menyesatkan).
- **D-06:** Surface pending: tampilkan indikator ringan "Menunggu Penilaian: N" di area kartu stat (reuse styling existing — bukan kartu besar baru).
- **D-08:** VM ripple — `AssessmentReportItem.IsPassed` `bool`→`bool?`; ctrl drop `?? false` (`AssessmentAdminController.cs:4737`); view 3-way (`UserAssessmentHistory.cshtml:172`). Build 0 error setelah nullable ripple.
- **D-09:** Include Excel `CMPController.cs:694` ExportRecords `null`→"Menunggu Penilaian".
- **D-10:** Include konsistensi grup `PassedCount` (`AssessmentAdminController.cs:2759/2775/2789/2821`) — pastikan exclude pending (lihat C-3: sudah exclude, verifikasi saja).
- **D-11:** xUnit — (a) VM nullable mapping (`null`→pending, bukan Failed), (b) passRate exclude-pending math (termasuk all-pending → 0/—). Playwright UAT 3 surface. **SEED_WORKFLOW:** snapshot DB sebelum seed sesi `Completed`+`IsPassed==null`, restore sesudah, tandai journal `cleaned`.

### Claude's Discretion
- **D-05/D-06/D-07** (tampilan stats + averageScore) — Claude pilih default sesuai pola kartu existing; planner boleh refine.
- **D-07 (planner konfirmasi):** `averageScore` — rekomendasi exclude pending juga (skor belum final); default aman tetap atas graded sessions. Planner putuskan saat plan 345-02. (Riset: lihat Q4 — Score pending = `a.Score ?? 0` skew rata-rata, rekomendasi exclude.)
- Mekanisme akses konstanta di Razor (D-02) — lihat Q5.

### Deferred Ideas (OUT OF SCOPE)
- **JS SignalR result-cell** `AssessmentMonitoringDetail.cshtml:1409` (edge post-edit sesi pending) — out of scope, follow-up.
- **Inklusi sesi `Status="Menunggu Penilaian"` murni** di `GetUnifiedRecords`/`GetAllWorkersHistory` → **Phase 346 REC-07** (sequential, depends label 345). Researcher TIDAK plan ini di 345 — lihat Q7 untuk batas eksak.
- **`MenungguPenilaianCount` untuk PrePost sub-rows** Monitoring — bila dianggap melebar, defer **Phase 348 (MAM)**.
- 6 surface verified aman (AssessmentMonitoringDetail L252-266, _HistoryTab L95-103, EditPesertaAnswers L20, CMP/Assessment L555, CMP/Results L45, _TrainingRecordsTab L249) — tidak disentuh.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CMP06R-01 | `RecordsWorkerDetail.cshtml:226-231` binary → 3-way (`null→"Menunggu Penilaian"` amber) | Q5 (akses konstanta Razor) + Records.cshtml:182-192 pola; lihat §Code Examples #1. Target view confirmed binary `if(IsPassed==true) Passed else Failed`. |
| CMP06R-02 | `UserAssessmentHistory` 3-layer: VM `bool→bool?` + ctrl drop `?? false` (L4737) + view 3-way (L172) + stats exclude-pending (L4744-4745) | Q1, Q4, C-1 (VM di CDPDashboardViewModel.cs:111 BUKAN ReportsDashboardViewModel), §Code Examples #2/#3. |
| CMP06R-03 | PDF `GeneratePerPesertaPdf` (L4620-4621, `BulkExportPdf`) binary "Tidak Lulus"+merah → 3-way + warna netral | Q3 (QuestPDF 2026.2.2 + `Colors.Orange.Darken2` valid; eligibility L4507 confirmed include pending), §Code Examples #4. |
| CMP06R-04 | Label unify — `GetUnifiedRecords:51` switch `null→PendingGrading` + `Records.cshtml:188` switch tambah case | Q7, C-2 (Records.cshtml saat ini tampil "Completed" bukan "Failed"), §Code Examples #5. |
| CMP06R-05 | Regression test — xUnit (VM nullable + passRate math) + Playwright UAT 3 surface | Q6 (HcPortal.Tests pattern + tests/e2e Playwright + dbSnapshot + accounts), §Validation Architecture. |

**Minor fold-in:** Excel `CMPController.cs:694` (D-09) + grup `PassedCount` (D-10) — keduanya ke Plan 345-01 / 345-02 sesuai split.
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Status badge unified records (CMP) | View (Razor) | Service (`WorkerDataService.GetUnifiedRecords` produces `Status`/`IsPassed`) | Badge rendering di view; label string di service. CMP06R-01/04. |
| UserAssessmentHistory stats + badge | Controller (`AssessmentAdminController.UserAssessmentHistory`) | VM (`AssessmentReportItem` / `UserAssessmentHistoryViewModel`) + View | Math stats inline di action; badge di view; VM nullability ripple. CMP06R-02. |
| Per-peserta PDF status | Controller (PDF gen, server-side QuestPDF) | — | `GeneratePerPesertaPdf` membangun byte[] PDF di controller; tidak ada client tier. CMP06R-03. |
| Excel export status cell | Controller (`CMPController.ExportRecordsTeamAssessment`) | — | Server-side ClosedXML; cell value langsung. Minor D-09. |
| Regression coverage | Test project (xUnit unit) | E2E (Playwright UAT) | Math/mapping = unit; visual 3 surface = e2e. CMP06R-05. |

**Tidak ada misassignment risk** — semua capability single-tier server-rendered (MVC, bukan SPA). Tidak ada logic yang salah taruh di browser.

---

## Open Questions — Answered (inti deliverable)

### Q1. `AssessmentSession.IsPassed` nullability → **`bool?` (nullable)**

[VERIFIED: read `Models/AssessmentSession.cs:38`]

```csharp
// Models/AssessmentSession.cs:38
public bool? IsPassed { get; set; }
```

Entity-level sudah nullable. `Score` juga nullable (`int? Score`, L26). Jadi sumber kebenaran (`AssessmentSessions`) tidak butuh perubahan tipe — semua kerugian terjadi di **projection/collapse downstream** (`?? false`) dan **binary view branch** (`else` menelan null).

### Q2. Group projection `IsPassed` semantics di L2759/2775/2789/2821 → **sudah `bool` collapsed, PassedCount sudah exclude pending**

[VERIFIED: read `AssessmentAdminController.cs:2697-2716` + L2759/2775/2789/2821]

Anonymous projection induk (sebelum grouping) meng-collapse null di L2712:

```csharp
// AssessmentAdminController.cs:2697-2716 (anonymous type, fed ke GroupBy)
.Select(a => new
{
    a.Id, a.Title, a.Category, a.Schedule, a.ExamWindowCloseDate, a.Status,
    a.IsTokenRequired, a.AccessToken, a.CreatedAt, a.AssessmentType, a.LinkedGroupId, a.DurationMinutes,
    IsCompleted = a.CompletedAt != null,
    IsPassed = a.IsPassed ?? false,                       // L2712 — null SUDAH jadi false di sini
    IsStarted = a.StartedAt != null,
    IsMenungguPenilaian = a.Status == "Menunggu Penilaian", // L2714 — flag pending TERPISAH
    a.HasManualGrading
})
```

Konsekuensi pada keempat `PassedCount`:
- **L2759** `postSubs.Count(a => a.IsPassed)` (PrePost PostSubRow)
- **L2775** `preSubs.Count(a => a.IsPassed)` (PrePost PreSubRow)
- **L2789** `postSubs.Count(a => a.IsPassed)` (PrePost PostSubRow, dup pola)
- **L2821** `g.Count(a => a.IsPassed)` (Standard group)

`a.IsPassed` di sini adalah `bool` (bukan `bool?`) → pending (`null`→`false`) **tidak terhitung sebagai passed**. **PassedCount sudah benar — TIDAK perlu guard.** Ini menutup kekhawatiran D-10.

**`MenungguPenilaianCount` — asimetri PrePost vs Standard:**
- Standard group **punya** `MenungguPenilaianCount = g.Count(a => a.IsMenungguPenilaian)` di **L2825**.
- PrePost sub-rows (`MonitoringSubRowViewModel`, dibangun L2767-2794) **tidak punya** `MenungguPenilaianCount`.

**PENTING — interpretasi `IsMenungguPenilaian`:** flag ini = `a.Status == "Menunggu Penilaian"` (status literal murni), BUKAN `Status=="Completed" && IsPassed==null`. Ini scope domain berbeda dari Phase 345 (yang menangani Completed+IsPassed-null). Phase 345 menyentuh **report surface (Records/History/PDF/Excel)**, sedangkan grup Monitoring memakai status literal. **Minimal-change consistency move (rekomendasi):** verifikasi saja bahwa keempat `PassedCount` exclude pending (sudah, via L2712) dan **JANGAN tambah `MenungguPenilaianCount` ke PrePost sub-rows** — itu eksplisit di-defer ke Phase 348 (MAM) per CONTEXT Deferred. Kalau planner ingin tetap menyentuh, batasi ke komentar/verifikasi, bukan tambah field VM baru.

→ **D-10 actionable scope = NOL perubahan kode** (sudah benar). Plan 345-02 cukup tambah test/assert yang membuktikan `PassedCount` tidak naik saat ada pending session, plus komentar inline kalau perlu.

### Q3. QuestPDF `Colors.Orange.Darken2` → **VALID (QuestPDF 2026.2.2)** + pola 3-way ternary

[VERIFIED: read `HcPortal.csproj` `<PackageReference Include="QuestPDF" Version="2026.2.2" />` + nuget cache `~/.nuget/packages/questpdf/2026.2.2`] [VERIFIED: read `AssessmentAdminController.cs:4620-4621`]

Kode sekarang (binary, null→merah):

```csharp
// AssessmentAdminController.cs:4620-4621 (di GeneratePerPesertaPdf, session = AssessmentSession entity)
c.Item().Text(t => { t.Span("Skor: ").Bold(); t.Span(session.Score?.ToString() ?? "—").Bold().FontColor(session.IsPassed == true ? QuestPDF.Helpers.Colors.Green.Darken2 : QuestPDF.Helpers.Colors.Red.Darken2); });
c.Item().Text(t => { t.Span("Status: ").Bold(); t.Span(session.IsPassed == true ? "Lulus" : "Tidak Lulus").Bold().FontColor(session.IsPassed == true ? QuestPDF.Helpers.Colors.Green.Darken2 : QuestPDF.Helpers.Colors.Red.Darken2); });
```

`QuestPDF.Helpers.Colors` adalah static class dengan palette Material penuh (Green/Red/Orange/Blue/Grey × Lighten/Darken tiers) — sudah dipakai di file ini (`Colors.Blue.Darken2` L4604, `Colors.Grey.Darken1` L4605/L4632, `Colors.Green.Darken2`/`Colors.Red.Darken2` L4620-4621). `Colors.Orange.Darken2` ada di palette yang sama. Namespace eksak: `QuestPDF.Helpers.Colors.Orange.Darken2` (fully-qualified, konsisten gaya file). [CITED: questpdf.com — Colors helper Material palette; tier Darken1/Darken2/Darken3 + base]

**Pola 3-way yang planner tulis** (extract ke local var supaya tidak triple-ternary):

```csharp
// Rekomendasi: hitung sekali di atas Text spans
var statusText  = session.IsPassed == true ? "Lulus" : session.IsPassed == false ? "Tidak Lulus" : HcPortal.Models.AssessmentConstants.AssessmentStatus.PendingGrading;
var statusColor = session.IsPassed == true ? QuestPDF.Helpers.Colors.Green.Darken2
                : session.IsPassed == false ? QuestPDF.Helpers.Colors.Red.Darken2
                : QuestPDF.Helpers.Colors.Orange.Darken2;
// lalu .Span(statusText).FontColor(statusColor) di kedua baris (Skor + Status)
```

**Eligibility confirm (bug reachable):** `BulkExportPdf` L4507 filter `s.Status != "Cancelled" && (s.CompletedAt != null || s.Score != null)` — sesi Completed+IsPassed-null punya `CompletedAt != null` → **MASUK** export → kena render L4620-4621. Bug nyata & ter-trigger. [VERIFIED: read L4499-4543]

### Q4. passRate edge math → formula graded-denominator + guard

[VERIFIED: read `AssessmentAdminController.cs:4722-4746`]

Kode sekarang:

```csharp
// AssessmentAdminController.cs:4726-4746
.Select(a => new AssessmentReportItem
{
    ...
    Score = a.Score ?? 0,              // L4735 — pending Score null → 0 (skew averageScore!)
    PassPercentage = a.PassPercentage,
    IsPassed = a.IsPassed ?? false,    // L4737 — DROP ?? false (D-08); jadi IsPassed = a.IsPassed
    CompletedAt = a.CompletedAt
})
.ToListAsync();

var totalAssessments = assessments.Count;                                        // L4743
var passedCount = assessments.Count(a => a.IsPassed);                             // L4744
var passRate = totalAssessments > 0 ? passedCount * 100.0 / totalAssessments : 0; // L4745 — denom = TOTAL (salah)
var averageScore = totalAssessments > 0 ? assessments.Average(a => (double)a.Score) : 0; // L4746
```

**Formula exact yang planner tulis** (setelah `IsPassed` jadi `bool?`):

```csharp
var totalAssessments = assessments.Count;
var gradedCount  = assessments.Count(a => a.IsPassed != null);            // graded = bukan pending
var pendingCount = assessments.Count(a => a.IsPassed == null);            // D-06 indikator "Menunggu Penilaian: N"
var passedCount  = assessments.Count(a => a.IsPassed == true);            // D-04
var passRate     = gradedCount > 0 ? passedCount * 100.0 / gradedCount : 0; // D-04 denom = graded; D-05 guard
// averageScore (D-07, planner-confirm): exclude pending karena Score pending = parsial/0 (L4735 ?? 0)
var gradedAssessments = assessments.Where(a => a.IsPassed != null).ToList();
var averageScore = gradedAssessments.Count > 0 ? gradedAssessments.Average(a => (double)a.Score) : 0;
```

**D-05 all-pending guard:** `gradedCount == 0` → `passRate` = 0 di C#, tapi **view** harus tampil "—"/"Belum ada penilaian" (bukan "0.0%"). Bawa `gradedCount`/`pendingCount` ke `UserAssessmentHistoryViewModel` (tambah 2 prop) supaya view bisa decide. View saat ini: `@Model.PassRate.ToString("F1")%` di **L68 + L101** (dua tempat: header mini-stat + kartu besar). Keduanya harus jadi conditional.

**D-07 averageScore — riset rekomendasi EXCLUDE (planner confirm):** alasan kuat — L4735 `Score = a.Score ?? 0` artinya sesi pending yang `Score==null` menyumbang **0** ke rata-rata, menyeret averageScore turun secara menyesatkan. Bahkan kalau pending punya partial MCQ score, itu belum final. Exclude pending → averageScore = rata-rata sesi yang benar-benar dinilai. **Flag: ASSUMED reasoning, butuh konfirmasi user/planner saat 345-02** (D-07 memang discretion).

### Q5. Razor constant access → **`@AssessmentConstants.AssessmentStatus.PendingGrading`** (atau fully-qualified)

[VERIFIED: read `Views/_ViewImports.cshtml:2`]

```cshtml
@* Views/_ViewImports.cshtml *@
@using HcPortal                       @* L1 *@
@using HcPortal.Models                @* L2 — AssessmentConstants ADA DI SINI (namespace HcPortal.Models) *@
@using HcPortal.Models.Guide
@using System.Text.Json
```

`AssessmentConstants` ada di `namespace HcPortal.Models` (`AssessmentConstants.cs:1`). Karena `_ViewImports.cshtml:2` sudah `@using HcPortal.Models`, **ketiga target view** (`RecordsWorkerDetail.cshtml`, `Records.cshtml`, `UserAssessmentHistory.cshtml` — semua di bawah `Views/`) bisa akses **tanpa @using tambahan**:

```cshtml
@* Paling ringkas (recommended) — class langsung visible *@
<span class="badge bg-warning text-dark">@AssessmentConstants.AssessmentStatus.PendingGrading</span>

@* Atau fully-qualified bila ingin eksplisit (D-02 menyebut pola ini) *@
<span class="badge bg-warning text-dark">@HcPortal.Models.AssessmentConstants.AssessmentStatus.PendingGrading</span>
```

Keduanya valid. **Rekomendasi: `@AssessmentConstants.AssessmentStatus.PendingGrading`** (ringkas, sudah ter-cover `_ViewImports`). Tidak perlu literal string di view → memenuhi D-02 sepenuhnya.

**Catatan konsistensi:** view existing (Records.cshtml:188) masih pakai literal `"Completed"`/`"Passed"`/`"Failed"` di switch. Itu bukan label PendingGrading jadi tidak melanggar D-02; tapi case baru yang ditambah (`"Menunggu Penilaian"`) sebaiknya **referensi konstanta** untuk match dengan service output. Karena `switch` C# butuh **constant pattern**, `const string` boleh dipakai sebagai case label: `AssessmentConstants.AssessmentStatus.PendingGrading => "bg-warning text-dark"` valid (PendingGrading adalah `const`).

### Q6. Test infrastructure → xUnit `HcPortal.Tests` + Playwright `tests/e2e` (lengkap di §Validation Architecture)

[VERIFIED: ls `HcPortal.Tests/` + read `HcPortal.Tests.csproj` + `OrgLabelControllerTests.cs` + ls `tests/e2e/` + read `tests/helpers/accounts.ts` + `tests/helpers/dbSnapshot.ts` + `tests/e2e/export-per-peserta.spec.ts`]

**xUnit (`HcPortal.Tests/`):**
- Stack: xUnit 2.9.3 + `Microsoft.EntityFrameworkCore.InMemory` 8.0.0 + `...SqlServer` 8.0.0, net8.0, `ProjectReference ..\HcPortal.csproj`.
- File ada: `CertificateStatusTests.cs`, `FileUploadHelperTests.cs`, `OrganizationControllerTests.cs`, `OrgLabelControllerTests.cs`, `OrgLabelMigrationIntegrationTests.cs`, `OrgLabelServiceTests.cs`. **Belum ada** test file untuk `AssessmentAdminController` / `WorkerDataService` / `UserAssessmentHistory`. → **Wave 0 gap: file test baru.**
- Pattern factory (dari `OrgLabelControllerTests.cs:28-65`): `DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())`, seed entity via `ctx.AddRange(...) + SaveChanges()`, instantiate controller dengan **`UserManager` = `null!`** (aman bila code-path yang dites return SEBELUM `GetUserAsync`). Untuk `UserAssessmentHistory`, action butuh `_context.Users.FirstOrDefaultAsync` → seed satu `ApplicationUser` + beberapa `AssessmentSession` dengan IsPassed = true/false/null.
- Run command: `dotnet test HcPortal.Tests` (precedent: STATE.md "20/20 PASS"). Quick: sama (proyek kecil, <1s).

**KENDALA UJI MATEMATIKA STATS — ekstrak helper:** math passRate/averageScore saat ini **inline di action `UserAssessmentHistory`** (L4743-4746). Untuk unit-test murni (D-11b), opsi:
- **Opsi A (rekomendasi):** ekstrak math ke method static testable, mis. `internal static (int total, int graded, int pending, int passed, double passRate, double avgScore) ComputeHistoryStats(List<AssessmentReportItem> items)` di controller (atau helper class). Test panggil langsung dengan list rakitan → tidak perlu DbContext/HttpContext. Bersih, deterministik.
- **Opsi B:** test action end-to-end pakai InMemory DbContext (seed sessions, panggil `UserAssessmentHistory(userId)`, cast `ViewResult.Model` ke `UserAssessmentHistoryViewModel`, assert `.PassRate`/`.PassedCount`). Lebih berat tapi tanpa refactor. UserManager bisa null hanya kalau action tidak panggilnya — **CEK:** action `UserAssessmentHistory` TIDAK panggil `_userManager` (cuma `_context`), jadi `null!` UserManager AMAN untuk Opsi B juga. [VERIFIED: read L4712-4763 — no `_userManager` reference]
- **Rekomendasi:** Opsi A untuk math (D-11b all-pending/graded), Opsi B atau projection-level test untuk VM nullable mapping (D-11a — buktikan `IsPassed==null` tetap null, bukan false). Planner pilih; Opsi A lebih sustainable.

**Playwright (`tests/e2e/`):**
- Config: `tests/playwright.config.ts`. Helpers shared di `tests/helpers/` (`accounts.ts`, `auth.ts`, `dbSnapshot.ts`, `utils.ts`); e2e-specific di `tests/e2e/helpers/`.
- Akun (dari `tests/helpers/accounts.ts`): `admin` = `admin@pertamina.com` / `123456`, `hc` = `meylisa.tjiang@pertamina.com` / `123456`. Keduanya bisa akses `UserAssessmentHistory` & `BulkExportPdf` (`[Authorize(Roles="Admin, HC")]`). `coachee` = `rino.prasetyo@pertamina.com` untuk surface `/CMP/RecordsWorkerDetail` (worker view) atau negative-test.
- Login pattern (inline, dari `export-per-peserta.spec.ts:28-37`): `page.goto('/Account/Login')` → fill `input[name="email"]` + `input[name="password"]` → `Promise.all([waitForURL(!includes('/Account/Login')), click('button[type="submit"]')])`. (Helper `auth.ts` ada tapi spec export pakai inline karena helper wait `/Home/**` yang tidak cocok HC/Coachee.)
- Spec relevan ada: `assessment.spec.ts`, `export-per-peserta.spec.ts` (PDF/Excel download), `edit-peserta-answers.spec.ts`. **Belum ada** spec khusus pending-grading display → **Wave 0 gap: spec baru** (atau extend `assessment.spec.ts`).
- PDF download test pattern (dari `export-per-peserta.spec.ts:41-55`): `triggerDownload(page, url)` = `waitForEvent('download')` + `page.goto(url).catch(()=>{})`; assert `download.path()` truthy + `suggestedFilename()` regex. **BulkExportPdf** menghasilkan `.zip` (L4534 `..._Bundle.zip`) — assert `/_Bundle\.zip$/`. (Catatan: konten PDF di dalam zip tidak mudah di-assert via Playwright; UAT visual badge dilakukan via screenshot manual atau via MCP-driven verify per pola Phase 344. Untuk regression otomatis, cukup assert download sukses + size>0; verifikasi label "Menunggu Penilaian" via human/MCP.)

**SEED_WORKFLOW (`docs/SEED_WORKFLOW.md` + helper `tests/helpers/dbSnapshot.ts`):**
- Helper sudah ada: `backup(snapshotPath)` / `restore(snapshotPath)` / `execScript(sqlPath)` / `queryScalar(sql)` / `queryString(sql)`. Target DB = `localhost\SQLEXPRESS` / `HcPortalDB_Dev` / Integrated Security (guard reject non-localhost).
- SOP D-11: (1) `queryString(SERVERPROPERTY('InstanceDefaultBackupPath'))` → resolve dir (C:\Temp blocked oleh service account); (2) `backup(<dir>\pre345.bak)`; (3) seed sesi `Status='Completed', IsPassed=NULL, CompletedAt=<now>, Score=<partial or NULL>` untuk user test (mis. via `execScript` SQL INSERT atau UI submit essay); (4) jalankan Playwright UAT 3 surface; (5) `restore(<dir>\pre345.bak)` (sukses ATAU gagal — finally block); (6) catat `docs/SEED_JOURNAL.md` klasifikasi `temporary + local-only`, tandai `cleaned`.
- **Klasifikasi seed:** `temporary + local-only` (untuk reproduce/UAT) — JANGAN ke `Data/SeedData.cs`.

### Q7. `GetUnifiedRecords` filter → `Status == "Completed"` (L33) + boundary Phase 346 confirmed OUT OF SCOPE

[VERIFIED: read `Services/WorkerDataService.cs:31-57`]

```csharp
// Services/WorkerDataService.cs:31-57
var assessments = await _context.AssessmentSessions
    .AsNoTracking()
    .Where(a => a.UserId == userId && a.Status == "Completed")   // L33 — HANYA "Completed" (literal)
    .ToListAsync();
...
unified.AddRange(assessments.Select(a => new UnifiedTrainingRecord
{
    ...
    IsPassed = a.IsPassed,                                       // L50 — preserved bool? (bagus)
    // Phase 337 CMP-06: three-way switch
    Status = a.IsPassed switch                                   // L52-57
    {
        true => "Passed",
        false => "Failed",
        null => "Completed"                                      // L56 — GANTI → PendingGrading (CMP06R-04)
    },
    ...
}));
```

**CMP06R-04 actionable:** ubah L56 `null => "Completed"` → `null => AssessmentConstants.AssessmentStatus.PendingGrading` (service sudah `using HcPortal.Models` L2). Lalu di `Records.cshtml:188` switch tambah case `"Menunggu Penilaian" => "bg-warning text-dark"` (atau referensi `AssessmentConstants.AssessmentStatus.PendingGrading =>`). [VERIFIED: read Records.cshtml:188 — switch saat ini punya `"Completed" => "bg-info"`, tidak ada case pending]

**BOUNDARY Phase 346 (jangan kerjakan di 345):** Filter L33 `Status == "Completed"` berarti sesi ber-`Status="Menunggu Penilaian"` **murni** (di-set GradingService untuk essay sebelum HC nilai) **tidak masuk** unified records sama sekali. Memasukkannya = **Phase 346 REC-07** (`include PendingGrading di GetUnifiedRecords:31 + GetAllWorkersHistory:136`), sequential depends label 345. **Researcher TIDAK plan inklusi status-murni di 345.** Phase 345 HANYA menangani sesi yang `Status=="Completed" && IsPassed==null` (yang SUDAH masuk filter, tapi salah label/warna). Garis batas: 345 = label & warna untuk yang sudah masuk; 346 = perluas filter agar yang status-murni ikut masuk.

**Catatan koordinasi baris berdekatan (hindari konflik 346/347):** `Records.cshtml:182-192` & `RecordsWorkerDetail.cshtml:226-231` akan disentuh 345 (label) lalu 346 (REC-01/03 tambah kolom Aksi) lalu 347 (POL-01 `Passed/Failed`→`Lulus/Tidak Lulus`). Plan 345 ubah HANYA branch `null` + switch case pending; jangan rename "Passed"/"Failed" (itu POL-01/347). Biarkan struktur minimal supaya merge 346/347 mulus.

---

## Standard Stack

Tidak ada library baru. Semua sudah terpasang.

### Core (terpasang, reuse)
| Library | Version | Purpose | Status |
|---------|---------|---------|--------|
| QuestPDF | 2026.2.2 | PDF per-peserta (`GeneratePerPesertaPdf`) — `Colors.Orange.Darken2` valid | [VERIFIED: HcPortal.csproj] |
| ClosedXML (`XLWorkbook`) | (existing) | Excel export (`CMPController.ExportRecordsTeamAssessment`) | [VERIFIED: CMPController.cs:682] |
| Bootstrap 5 | (existing) | Badge `bg-warning text-dark` (amber) | [VERIFIED: dipakai luas di views] |
| xunit | 2.9.3 | Unit test VM mapping + stats math | [VERIFIED: HcPortal.Tests.csproj] |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | DbContext test fixture | [VERIFIED: HcPortal.Tests.csproj] |
| @playwright/test | (existing) | E2E UAT 3 surface | [VERIFIED: tests/e2e/] |

**Installation:** none.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Label "Menunggu Penilaian" string | Literal `"Menunggu Penilaian"` tersebar | `AssessmentConstants.AssessmentStatus.PendingGrading` (const, L18) | D-02 + match service output + Phase 346 REC-07 wajib konstanta. Drift literal = bug. |
| 3-way badge logic baru | Buat helper/extension method baru | Pola existing `Records.cshtml:182-192` (inline `if/else if`) | Konsistensi gaya codebase; reviewer Phase 346/347 sudah kenal pola ini. |
| DB snapshot/restore untuk UAT | Manual sqlcmd ad-hoc | `tests/helpers/dbSnapshot.ts` (`backup`/`restore`) | Sudah ada guard non-localhost + InstanceDefaultBackupPath resolve. |
| Amber color PDF | Hex literal random | `QuestPDF.Helpers.Colors.Orange.Darken2` | Match Material tier yang dipakai Green/Red existing (kontras seimbang). |

## Common Pitfalls

### Pitfall 1: Edit VM di file yang salah (ReportsDashboardViewModel vs CDPDashboardViewModel)
**What goes wrong:** D-08/REQUIREMENTS bilang `Models/ReportsDashboardViewModel.cs`, tapi `AssessmentReportItem` ada di `Models/CDPDashboardViewModel.cs:111`. Edit file salah → field tetap `bool`, build error tak terduga atau perubahan tak berefek.
**How to avoid:** Plan 345-02 target **`Models/CDPDashboardViewModel.cs:111`** untuk `bool`→`bool?`. (`ReportsDashboardViewModel.cs` cuma punya `UserAssessmentHistoryViewModel` — itu yang ditambah prop `GradedCount`/`PendingCount`.)
**Warning signs:** grep `class AssessmentReportItem` → 1 hit di CDPDashboardViewModel.cs.

### Pitfall 2: `@if (item.IsPassed)` tidak compile setelah jadi bool?
**What goes wrong:** `UserAssessmentHistory.cshtml:172` saat ini `@if (item.IsPassed)` (bool langsung). Setelah D-08 jadi `bool?`, `if (bool?)` **compile error** (C# tak izinkan implicit bool? → bool di if).
**How to avoid:** ubah ke `@if (item.IsPassed == true) { Pass } else if (item.IsPassed == false) { Fail } else { Menunggu Penilaian }`. Ini sekaligus implementasi 3-way.
**Warning signs:** `dotnet build` error CS0266/CS0029 di view.

### Pitfall 3: passRate "0%" menyesatkan saat all-pending
**What goes wrong:** kalau semua sesi pending, `passedCount=0`, dan kalau denominator masih total → 0%. User HC mengira "semua gagal" padahal belum dinilai.
**How to avoid:** D-04 denom=gradedCount; D-05 `gradedCount==0` → view tampil "—"/"Belum ada penilaian". Bawa `gradedCount` ke VM; conditional di view L68 + L101 (DUA tempat).
**Warning signs:** kartu Pass Rate tampil "0.0%" untuk user yang cuma punya sesi pending.

### Pitfall 4: averageScore ketarik 0 oleh pending (Score ?? 0)
**What goes wrong:** L4735 `Score = a.Score ?? 0` → sesi pending Score-null menyumbang 0 ke `Average` → averageScore turun palsu.
**How to avoid:** D-07 exclude pending dari averageScore (rekomendasi riset, planner confirm). `gradedAssessments.Average(...)`.
**Warning signs:** averageScore jauh lebih rendah dari ekspektasi saat ada pending session.

### Pitfall 5: Sentuh baris yang Phase 346/347 akan ubah
**What goes wrong:** 345 rename "Passed"→"Lulus" (itu POL-01/347) atau tambah kolom Aksi (REC-01/346) → konflik merge saat phase berikutnya.
**How to avoid:** 345 ubah HANYA: branch `null` (RecordsWorkerDetail), switch case pending (Records), `?? false` drop (ctrl), stats math, PDF/Excel null-branch. JANGAN sentuh teks "Passed"/"Failed" true/false.
**Warning signs:** diff 345 menyentuh case `true =>`/`false =>` label atau colspan.

## Code Examples (planner copy-ready)

### #1 RecordsWorkerDetail.cshtml:226-231 — binary → 3-way (CMP06R-01)
```cshtml
@* SEBELUM (binary, null→Failed merah) — Views/CMP/RecordsWorkerDetail.cshtml:226-231 *@
@if (item.RecordType == "Assessment Online") {
    @if (item.IsPassed == true) {
        <span class="badge bg-success">Passed</span>
    } else {
        <span class="badge bg-danger">Failed</span>
    }
} else { ... }

@* SESUDAH (3-way) *@
@if (item.RecordType == "Assessment Online") {
    @if (item.IsPassed == true) {
        <span class="badge bg-success">Passed</span>
    } else if (item.IsPassed == false) {
        <span class="badge bg-danger">Failed</span>
    } else {
        <span class="badge bg-warning text-dark">@AssessmentConstants.AssessmentStatus.PendingGrading</span>
    }
} else { ... }
```
(`item` = `UnifiedTrainingRecord`, `IsPassed` = `bool?` — sudah benar, tinggal tambah branch.)

### #2 AssessmentAdminController.cs:4737 — drop `?? false` (CMP06R-02)
```csharp
// SEBELUM (L4737): IsPassed = a.IsPassed ?? false,
// SESUDAH:
IsPassed = a.IsPassed,   // bool? — pending tetap null (butuh AssessmentReportItem.IsPassed jadi bool?)
```

### #3 Stats math (CMP06R-02, D-04/05/06/07) — lihat Q4 untuk blok lengkap
```csharp
var gradedCount  = assessments.Count(a => a.IsPassed != null);
var pendingCount = assessments.Count(a => a.IsPassed == null);
var passedCount  = assessments.Count(a => a.IsPassed == true);
var passRate     = gradedCount > 0 ? passedCount * 100.0 / gradedCount : 0;
var gradedList   = assessments.Where(a => a.IsPassed != null).ToList();
var averageScore = gradedList.Count > 0 ? gradedList.Average(a => (double)a.Score) : 0;
// + UserAssessmentHistoryViewModel.GradedCount/PendingCount (prop baru) untuk view guard
```

### #4 GeneratePerPesertaPdf:4620-4621 — PDF 3-way + amber (CMP06R-03) — lihat Q3 untuk blok lengkap
```csharp
var statusText  = session.IsPassed == true ? "Lulus" : session.IsPassed == false ? "Tidak Lulus" : HcPortal.Models.AssessmentConstants.AssessmentStatus.PendingGrading;
var statusColor = session.IsPassed == true ? QuestPDF.Helpers.Colors.Green.Darken2 : session.IsPassed == false ? QuestPDF.Helpers.Colors.Red.Darken2 : QuestPDF.Helpers.Colors.Orange.Darken2;
// pakai statusText/statusColor di .Span(...).FontColor(...) baris Skor (4620) + Status (4621)
```

### #5 GetUnifiedRecords:56 + Records.cshtml:188 — label unify (CMP06R-04)
```csharp
// Services/WorkerDataService.cs:52-57
Status = a.IsPassed switch
{
    true  => "Passed",
    false => "Failed",
    null  => AssessmentConstants.AssessmentStatus.PendingGrading   // L56: ganti "Completed"
},
```
```cshtml
@* Views/CMP/Records.cshtml:188 — tambah case di switch sc *@
var sc = item.Status switch {
    "Passed" => "bg-success", "Valid" => "bg-success", "Completed" => "bg-info",
    "Permanent" => "bg-success", "Failed" => "bg-danger", "Expired" => "bg-warning text-dark",
    AssessmentConstants.AssessmentStatus.PendingGrading => "bg-warning text-dark",  // TAMBAH (const → valid case label)
    _ => "bg-secondary"
};
```

### #6 Excel CMPController.cs:694 — null→konstanta (D-09 minor)
```csharp
// SEBELUM (L694): ws.Cell(i + 2, 7).Value = r.IsPassed == true ? "Passed" : (r.IsPassed == false ? "Failed" : "");
// SESUDAH:
ws.Cell(i + 2, 7).Value = r.IsPassed == true ? "Passed" : (r.IsPassed == false ? "Failed" : AssessmentConstants.AssessmentStatus.PendingGrading);
```
(`r` = `AllWorkersHistoryRow`, `IsPassed` = `bool?` L26 — upstream `GetAllWorkersHistory` map `IsPassed = a.IsPassed` preserve null, bersih. CMPController butuh `using HcPortal.Models` — cek header file; bila belum ada, tambah.)

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (`HcPortal.Tests`, net8.0) + @playwright/test (`tests/e2e`) |
| Config file | `tests/playwright.config.ts` (e2e); xUnit via `HcPortal.Tests.csproj` |
| Quick run command | `dotnet test HcPortal.Tests` |
| Full suite command | `dotnet test HcPortal.Tests` (+ `npx playwright test` di `tests/` untuk e2e) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CMP06R-02 | VM nullable mapping: `IsPassed==null` tetap null (bukan false) | unit | `dotnet test HcPortal.Tests` | ❌ Wave 0 (new) |
| CMP06R-02 | passRate exclude-pending + all-pending guard (gradedCount==0 → 0/—) | unit | `dotnet test HcPortal.Tests` | ❌ Wave 0 (new) |
| CMP06R-02 | averageScore exclude pending (D-07) | unit | `dotnet test HcPortal.Tests` | ❌ Wave 0 (new) |
| D-10 | group PassedCount tidak naik karena pending (regression-guard) | unit | `dotnet test HcPortal.Tests` | ❌ Wave 0 (new) |
| CMP06R-01 | RecordsWorkerDetail badge amber untuk pending | e2e | `npx playwright test` | ❌ Wave 0 (new/extend) |
| CMP06R-02 | UserAssessmentHistory badge amber + stats benar | e2e | `npx playwright test` | ❌ Wave 0 (new/extend) |
| CMP06R-03 | BulkExportPdf download sukses (zip) — label via human/MCP verify | e2e | `npx playwright test` | ❌ Wave 0 (new/extend) |

### Sampling Rate
- **Per task commit:** `dotnet build` (0 error wajib — nullable ripple) + `dotnet test HcPortal.Tests` (math/mapping).
- **Per wave merge:** full `dotnet test HcPortal.Tests` + targeted Playwright spec.
- **Phase gate:** build 0 error + xUnit green + Playwright 3-surface UAT (badge amber visible, di-verify human/MCP per pola Phase 344) sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/AssessmentHistoryStatsTests.cs` (atau sejenis) — covers CMP06R-02 math (passRate/averageScore exclude-pending, all-pending guard) + VM nullable mapping. **Rekomendasi: ekstrak `ComputeHistoryStats` static helper** (Q6 Opsi A) supaya math testable tanpa DbContext.
- [ ] `tests/e2e/assessment-pending-grade.spec.ts` (atau extend `assessment.spec.ts`) — covers CMP06R-01/02/03 visual; reuse `accounts.ts` (admin/hc) + `dbSnapshot.ts` (seed Completed+IsPassed-null, restore finally).
- [ ] SEED helper invocation: backup→seed `Status='Completed', IsPassed=NULL`→UAT→restore→journal `cleaned` (docs/SEED_JOURNAL.md). **Klasifikasi: temporary + local-only.**
- Framework: sudah terpasang (xUnit + Playwright) — TIDAK ada install gap.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 8 | build + xUnit | ✓ (assumed — proyek net8.0 aktif) | net8.0 | — |
| QuestPDF | PDF gen | ✓ | 2026.2.2 | — |
| SQL Server Express (localhost\SQLEXPRESS) | UAT seed/restore | ✓ (DB=HcPortalDB_Dev per dbSnapshot.ts) | — | — |
| sqlcmd | dbSnapshot backup/restore | ✓ (helper assumes available) | — | — |
| Playwright + browsers | e2e UAT | ✓ (tests/node_modules present) | — | — |

**Tidak ada dependency hilang.** Semua tool yang dibutuhkan sudah ada di repo/lingkungan lokal (sesuai CLAUDE.md Develop Workflow: verifikasi lokal `dotnet build` + `dotnet run` localhost:5277 + DB lokal + Playwright).

## Project Constraints (from CLAUDE.md)

- **Bahasa:** respon user-facing Bahasa Indonesia (kode/path/identifier tetap English).
- **Develop Workflow:** lokal-first. Verifikasi: `dotnet build` + `dotnet run` (cek `http://localhost:5277`) + cek DB lokal + Playwright. Commit & push HANYA setelah verifikasi lokal. **Jangan edit kode/DB langsung di Dev/Prod.** Promosi ke Dev = tanggung jawab Team IT. No migration di phase ini → notifikasi IT tanpa flag migration.
- **Seed Data Workflow:** seed UAT (Completed+IsPassed-null) = `temporary + local-only`. WAJIB: snapshot DB (`dbSnapshot.backup`) sebelum, restore (`dbSnapshot.restore`) sesudah (sukses ATAU gagal — finally), catat `docs/SEED_JOURNAL.md` tandai `cleaned`. JANGAN biarkan seed temporary nempel; JANGAN promosikan ke `Data/SeedData.cs` tanpa review.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | D-07 averageScore sebaiknya exclude pending (Score `?? 0` skew rata-rata) | Q4 / Pitfall 4 | LOW — D-07 memang discretion; kalau user mau include, ubah 1 baris. Planner konfirmasi saat 345-02. |
| A2 | .NET 8 SDK + sqlcmd tersedia di environment | Environment Availability | LOW — proyek aktif build net8.0; dbSnapshot helper sudah mengasumsikan sqlcmd. Verifikasi saat eksekusi pertama. |
| A3 | BulkExportPdf label di dalam zip tidak praktis di-assert via Playwright otomatis → verifikasi via human/MCP | Q6 / Validation | LOW — pola Phase 344 sudah pakai MCP-driven UAT; download-success assert tetap otomatis. |
| A4 | `MenungguPenilaianCount` untuk PrePost sub-rows = di luar scope (defer Phase 348) | Q2 | LOW — eksplisit di CONTEXT Deferred; planner tidak boleh menambah field VM baru di 345. |

## Open Questions (residual untuk planner)

1. **averageScore include vs exclude pending (D-07)** — riset rekomendasi EXCLUDE (A1). Planner/user putuskan saat 345-02. Default aman bila ragu: exclude (lebih jujur).
2. **Test strategy math: extract helper (Opsi A) vs action-level (Opsi B)** — riset rekomendasi Opsi A (ekstrak `ComputeHistoryStats`) untuk testability + sustainability. Planner pilih; keduanya viable (action tidak panggil `_userManager` jadi Opsi B juga aman).
3. **Indikator D-06 placement** — kartu ke-4 kecil vs sub-line di kartu Pass Rate (L102 `@Model.PassedCount passed`). Riset rekomendasi: sub-line/badge kecil di kartu Pass Rate atau header mini-stat (reuse styling, bukan kartu besar). Planner finalisasi.

## Sources

### Primary (HIGH confidence — codebase read langsung)
- `Models/AssessmentSession.cs:38` — `bool? IsPassed` (Q1)
- `Controllers/AssessmentAdminController.cs:2697-2716, 2759-2825` — group projection + PassedCount + MenungguPenilaianCount (Q2, C-3)
- `Controllers/AssessmentAdminController.cs:4499-4543, 4620-4621` — BulkExportPdf eligibility + GeneratePerPesertaPdf binary status (Q3)
- `Controllers/AssessmentAdminController.cs:4712-4763` — UserAssessmentHistory stats math + projection (Q4)
- `Views/_ViewImports.cshtml:1-6` — `@using HcPortal.Models` (Q5)
- `Services/WorkerDataService.cs:31-57, 128, 195` — GetUnifiedRecords filter + 3-way switch + GetAllWorkersHistory IsPassed preserve (Q7)
- `Models/CDPDashboardViewModel.cs:100-113` — `AssessmentReportItem.IsPassed = bool` (C-1)
- `Models/AllWorkersHistoryRow.cs:26` — `bool? IsPassed` (Excel upstream clean)
- `Views/CMP/Records.cshtml:182-192`; `Views/CMP/RecordsWorkerDetail.cshtml:226-231`; `Views/Admin/UserAssessmentHistory.cshtml:68,101,172` — view branches (C-2, CMP06R-01/02/04)
- `Controllers/CMPController.cs:682-694` — Excel ExportRecordsTeamAssessment (D-09)
- `HcPortal.Tests/HcPortal.Tests.csproj` + `OrgLabelControllerTests.cs:28-65` — xUnit pattern (Q6)
- `tests/helpers/accounts.ts`, `tests/helpers/dbSnapshot.ts`, `tests/e2e/export-per-peserta.spec.ts` — Playwright + SEED (Q6)
- `HcPortal.csproj` — QuestPDF 2026.2.2 (Q3)

### Secondary (MEDIUM — official docs)
- [CITED: questpdf.com] — `Colors` helper Material palette (Orange.Darken2 tier exists, konsisten dengan Green/Red yang sudah dipakai)

## Metadata

**Confidence breakdown:**
- Q1-Q7 jawaban: **HIGH** — semua dari read/grep file:line langsung, bukan asumsi.
- QuestPDF `Colors.Orange.Darken2`: **HIGH** — versi terkonfirmasi + Green/Red.Darken2 sudah dipakai di file yang sama (palette identik).
- Test strategy: **HIGH** untuk infrastruktur (file/pattern terverifikasi); **MEDIUM** untuk pilihan ekstrak-helper (rekomendasi, planner decide).
- D-07 averageScore: **MEDIUM** (discretion, reasoning kuat tapi butuh user confirm).

**Research date:** 2026-06-04
**Valid until:** 2026-07-04 (stabil — codebase internal, no fast-moving external dep)
