// Phase 367 Plan 03 Task 1 — badge recompute (#16/#17, D-01): CompletionDisplayText == jumlah baris tampil per jenis.
// Pure model unit (tanpa DB): konstruksi WorkerTrainingStatus dengan list terkontrol, assert string badge.
// Membuktikan badge tak kontradiksi dgn list (semua AssessmentSessions + semua TrainingRecords), termasuk baris not-passed.
using System.Collections.Generic;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class BadgeRecomputeTests
{
    private static AssessmentSession Session(bool manual, bool? passed = true) =>
        new AssessmentSession { UserId = "u1", Title = "A", IsManualEntry = manual, IsPassed = passed };

    private static TrainingRecord Training(string status = "Valid") =>
        new TrainingRecord { UserId = "u1", Judul = "T", Status = status };

    // 3 assessment tampil (2 manual + 1 online) + 2 training tampil → badge 3 assessment + 2 training (= baris tampil).
    [Fact]
    public void Badge_CountsAllDisplayedRows_PerType()
    {
        var w = new WorkerTrainingStatus
        {
            AssessmentSessions = new List<AssessmentSession> { Session(true), Session(true), Session(false) },
            TrainingRecords = new List<TrainingRecord> { Training(), Training() }
        };
        Assert.Equal("5 record (3 assessment + 2 training)", w.CompletionDisplayText);
    }

    // Baris not-passed (online IsPassed=false, training Failed) TETAP tampil → ikut dihitung badge (BUKAN hanya IsPassed/Valid).
    [Fact]
    public void Badge_CountsNotPassedRows_Too()
    {
        var w = new WorkerTrainingStatus
        {
            AssessmentSessions = new List<AssessmentSession> { Session(false, passed: false) },
            TrainingRecords = new List<TrainingRecord> { Training("Failed") }
        };
        Assert.Equal("2 record (1 assessment + 1 training)", w.CompletionDisplayText);
    }

    [Fact]
    public void Badge_NoRecords_ShowsZero_NoContradiction()
    {
        var w = new WorkerTrainingStatus();
        Assert.Equal("0 record (0 assessment + 0 training)", w.CompletionDisplayText);
    }

    // Pasca cascade delete (list menyusut) → badge recompute turun mengikuti.
    [Fact]
    public void Badge_Recompute_ShrinksWhenListShrinks()
    {
        var sessions = new List<AssessmentSession> { Session(true), Session(true), Session(false) };
        var w = new WorkerTrainingStatus { AssessmentSessions = sessions, TrainingRecords = new List<TrainingRecord>() };
        Assert.Equal("3 record (3 assessment + 0 training)", w.CompletionDisplayText);

        sessions.RemoveAt(0); // simulasi 1 baris terhapus cascade
        Assert.Equal("2 record (2 assessment + 0 training)", w.CompletionDisplayText);
    }
}
