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
        public int CompletionPercentage
        {
            get
            {
                if (TotalTrainings == 0) return 0;
                return (int)((double)CompletedTrainings / TotalTrainings * 100);
            }
        }
        
        // Computed property: Has expiring certificates
        public bool HasExpiringCertificates => ExpiringSoonTrainings > 0;
    }
}
