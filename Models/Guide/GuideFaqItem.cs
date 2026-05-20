namespace HcPortal.Models.Guide;

public record GuideFaqItem(
    string Id,
    FaqCategory Category,
    string Question,
    string AnswerHtml,
    RoleGroup[] Roles,
    string[] Keywords
);
