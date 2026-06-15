// Phase 382 WSE-10 (TOK-02) — gate StartedAt==null pada sesi token-required (SaveAnswer + SubmitExam).
//
// KONVENSI: gate keputusan = PURE STATIC HELPER `CMPController.ShouldGateMissingStart(isTokenRequired, startedAt)`.
// SaveAnswer (return Json) + SubmitExam (return Redirect) keduanya memanggil helper yang sama (anti-drift).
// StartedAt di-set HANYA setelah VerifyToken sukses (StartExam lobby) → StartedAt==null && IsTokenRequired
// = proxy "belum lewat lobby token" → reject.
//
// RED sebelum Task 5: stub helper mengembalikan FALSE selalu (gate belum ada) → Test A/B fail.
using System;
using HcPortal.Controllers;
using Xunit;

namespace HcPortal.Tests;

public class TokenGateTests
{
    [Fact] // Test A (SaveAnswer): token-required && StartedAt==null → HARUS di-gate (reject)
    public void TokenGate_TokenRequired_NotStarted_IsGated()
    {
        Assert.True(CMPController.ShouldGateMissingStart(isTokenRequired: true, startedAt: null));
    }

    [Fact] // Test B (SubmitExam): idem — gate sebelum grading
    public void TokenGate_TokenRequired_NotStarted_SubmitGated()
    {
        // sama dengan Test A (helper tunggal dipakai kedua handler) — eksplisit untuk dokumentasi intent
        Assert.True(CMPController.ShouldGateMissingStart(true, null));
    }

    [Fact] // Test C: token-required tapi SUDAH started (lewat lobby) → TIDAK di-gate
    public void TokenGate_TokenRequired_AlreadyStarted_NotGated()
    {
        Assert.False(CMPController.ShouldGateMissingStart(true, new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc)));
    }

    [Fact] // Test D: sesi non-token (IsTokenRequired=false) → tidak pernah ter-gate, walau StartedAt==null
    public void TokenGate_NonToken_NotStarted_NotGated()
    {
        Assert.False(CMPController.ShouldGateMissingStart(isTokenRequired: false, startedAt: null));
    }
}
