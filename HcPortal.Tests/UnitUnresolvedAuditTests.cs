// Phase 401 Plan 01 Task 3 — PSU-05 audit channel hybrid (D-03).
// Kontrak D-03 dua kanal saat AssignmentUnit tak teresolusi:
//   - GATE (GetEligibleCoachees 401-02, AssessmentAdmin cert-gate 401-04): BLOCK + tulis
//     AuditLog "ProtonUnitUnresolved" (persisted) + ILogger Warning.
//   - READ-PATH (AutoCreateProgressForAssignment 401-02, CDP defensive 401-05): skip +
//     ILogger Warning SAJA (NO AuditLog — hindari flooding AuditLogs).
// Assertion yg butuh wiring di-Skip (nyebut downstream plan). Sanity (GREEN) buktikan
// primitif persisted-channel + label ActionType benar via AuditLogService.
using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HcPortal.Tests;

public class UnitUnresolvedAuditTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // Sanity (GREEN): primitif kanal persisted — LogAsync("ProtonUnitUnresolved") tulis 1 baris
    // AuditLog dgn ActionType benar. Ini fondasi gate-block channel (401-02/04).
    [Fact]
    public async Task AuditLog_ProtonUnitUnresolved_persists_one_row_with_correct_action()
    {
        await using var ctx = InMemoryContext();
        var before = ctx.AuditLogs.Count();

        await new AuditLogService(ctx).LogAsync(
            "actor1", "Actor One", "ProtonUnitUnresolved",
            "AssignmentUnit kosong — coachee diblok dari eligibility", targetType: "CoachCoacheeMapping");

        Assert.Equal(before + 1, ctx.AuditLogs.Count());
        Assert.Equal("ProtonUnitUnresolved", ctx.AuditLogs.Single().ActionType);
    }

    // Sanity (GREEN): CapturingLogger test-double menangkap Warning — primitif kanal read-path
    // (ILogger-only). Membuktikan assert `Entries.Any(Level==Warning)` bisa dipakai 401-05.
    [Fact]
    public void CapturingLogger_records_warning_entries_for_readpath_channel()
    {
        var logger = new CapturingLogger<UnitUnresolvedAuditTests>();
        logger.LogWarning("AssignmentUnit kosong — skip resolve (read-path)");

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    // TODO 401-02 (gate) / 401-05 (read-path): wire kedua kanal D-03.
    [Fact(Skip = "RED until 401-02 (gate AuditLog) / 401-05 (read-path ILogger) wire channels")]
    public Task GateBlock_writes_AuditLog_persisted_plus_warning_ReadPath_warning_only()
    {
        // GATE: empty AssignmentUnit di GetEligibleCoachees/AssessmentAdmin → AuditLogs delta +1
        //   (ActionType "ProtonUnitUnresolved") + CapturingLogger Warning.
        // READ-PATH: empty AssignmentUnit di AutoCreateProgressForAssignment/CDP → Warning saja,
        //   AuditLogs delta 0. Asserted in 401-02 / 401-05.
        return Task.CompletedTask;
    }
}
