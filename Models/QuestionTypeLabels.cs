namespace HcPortal.Models
{
    public static class QuestionTypeLabels
    {
        public static string Long(string? type) => type switch
        {
            "MultipleChoice" => "Single Choice (1 jawaban benar)",
            "MultipleAnswer" => "Multiple Answers (≥2 jawaban benar)",
            "Essay"          => "Essay",
            _                => "Single Choice (1 jawaban benar)"
        };

        public static string Short(string? type) => type switch
        {
            "MultipleChoice" => "Single Choice",
            "MultipleAnswer" => "Multiple Answers",
            "Essay"          => "Essay",
            _                => "Single Choice"
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
