namespace HcPortal.Models
{
    // 1. CLASS UNTUK HALAMAN SUSUNAN KKJ (MATRIX)
    public class KkjMatrixItem
    {
        public int No { get; set; }
        public string SkillGroup { get; set; } = "";
        public string SubSkillGroup { get; set; } = "";
        public string Indeks { get; set; } = "";     
        public string Kompetensi { get; set; } = ""; 
        
        // Target Level per Jabatan
        public string Target_SectionHead { get; set; } = "-";
        public string Target_SrSpv_GSH { get; set; } = "-";
        public string Target_ShiftSpv_GSH { get; set; } = "-";
        public string Target_Panelman_GSH { get; set; } = "-";
        public string Target_Operator_GSH { get; set; } = "-";
        public string Target_ShiftSpv_ARU { get; set; } = "-";
        public string Target_Panelman_ARU { get; set; } = "-";
        public string Target_Operator_ARU { get; set; } = "-";
        public string Target_SrSpv_Facility { get; set; } = "-";
        public string Target_JrAnalyst { get; set; } = "-";
        public string Target_HSE { get; set; } = "-";
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
        public string No { get; set; } = "";
        public string NamaKompetensi { get; set; } = "";
        public string IndikatorPerilaku { get; set; } = "";
        public string DetailIndikator { get; set; } = "";
        public string Silabus { get; set; } = "";
        public string Status { get; set; } = "";
        // Removed old properties: Implementasi, KodeSub, SubOps, SubPanel
    }

}