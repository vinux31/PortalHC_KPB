using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;

namespace HcPortal.Data
{
    /// <summary>
    /// Seed data untuk roles dan sample users
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

            // 2. Create Sample Users (Development only — test accounts with weak passwords)
            if (environment.IsDevelopment())
            {
                await CreateUsersAsync(userManager);
            }
            else
            {
                Console.WriteLine("Skipping test user seeding (non-Development environment).");
            }

            // 3. One-time cleanup: deactivate duplicate active ProtonTrackAssignments (CLN-01)
            await DeduplicateProtonTrackAssignments(context);

            // 4. One-time cleanup: merge split Kompetensi/SubKompetensi records and remove junk (CLN-02)
            await MergeProtonCatalogDuplicates(context);
        }

        /// <summary>
        /// CLN-01: Deactivates all but the latest active ProtonTrackAssignment per (CoacheeId, ProtonTrackId) pair.
        /// Idempotent — does nothing if no duplicates exist.
        /// </summary>
        public static async Task<int> DeduplicateProtonTrackAssignments(ApplicationDbContext context)
        {
            // Load all active assignments grouped by coachee+track
            var activeAssignments = await context.ProtonTrackAssignments
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.AssignedAt)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            var grouped = activeAssignments
                .GroupBy(a => new { a.CoacheeId, a.ProtonTrackId });

            var toDeactivate = new List<ProtonTrackAssignment>();
            foreach (var group in grouped)
            {
                if (group.Count() > 1)
                {
                    // Keep the first (latest AssignedAt / highest Id), deactivate the rest
                    toDeactivate.AddRange(group.Skip(1));
                }
            }

            if (toDeactivate.Count == 0)
            {
                Console.WriteLine("CLN-01: No duplicate active ProtonTrackAssignments found.");
                return 0;
            }

            var now = DateTime.UtcNow;
            foreach (var assignment in toDeactivate)
            {
                assignment.IsActive = false;
                assignment.DeactivatedAt = now;
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"CLN-01: Deactivated {toDeactivate.Count} duplicate ProtonTrackAssignment(s).");
            return toDeactivate.Count;
        }

        /// <summary>
        /// CLN-02: Merges split Kompetensi/SubKompetensi records for "1. Safe Work Practice" and removes junk test data.
        /// Problem: Kompetensi "1. Safe Work Practice &amp; Lifesaving Rules" was split across KId=2,5,6 and
        /// SubKompetensi "1.1 Safe Work Practice" across SKId=4,5,8 — causing deliverables 3-7 to appear before 1-2.
        /// Also removes junk test records (KId=3 "21312", KId=4 "eqweqw").
        /// Idempotent — does nothing if already merged (KId=2 no longer exists).
        /// </summary>
        public static async Task MergeProtonCatalogDuplicates(ApplicationDbContext context)
        {
            // Check if merge is needed — if KId=2 doesn't exist, already done
            var kompToMerge = await context.ProtonKompetensiList.FindAsync(2);
            if (kompToMerge == null)
            {
                Console.WriteLine("CLN-02: Kompetensi catalog already consolidated.");
                return;
            }

            int changes = 0;

            // --- Step 1: Merge SubKompetensi "1.1 Safe Work Practice" ---
            // SKId=8 (under KId=5) is the survivor — has deliverables 3-7
            // SKId=4 (under KId=2) has deliverable "1. Menjelaskan tingkatan budaya HSSE"
            // SKId=5 (under KId=2) has deliverable "2. Melakukan indentifikasi bahaya"
            // Move their deliverables to SKId=8
            var delivsToMove = await context.ProtonDeliverableList
                .Where(d => d.ProtonSubKompetensiId == 4 || d.ProtonSubKompetensiId == 5)
                .ToListAsync();
            foreach (var d in delivsToMove)
            {
                d.ProtonSubKompetensiId = 8; // survivor SK "1.1 Safe Work Practice"
            }
            changes += delivsToMove.Count;

            // --- Step 2: Fix deliverable Urutan under SKId=8 ---
            // After merge, SKId=8 will have all 7 deliverables — set Urutan 1-7 by name prefix
            var allDelivsInSK8 = await context.ProtonDeliverableList
                .Where(d => d.ProtonSubKompetensiId == 8)
                .ToListAsync();
            // Include the ones we just moved (they're tracked but not yet saved)
            var combined = allDelivsInSK8.Union(delivsToMove).Distinct().ToList();
            foreach (var d in combined)
            {
                // Extract leading number from name like "3.\tMemberikan..."
                var name = d.NamaDeliverable.TrimStart();
                if (name.Length > 0 && char.IsDigit(name[0]))
                {
                    var numStr = new string(name.TakeWhile(c => char.IsDigit(c)).ToArray());
                    if (int.TryParse(numStr, out var num))
                        d.Urutan = num;
                }
            }

            // --- Step 3: Move SubKompetensi from KId=6 to KId=5 ---
            // SKId=9 "1.2. Lifesaving Rules" and SKId=10 "1.3. Emergency Response"
            var subsToMove = await context.ProtonSubKompetensiList
                .Where(sk => sk.ProtonKompetensiId == 6)
                .ToListAsync();
            foreach (var sk in subsToMove)
            {
                sk.ProtonKompetensiId = 5; // survivor Kompetensi
            }
            changes += subsToMove.Count;

            // --- Step 4: Fix SubKompetensi Urutan under KId=5 ---
            // After merge: SK8 "1.1" → Urutan=1, SK9 "1.2" → Urutan=2, SK10 "1.3" → Urutan=3
            var sk8 = await context.ProtonSubKompetensiList.FindAsync(8);
            if (sk8 != null) sk8.Urutan = 1;
            var sk9 = await context.ProtonSubKompetensiList.FindAsync(9);
            if (sk9 != null) sk9.Urutan = 2;
            var sk10 = await context.ProtonSubKompetensiList.FindAsync(10);
            if (sk10 != null) sk10.Urutan = 3;

            // --- Step 5: Fix Kompetensi Urutan ---
            // KId=5 "1. Safe Work Practice" → Urutan=1 (was 3)
            var k5 = await context.ProtonKompetensiList.FindAsync(5);
            if (k5 != null) k5.Urutan = 1;

            // --- Step 6: Delete empty SubKompetensi (SKId=4, SKId=5) ---
            var emptySubs = await context.ProtonSubKompetensiList
                .Where(sk => sk.Id == 4 || sk.Id == 5)
                .ToListAsync();
            context.ProtonSubKompetensiList.RemoveRange(emptySubs);

            // --- Step 7: Delete orphaned Kompetensi (KId=2, KId=6) ---
            var orphanKomps = await context.ProtonKompetensiList
                .Where(k => k.Id == 2 || k.Id == 6)
                .ToListAsync();
            context.ProtonKompetensiList.RemoveRange(orphanKomps);

            // --- Step 8: Delete junk test records (KId=3 "21312", KId=4 "eqweqw") ---
            var junkKomps = await context.ProtonKompetensiList
                .Where(k => k.Id == 3 || k.Id == 4)
                .ToListAsync();
            if (junkKomps.Any())
            {
                var junkKIds = junkKomps.Select(k => k.Id).ToList();
                var junkSubs = await context.ProtonSubKompetensiList
                    .Where(sk => junkKIds.Contains(sk.ProtonKompetensiId))
                    .ToListAsync();
                var junkSKIds = junkSubs.Select(sk => sk.Id).ToList();
                var junkDelivs = await context.ProtonDeliverableList
                    .Where(d => junkSKIds.Contains(d.ProtonSubKompetensiId))
                    .ToListAsync();
                context.ProtonDeliverableList.RemoveRange(junkDelivs);
                context.ProtonSubKompetensiList.RemoveRange(junkSubs);
                context.ProtonKompetensiList.RemoveRange(junkKomps);
                changes += junkDelivs.Count + junkSubs.Count + junkKomps.Count;
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"CLN-02: Merged split Kompetensi/SubKompetensi and removed junk ({changes} records affected).");
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
                    UserName = "admin@pertamina.com",
                    Email = "admin@pertamina.com",
                    EmailConfirmed = true,
                    FullName = "Admin KPB",
                    Position = "System Administrator",
                    Section = null,
                    Unit = null,
                    RoleLevel = 1,
                    SelectedView = UserRoles.GetDefaultView(UserRoles.Admin)
                }, "123456", UserRoles.Admin),

                (new ApplicationUser
                {
                    UserName = "rino.prasetyo@pertamina.com",
                    Email = "rino.prasetyo@pertamina.com",
                    EmailConfirmed = true,
                    FullName = "Rino",
                    Position = "Operator",
                    Section = "GAST",
                    Unit = "Alkylation Unit (065)",
                    RoleLevel = 6,
                    SelectedView = UserRoles.GetDefaultView(UserRoles.Coachee)
                }, "123456", UserRoles.Coachee),

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
                    SelectedView = UserRoles.GetDefaultView(UserRoles.HC)
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
                    SelectedView = UserRoles.GetDefaultView(UserRoles.Direktur)
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
                    SelectedView = UserRoles.GetDefaultView(UserRoles.VP)
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
                    SelectedView = UserRoles.GetDefaultView(UserRoles.Manager)
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
                    SelectedView = UserRoles.GetDefaultView(UserRoles.SectionHead)
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
                    SelectedView = UserRoles.GetDefaultView(UserRoles.SrSupervisor)
                }, "123456", UserRoles.SrSupervisor),

                (new ApplicationUser
                {
                    UserName = "rustam.nugroho@pertamina.com",
                    Email = "rustam.nugroho@pertamina.com",
                    EmailConfirmed = true,
                    FullName = "Rustam Santiko",
                    Position = "Shift Supervisor",
                    Section = "GAST",
                    Unit = "Alkylation Unit (065)",
                    RoleLevel = 5,
                    SelectedView = UserRoles.GetDefaultView(UserRoles.Coach)
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
                    SelectedView = UserRoles.GetDefaultView(UserRoles.Coachee)
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
