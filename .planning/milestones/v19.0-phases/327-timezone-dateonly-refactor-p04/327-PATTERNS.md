# Phase 327: Timezone DateOnly Refactor (P04) - Pattern Map

**Mapped:** 2026-05-28
**Files analyzed:** 22 (8 Model + 7 Controller + 2 Service + 1 Migration NEW + 1 Test NEW + 3 categori Razor view audit only)
**Analogs found:** 22 / 22 (100% coverage — semua role punya analog konkret di repo)

> Bahasa: Bahasa Indonesia (mengikuti `CLAUDE.md`).
> Read-only scope: agent ini TIDAK modify source code. Hanya tulis file ini.

---

## File Classification

Tabel berikut konsolidasi semua file yang akan dimodifikasi (M) atau dibuat baru (N) di Phase 327, sumber dari CONTEXT.md §Implementation Order + RESEARCH.md Audit 1-12.

| # | Target File | M/N | Role | Data Flow | Closest Analog | Match Quality |
|---|-------------|-----|------|-----------|----------------|---------------|
| 1 | `HcPortal.Tests/CertificateStatusTests.cs` | N | test (xUnit) | pure-function test | `HcPortal.Tests/FileUploadHelperTests.cs` | exact (sibling test class, vanilla Assert, BI komentar) |
| 2 | `Models/TrainingRecord.cs` | M | entity (EF) | property + computed | `Models/AssessmentSession.cs` (sibling entity ValidUntil flip) | exact |
| 3 | `Models/AssessmentSession.cs` | M | entity (EF) | property | `Models/TrainingRecord.cs` (sibling entity) | exact |
| 4 | `Models/UnifiedTrainingRecord.cs` | M | view-model rollup | read-transform | `Models/CertificationManagementViewModel.cs` SertifikatRow | role-match |
| 5 | `Models/AnalyticsDashboardViewModel.cs` (ExpiringSoonItem) | M | view-model rollup | read-projection | `Models/UnifiedTrainingRecord.cs` (sibling rollup) | role-match |
| 6 | `Models/CertificationManagementViewModel.cs` | M | view-model + static derive method | read-transform | (self-modify) — analog method `TrainingRecord.IsExpiringSoon` | exact |
| 7 | `Models/CreateTrainingRecordViewModel.cs` | M | input VM (form binder) | request bind | `Models/EditTrainingRecordViewModel.cs` (sibling input VM) | exact |
| 8 | `Models/EditTrainingRecordViewModel.cs` | M | input VM (form binder) | request bind | `Models/CreateTrainingRecordViewModel.cs` (sibling) | exact |
| 9 | `Models/CreateManualAssessmentViewModel.cs` | M | input VM (form binder) | request bind | `Models/CreateTrainingRecordViewModel.cs` (sibling) | role-match |
| 10 | `Controllers/TrainingAdminController.cs` (Import + Add + Edit) | M | controller (MVC POST) | request→DB write + Excel parse | Phase 326 baseline Add/Edit handler (self prior phase) | exact |
| 11 | `Controllers/AssessmentAdminController.cs` | M | controller (MVC POST) | request→DB write + renewal pre-fill | `TrainingAdminController.cs` (sibling admin POST) | exact |
| 12 | `Controllers/CMPController.cs` | M | controller (mixed: view + JSON + Excel + PDF) | read-transform multi | `Controllers/CDPController.cs` (sibling read-aggregator) | exact |
| 13 | `Controllers/CDPController.cs` | M | controller (read aggregator) | read-transform | `Controllers/CMPController.cs` (sibling) | exact |
| 14 | `Controllers/RenewalController.cs` | M | controller (read aggregator) | read-transform | `Controllers/CMPController.cs` (sibling rollup orderby) | exact |
| 15 | `Controllers/HomeController.cs` | M | controller (dashboard counts) | read-aggregate | `Controllers/CMPController.cs` GetKPIData area | role-match |
| 16 | `Controllers/AdminBaseController.cs` | M | controller helper base | shared rollup build | (self — `BuildRenewalRowsAsync` analog di CMP) | exact |
| 17 | `Services/GradingService.cs` | M | service (cascade update) | DB write (ExecuteUpdateAsync) | `Services/WorkerDataService.cs` (sibling service) | role-match |
| 18 | `Services/WorkerDataService.cs` | M | service (read) | read-transform | `Services/GradingService.cs` (sibling) | role-match |
| 19 | `Migrations/{ts}_ChangeValidUntilToDateOnly.cs` | N | EF migration | schema alter | `Migrations/20260402024553_FixInterviewResultsJsonColumnType.cs` | **EXACT** — same `AlterColumn<T>` pattern type-change |
| 20 | `Migrations/ApplicationDbContextModelSnapshot.cs` | M (auto) | EF snapshot | metadata | auto-generated, no manual edit | n/a |
| 21 | Razor views 5 halaman wajib | smoke only | view (display) | render-only | `Views/Admin/EditAssessment.cshtml:458-462` (DateOnly compat baseline) | audit-only |
| 22 | `IT_NOTIFY.md` (inline sqlcmd block) | N | docs | runbook | Phase 323/324 `IT_NOTIFY.md` (sibling phase artifact) | exact |

