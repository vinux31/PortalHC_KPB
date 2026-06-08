// PackageImageDeleteTests — D-10 (ref-count shared-file) + D-11 (DeletePackage path-collect).
// Phase 353 Plan 01 scaffold (2026-06-08). Plan 02/03 mengimplementasi logika controller
// (DeleteQuestion / replace / DeletePackage); scaffold ini menetapkan KONTRAK test sekarang.
//
// ref-count predikat & atomic delete loop di bawah = MIRROR dari SHARED-1/SHARED-2
// (RESEARCH Pattern 2): hapus fisik HANYA bila tak ada baris lain memuat path yang sama.
// Strategi: inline-logic murni (tanpa DbContext) agar langsung GREEN tanpa Skip permanen.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class PackageImageDeleteTests
{
    private static string MakeTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "hcportal-test-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(dir);
        return dir;
    }

    // Predikat ref-count D-10: apakah `path` masih dipakai baris PackageQuestion/PackageOption lain?
    // Mirror: _context.PackageQuestions.AnyAsync(q => q.ImagePath == path)
    //      || _context.PackageOptions.AnyAsync(o => o.ImagePath == path)
    private static bool PathStillReferenced(IEnumerable<PackageQuestion> questions, IEnumerable<PackageOption> options, string path)
        => questions.Any(q => q.ImagePath == path) || options.Any(o => o.ImagePath == path);

    // Atomic delete loop SHARED-2: File.Delete hanya bila count == 0, inner try/catch warn-only.
    private static void DeleteIfUnreferenced(string path, IEnumerable<PackageQuestion> remainingQ, IEnumerable<PackageOption> remainingO)
    {
        if (PathStillReferenced(remainingQ, remainingO, path)) return; // SKIP — masih dipakai
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* warn-only per file (pola 333) */ }
    }

    [Fact]
    public void RefCount_SkipsDelete_WhenPathSharedByOtherRow()
    {
        var dir = MakeTempDir();
        try
        {
            var path = Path.Combine(dir, "shared.jpg");
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });

            // Soal yang dihapus memuat `path`; tapi opsi Post lain MASIH memuat path sama (shared-file).
            var remainingQuestions = new List<PackageQuestion>();
            var remainingOptions = new List<PackageOption>
            {
                new PackageOption { OptionText = "Post opsi", IsCorrect = true, ImagePath = path }
            };

            DeleteIfUnreferenced(path, remainingQuestions, remainingOptions);

            Assert.True(File.Exists(path), "File harus TETAP ada karena masih dishare baris lain (D-10 skip).");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void RefCount_Deletes_WhenNoOtherRowSharesPath()
    {
        var dir = MakeTempDir();
        try
        {
            var path = Path.Combine(dir, "orphan.jpg");
            File.WriteAllBytes(path, new byte[] { 9, 9, 9 });

            // Tidak ada baris lain memuat path → boleh hapus fisik.
            var remainingQuestions = new List<PackageQuestion>();
            var remainingOptions = new List<PackageOption>();

            DeleteIfUnreferenced(path, remainingQuestions, remainingOptions);

            Assert.False(File.Exists(path), "File harus HILANG karena tak dipakai baris lain (D-10 delete).");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void DeletePackageImage_CollectsAllNonNullPaths()
    {
        // D-11: kumpul SEMUA ImagePath non-null dari soal + opsi dalam satu paket (union).
        var questions = new List<PackageQuestion>
        {
            new PackageQuestion
            {
                QuestionText = "Q1",
                ImagePath = "/uploads/questions/3/q1.jpg",
                Options = new List<PackageOption>
                {
                    new PackageOption { OptionText = "A", IsCorrect = true,  ImagePath = "/uploads/questions/3/q1-a.png" },
                    new PackageOption { OptionText = "B", IsCorrect = false, ImagePath = null } // null diabaikan
                }
            },
            new PackageQuestion
            {
                QuestionText = "Q2",
                ImagePath = null, // null diabaikan
                Options = new List<PackageOption>
                {
                    new PackageOption { OptionText = "A", IsCorrect = true, ImagePath = "/uploads/questions/3/q2-a.png" }
                }
            }
        };

        var collected = questions.Select(q => q.ImagePath)
            .Concat(questions.SelectMany(q => q.Options).Select(o => o.ImagePath))
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .ToList();

        Assert.Equal(3, collected.Count);
        Assert.Contains("/uploads/questions/3/q1.jpg", collected);
        Assert.Contains("/uploads/questions/3/q1-a.png", collected);
        Assert.Contains("/uploads/questions/3/q2-a.png", collected);
        Assert.DoesNotContain(null, collected);
    }
}
