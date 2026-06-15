// Phase 382 WSE-09 (TMR-03) — AutoSubmitToken TIDAK dikonsumsi sebelum grading commit.
//
// Pitfall 3: token di-`TempData.Remove` di EnsureCanSubmitExamAsync SEBELUM grading. Bila grading throw
// (DB hiccup), retry kehilangan token → permanent reject (DoS). Fix: konsumsi token HANYA pada success
// path (setelah GradeAndCompleteAsync return true).
//
// KONVENSI: kontrak konsumsi = PURE HELPER `CMPController.ShouldConsumeAutoSubmitToken(gradingSucceeded)`.
// EnsureCanSubmitExamAsync hanya VALIDASI token (tidak remove); SubmitExam memanggil remove hanya saat
// helper => true (grading sukses). Test ini mengunci kontrak: konsumsi==true HANYA bila grading sukses.
//
// RED sebelum Task 4: stub helper mengembalikan TRUE selalu (model perilaku bug: konsumsi pre-grading /
// tanpa syarat sukses) → Test "fail-not-consumed" fail.
using HcPortal.Controllers;
using Xunit;

namespace HcPortal.Tests;

public class AutoSubmitTokenRetryTests
{
    [Fact] // RED→GREEN: grading GAGAL → token TIDAK boleh dikonsumsi (retry aman)
    public void AutoSubmitTokenRetry_GradingFailed_TokenNotConsumed()
    {
        Assert.False(CMPController.ShouldConsumeAutoSubmitToken(gradingSucceeded: false));
    }

    [Fact] // grading SUKSES → token dikonsumsi (one-shot, tak bisa replay)
    public void AutoSubmitTokenRetry_GradingSucceeded_TokenConsumed()
    {
        Assert.True(CMPController.ShouldConsumeAutoSubmitToken(gradingSucceeded: true));
    }
}
