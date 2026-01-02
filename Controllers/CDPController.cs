using Microsoft.AspNetCore.Mvc;
using HcPortal.Models; // Agar Controller kenal file IdpItem.cs tadi

namespace HcPortal.Controllers
{
    public class CDPController : Controller
    {
        public IActionResult Index()
        {
            // 1. KITA BUAT DATA PALSU (MOCK DATA)
            // Nanti di masa depan, bagian ini diganti koneksi Database
            var myIdpList = new List<IdpItem>
            {
                // 1. SAFE WORK PRACTICE
                new IdpItem { Id=1, Kompetensi="Safe Work Practice & Lifesaving Rules", SubKompetensi="Safe Work Practice Regulation", Deliverable="Laporan Pemahaman Regulasi", Aktivitas="Self Learning", Metode="Self Learning", DueDate=new DateTime(2025,1,30), Status="Done", Evidence="Regulasi.pdf", ApproveSrSpv="Approved", ApproveSectionHead="Approved", ApproveHC="Approved" },
                new IdpItem { Id=2, Kompetensi="Safe Work Practice & Lifesaving Rules", SubKompetensi="Supervision of Safe Work Practice", Deliverable="Logbook Supervisi", Aktivitas="OJT", Metode="Coaching", DueDate=new DateTime(2025,2,28), Status="In Progress", Evidence="-", ApproveSrSpv="Approved", ApproveSectionHead="Pending", ApproveHC="Pending" },
                new IdpItem { Id=3, Kompetensi="Safe Work Practice & Lifesaving Rules", SubKompetensi="Monitoring Safety Equipment Readiness", Deliverable="Checklist Equipment", Aktivitas="Field Monitoring", Metode="OJT", DueDate=new DateTime(2025,3,30), Status="Open", Evidence="-", ApproveSrSpv="Pending", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=4, Kompetensi="Safe Work Practice & Lifesaving Rules", SubKompetensi="Intervention & Safety Awareness", Deliverable="Laporan Intervensi", Aktivitas="Safety Patrol", Metode="OJT", DueDate=new DateTime(2025,4,30), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },

                // 2. ENERGY MANAGEMENT
                new IdpItem { Id=5, Kompetensi="Energy Management", SubKompetensi="Karakteristik Energi", Deliverable="Review Karakteristik", Aktivitas="Classroom", Metode="Classroom", DueDate=new DateTime(2025,5,30), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=6, Kompetensi="Energy Management", SubKompetensi="Prinsip Dasar Peralatan Energi", Deliverable="Makalah Prinsip Dasar", Aktivitas="Self Learning", Metode="Virtual Learning", DueDate=new DateTime(2025,6,30), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=7, Kompetensi="Energy Management", SubKompetensi="Data Collecting for Energy Evaluation", Deliverable="Data Logsheet", Aktivitas="Data Collection", Metode="OJT", DueDate=new DateTime(2025,7,30), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=8, Kompetensi="Energy Management", SubKompetensi="Boiler & Furnace Optimization", Deliverable="Optimization Report", Aktivitas="Simulation", Metode="Simulator", DueDate=new DateTime(2025,8,30), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },

                // 3. CATALYST & CHEMICAL
                new IdpItem { Id=9, Kompetensi="Catalyst & Chemical Management", SubKompetensi="Jenis & Fungsi Catalyst & Chemical", Deliverable="Katalog Chemical", Aktivitas="Inventory Check", Metode="OJT", DueDate=new DateTime(2025,5,15), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=10, Kompetensi="Catalyst & Chemical Management", SubKompetensi="Karakteristik Catalyst & Chemical", Deliverable="Laporan Analisa", Aktivitas="Lab Visit", Metode="OJT", DueDate=new DateTime(2025,6,15), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=11, Kompetensi="Catalyst & Chemical Management", SubKompetensi="Pengaruh Impurities", Deliverable="Case Study Impurities", Aktivitas="Study Case", Metode="Mentoring", DueDate=new DateTime(2025,7,15), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=12, Kompetensi="Catalyst & Chemical Management", SubKompetensi="Catalyst Make Up Consumption", Deliverable="Calculation Sheet", Aktivitas="Calculation", Metode="Mentoring", DueDate=new DateTime(2025,8,15), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=13, Kompetensi="Catalyst & Chemical Management", SubKompetensi="Chemical Make Up Consumption", Deliverable="Calculation Sheet", Aktivitas="Calculation", Metode="Mentoring", DueDate=new DateTime(2025,9,15), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },

                // 4. PROCESS CONTROL
                new IdpItem { Id=14, Kompetensi="Process Control & Computer Ops", SubKompetensi="Prinsip Dasar Pengendalian Variabel", Deliverable="Control Loop Diagram", Aktivitas="DCS Observation", Metode="OJT", DueDate=new DateTime(2025,9,30), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=15, Kompetensi="Process Control & Computer Ops", SubKompetensi="Prinsip Kerja Peralatan Pengendalian", Deliverable="Equipment Spec Review", Aktivitas="Field Walk", Metode="OJT", DueDate=new DateTime(2025,10,30), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=16, Kompetensi="Process Control & Computer Ops", SubKompetensi="Computer Operations", Deliverable="System Log", Aktivitas="Daily Ops", Metode="OJT", DueDate=new DateTime(2025,11,30), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },

                // 5. REFINERY PROCESS
                new IdpItem { Id=17, Kompetensi="Refinery Process Ops & Optimization", SubKompetensi="BOC / BEC", Deliverable="BOC/BEC Report", Aktivitas="Reporting", Metode="OJT", DueDate=new DateTime(2025,1,20), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=18, Kompetensi="Refinery Process Ops & Optimization", SubKompetensi="Feed & Product Specification", Deliverable="Spec Lab Analysis", Aktivitas="Sampling", Metode="OJT", DueDate=new DateTime(2025,2,20), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=19, Kompetensi="Refinery Process Ops & Optimization", SubKompetensi="P&ID, Line Up & Lay Out", Deliverable="Tracing Line Diagram", Aktivitas="Field Tracing", Metode="OJT", DueDate=new DateTime(2025,3,20), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=20, Kompetensi="Refinery Process Ops & Optimization", SubKompetensi="Start up, Shutdown & Emergency", Deliverable="SOP Review", Aktivitas="SOP Reading", Metode="Self Learning", DueDate=new DateTime(2025,4,20), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=21, Kompetensi="Refinery Process Ops & Optimization", SubKompetensi="Prinsip Dasar Fasilitas Kilang", Deliverable="Flow Presentation", Aktivitas="Presentation", Metode="Sharing Session", DueDate=new DateTime(2025,5,20), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=22, Kompetensi="Refinery Process Ops & Optimization", SubKompetensi="Identifikasi Bahaya Fasilitas Kilang", Deliverable="Risk Register", Aktivitas="Risk Assessment", Metode="Workshop", DueDate=new DateTime(2025,6,20), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                // Tahun 2
                new IdpItem { Id=23, Kompetensi="Refinery Process Ops & Optimization", SubKompetensi="Prinsip Dasar & Pengoperasian Alat", Deliverable="Ops Manual", Aktivitas="Unit Operation", Metode="OJT", DueDate=new DateTime(2025,7,20), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=24, Kompetensi="Refinery Process Ops & Optimization", SubKompetensi="Pengoperasian Sub Proses", Deliverable="Logsheet Record", Aktivitas="Unit Operation", Metode="OJT", DueDate=new DateTime(2025,8,20), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=25, Kompetensi="Refinery Process Ops & Optimization", SubKompetensi="Equipment Operations", Deliverable="Equipment Log", Aktivitas="Unit Operation", Metode="OJT", DueDate=new DateTime(2025,9,20), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                // Tahun 3
                new IdpItem { Id=26, Kompetensi="Refinery Process Ops & Optimization", SubKompetensi="Karakteristik Unit Operasi", Deliverable="Performance Trend", Aktivitas="Monitoring", Metode="OJT", DueDate=new DateTime(2025,10,20), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=27, Kompetensi="Refinery Process Ops & Optimization", SubKompetensi="Day to Day Monitoring", Deliverable="Daily Report", Aktivitas="Monitoring", Metode="OJT", DueDate=new DateTime(2025,11,20), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=28, Kompetensi="Refinery Process Ops & Optimization", SubKompetensi="Data Collecting for Optimization", Deliverable="Optimization Data", Aktivitas="Optimization", Metode="Project", DueDate=new DateTime(2025,12,10), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
                new IdpItem { Id=29, Kompetensi="Refinery Process Ops & Optimization", SubKompetensi="Operating Windows", Deliverable="Operating Window Chart", Aktivitas="Analysis", Metode="Project", DueDate=new DateTime(2025,12,20), Status="Open", Evidence="-", ApproveSrSpv="Not Started", ApproveSectionHead="Not Started", ApproveHC="Not Started" },
            };

            // 2. KIRIM DATA KE VIEW
            return View(myIdpList);
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

    }
}