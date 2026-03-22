using Microsoft.EntityFrameworkCore;
using HcPortal.Data;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Shared helper for NomorSertifikat generation (Phase 227 CLEN-04).
    /// Extracted from AdminController private methods to allow reuse in CMPController.
    /// </summary>
    public static class CertNumberHelper
    {
        public static string ToRomanMonth(int month) => month switch
        {
            1 => "I", 2 => "II", 3 => "III", 4 => "IV",
            5 => "V", 6 => "VI", 7 => "VII", 8 => "VIII",
            9 => "IX", 10 => "X", 11 => "XI", 12 => "XII",
            _ => throw new ArgumentOutOfRangeException(nameof(month))
        };

        public static string Build(int seq, DateTime date)
            => $"KPB/{seq:D3}/{ToRomanMonth(date.Month)}/{date.Year}";

        public static async Task<int> GetNextSeqAsync(ApplicationDbContext context, int year)
        {
            var existing = await context.AssessmentSessions
                .Where(s => s.NomorSertifikat != null && s.NomorSertifikat.EndsWith($"/{year}"))
                .Select(s => s.NomorSertifikat!)
                .ToListAsync();

            return existing.Count == 0 ? 1 :
                existing.Select(n => {
                    var parts = n.Split('/');
                    return parts.Length > 1 && int.TryParse(parts[1], out int v) ? v : 0;
                }).Max() + 1;
        }

        public static bool IsDuplicateKeyException(DbUpdateException ex)
        {
            return ex.InnerException?.Message.Contains("IX_AssessmentSessions_NomorSertifikat") == true
                || ex.InnerException?.Message.Contains("2601") == true
                || ex.InnerException?.Message.Contains("2627") == true;
        }
    }
}
