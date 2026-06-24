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

        /// <summary>
        /// CERT-03 (D-03) — assign NomorSertifikat atomik: retry+jitter di atas filtered unique index
        /// IX_AssessmentSessions_NomorSertifikat_Unique. Race-safe (WHERE NomorSertifikat==null + ExecuteUpdateAsync).
        /// Return true bila cert tersimpan; false bila gagal setelah semua attempt (caller WAJIB non-destruktif:
        /// sesi sudah Completed/IsPassed sebelum cert -> JANGAN rollback, tandai utk HC). Loop dikonsolidasi
        /// dari 3 site grading-time (kill-drift). Tanpa schema baru (migration=FALSE).
        /// </summary>
        public static async Task<bool> TryAssignNextSeqAsync(
            ApplicationDbContext context, int sessionId, DateTime certNow, int maxAttempts = 8)
        {
            int certYear = certNow.Year;
            int attempts = 0;
            while (attempts < maxAttempts)
            {
                attempts++;
                try
                {
                    var nextSeq = await GetNextSeqAsync(context, certYear);
                    var nomor = Build(nextSeq, certNow);
                    var updated = await context.AssessmentSessions
                        .Where(s => s.Id == sessionId && s.NomorSertifikat == null)
                        .ExecuteUpdateAsync(s => s.SetProperty(r => r.NomorSertifikat, nomor));
                    if (updated > 0) return true;
                    // WR-01: updated == 0 bisa (a) sudah ber-NomorSertifikat (idempotent OK) atau
                    // (b) sessionId tidak ada (anomali). Konfirmasi via re-query → true HANYA bila cert benar terisi.
                    return await context.AssessmentSessions
                        .AnyAsync(s => s.Id == sessionId && s.NomorSertifikat != null);
                }
                catch (DbUpdateException ex) when (attempts < maxAttempts && IsDuplicateKeyException(ex))
                {
                    // D-03 jitter: kurangi thundering-herd MAX+1 saat finalize burst.
                    await Task.Delay(Random.Shared.Next(10, 60));
                }
            }
            return false;   // gagal setelah maxAttempts — caller tandai non-destruktif (Wave 2).
        }
    }
}
