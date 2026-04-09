using Microsoft.AspNetCore.Routing;

namespace HcPortal.Helpers
{
    public sealed class MaintenanceScopeOption
    {
        public MaintenanceScopeOption(string key, string label)
        {
            Key = key;
            Label = label;
        }

        public string Key { get; }
        public string Label { get; }
    }

    public sealed class MaintenanceScopeGroup
    {
        public MaintenanceScopeGroup(string key, string label, IReadOnlyList<MaintenanceScopeOption> options)
        {
            Key = key;
            Label = label;
            Options = options;
        }

        public string Key { get; }
        public string Label { get; }
        public IReadOnlyList<MaintenanceScopeOption> Options { get; }
    }

    public static class MaintenanceScopeCatalog
    {
        private static readonly IReadOnlyList<MaintenanceScopeGroup> Groups = new List<MaintenanceScopeGroup>
        {
            new(
                "home",
                "Home",
                new List<MaintenanceScopeOption>
                {
                    new("home.dashboard", "Dashboard"),
                    new("home.guide", "Panduan")
                }),
            new(
                "cmp",
                "CMP",
                new List<MaintenanceScopeOption>
                {
                    new("cmp.index", "CMP - Halaman Utama"),
                    new("cmp.documents", "CMP - Dokumen KKJ"),
                    new("cmp.assessment", "CMP - Assessment"),
                    new("cmp.records", "CMP - Records"),
                    new("cmp.analytics", "CMP - Analytics")
                }),
            new(
                "cdp",
                "CDP",
                new List<MaintenanceScopeOption>
                {
                    new("cdp.index", "CDP - Halaman Utama"),
                    new("cdp.plan_idp", "CDP - Plan IDP"),
                    new("cdp.coaching_proton", "CDP - Coaching Proton"),
                    new("cdp.history_proton", "CDP - Histori Proton"),
                    new("cdp.certification", "CDP - Sertifikasi"),
                    new("cdp.dashboard", "CDP - Dashboard")
                }),
            new(
                "account",
                "Akun",
                new List<MaintenanceScopeOption>
                {
                    new("account.profile", "Profil"),
                    new("account.settings", "Pengaturan Akun")
                }),
            new(
                "admin",
                "Admin",
                new List<MaintenanceScopeOption>
                {
                    new("admin.index", "Admin - Halaman Utama"),
                    new("admin.workers", "Admin - Manajemen Pekerja"),
                    new("admin.organization", "Admin - Struktur Organisasi"),
                    new("admin.documents", "Admin - Dokumen Kompetensi"),
                    new("admin.proton_data", "Admin - Data Proton"),
                    new("admin.coach_mapping", "Admin - Mapping Coach"),
                    new("admin.assessment_training", "Admin - Assessment & Training"),
                    new("admin.renewal", "Admin - Renewal")
                })
        };

