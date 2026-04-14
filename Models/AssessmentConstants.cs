namespace HcPortal.Models
{
    public static class AssessmentConstants
    {
        public static class AssessmentType
        {
            public const string Manual = "Manual";
            public const string Online = "Online";
            public const string PreTest = "PreTest";
            public const string PostTest = "PostTest";
        }

        public static class AssessmentStatus
        {
            public const string Open = "Open";
            public const string Upcoming = "Upcoming";
            public const string Completed = "Completed";
        }

        public static class CertificateType
        {
            public const string Permanent = "Permanent";
            public const string Annual = "Annual";
            public const string ThreeYear = "3-Year";
        }

        public static class FileValidation
        {
            public const long MaxCertificateFileSizeBytes = 10 * 1024 * 1024; // 10MB
            public static readonly string[] AllowedCertificateExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        }
    }
}
