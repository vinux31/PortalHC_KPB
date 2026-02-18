namespace HcPortal.Models
{
    /// <summary>
    /// Model untuk menampilkan status training per worker
    /// Digunakan di RecordsWorkerList view
    /// </summary>
    public class WorkerTrainingStatus
    {
        public string WorkerId { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
        public string? NIP { get; set; }
        public string Position { get; set; } = string.Empty;
        public string? Section { get; set; }
        public string? Unit { get; set; }
        
        // Training Statistics
        public int TotalTrainings { get; set; }
        public int CompletedTrainings { get; set; }
        public int PendingTrainings { get; set; }
        public int ExpiringSoonTrainings { get; set; }
        
        // Detailed training records
        public List<TrainingRecord> TrainingRecords { get; set; } = new List<TrainingRecord>();
        
        // Computed property: Completion percentage
        // Backing field for manual override
        private int? _completionPercentage;
        
        // Computed property with manual override support
        public int CompletionPercentage
        {
            get
            {
                if (_completionPercentage.HasValue) return _completionPercentage.Value;
                if (TotalTrainings == 0) return 0;
                return (int)((double)CompletedTrainings / TotalTrainings * 100);
            }
            set
            {
                _completionPercentage = value;
            }
        }
        
        // Computed property: Has expiring certificates
        public bool HasExpiringCertificates => ExpiringSoonTrainings > 0;

        // Phase 10: assessment completion count (CompletedTrainings already exists above)
        public int CompletedAssessments { get; set; }  // IsPassed == true count

        // Computed display string: "5 completed (3 assessments + 2 trainings)"
        // CompletedTrainings counts Status == "Passed" || Status == "Valid" (set in GetWorkersInSection)
        public string CompletionDisplayText =>
            $"{CompletedAssessments + CompletedTrainings} completed " +
            $"({CompletedAssessments} assessments + {CompletedTrainings} trainings)";
    }
}
