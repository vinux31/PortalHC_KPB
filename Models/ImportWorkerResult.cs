namespace HcPortal.Models
{
    /// <summary>
    /// Result item for each row processed during bulk worker import
    /// </summary>
    public class ImportWorkerResult
    {
        public int RowNum { get; set; }
        public string Nama { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        /// <summary>Success, Error, Skip, or PerluReview</summary>
        public string Status { get; set; } = "Pending";
        public string Message { get; set; } = string.Empty;

        /// <summary>Set when Status is PerluReview — the Id of the existing inactive user that matches the imported email</summary>
        public string? ExistingUserId { get; set; }
    }
}
