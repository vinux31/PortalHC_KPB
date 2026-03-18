using System.ComponentModel.DataAnnotations;

namespace HcPortal.Models
{
    public class AssessmentCategory
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        public int DefaultPassPercentage { get; set; } = 70;

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        // Phase 195 — Sub-category hierarchy
        public int? ParentId { get; set; }

        // Phase 195 — Per-category signatory
        public string? SignatoryUserId { get; set; }

        // Navigation properties
        public AssessmentCategory? Parent { get; set; }
        public ICollection<AssessmentCategory> Children { get; set; } = new List<AssessmentCategory>();
        public ApplicationUser? Signatory { get; set; }
    }
}
