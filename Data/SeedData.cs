using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;

namespace HcPortal.Data
{
    /// <summary>
    /// Seed data untuk roles, admin bootstrap, dan organization units.
    /// Semua method idempotent dan production-safe.
    /// </summary>
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider, Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Create Roles (always — needed in all environments)
            await CreateRolesAsync(roleManager);

            // 2. Create bootstrap admin account (all environments — needed for first login)
            await CreateAdminUserAsync(userManager);

            // 3. Create test users (Development only)
            if (environment.IsDevelopment())
            {
                await CreateTestUsersAsync(userManager);
            }

            // 4. Seed OrganizationUnits (safety net for fresh deployment)
            await SeedOrganizationUnitsAsync(context);
        }

        /// <summary>
        /// Bootstrap admin account — created in all environments so first login is possible.
        /// Password should be changed after first login via Settings.
        /// Idempotent — skips if admin already exists.
        /// </summary>
        private static async Task CreateAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            const string adminEmail = "admin@pertamina.com";
            if (await userManager.FindByEmailAsync(adminEmail) != null)
                return;

            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Admin KPB",
                Position = "System Administrator",
                RoleLevel = 1,
                SelectedView = UserRoles.GetDefaultView(UserRoles.Admin)
            };

            var result = await userManager.CreateAsync(admin, "123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, UserRoles.Admin);
                Console.WriteLine($"Bootstrap admin '{adminEmail}' created. GANTI PASSWORD SEGERA via Settings.");
            }
        }

        /// <summary>
        /// Test users for development/UAT only. Not created in Production.
        /// </summary>
        private static async Task CreateTestUsersAsync(UserManager<ApplicationUser> userManager)
        {
            var testUsers = new (ApplicationUser User, string Password, string Role)[]
            {
                (new ApplicationUser { UserName = "meylisa.tjiang@pertamina.com", Email = "meylisa.tjiang@pertamina.com", EmailConfirmed = true, FullName = "Meylisa", Position = "HC Staff", RoleLevel = 2, SelectedView = UserRoles.GetDefaultView(UserRoles.HC) }, "123456", UserRoles.HC),
                (new ApplicationUser { UserName = "direktur@pertamina.com", Email = "direktur@pertamina.com", EmailConfirmed = true, FullName = "Budi Hartono", Position = "Direktur Operasi", RoleLevel = 3, SelectedView = UserRoles.GetDefaultView(UserRoles.Direktur) }, "123456", UserRoles.Direktur),
                (new ApplicationUser { UserName = "vp@pertamina.com", Email = "vp@pertamina.com", EmailConfirmed = true, FullName = "Siti Nurhaliza", Position = "VP Refinery", RoleLevel = 3, SelectedView = UserRoles.GetDefaultView(UserRoles.VP) }, "123456", UserRoles.VP),
                (new ApplicationUser { UserName = "manager@pertamina.com", Email = "manager@pertamina.com", EmailConfirmed = true, FullName = "Ahmad Yani", Position = "Manager Process", RoleLevel = 3, SelectedView = UserRoles.GetDefaultView(UserRoles.Manager) }, "123456", UserRoles.Manager),
                (new ApplicationUser { UserName = "taufik.hartopo@pertamina.com", Email = "taufik.hartopo@pertamina.com", EmailConfirmed = true, FullName = "Taufik Basuki", Position = "Section Head", Section = "GAST", RoleLevel = 4, SelectedView = UserRoles.GetDefaultView(UserRoles.SectionHead) }, "123456", UserRoles.SectionHead),
                (new ApplicationUser { UserName = "choirul.anam@pertamina.com", Email = "choirul.anam@pertamina.com", EmailConfirmed = true, FullName = "Choirul Anam", Position = "Sr Supervisor", Section = "GAST", Unit = "Alkylation Unit (065)", RoleLevel = 4, SelectedView = UserRoles.GetDefaultView(UserRoles.SrSupervisor) }, "123456", UserRoles.SrSupervisor),
                (new ApplicationUser { UserName = "rustam.nugroho@pertamina.com", Email = "rustam.nugroho@pertamina.com", EmailConfirmed = true, FullName = "Rustam Santiko", Position = "Shift Supervisor", Section = "GAST", Unit = "Alkylation Unit (065)", RoleLevel = 5, SelectedView = UserRoles.GetDefaultView(UserRoles.Coach) }, "123456", UserRoles.Coach),
                (new ApplicationUser { UserName = "rino.prasetyo@pertamina.com", Email = "rino.prasetyo@pertamina.com", EmailConfirmed = true, FullName = "Rino", Position = "Operator", Section = "GAST", Unit = "Alkylation Unit (065)", RoleLevel = 6, SelectedView = UserRoles.GetDefaultView(UserRoles.Coachee) }, "123456", UserRoles.Coachee),
                (new ApplicationUser { UserName = "iwan3@pertamina.com", Email = "iwan3@pertamina.com", EmailConfirmed = true, FullName = "Iwan", Position = "Operator", Section = "GAST", Unit = "Alkylation Unit (065)", RoleLevel = 6, SelectedView = UserRoles.GetDefaultView(UserRoles.Coachee) }, "123456", UserRoles.Coachee),
            };

            foreach (var (user, password, role) in testUsers)
            {
                if (await userManager.FindByEmailAsync(user.Email!) != null)
                    continue;

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(user, role);
            }
        }

        private static async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var roleName in UserRoles.AllRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    Console.WriteLine($"Role '{roleName}' created.");
                }
            }
        }

        /// <summary>
        /// Ensures OrganizationUnits exist in DB (safety net for fresh deployment).
        /// Idempotent — skips if any OrganizationUnit rows already exist.
        /// </summary>
        public static async Task SeedOrganizationUnitsAsync(ApplicationDbContext context)
        {
            if (await context.OrganizationUnits.AnyAsync())
                return;

            var sections = new[]
            {
                new { Name = "RFCC", Order = 1, Units = new[] { "RFCC LPG Treating Unit (062)", "Propylene Recovery Unit (063)" } },
                new { Name = "DHT / HMU", Order = 2, Units = new[] { "Diesel Hydrotreating Unit I & II (054 & 083)", "Hydrogen Manufacturing Unit (068)", "Common DHT H2 Compressor (085)" } },
                new { Name = "NGP", Order = 3, Units = new[] { "Saturated Gas Concentration Unit (060)", "Saturated LPG Treating Unit (064)", "Isomerization Unit (082)", "Common Facilities For NLP (160)", "Naphtha Hydrotreating Unit II (084)" } },
                new { Name = "GAST", Order = 4, Units = new[] { "RFCC NHT (053)", "Alkylation Unit (065)", "Wet Gas Sulfuric Acid Unit (066)", "SWS RFCC & Non RFCC (067 & 167)", "Amine Regeneration Unit I & II (069 & 079)", "Flare System (319)", "Sulfur Recovery Unit (169)" } }
            };

            foreach (var section in sections)
            {
                var parent = new OrganizationUnit { Name = section.Name, Level = 1, DisplayOrder = section.Order, IsActive = true };
                context.OrganizationUnits.Add(parent);
                await context.SaveChangesAsync();

                int unitOrder = 1;
                foreach (var unitName in section.Units)
                {
                    context.OrganizationUnits.Add(new OrganizationUnit
                    {
                        Name = unitName, ParentId = parent.Id, Level = 2, DisplayOrder = unitOrder++, IsActive = true
                    });
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
