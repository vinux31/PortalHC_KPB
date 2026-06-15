using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HcPortal.Data;

namespace HcPortal.Helpers
{
    public static class ImageFileCleanup
    {
        /// <summary>
        /// Phase 366: Ref-count + physical delete. Untuk tiap path, jika TIDAK ada baris
        /// PackageQuestion/PackageOption yang masih mereferensikannya (post-commit AnyAsync = false
        /// → path khusus-batch), File.Delete; selain itu SKIP (shared Pre/Post path selamat, SC#3).
        /// Warn-only per file (pola Phase 333). WAJIB dipanggil SETELAH tx.CommitAsync.
        /// Path hanya dari kolom DB ImagePath (upload flow tervalidasi) — helper TIDAK menerima
        /// path eksternal user. File.Delete confined di bawah webRootPath via Path.Combine.
        /// </summary>
        public static async Task DeleteUnreferencedAsync(
            ApplicationDbContext ctx,
            string webRootPath,
            ILogger logger,
            IEnumerable<string> paths,
            string source = "")
        {
            foreach (var relUrl in paths.Distinct())
            {
                if (string.IsNullOrEmpty(relUrl)) continue;
                bool stillUsedQ = await ctx.PackageQuestions.AnyAsync(x => x.ImagePath == relUrl);
                bool stillUsedO = await ctx.PackageOptions.AnyAsync(x => x.ImagePath == relUrl);
                if (stillUsedQ || stillUsedO) continue;
                try
                {
                    var physical = Path.Combine(webRootPath, relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
                }
                catch (Exception fex)
                {
                    logger.LogWarning(fex, "File.Delete post-commit failed ({Source}): {Path}", source, relUrl);
                }
            }
        }
    }
}
