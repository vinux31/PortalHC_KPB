using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HcPortal.Models
{
    public class BudgetItem
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = "Training"; // "Training" | "Assessment"

        [Required]
        [MaxLength(200)]
        public string Judul { get; set; } = "";

        [MaxLength(100)]
        public string? Kategori { get; set; }

        [MaxLength(100)]
        public string? SubKategori { get; set; }

        [Required]
        public int TahunAnggaran { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int JumlahPeserta { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BiayaPerOrang { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimasiBiayaTotal { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RealisasiBiaya { get; set; }

        [MaxLength(200)]
        public string? Vendor { get; set; }

        [MaxLength(500)]
        public string? Catatan { get; set; }

        [Required]
        public string CreatedByUserId { get; set; } = "";

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
