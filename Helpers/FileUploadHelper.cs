using Microsoft.AspNetCore.Http;

namespace HcPortal.Helpers
{
    public static class FileUploadHelper
    {
        private static readonly string[] AllowedCertificateExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        private const long MaxCertificateFileSizeBytes = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// Validates certificate file extension and size.
        /// Returns (true, null) if valid, (false, errorMessage) if invalid.
        /// </summary>
        public static (bool IsValid, string? Error) ValidateCertificateFile(IFormFile? file)
        {
            if (file == null || file.Length == 0) return (true, null);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedCertificateExtensions.Contains(ext))
                return (false, "Hanya file PDF, JPG, dan PNG yang diperbolehkan.");

            if (file.Length > MaxCertificateFileSizeBytes)
                return (false, "Ukuran file maksimal 10MB.");

            return (true, null);
        }

        /// <summary>
        /// Saves uploaded file to wwwroot/{subFolder}, returns relative URL.
        /// Returns null if file is null or empty.
        /// </summary>
        public static async Task<string?> SaveFileAsync(IFormFile? file, string webRootPath, string subFolder)
        {
            if (file == null || file.Length == 0) return null;
            var uploadDir = Path.Combine(webRootPath, subFolder);
            Directory.CreateDirectory(uploadDir);
            // HIGH-01 fix: timestamp ms + short GUID cegah collision saat dua upload
            // hit di detik/ms yang sama dengan filename identik.
            var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{uniqueId}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadDir, safeFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"/{subFolder.Replace('\\', '/')}/{safeFileName}";
        }

        /// <summary>
        /// Deletes file from wwwroot given relative URL.
        /// Handles null/empty URLs gracefully.
        /// </summary>
        public static void DeleteFile(string webRootPath, string? relativeUrl)
        {
            if (!string.IsNullOrEmpty(relativeUrl))
            {
                var oldPath = Path.Combine(webRootPath, relativeUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }
        }
    }
}
