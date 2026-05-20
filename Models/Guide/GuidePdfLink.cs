namespace HcPortal.Models.Guide;

public record GuidePdfLink(
    GuideModule Module,
    string Title,
    string Description,
    string FilePath,
    string CardCssClass,
    string BtnColorClass,
    RoleGroup[] Roles
);
