namespace HcPortal.Models;

/// <summary>
/// Flat projection for one training event across all workers.
/// Used in the History tab of RecordsWorkerList (Phase 40).
/// </summary>
public class AllWorkersHistoryRow
{
    public string WorkerName { get; set; } = "";
    public string? WorkerNIP { get; set; }

    // "Manual" or "Assessment Online"
    public string RecordType { get; set; } = "";

    public string Title { get; set; } = "";

    // Sort key: TanggalMulai ?? Tanggal (training) or CompletedAt ?? Schedule (assessment)
    public DateTime Date { get; set; }

    // Manual-only
    public string? Penyelenggara { get; set; }

    // Assessment-only
    public int? Score { get; set; }
    public bool? IsPassed { get; set; }
}
