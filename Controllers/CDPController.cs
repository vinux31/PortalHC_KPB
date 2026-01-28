using Microsoft.AspNetCore.Mvc;
using HcPortal.Models; // Agar Controller kenal file IdpItem.cs tadi

namespace HcPortal.Controllers
{
    public class CDPController : Controller
    {
        public IActionResult Index()
        {
            var data = new List<IdpCompetency>
            {
                new IdpCompetency
                {
                    Name = "1. Safe Work Practice & Lifesaving Rules",
                    Periods = new List<IdpImplementationPeriod>
                    {
                        new IdpImplementationPeriod
                        {
                            PeriodName = "Tahun Pertama",
                            OperatorCompetencies = new List<string>
                            {
                                "1.1. Safe Work Practice",
                                "1.2. Lifesaving Rules",
                                "1.3. Emergency Response"
                            },
                            PanelmanCompetencies = new List<string>
                            {
                                "1.1. Safe Work Practice Regulation",
                                "1.2. Supervision of Safe Work Practice and Lifesaving Rules",
                                "1.3. Monitoring of Safety Equipment Readiness and Maintenance",
                                "1.4. Intervention and Safety Awareness Development"
                            }
                        }
                    }
                },
                new IdpCompetency
                {
                    Name = "2. Energy Management",
                    Periods = new List<IdpImplementationPeriod>
                    {
                        new IdpImplementationPeriod
                        {
                            PeriodName = "Tahun Kedua",
                            OperatorCompetencies = new List<string>
                            {
                                "2.1. Karakteristik Energi",
                                "2.2. Prinsip – Prinsip Dasar Peralatan yang Menggunakan Energi",
                                "2.3. Data Collecting for Energy Consumption Evaluation"
                            },
                            PanelmanCompetencies = new List<string>
                            {
                                "2.1. Integrasi & Konversi Energi",
                                "2.2. Teknik – Teknik Konservasi Energi",
                                "2.3. Monitoring & Evaluasi Pemakaian Energi untuk Kebutuhan Operasi",
                                "2.4. Boiler & Furnace Optimization"
                            }
                        }
                    }
                },
                new IdpCompetency
                {
                    Name = "3. Catalyst & Chemical Management",
                    Periods = new List<IdpImplementationPeriod>
                    {
                        new IdpImplementationPeriod
                        {
                            PeriodName = "Tahun Kedua",
                            OperatorCompetencies = new List<string>
                            {
                                "3.1. Jenis & Fungsi Catalyst & Chemical",
                                "3.2. Karakteristik Catalyst & Chemical",
                                "3.3. Pengaruh Impurities pada Catalyst & Chemical Performance",
                                "3.4. Catalyst Make Up Consumption",
                                "3.5. Chemical Make Up Consumption"
                            },
                            PanelmanCompetencies = new List<string>
                            {
                                "3.1. Jenis & Fungsi Catalyst & Chemical",
                                "3.2. Karakteristik Catalyst & Chemical",
                                "3.3. Pengaruh Impurities pada Catalyst & Chemical Performance",
                                "3.4. Catalyst Make Up Consumption",
                                "3.5. Chemical Make Up Consumption"
                            }
                        }
                    }
                },
                new IdpCompetency
                {
                    Name = "4. Process Control & Computer Operations",
                    Periods = new List<IdpImplementationPeriod>
                    {
                        new IdpImplementationPeriod
                        {
                            PeriodName = "Tahun Ketiga",
                            OperatorCompetencies = new List<string>
                            {
                                "4.1. Prinsip Dasar Pengendalian & Pengukuran Variabel Oper",
                                "4.2. Prinsip Kerja Peralatan Pengendalian Proses",
                                "4.3. Computer Operations"
                            },
                            PanelmanCompetencies = new List<string>
                            {
                                "4.1. Tunning Controller",
                                "4.2. Process Control Analysis",
                                "4.3. Computer Operations"
                            }
                        }
                    }
                },
                new IdpCompetency
                {
                    Name = "5. Refinery Process Operations & Optimization",
                    Periods = new List<IdpImplementationPeriod>
                    {
                        new IdpImplementationPeriod
                        {
                            PeriodName = "Tahun Pertama",
                            OperatorCompetencies = new List<string>
                            {
                                "5.1. BOC / BEC",
                                "5.2. Feed & Product Specification",
                                "5.3. P&ID, Line Up & Lay Out",
                                "5.4. Start up, Shutdown & Emergency Unit",
                                "5.5. Prinsip Dasar & Pengoperasian Fasilitas Kilang",
                                "5.6. Identifikasi Bahaya pada Pengoperasian Fasilitas Kilang"
                            },
                            PanelmanCompetencies = new List<string>
                            {
                                "5.1. Routine Activities for Refinery Operations",
                                "5.2. Pengoperasian HMI (Human Machine Interface)",
                                "5.3. Operating Windows",
                                "5.4. Refinery Operations Analysis",
                                "5.5. Troubleshooting & Problem Solving",
                                "5.6. Operational Risk Identification"
                            }
                        },
                        new IdpImplementationPeriod
                        {
                            PeriodName = "Tahun Kedua",
                            OperatorCompetencies = new List<string>
                            {
                                "5.7. Prinsip Dasar & Pengoperasian Peralatan",
                                "5.8. Pengoperasian Sub Proses",
                                "5.9. Equipment Operations"
                            },
                            PanelmanCompetencies = new List<string>
                            {
                                "5.7. Pengoperasian & Interaksi Sub Proses",
                                "5.8. Startup, Shutdown & Emergency"
                            }
                        },
                        new IdpImplementationPeriod
                        {
                            PeriodName = "Tahun Ketiga",
                            OperatorCompetencies = new List<string>
                            {
                                "5.10. Karakteristik Unit Operasi",
                                "5.11. Day to Day Monitoring",
                                "5.12. Data Collecting for Process Optimization",
                                "5.13. Operating Windows"
                            },
                            PanelmanCompetencies = new List<string>
                            {
                                "5.8. Process Optimization"
                            }
                        }
                    }
                }
            };

            return View(data);
        }

