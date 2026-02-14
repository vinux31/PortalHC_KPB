using HcPortal.Models;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Helper to resolve target competency levels from KKJ matrix based on user position.
    /// Maps position strings to KkjMatrixItem target level column names.
    /// </summary>
    public static class PositionTargetHelper
    {
        /// <summary>
        /// Maps user Position strings to KkjMatrixItem target level property names.
        /// All 15 positions from the KKJ matrix are mapped here.
        /// </summary>
        private static readonly Dictionary<string, string> PositionColumnMap = new()
        {
            { "Section Head", "Target_SectionHead" },
            { "Sr Supervisor GSH", "Target_SrSpv_GSH" },
            { "Shift Supervisor GSH", "Target_ShiftSpv_GSH" },
            { "Panelman GSH 12-13", "Target_Panelman_GSH_12_13" },
            { "Panelman GSH 14", "Target_Panelman_GSH_14" },
            { "Operator GSH 8-11", "Target_Operator_GSH_8_11" },
            { "Operator GSH 12-13", "Target_Operator_GSH_12_13" },
            { "Shift Supervisor ARU", "Target_ShiftSpv_ARU" },
            { "Panelman ARU 12-13", "Target_Panelman_ARU_12_13" },
            { "Panelman ARU 14", "Target_Panelman_ARU_14" },
            { "Operator ARU 8-11", "Target_Operator_ARU_8_11" },
            { "Operator ARU 12-13", "Target_Operator_ARU_12_13" },
            { "Sr Supervisor Facility", "Target_SrSpv_Facility" },
            { "Jr Analyst", "Target_JrAnalyst" },
            { "HSE Officer", "Target_HSE" }
        };

        /// <summary>
        /// Gets the target competency level for a given KKJ competency and user position.
        /// Uses reflection to access the appropriate target level column from the KkjMatrixItem.
        /// </summary>
        /// <param name="competency">The KKJ competency matrix item</param>
        /// <param name="userPosition">The user's position (must match one of the 15 defined positions)</param>
        /// <returns>Target level (0-5), or 0 if position is not mapped or value is "-"</returns>
        public static int GetTargetLevel(KkjMatrixItem competency, string? userPosition)
        {
            if (string.IsNullOrWhiteSpace(userPosition))
                return 0;

            if (!PositionColumnMap.TryGetValue(userPosition, out var columnName))
                return 0; // Position not in the map

            // Use reflection to get the property value
            var propertyInfo = typeof(KkjMatrixItem).GetProperty(columnName);
            if (propertyInfo == null)
                return 0; // Property doesn't exist

            var value = propertyInfo.GetValue(competency) as string;
            if (string.IsNullOrWhiteSpace(value) || value == "-")
                return 0; // Not applicable for this position

            // Parse the level value
            if (int.TryParse(value, out var level))
            {
                return level;
            }

            return 0; // Unparseable value
        }

        /// <summary>
        /// Gets the KkjMatrixItem column name for a given position.
        /// Useful for testing and validation.
        /// </summary>
        /// <param name="position">The user's position</param>
        /// <returns>Column name or null if position not mapped</returns>
        public static string? GetColumnName(string? position)
        {
            if (string.IsNullOrWhiteSpace(position))
                return null;

            return PositionColumnMap.TryGetValue(position, out var columnName) ? columnName : null;
        }

        /// <summary>
        /// Gets all valid position keys that are mapped in the system.
        /// </summary>
        /// <returns>List of all 15 position strings</returns>
        public static List<string> GetAllPositions()
        {
            return PositionColumnMap.Keys.ToList();
        }
    }
}
