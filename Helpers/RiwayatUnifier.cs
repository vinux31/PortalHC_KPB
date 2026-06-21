using System.Collections.Generic;
using System.Linq;
using HcPortal.Models;

namespace HcPortal.Helpers
{
    /// <summary>
    /// v32.4 RTK-08 — penyatu PURE (EF-free, sinkron, unit-testable) attempt ter-arsip + attempt LIVE
    /// saat ini ke dalam <see cref="RiwayatAttemptViewModel"/> berurutan (terbaru dulu). Caller
    /// (RiwayatPercobaan action) menyuplai SEMUA fakta — helper tak menyentuh DbContext (mirror
    /// purity <see cref="RetakeArchiveBuilder"/>/<see cref="RetakeRules"/>, kill-drift).
    /// </summary>
    public static class RiwayatUnifier
    {
        /// <summary>
        /// Satukan attempt ter-arsip + attempt LIVE saat ini → daftar VM berurutan AttemptNumber DESC
        /// (terbaru dulu; current naik ke atas natural). Per-soal arsip di-group STRICT by AttemptHistoryId
        /// (Pitfall 3 — bukan by user/title) sehingga tak salah-attach. Current attempt hanya ditambah bila
        /// <paramref name="currentRows"/> non-empty (caller hanya isi saat session.Status=="Completed").
        /// </summary>
        public static List<RiwayatAttemptViewModel> Build(
            AssessmentSession current,
            IEnumerable<AssessmentAttemptHistory> histories,
            IEnumerable<AssessmentAttemptResponseArchive> archiveRows,
            IEnumerable<AssessmentAttemptResponseArchive> currentRows)
        {
            var histList = histories as IList<AssessmentAttemptHistory> ?? histories?.ToList() ?? new List<AssessmentAttemptHistory>();
            var archList = archiveRows as IList<AssessmentAttemptResponseArchive> ?? archiveRows?.ToList() ?? new List<AssessmentAttemptResponseArchive>();
            var curList  = currentRows as IList<AssessmentAttemptResponseArchive> ?? currentRows?.ToList() ?? new List<AssessmentAttemptResponseArchive>();

            // (1) Group baris arsip STRICT by AttemptHistoryId (anti salah-attach — Pitfall 3).
            var rowsByHistory = archList
                .GroupBy(a => a.AttemptHistoryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<RiwayatAttemptViewModel>();

            // (2) Satu VM per history ter-arsip (provenance skor/lulus/tanggal dari history).
            foreach (var h in histList)
            {
                result.Add(new RiwayatAttemptViewModel
                {
                    AttemptNumber = h.AttemptNumber,
                    ScorePercent  = h.Score,
                    IsPassed      = h.IsPassed,
                    CompletedAt   = h.CompletedAt,
                    IsCurrent     = false,
                    Rows          = rowsByHistory.TryGetValue(h.Id, out var hr) ? hr : new List<AssessmentAttemptResponseArchive>()
                });
            }

            // (3) Attempt LIVE saat ini (hanya bila ada baris live; provenance dari session).
            if (curList.Count > 0)
            {
                int maxArchived = histList.Count > 0 ? histList.Max(h => h.AttemptNumber) : 0;
                result.Add(new RiwayatAttemptViewModel
                {
                    AttemptNumber = maxArchived + 1,
                    ScorePercent  = current?.Score,
                    IsPassed      = current?.IsPassed,
                    CompletedAt   = current?.CompletedAt,
                    IsCurrent     = true,
                    Rows          = curList.ToList()
                });
            }

            // (4) Terbaru dulu (current floats ke atas natural — AttemptNumber tertinggi).
            return result.OrderByDescending(vm => vm.AttemptNumber).ToList();
        }
    }
}
