using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using HcPortal.Helpers;
using HcPortal.Models;
using HcPortal.ViewModels;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 396 INJ-10 (Wave 0 — TDD lock) — pure unit tests untuk <c>HcPortal.Helpers.InjectExcelHelper</c>
/// (BELUM ada implementasi → suite ini RED sampai Plan 02).
///
/// TANPA DB, TANPA fixture, TANPA trait Integration → masuk fast suite.
/// Pola in-memory builder Q/Mc/Ma/Essay dari <see cref="BuildAutoGenAnswersTests"/>.
///
/// KONTRAK yang dikunci test ini (Plan 02 WAJIB implementasi):
///   public static ClosedXML.Excel.XLWorkbook GenerateTemplate(
///       IReadOnlyList&lt;InjectQuestionSpec&gt; questions,
///       IReadOnlyList&lt;(string Nip, string Name)&gt; workers);
///   public static (List&lt;InjectWorkerAnswersVM&gt; Workers, List&lt;InjectRowError&gt; Errors, int SkippedBlank) ParseMatrix(
///       System.IO.Stream stream,
///       IReadOnlyList&lt;InjectQuestionSpec&gt; questions,
///       IReadOnlySet&lt;string&gt; allowedNips,
///       IReadOnlyDictionary&lt;string,string&gt; nipToUserId);
///
/// Risiko #1 (Pitfall 1): silent corruption dari ORDERING tak stabil. Round-trip test mengunci
/// SATU comparator gen↔parse (OrderBy(Order).ThenBy(TempId)) dan A=Options[0] (urutan authored),
/// BUKAN OrderBy(TempId). Blank=OMIT (D-06). Validasi per-baris/sel emit InjectRowError (D-09).
/// </summary>
public class InjectExcelHelperTests
{
    // ---- in-memory builders (no DB) — mirror BuildAutoGenAnswersTests ----

    /// <summary>Bangun InjectQuestionSpec. opts: (optTempId, isCorrect). Urutan opts = urutan authored (A=index 0).</summary>
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

    /// <summary>MC standar 4 opsi A-D: A(tempId*10+1)=benar, B/C/D salah.</summary>
    private static InjectQuestionSpec Mc(int tempId, int sv = 10, int? order = null) =>
        Q(tempId, "MultipleChoice", sv, order ?? tempId,
            (tempId * 10 + 1, true), (tempId * 10 + 2, false), (tempId * 10 + 3, false), (tempId * 10 + 4, false));

    /// <summary>MA standar 4 opsi A-D: A &amp; C benar, B &amp; D salah.</summary>
    private static InjectQuestionSpec Ma(int tempId, int sv = 10, int? order = null) =>
        Q(tempId, "MultipleAnswer", sv, order ?? tempId,
            (tempId * 10 + 1, true), (tempId * 10 + 2, false), (tempId * 10 + 3, true), (tempId * 10 + 4, false));

    private static InjectQuestionSpec Essay(int tempId, int sv = 10, int? order = null) =>
        new InjectQuestionSpec { TempId = tempId, QuestionType = "Essay", ScoreValue = sv, Order = order ?? tempId };

    // ---- ClosedXML helpers ----

    /// <summary>Kolom soal dalam matrix = SATU comparator gen↔parse (D-04). Data mulai kolom 3 (NIP=1, Nama=2).</summary>
    private static List<InjectQuestionSpec> Ordered(IEnumerable<InjectQuestionSpec> questions) =>
        questions.OrderBy(q => q.Order).ThenBy(q => q.TempId).ToList();

    /// <summary>Indeks kolom (1-based) untuk soal pada TempId tertentu. Kolom 1=NIP, 2=Nama, 3+=soal.</summary>
    private static int QuestionColumn(IReadOnlyList<InjectQuestionSpec> questions, int questionTempId)
    {
        var ord = Ordered(questions);
        int pos = ord.FindIndex(q => q.TempId == questionTempId);
        return 3 + pos;   // soal pertama di kolom 3
    }

    /// <summary>Tulis nilai sel pada sheet "Jawaban" untuk worker di row tertentu.</summary>
    private static void SetCell(XLWorkbook wb, int row, int col, string value)
    {
        var ws = wb.Worksheet("Jawaban");
        ws.Cell(row, col).Value = value;
    }

