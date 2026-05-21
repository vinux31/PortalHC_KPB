namespace HcPortal.Helpers
{
    public static class SheetNameSanitizer
    {
        private static readonly char[] InvalidChars = { '\\', '/', '?', '*', '[', ']', ':' };
        private const int ExcelSheetNameLimit = 31;

        /// <summary>
        /// Format sheet name {NIP}_{FullName}, truncate ke 31 char dengan collision guard.
        /// NIP-first ensures uniqueness karena NIP guaranteed unique per worker.
        /// </summary>
        /// <param name="nip">NIP peserta (akan di-scrub Excel-invalid chars).</param>
        /// <param name="fullName">Nama lengkap peserta (akan di-scrub).</param>
        /// <param name="usedNames">Set sheet name yang sudah dipakai (case-insensitive). Method ini akan menambahkan hasil ke set.</param>
        /// <returns>Sheet name Excel-safe (max 31 char, no invalid char, unique).</returns>
        public static string Sanitize(string nip, string fullName, ISet<string> usedNames)
        {
            string cleanNip = ScrubChars(nip ?? "");
            string cleanName = ScrubChars(fullName ?? "");
            string raw = $"{cleanNip}_{cleanName}";
            if (raw.Length > ExcelSheetNameLimit)
                raw = raw.Substring(0, ExcelSheetNameLimit);

            // Collision guard (rare — only if truncation creates collision)
            string candidate = raw;
            int counter = 2;
            while (usedNames.Contains(candidate))
            {
                string suffix = $"({counter})";
                int allowed = ExcelSheetNameLimit - suffix.Length;
                candidate = (raw.Length > allowed ? raw.Substring(0, allowed) : raw) + suffix;
                counter++;
            }
            usedNames.Add(candidate);
            return candidate;
        }

        private static string ScrubChars(string s)
        {
            foreach (var c in InvalidChars) s = s.Replace(c, '_');
            return s;
        }
    }
}
