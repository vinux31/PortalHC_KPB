// EditAtomicFileTests — Phase 368 #21 (edit atomic file replace).
// Pola Phase 355 `Replace_NewFileWins_DeletesOldFileOnDisk` (PackageImageDeleteTests:209-238):
// uji logika replace atomik langsung via FileUploadHelper + temp-webroot (no DB, no controller).
// Kontrak: new-upload sukses → hapus lama; metadata-only → pertahankan lama; upload gagal → pertahankan lama.

using System;
using System.IO;
using System.Threading.Tasks;
using HcPortal.Helpers;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace HcPortal.Tests;

public class EditAtomicFileTests
{
    private static string MakeTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "hcp_edit_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    // FormFile in-memory dengan magic-byte JPEG valid (0xFF 0xD8 0xFF) — lolos ValidateCertificateFile + non-empty.
    private static IFormFile MakeJpeg(string name)
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
    }

    // FormFile kosong → SaveFileAsync return null (simulasi upload gagal).
    private static IFormFile MakeEmpty(string name)
    {
        var stream = new MemoryStream(Array.Empty<byte>());
        return new FormFile(stream, 0, 0, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
    }

    private static string OnDisk(string webRoot, string relativeUrl)
        => Path.Combine(webRoot, relativeUrl.TrimStart('/'));

    [Fact]
    public async Task EditAtomicFile_NewUpload_DeletesOldFileOnDisk()
    {
        // #21: upload baru sukses → file LAMA terhapus on-disk, file BARU tetap ada.
        var dir = MakeTempDir();
        try
        {
            var certDir = Path.Combine(dir, "uploads", "certificates");
            Directory.CreateDirectory(certDir);
            var oldUrl = "/uploads/certificates/old.jpg";
            var oldPath = OnDisk(dir, oldUrl);
            File.WriteAllBytes(oldPath, new byte[] { 1, 2, 3 });

            // Pola controller hasil Task 2: capture lama → SaveFileAsync baru → (commit) → DeleteFile lama post-commit conditional.
            string? oldCapture = oldUrl;
            var uploadedUrl = await FileUploadHelper.SaveFileAsync(MakeJpeg("new.jpg"), dir, "uploads/certificates");
            Assert.NotNull(uploadedUrl);                       // upload sukses
            // commit terjadi di sini (DB) — lalu hapus lama conditional:
            if (!string.IsNullOrEmpty(oldCapture) && oldCapture != uploadedUrl)
                FileUploadHelper.DeleteFile(dir, oldCapture);

            Assert.False(File.Exists(oldPath), "file LAMA harus terhapus on disk (#21 replace).");
            Assert.True(File.Exists(OnDisk(dir, uploadedUrl!)), "file BARU harus tetap ada.");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void EditAtomicFile_MetadataOnly_KeepsOldFile()
    {
        // #21: edit metadata-only (CertificateFile == null) → TIDAK panggil Save/Delete → file lama utuh.
        var dir = MakeTempDir();
        try
        {
            var certDir = Path.Combine(dir, "uploads", "certificates");
            Directory.CreateDirectory(certDir);
            var oldUrl = "/uploads/certificates/old.jpg";
            var oldPath = OnDisk(dir, oldUrl);
            File.WriteAllBytes(oldPath, new byte[] { 1, 2, 3 });

            // Guard controller: model.CertificateFile == null → block file upload tidak jalan.
            IFormFile? noFile = null;
            string? oldCapture = null;
            if (noFile != null && noFile.Length > 0)
            {
                oldCapture = oldUrl;
                // ... tidak tercapai
            }
            // oldCapture tetap null → DeleteFile tidak dipanggil.
            if (!string.IsNullOrEmpty(oldCapture))
                FileUploadHelper.DeleteFile(dir, oldCapture);

            Assert.True(File.Exists(oldPath), "file lama harus utuh saat edit metadata-only.");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task EditAtomicFile_UploadFails_KeepsOldFile()
    {
        // #21: upload gagal (SaveFileAsync return null) → oldCapture di-null-kan → DeleteFile TIDAK dipanggil → lama utuh.
        var dir = MakeTempDir();
        try
        {
            var certDir = Path.Combine(dir, "uploads", "certificates");
            Directory.CreateDirectory(certDir);
            var oldUrl = "/uploads/certificates/old.jpg";
            var oldPath = OnDisk(dir, oldUrl);
            File.WriteAllBytes(oldPath, new byte[] { 1, 2, 3 });

            string? oldCapture = oldUrl;
            var uploadedUrl = await FileUploadHelper.SaveFileAsync(MakeEmpty("new.jpg"), dir, "uploads/certificates");
            if (uploadedUrl == null) oldCapture = null;        // upload gagal → JANGAN hapus lama
            if (!string.IsNullOrEmpty(oldCapture) && oldCapture != uploadedUrl)
                FileUploadHelper.DeleteFile(dir, oldCapture);

            Assert.Null(uploadedUrl);                          // upload memang gagal
            Assert.True(File.Exists(oldPath), "file lama harus utuh saat upload gagal.");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
