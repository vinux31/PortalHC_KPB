namespace HcPortal.Data;

/// <summary>
/// Seed file for AssessmentCompetencyMap data.
/// Note: Removed in Phase 90 — KkjMatrices table dropped and KkjMatrixItemId references obsoleted.
/// Seeding competency maps that reference KkjMatrixItemId is no longer meaningful.
/// This class is retained as a stub to avoid breaking any startup calls.
/// </summary>
public static class SeedCompetencyMappings
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Phase 90: KkjMatrices table dropped — competency mapping seed is now a no-op.
        // AssessmentCompetencyMap.KkjMatrixItemId is an orphaned int column (FK constraint removed).
        // No seed data is inserted.
        await Task.CompletedTask;
    }
}
