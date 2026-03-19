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
    public RecordType RecordType { get; set; }
    public string NamaWorker { get; set; } = "";
    public string? Bagian { get; set; }
    public string? Unit { get; set; }
    public string Judul { get; set; } = "";
    public string? Kategori { get; set; }
    public string? SubKategori { get; set; }
    public string? NomorSertifikat { get; set; }
    public DateTime? TanggalTerbit { get; set; }
    public DateTime? ValidUntil { get; set; }
    public CertificateStatus Status { get; set; }
    public string? SertifikatUrl { get; set; }

    /// <summary>
    /// True jika ada sesi/record renewal yang lulus mengarah ke sertifikat ini.
    /// Dihitung oleh BuildSertifikatRowsAsync. Orthogonal terhadap Status.
    /// </summary>
    public bool IsRenewed { get; set; }

    /// <summary>
    /// Derives certificate status from ValidUntil and CertificateType.
    /// For AssessmentSession rows pass certificateType: null — ValidUntil==null yields Permanent.
    /// Threshold of 30 days matches TrainingRecord.IsExpiringSoon.
    /// </summary>
    public static CertificateStatus DeriveCertificateStatus(DateTime? validUntil, string? certificateType)
    {
        if (certificateType == "Permanent" || validUntil == null)
            return CertificateStatus.Permanent;
        var days = (validUntil.Value - DateTime.Now).Days;
        if (days < 0) return CertificateStatus.Expired;
        if (days <= 30) return CertificateStatus.AkanExpired;
        return CertificateStatus.Aktif;
    }
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
