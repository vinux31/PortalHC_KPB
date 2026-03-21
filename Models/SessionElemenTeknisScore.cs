using System.ComponentModel.DataAnnotations.Schema;

namespace HcPortal.Models;

public class SessionElemenTeknisScore
{
    public int Id { get; set; }

    public int AssessmentSessionId { get; set; }
    [ForeignKey("AssessmentSessionId")]
    public virtual AssessmentSession AssessmentSession { get; set; } = null!;

    /// <summary>Nama elemen teknis. "Lainnya" untuk soal tanpa tag ET.</summary>
    public string ElemenTeknis { get; set; } = "";

    public int CorrectCount { get; set; }
    public int QuestionCount { get; set; }
}
