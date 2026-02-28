using HcPortal.Models;

namespace HcPortal.Services
{
    /// <summary>
    /// Hybrid authentication service: tries AD first for all users; for admin@pertamina.com
    /// falls back to local Identity auth if AD fails (timeout, server down, etc.).
    ///
    /// All other users: AD only — if AD is down they cannot login.
    /// Admin fallback is completely silent: no UX indication of which auth path was used.
    ///
    /// Phase 74: Enables dedicated Admin KPB user (local Identity) to work in production AD mode.
    /// </summary>
    public class HybridAuthService : IAuthService
    {
        private readonly LdapAuthService _adService;
        private readonly LocalAuthService _localService;
        private readonly ILogger<HybridAuthService> _logger;

        private const string AdminFallbackEmail = "admin@pertamina.com";

        public HybridAuthService(
            LdapAuthService adService,
            LocalAuthService localService,
            ILogger<HybridAuthService> logger)
        {
            _adService = adService;
            _localService = localService;
            _logger = logger;
        }

        public async Task<AuthResult> AuthenticateAsync(string email, string password)
        {
            bool isAdminFallback = email.Equals(AdminFallbackEmail, StringComparison.OrdinalIgnoreCase);

            if (isAdminFallback)
            {
                _logger.LogInformation("Hybrid auth: admin account, trying AD first then local fallback");

                var adResult = await _adService.AuthenticateAsync(email, password);
                if (adResult.Success)
                {
                    _logger.LogInformation("Hybrid auth: admin authenticated via AD");
                    return adResult;
                }

                // AD failed — try local auth silently (no UX indication of which path)
                _logger.LogInformation("Hybrid auth: AD failed for admin, trying local fallback");
                var localResult = await _localService.AuthenticateAsync(email, password);

                if (localResult.Success)
                    _logger.LogInformation("Hybrid auth: admin authenticated via local fallback");
                else
                    _logger.LogWarning("Hybrid auth: both AD and local failed for admin");

                return localResult;
            }

            // Non-admin: AD only — no fallback. If AD is down, return AD error directly.
            _logger.LogInformation("Hybrid auth: non-admin account, AD only");
            return await _adService.AuthenticateAsync(email, password);
        }
    }
}
