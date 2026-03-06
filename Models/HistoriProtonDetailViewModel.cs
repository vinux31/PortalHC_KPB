namespace HcPortal.Models;

public class HistoriProtonDetailViewModel
{
    public string Nama { get; set; } = "";
    public string NIP { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Section { get; set; } = "";
    public string Jalur { get; set; } = "";

    public List<ProtonTimelineNode> Nodes { get; set; } = new();
}

public class ProtonTimelineNode
{
    public int AssignmentId { get; set; }
    public string TahunKe { get; set; } = "";
    public int TahunUrutan { get; set; }
    public string Unit { get; set; } = "";
    public string CoachName { get; set; } = "";
    public string Status { get; set; } = "";
    public int? CompetencyLevel { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
