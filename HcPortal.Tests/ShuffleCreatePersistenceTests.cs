using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 372 SHUF-02 — Wave 0 stub. Implementasi diisi Plan 02 (372-02): membuktikan flag shuffle
/// dari form CreateAssessment POST tersimpan eksplisit di SEMUA write-site `new AssessmentSession`
/// (anti EF bool-false trap; default ON, OFF tersimpan false). Lihat 372-02-PLAN.md.
/// </summary>
[Trait("Category", "Integration")]
public class ShuffleCreatePersistenceTests
{
    [Fact(Skip = "Wave 0 stub — implementasi di Plan 02 (SHUF-02)")]
    public void Stub_ShuffleCreatePersistence() { }
}
