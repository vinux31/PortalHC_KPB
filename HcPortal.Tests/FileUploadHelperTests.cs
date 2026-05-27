// Unit test FileUploadHelper.ValidateCertificateFile (Phase 325 Plan 01 — Wave 0 foundation).
// 5 test GREEN sekarang (extension + null guard) + 2 test SKIP TODO Plan 02 (magic byte gate D-09).
// Plan 02 nanti hapus [Fact(Skip=...)] attribute + uncomment body setelah implementasi MatchesMagicByte.

using HcPortal.Helpers;
using HcPortal.Models;
using Microsoft.AspNetCore.Http;
using Xunit;
using System.IO;

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
}
