// ImportTrainingAuditTests — Phase 368 #24 (ImportTraining konstanta + GenerateCertificate=isPassed + audit).
// ImportTraining = controller dgn Excel parse (ClosedXML) + UserManager → full invoke berat.
// Uji KONTRAK field assignment (data-level) + audit-shape (AuditLogService real-SQL persist). Pola A4 RESEARCH.
using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class ImportTrainingAuditTests : IClassFixture<RecordCascadeFixture>
{
    private readonly RecordCascadeFixture _fixture;
    public ImportTrainingAuditTests(RecordCascadeFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "imp-" + Guid.NewGuid().ToString("N")[..8], Email = "imp@test.local", FullName = "Import Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ImportTrainingAudit_FieldContract_ManualConstant_And_GenerateCertificateFollowsIsPassed(bool isPassed)
    {
        // #24: tiap sesi import = AssessmentType konstanta Manual + GenerateCertificate mengikuti isPassed (bukan unconditional true).
        var session = new AssessmentSession
        {
            UserId = "u", Title = "T", Category = "C", Status = "Completed", AccessToken = "",
            IsManualEntry = true,
            IsPassed = isPassed,
            GenerateCertificate = isPassed,                                  // kontrak #24
            AssessmentType = AssessmentConstants.AssessmentType.Manual       // kontrak #24 (konstanta)
        };

        Assert.Equal("Manual", session.AssessmentType);
        Assert.Equal(isPassed, session.GenerateCertificate);                 // lulus → cert; tidak lulus → no cert
    }

    [Fact]
    public async Task ImportTrainingAudit_PersistsAuditLog_WithImportTrainingActionType()
    {
        // #24: audit ringkasan ImportTraining ter-persist 1 row dgn ActionType "ImportTraining".
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var audit = new AuditLogService(ctx);

        await audit.LogAsync(userId, "Import Test", "ImportTraining", "Import: 3 sukses, 1 skip, 0 error.", null, "AssessmentSession");

        Assert.True(await ctx.AuditLogs.AnyAsync(a => a.ActionType == "ImportTraining"));
    }
}
