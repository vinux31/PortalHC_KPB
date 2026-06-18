namespace HcPortal.Models
{
    /// <summary>
    /// ViewModel for the P-Sign (digital initial badge) component.
    /// Used by _PSign.cshtml partial view and future PDF evidence generation.
    /// </summary>
    public class PSignViewModel
    {
        public string LogoUrl { get; set; } = "/images/psign-pertamina.png";
        public string? Position { get; set; }
        // Scalar Unit dipertahankan sebagai fallback untuk pemanggil yang belum mengisi Units.
        public string? Unit { get; set; }
        // Multi-unit (MU-03, D-07): SEMUA unit pekerja (primary-first comma-join di cetak).
        public List<string>? Units { get; set; }
        public string? PrimaryUnit { get; set; }
        public string FullName { get; set; } = string.Empty;
    }
}
