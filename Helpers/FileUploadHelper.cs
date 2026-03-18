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
            var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadDir, safeFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"/{subFolder.Replace('\\', '/')}/{safeFileName}";
        }
    }
}
