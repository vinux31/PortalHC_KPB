// Phase 382 WSE-09 (TMR-01) — timer Standard kini DI-ENFORCE.
//
// KONVENSI TEST (repo): CMPController ber-ctor 14-dependency → konstruksi controller infeasible
// (lihat VerifyTokenTests.cs:3 "Controller construction is infeasible (14-dep ctor)"). Maka logika
// keputusan timer di-uji lewat PURE STATIC HELPER `CMPController.ShouldEnforceSubmitTimer` +
// `CMPController.EvaluateSubmitTimerDecision` (pola Phase 380 AccessTokenMatches / Phase 363 shared-core).
// Helper = sumber kebenaran tunggal; EnsureCanSubmitExamAsync mendelegasikan ke helper (anti-drift).
//
// RED sebelum Task 4: stub `ShouldEnforceSubmitTimer` mengembalikan perilaku BUG saat ini (allowlist —
// "Standard" tidak di-enforce → return false). Test A (Standard-late) HARUS fail. Setelah inversi → GREEN.
using System;
using HcPortal.Controllers;
using Xunit;

namespace HcPortal.Tests;

public class EnsureCanSubmitStandardTests
{
    // ---- ShouldEnforceSubmitTimer: TMR-01 allowlist→blocklist inversion ----

    [Fact] // RED→GREEN: "Standard" (AssessmentType Normal) HARUS di-enforce (bug: allowlist skip Standard)
    public void EnsureCanSubmitStandard_StandardType_IsEnforced()
    {
        Assert.True(CMPController.ShouldEnforceSubmitTimer("Standard"));
    }

    [Fact] // Online/PreTest/PostTest tetap di-enforce (tidak regresi)
    public void EnsureCanSubmitStandard_OnlinePrePost_AreEnforced()
    {
        Assert.True(CMPController.ShouldEnforceSubmitTimer("Online"));
        Assert.True(CMPController.ShouldEnforceSubmitTimer("PreTest"));
        Assert.True(CMPController.ShouldEnforceSubmitTimer("PostTest"));
    }

    [Fact] // Manual / null / kosong → tetap SKIP guard (blocklist)
    public void EnsureCanSubmitStandard_ManualOrNull_AreSkipped()
    {
        Assert.False(CMPController.ShouldEnforceSubmitTimer("Manual"));
        Assert.False(CMPController.ShouldEnforceSubmitTimer(null));
        Assert.False(CMPController.ShouldEnforceSubmitTimer(""));
    }

    // ---- EvaluateSubmitTimerDecision: tier-1 / tier-2 / pass (server-token authoritative) ----

    [Fact] // Test A: Standard elapsed>allowed TANPA token sah → Tier-1 reject (BlockNoGrace)
    public void EvaluateSubmitTimer_StandardLate_NoToken_RejectsTier1()
    {
        // elapsed 700s, allowed 600s (10 menit), tanpa server-approved token
        var d = CMPController.EvaluateSubmitTimerDecision(
            elapsedSec: 700, allowedSec: 600, graceSec: 720, serverApprovedAutoSubmit: false);
        Assert.Equal(CMPController.SubmitTimerDecision.BlockNoGrace, d);
    }

    [Fact] // Test B: Standard on-time (elapsed<allowed) → Pass (lanjut grading)
    public void EvaluateSubmitTimer_StandardOnTime_Passes()
    {
        var d = CMPController.EvaluateSubmitTimerDecision(
            elapsedSec: 300, allowedSec: 600, graceSec: 720, serverApprovedAutoSubmit: false);
        Assert.Equal(CMPController.SubmitTimerDecision.Pass, d);
    }

    [Fact] // D-05: on-time auto-submit sah (server-approved token) walau elapsed>allowed → tetap Pass ke grading
    public void EvaluateSubmitTimer_LateButServerApproved_Passes()
    {
        var d = CMPController.EvaluateSubmitTimerDecision(
            elapsedSec: 610, allowedSec: 600, graceSec: 720, serverApprovedAutoSubmit: true);
        Assert.Equal(CMPController.SubmitTimerDecision.Pass, d);
    }

    [Fact] // Tier-2: elapsed melewati grace → BlockGrace (no audit)
    public void EvaluateSubmitTimer_BeyondGrace_RejectsTier2()
    {
        var d = CMPController.EvaluateSubmitTimerDecision(
            elapsedSec: 800, allowedSec: 600, graceSec: 720, serverApprovedAutoSubmit: false);
        Assert.Equal(CMPController.SubmitTimerDecision.BlockGrace, d);
    }
}
