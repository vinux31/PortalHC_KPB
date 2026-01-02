namespace HcPortal.Models
{
    public class DashboardViewModel
    {
        public int TotalIdp { get; set; }
        public int IdpGrowth { get; set; } // e.g. +12
        public int CompletionRate { get; set; } // %
        public string CompletionTarget { get; set; } = "80% (Q4)";
        public int PendingAssessments { get; set; }
        public int BudgetUsedPercent { get; set; }
        public string BudgetUsedText { get; set; } = ""; // "Rp 450jt / 1M"

        // Chart Data Lists
        public List<string> ChartLabels { get; set; } = new List<string>();
        public List<int> ChartTarget { get; set; } = new List<int>();
        public List<int> ChartRealization { get; set; } = new List<int>();

        // Compliance List
        public List<UnitCompliance> TopUnits { get; set; } = new List<UnitCompliance>();
    }

    public class UnitCompliance
    {
        public string UnitName { get; set; } = "";
        public int Percentage { get; set; }
        public string ColorClass { get; set; } = "bg-primary"; // Helper for UI color
    }
}
