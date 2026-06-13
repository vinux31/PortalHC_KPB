// Phase 367 Plan 05 — kontrak hapus tab 1 (DeleteAssessment/Group/PrePost) yang di-DELEGASIKAN ke
// RecordCascadeDeleteService.ExecuteAsync: (a) L-03 NO-BLOCKER — turunan renewal IKUT terhapus (BUKAN diblokir
// seperti pre-check fase 325/329 lama), (b) #19 — file sertifikat manual fisik terhapus post-commit warn-only.
// Endpoint = thin wrapper (12 ctor dep, butuh HTTP harness penuh) → diuji via engine path yg dipanggil endpoint.
// Reuse RecordCascadeFixture (disposable real-SQL) + Fake* dari RecordCascadeIntegrationTests.cs.
using System;
using System.Collections.Generic;
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
public class CertFileTab1Tests : IClassFixture<RecordCascadeFixture>
{
    private readonly RecordCascadeFixture _fixture;
    public CertFileTab1Tests(RecordCascadeFixture fixture) => _fixture = fixture;

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
        var dir = Path.Combine(Path.GetTempPath(), "hcportal-tab1cert-" + Guid.NewGuid().ToString("N")[..8]);
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
        var u = new ApplicationUser { UserName = "tab1-" + Guid.NewGuid().ToString("N")[..8], Email = "tab1@test.local", FullName = "Tab1 Cert" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static AssessmentSession NewSession(string userId, string title, string? certUrl = null, int? renewsSession = null) =>
        new AssessmentSession
        {
            UserId = userId, Title = title, Category = "Test", Status = "Completed", AccessToken = "",
            Schedule = new DateTime(2026, 3, 1), ManualSertifikatUrl = certUrl, RenewsSessionId = renewsSession
        };

    // ── [Fact] 1 — L-03 NO-BLOCKER: induk tab 1 + turunan renewal (TR) IKUT terhapus, cert induk+anak hilang ──
    // SEBELUM 367: pre-check refTr+refAs>0 → BLOKIR (return). SESUDAH: cascade penuh.
    [Fact]
    public async Task Tab1Delete_RenewalChildTraining_NoBlocker_CertFilesDeleted()
    {
        var webRoot = MakeTempWebRoot();
        const string indukCert = "/uploads/certs/tab1/induk.pdf";
        const string anakCert = "/uploads/certs/tab1/anak-tr.pdf";
        try
        {
            int indukId, anakTrId;
            string indukPhysical, anakPhysical;
            await using (var ctx = NewCtx())
            {
                var userId = await SeedUserAsync(ctx);
                var induk = NewSession(userId, "IndukTab1", certUrl: indukCert);
                ctx.AssessmentSessions.Add(induk);
                await ctx.SaveChangesAsync();
                indukId = induk.Id;

                // Turunan renewal: TrainingRecord yg menjadikan induk sebagai sumber renewal (RenewsSessionId).
                var anak = new TrainingRecord { UserId = userId, Judul = "AnakRenewal", Tanggal = new DateTime(2027, 3, 1), Status = "Valid", RenewsSessionId = indukId, SertifikatUrl = anakCert };
                ctx.TrainingRecords.Add(anak);
                await ctx.SaveChangesAsync();
                anakTrId = anak.Id;

                indukPhysical = WriteFakeFile(webRoot, indukCert);
                anakPhysical = WriteFakeFile(webRoot, anakCert);
                Assert.True(File.Exists(indukPhysical));
                Assert.True(File.Exists(anakPhysical));

                var result = await MakeService(ctx, webRoot).ExecuteAsync("session", indukId, Array.Empty<int>(), userId, "Tester");
                Assert.True(result.Success);
                Assert.Equal(2, result.DeletedCount); // induk + anak (no-blocker)
            }
            await using (var verify = NewCtx())
            {
                Assert.Equal(0, await verify.AssessmentSessions.CountAsync(a => a.Id == indukId));    // induk hilang
                Assert.Equal(0, await verify.TrainingRecords.CountAsync(t => t.Id == anakTrId));      // turunan IKUT terhapus (L-03)
            }
            Assert.False(File.Exists(indukPhysical), "Cert induk harus terhapus post-commit (#19).");
            Assert.False(File.Exists(anakPhysical), "Cert turunan renewal harus terhapus post-commit (#19).");
        }
        finally
        {
            try { Directory.Delete(webRoot, recursive: true); } catch { /* best-effort */ }
        }
    }

    // ── [Fact] 2 — NO-BLOCKER rantai dalam: induk → TR child → AS grandchild semuanya terhapus ──
    [Fact]
    public async Task Tab1Delete_RenewalChain_AllDescendantsDeleted_NoBlocker()
    {
        var webRoot = MakeTempWebRoot();
        try
        {
            int indukId, childTrId, grandSId;
            await using (var ctx = NewCtx())
            {
                var userId = await SeedUserAsync(ctx);
                var induk = NewSession(userId, "ChainRoot");
                ctx.AssessmentSessions.Add(induk);
                await ctx.SaveChangesAsync();
                indukId = induk.Id;

                var childTr = new TrainingRecord { UserId = userId, Judul = "ChainChildTR", Tanggal = new DateTime(2027, 3, 1), Status = "Valid", RenewsSessionId = indukId };
                ctx.TrainingRecords.Add(childTr);
                await ctx.SaveChangesAsync();
                childTrId = childTr.Id;

                var grand = NewSession(userId, "ChainGrand");
                grand.RenewsTrainingId = childTrId; // grandchild me-renew child TR
                ctx.AssessmentSessions.Add(grand);
                await ctx.SaveChangesAsync();
                grandSId = grand.Id;

                var result = await MakeService(ctx, webRoot).ExecuteAsync("session", indukId, Array.Empty<int>(), userId, "Tester");
                Assert.True(result.Success);
                Assert.Equal(3, result.DeletedCount); // induk + child + grand
            }
            await using (var verify = NewCtx())
            {
                Assert.Equal(0, await verify.AssessmentSessions.CountAsync(a => a.Id == indukId || a.Id == grandSId));
                Assert.Equal(0, await verify.TrainingRecords.CountAsync(t => t.Id == childTrId));
            }
        }
        finally
        {
            try { Directory.Delete(webRoot, recursive: true); } catch { /* best-effort */ }
        }
    }

    // ── [Fact] 3 — semantik loop grup (DeleteAssessmentGroup/PrePost): per-sibling ExecuteAsync + deletedSet ──
    // Mereplikasi loop endpoint: 2 sibling masing-masing punya cert + turunan renewal → semua terhapus, cert hilang.
    [Fact]
    public async Task Tab1GroupLoop_PerSiblingCascade_AllDeleted_CertsGone()
    {
        var webRoot = MakeTempWebRoot();
        const string certA = "/uploads/certs/tab1/sibA.pdf";
        const string certB = "/uploads/certs/tab1/sibB.pdf";
        try
        {
            int sibAId, sibBId, childAId, childBId;
            string physA, physB;
            await using (var ctx = NewCtx())
            {
                var userId = await SeedUserAsync(ctx);
                var sibA = NewSession(userId, "GrupSesi", certUrl: certA);
                var sibB = NewSession(userId, "GrupSesi", certUrl: certB);
                ctx.AssessmentSessions.AddRange(sibA, sibB);
                await ctx.SaveChangesAsync();
                sibAId = sibA.Id; sibBId = sibB.Id;

                var childA = new TrainingRecord { UserId = userId, Judul = "RenewA", Tanggal = new DateTime(2027, 3, 1), Status = "Valid", RenewsSessionId = sibAId };
                var childB = new TrainingRecord { UserId = userId, Judul = "RenewB", Tanggal = new DateTime(2027, 3, 1), Status = "Valid", RenewsSessionId = sibBId };
                ctx.TrainingRecords.AddRange(childA, childB);
                await ctx.SaveChangesAsync();
                childAId = childA.Id; childBId = childB.Id;

                physA = WriteFakeFile(webRoot, certA);
                physB = WriteFakeFile(webRoot, certB);

                // Replika loop endpoint: foreach sibling, skip via deletedSet, ExecuteAsync per sibling unik.
                var siblings = new[] { sibAId, sibBId };
                var svc = MakeService(ctx, webRoot);
                int totalDeleted = 0;
                var deletedSet = new HashSet<int>();
                foreach (var sid in siblings)
                {
                    if (deletedSet.Contains(sid)) continue;
                    var r = await svc.ExecuteAsync("session", sid, Array.Empty<int>(), userId, "Tester");
                    Assert.True(r.Success);
                    totalDeleted += r.DeletedCount;
                    foreach (var d in r.DeletedSessionIds) deletedSet.Add(d);
                }
                Assert.Equal(4, totalDeleted); // 2 sibling + 2 turunan
            }
            await using (var verify = NewCtx())
            {
                Assert.Equal(0, await verify.AssessmentSessions.CountAsync(a => a.Id == sibAId || a.Id == sibBId));
                Assert.Equal(0, await verify.TrainingRecords.CountAsync(t => t.Id == childAId || t.Id == childBId));
            }
            Assert.False(File.Exists(physA), "Cert sibling A harus terhapus (#19).");
            Assert.False(File.Exists(physB), "Cert sibling B harus terhapus (#19).");
        }
        finally
        {
            try { Directory.Delete(webRoot, recursive: true); } catch { /* best-effort */ }
        }
    }

    // ── [Fact] 4 — FIX temuan kritis (HC guard bypass): endpoint kini gate izin HC atas SELURUH set cascade
    // (root + turunan), bukan cuma root. Bukti: root deletable (non-Completed, 0 jawaban) tapi turunan renewal
    // Completed + ada jawaban → set cascade (CollectCascadeIds) mengandung node Completed & responseCount>0 →
    // predikat guard EnsureCanDeleteAsync (anyCompleted || responseCount>0) AKAN blokir HC. Cek root-saja TIDAK blok.
    [Fact]
    public async Task Tab1CascadeSet_SurfacesCompletedAnsweredDescendant_SoHcGuardBlocks()
    {
        var webRoot = MakeTempWebRoot();
        try
        {
            await using var ctx = NewCtx();
            var userId = await SeedUserAsync(ctx);

            var root = NewSession(userId, "DeletableRoot");
            root.Status = "Upcoming"; // root sendiri lolos guard (non-Completed, tanpa jawaban)
            ctx.AssessmentSessions.Add(root);
            await ctx.SaveChangesAsync();

            var child = NewSession(userId, "CompletedChild"); // default Status="Completed"
            child.RenewsSessionId = root.Id;                  // turunan renewal dari root
            ctx.AssessmentSessions.Add(child);
            await ctx.SaveChangesAsync();

            // jawaban peserta pada TURUNAN (bukan root)
            var pkg = new AssessmentPackage
            {
                AssessmentSessionId = child.Id, PackageName = "P",
                Questions = new List<PackageQuestion> { new PackageQuestion { QuestionText = "Q", Options = new List<PackageOption> { new PackageOption { OptionText = "A", IsCorrect = true } } } }
            };
            ctx.AssessmentPackages.Add(pkg);
            await ctx.SaveChangesAsync();
            var qId = pkg.Questions.First().Id;
            ctx.PackageUserResponses.Add(new PackageUserResponse { AssessmentSessionId = child.Id, PackageQuestionId = qId });
            await ctx.SaveChangesAsync();

            // Replika perhitungan endpoint pasca-fix: set cascade PENUH + predikat guard.
            var nodes = await MakeService(ctx, webRoot).CollectCascadeIds("session", root.Id);
            var cascadeSessionIds = nodes.Where(n => n.Type == "session").Select(n => n.Id).ToList();
            var cascadeSessions = await ctx.AssessmentSessions.Where(a => cascadeSessionIds.Contains(a.Id)).ToListAsync();
            int responseCount = await ctx.PackageUserResponses.CountAsync(r => cascadeSessionIds.Contains(r.AssessmentSessionId));

            var rootInSet = cascadeSessions.First(s => s.Id == root.Id);
            Assert.NotEqual("Completed", rootInSet.Status);                                  // guard root-saja: LOLOS
            Assert.Contains(cascadeSessions, s => s.Id == child.Id && s.Status == "Completed"); // turunan TERCOVER set
            bool rootOnlyWouldBlock = rootInSet.Status == "Completed";
            bool fullSetBlocksHc = responseCount > 0 || cascadeSessions.Any(s => s.Status == "Completed");
            Assert.False(rootOnlyWouldBlock, "Guard root-saja (kode lama) TIDAK blok — itulah bug-nya.");
            Assert.True(fullSetBlocksHc, "Guard atas SET cascade (kode fix) HARUS blok HC: turunan Completed/ber-jawaban.");
        }
        finally
        {
            try { Directory.Delete(webRoot, recursive: true); } catch { /* best-effort */ }
        }
    }
}
