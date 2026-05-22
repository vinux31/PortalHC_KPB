# Phase 321: Assessment Edit Jawaban Peserta — Pattern Map

**Mapped:** 2026-05-21
**Files analyzed:** 13 (8 create + 5 modify)
**Analogs found:** 13 / 13 (100% coverage)
**Stack:** .NET 8 + EF Core 8 + Razor + Bootstrap 5 + vanilla JS + SignalR

> **Catatan path correction RESEARCH:** View folder = `Views/Admin/` (controller override `AssessmentAdminController.cs:53-57`), URL = `/Admin/...`, toast = `window.showAssessmentToast` (`assessment-hub.js:96`), modal Activity Log INLINE di `Views/Admin/AssessmentMonitoringDetail.cshtml:540-559` (bukan partial), DI fields `_hubContext` + `_gradingService` sudah di-inject (line 27-49).

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Models/AssessmentEditLog.cs` | model (entity) | event-driven (audit append-only) | `Models/AuditLog.cs` + `Models/SessionElemenTeknisScore.cs` | exact (audit) + exact (FK→AssessmentSession) |
| `Data/ApplicationDbContext.cs` | data-context | EF config (DbSet + fluent index) | existing `SessionElemenTeknisScores` DbSet line 77 + `AssessmentSession` block line 170-230 (HasIndex) | exact |
| `Migrations/{ts}_AddAssessmentEditLogs.cs` | migration | DDL CreateTable + CreateIndex | `Migrations/20260221032754_AddAuditLog.cs` (single table + 3 index) | exact |
| `Helpers/AssessmentEditEligibility.cs` | helper | pure-function + async DB-check | `Helpers/CertNumberHelper.cs` (static class + async DB query) | exact |
| `Models/ViewModels/EditPesertaAnswersViewModel.cs` | view-model | request-response (GET render) | `Models/EditTrainingRecordViewModel.cs` (form-bound POCO) | role-match (project pakai `Models/` flat namespace, BUKAN `Models/ViewModels/` subfolder) |
| `Models/ViewModels/EditAnswersSubmission.cs` | dto | request-response (POST + Preview) | `Models/CreateManualAssessmentViewModel.cs` (form POST DTO) | role-match (same namespace consideration) |
| `Services/GradingService.cs` | service | CRUD + compute + cascade | `GradingService.GradeAndCompleteAsync` line 49-326 (existing self-analog) | exact (refactor in-file) |
| `Controllers/AssessmentAdminController.cs` | controller | request-response (GET/POST + JSON) | `AssessmentAdminController.ExportAssessmentResults` line 3651 + `GetActivityLog` line 5643 (JSON endpoint) | exact (same file Phase 320 just shipped) |
| `Views/Admin/EditPesertaAnswers.cshtml` | view (form) | request-response | `Views/Admin/EditAssessment.cshtml` (form @model + AntiForgeryToken + asp-action) | role-match |
| `wwwroot/js/edit-peserta-answers.js` | js-frontend | event-driven (DOM + AJAX) | `wwwroot/js/assessment-hub.js` (vanilla IIFE + global expose) | role-match (different flow but same pattern: vanilla, no jQuery) |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` (per-row action column) | view-fragment | request-response | existing inline buttons line 286-338 (self-analog refactor) | exact |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` (Activity Log modal body refactor) | view-fragment | request-response + AJAX lazy-load | modal inline line 540-559 (self-analog refactor) | exact |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` (SignalR handler inline script) | js-handler | event-driven (SignalR push) | `workerSubmitted` handler line 1243-1300 (self-analog) | exact |
| `Views/Admin/_EditHistoryPartial.cshtml` | view (partial) | request-response (AJAX HTML fragment) | `Views/Admin/_PreviewQuestion.cshtml` (PartialView @model collection) | role-match |

---

## Pattern Assignments

### `Models/AssessmentEditLog.cs` (model, audit append-only)

**Primary analog:** `Models/AuditLog.cs` (generic audit pattern)
**Secondary analog:** `Models/SessionElemenTeknisScore.cs` (FK→AssessmentSession pattern)

**Imports + namespace pattern** (`AuditLog.cs:1-4`):
```csharp
using System.ComponentModel.DataAnnotations;

namespace HcPortal.Models
{
```

**Actor capture pattern** (`AuditLog.cs:11-30`) — apply identical convention `ActorUserId` + `ActorName` ("NIP - FullName") + `ActionType`:
```csharp
[Required]
public string ActorUserId { get; set; } = "";

[Required]
public string ActorName { get; set; } = "";

[Required]
[MaxLength(50)]
public string ActionType { get; set; } = "";
```

**UTC timestamp default pattern** (`AuditLog.cs:51`):
```csharp
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
```

**FK→AssessmentSession navigation pattern** (`SessionElemenTeknisScore.cs:9-11`):
```csharp
public int AssessmentSessionId { get; set; }
[ForeignKey("AssessmentSessionId")]
public virtual AssessmentSession AssessmentSession { get; set; } = null!;
```

> **Adaptasi:** Untuk `AssessmentEditLog` pakai nullable nav (`virtual AssessmentSession? AssessmentSession`) sesuai RESEARCH Task 1 line 146 — karena audit table standalone, nav property optional saja.

