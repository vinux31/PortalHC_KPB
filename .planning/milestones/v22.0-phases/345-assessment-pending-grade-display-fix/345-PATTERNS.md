# Phase 345: assessment-pending-grade-display-fix - Pattern Map

**Mapped:** 2026-06-04
**Files analyzed:** 2 NEW (xUnit test class + Playwright spec) + 7 EDIT-targets (light pass)
**Analogs found:** 2 / 2 NEW (exact + near-exact)

> **Catatan baca:** Phase 345 ~90% EDIT ke file yang sudah di-`file:line`-cite penuh di `345-RESEARCH.md`
> (§Code Examples #1–#6). File-file itu **tidak butuh analog** — planner copy excerpt langsung dari RESEARCH.
> Yang genuinely NEW dan butuh closest-analog mapping hanya **2 file test** (CMP06R-05). Sisanya = catatan
> "existing pattern at site" yang ringkas. Critical Corrections C-1/C-2/C-3 dari RESEARCH dihormati di bawah.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| **NEW** `HcPortal.Tests/AssessmentHistoryStatsTests.cs` | test (unit) | transform (pure math) + CRUD (VM mapping) | `HcPortal.Tests/CertificateStatusTests.cs` (math) + `HcPortal.Tests/OrgLabelControllerTests.cs` (InMemory ctrl) | exact (2 analog: 1 per sub-pattern) |
| **NEW** `tests/e2e/assessment-pending-grade.spec.ts` | test (e2e) | request-response + file-I/O (download) | `tests/e2e/export-per-peserta.spec.ts` (download+auth) + `global.setup.ts`/`global.teardown.ts` (SEED) | near-exact |
| EDIT `Views/CMP/RecordsWorkerDetail.cshtml:226-231` | view | request-response | `Views/CMP/Records.cshtml:182-192` (3-way template) | n/a (edit, RESEARCH #1) |
| EDIT `Views/CMP/Records.cshtml:188` | view | request-response | self (switch already 3-way, add case) | n/a (edit, RESEARCH #5) |
| EDIT `Views/Admin/UserAssessmentHistory.cshtml:68,101,172` | view | request-response | `Records.cshtml:182-192` (3-way) | n/a (edit, RESEARCH #2/#3) |
| EDIT `Controllers/AssessmentAdminController.cs:4620-4621/4737/4744-4746` | controller | transform + file-I/O (PDF) | self (existing QuestPDF spans + stats) | n/a (edit, RESEARCH #2/#3/#4) |
| EDIT `Services/WorkerDataService.cs:52-57` | service | transform (switch map) | self (switch already 3-way) | n/a (edit, RESEARCH #5) |
| EDIT `Controllers/CMPController.cs:694` | controller | file-I/O (Excel) | self (ternary cell) | n/a (edit, RESEARCH #6) |
| EDIT `Models/CDPDashboardViewModel.cs:111` | model (VM) | — | self (`bool`→`bool?`, C-1) | n/a (edit) |
| EDIT `Models/ReportsDashboardViewModel.cs` (UserAssessmentHistoryViewModel) | model (VM) | — | self (add `GradedCount`/`PendingCount` props) | n/a (edit) |

---

## Pattern Assignments — NEW FILES

### `HcPortal.Tests/AssessmentHistoryStatsTests.cs` (test/unit, transform + CRUD)

**Coverage (D-11):** (a) VM nullable mapping (`IsPassed==null` tetap null, BUKAN false) + (b) passRate exclude-pending math (graded denominator, all-pending → 0/—) + (D-07) averageScore exclude-pending + (D-10/C-3) regression-guard group `PassedCount` tidak naik karena pending.

**Strategi (RESEARCH Q6):** Math statistik saat ini **inline di action** `UserAssessmentHistory` (`AssessmentAdminController.cs:4743-4746`) → tidak testable in-place. **Opsi A (rekomendasi RESEARCH):** ekstrak `internal static ComputeHistoryStats(...)` → test math murni tanpa DbContext (pola `CertificateStatusTests`). **Opsi B:** action-level InMemory DbContext (pola `OrgLabelControllerTests`; aman karena action **tidak panggil `_userManager`**, cuma `_context` — [VERIFIED RESEARCH Q6 L4712-4763]). Planner pilih; di bawah dua analog untuk dua sub-pattern.

> **Catatan ekstraksi:** kalau planner pilih Opsi A, helper harus `internal` (bukan `private`) + tambah `[assembly: InternalsVisibleTo("HcPortal.Tests")]` ATAU jadikan `public`. Cek apakah `InternalsVisibleTo` sudah ada di proyek utama (kemungkinan belum — `OrgLabelService`/`SertifikatRow` yang dites saat ini `public`). Default aman = `public static` (pola `SertifikatRow.DeriveCertificateStatus` yang dites `CertificateStatusTests` adalah method publik).

---

#### Analog A — pure-math helper (untuk passRate/averageScore math, D-11b + D-07)

**Analog:** `HcPortal.Tests/CertificateStatusTests.cs` (44 baris, full read) — tes `SertifikatRow.DeriveCertificateStatus` static method dengan `[Theory]`+`[InlineData]` (6 case) + `[Fact]` null-edge (2 case). **Persis pola yang dibutuhkan untuk `ComputeHistoryStats`**: no DbContext, no UserManager, deterministik, boundary + null coverage.

**Imports pattern** (lines 5-9 — minimal, no EF/Identity):
```csharp
using System;
using HcPortal.Models;   // AssessmentReportItem ada di sini (namespace HcPortal.Models, CDPDashboardViewModel.cs)
using Xunit;

namespace HcPortal.Tests;
```

**Test class + helper pattern** (lines 11-15 — private static helper untuk merakit input):
```csharp
public class CertificateStatusTests
{
    // Helper: today UTC + offset hari, return DateOnly (Plan 04 GREEN post-flip).
    private static DateOnly Today(int offset) =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(offset);
```
→ Untuk 345: helper `private static AssessmentReportItem Item(bool? isPassed, int score) => new() { IsPassed = isPassed, Score = score };` (rakit `List<AssessmentReportItem>` untuk `ComputeHistoryStats`).

**[Theory]+[InlineData] boundary pattern** (lines 17-29 — banyak skenario, satu method):
```csharp
[Theory]
[InlineData(100, "Annual", CertificateStatus.Aktif)]           // > 30 hari = Aktif
[InlineData(30, "Annual", CertificateStatus.AkanExpired)]      // boundary inclusive
[InlineData(-1, "Annual", CertificateStatus.Expired)]          // sudah lewat
public void DeriveCertificateStatus_VariousScenarios_ReturnsExpected(
    int offset, string certificateType, CertificateStatus expected)
{
    var result = SertifikatRow.DeriveCertificateStatus(Today(offset), certificateType);
    Assert.Equal(expected, result);
}
```
→ Untuk 345, skenario math yang harus tercover (RESEARCH Q4 formula): mixed (1 pass + 1 fail + 1 null → passRate=50%, graded=2, pending=1, avg atas 2 graded), all-pass, all-fail, **all-pending edge (graded=0 → passRate=0, view tampil "—")**, empty list. Pakai `[Fact]` per-edge atau `[Theory]` dengan tuple expected.

**[Fact] null-edge pattern** (lines 31-43 — kasus khusus null):
```csharp
[Fact]
public void DeriveCertificateStatus_NullValidUntil_NonPermanent_ReturnsExpired()
{
    var result = SertifikatRow.DeriveCertificateStatus(null, null);
    Assert.Equal(CertificateStatus.Expired, result);
}
```
→ Untuk 345 = the **all-pending guard** test (`ComputeHistoryStats` dengan list semua `IsPassed==null` → `passRate==0`, `gradedCount==0`, `pendingCount==N`). Plus **VM nullable assert** (D-11a): rakit `AssessmentReportItem { IsPassed = null }` setelah `bool?` ripple → `Assert.Null(item.IsPassed)` (buktikan tidak collapse ke false).

---

#### Analog B — InMemory DbContext + controller (bila pilih Opsi B action-level, D-11a)

**Analog:** `HcPortal.Tests/OrgLabelControllerTests.cs` (192 baris, full read) — controller test dengan InMemory `ApplicationDbContext`, seed entity, UserManager `null!`-substitute, `JsonResult` reflection assert. **Pola factory + InMemory + null-UserManager** persis reusable untuk `UserAssessmentHistory` action (yang juga tidak sentuh `_userManager`).

**Imports pattern** (lines 6-15 — EF + MVC + project):
```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HcPortal.Controllers;
using HcPortal.Data;         // ApplicationDbContext
using HcPortal.Models;
using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests;
```

**Factory pattern — InMemory DbContext + seed + null UserManager** (lines 28-65, the load-bearing reuse):
```csharp
private static (OrgLabelController ctrl, ApplicationDbContext ctx) MakeControllerWithCtx(bool seedUnits = false)
{
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())   // unik per test = isolasi
        .Options;
    var ctx = new ApplicationDbContext(options);

    ctx.OrganizationLevelLabels.AddRange( /* seed entities */ );
    ctx.SaveChanges();

    // UserManager null-substitute: action UserAssessmentHistory TIDAK panggil GetUserAsync → null AMAN.
    #pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    var ctrl = new OrgLabelController(svc, ctx, null!);
    #pragma warning restore CS8625

    ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    return (ctrl, ctx);
}
```
→ Untuk 345 Opsi B: seed satu `ApplicationUser` + 3 `AssessmentSession` (`Status="Completed"`, `IsPassed` = true/false/null, `Score` = nilai/null). Instantiate `AssessmentAdminController(...)` dengan deps yang tidak terpakai = `null!`. **CEK constructor `AssessmentAdminController`** — kemungkinan butuh lebih banyak dep daripada `OrgLabelController` (UserManager, IWebHostEnvironment, logger, dll.); yang tidak disentuh action `UserAssessmentHistory` boleh `null!`. Ini risiko Opsi B (constructor berat) → memperkuat rekomendasi **Opsi A**.

**Entity seed reference — `AssessmentSession` shape** (untuk seed test, dari `Models/AssessmentSession.cs:7-39`):
```csharp
new AssessmentSession {
    UserId = "test-user-1", Title = "T", Category = "OJT",
    Status = "Completed",        // L20 — filter UserAssessmentHistory
    Score = null,                // L26 int? — pending = null (skew avg jika ?? 0)
    IsPassed = null,             // L38 bool? — TARGET: pending
    CompletedAt = DateTime.UtcNow
}
```

**Result-cast assert pattern (Opsi B)** — `OrgLabelControllerTests` pakai `JsonResult` reflection (lines 69-79); untuk `UserAssessmentHistory` (return `ViewResult`) polanya beda:
```csharp
// Untuk ViewResult.Model cast (BUKAN JsonResult):
var view = Assert.IsType<ViewResult>(result);
var vm = Assert.IsType<UserAssessmentHistoryViewModel>(view.Model);
Assert.Equal(50.0, vm.PassRate);        // graded denominator
Assert.Equal(1, vm.PendingCount);       // D-06 prop baru
```

**[Fact] reflection pattern untuk auth-contract** (lines 168-191) — `OrgLabelControllerTests` kunci `[Authorize(Roles=...)]` via reflection. **Opsional untuk 345** (auth bukan fokus phase ini), tapi tersedia bila planner mau kunci `UserAssessmentHistory` role contract:
```csharp
private static AuthorizeAttribute? RolesAttr(string method) =>
    typeof(AssessmentAdminController).GetMethod(method)!
        .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
        .Cast<AuthorizeAttribute>().FirstOrDefault();
[Fact] public void UserAssessmentHistory_RequiresAdminOrHc()
    => Assert.Equal("Admin, HC", RolesAttr(nameof(AssessmentAdminController.UserAssessmentHistory))!.Roles);
```

**Run command:** `dotnet test HcPortal.Tests` (precedent: 52/52 PASS Phase 344). Proyek kecil <2s.

**csproj — tidak perlu diubah** (`HcPortal.Tests.csproj`): sudah punya xunit 2.9.3 + EFCore.InMemory 8.0.0 + ProjectReference `..\HcPortal.csproj`. File baru auto-discovered (glob `*.cs`). [VERIFIED: read csproj]

---

### `tests/e2e/assessment-pending-grade.spec.ts` (test/e2e, request-response + file-I/O)

**Coverage (D-11):** CMP06R-01 (RecordsWorkerDetail badge amber) + CMP06R-02 (UserAssessmentHistory badge + stats) + CMP06R-03 (BulkExportPdf download sukses — **zip**, label via human/MCP). Reuse `accounts.ts` (admin/hc) + SEED via `dbSnapshot.ts`.

**Analog utama:** `tests/e2e/export-per-peserta.spec.ts` (98 baris, full read) — download + auth regression, inline `loginAny`, `triggerDownload`, account import. **Hampir 1:1** dengan kebutuhan CMP06R-03 (PDF download). Beda kunci: BulkExportPdf hasilkan `.zip` (`_Bundle.zip` per RESEARCH Q6), analog trigger `.xlsx` → **ganti regex suggestedFilename**.

**Imports + account pattern** (lines 17-25):
```typescript
import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';
// untuk SEED (lihat blok SEED di bawah):
import * as db from '../helpers/dbSnapshot';
```
Akun tersedia (`tests/helpers/accounts.ts`, full read): `admin` = `admin@pertamina.com`/`123456`, `hc` = `meylisa.tjiang@pertamina.com`/`123456`, `coachee` = `rino.prasetyo@pertamina.com`/`123456`. Admin+HC bisa akses UserAssessmentHistory & BulkExportPdf (`[Authorize(Roles="Admin, HC")]`); coachee untuk `/CMP/RecordsWorkerDetail` (worker view) atau negative-test.

**Inline login pattern** (lines 27-37 — WAJIB pakai inline, BUKAN `helpers/auth.ts`):
```typescript
// Inline login — accept any successful redirect away dari /Account/Login.
// (helpers/auth.ts wait "**/Home/**" — hanya Admin landing di /Home; HC/Coachee redirect CMP/CDP → helper gagal.)
async function loginAny(page: Page, accountKey: AccountKey) {
  const { email, password } = accounts[accountKey];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(url => !url.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}
```
> **PERINGATAN dari analog (lines 61-67):** HC login (`meylisa.tjiang`) **pre-existing broken** di test infra (form submit tidak persist session, redirect balik ke Login) — `export-per-peserta.spec.ts` men-`test.skip` HC test karena ini. **Planner 345-04:** kalau HC login masih broken, gunakan `admin` untuk surface UserAssessmentHistory/BulkExportPdf (admin punya akses sama via `Roles="Admin, HC"`), dan dokumentasikan skip HC + alasan (pola Phase 320). JANGAN anggap kegagalan HC sebagai bug Phase 345.

**Download trigger pattern** (lines 41-45 — `page.goto` throw saat response=download, catch + abaikan):
```typescript
async function triggerDownload(page: Page, url: string) {
  const downloadPromise = page.waitForEvent('download', { timeout: 30_000 });
  await page.goto(url).catch(() => { /* expected ketika response = file download */ });
  return downloadPromise;
}
```

**Download assert pattern** (lines 49-59 — path truthy + filename regex + size sanity):
```typescript
test('Admin: BulkExportPdf -> assert .zip download', async ({ page }) => {
  await loginAny(page, 'admin');
  const download = await triggerDownload(page, BULK_EXPORT_URL);
  const path = await download.path();
  expect(path).toBeTruthy();
  expect(download.suggestedFilename()).toMatch(/_Bundle\.zip$/);   // ← GANTI dari _Summary\.xlsx (BulkExportPdf = zip, RESEARCH Q6)
  const fs = await import('node:fs');
  expect(fs.statSync(path!).size).toBeGreaterThan(1024);
});
```
> **A3/RESEARCH Q6:** Label "Menunggu Penilaian" di dalam PDF-dalam-zip **tidak praktis di-assert via Playwright otomatis**. Auto-test cukup buktikan download sukses + size>0; **verifikasi badge amber via human/MCP** (pola Phase 344 MCP-driven UAT). Untuk surface web (RecordsWorkerDetail/UserAssessmentHistory) badge amber BISA di-assert via DOM: `expect(page.locator('.badge.bg-warning', { hasText: 'Menunggu Penilaian' })).toBeVisible()`.

**Negative-test pattern** (lines 75-84 — 403/redirect untuk role tak berizin), reuse bila planner mau kunci coachee tidak bisa BulkExportPdf.

---

#### SEED_WORKFLOW integration (D-11 — WAJIB snapshot→seed→restore)

**Analog:** `tests/e2e/global.setup.ts` (resolve dir → backup → seed → journal active) + `tests/e2e/global.teardown.ts` (restore → Layer 4 verify → journal cleaned), keduanya full read. **Ini pola finally-restore yang benar** (bukan inline try/finally di spec — lebih rapuh). `exam-types.spec.ts:22` import `dbSnapshot` tapi delegasi backup/restore ke global setup/teardown.

**Helper signatures** (`tests/helpers/dbSnapshot.ts`, full read — semua sudah ada, JANGAN hand-roll):
```typescript
import * as db from '../helpers/dbSnapshot';
await db.queryString(sql): Promise<string>   // resolve SERVERPROPERTY('InstanceDefaultBackupPath')
await db.backup(snapshotPath): Promise<void>  // BACKUP ... WITH INIT, FORMAT
await db.execScript(sqlPath): Promise<void>   // -i seed.sql (GO batch native)
await db.restore(snapshotPath): Promise<void> // SINGLE_USER + RESTORE WITH REPLACE + MULTI_USER
await db.queryScalar(sql): Promise<number>    // COUNT(*) validation
// Guard: REJECT non-localhost target (CLAUDE.md compliance, baked-in).
```

**Resolve backup dir + snapshot path pattern** (`global.setup.ts:43-53` — C:\Temp blocked, pakai default dir):
```typescript
const defaultBackupDirRaw = await db.queryString(
  `SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))`
);
const defaultBackupDir = defaultBackupDirRaw.replace(/\\+$/, '').replace(/\\/g, '/');
const ts = new Date().toISOString().replace(/[:.]/g, '-');   // sanitize untuk filename Windows
const snapshotPath = `${defaultBackupDir}/HcPortalDB_Dev-pre345-${ts}.bak`;
```

**Backup→seed→validate pattern** (`global.setup.ts:68-81`):
```typescript
await db.backup(snapshotPath);                    // §5.1 SOP — wajib SEBELUM seed temporary
await db.execScript(SEED_SQL);                    // seed Status='Completed', IsPassed=NULL, CompletedAt=now
const n = await db.queryScalar(`SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]PENDING345]%'`);
expect(n, 'Layer 1: pending sessions seeded').toBeGreaterThan(0);
```

**Restore-finally pattern** (`global.teardown.ts:64-95` — restore + verify 0 rows + flip journal):
```typescript
try {
  await db.restore(snapshotPath);                 // SUKSES atau GAGAL → tetap restore (finally)
} catch (e) {
  console.error('[teardown] RESTORE GAGAL — manual command:', /* sqlcmd fallback */);
  throw e;
}
const remaining = await db.queryScalar(`SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]PENDING345]%'`);
if (remaining !== 0) throw new Error(`Cleanup failed: ${remaining} rows remain post-RESTORE`);
// SEED_JOURNAL.md: regex replace 'active' → 'cleaned'
```

**SEED SQL — seed pending session** (planner tulis `tests/sql/pending345-seed.sql`, jalankan via `execScript`):
```sql
-- Seed sesi Completed + IsPassed=NULL untuk user test (klasifikasi: temporary + local-only).
-- Prefix Title '[PENDING345]' untuk Layer 1 filter + Layer 4 cleanup verify.
INSERT INTO AssessmentSessions (UserId, Title, Category, Schedule, DurationMinutes, Status,
    Progress, BannerColor, PassPercentage, AllowAnswerReview, GenerateCertificate,
    IsPassed, CompletedAt, Score, IsTokenRequired, AccessToken)
VALUES ('<test-userId>', '[PENDING345] Essay Pending', 'OJT', GETDATE(), 60, 'Completed',
    100, 'bg-primary', 70, 1, 0, NULL, GETDATE(), NULL, 0, '');
```
> Alternatif: seed via UI submit essay (lebih realistis, tapi lebih lambat). SQL INSERT lebih cepat untuk regression. **Klasifikasi WAJIB:** `temporary + local-only` — JANGAN ke `Data/SeedData.cs`. Catat `docs/SEED_JOURNAL.md`, tandai `cleaned` setelah restore. Target DB = `localhost\SQLEXPRESS` / `HcPortalDB_Dev`.

**Decision planner:** opsi (a) integrate ke `playwright.config.ts` `globalSetup`/`globalTeardown` baru (rapi, pola matrix), ATAU (b) `test.beforeAll`/`test.afterAll` di dalam spec dengan try/finally restore (lebih sederhana, cukup untuk 1 spec). RESEARCH tidak mewajibkan; (b) lebih ringan untuk scope 3-surface.

---

## EDIT-Targets — Existing Pattern at Site (compact, RESEARCH owns full excerpts)

> Semua sudah di-`file:line`-cite + excerpt copy-ready di `345-RESEARCH.md §Code Examples`. Di sini hanya
> ringkasan "pola yang ada di lokasi" supaya planner copy gaya sekitarnya. **Honor C-1/C-2/C-3.**

| Site | Pola existing di lokasi | Ubah jadi | RESEARCH ref |
|------|------------------------|-----------|--------------|
| `Views/CMP/RecordsWorkerDetail.cshtml:226-231` | binary `@if(IsPassed==true){Passed}else{Failed}` (null→Failed merah) | tambah `else if(==false)` + `else` amber badge `@AssessmentConstants.AssessmentStatus.PendingGrading` | #1 |
| `Views/CMP/Records.cshtml:188` | **C-2:** switch `sc` SUDAH 3-way (`"Completed"=>"bg-info"`, BUKAN Failed) | **label-unify only**: tambah case `AssessmentConstants.AssessmentStatus.PendingGrading => "bg-warning text-dark"`. JANGAN cari "Failed" di sini | #5 |
| `Services/WorkerDataService.cs:56` | switch `IsPassed`: `null => "Completed"` (L56), service sudah `using HcPortal.Models` | `null => AssessmentConstants.AssessmentStatus.PendingGrading` | #5 |
| `Views/Admin/UserAssessmentHistory.cshtml:172` | `@if(item.IsPassed)` (bool langsung — **akan CS0266 setelah bool?**) | 3-way `==true`/`==false`/`else` amber. **Pitfall 2** | #2 |
| `Views/Admin/UserAssessmentHistory.cshtml:68,101` | `@Model.PassRate.ToString("F1")%` (2 tempat: mini-stat + kartu) | conditional: `gradedCount==0` → "Belum ada penilaian"/"—" (D-05). + indikator "Menunggu Penilaian: {N}" (D-06) | #3 |
| `Controllers/AssessmentAdminController.cs:4737` | `IsPassed = a.IsPassed ?? false` (projection) | drop `?? false` → `IsPassed = a.IsPassed` (butuh VM `bool?` dulu) | #2 |
| `Controllers/AssessmentAdminController.cs:4743-4746` | inline math: `passRate = passedCount*100.0/totalAssessments` (denom=TOTAL, salah) | denom=gradedCount + pendingCount + averageScore exclude pending (D-04/05/06/07). **Ekstrak `ComputeHistoryStats` untuk testability** | #3, Q4 |
| `Controllers/AssessmentAdminController.cs:4620-4621` | binary QuestPDF: `IsPassed==true?"Lulus":"Tidak Lulus"` + `Green/Red.Darken2` (null→merah) | extract `statusText`/`statusColor` 3-way local var + `Colors.Orange.Darken2` | #4, Q3 |
| `Controllers/CMPController.cs:694` | ternary `IsPassed==true?"Passed":(==false?"Failed":"")` (null→empty) | `null` branch → `AssessmentConstants.AssessmentStatus.PendingGrading`. **Cek `using HcPortal.Models` di header CMPController** | #6 |
| `Controllers/AssessmentAdminController.cs:2759/2775/2789/2821` (group `PassedCount`) | **C-3:** projection L2712 sudah `IsPassed = a.IsPassed ?? false` → `Count(a=>a.IsPassed)` SUDAH exclude pending | **NOL perubahan kode** — verifikasi + regression test saja. JANGAN tambah `MenungguPenilaianCount` ke PrePost (defer Phase 348) | Q2, C-3 |
| `Models/CDPDashboardViewModel.cs:111` | **C-1:** `public bool IsPassed` (BUKAN di ReportsDashboardViewModel.cs!) | `bool` → `bool?`. **Pitfall 1** | C-1 |
| `Models/ReportsDashboardViewModel.cs` (`UserAssessmentHistoryViewModel`) | VM untuk view stats | tambah prop `GradedCount`/`PendingCount` (untuk view guard D-05/D-06) | Q4 |

---

## Shared Patterns

### Konstanta label (WAJIB D-02 — JANGAN literal string di C#)
**Source:** `Models/AssessmentConstants.cs:18` (`const string PendingGrading = "Menunggu Penilaian"`)
**Apply to:** SEMUA surface C# (controller/service/PDF/Excel) + Razor.
```csharp
// C#:    AssessmentConstants.AssessmentStatus.PendingGrading
// Razor: @AssessmentConstants.AssessmentStatus.PendingGrading   (Views/_ViewImports.cshtml:2 sudah @using HcPortal.Models)
// switch case label (const → valid): AssessmentConstants.AssessmentStatus.PendingGrading => "bg-warning text-dark"
```
Di test: `Assert.Equal(AssessmentConstants.AssessmentStatus.PendingGrading, vm.SomeLabel)` (jangan literal di assertion juga).

### Badge warna 3-way (D-01)
**Source:** `Views/CMP/Records.cshtml:182-192` (web), `AssessmentAdminController.cs:4620-4621` (PDF)
**Apply to:** RecordsWorkerDetail, UserAssessmentHistory view, PDF, Excel.
```
WEB:  true→bg-success | false→bg-danger | null→bg-warning text-dark (amber, WAJIB text-dark utk WCAG) | empty→text-muted "—"
PDF:  true→Colors.Green.Darken2 | false→Colors.Red.Darken2 | null→Colors.Orange.Darken2
```

### Test isolation (xUnit)
**Source:** `OrgLabelControllerTests.cs:30-31` — `UseInMemoryDatabase(Guid.NewGuid().ToString())` per test (isolasi); `null!` untuk dep tak-terpakai (`#pragma warning disable CS8625`).
**Apply to:** AssessmentHistoryStatsTests Opsi B.

### SEED snapshot/restore (D-11, SEED_WORKFLOW)
**Source:** `tests/helpers/dbSnapshot.ts` (`backup`/`restore`/`execScript`/`queryScalar`/`queryString`) + `global.setup.ts`/`global.teardown.ts` (orchestration + journal flip).
**Apply to:** Playwright spec — backup SEBELUM seed, restore di finally (sukses/gagal), journal `cleaned`.

---

## No Analog Found

Tidak ada. Kedua file NEW punya analog kuat di repo:
- xUnit math → `CertificateStatusTests.cs` (exact: pure static + Theory/Fact/null-edge)
- xUnit InMemory → `OrgLabelControllerTests.cs` (exact: factory + InMemory + null-UserManager)
- Playwright download → `export-per-peserta.spec.ts` (near-exact: download+auth+inline-login)
- SEED → `global.setup.ts`/`global.teardown.ts` + `dbSnapshot.ts` (exact: snapshot→seed→restore→journal)

Semua EDIT-target sudah `file:line`-cited di RESEARCH (tidak butuh analog).

---

## Metadata

**Analog search scope:** `HcPortal.Tests/` (6 test files), `tests/e2e/` (11 specs), `tests/helpers/` (accounts/dbSnapshot), `Models/`, `Controllers/`, `Services/`, `Views/`
**Files scanned:** 12 read penuh/parsial (3 upstream + 5 analog + 4 verifikasi target)
**Pattern extraction date:** 2026-06-04
**Critical Corrections honored:** C-1 (VM @ CDPDashboardViewModel.cs:111) · C-2 (Records.cshtml label-unify only, bukan Failed-fix) · C-3 (group PassedCount = verify-only, 0 code change)
