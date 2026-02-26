using Microsoft.EntityFrameworkCore;
using HcPortal.Models;

namespace HcPortal.Data
{
    /// <summary>
    /// Class untuk seeding data master (KKJ Matrix & CPDP Items)
    /// </summary>
    public static class SeedMasterData
    {
        public static async Task SeedKkjMatrixAsync(ApplicationDbContext context)
        {
            // Skip if already seeded
            if (await context.KkjMatrices.AnyAsync())
            {
                Console.WriteLine("ℹ️ KKJ Matrix data already exists, skipping...");
                return;
            }

            var kkjData = new List<KkjMatrixItem>
            {
                // 1. Gas Processing Operations
                new KkjMatrixItem { No=1, SkillGroup="Engineering", SubSkillGroup="Production & Processing Operations and Maintenance", Indeks="6.2.2", Kompetensi="Gas Processing Operations", 
                    Target_SectionHead="-", Target_SrSpv_GSH="2", Target_ShiftSpv_GSH="2", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="2", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="2", Target_Panelman_ARU_12_13="2", Target_Panelman_ARU_14="2", Target_Operator_ARU_8_11="1", Target_Operator_ARU_12_13="2", 
                    Target_SrSpv_Facility="2", Target_JrAnalyst="-", Target_HSE="2" },

                // 2. Material & Chemical Blending
                new KkjMatrixItem { No=2, SkillGroup="Engineering", SubSkillGroup="Production & Processing Operations and Maintenance", Indeks="6.2.5", Kompetensi="Material & Chemical Blending", 
                    Target_SectionHead="-", Target_SrSpv_GSH="2", Target_ShiftSpv_GSH="2", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="1", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="2", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="1", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="2", Target_JrAnalyst="-", Target_HSE="1" },

                // 3. Oil Processing Operations
                new KkjMatrixItem { No=3, SkillGroup="Engineering", SubSkillGroup="Production & Processing Operations and Maintenance", Indeks="6.2.6", Kompetensi="Oil Processing Operations", 
                    Target_SectionHead="3", Target_SrSpv_GSH="2", Target_ShiftSpv_GSH="2", 
                    Target_Panelman_GSH_12_13="2", Target_Panelman_GSH_14="2", Target_Operator_GSH_8_11="1", Target_Operator_GSH_12_13="2",
                    Target_ShiftSpv_ARU="2", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="2", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="2", Target_JrAnalyst="-", Target_HSE="2" },

                // 4. Refinery Process Operations
                new KkjMatrixItem { No=4, SkillGroup="Engineering", SubSkillGroup="Production & Processing Operations and Maintenance", Indeks="6.2.7", Kompetensi="Refinery Process Operations", 
                    Target_SectionHead="3", Target_SrSpv_GSH="3", Target_ShiftSpv_GSH="3", 
                    Target_Panelman_GSH_12_13="2", Target_Panelman_GSH_14="3", Target_Operator_GSH_8_11="2", Target_Operator_GSH_12_13="2",
                    Target_ShiftSpv_ARU="3", Target_Panelman_ARU_12_13="2", Target_Panelman_ARU_14="3", Target_Operator_ARU_8_11="2", Target_Operator_ARU_12_13="2", 
                    Target_SrSpv_Facility="3", Target_JrAnalyst="2", Target_HSE="3" },

                // 5. Catalyst & Chemical Management
                new KkjMatrixItem { No=5, SkillGroup="Engineering", SubSkillGroup="Process Engineering", Indeks="7.1.1", Kompetensi="Catalyst & Chemical Management", 
                    Target_SectionHead="-", Target_SrSpv_GSH="2", Target_ShiftSpv_GSH="2", 
                    Target_Panelman_GSH_12_13="1", Target_Panelman_GSH_14="1", Target_Operator_GSH_8_11="1", Target_Operator_GSH_12_13="1",
                    Target_ShiftSpv_ARU="1", Target_Panelman_ARU_12_13="1", Target_Panelman_ARU_14="1", Target_Operator_ARU_8_11="1", Target_Operator_ARU_12_13="2", 
                    Target_SrSpv_Facility="1", Target_JrAnalyst="-", Target_HSE="-" },

                // 6. Energy Management
                new KkjMatrixItem { No=6, SkillGroup="Engineering", SubSkillGroup="Process Engineering", Indeks="7.1.2", Kompetensi="Energy Management", 
                    Target_SectionHead="3", Target_SrSpv_GSH="3", Target_ShiftSpv_GSH="3", 
                    Target_Panelman_GSH_12_13="2", Target_Panelman_GSH_14="3", Target_Operator_GSH_8_11="2", Target_Operator_GSH_12_13="2",
                    Target_ShiftSpv_ARU="1", Target_Panelman_ARU_12_13="2", Target_Panelman_ARU_14="3", Target_Operator_ARU_8_11="2", Target_Operator_ARU_12_13="2", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="2", Target_HSE="-" },

                // 7. Process Control
                new KkjMatrixItem { No=7, SkillGroup="Engineering", SubSkillGroup="Process Engineering", Indeks="7.1.4", Kompetensi="Process Control", 
                    Target_SectionHead="-", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="2", 
                    Target_Panelman_GSH_12_13="1", Target_Panelman_GSH_14="2", Target_Operator_GSH_8_11="1", Target_Operator_GSH_12_13="1",
                    Target_ShiftSpv_ARU="2", Target_Panelman_ARU_12_13="1", Target_Panelman_ARU_14="2", Target_Operator_ARU_8_11="1", Target_Operator_ARU_12_13="1", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="-", Target_HSE="-" },

                // 8. Commissioning & Operational Readiness
                new KkjMatrixItem { No=8, SkillGroup="HSSE", SubSkillGroup="Project Engineering", Indeks="7.7.1", Kompetensi="Commissioning & Operational Readiness", 
                    Target_SectionHead="3", Target_SrSpv_GSH="3", Target_ShiftSpv_GSH="2", 
                    Target_Panelman_GSH_12_13="2", Target_Panelman_GSH_14="2", Target_Operator_GSH_8_11="2", Target_Operator_GSH_12_13="2",
                    Target_ShiftSpv_ARU="2", Target_Panelman_ARU_12_13="2", Target_Panelman_ARU_14="2", Target_Operator_ARU_8_11="2", Target_Operator_ARU_12_13="2", 
                    Target_SrSpv_Facility="3", Target_JrAnalyst="2", Target_HSE="2" },
                
                // 9. Cost Engineering
                new KkjMatrixItem { No=9, SkillGroup="HSSE", SubSkillGroup="Project Engineering", Indeks="7.7.2", Kompetensi="Cost Engineering", 
                    Target_SectionHead="2", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="2", Target_JrAnalyst="1", Target_HSE="-" },

                // 10. Sourcing Procurement
                new KkjMatrixItem { No=10, SkillGroup="Operation & Maintenance", SubSkillGroup="Procurement", Indeks="9.2.3", Kompetensi="Sourcing Procurement", 
                    Target_SectionHead="2", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="-", Target_HSE="-" },

                // 11. Process Hazard Analysis
                new KkjMatrixItem { No=11, SkillGroup="Operation & Maintenance", SubSkillGroup="Safety", Indeks="12.2.12", Kompetensi="Process Hazard Analysis", 
                    Target_SectionHead="2", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="-", Target_HSE="2" },

                // 12. Process Safety Management
                new KkjMatrixItem { No=12, SkillGroup="Operation & Maintenance", SubSkillGroup="Safety", Indeks="12.2.13", Kompetensi="Process Safety Management", 
                    Target_SectionHead="-", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="-", Target_HSE="1" },

                // 13. Safe Work Practice
                new KkjMatrixItem { No=13, SkillGroup="Operation & Maintenance", SubSkillGroup="Safety", Indeks="12.2.14", Kompetensi="Safe Work Practice & Lifesaving Rules", 
                    Target_SectionHead="3", Target_SrSpv_GSH="3", Target_ShiftSpv_GSH="3", 
                    Target_Panelman_GSH_12_13="2", Target_Panelman_GSH_14="2", Target_Operator_GSH_8_11="2", Target_Operator_GSH_12_13="2",
                    Target_ShiftSpv_ARU="3", Target_Panelman_ARU_12_13="2", Target_Panelman_ARU_14="2", Target_Operator_ARU_8_11="2", Target_Operator_ARU_12_13="2", 
                    Target_SrSpv_Facility="3", Target_JrAnalyst="2", Target_HSE="3" },

                // 14. Incident Investigation
                new KkjMatrixItem { No=14, SkillGroup="Operation & Maintenance", SubSkillGroup="Safety", Indeks="12.2.9", Kompetensi="Incident Investigation", 
                    Target_SectionHead="-", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="-", Target_HSE="1" },

                // 15. Individual Performance Management
                new KkjMatrixItem { No=15, SkillGroup="Operation & Maintenance", SubSkillGroup="People Management", Indeks="13.2.6", Kompetensi="Individual Performance Management", 
                    Target_SectionHead="2", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="-", Target_HSE="-" },

                // 16. Manpower Planning
                new KkjMatrixItem { No=16, SkillGroup="Operation & Maintenance", SubSkillGroup="People Management", Indeks="13.2.8", Kompetensi="Manpower Planning", 
                    Target_SectionHead="2", Target_SrSpv_GSH="2", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="-", Target_HSE="-" },

                // 17. Project Risk Management
                new KkjMatrixItem { No=17, SkillGroup="Operation & Maintenance", SubSkillGroup="Risk Management", Indeks="15.1.3", Kompetensi="Project Risk Management", 
                    Target_SectionHead="2", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="2", Target_JrAnalyst="1", Target_HSE="-" }
            };

            await context.KkjMatrices.AddRangeAsync(kkjData);
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ Seeded {kkjData.Count} KKJ Matrix items successfully!");
        }

