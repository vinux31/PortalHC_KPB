using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 360 (PBYP-02/04/05) — integration test §5.1 ExecuteInstantBypassAsync di real SQL Server
/// (reuse ProtonCompletionFixture disposable HcPortalDB_Test_&lt;guid&gt;; MigrateAsync membuktikan
/// migration#2 PendingProtonBypass + ProtonTrackAssignments.Origin apply).
/// Tiap fact pakai coacheeId unik — DB shared antar-fact dalam fixture.
/// </summary>
[Trait("Category", "Integration")]
public class ProtonBypassServiceTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public ProtonBypassServiceTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public List<(string UserId, string Type)> Sent { get; } = new();
        public Task<bool> SendAsync(string userId, string type, string title, string message, string? actionUrl = null)
        { Sent.Add((userId, type)); return Task.FromResult(true); }
        public Task<List<UserNotification>> GetAsync(string userId, int count = 50)
            => Task.FromResult(new List<UserNotification>());
        public Task<bool> MarkAsReadAsync(int notificationId, string userId) => Task.FromResult(true);
        public Task<int> MarkAllAsReadAsync(string userId) => Task.FromResult(0);
        public Task<int> GetUnreadCountAsync(string userId) => Task.FromResult(0);
        public Task<bool> SendByTemplateAsync(string userId, string type, Dictionary<string, object>? context = null)
        { Sent.Add((userId, type)); return Task.FromResult(true); }
        public Task<bool> DeleteAsync(int notificationId, string userId) => Task.FromResult(true);
    }

    private static ProtonBypassService NewBypassSvc(ApplicationDbContext ctx, FakeNotificationService? notif = null)
        => new(ctx,
               new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance),
               notif ?? new FakeNotificationService(),
               new AuditLogService(ctx),
               NullLogger<ProtonBypassService>.Instance);

    private static async Task<int> TrackIdAsync(ApplicationDbContext ctx, string trackType, string tahunKe)
        => (await ctx.ProtonTracks.FirstAsync(t => t.TrackType == trackType && t.TahunKe == tahunKe)).Id;

    private static async Task<ProtonTrackAssignment> SeedAssignmentAsync(
        ApplicationDbContext ctx, string coacheeId, int trackId, bool active = true, string? origin = null)
    {
        var a = new ProtonTrackAssignment { CoacheeId = coacheeId, AssignedById = "hc", ProtonTrackId = trackId, IsActive = active, Origin = origin };
        ctx.ProtonTrackAssignments.Add(a);
        await ctx.SaveChangesAsync();
        return a;
    }

    /// <summary>Seed chain Kompetensi(unit)→Sub→N Deliverable untuk track; return deliverable ids.</summary>
    private static async Task<List<int>> SeedDeliverablesAsync(ApplicationDbContext ctx, int trackId, string unit, int count)
    {
        var komp = new ProtonKompetensi { Bagian = "Bagian-T", Unit = unit, NamaKompetensi = $"K-{Guid.NewGuid():N}", Urutan = 1, ProtonTrackId = trackId };
        ctx.ProtonKompetensiList.Add(komp);
        await ctx.SaveChangesAsync();
        var sub = new ProtonSubKompetensi { ProtonKompetensiId = komp.Id, NamaSubKompetensi = "Sub", Urutan = 1 };
        ctx.ProtonSubKompetensiList.Add(sub);
        await ctx.SaveChangesAsync();
        var dels = Enumerable.Range(1, count)
            .Select(i => new ProtonDeliverable { ProtonSubKompetensiId = sub.Id, NamaDeliverable = $"D{i}", Urutan = i })
            .ToList();
        ctx.ProtonDeliverableList.AddRange(dels);
        await ctx.SaveChangesAsync();
        return dels.Select(d => d.Id).ToList();
    }

    private static async Task SeedProgressAsync(ApplicationDbContext ctx, string coacheeId, int assignmentId, IEnumerable<int> deliverableIds, string status)
    {
        foreach (var dId in deliverableIds)
        {
            ctx.ProtonDeliverableProgresses.Add(new ProtonDeliverableProgress
            {
                CoacheeId = coacheeId,
                ProtonDeliverableId = dId,
                ProtonTrackAssignmentId = assignmentId,
                Status = status,
                CreatedAt = DateTime.UtcNow
            });
        }
        await ctx.SaveChangesAsync();
    }

    private static BypassRequest Req(string coachee, int sourceTrack, int targetTrack, string mode,
        string targetUnit = "U-360", string? targetCoachId = null)
        => new(coachee, sourceTrack, targetTrack, targetUnit, targetCoachId, "Alasan bypass test", mode, null, "hc-init");

    [Fact]
    public async Task CL_BSatuA_TerbitPenandaSource_SebelumDeactivate()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"clba-{Guid.NewGuid():N}";
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        var source = await SeedAssignmentAsync(ctx, coachee, t1);
        var dels = await SeedDeliverablesAsync(ctx, t1, $"U-{coachee[..8]}", 2);
        await SeedProgressAsync(ctx, coachee, source.Id, dels, "Submitted");

        var result = await NewBypassSvc(ctx).ExecuteInstantBypassAsync(
            Req(coachee, t1, t2, "CL-B(a)", targetUnit: $"U-{coachee[..8]}"));

        Assert.True(result.Success, result.Message);
        // Pitfall 1: penanda source TERBIT (EnsureAsync sebelum deactivate) dengan Origin="Bypass".
        var penanda = await ctx.ProtonFinalAssessments.SingleAsync(fa => fa.ProtonTrackAssignmentId == source.Id);
        Assert.Equal("Bypass", penanda.Origin);
        // Force-approve CL-B(a): semua progress source Approved.
        var statuses = await ctx.ProtonDeliverableProgresses
            .Where(p => p.ProtonTrackAssignmentId == source.Id).Select(p => p.Status).ToListAsync();
        Assert.All(statuses, s => Assert.Equal("Approved", s));
        // Source deactivated, target aktif ber-Origin Bypass.
        await ctx.Entry(source).ReloadAsync();
        Assert.False(source.IsActive);
        var target = await ctx.ProtonTrackAssignments.SingleAsync(a => a.CoacheeId == coachee && a.IsActive);
        Assert.Equal(t2, target.ProtonTrackId);
        Assert.Equal("Bypass", target.Origin);
    }

    [Fact]
    public async Task CL_BSatuA_ForceApprove_SkipYangSudahApproved_ProvenanceUtuh()
    {
        // WR-03 (review 360): force-approve D-13 hanya menyentuh progress BELUM Approved —
        // approval sah coach (ApprovedById/ApprovedAt) tidak ditimpa, tanpa history bising.
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"wr03a-{Guid.NewGuid():N}";
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        var source = await SeedAssignmentAsync(ctx, coachee, t1);
        var dels = await SeedDeliverablesAsync(ctx, t1, $"U-{coachee[..8]}", 2);
        var approvedAt = DateTime.UtcNow.AddDays(-30);
        var pApproved = new ProtonDeliverableProgress
        {
            CoacheeId = coachee, ProtonDeliverableId = dels[0], ProtonTrackAssignmentId = source.Id,
            Status = "Approved", ApprovedById = "coach-asli", ApprovedAt = approvedAt, CreatedAt = DateTime.UtcNow
        };
        var pSubmitted = new ProtonDeliverableProgress
        {
            CoacheeId = coachee, ProtonDeliverableId = dels[1], ProtonTrackAssignmentId = source.Id,
            Status = "Submitted", CreatedAt = DateTime.UtcNow
        };
        ctx.ProtonDeliverableProgresses.AddRange(pApproved, pSubmitted);
        await ctx.SaveChangesAsync();

        var result = await NewBypassSvc(ctx).ExecuteInstantBypassAsync(
            Req(coachee, t1, t2, "CL-B(a)", targetUnit: $"U-{coachee[..8]}"));

        Assert.True(result.Success, result.Message);
        // Yang sudah Approved sah: provenance coach UTUH + TANPA history Bypassed-AutoApprove.
        var freshApproved = await ctx.ProtonDeliverableProgresses.AsNoTracking().SingleAsync(p => p.Id == pApproved.Id);
        Assert.Equal("Approved", freshApproved.Status);
        Assert.Equal("coach-asli", freshApproved.ApprovedById);
        Assert.Equal(approvedAt, freshApproved.ApprovedAt!.Value, TimeSpan.FromSeconds(1));
        Assert.False(await ctx.DeliverableStatusHistories.AnyAsync(
            h => h.ProtonDeliverableProgressId == pApproved.Id && h.StatusType == "Bypassed-AutoApprove"));
        // Yang BELUM Approved: tetap di-force-approve HC + history D-13.
        var freshSubmitted = await ctx.ProtonDeliverableProgresses.AsNoTracking().SingleAsync(p => p.Id == pSubmitted.Id);
        Assert.Equal("Approved", freshSubmitted.Status);
        Assert.Equal("hc-init", freshSubmitted.ApprovedById);
        Assert.True(await ctx.DeliverableStatusHistories.AnyAsync(
            h => h.ProtonDeliverableProgressId == pSubmitted.Id && h.StatusType == "Bypassed-AutoApprove"));
    }

    [Fact]
    public async Task CL_BSatuB_ForceApprove_SkipYangSudahApproved()
    {
        // WR-03 lokasi kedua: jalur pending CL-B(b) §5.2 — progress Approved sah tak disentuh.
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"wr03b-{Guid.NewGuid():N}";
        await SeedUserAsync(ctx, coachee);
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        var source = await SeedAssignmentAsync(ctx, coachee, t1);
        var dels = await SeedDeliverablesAsync(ctx, t1, $"U-{coachee[..8]}", 2);
        var pApproved = new ProtonDeliverableProgress
        {
            CoacheeId = coachee, ProtonDeliverableId = dels[0], ProtonTrackAssignmentId = source.Id,
            Status = "Approved", ApprovedById = "coach-asli", ApprovedAt = DateTime.UtcNow.AddDays(-7),
            CreatedAt = DateTime.UtcNow
        };
        var pSubmitted = new ProtonDeliverableProgress
        {
            CoacheeId = coachee, ProtonDeliverableId = dels[1], ProtonTrackAssignmentId = source.Id,
            Status = "Submitted", CreatedAt = DateTime.UtcNow
        };
        ctx.ProtonDeliverableProgresses.AddRange(pApproved, pSubmitted);
        await ctx.SaveChangesAsync();

        var result = await NewBypassSvc(ctx).BypassSaveAsync(
            Req(coachee, t1, t2, "CL-B(b)", targetUnit: $"U-{coachee[..8]}"));

        Assert.True(result.Success, result.Message);
        var freshApproved = await ctx.ProtonDeliverableProgresses.AsNoTracking().SingleAsync(p => p.Id == pApproved.Id);
        Assert.Equal("coach-asli", freshApproved.ApprovedById); // provenance coach utuh
        Assert.False(await ctx.DeliverableStatusHistories.AnyAsync(
            h => h.ProtonDeliverableProgressId == pApproved.Id && h.StatusType == "Bypassed-AutoApprove"));
        var freshSubmitted = await ctx.ProtonDeliverableProgresses.AsNoTracking().SingleAsync(p => p.Id == pSubmitted.Id);
        Assert.Equal("Approved", freshSubmitted.Status);
        Assert.Equal("hc-init", freshSubmitted.ApprovedById);
    }

    [Fact]
    public async Task CL_A_PindahInstan()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"cla-{Guid.NewGuid():N}";
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        var source = await SeedAssignmentAsync(ctx, coachee, t1);
        var dels = await SeedDeliverablesAsync(ctx, t1, $"U-{coachee[..8]}", 2);
        await SeedProgressAsync(ctx, coachee, source.Id, dels, "Approved"); // SourceComplete
        ctx.ProtonFinalAssessments.Add(new ProtonFinalAssessment // SourceHasFinal (B-03)
        {
            CoacheeId = coachee, CreatedById = "hc", ProtonTrackAssignmentId = source.Id,
            Status = "Completed", Origin = "Interview", CompletedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var result = await NewBypassSvc(ctx).ExecuteInstantBypassAsync(
            Req(coachee, t1, t2, "CL-A", targetUnit: $"U-{coachee[..8]}"));

        Assert.True(result.Success, result.Message);
        await ctx.Entry(source).ReloadAsync();
        Assert.False(source.IsActive);
        var target = await ctx.ProtonTrackAssignments.SingleAsync(a => a.CoacheeId == coachee && a.IsActive);
        Assert.Equal(t2, target.ProtonTrackId);
        Assert.Equal("Bypass", target.Origin);
        // CL-A no-op tutup tahun asal: penanda tetap 1 (yang lama), tidak dobel.
        Assert.Equal(1, await ctx.ProtonFinalAssessments.CountAsync(fa => fa.ProtonTrackAssignmentId == source.Id));
    }

    [Fact]
    public async Task CL_C_Tinggalkan_TanpaPenandaBaru()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"clc-{Guid.NewGuid():N}";
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        var t3 = await TrackIdAsync(ctx, "Operator", "Tahun 3");
        var source = await SeedAssignmentAsync(ctx, coachee, t2);

        var result = await NewBypassSvc(ctx).ExecuteInstantBypassAsync(Req(coachee, t2, t3, "CL-C"));

        Assert.True(result.Success, result.Message);
        await ctx.Entry(source).ReloadAsync();
        Assert.False(source.IsActive);
        Assert.NotNull(source.DeactivatedAt);
        var target = await ctx.ProtonTrackAssignments.SingleAsync(a => a.CoacheeId == coachee && a.IsActive);
        Assert.Equal(t3, target.ProtonTrackId);
        // CL-C: TANPA penanda baru untuk source (tinggalkan tanpa nilai).
        Assert.Equal(0, await ctx.ProtonFinalAssessments.CountAsync(fa => fa.ProtonTrackAssignmentId == source.Id));
    }

    [Fact]
    public async Task Bootstrap_PakaiUnitForm_BukanMapping()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"unitform-{Guid.NewGuid():N}";
        var t1 = await TrackIdAsync(ctx, "Panelman", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Panelman", "Tahun 2");
        var source = await SeedAssignmentAsync(ctx, coachee, t1);
        var unitForm = $"U-FORM-{coachee[..8]}";
        var unitLama = $"U-LAMA-{coachee[..8]}";
        var delsForm = await SeedDeliverablesAsync(ctx, t2, unitForm, 2);
        var delsLama = await SeedDeliverablesAsync(ctx, t2, unitLama, 2);
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping
        { CoacheeId = coachee, CoachId = "coach-x", AssignmentUnit = unitLama, IsActive = true, StartDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var result = await NewBypassSvc(ctx).ExecuteInstantBypassAsync(
            Req(coachee, t1, t2, "CL-C", targetUnit: unitForm));

        Assert.True(result.Success, result.Message);
        // PBYP-05: progress dibuat untuk deliverable unit FORM, BUKAN unit mapping lama.
        var createdIds = await ctx.ProtonDeliverableProgresses
            .Where(p => p.CoacheeId == coachee).Select(p => p.ProtonDeliverableId).ToListAsync();
        Assert.Equal(delsForm.OrderBy(x => x), createdIds.OrderBy(x => x));
        Assert.Empty(createdIds.Intersect(delsLama));
    }

    [Fact]
    public async Task Coach_DeactivateLamaCreateBaru_E15()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"coachswap-{Guid.NewGuid():N}";
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        await SeedAssignmentAsync(ctx, coachee, t1);
        var lama = new CoachCoacheeMapping
        {
            CoacheeId = coachee, CoachId = "coach-lama", AssignmentUnit = "U-360",
            AssignmentSection = "Sec-1", IsActive = true, StartDate = DateTime.UtcNow.AddYears(-1)
        };
        ctx.CoachCoacheeMappings.Add(lama);
        await ctx.SaveChangesAsync();

        var result = await NewBypassSvc(ctx).ExecuteInstantBypassAsync(
            Req(coachee, t1, t2, "CL-C", targetCoachId: "coach-baru"));

        Assert.True(result.Success, result.Message);
        await ctx.Entry(lama).ReloadAsync();
        Assert.False(lama.IsActive); // E15: deactivate lama dulu
        var baru = await ctx.CoachCoacheeMappings.SingleAsync(m => m.CoacheeId == coachee && m.IsActive);
        Assert.Equal("coach-baru", baru.CoachId);
        Assert.Equal("U-360", baru.AssignmentUnit);
        Assert.NotEqual(default, baru.StartDate);           // W-13: StartDate wajib di-set
        Assert.Equal("Sec-1", baru.AssignmentSection);      // I-04: Section warisi mapping lama
    }

    [Fact]
    public async Task KeepCoach_GantiUnit_UpdateMappingUnit_GateKonsisten()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"d16b-{Guid.NewGuid():N}";
        var t1 = await TrackIdAsync(ctx, "Panelman", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Panelman", "Tahun 2");
        await SeedAssignmentAsync(ctx, coachee, t1);
        var unitForm = $"U-D16B-{coachee[..6]}";
        await SeedDeliverablesAsync(ctx, t2, unitForm, 2);
        var mapping = new CoachCoacheeMapping
        { CoacheeId = coachee, CoachId = "coach-keep", AssignmentUnit = "U-LAMA-D16B", IsActive = true, StartDate = DateTime.UtcNow };
        ctx.CoachCoacheeMappings.Add(mapping);
        await ctx.SaveChangesAsync();

        // TargetCoachId NULL (keep coach) + TargetUnit BEDA → D-16b update unit mapping, tanpa recreate.
        var result = await NewBypassSvc(ctx).ExecuteInstantBypassAsync(
            Req(coachee, t1, t2, "CL-C", targetUnit: unitForm, targetCoachId: null));

        Assert.True(result.Success, result.Message);
        var aktif = await ctx.CoachCoacheeMappings.Where(m => m.CoacheeId == coachee && m.IsActive).ToListAsync();
        var satu = Assert.Single(aktif);                 // tidak ada deactivate/recreate
        Assert.Equal(mapping.Id, satu.Id);               // mapping yang SAMA
        Assert.Equal("coach-keep", satu.CoachId);        // coach dipertahankan (D-16)
        Assert.Equal(unitForm, satu.AssignmentUnit);     // D-16b: unit di-update ke unit FORM
        // Gate konsisten: progress yang di-bootstrap = deliverable unit yang kini ada di mapping.
        var progressDelIds = await ctx.ProtonDeliverableProgresses
            .Where(p => p.CoacheeId == coachee).Select(p => p.ProtonDeliverableId).ToListAsync();
        var unitDelIds = await ctx.ProtonDeliverableList
            .Where(d => d.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrackId == t2
                     && d.ProtonSubKompetensi!.ProtonKompetensi!.Unit!.Trim() == unitForm)
            .Select(d => d.Id).ToListAsync();
        Assert.Equal(unitDelIds.OrderBy(x => x), progressDelIds.OrderBy(x => x));
    }

    [Fact]
    public async Task KeepCoach_TargetUnitKosong_MappingTidakDikorupsi()
    {
        // WR-02 (review 360): guard defensif D-16b — TargetUnit kosong TIDAK boleh menimpa
        // AssignmentUnit mapping coach aktif jadi string kosong (merusak resolve unit gate 100%).
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"wr02-{Guid.NewGuid():N}";
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        await SeedAssignmentAsync(ctx, coachee, t1);
        var unitLama = $"U-WR02-{coachee[..6]}";
        var mapping = new CoachCoacheeMapping
        { CoacheeId = coachee, CoachId = "coach-keep", AssignmentUnit = unitLama, IsActive = true, StartDate = DateTime.UtcNow };
        ctx.CoachCoacheeMappings.Add(mapping);
        await ctx.SaveChangesAsync();

        var result = await NewBypassSvc(ctx).ExecuteInstantBypassAsync(
            Req(coachee, t1, t2, "CL-C", targetUnit: "", targetCoachId: null));

        Assert.True(result.Success, result.Message); // bootstrap di-skip (warning saja), bypass jalan
        await ctx.Entry(mapping).ReloadAsync();
        Assert.True(mapping.IsActive);
        Assert.Equal(unitLama, mapping.AssignmentUnit); // unit mapping TIDAK tertimpa kosong
    }

    [Fact]
    public async Task CL_C_TurunKeTrackPernahDijalani_CountProgressTetapN()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"b06-{Guid.NewGuid():N}";
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        var unitX = $"U-B06-{coachee[..6]}";
        var delsT1 = await SeedDeliverablesAsync(ctx, t1, unitX, 3); // N = 3
        // Histori: Tahun 1 pernah dijalani (assignment LAMA inactive) dengan N progress Approved.
        var asgLama = await SeedAssignmentAsync(ctx, coachee, t1, active: false);
        await SeedProgressAsync(ctx, coachee, asgLama.Id, delsT1, "Approved");
        // Sekarang aktif di Tahun 2.
        await SeedAssignmentAsync(ctx, coachee, t2, active: true);

        // Bypass CL-C turun Tahun2 → Tahun1, unit sama X.
        var result = await NewBypassSvc(ctx).ExecuteInstantBypassAsync(
            Req(coachee, t2, t1, "CL-C", targetUnit: unitX));

        Assert.True(result.Success, result.Message);
        // B-06: count progress per (coachee, deliverable unit X) TETAP N — bukan 2N.
        var statuses = await ctx.ProtonDeliverableProgresses
            .Where(p => p.CoacheeId == coachee && delsT1.Contains(p.ProtonDeliverableId))
            .Select(p => p.Status).ToListAsync();
        Assert.Equal(delsT1.Count, statuses.Count);
        // Worker TIDAK permanen tak-eligible: count match + semua Approved → gate bisa true.
        Assert.True(CoacheeEligibilityCalculator.IsEligiblePerUnit(statuses, delsT1.Count));
    }

    // ===== Plan 04: jalur pending CL-B(b) + lifecycle Menunggu→Siap =====

    /// <summary>
    /// Seed ApplicationUser untuk coachee — bare AssessmentSession.UserId punya FK required
    /// ke AspNetUsers (beda dari ProtonTrackAssignment.CoacheeId yang tanpa FK).
    /// </summary>
    private static async Task SeedUserAsync(ApplicationDbContext ctx, string userId)
    {
        ctx.Users.Add(new ApplicationUser { Id = userId, UserName = userId, FullName = $"Worker {userId[..8]}" });
        await ctx.SaveChangesAsync();
    }

    /// <summary>Seed worker (user + 1 assignment aktif Tahun 1) + buat pending CL-B(b) via BypassSaveAsync.</summary>
    private async Task<PendingProtonBypass> SeedPendingViaSaveAsync(
        ApplicationDbContext ctx, string coachee, FakeNotificationService notif)
    {
        await SeedUserAsync(ctx, coachee);
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        var source = await SeedAssignmentAsync(ctx, coachee, t1);
        var dels = await SeedDeliverablesAsync(ctx, t1, $"U-{coachee[..8]}", 2);
        await SeedProgressAsync(ctx, coachee, source.Id, dels, "Submitted");
        var result = await NewBypassSvc(ctx, notif).BypassSaveAsync(
            Req(coachee, t1, t2, "CL-B(b)", targetUnit: $"U-{coachee[..8]}"));
        Assert.True(result.Success, result.Message);
        return await ctx.PendingProtonBypasses.SingleAsync(p => p.CoacheeId == coachee);
    }

    [Fact]
    public async Task CL_BSatuB_BuatPending_BareSession_NoPackage()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"clbb-{Guid.NewGuid():N}";
        await SeedUserAsync(ctx, coachee);
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        var source = await SeedAssignmentAsync(ctx, coachee, t1);
        var dels = await SeedDeliverablesAsync(ctx, t1, $"U-{coachee[..8]}", 2);
        await SeedProgressAsync(ctx, coachee, source.Id, dels, "Submitted");

        var result = await NewBypassSvc(ctx).BypassSaveAsync(
            Req(coachee, t1, t2, "CL-B(b)", targetUnit: $"U-{coachee[..8]}"));

        Assert.True(result.Success, result.Message);
        Assert.True(result.ShowAttachPackageReminder); // D-02
        // Pending Menunggu + linked ke session bare.
        var pending = await ctx.PendingProtonBypasses.SingleAsync(p => p.CoacheeId == coachee);
        Assert.Equal("Menunggu", pending.Status);
        Assert.Equal(result.PendingId, pending.Id);
        var session = await ctx.AssessmentSessions.SingleAsync(s => s.Id == pending.LinkedAssessmentSessionId);
        Assert.Equal("Assessment Proton", session.Category);
        Assert.Equal("Upcoming", session.Status);
        Assert.True(session.UserId == coachee);          // B-05/W-06: per-worker key
        Assert.NotNull(session.AssessmentType);          // B-05: NOT NULL di DB nyata
        Assert.Equal(t1, session.ProtonTrackId);         // exam SOURCE-year
        Assert.True(session.GenerateCertificate);
        // D-01: bare — TANPA paket.
        Assert.Equal(0, await ctx.UserPackageAssignments.CountAsync(a => a.AssessmentSessionId == session.Id));
        // Deliverable source force-approved + history D-13.
        var statuses = await ctx.ProtonDeliverableProgresses
            .Where(p => p.ProtonTrackAssignmentId == source.Id).Select(p => p.Status).ToListAsync();
        Assert.All(statuses, s => Assert.Equal("Approved", s));
        Assert.True(await ctx.DeliverableStatusHistories.AnyAsync(h => h.StatusType == "Bypassed-AutoApprove"
            && ctx.ProtonDeliverableProgresses.Any(p => p.Id == h.ProtonDeliverableProgressId && p.ProtonTrackAssignmentId == source.Id)));
        // Worker BELUM pindah: assignment aktif tetap source.
        var aktif = await ctx.ProtonTrackAssignments.SingleAsync(a => a.CoacheeId == coachee && a.IsActive);
        Assert.Equal(t1, aktif.ProtonTrackId);
        // TANPA penanda — terbit nanti oleh GradingService (Origin="Exam").
        Assert.Equal(0, await ctx.ProtonFinalAssessments.CountAsync(fa => fa.ProtonTrackAssignmentId == source.Id));
    }

    [Fact]
    public async Task MarkPendingReady_FlipSiap_KirimNotif()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"flip-{Guid.NewGuid():N}";
        var notif = new FakeNotificationService();
        var pending = await SeedPendingViaSaveAsync(ctx, coachee, notif);

        var flipped = await NewBypassSvc(ctx, notif).MarkPendingReadyIfAnyAsync(pending.LinkedAssessmentSessionId);

        Assert.True(flipped);
        var fresh = await ctx.PendingProtonBypasses.AsNoTracking().SingleAsync(p => p.Id == pending.Id);
        Assert.Equal("Siap", fresh.Status);
        // Notif ke InitiatedById (HC) — worker TIDAK dapat notif ini (T-360-13).
        Assert.Contains(notif.Sent, s => s.UserId == "hc-init" && s.Type == "PROTON_BYPASS_READY");
        Assert.DoesNotContain(notif.Sent, s => s.UserId == coachee && s.Type == "PROTON_BYPASS_READY");
    }

    [Fact]
    public async Task Revert_PassFail_PendingBalikMenunggu()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"revert-{Guid.NewGuid():N}";
        var notif = new FakeNotificationService();
        var pending = await SeedPendingViaSaveAsync(ctx, coachee, notif);
        var svc = NewBypassSvc(ctx, notif);
        Assert.True(await svc.MarkPendingReadyIfAnyAsync(pending.LinkedAssessmentSessionId)); // → Siap

        await svc.RevertPendingToMenungguAsync(pending.LinkedAssessmentSessionId); // D-15

        var fresh = await ctx.PendingProtonBypasses.AsNoTracking().SingleAsync(p => p.Id == pending.Id);
        Assert.Equal("Menunggu", fresh.Status);
        // Worker belum pindah (Opsi B) — assignment aktif tak berubah (tetap source Tahun 1).
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var aktif = await ctx.ProtonTrackAssignments.SingleAsync(a => a.CoacheeId == coachee && a.IsActive);
        Assert.Equal(t1, aktif.ProtonTrackId);
    }

    [Fact]
    public async Task D10_DobelPending_Tolak()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"d10-{Guid.NewGuid():N}";
        var notif = new FakeNotificationService();
        await SeedPendingViaSaveAsync(ctx, coachee, notif); // pending Menunggu aktif

        // Bypass kedua (mode apapun — di sini CL-C) → ditolak D-10.
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        var result = await NewBypassSvc(ctx, notif).BypassSaveAsync(Req(coachee, t1, t2, "CL-C"));

        Assert.False(result.Success);
        Assert.Contains("rencana bypass aktif", result.Message);
        Assert.Equal(1, await ctx.PendingProtonBypasses.CountAsync(p => p.CoacheeId == coachee)); // tetap 1
    }

    [Fact]
    public async Task D10_RaceDobelPending_UniqueIndexTolakRequestKedua()
    {
        // WR-01 (review 360): simulasi race dobel-klik — dua request konkuren sama-sama lolos
        // cek D-10 (AnyAsync di BypassSaveAsync, DI LUAR tx) lalu sama-sama insert. Request kedua
        // WAJIB ditolak filtered unique index IX_PendingProtonBypasses_CoacheeId_ActiveUnique.
        await using var ctx1 = new ApplicationDbContext(_fixture.Options);
        await using var ctx2 = new ApplicationDbContext(_fixture.Options);
        var coachee = $"race-{Guid.NewGuid():N}";
        await SeedUserAsync(ctx1, coachee);
        var t1 = await TrackIdAsync(ctx1, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx1, "Operator", "Tahun 2");
        var source = await SeedAssignmentAsync(ctx1, coachee, t1);
        var dels = await SeedDeliverablesAsync(ctx1, t1, $"U-{coachee[..8]}", 2);
        await SeedProgressAsync(ctx1, coachee, source.Id, dels, "Submitted");
        var req = Req(coachee, t1, t2, "CL-B(b)", targetUnit: $"U-{coachee[..8]}");

        // Langsung ExecutePendingBypassAsync (melewati cek D-10 BypassSaveAsync) = kondisi race.
        var first = await NewBypassSvc(ctx1).ExecutePendingBypassAsync(req);
        var second = await NewBypassSvc(ctx2).ExecutePendingBypassAsync(req);

        Assert.True(first.Success, first.Message);
        Assert.False(second.Success);
        Assert.Contains("rencana bypass aktif", second.Message);
        // Tetap 1 pending + 1 bare session — insert kedua rollback all-or-nothing (tanpa yatim).
        await using var verify = new ApplicationDbContext(_fixture.Options);
        Assert.Equal(1, await verify.PendingProtonBypasses.CountAsync(p => p.CoacheeId == coachee));
        Assert.Equal(1, await verify.AssessmentSessions.CountAsync(
            s => s.UserId == coachee && s.Category == "Assessment Proton"));
    }

    [Fact]
    public async Task D10_SetelahDibatalkan_BolehBuatPendingBaru()
    {
        // WR-01 guard filter: unique index hanya mengunci status AKTIF (Menunggu/Siap) —
        // pending Dibatalkan/Selesai TIDAK memblokir rencana bypass baru.
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"reuse-{Guid.NewGuid():N}";
        var notif = new FakeNotificationService();
        var svc = NewBypassSvc(ctx, notif);
        var pending = await SeedPendingViaSaveAsync(ctx, coachee, notif);
        var cancel = await svc.CancelPendingAsync(pending.Id, "hc2", "HC Dua");
        Assert.True(cancel.Success, cancel.Message);

        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        var result = await svc.BypassSaveAsync(Req(coachee, t1, t2, "CL-B(b)", targetUnit: $"U-{coachee[..8]}"));

        Assert.True(result.Success, result.Message);
        Assert.Equal(2, await ctx.PendingProtonBypasses.CountAsync(p => p.CoacheeId == coachee));
        Assert.Equal(1, await ctx.PendingProtonBypasses.CountAsync(
            p => p.CoacheeId == coachee && p.Status == "Menunggu"));
    }

    [Fact]
    public async Task MarkPendingReady_NoTransaction_SafeRepeat()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"repeat-{Guid.NewGuid():N}";
        var notif = new FakeNotificationService();
        var pending = await SeedPendingViaSaveAsync(ctx, coachee, notif);
        var svc = NewBypassSvc(ctx, notif);

        var first = await svc.MarkPendingReadyIfAnyAsync(pending.LinkedAssessmentSessionId);
        var second = await svc.MarkPendingReadyIfAnyAsync(pending.LinkedAssessmentSessionId);

        Assert.True(first);
        Assert.False(second); // rows==0 / sudah Siap — idempotent
        var fresh = await ctx.PendingProtonBypasses.AsNoTracking().SingleAsync(p => p.Id == pending.Id);
        Assert.Equal("Siap", fresh.Status);
        Assert.Equal(1, notif.Sent.Count(s => s.Type == "PROTON_BYPASS_READY")); // notif tidak dobel
    }

    [Fact]
    public async Task E8_DobelAssignment_Tolak()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"e8-{Guid.NewGuid():N}";
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        // B-04: worker punya 2 assignment AKTIF (data rusak/legacy).
        await SeedAssignmentAsync(ctx, coachee, t1, active: true);
        await SeedAssignmentAsync(ctx, coachee, t2, active: true);

        var result = await NewBypassSvc(ctx).BypassSaveAsync(Req(coachee, t1, t2, "CL-B(b)"));

        Assert.False(result.Success);
        Assert.Contains("assignment aktif", result.Message);
        // TIDAK ada pending dibuat.
        Assert.Equal(0, await ctx.PendingProtonBypasses.CountAsync(p => p.CoacheeId == coachee));
    }

    // ===== Plan 05: ConfirmBypassAsync §5.3 + CancelPendingAsync §8.1 =====

    /// <summary>Set linked exam lulus (Completed+IsPassed) + flip pending Siap + terbit penanda Origin="Exam".</summary>
    private static async Task<ProtonTrackAssignment> MakePendingSiapLulusAsync(
        ApplicationDbContext ctx, ProtonBypassService svc, PendingProtonBypass pending)
    {
        var session = await ctx.AssessmentSessions.FindAsync(pending.LinkedAssessmentSessionId);
        session!.IsPassed = true;
        session.Status = "Completed";
        await ctx.SaveChangesAsync();
        Assert.True(await svc.MarkPendingReadyIfAnyAsync(pending.LinkedAssessmentSessionId)); // → Siap
        // Flip via ExecuteUpdateAsync bypass change tracker — reload supaya FindAsync (ctx sama
        // di test; produksi context per-request fresh) lihat Status="Siap".
        await ctx.Entry(pending).ReloadAsync();
        var source = await ctx.ProtonTrackAssignments.SingleAsync(
            a => a.CoacheeId == pending.CoacheeId && a.ProtonTrackId == pending.SourceProtonTrackId && a.IsActive);
        ctx.ProtonFinalAssessments.Add(new ProtonFinalAssessment
        {
            CoacheeId = pending.CoacheeId, CreatedById = "grading", ProtonTrackAssignmentId = source.Id,
            Status = "Completed", Origin = "Exam", CompletedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
        return source;
    }

    [Fact]
    public async Task Confirm_HappyPath_Pindah_Selesai()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"cfm-{Guid.NewGuid():N}";
        var notif = new FakeNotificationService();
        var svc = NewBypassSvc(ctx, notif);
        var pending = await SeedPendingViaSaveAsync(ctx, coachee, notif);
        var t2 = pending.TargetProtonTrackId;
        await SeedDeliverablesAsync(ctx, t2, $"U-{coachee[..8]}", 2); // deliverable target utk bootstrap W-02
        var source = await MakePendingSiapLulusAsync(ctx, svc, pending);

        var result = await svc.ConfirmBypassAsync(pending.Id, "hc2", "HC Dua");

        Assert.True(result.Success, result.Message);
        var freshPending = await ctx.PendingProtonBypasses.AsNoTracking().SingleAsync(p => p.Id == pending.Id);
        Assert.Equal("Selesai", freshPending.Status);
        Assert.NotNull(freshPending.ResolvedAt);
        // Source inactive, target aktif Origin="Bypass".
        var freshSource = await ctx.ProtonTrackAssignments.AsNoTracking().SingleAsync(a => a.Id == source.Id);
        Assert.False(freshSource.IsActive);
        var target = await ctx.ProtonTrackAssignments.AsNoTracking()
            .SingleAsync(a => a.CoacheeId == coachee && a.IsActive);
        Assert.Equal(t2, target.ProtonTrackId);
        Assert.Equal("Bypass", target.Origin);
        // Linked session TIDAK "Dibatalkan" — bukti kelulusan dipertahankan (T-360-17).
        var linked = await ctx.AssessmentSessions.AsNoTracking().SingleAsync(s => s.Id == pending.LinkedAssessmentSessionId);
        Assert.NotEqual("Dibatalkan", linked.Status);
        // W-02: bootstrap target — progress tahun tujuan dibuat.
        Assert.Equal(2, await ctx.ProtonDeliverableProgresses.CountAsync(p => p.ProtonTrackAssignmentId == target.Id));
        // TANPA re-create penanda: penanda source tetap 1 (Origin="Exam").
        Assert.Equal(1, await ctx.ProtonFinalAssessments.CountAsync(fa => fa.ProtonTrackAssignmentId == source.Id));
    }

    [Fact]
    public async Task Confirm_Stale_AssignmentBerubah_Tolak()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"stale-{Guid.NewGuid():N}";
        var notif = new FakeNotificationService();
        var svc = NewBypassSvc(ctx, notif);
        var pending = await SeedPendingViaSaveAsync(ctx, coachee, notif);
        var source = await MakePendingSiapLulusAsync(ctx, svc, pending);
        // D-11: worker dipindah manual — assignment asal sudah TIDAK aktif.
        source.IsActive = false;
        await ctx.SaveChangesAsync();

        var result = await svc.ConfirmBypassAsync(pending.Id, "hc2", "HC Dua");

        Assert.False(result.Success);
        var freshPending = await ctx.PendingProtonBypasses.AsNoTracking().SingleAsync(p => p.Id == pending.Id);
        Assert.Equal("Siap", freshPending.Status); // tak pindah, pending tetap Siap
    }

    [Fact]
    public async Task Confirm_DobelKlik_Atomik_PindahSekali()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"dobel-{Guid.NewGuid():N}";
        var notif = new FakeNotificationService();
        var svc = NewBypassSvc(ctx, notif);
        var pending = await SeedPendingViaSaveAsync(ctx, coachee, notif);
        var t2 = pending.TargetProtonTrackId;
        await SeedDeliverablesAsync(ctx, t2, $"U-{coachee[..8]}", 2);
        await MakePendingSiapLulusAsync(ctx, svc, pending);

        var r1 = await svc.ConfirmBypassAsync(pending.Id, "hc2", "HC Dua");
        var r2 = await svc.ConfirmBypassAsync(pending.Id, "hc3", "HC Tiga");

        // D-12: hanya SATU yang sukses — pindah jalan sekali.
        Assert.True(r1.Success ^ r2.Success, $"r1={r1.Success} r2={r2.Success}");
        Assert.Equal(1, await ctx.ProtonTrackAssignments.AsNoTracking()
            .CountAsync(a => a.CoacheeId == coachee && a.ProtonTrackId == t2)); // target tak dobel
        Assert.Equal(1, await ctx.ProtonTrackAssignments.AsNoTracking()
            .CountAsync(a => a.CoacheeId == coachee && a.IsActive)); // tetap 1 aktif (E8 terjaga)
    }

    [Fact]
    public async Task Cancel_BelumKerjakan_CancelExam()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"cnl1-{Guid.NewGuid():N}";
        var notif = new FakeNotificationService();
        var pending = await SeedPendingViaSaveAsync(ctx, coachee, notif); // Menunggu, session Upcoming

        var result = await NewBypassSvc(ctx, notif).CancelPendingAsync(pending.Id, "hc2", "HC Dua");

        Assert.True(result.Success, result.Message);
        var freshPending = await ctx.PendingProtonBypasses.AsNoTracking().SingleAsync(p => p.Id == pending.Id);
        Assert.Equal("Dibatalkan", freshPending.Status);
        Assert.NotNull(freshPending.ResolvedAt);
        // Sesi exam belum dikerjakan → auto-cancel (PBYP-06).
        var linked = await ctx.AssessmentSessions.AsNoTracking().SingleAsync(s => s.Id == pending.LinkedAssessmentSessionId);
        Assert.Equal("Dibatalkan", linked.Status);
    }

    [Fact]
    public async Task Cancel_SudahLulus_PertahankanHasil()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"cnl2-{Guid.NewGuid():N}";
        var notif = new FakeNotificationService();
        var svc = NewBypassSvc(ctx, notif);
        var pending = await SeedPendingViaSaveAsync(ctx, coachee, notif);
        var source = await MakePendingSiapLulusAsync(ctx, svc, pending); // Siap + penanda Exam

        var result = await svc.CancelPendingAsync(pending.Id, "hc2", "HC Dua");

        Assert.True(result.Success, result.Message);
        var freshPending = await ctx.PendingProtonBypasses.AsNoTracking().SingleAsync(p => p.Id == pending.Id);
        Assert.Equal("Dibatalkan", freshPending.Status);
        // Worker lulus → hasil exam DIPERTAHANKAN (penanda Origin="Exam" sah).
        var linked = await ctx.AssessmentSessions.AsNoTracking().SingleAsync(s => s.Id == pending.LinkedAssessmentSessionId);
        Assert.NotEqual("Dibatalkan", linked.Status);
        Assert.Equal(1, await ctx.ProtonFinalAssessments.CountAsync(
            fa => fa.ProtonTrackAssignmentId == source.Id && fa.Origin == "Exam"));
    }

    [Fact]
    public async Task Cancel_SudahKerjakanGagal_SessionTetapCompleted()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"cnl3-{Guid.NewGuid():N}";
        var notif = new FakeNotificationService();
        var pending = await SeedPendingViaSaveAsync(ctx, coachee, notif);
        // Worker kerjakan exam tapi GAGAL: Completed + IsPassed=false → pending TETAP Menunggu (MISS-2).
        var session = await ctx.AssessmentSessions.FindAsync(pending.LinkedAssessmentSessionId);
        session!.IsPassed = false;
        session.Status = "Completed";
        await ctx.SaveChangesAsync();

        var result = await NewBypassSvc(ctx, notif).CancelPendingAsync(pending.Id, "hc2", "HC Dua");

        Assert.True(result.Success, result.Message);
        var freshPending = await ctx.PendingProtonBypasses.AsNoTracking().SingleAsync(p => p.Id == pending.Id);
        Assert.Equal("Dibatalkan", freshPending.Status);
        // W-03: session Completed-gagal TIDAK di-overwrite "Dibatalkan" — jejak dipertahankan.
        var linked = await ctx.AssessmentSessions.AsNoTracking().SingleAsync(s => s.Id == pending.LinkedAssessmentSessionId);
        Assert.Equal("Completed", linked.Status);
        Assert.False(linked.IsPassed);
    }
}
