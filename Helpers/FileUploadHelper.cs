using Microsoft.AspNetCore.Http;

namespace HcPortal.Helpers
{
    public static class FileUploadHelper
    {
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
    }
}
