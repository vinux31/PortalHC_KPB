namespace HcPortal.Services
{
    /// <summary>
    /// Org label service — read + cache label tier organisasi (Bagian/Unit/Sub-unit/...).
    /// Phase 340 — milestone v21.0 ORG-LABEL-02.
    /// </summary>
    public interface IOrgLabelService
    {
        /// <summary>Resolve label untuk level. Fallback "Level {N}" bila tidak ada (D-07).</summary>
        /// <param name="level">Level int (0-indexed root).</param>
        /// <returns>Label string atau fallback "Level {level}".</returns>
        string GetLabel(int level);

        /// <summary>Get all configured labels sebagai immutable dict (cached, no-TTL).</summary>
        /// <returns>Read-only dict { level => label }, sorted by level asc.</returns>
        IReadOnlyDictionary<int, string> GetAll();

        /// <summary>Update label existing level. Invalidate cache + audit log. Throws bila level tidak ada.</summary>
        /// <param name="level">Level int existing.</param>
        /// <param name="label">Label baru.</param>
        /// <param name="userId">ActorUserId (Identity ID).</param>
        /// <param name="actorName">Display name (NIP + FullName), resolved oleh controller.</param>
        Task UpdateAsync(int level, string label, string userId, string actorName);

        /// <summary>Add new level + label. Invalidate cache + audit log. Throws bila level sudah ada.</summary>
        Task AddAsync(int level, string label, string userId, string actorName);

        /// <summary>Delete level (highest only, precondition: tidak dipakai unit). Invalidate cache + audit log.</summary>
        Task DeleteAsync(int level, string userId, string actorName);

        /// <summary>MAX(Level) dari OrganizationLevelLabels cached. Return 0 bila tabel kosong.</summary>
        int GetMaxConfiguredLevel();

        /// <summary>MAX(Level) dari OrganizationUnits live query (no cache, D-08). Return 0 bila tabel kosong.</summary>
        Task<int> GetMaxUsedLevelAsync();
    }
}
