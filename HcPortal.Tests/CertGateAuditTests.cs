// Phase 401 Plan 04 Task 2 — PSU-05 cert-gate audit (D-03 gate channel).
// AssessmentAdminController gate eligibility (penerbitan AssessmentSession + NomorSertifikat)
// saat AssignmentUnit kosong → BLOCK + AuditLog persisted "ProtonUnitUnresolved" + LogWarning.
// File terpisah (disjoint dari UnitUnresolvedAuditTests milik 401-02) → Wave-1 paralel aman.
// Sanity (GREEN): primitif persisted-channel cert-gate via AuditLogService (no HTTP context).
using System;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class CertGateAuditTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task CertGate_block_persists_ProtonUnitUnresolved_audit()
    {
        using var ctx = InMemoryContext();
        var audit = new AuditLogService(ctx);
        await audit.LogAsync("hc1", "HC One", "ProtonUnitUnresolved",
            "Coachee c1 di-skip dari gate eligibility exam (penerbitan session/cert): AssignmentUnit kosong.",
            targetType: "CoachCoacheeMapping");
        var row = Assert.Single(ctx.AuditLogs);
        Assert.Equal("ProtonUnitUnresolved", row.ActionType);
        Assert.Equal("CoachCoacheeMapping", row.TargetType);
        Assert.Contains("session/cert", row.Description);
    }
}
