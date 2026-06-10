using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HcPortal.Models;

namespace HcPortal.Data
{
    /// <summary>
    /// Seed data untuk roles, admin bootstrap, dan organization units.
    /// Semua method idempotent dan production-safe.
    /// </summary>
    public class SeedData
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
        /// Self-heal: pastikan 6 ProtonTrack master ada (Panelman/Operator × Tahun 1-3).
        /// Insert-if-missing by (TrackType,TahunKe) — idempotent, preserve existing.
        /// Track aslinya di-seed migration CreateProtonTrackTable (sekali); ini bikin tahan-banting
        /// kalau baris hilang (mis. restore DB). Tak memperbaiki ref ProtonTrackId lama yang dangling.
        /// </summary>
        public static async Task SeedProtonTracksAsync(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                var expected = new[]
                {
                    new ProtonTrack { TrackType = "Panelman", TahunKe = "Tahun 1", DisplayName = "Panelman - Tahun 1", Urutan = 1 },
                    new ProtonTrack { TrackType = "Panelman", TahunKe = "Tahun 2", DisplayName = "Panelman - Tahun 2", Urutan = 2 },
                    new ProtonTrack { TrackType = "Panelman", TahunKe = "Tahun 3", DisplayName = "Panelman - Tahun 3", Urutan = 3 },
                    new ProtonTrack { TrackType = "Operator", TahunKe = "Tahun 1", DisplayName = "Operator - Tahun 1", Urutan = 4 },
                    new ProtonTrack { TrackType = "Operator", TahunKe = "Tahun 2", DisplayName = "Operator - Tahun 2", Urutan = 5 },
                    new ProtonTrack { TrackType = "Operator", TahunKe = "Tahun 3", DisplayName = "Operator - Tahun 3", Urutan = 6 },
                };

                // Pre-check orphan (log saja — tidak diperbaiki, lihat spec Non-Goals)
                var orphanKomp = await context.ProtonKompetensiList
                    .CountAsync(k => !context.ProtonTracks.Any(t => t.Id == k.ProtonTrackId));
                var orphanAssign = await context.ProtonTrackAssignments
                    .CountAsync(a => !context.ProtonTracks.Any(t => t.Id == a.ProtonTrackId));
                if (orphanKomp > 0 || orphanAssign > 0)
                    logger.LogWarning("SeedProtonTracks: orphan ProtonTrackId — Kompetensi={K}, Assignment={A} (pre-existing, tak diperbaiki).",
                        orphanKomp, orphanAssign);

                var existingKeys = await context.ProtonTracks
                    .Select(t => new { t.TrackType, t.TahunKe }).ToListAsync();
                var existingSet = new HashSet<string>(existingKeys.Select(k => $"{k.TrackType}|{k.TahunKe}"));

                var missing = expected.Where(e => !existingSet.Contains($"{e.TrackType}|{e.TahunKe}")).ToList();
                if (missing.Count == 0)
                {
                    logger.LogInformation("SeedProtonTracks: 6 track sudah lengkap, no-op.");
                    return;
                }

                context.ProtonTracks.AddRange(missing);
                await context.SaveChangesAsync();   // satu save → atomik (EF implicit transaction)
                logger.LogInformation("SeedProtonTracks: {Count} track di-seed.", missing.Count);
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "SeedProtonTracks: SaveChanges gagal, dilewati (data tak berubah).");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SeedProtonTracks: error tak terduga, dilewati.");
            }
        }
    }
}
