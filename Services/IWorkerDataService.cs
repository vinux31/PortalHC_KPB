using HcPortal.Models;

namespace HcPortal.Services
{
    public interface IWorkerDataService
    {
        Task<List<UnifiedTrainingRecord>> GetUnifiedRecords(string userId);
        Task<(List<AllWorkersHistoryRow> assessment, List<AllWorkersHistoryRow> training)> GetAllWorkersHistory();
        Task<List<WorkerTrainingStatus>> GetWorkersInSection(string? section, string? unitFilter = null, string? category = null, string? search = null, string? statusFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null);
        Task NotifyIfGroupCompleted(AssessmentSession completedSession);
    }
}
