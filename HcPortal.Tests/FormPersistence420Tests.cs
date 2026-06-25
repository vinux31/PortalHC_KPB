// FormPersistence420Tests — v32.7 Phase 420 FORM-01/02/03/04.
// Integration real-SQL: membuktikan kontrak persistensi field form Create/Edit assessment.
//
// Pendekatan = data/persistence level (project ini TAK punya WebApplicationFactory).
// Test mereplikasi SHAPE object-init build-loop + sibling-loop controller VERBATIM
// (SETELAH fix Task 1-2) — termasuk penyalinan eksplisit retake×3 + ValidUntil + Math.Clamp.
// Bila controller belum di-fix, test ini tetap membuktikan INVARIANT data yang DIHARAPKAN
// (apa yang HARUS tersimpan); wiring controller di-cover oleh anchored-insertion + grep gate
// (420-01 Task 1-2) + build hijau. Test ini = jaring pengaman invariant EF persist nilai
// eksplisit (incl. false) lewat real SQL Server — yang InMemory tak bisa jamin vs DB DEFAULT.
//
// Template: ShuffleCreatePersistenceTests.cs (persist→reread) + RetakeSettingsEndpointTests.cs
// (replika sibling-loop endpoint + clamp). Pakai RetakeServiceFixture (real-SQL, full migration
// chain incl AddRetakeColumnsAndArchive) — JANGAN bangun harness baru.
//
// [Trait("Category","Integration")] → CI SQL-less skip via dotnet test --filter "Category!=Integration".
using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// v32.7 Phase 420 — real-SQL: persistensi field form Create/Edit assessment.
/// - FORM-02 (std/Pre/Post): retake config tersimpan di 3 jalur build Create (bukan EF-default).
/// - FORM-03 + FORM-04 (Edit std): retake×3 + ValidUntil tersimpan ke SEMUA sibling (bukan no-op).
/// - FORM-01 (invariant data-level): shuffle write di Edit std loop benar (bug E-01 murni RENDER,
///   ditangkap e2e Plan 03). Membuktikan write tak memaksa false saat model true.
/// </summary>
[Trait("Category", "Integration")]
public class FormPersistence420Tests : IClassFixture<RetakeServiceFixture>
{
    private readonly RetakeServiceFixture _fixture;

