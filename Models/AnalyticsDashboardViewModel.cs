namespace HcPortal.Models
{
    // ============================================================
    // ViewModel untuk initial page load Analytics Dashboard
    // ============================================================
    public class AnalyticsDashboardViewModel
    {
        public List<string> Sections { get; set; } = new();
        public List<string> Categories { get; set; } = new();
    }

    // ============================================================
    // JSON response dari GET /CMP/GetAnalyticsData
    // ============================================================
    public class AnalyticsDataResult
    {
        public List<FailRateItem> FailRate { get; set; } = new();
        public List<TrendItem> Trend { get; set; } = new();
        public List<EtBreakdownItem> EtBreakdown { get; set; } = new();
        public List<ExpiringSoonItem> ExpiringSoon { get; set; } = new();
    }

    // ============================================================
    // Fail rate per Bagian + Kategori
    // ============================================================
    public class FailRateItem
    {
        public string Section { get; set; } = "";
        public string Category { get; set; } = "";
        public int Total { get; set; }
        public int Failed { get; set; }
        public double FailRatePercent => Total > 0 ? Failed * 100.0 / Total : 0;
    }

    // ============================================================
    // Trend assessment lulus/gagal per bulan
    // ============================================================
    public class TrendItem
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Label => $"{Year}-{Month:D2}";
        public int Passed { get; set; }
        public int Failed { get; set; }
    }

    // ============================================================
    // Rata-rata skor Elemen Teknis per kategori
    // ============================================================
    public class EtBreakdownItem
    {
        public string ElemenTeknis { get; set; } = "";
        public string Category { get; set; } = "";
        public double AvgPct { get; set; }
        public double MinPct { get; set; }
        public double MaxPct { get; set; }
        public int SampleCount { get; set; }
    }

    // ============================================================
    // Sertifikat yang akan expired dalam 30 hari
    // ============================================================
    public class ExpiringSoonItem
    {
        public string NamaPekerja { get; set; } = "";
        public string NamaSertifikat { get; set; } = "";
        public DateTime TanggalExpired { get; set; }
        public string SectionUnit { get; set; } = "";
    }
}
