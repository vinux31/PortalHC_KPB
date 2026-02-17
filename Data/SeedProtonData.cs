using Microsoft.EntityFrameworkCore;
using HcPortal.Models;

namespace HcPortal.Data
{
    /// <summary>
    /// Seeds the Proton master deliverable hierarchy (Kompetensi > SubKompetensi > Deliverable).
    /// Idempotent: skips if data already exists.
    /// </summary>
    public static class SeedProtonData
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Skip if already seeded
            if (await context.ProtonKompetensiList.AnyAsync())
            {
                Console.WriteLine("ℹ️ Proton Kompetensi data already exists, skipping...");
                return;
            }

            // ================================================================
            // Operator Tahun 1 — real data
            // ================================================================

            // K1: Safe Work Practice & Lifesaving Rules
            var k1 = new ProtonKompetensi
            {
                NamaKompetensi = "Safe Work Practice & Lifesaving Rules",
                TrackType = "Operator",
                TahunKe = "Tahun 1",
                Urutan = 1,
                SubKompetensiList = new List<ProtonSubKompetensi>
                {
                    new ProtonSubKompetensi
                    {
                        NamaSubKompetensi = "Safe Work Practice",
                        Urutan = 1,
                        Deliverables = new List<ProtonDeliverable>
                        {
                            new ProtonDeliverable { NamaDeliverable = "Mampu memahami 5 Tingkatan Budaya HSSE", Urutan = 1 },
                            new ProtonDeliverable { NamaDeliverable = "Mampu memahami Pengertian Bahaya", Urutan = 2 },
                            new ProtonDeliverable { NamaDeliverable = "Mampu memahami 9 Perilaku Wajib", Urutan = 3 },
                        }
                    },
                    new ProtonSubKompetensi
                    {
                        NamaSubKompetensi = "Lifesaving Rules",
                        Urutan = 2,
                        Deliverables = new List<ProtonDeliverable>
                        {
                            new ProtonDeliverable { NamaDeliverable = "Mampu memahami Lifesaving Rules Pertamina", Urutan = 1 },
                            new ProtonDeliverable { NamaDeliverable = "Mampu menerapkan Lifesaving Rules di area kerja", Urutan = 2 },
                        }
                    }
                }
            };

            // K2: Dasar Operasi Kilang
            var k2 = new ProtonKompetensi
            {
                NamaKompetensi = "Dasar Operasi Kilang",
                TrackType = "Operator",
                TahunKe = "Tahun 1",
                Urutan = 2,
                SubKompetensiList = new List<ProtonSubKompetensi>
                {
                    new ProtonSubKompetensi
                    {
                        NamaSubKompetensi = "Pengenalan Proses",
                        Urutan = 1,
                        Deliverables = new List<ProtonDeliverable>
                        {
                            new ProtonDeliverable { NamaDeliverable = "Mampu menjelaskan proses distilasi", Urutan = 1 },
                            new ProtonDeliverable { NamaDeliverable = "Mampu mengidentifikasi peralatan utama kilang", Urutan = 2 },
                        }
                    },
                    new ProtonSubKompetensi
                    {
                        NamaSubKompetensi = "Parameter Operasi",
                        Urutan = 2,
                        Deliverables = new List<ProtonDeliverable>
                        {
                            new ProtonDeliverable { NamaDeliverable = "Mampu membaca parameter operasi", Urutan = 1 },
                            new ProtonDeliverable { NamaDeliverable = "Mampu melaporkan anomali parameter", Urutan = 2 },
                        }
                    }
                }
            };

            // K3: Keselamatan Proses
            var k3 = new ProtonKompetensi
            {
                NamaKompetensi = "Keselamatan Proses",
                TrackType = "Operator",
                TahunKe = "Tahun 1",
                Urutan = 3,
                SubKompetensiList = new List<ProtonSubKompetensi>
                {
                    new ProtonSubKompetensi
                    {
                        NamaSubKompetensi = "Hazard Identification",
                        Urutan = 1,
                        Deliverables = new List<ProtonDeliverable>
                        {
                            new ProtonDeliverable { NamaDeliverable = "Mampu melakukan identifikasi bahaya", Urutan = 1 },
                            new ProtonDeliverable { NamaDeliverable = "Mampu menggunakan JSA/HIRADC", Urutan = 2 },
                        }
                    }
                }
            };

