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
    /// Phase 424 GRDF-02: pemilihan MC kini DEDUPE last-write-wins (response dgn SubmittedAt terbaru), konvergen
    /// dengan jalur grading kanonik (<c>GradingService.GradeAndCompleteAsync</c>) dan
    /// <c>ComputeScoreAndETInternalAsync</c> — jadikan klaim "single source of truth" benar untuk MC/MA/Essay.
    /// MA TIDAK ter-dedupe (dibaca penuh multi-row, all-or-nothing — Pitfall 3). Formula pct tetap LOCKED (D-04).
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
            // Phase 424 GRDF-02 (D-06): dedupe last-write-wins per soal MC/single-answer — PORT VERBATIM dari
            // GradingService.cs:87-90 (kanonik PATH 1). Ganti FirstOrDefault non-deterministik → response FINAL
            // (SubmittedAt terbaru). MA dibaca penuh (multi-row) → JANGAN masuk dedupe ini (Pitfall 3).
            var finalByQuestion = respList
                .Where(r => r.PackageOptionId.HasValue)
                .GroupBy(r => r.PackageQuestionId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.SubmittedAt).First());
            int totalScore = 0, maxScore = 0;
            foreach (var q in questions)
            {
                maxScore += q.ScoreValue;
                switch (q.QuestionType ?? "MultipleChoice")
                {
                    case "MultipleChoice":
                        var mcResp = finalByQuestion.TryGetValue(q.Id, out var fr) ? fr : null;
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

        /// <summary>
        /// Phase 383 ECG-01 — Per-question correctness untuk DISPLAY (count "(X/Y benar)", Elemen Teknis,
        /// Tinjauan badge, PDF export). bool?: true=Benar, false=Salah, null=essay belum dinilai (pending).
        /// Single source of truth (kill-drift Phase 363/365/376) — JANGAN recompute correctness inline lagi.
        /// MC/MA me-mirror DISPLAY-path inline CMPController.Results (L2259-2324) byte-for-byte:
        ///   - MultipleChoice: benar iff satu-satunya opsi terpilih ber-IsCorrect; 0 terpilih → false.
        ///   - MultipleAnswer: selected.Count > 0 &amp;&amp; selected.SetEquals(correct) — non-empty guard (GRD-02).
        ///     (SetEquals simetris: ≡ correct.SetEquals(selected) di inline L2263.)
        ///   - Essay (D-02): EssayScore.HasValue ? EssayScore.Value > 0 : null.
        /// Pure: hanya System.Linq + HcPortal.Models, sinkron, EF-free, unit-testable. Tidak menyentuh Compute (D-04 formula LOCKED).
        /// </summary>
        public static bool? IsQuestionCorrect(PackageQuestion q, IEnumerable<PackageUserResponse> responsesForQ)
        {
            var list = responsesForQ as IList<PackageUserResponse> ?? responsesForQ.ToList();
            switch (q.QuestionType ?? "MultipleChoice")
            {
                case "MultipleAnswer":
                {
                    var selected = list.Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue).Select(r => r.PackageOptionId!.Value).ToHashSet();
                    var correct  = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                    return selected.Count > 0 && selected.SetEquals(correct);   // GRD-02 non-empty guard
                }
                case "Essay":
                {
                    var essay = list.FirstOrDefault(r => r.PackageQuestionId == q.Id);
                    if (essay?.EssayScore.HasValue != true) return null;        // pending
                    return essay.EssayScore.Value > 0;                          // D-02
                }
                default: // MultipleChoice
                {
                    var sel = list.Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue).Select(r => r.PackageOptionId!.Value).ToHashSet();
                    if (sel.Count == 0) return false;
                    var opt = q.Options.FirstOrDefault(o => sel.Contains(o.Id));
                    return opt != null && opt.IsCorrect;
                }
            }
        }

        /// <summary>
        /// Phase 386 PXF-05 (F-17/F-DEV-02 D-10) — display string for the "Jawaban" cell, shared by PDF
        /// (GeneratePerPesertaPdf) AND Excel (AddDetailPerSoalSheet). Pure, EF-free, unit-testable.
        ///   MA   -> join ALL selected OptionText in option-Id order, ", " separator (mirror Excel L4860-4861).
        ///   MC   -> single selected OptionText.
        ///   Essay-> TextAnswer truncated to 300 chars + "..." (mirror PDF L5083).
        ///   none -> "—" (em dash U+2014).
        /// Wired into PDF/Excel export paths in Wave 4 (kill-drift, single source of display truth alongside
        /// IsQuestionCorrect). Does NOT touch Compute or IsQuestionCorrect (D-11 — scoring/correctness untouched).
        /// </summary>
        public static string BuildAnswerCell(PackageQuestion q, IEnumerable<PackageUserResponse> responsesForQ)
        {
            var list = responsesForQ as IList<PackageUserResponse> ?? responsesForQ.ToList();
            switch (q.QuestionType ?? "MultipleChoice")
            {
                case "Essay":
                {
                    var essay = list.FirstOrDefault(r => r.PackageQuestionId == q.Id);
                    var txt = essay?.TextAnswer;
                    if (string.IsNullOrEmpty(txt)) return "—";
                    return txt.Length > 300 ? txt.Substring(0, 300) + "..." : txt;
                }
                case "MultipleAnswer":
                {
                    var selectedIds = list.Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue)
                                          .Select(r => r.PackageOptionId!.Value).ToHashSet();
                    var joined = string.Join(", ",
                        q.Options.Where(o => selectedIds.Contains(o.Id)).OrderBy(o => o.Id).Select(o => o.OptionText));
                    return string.IsNullOrEmpty(joined) ? "—" : joined;
                }
                default: // MultipleChoice
                {
                    var resp = list.FirstOrDefault(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue);
                    if (resp == null) return "—";
                    var opt = q.Options.FirstOrDefault(o => o.Id == resp.PackageOptionId!.Value);
                    return opt?.OptionText ?? "—";
                }
            }
        }
    }
}
