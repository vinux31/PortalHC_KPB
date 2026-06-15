// Phase 386 PXF-04 (F-04) — integration count-parity test: 4 fixture + upsert + status-guard.
//
// Tujuan: kunci agar 4 titik production "pending essay count" memakai predikat IDENTIK (byte-identik)
// SESUDAH fix Wave 3. Akar F-04: essay dikosongkan → dead-end finalize karena 4 titik hitung-pending
// divergen (sebagian pakai `EssayScore == null` saja → menghitung baris kosong sebagai pending yang
// tak akan pernah dinilai → tombol "Selesaikan" hilang / finalize ditolak selamanya).
//
// Predikat BARU (fixed) yang di-encode SEMUA mirror (D-05/D-06/D-06a, RESEARCH §B5):
//     !string.IsNullOrWhiteSpace(TextAnswer) && EssayScore == null
//   (whitespace-only TextAnswer = BUKAN pending; baris tak-ada = BUKAN pending; sudah dinilai = BUKAN pending.)
//
// 4 production predicate sites yang di-mirror (AssessmentAdminController.cs, verified 2026-06-15):
//   Site 1 (Page)        L3500       items.Count(i => i.EssayScore == null)            — in-memory, EssayGradingItemViewModel (question-domain; row absen → TextAnswer null)
//   Site 2 (Finalize)    L3620       essayResponses.Any(r => r.EssayScore == null)     — in-memory list response
//   Site 3 (Submit)      L3547-3551  Join Essay + .CountAsync(r => r.EssayScore == null) — EF
//   Site 4 (Monitoring)  L3308-3314  Join Essay + GroupBy + Where(r => r.EssayScore == null) — EF
// CATATAN: 4 site SAAT INI (pra-fix) hanya `EssayScore == null`. Mirror di sini encode predikat BARU →
// Wave 3 menyamakan production ke mirror. Wave 0: test ini GREEN di level mirror (4 mirror saling setuju
// pada 4 fixture); Wave 3 membuat controller nyata cocok.
//
// [Trait("Category","Integration")] → di-skip oleh `--filter "Category!=Integration"` (butuh SQLEXPRESS).
// Reuse EssayFinalizeRecomputeFixture (public, same assembly) — disposable real-SQL DB.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class EssayEmptyPendingParityTests : IClassFixture<EssayFinalizeRecomputeFixture>
{
    private readonly EssayFinalizeRecomputeFixture _fixture;
    public EssayEmptyPendingParityTests(EssayFinalizeRecomputeFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // 4 varian fixture mandatory (D-12). Mengontrol baris PackageUserResponse essay tunggal.
    public enum EssayRowVariant
    {
        NoRow,          // tidak ada PackageUserResponse sama sekali
        WhitespaceText, // TextAnswer whitespace-only (mis. "  " atau "\t\n"), EssayScore=null
        FilledUngraded, // TextAnswer berisi, EssayScore=null
        Graded          // TextAnswer berisi, EssayScore=80
    }

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "parity-" + Guid.NewGuid().ToString("N")[..8], Email = "parity@test.local", FullName = "Parity Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    /// <summary>
    /// Seed essay-only session (1 Essay question, Status=PendingGrading, HasManualGrading=true).
    /// Baris response tunggal dikontrol `variant`. `whitespaceText` override teks whitespace (uji "  " vs "\t\n").
    /// Helper LOKAL (bukan ubah SeedEssayOnlyAsync yang v30.0-locked) — parametrize row presence/teks.
    /// </summary>
    private static async Task<int> SeedEssayParityAsync(ApplicationDbContext ctx, string userId,
        EssayRowVariant variant, string status = AssessmentConstants.AssessmentStatus.PendingGrading,
        string whitespaceText = "  ", int scoreValue = 100)
    {
        var session = new AssessmentSession
        {
            UserId = userId, Title = "Essay Parity Exam", Category = "IHT", Status = status, AccessToken = "",
            Schedule = new DateTime(2026, 2, 1), Score = 0, HasManualGrading = true,
            PassPercentage = 70, NomorSertifikat = null, GenerateCertificate = true
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();

        var pkg = new AssessmentPackage { AssessmentSessionId = session.Id, PackageName = "Paket A", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();

        var q = new PackageQuestion { AssessmentPackageId = pkg.Id, QuestionType = "Essay", ScoreValue = scoreValue, Order = 1, QuestionText = "Jelaskan." };
        ctx.PackageQuestions.Add(q);
        await ctx.SaveChangesAsync();

        // Baris response sesuai varian
        switch (variant)
        {
            case EssayRowVariant.NoRow:
                break; // tidak menambah PackageUserResponse
            case EssayRowVariant.WhitespaceText:
                ctx.PackageUserResponses.Add(new PackageUserResponse
                { AssessmentSessionId = session.Id, PackageQuestionId = q.Id, TextAnswer = whitespaceText, EssayScore = null });
                break;
            case EssayRowVariant.FilledUngraded:
                ctx.PackageUserResponses.Add(new PackageUserResponse
                { AssessmentSessionId = session.Id, PackageQuestionId = q.Id, TextAnswer = "Jawaban peserta", EssayScore = null });
                break;
            case EssayRowVariant.Graded:
                ctx.PackageUserResponses.Add(new PackageUserResponse
                { AssessmentSessionId = session.Id, PackageQuestionId = q.Id, TextAnswer = "Jawaban peserta", EssayScore = 80 });
                break;
        }
        ctx.UserPackageAssignments.Add(new UserPackageAssignment
        {
            AssessmentSessionId = session.Id, AssessmentPackageId = pkg.Id, UserId = userId,
            ShuffledQuestionIds = JsonSerializer.Serialize(new List<int> { q.Id })
        });
        await ctx.SaveChangesAsync();
        return session.Id;
    }

    // ============================================================================================
    // 4 mirror count-builders — encode predikat BARU (fixed). Drift-guard WAJIB tiap builder.
    // ============================================================================================

    // Mirror SITE 4 (Monitoring, L3308-3314): EF Join Essay + GroupBy SessionId + Where predikat.
    // DRIFT-GUARD: bila body controller site 4 (L3308-3314) berubah, perbarui mirror ini.
    private static async Task<int> MonitoringPendingCountAsync(ApplicationDbContext ctx, int sessionId)
    {
        var raw = await ctx.PackageUserResponses
            .Where(r => r.AssessmentSessionId == sessionId
                        && !string.IsNullOrWhiteSpace(r.TextAnswer) && r.EssayScore == null)   // PREDIKAT BARU
            .Join(ctx.PackageQuestions.Where(q => q.QuestionType == "Essay"),
                r => r.PackageQuestionId, q => q.Id, (r, q) => r.AssessmentSessionId)
            .GroupBy(sid => sid)
            .Select(g => new { SessionId = g.Key, Count = g.Count() })
            .ToListAsync();
        return raw.FirstOrDefault()?.Count ?? 0;
    }

    // Mirror SITE 1 (Page, L3500): in-memory atas EssayGradingItemViewModel-shaped (question-domain).
    // Baris essay yang TIDAK ada di DB → TextAnswer null (IsNullOrWhiteSpace true) → BUKAN pending.
    // DRIFT-GUARD: bila body controller site 1 (L3486-3500) berubah, perbarui mirror ini.
    private static async Task<int> PagePendingCountAsync(ApplicationDbContext ctx, int sessionId)
    {
        var pa = await ctx.UserPackageAssignments.FirstAsync(a => a.AssessmentSessionId == sessionId);
        var shuffled = pa.GetShuffledQuestionIds();
        var essayQs = await ctx.PackageQuestions
            .Where(q => shuffled.Contains(q.Id) && q.QuestionType == "Essay").ToListAsync();
        var respMap = await ctx.PackageUserResponses
            .Where(r => r.AssessmentSessionId == sessionId && essayQs.Select(q => q.Id).Contains(r.PackageQuestionId))
            .ToDictionaryAsync(r => r.PackageQuestionId);
        var items = essayQs.Select(q => new EssayGradingItemViewModel
        {
            QuestionId = q.Id,
            TextAnswer = respMap.TryGetValue(q.Id, out var r) ? r.TextAnswer : null,    // row absen → null
            EssayScore = respMap.TryGetValue(q.Id, out var r2) ? r2.EssayScore : null
        }).ToList();
        return items.Count(i => !string.IsNullOrWhiteSpace(i.TextAnswer) && i.EssayScore == null);   // PREDIKAT BARU
    }

    // Mirror SITE 3 (Submit, L3547-3551): EF Join Essay + CountAsync predikat.
    // DRIFT-GUARD: bila body controller site 3 (L3547-3551) berubah, perbarui mirror ini.
    private static async Task<int> SubmitPendingCountAsync(ApplicationDbContext ctx, int sessionId)
    {
        return await ctx.PackageUserResponses
            .Where(r => r.AssessmentSessionId == sessionId)
            .Join(ctx.PackageQuestions.Where(q => q.QuestionType == "Essay"),
                r => r.PackageQuestionId, q => q.Id, (r, q) => r)
            .CountAsync(r => !string.IsNullOrWhiteSpace(r.TextAnswer) && r.EssayScore == null);   // PREDIKAT BARU
    }

    // Mirror SITE 2 (Finalize gate, L3620): in-memory atas essayResponses list .Count predikat.
    // DRIFT-GUARD: bila body controller site 2 (L3615-3620) berubah, perbarui mirror ini.
    private static async Task<int> FinalizeGatePendingCountAsync(ApplicationDbContext ctx, int sessionId)
    {
        var pa = await ctx.UserPackageAssignments.FirstAsync(a => a.AssessmentSessionId == sessionId);
        var shuffled = pa.GetShuffledQuestionIds();
        var essayQIds = await ctx.PackageQuestions
            .Where(q => shuffled.Contains(q.Id) && q.QuestionType == "Essay").Select(q => q.Id).ToListAsync();
        var essayResponses = await ctx.PackageUserResponses
            .Where(r => r.AssessmentSessionId == sessionId && essayQIds.Contains(r.PackageQuestionId)).ToListAsync();
        return essayResponses.Count(r => !string.IsNullOrWhiteSpace(r.TextAnswer) && r.EssayScore == null);   // PREDIKAT BARU
    }

    // Helper: jalankan keempat mirror, kembalikan array [Monitoring, Page, Submit, FinalizeGate].
    private async Task<int[]> AllFourCountsAsync(int sessionId)
    {
        await using var ctx = NewCtx();
        return new[]
        {
            await MonitoringPendingCountAsync(ctx, sessionId),
            await PagePendingCountAsync(ctx, sessionId),
            await SubmitPendingCountAsync(ctx, sessionId),
            await FinalizeGatePendingCountAsync(ctx, sessionId),
        };
    }

    // Mirror SITE Submit upsert (Wave 3 D-08): NoRow → create row + score. Status-guard reject non-PendingGrading.
    // DRIFT-GUARD: mirror Wave-3 SubmitEssayScore upsert (saat ini production hanya FirstOrDefault → reject NoRow).
    private static async Task<(bool success, string? message)> MirrorSubmitUpsertAsync(
        ApplicationDbContext ctx, int sessionId, int questionId, int score)
    {
        // Status-guard (D-08, T-386-AUTHZ): hanya PendingGrading boleh dinilai.
        var session = await ctx.AssessmentSessions.FindAsync(sessionId);
        if (session == null) return (false, "Session tidak ditemukan");
        if (session.Status != AssessmentConstants.AssessmentStatus.PendingGrading)
            return (false, "Status sesi tidak mengizinkan penilaian");
        var question = await ctx.PackageQuestions.FindAsync(questionId);
        if (question == null) return (false, "Soal tidak ditemukan");
        if (score < 0 || score > question.ScoreValue) return (false, $"Skor harus antara 0 dan {question.ScoreValue}");
        // UPSERT (D-08): baris tak ada → buat baru (TextAnswer null), lalu set EssayScore.
        var response = await ctx.PackageUserResponses
            .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == questionId);
        if (response == null)
        {
            response = new PackageUserResponse { AssessmentSessionId = sessionId, PackageQuestionId = questionId, TextAnswer = null };
            ctx.PackageUserResponses.Add(response);
        }
        response.EssayScore = score;
        await ctx.SaveChangesAsync();
        return (true, null);
    }

    private static async Task<PackageQuestion> QuestionOfSessionAsync(ApplicationDbContext ctx, int sessionId)
    {
        var pkgIds = await ctx.AssessmentPackages.Where(p => p.AssessmentSessionId == sessionId).Select(p => p.Id).ToListAsync();
        return await ctx.PackageQuestions.FirstAsync(q => pkgIds.Contains(q.AssessmentPackageId));
    }

    // ============================================================================================
    // [Fact] count-parity 4 fixture
    // ============================================================================================

    [Fact]
    public async Task PendingCount_IdenticalAcrossAllFourSites_NoRow()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedEssayParityAsync(ctx, userId, EssayRowVariant.NoRow);

        var counts = await AllFourCountsAsync(sessionId);

        Assert.All(counts, c => Assert.Equal(0, c));   // baris tak ada → BUKAN pending (4 site setuju)
    }

    [Fact]
    public async Task PendingCount_IdenticalAcrossAllFourSites_WhitespaceText()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        // varian "  "
        var sessionSpaces = await SeedEssayParityAsync(ctx, userId, EssayRowVariant.WhitespaceText, whitespaceText: "  ");
        // varian "\t\n"
        var sessionTabNl = await SeedEssayParityAsync(ctx, userId, EssayRowVariant.WhitespaceText, whitespaceText: "\t\n");

        var countsSpaces = await AllFourCountsAsync(sessionSpaces);
        var countsTabNl = await AllFourCountsAsync(sessionTabNl);

        // whitespace = BUKAN pending (D-05). EF site & in-memory site WAJIB setuju (validasi nuansa A2).
        Assert.All(countsSpaces, c => Assert.Equal(0, c));
        Assert.All(countsTabNl, c => Assert.Equal(0, c));
    }

    [Fact]
    public async Task PendingCount_IdenticalAcrossAllFourSites_FilledUngraded()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedEssayParityAsync(ctx, userId, EssayRowVariant.FilledUngraded);

        var counts = await AllFourCountsAsync(sessionId);

        Assert.All(counts, c => Assert.Equal(1, c));   // berisi + belum dinilai → pending (4 site setuju)
    }

    [Fact]
    public async Task PendingCount_IdenticalAcrossAllFourSites_Graded()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedEssayParityAsync(ctx, userId, EssayRowVariant.Graded);

        var counts = await AllFourCountsAsync(sessionId);

        Assert.All(counts, c => Assert.Equal(0, c));   // sudah dinilai (EssayScore=80) → BUKAN pending
    }

    // ============================================================================================
    // [Fact] upsert + status-guard (D-08)
    // ============================================================================================

    [Fact]
    public async Task SubmitEssayScore_NoRow_UpsertCreatesRowAndScores()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedEssayParityAsync(ctx, userId, EssayRowVariant.NoRow);
        var q = await QuestionOfSessionAsync(ctx, sessionId);

        var (ok, _) = await MirrorSubmitUpsertAsync(ctx, sessionId, q.Id, 80);

        Assert.True(ok);
        await using var verify = NewCtx();
        var resp = await verify.PackageUserResponses.FirstAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == q.Id);
        Assert.Equal(80, resp.EssayScore);   // baris baru dibuat + skor (D-08 upsert)
        Assert.Null(resp.TextAnswer);        // upsert tak mengarang teks jawaban
    }

    [Fact]
    public async Task SubmitEssayScore_NonPendingGrading_Rejected()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        // sesi Completed → status-guard tolak (D-08, tutup F-03 widening / T-386-AUTHZ)
        var sessionId = await SeedEssayParityAsync(ctx, userId, EssayRowVariant.FilledUngraded,
            status: AssessmentConstants.AssessmentStatus.Completed);
        var q = await QuestionOfSessionAsync(ctx, sessionId);

        var (ok, _) = await MirrorSubmitUpsertAsync(ctx, sessionId, q.Id, 80);

        Assert.False(ok);   // ditolak status-guard
        await using var verify = NewCtx();
        var resp = await verify.PackageUserResponses.FirstAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == q.Id);
        Assert.Null(resp.EssayScore);   // tak ada baris yang dimutasi
    }
}
