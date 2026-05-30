using HcPortal.Models;

namespace HcPortal.Services
{
    public interface IWorkerDataService
    {
        Task<List<UnifiedTrainingRecord>> GetUnifiedRecords(string userId);
        Task<(List<AllWorkersHistoryRow> assessment, List<AllWorkersHistoryRow> training)> GetAllWorkersHistory(
            IEnumerable<string>? workerIds = null,
            DateTime? from = null,
            DateTime? to = null,
            string? category = null,
            string? subCategory = null);
        Task<List<WorkerTrainingStatus>> GetWorkersInSection(string? section, string? unitFilter = null, string? category = null, string? search = null, string? statusFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? subCategory = null);
        Task NotifyIfGroupCompleted(AssessmentSession completedSession);
    }
}