---

### `Data/ApplicationDbContext.cs` (data-context, EF config)

**Analog:** existing `SessionElemenTeknisScores` DbSet (line 77) + `AssessmentSession` fluent block (line 170-230)

**DbSet registration pattern** (line 76-77):
```csharp
// ET Scores per Session — Phase 223
public DbSet<SessionElemenTeknisScore> SessionElemenTeknisScores { get; set; }
```

> Apply identical: insert dekat line 78 dengan comment header `// Edit Audit per Question — Phase 321`.

**Fluent index pattern with descending order** (line 177-186 + line 205-209):
```csharp
// Indexes for performance
entity.HasIndex(a => a.UserId);
entity.HasIndex(a => new { a.UserId, a.Status });
entity.HasIndex(a => a.Schedule);

// Phase 192: Unique filtered index on NomorSertifikat (excludes nulls)
entity.HasIndex(a => a.NomorSertifikat)
    .IsUnique()
    .HasFilter("[NomorSertifikat] IS NOT NULL")
    .HasDatabaseName("IX_AssessmentSessions_NomorSertifikat_Unique");
```

> Apply pattern: composite index `(AssessmentSessionId, EditedAt DESC)` dengan `.IsDescending(false, true)` + `.HasDatabaseName("IX_AssessmentEditLogs_SessionId_EditedAt")`. RESEARCH Task 1 Step 2 line 184-194 sudah kasih markup verbatim.

**Restrict cascade pattern** (line 256-258 KkjFile block):
```csharp
entity.HasOne(f => f.OrganizationUnit)
      .WithMany(b => b.KkjFiles)
      .HasForeignKey(f => f.OrganizationUnitId)
      .OnDelete(DeleteBehavior.Restrict);
```

> Apply identical `OnDelete(DeleteBehavior.Restrict)` untuk FK AssessmentEditLog → AssessmentSession (RESEARCH Task 1 line 188-189).

---

### `Migrations/{ts}_AddAssessmentEditLogs.cs` (migration, DDL)

**Analog:** `Migrations/20260221032754_AddAuditLog.cs`

**Boilerplate + table create + multi-index pattern** (`AddAuditLog.cs:1-47`):
```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ActorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ...
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActionType",
                table: "AuditLogs",
                column: "ActionType");
            ...
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditLogs");
        }
    }
}
```

> File ini di-generate otomatis oleh `dotnet ef migrations add AddAssessmentEditLogs`. Pattern di atas konfirmasi shape outputnya. Note: index descending `EditedAt DESC` akan keluar sebagai annotation pada CreateIndex setelah `.IsDescending()` di fluent (T-SQL generation auto). Verify file hasil generate sesuai RESEARCH Task 1 Step 3 expected output.

---

### `Helpers/AssessmentEditEligibility.cs` (helper, pure + async DB)

**Analog:** `Helpers/CertNumberHelper.cs` (static class, mixed pure + async)

**Static class + namespace pattern** (`CertNumberHelper.cs:1-11`):
```csharp
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Shared helper for NomorSertifikat generation (Phase 227 CLEN-04).
    /// Extracted from AdminController private methods to allow reuse in CMPController.
    /// </summary>
    public static class CertNumberHelper
    {
```

**Async helper pattern with `ApplicationDbContext`** (`CertNumberHelper.cs:23-35`):
```csharp
public static async Task<int> GetNextSeqAsync(ApplicationDbContext context, int year)
{
    var existing = await context.AssessmentSessions
        .Where(s => s.NomorSertifikat != null && s.NomorSertifikat.EndsWith($"/{year}"))
        .Select(s => s.NomorSertifikat!)
        .ToListAsync();
    ...
}
```

**Pure sync helper pattern** (`CertNumberHelper.cs:12-21`):
```csharp
public static string ToRomanMonth(int month) => month switch { ... };
public static string Build(int seq, DateTime date) => ...;
```

> Apply: 2 method — `IsEditableAsync(ApplicationDbContext, AssessmentSession)` (async, query UserPackageAssignment) + `IsEditableShallow(AssessmentSession)` (sync, view-side gating). RESEARCH Task 2 Step 1 markup verbatim.

---

### `Models/ViewModels/EditPesertaAnswersViewModel.cs` + `Models/ViewModels/EditAnswersSubmission.cs` (view-model, DTO)

**Project convention warning:** Existing project pakai **`namespace HcPortal.Models`** flat (file langsung di `Models/`), bukan `Models/ViewModels/` subfolder. Contoh: `Models/EditTrainingRecordViewModel.cs:4` → `namespace HcPortal.Models`, `Models/AssessmentMonitoringViewModel.cs:1` → `namespace HcPortal.Models`.

**RESEARCH Task 5 + Task 8 menggunakan namespace `HcPortal.Models.ViewModels`** (new subfolder). Planner WAJIB konfirmasi: apakah ikut RESEARCH (introduce subfolder baru — boleh, controller pakai full qualifier `HcPortal.Models.ViewModels.EditAnswersSubmission`) ATAU ikut existing convention flat (`Models/EditPesertaAnswersViewModel.cs` namespace `HcPortal.Models`).

