namespace HcPortal.Models
{
    public class BudgetTrainingImportResult
    {
        public int Row { get; set; }
        public string Judul { get; set; } = "";
        public string Status { get; set; } = "";  // "Success", "Error", "Skip"
        public string Message { get; set; } = "";
    }
}
