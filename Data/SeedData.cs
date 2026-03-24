using System.Text.Json;
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

            // 3. Historical utility: deactivate duplicate active ProtonTrackAssignments (CLN-01)
            // Retained for reference — idempotent (no writes if no duplicates found).
            // This was a one-time data correction; the query runs on every startup but is safe.
            await DeduplicateProtonTrackAssignments(context);

            // 4. Historical utility: merge split Kompetensi/SubKompetensi records and remove junk (CLN-02)
            // Retained for reference — self-guarded (returns early if KId=2 not found, i.e., already run).
            await MergeProtonCatalogDuplicates(context);

            // 5. Seed OrganizationUnits (safety net for fresh deployment)
            await SeedOrganizationUnitsAsync(context);

            // 6. Seed UAT data (Development only — assessment, coach-coachee, proton)
            if (environment.IsDevelopment())
            {
                await SeedUatDataAsync(userManager, context);
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

        // =====================================================================
        // UAT SEED METHODS (Development only)
        // =====================================================================

        public static async Task SeedUatDataAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            // Idempotency guard
            if (await context.AssessmentSessions.AnyAsync(s => s.Title == "OJT Proses Alkylation Q1-2026"))
            {
                Console.WriteLine("UAT-SEED: Data UAT sudah ada, skip.");
                return;
            }

            var rino = await userManager.FindByEmailAsync("rino.prasetyo@pertamina.com");
            var iwan = await userManager.FindByEmailAsync("iwan3@pertamina.com");
            var rustam = await userManager.FindByEmailAsync("rustam.nugroho@pertamina.com");
            if (rino == null || iwan == null || rustam == null)
            {
                Console.WriteLine("UAT-SEED: User Rino/Iwan/Rustam tidak ditemukan, skip.");
                return;
            }

            var now = DateTime.UtcNow;

            // 1. Coach-Coachee Mapping
            await SeedCoachCoacheeMappingAsync(context, rustam.Id, rino.Id, now);

            // 2. ProtonTrackAssignment
            await SeedProtonTrackAssignmentAsync(context, rino.Id, rustam.Id, now);

            // 3. AssessmentCategory sub-kategori
            await SeedAssessmentCategoriesAsync(context);

            // 4. Assessment reguler open
            var (questions, package) = await SeedRegularAssessmentOpenAsync(context, rino.Id, iwan.Id, now);

            // 5. Completed assessment lulus untuk Rino (stub — implementasi Plan 02)
            await SeedCompletedAssessmentPassAsync(context, rino.Id, now, questions);

            // 6. Completed assessment gagal untuk Rino (stub — implementasi Plan 02)
            await SeedCompletedAssessmentFailAsync(context, rino.Id, now, questions);

            // 7. Assessment Proton (stub — implementasi Plan 02)
            await SeedProtonAssessmentsAsync(context, rino.Id, now);

            Console.WriteLine("UAT-SEED: Selesai seed data UAT.");
        }

        private static async Task SeedCoachCoacheeMappingAsync(ApplicationDbContext context, string rustamId, string rinoId, DateTime now)
        {
            if (await context.CoachCoacheeMappings.AnyAsync(m => m.CoacheeId == rinoId && m.IsActive))
            {
                Console.WriteLine("UAT-SEED: CoachCoacheeMapping Rino sudah ada, skip.");
                return;
            }

            context.CoachCoacheeMappings.Add(new CoachCoacheeMapping
            {
                CoachId = rustamId,
                CoacheeId = rinoId,
                IsActive = true,
                StartDate = now,
                AssignmentSection = "GAST",
                AssignmentUnit = "Alkylation Unit (065)"
            });
            await context.SaveChangesAsync();
            Console.WriteLine("UAT-SEED: CoachCoacheeMapping Rustam->Rino ditambahkan.");
        }

        private static async Task SeedProtonTrackAssignmentAsync(ApplicationDbContext context, string rinoId, string rustamId, DateTime now)
        {
            var track = await context.ProtonTracks
                .FirstOrDefaultAsync(t => t.TrackType == "Operator" && t.TahunKe == "Tahun 1");
            if (track == null)
            {
                Console.WriteLine("UAT-SEED: ProtonTrack 'Operator Tahun 1' tidak ditemukan, skip ProtonTrackAssignment.");
                return;
            }

            if (await context.ProtonTrackAssignments.AnyAsync(a => a.CoacheeId == rinoId && a.ProtonTrackId == track.Id && a.IsActive))
            {
                Console.WriteLine("UAT-SEED: ProtonTrackAssignment Rino sudah ada, skip.");
                return;
            }

            context.ProtonTrackAssignments.Add(new ProtonTrackAssignment
            {
                CoacheeId = rinoId,
                AssignedById = rustamId,
                ProtonTrackId = track.Id,
                IsActive = true,
                AssignedAt = now
            });
            await context.SaveChangesAsync();
            Console.WriteLine("UAT-SEED: ProtonTrackAssignment Rino ditambahkan.");
        }

        private static async Task SeedAssessmentCategoriesAsync(ApplicationDbContext context)
        {
            if (await context.AssessmentCategories.AnyAsync(c => c.Name == "Assessment OJT"))
            {
                Console.WriteLine("UAT-SEED: AssessmentCategory 'Assessment OJT' sudah ada, skip.");
                return;
            }

            var parentOjt = new AssessmentCategory { Name = "Assessment OJT", DefaultPassPercentage = 70, IsActive = true, SortOrder = 1 };
            context.AssessmentCategories.Add(parentOjt);
            await context.SaveChangesAsync();

            context.AssessmentCategories.Add(new AssessmentCategory
            {
                Name = "Alkylation",
                DefaultPassPercentage = 70,
                IsActive = true,
                SortOrder = 1,
                ParentId = parentOjt.Id
            });

            var parentProton = new AssessmentCategory { Name = "Assessment Proton", DefaultPassPercentage = 70, IsActive = true, SortOrder = 2 };
            context.AssessmentCategories.Add(parentProton);

            await context.SaveChangesAsync();
            Console.WriteLine("UAT-SEED: AssessmentCategory OJT + Alkylation + Proton ditambahkan.");
        }

        private static async Task<(List<PackageQuestion> questions, AssessmentPackage package)> SeedRegularAssessmentOpenAsync(
            ApplicationDbContext context, string rinoId, string iwanId, DateTime now)
        {
            // Create AssessmentSession
            var session = new AssessmentSession
            {
                Title = "OJT Proses Alkylation Q1-2026",
                UserId = rinoId,
                Category = "Assessment OJT",
                Schedule = now.AddDays(7),
                DurationMinutes = 60,
                Status = "Open",
                PassPercentage = 70,
                AllowAnswerReview = true,
                GenerateCertificate = true,
                AccessToken = "UAT-TOKEN-001",
                IsTokenRequired = false,
                CreatedAt = now
            };
            context.AssessmentSessions.Add(session);
            await context.SaveChangesAsync();

            // Create AssessmentPackage
            var package = new AssessmentPackage
            {
                AssessmentSessionId = session.Id,
                PackageName = "Paket A",
                PackageNumber = 1
            };
            context.AssessmentPackages.Add(package);
            await context.SaveChangesAsync();

            // Create 15 questions with 4 ET merata
            var questionsData = new[]
            {
                // ET: Proses Distilasi (Q1-4)
                new { Text = "Apa fungsi utama kolom distilasi dalam unit Alkylation?", ET = "Proses Distilasi", Opts = new[] { "Memisahkan komponen berdasarkan titik didih", "Mencampur bahan kimia", "Menurunkan tekanan sistem", "Menghasilkan listrik" }, CorrectIdx = 0 },
                new { Text = "Suhu operasi normal reboiler distilasi adalah...", ET = "Proses Distilasi", Opts = new[] { "150-200°C", "50-80°C", "300-400°C", "Suhu kamar" }, CorrectIdx = 0 },
                new { Text = "Reflux ratio yang terlalu tinggi menyebabkan...", ET = "Proses Distilasi", Opts = new[] { "Konsumsi energi berlebih", "Produk lebih kotor", "Tekanan turun drastis", "Tidak berpengaruh" }, CorrectIdx = 0 },
                new { Text = "Indikator flooding pada kolom distilasi adalah...", ET = "Proses Distilasi", Opts = new[] { "Pressure drop naik tajam", "Level turun", "Suhu naik", "Flow rate stabil" }, CorrectIdx = 0 },
                // ET: Keselamatan Kerja (Q5-8)
                new { Text = "APD wajib di area Alkylation meliputi...", ET = "Keselamatan Kerja", Opts = new[] { "Helm, kacamata safety, sarung tangan asam", "Hanya helm", "Sepatu biasa dan helm", "Tidak perlu APD" }, CorrectIdx = 0 },
                new { Text = "Langkah pertama saat terjadi kebocoran HF adalah...", ET = "Keselamatan Kerja", Opts = new[] { "Evakuasi searah angin dan aktifkan alarm", "Tutup kebocoran langsung", "Lanjutkan bekerja", "Telepon keluarga" }, CorrectIdx = 0 },
                new { Text = "Frekuensi safety talk di area proses adalah...", ET = "Keselamatan Kerja", Opts = new[] { "Setiap shift change", "Sekali sebulan", "Sekali setahun", "Tidak perlu" }, CorrectIdx = 0 },
                new { Text = "Tujuan Job Safety Analysis (JSA) adalah...", ET = "Keselamatan Kerja", Opts = new[] { "Mengidentifikasi bahaya setiap langkah kerja", "Membuat laporan keuangan", "Menghitung bonus", "Mengganti peralatan" }, CorrectIdx = 0 },
                // ET: Operasi Pompa (Q9-12)
                new { Text = "Jenis pompa yang umum di unit Alkylation adalah...", ET = "Operasi Pompa", Opts = new[] { "Centrifugal pump", "Pompa tangan", "Pompa angin", "Water wheel" }, CorrectIdx = 0 },
                new { Text = "Tanda kavitasi pada pompa sentrifugal adalah...", ET = "Operasi Pompa", Opts = new[] { "Suara gemericik dan getaran abnormal", "Pompa berjalan normal", "Flow naik drastis", "Motor mati" }, CorrectIdx = 0 },
                new { Text = "Mechanical seal pada pompa berfungsi untuk...", ET = "Operasi Pompa", Opts = new[] { "Mencegah kebocoran fluida proses", "Menambah tekanan", "Mendinginkan motor", "Mempercepat putaran" }, CorrectIdx = 0 },
                new { Text = "Prosedur alignment pompa dilakukan saat...", ET = "Operasi Pompa", Opts = new[] { "Setelah maintenance atau instalasi baru", "Setiap jam", "Saat pompa berjalan", "Tidak pernah" }, CorrectIdx = 0 },
                // ET: Instrumentasi (Q13-15)
                new { Text = "Transmitter tekanan pada reaktor berfungsi untuk...", ET = "Instrumentasi", Opts = new[] { "Mengukur dan mengirim sinyal tekanan ke DCS", "Menghasilkan tekanan", "Menyimpan data manual", "Mengganti operator" }, CorrectIdx = 0 },
                new { Text = "Kalibrasi instrumen dilakukan untuk...", ET = "Instrumentasi", Opts = new[] { "Memastikan akurasi pembacaan", "Mengganti instrumen", "Mematikan sistem", "Mengurangi biaya" }, CorrectIdx = 0 },
                new { Text = "Control valve gagal membuka (fail-open) dipasang pada...", ET = "Instrumentasi", Opts = new[] { "Cooling water line", "Fuel gas line", "Vent line", "Drain line" }, CorrectIdx = 0 },
            };

            var questions = new List<PackageQuestion>();
            int order = 1;
            foreach (var qData in questionsData)
            {
                var q = new PackageQuestion
                {
                    AssessmentPackageId = package.Id,
                    QuestionText = qData.Text,
                    Order = order++,
                    ScoreValue = 10,
                    ElemenTeknis = qData.ET
                };
                questions.Add(q);
            }
            context.PackageQuestions.AddRange(questions);
            await context.SaveChangesAsync();

            // Create 4 options per question
            var allOptions = new List<PackageOption>();
            for (int i = 0; i < questions.Count; i++)
            {
                var qData = questionsData[i];
                for (int j = 0; j < qData.Opts.Length; j++)
                {
                    allOptions.Add(new PackageOption
                    {
                        PackageQuestionId = questions[i].Id,
                        OptionText = qData.Opts[j],
                        IsCorrect = j == qData.CorrectIdx
                    });
                }
            }
            context.PackageOptions.AddRange(allOptions);
            await context.SaveChangesAsync();

            // Reload questions with options for ShuffledOptionIdsPerQuestion
            var questionsWithOptions = await context.PackageQuestions
                .Where(q => q.AssessmentPackageId == package.Id)
                .Include(q => q.Options)
                .ToListAsync();

            var questionIds = questionsWithOptions.Select(q => q.Id).ToList();
            var optionIdsPerQuestion = questionsWithOptions.ToDictionary(
                q => q.Id.ToString(),
                q => q.Options.Select(o => o.Id).ToList()
            );

            // Create UserPackageAssignment for Rino and Iwan
            var assignments = new[]
            {
                new UserPackageAssignment
                {
                    AssessmentSessionId = session.Id,
                    AssessmentPackageId = package.Id,
                    UserId = rinoId,
                    ShuffledQuestionIds = JsonSerializer.Serialize(questionIds),
                    ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(optionIdsPerQuestion),
                    IsCompleted = false,
                    SavedQuestionCount = 15
                },
                new UserPackageAssignment
                {
                    AssessmentSessionId = session.Id,
                    AssessmentPackageId = package.Id,
                    UserId = iwanId,
                    ShuffledQuestionIds = JsonSerializer.Serialize(questionIds),
                    ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(optionIdsPerQuestion),
                    IsCompleted = false,
                    SavedQuestionCount = 15
                }
            };
            context.UserPackageAssignments.AddRange(assignments);
            await context.SaveChangesAsync();

            Console.WriteLine($"UAT-SEED: Assessment reguler 'OJT Proses Alkylation Q1-2026' dibuat (sessionId={session.Id}, {questions.Count} soal, 2 assignment).");
            return (questionsWithOptions, package);
        }

        private static Task SeedCompletedAssessmentPassAsync(ApplicationDbContext context, string rinoId, DateTime now, List<PackageQuestion> questions)
        {
            // Stub — akan diimplementasi Plan 02
            Console.WriteLine("UAT-SEED: SeedCompletedAssessmentPassAsync — stub (Plan 02).");
            return Task.CompletedTask;
        }

        private static Task SeedCompletedAssessmentFailAsync(ApplicationDbContext context, string rinoId, DateTime now, List<PackageQuestion> questions)
        {
            // Stub — akan diimplementasi Plan 02
            Console.WriteLine("UAT-SEED: SeedCompletedAssessmentFailAsync — stub (Plan 02).");
            return Task.CompletedTask;
        }

        private static Task SeedProtonAssessmentsAsync(ApplicationDbContext context, string rinoId, DateTime now)
        {
            // Stub — akan diimplementasi Plan 02
            Console.WriteLine("UAT-SEED: SeedProtonAssessmentsAsync — stub (Plan 02).");
            return Task.CompletedTask;
        }

        // =====================================================================

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