**Analog DTO pattern** (`Models/EditTrainingRecordViewModel.cs:1-20`):
```csharp
using System.ComponentModel.DataAnnotations;

namespace HcPortal.Models
{
    public class EditTrainingRecordViewModel
    {
        // Hidden inputs — no validation attributes
        public int Id { get; set; }
        public string WorkerId { get; set; } = "";
        public string WorkerName { get; set; } = "";

        [Required(ErrorMessage = "Nama Pelatihan harus diisi")]
        [Display(Name = "Nama Pelatihan")]
        public string Judul { get; set; } = "";
```

> Apply pattern: collection properties default empty (`= new()`), POCO no behavior. RESEARCH Task 5 Step 1 + Task 8 Step 1 sudah kasih markup verbatim.

---

### `Services/GradingService.cs` (service — extend in-file)

**Analog:** existing `GradeAndCompleteAsync` (line 49-327) sebagai self-analog. Insert method baru SETELAH line 326 (sebelum closing brace class).

**Service signature pattern** (line 16-30):
```csharp
public class GradingService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkerDataService _workerDataService;
    private readonly ILogger<GradingService> _logger;

    public GradingService(
        ApplicationDbContext context,
        IWorkerDataService workerDataService,
        ILogger<GradingService> logger)
    { ... }
```

**ExecuteUpdateAsync with status guard pattern** (line 231-248) — referensi inti untuk `RegradeAfterEditAsync`:
```csharp
// ExecuteUpdateAsync dengan WHERE Status != "Completed" sebagai status guard (D-04)
var rowsAffected = await _context.AssessmentSessions
    .Where(s => s.Id == session.Id && s.Status != "Completed")
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.Score, finalPercentage)
        .SetProperty(r => r.Status, "Completed")
        .SetProperty(r => r.Progress, 100)
        .SetProperty(r => r.IsPassed, isPassed)
        .SetProperty(r => r.CompletedAt, DateTime.UtcNow)
    );

if (rowsAffected == 0)
{
    _logger.LogWarning("GradingService: race condition untuk session {SessionId} — session sudah Completed, skip.", session.Id);
    return false;
}
```

> Apply identical `ExecuteUpdateAsync` + guard `WHERE Status == "Completed"` (kebalikan, karena re-grade existing Completed) + rowsAffected check + LogWarning + throw. RESEARCH Task 4 line 465-479 markup verbatim.

**Cert generation with retry pattern** (line 287-321) — referensi inti untuk Fail→Pass cascade:
```csharp
if (session.GenerateCertificate && isPassed)
{
    var certNow = DateTime.Now;
    int certYear = certNow.Year;
    int certAttempts = 0;
    const int maxCertAttempts = 3;
    bool certSaved = false;

    while (!certSaved && certAttempts < maxCertAttempts)
    {
        certAttempts++;
        try
        {
            var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, certYear);
            await _context.AssessmentSessions
                .Where(s => s.Id == session.Id && s.NomorSertifikat == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow))
                );
            certSaved = true;
        }
        catch (DbUpdateException ex) when (certAttempts < maxCertAttempts && CertNumberHelper.IsDuplicateKeyException(ex))
        {
            // Retry dengan sequence baru (T-296-03)
        }
    }
}
```

> Apply identical retry-3x pattern di `RegradeAfterEditAsync` Fail→Pass branch (RESEARCH Task 4 line 515-534). Tambah `.SetProperty(r => r.ValidUntil, validUntil)` (= `certNow.AddYears(3)`).

**TrainingRecord upsert pattern** (line 258-285):
```csharp
var judul = $"Assessment: {session.Title}";
bool trainingRecordExists = await _context.TrainingRecords.AnyAsync(t =>
    t.UserId == session.UserId &&
    t.Judul == judul &&
    t.Tanggal == session.Schedule);

if (!trainingRecordExists && session.AssessmentType != "PreTest")
{
    try
    {
        _context.TrainingRecords.Add(new TrainingRecord
        {
            UserId = session.UserId,
            Judul = judul,
            Kategori = session.Category ?? "Assessment",
            Tanggal = session.Schedule,
            TanggalSelesai = DateTime.UtcNow,
            Penyelenggara = "Internal",
            Status = isPassed ? "Passed" : "Failed"
        });
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        _logger.LogWarning("Duplicate TrainingRecord detected ...");
    }
}
```

> Apply identical pattern di `RegradeAfterEditAsync` (RESEARCH Task 4 line 543-564). Carry `Kategori = session.Category ?? "Assessment"` + `Penyelenggara = "Internal"` defensive default.

---

### `Controllers/AssessmentAdminController.cs` (controller — extend in-file)

**Analog:** existing actions di same file. Recent Phase 320 ship `ExportAssessmentResults` line 3651, `GetActivityLog` line 5643 (JSON endpoint).

