namespace HcPortal.Services
{
    /// <summary>
    /// Authentication service interface supporting Local (password hash) and AD (LDAP) implementations.
    /// Phase 72 login controller uses this interface — implementation is transparent.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticate user by email and password.
        /// Returns AuthResult with Success=true and user details on success,
        /// or Success=false with a user-safe ErrorMessage on failure.
        /// </summary>
        Task<AuthResult> AuthenticateAsync(string email, string password);
    }
}
