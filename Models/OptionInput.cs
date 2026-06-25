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
    /// Phase 420 (D-01) menambah properti Id (carrier identity) — TIDAK melanggar T-418-06:
    /// Id client-supplied DIVALIDASI server-side (Id ∈ q.Options) sebelum dipakai; forged Id ditolak.
    /// </summary>
    public class OptionInput
    {
        /// <summary>
        /// Phase 420 (D-01) — carrier identity opsi existing. Di-bind dari hidden input
        /// options[i].Id. Null = baris opsi BARU (ADD). Non-null = match record PackageOption
        /// existing untuk UPDATE/REMOVE by stable Id (BUKAN posisi).
        ///
        /// KEAMANAN (revisi T-418-06): Id KINI disuplai client (wajib untuk identity-matching),
        /// TAPI server WAJIB memvalidasi setiap Id non-null ∈ q.Options soal ini SEBELUM dipakai
        /// (lihat EditQuestion POST anti-tamper). Id asing/forged → tolak seluruh edit (fail-closed).
        /// Ini menetralkan mass-assignment/IDOR via validasi eksplisit, BUKAN via melarang properti.
        /// </summary>
        public int? Id { get; set; }

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
