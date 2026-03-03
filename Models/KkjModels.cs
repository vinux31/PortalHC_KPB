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

    // CPDP File — represents an uploaded PDF/Excel file for a given bagian (mirrors KkjFile)
    public class CpdpFile
    {
        public int Id { get; set; }
        public int BagianId { get; set; }
        public KkjBagian Bagian { get; set; } = null!;
        public string FileName { get; set; } = "";      // Original filename (display)
        public string FilePath { get; set; } = "";      // Relative path: /uploads/cpdp/{bagianId}/{safeName}
        public long FileSizeBytes { get; set; }
        public string FileType { get; set; } = "";      // "pdf", "xlsx", "xls"
        public string? Keterangan { get; set; }         // Optional description from upload form
        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
        public string UploaderName { get; set; } = "";
        public bool IsArchived { get; set; } = false;   // True = moved to history
    }


}
