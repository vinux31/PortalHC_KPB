using System.ComponentModel.DataAnnotations;

namespace HcPortal.Models
{
    /// <summary>
    /// Junction multi-unit pekerja (dalam 1 Bagian). Satu pekerja boleh punya >1 Unit;
    /// tepat 1 baris IsPrimary=1 yang mirror ke <see cref="ApplicationUser.Unit"/> (invariant #3).
    /// Pola disalin dari <see cref="CoachCoacheeMapping"/> (int Id, string FK-id, string Unit-name, bool IsActive).
    /// </summary>
    public class UserUnit
    {
        public int Id { get; set; }

        /// <summary>FK -> AspNetUsers.Id (Users.Id), ON DELETE CASCADE.</summary>
        public string UserId { get; set; } = "";

        /// <summary>
        /// NAME-string, anak dari Section pekerja. [MaxLength(200)] WAJIB karena index (UserId, Unit)
        /// dipasang — SQL Server tolak nvarchar(max) sebagai index key (Pitfall 2).
        /// </summary>
        [MaxLength(200)]
        public string Unit { get; set; } = "";

        /// <summary>Tepat 1 baris IsPrimary=1 per user (enforce DB via filtered-unique index).</summary>
        public bool IsPrimary { get; set; } = false;

        public bool IsActive { get; set; } = true;

        /// <summary>Nav ke ApplicationUser (FK UserId).</summary>
        public ApplicationUser? User { get; set; }
    }
}
