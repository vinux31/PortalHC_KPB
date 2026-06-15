namespace HcPortal.Models
{
    public static class QuestionTypeLabels
    {
        public static string Long(string? type) => type switch
        {
            "MultipleChoice" => "Single Answer (1 jawaban benar)",
            "MultipleAnswer" => "Multiple Answer (≥2 jawaban benar)",
            "Essay"          => "Essay",
            _                => "Single Answer (1 jawaban benar)"
        };

        public static string Short(string? type) => type switch
        {
            "MultipleChoice" => "Single Answer",
            "MultipleAnswer" => "Multiple Answer",
            "Essay"          => "Essay",
            _                => "Single Answer"
        };

        public static string BadgeClass(string? type) => type switch
        {
            "MultipleChoice" => "bg-secondary",
            "MultipleAnswer" => "bg-primary",
            "Essay"          => "bg-info text-dark",
            _                => "bg-secondary"
        };
    }
}
