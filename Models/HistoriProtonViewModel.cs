namespace HcPortal.Models;

public class HistoriProtonViewModel
{
    public List<HistoriProtonWorkerRow> Workers { get; set; } = new();
    public string? SearchQuery { get; set; }
    public string? FilterSection { get; set; }
    public string? FilterUnit { get; set; }
    public string? FilterJalur { get; set; }
    public string? FilterStatus { get; set; }
    public List<string> AvailableSections { get; set; } = new();
    public List<string> AvailableUnits { get; set; } = new();
}

public class HistoriProtonWorkerRow
{
    public string UserId { get; set; } = "";
    public string Nama { get; set; } = "";
    public string NIP { get; set; } = "";
    public string Section { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Jalur { get; set; } = ""; // Panelman or Operator
    public bool Tahun1Done { get; set; }
    public bool Tahun2Done { get; set; }
    public bool Tahun3Done { get; set; }
    public bool Tahun1InProgress { get; set; }
    public bool Tahun2InProgress { get; set; }
    public bool Tahun3InProgress { get; set; }
    public string Status { get; set; } = ""; // Lulus, Dalam Proses
}
