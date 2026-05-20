namespace HcPortal.Models.Guide;

public record GuideModuleCardVm(
    GuideModule Module,
    string Title,
    string ShortLabel,
    string IconCssClass,
    string CardCssClass,
    int ItemCount,
    int AosDelay,
    RoleGroup[] Roles,
    string[] Keywords
);
