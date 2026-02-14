using HcPortal.Models;

namespace HcPortal.Models.Competency
{
    /// <summary>
    /// Tracks each user's current level per competency with history.
    /// One record per user per competency (enforced by unique index).
    /// </summary>
    public class UserCompetencyLevel
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign Key to ApplicationUser
        /// </summary>
        public string UserId { get; set; } = "";

        /// <summary>
        /// Navigation property to User
        /// </summary>
        public ApplicationUser? User { get; set; }

        /// <summary>
        /// Foreign Key to KkjMatrixItem
        /// </summary>
        public int KkjMatrixItemId { get; set; }

        /// <summary>
        /// Navigation property to KKJ competency
        /// </summary>
        public KkjMatrixItem? KkjMatrixItem { get; set; }

        /// <summary>
        /// Current competency level achieved (0-5, default 0)
        /// 0 = Not started
        /// 1-5 = Competency levels per KKJ matrix
        /// </summary>
        public int CurrentLevel { get; set; } = 0;

        /// <summary>
        /// Target level for this user's position (0-5)
        /// Denormalized from KKJ matrix at creation time
        /// </summary>
        public int TargetLevel { get; set; } = 0;

        /// <summary>
        /// How the level was achieved: "Assessment", "Manual", "Training"
        /// </summary>
        public string Source { get; set; } = "";

        /// <summary>
        /// Foreign Key to AssessmentSession (nullable)
        /// Set when Source = "Assessment"
        /// </summary>
        public int? AssessmentSessionId { get; set; }

        /// <summary>
        /// Navigation property to AssessmentSession
        /// </summary>
        public AssessmentSession? AssessmentSession { get; set; }

        /// <summary>
        /// When the current level was first achieved
        /// </summary>
        public DateTime AchievedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the level was last updated (for history tracking)
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// UserId of HC staff who manually adjusted the level
        /// Only set when Source = "Manual"
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Computed property: gap between target and current level
        /// Positive = gap exists, 0 = met, negative = exceeded
        /// </summary>
        public int Gap => TargetLevel - CurrentLevel;
    }
}
