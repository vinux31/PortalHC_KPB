using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HcPortal.Models
{
    /// <summary>
    /// Extended user model dengan custom properties untuk HC Portal
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Nama lengkap user
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// NIP / Employee ID (opsional)
        /// </summary>
        public string? NIP { get; set; }

        /// <summary>
        /// Jabatan/Position
        /// </summary>
        public string? Position { get; set; }

        /// <summary>
        /// Bagian (Section): RFCC, DHT/HMU, NGP, GAST
        /// </summary>
        public string? Section { get; set; }

        /// <summary>
        /// Unit kerja
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// Directorate
        /// </summary>
        public string? Directorate { get; set; }

        /// <summary>
        /// Tanggal bergabung
        /// </summary>
        public DateTime? JoinDate { get; set; }

        /// <summary>
        /// Role Level (1-6) untuk hierarki RBAC
        /// Level 1: Admin
        /// Level 2: HC
        /// Level 3: Direktur, VP, Manager
        /// Level 4: Section Head, Sr Supervisor
        /// Level 5: Coach
        /// Level 6: Coachee
        /// </summary>
        public int RoleLevel { get; set; } = 6; // Default: Coachee

        /// <summary>
        /// User's preferred portal view (HC, Atasan, Coach, Coachee)
        /// Default is determined by RoleLevel
        /// Only Admin (Level 1) can change this setting
        /// </summary>
        public string SelectedView { get; set; } = "Coachee";

        /// <summary>
        /// Authentication source: "Local" (password hash) or "AD" (LDAP query to Pertamina Active Directory).
        /// Determines which IAuthService implementation to use at login.
        /// Default "Local" ensures backward compatibility for all existing users.
        /// </summary>
        [MaxLength(10)]
        public string AuthSource { get; set; } = "Local";

        /// <summary>
        /// Navigation property for TrainingRecords
        /// </summary>
        public virtual ICollection<TrainingRecord> TrainingRecords { get; set; } = new List<TrainingRecord>();
    }
}
