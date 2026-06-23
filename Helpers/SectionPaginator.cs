using System;
using System.Collections.Generic;
using HcPortal.Models;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Phase 417 (PAG-01/02/03) — Pure section-aware page computation (no EF, no DB, fully synchronous).
    /// Single source of truth untuk PageNumber per-soal yang dikonsumsi CMPController.StartExam + StartExam.cshtml.
    /// Algoritma §7.2: iterasi soal yang SUDAH urut Section 1→2→…→Lainnya (dari GetShuffledQuestionIds, Phase 416),
    /// naikkan halaman bila (a) Section berubah ke Section ber-StartNewPage=true (kecuali soal pertama), ATAU
    /// (b) halaman sudah berisi perPage soal.
    ///
    /// BACKWARD-COMPAT (invariant): bila SEMUA SectionNumber=null → sectionChanged hanya true di soal pertama
    /// (sentinel) → needNewPageForSection selalu false (guard !firstQuestion) → page hanya naik karena pageFull
    /// → PageNumber IDENTIK index/perPage lama (golden-baseline). NON-RNG, deterministik, idempotent.
    ///
    /// Page-number TIDAK disimpan per-soal (D-11) — selalu dihitung saat render. Pure by design
    /// (only System/Collections/HcPortal.Models) → unit-testable tanpa database.
    /// </summary>
    public static class SectionPaginator
    {
        public static void ComputePages(IList<ExamQuestionItem> ordered, int perPage)
        {
            if (ordered == null) throw new ArgumentNullException(nameof(ordered));
            if (perPage < 1) perPage = 1;

            int page = 0;
            int countOnPage = 0;
            int? prevSection = -1;          // sentinel ≠ section nyata / null
            bool firstQuestion = true;

            foreach (var q in ordered)
            {
                bool sectionChanged = !Equals(q.SectionNumber, prevSection);
                bool needNewPageForSection = sectionChanged && q.SectionStartNewPage && !firstQuestion;
                bool pageFull = countOnPage >= perPage;

                if (needNewPageForSection || pageFull)
                {
                    page++;
                    countOnPage = 0;
                }

                q.PageNumber = page;
                q.IsSectionStart = sectionChanged;
                q.IsSectionContinuation = !sectionChanged && countOnPage == 0;

                countOnPage++;
                prevSection = q.SectionNumber;
                firstQuestion = false;
            }
        }

        /// <summary>Clamp page-index resume ke [0, maxPage]; di luar rentang/negatif → 0 (D-417-05).</summary>
        public static int ClampResumePage(int requested, int maxPage)
        {
            if (requested < 0 || requested > maxPage) return 0;
            return requested;
        }
    }
}
