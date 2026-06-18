using System;
using System.Collections.Generic;
using System.Linq;
using HcPortal.Helpers;
using HcPortal.Models;
using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 395 INJ-09 — pure unit tests untuk <see cref="InjectAssessmentService.BuildAutoGenAnswers"/>
/// dan <see cref="InjectAssessmentService.ComputeAutoGenSeed"/>.
///
/// TANPA DB, TANPA fixture, TANPA [Trait("Category","Integration")] → masuk fast suite.
/// Pola in-memory builder dari <see cref="AssessmentScoreAggregatorTests"/>.
///
/// "Skor >= target" dibuktikan dengan memetakan pola usulan (AutoGenResult.Answers) + soal
/// ke in-memory PackageQuestion/PackageUserResponse (TempId = Id sintetis), lalu memanggil
/// <see cref="AssessmentScoreAggregator.Compute"/> — engine identik commit (preview == commit).
/// Truncation int + lulus '>=' (formula AssessmentScoreAggregator.cs:58-59) adalah sumber kebenaran.
///
/// ComputeAutoGenSeed WAJIB deterministik lintas-proses (SHA-256, BUKAN string.GetHashCode()).
/// </summary>
public class BuildAutoGenAnswersTests
{
    // ---- in-memory builders (no DB) ----

    /// <summary>Bangun InjectQuestionSpec. opts: (optTempId, isCorrect). Type kosong default MultipleChoice.</summary>
    private static InjectQuestionSpec Q(int tempId, string type, int sv, int order,
        params (int optTemp, bool correct)[] opts) =>
        new InjectQuestionSpec
        {
            TempId = tempId,
            QuestionType = type,
            ScoreValue = sv,
            Order = order,
            Options = opts.Select(o => new InjectOptionSpec { TempId = o.optTemp, IsCorrect = o.correct }).ToList()
        };

    /// <summary>MC standar 2 opsi: opsi (tempId*10+1)=benar, (tempId*10+2)=salah.</summary>
    private static InjectQuestionSpec Mc(int tempId, int sv = 10, int? order = null) =>
        Q(tempId, "MultipleChoice", sv, order ?? tempId, (tempId * 10 + 1, true), (tempId * 10 + 2, false));

    /// <summary>MA standar 3 opsi: (tempId*10+1)=benar, (tempId*10+2)=benar, (tempId*10+3)=salah.</summary>
    private static InjectQuestionSpec Ma(int tempId, int sv = 10, int? order = null) =>
        Q(tempId, "MultipleAnswer", sv, order ?? tempId,
            (tempId * 10 + 1, true), (tempId * 10 + 2, true), (tempId * 10 + 3, false));

    private static InjectQuestionSpec Essay(int tempId, int sv, int? order = null) =>
        new InjectQuestionSpec { TempId = tempId, QuestionType = "Essay", ScoreValue = sv, Order = order ?? tempId };

    // ---- mapper: pola usulan + soal → in-memory PackageQuestion/Response → Compute (preview == commit) ----
    private static ScoreAggregateResult GradePattern(
        IReadOnlyList<InjectQuestionSpec> questions,
        IEnumerable<InjectAnswerSpec> answers,
        int passPercentage)
    {
        var qInMem = questions.Select(q => new PackageQuestion
        {
            Id = q.TempId,
            QuestionType = q.QuestionType,
            ScoreValue = q.ScoreValue,
            Options = q.Options.Select(o => new PackageOption { Id = o.TempId, IsCorrect = o.IsCorrect }).ToList()
        }).ToList();

        var byTemp = questions.ToDictionary(q => q.TempId);
        var respInMem = new List<PackageUserResponse>();
        foreach (var a in answers)
        {
            var q = byTemp[a.QuestionTempId];
            if ((q.QuestionType ?? "MultipleChoice") == "Essay")
                respInMem.Add(new PackageUserResponse { PackageQuestionId = q.TempId, EssayScore = a.EssayScore, TextAnswer = a.TextAnswer });
            else
                foreach (var optTemp in a.SelectedOptionTempIds)
                    respInMem.Add(new PackageUserResponse { PackageQuestionId = q.TempId, PackageOptionId = optTemp });
        }
        return AssessmentScoreAggregator.Compute(qInMem, respInMem, passPercentage);
    }

    // =========================================================================
    // HIT-TARGET
    // =========================================================================

