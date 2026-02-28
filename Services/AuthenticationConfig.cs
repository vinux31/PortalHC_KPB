namespace HcPortal.Services
{
    /// <summary>
    /// Configuration POCO for authentication providers.
    /// Binds from appsettings.json "Authentication" section.
    /// Supports environment variable overrides (e.g., Authentication:UseActiveDirectory=true on prod server).
    /// </summary>
    public class AuthenticationConfig
    {
        /// <summary>
        /// Toggle: false = LocalAuthService (dev), true = LdapAuthService (prod).
        /// Override via environment variable: Authentication__UseActiveDirectory=true
        /// </summary>
        public bool UseActiveDirectory { get; set; } = false;

        /// <summary>
        /// LDAP connection path. Locked to Pertamina KPB OU.
        /// </summary>
        public string LdapPath { get; set; } = "LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com";

        /// <summary>
        /// LDAP connection timeout in milliseconds. 5000ms prevents hung login page.
        /// </summary>
        public int LdapTimeout { get; set; } = 5000;

        /// <summary>
        /// AD attribute mapping — configurable without code changes to accommodate IT variations.
        /// </summary>
        public LdapAttributeMapping AttributeMapping { get; set; } = new();
    }

    /// <summary>
    /// Maps ApplicationUser fields to AD LDAP attribute names.
    /// Defaults reflect standard Pertamina AD schema.
    /// </summary>
    public class LdapAttributeMapping
    {
        /// <summary>LDAP attribute for email. Default: "mail"</summary>
        public string Email { get; set; } = "mail";

        /// <summary>LDAP attribute for full name. Default: "displayName"</summary>
        public string FullName { get; set; } = "displayName";

        /// <summary>
        /// LDAP attribute for NIP/employee ID.
        /// Default: "employeeID" — verify with IT (may be "employeeNumber").
        /// NIP is NOT synced from AD in current scope (Phase 72 syncs FullName + Email only).
        /// </summary>
        public string NIP { get; set; } = "employeeID";
    }
}
