// Phase 386 PXF-05 (F-17) — pure unit test untuk AssessmentScoreAggregator.BuildAnswerCell (RED Wave 0)
// + regression IsQuestionCorrect MA (helper existing, REUSE as-is).
//
// RED Wave 0: method BuildAnswerCell BELUM ADA — ditambahkan Wave 1 (Plan 02) di file
// Helpers/AssessmentScoreAggregator.cs (beside IsQuestionCorrect). File ini SENGAJA tidak compile
// sampai Wave 1, gagal dengan CS0117 mereferensikan symbol `BuildAnswerCell` (RED yang diharapkan).
//
// Signature target (dari <interfaces> PLAN 386-01):
//   public static string BuildAnswerCell(PackageQuestion q, IEnumerable<PackageUserResponse> responsesForQ);
//     MA    -> join SEMUA OptionText terpilih (urut Id), join ", " (comma-space); "—" bila kosong
//     MC    -> OptionText tunggal terpilih; "—" bila kosong
//     Essay -> TextAnswer truncate 300 + "..."; "—" bila kosong
//
// Format join MA = ", " (comma-space) — keputusan D-10 (preseden Excel L4860). Wave 1 WAJIB cocok format ini.
// Em dash kosong = "—" (U+2014), konsisten dgn placeholder "tidak ada jawaban" di surface display existing.
//
// Pure xUnit: TANPA [Trait] → selalu jalan. Builder Q/Resp di-COPY dari IsQuestionCorrectTests:23-33,
// DIEXTEND: PackageOption bawa OptionText, PackageUserResponse bawa TextAnswer (param `text`).
using System.Collections.Generic;
using System.Linq;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class PdfAnswerCellTests
{
    // ---- in-memory builders (no DB) — extend IsQuestionCorrectTests:23-33 dgn OptionText + TextAnswer ----
    private static PackageQuestion Q(int id, string type, int scoreValue, params (int optId, bool correct, string text)[] opts) =>
        new PackageQuestion
        {
            Id = id,
            QuestionType = type,
            ScoreValue = scoreValue,
            Options = opts.Select(o => new PackageOption { Id = o.optId, IsCorrect = o.correct, OptionText = o.text }).ToList()
        };

    private static PackageUserResponse Resp(int qId, int? optId = null, int? essay = null, string? text = null) =>
        new PackageUserResponse { PackageQuestionId = qId, PackageOptionId = optId, EssayScore = essay, TextAnswer = text };

    // ============ MultipleAnswer (3) ============

    // MA exact set: 2 opsi benar persis terpilih → IsQuestionCorrect true; BuildAnswerCell join SEMUA terpilih (urut Id).
    [Fact]
    public void MultipleAnswer_ExactSet_CorrectAndAllOptionsJoined()
    {
        var q = Q(1, "MultipleAnswer", 100, (10, true, "Avtur"), (11, false, "Bensin"), (12, true, "Solar"));
        var responses = new[] { Resp(1, optId: 10), Resp(1, optId: 12) };

        Assert.True(AssessmentScoreAggregator.IsQuestionCorrect(q, responses) == true);
        Assert.Equal("Avtur, Solar", AssessmentScoreAggregator.BuildAnswerCell(q, responses));
    }

    // MA partial-subset: hanya 1 dari 2 benar → Salah; tapi cell tetap menampilkan opsi terpilih.
    [Fact]
    public void MultipleAnswer_PartialSubset_IncorrectButOptionsJoined()
    {
        var q = Q(1, "MultipleAnswer", 100, (10, true, "Avtur"), (11, false, "Bensin"), (12, true, "Solar"));
        var responses = new[] { Resp(1, optId: 10) };

        Assert.True(AssessmentScoreAggregator.IsQuestionCorrect(q, responses) != true);
        Assert.Equal("Avtur", AssessmentScoreAggregator.BuildAnswerCell(q, responses));
    }

    // MA superset: semua benar + 1 salah terpilih → Salah (guard tak permisif — akar F-17 FirstOrDefault).
    [Fact]
    public void MultipleAnswer_Superset_Incorrect()
    {
        var q = Q(1, "MultipleAnswer", 100, (10, true, "Avtur"), (11, true, "Solar"), (12, false, "Bensin"));
        var responses = new[] { Resp(1, optId: 10), Resp(1, optId: 11), Resp(1, optId: 12) };

        Assert.True(AssessmentScoreAggregator.IsQuestionCorrect(q, responses) != true);
    }

    // ============ MultipleChoice (1) ============

    // MC tunggal: 1 opsi benar terpilih → Benar; cell = OptionText tunggal (byte-identik label).
    [Fact]
    public void MultipleChoice_SingleOption_ByteIdenticalLabel()
    {
        var q = Q(1, "MultipleChoice", 10, (10, true, "A"), (11, false, "B"));
        var responses = new[] { Resp(1, optId: 10) };

        Assert.True(AssessmentScoreAggregator.IsQuestionCorrect(q, responses) == true);
        Assert.Equal("A", AssessmentScoreAggregator.BuildAnswerCell(q, responses));
    }

    // ============ Essay (1) ============

    // Essay teks panjang → truncate 300 + "..." → total 303 char.
    [Fact]
    public void Essay_TextAnswer_Truncated()
    {
        var q = Q(1, "Essay", 100);
        var longText = new string('x', 350);
        var responses = new[] { Resp(1, text: longText) };

        var cell = AssessmentScoreAggregator.BuildAnswerCell(q, responses);

        Assert.EndsWith("...", cell);
        Assert.Equal(303, cell.Length);   // 300 + "..."
    }

    // ============ Empty (1) ============

    // Tak ada response (kosong) → cell = em dash "—".
    [Fact]
    public void Empty_NoResponse_ReturnsDash()
    {
        var q = Q(1, "MultipleAnswer", 100, (10, true, "Avtur"), (11, true, "Solar"));
        var responses = System.Array.Empty<PackageUserResponse>();

        Assert.Equal("—", AssessmentScoreAggregator.BuildAnswerCell(q, responses));
    }
}
