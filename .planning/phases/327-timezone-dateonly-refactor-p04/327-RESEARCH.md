# Phase 327: Timezone DateOnly Refactor (P04) - Research

**Researched:** 2026-05-28
**Domain:** EF Core 8 type migration (`DateTime? → DateOnly?`) cascade end-to-end + xUnit unit test boundary coverage
**Confidence:** HIGH (semua claim cross-verified via grep codebase + Microsoft official docs + Phase 325 baseline)

## Summary

Phase 327 mengeliminasi timezone drift permanen ValidUntil dengan flip type `DateTime? → DateOnly?` di **2 entity + 4 VM + 5 rollup props + 2 computed props + 1 static method signature**, plus EF migration `ChangeValidUntilToDateOnly` (datetime2 → date). Audit codebase concrete: **24 file C# touched** (5 Controller + 2 Service + 8 Model + 2 Migration + 7 Razor view minor format check). xUnit 8 test case di file baru `HcPortal.Tests/CertificateStatusTests.cs` mirror Phase 325 vanilla pattern (no FluentAssertions).

**Primary recommendation:** Implement sequential strict 11-step per CONTEXT.md §Implementation Order. Eksekusi xUnit foundation Wave 0 dulu (baseline GREEN dengan DateTime arithmetic) → entity flip → computed props rewrite → VM cascade → DeriveCertificateStatus refactor → controller call-site fix → ImportTraining audit → **EF migration apply lokal dengan SEED_WORKFLOW backup** → Razor smoke 5 halaman wajib + PDF + Network tab JSON inspect. Critical risk: **TagHelper `asp-for` DateOnly format bug (dotnet/aspnetcore #47628)** — mitigation via `[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]` attribute kalau smoke gagal.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Strategy A DateOnly migration (bukan B UtcNow standardize). Cert validity semantik harian — komponen jam tidak relevan.
- **D-02:** Migration name `ChangeValidUntilToDateOnly`. Apply: `TrainingRecords.ValidUntil datetime2 → date`, `AssessmentSessions.ValidUntil datetime2 → date`. SQL Server auto `CAST(datetime2 AS date)` truncate time component.
- **D-03:** Rollback EF `Down()` revert date → datetime2 (data jam = 00:00:00 acceptable lossy).
- **D-04:** Razor `[DataType(DataType.Date)]` annotation tetap (kompatibel DateOnly?).
- **D-05:** .NET 8 confirmed (`<TargetFramework>net8.0</TargetFramework>`) → DateOnly native binder + EF Core LINQ translation native.
- **D-06:** DeriveCertificateStatus signature refactor `DateOnly? validUntil`. Today = `DateOnly.FromDateTime(DateTime.UtcNow)`. `days = validUntil.DayNumber - today.DayNumber`.
- **D-07:** Entity + ALL 4 VM flip DateOnly?:
  - `Models/CreateTrainingRecordViewModel.cs:51`
  - `Models/EditTrainingRecordViewModel.cs:53`
  - `Models/CreateManualAssessmentViewModel.cs:34, 97` (Create + EditManualAssessmentViewModel)
  - `Models/CertificationManagementViewModel.cs:38` (SertifikatRow.ValidUntil)
- **D-08:** Semua rollup props flip DateOnly?:
  - `Models/CertificationManagementViewModel.cs:74` (CertificateChainGroup.LatestValidUntil)
  - `Models/CertificationManagementViewModel.cs:108` (CertificateGroup.MinValidUntil) — actual: `RenewalGroup.MinValidUntil` per grep
  - `Models/UnifiedTrainingRecord.cs:26` (UnifiedTrainingRecord.ValidUntil)
  - `Models/UnifiedTrainingRecord.cs:40` (IsExpired) — flip + DateTime.Now → UtcNow alignment
  - `Models/AnalyticsDashboardViewModel.cs:68` (ExpiringSoonItem.TanggalExpired) — **CONFIRMED nama exact via grep** (bukan RenewalCertificateRow)
- **D-09:** `DateOnly.FromDateTime(DateTime.UtcNow)` per spec verbatim.
- **D-10:** Rewrite computed props `DaysUntilExpiry` + `IsExpiringSoon` ke DateOnly arithmetic. Pattern: `(ValidUntil.Value.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber)`.
- **D-11:** Pre-migration sqlcmd inline IT_NOTIFY.md + execute lokal manual. Snapshot DB pakai SEED_WORKFLOW.
- **D-12:** Defer Phase 326 sisa finding (validator order self-renewal di Edit + Tom Select UX) ke v20.0 backlog.
- **D-13:** ImportTraining controller `TrainingAdminController.cs:1037` (Assessment) + `:1138` (Training) — cast `DateTime.TryParse(...) → DateOnly.FromDateTime(...)`. Existing pattern pakai string parse, bukan ClosedXML `cell.GetDateTime()`.
- **D-14:** xUnit + Assert vanilla (no FluentAssertions). Test file `HcPortal.Tests/CertificateStatusTests.cs`. Coverage minimum 8 case (Theory + InlineData).
- **D-15:** Audit JSON API endpoint, accept default `"yyyy-MM-dd"` System.Text.Json serialization. Mitigasi: Network tab inspect 5 halaman wajib.

### Claude's Discretion

- Test file naming: `CertificateStatusTests.cs` (per-class pattern mirror `FileUploadHelperTests.cs`).
- Theory data: `[InlineData(...)]` inline (8 case manageable, MemberData overkill).
- Razor format string: biarkan apa adanya (DateOnly default IFormatProvider OK untuk specifier `yyyy-MM-dd` + `dd MMMM yyyy`).
- EF migration file edit: accept generated `AlterColumn` kalau valid; manual `migrationBuilder.Sql(...)` fallback kalau EF generate weird drop+re-add.

### Deferred Ideas (OUT OF SCOPE)

- Phase 326 sisa non-blocking finding (validator order self-renewal + Tom Select UX) → v20.0 backlog
- `DateTime.Now` standardize di non-ValidUntil sites (audit/logging/CreatedAt) → v20.0
- Helper `Clock.Today()` centralized + DI swap → defer indefinitely
- Explicit `JsonConverter` DateOnly format spoof → lawan tujuan migrasi
- P05 Soft Delete proper → v20.0
- P09 DB CHECK constraint Permanent+ValidUntil → v20.0
- P11 Renewal cache + P12 RBAC integration test → v20.0
- TrainingRecord computed props delete kalau unused → audit confirmed: `IsExpiringSoon` ada 1 call site di WorkerDataService.cs:264 (KEEP, rewrite per D-10); `DaysUntilExpiry` zero call site di Controllers/Services/Views (grep verified) — **decision Q3 default rewrite (D-10) tetap berlaku**, tidak delete.

</user_constraints>

## Project Constraints (from CLAUDE.md)

- Respond in **Bahasa Indonesia** (semua komentar code, error message, log message).
- **Develop Workflow:** Lokal → Dev → Prod. Verifikasi lokal wajib `dotnet build` + `dotnet run` di `http://localhost:5277` + DB lokal smoke + Playwright kalau ada. Push ke origin/main hanya setelah verifikasi lokal pass. IT promo Dev/Prod = tanggung jawab IT (notify dengan commit hash + flag migration).
- **Seed Data Workflow:** Snapshot DB lokal via `sqlcmd ... BACKUP DATABASE` SEBELUM apply migration. Catat di `docs/SEED_JOURNAL.md` dengan klasifikasi (Phase 327 = `permanent + prod-required` karena schema change, BUKAN seed temporary — tapi pre-migration snapshot tetap dipakai untuk rollback safety). Restore kalau drama.
- **Jangan edit kode/DB langsung di server Dev/Prod** + **jangan push tanpa verifikasi lokal**.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| EF migration type alter (datetime2 → date) | Database / Storage | API / Backend | EF Core 8 native `AlterColumn` translation; SQL Server `CAST` implicit |
| Entity property flip ValidUntil | API / Backend (Models) | Database / Storage | EF model property change drives migration generation |
| DeriveCertificateStatus computation | API / Backend (Models) | — | Pure function, business logic — ZERO browser-tier presence |
| Razor TagHelper form binding | Frontend Server (SSR) | Browser / Client | `asp-for` runs server-side; `<input type="date">` HTML5 native client widget |
| JSON API response serialization | API / Backend (Controllers) | Browser / Client | System.Text.Json server-side; consumer JS `new Date(jsonString)` parses client-side |
| Excel parse ImportTraining | API / Backend (Controllers) | — | ClosedXML server-side workbook read |
| PDF generation `/CMP/CertificatePdf` | API / Backend (Controllers) | — | QuestPDF server-side render, response = PDF bytes |
| xUnit test execution | Test Harness | — | `dotnet test` CLI, separate sibling project HcPortal.Tests/ |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET | 8.0.418 | Target framework | Confirmed via `HcPortal.csproj:3` + `dotnet --version` [VERIFIED: dotnet CLI] |
| EF Core SqlServer | 8.0.0 | ORM + migrations | Confirmed `HcPortal.csproj:18` [VERIFIED: csproj] |
| EF Core Tools | 8.0.0 | `dotnet ef migrations add/update` CLI | `HcPortal.csproj:19` [VERIFIED: csproj] |
| xUnit | 2.9.3 | Unit test framework | `HcPortal.Tests/HcPortal.Tests.csproj:13` [VERIFIED: csproj, Phase 325 baseline] |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test runner discovery | `HcPortal.Tests.csproj:12` [VERIFIED: csproj] |
| xunit.runner.visualstudio | 3.0.1 | VS + dotnet test runner adapter | `HcPortal.Tests.csproj:14` [VERIFIED: csproj] |
| ClosedXML | 0.105.0 | Excel parse ImportTraining | `HcPortal.csproj:14` [VERIFIED: csproj] |
| QuestPDF | 2026.2.2 | PDF rendering CertificatePdf | `HcPortal.csproj:27` [VERIFIED: csproj] |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Text.Json | (bundled .NET 8) | JSON serialization `return Json(...)` | Default per ASP.NET Core; DateOnly serialize `"yyyy-MM-dd"` string format [CITED: learn.microsoft.com System.Text.Json] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| DateOnly migration | UtcNow standardize (Strategy B) | Rejected per D-01: rentan miss occurrence, helper `WibToUtc()` fragile |
| xUnit + Assert vanilla | FluentAssertions | Rejected per D-14: zero new dependency, Phase 325 baseline match |
| Custom `JsonConverter<DateOnly>` | Default System.Text.Json | Rejected per D-15: lawan tujuan migrasi, default `"yyyy-MM-dd"` cukup |
| Helper `Clock.Today()` DI swap | Direct `DateOnly.FromDateTime(DateTime.UtcNow)` | Rejected: overkill tanpa strategi clock-mocking lebih luas |

**No new packages required** — `D-14 + D-15 + D-09` semua leverage existing dependencies.

**Version verification:**
- .NET 8.0.418: `dotnet --version` [VERIFIED: 2026-05-28 13:00 WIB local]
- ClosedXML 0.105.0: confirmed in `HcPortal.csproj:14`. Latest stable [VERIFIED: code-maze + ClosedXML wiki via WebSearch 2026-05-28]
- EF Core 8.0.0 native DateOnly support: [CITED: learn.microsoft.com EF Core 8 breaking changes — "date and time are scaffolded as DateOnly and TimeOnly"]

## Architecture Patterns

### System Architecture Diagram

```
                    ┌─────────────────────────────────────────────────────────┐
                    │              Phase 327 Data Flow                         │
                    └─────────────────────────────────────────────────────────┘

   [Browser]                       [Frontend Server / Razor SSR]
   ────────                        ────────────────────────────
   <input type="date">             asp-for="ValidUntil"             [DataType(Date)]
   value="yyyy-MM-dd"   ────POST──> TagHelper renders               + DateOnly?
                                    yyyy-MM-dd binding              VM property
                                         │
                                         ▼
                              ModelBinder DateOnly parse
                                  (ASP.NET Core 8 native)
                                         │
                                         ▼
                              [API / Controller]
                              ─────────────────
                              POST /Admin/AddTraining
                              POST /Admin/EditTraining
                              POST /Admin/AddManualAssessment
                              POST /Admin/EditManualAssessment
                              POST /Admin/ImportTraining (Excel)
                              POST /Admin/Create/EditAssessment
                                         │
                                         ▼
                              [Service Layer]
                              ──────────────
                              GradingService.RegradeAfterEditAsync
                                  ExecuteUpdateAsync SetProperty(ValidUntil, ...)
                                         │
                                         ▼
                              [Entity / EF Model]
                              ──────────────────
                              TrainingRecord.ValidUntil : DateOnly?
                              AssessmentSession.ValidUntil : DateOnly?
                                         │
                                         ▼
                              [Database / SQL Server]
                              ─────────────────────
                              TrainingRecords.ValidUntil : date
                              AssessmentSessions.ValidUntil : date
                                         │
                              ┌──────────┴──────────────┐
                              │                          │
                              ▼                          ▼
                       [Read Path]                [Display Path]
                       ──────────                 ──────────────
              SertifikatRow.DeriveCertificateStatus    Razor format
              (DateOnly? validUntil, string?)          .ToString("dd MMM yyyy")
                       │                                .ToString("yyyy-MM-dd")
                       ▼
              CertificateStatus enum                   [PDF QuestPDF]
              (Aktif/AkanExpired/Expired/Permanent)    "dd MMMM yyyy" id-ID
                       │
                       ▼
              [JSON API consumer]
              ──────────────────
              GetExpiringSoonData → "yyyy-MM-dd"
              analyticsDashboard.js: new Date("yyyy-MM-dd")
                  → UTC midnight interpret
                  → .toLocaleDateString('id-ID') WIB convert
              ⚠️ RISK: boundary day shift kalau jam server UTC
```

### Pattern 1: DateOnly Computation via DayNumber
**What:** Replace `(DateTime - DateTime).Days` dengan `(DateOnly.DayNumber - DateOnly.DayNumber)`.
**When to use:** Setiap arithmetic boundary check ValidUntil — `IsExpiringSoon`, `DaysUntilExpiry`, `DeriveCertificateStatus`.
**Example:**
```csharp
// Source: Models/CertificationManagementViewModel.cs (refactored per D-06 spec §7.3)
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

### Pattern 2: EF Migration AlterColumn Type Change
**What:** EF Core 8 native `AlterColumn` translation untuk type change `datetime2 → date`.
**When to use:** Single migration `ChangeValidUntilToDateOnly` covering 2 tabel.
**Example expected output (generated):**
```csharp
// Migrations/{timestamp}_ChangeValidUntilToDateOnly.cs (EXPECTED — verify after `dotnet ef migrations add`)
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
    migrationBuilder.AlterColumn<DateTime>(
        name: "ValidUntil",
        table: "TrainingRecords",
        type: "datetime2",
        nullable: true,
        oldClrType: typeof(DateOnly),
        oldType: "date",
        oldNullable: true);
    // (sama untuk AssessmentSessions)
}
```
**Verification:** Setelah `dotnet ef migrations add ChangeValidUntilToDateOnly`, **WAJIB review file generated** — kalau EF generate `DropColumn` + `AddColumn` (data loss path), pakai manual `migrationBuilder.Sql("ALTER TABLE TrainingRecords ALTER COLUMN ValidUntil date NULL;")` fallback. [CITED: learn.microsoft.com EF Core 8 breaking changes — "if the column is constrained in some way ... AlterColumn may fail"]

### Pattern 3: ClosedXML String Parse → DateOnly Cast
**What:** Existing import code pakai string parse (`DateTime.TryParse(validUntilStr, out var vu)`), bukan native `cell.GetDateTime()`. Cast result via `DateOnly.FromDateTime(vu)`.
**Reference:** `Controllers/TrainingAdminController.cs:1037` (Assessment) + `:1138` (Training).
**Example (post-flip):**
```csharp
// SEBELUM (Training row L1138):
ValidUntil = DateTime.TryParse(validUntilStr, out var vu) ? vu : (DateTime?)null,