        private static readonly IReadOnlyDictionary<string, string> LabelsByKey = Groups
            .SelectMany(group => group.Options)
            .ToDictionary(option => option.Key, option => option.Label, StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlyList<string> OrderedKeys = Groups
            .SelectMany(group => group.Options)
            .Select(option => option.Key)
            .ToList();

        public static IReadOnlyList<MaintenanceScopeGroup> GetGroups() => Groups;

        public static string NormalizeSelectedKeys(string? selectedKeys)
        {
            if (string.IsNullOrWhiteSpace(selectedKeys))
            {
                return string.Empty;
            }

            var requested = selectedKeys
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var normalized = OrderedKeys
                .Where(key => requested.Contains(key))
                .ToList();

            return string.Join(',', normalized);
        }

        public static IReadOnlyCollection<string> GetSelectedKeySet(string? scope)
        {
            if (string.IsNullOrWhiteSpace(scope) || string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<string>();
            }

            return NormalizeSelectedKeys(scope)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public static IReadOnlyList<string> GetLabels(string? scope)
        {
            return GetSelectedKeySet(scope)
                .Select(GetLabel)
                .ToList();
        }

        public static string GetLabel(string key)
        {
            return LabelsByKey.TryGetValue(key, out var label) ? label : key;
        }

        public static string GetSummary(string? scope, int maxVisible = 3)
        {
            if (string.IsNullOrWhiteSpace(scope) || string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase))
            {
                return "Seluruh Website";
            }

            var labels = GetLabels(scope);
            if (labels.Count == 0)
            {
                return "Tidak ada halaman terpilih";
            }

            if (labels.Count <= maxVisible)
            {
                return string.Join(", ", labels);
            }

            return string.Join(", ", labels.Take(maxVisible)) + $" +{labels.Count - maxVisible} lainnya";
        }

        public static string? ResolveScopeKey(RouteValueDictionary routeValues)
        {
            var controller = routeValues["controller"]?.ToString();
            var action = routeValues["action"]?.ToString();

            if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(action))
            {
                return null;
            }

            switch (controller)
            {
                case "Home":
                    return action switch
                    {
                        "Index" => "home.dashboard",
                        "Guide" => "home.guide",
                        "GuideDetail" => "home.guide",
                        _ => null
                    };
                case "CMP":
                    return action switch
                    {
                        "Index" => "cmp.index",
                        "DokumenKkj" => "cmp.documents",
                        "KkjFileDownload" => "cmp.documents",
                        "CpdpFileDownload" => "cmp.documents",
                        "Assessment" => "cmp.assessment",
                        "VerifyToken" => "cmp.assessment",
                        "StartExam" => "cmp.assessment",
                        "SaveAnswer" => "cmp.assessment",
                        "UpdateSessionProgress" => "cmp.assessment",
                        "ExamSummary" => "cmp.assessment",
                        "SubmitExam" => "cmp.assessment",
                        "AbandonExam" => "cmp.assessment",
                        "Results" => "cmp.assessment",
                        "Certificate" => "cmp.assessment",
                        "CertificatePdf" => "cmp.assessment",
                        "Records" => "cmp.records",
                        "RecordsWorkerDetail" => "cmp.records",
                        "ExportRecords" => "cmp.records",
                        "ExportRecordsTeamAssessment" => "cmp.records",
                        "ExportRecordsTeamTraining" => "cmp.records",
                        "RecordsTeamPartial" => "cmp.records",
                        "AnalyticsDashboard" => "cmp.analytics",
                        "GetAnalyticsData" => "cmp.analytics",
                        "GetAnalyticsCascadeUnits" => "cmp.analytics",
                        "GetAnalyticsCascadeSubKategori" => "cmp.analytics",
                        "GetPrePostAssessmentList" => "cmp.analytics",
                        "GetItemAnalysisData" => "cmp.analytics",
                        "GetGainScoreData" => "cmp.analytics",
                        "ExportItemAnalysisExcel" => "cmp.analytics",
                        "ExportGainScoreExcel" => "cmp.analytics",
                        _ => null
                    };
                case "CDP":
                    return action switch
                    {
                        "Index" => "cdp.index",
                        "PlanIdp" => "cdp.plan_idp",
                        "GuidanceDownload" => "cdp.plan_idp",
                        "GetCascadeOptions" => "cdp.plan_idp",
                        "GetSubCategories" => "cdp.plan_idp",
                        "CoachingProton" => "cdp.coaching_proton",
                        "FilterCoachingProton" => "cdp.coaching_proton",
                        "Deliverable" => "cdp.coaching_proton",
                        "ApproveDeliverable" => "cdp.coaching_proton",
                        "RejectDeliverable" => "cdp.coaching_proton",
                        "HCReviewDeliverable" => "cdp.coaching_proton",
                        "UploadEvidence" => "cdp.coaching_proton",
                        "DownloadEvidence" => "cdp.coaching_proton",
                        "ApproveFromProgress" => "cdp.coaching_proton",
                        "RejectFromProgress" => "cdp.coaching_proton",
                        "HCReviewFromProgress" => "cdp.coaching_proton",
                        "SubmitEvidenceWithCoaching" => "cdp.coaching_proton",
                        "EditCoachingSession" => "cdp.coaching_proton",
                        "DeleteCoachingSession" => "cdp.coaching_proton",
                        "DownloadEvidencePdf" => "cdp.coaching_proton",
                        "GetCoacheeDeliverables" => "cdp.coaching_proton",
                        "BatchHCApprove" => "cdp.coaching_proton",
                        "ExportProgressExcel" => "cdp.coaching_proton",
                        "ExportProgressPdf" => "cdp.coaching_proton",
                        "ExportBottleneckReport" => "cdp.coaching_proton",
                        "ExportCoachingTracking" => "cdp.coaching_proton",
                        "ExportWorkloadSummary" => "cdp.coaching_proton",
                        "HistoriProton" => "cdp.history_proton",
                        "HistoriProtonDetail" => "cdp.history_proton",
                        "ExportHistoriProton" => "cdp.history_proton",
                        "CertificationManagement" => "cdp.certification",
                        "FilterCertificationManagement" => "cdp.certification",
                        "ExportSertifikatExcel" => "cdp.certification",
                        "Dashboard" => "cdp.dashboard",
                        _ => null
                    };
                case "Account":
                    return action switch
                    {
                        "Profile" => "account.profile",
                        "Settings" => "account.settings",
                        "EditProfile" => "account.settings",
                        "ChangePassword" => "account.settings",
                        _ => null
                    };
                case "Admin":
                    return action switch
                    {
                        "Index" => "admin.index",
                        "Maintenance" => "admin.index",
                        _ => null
                    };
                case "Worker":
                    return "admin.workers";
                case "Organization":
                    return "admin.organization";
                case "DocumentAdmin":
                    return "admin.documents";
                case "ProtonData":
                    return "admin.proton_data";
                case "CoachMapping":
                    return "admin.coach_mapping";
                case "AssessmentAdmin":
                case "TrainingAdmin":
                    return "admin.assessment_training";
                case "Renewal":
                    return "admin.renewal";
                default:
                    return null;
            }
        }
    }
}
