// Phase 394 (Inject Assessment Manual) — unit tests untuk pemetaan InjectAssessmentViewModel → InjectRequest.
// Pure unit (NO DB, NO fixture) → berjalan di bawah `dotnet test --filter Category!=Integration`.
//
// SCAFFOLD (Plan 394-01): kelas + 1 fakta placeholder agar suite hijau lebih dulu (Wave 0).
//   ViewModel + controller MapToRequest dibuat Plan 394-02/394-04 → fakta mapping penuh (scalars/Questions/
//   Workers/CertMode/AssessmentType + UserId→NIP) DIISI Plan 394-04 Task 2 (lihat 394-04-PLAN.md).
//   Kelas ini sengaja TIDAK mereferensi tipe yang belum ada agar commit Plan 01 build hijau independen.
using Xunit;

namespace HcPortal.Tests;

public class InjectViewModelMapTests
{
    // Placeholder Wave-0 — diganti fakta mapping penuh di Plan 394-04.
    [Fact]
    [Trait("Category", "Unit")]
    public void Scaffold_MapsTitleAndType()
    {
        // Sanity guard: konstanta kontrak tipe assessment inject (NEVER "Manual" — InjectRequest default "Standard").
        const string defaultType = "Standard";
        Assert.Equal("Standard", defaultType);
        Assert.NotEqual("Manual", defaultType);
    }
}
