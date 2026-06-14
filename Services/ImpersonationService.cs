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
    /// Keputusan identitas efektif saat impersonasi (Phase 377 — D-03/D-04/SC2/SC4).
    /// </summary>
    public enum EffectiveUserDecision { UseRealUser, RoleModeEmpty, TargetUser }

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

        /// <summary>
        /// Pure decision (Phase 377 D-05): terjemahkan state impersonasi → keputusan identitas efektif.
        /// Fail-closed: mode-role / target-null → RoleModeEmpty (BUKAN admin asli). Diuji ImpersonationIdentityTests.
        /// </summary>
        public static EffectiveUserDecision ResolveEffectiveUserDecision(
            bool isImpersonating, bool isExpired, string? mode, string? targetUserId)
        {
            if (!isImpersonating || isExpired) return EffectiveUserDecision.UseRealUser; // SC4 + V3 expiry
            if (mode == "role") return EffectiveUserDecision.RoleModeEmpty;              // D-03
            if (mode == "user")
                return string.IsNullOrEmpty(targetUserId)
                    ? EffectiveUserDecision.RoleModeEmpty                                 // D-04 trigger, fail-closed
                    : EffectiveUserDecision.TargetUser;                                  // SC2
            return EffectiveUserDecision.UseRealUser;                                     // mode tak dikenal → aman
        }

        /// <summary>
        /// Effective target user-id: X.Id saat TargetUser, null saat UseRealUser/RoleModeEmpty (D-05 single-source).
        /// </summary>
        public string? GetEffectiveTargetUserId()
        {
            var decision = ResolveEffectiveUserDecision(IsImpersonating(), IsExpired(), GetMode(), GetTargetUserId());
            return decision == EffectiveUserDecision.TargetUser ? GetTargetUserId() : null;
        }

        /// <summary>
        /// Resolve ApplicationUser efektif. UserManager = parameter (service tak inject UserManager).
        /// Caller branch by Decision: UseRealUser → resolve real principal (SC4); RoleModeEmpty → kosong+hint (D-03);
        /// TargetUser → user X (sudah di-resolve). target==null (D-04) di-handle middleware sebelum controller — fail-closed, jangan admin.
        /// </summary>
        public async System.Threading.Tasks.Task<(HcPortal.Models.ApplicationUser? User, EffectiveUserDecision Decision)> GetEffectiveUserAsync(
            Microsoft.AspNetCore.Identity.UserManager<HcPortal.Models.ApplicationUser> userManager)
        {
            var decision = ResolveEffectiveUserDecision(IsImpersonating(), IsExpired(), GetMode(), GetTargetUserId());
            if (decision != EffectiveUserDecision.TargetUser) return (null, decision);
            var target = await userManager.FindByIdAsync(GetTargetUserId()!);
            return (target, EffectiveUserDecision.TargetUser);
        }
    }
}