    [Fact] // equal-weight: N=10 MC SV=10, target=80 → tepat k=8 benar → Percentage == 80 (smallest-such >= target)
    public void HitTarget_EqualWeight_Target80_Exactly80()
    {
        var questions = Enumerable.Range(1, 10).Select(i => Mc(i)).ToList();
        int seed = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul", "Kat", new DateTime(2026, 6, 1), 80);

        var res = InjectAssessmentService.BuildAutoGenAnswers(questions, 80, seed);

        Assert.True(res.TargetReachable);
        var agg = GradePattern(questions, res.Answers, passPercentage: 70);
        Assert.Equal(80, agg.Percentage); // smallest-such >= 80
    }

    [Fact] // smallest-such: target=75 (N=10 equal) → k=8 (floor(70)<75; floor(80)>=75) → 80, BUKAN 90
    public void HitTarget_EqualWeight_Target75_Returns80_NotOvershootTo90()
    {
        var questions = Enumerable.Range(1, 10).Select(i => Mc(i)).ToList();
        int seed = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul", "Kat", new DateTime(2026, 6, 1), 75);

        var res = InjectAssessmentService.BuildAutoGenAnswers(questions, 75, seed);

        var agg = GradePattern(questions, res.Answers, passPercentage: 70);
        Assert.Equal(80, agg.Percentage); // smallest k with floor>=75 is k=8 → 80, not 90
    }

    [Theory] // berbagai target equal-weight → selalu >= target (re-cek floor)
    [InlineData(50)]
    [InlineData(60)]
    [InlineData(70)]
    [InlineData(90)]
    [InlineData(100)]
    public void HitTarget_EqualWeight_VariousTargets_AtLeastTarget(int target)
    {
        var questions = Enumerable.Range(1, 10).Select(i => Mc(i)).ToList();
        int seed = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul", "Kat", new DateTime(2026, 6, 1), target);

        var res = InjectAssessmentService.BuildAutoGenAnswers(questions, target, seed);

        Assert.True(res.TargetReachable);
        var agg = GradePattern(questions, res.Answers, passPercentage: 70);
        Assert.True(agg.Percentage >= target, $"Percentage {agg.Percentage} < target {target}");
    }

    // =========================================================================
    // BOUNDARY off-by-one — MIXED WEIGHT (re-cek floor setelah seleksi, bukan asumsi k)
    // =========================================================================

    [Theory] // campur ScoreValue 10,10,10,20,20,30 (Σ=100) → hasil >= target untuk berbagai target
    [InlineData(40)]
    [InlineData(50)]
    [InlineData(55)]
    [InlineData(60)]
    [InlineData(70)]
    [InlineData(80)]
    public void Boundary_MixedWeight_AtLeastTarget(int target)
    {
        var questions = new List<InjectQuestionSpec>
        {
            Mc(1, 10), Mc(2, 10), Mc(3, 10), Mc(4, 20), Mc(5, 20), Mc(6, 30)
        };
        int seed = InjectAssessmentService.ComputeAutoGenSeed("777", "Mix", "K", new DateTime(2026, 6, 2), target);

        var res = InjectAssessmentService.BuildAutoGenAnswers(questions, target, seed);

        Assert.True(res.TargetReachable);
        var agg = GradePattern(questions, res.Answers, passPercentage: 70);
        Assert.True(agg.Percentage >= target, $"Percentage {agg.Percentage} < target {target}");
    }

    [Fact] // mixed: MA + MC tercampur SV beda → tetap >= target
    public void Boundary_MixedTypeAndWeight_AtLeastTarget()
    {
        var questions = new List<InjectQuestionSpec>
        {
            Mc(1, 10), Ma(2, 20), Mc(3, 30), Ma(4, 40)
        };
        int seed = InjectAssessmentService.ComputeAutoGenSeed("888", "MixType", "K", new DateTime(2026, 6, 3), 60);

        var res = InjectAssessmentService.BuildAutoGenAnswers(questions, 60, seed);

        Assert.True(res.TargetReachable);
        var agg = GradePattern(questions, res.Answers, passPercentage: 70);
        Assert.True(agg.Percentage >= 60, $"Percentage {agg.Percentage} < 60");
    }

    // =========================================================================
    // CEILING ESSAY (D-08.3) — target > ceiling MC/MA-only → TargetReachable=false (TIDAK di-cap)
    // =========================================================================

