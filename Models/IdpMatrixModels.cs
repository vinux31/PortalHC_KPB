namespace HcPortal.Models
{
    public class IdpCompetency
    {
        public string Name { get; set; } = "";
        public List<IdpImplementationPeriod> Periods { get; set; } = new List<IdpImplementationPeriod>();
    }

    public class IdpImplementationPeriod
    {
        public string PeriodName { get; set; } = ""; // e.g. "Tahun Pertama"
        public List<string> OperatorCompetencies { get; set; } = new List<string>();
        public List<string> PanelmanCompetencies { get; set; } = new List<string>();
    }
}