        public IActionResult Dashboard()
        {
            // Simulate fetching data from service/database
            var model = new DashboardViewModel
            {
                TotalIdp = 142,
                IdpGrowth = 12,
                CompletionRate = 68,
                CompletionTarget = "80% (Q4)",
                PendingAssessments = 15,
                BudgetUsedPercent = 45,
                BudgetUsedText = "Rp 450jt / 1M",
                
                // Chart Data (Jan - Jun)
                ChartLabels = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun" },
                ChartTarget = new List<int> { 100, 100, 120, 120, 150, 150 },
                ChartRealization = new List<int> { 95, 110, 115, 140, 145, 160 },

                // Compliance Data
                TopUnits = new List<UnitCompliance>
                {
                    new UnitCompliance { UnitName = "SRU Unit", Percentage = 95, ColorClass = "bg-success" },
                    new UnitCompliance { UnitName = "RFCC Unit", Percentage = 92, ColorClass = "bg-success" },
                    new UnitCompliance { UnitName = "Utilities", Percentage = 88, ColorClass = "bg-primary" },
                    new UnitCompliance { UnitName = "Maintenance", Percentage = 74, ColorClass = "bg-warning" },
                    new UnitCompliance { UnitName = "Procurement", Percentage = 40, ColorClass = "bg-danger" }
                }
            };

            return View(model);
        }

        public IActionResult Coaching()
        {
            // Data Dummy Riwayat Coaching
            var history = new List<CoachingLog>
            {
                new CoachingLog { 
                    Id = 1, 
                    CoachName = "Budi Santoso (Mgr)", 
                    CoacheeName = "User (Anda)", 
                    Topik = "Review Target Q1", 
                    Tanggal = new DateTime(2025, 01, 10),
                    Status = "Verified",
                    Catatan = "Progress memuaskan, pertahankan."
                },
                new CoachingLog { 
                    Id = 2, 
                    CoachName = "Siti Aminah (Senior Eng)", 
                    CoacheeName = "User (Anda)", 
                    Topik = "Teknis Troubleshooting Pompa", 
                    Tanggal = new DateTime(2025, 02, 05),
                    Status = "Submitted",
                    Catatan = "Menunggu upload evidence foto lapangan."
                }
            };

            return View(history);
        }

