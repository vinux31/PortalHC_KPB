using HcPortal.Models;

namespace HcPortal.Models.ViewModels
{
    public class CMPRecordsViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public int RoleLevel { get; set; }
        public List<UnifiedTrainingRecord> UnifiedRecords { get; set; } = new();
        public int AssessmentCount { get; set; }
        public int TrainingCount { get; set; }
        public int TotalCount { get; set; }
        public List<int> YearOptions { get; set; } = new();
    }
}
