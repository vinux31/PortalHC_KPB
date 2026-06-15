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
    }
}
