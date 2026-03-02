using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Helper to resolve target competency levels from KKJ matrix based on user position.
    /// Uses PositionColumnMapping table to map position → KkjColumn → KkjTargetValue.
    /// Replaces the old hardcoded reflection-based approach (Phase 89).
    /// </summary>
    public static class PositionTargetHelper
    {
        /// <summary>
        /// Gets the target competency level for a given KKJ matrix item and user position.
        /// Queries PositionColumnMapping + KkjTargetValue for the dynamic column approach.
        /// </summary>
        /// <param name="context">ApplicationDbContext for DB queries</param>
        /// <param name="kkjMatrixItemId">ID of the KKJ competency item</param>
        /// <param name="userPosition">User's position string (must match a PositionColumnMapping.Position)</param>
        /// <returns>Target level (0-5), or 0 if position not mapped or value is "-" or unparseable</returns>
        public static async Task<int> GetTargetLevelAsync(ApplicationDbContext context, int kkjMatrixItemId, string? userPosition)
        {
            if (string.IsNullOrWhiteSpace(userPosition))
                return 0;

            // Find the KkjColumn mapped to this position
            var mapping = await context.PositionColumnMappings
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Position == userPosition);

            if (mapping == null)
                return 0; // Position not mapped — admin needs to set up PositionColumnMapping

            // Find the target value for this competency + column cell
            var targetValue = await context.KkjTargetValues
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.KkjMatrixItemId == kkjMatrixItemId && v.KkjColumnId == mapping.KkjColumnId);

            if (targetValue == null || targetValue.Value == "-")
                return 0; // Not applicable or empty for this position

            // Parse the level (typically "1"-"5")
            if (int.TryParse(targetValue.Value, out var level) && level >= 0 && level <= 5)
                return level;

            return 0; // Unparseable value
        }

        /// <summary>
        /// Checks whether a user position has a PositionColumnMapping configured.
        /// Returns false if position is null/empty or unmapped.
        /// </summary>
        public static async Task<bool> IsPositionMapped(ApplicationDbContext context, string? userPosition)
        {
            if (string.IsNullOrWhiteSpace(userPosition))
                return false;

            return await context.PositionColumnMappings
                .AsNoTracking()
                .AnyAsync(m => m.Position == userPosition);
        }

        /// <summary>
        /// Gets all mapped position strings from the database.
        /// Useful for dropdowns and validation.
        /// </summary>
        public static async Task<List<string>> GetAllPositionsAsync(ApplicationDbContext context)
        {
            return await context.PositionColumnMappings
                .AsNoTracking()
                .Select(m => m.Position)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();
        }
    }
}
