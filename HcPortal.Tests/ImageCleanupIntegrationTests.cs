using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 366 SC#4 — integration test real-SQL (pola Phase 344 TEST-05 disposable) yang meng-exercise
/// helper PRODUKSI <see cref="ImageFileCleanup.DeleteUnreferencedAsync"/> end-to-end atas SQL Server nyata.
///
/// Membuktikan:
///   [Fact] 1 SC#2 — file gambar orphan TERHAPUS saat seluruh cascade (session→paket→soal→opsi) dihapus.
///   [Fact] 2 SC#3 — shared-path Pre/Post SELAMAT saat HANYA 1 sisi dihapus (regresi termahal SYN-01),
///                   karena post-commit AnyAsync masih true (sisi Post merefer path).
///
/// Pakai DB disposable HcPortalDB_Test_&lt;guid&gt; di localhost\SQLEXPRESS, di-drop pada sukses
/// (DisposeAsync) DAN gagal mid-setup (InitializeAsync catch). HcPortalDB_Dev TIDAK pernah disentuh —
/// jadi tak perlu SEED_WORKFLOW snapshot/restore.
///
/// [Trait("Category","Integration")] → CI SQL-less skip via: dotnet test --filter "Category!=Integration".
/// </summary>
public class ImageCleanupFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    public DbContextOptions<ApplicationDbContext> Options => _options;

    public ImageCleanupFixture()
    {
        // localhost-only + Integrated Security (mirror connstr guard; no secrets/env). SQLEXPRESS self-signed cert → TrustServerCertificate=True.
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try
        {
            await using var ctx = new ApplicationDbContext(_options);
            await ctx.Database.MigrateAsync(); // full migration chain (real DDL, bukan schema-from-model). Tak perlu seed tambahan.
        }
        catch (Exception ex)
        {
            try { await using var cleanup = new ApplicationDbContext(_options); await cleanup.Database.EnsureDeletedAsync(); } catch { /* best-effort */ }
            throw new Xunit.Sdk.XunitException(
                $"Phase 366 integration setup failed during MigrateAsync of disposable DB {DbName}. " +
                $"Indikasi MIGRATION-CHAIN break (full chain runs), BUKAN bug image-cleanup. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

// Isolasi: kedua [Fact] berbagi 1 DB disposable (IClassFixture) TAPI tiap test memakai relUrl
// unik (orphan.jpg vs shared.jpg), session/paket/soal sendiri, dan temp-webroot sendiri (MakeTempWebRoot
// per test) — jadi tak ada cross-test contamination meski rows persist antar Fact. Pakai DbContext baru
// per test (await using). Bila kelak butuh isolasi DB penuh, ganti IClassFixture → IAsyncLifetime per-test.
[Trait("Category", "Integration")]
public class ImageCleanupIntegrationTests : IClassFixture<ImageCleanupFixture>
{
    private readonly ImageCleanupFixture _fixture;

    public ImageCleanupIntegrationTests(ImageCleanupFixture fixture)
    {
        _fixture = fixture;
    }

    private static string MakeTempWebRoot()
    {
        var dir = Path.Combine(Path.GetTempPath(), "hcportal-imgtest-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string WriteFakeImage(string webRoot, string relUrl)
    {
        var physical = Path.Combine(webRoot, relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(physical)!);
        File.WriteAllBytes(physical, new byte[] { 1, 2, 3 });
        return physical;
    }

    private static ILogger SilentLogger() => NullLogger.Instance;

    // AssessmentSessions.UserId punya FK ke Users (FK_AssessmentSessions_Users_UserId) → seed user dulu.
    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var user = new ApplicationUser
        {
            UserName = "imgtest-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Email = "imgtest@test.local",
            FullName = "Img Test"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user.Id;
    }

    private static AssessmentSession NewSession(string userId, string title, string? type = null) => new AssessmentSession
    {
        UserId = userId,
        Title = title,
        Category = "Test",
        Status = "Open",
        AccessToken = "",
        AssessmentType = type
    };

    // [Fact] 1 — SC#2: orphan path terhapus saat seluruh cascade dihapus.
    [Fact]
    public async Task OrphanPath_Deleted_WhenFullCascade()
    {
        var webRoot = MakeTempWebRoot();
        const string relUrl = "/uploads/questions/test/orphan.jpg";
        try
        {
            await using (var ctx = new ApplicationDbContext(_fixture.Options))
            {
                var userId = await SeedUserAsync(ctx);
                var session = NewSession(userId, "Phase366 Orphan");
                ctx.AssessmentSessions.Add(session);
                await ctx.SaveChangesAsync();

                var pkg = new AssessmentPackage
                {
                    AssessmentSessionId = session.Id,
                    PackageName = "P1",
                    Questions = new List<PackageQuestion>
                    {
                        new PackageQuestion
                        {
                            QuestionText = "Q1",
                            ImagePath = relUrl,
                            Options = new List<PackageOption>
                            {
                                new PackageOption { OptionText = "A", IsCorrect = true, ImagePath = relUrl }
                            }
                        }
                    }
                };
                ctx.AssessmentPackages.Add(pkg);
                await ctx.SaveChangesAsync();

                var physical = WriteFakeImage(webRoot, relUrl);
                Assert.True(File.Exists(physical)); // precondition

                // act: tiru cascade DeleteAssessment — collect SEBELUM RemoveRange, helper SETELAH commit.
                var imagePaths = pkg.Questions
                    .SelectMany(q => new[] { q.ImagePath }.Concat(q.Options.Select(o => o.ImagePath)))
                    .Where(p => !string.IsNullOrEmpty(p)).Select(p => p!).Distinct().ToList();

                foreach (var q in pkg.Questions) ctx.PackageOptions.RemoveRange(q.Options);
                ctx.PackageQuestions.RemoveRange(pkg.Questions);
                ctx.AssessmentPackages.Remove(pkg);
                ctx.AssessmentSessions.Remove(session);
                await ctx.SaveChangesAsync(); // commit cascade

                await ImageFileCleanup.DeleteUnreferencedAsync(ctx, webRoot, SilentLogger(), imagePaths, "test-orphan");

                Assert.False(File.Exists(physical), "Orphan path harus terhapus (SC#2 — tak ada baris lain merefer).");
            }
        }
        finally
        {
            try { Directory.Delete(webRoot, recursive: true); } catch { /* best-effort */ }
        }
    }

    // [Fact] 2 — SC#3: shared-path Pre/Post SELAMAT saat 1 sisi dihapus (regresi termahal SYN-01).
    [Fact]
    public async Task SharedPrePostPath_Survives_WhenOneSideDeleted()
    {
        var webRoot = MakeTempWebRoot();
        const string relUrl = "/uploads/questions/test/shared.jpg";
        try
        {
            await using (var ctx = new ApplicationDbContext(_fixture.Options))
            {
                // Pre + Post share ImagePath string identik (shared-file v24.0 SYN-01).
                var userId = await SeedUserAsync(ctx);
                var preSession = NewSession(userId, "Phase366 Pre", "PreTest");
                var postSession = NewSession(userId, "Phase366 Post", "PostTest");
                ctx.AssessmentSessions.AddRange(preSession, postSession);
                await ctx.SaveChangesAsync();

                var prePkg = new AssessmentPackage
                {
                    AssessmentSessionId = preSession.Id, PackageName = "Pre",
                    Questions = new List<PackageQuestion> { new PackageQuestion { QuestionText = "Pre Q", ImagePath = relUrl } }
                };
                var postPkg = new AssessmentPackage
                {
                    AssessmentSessionId = postSession.Id, PackageName = "Post",
                    Questions = new List<PackageQuestion> { new PackageQuestion { QuestionText = "Post Q", ImagePath = relUrl } }
                };
                ctx.AssessmentPackages.AddRange(prePkg, postPkg);
                await ctx.SaveChangesAsync();

                var physical = WriteFakeImage(webRoot, relUrl);

                // act: hapus HANYA sisi Pre (Post MASIH merefer path), commit, lalu helper.
                var imagePaths = prePkg.Questions
                    .SelectMany(q => new[] { q.ImagePath }.Concat(q.Options.Select(o => o.ImagePath)))
                    .Where(p => !string.IsNullOrEmpty(p)).Select(p => p!).Distinct().ToList();

                foreach (var q in prePkg.Questions) ctx.PackageOptions.RemoveRange(q.Options);
                ctx.PackageQuestions.RemoveRange(prePkg.Questions);
                ctx.AssessmentPackages.Remove(prePkg);
                ctx.AssessmentSessions.Remove(preSession);
                await ctx.SaveChangesAsync(); // commit (Post tetap ada)

                await ImageFileCleanup.DeleteUnreferencedAsync(ctx, webRoot, SilentLogger(), imagePaths, "test-shared");

                Assert.True(File.Exists(physical), "Shared path harus SELAMAT (SC#3 — Post masih merefer → AnyAsync true → SKIP).");
            }
        }
        finally
        {
            try { Directory.Delete(webRoot, recursive: true); } catch { /* best-effort */ }
        }
    }
}
