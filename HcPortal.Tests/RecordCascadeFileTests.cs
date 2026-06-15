// Phase 367 Plan 02 Task 2 — [Fact] file sertifikat fisik terhapus POST-commit (#19, L-08).
// Reuse RecordCascadeFixture (disposable real-SQL) + FakeWebHostEnvironment dari RecordCascadeIntegrationTests.cs.
// Pola temp-webroot ImageCleanupIntegrationTests:83-95. warn-only: file hilang ditoleransi (tak melempar).
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class RecordCascadeFileTests : IClassFixture<RecordCascadeFixture>
{
    private readonly RecordCascadeFixture _fixture;
    public RecordCascadeFileTests(RecordCascadeFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static RecordCascadeDeleteService MakeService(ApplicationDbContext ctx, string webRoot)
    {
        var audit = new AuditLogService(ctx);
        var proton = new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, new FakeNotificationService(), audit);
        var env = new FakeWebHostEnvironment { WebRootPath = webRoot };
        return new RecordCascadeDeleteService(ctx, NullLogger<RecordCascadeDeleteService>.Instance, proton, audit, env);
    }

    private static string MakeTempWebRoot()
    {
        var dir = Path.Combine(Path.GetTempPath(), "hcportal-casctest-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string WriteFakeFile(string webRoot, string relUrl)
    {
        var physical = Path.Combine(webRoot, relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(physical)!);
        File.WriteAllBytes(physical, new byte[] { 1, 2, 3 });
        return physical;
    }

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "cascf-" + Guid.NewGuid().ToString("N")[..8], Email = "cascf@test.local", FullName = "Casc File" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    // #19 — file sertifikat manual (AssessmentSession.ManualSertifikatUrl) terhapus post-commit.
    [Fact]
    public async Task ManualCertFile_DeletedPostCommit_OnSessionDelete()
    {
        var webRoot = MakeTempWebRoot();
        const string relUrl = "/uploads/certs/test/session-cert.pdf";
        try
        {
            int sessionId;
            string physical;
            await using (var ctx = NewCtx())
            {
                var userId = await SeedUserAsync(ctx);
                var s = new AssessmentSession { UserId = userId, Title = "CertSrc", Category = "Test", Status = "Completed", AccessToken = "", Schedule = new DateTime(2026, 2, 1), ManualSertifikatUrl = relUrl };
                ctx.AssessmentSessions.Add(s);
                await ctx.SaveChangesAsync();
                sessionId = s.Id;
                physical = WriteFakeFile(webRoot, relUrl);
                Assert.True(File.Exists(physical)); // precondition

                var result = await MakeService(ctx, webRoot).ExecuteAsync("session", sessionId, Array.Empty<int>(), userId, "Tester");
                Assert.True(result.Success);
            }
            Assert.False(File.Exists(physical), "File sertifikat manual harus terhapus POST-commit (#19).");
        }
        finally
        {
            try { Directory.Delete(webRoot, recursive: true); } catch { /* best-effort */ }
        }
    }

    // #19 — file sertifikat TrainingRecord (SertifikatUrl) terhapus post-commit.
    [Fact]
    public async Task TrainingCertFile_DeletedPostCommit_OnTrainingDelete()
    {
        var webRoot = MakeTempWebRoot();
        const string relUrl = "/uploads/certs/test/training-cert.pdf";
        try
        {
            int trainingId;
            string physical;
            await using (var ctx = NewCtx())
            {
                var userId = await SeedUserAsync(ctx);
                var t = new TrainingRecord { UserId = userId, Judul = "TrainCert", Tanggal = new DateTime(2026, 2, 2), Status = "Valid", SertifikatUrl = relUrl };
                ctx.TrainingRecords.Add(t);
                await ctx.SaveChangesAsync();
                trainingId = t.Id;
                physical = WriteFakeFile(webRoot, relUrl);
                Assert.True(File.Exists(physical));

                var result = await MakeService(ctx, webRoot).ExecuteAsync("training", trainingId, Array.Empty<int>(), userId, "Tester");
                Assert.True(result.Success);
            }
            Assert.False(File.Exists(physical), "File sertifikat training harus terhapus POST-commit (#19).");
        }
        finally
        {
            try { Directory.Delete(webRoot, recursive: true); } catch { /* best-effort */ }
        }
    }

    // warn-only: file fisik hilang (sudah terhapus manual) TIDAK melempar — cascade tetap Success.
    [Fact]
    public async Task MissingCertFile_WarnOnly_DoesNotThrow()
    {
        var webRoot = MakeTempWebRoot();
        try
        {
            int sessionId; string userId;
            await using (var ctx = NewCtx())
            {
                userId = await SeedUserAsync(ctx);
                var s = new AssessmentSession { UserId = userId, Title = "MissingCert", Category = "Test", Status = "Completed", AccessToken = "", Schedule = new DateTime(2026, 2, 1), ManualSertifikatUrl = "/uploads/certs/test/nonexistent.pdf" };
                ctx.AssessmentSessions.Add(s);
                await ctx.SaveChangesAsync();
                sessionId = s.Id;

                var result = await MakeService(ctx, webRoot).ExecuteAsync("session", sessionId, Array.Empty<int>(), userId, "Tester");
                Assert.True(result.Success); // file tak ada → tetap sukses (warn-only)
            }
            await using (var verify = NewCtx())
                Assert.Equal(0, await verify.AssessmentSessions.CountAsync(a => a.Id == sessionId));
        }
        finally
        {
            try { Directory.Delete(webRoot, recursive: true); } catch { /* best-effort */ }
        }
    }
}
