using HcPortal.Models;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Helper to resolve target competency levels from KKJ matrix based on user position.
    /// TODO(89-03): This helper is being refactored for the Phase 89 dynamic columns redesign.
    /// The hardcoded Target_* column approach is replaced by KkjTargetValue + PositionColumnMapping tables.
    /// </summary>
    public static class PositionTargetHelper
    {
        /// <summary>
        /// All valid position keys in the system (used for UI dropdowns, etc.)
        /// NOTE: These are the legacy position names — new system uses PositionColumnMapping table.
        /// </summary>
        private static readonly List<string> KnownPositions = new()
        {
            "Section Head",
            "Sr Supervisor GSH",
            "Shift Supervisor GSH",
            "Panelman GSH 12-13",
            "Panelman GSH 14",
            "Operator GSH 8-11",
            "Operator GSH 12-13",
            "Shift Supervisor ARU",
            "Panelman ARU 12-13",
            "Panelman ARU 14",
            "Operator ARU 8-11",
            "Operator ARU 12-13",
            "Sr Supervisor Facility",
            "Jr Analyst",
            "HSE Officer"
        };

        /// <summary>
        /// Gets the target competency level for a given KKJ competency and user position.
        /// TODO(89-03): Updated to use KkjTargetValue + PositionColumnMapping tables.
        /// This stub returns 0 until the dynamic columns refactor is complete.
        /// </summary>
        public static int GetTargetLevel(KkjMatrixItem competency, string? userPosition)
        {
            // TODO(89-03): Implement using PositionColumnMapping → KkjColumn → KkjTargetValue lookup
            return 0;
        }

        /// <summary>
        /// Gets the KkjMatrixItem column name for a given position.
        /// TODO(89-03): Deprecated — use PositionColumnMapping table instead.
        /// </summary>
        public static string? GetColumnName(string? position)
        {
            // TODO(89-03): Deprecated — positions now map to KkjColumn via PositionColumnMapping table
            return null;
        }

        /// <summary>
        /// Gets all valid position keys that are mapped in the system.
        /// </summary>
        public static List<string> GetAllPositions()
        {
            return KnownPositions.ToList();
        }
    }
}
