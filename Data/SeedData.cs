using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;

namespace HcPortal.Data
{
    /// <summary>
    /// Seed data untuk roles dan organization units (production-safe).
    /// </summary>
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider, Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Create Roles (always — needed in all environments)
            await CreateRolesAsync(roleManager);

            // 2. Seed OrganizationUnits (safety net for fresh deployment)
            await SeedOrganizationUnitsAsync(context);
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
