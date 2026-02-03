namespace HcPortal.Models
{
    /// <summary>
    /// ViewModel untuk halaman seleksi Bagian/Unit di Records page
    /// </summary>
    public class RecordsSelectViewModel
    {
        public string? SelectedSection { get; set; }
        public string? SelectedUnit { get; set; }
        public List<string> AvailableSections { get; set; } = new List<string>();
        public List<string> AvailableUnits { get; set; } = new List<string>();
        public string? UserRole { get; set; }
    }
}
