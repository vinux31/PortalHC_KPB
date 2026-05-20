namespace HcPortal.Models.Guide;

public record GuideViewModel(
    string UserRole,
    IReadOnlyList<GuideModuleCardVm> ModuleCards,
    IReadOnlyDictionary<FaqCategory, IReadOnlyList<GuideFaqItem>> FaqsByCategory
);