        public IActionResult Progress()
        {
            // Flattened data from the IDP Plan for tracking purposes
            var data = new List<TrackingItem>
            {
                // 1. Safe Work Practice
                new TrackingItem { Id=1, Kompetensi="Safe Work Practice & Lifesaving Rules", Periode="Tahun Pertama", SubKompetensi="1.1. Safe Work Practice (Op)", EvidenceStatus="Uploaded", ApprovalSrSpv="Approved", ApprovalSectionHead="Approved", ApprovalHC="Approved" },
                new TrackingItem { Id=2, Kompetensi="Safe Work Practice & Lifesaving Rules", Periode="Tahun Pertama", SubKompetensi="1.2. Lifesaving Rules (Op)", EvidenceStatus="Uploaded", ApprovalSrSpv="Approved", ApprovalSectionHead="Approved", ApprovalHC="Approved" },
                new TrackingItem { Id=3, Kompetensi="Safe Work Practice & Lifesaving Rules", Periode="Tahun Pertama", SubKompetensi="1.1. Safe Work Practice Regulation (Pnl)", EvidenceStatus="Pending", ApprovalSrSpv="Approved", ApprovalSectionHead="Pending", ApprovalHC="Pending", SupervisorComments="Mohon lampirkan sertifikat terbaru" },
                new TrackingItem { Id=4, Kompetensi="Safe Work Practice & Lifesaving Rules", Periode="Tahun Pertama", SubKompetensi="1.2. Supervision of Safe Work Practice (Pnl)", EvidenceStatus="Pending", ApprovalSrSpv="Pending", ApprovalSectionHead="Not Started", ApprovalHC="Not Started" },
                
                // 2. Energy Management
                new TrackingItem { Id=5, Kompetensi="Energy Management", Periode="Tahun Kedua", SubKompetensi="2.1. Karakteristik Energi (Op)", EvidenceStatus="Pending", ApprovalSrSpv="Not Started", ApprovalSectionHead="Not Started", ApprovalHC="Not Started" },
                new TrackingItem { Id=6, Kompetensi="Energy Management", Periode="Tahun Kedua", SubKompetensi="2.1. Integrasi & Konversi Energi (Pnl)", EvidenceStatus="Pending", ApprovalSrSpv="Not Started", ApprovalSectionHead="Not Started", ApprovalHC="Not Started" },

                // 3. Catalyst
                new TrackingItem { Id=7, Kompetensi="Catalyst & Chemical Management", Periode="Tahun Kedua", SubKompetensi="3.1. Jenis & Fungsi Catalyst (Op)", EvidenceStatus="Pending", ApprovalSrSpv="Not Started", ApprovalSectionHead="Not Started", ApprovalHC="Not Started" },
                
                // 4. Process Control
                new TrackingItem { Id=8, Kompetensi="Process Control & Computer Ops", Periode="Tahun Ketiga", SubKompetensi="4.1. Prinsip Dasar Pengendalian (Op)", EvidenceStatus="Pending", ApprovalSrSpv="Not Started", ApprovalSectionHead="Not Started", ApprovalHC="Not Started" },

                // 5. Refinery Process
                new TrackingItem { Id=9, Kompetensi="Refinery Process Ops & Optimization", Periode="Tahun Pertama", SubKompetensi="5.1. BOC / BEC (Op)", EvidenceStatus="Uploaded", ApprovalSrSpv="Approved", ApprovalSectionHead="Approved", ApprovalHC="Pending" },
                new TrackingItem { Id=10, Kompetensi="Refinery Process Ops & Optimization", Periode="Tahun Pertama", SubKompetensi="5.1. Routine Activities (Pnl)", EvidenceStatus="Uploaded", ApprovalSrSpv="Approved", ApprovalSectionHead="Pending", ApprovalHC="Pending", SupervisorComments="Video evidence kurang jelas audionya" },
                new TrackingItem { Id=11, Kompetensi="Refinery Process Ops & Optimization", Periode="Tahun Kedua", SubKompetensi="5.7. Prinsip Dasar Peralatan (Op)", EvidenceStatus="Pending", ApprovalSrSpv="Not Started", ApprovalSectionHead="Not Started", ApprovalHC="Not Started" },
            };

            return View(data);
        }

    }
}