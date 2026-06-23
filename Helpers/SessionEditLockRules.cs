using HcPortal.Models;

namespace HcPortal.Helpers
{
    /// <summary>
    /// v32.7 Phase 422 D-07/SHFX-03 — lock edit paket/soal yang PURE (EF-free) dan server-authoritative.
    /// Angkat keputusan inline yang sebelumnya cuma di ViewBag (AssessmentAdminController.cs:5811
    /// <c>ViewBag.IsSamePackageLocked = isPostSession &amp;&amp; assessment.SamePackage</c>) jadi predicate
    /// tunggal supaya bisa di-unit-test tanpa instansiasi controller (pola <see cref="RetakeRules"/>).
    ///
    /// <para>Dipakai DI DUA TEMPAT: guard 5 endpoint POST (Wave 2 — tolak-keras, defense-in-depth
    /// terhadap root-cause SHUF-ISS-02 view-only-lock) + view friendly disable tombol (Wave 3 — UX layer).</para>
    /// </summary>
    public static class SessionEditLockRules
    {
        /// <summary>
        /// True HANYA bila sesi = Post-Test ber-SamePackage (paket Post adalah salinan terkunci dari Pre).
        /// False untuk Pre-Test, Standard, atau Post-Test non-SamePackage (backward-compat WAJIB —
        /// grup tanpa SamePackage tak tersentuh lock).
        /// </summary>
        public static bool IsSessionEditLocked(AssessmentSession s)
            => s.AssessmentType == "PostTest" && s.SamePackage;
    }
}
