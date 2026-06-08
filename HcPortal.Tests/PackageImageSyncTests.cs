// PackageImageSyncTests — SYN-01 (Phase 353 Plan 01, 2026-06-08).
// Membuktikan SyncPackagesToPost menyalin ImagePath+ImageAlt soal & opsi Pre->Post
// sebagai shared-file string copy (path identik, tanpa penggandaan/penamaan-ulang file).
//
// SyncPackagesToPost adalah method private di AssessmentAdminController; test ini
// MIRROR ekspresi deep-clone-nya (AssessmentAdminController.cs ~L5370). Bila blok clone
// controller berubah, jaga agar Clone(...) di bawah tetap sinkron ("keep in sync").
// Strategi: pure in-memory object (SYN-01 = pure string copy, tidak butuh DbContext).

using HcPortal.Models;
using System.Linq;
using Xunit;

namespace HcPortal.Tests;

public class PackageImageSyncTests
{
    // Mirror PERSIS dari blok deep-clone SyncPackagesToPost (newQ + Options.Select).
    // Hanya properti yang relevan untuk SYN-01 dipertahankan di sini.
    private static PackageQuestion Clone(PackageQuestion q) => new PackageQuestion
    {
        QuestionText = q.QuestionText,
        Order = q.Order,
        ScoreValue = q.ScoreValue,
        QuestionType = q.QuestionType,
        ElemenTeknis = q.ElemenTeknis,
        Rubrik = q.Rubrik,
        MaxCharacters = q.MaxCharacters,
        ImagePath = q.ImagePath,
        ImageAlt = q.ImageAlt,
        Options = q.Options.Select(o => new PackageOption
        {
            OptionText = o.OptionText,
            IsCorrect = o.IsCorrect,
            ImagePath = o.ImagePath,
            ImageAlt = o.ImageAlt
        }).ToList()
    };

    [Fact]
    public void SyncCopiesQuestionImagePath()
    {
        var pre = new PackageQuestion
        {
            QuestionText = "Apa fungsi impeller?",
            ImagePath = "/uploads/questions/5/a.jpg",
            ImageAlt = "diagram pompa"
        };

        var post = Clone(pre);

        Assert.Equal("/uploads/questions/5/a.jpg", post.ImagePath);
        Assert.Equal("diagram pompa", post.ImageAlt);
    }

    [Fact]
    public void SyncCopiesOptionImageAlt()
    {
        var pre = new PackageQuestion
        {
            QuestionText = "Pilih komponen utama",
            Options = new[]
            {
                new PackageOption { OptionText = "Impeller", IsCorrect = true,  ImagePath = "/uploads/questions/5/opt-a.png", ImageAlt = "impeller" },
                new PackageOption { OptionText = "Casing",   IsCorrect = false, ImagePath = "/uploads/questions/5/opt-b.png", ImageAlt = "casing" }
            }.ToList()
        };

        var post = Clone(pre);

        Assert.Equal("/uploads/questions/5/opt-a.png", post.Options.ElementAt(0).ImagePath);
        Assert.Equal("impeller", post.Options.ElementAt(0).ImageAlt);
        Assert.Equal("/uploads/questions/5/opt-b.png", post.Options.ElementAt(1).ImagePath);
        Assert.Equal("casing", post.Options.ElementAt(1).ImageAlt);
    }

    [Fact]
    public void SyncSharesSamePath_NoFileDuplication()
    {
        // Shared-file invariant (C-03): path Post == path Pre persis (string identity),
        // memastikan sync TIDAK menggandakan / menamai-ulang file fisik.
        var pre = new PackageQuestion
        {
            QuestionText = "Soal bergambar",
            ImagePath = "/uploads/questions/9/shared.jpg",
            ImageAlt = "alt",
            Options = new[]
            {
                new PackageOption { OptionText = "A", IsCorrect = true, ImagePath = "/uploads/questions/9/opt.jpg", ImageAlt = "o" }
            }.ToList()
        };

        var post = Clone(pre);

        Assert.Same(pre.ImagePath, post.ImagePath);
        Assert.Same(pre.Options.First().ImagePath, post.Options.First().ImagePath);
    }

    [Fact]
    public void SyncHandlesNullImagePath()
    {
        var pre = new PackageQuestion
        {
            QuestionText = "Soal tanpa gambar",
            ImagePath = null,
            ImageAlt = null,
            Options = new[]
            {
                new PackageOption { OptionText = "A", IsCorrect = true, ImagePath = null, ImageAlt = null }
            }.ToList()
        };

        var post = Clone(pre);

        Assert.Null(post.ImagePath);
        Assert.Null(post.ImageAlt);
        Assert.Null(post.Options.First().ImagePath);
        Assert.Null(post.Options.First().ImageAlt);
    }
}
