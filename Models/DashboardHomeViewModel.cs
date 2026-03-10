namespace HcPortal.Models
{
    /// <summary>
    /// ViewModel for the minimalist homepage
    /// </summary>
    public class DashboardHomeViewModel
    {
        public ApplicationUser CurrentUser { get; set; } = null!;
        public string Greeting { get; set; } = string.Empty;
    }
}
