using System;
using System.Collections.Generic;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Phase 418 D-418-02 — pure edit-shrink guard logic (RED Wave 0 STUB).
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
    /// KONTRAK (Plan 418-02 WAJIB implementasikan body ini, signature TIDAK boleh berubah):
    ///   FindBlockedOptionIds(removedOptionIds, answeredOptionIds) = irisan distinct kedua himpunan.
    ///   Empty = boleh hapus; non-empty = blokir (daftar Id opsi terblok).
    ///
    /// STUB RED Wave 0 (Plan 418-01): body sengaja NotImplementedException agar test
    /// EditShrinkGuardLogicTests gagal di runtime (true RED) sambil test project tetap compile.
    /// Plan 418-02 (GREEN) mengganti body dengan implementasi irisan nyata.
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
        {
            // RED Wave 0 STUB — Plan 418-02 mengisi implementasi irisan distinct.
            throw new NotImplementedException(
                "OptionShrinkGuard.FindBlockedOptionIds belum diimplementasikan — diisi di Plan 418-02 (GREEN).");
        }
    }
}
