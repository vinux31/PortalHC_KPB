namespace HcPortal.Models
{
    public class ImportTrainingResult
    {
        public string NIP { get; set; } = "";
        public string Judul { get; set; } = "";
        public string Status { get; set; } = "";  // "Success" | "Error" | "Skip" (#12 D-02: dup EXACT dilewati)
        public string Message { get; set; } = "";
    }
}