    public FormPersistence420Tests(RetakeServiceFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser
        {
            UserName = "form420-" + Guid.NewGuid().ToString("N")[..8],
            Email = "form420@test.local",
            FullName = "Form Persistence 420 Test"
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private async Task<AssessmentSession> PersistAndReadAsync(AssessmentSession session)
    {
        int id;
        await using (var ctx = NewCtx())
        {
            session.UserId = await SeedUserAsync(ctx);
            ctx.AssessmentSessions.Add(session);
            await ctx.SaveChangesAsync();
            id = session.Id;
        }
        await using var readCtx = NewCtx();
        return await readCtx.AssessmentSessions.AsNoTracking().FirstAsync(s => s.Id == id);
    }

    // ── Test 1: FORM-02 standard build — retake config disalin penuh dari model + clamp ────────
    // Replika SHAPE object-init standard (:1467-1491 SETELAH fix): AllowRetake/MaxAttempts/
    // RetakeCooldownHours = model.* dengan Math.Clamp. model: AllowRetake=true, MaxAttempts=4,
    // RetakeCooldownHours=12 → reread harus identik (bukan EF default false/2/24).
    [Fact]
    public async Task Create_StandardBuild_PersistsRetakeConfig()
    {
        // model "form" (nilai yang diisi HC di Create standard).
        var model = new AssessmentSession { AllowRetake = true, MaxAttempts = 4, RetakeCooldownHours = 12 };

        // Replika object-init standard build controller SETELAH fix FORM-02 lokasi-1.
        var session = new AssessmentSession
        {
            Title = "FORM02-STD-" + Guid.NewGuid().ToString("N")[..8],
            Category = "Test",
            Status = "Open",
            AccessToken = "",
            ShuffleQuestions = model.ShuffleQuestions,
            ShuffleOptions = model.ShuffleOptions,
            AllowRetake = model.AllowRetake,                                  // FORM-02 std
            MaxAttempts = Math.Clamp(model.MaxAttempts, 1, 5),               // FORM-02 + clamp
            RetakeCooldownHours = Math.Clamp(model.RetakeCooldownHours, 0, 168),
            AssessmentType = "Standard"
        };

        var row = await PersistAndReadAsync(session);
        Assert.True(row.AllowRetake);
        Assert.Equal(4, row.MaxAttempts);
        Assert.Equal(12, row.RetakeCooldownHours);
    }

    // ── Test 2: FORM-02 Pre baseline — AllowRetake=false EKSPLISIT (D-03), retake disalin grup ──
    // Pre = baseline murni (D-03): AllowRetake=false eksplisit walau model.AllowRetake=true.
    // MaxAttempts/RetakeCooldownHours tetap disalin (konsistensi grup, perilaku OFF).
    [Fact]
    public async Task Create_PreBuild_PersistsBaselineRetakeOff()
    {
        var model = new AssessmentSession { AllowRetake = true, MaxAttempts = 4, RetakeCooldownHours = 12 };

        // Replika object-init Pre build controller SETELAH fix FORM-02 lokasi-2 (D-03).
        var preSession = new AssessmentSession
        {
            Title = "FORM02-PRE-" + Guid.NewGuid().ToString("N")[..8],
            Category = "Test",
            Status = "Upcoming",
            AccessToken = "",
            ShuffleQuestions = model.ShuffleQuestions,
            ShuffleOptions = model.ShuffleOptions,
            AllowRetake = false,                                             // D-03: Pre baseline, retake OFF eksplisit
            MaxAttempts = Math.Clamp(model.MaxAttempts, 1, 5),              // disalin untuk konsistensi grup
            RetakeCooldownHours = Math.Clamp(model.RetakeCooldownHours, 0, 168),
            GenerateCertificate = false,
            AssessmentType = "PreTest"
        };

        var row = await PersistAndReadAsync(preSession);
        Assert.False(row.AllowRetake);          // baseline OFF (D-03) walau model true
        Assert.Equal(4, row.MaxAttempts);       // tetap disalin (grup konsisten)
        Assert.Equal(12, row.RetakeCooldownHours);
    }

    // ── Test 3: FORM-02 Post build — retake disalin penuh + clamp (outlier → batas) ────────────
    // Post = relevan retake → salin dari model + clamp. model outlier: MaxAttempts=9 → 5;
    // RetakeCooldownHours=999 → 168.
    [Fact]
    public async Task Create_PostBuild_PersistsRetakeWithClamp()
    {
        var model = new AssessmentSession { AllowRetake = true, MaxAttempts = 9, RetakeCooldownHours = 999 };

        // Replika object-init Post build controller SETELAH fix FORM-02 lokasi-3.
        var postSession = new AssessmentSession
        {
            Title = "FORM02-POST-" + Guid.NewGuid().ToString("N")[..8],
            Category = "Test",
            Status = "Upcoming",
            AccessToken = "",
            ShuffleQuestions = model.ShuffleQuestions,
            ShuffleOptions = model.ShuffleOptions,
            AllowRetake = model.AllowRetake,                                 // FORM-02 Post
            MaxAttempts = Math.Clamp(model.MaxAttempts, 1, 5),              // 9 → 5
            RetakeCooldownHours = Math.Clamp(model.RetakeCooldownHours, 0, 168), // 999 → 168
            GenerateCertificate = model.GenerateCertificate,
            AssessmentType = "PostTest"
        };

        var row = await PersistAndReadAsync(postSession);
        Assert.True(row.AllowRetake);
        Assert.Equal(5, row.MaxAttempts);            // clamp upper
        Assert.Equal(168, row.RetakeCooldownHours);  // clamp upper
    }

    // ── Test 4: FORM-03 + FORM-04 Edit std loop — ValidUntil + retake×3 ke SEMUA sibling ───────
    // Seed 3 sibling (Title/Category/Schedule.Date sama) → replika Edit std loop (:2072-2089
    // SETELAH fix: set ValidUntil + retake×3 dengan clamp) → reread semua → assert SEMUA punya
    // ValidUntil & retake = nilai baru.
    [Fact]
    public async Task EditStdLoop_PersistsValidUntilAndRetakeToAllSiblings()
    {
        var marker = "FORM0304-" + Guid.NewGuid().ToString("N")[..8];
        var sched = new DateTime(2026, 5, 1, 8, 0, 0);

        // Seed 3 sibling (nilai awal kontras: AllowRetake=false, ValidUntil=null).
        await using (var ctx = NewCtx())
        {
            for (int i = 0; i < 3; i++)
            {
                var userId = await SeedUserAsync(ctx);
                ctx.AssessmentSessions.Add(new AssessmentSession
                {
                    UserId = userId,
                    Title = marker,
                    Category = "Test",
                    Status = "Open",
                    AccessToken = "",
                    Schedule = sched,
                    AllowRetake = false,
                    MaxAttempts = 2,
                    RetakeCooldownHours = 24,
                    ValidUntil = null
                });
                await ctx.SaveChangesAsync();
            }
        }

        // "model" Edit form: retake ON outlier + ValidUntil baru.
        var model = new AssessmentSession
        {
            AllowRetake = true,
            MaxAttempts = 9,                 // → clamp 5
            RetakeCooldownHours = 200,       // → clamp 168
            ValidUntil = new DateOnly(2027, 1, 31)  // model.ValidUntil = DateOnly? (TZ-01 v19.0 refactor)
        };

        // Replika body Edit std loop SETELAH fix FORM-03 + FORM-04.
        await using (var ctx = NewCtx())
        {
            var anchor = await ctx.AssessmentSessions.FirstAsync(s => s.Title == marker);
            var siblings = await ctx.AssessmentSessions
                .Where(a => a.Title == anchor.Title && a.Category == anchor.Category && a.Schedule.Date == anchor.Schedule.Date)
                .ToListAsync();
            var now = DateTime.UtcNow;
            foreach (var sibling in siblings)
            {
                sibling.ValidUntil = model.ValidUntil;                                       // FORM-04
                sibling.AllowRetake = model.AllowRetake;                                     // FORM-03
                sibling.MaxAttempts = Math.Clamp(model.MaxAttempts, 1, 5);                   // FORM-03 + clamp
                sibling.RetakeCooldownHours = Math.Clamp(model.RetakeCooldownHours, 0, 168); // FORM-03 + clamp
                sibling.UpdatedAt = now;
            }
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = NewCtx();
        var rows = await readCtx.AssessmentSessions.AsNoTracking().Where(s => s.Title == marker).ToListAsync();
        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.True(r.AllowRetake));                                   // FORM-03
        Assert.All(rows, r => Assert.Equal(5, r.MaxAttempts));                               // FORM-03 clamp
        Assert.All(rows, r => Assert.Equal(168, r.RetakeCooldownHours));                     // FORM-03 clamp
        Assert.All(rows, r => Assert.Equal(new DateOnly(2027, 1, 31), r.ValidUntil));        // FORM-04
    }

    // ── Test 5: FORM-01 invariant (regresi data-level) — shuffle write Edit benar ──────────────
    // Seed sesi ShuffleQuestions=true/ShuffleOptions=true → replika Edit std loop write shuffle
    // dari model.Shuffle*=true → reread → keduanya tetap true. Membuktikan WRITE benar; bug E-01
    // murni RENDER (checkbox absen di view → POST bind false), ditangkap e2e Plan 03.
    [Fact]
    public async Task EditStdLoop_ShuffleWrite_PersistsTrueWhenModelTrue()
    {
        var marker = "FORM01-" + Guid.NewGuid().ToString("N")[..8];
        var sched = new DateTime(2026, 5, 2, 8, 0, 0);

        await using (var ctx = NewCtx())
        {
            var userId = await SeedUserAsync(ctx);
            ctx.AssessmentSessions.Add(new AssessmentSession
            {
                UserId = userId,
                Title = marker,
                Category = "Test",
                Status = "Open",
                AccessToken = "",
                Schedule = sched,
                ShuffleQuestions = true,
                ShuffleOptions = true
            });
            await ctx.SaveChangesAsync();
        }

        // "model" Edit form: shuffle keduanya true (checkbox tercentang — render benar di view).
        var model = new AssessmentSession { ShuffleQuestions = true, ShuffleOptions = true };

        await using (var ctx = NewCtx())
        {
            var anchor = await ctx.AssessmentSessions.FirstAsync(s => s.Title == marker);
            var siblings = await ctx.AssessmentSessions
                .Where(a => a.Title == anchor.Title && a.Category == anchor.Category && a.Schedule.Date == anchor.Schedule.Date)
                .ToListAsync();
            foreach (var sibling in siblings)
            {
                sibling.ShuffleQuestions = model.ShuffleQuestions;   // write existing (:2084) — BENAR
                sibling.ShuffleOptions = model.ShuffleOptions;        // write existing (:2085) — BENAR
                sibling.UpdatedAt = DateTime.UtcNow;
            }
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = NewCtx();
        var row = await readCtx.AssessmentSessions.AsNoTracking().FirstAsync(s => s.Title == marker);
        Assert.True(row.ShuffleQuestions);   // tidak ter-reset OFF (write benar; akar E-01 = render)
        Assert.True(row.ShuffleOptions);
    }
}
