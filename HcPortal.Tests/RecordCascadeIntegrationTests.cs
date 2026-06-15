// Phase 367 Plan 02 Task 2 — integration real-SQL per-tabel untuk RecordCascadeDeleteService.ExecuteAsync.
// Pola disposable DB (ImageCleanupIntegrationTests): HcPortalDB_Test_{guid} @localhost\SQLEXPRESS, MigrateAsync, drop on dispose.
// [Trait Category=Integration] → skip via --filter "Category!=Integration". Tiap [Fact] seed user unik (Guid) + assert by id spesifik.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HcPortal.Tests;

public class RecordCascadeFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public RecordCascadeFixture()
    {
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try
        {
            await using var ctx = new ApplicationDbContext(_options);
            await ctx.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            try { await using var cleanup = new ApplicationDbContext(_options); await cleanup.Database.EnsureDeletedAsync(); } catch { /* best-effort */ }
            throw new Xunit.Sdk.XunitException(
                $"Phase 367 integration setup failed during MigrateAsync of disposable DB {DbName}. Indikasi MIGRATION-CHAIN break, BUKAN bug cascade. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

// Fake IWebHostEnvironment — hanya WebRootPath yang dipakai engine (File.Delete post-commit).
internal sealed class FakeWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; } = null!;
    public string ApplicationName { get; set; } = "HcPortal.Tests";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
    public string ContentRootPath { get; set; } = "";
    public string EnvironmentName { get; set; } = "Test";
}

[Trait("Category", "Integration")]
public class RecordCascadeIntegrationTests : IClassFixture<RecordCascadeFixture>
{
    private readonly RecordCascadeFixture _fixture;
    public RecordCascadeIntegrationTests(RecordCascadeFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static RecordCascadeDeleteService MakeService(ApplicationDbContext ctx, string webRoot = "")
    {
        var audit = new AuditLogService(ctx);
        var proton = new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, new FakeNotificationService(), audit);
        var env = new FakeWebHostEnvironment { WebRootPath = webRoot };
        return new RecordCascadeDeleteService(ctx, NullLogger<RecordCascadeDeleteService>.Instance, proton, audit, env);
    }

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "casc-" + Guid.NewGuid().ToString("N")[..8], Email = "casc@test.local", FullName = "Casc Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static AssessmentSession NewSession(string userId, string title = "Exam", int? renewsSession = null, int? renewsTraining = null, int? protonTrackId = null) =>
        new AssessmentSession
        {
            UserId = userId, Title = title, Category = "Test", Status = "Completed", AccessToken = "",
            Schedule = new DateTime(2026, 2, 1), RenewsSessionId = renewsSession, RenewsTrainingId = renewsTraining, ProtonTrackId = protonTrackId
        };

    private static TrainingRecord NewTraining(string userId, string judul = "T", int? renewsSession = null, int? renewsTraining = null) =>
        new TrainingRecord { UserId = userId, Judul = judul, Tanggal = new DateTime(2026, 2, 2), Status = "Valid", RenewsSessionId = renewsSession, RenewsTrainingId = renewsTraining };

    // ── [Fact] 1 — full cascade: node + semua artefak terhapus (#5/#11) ─────────────────
    [Fact]
    public async Task FullCascade_DeletesNodesAndAllArtefacts()
    {
        int rootId, childTrId, grandSId;
        await using (var ctx = NewCtx())
        {
            var userId = await SeedUserAsync(ctx);
            var root = NewSession(userId, "Root");
            ctx.AssessmentSessions.Add(root);
            await ctx.SaveChangesAsync();
            rootId = root.Id;

            // renewal chain: child TR (RenewsSessionId=root) -> grand session (RenewsTrainingId=child)
            var child = NewTraining(userId, "Child", renewsSession: rootId);
            ctx.TrainingRecords.Add(child);
            await ctx.SaveChangesAsync();
            childTrId = child.Id;
            var grand = NewSession(userId, "Grand", renewsTraining: childTrId);
            ctx.AssessmentSessions.Add(grand);
            await ctx.SaveChangesAsync();
            grandSId = grand.Id;

            // artefak pada root: package+Q+O, UPA, response, attempt history, edit log
            var pkg = new AssessmentPackage
            {
                AssessmentSessionId = rootId, PackageName = "P1",
                Questions = new List<PackageQuestion> { new PackageQuestion { QuestionText = "Q1", Options = new List<PackageOption> { new PackageOption { OptionText = "A", IsCorrect = true } } } }
            };
            ctx.AssessmentPackages.Add(pkg);
            await ctx.SaveChangesAsync();
            var qId = pkg.Questions.First().Id;

            ctx.UserPackageAssignments.Add(new UserPackageAssignment { AssessmentSessionId = rootId, AssessmentPackageId = pkg.Id, UserId = userId });
            ctx.PackageUserResponses.Add(new PackageUserResponse { AssessmentSessionId = rootId, PackageQuestionId = qId });
            ctx.AssessmentAttemptHistory.Add(new AssessmentAttemptHistory { SessionId = rootId, UserId = userId, Title = "Root", Category = "Test", AttemptNumber = 1 });
            ctx.AssessmentEditLogs.Add(new AssessmentEditLog { AssessmentSessionId = rootId, PackageQuestionId = qId, ActorUserId = userId, ActorName = "x", ActorRole = "Admin", ReasonCode = "EDIT" });
            await ctx.SaveChangesAsync();

            var result = await MakeService(ctx).ExecuteAsync("session", rootId, Array.Empty<int>(), userId, "Tester");
            Assert.True(result.Success);
            Assert.Equal(3, result.DeletedCount); // root + child + grand
        }

        await using (var verify = NewCtx())
        {
            Assert.Equal(0, await verify.AssessmentSessions.CountAsync(a => a.Id == rootId || a.Id == grandSId));
            Assert.Equal(0, await verify.TrainingRecords.CountAsync(t => t.Id == childTrId));
            Assert.Equal(0, await verify.AssessmentEditLogs.CountAsync(e => e.AssessmentSessionId == rootId));
            Assert.Equal(0, await verify.PackageUserResponses.CountAsync(r => r.AssessmentSessionId == rootId));
            Assert.Equal(0, await verify.AssessmentAttemptHistory.CountAsync(h => h.SessionId == rootId));
            Assert.Equal(0, await verify.UserPackageAssignments.CountAsync(a => a.AssessmentSessionId == rootId));
            Assert.Equal(0, await verify.AssessmentPackages.CountAsync(p => p.AssessmentSessionId == rootId));
        }
    }

    // ── [Fact] 2 — PendingProtonBypass SOFT-CANCEL, BUKAN hilang (#10/L-04) ──────────────
    [Fact]
    public async Task PendingBypass_SoftCancelled_NotDeleted()
    {
        int sessionId, bypassId;
        await using (var ctx = NewCtx())
        {
            var userId = await SeedUserAsync(ctx);
            var s = NewSession(userId, "BypassSrc");
            ctx.AssessmentSessions.Add(s);
            await ctx.SaveChangesAsync();
            sessionId = s.Id;
            var bp = new PendingProtonBypass { CoacheeId = userId, SourceProtonTrackId = 1, TargetProtonTrackId = 2, TargetUnit = "U", Reason = "r", LinkedAssessmentSessionId = sessionId, Status = "Menunggu", InitiatedById = userId };
            ctx.PendingProtonBypasses.Add(bp);
            await ctx.SaveChangesAsync();
            bypassId = bp.Id;

            await MakeService(ctx).ExecuteAsync("session", sessionId, Array.Empty<int>(), userId, "Tester");
        }
        await using (var verify = NewCtx())
        {
            var bp = await verify.PendingProtonBypasses.FirstOrDefaultAsync(p => p.Id == bypassId);
            Assert.NotNull(bp); // BUKAN hard-delete
            Assert.Equal("Dibatalkan", bp!.Status);
            Assert.NotNull(bp.ResolvedAt);
        }
    }

    // ── [Fact] 3 — LinkedSessionId pasangan di-null-clear (#8) ───────────────────────────
    [Fact]
    public async Task LinkedSession_NullCleared_OnPartner()
    {
        int preId, postId;
        await using (var ctx = NewCtx())
        {
            var userId = await SeedUserAsync(ctx);
            var pre = NewSession(userId, "Pre");
            ctx.AssessmentSessions.Add(pre);
            await ctx.SaveChangesAsync();
            preId = pre.Id;
            var post = NewSession(userId, "Post");
            post.LinkedSessionId = preId; // post menunjuk pre
            ctx.AssessmentSessions.Add(post);
            await ctx.SaveChangesAsync();
            postId = post.Id;

            await MakeService(ctx).ExecuteAsync("session", preId, Array.Empty<int>(), userId, "Tester"); // hapus pre
        }
        await using (var verify = NewCtx())
        {
            var post = await verify.AssessmentSessions.FirstOrDefaultAsync(a => a.Id == postId);
            Assert.NotNull(post);
            Assert.Null(post!.LinkedSessionId); // di-null-clear, bukan dangling
        }
    }

    // ── [Fact] 4 — Origin='Exam' tercabut, Interview KEBAL (#9) ──────────────────────────
    [Fact]
    public async Task ProtonOrigin_ExamRemoved_InterviewImmune()
    {
        int sessionId, examFaId, interviewFaId;
        await using (var ctx = NewCtx())
        {
            var userId = await SeedUserAsync(ctx);
            // ProtonTracks 6-baris standar sudah di-seed migration (unik TrackType+TahunKe) → REUSE. IX unik 1 FA per assignment
            // → Exam & Interview di assignment/track BERBEDA. RemoveExamOriginAsync hanya menargetkan track session (track1).
            var tracks = await ctx.ProtonTracks.OrderBy(t => t.Id).Take(2).ToListAsync();
            var asgExam = new ProtonTrackAssignment { CoacheeId = userId, AssignedById = userId, ProtonTrackId = tracks[0].Id, IsActive = true };
            var asgInterview = new ProtonTrackAssignment { CoacheeId = userId, AssignedById = userId, ProtonTrackId = tracks[1].Id, IsActive = true };
            ctx.ProtonTrackAssignments.AddRange(asgExam, asgInterview);
            await ctx.SaveChangesAsync();
            var examFa = new ProtonFinalAssessment { CoacheeId = userId, CreatedById = userId, ProtonTrackAssignmentId = asgExam.Id, Status = "Completed", Origin = "Exam" };
            var interviewFa = new ProtonFinalAssessment { CoacheeId = userId, CreatedById = userId, ProtonTrackAssignmentId = asgInterview.Id, Status = "Completed", Origin = "Interview" };
            ctx.ProtonFinalAssessments.AddRange(examFa, interviewFa);
            var s = NewSession(userId, "ProtonExam", protonTrackId: tracks[0].Id);
            ctx.AssessmentSessions.Add(s);
            await ctx.SaveChangesAsync();
            sessionId = s.Id; examFaId = examFa.Id; interviewFaId = interviewFa.Id;

            await MakeService(ctx).ExecuteAsync("session", sessionId, Array.Empty<int>(), userId, "Tester");
        }
        await using (var verify = NewCtx())
        {
            Assert.Equal(0, await verify.ProtonFinalAssessments.CountAsync(fa => fa.Id == examFaId));      // Exam tercabut
            Assert.Equal(1, await verify.ProtonFinalAssessments.CountAsync(fa => fa.Id == interviewFaId)); // Interview KEBAL
        }
    }

    // ── [Fact] 5 — notif eksak-match terhapus, non-record-bound BERTAHAN (#6) ────────────
    [Fact]
    public async Task Notif_ExactMatchRemoved_OtherSurvives()
    {
        int sessionId, startExamNotifId, otherNotifId;
        await using (var ctx = NewCtx())
        {
            var userId = await SeedUserAsync(ctx);
            var s = NewSession(userId, "NotifSrc");
            ctx.AssessmentSessions.Add(s);
            await ctx.SaveChangesAsync();
            sessionId = s.Id;
            var n1 = new UserNotification { UserId = userId, Type = "EXAM", Title = "t", Message = "m", ActionUrl = $"/CMP/StartExam/{sessionId}" };
            var n2 = new UserNotification { UserId = userId, Type = "OTHER", Title = "t", Message = "m", ActionUrl = "/CDP/ProtonProgress" };
            ctx.UserNotifications.AddRange(n1, n2);
            await ctx.SaveChangesAsync();
            startExamNotifId = n1.Id; otherNotifId = n2.Id;

            await MakeService(ctx).ExecuteAsync("session", sessionId, Array.Empty<int>(), userId, "Tester");
        }
        await using (var verify = NewCtx())
        {
            Assert.Equal(0, await verify.UserNotifications.CountAsync(n => n.Id == startExamNotifId)); // eksak-match terhapus
            Assert.Equal(1, await verify.UserNotifications.CountAsync(n => n.Id == otherNotifId));     // non-record-bound BERTAHAN
        }
    }

    // ── [Fact] 6 — AuditLog 1 entri CascadeDelete berisi daftar Id (L-08) ────────────────
    [Fact]
    public async Task Audit_OneEntry_ContainsDeletedIds()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var s = NewSession(userId, "AuditSrc");
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        int sessionId = s.Id;

        var before = await ctx.AuditLogs.CountAsync(a => a.ActionType == "CascadeDelete");
        await MakeService(ctx).ExecuteAsync("session", sessionId, Array.Empty<int>(), userId, "Tester");

        await using var verify = NewCtx();
        var entries = await verify.AuditLogs.Where(a => a.ActionType == "CascadeDelete" && a.TargetId == sessionId).ToListAsync();
        Assert.Single(entries);
        Assert.Contains(sessionId.ToString(), entries[0].Description);
    }

    // ── [Fact] 7 — rollback-on-exception: DB utuh saat SaveChanges gagal ────────────────
    [Fact]
    public async Task Rollback_OnException_LeavesDbIntact()
    {
        int sessionId;
        await using (var ctx = NewCtx())
        {
            var userId = await SeedUserAsync(ctx);
            var s = NewSession(userId, "RollbackSrc");
            ctx.AssessmentSessions.Add(s);
            await ctx.SaveChangesAsync();
            sessionId = s.Id;

            await using var throwing = new ThrowingDbContext(_fixture.Options);
            var audit = new AuditLogService(throwing);
            var proton = new ProtonCompletionService(throwing, NullLogger<ProtonCompletionService>.Instance, new FakeNotificationService(), audit);
            var svc = new RecordCascadeDeleteService(throwing, NullLogger<RecordCascadeDeleteService>.Instance, proton, audit, new FakeWebHostEnvironment());
            var result = await svc.ExecuteAsync("session", sessionId, Array.Empty<int>(), userId, "Tester");
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage); // pesan generik (no info leak)
            Assert.DoesNotContain("injected", result.ErrorMessage!); // detail internal TIDAK bocor
        }
        await using (var verify = NewCtx())
        {
            Assert.Equal(1, await verify.AssessmentSessions.CountAsync(a => a.Id == sessionId)); // utuh — rollback
        }
    }

    // ── [Fact] 8 — invariant preview-set == execute-set ─────────────────────────────────
    [Fact]
    public async Task PreviewSet_EqualsExecuteSet()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var root = NewSession(userId, "PvRoot");
        ctx.AssessmentSessions.Add(root);
        await ctx.SaveChangesAsync();
        var child = NewTraining(userId, "PvChild", renewsSession: root.Id);
        ctx.TrainingRecords.Add(child);
        await ctx.SaveChangesAsync();

        var preview = await MakeService(ctx).BuildPreviewAsync("session", root.Id);
        var previewSet = preview.Where(n => !n.IsMirrorCandidate)
            .Select(n => (n.Type, n.Id)).OrderBy(x => x.Type).ThenBy(x => x.Id).ToList();

        var result = await MakeService(ctx).ExecuteAsync("session", root.Id, Array.Empty<int>(), userId, "Tester");
        var executeSet = result.DeletedSessionIds.Select(id => ("session", id))
            .Concat(result.DeletedTrainingIds.Select(id => ("training", id)))
            .OrderBy(x => x.Item1).ThenBy(x => x.Item2).ToList();

        Assert.Equal(previewSet, executeSet);
    }

    // Context yang melempar pada SaveChangesAsync → memicu rollback path ExecuteAsync.
    private sealed class ThrowingDbContext : ApplicationDbContext
    {
        public ThrowingDbContext(DbContextOptions<ApplicationDbContext> o) : base(o) { }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new DbUpdateException("injected failure for rollback test");
    }
}
