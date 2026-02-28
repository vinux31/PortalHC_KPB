namespace HcPortal.Services
{
    /// <summary>
    /// Result DTO returned by IAuthService.AuthenticateAsync.
    /// Used by Phase 72 AccountController to complete login flow.
    /// </summary>
    public class AuthResult
    {
        /// <summary>True if authentication succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>
        /// ApplicationUser.Id — set on success only.
        /// Used by Phase 72 AccountController for SignInAsync after AD auth.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// User email — from DB or synced from AD.
        /// Used by Phase 72 for FullName sync after AD auth.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Full name — from ApplicationUser.FullName or from AD displayName attribute.
        /// Phase 72 will sync this back to DB for AD users.
        /// </summary>
        public string? FullName { get; set; }

        /// <summary>
        /// User-safe error message. No technical details exposed.
        /// Display directly to login page ModelState.
        /// Null when Success=true.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
