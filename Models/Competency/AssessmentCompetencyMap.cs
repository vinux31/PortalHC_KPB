using HcPortal.Models;

namespace HcPortal.Models.Competency
{
    /// <summary>
    /// Join table linking assessment categories to KKJ competencies.
    /// Defines which competency level is granted when an assessment is passed.
    /// </summary>
    public class AssessmentCompetencyMap
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign Key to KkjMatrixItem
        /// </summary>
        public int KkjMatrixItemId { get; set; }

        /// <summary>
        /// Navigation property to KKJ competency
        /// </summary>
        public KkjMatrixItem? KkjMatrixItem { get; set; }

        /// <summary>
        /// Assessment category (e.g., "Assessment OJ", "IHT", "Licencor")
        /// Matches AssessmentSession.Category
        /// </summary>
        public string AssessmentCategory { get; set; } = "";

        /// <summary>
        /// Optional pattern match against AssessmentSession.Title for finer-grained mapping
        /// If null, applies to all assessments in the category
        /// </summary>
        public string? TitlePattern { get; set; }

        /// <summary>
        /// Competency level awarded when assessment is passed (1-5 scale)
        /// </summary>
        public int LevelGranted { get; set; }

        /// <summary>
        /// Minimum score required to grant this competency level
        /// If null, uses the assessment's PassPercentage
        /// </summary>
        public int? MinimumScoreRequired { get; set; }
    }
}
