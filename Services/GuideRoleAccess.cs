using HcPortal.Models;
using HcPortal.Models.Guide;

namespace HcPortal.Services;

public static class GuideRoleAccess
{
    public static RoleGroup[] GroupsFor(string userRole) => userRole switch
    {
        UserRoles.Admin or UserRoles.HC
            => new[] { RoleGroup.AdminHC, RoleGroup.Manager, RoleGroup.Atasan,
                       RoleGroup.Coach, RoleGroup.Coachee, RoleGroup.All },

        UserRoles.Direktur or UserRoles.VP or UserRoles.Manager
            => new[] { RoleGroup.Manager, RoleGroup.All },

        UserRoles.SectionHead or UserRoles.SrSupervisor
            => new[] { RoleGroup.Atasan, RoleGroup.All },

        UserRoles.Coach or UserRoles.Supervisor
            => new[] { RoleGroup.Coach, RoleGroup.All },

        UserRoles.Coachee
            => new[] { RoleGroup.Coachee, RoleGroup.All },

        _ => new[] { RoleGroup.All }
    };

    public static bool CanSee(string userRole, RoleGroup[] itemRoles)
        => GroupsFor(userRole).Any(g => itemRoles.Contains(g));

    public static string BadgeLabel(RoleGroup[] roles)
    {
        if (roles.Contains(RoleGroup.All)) return string.Empty;
        if (roles.Length == 0) return string.Empty;

        var labels = roles.Select(g => g switch
        {
            RoleGroup.AdminHC => "Admin & HC",
            RoleGroup.Manager => "Manager",
            RoleGroup.Atasan  => "Atasan",
            RoleGroup.Coach   => "Coach",
            RoleGroup.Coachee => "Coachee",
            _                 => g.ToString()
        });
        return string.Join(" & ", labels);
    }
}