---

## Pattern Assignments

### 1. `HcPortal.Tests/CertificateStatusTests.cs` (NEW test file, xUnit pure-function)

**Analog:** `HcPortal.Tests/FileUploadHelperTests.cs` (Phase 325 baseline, 191 baris, 9 test method)

**Imports + namespace pattern** (FileUploadHelperTests.cs L1-14):
```csharp
// Unit test FileUploadHelper.ValidateCertificateFile (Phase 325 Plan 01 — Wave 0 foundation).
// 5 test GREEN sekarang ...

using HcPortal.Helpers;
using HcPortal.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HcPortal.Tests;

public class FileUploadHelperTests
{
```
→ Mirror exactly untuk file baru: top comment Bahasa Indonesia menyebut "Phase 327", `using HcPortal.Models;` + `using Xunit;`, `namespace HcPortal.Tests;`, `public class CertificateStatusTests`.

**[Fact] pattern** (FileUploadHelperTests.cs L48-54):
```csharp
[Fact]
public void ValidateCertificateFile_NullFile_ReturnsValid()
{
    var (ok, err) = FileUploadHelper.ValidateCertificateFile(null);
    Assert.True(ok);
    Assert.Null(err);
}
```
→ Style **vanilla `Assert.Equal/True/Null`** (NO FluentAssertions per D-14). Naming convention `Method_Scenario_ExpectedOutcome`.

**[Theory] + [InlineData] pattern to apply** (RESEARCH.md Pattern 4 ready-to-paste):
```csharp
[Theory]
[InlineData(100, "Annual", CertificateStatus.Aktif)]
[InlineData(30, "Annual", CertificateStatus.AkanExpired)]
[InlineData(1, "Annual", CertificateStatus.AkanExpired)]
[InlineData(0, "Annual", CertificateStatus.AkanExpired)]
[InlineData(-1, "Annual", CertificateStatus.Expired)]
[InlineData(100, "Permanent", CertificateStatus.Permanent)]
public void DeriveCertificateStatus_VariousScenarios_ReturnsExpected(
    int offset, string certificateType, CertificateStatus expected)
{
    var result = SertifikatRow.DeriveCertificateStatus(Today(offset), certificateType);
    Assert.Equal(expected, result);
}
```

**Coverage:** 6 Theory case + 2 Fact (null+nonPermanent → Expired; null+Permanent → Permanent) = 8 method total.

---

### 2. `Models/TrainingRecord.cs` (entity flip + computed props rewrite)

**Analog:** `Models/AssessmentSession.cs` (sibling entity, identical pattern at `:65`)

**Property flip target** (TrainingRecord.cs L43, verified):
```csharp
// SEBELUM:
public DateTime? ValidUntil { get; set; }  // Certificate validity end date

// SESUDAH:
public DateOnly? ValidUntil { get; set; }  // Certificate validity end date (Phase 327 P04 DateOnly migration)
```

**Computed prop rewrite** (TrainingRecord.cs L71-95, verified):
```csharp
// SEBELUM (L71-82):
public bool IsExpiringSoon
{
    get
    {
        if (ValidUntil.HasValue && Status == "Valid")
        {
            var daysUntilExpiry = (ValidUntil.Value - DateTime.UtcNow).Days;
            return daysUntilExpiry <= 30 && daysUntilExpiry >= 0;
        }
        return false;
    }
}

// SESUDAH (per D-09 + D-10, RESEARCH.md Audit 5):
public bool IsExpiringSoon
{
    get
    {
        if (ValidUntil.HasValue && Status == "Valid")
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var daysUntilExpiry = ValidUntil.Value.DayNumber - today.DayNumber;
            return daysUntilExpiry <= 30 && daysUntilExpiry >= 0;
        }
        return false;
    }
}
```
Sama pattern untuk `DaysUntilExpiry` (L85-95).

