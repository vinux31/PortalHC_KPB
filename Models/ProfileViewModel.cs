namespace HcPortal.Models
{
    /// <summary>
    /// ViewModel for the Profile page — read-only display of user information.
    /// </summary>
    public class ProfileViewModel
    {
        public string? FullName { get; set; }
        public string? NIP { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Position { get; set; }
        public string? Directorate { get; set; }
        public string? Section { get; set; }
        public string? Unit { get; set; }
        // Multi-unit (MU-03): semua unit pekerja + penanda primary. Section TETAP scalar.
        public List<string> Units { get; set; } = new();
        public string? PrimaryUnit { get; set; }
        public string? Role { get; set; }
        public int RoleLevel { get; set; }
        public PSignViewModel? PSign { get; set; }
    }
}
