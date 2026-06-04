// Phase 346 Plan 06 (REC-04): regression lock untuk CMPController.IsResultsAuthorized (Plan 03).
// Authz single-source: owner || roleLevel 1-3 (Admin/HC/L3) || (L4 section-scoped, Section non-null).
// Pure static (analog AssessmentHistoryStatsTests) — 1 matrix mewakili Results+Certificate+CertificatePdf
// (ketiga action memanggil helper yang sama; parameter roleLevel/section identik).
using HcPortal.Controllers;
using Xunit;

namespace HcPortal.Tests;

public class ResultsAuthorizationTests
{
    [Theory]
    // ownerUserId, currentUserId, roleLevel, currentSection, ownerSection, expected
    [InlineData("u1", "u1", 6, "A", "A", true)]    // owner (walau L6 coachee)
    [InlineData("u1", "x",  1, null, "A", true)]   // Admin (roleLevel<=3, section irrelevant)
    [InlineData("u1", "x",  2, null, "A", true)]   // HC
    [InlineData("u1", "x",  3, "X", "A", true)]    // L3 (Direktur/VP/Manager) full
    [InlineData("u1", "x",  4, "A", "A", true)]    // L4 same section
    [InlineData("u1", "x",  4, "B", "A", false)]   // L4 beda section -> Forbid
    [InlineData("u1", "x",  4, null, null, false)] // L4 Section null guard (T-346-01)
    [InlineData("u1", "x",  4, "", "", false)]     // L4 Section empty guard (T-346-01)
    [InlineData("u1", "x",  5, "A", "A", false)]   // L5 Coach non-owner (T-346-03)
    [InlineData("u1", "x",  6, "A", "A", false)]   // L6 Coachee non-owner (T-346-03)
    [InlineData("u1", "x",  0, "A", "A", false)]   // roleLevel 0 (no-role/error) ditolak (T-346-05)
    public void IsResultsAuthorized_Matrix(string owner, string cur, int lvl, string? curSec, string? ownSec, bool expected)
        => Assert.Equal(expected, CMPController.IsResultsAuthorized(owner, cur, lvl, curSec, ownSec));
}
