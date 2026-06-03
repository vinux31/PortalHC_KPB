// Phase 341 — ViewModel untuk page /Admin/ManageOrgLevelLabels (D-10 server-render Razor model-bound).
namespace HcPortal.Models.ViewModels
{
    public class ManageOrgLevelLabelsViewModel
    {
        public List<LabelRowVM> Rows { get; set; } = new();
        public int MaxConfigured { get; set; }
        public int MaxUsed { get; set; }
        public int NextAddLevel { get; set; }
    }

    public class LabelRowVM
    {
        public int Level { get; set; }
        public string? Label { get; set; }  // null = "(belum diset)" buffer row
        public bool IsHighest { get; set; }
        public bool IsUsed { get; set; }
        public bool CanDelete { get; set; }
    }
}
