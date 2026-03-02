namespace HcPortal.Models
{
    // KKJ Matrix Bagian (section grouping — used for tab navigation and file grouping)
    public class KkjBagian
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";           // e.g. "RFCC", "GAST", "NGP", "DHT/HMU"
        public int DisplayOrder { get; set; } = 0;

        // Navigation collection for KKJ files
        public ICollection<KkjFile> Files { get; set; } = new List<KkjFile>();
    }

    // KKJ File — represents an uploaded PDF/Excel file for a given bagian
    public class KkjFile
    {
        public int Id { get; set; }
        public int BagianId { get; set; }
        public KkjBagian Bagian { get; set; } = null!;
        public string FileName { get; set; } = "";      // Original filename (display)
        public string FilePath { get; set; } = "";      // Relative path: /uploads/kkj/{bagianId}/{safeName}
        public long FileSizeBytes { get; set; }
        public string FileType { get; set; } = "";      // "pdf", "xlsx", "xls"
        public string? Keterangan { get; set; }         // Optional description from upload form
        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
        public string UploaderName { get; set; } = "";
        public bool IsArchived { get; set; } = false;   // True = moved to history
    }

    // 2. CLASS UNTUK HALAMAN MAPPING (GAP ANALYSIS) - KITA KEMBALIKAN
    public class GapAnalysisItem
    {
        public string? Kompetensi { get; set; }
        public int LevelTarget { get; set; }
        public string? AktivitasIdp { get; set; }
        public string? Status { get; set; } // "Covered", "Gap", "Partial"
    }

    // 3. CLASS BARU UNTUK TABEL CPDP (MAPPING)
    public class CpdpItem
    {
        public int Id { get; set; }           // Primary Key for EF Core
        public string No { get; set; } = "";  // Display number (can be "5.1", "5.2", etc.)
        public string NamaKompetensi { get; set; } = "";
        public string IndikatorPerilaku { get; set; } = "";
        public string DetailIndikator { get; set; } = "";
        public string Silabus { get; set; } = "";
        public string TargetDeliverable { get; set; } = "";
        public string Status { get; set; } = "";
        public string Section { get; set; } = "";   // RFCC | GAST | NGP | DHT
        // Removed old properties: Implementasi, KodeSub, SubOps, SubPanel
    }

}