    [Fact] // 2 MC (SV=10) + 1 essay (SV=80); maxScore=100; ceiling MC/MA = floor(20/100*100)=20; target=50 > 20 → false
    public void EssayCeiling_TargetAboveCeiling_TargetReachableFalse()
    {
        var questions = new List<InjectQuestionSpec>
        {
            Mc(1, 10), Mc(2, 10), Essay(3, 80)
        };
        int seed = InjectAssessmentService.ComputeAutoGenSeed("999", "Essay", "K", new DateTime(2026, 6, 4), 50);

        var res = InjectAssessmentService.BuildAutoGenAnswers(questions, 50, seed);

        Assert.Equal(20, res.CeilingPercent);
        Assert.Equal(100, res.MaxScoreIncludingEssay);
        Assert.False(res.TargetReachable); // target 50 > ceiling 20 → BLOCKING (jangan cap diam-diam)
    }

    [Fact] // target == ceiling → reachable=true (best-effort semua MC benar)
    public void EssayCeiling_TargetAtCeiling_TargetReachableTrue()
    {
        var questions = new List<InjectQuestionSpec>
        {
            Mc(1, 10), Mc(2, 10), Essay(3, 80)
        };
        int seed = InjectAssessmentService.ComputeAutoGenSeed("999", "Essay", "K", new DateTime(2026, 6, 4), 20);

        var res = InjectAssessmentService.BuildAutoGenAnswers(questions, 20, seed);

        Assert.Equal(20, res.CeilingPercent);
        Assert.True(res.TargetReachable);
    }

    // =========================================================================
    // ESSAY TIDAK DISENTUH (D-08) — auto-gen hanya MC/MA
    // =========================================================================

    [Fact] // Answers TIDAK pernah memuat QuestionTempId milik soal Essay
    public void Essay_NeverEmittedInAnswers()
    {
        var questions = new List<InjectQuestionSpec>
        {
            Mc(1, 10), Mc(2, 10), Essay(3, 10), Essay(4, 10)
        };
        int seed = InjectAssessmentService.ComputeAutoGenSeed("123", "E", "K", new DateTime(2026, 6, 5), 50);

        var res = InjectAssessmentService.BuildAutoGenAnswers(questions, 50, seed);

        var essayTempIds = new HashSet<int> { 3, 4 };
        Assert.DoesNotContain(res.Answers, a => essayTempIds.Contains(a.QuestionTempId));
        // semua soal MC/MA harus ter-emit (benar/salah eksplisit)
        Assert.Contains(res.Answers, a => a.QuestionTempId == 1);
        Assert.Contains(res.Answers, a => a.QuestionTempId == 2);
    }

    // =========================================================================
    // SEED — reproducible & komposisi (NIP + identitas room + target); NIP-saja DITOLAK
    // =========================================================================

    [Fact] // input sama → seed identik (deterministik lintas-proses)
    public void Seed_SameInput_SameSeed()
    {
        int s1 = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul", "Kat", new DateTime(2026, 6, 1), 80);
        int s2 = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul", "Kat", new DateTime(2026, 6, 1), 80);
        Assert.Equal(s1, s2);
    }

    [Fact] // seed selalu non-negatif (untuk new Random(seed))
    public void Seed_NonNegative()
    {
        for (int i = 0; i < 50; i++)
        {
            int s = InjectAssessmentService.ComputeAutoGenSeed("nip" + i, "T" + i, "C" + i, new DateTime(2026, 6, 1).AddDays(i), 50 + i);
            Assert.True(s >= 0, $"seed {s} negatif");
        }
    }

    [Fact] // ganti NIP → seed berbeda
    public void Seed_DifferentNip_DifferentSeed()
    {
        int s1 = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul", "Kat", new DateTime(2026, 6, 1), 80);
        int s2 = InjectAssessmentService.ComputeAutoGenSeed("456", "Judul", "Kat", new DateTime(2026, 6, 1), 80);
        Assert.NotEqual(s1, s2);
    }

    [Fact] // ganti Title → seed berbeda (NIP-saja DITOLAK: room identity ikut)
    public void Seed_DifferentTitle_DifferentSeed()
    {
        int s1 = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul A", "Kat", new DateTime(2026, 6, 1), 80);
        int s2 = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul B", "Kat", new DateTime(2026, 6, 1), 80);
        Assert.NotEqual(s1, s2);
    }

    [Fact] // ganti Category → seed berbeda
    public void Seed_DifferentCategory_DifferentSeed()
    {
        int s1 = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul", "Kat A", new DateTime(2026, 6, 1), 80);
        int s2 = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul", "Kat B", new DateTime(2026, 6, 1), 80);
        Assert.NotEqual(s1, s2);
    }

