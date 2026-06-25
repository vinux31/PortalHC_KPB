using Microsoft.AspNetCore.Mvc;

namespace HcPortal.Helpers
{
    /// <summary>
    /// v32.7 Phase 425 (CLN-05 / VAL-07) — guard-helper MINIMAL untuk menyeragamkan respons JSON gagal.
    /// Membungkus <c>Json(new { success = false, message })</c> agar konsisten/DRY TANPA mengubah signature
    /// action atau shape output (byte-identik: {"success":false,"message":"..."} camelCase via default
    /// System.Text.Json — Program.cs tidak punya AddJsonOptions kustom). D-04: minimal, TANPA
    /// [ApiController]/DTO ber-anotasi. Analog pola static EF-free <see cref="CertIssuanceRules"/> (fase 423).
    /// Diterapkan SELEKTIF ke cluster representatif (SubmitEssayScore), BUKAN sweep semua call-site.
    /// </summary>
    public static class ControllerGuards
    {
        // Identik dgn ControllerBase.Json(new { success = false, message }) — lewat JsonResult pipeline MVC.
        // Shape: {"success":false,"message":"..."} (camelCase, urutan success lalu message).
        public static JsonResult JsonFail(this ControllerBase controller, string message)
            => new JsonResult(new { success = false, message });
    }
}