**Class-level routing + authorization pattern** (line 17-30):
```csharp
namespace HcPortal.Controllers
{
    [Route("Admin/[action]")]
    public class AssessmentAdminController : AdminBaseController
    {
        private readonly IMemoryCache _cache;
        ...
        private readonly IHubContext<AssessmentHub> _hubContext;
        private readonly IWorkerDataService _workerDataService;
        private readonly GradingService _gradingService;
```

> **CRITICAL:** Pakai field `_hubContext` + `_gradingService` (sudah di-inject ctor line 27-49). JANGAN tambah `[FromServices]` parameter di action signature (RESEARCH koreksi point 7).

**View resolution override pattern** (line 52-57):
```csharp
// Override View resolution to use Views/Admin/ folder
protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
...
protected new PartialViewResult PartialView(string viewName, object? model) => base.PartialView(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);
```

> Konsekuensi: `return View(vm)` di `EditPesertaAnswers` action resolve ke `~/Views/Admin/EditPesertaAnswers.cshtml`. `return PartialView("_EditHistoryPartial", logs)` resolve ke `~/Views/Admin/_EditHistoryPartial.cshtml`. **JANGAN buat `Views/AssessmentAdmin/` folder.**

**HttpGet + Authorize + redirect pattern** (typical action — `ExportAssessmentResults` line 3649-3665):
```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> ExportAssessmentResults(string title, string category, DateTime scheduleDate)
{
    var sessions = await _context.AssessmentSessions...

    if (!sessions.Any())
    {
        TempData["Error"] = "No sessions found for this assessment group.";
        return RedirectToAction("ManageAssessment");
    }
    ...
}
```

> Apply identical:
> - `[HttpGet]` + `[Authorize(Roles = "Admin, HC")]` untuk GET (EDIT-01)
> - `[HttpPost]` + `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` untuk POST (RESEARCH Task 8)
> - TempData["Error"] pattern untuk validation failure + redirect

**JSON endpoint pattern** (`GetActivityLog` line 5641-5699):
```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> GetActivityLog(int sessionId)
{
    var session = await _context.AssessmentSessions
        .FirstOrDefaultAsync(s => s.Id == sessionId);

    if (session == null)
        return NotFound(new { error = "Session not found." });
    ...
    return Json(new { summary, events = eventsFormatted });
}
```

> Apply identical untuk `PreviewEditScore` (POST + JSON). RESEARCH Task 9 line 1410-1435 markup verbatim. Return shape: `{oldScore, newScore, oldIsPassed, newIsPassed, hasCert, nomorSertifikat, willGenerateCert}`.

**PartialView return pattern** (line 5630):
```csharp
return PartialView("_PreviewQuestion", q);
```

> Apply identical untuk `EditHistoryPartial` action (RESEARCH Task 12 Step 3): `return PartialView("_EditHistoryPartial", logs)`.

**AuditLog write pattern** — search `_auditLog.LogAsync` in controller (line 409, 462, 492, 515, 1383, 1425, 1885, 1988, 2134). Sample (line 409):
```csharp
await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "AddCategory", ...);
```

> Untuk Phase 321 dual-write, RESEARCH Task 8 line 1333-1341 pakai direct `_context.AuditLogs.Add(new AuditLog { ... })` instead of `_auditLog.LogAsync(...)` — verify saat execute apakah `AuditLogService` punya method untuk shape `ActionType = "EditAssessmentAnswer"`. Kalau ada, prefer `_auditLog.LogAsync` for consistency.

---

### `Views/Admin/EditPesertaAnswers.cshtml` (view — form page)

**Analog:** `Views/Admin/EditAssessment.cshtml` (form @model + AntiForgeryToken)

**Header + ViewData + breadcrumb pattern** (`EditAssessment.cshtml:1-46`):
```html
@model HcPortal.Models.AssessmentSession

@{
    ViewData["Title"] = "Edit Assessment";
    ViewData["ContainerClass"] = "container-fluid";
}

<div class="container-fluid px-4 py-4">
    <!-- Breadcrumb -->
    <nav aria-label="breadcrumb" class="mb-3">
        <ol class="breadcrumb">
            <li class="breadcrumb-item"><a href="@Url.Action("Index", "Admin")">Kelola Data</a></li>
            <li class="breadcrumb-item"><a href="@Url.Action("ManageAssessment", "AssessmentAdmin")">Manage Assessment &amp; Training</a></li>
            <li class="breadcrumb-item active" aria-current="page">Edit Assessment</li>
        </ol>
    </nav>

    <!-- Page Header -->
    <div class="mb-4">
        <div class="d-flex justify-content-between align-items-center">
            <div>
                <h2 class="fw-bold mb-1">Edit Assessment</h2>
                <p class="text-muted mb-0">Perbarui detail dan konfigurasi assessment</p>
            </div>
            <a href="@Url.Action("ManageAssessment", "AssessmentAdmin")" class="btn btn-outline-secondary">
                <i class="bi bi-arrow-left me-1"></i> Kembali
            </a>
        </div>
    </div>

    <!-- Warning Alert -->
    @if (TempData["Warning"] != null)
    {
        <div class="alert alert-warning alert-dismissible fade show" role="alert">
            <i class="bi bi-exclamation-circle-fill me-2"></i>
            @TempData["Warning"]
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    }
```

