using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using HcPortal.Models;

namespace HcPortal.Controllers
{
    public class CDPController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public CDPController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index(string? bagian = null, string? unit = null, string? level = null)
        {
            // Get current user and their role
            var user = await _userManager.GetUserAsync(User);
            string userRole = "Operator"; // Default
            int userLevel = 6; // Default: Coachee
            
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRole = roles.FirstOrDefault() ?? "Operator";
                userLevel = user.RoleLevel;
            }

            // Check if HC user has selected a bagian
            bool hasBagianSelected = !string.IsNullOrEmpty(bagian);

            // Pass role and selection to view
            ViewBag.UserRole = userRole;
            ViewBag.UserLevel = userLevel;
            ViewBag.HasBagianSelected = hasBagianSelected;
            ViewBag.SelectedBagian = bagian ?? "GAST";
            ViewBag.SelectedUnit = unit ?? "RFCC NHT";
            ViewBag.SelectedLevel = level ?? "Operator";

            // Build PDF filename based on selection
            var pdfFileName = $"{(bagian ?? "GAST").Replace(" ", "").Replace("/", "")}_{(unit ?? "RFCC NHT").Replace(" ", "").Replace("/", "")}_{level ?? "Operator"}_Kompetensi_02022026.pdf";
            ViewBag.PdfFileName = pdfFileName;

            return View();
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
                    SubKompetensi = "Review Target Q1", 
                    Tanggal = new DateTime(2025, 01, 10),
                    Status = "Submitted",
                    CatatanCoach = "Progress memuaskan, pertahankan."
                },
                new CoachingLog { 
                    Id = 2, 
                    CoachName = "Siti Aminah (Senior Eng)", 
                    CoacheeName = "User (Anda)", 
                    SubKompetensi = "Teknis Troubleshooting Pompa", 
                    Tanggal = new DateTime(2025, 02, 05),
                    Status = "Submitted",
                    CatatanCoach = "Menunggu upload evidence foto lapangan."
                }
            };

            return View(history);
        }

        public async Task<IActionResult> Progress(string? bagian = null, string? unit = null, string? coacheeId = null)
        {
            // Get current user and their role
            var user = await _userManager.GetUserAsync(User);
            string userRole = "Coachee"; // Default
            int userLevel = 6; // Default: Coachee
            
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRole = roles.FirstOrDefault() ?? "Coachee";
                userLevel = user.RoleLevel;
            }

            // For Level 4 (Section Head, Sr Supervisor): Bagian is auto-filled from user profile
            if (userLevel == 4 && user != null && string.IsNullOrEmpty(bagian))
            {
                bagian = user.Section;
            }

            // Pass user context to view
            ViewBag.UserRole = userRole;
            ViewBag.UserLevel = userLevel;
            ViewBag.UserSection = user?.Section;
            ViewBag.UserUnit = user?.Unit;
            ViewBag.UserFullName = user?.FullName;
            ViewBag.UserPosition = user?.Position;
            
            ViewBag.SelectedBagian = bagian;
            ViewBag.SelectedUnit = unit;
            ViewBag.SelectedCoacheeId = coacheeId;

            // Mock data: Coachees for Coach role
            if (userRole == "Coach")
            {
                var mockCoachees = new List<ApplicationUser>
                {
                    new ApplicationUser { Id = "coachee1", FullName = "Ahmad Fauzi", Position = "Operator I" },
                    new ApplicationUser { Id = "coachee2", FullName = "Siti Nurhaliza", Position = "Operator II" },
                    new ApplicationUser { Id = "coachee3", FullName = "Bambang Wijaya", Position = "Panelman" }
                };
                ViewBag.Coachees = mockCoachees;
            }

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