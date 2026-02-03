namespace HcPortal.Models
{
    /// <summary>
    /// Role constants untuk RBAC system
    /// </summary>
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string HC = "HC";
        public const string SectionHead = "Section Head";
        public const string SrSupervisor = "Sr Supervisor";
        public const string Coach = "Coach";
        public const string Coachee = "Coachee";

        /// <summary>
        /// Get all roles as list
        /// </summary>
        public static List<string> AllRoles => new List<string>
        {
            Admin, HC, SectionHead, SrSupervisor, Coach, Coachee
        };
    }
}