    [Fact] // ganti CompletedAt (tanggal) → seed berbeda
    public void Seed_DifferentCompletedAt_DifferentSeed()
    {
        int s1 = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul", "Kat", new DateTime(2026, 6, 1), 80);
        int s2 = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul", "Kat", new DateTime(2026, 6, 2), 80);
        Assert.NotEqual(s1, s2);
    }

    [Fact] // beda jam dalam tanggal yang SAMA → seed identik (CompletedAt hanya yyyy-MM-dd; preview vs commit)
    public void Seed_SameDateDifferentTime_SameSeed()
    {
        int s1 = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul", "Kat", new DateTime(2026, 6, 1, 8, 0, 0), 80);
        int s2 = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul", "Kat", new DateTime(2026, 6, 1, 17, 30, 0), 80);
        Assert.Equal(s1, s2);
    }

    [Fact] // ganti target → seed berbeda
    public void Seed_DifferentTarget_DifferentSeed()
    {
        int s1 = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul", "Kat", new DateTime(2026, 6, 1), 80);
        int s2 = InjectAssessmentService.ComputeAutoGenSeed("123", "Judul", "Kat", new DateTime(2026, 6, 1), 81);
        Assert.NotEqual(s1, s2);
    }

    // =========================================================================
    // SEED → POLA: dua worker beda di room sama → himpunan soal-benar BERBEDA, keduanya >= target
    // =========================================================================

    [Fact]
    public void SeedToPattern_DifferentWorkersSameRoom_DifferentCorrectSet_BothMeetTarget()
    {
        // N=10 MC equal-weight, target=60 → k=6 benar. Cukup besar untuk variasi pola antar pekerja.
        var questions = Enumerable.Range(1, 10).Select(i => Mc(i)).ToList();
        int seedA = InjectAssessmentService.ComputeAutoGenSeed("111", "Judul", "Kat", new DateTime(2026, 6, 1), 60);
        int seedB = InjectAssessmentService.ComputeAutoGenSeed("222", "Judul", "Kat", new DateTime(2026, 6, 1), 60);

        var resA = InjectAssessmentService.BuildAutoGenAnswers(questions, 60, seedA);
        var resB = InjectAssessmentService.BuildAutoGenAnswers(questions, 60, seedB);

        // himpunan soal yang dijawab benar
        var correctA = CorrectQuestionTempIds(questions, resA.Answers);
        var correctB = CorrectQuestionTempIds(questions, resB.Answers);
        Assert.False(correctA.SetEquals(correctB), "pola dua worker di room sama identik (seharusnya bervariasi)");

        // keduanya tetap >= target
        Assert.True(GradePattern(questions, resA.Answers, 70).Percentage >= 60);
        Assert.True(GradePattern(questions, resB.Answers, 70).Percentage >= 60);
    }

    /// <summary>Himpunan TempId soal yang pola-nya di-grade BENAR (menyumbang poin).</summary>
    private static HashSet<int> CorrectQuestionTempIds(
        IReadOnlyList<InjectQuestionSpec> questions, IEnumerable<InjectAnswerSpec> answers)
    {
        var byTemp = questions.ToDictionary(q => q.TempId);
        var correct = new HashSet<int>();
        foreach (var a in answers)
        {
            var q = byTemp[a.QuestionTempId];
            var correctOpts = q.Options.Where(o => o.IsCorrect).Select(o => o.TempId).ToHashSet();
            var sel = a.SelectedOptionTempIds.ToHashSet();
            if ((q.QuestionType ?? "MultipleChoice") == "MultipleChoice")
            {
                if (sel.Count == 1 && correctOpts.Contains(sel.First())) correct.Add(q.TempId);
            }
            else if (q.QuestionType == "MultipleAnswer")
            {
                if (sel.SetEquals(correctOpts)) correct.Add(q.TempId);
            }
        }
        return correct;
    }

    // =========================================================================
    // DEGENERATE option-set — forced-correct & MA salah
    // =========================================================================

    [Fact] // MC dengan SEMUA opsi IsCorrect → tak bisa dibuat salah → forced-correct (selalu menyumbang poin)
    public void Degenerate_McAllOptionsCorrect_ForcedCorrect()
    {
        // 1 soal MC degenerate (2 opsi keduanya benar) + 4 MC normal. target=20 → harus reachable.
        var questions = new List<InjectQuestionSpec>
        {
            Q(1, "MultipleChoice", 10, 1, (11, true), (12, true)), // degenerate all-correct
            Mc(2), Mc(3), Mc(4), Mc(5)
        };
        int seed = InjectAssessmentService.ComputeAutoGenSeed("123", "Deg", "K", new DateTime(2026, 6, 6), 20);

        var res = InjectAssessmentService.BuildAutoGenAnswers(questions, 20, seed);

        // soal 1 selalu di-grade benar apa pun opsi yang dipilih → konfirmasi opsinya valid (1 opsi terpilih)
        var ans1 = res.Answers.First(a => a.QuestionTempId == 1);
        Assert.Single(ans1.SelectedOptionTempIds);
        var agg = GradePattern(questions, res.Answers, 70);
        Assert.True(agg.Percentage >= 20);
    }

