// Phase 357 LBL-02 — lock wording baru Single Answer/Multiple Answer/Essay (override Phase 305).
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class QuestionTypeLabelsTests
{
    [Theory]
    [InlineData("MultipleChoice", "Single Answer (1 jawaban benar)")]
    [InlineData("MultipleAnswer", "Multiple Answer (≥2 jawaban benar)")]
    [InlineData("Essay", "Essay")]
    [InlineData(null, "Single Answer (1 jawaban benar)")] // fallback
    public void Long_NewWording(string? input, string expected) =>
        Assert.Equal(expected, QuestionTypeLabels.Long(input));

    [Theory]
    [InlineData("MultipleChoice", "Single Answer")]
    [InlineData("MultipleAnswer", "Multiple Answer")]
    [InlineData("Essay", "Essay")]
    [InlineData(null, "Single Answer")] // fallback
    public void Short_NewWording(string? input, string expected) =>
        Assert.Equal(expected, QuestionTypeLabels.Short(input));
}
