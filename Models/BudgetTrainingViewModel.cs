namespace HcPortal.Models
{
    public class BudgetTrainingViewModel
    {
        public List<BudgetItem> Items { get; set; } = new();

        // Filter state
        public int? FilterTahun { get; set; }
        public string? FilterType { get; set; }
        public string? FilterKategori { get; set; }
        public string? Search { get; set; }

        // Summary
        public decimal TotalRencana { get; set; }
        public decimal TotalRealisasi { get; set; }
        public decimal Variance => TotalRencana - TotalRealisasi;
        public int TotalItems { get; set; }

        // Chart data
        public List<BudgetChartData> ChartData { get; set; } = new();

        // Dropdown options
        public List<int> AvailableTahun { get; set; } = new();
        public List<string> AvailableKategori { get; set; } = new();

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;
    }

    public class BudgetChartData
    {
        public string Kategori { get; set; } = "";
        public decimal Rencana { get; set; }
        public decimal Realisasi { get; set; }
    }

}
