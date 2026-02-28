namespace HcPortal.Models
{
    /// <summary>
    /// Role constants untuk RBAC system
    /// </summary>
    public static class UserRoles
    {
        // Level 1
        public const string Admin = "Admin";
        
        // Level 2
        public const string HC = "HC";
        
        // Level 3 - Management
        public const string Direktur = "Direktur";
        public const string VP = "VP";
        public const string Manager = "Manager";
        
        // Level 4 - Supervisory
        public const string SectionHead = "Section Head";
        public const string SrSupervisor = "Sr Supervisor";
        
        // Level 5 - Coaching
        public const string Coach = "Coach";
        
        // Level 6 - Operational
        public const string Coachee = "Coachee";

        /// <summary>
        /// Get all roles as list
        /// </summary>
        public static List<string> AllRoles => new List<string>
        {
            Admin, HC, Direktur, VP, Manager, 
            SectionHead, SrSupervisor, Coach, Coachee
        };

        /// <summary>
        /// Get role level from role name
        /// </summary>
        public static int GetRoleLevel(string roleName)
        {
            return roleName switch
            {
                Admin => 1,
                HC => 2,
                Direktur or VP or Manager => 3,
                SectionHead or SrSupervisor => 4,
                Coach => 5,
                Coachee => 6,
                _ => 6 // Default to lowest level
            };
        }

        /// <summary>
        /// Check if role has full access to all sections
        /// </summary>
        public static bool HasFullAccess(int level) => level <= 3;

        /// <summary>
        /// Check if role has section-level access
        /// </summary>
        public static bool HasSectionAccess(int level) => level == 4;

        /// <summary>
        /// Check if role is coaching-related
        /// </summary>
        public static bool IsCoachingRole(int level) => level >= 5;

        /// <summary>
        /// Get the default SelectedView for a given role name.
        /// Mapping: Admin→"Admin", HC→"HC", Coach→"Coach", management roles→"Atasan", default→"Coachee"
        /// </summary>
        public static string GetDefaultView(string roleName)
        {
            return roleName switch
            {
                Admin => "Admin",
                HC => "HC",
                Coach => "Coach",
                Direktur or VP or Manager or SectionHead or SrSupervisor => "Atasan",
                _ => "Coachee"
            };
        }
    }
}
