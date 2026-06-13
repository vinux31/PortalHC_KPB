// Phase 367 Plan 06 Task 1 — honest HTMX trigger split (L-06, #1): sukses vs gagal HX-Trigger BERBEDA
// (akar bug "sukses palsu" = DeleteTabResult SELALU recordDeleted). Static helper TrainingAdminController
// testable tanpa HttpContext (pola 04 anti-drift).
// CATATAN (Pitfall 6): render partial _CascadePreviewModal (GET DeletePreview) = runtime-verified via Playwright
// Plan 08 Task 2 — proyek TIDAK punya WebApplicationFactory/TestServer/ViewRender infra (dikonfirmasi grep saat eksekusi).
// dotnet test full suite (regresi) tetap dijalankan sebagai automated minimum.
using System.Text.Json;
using HcPortal.Controllers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class RecordCascadeUiTests
{
    // D-19 (fix temuan 06): sesi Pre/Post tak boleh dihapus satuan via endpoint generik (cegah orphan pasangan).
    // Static shared (AdminBaseController) dipakai tab-1 DeleteAssessment + tab-2 DeleteManualAssessment (single-source).
    [Theory]
    [InlineData("PreTest", true)]
    [InlineData("PostTest", true)]
    [InlineData("Manual", false)]
    [InlineData("Standard", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsPrePostSession_TrueOnlyForPrePost(string? assessmentType, bool expected)
    {
        var s = new AssessmentSession { Title = "x", Category = "x", AccessToken = "", AssessmentType = assessmentType };
        Assert.Equal(expected, AdminBaseController.IsPrePostSession(s));
    }

    // #1/L-06: trigger SUKSES = "recordDeleted" (konstan; dikonsumsi hidden re-fetch _TrainingRecordsTab).
    [Fact]
    public void SuccessTrigger_IsRecordDeleted()
    {
        Assert.Equal("recordDeleted", TrainingAdminController.RecordDeletedTrigger);
    }

    // #1/L-06: trigger GAGAL = payload JSON recordDeleteFailed + pesan, BUKAN trigger sukses → flash merah bukan sukses-palsu.
    [Fact]
    public void FailureTrigger_ContainsRecordDeleteFailedAndMessage_NotSuccess()
    {
        var json = TrainingAdminController.BuildRecordDeleteFailedTrigger("Gagal menghapus record.");
        Assert.Contains("recordDeleteFailed", json);
        Assert.Contains("Gagal menghapus record.", json);
        Assert.NotEqual(TrainingAdminController.RecordDeletedTrigger, json);
    }

    // Bentuk payload: {"recordDeleteFailed":{"pesan":"..."}} — dikonsumsi listener HTMX (UI-SPEC IC-2/S3).
    [Fact]
    public void FailureTrigger_HasExpectedJsonShape()
    {
        var json = TrainingAdminController.BuildRecordDeleteFailedTrigger("pesan uji");
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("recordDeleteFailed", out var inner));
        Assert.True(inner.TryGetProperty("pesan", out var pesan));
        Assert.Equal("pesan uji", pesan.GetString());
    }

    // V7 generik: helper hanya menserialisasi pesan dari caller — tak menambah field internal (stack/exception) sendiri.
    [Fact]
    public void FailureTrigger_OnlySerializesGivenMessage_NoLeak()
    {
        var json = TrainingAdminController.BuildRecordDeleteFailedTrigger("X");
        using var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("recordDeleteFailed");
        int count = 0; foreach (var _ in inner.EnumerateObject()) count++;
        Assert.Equal(1, count);
        Assert.Equal("X", inner.GetProperty("pesan").GetString());
    }
}
