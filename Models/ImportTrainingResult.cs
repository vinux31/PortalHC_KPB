namespace HcPortal.Models
{
    public class ImportTrainingResult
    {
        public string NIP { get; set; } = "";
        public string Judul { get; set; } = "";
        public string Status { get; set; } = "";  // "Success" or "Error"
        public string Message { get; set; } = "";
    }
}
