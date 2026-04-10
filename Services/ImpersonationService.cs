namespace HcPortal.Services
{
    /// <summary>
    /// Session key constants for impersonation state.
    /// </summary>
    public static class ImpersonationKeys
    {
        public const string Mode = "Impersonate_Mode";
        public const string TargetRole = "Impersonate_TargetRole";
        public const string TargetUserId = "Impersonate_TargetUserId";
        public const string TargetUserName = "Impersonate_TargetUserName";
        public const string StartedAt = "Impersonate_StartedAt";
    }

    /// <summary>
    /// Manages impersonation session state. Admin can impersonate a role or a specific user.
    /// </summary>
    public class ImpersonationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const int ExpirationMinutes = 30;

        public ImpersonationService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public bool IsImpersonating()
        {
            return Session.GetString(ImpersonationKeys.Mode) != null;
        }

        public void StartRole(string role)
        {
            Session.SetString(ImpersonationKeys.Mode, "role");
            Session.SetString(ImpersonationKeys.TargetRole, role);
            Session.Remove(ImpersonationKeys.TargetUserId);
            Session.Remove(ImpersonationKeys.TargetUserName);
            Session.SetString(ImpersonationKeys.StartedAt, DateTime.UtcNow.Ticks.ToString());
        }

        public void StartUser(string userId, string userName)
        {
            Session.SetString(ImpersonationKeys.Mode, "user");
            Session.SetString(ImpersonationKeys.TargetUserId, userId);
            Session.SetString(ImpersonationKeys.TargetUserName, userName);
            Session.Remove(ImpersonationKeys.TargetRole);
            Session.SetString(ImpersonationKeys.StartedAt, DateTime.UtcNow.Ticks.ToString());
        }

        public void Stop()
        {
            Session.Remove(ImpersonationKeys.Mode);
            Session.Remove(ImpersonationKeys.TargetRole);
            Session.Remove(ImpersonationKeys.TargetUserId);
            Session.Remove(ImpersonationKeys.TargetUserName);
            Session.Remove(ImpersonationKeys.StartedAt);
        }

        public bool IsExpired()
        {
            var startedAtStr = Session.GetString(ImpersonationKeys.StartedAt);
            if (string.IsNullOrEmpty(startedAtStr) || !long.TryParse(startedAtStr, out var ticks))
                return true;

            var startedAt = new DateTime(ticks, DateTimeKind.Utc);
            return (DateTime.UtcNow - startedAt).TotalMinutes > ExpirationMinutes;
        }

        public string? GetDisplayName()
        {
            var mode = GetMode();
            if (mode == "user")
                return Session.GetString(ImpersonationKeys.TargetUserName);
            if (mode == "role")
                return GetTargetRole();
            return null;
        }

        public string? GetMode()
        {
            return Session.GetString(ImpersonationKeys.Mode);
        }

        public string? GetTargetRole()
        {
            return Session.GetString(ImpersonationKeys.TargetRole);
        }

        public string? GetTargetUserId()
        {
            return Session.GetString(ImpersonationKeys.TargetUserId);
        }

        /// <summary>
        /// Returns the effective role level during impersonation, or null if not impersonating.
        /// </summary>
        public int? GetEffectiveRoleLevel()
        {
            if (!IsImpersonating() || IsExpired()) return null;

            var mode = GetMode();
            if (mode == "role")
            {
                var role = GetTargetRole();
                return role != null ? HcPortal.Models.UserRoles.GetRoleLevel(role) : null;
            }
            // mode == "user": role level is stored in HttpContext.Items by middleware
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx?.Items["ImpersonateTargetRoleLevel"] is int level)
                return level;
            // fallback: resolve from target role name
            var targetRole = ctx?.Items["ImpersonateTargetRole"]?.ToString();
            return targetRole != null ? HcPortal.Models.UserRoles.GetRoleLevel(targetRole) : null;
        }

        /// <summary>
        /// Returns the effective SelectedView during impersonation, or null if not impersonating.
        /// </summary>
        public string? GetEffectiveSelectedView()
        {
            if (!IsImpersonating() || IsExpired()) return null;

            var mode = GetMode();
            if (mode == "role")
            {
                var role = GetTargetRole();
                return role != null ? HcPortal.Models.UserRoles.GetDefaultView(role) : null;
            }
            // mode == "user": resolve from context items
            var ctx = _httpContextAccessor.HttpContext;
            return ctx?.Items["ImpersonateTargetSelectedView"]?.ToString();
        }
    }
}
