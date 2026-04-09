using System.ComponentModel.DataAnnotations;

namespace HcPortal.Models
{
    public class MaintenanceMode
    {
        public int Id { get; set; }
        public bool IsEnabled { get; set; }

        [Required]
        public string Message { get; set; } = "";

        public DateTime? ScheduledStartTime { get; set; }
        public DateTime? ScheduledEndTime { get; set; }

        // "All" atau comma-separated module keys: "CMP,CDP,Proton,Admin,Home,Account"
        public string Scope { get; set; } = "All";

        public string? ActivatedByUserId { get; set; }
        public string? ActivatedByName { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? DeactivatedAt { get; set; }
    }
}
