// Phase 346 Plan 06 (REC-06 + REC-07): WorkerDataService.GetWorkersInSection searchScope
// (Nama/Training/Keduanya/Null) + GetUnifiedRecords include-PendingGrading.
// Strategy: InMemory DB (Guid per test) + UserManager/NotificationService/Logger null-substitute —
// GetWorkersInSection/GetUnifiedRecords query-path hanya deref _context (verified WorkerDataService.cs).
// Pitfall: InMemory case-sensitive; filter pakai .ToLower() di kedua sisi. Status pending pakai konstanta.
using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class WorkerDataServiceSearchTests
{
    private static WorkerDataService MakeService(out ApplicationDbContext ctx)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        ctx = new ApplicationDbContext(options);
#pragma warning disable CS8625 // null-substitute: query-path tidak deref _userManager/_notificationService/_logger
        return new WorkerDataService(ctx, null!, null!, null!);
#pragma warning restore CS8625
    }

    private static ApplicationUser User(string id, string name, string section, string? nip = null) =>
        new ApplicationUser
        {
            Id = id, FullName = name, Section = section, Unit = "U1", NIP = nip,
            IsActive = true, UserName = id, Email = id + "@test.local"
        };

    private static TrainingRecord Training(int id, string userId, string judul) =>
        new TrainingRecord { Id = id, UserId = userId, Judul = judul, Tanggal = new DateTime(2026, 1, 1), Status = "Valid" };

    private static AssessmentSession Session(int id, string userId, string status, bool? isPassed) =>
        new AssessmentSession
        {
            Id = id, UserId = userId, Status = status, IsPassed = isPassed,
            Title = "Asm " + id, Schedule = new DateTime(2026, 1, 1), Score = 0, GenerateCertificate = false
        };

    // ── REC-06 searchScope ─────────────────────────────────────────────────────

    [Fact]
    public async Task Scope_Nama_FiltersByName()
    {
        var svc = MakeService(out var ctx);
        ctx.Users.AddRange(User("u1", "Budi Santoso", "A"), User("u2", "Andi Wijaya", "A"));
        await ctx.SaveChangesAsync();
        var result = await svc.GetWorkersInSection("A", search: "budi", searchScope: "Nama");
        Assert.Single(result);
        Assert.Equal("u1", result[0].WorkerId);
    }

    [Fact]
    public async Task Scope_Training_FiltersByJudul()
    {
        var svc = MakeService(out var ctx);
        ctx.Users.AddRange(User("u1", "Budi", "A"), User("u2", "Andi", "A"));
        ctx.TrainingRecords.Add(Training(1, "u1", "K3 Safety Awareness"));
        await ctx.SaveChangesAsync();
        var result = await svc.GetWorkersInSection("A", search: "k3", searchScope: "Training");
        Assert.Single(result);
        Assert.Equal("u1", result[0].WorkerId);
    }

    [Fact]
    public async Task Scope_Keduanya_Union_NameOrTraining()
    {
        var svc = MakeService(out var ctx);
        // u1 cocok via nama, u2 cocok via judul training, u3 noise (tidak cocok)
        ctx.Users.AddRange(User("u1", "K3man", "A"), User("u2", "Other", "A"), User("u3", "Noise", "A"));
        ctx.TrainingRecords.Add(Training(1, "u2", "K3 Safety"));
        await ctx.SaveChangesAsync();
        var result = await svc.GetWorkersInSection("A", search: "k3", searchScope: "Keduanya");
        var ids = result.Select(r => r.WorkerId).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "u1", "u2" }, ids);
    }

    [Fact]
    public async Task Scope_Null_NoFilter_BackwardCompat()
    {
        var svc = MakeService(out var ctx);
        ctx.Users.AddRange(User("u1", "Budi", "A"), User("u2", "Andi", "A"));
        await ctx.SaveChangesAsync();
        var result = await svc.GetWorkersInSection("A"); // tanpa search/searchScope (caller lama)
        Assert.Equal(2, result.Count);
    }

    // H1 regresi: caller lama (ManageAssessmentTab_Training) kirim search TANPA searchScope.
    // scope null harus di-treat "Nama" → search ke-apply, bukan ke-drop diam-diam.
    [Fact]
    public async Task Scope_Null_WithSearch_FiltersByName_H1()
    {
        var svc = MakeService(out var ctx);
        ctx.Users.AddRange(User("u1", "Budi Santoso", "A"), User("u2", "Andi Wijaya", "A"));
        await ctx.SaveChangesAsync();
        var result = await svc.GetWorkersInSection("A", search: "budi"); // searchScope null (caller lama)
        Assert.Single(result);
        Assert.Equal("u1", result[0].WorkerId);
    }

    // ── REC-07 include PendingGrading ──────────────────────────────────────────

    [Fact]
    public async Task GetUnifiedRecords_IncludesPendingGrading()
    {
        var svc = MakeService(out var ctx);
        ctx.Users.Add(User("u1", "Budi", "A"));
        ctx.AssessmentSessions.AddRange(
            Session(1, "u1", "Completed", true),
            Session(2, "u1", AssessmentConstants.AssessmentStatus.PendingGrading, null));
        await ctx.SaveChangesAsync();
        var result = await svc.GetUnifiedRecords("u1");
        Assert.Equal(2, result.Count);
        // Sesi pending (IsPassed null) -> label "Menunggu Penilaian" via switch Phase 345
        Assert.Contains(result, r => r.Status == AssessmentConstants.AssessmentStatus.PendingGrading);
    }

    [Fact]
    public async Task GetUnifiedRecords_ExcludesOtherStatus()
    {
        var svc = MakeService(out var ctx);
        ctx.Users.Add(User("u1", "Budi", "A"));
        ctx.AssessmentSessions.AddRange(
            Session(1, "u1", "Completed", true),
            Session(2, "u1", "InProgress", null)); // bukan Completed/PendingGrading -> exclude
        await ctx.SaveChangesAsync();
        var result = await svc.GetUnifiedRecords("u1");
        Assert.Single(result.Where(r => r.RecordType == "Assessment Online"));
    }

    // ── SF-01 / SF-06: assessment-title search (Phase 350) ─────────────────────

    [Fact]
    public async Task Scope_Training_FiltersByAssessmentTitle()
    {
        var svc = MakeService(out var ctx);
        ctx.Users.AddRange(User("u1", "Budi", "A"), User("u2", "Andi", "A"));
        var s = Session(1, "u1", "Completed", true); s.Title = "OJT v14.2 Migas";
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        var result = await svc.GetWorkersInSection("A", search: "ojt v14.2", searchScope: "Training");
        Assert.Single(result);
        Assert.Equal("u1", result[0].WorkerId);
    }

    [Fact]
    public async Task Scope_Keduanya_Union_IncludesAssessment()
    {
        var svc = MakeService(out var ctx);
        ctx.Users.AddRange(User("u1", "Budi", "A"), User("u2", "Andi", "A"));
        var s = Session(1, "u1", "Completed", true); s.Title = "OJT v14.2 Migas";
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        var ids = (await svc.GetWorkersInSection("A", search: "ojt v14.2", searchScope: "Keduanya"))
                  .Select(w => w.WorkerId).ToList();
        Assert.Contains("u1", ids);
    }

    [Fact]
    public async Task Search_DoesNotMutate_BadgeCounts_D07()
    {
        var svc = MakeService(out var ctx);
        ctx.Users.Add(User("u1", "Budi", "A"));
        var s1 = Session(1, "u1", "Completed", true); s1.Title = "OJT v14.2";
        var s2 = Session(2, "u1", "Completed", true); s2.Title = "Lain";
        ctx.AssessmentSessions.AddRange(s1, s2);
        ctx.TrainingRecords.Add(Training(1, "u1", "Training X"));
        await ctx.SaveChangesAsync();
        var matched = await svc.GetWorkersInSection("A", search: "ojt", searchScope: "Keduanya");
        Assert.Single(matched);
        Assert.Equal(2, matched[0].CompletedAssessments); // both passed, NOT 1 (badge unaffected by search)
        Assert.Equal(1, matched[0].TotalTrainings);
    }

    [Fact]
    public async Task Keduanya_AssessmentTitle_ReturnsWorker_ForExport()
    {
        var svc = MakeService(out var ctx);
        ctx.Users.AddRange(User("u1", "Budi", "A"), User("u2", "Andi", "A"));
        var s = Session(1, "u1", "Completed", true); s.Title = "OJT v14.2";
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        var ids = (await svc.GetWorkersInSection("A", search: "ojt v14.2", searchScope: "Keduanya"))
                  .Select(w => w.WorkerId).ToList();
        Assert.Contains("u1", ids);
    }
}
