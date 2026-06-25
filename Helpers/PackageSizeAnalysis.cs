using System.Collections.Generic;
using System.Linq;
using HcPortal.Models;

namespace HcPortal.Helpers
{
    /// <summary>
    /// v32.7 Phase 422 D-05/SHFX-07 — SATU sumber kebenaran komputasi ukuran paket-soal.
    /// Ganti duplikasi yang sebelumnya dihitung 2x: controller AssessmentAdminController.cs:5844-5856
    /// + view ManagePackages.cshtml:72-78 (kill-drift, cegah SHUF-ISS-07 view↔controller drift).
    ///
    /// <para><b>Pure by design:</b> EF-free, hanya membaca <c>Questions.Count</c> dari entity in-memory.
    /// Caller (GET ViewBag) menyuplai daftar paket sudah ter-Include; helper hitung mismatch/refCount/withQ.
    /// Paket TANPA soal diabaikan (tak ikut hitung referensi/mismatch).</para>
    /// </summary>
    public static class PackageSizeAnalysis
    {
        /// <summary>
        /// Hasil komputasi paritas ukuran paket.
        /// <list type="bullet">
        /// <item><c>PackagesWithQuestions</c> — jumlah paket yang punya >=1 soal.</item>
        /// <item><c>ReferenceCount</c> — jumlah soal paket-ber-soal pertama (null bila tak ada paket-ber-soal).</item>
        /// <item><c>HasMismatch</c> — true bila ada paket-ber-soal yang jumlah soalnya beda dari referensi.</item>
        /// </list>
        /// </summary>
        public readonly record struct Result(int PackagesWithQuestions, int? ReferenceCount, bool HasMismatch);

        public static Result Compute(IEnumerable<AssessmentPackage> packages)
        {
            var withQ = packages.Where(p => p.Questions != null && p.Questions.Any()).ToList();
            if (withQ.Count == 0) return new Result(0, null, false);
            int refCount = withQ[0].Questions.Count;
            return new Result(withQ.Count, refCount, withQ.Any(p => p.Questions.Count != refCount));
        }
    }
}
