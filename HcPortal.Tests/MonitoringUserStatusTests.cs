// Unit test AssessmentAdminController.DeriveUserStatus (Phase 348 P02 — MAM-04).
// Essay-pending session (Status=PendingGrading + CompletedAt terisi + IsPassed=null) HARUS
// derive "Menunggu Penilaian", BUKAN "Completed" — PendingGrading dicek SEBELUM CompletedAt.
// 6-cabang derivation order coverage.

using System;
using HcPortal.Controllers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class MonitoringUserStatusTests
{
    [Theory]
    // essay-pending: CompletedAt terisi tapi PendingGrading menang (cabang pertama) — INTI MAM-04
    [InlineData(AssessmentConstants.AssessmentStatus.PendingGrading, true, true, "Menunggu Penilaian")]
    [InlineData("Completed", true, true, "Completed")]   // completed normal tak berubah
    [InlineData("Cancelled", false, false, "Dibatalkan")]
    [InlineData("Abandoned", false, false, "Abandoned")]
    [InlineData("Open", false, true, "InProgress")]      // started, belum completed
    [InlineData("Open", false, false, "Not started")]    // belum mulai
    public void DeriveUserStatus_Scenarios_ReturnsExpected(
        string status, bool completed, bool started, string expected)
    {
        DateTime? completedAt = completed ? DateTime.UtcNow : (DateTime?)null;
        DateTime? startedAt = started ? DateTime.UtcNow : (DateTime?)null;

        var result = AssessmentAdminController.DeriveUserStatus(status, completedAt, startedAt);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void DeriveUserStatus_PendingGrading_WinsOverCompletedAt()
    {
        // Regresi guard: walau CompletedAt terisi (essay flow set keduanya), PendingGrading menang.
        var result = AssessmentAdminController.DeriveUserStatus(
            AssessmentConstants.AssessmentStatus.PendingGrading, DateTime.UtcNow, DateTime.UtcNow);

        Assert.Equal("Menunggu Penilaian", result);
    }
}
