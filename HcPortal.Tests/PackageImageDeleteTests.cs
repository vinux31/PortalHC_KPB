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
    //
    // Phase 366 (D-04): mirror in-memory ini DIPERTAHANKAN HANYA sebagai fast logic-contract test
    // (tak butuh SQL), BUKAN implementasi paralel. Production source of truth:
    // Helpers/ImageFileCleanup.cs. Integration coverage: ImageCleanupIntegrationTests.cs
    // (real-SQL, exercise helper produksi end-to-end). Kalau ubah kontrak ref-count, ubah helper
    // produksi + integration test dulu — mirror ini ikut, jangan jadi sumber-kebenaran kedua.
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

    // Mirror of AssessmentAdminController.ApplyOptionImageIntent (D-05 resolution) — keep in sync.
    // savedNewPath mensimulasikan hasil SaveFileAsync (file baru). newFilePresent=true berarti admin pilih file.
    private static void ApplyIntent(PackageOption target, bool newFilePresent, string? savedNewPath,
        string? alt, bool removeChecked, List<string> deleteList)
    {
        if (newFilePresent)
        {
            if (!string.IsNullOrEmpty(target.ImagePath)) deleteList.Add(target.ImagePath!);
            target.ImagePath = savedNewPath;
            target.ImageAlt = alt;        // IGNORE checkbox (file baru menang)
        }
        else if (removeChecked)
        {
            if (!string.IsNullOrEmpty(target.ImagePath)) deleteList.Add(target.ImagePath!);
            target.ImagePath = null;
            target.ImageAlt = null;
        }
        else
        {
            target.ImageAlt = alt;        // keep gambar
        }
    }

    [Fact]
    public void RefCount_DeletePackage_SkipsShared_DeletesOrphan()
    {
        // D-11: mirror DeletePackage ref-count loop. Path dishare Post → SKIP; orphan (Pre+Post hilang) → delete.
        var dir = MakeTempDir();
        try
        {
            var shared = Path.Combine(dir, "shared.jpg");
            var orphan = Path.Combine(dir, "orphan.jpg");
            File.WriteAllBytes(shared, new byte[] { 1 });
            File.WriteAllBytes(orphan, new byte[] { 2 });

            // Path-collect dari paket Pre yang dihapus (union soal+opsi).
            var collected = new List<string> { shared, orphan };

            // Setelah cascade+auto-sync: `shared` masih dipakai opsi Post; `orphan` tidak.
            var remainingQuestions = new List<PackageQuestion>();
            var remainingOptions = new List<PackageOption>
            {
                new PackageOption { OptionText = "Post", IsCorrect = true, ImagePath = shared }
            };

            foreach (var relUrl in collected.Distinct())
                DeleteIfUnreferenced(relUrl, remainingQuestions, remainingOptions);

            Assert.True(File.Exists(shared), "shared harus tetap (D-10 skip, masih dipakai Post).");
            Assert.False(File.Exists(orphan), "orphan harus terhapus (Pre+Post hilang).");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ReplaceConflict_NewFileWins_OverRemoveCheckbox()
    {
        // D-05: file baru DIPILIH + checkbox hapus DICENTANG pada item sama → file baru menang.
        var target = new PackageOption { OptionText = "A", IsCorrect = true, ImagePath = "/uploads/questions/7/old.jpg", ImageAlt = "lama" };
        var deleteList = new List<string>();

        ApplyIntent(target, newFilePresent: true, savedNewPath: "/uploads/questions/7/new.jpg",
            alt: "baru", removeChecked: true, deleteList);

        Assert.Equal("/uploads/questions/7/new.jpg", target.ImagePath);   // path baru menang
        Assert.NotNull(target.ImagePath);
        Assert.Contains("/uploads/questions/7/old.jpg", deleteList);       // path lama jadi delete-candidate
        Assert.Equal("baru", target.ImageAlt);
    }

    [Fact]
    public void Replace_NewFileWins_DeletesOldFileOnDisk()
    {
        // SYN-02 replace: gap D-02 — bukti file LAMA benar di-File.Delete dari disk
        // (bukan sekadar masuk delete-candidate list seperti ReplaceConflict di atas).
        var dir = MakeTempDir();
        try
        {
            var oldPath = Path.Combine(dir, "old.jpg");
            var newPath = Path.Combine(dir, "new.jpg");
            File.WriteAllBytes(oldPath, new byte[] { 1 });
            File.WriteAllBytes(newPath, new byte[] { 2 });

            var target = new PackageOption { OptionText = "A", IsCorrect = true, ImagePath = oldPath };
            var deleteList = new List<string>();
            ApplyIntent(target, newFilePresent: true, savedNewPath: newPath,
                alt: "baru", removeChecked: false, deleteList);

            // Tidak ada baris lain memuat oldPath → harus dihapus on disk.
            var remainingQ = new List<PackageQuestion>();
            var remainingO = new List<PackageOption> { target }; // target kini menunjuk newPath
            foreach (var p in deleteList.Distinct())
                DeleteIfUnreferenced(p, remainingQ, remainingO);

            Assert.False(File.Exists(oldPath), "file LAMA harus terhapus on disk (SYN-02 replace).");
            Assert.True(File.Exists(newPath), "file BARU harus tetap ada.");
            Assert.Equal(newPath, target.ImagePath);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void OptionPreserve_KeepsImagePath_WhenOptionUntouched()
    {
        // OQ1: edit teks opsi tanpa file & tanpa checkbox → ImagePath opsi TETAP (tidak hilang).
        var target = new PackageOption { OptionText = "A", IsCorrect = true, ImagePath = "/uploads/questions/7/keep.jpg", ImageAlt = "tetap" };
        var deleteList = new List<string>();

        // update text saja (di controller: slot.OptionText/IsCorrect di-set) lalu intent keep:
        target.OptionText = "A (diedit)";
        ApplyIntent(target, newFilePresent: false, savedNewPath: null,
            alt: "tetap", removeChecked: false, deleteList);

        Assert.Equal("/uploads/questions/7/keep.jpg", target.ImagePath);   // gambar TIDAK berubah
        Assert.Empty(deleteList);                                          // tidak ada delete-candidate
        Assert.Equal("A (diedit)", target.OptionText);
    }
}
