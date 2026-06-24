using Microsoft.AspNetCore.Http;

namespace HcPortal.Models
{
    /// <summary>
    /// Phase 418 (OPT-01) — binding model untuk opsi jawaban dinamis (2–6) di form authoring
    /// (CreateQuestion / EditQuestion POST). Menggantikan 16 param diskret A–D
    /// (optionA..D + correctA..D + optionA..DImage/Alt + removeOptionA..DImage).
    ///
    /// Di-bind via indexed convention ASP.NET Core: options[i].Text, options[i].IsCorrect,
    /// options[i].Image (file), options[i].ImageAlt, options[i].RemoveImage.
    ///
    /// KEAMANAN (T-418-06 mass-assignment): properti di-whitelist EKSPLISIT.
    /// JANGAN tambah properti Id — Id PackageOption ditentukan SERVER (preserve via existing[i]),
    /// tidak boleh disuplai client.
    /// </summary>
    public class OptionInput
    {
        /// <summary>Teks opsi. Kosong/whitespace = baris diabaikan (mirror aturan import "kosong diabaikan").</summary>
        public string? Text { get; set; }

        /// <summary>Flag jawaban benar. Untuk MultipleChoice di-override server via correctIndex (single-select).</summary>
        public bool IsCorrect { get; set; }

        /// <summary>Gambar opsi (authoring only). Inject tidak punya gambar (scope Phase 394).</summary>
        public IFormFile? Image { get; set; }

        /// <summary>Alt-text gambar opsi (a11y).</summary>
        public string? ImageAlt { get; set; }

        /// <summary>Niat hapus gambar opsi existing (EditQuestion).</summary>
        public bool RemoveImage { get; set; }
    }
}
