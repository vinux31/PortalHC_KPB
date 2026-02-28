using HcPortal.Models;
using Microsoft.AspNetCore.Identity;

namespace HcPortal.Services
{
    /// <summary>
    /// Local authentication service wrapping ASP.NET Core Identity SignInManager.
    /// Used when Authentication:UseActiveDirectory=false (development and local-only deployments).
    /// Registered via IAuthService DI in Program.cs (see Phase 71-72).
    /// </summary>
    public class LocalAuthService : IAuthService
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LocalAuthService> _logger;

        public LocalAuthService(SignInManager<ApplicationUser> signInManager, ILogger<LocalAuthService> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user by email + password using Identity PasswordSignInAsync.
        /// Finds user by email first, then validates password hash.
        /// Returns AuthResult with user details on success or generic error on failure.
        /// </summary>
        public async Task<AuthResult> AuthenticateAsync(string email, string password)
        {
            _logger.LogInformation("Local auth attempt for {Email}", email);

            // Find user by email
            var user = await _signInManager.UserManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Local auth failed: user not found for {Email}", email);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Username atau password salah"
                };
            }

            // Validate password via Identity (no lockout — intranet app behind firewall)
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("Local auth success for {Email}", email);
                return new AuthResult
                {
                    Success = true,
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName
                };
            }

            _logger.LogWarning("Local auth failed: wrong password for {Email}", email);
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Username atau password salah"
            };
        }
    }
}
