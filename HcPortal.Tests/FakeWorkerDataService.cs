using HcPortal.Models;
using HcPortal.Services;

namespace HcPortal.Tests;

/// <summary>
/// Phase 382 — fake IWorkerDataService untuk test GradingService tanpa men-drag UserManager.
/// GradingService.GradeAndCompleteAsync hanya memanggil NotifyIfGroupCompleted (no-op di sini);
/// method read lain tak dipanggil pada path grading non-Proton yang diuji.
/// </summary>
internal sealed class FakeWorkerDataService : IWorkerDataService
{
    public Task<List<UnifiedTrainingRecord>> GetUnifiedRecords(string userId)
        => Task.FromResult(new List<UnifiedTrainingRecord>());

    public Task<(List<AllWorkersHistoryRow> assessment, List<AllWorkersHistoryRow> training)> GetAllWorkersHistory(
        IEnumerable<string>? workerIds = null,
        DateTime? from = null,
        DateTime? to = null,
        string? category = null,
        string? subCategory = null)
        => Task.FromResult((new List<AllWorkersHistoryRow>(), new List<AllWorkersHistoryRow>()));

    public Task<List<WorkerTrainingStatus>> GetWorkersInSection(string? section, string? unitFilter = null, string? category = null, string? search = null, string? statusFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? subCategory = null, string? searchScope = null)
        => Task.FromResult(new List<WorkerTrainingStatus>());

    public Task NotifyIfGroupCompleted(AssessmentSession completedSession)
        => Task.CompletedTask;
}
