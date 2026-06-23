using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Helpers
{
    /// <summary>
    /// v32.7 RTH-03 (RTK-LOGIC-03, D-05) — SATU sumber penghitungan percobaan era-retake.
    /// Kill-drift (pola <see cref="RetakeRules"/>/<see cref="ShuffleToggleRules"/>): predikat snapshot-presence
    /// — hanya <see cref="AssessmentAttemptHistory"/> ber-child <see cref="AssessmentAttemptResponseArchive"/>
    /// yang dihitung. Arsip legacy HC-reset pre-v32.4 (tanpa child) natural-excluded di SEMUA situs.
    ///
    /// <para><b>DB-aware</b> (beda dari ShuffleToggleRules pure) — predikat di-translate EF identik dengan
    /// inline existing (Pitfall: ekspresi <c>archive.Any(a =&gt; a.AttemptHistoryId == h.Id)</c> byte-identik).
    /// JANGAN sertakan <c>+1</c> di sini — caller menambah current-attempt (semantik "arsip vs percobaan" eksplisit
    /// di call-site).</para>
    ///
    /// <para><b>Dua bentuk (Pitfall 3 — JANGAN collapse):</b>
    /// <see cref="CountForUserAsync"/> = per-user count (cap a/b/c di RetakeService/CMP);
    /// <see cref="MaxInGroupAsync"/> = MAX di antara semua user di grup (warning d di ManagePackages).</para>
    /// </summary>
    public static class RetakeCountingRules
    {
        /// <summary>
        /// Base query snapshot-presence (Title/Category + arsip ber-child) — IDENTIK di keempat situs.
        /// Mirror verbatim predikat inline existing RetakeService:145-150/:237-242, CMP:2472-2475.
        /// </summary>
        private static IQueryable<AssessmentAttemptHistory> EraRetakeBase(
            ApplicationDbContext context, string? title, string? category)
            => context.AssessmentAttemptHistory
                .Where(h => h.Title == title && h.Category == category
                         && context.AssessmentAttemptResponseArchives.Any(a => a.AttemptHistoryId == h.Id));

        /// <summary>
        /// Cap per-user (a/b/c): jumlah arsip era-retake untuk (userId, title, category). TANPA <c>+1</c>.
        /// </summary>
        public static Task<int> CountForUserAsync(
            ApplicationDbContext context, string? userId, string? title, string? category)
            => EraRetakeBase(context, title, category)
                .Where(h => h.UserId == userId)
                .CountAsync();

        /// <summary>
        /// Warning (d): MAX jumlah arsip era-retake di antara SEMUA user di grup (title, category). TANPA <c>+1</c>.
        /// GroupBy(UserId) dipertahankan (BUKAN per-user) — yang ditambahkan vs situs lama HANYA filter snapshot-presence.
        /// Grup kosong → 0 (FirstOrDefault int default).
        /// </summary>
        public static Task<int> MaxInGroupAsync(
            ApplicationDbContext context, string? title, string? category)
            => EraRetakeBase(context, title, category)
                .GroupBy(h => h.UserId)
                .Select(g => g.Count())
                .OrderByDescending(c => c)
                .FirstOrDefaultAsync();
    }
}
