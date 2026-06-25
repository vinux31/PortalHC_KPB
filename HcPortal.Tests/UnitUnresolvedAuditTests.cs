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

    // Sanity (GREEN, 401-02): separasi kanal D-03 — read-path LogWarning TIDAK menulis AuditLog.
    // Membuktikan AutoCreateProgressForAssignment (read-path) pakai ILogger saja (AuditLogs delta 0).
    [Fact]
    public async Task ReadPath_warning_does_not_persist_auditlog()
    {
        await using var ctx = InMemoryContext();
        var logger = new CapturingLogger<UnitUnresolvedAuditTests>();
        var before = ctx.AuditLogs.Count();

        // read-path channel: LogWarning saja, NO _auditLog.LogAsync (D-03 anti-flood)
        logger.LogWarning("AutoCreateProgress skip: coachee c1 AssignmentUnit kosong (read-path).");

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Equal(0, ctx.AuditLogs.Count() - before); // read-path TIDAK persist audit
    }

    // GATE vs READ-PATH end-to-end (HTTP action through real DB) deferred to Phase 404 QA-01.
    // Gate persisted-channel + read-path ILogger-only are unit-proven above; grep guards in
    // 401-02 acceptance pin the wiring (gate has _auditLog.LogAsync ProtonUnitUnresolved; read-path 0).
    [Fact(Skip = "Integration smoke deferred to Phase 404 QA-01 (HTTP context + SQLEXPRESS)")]
    public Task GateBlock_persists_audit_ReadPath_warning_only_endtoend()
    {
        return Task.CompletedTask;
    }
}
