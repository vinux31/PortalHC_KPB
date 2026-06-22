// Phase 414 (D-03): regression lock untuk CMPController.CanReviewAnswers.
// Decouple gate tinjauan jawaban: non-owner SELALU lihat (sudah lolos IsResultsAuthorized); owner ikut toggle.
// Pure static (pola ResultsAuthorizationTests) — no DB / no WebApplicationFactory.
using HcPortal.Controllers;
using Xunit;

namespace HcPortal.Tests;

public class CanReviewAnswersTests
{
    [Theory]
    // allowAnswerReview, isOwner, expected
    [InlineData(false, false, true)]   // non-owner + OFF -> bypass, lihat review (SC-1, inti fix)
    [InlineData(true,  false, true)]   // non-owner + ON  -> lihat (tak berubah)
    [InlineData(false, true,  false)]  // owner + OFF      -> tetap diblok (SC-2, perilaku worker)
    [InlineData(true,  true,  true)]   // owner + ON       -> lihat (SC-3, tak berubah)
    public void CanReviewAnswers_Matrix(bool allow, bool isOwner, bool expected)
        => Assert.Equal(expected, CMPController.CanReviewAnswers(allow, isOwner));
}
