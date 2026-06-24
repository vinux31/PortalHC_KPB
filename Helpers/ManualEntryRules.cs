namespace HcPortal.Helpers
{
    /// <summary>
    /// v32.7 Phase 425 (CLN-02 / FLD-5.2-04, FLD-5.2-05) — cross-validation entry manual.
    /// Pure EF-free supaya bisa di-unit-test tanpa DbContext/controller. Analog
    /// <see cref="CertIssuanceRules"/> (fase 423) &amp; <see cref="ExamTimeRules"/> (fase 424).
    /// </summary>
    public static class ManualEntryRules
    {
        // CLN-02 — true bila status Lulus/Tidak-Lulus TIDAK selaras dgn (Score >= PassPercentage).
        // Score nullable: null => tidak ada basis cross-validate => return false (skip warning, no NRE).
        // Boundary: Score == PassPercentage dianggap LULUS (>=).
        public static bool PassStatusMismatch(int? score, int passPercentage, bool isPassed)
            => score.HasValue && (score.Value >= passPercentage) != isPassed;
    }
}
