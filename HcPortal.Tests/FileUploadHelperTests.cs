// Unit test FileUploadHelper.ValidateCertificateFile (Phase 325 Plan 01 — Wave 0 foundation).
// 5 test GREEN sekarang (extension + null guard) + 2 test SKIP TODO Plan 02 (magic byte gate D-09).
// Plan 02 nanti hapus [Fact(Skip=...)] attribute + uncomment body setelah implementasi MatchesMagicByte.

using HcPortal.Helpers;
using HcPortal.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HcPortal.Tests;

public class FileUploadHelperTests
{
    // Helper buat IFormFile in-memory dari byte array — tidak perlu file system.
    private static IFormFile MakeFile(string fileName, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }

    // Test ILogger yang capture LogWarning calls untuk verify D-10 audit trail.
    private sealed class TestLogger : ILogger
    {
        public readonly List<(LogLevel Level, string Message)> Logs = new();
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception? exception, System.Func<TState, System.Exception?, string> formatter)
        {
            Logs.Add((logLevel, formatter(state, exception)));
        }
    }

    private static string MakeTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "hcportal-test-" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void ValidateCertificateFile_NullFile_ReturnsValid()
    {
        var (ok, err) = FileUploadHelper.ValidateCertificateFile(null);
        Assert.True(ok);
        Assert.Null(err);
    }

    [Fact]
    public void ValidateCertificateFile_ValidPdf_ReturnsValid()
    {
        var pdf = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4
        var (ok, _) = FileUploadHelper.ValidateCertificateFile(MakeFile("test.pdf", pdf));
        Assert.True(ok);
    }

    [Fact]
    public void ValidateCertificateFile_ValidJpg_ReturnsValid()
    {
        var jpg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        var (ok, _) = FileUploadHelper.ValidateCertificateFile(MakeFile("test.jpg", jpg));
        Assert.True(ok);
    }

    [Fact]
    public void ValidateCertificateFile_ValidPng_ReturnsValid()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var (ok, _) = FileUploadHelper.ValidateCertificateFile(MakeFile("test.png", png));
        Assert.True(ok);
    }

    [Fact]
    public void ValidateCertificateFile_UnsupportedExtension_ReturnsInvalid()
    {
        var bytes = new byte[] { 0x00, 0x01, 0x02 };
        var (ok, err) = FileUploadHelper.ValidateCertificateFile(MakeFile("test.docx", bytes));
        Assert.False(ok);
        Assert.Contains("PDF, JPG", err!);
    }

    [Fact]
    public void ValidateCertificateFile_ExeRenamedPdf_ReturnsInvalidMagicByte()
    {
        var exe = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 }; // MZ exe
        var (ok, err) = FileUploadHelper.ValidateCertificateFile(MakeFile("malware.pdf", exe));
        Assert.False(ok);
        Assert.Contains("magic byte", err!);
    }

    [Fact]
    public void MatchesMagicByte_JpegAliasMatchesJpg()
    {
        var jpg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE1, 0x00, 0x00, 0x00, 0x00 }; // EXIF variant
        Assert.True(AssessmentConstants.FileValidation.MatchesMagicByte(".jpeg", jpg));
        Assert.True(AssessmentConstants.FileValidation.MatchesMagicByte(".jpg", jpg));
    }

    // === SaveFileAsync — SC-1 P01 path traversal coverage (D-01 + D-10) ===

    [Fact]
    public async Task SaveFileAsync_PathTraversalFilename_StripsToFlatNameNoEscape()
    {
        // Phase 325 SC-1: simulasi attack filename "../../etc/passwd.pdf" tidak boleh escape uploads dir.
        var tempRoot = MakeTempDir();
        try
        {
            var pdf = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
            var file = MakeFile("../../malicious.pdf", pdf);

            var url = await FileUploadHelper.SaveFileAsync(file, tempRoot, "uploads");

            Assert.NotNull(url);
            var uploadsDir = Path.Combine(tempRoot, "uploads");
            Assert.True(Directory.Exists(uploadsDir), "uploads subdir harus ada");

            var savedFiles = Directory.GetFiles(uploadsDir);
            Assert.Single(savedFiles);

            var savedName = Path.GetFileName(savedFiles[0]);
            Assert.DoesNotContain("..", savedName);
            Assert.DoesNotContain("/", savedName);
            Assert.DoesNotContain("\\", savedName);
            Assert.EndsWith("_malicious.pdf", savedName);

            // CRITICAL: tidak ada file yang escape ke parent (tempRoot atau parent-of-tempRoot).
            var rootFiles = Directory.GetFiles(tempRoot);
            Assert.Empty(rootFiles);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SaveFileAsync_PathTraversalFilename_LogsWarningD10()
    {
        // Phase 325 D-10: audit trail wajib log Warning saat filename mengandung path component.
        var tempRoot = MakeTempDir();
        var logger = new TestLogger();
        try
        {
            var pdf = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            var file = MakeFile("../../etc/passwd.pdf", pdf);

            await FileUploadHelper.SaveFileAsync(file, tempRoot, "uploads", logger);

            Assert.Contains(logger.Logs, l =>
                l.Level == LogLevel.Warning &&
                l.Message.Contains("Path traversal attempt") &&
                l.Message.Contains("../../etc/passwd.pdf") &&
                l.Message.Contains("passwd.pdf"));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SaveFileAsync_NormalFilename_NoWarningLogged()
    {
        // Phase 325 D-10: filename normal (tanpa path component) TIDAK trigger LogWarning (avoid log noise).
        var tempRoot = MakeTempDir();
        var logger = new TestLogger();
        try
        {
            var pdf = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            var file = MakeFile("normal.pdf", pdf);

            await FileUploadHelper.SaveFileAsync(file, tempRoot, "uploads", logger);

            Assert.DoesNotContain(logger.Logs, l =>
                l.Level == LogLevel.Warning &&
                l.Message.Contains("Path traversal attempt"));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
