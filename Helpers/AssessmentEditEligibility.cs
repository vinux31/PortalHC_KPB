using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Cek apakah AssessmentSession boleh di-edit oleh Admin/HC (spec 4.2, REQ EDIT-02).
    /// Aturan gating:
    ///   - Status = "Completed"
    ///   - bukan IsManualEntry
    ///   - bukan Assessment Proton Tahun 3 (per CONTEXT CD-01: Category=="Assessment Proton" && TahunKe=="Tahun 3")
    ///   - punya UserPackageAssignment (async variant only — sync skip DB check)
    /// </summary>
    public static class AssessmentEditEligibility
    {
        public static async Task<bool> IsEditableAsync(ApplicationDbContext db, AssessmentSession s)
        {
            if (s == null) return false;
            if (s.Status != "Completed") return false;
            if (s.IsManualEntry) return false;
            if (s.Category == "Assessment Proton" && s.TahunKe == "Tahun 3") return false;

            bool hasAssignment = await db.UserPackageAssignments
                .AnyAsync(a => a.AssessmentSessionId == s.Id);
            return hasAssignment;
        }

        /// <summary>Sync version for view-side rendering (skip assignment DB check; render-time only).</summary>
        public static bool IsEditableShallow(AssessmentSession s)
        {
            if (s == null) return false;
            if (s.Status != "Completed") return false;
            if (s.IsManualEntry) return false;
            if (s.Category == "Assessment Proton" && s.TahunKe == "Tahun 3") return false;
            return true;
        }
    }
}
