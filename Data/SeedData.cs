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

            // 3. Seed OrganizationUnits (safety net for fresh deployment)
            await SeedOrganizationUnitsAsync(context);

            // 4. Seed OrganizationLevelLabels — Phase 340 D-01 (permanent + prod-required)
            await SeedOrganizationLevelLabelsAsync(context);

            // 5. Seed ProtonTracks (safety net — sebelumnya hanya di-seed via migration
            //    CreateProtonTrackTable; kalau migration tak jalan / baris terhapus, dropdown
            //    Track + StatusData ProtonData kosong. Idempotent, production-safe.)
            await SeedProtonTracksAsync(context);
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
                var parent = new OrganizationUnit { Name = section.Name, Level = 0, DisplayOrder = section.Order, IsActive = true };
                context.OrganizationUnits.Add(parent);
                await context.SaveChangesAsync();

                int unitOrder = 1;
                foreach (var unitName in section.Units)
                {
                    context.OrganizationUnits.Add(new OrganizationUnit
                    {
                        Name = unitName, ParentId = parent.Id, Level = 1, DisplayOrder = unitOrder++, IsActive = true
                    });
                }
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Seed default 3 baris OrganizationLevelLabels (Level 0='Bagian', 1='Unit', 2='Sub-unit').
        /// Klasifikasi: permanent + prod-required (per docs/SEED_WORKFLOW.md §3, Phase 340 D-01).
        /// Idempotent — skip bila tabel sudah ada baris (preserve HC custom label).
        /// </summary>
        public static async Task SeedOrganizationLevelLabelsAsync(ApplicationDbContext context)
        {
            if (await context.OrganizationLevelLabels.AnyAsync())
                return;

            var defaults = new[]
            {
                new OrganizationLevelLabel { Level = 0, Label = "Bagian",   UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
                new OrganizationLevelLabel { Level = 1, Label = "Unit",     UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
                new OrganizationLevelLabel { Level = 2, Label = "Sub-unit", UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
            };
            context.OrganizationLevelLabels.AddRange(defaults);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seed 6 baris ProtonTrack master (Panelman/Operator × Tahun 1/2/3).
        /// Klasifikasi: permanent + prod-required. Sumber kebenaran sama dengan migration
        /// 20260223060707_CreateProtonTrackTable Step 5; ini safety-net agar tabel auto-isi
        /// tiap startup bila kosong (mis. migration tak ter-apply / baris terhapus di Dev).
        /// Idempotent — skip bila tabel sudah ada baris.
        /// DisplayName + Urutan dibangun konsisten: "TrackType - TahunKe", urut Panelman 1-3 lalu Operator 4-6.
        /// </summary>
        public static async Task SeedProtonTracksAsync(ApplicationDbContext context)
        {
            if (await context.ProtonTracks.AnyAsync())
                return;

            var trackTypes = new[] { "Panelman", "Operator" };
            var tahunList = new[] { "Tahun 1", "Tahun 2", "Tahun 3" };

            int urutan = 1;
            foreach (var trackType in trackTypes)
            {
                foreach (var tahunKe in tahunList)
                {
                    context.ProtonTracks.Add(new ProtonTrack
                    {
                        TrackType = trackType,
                        TahunKe = tahunKe,
                        DisplayName = $"{trackType} - {tahunKe}",
                        Urutan = urutan++
                    });
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
