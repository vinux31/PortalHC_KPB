using HcPortal.Models.Competency;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Data;

public static class SeedCompetencyMappings
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Skip if already seeded
        if (await context.AssessmentCompetencyMaps.AnyAsync())
            return;

        // Load all KKJ matrix items to reference by Kompetensi name
        var kkjItems = await context.KkjMatrices.ToListAsync();

        if (!kkjItems.Any()) return; // No KKJ data to map to

        var mappings = new List<AssessmentCompetencyMap>();

        // Map assessment categories to relevant KKJ competency groups
        // Strategy: Use SkillGroup to broadly map categories
        // "Assessment OJ" -> Maps to OJ-related competencies (On-the-Job)
        // "IHT" -> Maps to In-House Training competencies
        // "Licencor" -> Maps to licensing/certification competencies
        // "OTS" -> Maps to Operator Training Simulator competencies
        // "Mandatory HSSE Training" -> Maps to HSSE competencies

        // Group KKJ items by SkillGroup for mapping
        var skillGroups = kkjItems.GroupBy(k => k.SkillGroup).ToDictionary(g => g.Key, g => g.ToList());

        // Assessment OJ -> Technical/operational competencies (Level 1-2)
        foreach (var item in kkjItems.Where(k =>
            k.SkillGroup.Contains("Teknis", StringComparison.OrdinalIgnoreCase) ||
            k.SkillGroup.Contains("Operasi", StringComparison.OrdinalIgnoreCase) ||
            k.SkillGroup.Contains("Operation", StringComparison.OrdinalIgnoreCase)))
        {
            mappings.Add(new AssessmentCompetencyMap
            {
                KkjMatrixItemId = item.Id,
                AssessmentCategory = "Assessment OJ",
                TitlePattern = null, // Broad mapping
                LevelGranted = 2,
                MinimumScoreRequired = null // Use assessment PassPercentage
            });
        }

        // IHT -> Knowledge-based competencies (Level 1)
        foreach (var item in kkjItems.Take(10)) // Map to first 10 core competencies
        {
            mappings.Add(new AssessmentCompetencyMap
            {
                KkjMatrixItemId = item.Id,
                AssessmentCategory = "IHT",
                TitlePattern = null,
                LevelGranted = 1,
                MinimumScoreRequired = null
            });
        }

        // Licencor -> Higher-level certifications (Level 3)
        foreach (var item in kkjItems.Where(k =>
            k.SkillGroup.Contains("Teknis", StringComparison.OrdinalIgnoreCase) ||
            k.SkillGroup.Contains("Operasi", StringComparison.OrdinalIgnoreCase) ||
            k.SkillGroup.Contains("Operation", StringComparison.OrdinalIgnoreCase)))
        {
            mappings.Add(new AssessmentCompetencyMap
            {
                KkjMatrixItemId = item.Id,
                AssessmentCategory = "Licencor",
                TitlePattern = null,
                LevelGranted = 3,
                MinimumScoreRequired = 80 // Higher bar for licensing
            });
        }

        // HSSE Training -> Safety/HSE competencies (Level 1)
        foreach (var item in kkjItems.Where(k =>
            k.Kompetensi.Contains("HSE", StringComparison.OrdinalIgnoreCase) ||
            k.Kompetensi.Contains("Safety", StringComparison.OrdinalIgnoreCase) ||
            k.Kompetensi.Contains("Keselamatan", StringComparison.OrdinalIgnoreCase) ||
            k.SkillGroup.Contains("HSE", StringComparison.OrdinalIgnoreCase)))
        {
            mappings.Add(new AssessmentCompetencyMap
            {
                KkjMatrixItemId = item.Id,
                AssessmentCategory = "Mandatory HSSE Training",
                TitlePattern = null,
                LevelGranted = 1,
                MinimumScoreRequired = null
            });
        }

        if (mappings.Any())
        {
            await context.AssessmentCompetencyMaps.AddRangeAsync(mappings);
            await context.SaveChangesAsync();
        }
    }
}
