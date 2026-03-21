namespace HcPortal.Models
{
    // KKJ File — represents an uploaded PDF/Excel file for a given organization unit
    public class KkjFile
    {
        public int Id { get; set; }
        public int OrganizationUnitId { get; set; }
        public OrganizationUnit OrganizationUnit { get; set; } = null!;
        public string FileName { get; set; } = "";      // Original filename (display)
        public string FilePath { get; set; } = "";      // Relative path: /uploads/kkj/{bagianId}/{safeName}
        public long FileSizeBytes { get; set; }
        public string FileType { get; set; } = "";      // "pdf", "xlsx", "xls"
        public string? Keterangan { get; set; }         // Optional description from upload form
        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
        public string UploaderName { get; set; } = "";
        public bool IsArchived { get; set; } = false;   // True = moved to history
    }

    // CPDP File — represents an uploaded PDF/Excel file for a given organization unit (mirrors KkjFile)
    public class CpdpFile
    {
        public int Id { get; set; }
        public int OrganizationUnitId { get; set; }
        public OrganizationUnit OrganizationUnit { get; set; } = null!;
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
