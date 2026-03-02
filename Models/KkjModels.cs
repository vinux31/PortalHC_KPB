namespace HcPortal.Models
{
    // 1. CLASS UNTUK HALAMAN SUSUNAN KKJ (MATRIX)
    public class KkjMatrixItem
    {
        public int Id { get; set; }           // Primary Key for EF Core
        public int No { get; set; }           // Display number
        public string SkillGroup { get; set; } = "";
        public string SubSkillGroup { get; set; } = "";
        public string Indeks { get; set; } = "";
        public string Kompetensi { get; set; } = "";

        // Bagian grouping (FK by name to KkjBagian.Name)
        public string Bagian { get; set; } = "";   // e.g. "RFCC", "GAST", "NGP", "DHT/HMU"

        // Navigation collection for dynamic target values
        public ICollection<KkjTargetValue> TargetValues { get; set; } = new List<KkjTargetValue>();
    }

    // KKJ Matrix Bagian (section grouping with dynamic column definitions)
    public class KkjBagian
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";           // e.g. "RFCC", "GAST", "NGP", "DHT/HMU"
        public int DisplayOrder { get; set; } = 0;

        // Navigation collection for dynamic column definitions
        public ICollection<KkjColumn> Columns { get; set; } = new List<KkjColumn>();
    }

    // Target column definition per Bagian (replaces hardcoded 15 Label_* columns)
    public class KkjColumn
    {
        public int Id { get; set; }
        public int BagianId { get; set; }           // FK to KkjBagian
        public string Name { get; set; } = "";       // e.g., "Section Head", "Operator Process Water"
        public int DisplayOrder { get; set; } = 0;

        // Navigation properties
        public KkjBagian Bagian { get; set; } = null!;
        public ICollection<KkjTargetValue> TargetValues { get; set; } = new List<KkjTargetValue>();
        public ICollection<PositionColumnMapping> PositionMappings { get; set; } = new List<PositionColumnMapping>();
    }

    // Target value for a specific KKJ item + column cell (replaces hardcoded 15 Target_* columns)
    public class KkjTargetValue
    {
        public int Id { get; set; }
        public int KkjMatrixItemId { get; set; }    // FK to KkjMatrixItem
        public int KkjColumnId { get; set; }         // FK to KkjColumn
        public string Value { get; set; } = "-";     // Typically "1"-"5" or "-"

        // Navigation properties
        public KkjMatrixItem KkjMatrixItem { get; set; } = null!;
        public KkjColumn KkjColumn { get; set; } = null!;
    }

    // Maps a user position string to a KkjColumn (replaces hardcoded Dictionary in PositionTargetHelper)
    public class PositionColumnMapping
    {
        public int Id { get; set; }
        public string Position { get; set; } = "";  // e.g., "Section Head", "Operator GSH 8-11"
        public int KkjColumnId { get; set; }         // FK to KkjColumn

        // Navigation property
        public KkjColumn KkjColumn { get; set; } = null!;
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