> Apply pattern: `@model HcPortal.Models.ViewModels.EditPesertaAnswersViewModel` (atau `HcPortal.Models...` per namespace decision di section di atas), `ViewData["Title"]`, TempData display block. **UI-SPEC override:** RESEARCH Task 6 markup pakai back-link sederhana (bukan breadcrumb full) + h3 (bukan h2) — ikut UI-SPEC + RESEARCH Task 6 line 770-924 verbatim. Breadcrumb pattern di atas ADA referensi saja kalau planner preferensi konsistensi dengan EditAssessment.

**Form + AntiForgeryToken pattern** (RESEARCH Task 6 line 798-803, mirror existing project convention):
```html
<form id="editAnswersForm" method="post"
      asp-action="SubmitEditAnswers"
      data-preview-url="@Url.Action("PreviewEditScore", new { sessionId = Model.Session.Id })">
    @Html.AntiForgeryToken()
    <input type="hidden" name="SessionId" value="@Model.Session.Id" />
    <input type="hidden" name="UpdatedAt" value="@Model.UpdatedAt.ToString("O")" />
```

**Scripts section pattern** (typical Razor):
```html
@section Scripts {
  <script src="~/js/edit-peserta-answers.js" asp-append-version="true"></script>
}
```

> Apply RESEARCH Task 6 Step 1 markup line 765-924 verbatim — D-05 verbose reason labels + D-06 modal copy sudah reconciled.

---

### `wwwroot/js/edit-peserta-answers.js` (frontend — dirty state + AJAX + modal)

**Analog:** `wwwroot/js/assessment-hub.js` (vanilla IIFE + DOM API + no jQuery)

**Vanilla DOMContentLoaded + querySelectorAll pattern** (RESEARCH Task 7 line 963-1133 verbatim).

**Toast invocation pattern** (`assessment-hub.js:9-33` — showToast definition + line 96 global expose):
```javascript
function showToast(message, linkUrl, linkText) {
    var el = document.createElement('div');
    el.className = 'assessment-toast';
    el.textContent = message;
    ...
    document.body.appendChild(el);
    requestAnimationFrame(function () { el.classList.add('visible'); });
    setTimeout(function () { ... }, 5000);
}
...
window.showAssessmentToast = showToast;
```

> **JANGAN duplikat fungsi toast**. Reuse via `window.showAssessmentToast(message)` per CONTEXT D-07 + UI-SPEC §Toast SignalR (RESEARCH koreksi point 4). Edit-peserta-answers.js mostly local form state; toast hanya dipakai di SignalR handler (di view inline script, BUKAN di file ini).

**Bootstrap modal API pattern** (RESEARCH Task 7 line 968):
```javascript
const flipModal = new bootstrap.Modal(document.getElementById("flipConfirmModal"));
...
flipModal.show();
flipModal.hide();
```

**Fetch with AntiForgeryToken pattern** (RESEARCH Task 7 line 1084-1090):
```javascript
const resp = await fetch(previewUrl, {
    method: "POST",
    body: fd,
    headers: {
        "RequestVerificationToken": form.querySelector('input[name=__RequestVerificationToken]').value
    }
});
```

> Apply identical for `PreviewEditScore` AJAX call. Header name `RequestVerificationToken` (no `X-` prefix) is the ASP.NET Core convention.

---

### `Views/Admin/AssessmentMonitoringDetail.cshtml` (per-row action column refactor)

**Analog:** existing inline buttons line 286-338 (self-analog refactor)

**Existing inline button pattern** (line 286-338):
```html
<td>
    @* Reshuffle -- package mode, Not started or Abandoned *@
    @if (Model.IsPackageMode && (session.UserStatus == "Not started" || session.UserStatus == "Abandoned"))
    {
        <button type="button"
                class="btn btn-outline-primary btn-sm me-1"
                data-session-id="@session.Id"
                onclick="reshuffleWorker(@session.Id)"
                title="Reshuffle paket ujian">
            <i class="bi bi-shuffle me-1"></i>Reshuffle
        </button>
    }
    @* Akhiri Ujian -- InProgress only *@
    @if (session.UserStatus == "InProgress")
    {
        <form asp-action="AkhiriUjian" asp-controller="AssessmentAdmin" method="post" class="d-inline me-1"
              onsubmit="return confirm('Akhiri ujian untuk @(session.UserFullName)? ...')">
            @Html.AntiForgeryToken()
            <input type="hidden" name="id" value="@session.Id" />
            <button type="submit" class="btn btn-danger btn-sm">
                <i class="bi bi-x-circle me-1"></i>Akhiri Ujian
            </button>
        </form>
    }
    @* Reset -- all statuses except Cancelled *@
    @if (session.UserStatus != "Cancelled")
    {
        <form asp-action="ResetAssessment" asp-controller="AssessmentAdmin" method="post" class="d-inline me-1"
              onsubmit="return confirm('Reset sesi ini? ...')">
            @Html.AntiForgeryToken()
            <input type="hidden" name="id" value="@session.Id" />
            <button type="submit" class="btn btn-warning btn-sm">
                <i class="bi bi-arrow-counterclockwise me-1"></i>Reset
            </button>
        </form>
    }
    @* View Results -- Completed only *@
    @if (session.UserStatus == "Completed")
    {
        <a href="@Url.Action("Results", "CMP", new { id = session.Id })" target="_blank" class="btn btn-success btn-sm">
            View Results
        </a>
    }
    @* Activity Log button -- always shown *@
    <button type="button"
            class="btn btn-outline-secondary btn-sm ms-1 btn-activity-log"
            data-session-id="@session.Id"
            data-worker-name="@session.UserFullName"
            title="Lihat activity log">
        <i class="bi bi-clock-history"></i>
    </button>
</td>
```

