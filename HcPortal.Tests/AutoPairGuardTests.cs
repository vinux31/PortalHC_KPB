// Phase 424 GRDF-04 (FLOW-03 / D-08) — sentinel forward-only: pola judul Pre/Post-Test yang DULU memicu
// auto-pair LinkedGroupId untuk Standard kini TAK lagi diterapkan otomatis. Call-site di CreateAssessment
// dihapus (acceptance grep: TryAutoDetectCounterpartGroup(model.Title -> 0). Test ini mengunci definisi pola
// (pure LooksLikePrePostTitle) sebagai dokumentasi + regression sentinel. Verifikasi end-to-end create-path
// (Standard judul "Pre Test X" tak ter-auto-link) di UAT live Plan 03 Task 2 langkah 5.
using HcPortal.Controllers;
using Xunit;

namespace HcPortal.Tests;

public class AutoPairGuardTests
{
    [Theory]
    [InlineData("Pre Test OJT GAST Cilacap")]
    [InlineData("PreTest OJT GAST")]
    [InlineData("Post Test OJT GAST Cilacap")]
    [InlineData("post test welding ru iv")]   // case-insensitive
    public void PrePostStyleTitles_MatchPattern(string title)
        => Assert.True(AssessmentAdminController.LooksLikePrePostTitle(title));

    [Theory]
    [InlineData("Assessment Welding RU IV")]
    [InlineData("Ujian Kompetensi Operator")]
    [InlineData("Pretest")]        // tak ada konten setelah "Test"
    [InlineData("Pre Test")]       // tanpa rest (tak ada konten setelah Test)
    [InlineData("")]
    [InlineData(null)]
    public void NonPrePostTitles_DoNotMatch(string? title)
        => Assert.False(AssessmentAdminController.LooksLikePrePostTitle(title));
}
