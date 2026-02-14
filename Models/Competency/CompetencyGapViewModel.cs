namespace HcPortal.Models.Competency
{
    /// <summary>
    /// ViewModel for displaying a user's complete competency gap analysis
    /// </summary>
    public class CompetencyGapViewModel
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string? Position { get; set; }
        public string? Section { get; set; }

        /// <summary>
        /// List of all competencies for this user with gap details
        /// </summary>
        public List<CompetencyGapItem> Competencies { get; set; } = new List<CompetencyGapItem>();

        /// <summary>
        /// Total number of competencies applicable to this user's position
        /// </summary>
        public int TotalCompetencies { get; set; }

        /// <summary>
        /// Number of competencies that have met or exceeded target level
        /// </summary>
        public int CompetenciesMet { get; set; }

        /// <summary>
        /// Number of competencies with gaps (below target)
        /// </summary>
        public int CompetenciesGapped { get; set; }

        /// <summary>
        /// Overall progress percentage (0-100)
        /// </summary>
        public double OverallProgress { get; set; }
    }

    /// <summary>
    /// Individual competency item with gap analysis details
    /// </summary>
    public class CompetencyGapItem
    {
        public int KkjMatrixItemId { get; set; }
        public string Kompetensi { get; set; } = "";
        public string SkillGroup { get; set; } = "";

        public int CurrentLevel { get; set; }
        public int TargetLevel { get; set; }
        public int Gap { get; set; }

        /// <summary>
        /// Status: "Met", "Gap", "Not Started"
        /// </summary>
        public string Status { get; set; } = "";

        /// <summary>
        /// When the competency level was last updated
        /// </summary>
        public DateTime? LastUpdated { get; set; }

        /// <summary>
        /// Title of the last assessment that contributed to this competency
        /// </summary>
        public string? LastAssessmentTitle { get; set; }

        /// <summary>
        /// Whether there's an IDP activity targeting this competency
        /// </summary>
        public bool HasIdpActivity { get; set; }

        /// <summary>
        /// Suggested action to close the gap (e.g., "Complete Assessment OJ", "Enroll in IHT")
        /// </summary>
        public string? SuggestedAction { get; set; }
    }
}