> **Refactor strategy:** PRESERVE handler/behavior existing (form action + onsubmit confirm + onclick reshuffleWorker function call), HANYA reposisi markup ke dropdown ⋮ structure per RESEARCH Task 10 line 1476-1554 verbatim. Status strings: `"Not started"` (lowercase 's' + space) + `"InProgress"` (no space) + `"Cancelled"` + `"Completed"` — verify dari line 288, 299, 311, 323.

**Conditional render via static helper** (new pattern but trivial):
```html
@{ var canEdit = HcPortal.Helpers.AssessmentEditEligibility.IsEditableShallow(session); }
...
@if (canEdit) { <li><a class="dropdown-item" ...>Edit Jawaban</a></li> }
```

---

### `Views/Admin/AssessmentMonitoringDetail.cshtml` (Activity Log modal body refactor)

**Analog:** existing modal inline line 540-559 (self-analog refactor)

**Existing modal structure** (line 540-559):
```html
<!-- Activity Log Modal -->
<div class="modal fade" id="activityLogModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">
                    <i class="bi bi-clock-history me-2 text-primary"></i>Activity Log:
                    <span id="logWorkerName" class="text-primary"></span>
                </h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <div id="logSummary" class="mb-3 p-2 bg-light rounded small text-muted"></div>
                <div id="logTimeline"></div>
                <div id="logLoading" class="text-center d-none">
                    <div class="spinner-border spinner-border-sm me-2"></div>Memuat...
                </div>
            </div>
        </div>
    </div>
</div>
```

> **Refactor strategy:** Bungkus existing `#logSummary`+`#logTimeline`+`#logLoading` ke dalam `.tab-pane#tab-timeline.show.active`, tambah tab nav header + tab-pane `#tab-edit-history`. JANGAN ubah ID existing (`logSummary`, `logTimeline`, `logLoading`) — JS line 1076 `fetch(appUrl('/Admin/GetActivityLog?...'))` populate ke selectors itu (PRESERVE).

> Markup refactor verbatim di RESEARCH Task 12 Step 2 line 1689-1721.

**Click delegate pattern existing** (line 926-934):
```javascript
document.addEventListener('click', function (e) {
    var btn = e.target.closest('.btn-activity-log');
    if (btn) {
        var sid = parseInt(btn.getAttribute('data-session-id'), 10);
        var wname = btn.getAttribute('data-worker-name') || '';
        showActivityLog(sid, wname);
    }
});
```

> Apply pattern: tambah event listener `click` untuk reset Edit History tab cache + set `modal.dataset.currentSessionId` (RESEARCH Task 12 Step 5 line 1830-1841).

---

### `Views/Admin/AssessmentMonitoringDetail.cshtml` (SignalR `workerAnswerEdited` handler inline)

**Analog:** existing `workerSubmitted` handler line 1244-1300 (self-analog)

**SignalR handler pattern** (line 1244-1300):
```javascript
window.assessmentHub.on('workerSubmitted', function(data) {
    var tr = document.querySelector('tr[data-session-id="' + data.sessionId + '"]');
    ...
    if (tr) {
        var tds = tr.querySelectorAll('td');
        // Progress (col 1)
        if (tds[1] && data.totalQuestions > 0) tds[1].textContent = data.totalQuestions + '/' + data.totalQuestions;
        // Status (col 2)
        if (tds[2]) tds[2].innerHTML = '<span class="badge bg-success">Completed</span>';
        // Score (col 3)
        if (tds[3]) tds[3].textContent = data.score !== null && data.score !== undefined ? data.score + '%' : '—';
        // Result (col 4)
        if (tds[4]) {
            tds[4].textContent = data.result || '—';
            tds[4].className = 'result-cell ' + (data.result === 'Pass' ? 'text-success fw-semibold' : ...);
        }
        ...
    }
    if (typeof window.showAssessmentToast === 'function') {
        window.showAssessmentToast(data.workerName + ' menyelesaikan ujian (Skor: ' + data.score + '%)');
    }
    ...
});
```

> Apply identical pattern untuk `workerAnswerEdited` (RESEARCH Task 11 line 1604-1633 verbatim). Selector `tr[data-session-id]` + `.result-cell` confirmed exist (line 282). **Score cell** kemungkinan tanpa class — verify dengan `grep` saat execute (RESEARCH Task 11 Step 3 line 1645). Kalau no class, tambah di markup Task 10 (`tds[3]` aksi via index — atau preferensi pakai class `.score-cell`).

