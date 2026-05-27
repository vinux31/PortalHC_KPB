using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HcPortal.Models;

namespace HcPortal.Helpers
{
    public static class FileUploadHelper
    {
        /// <summary>
        /// Phase 325 D-02/D-03/D-09: Validasi ekstensi + size + magic byte signature.
        /// Returns (true, null) kalau valid, (false, errorMessage) kalau invalid.
        /// </summary>
        public static (bool IsValid, string? Error) ValidateCertificateFile(IFormFile? file)
        {
            if (file == null || file.Length == 0) return (true, null);

            // Spec line 100: tambah .ToLowerInvariant() (micro-cleanup walau HashSet sudah OrdinalIgnoreCase).
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AssessmentConstants.FileValidation.AllowedCertificateExtensions.Contains(ext))
                return (false, "Hanya file PDF, JPG, dan PNG yang diperbolehkan.");

            if (file.Length > AssessmentConstants.FileValidation.MaxCertificateFileSizeBytes)
                return (false, "Ukuran file maksimal 10MB.");

            // Phase 325 D-02/D-03: Magic byte check — baca 8 byte awal stream, reset Position=0 untuk SaveFileAsync reuse.
            using var stream = file.OpenReadStream();
            var header = new byte[8];
            var read = stream.Read(header, 0, 8);
            stream.Position = 0; // reset agar SaveFileAsync tetap dapat full stream

            // Pitfall 5 RESEARCH: reject file lebih kecil dari smallest magic prefix (JPG 3-byte).
            if (read < 3)
                return (false, "Isi file tidak cocok dengan ekstensi (magic byte mismatch).");

            if (!AssessmentConstants.FileValidation.MatchesMagicByte(ext, header))
                return (false, "Isi file tidak cocok dengan ekstensi (magic byte mismatch).");

            return (true, null);
        }

        /// <summary>
        /// Phase 325 D-01/D-10: Save file ke wwwroot/{subFolder}, strip directory component dari filename (path traversal),
        /// log warning kalau original filename mengandung path component (audit trail).
        /// Returns relative URL, atau null kalau file null/empty.
        /// </summary>
        public static async Task<string?> SaveFileAsync(
            IFormFile? file,
            string webRootPath,
            string subFolder,
            ILogger? logger = null)
        {
            if (file == null || file.Length == 0) return null;

            var uploadDir = Path.Combine(webRootPath, subFolder);
            Directory.CreateDirectory(uploadDir);

            // HIGH-01 fix: timestamp ms + short GUID cegah collision saat concurrent upload
            var uniqueId = Guid.NewGuid().ToString("N")[..8];

            // Phase 325 D-01: Strip directory component dari filename (defense-in-depth path traversal).
            var originalName = Path.GetFileName(file.FileName);

            // Phase 325 D-10: Audit trail kalau filename asli mengandung path component (forensik attack pattern).
            if (logger != null && !string.Equals(originalName, file.FileName, StringComparison.Ordinal))
            {
                logger.LogWarning(
                    "Path traversal attempt: filename={Original} stripped to {Safe}",
                    file.FileName, originalName);
            }

            var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{uniqueId}_{originalName}";
            var filePath = Path.Combine(uploadDir, safeFileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            // Normalisasi path separator untuk URL
            return $"/{subFolder.Trim().Trim('\\', '/')}/{safeFileName}";
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
