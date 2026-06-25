namespace HcPortal.Helpers
{
    /// <summary>
    /// Phase 374 — keputusan pure UI toggle shuffle di ManagePackages.
    /// Dipakai DI DUA TEMPAT (GET ViewBag + POST guard) untuk cegah divergensi (Pitfall 2).
    /// Tidak menyentuh database/EF: caller menyediakan fakta (anyStarted/anyAssignment dst).
    /// </summary>
    public static class ShuffleToggleRules
    {
        // SHUF-11: terkunci bila ada peserta mulai ATAU ada assignment di grup sibling.
        public static bool IsShuffleLocked(bool anyStarted, bool anyAssignment)
            => anyStarted || anyAssignment;

        // SHUF-14: sembunyikan total untuk Proton Tahun 3 ATAU Manual entry.
        public static bool ShouldHideShuffleToggle(string? category, string? tahunKe, bool isManualEntry)
            => (category == "Assessment Proton" && tahunKe == "Tahun 3") || isManualEntry;

        // SHUF-12: warning ukuran-paket-beda hanya bila >=2 paket-ber-soal, Acak Soal OFF, dan ukuran beda.
        public static bool ShouldShowSizeMismatchWarning(int packagesWithQuestions, bool shuffleQuestions, bool hasMismatch)
            => packagesWithQuestions >= 2 && !shuffleQuestions && hasMismatch;

        // v32.7 Phase 422 D-04/SHFX-07: warning K=min truncation ON-path (mirror OFF-path di atas, hanya
        // beda !shuffleQuestions -> shuffleQuestions). Saat Acak Soal ON + ukuran paket beda, ShuffleEngine
        // memangkas tiap paket ke K=min (Helpers/ShuffleEngine.cs:117) -> soal "ekstra" tak pernah muncul.
        // Helper hanya MEMBACA fakta mismatch; TIDAK menduplikasi algoritma min. Teks UI: "soal dipangkas ke K=min".
        public static bool ShouldShowKMinTruncationWarning(int packagesWithQuestions, bool shuffleQuestions, bool hasMismatch)
            => packagesWithQuestions >= 2 && shuffleQuestions && hasMismatch;
    }
}
