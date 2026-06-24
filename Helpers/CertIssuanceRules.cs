using System;
using System.Text.RegularExpressions;
using HcPortal.Models;

namespace HcPortal.Helpers
{
    /// <summary>
    /// v32.7 Phase 423 (CERT-01..07) — single source-of-truth kelayakan &amp; derivasi sertifikat.
    /// Pure EF-free supaya bisa di-unit-test tanpa DbContext/controller. Dipakai DI EMPAT cert-issue
    /// site (Wave 2) + DUA view PendingGrading (Wave 3). Analog <see cref="SessionEditLockRules"/> (fase 422).
    /// </summary>
    public static class CertIssuanceRules
    {
        // CERT-01 (D-01) — gate tunggal: tolak PreTest, wajib GenerateCertificate &amp;&amp; lulus. Dipakai di 4 site.
        public static bool ShouldIssueCertificate(AssessmentSession s)
            => s.GenerateCertificate
               && s.IsPassed == true
               && s.AssessmentType != AssessmentConstants.AssessmentType.PreTest;

        // CERT-02/06 (D-04/D-05/D-10) — derive ValidUntil dari CompletedAt utk CertificateType KANONIK saja.
        // Permanent->null; Annual->+1y; 3-Year->+3y; non-kanonik/null->null (caller pakai input apa adanya).
        public static DateOnly? DeriveValidUntil(string? certificateType, DateTime? completedAt)
        {
            if (completedAt == null) return null;
            var baseDate = DateOnly.FromDateTime(completedAt.Value);
            return certificateType switch
            {
                AssessmentConstants.CertificateType.Permanent => null,
                AssessmentConstants.CertificateType.Annual    => baseDate.AddYears(1),
                AssessmentConstants.CertificateType.ThreeYear => baseDate.AddYears(3),
                _ => null
            };
        }

        // CERT-04 (D-02) — nomor manual TIDAK boleh menyerupai format auto KPB/{seq:D3}/{ROMAN}/{YEAR}.
        public static bool ResemblesAutoCertFormat(string? nomor)
            => !string.IsNullOrWhiteSpace(nomor)
               && Regex.IsMatch(nomor!, @"^KPB/\d{3}/[IVX]+/\d{4}$");

        // CERT-07 (D-09) — umur PendingGrading (CompletedAt UTC) -> bootstrap badge class. >7=merah, >3=kuning.
        public static string PendingAgeBadgeClass(DateTime? completedAtUtc, DateTime nowUtc)
        {
            if (completedAtUtc == null) return "bg-secondary";
            var days = (nowUtc - completedAtUtc.Value).TotalDays;
            if (days > 7) return "bg-danger";
            if (days > 3) return "bg-warning text-dark";
            return "bg-secondary";
        }
    }
}
