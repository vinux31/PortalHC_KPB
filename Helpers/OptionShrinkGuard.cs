using System.Collections.Generic;
using System.Linq;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Phase 418 D-418-02 — pure edit-shrink guard logic (GREEN — Plan 418-02).
    ///
    /// Latar: saat <c>EditQuestion</c> MENGHAPUS/menyusutkan <c>PackageOption</c> (mis. 4→3, atau konversi
    /// tipe yang menghapus opsi), opsi yang sudah pernah dijawab peserta direferensikan oleh
    /// <c>PackageUserResponse.PackageOptionId</c> dengan FK = Restrict (ApplicationDbContext.cs:561).
    /// Menghapusnya melempar <c>DbUpdateException</c> → 500 mentah (hazard backlog 999.14).
    ///
    /// Helper ini mengisolasi keputusan murni "opsi mana yang terblokir" sebagai irisan set —
    /// <c>removedOptionIds ∩ answeredOptionIds</c> — agar bisa diuji TANPA DbContext (pola
    /// QuestionOptionValidator). Controller (Plan 418-02) memanggilnya SEBELUM SaveChangesAsync:
    /// bila hasilnya tidak kosong → tolak via TempData["Error"] + redirect (bukan 500).
    ///
    /// KONTRAK (locked oleh Plan 418-01, signature TIDAK boleh berubah):
    ///   FindBlockedOptionIds(removedOptionIds, answeredOptionIds) = irisan distinct kedua himpunan.
    ///   Empty = boleh hapus; non-empty = blokir (daftar Id opsi terblok).
    /// </summary>
    public static class OptionShrinkGuard
    {
        /// <summary>
        /// Kembalikan Id opsi yang AKAN dihapus TAPI sudah dijawab peserta
        /// (irisan <paramref name="removedOptionIds"/> ∩ <paramref name="answeredOptionIds"/>, distinct).
        /// Daftar kosong = aman dihapus.
        /// </summary>
        public static IReadOnlyList<int> FindBlockedOptionIds(
            IEnumerable<int> removedOptionIds,
            IEnumerable<int> answeredOptionIds)
            // Intersect sudah set-semantics (mengembalikan elemen distinct) — tak perlu .Distinct() lagi (IN-01).
            => removedOptionIds.Intersect(answeredOptionIds).ToList();
    }
}
