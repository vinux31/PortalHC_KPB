namespace HcPortal.Models
{
    public class CareerHistory
    {
        public int Tahun { get; set; }
        public string Jabatan { get; set; } = "";
        public string Unit { get; set; } = "";     // Contoh: SRU, RFCC
        public string Tipe { get; set; } = "";     // "Promosi", "Mutasi", "New Hire"
        public string NoSK { get; set; } = "";     // Nomor Surat Keputusan
        public string Keterangan { get; set; } = "";
    }
}