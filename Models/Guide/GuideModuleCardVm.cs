namespace HcPortal.Models.Guide;

public record GuideModuleCardVm(
    GuideModule Module,
    string Title,
    string IconCssClass,
    string CardCssClass,
    int ItemCount,
    RoleGroup[] Roles
);
