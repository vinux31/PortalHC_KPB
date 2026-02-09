namespace HcPortal.Models
{
    public class IdpItem
    {
        public int Id { get; set; }
        
        // Foreign Key to User
        public string UserId { get; set; } = "";
        public ApplicationUser? User { get; set; }
        
        // Tambahkan tanda tanya (?) setelah string
        public string? Kompetensi { get; set; } 
        public string? SubKompetensi { get; set; } // Baru
        public string? Deliverable { get; set; } // Baru
        
        // Kolom lama yg mungkin masih oke disimpan, atau bisa kita abaikan di View baru
        public string? Aktivitas { get; set; }
        public string? Metode { get; set; }
        public DateTime DueDate { get; set; }
        public string? Status { get; set; } // Status Overall
        
        public string? Evidence { get; set; } // Path file
        
        // Approval Status: "Approved", "Rejected", "Pending", "Not Started"
        public string? ApproveSrSpv { get; set; }
        public string? ApproveSectionHead { get; set; }
        public string? ApproveHC { get; set; }
    }
}