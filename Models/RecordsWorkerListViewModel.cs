namespace HcPortal.Models;

/// <summary>
/// Wrapper ViewModel for RecordsWorkerList view (Phase 40).
/// Mirrors CDPDashboardViewModel pattern — wraps two sub-lists.
/// </summary>
public class RecordsWorkerListViewModel
{
    // Existing worker-list tab data (was the entire model before Phase 40)
    public List<WorkerTrainingStatus> Workers { get; set; } = new();

    // New History tab data — all workers, merged, sorted by Date descending
    public List<AllWorkersHistoryRow> History { get; set; } = new();
}
