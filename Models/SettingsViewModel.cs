using System.ComponentModel.DataAnnotations;

namespace HcPortal.Models
{
    /// <summary>
    /// Composite ViewModel for the Settings page (GET).
    /// Combines read-only display fields with editable sub-models.
    /// </summary>
    public class SettingsViewModel
    {
        public EditProfileViewModel EditProfile { get; set; } = new();
        // Read-only display fields (not editable via forms)
        public string? NIP { get; set; }

        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        public string? Email { get; set; }

        public string Role { get; set; } = "—";
        public string? Section { get; set; }
        public string? Directorate { get; set; }
        public string? Unit { get; set; }
        public PSignViewModel? PSign { get; set; }
    }

    /// <summary>
    /// ViewModel for the Edit Profile form — editable fields only.
    /// </summary>
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Nama lengkap harus diisi")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Position { get; set; }

        [StringLength(30)]
        [RegularExpression(@"^[\d\s\-\+\(\)]+$", ErrorMessage = "Format nomor telepon tidak valid")]
        public string? PhoneNumber { get; set; }
    }

}