        public static async Task SeedCpdpItemsAsync(ApplicationDbContext context)
        {
            // Skip if already seeded
            if (await context.CpdpItems.AnyAsync())
            {
                Console.WriteLine("ℹ️ CPDP Items data already exists, skipping...");
                return;
            }

            var cpdpData = new List<CpdpItem>
            {
                // 1. Safe Work Practice & Lifesaving Rules
                new CpdpItem { No="1", NamaKompetensi="Safe Work Practice & Lifesaving Rules", IndikatorPerilaku="2 (Intermediate)",
                    DetailIndikator="Memahami dan mampu menerapkan cara kerja yang aman (safe work practice & lifesaving rules) sesuai dengan risiko keselamatan terkait aktivitasnya",
                    Silabus="1.1. Safe Work Practice",
                    TargetDeliverable="Target 1.1\n1. Mampu memahami 5 Tingkatan Budaya HSSE.\n2. Mampu memahami Pengertian Bahaya menurut standar ISO & OSHA.\n3. Mampu memahami Lessons Learned.\n4. Mampu memahami 9 Perilaku Wajib.\n5. Mampu memahami Grafik Flammable Range.\n6. Mampu memahami Pengamatan Keselamatan Kerja (PEKA)\n7. Mampu memahami Skill Champion SLP",
                    Section = "RFCC" },

                new CpdpItem { No="1", NamaKompetensi="Safe Work Practice & Lifesaving Rules", IndikatorPerilaku="2 (Intermediate)",
                    DetailIndikator="Memahami dan mampu menerapkan cara kerja yang aman",
                    Silabus="1.2. Lifesaving Rules",
                    TargetDeliverable="Target 1.2\n1. Mengetahui Regulasi Pemerintah tentang HSSE\n2. Mampu memahami HSSE Golden Rules\n3. Mampu memahami Safety Data Sheet\n4. Mampu memahami 10 Corporate Life Saving Rules (CLSR) 2024\n5. Mampu memahami Pembuatan Job Safety Analysis (JSA)\n6. Mampu memahami Prosedur & Pembuatan SIKA",
                    Section = "RFCC" },

                new CpdpItem { No="1", NamaKompetensi="Safe Work Practice & Lifesaving Rules", IndikatorPerilaku="2 (Intermediate)",
                    DetailIndikator="Memahami dan mampu menerapkan penanggulangan apabila terjadi kondisi darurat",
                    Silabus="1.3. Emergency Response",
                    TargetDeliverable="Target 1.3\n1. Mampu memahami Metode Pemadaman Kebakaran\n2. Mampu memahami Media Pemadam Kebakaran\n3. Mampu memahami Prosedur Keadaan Darurat\n4. Mampu memahami Jenis-jenis Peralatan Proteksi Kebakaran",
                    Section = "RFCC" },

                // 2. Energy Management
                new CpdpItem { No="2", NamaKompetensi="Energy Management", IndikatorPerilaku="2 (Intermediate)",
                    DetailIndikator="Memahami karakteristik energi yang digunakan (listrik, steam, fuel oil, fuel gas, dll.)",
                    Silabus="2.1. Karakteristik Energi",
                    TargetDeliverable="Target 2.1\n1. Mengetahui sumber fuel gas dan spesifikasinya.\n2. Mengetahui parameter kondisi operasi fuel gas.\n3. Mengetahui parameter kondisi operasi HDS Heater.",
                    Section = "RFCC" },

                new CpdpItem { No="2", NamaKompetensi="Energy Management", IndikatorPerilaku="2 (Intermediate)",
                    DetailIndikator="Memahami prinsip - prinsip dasar equipment yang menggunakan energi",
                    Silabus="2.2. Prinsip Dasar Peralatan Energi",
                    TargetDeliverable="Target 2.2\n1. Mampu memahami fungsi dan prinsip kerja dari HDS Heater & Splitter Reboiler.\n2. Mampu memahami sistem pengaman (Interlock, Alarm, dan Permissive) pada HDS Heater.",
                    Section = "RFCC" },

                // 3. Catalyst & Chemical Management
                new CpdpItem { No="3", NamaKompetensi="Catalyst & Chemical Management", IndikatorPerilaku="1 (Basic)",
                    DetailIndikator="Memahami jenis-jenis dan fungsi catalyst dan chemical",
                    Silabus="3.1. Jenis & Fungsi Catalyst & Chemical",
                    TargetDeliverable="Target 3.1\n1. Memahami fungsi catalyst dan chemical pada unit RFCC NHT",
                    Section = "RFCC" },

                new CpdpItem { No="3", NamaKompetensi="Catalyst & Chemical Management", IndikatorPerilaku="1 (Basic)",
                    DetailIndikator="Memahami karakteristik/ performance catalyst dan chemical",
                    Silabus="3.2. Karakteristik Catalyst & Chemical",
                    TargetDeliverable="Target 3.2\n1. Mampu menjelaskan karakteristik catalyst dan chemical di unit RFCC NHT\n2. Mampu menjelaskan parameter teknis yang mempengaruhi performance catalyst",
                    Section = "RFCC" },

                // 4. Process Control
                new CpdpItem { No="4", NamaKompetensi="Process Control & Computer Operations", IndikatorPerilaku="1 (Basic)",
                    DetailIndikator="Memahami prinsip-prinsip dasar pengendalian dan pengukuran variabel-variabel operasi",
                    Silabus="4.1. Prinsip Dasar Pengendalian & Pengukuran",
                    TargetDeliverable="Target 4.1\n1. Mampu memahami Basic Process Control, Cascade Control, Split Range Control\n2. Mampu memahami field instrument pada unit proses\n3. Mampu memahami istilah-istilah umum di HMI DCS",
                    Section = "RFCC" },

                // 5. Refinery Process Operations
                new CpdpItem { No="5.1", NamaKompetensi="Refinery Process Operations", IndikatorPerilaku="2 (Intermediate)",
                    DetailIndikator="Memahami prinsip-prinsip dasar dan mampu menjalankan prosedur pengoperasian fasilitas refinery process",
                    Silabus="5.5. Prinsip Dasar & Pengoperasian Fasilitas Kilang",
                    TargetDeliverable="Target 5.5\n1. Memahami distribusi dan fungsi utilitas di unit RFCC NHT.\n2. Mampu membuat block diagram dan PFD unit RFCC NHT.\n3. Mampu memahami deskripsi proses RFCC NHT",
                    Section = "RFCC" },

                new CpdpItem { No="5.2", NamaKompetensi="Oil Processing Operations", IndikatorPerilaku="1 (Basic)",
                    DetailIndikator="Memahami prinsip-prinsip dasar pengoperasian fasilitas oil processing",
                    Silabus="5.7. Prinsip Dasar & Pengoperasian Peralatan",
                    TargetDeliverable="Target 5.7\n1. Mampu menjelaskan prinsip dasar kerja masing-masing peralatan pada unit RFCC NHT.\n2. Mampu menjelaskan troubleshooting equipment critical",
                    Section = "RFCC" }
            };

            await context.CpdpItems.AddRangeAsync(cpdpData);
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ Seeded {cpdpData.Count} CPDP items successfully!");
        }

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
