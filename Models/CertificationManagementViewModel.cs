namespace HcPortal.Models;

// ============================================================
// Enums
// ============================================================

public enum RecordType
{
    Training,
    Assessment
}

public enum CertificateStatus
{
    Aktif,
    AkanExpired,
    Expired,
    Permanent
}

// ============================================================
// Flat row unifying TrainingRecord and AssessmentSession
// ============================================================

public class SertifikatRow
{
    public int SourceId { get; set; }
    public string WorkerId { get; set; } = "";
    public RecordType RecordType { get; set; }
    public string NamaWorker { get; set; } = "";
    public string? Bagian { get; set; }
    public string? Unit { get; set; }
    public string Judul { get; set; } = "";
    public string? Kategori { get; set; }
    public string? SubKategori { get; set; }
    public string? NomorSertifikat { get; set; }
    public DateTime? TanggalTerbit { get; set; }
    public DateOnly? ValidUntil { get; set; }  // Phase 327 — DateOnly migrasi P04
    public CertificateStatus Status { get; set; }
    public string? SertifikatUrl { get; set; }

    /// <summary>
    /// True jika ada sesi/record renewal yang lulus mengarah ke sertifikat ini.
    /// Dihitung oleh BuildSertifikatRowsAsync. Orthogonal terhadap Status.
    /// </summary>
    public bool IsRenewed { get; set; }

    /// <summary>
    /// Derives certificate status from ValidUntil and CertificateType.
    /// For AssessmentSession rows pass certificateType: null — ValidUntil==null (cert lulus tanpa
    /// kedaluwarsa) yields Aktif (Permanen secara efektif), BUKAN Expired (Phase 382 CERT-01 / D-08).
    /// certificateType "Permanent" tetap yields Permanent. Threshold 30 hari = TrainingRecord.IsExpiringSoon.
    /// </summary>
    public static CertificateStatus DeriveCertificateStatus(DateOnly? validUntil, string? certificateType)
    {
        // Phase 327 P04 — DateOnly signature + DayNumber arithmetic + UtcNow alignment (D-06, D-09).
        if (certificateType == "Permanent")
            return CertificateStatus.Permanent;
        if (validUntil == null)
            return CertificateStatus.Aktif; // CERT-01 (D-08): cert lulus tanpa kedaluwarsa = Aktif/Permanen (BUKAN Expired) — single-source, semua consumer ikut via Status enum
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var days = validUntil.Value.DayNumber - today.DayNumber;
        if (days < 0) return CertificateStatus.Expired;
        if (days <= 30) return CertificateStatus.AkanExpired;
        return CertificateStatus.Aktif;
    }

    /// <summary>
    /// #25 (D-03): child-category Name → parent Name lookup dengan GroupBy-dedup (cegah ArgumentException/500
    /// pada duplicate child Name lintas parent berbeda). Single-source dikonsumsi CMP + CDP (anti-drift) —
    /// home NETRAL di SertifikatRow (CMP/CDP plain Controller, bukan turunan AdminBaseController).
    /// </summary>
    public static Dictionary<string, string> BuildParentNameLookup(IEnumerable<(int Id, string Name, int? ParentId)> categories)
    {
        var list = categories as IList<(int Id, string Name, int? ParentId)> ?? categories.ToList();
        var byId = list.ToDictionary(c => c.Id);
        return list
            .Where(c => c.ParentId != null && byId.ContainsKey(c.ParentId.Value))
            .GroupBy(c => c.Name)
            .ToDictionary(g => g.Key, g => byId[g.First().ParentId!.Value].Name);
    }
}

// ============================================================
// Certificate History chain grouping
// ============================================================

public class CertificateChainGroup
{
    public string ChainTitle { get; set; } = "";
    public List<SertifikatRow> Certificates { get; set; } = new();
    public DateOnly? LatestValidUntil { get; set; }  // Phase 327 — DateOnly migrasi P04
}

// ============================================================
// Page ViewModel
// ============================================================

public class CertificationManagementViewModel
{
    public List<SertifikatRow> Rows { get; set; } = new();
    public int TotalCount { get; set; } = 0;
    public int AktifCount { get; set; } = 0;
    public int AkanExpiredCount { get; set; } = 0;
    public int ExpiredCount { get; set; } = 0;
    public int PermanentCount { get; set; } = 0;
    public int CurrentPage { get; set; } = 0;
    public int TotalPages { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public int RoleLevel { get; set; } = 1;
}

// ============================================================
// Grouped View — Renewal Certificate accordion per judul
// ============================================================

public class RenewalGroup
{
    public string GroupKey { get; set; } = "";
    public string Judul { get; set; } = "";
    public string? Kategori { get; set; }
    public string? SubKategori { get; set; }
    public int TotalCount { get; set; }
    public int ExpiredCount { get; set; }
    public int AkanExpiredCount { get; set; }
    public DateOnly? MinValidUntil { get; set; }  // Phase 327 — DateOnly migrasi P04
    public List<SertifikatRow> Rows { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// ============================================================
// Grouped sertifikat per judul (for CMP grouped view)
// ============================================================

public class SertifikatGroupRow
{
    public string Judul { get; set; } = "";
    public string? Kategori { get; set; }
    public string? SubKategori { get; set; }
    public int JumlahWorker { get; set; }
}

public class SertifikatGroupViewModel
{
    public List<SertifikatGroupRow> Groups { get; set; } = new();
    public int TotalCount { get; set; }
    public int MandatoryCount { get; set; }
    public int NonMandatoryCount { get; set; }
    public int OjtCount { get; set; }
    public int IhtCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int RoleLevel { get; set; } = 1;
}

public class SertifikatDetailViewModel
{
    public string Judul { get; set; } = "";
    public string? Kategori { get; set; }
    public string? SubKategori { get; set; }
    public List<SertifikatRow> Rows { get; set; } = new();
    public int TotalCount { get; set; }
    public int AktifCount { get; set; }
    public int AkanExpiredCount { get; set; }
    public int ExpiredCount { get; set; }
    public int PermanentCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int RoleLevel { get; set; } = 1;
}

public class RenewalGroupViewModel
{
    public List<RenewalGroup> Groups { get; set; } = new();
    public int TotalExpiredCount { get; set; }
    public int TotalAkanExpiredCount { get; set; }
    public bool IsFiltered { get; set; }
}