---

### 3. `Models/AssessmentSession.cs` (entity flip — single line)

**Analog:** `Models/TrainingRecord.cs:43` (sibling)

**Target:** L65 `public DateTime? ValidUntil { get; set; }` → `public DateOnly? ValidUntil { get; set; }`. Zero computed prop di entity ini.

---

### 4. `Models/CertificationManagementViewModel.cs` (VM + DeriveCertificateStatus + 2 rollup)

**Analog:** SELF (DeriveCertificateStatus body) + `Models/TrainingRecord.cs:71-82` (DayNumber arithmetic pattern)

**Existing method to refactor** (CertificationManagementViewModel.cs L48-63, verified):
```csharp
// SEBELUM:
public static CertificateStatus DeriveCertificateStatus(DateTime? validUntil, string? certificateType)
{
    if (certificateType == "Permanent")
        return CertificateStatus.Permanent;
    if (validUntil == null)
        return CertificateStatus.Expired;
    var days = (validUntil.Value - DateTime.UtcNow).Days;
    if (days < 0) return CertificateStatus.Expired;
    if (days <= 30) return CertificateStatus.AkanExpired;
    return CertificateStatus.Aktif;
}
```

**Refactor (per D-06, ready-to-paste per RESEARCH.md "Code Examples"):**
```csharp
public static CertificateStatus DeriveCertificateStatus(DateOnly? validUntil, string? certificateType)
{
    if (certificateType == "Permanent") return CertificateStatus.Permanent;
    if (validUntil == null) return CertificateStatus.Expired;
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var days = validUntil.Value.DayNumber - today.DayNumber;
    if (days < 0) return CertificateStatus.Expired;
    if (days <= 30) return CertificateStatus.AkanExpired;
    return CertificateStatus.Aktif;
}
```

**3 property flip:**
- L38 `public DateTime? ValidUntil { get; set; }` (SertifikatRow) → `DateOnly?`
- L74 `public DateTime? LatestValidUntil { get; set; }` (CertificateChainGroup) → `DateOnly?`
- L108 `public DateTime? MinValidUntil { get; set; }` (RenewalGroup, **bukan CertificateGroup** per RESEARCH Audit confirm) → `DateOnly?`

---

### 5. `Models/UnifiedTrainingRecord.cs` (rollup VM flip + IsExpired alignment)

**Analog:** `Models/TrainingRecord.cs:71-82` (DayNumber arithmetic) + `SertifikatRow.DeriveCertificateStatus` (paralel approach)

**Touch sites (verified RESEARCH Audit 4):**
- L26 `public DateTime? ValidUntil { get; set; }` → `DateOnly?`
- L40 `public bool IsExpired => ValidUntil.HasValue && ValidUntil.Value < DateTime.Now;` → rewrite full:
  ```csharp
  public bool IsExpired => ValidUntil.HasValue && ValidUntil.Value < DateOnly.FromDateTime(DateTime.UtcNow);
  ```
  *(D-09 UtcNow alignment kill 2nd tz bug DateTime.Now → UtcNow)*

---

### 6. `Models/AnalyticsDashboardViewModel.cs` (ExpiringSoonItem.TanggalExpired flip)

**Analog:** `Models/CertificationManagementViewModel.cs:74` (CertificateChainGroup.LatestValidUntil — sibling rollup VM prop, identical flip)

**Target** (per RESEARCH Audit 2 confirmed via grep):
- L68 `public DateTime TanggalExpired { get; set; }` (NON-nullable, beda dari ValidUntil) → `public DateOnly TanggalExpired { get; set; }`
- Assign sites di CMPController auto-compat via `t.ValidUntil!.Value` (DateOnly?.Value → DateOnly).

---

### 7-9. `Models/Create+Edit{Training,ManualAssessment}ViewModel.cs` (4 input VM flip)

**Analog:** Sibling VM (`EditTrainingRecordViewModel.cs:53` ↔ `CreateTrainingRecordViewModel.cs:51`)

