using System.IO;
using System.Linq;
using ClosedXML.Excel;
using HcPortal.Helpers;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 999.17 (DRP-01/02, SKR-01) — pure unit tests untuk <c>HcPortal.Helpers.QuestionTemplateBuilder</c>.
/// TANPA DB, TANPA fixture, TANPA trait Integration → masuk fast suite (pola <see cref="InjectExcelHelperTests"/>).
///
/// Task 1 (struktur): header 13/9-kolom + posisi QuestionType/Rubrik + round-trip serialisasi.
/// Task 2 (DataValidation): dropdown QuestionType (semua varian) + numeric Skor 1-100 (Universal) +
/// kolom Skor(14/N) + DV-SKIP kolom lain + legacy TANPA Skor. (Lihat region "DataValidation".)
/// </summary>
public class QuestionTemplateBuilderTests
{
    /// <summary>Round-trip via MemoryStream membuktikan workbook (+ DataValidation) ter-serialize ke OOXML.</summary>
    private static XLWorkbook RoundTrip(XLWorkbook wb)
    {
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return new XLWorkbook(ms);
    }

    /// <summary>Jumlah kolom header (row 1) — abaikan baris instruksi yang hanya menulis kolom 1.</summary>
    private static int HeaderColumnCount(IXLWorksheet ws) =>
        ws.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;

    // =========================================================================
    // STRUKTUR — header Universal 13-kolom; QuestionType=12(L), Rubrik=13(M)
    // =========================================================================

    [Fact]
    public void Build_Universal_HasExpandedHeader_QuestionTypeAndRubrikInPlace()
    {
        using var wb = QuestionTemplateBuilder.Build("Universal");
        var ws = wb.Worksheets.First();

        Assert.Equal(13, HeaderColumnCount(ws));
        Assert.Equal("Pertanyaan", ws.Cell(1, 1).GetString());
        Assert.Equal("Jawaban Benar", ws.Cell(1, 8).GetString());
        Assert.Equal("QuestionType", ws.Cell(1, 12).GetString());
        Assert.Equal("Rubrik", ws.Cell(1, 13).GetString());
    }

    // =========================================================================
    // STRUKTUR — legacy 9-kolom; QuestionType=8(H)
    // =========================================================================

    [Theory]
    [InlineData("MC")]
    [InlineData("MA")]
    [InlineData("Essay")]
    public void Build_Legacy_Has9ColumnHeader_QuestionTypeAtH(string type)
    {
        using var wb = QuestionTemplateBuilder.Build(type);
        var ws = wb.Worksheets.First();

        Assert.Equal(9, HeaderColumnCount(ws));
        Assert.Equal("Jawaban Benar", ws.Cell(1, 6).GetString());
        Assert.Equal("QuestionType", ws.Cell(1, 8).GetString());
        Assert.Equal("Rubrik", ws.Cell(1, 9).GetString());
    }

    // =========================================================================
    // STRUKTUR — round-trip serialisasi OOXML (header bertahan)
    // =========================================================================

    [Fact]
    public void Build_Universal_SurvivesRoundTrip()
    {
        using var src = QuestionTemplateBuilder.Build("Universal");
        using var wb = RoundTrip(src);
        var ws = wb.Worksheets.First();

        Assert.Equal("Question Import", ws.Name);
        Assert.Equal("QuestionType", ws.Cell(1, 12).GetString());
    }
}