// SESUDAH:
ValidUntil = DateTime.TryParse(validUntilStr, out var vu) ? DateOnly.FromDateTime(vu) : (DateOnly?)null,
```
**Note:** ClosedXML 0.105 **TIDAK support DateOnly native** [VERIFIED: github.com/ClosedXML/ClosedXML/issues/2227 via WebSearch] — cast manual wajib.

### Pattern 4: xUnit Theory + InlineData untuk Boundary Coverage
**What:** Parameterized test 8 case di file baru `CertificateStatusTests.cs`. Mirror Phase 325 `FileUploadHelperTests.cs` vanilla pattern.
**Example:**
```csharp
// Source: HcPortal.Tests/CertificateStatusTests.cs (NEW — Phase 327)
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class CertificateStatusTests
{
    // Helper: today + offset days, terbungkus DateOnly
    private static DateOnly Today(int offset) =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(offset);

    [Theory]
    // validUntilOffset, certificateType, expected
    [InlineData(100, "Annual", CertificateStatus.Aktif)]              // > 30 hari
    [InlineData(30, "Annual", CertificateStatus.AkanExpired)]         // boundary inclusive
    [InlineData(1, "Annual", CertificateStatus.AkanExpired)]          // 1 hari lagi
    [InlineData(0, "Annual", CertificateStatus.AkanExpired)]          // hari ini (days = 0)
    [InlineData(-1, "Annual", CertificateStatus.Expired)]             // kemarin
    [InlineData(100, "Permanent", CertificateStatus.Permanent)]       // Permanent override
    public void DeriveCertificateStatus_VariousScenarios_ReturnsExpected(
        int offset, string certificateType, CertificateStatus expected)
    {
        var result = SertifikatRow.DeriveCertificateStatus(Today(offset), certificateType);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DeriveCertificateStatus_NullValidUntil_NonPermanent_ReturnsExpired()
    {
        var result = SertifikatRow.DeriveCertificateStatus(null, null);
        Assert.Equal(CertificateStatus.Expired, result);
    }

    [Fact]
    public void DeriveCertificateStatus_NullValidUntil_Permanent_ReturnsPermanent()
    {
        var result = SertifikatRow.DeriveCertificateStatus(null, "Permanent");
        Assert.Equal(CertificateStatus.Permanent, result);
    }
}
```
**Count:** 6 Theory case + 2 Fact = 8 test methods. Komentar Bahasa Indonesia (per CLAUDE.md).

### Recommended Project Structure
```
HcPortal/
├── Models/
│   ├── TrainingRecord.cs                       # ValidUntil: DateOnly?
│   ├── AssessmentSession.cs                    # ValidUntil: DateOnly?
│   ├── CertificationManagementViewModel.cs     # SertifikatRow + DeriveCertificateStatus + rollups
│   ├── UnifiedTrainingRecord.cs                # ValidUntil + IsExpired
│   ├── AnalyticsDashboardViewModel.cs          # ExpiringSoonItem.TanggalExpired
│   ├── CreateTrainingRecordViewModel.cs        # VM
│   ├── EditTrainingRecordViewModel.cs          # VM (post Phase 326 baseline 3 field renewal)
│   └── CreateManualAssessmentViewModel.cs      # Create + Edit ManualAssessment VM
├── Controllers/
│   ├── TrainingAdminController.cs              # AddTraining + EditTraining + ImportTraining
│   ├── AssessmentAdminController.cs            # CreateAssessment + EditAssessment + Renewal pre-fill
│   ├── CMPController.cs                        # 7× `var today = ...` + Records + Analytics endpoints
│   ├── CDPController.cs                        # CertificationManagement query
│   ├── RenewalController.cs                    # RenewalCertificate query + display
│   ├── HomeController.cs                       # CERT_EXPIRED notification + AlertCounts
│   └── AdminBaseController.cs                  # BuildRenewalRowsAsync helper
├── Services/
│   ├── GradingService.cs                       # RegradeAfterEditAsync cascade
│   └── WorkerDataService.cs                    # GetUnifiedRecords + IsExpiringSoon usage
├── Migrations/
│   └── {ts}_ChangeValidUntilToDateOnly.cs      # NEW
└── HcPortal.Tests/                             # Phase 325 bootstrap
    ├── HcPortal.Tests.csproj
    ├── FileUploadHelperTests.cs                # Phase 325 existing
    └── CertificateStatusTests.cs               # NEW Phase 327
```

### Anti-Patterns to Avoid
- **Cross-type comparison `DateOnly == DateTime`** — compile error. Setiap call site `var today = DateTime.UtcNow.AddHours(7).Date` HARUS rewrite ke `var today = DateOnly.FromDateTime(DateTime.UtcNow)` SEBELUM ValidUntil query expr lain di-flip (else build error cascade).
- **`ValidUntil.Value.ToString()` tanpa format specifier** — DateOnly default ToString tergantung culture, di .NET 8 .NET 6+ standard `yyyy-MM-dd` ISO. Tetap pakai explicit format specifier untuk safety.
- **Skip pre-migration SQL check** — D-11 sqlcmd wajib. SQL Server `CAST(datetime2 AS date)` lossy untuk komponen jam — kalau ada row punya jam non-zero, data semantik berubah.
- **Custom JsonConverter DateOnly format spoof** — D-15 reject. Default `"yyyy-MM-dd"` ISO consistent dengan tujuan migrasi.
- **Asumsi EF migration auto-perfect** — selalu review file generated SEBELUM apply. Kalau drop+re-add column (data loss), pakai manual SQL.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Date arithmetic | Custom diff calc | `DateOnly.DayNumber` subtraction | Native DateOnly API, zero ambiguity, immune timezone |
| EF type migration | Manual SQL ALTER TABLE first | `dotnet ef migrations add` then review | Generates Up/Down + snapshot update; manual SQL hanya kalau generated weird |
| Pre-migration safety check | Custom snapshot logic | `sqlcmd ... BACKUP DATABASE` per SEED_WORKFLOW.md | Existing pattern proven Phase 324 cleanup |
| DateOnly form binding | Custom IModelBinder | ASP.NET Core 8 native binder | `[DataType(DataType.Date)]` + `asp-for` works out-of-box [CITED: learn.microsoft.com aspnetcore-8.0 model-binding] |
| xUnit boundary parameterization | Repetitive `[Fact]` | `[Theory] + [InlineData]` | DRY 8 case → 1 method |
| Today-reference mocking | `IClock` DI abstraction | Direct `DateOnly.FromDateTime(DateTime.UtcNow)` | D-09 + deferred decision: overkill saat ini, defer indefinitely |

**Key insight:** EF Core 8 + ASP.NET Core 8 punya **native DateOnly support end-to-end** (binder, migration, query translation). Hand-roll = anti-pattern. Phase 327 cuma agregasi flip type + cascade fix existing pattern — bukan engineering challenge baru.

## Runtime State Inventory

> Phase 327 = refactor + migration. Audit lengkap 5 kategori wajib.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| **Stored data** | `TrainingRecords.ValidUntil` (datetime2) + `AssessmentSessions.ValidUntil` (datetime2) di SQL Server lokal + Dev + Prod | EF migration `ChangeValidUntilToDateOnly` apply (lokal manual via `dotnet ef database update`, Dev + Prod via IT promo batch). Pre-check sqlcmd D-11 confirm komponen jam zero. |
| **Live service config** | None — verified by inspection. PortalHC tidak pakai n8n/Datadog/Cloudflare/Tailscale config yang embed ValidUntil string | Tidak ada |
| **OS-registered state** | None — verified. Tidak ada Task Scheduler/pm2/systemd unit yang embed "ValidUntil" sebagai identifier (cert expiry handled via DB query, bukan OS scheduler) | Tidak ada |
| **Secrets / env vars** | None — verified by inspection `Program.cs` + `appsettings.json`. Tidak ada env var atau secret key yang embed ValidUntil | Tidak ada |
| **Build artifacts / installed packages** | `HcPortal.Tests/bin/` + `HcPortal.Tests/obj/` (Phase 325 bootstrap) — sudah gitignored | Tidak ada — `dotnet build` regenerate otomatis |

**Cascade impact in code (NOT runtime state, in-source):**
- 24 file C# touched (5 Controller + 2 Service + 8 Model + 1 Migration + 7 Razor view + 1 test).
- `ApplicationDbContextModelSnapshot.cs:510, 1829` — auto-update saat `dotnet ef migrations add`.
- `wwwroot/js/analyticsDashboard.js:852-853` — JS consumer `new Date(d.tanggalExpired)` — code TIDAK perlu ubah (string `"yyyy-MM-dd"` valid JS Date parse), tapi **risk timezone interpret** (see Pitfall 3).

## Audit 1: ImportTraining Controller Enumeration

| Aspect | Finding | Action |
|--------|---------|--------|
| Filename | `Controllers/TrainingAdminController.cs` [VERIFIED: grep] | — |
| Action method | `ImportTraining` POST L954 (single handler, 2 branch `isAssessmentImport`) | — |
| ClosedXML cell parse loop | L991 `foreach (var row in ws.RowsUsed().Skip(1))` | — |
| Cell API call | **`row.Cell(N).GetString().Trim()`** — string parse via `DateTime.TryParse` (BUKAN `cell.GetDateTime()`) | Keep pattern, cast result |
| ValidUntil parse line — Assessment branch | L1005 `var validUntilStr = row.Cell(10).GetString().Trim();` + L1037 `ValidUntil = DateTime.TryParse(validUntilStr, out var vu) ? vu : null,` | Flip L1037: `... ? DateOnly.FromDateTime(vu) : (DateOnly?)null,` |
| ValidUntil parse line — Training branch | L1095 area `var validUntilStr = row.Cell(11).GetString().Trim();` (similar pattern) + L1138 `ValidUntil = DateTime.TryParse(validUntilStr, out var vu) ? vu : (DateTime?)null,` | Flip L1138: `... ? DateOnly.FromDateTime(vu) : (DateOnly?)null,` |
| Existing null/empty guard | `string.IsNullOrWhiteSpace(...)` check + `DateTime.TryParse` returns false untuk empty → null assigned. Pattern preserved. | Tidak ada perubahan guard logic |
| Test fixture Excel file location | TIDAK ADA fixture file di `tests/` atau `wwwroot/` — template download via `DownloadImportTrainingTemplate` L841 (generated at runtime, bukan static file) | Manual smoke test create 1-row Excel saat UAT |
| Razor template doc | `Views/Admin/ImportTraining.cshtml:210, 244` — "Tanggal berlaku, format YYYY-MM-DD" — copy text tetap valid setelah flip | Tidak ada perubahan |

## Audit 2: RenewalCertificateRow VM Field Nama Exact

**CRITICAL FINDING:** Nama kelas exact = **`ExpiringSoonItem`** di `Models/AnalyticsDashboardViewModel.cs:64-70`. Property = `TanggalExpired` (DateTime). **CONTEXT.md menyebut "RenewalCertificateRow" — itu nama placeholder/working assumption, BUKAN nama exact kelas.**

| Aspect | Finding |
|--------|---------|
| Kelas exact | `ExpiringSoonItem` di `Models/AnalyticsDashboardViewModel.cs:64` |
| Property | `public DateTime TanggalExpired { get; set; }` di L68 |
| Assign site 1 | `Controllers/CMPController.cs:2611` — `TanggalExpired = t.ValidUntil!.Value,` (TrainingRecord, GetAnalyticsData) |
| Assign site 2 | `Controllers/CMPController.cs:2634` — `TanggalExpired = s.ValidUntil!.Value,` (AssessmentSession, GetAnalyticsData) |
| Read/order site | `Controllers/CMPController.cs:2641` — `.OrderBy(e => e.TanggalExpired)` |
| Anonymous object sibling (JSON return endpoint `GetExpiringSoonData`) | `Controllers/CMPController.cs:2992, 3011` — `tanggalExpired = t.ValidUntil!.Value,` (camelCase, anonymous object — NOT ExpiringSoonItem) |

**Action:** Flip `ExpiringSoonItem.TanggalExpired : DateTime → DateOnly` (L68). Setelah ValidUntil DateOnly, cast `t.ValidUntil!.Value` (DateOnly) langsung assign — `!.Value` operator tetap valid untuk DateOnly?. Anonymous object L2992 + L3011 di JSON endpoint juga inherit DateOnly automatic via type inference.

## Audit 3: JSON API Endpoint Enumeration (Return ValidUntil)

| Endpoint | Controller:Line | Response Shape | Consumer | Risk |
|----------|----------------|----------------|----------|------|
| `GET /CMP/GetExpiringSoonData` | `CMPController.cs:2972-3020` | `[{ namaPekerja, namaSertifikat, tanggalExpired, sectionUnit }, ...]` anonymous | `wwwroot/js/analyticsDashboard.js:852-853` (`new Date(d.tanggalExpired).toLocaleDateString('id-ID')`) | **MEDIUM** — JS Date parse `"yyyy-MM-dd"` interpret UTC midnight, lalu toLocaleDateString WIB convert bisa shift 1 hari kalau UTC midnight + WIB tz = next/prev day. Manual smoke verify. |
| `GET /CMP/GetTrendData` | `CMPController.cs:2926` | `{ trend, gainScoreTrend }` — TIDAK include ValidUntil | analyticsDashboard.js trend chart | Zero |
| `GET /CMP/GetKPIData` | `CMPController.cs:2812` | `{ totalSessions, passRate, expiringCount, avgGainScore }` — only int counts | analyticsDashboard.js KPI cards | Zero |
| `GET /CMP/GetEtBreakdown` | `CMPController.cs:2967` | `etBreakdown[]` — score percentages | analyticsDashboard.js | Zero |
| `GET /CMP/GetFailRateData` | `CMPController.cs:2847` | `failRate[]` | analyticsDashboard.js | Zero |
| `GET /CMP/GetFailRateDrillDown` | `CMPController.cs:3048` | `[{ namaPekerja, skor, tanggalAssessment, status }, ...]` — `tanggalAssessment` = CompletedAt (BUKAN ValidUntil, tetap DateTime) | analyticsDashboard.js drill-down modal | Zero |
| `GET /CMP/GetAnalyticsData` | `CMPController.cs:2698` | `AnalyticsDataResult` includes `ExpiringSoonItem[]` (typed `TanggalExpired`) | analyticsDashboard.js initial render | **MEDIUM** — same risk #1 |
| RenewalController endpoints | `Controllers/RenewalController.cs:46, 122, 148` | Server-side render Razor partial (BUKAN return Json) | Razor view | Zero JSON impact |
| AssessmentAdminController return Json | Multiple at L2514+, 2519, dll | NONE touch ValidUntil (auth/token/save responses) | — | Zero |

**Mitigasi (per D-15):** Smoke verify 5 halaman wajib + browser DevTools Network tab cek `GET /CMP/GetExpiringSoonData` + `GET /CMP/GetAnalyticsData` response — pastikan `tanggalExpired` serialize sebagai `"2027-03-15"` (yyyy-MM-dd ISO) bukan ISO 8601 datetime `"2027-03-15T00:00:00"`. Kalau JS render "14 Mar 2027" alih-alih "15 Mar 2027" → confirm timezone shift, escalate ke fix later.

## Audit 4: `var today = ...` + Query Expression Enumeration

### `var today` declarations (ValidUntil context)
| File:Line | Pattern | Rewrite |
|-----------|---------|---------|
| `Controllers/CMPController.cs:2517` | `var today = DateTime.UtcNow.AddHours(7).Date;` | `var today = DateOnly.FromDateTime(DateTime.UtcNow);` |
| `Controllers/CMPController.cs:2737` | same | same |
| `Controllers/CMPController.cs:2821` | same | same |
| `Controllers/CMPController.cs:2856` | same | same |
| `Controllers/CMPController.cs:2935` | same | same |
| `Controllers/CMPController.cs:2975` | same | same |
| `Controllers/CMPController.cs:3029` | same | same |
| `Controllers/CMPController.cs:3057` | same | same |
| `Controllers/CMPController.cs:3105` | same | same |
| `Controllers/CMPController.cs:3152` | same | same |
| `Controllers/HomeController.cs:78` | same | same |
| `Controllers/HomeController.cs:164` | same | same |
| `Controllers/HomeController.cs:277` | same | same |
| `Controllers/CDPController.cs:2187` | `var today = DateTime.Today;` | **NO TOUCH** — context = `MED-03 fix: bound Date coaching submit` (date di coaching deliverable, BUKAN ValidUntil). Out of scope Phase 327. |

### Query expression touch sites (ValidUntil)
| File:Line | Pattern | Notes |
|-----------|---------|-------|
| `CMPController.cs:2597-2599` | `t.ValidUntil >= today && t.ValidUntil <= thirtyDaysFromNow` (TrainingExpiring) | Compatible after flip — EF Core 8 native DateOnly LINQ → SQL `date` comparison |
| `CMPController.cs:2620-2622` | sama (AssessmentSession) | sama |
| `CMPController.cs:2762` | sama (count expiring training) | sama |
| `CMPController.cs:2764` | sama (count expiring session) | sama |
| `CMPController.cs:2980-2981` | `t.ValidUntil >= today && t.ValidUntil <= futureDate` (GetExpiringSoonData) | sama |
| `CMPController.cs:3000` | sama (Session in GetExpiringSoonData) | sama |
| `CMPController.cs:2611` | `TanggalExpired = t.ValidUntil!.Value` | Setelah ExpiringSoonItem.TanggalExpired flip DateOnly, cast `!.Value` valid (DateOnly?.Value → DateOnly) |
| `CMPController.cs:2634` | sama (Session) | sama |
| `CMPController.cs:2992` | `tanggalExpired = t.ValidUntil!.Value` (anonymous obj GetExpiringSoonData) | Anonymous prop type inferred DateOnly |
| `CMPController.cs:3011` | sama (Session anonymous) | sama |
| `CMPController.cs:626` | `r.ValidUntil?.ToString("yyyy-MM-dd")` (Excel export) | DateOnly compat — `yyyy-MM-dd` standard format |
| `CMPController.cs:2083-2086` | `assessment.ValidUntil.Value.ToString("dd MMMM yyyy", culture)` (PDF QuestPDF) | DateOnly compat — specifier `d`/`M`/`y` identik |
| `CMPController.cs:3860` | `r.ValidUntil?.ToString("dd MMM yyyy")` (Excel export team) | DateOnly compat |
| `CMPController.cs:3969, 4032, 4066, 4092` | Anonymous select `t.ValidUntil` + `ValidUntil = ...` SertifikatRow assign | All compat — DeriveCertificateStatus accepts DateOnly? after refactor |
| `CMPController.cs:4033, 4093` | `DeriveCertificateStatus(t.ValidUntil, ...)` | Compat after signature refactor |
| `CDPController.cs:3674` | `r.ValidUntil?.ToString("dd MMM yyyy")` (Excel) | DateOnly compat |
| `CDPController.cs:3753, 3822, 3858, 3885` | Same SertifikatRow build pattern | Compat |
| `CDPController.cs:3823, 3886` | `DeriveCertificateStatus(t.ValidUntil, ...)` | Compat |
| `RenewalController.cs:46, 62, 122, 123, 148, 149` | SertifikatRow build + DeriveCertificateStatus | Compat |
| `RenewalController.cs:189-190` | `g.OrderByDescending(c => c.ValidUntil ?? DateTime.MaxValue).ToList()` | **NEEDS FIX** — `DateTime.MaxValue` mismatch DateOnly. Rewrite: `?? DateOnly.MaxValue` |
| `RenewalController.cs:198, 201, 263, 281, 289, 339` | Same pattern `?? DateTime.MaxValue` | **NEEDS FIX** — all 6 sites cast ke `DateOnly.MaxValue` |
| `AdminBaseController.cs:131, 201` | `?? DateTime.MaxValue` (similar pattern in BuildRenewalRowsAsync) | **NEEDS FIX** |
| `HomeController.cs:103, 111` | `t.ValidUntil.HasValue && t.ValidUntil.Value < today` | Compat after today flip |
| `HomeController.cs:165-206` | Multi expiring/akan-expired count via LINQ Count | Compat |
| `TrainingAdminController.cs:269-270, 335, 370, 401, 446, 498-499, 538, 670, 718, 770, 1037, 1138` | Add/Edit/Import POST handlers ValidUntil assign | Compat (VM already DateOnly after flip) |
| `AssessmentAdminController.cs:675-676, 704-705, 750-751, 778-779` | `if (sourceSession.ValidUntil.HasValue) model.ValidUntil = sourceSession.ValidUntil.Value.AddYears(1);` | Compat — DateOnly punya `AddYears()` method [CITED: learn.microsoft.com System.DateOnly.AddYears] |
| `AssessmentAdminController.cs:1164, 1198, 1291, 1648, 1749` | ValidUntil assign POST CreateAssessment + EditAssessment | Compat after VM flip |
| `GradingService.cs:465` | `.SetProperty(r => r.ValidUntil, (DateTime?)null)` | **NEEDS FIX** — cast `(DateTime?)null` → `(DateOnly?)null` |
| `GradingService.cs:488, 493` | `var validUntil = certNow.AddYears(3);` + `.SetProperty(r => r.ValidUntil, validUntil)` | **NEEDS FIX** — `certNow = DateTime.Now`, `validUntil` type DateTime. Rewrite: `var validUntil = DateOnly.FromDateTime(certNow).AddYears(3);` |
| `WorkerDataService.cs:65, 264` | `ValidUntil = t.ValidUntil` + `tr.IsExpiringSoon` count | Compat |

## Audit 5: TrainingRecord Computed Props Call Sites

| Property | Grep Result | Call Sites Found |
|----------|-------------|------------------|
| `TrainingRecord.IsExpiringSoon` | **1 production call site** | `Services/WorkerDataService.cs:264` — `var expiringTrainings = trainingRecords.Count(tr => tr.IsExpiringSoon);` |
| `TrainingRecord.DaysUntilExpiry` | **0 production call sites** (grep Controllers/+Services/+Views/ returns zero hit) | None |

**Decision per D-10 (default rewrite):** KEEP both, rewrite arithmetic ke DateOnly. Rationale: `IsExpiringSoon` ada 1 active call site, rewrite trivial. `DaysUntilExpiry` unused tapi part of public API surface — opsi delete defer ke v20.0 (per CONTEXT.md deferred line 307).

**Rewrite (per D-10 + D-09):**
```csharp
// Models/TrainingRecord.cs (refactored)
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

public int? DaysUntilExpiry
{
    get
    {
        if (ValidUntil.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return ValidUntil.Value.DayNumber - today.DayNumber;
        }
        return null;
    }
}
```

## Audit 6: AssessmentSession / Service Touch Sites

| File:Line | Context | Action |
|-----------|---------|--------|
| `Services/GradingService.cs:465` | `ExecuteUpdateAsync` SetProperty cast `(DateTime?)null` (RegradeAfterEditAsync Pass→Fail revoke) | Rewrite cast → `(DateOnly?)null` |
| `Services/GradingService.cs:488` | `var validUntil = certNow.AddYears(3);` (`certNow = DateTime.Now` L476) | Rewrite: `var validUntil = DateOnly.FromDateTime(certNow).AddYears(3);` |
| `Services/GradingService.cs:493` | `.SetProperty(r => r.ValidUntil, validUntil)` | Auto-compat after L488 fix |
| `Services/WorkerDataService.cs:65` | `ValidUntil = t.ValidUntil` (UnifiedTrainingRecord build) | Auto-compat after UnifiedTrainingRecord.ValidUntil flip |
| `Services/WorkerDataService.cs:264` | `tr.IsExpiringSoon` count | Auto-compat after computed prop rewrite |
| `Controllers/AssessmentAdminController.cs:675-779` | Renewal pre-fill `model.ValidUntil = sourceSession.ValidUntil.Value.AddYears(1);` (4× occurrence) | Auto-compat — DateOnly.AddYears() native |
| `Controllers/AssessmentAdminController.cs:1037, 1138` (ImportTraining) | covered di Audit 1 | per D-13 |
| `Controllers/HomeController.cs:78-206` | `TriggerCertExpiredNotificationsAsync` + `GetCertAlertCountsAsync` | Auto-compat after today + entity flip |
| `Controllers/AdminBaseController.cs:62-201` | `BuildRenewalRowsAsync` helper | Auto-compat + fix L131, L201 `?? DateTime.MaxValue` → `?? DateOnly.MaxValue` |

## Audit 7: xUnit Project Structure HcPortal.Tests/

| Aspect | Finding |
|--------|---------|
| Directory exists | ✓ `HcPortal.Tests/` sibling HcPortal.csproj [VERIFIED: ls] |
| Project file | `HcPortal.Tests/HcPortal.Tests.csproj` 28 lines [VERIFIED] |
| SDK | `Microsoft.NET.Sdk` (BUKAN Web SDK — per Phase 325 RESEARCH §Risk 4) |
| TargetFramework | `net8.0` match parent |
| xUnit version | 2.9.3 [VERIFIED: csproj:13] |
| Microsoft.NET.Test.Sdk | 17.13.0 [VERIFIED: csproj:12] |
| xunit.runner.visualstudio | 3.0.1 [VERIFIED: csproj:14] |
| coverlet.collector | 6.0.4 (opsional, sudah ada) |
| ProjectReference | `..\HcPortal.csproj` [VERIFIED: csproj:25] |
| FluentAssertions presence | **ABSENT** [VERIFIED: csproj grep] — match D-14 requirement |
| Existing test file | `HcPortal.Tests/FileUploadHelperTests.cs` (191 lines, 9 test method: 5 Validate + 1 MatchesMagicByte + 3 SaveFileAsync path traversal) |
| Test runner invocation | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --nologo` per Phase 325 plan baseline |
| Solution registration | ✓ `HcPortal.sln:7` entry `HcPortal.Tests\HcPortal.Tests.csproj` GUID {83522F87-...} [VERIFIED: sln] |
| Build inclusion | EXCLUDED dari main build per `HcPortal.csproj:10` `DefaultItemExcludes` |
| Komentar pattern | Bahasa Indonesia (Phase 325 baseline: "Unit test FileUploadHelper.ValidateCertificateFile (Phase 325 Plan 01)") |

**Action Phase 327:** Add 1 file baru `HcPortal.Tests/CertificateStatusTests.cs` (8 test). Zero csproj change. Zero new dependency.

## Audit 8: EF Migration Generation Expected Output

| Aspect | Finding |
|--------|---------|
| Current schema definition | `Migrations/ApplicationDbContextModelSnapshot.cs:510, 1829` — `b.Property<DateTime?>("ValidUntil").HasColumnType("datetime2");` (both tables) |
| Historical migration | `Migrations/20260317132516_AddValidUntilToAssessmentSession.cs` — AddColumn `datetime2 nullable` |
| TrainingRecords.ValidUntil migration origin | Pre-2026-03-17 — not relevant, just AlterColumn now |
| Latest migration | `20260521232810_AddAssessmentEditLogs` (May 21) — no schema overlap with ValidUntil |
| Expected `Up()/Down()` | See Pattern 2 above. 2× `AlterColumn` (TrainingRecords + AssessmentSessions). |
| EF Core 8 quirks nullable DateOnly | Native support since EF Core 8 [CITED: erikej.github.io 2023-09-03 + learn.microsoft.com breaking-changes]. `HaveColumnType("date")` default via convention — explicit override NOT needed |
| Pre-migration sqlcmd validation | Per D-11 (already drafted in CONTEXT.md L90-93). Syntax valid SQL Server T-SQL. |
| Probability komponen jam non-zero | Effectively zero — semua POST handler pakai `[DataType(DataType.Date)]` `<input type="date">` HTML5 → submit `yyyy-MM-dd` → ASP.NET binder set DateTime midnight → DB datetime2 midnight. Pre-check confirm only. |
| ApplicationDbContextModelSnapshot auto-update | Yes — `dotnet ef migrations add` auto-rewrite L510 + L1829 jadi `b.Property<DateOnly?>("ValidUntil").HasColumnType("date");` |
| EF generate weird (Drop+AddColumn vs AlterColumn) risk | LOW — `datetime2 → date` adalah simple in-place ALTER yang SQL Server support. AlterColumn expected. Manual `migrationBuilder.Sql(...)` fallback only kalau review file menunjukkan DropColumn. |

## Audit 9: Razor TagHelper `asp-for` + DateOnly? Compatibility .NET 8

| Aspect | Finding |
|--------|---------|
| `asp-for="ValidUntil"` rendering DateOnly? | ASP.NET Core 8 native binder support. **TAPI** ada known issue [CITED: github.com/dotnet/aspnetcore #47628] — Input TagHelper render DateOnly format `d-M-yyyy` instead of `yyyy-MM-dd` → break HTML5 `<input type="date">` |
| Issue status .NET 8 | Backlog milestone, PR #47957 status unclear via WebSearch [LOW confidence] |
| **Recommended workaround** | Add `[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]` attribute pada VM properties [CITED: github.com/dotnet/aspnetcore #47628 author workaround] |
| Razor view `value="@Model.ValidUntil?.ToString("yyyy-MM-dd")"` (EditAssessment.cshtml:462) | DateOnly `.ToString("yyyy-MM-dd")` output identik dengan DateTime — same standard format specifier. Compat. |
| Razor view `min="@DateTime.Today.ToString("yyyy-MM-dd")"` (EditAssessment.cshtml:463) | INDEPENDENT — DateTime.Today untuk HTML5 min attribute, BUKAN ValidUntil binding. NO TOUCH. |
| Razor TagHelper `[DataType(DataType.Date)]` annotation | Compat dengan DateOnly per CONTEXT.md D-04 |
| Form binding gotchas | (1) Locale-dependent culture `dd-MM-yyyy` rendering bug. (2) Nullable DateOnly binding empty string → null behavior consistent dengan DateTime?. (3) Minimal API DateOnly binding broken untuk format selain ISO [CITED: github.com/dotnet/aspnetcore #47734] — Phase 327 pakai MVC (bukan Minimal API) → not applicable. |

**Mitigation strategy:** SMOKE FIRST (test Plan 02 lokal browser inspect input value attribute) — kalau render `dd-MM-yyyy`, add `[DisplayFormat]` attribute ke 4 VM. Kalau render `yyyy-MM-dd` correctly, skip workaround.

## Audit 10: System.Text.Json Default DateOnly Serialization

| Aspect | Finding |
|--------|---------|
| Default output format .NET 8 | `"yyyy-MM-dd"` ISO 8601 date-only string [CITED: learn.microsoft.com System.Text.Json] |
| Custom JsonSerializerOptions di Program.cs | **ABSENT** [VERIFIED: grep "JsonSerializerOptions\|AddJsonOptions" Program.cs returns 0 hit] — default config |
| Consumer JS impact `analyticsDashboard.js:852` | `new Date("2027-03-15")` interpret as UTC midnight (per ECMAScript spec for ISO date-only string) → `.toLocaleDateString('id-ID')` convert WIB → display "15 Mar 2027" (kalau jam server UTC midnight + WIB +7 = same day) atau shift kalau jam beda |
| Consumer JS line 853 | `Math.ceil((new Date(d.tanggalExpired) - now) / (1000 * 60 * 60 * 24))` — calculate sisaHari. Risk shift ±1 day kalau midnight UTC vs midnight WIB beda hari. |
| Mitigation | Per D-15: SMOKE verify. Manual UAT 1 row Excel ValidUntil = today+1 → cek `analyticsDashboard.js` render "1 hari" badge. Kalau shift → escalate. |
| Backward compat risk old ISO 8601 `"2027-03-15T00:00:00"` | None — DateOnly serialize selalu `"yyyy-MM-dd"` (no T component) post-migration. JS Date constructor parse both formats. |

## Audit 11: Validation Architecture (Nyquist Dim 8)

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.13.0 |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (no separate xunit.runner.json) |
| Quick run command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --nologo --filter "FullyQualifiedName~CertificateStatusTests"` |
| Full suite command | `dotnet test HcPortal.sln --nologo` |
| Phase 325 baseline coverage | 9 test method `FileUploadHelperTests` (5 Validate + 1 MatchesMagicByte + 3 SaveFileAsync path traversal) — all GREEN post-Phase 325 |

### Phase Requirements → Test Map

(Phase 327 has no formal REQ-IDs; SCs documented di `ROADMAP.md:663-672`. Map SC-N → test type.)

| SC ID | Behavior | Test Type | Automated Command | File Exists? |
|-------|----------|-----------|-------------------|-------------|
| SC-1 | EF migration apply sukses (datetime2 → date 2 tabel) | manual-only (DB schema verify) | `sqlcmd -Q "SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='TrainingRecords' AND COLUMN_NAME='ValidUntil';"` returns `date` | ❌ Wave 0 (procedure-based, no test file) |
| SC-2 | Pre-migration zero row jam non-zero | manual-only (sqlcmd) | Per D-11 sqlcmd inline IT_NOTIFY.md | ❌ Wave 0 procedure |
| SC-3 | `DeriveCertificateStatus` 5 case pass | unit (xUnit Theory + Fact) | `dotnet test --filter "FullyQualifiedName~CertificateStatusTests" -v normal` | ❌ NEW `HcPortal.Tests/CertificateStatusTests.cs` |
| SC-4 | Add training Annual + ValidUntil today+1 → "AkanExpired" display | manual smoke (browser) + unit covered SC-3 | Browser POST `/Admin/AddTraining` + view `/Admin/ManageAssessment` tab Training | Manual UAT |
| SC-5 | Display 5 halaman wajib tanpa jam | manual smoke (browser visual) | Browser navigate 5 routes + visual verify | Manual UAT |
| SC-6 | PDF `/CMP/CertificatePdf/{id}` format tetap correct | manual smoke (PDF visual) | Browser navigate `/CMP/CertificatePdf/{id}` + open PDF | Manual UAT |
| SC-7 | Rollback EF `Down()` migration siap | manual procedure (kalau drama) | `dotnet ef database update {prev-migration}` | Procedure-based |

### Sampling Rate
- **Per task commit:** `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --nologo --filter "FullyQualifiedName~CertificateStatusTests"` (target ~5s)
- **Per wave merge:** `dotnet test HcPortal.sln --nologo` (full suite ~10s, includes Phase 325 FileUploadHelperTests + Phase 327 CertificateStatusTests)
- **Phase gate:** Full suite green + manual UAT 7 SC pass before user push approval

### Wave 0 Gaps
- [ ] `HcPortal.Tests/CertificateStatusTests.cs` — covers SC-3 (8 test method: 6 Theory + 2 Fact)
- [ ] Pre-migration sqlcmd script — inline IT_NOTIFY.md per D-11 (not separate file)
- [ ] Manual UAT checklist — SC-4 + SC-5 + SC-6 step-by-step di `327-VALIDATION.md` (planner output)

*(Framework install: skip — Phase 325 already bootstrapped HcPortal.Tests.csproj)*

## Audit 12: PDF QuestPDF Rendering DateOnly Compatibility

| File:Line | Pattern | Compat? |
|-----------|---------|---------|
| `Controllers/CMPController.cs:2083-2086` | `if (assessment.ValidUntil.HasValue) ... .Text($"Berlaku Hingga: {assessment.ValidUntil.Value.ToString("dd MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("id-ID"))}")` | **YES** — DateOnly.ToString dengan format specifier "dd MMMM yyyy" + culture id-ID output identik dengan DateTime. Specifier `d`/`M`/`y` semua punya semantic same di DateOnly + DateTime. |
| Other PDF gen paths touch ValidUntil | NONE — grep verified. CertificatePdf single endpoint. | — |
| QuestPDF API compatibility | QuestPDF take string parameter in `.Text(...)` — type-agnostic. Pre-format via `.ToString()` di C# side. | — |

**Verification step:** Manual smoke UAT — generate 1 PDF lokal + visual cek tanggal render "15 Maret 2027" tanpa jam.

## Common Pitfalls

### Pitfall 1: Compile Error Cascade (Order Matters)
**What goes wrong:** Flip entity ValidUntil → DateOnly DULU sebelum rewrite `var today = ...` controllers → build error `Cannot apply >= to DateOnly and DateTime`.
**Why it happens:** EF translates ValidUntil comparison ke today, type mismatch.
**How to avoid:** Per CONTEXT.md §Implementation Order — step 2 (entity flip) → step 3 (computed props) → step 4 (VM cascade) → step 5 (DeriveCertificateStatus) → step 6 (controller call site fix). Build SETIAP step, FIX error sebelum lanjut.
**Warning signs:** `dotnet build` error CS1503 "cannot convert from 'System.DateTime' to 'System.DateOnly'".

### Pitfall 2: `DateTime.MaxValue` Fallback in OrderBy
**What goes wrong:** Existing pattern `?? DateTime.MaxValue` (RenewalController L189-201, L263, L281, L289, L339; AdminBaseController L131, L201) tetap pakai DateTime — compile error.
**Why it happens:** `??` operator requires both operands same type after flip.
**How to avoid:** Sweep grep `?? DateTime.MaxValue` di Controllers/+Services/ → rewrite `?? DateOnly.MaxValue` setiap site terkait ValidUntil. (`DateTime.MaxValue` outside ValidUntil context tetap unchanged.)
**Warning signs:** Build error CS0019 "Operator '??' cannot be applied to operands of type 'DateOnly?' and 'DateTime'".

### Pitfall 3: JSON Consumer JS Timezone Shift
**What goes wrong:** `analyticsDashboard.js:852` `new Date("2027-03-15")` interpret UTC midnight → `.toLocaleDateString('id-ID')` WIB convert bisa shift 1 hari kalau jam server UTC midnight ± WIB tz.
**Why it happens:** ECMAScript spec for ISO date-only string (no T component) = UTC midnight, bukan local.
**How to avoid:** Smoke UAT 1 row Excel ValidUntil = today+1 → cek display "1 hari" badge di `/CMP/AnalyticsDashboard`. Kalau "0 hari" atau "2 hari" → shift confirmed → fix di JS via parse split `var parts = d.tanggalExpired.split('-'); var dt = new Date(parts[0], parts[1]-1, parts[2]);` (constructor 3-arg = local time).
**Warning signs:** Badge "sisa hari" off-by-one vs expectation manual.

### Pitfall 4: EF AlterColumn Drop+Re-add Path
**What goes wrong:** EF generator detect type change `datetime2 → date` not as in-place ALTER, generate `DropColumn + AddColumn` → data loss.
**Why it happens:** Constraint detection (index, FK) kadang miss in-place capability.
**How to avoid:** WAJIB review file Migrations/{ts}_ChangeValidUntilToDateOnly.cs SETELAH `dotnet ef migrations add`. Kalau Up() contains `DropColumn`, replace dengan manual SQL: `migrationBuilder.Sql("ALTER TABLE TrainingRecords ALTER COLUMN ValidUntil date NULL;");`
**Warning signs:** Generated migration contains both `migrationBuilder.DropColumn(name: "ValidUntil", ...)` and `migrationBuilder.AddColumn<DateOnly>(name: "ValidUntil", ...)`.

### Pitfall 5: Razor TagHelper Format Bug
**What goes wrong:** `<input asp-for="ValidUntil" type="date" />` render `value="15-3-2027"` (d-M-yyyy) instead `value="2027-03-15"` (yyyy-MM-dd) → HTML5 date input reject + data loss after POST.
**Why it happens:** Known bug dotnet/aspnetcore #47628 — DateOnly TagHelper format defaulting locale instead ISO.
**How to avoid:** SMOKE FIRST Plan 02 — browser DevTools inspect input value attribute. Kalau bug muncul, add `[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]` ke 4 VM ValidUntil property.
**Warning signs:** Browser inspect `<input value="15-3-2027">` instead `value="2027-03-15"`; POST submit empty ValidUntil setelah edit form.

### Pitfall 6: GradingService SetProperty Cast
**What goes wrong:** `GradingService.cs:465` cast `(DateTime?)null` jadi compile error setelah entity flip.
**Why it happens:** Explicit type cast tidak inferred.
**How to avoid:** Sweep cast `(DateTime?)null` di `Services/GradingService.cs` → rewrite `(DateOnly?)null` (2 site: L465 + L488 implicit via `certNow.AddYears(3)`).
**Warning signs:** Build error CS0030 "Cannot convert type 'DateTime?' to 'DateOnly?'".

### Pitfall 7: Pre-migration Skip
**What goes wrong:** Apply migration tanpa pre-check sqlcmd → ada row ValidUntil punya jam non-zero (e.g., legacy import dengan timestamp) → SQL Server CAST silently truncate → semantic shift (data hari diff dengan expectation).
**Why it happens:** Workflow rush, skip D-11.
**How to avoid:** WAJIB sqlcmd D-11 jalan SEBELUM `dotnet ef database update`. Kalau result > 0, escalate manual review row dulu.
**Warning signs:** Pre-check COUNT > 0.

## Code Examples

Verified patterns ready-to-paste:

### DeriveCertificateStatus Refactor (per D-06)
```csharp
// Source: Models/CertificationManagementViewModel.cs:53-63 (refactored)
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

### Controller `var today` Rewrite Pattern (10+ sites CMPController)
```csharp
// SEBELUM:
var today = DateTime.UtcNow.AddHours(7).Date;
var thirtyDaysFromNow = today.AddDays(30);

// SESUDAH:
var today = DateOnly.FromDateTime(DateTime.UtcNow);
var thirtyDaysFromNow = today.AddDays(30);
// Query expr di bawah ini UNCHANGED — EF translates DateOnly comparison native to SQL date
```

### ImportTraining ClosedXML Cast (per D-13)
```csharp
// Source: Controllers/TrainingAdminController.cs:1037 (Assessment) + :1138 (Training)
// SEBELUM:
ValidUntil = DateTime.TryParse(validUntilStr, out var vu) ? vu : (DateTime?)null,

// SESUDAH:
ValidUntil = DateTime.TryParse(validUntilStr, out var vu) ? DateOnly.FromDateTime(vu) : (DateOnly?)null,
```

### GradingService SetProperty Fix
```csharp
// Source: Services/GradingService.cs:465 + :488
// SEBELUM:
.SetProperty(r => r.ValidUntil, (DateTime?)null)
// ...
var validUntil = certNow.AddYears(3); // certNow = DateTime.Now (L476)
.SetProperty(r => r.ValidUntil, validUntil)

// SESUDAH:
.SetProperty(r => r.ValidUntil, (DateOnly?)null)
// ...
var validUntil = DateOnly.FromDateTime(certNow).AddYears(3);
.SetProperty(r => r.ValidUntil, validUntil)
```

### xUnit Boundary Test File Template
(See Pattern 4 above — complete file content.)

### Pre-migration sqlcmd Snippet (per D-11, IT_NOTIFY.md inline)
```sql
-- Konfirmasi zero row ValidUntil punya komponen jam non-zero sebelum migration.
-- Apabila result > 0, eskalasi manual review SEBELUM apply.

SELECT COUNT(*) AS TR_NonMidnight FROM TrainingRecords
WHERE ValidUntil IS NOT NULL AND CAST(ValidUntil AS TIME) <> '00:00:00';

SELECT COUNT(*) AS AS_NonMidnight FROM AssessmentSessions
WHERE ValidUntil IS NOT NULL AND CAST(ValidUntil AS TIME) <> '00:00:00';
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `DateTime` for date-only semantic | `DateOnly` struct (.NET 6+) | .NET 6 GA 2021-11 | Eliminate tz drift, semantic clarity, native EF Core 8 + ASP.NET Core 8 binding |
| `(date - DateTime.UtcNow).Days` arithmetic | `(date.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber)` | EF Core 8 / .NET 8 (2023-11) | Integer subtraction no float, no tz confusion |
| Custom `JsonConverter<DateOnly>` workaround | Default System.Text.Json `"yyyy-MM-dd"` ISO | .NET 7+ default | Zero config needed |
| Manual SQL migration for type change | EF Core 8 `AlterColumn` generator | EF Core 8 | Auto Up/Down with snapshot |

**Deprecated/outdated:**
- `IClock` DI abstraction for testability (Q2 option C rejected per CONTEXT.md) — overkill saat ini, defer indefinitely
- FluentAssertions di unit test — Phase 325 baseline pakai Assert vanilla, D-14 lock konsistensi

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | TagHelper `asp-for="ValidUntil"` DateOnly format bug (#47628) **mungkin masih ada** di .NET 8.0.418 | Audit 9, Pitfall 5 | MEDIUM — kalau bug aktif, form submit data loss tanpa workaround. Mitigation: smoke test Plan 02 dulu, add `[DisplayFormat]` kalau perlu. |
| A2 | EF Core 8 generate clean `AlterColumn` (not Drop+AddColumn) untuk `datetime2 → date` simple type change | Pattern 2, Pitfall 4 | LOW — review file post `dotnet ef migrations add`. Pattern matches Microsoft docs expectation. Fallback manual SQL ready. |
| A3 | JSON consumer JS `new Date("yyyy-MM-dd")` UTC midnight → WIB shift bisa off-by-one | Audit 10, Pitfall 3 | LOW-MEDIUM — depends on physical server tz config (production WIB UTC+7 or UTC server). Smoke verify. |
| A4 | Default System.Text.Json DateOnly serialization `"yyyy-MM-dd"` ISO **applies even tanpa custom JsonSerializerOptions** | Audit 10 | LOW — per .NET 8 official docs default behavior. Smoke confirm Network tab. |
| A5 | `DateOnly.AddYears(int)` method exists native | Audit 4 (AssessmentAdminController renewal pre-fill) | NONE — verified via WebSearch [CITED: learn.microsoft.com System.DateOnly] |

## Open Questions (RESOLVED)

1. **JSON timezone shift severity di production** — RESOLVED: Smoke verify pertama via lokal dev (Windows local tz WIB) di Plan 08 Pitfall 3 task (DevTools Network tab + badge "sisa hari" check). Kalau lokal OK, asumsi Dev + Prod sama. Document anomaly di IT_NOTIFY.md kalau ketemu.
   - What we know: Default System.Text.Json serialize `"yyyy-MM-dd"`. JS `new Date("yyyy-MM-dd")` interpret UTC midnight per ECMAScript spec.
   - What was unclear: Whether physical production server is UTC or WIB-localized — IIS Windows server biasanya local time, which means UtcNow = LocalTime - 7h. JS UTC parse interpret + WIB convert balik bisa cancel out OR shift, tergantung config.
   - Resolution path: Plan 08 Task 1 smoke verify lokal; fix di `wwwroot/js/analyticsDashboard.js:852-853` split parse kalau shift confirmed.

2. **Razor TagHelper #47628 bug status di .NET 8.0.418** — RESOLVED: SMOKE FIRST Plan 06 Task 2 (browser inspect input value attribute). Conditional retrofit `[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]` di Plan 06 Task 3 (4 VM) kalau bug aktif.
   - What we know: Bug filed 2023, workaround `[DisplayFormat]` documented. PR #47957 status unclear.
   - What was unclear: Apakah bug fix sudah merged + released di .NET 8.0.x latest patch.
   - Resolution path: Smoke-first non-blocking — Plan 06 checkpoint task verify, fallback `[DisplayFormat]` retrofit ready.

3. **DaysUntilExpiry delete vs rewrite** — RESOLVED: KEEP rewrite per D-10 (Plan 02 §1 rewrite DayNumber arithmetic). Delete decision defer v20.0 audit cleanup.
   - What we know: Zero call site di production code (grep verified Audit 5). CONTEXT.md D-10 default rewrite.
   - What was unclear: Apakah ada masa depan use case (Razor view future) yang reference.
   - Resolution path: Trivial rewrite cost; delete defer v20.0 backlog per CONTEXT.md deferred ideas.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | Build, test, EF CLI | ✓ | 8.0.418 | — |
| EF Core Tools | `dotnet ef migrations` | ✓ (bundled csproj 8.0.0) | 8.0.0 | — |
| SQL Server (lokal) | DB migration apply + sqlcmd pre-check | ✓ (LocalDB or instance) | — | — |
| sqlcmd | Pre-migration check + BACKUP | ✓ (Windows SQL Server install) | — | SSMS GUI alternative |
| xUnit + Test SDK | Unit test execution | ✓ (HcPortal.Tests bootstrap Phase 325) | 2.9.3 + 17.13.0 | — |
| ClosedXML | Excel import (no change needed) | ✓ | 0.105.0 | — |
| QuestPDF | PDF generation smoke | ✓ | 2026.2.2 | — |
| Browser (Edge/Chrome) | Manual UAT 5 halaman wajib + PDF visual + DevTools Network | ✓ | — | — |
| Playwright | E2E (kalau ada scenario sentuh ValidUntil) | ✓ (tests/ existing) | — | Manual browser smoke |

**Missing dependencies with no fallback:** None — full env available.
**Missing dependencies with fallback:** None.

## Sources

### Primary (HIGH confidence)
- `HcPortal.csproj` — .NET 8.0, EF Core 8.0.0, ClosedXML 0.105.0, QuestPDF 2026.2.2 [VERIFIED: grep + Read]
- `HcPortal.Tests/HcPortal.Tests.csproj` — xUnit 2.9.3, no FluentAssertions [VERIFIED]
- `HcPortal.sln` — 2 project entry [VERIFIED]
- `Models/CertificationManagementViewModel.cs:53-63` — DeriveCertificateStatus current signature [VERIFIED: Read]
- `Models/TrainingRecord.cs:43, 71-95` — ValidUntil + computed props [VERIFIED]
- `Models/AssessmentSession.cs:65` — ValidUntil [VERIFIED]
- `Models/UnifiedTrainingRecord.cs:26, 40` — ValidUntil + IsExpired (DateTime.Now bug) [VERIFIED]
- `Models/AnalyticsDashboardViewModel.cs:64-70` — ExpiringSoonItem.TanggalExpired (nama exact) [VERIFIED]
- `Models/Create/Edit TrainingRecord/ManualAssessment ViewModels` — 4 VM ValidUntil [VERIFIED]
- `Controllers/TrainingAdminController.cs:840-1164` — ImportTraining handler + cell parse pattern [VERIFIED]
- `Controllers/CMPController.cs:2517-3152` — 10× var today + ValidUntil query expr + ExpiringSoon endpoints [VERIFIED]
- `Controllers/CDPController.cs:3674-3886` — ValidUntil display + DeriveCertificateStatus calls [VERIFIED]
- `Controllers/RenewalController.cs:46-339` — ValidUntil cascade + `?? DateTime.MaxValue` 6 sites [VERIFIED]
- `Controllers/HomeController.cs:78-206` — CERT_EXPIRED notification + GetCertAlertCounts [VERIFIED]
- `Controllers/AdminBaseController.cs:44-205` — BuildRenewalRowsAsync [VERIFIED]
- `Controllers/AssessmentAdminController.cs:675-1749` — Renewal pre-fill AddYears + Create/Edit POST [VERIFIED]
- `Services/GradingService.cs:465, 488, 493` — SetProperty ValidUntil cast [VERIFIED]
- `Services/WorkerDataService.cs:65, 264` — UnifiedTrainingRecord build + IsExpiringSoon usage [VERIFIED]
- `wwwroot/js/analyticsDashboard.js:852-853` — `new Date(d.tanggalExpired)` consumer [VERIFIED]
- `Migrations/ApplicationDbContextModelSnapshot.cs:510, 1829` — current schema datetime2 [VERIFIED]
- `Migrations/20260317132516_AddValidUntilToAssessmentSession.cs` — historical AddColumn [VERIFIED]
- `Program.cs` — no custom JsonSerializerOptions [VERIFIED: grep zero hit]
- `Views/Admin/*.cshtml` + `Views/CMP/*.cshtml` + `Views/CDP/*.cshtml` — TagHelper + display format sites [VERIFIED]

### Secondary (HIGH-MEDIUM confidence)
- [Microsoft Learn — EF Core 8 breaking changes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-8.0/breaking-changes) — date/time scaffolded as DateOnly/TimeOnly [VERIFIED via WebSearch + WebFetch]
- [Microsoft Learn — System.DateOnly](https://learn.microsoft.com/en-us/dotnet/api/system.dateonly) — AddYears, DayNumber, FromDateTime methods [CITED]
- [Microsoft Learn — Model Binding ASP.NET Core 8](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-8.0) — DateOnly supported simple type [CITED]
- [ErikEJ's blog — DateOnly TimeOnly with EF Core 8](https://erikej.github.io/efcore/sqlserver/2023/09/03/efcore-dateonly-timeonly.html) — Native SQL Server provider support [CITED]
- [github.com/ClosedXML/ClosedXML #2227](https://github.com/ClosedXML/ClosedXML/issues/2227) — DateOnly not supported, cast manual required [VERIFIED]

### Tertiary (LOW confidence — needs validation)
- [github.com/dotnet/aspnetcore #47628](https://github.com/dotnet/aspnetcore/issues/47628) — DateOnly TagHelper format bug, status unclear .NET 8.0.418 [LOW — assumption A1, smoke verify Plan 02]
- [github.com/dotnet/aspnetcore #47734](https://github.com/dotnet/aspnetcore/issues/47734) — Minimal API DateOnly binding broken non-ISO format (NOT applicable Phase 327 MVC)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all versions verified csproj + dotnet CLI
- Architecture: HIGH — codebase grep audit complete, 24 file touched mapped
- Pitfalls: HIGH-MEDIUM — 7 pitfalls documented from grep audit + Microsoft docs cross-ref, Pitfall 5 (TagHelper bug) MEDIUM confidence pending smoke
- ImportTraining audit (Audit 1): HIGH — line numbers verified, parse pattern confirmed
- RenewalCertificateRow VM (Audit 2): HIGH — corrected naming `ExpiringSoonItem` via grep
- JSON API enumeration (Audit 3): HIGH — all 7 CMPController Json endpoints inspected, 2 carry ValidUntil
- `var today` enumeration (Audit 4): HIGH — 14 grep hits verified per controller
- Computed props (Audit 5): HIGH — grep zero hit DaysUntilExpiry production
- AssessmentSession/Service (Audit 6): HIGH — all sites mapped
- xUnit project structure (Audit 7): HIGH — Phase 325 baseline verified via file Read
- EF migration expected (Audit 8): MEDIUM — generated output is prediction, must review post-`dotnet ef migrations add`
- Razor TagHelper (Audit 9): MEDIUM — known bug #47628 status .NET 8 unclear, smoke verify
- System.Text.Json (Audit 10): HIGH — default behavior documented, custom config absent
- Validation Architecture (Audit 11): HIGH — Phase 325 baseline pattern proven
- PDF QuestPDF (Audit 12): HIGH — single touch site, format specifier date-only safe

**Research date:** 2026-05-28
**Valid until:** 2026-06-11 (14 days — EF Core 8 + ASP.NET Core 8 stable, low churn risk)
