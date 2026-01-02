namespace HcPortal.Models
{
    public class IdpItem
    {
        public int Id { get; set; }
        // Tambahkan tanda tanya (?) setelah string
        public string? Kompetensi { get; set; } 
        public string? Aktivitas { get; set; }
        public string? Metode { get; set; }
        public DateTime DueDate { get; set; }
        public string? Status { get; set; }
        public string? Evidence { get; set; }
    }
}