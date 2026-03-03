using Microsoft.EntityFrameworkCore;
using HcPortal.Models;

namespace HcPortal.Data
{
    /// <summary>
    /// Class untuk seeding data master.
    /// Note: KKJ Matrix seed data removed in Phase 90 — KkjMatrices table dropped.
    /// Note: CpdpItems seed data removed in Phase 93 — CpdpItems table dropped.
    /// </summary>
    public static class SeedMasterData
    {
        public static async Task SeedSampleTrainingRecordsAsync(ApplicationDbContext context)
        {
            // Skip if already seeded
            if (await context.TrainingRecords.AnyAsync())
            {
                Console.WriteLine("ℹ️ Training Records data already exists, skipping...");
                return;
            }

            // Get sample users
            var sampleUsers = await context.Users.Take(3).ToListAsync();
            if (!sampleUsers.Any())
            {
                Console.WriteLine("⚠️ No users found for training records seeding.");
                return;
            }

            var trainingRecords = new List<TrainingRecord>();
            
            foreach (var user in sampleUsers)
            {
                // Add sample training records for each user
                trainingRecords.AddRange(new[]
                {
                    new TrainingRecord 
                    { 
                        UserId = user.Id,
                        Judul = "Basic Fire Fighting & Emergency Response", 
                        Kategori = "MANDATORY", 
                        Tanggal = new DateTime(2024, 2, 10),
                        Penyelenggara = "External - HSSE Provider",
                        Status = "Valid",
                        CertificateType = "Annual",
                        ValidUntil = new DateTime(2025, 2, 10),
                        SertifikatUrl = "/certificates/hsse-fire-2024.pdf"
                    },
                    new TrainingRecord 
                    { 
                        UserId = user.Id,
                        Judul = "PROTON Assessment: Distillation Unit Operations", 
                        Kategori = "Proton", 
                        Tanggal = new DateTime(2024, 11, 15),
                        Penyelenggara = "NSO",
                        Status = "Passed",
                        CertificateType = "Permanent",
                        SertifikatUrl = "/certificates/proton-distillation-2024.pdf"
                    },
                    new TrainingRecord 
                    { 
                        UserId = user.Id,
                        Judul = "On Job Training: Panel Operator Competency", 
                        Kategori = "OJT", 
                        Tanggal = new DateTime(2024, 9, 12),
                        Penyelenggara = "Internal",
                        Status = "Passed",
                        CertificateType = "Permanent",
                        SertifikatUrl = "/certificates/ojt-panel-2024.pdf"
                    }
                });
            }

            await context.TrainingRecords.AddRangeAsync(trainingRecords);
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ Seeded {trainingRecords.Count} Training Records for {sampleUsers.Count} users successfully!");
        }
    }
}
