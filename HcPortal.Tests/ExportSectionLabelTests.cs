// Phase 419 PAG-04 (Wave-0 RED) — kontrak export label Section di sheet "Detail Per Soal".
// Drive REAL ExcelExportHelper.AddDetailPerSoalSheet atas SQL Server NYATA (SectionFixture, de-tautology:
// NO replica logika ordering/band di test). RED sekarang untuk band-header + ordering per (SectionNumber, Order);
// GREEN untuk backward-compat (assessment tanpa Section = output legacy identik). Plan 02 membuat RED -> GREEN.
//
// Kontrak yang dikunci (Plan 02 WAJIB penuhi):
//   1. Saat package punya >=1 Section: tambah baris band-header merged "Section {n}: {Nama}" di ATAS grup kolom
//      tiap Section; grup "Lainnya" (SectionId null) terakhir. Urutan kolom soal = (SectionNumber ?? max, Order, Id).
//   2. Saat package TANPA Section: TIDAK ada band (output legacy — kolom No/Nama/NIP di row 1, data row 2).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class ExportSectionLabelTests : IClassFixture<SectionFixture>
{
    private readonly SectionFixture _fixture;
    public ExportSectionLabelTests(SectionFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // Seed 1 session (Completed) + 1 package dengan distribusi Section. dist: (sectionNumber null=Lainnya, name, count).
    // Order soal increment sesuai urutan list (sengaja TIDAK section-sorted agar RED bisa membedakan flat-by-Order).
    private static async Task<(int sessionId, int packageId)> SeedPackageAsync(
        ApplicationDbContext ctx, IEnumerable<(int? sectionNumber, string? name, int count)> dist)
    {
        var u = new ApplicationUser
        {
            UserName = "wkr-" + Guid.NewGuid().ToString("N")[..8], Email = "wkr@test.local",
            FullName = "Worker Test", NIP = "12345", IsActive = true, RoleLevel = 6
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();

        var s = new AssessmentSession
        {
            UserId = u.Id, Title = "Export-" + Guid.NewGuid().ToString("N")[..8],
            Category = "OJT", AssessmentType = "Standard", Status = "Completed",
            AccessToken = "", IsTokenRequired = false, Schedule = DateTime.UtcNow,
            DurationMinutes = 60, PassPercentage = 70, Progress = 0, Score = 0,
            ShuffleQuestions = false, ShuffleOptions = false
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();

        var pkg = new AssessmentPackage { AssessmentSessionId = s.Id, PackageName = "Paket 1", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();

        int order = 1;
        foreach (var (sn, name, count) in dist)
        {
            int? sectionId = null;
            if (sn.HasValue)
            {
                var sec = new AssessmentPackageSection { AssessmentPackageId = pkg.Id, SectionNumber = sn.Value, Name = name };
                ctx.AssessmentPackageSections.Add(sec);
                await ctx.SaveChangesAsync();
                sectionId = sec.Id;
            }
            for (int i = 0; i < count; i++)
            {
                ctx.PackageQuestions.Add(new PackageQuestion
                {
                    AssessmentPackageId = pkg.Id, QuestionText = $"Q-S{sn}-{i}", Order = order++,
                    ScoreValue = 10, QuestionType = "MultipleChoice", SectionId = sectionId,
                    Options = new List<PackageOption> { new() { OptionText = "A", IsCorrect = true }, new() { OptionText = "B", IsCorrect = false } }
                });
            }
            await ctx.SaveChangesAsync();
        }
        return (s.Id, pkg.Id);
    }

    private static IXLWorksheet BuildSheet(ApplicationDbContext ctx, int sessionId, int packageId)
    {
        var session = ctx.AssessmentSessions.Find(sessionId)!;
        var questions = ctx.PackageQuestions.Where(q => q.AssessmentPackageId == packageId)
            .Include(q => q.Options).Include(q => q.Section).ToList();
        var wb = new XLWorkbook();
        ExcelExportHelper.AddDetailPerSoalSheet(wb, new List<AssessmentSession> { session }, new List<PackageUserResponse>(), questions);
        return wb.Worksheet("Detail Per Soal");
    }

    // Semua cell ber-label Section ("Section {n}: ..." atau "Lainnya"), urut kolom kiri->kanan.
    private static List<string> SectionBandLabelsInColumnOrder(IXLWorksheet ws)
        => ws.CellsUsed()
            .Select(c => new { text = c.GetString(), col = c.Address.ColumnNumber })
            .Where(x => Regex.IsMatch(x.text, @"^Section \d+:") || x.text == "Lainnya")
            .OrderBy(x => x.col)
            .Select(x => x.text)
            .ToList();

    // RED: band-header merged "Section {n}: {Nama}" belum dirender (helper masih flat OrderBy(Order)).
    [Fact]
    public async Task BandHeader_RendersSectionLabelRow()
    {
        int sessionId, packageId;
        await using (var seed = NewCtx())
            (sessionId, packageId) = await SeedPackageAsync(seed, new (int?, string?, int)[]
            {
                (2, "Sec2", 2), (1, "Sec1", 3), (null, null, 1)
            });

        await using var ctx = NewCtx();
        var ws = BuildSheet(ctx, sessionId, packageId);
        var labels = SectionBandLabelsInColumnOrder(ws);

        Assert.Contains("Section 1: Sec1", labels);
        Assert.Contains("Section 2: Sec2", labels);
    }

    // RED: urutan kolom soal harus per (SectionNumber, Order) -> band kiri->kanan = Section 1, Section 2, Lainnya.
    // Saat ini helper flat OrderBy(Order) -> Section 2 dulu (Order 1,2) -> band order salah / belum ada.
    [Fact]
    public async Task BandHeader_OrdersBySectionNumberThenOrder()
    {
        int sessionId, packageId;
        await using (var seed = NewCtx())
            (sessionId, packageId) = await SeedPackageAsync(seed, new (int?, string?, int)[]
            {
                (2, "Sec2", 2), (1, "Sec1", 3), (null, null, 1)
            });

        await using var ctx = NewCtx();
        var ws = BuildSheet(ctx, sessionId, packageId);
        var labels = SectionBandLabelsInColumnOrder(ws);

        Assert.Equal(new[] { "Section 1: Sec1", "Section 2: Sec2", "Lainnya" }, labels);
    }

    // GREEN sekarang DAN setelah Plan 02: assessment tanpa Section -> tak ada band (output legacy identik).
    [Fact]
    public async Task NoSection_BackwardCompat()
    {
        int sessionId, packageId;
        await using (var seed = NewCtx())
            (sessionId, packageId) = await SeedPackageAsync(seed, new (int?, string?, int)[]
            {
                (null, null, 3)
            });

        await using var ctx = NewCtx();
        var ws = BuildSheet(ctx, sessionId, packageId);

        Assert.Empty(SectionBandLabelsInColumnOrder(ws));
        Assert.Equal("No", ws.Cell(1, 1).GetString());
        Assert.Equal("Nama", ws.Cell(1, 2).GetString());
        Assert.Equal("NIP", ws.Cell(1, 3).GetString());
    }
}