**Pattern (verified L51, L53, L34, L97):**
```csharp
// SEBELUM:
[DataType(DataType.Date)]
[Display(Name = "Valid Until")]
public DateTime? ValidUntil { get; set; }

// SESUDAH:
[DataType(DataType.Date)]
[Display(Name = "Valid Until")]
public DateOnly? ValidUntil { get; set; }
```
**Note (per RESEARCH Audit 9 + Pitfall 5):** `[DataType(DataType.Date)]` annotation tetap (D-04). **JANGAN pre-add `[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]`** — smoke first. Tambah hanya kalau browser inspect render `dd-MM-yyyy` (TagHelper bug #47628 confirmed aktif).

---

### 10. `Controllers/TrainingAdminController.cs` (ImportTraining + Add + Edit POST)

**Analog:** Self post-Phase 326 baseline (Add/Edit handler L442+ EditTrainingRecordViewModel 3 field renewal, per memory).

**Pattern A — ImportTraining ClosedXML cast** (RESEARCH Audit 1 L1037 + L1138 confirmed):
```csharp
// SEBELUM (L1037 Assessment branch):
ValidUntil = DateTime.TryParse(validUntilStr, out var vu) ? vu : (DateTime?)null,

// SESUDAH:
ValidUntil = DateTime.TryParse(validUntilStr, out var vu) ? DateOnly.FromDateTime(vu) : (DateOnly?)null,
```
Sama pattern di L1138 (Training branch).

**Pattern B — Other ValidUntil assign call sites** (RESEARCH Audit 4 L269-770 enumerated): auto-compat setelah VM flip. Verify build PER step.

---

### 11. `Controllers/AssessmentAdminController.cs` (renewal pre-fill + POST)

**Analog:** `Controllers/TrainingAdminController.cs` (sibling admin POST handler) + same DateOnly.AddYears() API

**Pattern (RESEARCH Audit 4 L675-779 enumerated):**
```csharp
// VERIFIED COMPAT (no rewrite — DateOnly.AddYears native):
if (sourceSession.ValidUntil.HasValue)
    model.ValidUntil = sourceSession.ValidUntil.Value.AddYears(1);
```
Zero touch — auto-compat. Build verify.

---

### 12. `Controllers/CMPController.cs` (10× `var today` rewrite + query + PDF + Excel + JSON)

**Analog:** Self (10 `var today` declarations identical pattern, RESEARCH Audit 4 L2517-3152).

**Pattern A — `var today` rewrite (10 sites)** (RESEARCH Code Examples ready-to-paste):
```csharp
// SEBELUM (L2517, L2737, L2821, L2856, L2935, L2975, L3029, L3057, L3105, L3152):
var today = DateTime.UtcNow.AddHours(7).Date;
var thirtyDaysFromNow = today.AddDays(30);

// SESUDAH:
var today = DateOnly.FromDateTime(DateTime.UtcNow);
var thirtyDaysFromNow = today.AddDays(30);
// Query expr di bawahnya UNCHANGED — EF Core 8 native DateOnly LINQ translation
```

**Pattern B — Query compatibility (no touch needed)**:
```csharp
// L2597-2599, L2620-2622, L2762-2764, L2980-3000: AUTO-COMPAT
t.ValidUntil >= today && t.ValidUntil <= thirtyDaysFromNow  // EF Core 8 native
```

**Pattern C — PDF QuestPDF (no touch)** (L2083-2086):
```csharp
.Text($"Berlaku Hingga: {assessment.ValidUntil.Value.ToString("dd MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("id-ID"))}")
// DateOnly.ToString format specifier identik DateTime → output identik
```

**Pattern D — Anonymous JSON object (no touch)** (L2992, L3011):
```csharp
tanggalExpired = t.ValidUntil!.Value  // type inferred DateOnly post-flip
```

---

### 13-14. `Controllers/CDPController.cs` + `Controllers/RenewalController.cs`

**Analog:** `Controllers/CMPController.cs` (sibling — same SertifikatRow build + DeriveCertificateStatus call sites).

**Auto-compat sites:** CDPController L3674, L3753, L3822-3858, L3885; RenewalController L46, L62, L122-148.

**CRITICAL FIX sites (NEEDS REWRITE, per RESEARCH Audit 4 Pitfall 2):**
```csharp
// SEBELUM (RenewalController.cs:189, 198, 201, 263, 281, 289, 339):
g.OrderByDescending(c => c.ValidUntil ?? DateTime.MaxValue).ToList()

// SESUDAH:
g.OrderByDescending(c => c.ValidUntil ?? DateOnly.MaxValue).ToList()
```
Sama pattern di `AdminBaseController.cs:131, 201`.

---

### 15. `Controllers/HomeController.cs` (3× `var today` + cert alert counts)

**Analog:** `Controllers/CMPController.cs:2517` (same `var today` rewrite pattern, sibling controller)

**Touch (RESEARCH Audit 4 verified):** L78, L164, L277 — `var today = DateTime.UtcNow.AddHours(7).Date;` → `var today = DateOnly.FromDateTime(DateTime.UtcNow);`. L103, L111, L165-206 query expr auto-compat.

---

### 16. `Controllers/AdminBaseController.cs` (shared rollup helper)

**Analog:** `Controllers/CMPController.cs` SertifikatRow build pattern (rollup query similar shape).

**Touch:** L131, L201 — `?? DateTime.MaxValue` → `?? DateOnly.MaxValue` (cascade fix per Pitfall 2). L132, L186 DeriveCertificateStatus call sites auto-compat.

---

### 17. `Services/GradingService.cs` (ExecuteUpdateAsync cascade)

**Analog:** `Services/WorkerDataService.cs` (sibling service, read pattern) — read-only, no Direct ValidUntil mutate. **GradingService unique cascade write — no closer analog**.

**Touch (RESEARCH Audit 6 + "Code Examples"):**
```csharp
// SEBELUM (L465 RegradeAfterEditAsync Pass→Fail revoke):
.SetProperty(r => r.ValidUntil, (DateTime?)null)

// SESUDAH:
.SetProperty(r => r.ValidUntil, (DateOnly?)null)

// SEBELUM (L488):
var validUntil = certNow.AddYears(3);  // certNow = DateTime.Now (L476)

// SESUDAH:
var validUntil = DateOnly.FromDateTime(certNow).AddYears(3);
// L493 SetProperty auto-compat
```

---

### 18. `Services/WorkerDataService.cs` (read-only auto-compat)

**Analog:** `Services/GradingService.cs` (sibling)

**Touch:** L65 (assign in UnifiedTrainingRecord build), L264 (`tr.IsExpiringSoon` count) — **auto-compat zero rewrite** setelah entity + UnifiedTrainingRecord flip. Build verify only.

---

### 19. `Migrations/{ts}_ChangeValidUntilToDateOnly.cs` (NEW EF migration)

**Analog:** `Migrations/20260402024553_FixInterviewResultsJsonColumnType.cs` — **EXACT MATCH** sibling migration yang juga AlterColumn type-change tanpa data drop.

**Analog file (verified, 36 baris):**
```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    public partial class FixInterviewResultsJsonColumnType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "InterviewResultsJson",
                table: "AssessmentSessions",
                type: "NVARCHAR(MAX)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "InterviewResultsJson",
                table: "AssessmentSessions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR(MAX)",
                oldNullable: true);
        }
    }
}
```

**Expected output Phase 327 (RESEARCH Pattern 2):**
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AlterColumn<DateOnly>(
        name: "ValidUntil",
        table: "TrainingRecords",
        type: "date",
        nullable: true,
        oldClrType: typeof(DateTime),
        oldType: "datetime2",
        oldNullable: true);

    migrationBuilder.AlterColumn<DateOnly>(
        name: "ValidUntil",
        table: "AssessmentSessions",
        type: "date",
        nullable: true,
        oldClrType: typeof(DateTime),
        oldType: "datetime2",
        oldNullable: true);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // mirror, swap DateTime ↔ DateOnly + datetime2 ↔ date
}
```

**Verification (per Pitfall 4):** Setelah `dotnet ef migrations add ChangeValidUntilToDateOnly`, review file generated. Kalau ada `DropColumn + AddColumn`, fallback manual `migrationBuilder.Sql("ALTER TABLE TrainingRecords ALTER COLUMN ValidUntil date NULL;");`.

---

### 20. `Migrations/ApplicationDbContextModelSnapshot.cs` (auto-update, no manual edit)

EF generator auto-rewrite L510 + L1829:
```csharp
// AUTO-CHANGED:
b.Property<DateTime?>("ValidUntil").HasColumnType("datetime2");
// → 
b.Property<DateOnly?>("ValidUntil").HasColumnType("date");
```

---

### 21. Razor Views (audit-only, 7 file display + 5 form)

**Analog:** `Views/Admin/EditAssessment.cshtml:458-462` — confirmed DateOnly compat baseline (`value="@Model.ValidUntil?.ToString("yyyy-MM-dd")"` + `[DataType(DataType.Date)]` + `<input type="date">`).

**Audit list (RESEARCH canonical_refs):**
- Display format `.ToString("dd MMM yyyy", culture)` + `.ToString("yyyy-MM-dd")` — DateOnly compat (specifier `d`/`M`/`y` identik output).
- Form binding `<input asp-for="ValidUntil" type="date" />` — smoke first; add `[DisplayFormat]` to VM kalau bug #47628 confirmed.

**5 halaman wajib smoke (per spec §7.6):**
1. `/Admin/ManageAssessment` tab Training
2. `/Admin/RenewalCertificate`
3. `/CMP/Records`
4. `/CDP/CertificationManagement`
5. Worker dashboard sertifikat
6. `/CMP/CertificatePdf/{id}` (PDF smoke bonus)

---

### 22. `IT_NOTIFY.md` inline sqlcmd block (NEW per phase, append/create)

**Analog:** Phase 323/324 IT_NOTIFY.md (pattern existing per memory — "MIGRATION REQUIRED" section format).

**Content (RESEARCH "Code Examples" ready-to-paste, per D-11):**
```sql
-- Konfirmasi zero row ValidUntil punya komponen jam non-zero sebelum migration.
-- Apabila result > 0, eskalasi manual review SEBELUM apply.

