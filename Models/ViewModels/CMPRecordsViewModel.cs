using HcPortal.Models;

namespace HcPortal.Models.ViewModels
{
    public class CMPRecordsViewModel
    {
        // Phase 377 (D-03): nullable — saat impersonate mode-role, User=null (render kosong+hint, BUKAN identitas admin).
        public ApplicationUser? User { get; set; }
        public int RoleLevel { get; set; }
        public List<UnifiedTrainingRecord> UnifiedRecords { get; set; } = new();
        public int AssessmentCount { get; set; }
        public int TrainingCount { get; set; }
        public int TotalCount { get; set; }
        public List<int> YearOptions { get; set; } = new();
    }
}