    private static MemoryStream ToStream(XLWorkbook wb)
    {
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static IReadOnlySet<string> Nips(params string[] nips) => new HashSet<string>(nips);

    private static IReadOnlyDictionary<string, string> NipMap(params (string nip, string userId)[] map) =>
        map.ToDictionary(m => m.nip, m => m.userId);

    // =========================================================================
    // 1) ROUND-TRIP — generate → parse memetakan sel ke TempId yang TEPAT
    // =========================================================================

    [Fact]
    public void Generate_Then_Parse_RoundTrip_MapsCellsToCorrectTempIds()
    {
        var questions = new List<InjectQuestionSpec> { Mc(101, 10, 1), Ma(102, 10, 2), Essay(103, 10, 3) };
        var wb = InjectExcelHelper.GenerateTemplate(questions, new[] { ("123", "Budi") });

        // row 2 = worker pertama
        SetCell(wb, 2, QuestionColumn(questions, 101), "A");      // MC → opsi A
        SetCell(wb, 2, QuestionColumn(questions, 102), "A,C");    // MA → opsi A & C
        SetCell(wb, 2, QuestionColumn(questions, 103), "8");      // Essay skor

        using var stream = ToStream(wb);
        var (workers, errors, _) = InjectExcelHelper.ParseMatrix(
            stream, questions, Nips("123"), NipMap(("123", "uid1")));

        Assert.Empty(errors);
        var w = Assert.Single(workers);
        Assert.Equal("uid1", w.UserId);
        Assert.Equal("manual", w.Mode);

        var aMc = w.Answers.First(a => a.QuestionTempId == 101);
        Assert.Equal(new List<int> { 101 * 10 + 1 }, aMc.SelectedOptionTempIds);   // A = Options[0].TempId

        var aMa = w.Answers.First(a => a.QuestionTempId == 102);
        Assert.Equal(new List<int> { 102 * 10 + 1, 102 * 10 + 3 }, aMa.SelectedOptionTempIds);  // A,C authored order

        var aEssay = w.Answers.First(a => a.QuestionTempId == 103);
        Assert.Equal(8, aEssay.EssayScore);
    }

    // =========================================================================
    // 2) Letter → urutan AUTHORED (A=Options[0]), BUKAN OrderBy(TempId)
    // =========================================================================

    [Fact]
    public void LetterMaps_ToAuthoredOptionOrder_NotTempIdSort()
    {
        // Opsi sengaja OUT of letter order: A.TempId=99, B.TempId=10
        var mc = Q(201, "MultipleChoice", 10, 1, (99, true), (10, false));
        var questions = new List<InjectQuestionSpec> { mc };
        var wb = InjectExcelHelper.GenerateTemplate(questions, new[] { ("123", "Budi") });

        SetCell(wb, 2, QuestionColumn(questions, 201), "A");

        using var stream = ToStream(wb);
        var (workers, errors, _) = InjectExcelHelper.ParseMatrix(
            stream, questions, Nips("123"), NipMap(("123", "uid1")));

        Assert.Empty(errors);
        var ans = workers.Single().Answers.First(a => a.QuestionTempId == 201);
        Assert.Equal(new List<int> { 99 }, ans.SelectedOptionTempIds);   // A = Options[0] = TempId 99, BUKAN 10 (terkecil)
    }

    // =========================================================================
    // 3) Sel kosong → OMIT (bukan spec MC/MA kosong) + SkippedBlank>=1 (D-06)
    // =========================================================================

    [Fact]
    public void BlankCell_IsOmitted_NotEmptySpec()
    {
        var questions = new List<InjectQuestionSpec> { Mc(301, 10, 1), Mc(302, 10, 2) };
        var wb = InjectExcelHelper.GenerateTemplate(questions, new[] { ("123", "Budi") });

        // Soal 301 di-kosongkan; 302 diisi
        SetCell(wb, 2, QuestionColumn(questions, 302), "A");

        using var stream = ToStream(wb);
        var (workers, errors, skipped) = InjectExcelHelper.ParseMatrix(
            stream, questions, Nips("123"), NipMap(("123", "uid1")));

        Assert.Empty(errors);
        var w = Assert.Single(workers);
        Assert.DoesNotContain(w.Answers, a => a.QuestionTempId == 301);   // OMIT, bukan SelectedOptionTempIds=[]
        Assert.Contains(w.Answers, a => a.QuestionTempId == 302);
        Assert.True(skipped >= 1, $"SkippedBlank {skipped} < 1");
    }

    // =========================================================================
    // 4) MA daftar koma "A,C" → 2 TempId dalam urutan authored
    // =========================================================================

    [Fact]
    public void MultipleAnswer_CommaList_ProducesMultipleTempIds()
    {
        var questions = new List<InjectQuestionSpec> { Ma(401, 10, 1) };
        var wb = InjectExcelHelper.GenerateTemplate(questions, new[] { ("123", "Budi") });

        SetCell(wb, 2, QuestionColumn(questions, 401), "A,C");

        using var stream = ToStream(wb);
        var (workers, errors, _) = InjectExcelHelper.ParseMatrix(
            stream, questions, Nips("123"), NipMap(("123", "uid1")));

        Assert.Empty(errors);
        var ans = workers.Single().Answers.First(a => a.QuestionTempId == 401);
        Assert.Equal(new List<int> { 401 * 10 + 1, 401 * 10 + 3 }, ans.SelectedOptionTempIds);  // A=Options[0], C=Options[2]
    }

    // =========================================================================
    // 5) Essay — skor diparse, teks OPSIONAL (D-05)
    // =========================================================================

    [Fact]
    public void Essay_ScoreParsed_TextOptional()
    {
        var questions = new List<InjectQuestionSpec> { Essay(501, 10, 1) };

        // (a) skor=6, teks kosong → EssayScore==6, TextAnswer==null
        var wb1 = InjectExcelHelper.GenerateTemplate(questions, new[] { ("123", "Budi") });
        SetCell(wb1, 2, QuestionColumn(questions, 501), "6");
        using (var s1 = ToStream(wb1))
        {
            var (workers, errors, _) = InjectExcelHelper.ParseMatrix(
                s1, questions, Nips("123"), NipMap(("123", "uid1")));
            Assert.Empty(errors);
            var ans = workers.Single().Answers.First(a => a.QuestionTempId == 501);
            Assert.Equal(6, ans.EssayScore);
            Assert.True(string.IsNullOrEmpty(ans.TextAnswer), "TextAnswer harus null/kosong saat tidak diisi");
        }

        // (b) skor=6 + teks "jawaban" → TextAnswer=="jawaban"
        var wb2 = InjectExcelHelper.GenerateTemplate(questions, new[] { ("123", "Budi") });
        int scoreCol = QuestionColumn(questions, 501);
        SetCell(wb2, 2, scoreCol, "6");
        SetCell(wb2, 2, scoreCol + 1, "jawaban");   // kolom teks essay di sebelah kolom skor
        using (var s2 = ToStream(wb2))
        {
            var (workers, errors, _) = InjectExcelHelper.ParseMatrix(
                s2, questions, Nips("123"), NipMap(("123", "uid1")));
            Assert.Empty(errors);
            var ans = workers.Single().Answers.First(a => a.QuestionTempId == 501);
            Assert.Equal(6, ans.EssayScore);
            Assert.Equal("jawaban", ans.TextAnswer);
        }
    }

    // =========================================================================
    // 6) NIP tidak ada di picker → InjectRowError, 0 worker untuk NIP itu (D-02)
    // =========================================================================

    [Fact]
    public void NipNotInPicker_ProducesRowError()
    {
        var questions = new List<InjectQuestionSpec> { Mc(601, 10, 1) };
        var wb = InjectExcelHelper.GenerateTemplate(questions, new[] { ("999", "Asing") });

        SetCell(wb, 2, 1, "999");                              // NIP baris (bukan di picker)
        SetCell(wb, 2, QuestionColumn(questions, 601), "A");

        using var stream = ToStream(wb);
        var (workers, errors, _) = InjectExcelHelper.ParseMatrix(
            stream, questions, Nips("123"), NipMap(("123", "uid1")));

        Assert.DoesNotContain(workers, w => w.UserId == "999");
        Assert.Contains(errors, e => e.Message.Contains("999") && e.Message.Contains("pekerja terpilih"));
    }

    // =========================================================================
    // 7) Huruf opsi invalid "E" → InjectRowError per-sel (D-09); jawaban soal itu tak ditambahkan
    // =========================================================================

    [Fact]
    public void InvalidOptionLetter_ProducesPerCellError()
    {
        var questions = new List<InjectQuestionSpec> { Mc(701, 10, 1) };   // hanya 4 opsi A-D
        var wb = InjectExcelHelper.GenerateTemplate(questions, new[] { ("123", "Budi") });

        SetCell(wb, 2, QuestionColumn(questions, 701), "E");   // opsi E tidak ada

        using var stream = ToStream(wb);
        var (workers, errors, _) = InjectExcelHelper.ParseMatrix(
            stream, questions, Nips("123"), NipMap(("123", "uid1")));

        Assert.Contains(errors, e => e.Message.Contains("E"));
        // jawaban untuk soal invalid tidak boleh ditambahkan dengan opsi keliru
        Assert.DoesNotContain(
            workers.SelectMany(w => w.Answers),
            a => a.QuestionTempId == 701 && a.SelectedOptionTempIds.Count > 0);
    }

    // =========================================================================
    // 8) Skor essay di luar rentang (>ScoreValue) → InjectRowError (D-09)
    // =========================================================================

    [Fact]
    public void EssayScoreOutOfRange_ProducesError()
    {
        var questions = new List<InjectQuestionSpec> { Essay(801, 10, 1) };   // ScoreValue=10
        var wb = InjectExcelHelper.GenerateTemplate(questions, new[] { ("123", "Budi") });

        SetCell(wb, 2, QuestionColumn(questions, 801), "15");   // 15 > max 10

        using var stream = ToStream(wb);
        var (_, errors, _) = InjectExcelHelper.ParseMatrix(
            stream, questions, Nips("123"), NipMap(("123", "uid1")));

        Assert.Contains(errors, e => e.Message.Contains("15") || e.Message.Contains("10"));
    }
}
