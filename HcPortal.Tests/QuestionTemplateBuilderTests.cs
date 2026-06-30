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
    // STRUKTUR — header Universal 14-kolom; QuestionType=12(L), Rubrik=13(M), Skor=14(N)
    // Marker dual-format (Opsi E@6, Opsi F@7, No.Section@9, Nama Section@10) TAK bergeser (D-07/Pitfall 1).
    // =========================================================================

    [Fact]
    public void Build_Universal_HasExpandedHeader_QuestionTypeRubrikSkorInPlace()
    {
        using var wb = QuestionTemplateBuilder.Build("Universal");
        var ws = wb.Worksheets.First();

        Assert.Equal(14, HeaderColumnCount(ws));
        Assert.Equal("Pertanyaan", ws.Cell(1, 1).GetString());
        Assert.Equal("Opsi E", ws.Cell(1, 6).GetString());          // marker 6 — tak bergeser
        Assert.Equal("Opsi F", ws.Cell(1, 7).GetString());          // marker 7 — tak bergeser
        Assert.Equal("Jawaban Benar", ws.Cell(1, 8).GetString());
        Assert.Equal("No. Section", ws.Cell(1, 9).GetString());     // marker 9 — tak bergeser
        Assert.Equal("Nama Section", ws.Cell(1, 10).GetString());   // marker 10 — tak bergeser
        Assert.Equal("QuestionType", ws.Cell(1, 12).GetString());
        Assert.Equal("Rubrik", ws.Cell(1, 13).GetString());
        Assert.Equal("Skor", ws.Cell(1, 14).GetString());           // SKOR-COL (D-07) kolom N
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

    // =========================================================================
    // DataValidation — DV-TYPE dropdown QuestionType (D-01/02/03)
    // =========================================================================

    [Fact]
    public void Build_Universal_QuestionTypeColumn_HasDropdownList()
    {
        using var wb = QuestionTemplateBuilder.Build("Universal");
        var ws = wb.Worksheets.First();

        // QuestionType di Universal = kolom L (12), range baris 2-1000.
        Assert.True(ws.DataValidations.TryGet(ws.Range("L2:L1000").RangeAddress, out var dvType));
        Assert.Equal(XLAllowedValues.List, dvType.AllowedValues);
        Assert.True(dvType.InCellDropdown);
        Assert.Contains("MultipleChoice", dvType.Value);
        Assert.Contains("MultipleAnswer", dvType.Value);
        Assert.Contains("Essay", dvType.Value);
    }

    [Theory]
    [InlineData("MC")]
    [InlineData("MA")]
    [InlineData("Essay")]
    public void Build_Legacy_QuestionTypeColumnH_HasDropdownList(string type)
    {
        using var wb = QuestionTemplateBuilder.Build(type);
        var ws = wb.Worksheets.First();

        // QuestionType di legacy = kolom H (8), range baris 2-1000.
        Assert.True(ws.DataValidations.TryGet(ws.Range("H2:H1000").RangeAddress, out var dvType));
        Assert.Equal(XLAllowedValues.List, dvType.AllowedValues);
        Assert.True(dvType.InCellDropdown);
        Assert.Contains("MultipleChoice", dvType.Value);
    }

    // =========================================================================
    // DataValidation — SKOR-DV numeric WholeNumber 1-100 (D-10), HANYA Universal
    // =========================================================================

    [Fact]
    public void Build_Universal_SkorColumn_HasWholeNumberBetween1And100()
    {
        using var wb = QuestionTemplateBuilder.Build("Universal");
        var ws = wb.Worksheets.First();

        // Skor di Universal = kolom N (14), range baris 2-1000.
        Assert.True(ws.DataValidations.TryGet(ws.Range("N2:N1000").RangeAddress, out var dvScore));
        Assert.Equal(XLAllowedValues.WholeNumber, dvScore.AllowedValues);
        Assert.Equal(XLOperator.Between, dvScore.Operator);
        Assert.Equal("1", dvScore.MinValue);
        Assert.Equal("100", dvScore.MaxValue);
    }

    // =========================================================================
    // DV-SKIP — kolom selain QuestionType & Skor TANPA dropdown (D-04/D-05)
    // =========================================================================

    [Fact]
    public void Build_Universal_OtherColumns_HaveNoDataValidation()
    {
        using var wb = QuestionTemplateBuilder.Build("Universal");
        var ws = wb.Worksheets.First();

        // Jawaban Benar (H=8) — campur MC/MA/Essay → TANPA dropdown (D-04).
        Assert.False(ws.DataValidations.TryGet(ws.Range("H2:H1000").RangeAddress, out _));
        // Opsi A (B=2), No. Section (I=9), Nama Section (J=10) — TANPA dropdown (D-05).
        Assert.False(ws.DataValidations.TryGet(ws.Range("B2:B1000").RangeAddress, out _));
        Assert.False(ws.DataValidations.TryGet(ws.Range("I2:I1000").RangeAddress, out _));
        Assert.False(ws.DataValidations.TryGet(ws.Range("J2:J1000").RangeAddress, out _));
    }

    // =========================================================================
    // LEGACY-SAFE — legacy TANPA kolom Skor & TANPA Skor-DV (D-07)
    // =========================================================================

    [Theory]
    [InlineData("MC")]
    [InlineData("MA")]
    [InlineData("Essay")]
    public void Build_Legacy_HasNoSkorColumnNorSkorValidation(string type)
    {
        using var wb = QuestionTemplateBuilder.Build(type);
        var ws = wb.Worksheets.First();

        Assert.Equal(9, HeaderColumnCount(ws));                 // tetap 9-kolom (tanpa Skor)
        Assert.False(ws.DataValidations.TryGet(ws.Range("N2:N1000").RangeAddress, out _));
    }

    // =========================================================================
    // Round-trip serialisasi — DataValidation bertahan ke OOXML (bukan hanya model in-memory)
    // =========================================================================

    [Fact]
    public void Build_Universal_DataValidations_SurviveRoundTrip()
    {
        using var src = QuestionTemplateBuilder.Build("Universal");
        using var wb = RoundTrip(src);
        var ws = wb.Worksheets.First();

        Assert.True(ws.DataValidations.TryGet(ws.Range("L2:L1000").RangeAddress, out var dvType));
        Assert.Equal(XLAllowedValues.List, dvType.AllowedValues);
        Assert.Contains("MultipleChoice", dvType.Value);

        Assert.True(ws.DataValidations.TryGet(ws.Range("N2:N1000").RangeAddress, out var dvScore));
        Assert.Equal(XLAllowedValues.WholeNumber, dvScore.AllowedValues);
        Assert.Equal("1", dvScore.MinValue);
        Assert.Equal("100", dvScore.MaxValue);
    }
}
