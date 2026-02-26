namespace HcPortal.Models;

/// <summary>
/// Wrapper ViewModel for RecordsWorkerList view (Phase 46).
/// Mirrors CDPDashboardViewModel pattern — wraps sub-lists for Daftar Pekerja, Riwayat Assessment, Riwayat Training.
/// </summary>
public class RecordsWorkerListViewModel
{
    // Existing worker-list tab data (was the entire model before Phase 40)
    public List<WorkerTrainingStatus> Workers { get; set; } = new();

    // Phase 46: Split History into Assessment + Training sub-tabs
    public List<AllWorkersHistoryRow> AssessmentHistory { get; set; } = new();
    public List<AllWorkersHistoryRow> TrainingHistory { get; set; } = new();

    // Distinct assessment titles for the filter dropdown
    public List<string> AssessmentTitles { get; set; } = new();
}
