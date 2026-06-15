using System.Linq;
using HcPortal.Models;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Phase 386 PXF-02 (F-DEV-01) — pure option-presence validation shared by CreateQuestion + EditQuestion.
    /// Does NOT replicate the existing correctCount gate (MC==1, MA>=2 correct) — that stays in the controller
    /// (AssessmentAdminController.cs:6440-6456). Adds ONLY the text-presence checks:
    ///   D-01: a question with options needs ≥2 options that actually contain text.
    ///   D-03: every option flagged as a correct answer must itself contain text.
    /// "ber-teks" = !string.IsNullOrWhiteSpace (D-02, aligned with the text-gated persist loop L6488).
    /// Pure: only System.Linq + HcPortal.Models — unit-testable (pattern IsQuestionCorrectTests).
    ///
    /// Extracted (NOT inlined twice) per CONTEXT D-12b LOCKED decision so CreateQuestion + EditQuestion
    /// share ONE tested rule (kill-drift). Wired into both POST endpoints in Wave 2 (Plan 03).
    /// </summary>
    public static class QuestionOptionValidator
    {
        public static (bool ok, string? error) ValidateQuestionOptions(string type, string?[] texts, bool[] corrects)
        {
            if (type != "MultipleChoice" && type != "MultipleAnswer")
                return (true, null); // Essay (and any non-option type) bypasses entirely

            int filled = texts.Count(t => !string.IsNullOrWhiteSpace(t));
            if (filled < 2)
                return (false, $"{QuestionTypeLabels.Short(type)} membutuhkan minimal 2 opsi jawaban yang berisi teks.");

            for (int i = 0; i < texts.Length && i < corrects.Length; i++)
                if (corrects[i] && string.IsNullOrWhiteSpace(texts[i]))
                    return (false, $"Opsi yang ditandai sebagai jawaban benar harus berisi teks ({QuestionTypeLabels.Short(type)}).");

            return (true, null);
        }
    }
}
