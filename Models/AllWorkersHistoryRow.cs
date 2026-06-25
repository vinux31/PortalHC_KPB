namespace HcPortal.Models;

/// <summary>
/// Flat projection for one training event across all workers.
/// Used in the History tab of RecordsWorkerList (Phase 40).
/// </summary>
public class AllWorkersHistoryRow
{
    public string WorkerId { get; set; } = "";
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

    // Phase 46: Attempt sequencing for Riwayat Assessment sub-tab
    public int? AttemptNumber { get; set; }

    // Phase 337 CMP-05: Category + SubCategory for filter parity di ExportRecordsTeamTraining
    public string? Kategori { get; set; }
    public string? SubKategori { get; set; }

    // Phase 338 CIL-03 (D-09): SessionId untuk drill-down /CMP/Results/{id}
    // Populate hanya di current Completed branch (archivedRows = null karena history pakai
    // AssessmentAttemptHistory.Id yang BUKAN AssessmentSession.Id — drill-down N/A untuk archived).
    // Training branch tetap null (training tidak punya session concept).
    public int? SessionId { get; set; }

    // RTK-12 (Phase 407): tandai baris percobaan aktif saat ini untuk badge "Percobaan saat ini".
    // Mirror RiwayatAttemptViewModel.IsCurrent — set true hanya untuk current Completed branch.
    public bool IsCurrentAttempt { get; set; }
}
