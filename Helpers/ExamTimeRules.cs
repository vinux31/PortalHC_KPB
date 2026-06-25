namespace HcPortal.Helpers;

/// <summary>
/// Phase 424 GRDF-05 — durasi ujian aktif (detik) = (durationMinutes + extraTimeMinutes) * 60.
/// Pure, EF-free, sinkron, unit-testable. Single source of truth untuk clamp ElapsedSeconds:
/// menyatukan situs yang sudah benar memasukkan ExtraTimeMinutes (CMPController.cs :1175/:1548/:1626/:4596)
/// dan MEMPERBAIKI situs yang bolong (CMPController.cs:469 over-clamp tanpa ExtraTime → FLOW-02 root,
/// penyebab under-report export "Durasi Aktual"). extraTimeMinutes null diperlakukan 0.
/// </summary>
public static class ExamTimeRules
{
    public static int AllowedExamSeconds(int durationMinutes, int? extraTimeMinutes)
        => (durationMinutes + (extraTimeMinutes ?? 0)) * 60;
}