    [Fact] // MC degenerate all-correct membuat ceiling minimum > 0: target sangat rendah tetap reachable
    public void Degenerate_McAllCorrect_ContributesPoints()
    {
        // 1 soal all-correct SV=10 + 1 essay SV=10 → maxScore=20; MC ceiling=floor(10/20*100)=50.
        var questions = new List<InjectQuestionSpec>
        {
            Q(1, "MultipleChoice", 10, 1, (11, true), (12, true)),
            Essay(2, 10)
        };
        int seed = InjectAssessmentService.ComputeAutoGenSeed("123", "Deg2", "K", new DateTime(2026, 6, 6), 50);

        var res = InjectAssessmentService.BuildAutoGenAnswers(questions, 50, seed);

        Assert.Equal(50, res.CeilingPercent);
        Assert.True(res.TargetReachable);
        // soal forced-correct menyumbang poin → skor >= 50
        var agg = GradePattern(questions, res.Answers, 70);
        Assert.True(agg.Percentage >= 50);
    }

    [Fact] // MC 1 opsi saja → forced-correct
    public void Degenerate_McSingleOption_ForcedCorrect()
    {
        var questions = new List<InjectQuestionSpec>
        {
            Q(1, "MultipleChoice", 10, 1, (11, true)), // 1 opsi saja
            Mc(2), Mc(3), Mc(4)
        };
        int seed = InjectAssessmentService.ComputeAutoGenSeed("123", "Deg3", "K", new DateTime(2026, 6, 6), 25);

        var res = InjectAssessmentService.BuildAutoGenAnswers(questions, 25, seed);

        var ans1 = res.Answers.First(a => a.QuestionTempId == 1);
        Assert.Equal(new List<int> { 11 }, ans1.SelectedOptionTempIds);
        var agg = GradePattern(questions, res.Answers, 70);
        Assert.True(agg.Percentage >= 25);
    }

    [Fact] // MA (>=2 opsi, ada opsi salah) dipilih "salah" → SetEquals(correct)=false saat di-grade (0 poin)
    public void MaWrong_SetEqualsFalse_ContributesNoPoints()
    {
        // 1 MA SV=50 + 1 MC SV=50. target=50 → MC benar cukup (50%), MA dibuat salah.
        var questions = new List<InjectQuestionSpec>
        {
            Ma(1, 50), Mc(2, 50)
        };
        int seed = InjectAssessmentService.ComputeAutoGenSeed("123", "MaWrong", "K", new DateTime(2026, 6, 7), 50);

        var res = InjectAssessmentService.BuildAutoGenAnswers(questions, 50, seed);

        var agg = GradePattern(questions, res.Answers, 70);
        Assert.True(agg.Percentage >= 50);

        // bila MA dibuat salah, pola MA-nya bukan SetEquals(correct)
        var maAns = res.Answers.First(a => a.QuestionTempId == 1);
        var maCorrect = questions[0].Options.Where(o => o.IsCorrect).Select(o => o.TempId).ToHashSet();
        var maSel = maAns.SelectedOptionTempIds.ToHashSet();
        bool maGradedCorrect = maSel.SetEquals(maCorrect);
        // dengan target 50 dan dua soal SV=50, salah satu dibuat salah; konsistensi: skor tepat sesuai grading
        int expectedFromCorrect = (maGradedCorrect ? 50 : 0)
            + (IsMcGradedCorrect(questions[1], res.Answers.First(a => a.QuestionTempId == 2)) ? 50 : 0);
        Assert.Equal((int)((double)expectedFromCorrect / 100 * 100), agg.Percentage);
    }

    private static bool IsMcGradedCorrect(InjectQuestionSpec q, InjectAnswerSpec a)
    {
        var correctOpts = q.Options.Where(o => o.IsCorrect).Select(o => o.TempId).ToHashSet();
        return a.SelectedOptionTempIds.Count == 1 && correctOpts.Contains(a.SelectedOptionTempIds[0]);
    }
}