**Toast reuse pattern** (line 1235-1236, 1279-1280):
```javascript
if (typeof window.showAssessmentToast === 'function') {
    window.showAssessmentToast(data.workerName + ' memulai ujian');
}
```

> Apply identical pattern. **Open question RESEARCH line 1638:** existing toast = 5 detik auto-dismiss (assessment-hub.js:32), UI-SPEC D-07 spec = 8 detik. Recommend: keep 5 detik (existing consistency) unless user override.

---

### `Views/Admin/_EditHistoryPartial.cshtml` (partial view)

**Analog:** `Views/Admin/_PreviewQuestion.cshtml`

**Partial view header pattern** (`_PreviewQuestion.cshtml:1-7`):
```html
@using HcPortal.Models
@model PackageQuestion

@{
    var qtype = Model.QuestionType ?? "MultipleChoice";
}
```

> Apply: `@model List<HcPortal.Models.AssessmentEditLog>` + local helper function `string ReasonLabel(string code) => code switch { ... }` (D-05 verbose mapping). RESEARCH Task 12 Step 4 line 1748-1788 markup verbatim.

**Empty-state pattern** (typical):
```html
@if (!Model.Any())
{
    <p class="text-muted">Belum ada edit untuk sesi ini.</p>
}
```

**List timeline pattern** (RESEARCH Task 12 Step 4 line 1769-1787):
```html
<ul class="list-unstyled">
    @foreach (var log in Model)
    {
        <li class="border-bottom pb-2 mb-2">
            <div class="small text-muted">[@log.EditedAt.ToString("yyyy-MM-dd HH:mm")] Soal #@log.PackageQuestionId</div>
            <div class="fw-bold">@log.QuestionTextSnapshot</div>
            <div>
                <span class="text-muted">@(string.IsNullOrEmpty(log.OldAnswerTextSnapshot) ? "—" : log.OldAnswerTextSnapshot)</span>
                → <span class="text-success">@log.NewAnswerTextSnapshot</span>
            </div>
            <div class="small">oleh @log.ActorRole (@log.ActorName)</div>
            <div class="small text-muted">
                Alasan: @ReasonLabel(log.ReasonCode)
                @if (!string.IsNullOrEmpty(log.ReasonText)) { @($" — {log.ReasonText}") }
            </div>
        </li>
    }
</ul>
```

---

## Shared Patterns (cross-cutting)

### 1. Authorization
**Source:** `Controllers/AssessmentAdminController.cs:62, 117, 3650, 5642` (every Admin/HC action)
**Apply to:** All 4 new controller actions (`EditPesertaAnswers` GET, `SubmitEditAnswers` POST, `PreviewEditScore` POST, `EditHistoryPartial` GET)
```csharp
[HttpGet]   // or [HttpPost] + [ValidateAntiForgeryToken]
[Authorize(Roles = "Admin, HC")]
```

### 2. Error Handling (TempData + redirect)
**Source:** `Controllers/AssessmentAdminController.cs:3661-3665` + RESEARCH Task 8 line 1369-1375
**Apply to:** All write actions (POST SubmitEditAnswers)
```csharp
try
{
    using var tx = await _context.Database.BeginTransactionAsync();
    ...
    await tx.CommitAsync();
    TempData["Success"] = "...";
    return RedirectToAction(...);
}
catch (Exception ex)
{
    await tx.RollbackAsync();
    _logger.LogError(ex, "Edit jawaban gagal untuk session {SessionId}", session.Id);
    TempData["Error"] = "Terjadi kesalahan saat menyimpan. Coba lagi atau hubungi administrator.";
    return RedirectToAction(...);
}
```

### 3. Anti-Forgery Token
**Source:** existing form pattern (`AssessmentMonitoringDetail.cshtml:303, 315`)
**Apply to:** All POST forms (EditPesertaAnswers form + flip preview AJAX)
```html
@Html.AntiForgeryToken()
```
+ Controller decorator:
```csharp
[ValidateAntiForgeryToken]
```
+ AJAX header:
```javascript
headers: { "RequestVerificationToken": form.querySelector('input[name=__RequestVerificationToken]').value }
```

### 4. SignalR Broadcast to `monitor-{batchKey}` Group
**Source:** `Hubs/AssessmentHub.cs:57` (group naming convention) + RESEARCH Task 8 line 1349-1357
**Apply to:** POST SubmitEditAnswers (broadcast `workerAnswerEdited`)
```csharp
var batchKey = $"{session.Title}|{session.Category}|{session.Schedule.Date:yyyy-MM-dd}";
await _hubContext.Clients.Group($"monitor-{batchKey}").SendAsync("workerAnswerEdited", new
{
    sessionId = session.Id,
    workerName = session.User?.FullName ?? "Unknown",
    oldScore, newScore, oldIsPassed, newIsPassed,
    actorName, actorRole
});
```

### 5. Cache Invalidation
**Source:** field `_cache` (IMemoryCache) injected line 22 + RESEARCH Task 8 line 1347
**Apply to:** POST SubmitEditAnswers
```csharp
_cache.Remove($"exam-status-{session.Id}");
```