            await context.ProtonKompetensiList.AddRangeAsync(k1, k2, k3);

            // ================================================================
            // Panelman Tahun 1 — placeholder data
            // TODO: Replace with actual Panelman data
            // ================================================================

            var panelmanTahun1 = new ProtonKompetensi
            {
                NamaKompetensi = "Kompetensi Panelman Tahun 1 (Placeholder)",
                TrackType = "Panelman",
                TahunKe = "Tahun 1",
                Urutan = 1,
                SubKompetensiList = new List<ProtonSubKompetensi>
                {
                    new ProtonSubKompetensi
                    {
                        NamaSubKompetensi = "Sub Kompetensi Panelman 1 (Placeholder)",
                        Urutan = 1,
                        Deliverables = new List<ProtonDeliverable>
                        {
                            new ProtonDeliverable { NamaDeliverable = "Deliverable Panelman 1.1 (Placeholder)", Urutan = 1 },
                            new ProtonDeliverable { NamaDeliverable = "Deliverable Panelman 1.2 (Placeholder)", Urutan = 2 },
                            new ProtonDeliverable { NamaDeliverable = "Deliverable Panelman 1.3 (Placeholder)", Urutan = 3 },
                        }
                    }
                }
            };

            await context.ProtonKompetensiList.AddAsync(panelmanTahun1);

            // ================================================================
            // Operator Tahun 2 — placeholder data
            // TODO: Replace with actual Tahun 2 data
            // ================================================================

            var operatorTahun2 = new ProtonKompetensi
            {
                NamaKompetensi = "Kompetensi Operator Tahun 2 (Placeholder)",
                TrackType = "Operator",
                TahunKe = "Tahun 2",
                Urutan = 1,
                SubKompetensiList = new List<ProtonSubKompetensi>
                {
                    new ProtonSubKompetensi
                    {
                        NamaSubKompetensi = "Sub Kompetensi Operator Tahun 2 (Placeholder)",
                        Urutan = 1,
                        Deliverables = new List<ProtonDeliverable>
                        {
                            new ProtonDeliverable { NamaDeliverable = "Deliverable Operator Tahun 2.1 (Placeholder)", Urutan = 1 },
                            new ProtonDeliverable { NamaDeliverable = "Deliverable Operator Tahun 2.2 (Placeholder)", Urutan = 2 },
                        }
                    }
                }
            };

            await context.ProtonKompetensiList.AddAsync(operatorTahun2);

            // ================================================================
            // Operator Tahun 3 — placeholder data
            // TODO: Replace with actual Tahun 3 data
            // ================================================================

            var operatorTahun3 = new ProtonKompetensi
            {
                NamaKompetensi = "Kompetensi Operator Tahun 3 (Placeholder)",
                TrackType = "Operator",
                TahunKe = "Tahun 3",
                Urutan = 1,
                SubKompetensiList = new List<ProtonSubKompetensi>
                {
                    new ProtonSubKompetensi
                    {
                        NamaSubKompetensi = "Sub Kompetensi Operator Tahun 3 (Placeholder)",
                        Urutan = 1,
                        Deliverables = new List<ProtonDeliverable>
                        {
                            new ProtonDeliverable { NamaDeliverable = "Deliverable Operator Tahun 3.1 (Placeholder)", Urutan = 1 },
                            new ProtonDeliverable { NamaDeliverable = "Deliverable Operator Tahun 3.2 (Placeholder)", Urutan = 2 },
                        }
                    }
                }
            };

            await context.ProtonKompetensiList.AddAsync(operatorTahun3);

            await context.SaveChangesAsync();
            Console.WriteLine("✅ Seeded Proton deliverable hierarchy (Operator Tahun 1: 3 Kompetensi, 6 SubKompetensi, 13 Deliverables + placeholders for Panelman and Tahun 2/3) successfully!");
        }
    }
}
