namespace HcPortal.Models
{
    /// <summary>
    /// Static class untuk mapping struktur organisasi: Bagian → Units
    /// Berdasarkan Plant di Fungsi CSU Process - ISBL
    /// </summary>
    public static class OrganizationStructure
    {
        /// <summary>
        /// Dictionary mapping Bagian ke list Units
        /// </summary>
        public static readonly Dictionary<string, List<string>> SectionUnits = new()
        {
            {
                "RFCC", new List<string>
                {
                    "RFCC LPG Treating Unit (062)",
                    "Propylene Recovery Unit (063)"
                }
            },
            {
                "DHT / HMU", new List<string>
                {
                    "Diesel Hydrotreating Unit I & II (054 & 083)",
                    "Hydrogen Manufacturing Unit (068)",
                    "Common DHT H2 Compressor (085)"
                }
            },
            {
                "NGP", new List<string>
                {
                    "Saturated Gas Concentration Unit (060)",
                    "Saturated LPG Treating Unit (064)",
                    "Isomerization Unit (082)",
                    "Common Facilities For NLP (160)",
                    "Naphtha Hydrotreating Unit II (084)"
                }
            },
            {
                "GAST", new List<string>
                {
                    "RFCC NHT (053)",
                    "Alkylation Unit (065)",
                    "Wet Gas Sulfuric Acid Unit (066)",
                    "SWS RFCC & Non RFCC (067 & 167)",
                    "Amine Regeneration Unit I & II (069 & 079)",
                    "Flare System (319)",
                    "Sulfur Recovery Unit (169)"
                }
            }
        };

        /// <summary>
        /// Get all available Bagian (Sections)
        /// </summary>
        public static List<string> GetAllSections()
        {
            return SectionUnits.Keys.ToList();
        }

        /// <summary>
        /// Get all Units for a specific Bagian
        /// </summary>
        public static List<string> GetUnitsForSection(string section)
        {
            if (SectionUnits.TryGetValue(section, out var units))
            {
                return units;
            }
            return new List<string>();
        }

        /// <summary>
        /// Get Bagian (Section) for a specific Unit
        /// </summary>
        public static string? GetSectionForUnit(string unit)
        {
            foreach (var kvp in SectionUnits)
            {
                if (kvp.Value.Contains(unit))
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// Get total count of all units
        /// </summary>
        public static int GetTotalUnitsCount()
        {
            return SectionUnits.Values.Sum(units => units.Count);
        }
    }
}
