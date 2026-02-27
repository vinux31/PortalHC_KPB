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
        public ChangePasswordViewModel ChangePassword { get; set; } = new();

        // Read-only display fields (not editable via forms)
        public string? NIP { get; set; }
        public string? Email { get; set; }
        public string Role { get; set; } = "—";
        public string? Section { get; set; }
        public string? Directorate { get; set; }
        public string? Unit { get; set; }
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

        [StringLength(20)]
        public string? PhoneNumber { get; set; }
    }

    /// <summary>
    /// ViewModel for the Change Password form.
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Password lama harus diisi")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password baru harus diisi")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi password harus diisi")]
        [Compare("NewPassword", ErrorMessage = "Password baru dan konfirmasi tidak cocok")]
        [DataType(DataType.Password)]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
