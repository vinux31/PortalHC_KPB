using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace HcPortal.Models
{
    public class UserPackageAssignment
    {
        [Key]
        public int Id { get; set; }

        // FK to AssessmentSession — one assignment record per session-user pair
        public int AssessmentSessionId { get; set; }
        [ForeignKey("AssessmentSessionId")]
        public virtual AssessmentSession AssessmentSession { get; set; } = null!;

        // FK to the package assigned to this user (Restrict — don't cascade-delete assignments when package deleted)
        public int AssessmentPackageId { get; set; }
        [ForeignKey("AssessmentPackageId")]
        public virtual AssessmentPackage AssessmentPackage { get; set; } = null!;

        /// <summary>
        /// String FK to the assigned user (no nav property — consistent with Phase 6 "String IDs (no FK)" decision).
        /// </summary>
        public string UserId { get; set; } = "";

        /// <summary>
        /// JSON array of PackageQuestion.Id in per-user display order after shuffle.
        /// Example: [42, 17, 33, ...]
        /// </summary>
        public string ShuffledQuestionIds { get; set; } = "[]";

        /// <summary>
        /// JSON dictionary mapping PackageQuestion.Id (as string key) to an ordered array of PackageOption.Id.
        /// Example: {"42": [101, 103, 102, 104], "17": [...], ...}
        /// Represents per-user option shuffle for every question in the assigned package.
        /// </summary>
        public string ShuffledOptionIdsPerQuestion { get; set; } = "{}";

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Set to true when the user submits the exam.</summary>
        public bool IsCompleted { get; set; } = false;

        // ========== Helper methods (not mapped to DB) ==========

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>Deserializes ShuffledQuestionIds to a typed list.</summary>
        public List<int> GetShuffledQuestionIds()
        {
            try
            {
                return JsonSerializer.Deserialize<List<int>>(ShuffledQuestionIds, _jsonOptions) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        /// <summary>
        /// Deserializes ShuffledOptionIdsPerQuestion to a dictionary of (PackageQuestion.Id -> ordered PackageOption.Id list).
        /// </summary>
        public Dictionary<int, List<int>> GetShuffledOptionIds()
        {
            try
            {
                // JSON keys are strings; deserialize as string-keyed dict then convert keys to int.
                var raw = JsonSerializer.Deserialize<Dictionary<string, List<int>>>(ShuffledOptionIdsPerQuestion, _jsonOptions);
                if (raw == null) return new Dictionary<int, List<int>>();

                return raw.ToDictionary(
                    kvp => int.Parse(kvp.Key),
                    kvp => kvp.Value
                );
            }
            catch
            {
                return new Dictionary<int, List<int>>();
            }
        }
    }
}