### 6. Logging
**Source:** `_logger.LogInformation` / `LogWarning` / `LogError` (line 57-58, 209, 245, 282, 318)
**Apply to:** All write actions + RegradeAfterEditAsync cascade
```csharp
_logger.LogInformation("RegradeAfterEditAsync: session {SessionId} flip Pass→Fail — cert dicabut, TR=Failed.", session.Id);
_logger.LogWarning("GradingService.RegradeAfterEditAsync: session {SessionId} bukan Completed (race).", session.Id);
_logger.LogError(ex, "Edit jawaban gagal untuk session {SessionId}", session.Id);
```

### 7. Bootstrap 5 + Icons + No-New-Asset
**Source:** existing CDN (Layout) + `_PreviewQuestion.cshtml`, `AssessmentMonitoringDetail.cshtml` (`bi bi-*` consistent)
**Apply to:** All view markup
- Icons: `bi bi-pencil-square`, `bi bi-arrow-counterclockwise`, `bi bi-x-octagon`, `bi bi-shuffle`, `bi bi-clock-history`, `bi bi-bar-chart-line` (UI-SPEC §Surface 2)
- NO new CSS file (UI-SPEC line 35), inline `style=""` minimal hanya untuk `min-height:40px` di dropdown toggle (UI-SPEC §Spacing eksepsi)
- NO new NuGet (CONTEXT D-12)

### 8. Bahasa Indonesia + UI-SPEC Verbose Copy
**Source:** CLAUDE.md mandatory + UI-SPEC §Copywriting Contract
**Apply to:** All user-facing strings (controller TempData, view labels, JS alerts, modal copy)
- Reason labels D-05 (verbose Bahasa Indonesia)
- Flip modal D-06 eksplisit konsekuensi
- Toast D-07 template `{actorRole} {actorName} edit jawaban {workerName}: Score X→Y, Pass→Fail`

### 9. Project Convention Flags (planner WAJIB resolve)
| Flag | Existing convention | RESEARCH guidance | Decision needed |
|------|---------------------|-------------------|-----------------|
| ViewModel folder | `Models/*.cs` flat namespace `HcPortal.Models` | `Models/ViewModels/*.cs` namespace `HcPortal.Models.ViewModels` | Confirm with planner: introduce subfolder or stay flat. Either works; pick one and apply consistently across `EditPesertaAnswersViewModel.cs` + `EditAnswersSubmission.cs`. |
| Toast timeout | 5 detik (`assessment-hub.js:32`) | UI-SPEC D-07 spec says 8 detik | Recommend keep 5 detik (consistency across all SignalR events). Override only if user request. |
| Score cell class | Verify with grep `score-cell` | RESEARCH Task 11 Step 3 line 1645 says verify | If missing, add `class="score-cell"` to existing markup (Task 10 refactor) or use `tds[N]` index access in handler. |
| AuditLog dual-write | Existing `_auditLog.LogAsync(...)` pattern | RESEARCH Task 8 line 1333 uses direct `_context.AuditLogs.Add(...)` | Verify if `AuditLogService` supports `ActionType = "EditAssessmentAnswer"` shape; prefer service method for consistency, fallback to direct insert if shape mismatch. |

---

## No Analog Found

**Zero gaps.** All 13 target files have at least role-match analog in existing codebase. This reflects Phase 321 = incremental layer addition on top of mature Assessment domain (Phase 320 just shipped same controller, GradingService established Phase 296, SignalR established Phase 53+).

---

## Metadata

**Analog search scope:**
- `Models/*.cs` (60 files scanned)
- `Helpers/*.cs` (7 files scanned)
- `Migrations/*.cs` (~100 files, sampled recent 20 + AddAuditLog reference)
- `Services/GradingService.cs` (existing self-analog)
- `Controllers/AssessmentAdminController.cs` (5879 lines, sampled around line 17-57, 3651, 5630-5700)
- `Views/Admin/*.cshtml` (9 files; deep read EditAssessment + AssessmentMonitoringDetail + _PreviewQuestion)
- `Views/Shared/_*.cshtml` (5 files — confirm no `_ActivityLogModal.cshtml` exists per RESEARCH koreksi)
- `Hubs/AssessmentHub.cs` (group naming verified)
- `wwwroot/js/assessment-hub.js` (toast + hub reuse verified)

**Files scanned:** ~25 source files (read-only)
**Pattern extraction date:** 2026-05-21
**Phase consumer:** `gsd-planner` will produce 4 PLAN files (per CONTEXT D-01):
- `321-01-PLAN.md` (Task 1-2 foundation) — uses patterns: AuditLog, SessionElemenTeknisScore, CertNumberHelper analogs
- `321-02-PLAN.md` (Task 3-4 service) — uses pattern: GradingService self-analog (lines 49-326)
- `321-03-PLAN.md` (Task 5-11 controller+view+JS+dropdown+SignalR) — uses patterns: ExportAssessmentResults, GetActivityLog, EditAssessment view, workerSubmitted handler, dropdown refactor
- `321-04-PLAN.md` (Task 12-13 activity-log+UAT) — uses pattern: _PreviewQuestion partial, modal inline refactor
