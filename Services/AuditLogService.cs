using HcPortal.Data;
using HcPortal.Models;

namespace HcPortal.Services
{
    /// <summary>
    /// Simple audit log writer. Injected into controllers that need audit logging.
    /// </summary>
    public class AuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Write one audit log row. Calls SaveChangesAsync internally.
        /// </summary>
        public async Task LogAsync(
            string actorUserId,
            string actorName,
            string actionType,
            string description,
            int? targetId = null,
            string? targetType = null)
        {
            var entry = new AuditLog
            {
                ActorUserId = actorUserId,
                ActorName = actorName,
                ActionType = actionType,
                Description = description,
                TargetId = targetId,
                TargetType = targetType,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(entry);
            await _context.SaveChangesAsync();
        }
    }
}
