using System.Collections.Generic;
using System.Linq;

namespace HcPortal.Helpers
{
    /// <summary>
    /// AF-1 (Phase 356): coachee eligible bila punya TEPAT expectedCount progress untuk unit-nya
    /// dan SEMUANYA "Approved". expectedCount &lt;= 0 → tidak eligible (track Tahun 3 tanpa
    /// deliverable ditangani TERPISAH di call-site GetEligibleCoachees per D-02, bukan di helper ini).
    /// Ekstrak dari GetEligibleCoachees agar logic eligibility per-unit dapat di-test tanpa DbContext.
    /// </summary>
    public static class CoacheeEligibilityCalculator
    {
        public static bool IsEligiblePerUnit(IReadOnlyList<string> myProgressStatuses, int expectedCount)
        {
            if (expectedCount <= 0) return false;
            if (myProgressStatuses.Count != expectedCount) return false;
            return myProgressStatuses.All(s => s == "Approved");
        }
    }
}
