using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using HcPortal.Data;
using HcPortal.Models;

namespace HcPortal.Services
{
    /// <summary>
    /// In-memory cached org label service. Phase 340 milestone v21.0.
    /// Cache: no-TTL, manual invalidate on every mutation (D-02).
    /// Audit log: reuse AuditLogService (D-04).
    /// </summary>
    public class OrgLabelService : IOrgLabelService
    {
        private const string LabelsCacheKey = "OrgLabels:All";

        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly AuditLogService _auditLog;

        public OrgLabelService(
            ApplicationDbContext context,
            IMemoryCache cache,
            AuditLogService auditLog)
        {
            _context = context;
            _cache = cache;
            _auditLog = auditLog;
        }

        public string GetLabel(int level)
        {
            var dict = GetAll();
            return dict.TryGetValue(level, out var label) ? label : $"Level {level}";
        }

        public IReadOnlyDictionary<int, string> GetAll()
        {
            return _cache.GetOrCreate(LabelsCacheKey, entry =>
            {
                return (IReadOnlyDictionary<int, string>)_context.OrganizationLevelLabels
                    .AsNoTracking()
                    .OrderBy(l => l.Level)
                    .ToDictionary(l => l.Level, l => l.Label);
            })!;
        }

        public async Task UpdateAsync(int level, string label, string userId, string actorName)
        {
            var row = await _context.OrganizationLevelLabels.FindAsync(level);
            if (row == null)
                throw new InvalidOperationException($"Level {level} not configured");

            var oldLabel = row.Label;
            row.Label = label;
            row.UpdatedAt = DateTime.UtcNow;
            row.UpdatedBy = userId;
            await _context.SaveChangesAsync();

            _cache.Remove(LabelsCacheKey);

            await _auditLog.LogAsync(
                actorUserId: userId,
                actorName: actorName,
                actionType: "OrgLabel-Update",
                description: $"Level {level}: '{oldLabel}' → '{label}'",
                targetId: level,
                targetType: "OrganizationLevelLabel"
            );
        }

        public async Task AddAsync(int level, string label, string userId, string actorName)
        {
            if (await _context.OrganizationLevelLabels.AnyAsync(l => l.Level == level))
                throw new InvalidOperationException($"Level {level} already exists");

            var row = new OrganizationLevelLabel
            {
                Level = level,
                Label = label,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            };
            _context.OrganizationLevelLabels.Add(row);
            await _context.SaveChangesAsync();

            _cache.Remove(LabelsCacheKey);

            await _auditLog.LogAsync(
                actorUserId: userId,
                actorName: actorName,
                actionType: "OrgLabel-Add",
                description: $"Level {level}: '{label}' created",
                targetId: level,
                targetType: "OrganizationLevelLabel"
            );
        }

        public async Task DeleteAsync(int level, string userId, string actorName)
        {
            var row = await _context.OrganizationLevelLabels.FindAsync(level);
            if (row == null)
                throw new InvalidOperationException($"Level {level} not configured");

            var oldLabel = row.Label;
            _context.OrganizationLevelLabels.Remove(row);
            await _context.SaveChangesAsync();

            _cache.Remove(LabelsCacheKey);

            await _auditLog.LogAsync(
                actorUserId: userId,
                actorName: actorName,
                actionType: "OrgLabel-Delete",
                description: $"Level {level}: '{oldLabel}' deleted",
                targetId: level,
                targetType: "OrganizationLevelLabel"
            );
        }

        public int GetMaxConfiguredLevel()
        {
            var dict = GetAll();
            return dict.Count == 0 ? 0 : dict.Keys.Max();
        }

        public async Task<int> GetMaxUsedLevelAsync()
        {
            if (!await _context.OrganizationUnits.AnyAsync())
                return 0;
            return await _context.OrganizationUnits.MaxAsync(u => u.Level);
        }
    }
}
