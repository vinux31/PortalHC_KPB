namespace HcPortal.Models.Guide;

public record GuideItem(
    string Id,
    GuideModule Module,
    string Title,
    RoleGroup[] Roles,
    GuideStep[] Steps,
    string[] Keywords
);
