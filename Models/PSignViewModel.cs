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
        public string? Unit { get; set; }
        public string FullName { get; set; } = string.Empty;
    }
}
