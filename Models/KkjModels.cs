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
        
        // Target Level per Jabatan
        // Target Level per Jabatan
        // 1. Section Head GSH, Alkylation & Sour Treating (18)
        public string Target_SectionHead { get; set; } = "-"; 

        // 2. Sr Supervisor GSH, Alkylation & Sour Treating (16)
        public string Target_SrSpv_GSH { get; set; } = "-";

        // 3. Shift Supervisor GSH & Alkylation (15)
        public string Target_ShiftSpv_GSH { get; set; } = "-";

        // 4. Panelman GSH & Alkylation (12-13)
        public string Target_Panelman_GSH_12_13 { get; set; } = "-";

        // 5. Panelman GSH & Alkylation (14)
        public string Target_Panelman_GSH_14 { get; set; } = "-";

        // 6. Operator GSH & Alkylation (8-11)
        public string Target_Operator_GSH_8_11 { get; set; } = "-";

        // 7. Operator GSH & Alkylation (12-13)
        public string Target_Operator_GSH_12_13 { get; set; } = "-";

        // 8. Shift Supervisor ARU & Sour Treating (15)
        public string Target_ShiftSpv_ARU { get; set; } = "-";

        // 9. Panelman ARU & Sour Treating (12-13)
        public string Target_Panelman_ARU_12_13 { get; set; } = "-";

        // 10. Panelman ARU & Sour Treating (14)
        public string Target_Panelman_ARU_14 { get; set; } = "-";

        // 11. Operator ARU & Sour Treating (8-11)
        public string Target_Operator_ARU_8_11 { get; set; } = "-";
        
        // 12. Operator ARU & Sour Treating (12-13)
        public string Target_Operator_ARU_12_13 { get; set; } = "-";

        // 13. Sr Supervisor Facility & Quality (16)
        public string Target_SrSpv_Facility { get; set; } = "-";

        // 14. Jr Assistant Material & Analyst Data (10-12)
        public string Target_JrAnalyst { get; set; } = "-";

        // 15. Officer HSE Compliance (14)
        public string Target_HSE { get; set; } = "-";

        // Bagian grouping (FK by name to KkjBagian.Name)
        public string Bagian { get; set; } = "";   // e.g. "RFCC", "GAST", "NGP", "DHT/HMU"
    }

    // KKJ Matrix Bagian (section grouping with editable column headers)
    public class KkjBagian
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";           // e.g. "RFCC", "GAST", "NGP", "DHT/HMU"
        public int DisplayOrder { get; set; } = 0;

        // Editable column header labels for each Target_* field
        // Defaults mirror the existing static headers in KkjMatrix.cshtml
        public string Label_SectionHead        { get; set; } = "Section Head";
        public string Label_SrSpv_GSH          { get; set; } = "Sr Spv GSH";
        public string Label_ShiftSpv_GSH       { get; set; } = "Shift Spv GSH";
        public string Label_Panelman_GSH_12_13 { get; set; } = "Panelman GSH 12-13";
        public string Label_Panelman_GSH_14    { get; set; } = "Panelman GSH 14";
        public string Label_Operator_GSH_8_11  { get; set; } = "Op GSH 8-11";
        public string Label_Operator_GSH_12_13 { get; set; } = "Op GSH 12-13";
        public string Label_ShiftSpv_ARU       { get; set; } = "Shift Spv ARU";
        public string Label_Panelman_ARU_12_13 { get; set; } = "Panelman ARU 12-13";
        public string Label_Panelman_ARU_14    { get; set; } = "Panelman ARU 14";
        public string Label_Operator_ARU_8_11  { get; set; } = "Op ARU 8-11";
        public string Label_Operator_ARU_12_13 { get; set; } = "Op ARU 12-13";
        public string Label_SrSpv_Facility     { get; set; } = "Sr Spv Facility";
        public string Label_JrAnalyst          { get; set; } = "Jr Analyst";
        public string Label_HSE                { get; set; } = "HSE";
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