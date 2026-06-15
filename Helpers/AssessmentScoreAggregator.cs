using System.Collections.Generic;
using System.Linq;
using HcPortal.Models;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Result of aggregating an assessment session's score across MC / MA / Essay questions.
    /// </summary>
    public readonly record struct ScoreAggregateResult(int TotalScore, int MaxScore, int Percentage, bool IsPassed);

    /// <summary>
    /// Phase 376 GRADE-01/02 — Pure score aggregator. Pure by design (only System.Linq / HcPortal.Models),
    /// fully synchronous, EF-free, no logging dependency → unit-testable without a database. Single source of truth for score
    /// aggregation shared by the forward path (<c>AssessmentAdminController.FinalizeEssayGrading</c>) AND
    /// the <c>RecomputeEssayScores</c> repair endpoint (Plan 03), so the two cannot diverge — kill-drift
    /// pattern (Phase 363/365).
    ///
    /// Math is ported VERBATIM from the previous inline block at FinalizeEssayGrading (L3535-3564).
    /// Formula D-04 LOCKED: <c>percentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0</c>;
    /// <c>isPassed = percentage >= passPercentage</c>. maxScore==0 → percentage 0, never throws (D-05).
    /// Grading uses PackageOption.Id (not letter/position), so option order never affects scoring.
    /// </summary>
    public static class AssessmentScoreAggregator
    {
        public static ScoreAggregateResult Compute(
            IEnumerable<PackageQuestion> questions,
            IEnumerable<PackageUserResponse> responses,
            int passPercentage)
        {
            var respList = responses as IList<PackageUserResponse> ?? responses.ToList();
            int totalScore = 0, maxScore = 0;
            foreach (var q in questions)
            {
                maxScore += q.ScoreValue;
                switch (q.QuestionType ?? "MultipleChoice")
                {
                    case "MultipleChoice":
                        var mcResp = respList.FirstOrDefault(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue);
                        if (mcResp != null)
                        {
                            var opt = q.Options.FirstOrDefault(o => o.Id == mcResp.PackageOptionId!.Value);
                            if (opt != null && opt.IsCorrect) totalScore += q.ScoreValue;
                        }
                        break;
                    case "MultipleAnswer":
                        var maSelected = respList.Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue)
                            .Select(r => r.PackageOptionId!.Value).ToHashSet();
                        var maCorrect = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                        if (maSelected.SetEquals(maCorrect)) totalScore += q.ScoreValue;
                        break;
                    case "Essay":
                        var essayResp = respList.FirstOrDefault(r => r.PackageQuestionId == q.Id);
                        if (essayResp?.EssayScore.HasValue == true) totalScore += essayResp.EssayScore.Value;
                        break;
                }
            }
            int percentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;   // D-04 LOCKED
            return new ScoreAggregateResult(totalScore, maxScore, percentage, percentage >= passPercentage);
        }
    }
}
