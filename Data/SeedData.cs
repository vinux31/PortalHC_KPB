using Microsoft.AspNetCore.Identity;
using HcPortal.Models;

namespace HcPortal.Data
{
    /// <summary>
    /// Seed data untuk roles dan sample users
    /// </summary>
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Create Roles
            await CreateRolesAsync(roleManager);

            // 2. Create Sample Users
            await CreateUsersAsync(userManager);
        }

        private static async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var roleName in UserRoles.AllRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    Console.WriteLine($"✅ Role '{roleName}' created.");
                }
            }
        }

        private static async Task CreateUsersAsync(UserManager<ApplicationUser> userManager)
        {
            // Sample users sesuai permintaan
            var sampleUsers = new List<(ApplicationUser User, string Password, string Role)>
            {
                (new ApplicationUser
                {
                    UserName = "rino.prasetyo@pertamina.com",
                    Email = "rino.prasetyo@pertamina.com",
                    EmailConfirmed = true,
                    FullName = "Rino",
                    Position = "System Administrator",
                    Section = null,
                    Unit = null,
                    RoleLevel = 1,
                    SelectedView = "Admin"  // Admin default view
                }, "123456", UserRoles.Admin),

                (new ApplicationUser
                {
                    UserName = "meylisa.tjiang@pertamina.com",
                    Email = "meylisa.tjiang@pertamina.com",
                    EmailConfirmed = true,
                    FullName = "Meylisa",
                    Position = "HC Staff",
                    Section = null, // HC can access all sections
                    Unit = null,
                    RoleLevel = 2,
                    SelectedView = "HC"  // HC view
                }, "123456", UserRoles.HC),

                // Level 3 - Management (NEW)
                (new ApplicationUser
                {
                    UserName = "direktur@pertamina.com",
                    Email = "direktur@pertamina.com",
                    EmailConfirmed = true,
                    FullName = "Budi Hartono",
                    Position = "Direktur Operasi",
                    Section = null, // Full access
                    Unit = null,
                    RoleLevel = 3,
                    SelectedView = "Atasan"  // Atasan view
                }, "123456", UserRoles.Direktur),

                (new ApplicationUser
                {
                    UserName = "vp@pertamina.com",
                    Email = "vp@pertamina.com",
                    EmailConfirmed = true,
                    FullName = "Siti Nurhaliza",
                    Position = "VP Refinery",
                    Section = null, // Full access
                    Unit = null,
                    RoleLevel = 3,
                    SelectedView = "Atasan"  // Atasan view
                }, "123456", UserRoles.VP),

                (new ApplicationUser
                {
                    UserName = "manager@pertamina.com",
                    Email = "manager@pertamina.com",
                    EmailConfirmed = true,
                    FullName = "Ahmad Yani",
                    Position = "Manager Process",
                    Section = null, // Full access
                    Unit = null,
                    RoleLevel = 3,
                    SelectedView = "Atasan"  // Atasan view
                }, "123456", UserRoles.Manager),

                (new ApplicationUser
                {
                    UserName = "taufik.hartopo@pertamina.com",
                    Email = "taufik.hartopo@pertamina.com",
                    EmailConfirmed = true,
                    FullName = "Taufik Basuki",
                    Position = "Section Head",
                    Section = "GAST",
                    Unit = null, // Section Head can access all units in their section
                    RoleLevel = 4,
                    SelectedView = "Atasan"  // Atasan view
                }, "123456", UserRoles.SectionHead),

                (new ApplicationUser
                {
                    UserName = "choirul.anam@pertamina.com",
                    Email = "choirul.anam@pertamina.com",
                    EmailConfirmed = true,
                    FullName = "Choirul Anam",
                    Position = "Sr Supervisor",
                    Section = "GAST",
                    Unit = "Alkylation Unit (065)",
                    RoleLevel = 4,
                    SelectedView = "Atasan"  // Atasan view
                }, "123456", UserRoles.SrSupervisor),

                (new ApplicationUser
                {
                    UserName = "rustam.nugroho@pertamina.com",
                    Email = "rustam.nugroho@pertamina.com",
                    EmailConfirmed = true,
                    FullName = "Rustam Santiko",
                    Position = "Coach",
                    Section = "GAST",
                    Unit = "Alkylation Unit (065)",
                    RoleLevel = 5,
                    SelectedView = "Coach"  // Coach view
                }, "123456", UserRoles.Coach),

                (new ApplicationUser
                {
                    UserName = "iwan3@pertamina.com",
                    Email = "iwan3@pertamina.com",
                    EmailConfirmed = true,
                    FullName = "Iwan",
                    Position = "Operator",
                    Section = "GAST",
                    Unit = "Alkylation Unit (065)",
                    RoleLevel = 6,
                    SelectedView = "Coachee"  // Coachee view
                }, "123456", UserRoles.Coachee)
            };

            foreach (var (user, password, role) in sampleUsers)
            {
                // Check if user exists
                var existingUser = await userManager.FindByEmailAsync(user.Email!);
                if (existingUser == null)
                {
                    var result = await userManager.CreateAsync(user, password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, role);
                        Console.WriteLine($"✅ User '{user.FullName}' ({user.Email}) created with role '{role}'.");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to create user '{user.Email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"ℹ️ User '{user.Email}' already exists.");
                }
            }
        }
    }
}