SELECT COUNT(*) AS TR_NonMidnight FROM TrainingRecords
WHERE ValidUntil IS NOT NULL AND CAST(ValidUntil AS TIME) <> '00:00:00';

SELECT COUNT(*) AS AS_NonMidnight FROM AssessmentSessions
WHERE ValidUntil IS NOT NULL AND CAST(ValidUntil AS TIME) <> '00:00:00';
```

Plus capture: commit hashes Phase 325+326+327 batch, migration name `ChangeValidUntilToDateOnly`, `dotnet ef database update` command.

---

## Shared Patterns (Cross-Cutting)

### Shared Pattern A: DateOnly DayNumber Arithmetic (per D-10)

**Source:** `Models/TrainingRecord.cs:75-78` (post-refactor) — single canonical formula
**Apply to:** `TrainingRecord.IsExpiringSoon`, `TrainingRecord.DaysUntilExpiry`, `CertificationManagementViewModel.DeriveCertificateStatus`, `UnifiedTrainingRecord.IsExpired`

**Canonical formula:**
```csharp
var today = DateOnly.FromDateTime(DateTime.UtcNow);
var days = validUntil.Value.DayNumber - today.DayNumber;
```

Replace EVERY occurrence of `(date - DateTime.UtcNow).Days` + `(date.Value - DateTime.UtcNow).Days` di scope ValidUntil. **Sweep grep:** `(ValidUntil.Value - DateTime` returns ≥ 3 sites.

---

### Shared Pattern B: `var today` Controller Declaration (per D-09)

**Source:** `Controllers/CMPController.cs:2517` (post-refactor) — single canonical line
**Apply to:** CMPController (10×) + HomeController (3×) = **13 sites total**

**Canonical:**
```csharp
var today = DateOnly.FromDateTime(DateTime.UtcNow);
```

**Sweep grep:** `var today = DateTime.UtcNow.AddHours(7).Date;` → return 13 hit per RESEARCH Audit 4.

**Out of scope:** `CDPController.cs:2187` `var today = DateTime.Today;` (MED-03 coaching deliverable, NOT ValidUntil context — NO TOUCH).

---

### Shared Pattern C: `?? DateOnly.MaxValue` Fallback (per Pitfall 2)

**Source:** `RenewalController.cs:189` (post-refactor)
**Apply to:** RenewalController 7 sites (L189, L198, L201, L263, L281, L289, L339) + AdminBaseController 2 sites (L131, L201) = **9 sites total**

**Canonical:**
```csharp
g.OrderByDescending(c => c.ValidUntil ?? DateOnly.MaxValue).ToList()
```

**Sweep grep:** `?? DateTime.MaxValue` di Controllers/+Services/ → filter context ValidUntil only. **Hindari false-positive:** `DateTime.MaxValue` outside ValidUntil (e.g., coaching deliverable) tetap unchanged.

---

### Shared Pattern D: ImportTraining DateOnly Cast (per D-13)

**Source:** `Controllers/TrainingAdminController.cs:1037` (post-refactor)
**Apply to:** L1037 (Assessment branch) + L1138 (Training branch) = **2 sites**

**Canonical:**
```csharp
ValidUntil = DateTime.TryParse(validUntilStr, out var vu) ? DateOnly.FromDateTime(vu) : (DateOnly?)null,
```

---

### Shared Pattern E: GradingService SetProperty Cast (per Pitfall 6)

**Source:** `Services/GradingService.cs:465` (post-refactor)
**Apply to:** L465 (explicit null) + L488 (implicit via certNow.AddYears) = **2 sites**

**Canonical:**
```csharp
.SetProperty(r => r.ValidUntil, (DateOnly?)null)
// dan:
var validUntil = DateOnly.FromDateTime(certNow).AddYears(3);
```

---

### Shared Pattern F: Razor TagHelper DateOnly Format Fallback (per Pitfall 5, conditional)

**Source:** GitHub aspnetcore #47628 author workaround
**Apply to:** 4 input VM ValidUntil property — **HANYA KALAU smoke confirm bug aktif** di .NET 8.0.418.

**Canonical (kalau perlu):**
```csharp
[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
[DataType(DataType.Date)]
public DateOnly? ValidUntil { get; set; }
```

**Decision rule:** Browser DevTools inspect `<input>` value attribute setelah render Edit form:
- `value="2027-03-15"` → tidak perlu workaround.
- `value="15-3-2027"` → tambah `[DisplayFormat]` ke 4 VM.

---

### Shared Pattern G: xUnit Test Naming + Komentar BI

**Source:** `HcPortal.Tests/FileUploadHelperTests.cs` L1-3 + L48-54
**Apply to:** All new test methods di CertificateStatusTests.cs

**Convention:**
- Top-of-file komentar Bahasa Indonesia menyebut "Phase 327 P04"
- Method naming `Method_Scenario_ExpectedOutcome`
- `using Xunit;` + `using HcPortal.Models;`
- Vanilla `Assert.Equal/True/Null` (NO FluentAssertions, NO Moq)
- `namespace HcPortal.Tests;` (file-scoped)

---

## No Analog Found

**Zero file masuk kategori ini.** Semua 22 file punya analog konkret di repo. Migration `ChangeValidUntilToDateOnly` punya analog persis di `20260402024553_FixInterviewResultsJsonColumnType.cs` (sama-sama `AlterColumn<T>` type-change). Test file punya analog persis di `FileUploadHelperTests.cs` (sama Phase 325 baseline pattern xUnit vanilla).

---

## Metadata

**Analog search scope:**
- `Models/` (22 file scanned for entity + VM siblings)
- `Controllers/` (12 file scanned for POST handler + JSON endpoint siblings)
- `Services/` (4 file scanned for cascade update siblings)
- `Migrations/` (200+ file, filter `AlterColumn<` returns 4 matches; closest = `FixInterviewResultsJsonColumnType`)
- `HcPortal.Tests/` (1 file existing = FileUploadHelperTests.cs)
- `Views/Admin/`, `Views/CMP/`, `Views/CDP/`, `Views/Shared/` (Razor display/form audit)

**Pattern extraction sources verified:**
- `Models/TrainingRecord.cs` L40-95 (Read tool)
- `Models/CertificationManagementViewModel.cs` L35-154 (Read tool)
- `HcPortal.Tests/FileUploadHelperTests.cs` L1-60 (Read tool)
- `Migrations/20260317132516_AddValidUntilToAssessmentSession.cs` (Read tool, full)
- `Migrations/20260402024553_FixInterviewResultsJsonColumnType.cs` (Read tool, full — EXACT migration analog)
- RESEARCH.md Audit 1-12 (12 audit tables cross-referenced)
- CONTEXT.md §canonical_refs + §Implementation Order (decision lock authority)

**Pattern extraction date:** 2026-05-28

**Counter-verification:** Setiap excerpt punya file:line citation. Sweep grep yang implied di Shared Pattern A/B/C punya count expectation (3+, 13, 9) untuk validation post-implementation.
