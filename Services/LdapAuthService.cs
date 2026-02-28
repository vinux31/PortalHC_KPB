using System.DirectoryServices;
using HcPortal.Models;
using Microsoft.AspNetCore.Identity;

namespace HcPortal.Services
{
    /// <summary>
    /// LDAP authentication service for Pertamina Active Directory.
    /// Used in production when Authentication:UseActiveDirectory=true (global config toggle — no per-user flag).
    /// Connects to OU=KPB,OU=KPI,DC=pertamina,DC=com via System.DirectoryServices.
    ///
    /// Design decisions:
    /// - User credential bind: tests password directly without service account. No extra secret to manage.
    /// - Always fresh DirectoryEntry per request: never reuse connections (stale connection COMException after 5 min).
    /// - 5-second timeout via Task.WhenAny: prevents 20-40s ADSI default hang on unreachable server.
    /// - Generic error messages: COMException/technical details never reach the caller or UI.
    /// - samaccountname filter with LDAP escaping: prevents LDAP injection attacks.
    /// </summary>
    public class LdapAuthService : IAuthService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<LdapAuthService> _logger;

        public LdapAuthService(IConfiguration config, ILogger<LdapAuthService> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user via Pertamina LDAP.
        /// Flow: bind as user (validates password) -> search for user attributes.
        /// Returns AuthResult with FullName and Email from AD on success.
        /// Returns generic Indonesian error messages on any failure.
        /// </summary>
        public async Task<AuthResult> AuthenticateAsync(string email, string password)
        {
            _logger.LogInformation("LDAP auth attempt for {Email}", email);

            // Wrap synchronous DirectoryServices call in Task with timeout
            // System.DirectoryServices is COM-based (synchronous only)
            // Task.WhenAny enforces 5-second timeout instead of 20-40s ADSI default
            var authTask = Task.Run(() => AuthenticateViaLdap(email, password));
            var timeoutMs = _config.GetValue<int>("Authentication:LdapTimeout", 5000);
            var completedTask = await Task.WhenAny(authTask, Task.Delay(timeoutMs));

            if (completedTask != authTask)
            {
                _logger.LogError("LDAP auth timeout after {TimeoutMs}ms for {Email}", timeoutMs, email);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Tidak dapat menghubungi server autentikasi. Silakan coba lagi nanti."
                };
            }

            return await authTask;
        }

        private AuthResult AuthenticateViaLdap(string email, string password)
        {
            var ldapPath = _config.GetValue<string>("Authentication:LdapPath")
                           ?? "LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com";
            var emailAttr = _config.GetValue<string>("Authentication:AttributeMapping:Email") ?? "mail";
            var fullNameAttr = _config.GetValue<string>("Authentication:AttributeMapping:FullName") ?? "displayName";

            try
            {
                // Step 1: Bind as user with provided credentials to verify password
                // IMPORTANT: Always create fresh DirectoryEntry — never reuse across requests
                // Reuse causes "LDAP server unavailable" COMException after 5 minutes
                using (var bindEntry = new DirectoryEntry(ldapPath, email, password))
                {
                    bindEntry.AuthenticationType = AuthenticationTypes.Secure;

                    // Trigger actual LDAP bind — accessing NativeObject forces authentication
                    // If credentials are wrong, this throws COMException with HRESULT -2147023570
                    var nativeObject = bindEntry.NativeObject;
                }

                // Step 2: Credential bind succeeded — now search for user attributes
                // Search as anonymous (credentials already verified in Step 1)
                using (var searchEntry = new DirectoryEntry(ldapPath))
                using (var searcher = new DirectorySearcher(searchEntry))
                {
                    // Escape LDAP special chars to prevent injection: * ( ) \ / NUL
                    var escapedEmail = EscapeLdapFilterValue(email);
                    searcher.Filter = $"(samaccountname={escapedEmail})";
                    searcher.SearchScope = SearchScope.Subtree;
                    searcher.PropertiesToLoad.Add(emailAttr);
                    searcher.PropertiesToLoad.Add(fullNameAttr);

                    var result = searcher.FindOne();

                    if (result == null)
                    {
                        // Bind succeeded (password correct) but user not found in OU — edge case
                        _logger.LogWarning("LDAP auth: bind succeeded but samaccountname not found in OU for {Email}", email);
                        return new AuthResult
                        {
                            Success = false,
                            ErrorMessage = "Username atau password salah"
                        };
                    }

                    var ldapEmail = result.Properties[emailAttr]?.Count > 0
                        ? result.Properties[emailAttr][0]?.ToString() ?? email
                        : email;

                    var fullName = result.Properties[fullNameAttr]?.Count > 0
                        ? result.Properties[fullNameAttr][0]?.ToString() ?? string.Empty
                        : string.Empty;

                    _logger.LogInformation("LDAP auth success for {Email}, FullName={FullName}", email, fullName);

                    return new AuthResult
                    {
                        Success = true,
                        Email = ldapEmail,
                        FullName = fullName
                        // UserId: not available from LDAP — Phase 72 AccountController
                        // looks up ApplicationUser by email and sets UserId from DB
                    };
                }
            }
            catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == unchecked((int)0x8007052E))
            {
                // HRESULT 0x8007052E = ERROR_LOGON_FAILURE — wrong credentials
                _logger.LogWarning("LDAP auth failed: invalid credentials for {Email}", email);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Username atau password salah"
                };
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                // Other COMExceptions: server unreachable, domain not found, SSL errors, etc.
                _logger.LogError(ex, "LDAP connection error for {Email}, HRESULT={HResult}", email, ex.HResult);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Tidak dapat menghubungi server autentikasi. Silakan coba lagi nanti."
                };
            }
            catch (Exception ex)
            {
                // Catch-all: unexpected errors must never expose technical details
                _logger.LogError(ex, "Unexpected LDAP error for {Email}", email);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Tidak dapat menghubungi server autentikasi. Silakan coba lagi nanti."
                };
            }
        }

        /// <summary>
        /// Escape LDAP filter special characters to prevent LDAP injection attacks.
        /// Special chars: NUL, (, ), *, \, /, and characters with codes 0x01-0x1F.
        /// Per RFC 4515 LDAP filter specification.
        /// </summary>
        private static string EscapeLdapFilterValue(string value)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\5c"); break;
                    case '*':  sb.Append("\\2a"); break;
                    case '(':  sb.Append("\\28"); break;
                    case ')':  sb.Append("\\29"); break;
                    case '\0': sb.Append("\\00"); break;
                    case '/':  sb.Append("\\2f"); break;
                    default:
                        if (c < 0x20)
                            sb.Append($"\\{(int)c:x2}");
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
