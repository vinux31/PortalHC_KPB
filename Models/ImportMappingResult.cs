namespace HcPortal.Models
{
    /// <summary>
    /// Result item for each row processed during bulk coach-coachee mapping import
    /// </summary>
    public class ImportMappingResult
    {
        public int RowNum { get; set; }
        public string NipCoach { get; set; } = string.Empty;
        public string NipCoachee { get; set; } = string.Empty;

        /// <summary>Success, Error, Skip, or Reactivated</summary>
        public string Status { get; set; } = "Pending";
        public string Message { get; set; } = string.Empty;
    }
}
