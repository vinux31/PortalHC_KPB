using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Helpers;

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

            // UserPackageAssignment NOT pre-created — system builds it on exam start
            // with proper Fisher-Yates shuffle + cross-package distribution via BuildCrossPackageAssignment()

            Console.WriteLine($"UAT-SEED: Assessment reguler 'OJT Proses Alkylation Q1-2026' dibuat (sessionId={session.Id}, {questions.Count} soal, assignment dibuat saat exam start).");
            return (questionsWithOptions, package);
        }

        private static async Task SeedCompletedAssessmentPassAsync(ApplicationDbContext context, string rinoId, DateTime now, List<PackageQuestion> questions)
        {
            var certDate = now.AddDays(-30);

            // 1. Buat AssessmentSession lulus
            var session = new AssessmentSession
            {
                Title = "OJT Proses Alkylation Q4-2025 (Lulus)",
                UserId = rinoId,
                Category = "Assessment OJT",
                Schedule = certDate,
                DurationMinutes = 60,
                Status = "Completed",
                Progress = 100,
                Score = 80,
                PassPercentage = 70,
                IsPassed = true,
                AllowAnswerReview = true,
                GenerateCertificate = true,
                StartedAt = certDate,
                CompletedAt = certDate.AddMinutes(45),
                AccessToken = "UAT-TOKEN-PASS",
                IsTokenRequired = false,
                CreatedAt = certDate
            };
            context.AssessmentSessions.Add(session);
            await context.SaveChangesAsync();

            // 2. Generate NomorSertifikat
            var nextSeq = await CertNumberHelper.GetNextSeqAsync(context, certDate.Year);
            session.NomorSertifikat = CertNumberHelper.Build(nextSeq, certDate);
            session.ValidUntil = certDate.AddYears(1);
            await context.SaveChangesAsync();

            // 3. Buat AssessmentPackage + copy soal
            var package = new AssessmentPackage
            {
                AssessmentSessionId = session.Id,
                PackageName = "Paket A",
                PackageNumber = 1
            };
            context.AssessmentPackages.Add(package);
            await context.SaveChangesAsync();

            var newQuestions = new List<PackageQuestion>();
            foreach (var orig in questions)
            {
                var q = new PackageQuestion
                {
                    AssessmentPackageId = package.Id,
                    QuestionText = orig.QuestionText,
                    Order = orig.Order,
                    ScoreValue = orig.ScoreValue,
                    ElemenTeknis = orig.ElemenTeknis
                };
                newQuestions.Add(q);
            }
            context.PackageQuestions.AddRange(newQuestions);
            await context.SaveChangesAsync();

            // Copy options per soal
            for (int i = 0; i < questions.Count; i++)
            {
                foreach (var origOpt in questions[i].Options)
                {
                    context.PackageOptions.Add(new PackageOption
                    {
                        PackageQuestionId = newQuestions[i].Id,
                        OptionText = origOpt.OptionText,
                        IsCorrect = origOpt.IsCorrect
                    });
                }
            }
            await context.SaveChangesAsync();

            // Reload newQuestions with options
            var newQsWithOptions = await context.PackageQuestions
                .Where(q => q.AssessmentPackageId == package.Id)
                .Include(q => q.Options)
                .ToListAsync();

            // 4. UserPackageAssignment untuk Rino
            var qIds = newQsWithOptions.Select(q => q.Id).ToList();
            var optIdsMap = newQsWithOptions.ToDictionary(
                q => q.Id.ToString(),
                q => q.Options.Select(o => o.Id).ToList()
            );
            context.UserPackageAssignments.Add(new UserPackageAssignment
            {
                AssessmentSessionId = session.Id,
                AssessmentPackageId = package.Id,
                UserId = rinoId,
                IsCompleted = true,
                SavedQuestionCount = 15,
                ShuffledQuestionIds = JsonSerializer.Serialize(qIds),
                ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(optIdsMap)
            });
            await context.SaveChangesAsync();

            // 5. PackageUserResponse — distribusi 12 benar dari 15 (skor 80)
            // ET "Proses Distilasi" (Q0-3): 3 benar, 1 salah (Q3 salah)
            // ET "Keselamatan Kerja" (Q4-7): 3 benar, 1 salah (Q7 salah)
            // ET "Operasi Pompa" (Q8-11): 3 benar, 1 salah (Q11 salah)
            // ET "Instrumentasi" (Q12-14): 3 benar, 0 salah
            var passCorrectMap = new bool[] {
                true, true, true, false,   // Proses Distilasi
                true, true, true, false,   // Keselamatan Kerja
                true, true, true, false,   // Operasi Pompa
                true, true, true           // Instrumentasi
            };
            for (int i = 0; i < newQsWithOptions.Count; i++)
            {
                var q = newQsWithOptions[i];
                var shouldBeCorrect = i < passCorrectMap.Length && passCorrectMap[i];
                var chosen = shouldBeCorrect
                    ? q.Options.First(o => o.IsCorrect)
                    : q.Options.First(o => !o.IsCorrect);
                context.PackageUserResponses.Add(new PackageUserResponse
                {
                    AssessmentSessionId = session.Id,
                    PackageQuestionId = q.Id,
                    PackageOptionId = chosen.Id,
                    SubmittedAt = session.CompletedAt!.Value
                });
            }
            await context.SaveChangesAsync();

            // 6. SessionElemenTeknisScore
            context.SessionElemenTeknisScores.AddRange(new[]
            {
                new SessionElemenTeknisScore { AssessmentSessionId = session.Id, ElemenTeknis = "Proses Distilasi", CorrectCount = 3, QuestionCount = 4 },
                new SessionElemenTeknisScore { AssessmentSessionId = session.Id, ElemenTeknis = "Keselamatan Kerja", CorrectCount = 3, QuestionCount = 4 },
                new SessionElemenTeknisScore { AssessmentSessionId = session.Id, ElemenTeknis = "Operasi Pompa", CorrectCount = 3, QuestionCount = 4 },
                new SessionElemenTeknisScore { AssessmentSessionId = session.Id, ElemenTeknis = "Instrumentasi", CorrectCount = 3, QuestionCount = 3 },
            });
            await context.SaveChangesAsync();

            // 7. AssessmentAttemptHistory
            context.AssessmentAttemptHistory.Add(new AssessmentAttemptHistory
            {
                SessionId = session.Id,
                UserId = rinoId,
                Title = session.Title,
                Category = session.Category,
                Score = 80,
                IsPassed = true,
                StartedAt = session.StartedAt,
                CompletedAt = session.CompletedAt,
                AttemptNumber = 1,
                ArchivedAt = session.CompletedAt!.Value
            });
            await context.SaveChangesAsync();

            Console.WriteLine($"UAT-SEED: Completed assessment LULUS '{session.Title}' dibuat (sertifikat: {session.NomorSertifikat}).");
        }

        private static async Task SeedCompletedAssessmentFailAsync(ApplicationDbContext context, string rinoId, DateTime now, List<PackageQuestion> questions)
        {
            var failDate = now.AddDays(-60);

            // 1. Buat AssessmentSession gagal
            var session = new AssessmentSession
            {
                Title = "OJT Proses Alkylation Q3-2025 (Gagal)",
                UserId = rinoId,
                Category = "Assessment OJT",
                Schedule = failDate,
                DurationMinutes = 60,
                Status = "Completed",
                Progress = 100,
                Score = 40,
                PassPercentage = 70,
                IsPassed = false,
                AllowAnswerReview = true,
                GenerateCertificate = false,
                StartedAt = failDate,
                CompletedAt = failDate.AddMinutes(30),
                AccessToken = "UAT-TOKEN-FAIL",
                IsTokenRequired = false,
                CreatedAt = failDate
            };
            context.AssessmentSessions.Add(session);
            await context.SaveChangesAsync();
            // TANPA NomorSertifikat dan TANPA ValidUntil

            // 2. Buat AssessmentPackage + copy soal
            var package = new AssessmentPackage
            {
                AssessmentSessionId = session.Id,
                PackageName = "Paket A",
                PackageNumber = 1
            };
            context.AssessmentPackages.Add(package);
            await context.SaveChangesAsync();

            var newQuestions = new List<PackageQuestion>();
            foreach (var orig in questions)
            {
                var q = new PackageQuestion
                {
                    AssessmentPackageId = package.Id,
                    QuestionText = orig.QuestionText,
                    Order = orig.Order,
                    ScoreValue = orig.ScoreValue,
                    ElemenTeknis = orig.ElemenTeknis
                };
                newQuestions.Add(q);
            }
            context.PackageQuestions.AddRange(newQuestions);
            await context.SaveChangesAsync();

            // Copy options per soal
            for (int i = 0; i < questions.Count; i++)
            {
                foreach (var origOpt in questions[i].Options)
                {
                    context.PackageOptions.Add(new PackageOption
                    {
                        PackageQuestionId = newQuestions[i].Id,
                        OptionText = origOpt.OptionText,
                        IsCorrect = origOpt.IsCorrect
                    });
                }
            }
            await context.SaveChangesAsync();

            // Reload newQuestions with options
            var newQsWithOptions = await context.PackageQuestions
                .Where(q => q.AssessmentPackageId == package.Id)
                .Include(q => q.Options)
                .ToListAsync();

            // 3. UserPackageAssignment untuk Rino
            var qIds = newQsWithOptions.Select(q => q.Id).ToList();
            var optIdsMap = newQsWithOptions.ToDictionary(
                q => q.Id.ToString(),
                q => q.Options.Select(o => o.Id).ToList()
            );
            context.UserPackageAssignments.Add(new UserPackageAssignment
            {
                AssessmentSessionId = session.Id,
                AssessmentPackageId = package.Id,
                UserId = rinoId,
                IsCompleted = true,
                SavedQuestionCount = 15,
                ShuffledQuestionIds = JsonSerializer.Serialize(qIds),
                ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(optIdsMap)
            });
            await context.SaveChangesAsync();

            // 4. PackageUserResponse — distribusi 6 benar dari 15 (skor 40)
            // ET "Proses Distilasi" (Q0-3): 2 benar, 2 salah (Q0,Q1 benar; Q2,Q3 salah)
            // ET "Keselamatan Kerja" (Q4-7): 1 benar, 3 salah (Q4 benar; Q5,Q6,Q7 salah)
            // ET "Operasi Pompa" (Q8-11): 2 benar, 2 salah (Q8,Q9 benar; Q10,Q11 salah)
            // ET "Instrumentasi" (Q12-14): 1 benar, 2 salah (Q12 benar; Q13,Q14 salah)
            var failCorrectMap = new bool[] {
                true, true, false, false,   // Proses Distilasi
                true, false, false, false,  // Keselamatan Kerja
                true, true, false, false,   // Operasi Pompa
                true, false, false          // Instrumentasi
            };
            for (int i = 0; i < newQsWithOptions.Count; i++)
            {
                var q = newQsWithOptions[i];
                var shouldBeCorrect = i < failCorrectMap.Length && failCorrectMap[i];
                var chosen = shouldBeCorrect
                    ? q.Options.First(o => o.IsCorrect)
                    : q.Options.First(o => !o.IsCorrect);
                context.PackageUserResponses.Add(new PackageUserResponse
                {
                    AssessmentSessionId = session.Id,
                    PackageQuestionId = q.Id,
                    PackageOptionId = chosen.Id,
                    SubmittedAt = session.CompletedAt!.Value
                });
            }
            await context.SaveChangesAsync();

            // 5. SessionElemenTeknisScore
            context.SessionElemenTeknisScores.AddRange(new[]
            {
                new SessionElemenTeknisScore { AssessmentSessionId = session.Id, ElemenTeknis = "Proses Distilasi", CorrectCount = 2, QuestionCount = 4 },
                new SessionElemenTeknisScore { AssessmentSessionId = session.Id, ElemenTeknis = "Keselamatan Kerja", CorrectCount = 1, QuestionCount = 4 },
                new SessionElemenTeknisScore { AssessmentSessionId = session.Id, ElemenTeknis = "Operasi Pompa", CorrectCount = 2, QuestionCount = 4 },
                new SessionElemenTeknisScore { AssessmentSessionId = session.Id, ElemenTeknis = "Instrumentasi", CorrectCount = 1, QuestionCount = 3 },
            });
            await context.SaveChangesAsync();

            // 6. AssessmentAttemptHistory
            context.AssessmentAttemptHistory.Add(new AssessmentAttemptHistory
            {
                SessionId = session.Id,
                UserId = rinoId,
                Title = session.Title,
                Category = session.Category,
                Score = 40,
                IsPassed = false,
                StartedAt = session.StartedAt,
                CompletedAt = session.CompletedAt,
                AttemptNumber = 1,
                ArchivedAt = session.CompletedAt!.Value
            });
            await context.SaveChangesAsync();

            Console.WriteLine($"UAT-SEED: Completed assessment GAGAL '{session.Title}' dibuat (tanpa sertifikat).");
        }

        private static async Task SeedProtonAssessmentsAsync(ApplicationDbContext context, string rinoId, DateTime now)
        {
            // 1. Lookup ProtonTrack Tahun 1
            var trackT1 = await context.ProtonTracks.FirstOrDefaultAsync(t => t.TahunKe == "Tahun 1");
            if (trackT1 == null)
            {
                Console.WriteLine("UAT-SEED: ProtonTrack Tahun 1 tidak ditemukan, skip Proton assessments.");
                return;
            }

            // 2. Buat AssessmentSession Proton Tahun 1
            var sessionT1 = new AssessmentSession
            {
                Title = "Assessment Proton Tahun 1",
                UserId = rinoId,
                Category = "Assessment Proton",
                Schedule = now.AddDays(14),
                DurationMinutes = 90,
                Status = "Open",
                PassPercentage = 70,
                AllowAnswerReview = true,
                GenerateCertificate = false,
                AccessToken = "UAT-PROTON-T1",
                IsTokenRequired = false,
                CreatedAt = now,
                ProtonTrackId = trackT1.Id,
                TahunKe = "Tahun 1"
            };
            context.AssessmentSessions.Add(sessionT1);
            await context.SaveChangesAsync();

            // 3. Lookup ProtonTrack Tahun 3
            var trackT3 = await context.ProtonTracks.FirstOrDefaultAsync(t => t.TahunKe == "Tahun 3");
            if (trackT3 == null)
            {
                Console.WriteLine("UAT-SEED: ProtonTrack Tahun 3 tidak ditemukan, skip Proton Tahun 3.");
            }
            else
            {
                // 4. Buat AssessmentSession Proton Tahun 3
                var sessionT3 = new AssessmentSession
                {
                    Title = "Assessment Proton Tahun 3",
                    UserId = rinoId,
                    Category = "Assessment Proton",
                    Schedule = now.AddDays(21),
                    DurationMinutes = 120,
                    Status = "Open",
                    PassPercentage = 70,
                    AllowAnswerReview = false,
                    GenerateCertificate = false,
                    AccessToken = "UAT-PROTON-T3",
                    IsTokenRequired = false,
                    CreatedAt = now,
                    ProtonTrackId = trackT3.Id,
                    TahunKe = "Tahun 3"
                };
                context.AssessmentSessions.Add(sessionT3);
                await context.SaveChangesAsync();
            }

            Console.WriteLine("UAT-SEED: Assessment Proton Tahun 1 + Tahun 3 untuk Rino selesai.");
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
