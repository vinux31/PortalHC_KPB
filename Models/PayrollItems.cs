namespace HcPortal.Models
{
    public class PayrollItem
    {
        public int Id { get; set; }
        public string Periode { get; set; } = ""; // Contoh: "Desember 2025"
        public DateTime TanggalTransfer { get; set; }
        
        // Komponen Gaji (Simulasi)
        public decimal GajiPokok { get; set; }
        public decimal Tunjangan { get; set; }
        public decimal Potongan { get; set; }
        
        // Total
        public decimal TakeHomePay => (GajiPokok + Tunjangan) - Potongan;
        
        public string Status { get; set; } = ""; // "Paid", "Processing"
    }
}