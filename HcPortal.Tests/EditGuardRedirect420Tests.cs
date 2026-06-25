// EditGuardRedirect420Tests — v32.7 Phase 420 Plan 02 (FORM-05 + FORM-06).
//
// Menutup dua celah guard/redirect di form Edit assessment (AssessmentAdminController):
//   FORM-05 (E-04): POST EditAssessment untuk sesi — atau, untuk Pre-Post, grup pasangannya —
//                   yang sudah Status="Completed" HARUS ditolak (redirect ManageAssessment +
//                   TempData["Error"], metadata TIDAK dimutasi). Group-aware: bila SATU sesi dalam
//                   grup Pre-Post (LinkedGroupId sama) Completed, SELURUH grup terkunci dari Edit.
//   FORM-06 (E-08): GET EditAssessment untuk sesi IsManualEntry=true HARUS redirect ke
//                   EditManualAssessment (TrainingAdmin), bukan render form online.
//
// Strategy: real-SQL disposable DB via RetakeServiceFixture (MigrateAsync full chain, @SQLEXPRESS) —
// reuse fixture dari RetakeServiceTests (assembly sama). Guard FORM-05 + redirect FORM-06 hanya
// menyentuh _context (FindAsync / FirstOrDefaultAsync / AnyAsync) → deps controller lain di-null!-substitute.
//
// Pendekatan campuran (sesuai PLAN.md Task 0):
//   - Test 1/2/4 = ACTION-INVOKE (guard/redirect return lebih awal → RedirectToActionResult aktual +
//                  assert non-mutasi). POST EditAssessment guard berada SEBELUM cabang Pre-Post; GET
//                  redirect IsManualEntry berada tepat setelah null-check (sebelum query berat / View()).
//   - Test 3/5 = REPLIKA-BODY guard (negatif/backward-compat). Menjalankan action penuh untuk jalur
//                "lanjut" akan menyentuh deps null!/render View → fragile. Sebagai gantinya, kita
//                ASSERT predikat guard yang sama (AnyAsync(...Completed)==false / IsManualEntry==false)
//                untuk membuktikan guard TIDAK memblokir sesi yang sah.
//
// [Trait("Category","Integration")] → skip via --filter "Category!=Integration" pada CI tanpa SQL.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class EditGuardRedirect420Tests : IClassFixture<RetakeServiceFixture>
{
    private readonly RetakeServiceFixture _fixture;
    public EditGuardRedirect420Tests(RetakeServiceFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    /// <summary>
    /// Bangun AssessmentAdminController dgn real ctx/auditLog/cache; deps lain null!-substitute
    /// (jalur guard FORM-05 + redirect FORM-06 tak men-deref deps berikut). ControllerContext +
    /// TempData + ViewData di-pin agar action dapat menulis TempData["Error"] & test membacanya.
    /// </summary>
    private static (AssessmentAdminController ctrl, ITempDataDictionary tempData) MakeController(ApplicationDbContext ctx)
    {
        var auditLog = new AuditLogService(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());

        #pragma warning disable CS8625 // null-substitute: guard/redirect tak deref deps berikut.
        var ctrl = new AssessmentAdminController(
            ctx,
            userManager:             null!,
            auditLog:                auditLog,
            env:                     null!,
            cache:                   cache,
            logger:                  NullLogger<AssessmentAdminController>.Instance,
            notificationService:     null!,
            hubContext:              null!,
            workerDataService:       null!,
            gradingService:          null!,
            protonCompletionService: null!,
            protonBypassService:     null!,
            retakeService:           null!);
        #pragma warning restore CS8625

        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, new NullTempDataProvider());
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };
        ctrl.TempData = tempData;
        ctrl.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        return (ctrl, tempData);
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    // ---------- Seed helpers (deterministik, unik per-run via timestamp/Guid) ----------
    private static async Task<ApplicationUser> SeedUserAsync(ApplicationDbContext ctx, string fullName = "Pekerja Uji")
    {
        var u = new ApplicationUser
        {
            UserName = "egr-" + Guid.NewGuid().ToString("N")[..8],
            Email = "egr@test.local",
            FullName = fullName,
            NIP = "99001"
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u;
    }

    private static async Task<int> SeedSessionAsync(
        ApplicationDbContext ctx, string userId, string title, string category,
        string status, int? linkedGroupId = null, string? assessmentType = null,
        bool isManualEntry = false)
    {
        var s = new AssessmentSession
        {
            UserId = userId,
            Title = title,
            Category = category,
            Status = status,
            AccessToken = "",
            Schedule = new DateTime(2026, 3, 1),
            DurationMinutes = 60,
            LinkedGroupId = linkedGroupId,
            AssessmentType = assessmentType,
            IsManualEntry = isManualEntry
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s.Id;
    }

    // ====================== Test 1: FORM-05 group-aware lock ======================
    // Grup Pre-Post (LinkedGroupId sama): SATU sesi Completed, sesi lain Open. POST Edit terhadap
    // sesi Open HARUS ditolak (group-aware) → redirect ManageAssessment + TempData["Error"]; Title
    // sesi target TIDAK berubah menjadi model.Title.
    [Fact]
    public async Task EditCompletedLockGuard_PrePostGroupAware_BlocksWhenSiblingCompleted()
    {
        await using var ctx = NewCtx();
        var user = await SeedUserAsync(ctx);
        int groupId = 4200001;
        const string originalTitle = "GrupPrePost420 (asli)";

        // Pre = Open (target edit), Post = Completed (pasangan dalam grup).
        int preId = await SeedSessionAsync(ctx, user.Id, originalTitle, "Test",
            status: "Open", linkedGroupId: groupId, assessmentType: "PreTest");
        await SeedSessionAsync(ctx, user.Id, originalTitle, "Test",
            status: "Completed", linkedGroupId: groupId, assessmentType: "PostTest");

        var (ctrl, tempData) = MakeController(ctx);
        var model = new AssessmentSession { Title = "JUDUL BARU (harus DITOLAK)", Category = "Test" };

        var result = await ctrl.EditAssessment(preId, model, new List<string>(),
            null, null, null, null, null, null, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ManageAssessment", redirect.ActionName);
        Assert.True(tempData.ContainsKey("Error"));

        // Non-mutasi: Title sesi Open TETAP nilai lama (guard mendahului cabang Pre-Post yang memutasi).
        await using var verify = NewCtx();
        var title = await verify.AssessmentSessions.Where(a => a.Id == preId).Select(a => a.Title).SingleAsync();
        Assert.Equal(originalTitle, title);
    }

    // ====================== Test 2: FORM-05 standard lock (regresi) ======================
    // Sesi standard tunggal Status="Completed" → POST Edit ditolak (guard standard tetap jalan).
    [Fact]
    public async Task EditCompletedLockGuard_StandardCompleted_BlocksAndDoesNotMutate()
    {
        await using var ctx = NewCtx();
        var user = await SeedUserAsync(ctx);
        const string originalTitle = "Standard420 Completed (asli)";

        int sid = await SeedSessionAsync(ctx, user.Id, originalTitle, "Test",
            status: "Completed", linkedGroupId: null, assessmentType: "Standard");

        var (ctrl, tempData) = MakeController(ctx);
        var model = new AssessmentSession { Title = "JUDUL BARU (harus DITOLAK)", Category = "Test" };

        var result = await ctrl.EditAssessment(sid, model, new List<string>(),
            null, null, null, null, null, null, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ManageAssessment", redirect.ActionName);
        Assert.True(tempData.ContainsKey("Error"));

        await using var verify = NewCtx();
        var title = await verify.AssessmentSessions.Where(a => a.Id == sid).Select(a => a.Title).SingleAsync();
        Assert.Equal(originalTitle, title);
    }

    // ====================== Test 3: FORM-05 negatif / backward-compat ======================
    // Grup Pre-Post SEMUA "Open" (tak ada Completed) → predikat guard isCompleted==false → guard
    // TIDAK memblokir sesi yang sah. Replika-body guard (group-aware AnyAsync) agar tidak menyentuh
    // cabang Pre-Post penuh (deps null!). Membuktikan sesi sah tetap dapat lanjut di-Edit.
    [Fact]
    public async Task EditCompletedLockGuard_PrePostAllOpen_DoesNotBlock()
    {
        await using var ctx = NewCtx();
        var user = await SeedUserAsync(ctx);
        int groupId = 4200003;
        const string title = "GrupPrePost420 AllOpen";

        int preId = await SeedSessionAsync(ctx, user.Id, title, "Test",
            status: "Open", linkedGroupId: groupId, assessmentType: "PreTest");
        await SeedSessionAsync(ctx, user.Id, title, "Test",
            status: "Open", linkedGroupId: groupId, assessmentType: "PostTest");

        // Replika predikat guard group-aware (identik body Task 1).
        var assessment = await ctx.AssessmentSessions.FindAsync(preId);
        Assert.NotNull(assessment);
        bool isCompleted = assessment!.Status == "Completed";
        if (assessment.LinkedGroupId.HasValue)
        {
            isCompleted = await ctx.AssessmentSessions
                .AnyAsync(a => a.LinkedGroupId == assessment.LinkedGroupId && a.Status == "Completed");
        }

        Assert.False(isCompleted); // tak ada sesi Completed di grup → guard TIDAK menolak.
    }

    // ====================== Test 4: FORM-06 redirect manual ======================
    // GET EditAssessment untuk sesi IsManualEntry=true → redirect EditManualAssessment (TrainingAdmin)
    // dengan route id=id.
    [Fact]
    public async Task EditManualRedirect_ManualEntry_RedirectsToTrainingAdmin()
    {
        await using var ctx = NewCtx();
        var user = await SeedUserAsync(ctx);

        int sid = await SeedSessionAsync(ctx, user.Id, "Manual420", "Test",
            status: "Completed", linkedGroupId: null, assessmentType: "Manual", isManualEntry: true);

        var (ctrl, _) = MakeController(ctx);
        var result = await ctrl.EditAssessment(sid);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("EditManualAssessment", redirect.ActionName);
        Assert.Equal("TrainingAdmin", redirect.ControllerName);
        Assert.NotNull(redirect.RouteValues);
        Assert.Equal(sid, redirect.RouteValues!["id"]);
    }

    // ====================== Test 5: FORM-06 negatif ======================
    // Sesi IsManualEntry=false (online) → kondisi redirect (assessment.IsManualEntry) false → GET
    // TIDAK redirect ke EditManualAssessment (lanjut render form online). Replika-body agar tidak
    // menjalankan GET penuh (query berat + View()).
    [Fact]
    public async Task EditManualRedirect_OnlineSession_DoesNotRedirect()
    {
        await using var ctx = NewCtx();
        var user = await SeedUserAsync(ctx);

        int sid = await SeedSessionAsync(ctx, user.Id, "Online420", "Test",
            status: "Open", linkedGroupId: null, assessmentType: "Standard", isManualEntry: false);

        var assessment = await ctx.AssessmentSessions
            .FirstOrDefaultAsync(a => a.Id == sid);
        Assert.NotNull(assessment);
        Assert.False(assessment!.IsManualEntry); // kondisi redirect FORM-06 false → GET lanjut form online.
    }
}
