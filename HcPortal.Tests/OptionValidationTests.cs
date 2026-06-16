// Phase 386 PXF-02 (F-DEV-01) — pure unit test untuk QuestionOptionValidator.ValidateQuestionOptions.
//
// RED Wave 0: helper HcPortal.Helpers.QuestionOptionValidator BELUM ADA — dibuat Wave 1 (Plan 02).
// File ini SENGAJA tidak compile sampai Wave 1 menambahkan helper dengan signature di <interfaces>
// PLAN 386-01:
//   public static (bool ok, string? error) ValidateQuestionOptions(string type, string?[] texts, bool[] corrects);
// Build akan gagal dengan CS0117/CS0103 mereferensikan symbol `ValidateQuestionOptions` — itu RED yang
// diharapkan untuk Wave 0 (gate acceptance PXF-02 dikunci di depan).
//
// Pure xUnit: TANPA [Trait] → selalu jalan (tidak butuh SQLEXPRESS). Mengunci HANYA cek text-presence
// yang ditambahkan (D-01 ≥2 opsi berisi untuk MA, D-03 correct-flag wajib punya teks). ValidateQuestionOptions
// TIDAK menegakkan correctCount — gate itu tetap di controller existing.
using HcPortal.Helpers;
using Xunit;

namespace HcPortal.Tests;

public class OptionValidationTests
{
    // ============ MultipleChoice (3) ============

    // F-DEV-01 root: correctA=true tapi optionA kosong → harus ditolak (soal 0-opsi membekukan submit awal).
    [Fact]
    public void MultipleChoice_ZeroOptions_Rejected()
    {
        var (ok, error) = QuestionOptionValidator.ValidateQuestionOptions(
            "MultipleChoice",
            new string?[] { null, null, null, null },
            new bool[] { true, false, false, false });

        Assert.False(ok);
        Assert.False(string.IsNullOrEmpty(error));
    }

    // D-03: flag benar pada opsi yang TIDAK ber-teks → ditolak (correctA pada optionA kosong).
    [Fact]
    public void MultipleChoice_CorrectOptionWithoutText_Rejected()
    {
        var (ok, _) = QuestionOptionValidator.ValidateQuestionOptions(
            "MultipleChoice",
            new string?[] { null, "Bensin", null, null },
            new bool[] { true, false, false, false });

        Assert.False(ok);
    }

    // Valid MC: 2 opsi berisi, 1 ditandai benar (pada opsi yang berisi) → diterima.
    [Fact]
    public void MultipleChoice_TwoFilledOneCorrect_Accepted()
    {
        var (ok, error) = QuestionOptionValidator.ValidateQuestionOptions(
            "MultipleChoice",
            new string?[] { "Avtur", "Bensin", null, null },
            new bool[] { true, false, false, false });

        Assert.True(ok);
        Assert.Null(error);
    }

    // ============ MultipleAnswer (3) ============

    // D-01: MA wajib ≥2 opsi berisi teks; hanya 1 berisi → ditolak.
    [Fact]
    public void MultipleAnswer_OneFilled_Rejected()
    {
        var (ok, _) = QuestionOptionValidator.ValidateQuestionOptions(
            "MultipleAnswer",
            new string?[] { "Avtur", null, null, null },
            new bool[] { true, true, false, false });

        Assert.False(ok);
    }

    // D-03: correctC ditandai pada optionC yang kosong → ditolak.
    [Fact]
    public void MultipleAnswer_CorrectOptionWithoutText_Rejected()
    {
        var (ok, _) = QuestionOptionValidator.ValidateQuestionOptions(
            "MultipleAnswer",
            new string?[] { "Avtur", "Solar", null, null },
            new bool[] { true, false, true, false });

        Assert.False(ok);
    }

    // Valid MA: 3 opsi berisi, 2 ditandai benar (keduanya berisi) → diterima.
    [Fact]
    public void MultipleAnswer_TwoFilledTwoCorrect_Accepted()
    {
        var (ok, error) = QuestionOptionValidator.ValidateQuestionOptions(
            "MultipleAnswer",
            new string?[] { "Avtur", "Solar", "Bensin", null },
            new bool[] { true, true, false, false });

        Assert.True(ok);
        Assert.Null(error);
    }

    // ============ Essay (1) ============

    // Essay melewati validasi opsi sepenuhnya — semua teks null + tak ada flag benar → tetap diterima.
    [Fact]
    public void Essay_AnyOptions_Accepted()
    {
        var (ok, error) = QuestionOptionValidator.ValidateQuestionOptions(
            "Essay",
            new string?[] { null, null, null, null },
            new bool[] { false, false, false, false });

        Assert.True(ok);
        Assert.Null(error);
    }
}
