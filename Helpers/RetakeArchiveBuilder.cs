using System.Collections.Generic;
using System.Linq;
using HcPortal.Models;

namespace HcPortal.Helpers
{
    /// <summary>
    /// v32.4 RTK-02 — builder PURE (EF-free, sinkron, unit-testable) yang membentuk daftar
    /// <see cref="AssessmentAttemptResponseArchive"/> beku dari satu attempt SEBELUM
    /// <see cref="PackageUserResponse"/> dihapus oleh RetakeService (plan 405-03).
    ///
    /// <para><b>Verdict via aggregator (kill-drift):</b> benar/salah dihitung lewat
    /// <see cref="AssessmentScoreAggregator.IsQuestionCorrect"/> — JANGAN re-grade inline (akan divergen
    /// dari Results/PDF/Excel). bool?: true=Benar, false=Salah, null=essay belum dinilai (pending).</para>
    ///
    /// <para><b>Essay full-text (Pitfall 2):</b> <see cref="AssessmentScoreAggregator.BuildAnswerCell"/>
    /// me-TRUNCATE essay ke 300 char (untuk DISPLAY PDF/Excel). Archive bersifat PERMANEN (D-04 / ISO 17024)
    /// sehingga untuk Essay disimpan <c>TextAnswer</c> PENUH (no truncate). MC/MA tetap pakai
    /// <c>BuildAnswerCell</c> (OptionText, tak ter-truncate).</para>
    /// </summary>
    public static class RetakeArchiveBuilder
    {
        public static List<AssessmentAttemptResponseArchive> Build(
            int attemptHistoryId,
            IEnumerable<PackageQuestion> questions,
            IEnumerable<PackageUserResponse> responses)
        {
            var respList = responses as IList<PackageUserResponse> ?? responses.ToList();
            var rows = new List<AssessmentAttemptResponseArchive>();

            foreach (var q in questions)
            {
                var forQ = respList.Where(r => r.PackageQuestionId == q.Id).ToList();
                bool? verdict = AssessmentScoreAggregator.IsQuestionCorrect(q, forQ);
                bool isEssay = (q.QuestionType ?? "MultipleChoice") == "Essay";

                int awardedScore;
                string? answerText;
                if (isEssay)
                {
                    var essayResp = forQ.FirstOrDefault(r => r.PackageQuestionId == q.Id);
                    awardedScore = essayResp?.EssayScore ?? 0;
                    answerText = essayResp?.TextAnswer;   // FULL-TEXT (Pitfall 2 — no truncate)
                }
                else
                {
                    awardedScore = verdict == true ? q.ScoreValue : 0;
                    answerText = AssessmentScoreAggregator.BuildAnswerCell(q, forQ);   // MC/MA display
                }

                rows.Add(new AssessmentAttemptResponseArchive
                {
                    AttemptHistoryId = attemptHistoryId,
                    PackageQuestionId = q.Id,
                    QuestionText = q.QuestionText ?? "",
                    AnswerText = answerText,
                    IsCorrect = verdict,
                    AwardedScore = awardedScore
                });
            }

            return rows;
        }
    }
}
