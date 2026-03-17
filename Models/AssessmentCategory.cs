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
    }
}
