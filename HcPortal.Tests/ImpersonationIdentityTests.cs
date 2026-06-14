// Phase 377 Plan 01 (IMP-02) — Wave-0 RED.
// Pure-logic kontrak resolver-decision impersonasi. Implementasi ResolveEffectiveUserDecision +
// enum EffectiveUserDecision di Plan 02 (Services/ImpersonationService.cs). Test ini RED
// (gagal kompilasi CS0117/CS0103 — symbol belum ada) sampai Plan 02 merged → lalu GREEN.
//
// Pola pure static [Theory]+[InlineData] (analog ResultsAuthorizationTests) — NO Moq, NO HTTP context.
// Mengunci fail-closed: (impersonate, user, target=null) → RoleModeEmpty, BUKAN UseRealUser (T-377-03).
using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests;

public class ImpersonationIdentityTests
{
    [Theory]
    // isImpersonating, isExpired, mode, targetUserId, expected
    [InlineData(false, false, "user", "X", EffectiveUserDecision.UseRealUser)]   // SC4: non-impersonate identik
    [InlineData(false, true,  "role", "X", EffectiveUserDecision.UseRealUser)]   // isImpersonating=false short-circuit dominan (variant)
    [InlineData(true,  true,  "user", "X", EffectiveUserDecision.UseRealUser)]   // expired = treat as not impersonating (V3 session)
    [InlineData(true,  false, "role", null, EffectiveUserDecision.RoleModeEmpty)] // D-03: mode-role → kosong+hint
    [InlineData(true,  false, "user", null, EffectiveUserDecision.RoleModeEmpty)] // D-04 trigger: fail-closed, JANGAN admin
    [InlineData(true,  false, "user", "",   EffectiveUserDecision.RoleModeEmpty)] // target kosong di-treat seperti null (string.IsNullOrEmpty)
    [InlineData(true,  false, "user", "X",  EffectiveUserDecision.TargetUser)]    // SC2: pakai TargetUserId
    public void ResolveEffectiveUserDecision_Matrix(
        bool isImpersonating, bool isExpired, string? mode, string? targetUserId, EffectiveUserDecision expected)
    {
        Assert.Equal(
            expected,
            ImpersonationService.ResolveEffectiveUserDecision(isImpersonating, isExpired, mode, targetUserId));
    }
}
