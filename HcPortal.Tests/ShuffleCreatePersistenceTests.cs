using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 372 SHUF-02 — real-SQL: flag shuffle dari "model" form tersimpan EKSPLISIT di baris baru
/// (ON default DAN OFF eksplisit), tidak kena EF bool-false trap dan tidak dipaksa DB-DEFAULT.
///
/// Pendekatan: data/persistence level. `CreateFromModel` mereplikasi shape object-init create-loop
/// controller VERBATIM (`ShuffleQuestions = model.ShuffleQuestions, ShuffleOptions = model.ShuffleOptions`).
/// Wiring 8 lokasi di controller di-cover terpisah oleh anchored-insertion (anchor `AllowAnswerReview`)
/// + grep count >= 8 + build hijau (372-02 Task 1). Test ini membuktikan INVARIANT data: EF persist
/// nilai eksplisit (incl. false) lewat real SQL Server — yang InMemory tak bisa jamin terhadap DB DEFAULT.
///
/// [Trait("Category","Integration")] → CI SQL-less skip via dotnet test --filter "Category!=Integration".
/// </summary>
[Trait("Category", "Integration")]
public class ShuffleCreatePersistenceTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public ShuffleCreatePersistenceTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var user = new ApplicationUser
        {
            UserName = "shufcreate-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Email = "shufcreate@test.local",
            FullName = "Shuffle Create Test"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user.Id;
    }

    // Replika VERBATIM shape object-init create-loop controller (AssessmentAdminController create loops):
    //   new AssessmentSession { ..., ShuffleQuestions = model.ShuffleQuestions, ShuffleOptions = model.ShuffleOptions }
    private static AssessmentSession CreateFromModel(string userId, AssessmentSession model, string title) => new AssessmentSession
    {
        UserId = userId,
        Title = title,
        Category = "Test",
        Status = "Open",
        AccessToken = "",
        ShuffleQuestions = model.ShuffleQuestions,
        ShuffleOptions = model.ShuffleOptions
    };

    private async Task<bool[]> PersistAndReadAsync(AssessmentSession model, string title)
    {
        int id;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var userId = await SeedUserAsync(ctx);
            var session = CreateFromModel(userId, model, title);
            ctx.AssessmentSessions.Add(session);
            await ctx.SaveChangesAsync();
            id = session.Id;
        }
        await using var readCtx = new ApplicationDbContext(_fixture.Options);
        var row = await readCtx.AssessmentSessions.AsNoTracking().FirstAsync(s => s.Id == id);
        return new[] { row.ShuffleQuestions, row.ShuffleOptions };
    }

    // SHUF-02a — model ON (default checked) → baris baru tersimpan true/true.
    [Fact]
    public async Task Create_ModelOn_PersistsTrue()
    {
        var model = new AssessmentSession { ShuffleQuestions = true, ShuffleOptions = true };
        var read = await PersistAndReadAsync(model, "SHUF-02a-ON");
        Assert.True(read[0]);
        Assert.True(read[1]);
    }

    // SHUF-02b — model OFF (checkbox unchecked) → tersimpan false/false (BUKAN dipaksa true; anti EF bool-trap + DB DEFAULT).
    [Fact]
    public async Task Create_ModelOff_PersistsFalse_NotForcedTrue()
    {
        var model = new AssessmentSession { ShuffleQuestions = false, ShuffleOptions = false };
        var read = await PersistAndReadAsync(model, "SHUF-02b-OFF");
        Assert.False(read[0]);
        Assert.False(read[1]);
    }

    // SHUF-02c — independensi (D-10): Acak Soal ON + Acak Pilihan OFF round-trip beda nilai tetap.
    [Fact]
    public async Task Create_FlagsIndependent_RoundTrip()
    {
        var model = new AssessmentSession { ShuffleQuestions = true, ShuffleOptions = false };
        var read = await PersistAndReadAsync(model, "SHUF-02c-INDEP");
        Assert.True(read[0]);
        Assert.False(read[1]);
    }
}
