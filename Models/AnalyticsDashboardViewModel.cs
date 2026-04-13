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
        public List<GainScoreTrendItem> GainScoreTrend { get; set; } = new();
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

    // ============================================================
    // Item Analysis Models (RPT-01, RPT-02, RPT-03)
    // ============================================================
    public class ItemAnalysisResult
    {
        public int TotalResponden { get; set; }
        public bool IsLowN { get; set; } // true jika TotalResponden < 30 (per D-04)
        public List<ItemAnalysisRow> Items { get; set; } = new();
    }

    public class ItemAnalysisRow
    {
        public int QuestionNumber { get; set; }
        public string QuestionText { get; set; } = "";
        public string QuestionType { get; set; } = "MultipleChoice";
        public double DifficultyIndex { get; set; } // p-value 0.0-1.0 (RPT-01, D-03)
        public double? DiscriminationIndex { get; set; } // Kelley D-index (RPT-02, D-03)
        public int TotalResponden { get; set; }
        public bool IsLowN { get; set; } // per D-04: warning jika < 30
        public List<DistractorRow> Distractors { get; set; } = new(); // RPT-03, D-05
    }

    public class DistractorRow
    {
        public string OptionText { get; set; } = "";
        public bool IsCorrect { get; set; }
        public int Count { get; set; }
        public double Percent { get; set; }
    }

    // ============================================================
    // Gain Score Models (RPT-04, RPT-07)
    // ============================================================
    public class GainScoreResult
    {
        public List<GainScorePerWorker> PerWorker { get; set; } = new(); // D-07 view 1
        public List<GainScorePerElemen> PerElemen { get; set; } = new(); // D-07 view 2
        public List<GroupComparisonItem> GroupComparison { get; set; } = new(); // RPT-07
    }

    public class GainScorePerWorker
    {
        public string NamaPekerja { get; set; } = "";
        public string NIP { get; set; } = "";
        public string Section { get; set; } = "";
        public double PreScore { get; set; }
        public double PostScore { get; set; }
        public double GainScore { get; set; }
    }

    public class GainScorePerElemen
    {
        public string ElemenTeknis { get; set; } = "";
        public double AvgPre { get; set; }
        public double AvgPost { get; set; }
        public double AvgGain { get; set; }
    }

    public class GroupComparisonItem
    {
        public string GroupName { get; set; } = "";
        public int WorkerCount { get; set; }
        public double AvgPreScore { get; set; }
        public double AvgPostScore { get; set; }
        public double AvgGainScore { get; set; }
    }

    // ============================================================
    // Assessment List untuk dropdown PrePostTest
    // ============================================================
    public class PrePostAssessmentListItem
    {
        public int LinkedGroupId { get; set; }
        public string Title { get; set; } = "";
        public int TotalWorker { get; set; }
    }

    // ============================================================
    // Summary aggregates untuk Analytics Dashboard cards
    // ============================================================
    public class AnalyticsSummaryResult
    {
        public int TotalSessions { get; set; }
        public double PassRate { get; set; }
        public int ExpiringCount { get; set; }
        public double AvgGainScore { get; set; }
    }

    // ============================================================
    // Drill-down detail per Bagian + Kategori
    // ============================================================
    public class DrillDownItem
    {
        public string NamaPekerja { get; set; } = "";
        public double Skor { get; set; }
        public DateTime TanggalAssessment { get; set; }
        public string Status { get; set; } = "";
    }

    // ============================================================
    // Gain Score Trend per bulan (D-11, D-12, RPT-06)
    // ============================================================
    public class GainScoreTrendItem
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Label => $"{Year}-{Month:D2}";
        public double AvgGainScore { get; set; }
        public int SampleCount { get; set; }
    }
}
