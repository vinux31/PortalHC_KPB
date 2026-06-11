// Quick fix 260611-m9r — uji helper ApplySevenDayWindow (override window 7-hari saat search).
//
// Behavior yang diuji (static + pure → LINQ-to-Objects, TANPA construct controller / DbContext):
//   - search KOSONG  → window 7-hari berlaku (sesi lama tersaring, sesi baru lolos).
//   - search NON-EMPTY → window di-SKIP (sesi lama >7 hari ikut muncul) — preseden CIL-02 Phase 338.
// Null-coalesce: sesi tanpa ExamWindowCloseDate jatuh ke Schedule untuk cek window.
using System;
using System.Linq;
using HcPortal.Controllers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class AssessmentSearchWindowTests
{
    // Factory minimum aman (mirror WorkerDataServiceSearchTests:40-45);
    // ExamWindowCloseDate=null untuk uji fallback ke Schedule.
    private static AssessmentSession Session(int id, string title, DateTime schedule) =>
        new AssessmentSession
        {
            Id = id, UserId = "u1", Status = "Closed", IsPassed = false,
            Title = title, Schedule = schedule, Score = 0, GenerateCertificate = false,
            ExamWindowCloseDate = null
        };

    [Fact]
    public void Window_SearchKosong_SesiLama_Dikecualikan()
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var data = new[] { Session(1, "Post Test OJT", DateTime.UtcNow.AddDays(-30)) }.AsQueryable();

        var result = AssessmentAdminController.ApplySevenDayWindow(data, search: null, cutoff).ToList();

        Assert.Empty(result); // window jalan → sesi lama (fallback Schedule -30 hari) tersaring
    }

    [Fact]
    public void Window_SearchAda_SesiLama_Disertakan()
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var data = new[] { Session(1, "Post Test OJT", DateTime.UtcNow.AddDays(-30)) }.AsQueryable();

        var result = AssessmentAdminController.ApplySevenDayWindow(data, search: "OJT", cutoff).ToList();

        Assert.Single(result); // search non-empty → window di-skip, sesi lama ikut muncul
    }

    [Fact]
    public void Window_SearchKosong_SesiBaru_Disertakan()
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var data = new[] { Session(1, "Quiz Harian", DateTime.UtcNow.AddDays(-1)) }.AsQueryable();

        var result = AssessmentAdminController.ApplySevenDayWindow(data, search: null, cutoff).ToList();

        Assert.Single(result); // window jalan → sesi baru (Schedule kemarin) tetap lolos
    }
}
