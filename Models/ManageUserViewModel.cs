using System.ComponentModel.DataAnnotations;

namespace HcPortal.Models
{
    /// <summary>
    /// ViewModel untuk form Create/Edit Worker
    /// </summary>
    public class ManageUserViewModel
    {
        /// <summary>
        /// User ID (null for create, populated for edit)
        /// </summary>
        public string? Id { get; set; }

        [Required(ErrorMessage = "Nama lengkap harus diisi")]
        [Display(Name = "Nama Lengkap")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email harus diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "NIP / Nopeg")]
        [StringLength(20)]
        public string? NIP { get; set; }

        [Display(Name = "Jabatan / Position")]
        [StringLength(100)]
        public string? Position { get; set; }

        [Display(Name = "Bagian (Section)")]
        public string? Section { get; set; }

        [Display(Name = "Unit")]
        public string? Unit { get; set; }

        // Phase 399 (MU-01/02) — multi-unit dalam 1 Bagian. Section TETAP scalar (invariant #1).
        /// <summary>Daftar unit pekerja (≥0). Bind dari checkbox-list name="Units".</summary>
        [Display(Name = "Unit Penugasan")]
        public List<string> Units { get; set; } = new();

        /// <summary>Unit utama (mirror ke ApplicationUser.Unit). Bind dari radio name="PrimaryUnit".</summary>
        [Display(Name = "Unit Utama")]
        public string? PrimaryUnit { get; set; }

        /// <summary>MU-07 round-trip — operator menyetujui nonaktifkan mapping coach saat hapus unit.</summary>
        public bool ConfirmedDeactivate { get; set; }

        /// <summary>MU-07 display — daftar mapping coach yang akan terdampak (dinonaktifkan) bila lanjut.</summary>
        public List<string> ImpactedMappings { get; set; } = new();

        [Display(Name = "Directorate")]
        [StringLength(100)]
        public string? Directorate { get; set; }

        [Display(Name = "Tanggal Bergabung")]
        [DataType(DataType.Date)]
        public DateTime? JoinDate { get; set; }

        [Required(ErrorMessage = "Role harus dipilih")]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Coachee";

        /// <summary>
        /// Password — required on Create, optional on Edit (blank = keep current)
        /// </summary>
        [Display(Name = "Password")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        public string? Password { get; set; }

        [Display(Name = "Konfirmasi Password")]
        [Compare("Password", ErrorMessage = "Password dan Konfirmasi tidak cocok")]
        public string? ConfirmPassword { get; set; }

        /// <summary>
        /// Flag to indicate if this is an edit form (affects password validation)
        /// </summary>
        public bool IsEdit => !string.IsNullOrEmpty(Id);
    }
}
