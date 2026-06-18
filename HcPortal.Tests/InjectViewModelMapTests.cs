// Phase 394 (Inject Assessment Manual) — unit tests untuk InjectAssessmentController.MapToRequest
// (InjectAssessmentViewModel → InjectRequest). Pure unit (NO DB) → berjalan di bawah
// `dotnet test --filter Category!=Integration`. Plan 394-04 Task 2.
using System;
using System.Collections.Generic;
using System.Linq;
using HcPortal.Controllers;
using HcPortal.Models;
using HcPortal.ViewModels;
using Xunit;

namespace HcPortal.Tests;

public class InjectViewModelMapTests
{
    private static IReadOnlyDictionary<string, string> Nip(params (string id, string nip)[] pairs)
        => pairs.ToDictionary(p => p.id, p => p.nip);

    // Phase 395: MapToRequest kini terima workerAnswers (per-worker jawaban). Test 394 ini menguji
    // mapping scalar/question/cert/NIP TANPA jawaban → lewatkan list kosong (Answers tetap kosong).
    private static IReadOnlyList<InjectAssessmentViewModel.InjectWorkerAnswersVM> NoAnswers()
        => new List<InjectAssessmentViewModel.InjectWorkerAnswersVM>();

    [Fact]
    [Trait("Category", "Unit")]
    public void Maps_scalars()
    {
        var vm = new InjectAssessmentViewModel
        {
            Title = "Uji Inject",
            Category = "Mandatory",
            AssessmentType = "PreTest",
            CompletedAt = new DateTime(2026, 6, 10),
            DurationMinutes = 45,
            PassPercentage = 80,
            AllowAnswerReview = false
        };

        var req = InjectAssessmentController.MapToRequest(vm, Nip(), NoAnswers());

        Assert.Equal("Uji Inject", req.Title);
        Assert.Equal("Mandatory", req.Category);
        Assert.Equal("PreTest", req.AssessmentType);
        Assert.Contains(req.AssessmentType, new[] { "Standard", "PreTest", "PostTest" });
        Assert.NotEqual("Manual", req.AssessmentType);   // D-deviation: never "Manual"
        Assert.Equal(new DateTime(2026, 6, 10), req.CompletedAt);
        Assert.Equal(45, req.DurationMinutes);
        Assert.Equal(80, req.PassPercentage);
        Assert.False(req.AllowAnswerReview);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Maps_questions()
    {
        var vm = new InjectAssessmentViewModel();
        vm.Questions.Add(new InjectAssessmentViewModel.InjectQuestionVM
        {
            QuestionText = "Apa itu X?",
            QuestionType = "MultipleChoice",
            ScoreValue = 20,
            TempId = 1,
            ElemenTeknis = "Proses",
            Options = new()
            {
                new() { OptionText = "A", IsCorrect = true, TempId = 1 },
                new() { OptionText = "B", IsCorrect = false, TempId = 2 },
                new() { OptionText = "C", IsCorrect = false, TempId = 3 },
                new() { OptionText = "D", IsCorrect = false, TempId = 4 },
            }
        });
        vm.Questions.Add(new InjectAssessmentViewModel.InjectQuestionVM
        {
            QuestionText = "Jelaskan Y",
            QuestionType = "Essay",
            ScoreValue = 30,
            TempId = 2,
            Rubrik = "Kunci Y"
        });

        var req = InjectAssessmentController.MapToRequest(vm, Nip(), NoAnswers());

        Assert.Equal(2, req.Questions.Count);
        var mc = req.Questions[0];
        Assert.Equal("Apa itu X?", mc.QuestionText);
        Assert.Equal("MultipleChoice", mc.QuestionType);
        Assert.Equal(20, mc.ScoreValue);
        Assert.Equal(0, mc.Order);
        Assert.Equal("Proses", mc.ElemenTeknis);
        Assert.Equal(4, mc.Options.Count);
        Assert.Single(mc.Options.Where(o => o.IsCorrect));
        Assert.Equal("A", mc.Options[0].OptionText);

        var essay = req.Questions[1];
        Assert.Equal("Essay", essay.QuestionType);
        Assert.Equal("Kunci Y", essay.Rubrik);
        Assert.Equal(1, essay.Order);
        Assert.Empty(essay.Options);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Maps_cert()
    {
        // Manual + Permanent → ManualCertNumber carried, CertValidUntil null
        var manual = new InjectAssessmentViewModel
        {
            CertMode = InjectCertMode.Manual,
            ManualCertNumber = "KPB/009/VI/2026",
            CertPermanent = true,
            UserIds = new() { "u1" }
        };
        var rm = InjectAssessmentController.MapToRequest(manual, Nip(("u1", "NIP001")), NoAnswers());
        Assert.Single(rm.Workers);
        Assert.Equal("KPB/009/VI/2026", rm.Workers[0].ManualCertNumber);
        Assert.Null(rm.Workers[0].CertValidUntil);
        Assert.Equal(InjectCertMode.Manual, rm.CertMode);

        // Auto → ManualCertNumber null (ignored); ValidUntil mapped from DateTime
        var auto = new InjectAssessmentViewModel
        {
            CertMode = InjectCertMode.Auto,
            ManualCertNumber = "should-be-ignored",
            CertPermanent = false,
            CertValidUntil = new DateTime(2027, 1, 1),
            UserIds = new() { "u1" }
        };
        var ra = InjectAssessmentController.MapToRequest(auto, Nip(("u1", "NIP001")), NoAnswers());
        Assert.Null(ra.Workers[0].ManualCertNumber);
        Assert.Equal(new DateOnly(2027, 1, 1), ra.Workers[0].CertValidUntil);

        // None → ManualCertNumber null
        var none = new InjectAssessmentViewModel
        {
            CertMode = InjectCertMode.None,
            ManualCertNumber = "x",
            UserIds = new() { "u1" }
        };
        var rn = InjectAssessmentController.MapToRequest(none, Nip(("u1", "NIP001")), NoAnswers());
        Assert.Null(rn.Workers[0].ManualCertNumber);
        Assert.Equal(InjectCertMode.None, rn.CertMode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Resolves_UserIds_to_NIP()
    {
        var vm = new InjectAssessmentViewModel { UserIds = new() { "u1", "u2", "u3" } };
        var dict = Nip(("u1", "NIP001"), ("u2", "NIP002"));   // u3 absent (no NIP)

        var req = InjectAssessmentController.MapToRequest(vm, dict, NoAnswers());

        Assert.Equal(2, req.Workers.Count);   // u3 skipped (null-NIP)
        Assert.Equal(new[] { "NIP001", "NIP002" }, req.Workers.Select(w => w.Nip).ToArray());
        Assert.All(req.Workers, w => Assert.Empty(w.Answers));   // Answers empty in 394
    }
}
