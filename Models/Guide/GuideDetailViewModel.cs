namespace HcPortal.Models.Guide;

public record GuideDetailViewModel(
    string UserRole,
    GuideModule Module,
    string ModuleTitle,
    string ModuleIcon,
    string ModuleBreadcrumb,
    string ModuleCategory,
    GuidePdfLink? Pdf,
    IReadOnlyList<GuideItem> Items
);
