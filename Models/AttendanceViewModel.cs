namespace HcPortal.Models
{
    public class AttendanceViewModel
    {
        public int TotalDays { get; set; }
        public int TotalOvertimeHours { get; set; }
        public string CurrentPeriod { get; set; } = "December 2025";
        public List<AttendanceLog> Logs { get; set; } = new List<AttendanceLog>();
        public List<string> AvailablePeriods { get; set; } = new List<string>();
    }
}
