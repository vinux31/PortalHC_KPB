using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using HcPortal.Models;
using HcPortal.Data;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Controllers
{
    [Authorize]
    public class CMPController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public CMPController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // --- HALAMAN 1: SUSUNAN KKJ (MATRIX VIEW) ---
        public async Task<IActionResult> Kkj(string? section)
        {
            // Get current user and role
            var user = await _userManager.GetUserAsync(User);
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var userRole = userRoles.FirstOrDefault();
            int userLevel = user?.RoleLevel ?? 6;
            
            ViewBag.UserRole = userRole;
            ViewBag.UserLevel = userLevel;
            ViewBag.SelectedSection = section;
            
            // If Level 1-3 (Admin, HC, Management) and no section selected, show selection page
            if (UserRoles.HasFullAccess(userLevel) && string.IsNullOrEmpty(section))
            {
                return View("KkjSectionSelect");
            }

            // ✅ QUERY FROM DATABASE instead of hardcoded data
            var matrixData = await _context.KkjMatrices
                .OrderBy(k => k.No)
                .ToListAsync();

            return View(matrixData);
        }

        // --- HALAMAN 2: MAPPING KKJ - CPDP ---
        public async Task<IActionResult> Mapping()
        {
            // ✅ QUERY FROM DATABASE instead of hardcoded data
            var cpdpData = await _context.CpdpItems
                .OrderBy(c => c.No)
                .ToListAsync();

            return View(cpdpData);
        }
        // --- HALAMAN 3: ASSESSMENT LOBBY ---
        public IActionResult Assessment()
        {
            // 1. KITA BUAT LIST DATA (Pastikan variabel ini ada)
            var exams = new List<AssessmentSession>
            {
                // 1. OJT (Ex Assessment OJ)
                new AssessmentSession { 
                    Id = 201, Title = "On Job Assessment: Field Operator", Category = "OJT", Type = "OJT",
                    Schedule = DateTime.Now.AddDays(-2), DurationMinutes = 120, Status = "Open", 
                    Progress = 25, BannerColor = "bg-primary", IsTokenRequired = false 
                },
                new AssessmentSession { 
                    Id = 202, Title = "Panel Operator Competency", Category = "OJT", Type = "OJT",
                    Schedule = DateTime.Now.AddDays(5), DurationMinutes = 90, Status = "Upcoming", 
                    Progress = 0, BannerColor = "bg-primary", IsTokenRequired = false 
                },

                // 2. IHT
                new AssessmentSession { 
                    Id = 203, Title = "Internal Training: Pump Maintenance", Category = "IHT", Type = "IHT",
                    Schedule = DateTime.Now.AddDays(-10), DurationMinutes = 60, Status = "Completed", 
                    Progress = 100, Score = 85, BannerColor = "bg-success", IsTokenRequired = false 
                },

                // 3. Training Licencor (Ex Licencor)
                new AssessmentSession { 
                    Id = 204, Title = "Boiler Class 1 License", Category = "Training Licencor", Type = "Training Licencor",
                    Schedule = DateTime.Now.AddDays(14), DurationMinutes = 180, Status = "Upcoming", 
                    Progress = 0, BannerColor = "bg-danger", IsTokenRequired = true 
                },

                // 4. OTS
                new AssessmentSession { 
                    Id = 205, Title = "OTS Simulation: Blackout Recovery", Category = "OTS", Type = "OTS",
                    Schedule = DateTime.Now, DurationMinutes = 120, Status = "Open", 
                    Progress = 10, BannerColor = "bg-warning", IsTokenRequired = true 
                },

                // 5. Mandatory HSSE Training
                new AssessmentSession { 
                    Id = 206, Title = "Basic Fire Fighting", Category = "Mandatory HSSE Training", Type = "Mandatory HSSE Training",
                    Schedule = DateTime.Now.AddMonths(-1), DurationMinutes = 45, Status = "Completed", 
                    Progress = 100, Score = 92, BannerColor = "bg-info", IsTokenRequired = false 
                },
                
                // 6. PROTON (New)
                new AssessmentSession { 
                    Id = 208, Title = "PROTON Simulation: Distillation Unit", Category = "Proton", Type = "Proton",
                    Schedule = DateTime.Now, DurationMinutes = 90, Status = "Open", 
                    Progress = 0, BannerColor = "bg-purple", IsTokenRequired = true // MUST HAVE TOKEN
                }
            };

            // 2. KRUSIAL: Data 'exams' HARUS dimasukkan ke dalam kurung View()
            // KALAU INI KOSONG -> HALAMAN AKAN BLANK/ERROR
            return View(exams); 
        }

        // HALAMAN 4: CAPABILITY BUILDING RECORDS
        public async Task<IActionResult> Records(string? section, string? category, string? search, string? statusFilter, string? isFiltered)
        {
            // Get current user and role
            var user = await _userManager.GetUserAsync(User);
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var userRole = userRoles.FirstOrDefault();
            int userLevel = user?.RoleLevel ?? 6;
            
            // Check if this is an initial load (no filter applied explicitly)
            // We use the hidden input 'isFiltered' from the form to differentiate
            bool isInitialState = string.IsNullOrEmpty(isFiltered);

            // Default section to GAST if not specified, JUST for the dropdown display
            // REMOVED: User wants "Semua Bagian" option
            // if (string.IsNullOrEmpty(section))
            // {
            //     section = "GAST";
            // }

            ViewBag.UserRole = userRole;
            ViewBag.UserLevel = userLevel;
            ViewBag.SelectedSection = section; // Can be null/empty for "Semua Bagian"
            ViewBag.SelectedCategory = category;
            ViewBag.SearchTerm = search;
            ViewBag.SelectedStatus = statusFilter;
            ViewBag.IsInitialState = isInitialState;
            
            // Determine if we should show Status column (Filter Mode) or Stats columns (Default Mode)
            bool isFilterMode = !string.IsNullOrEmpty(category);
            ViewBag.IsFilterMode = isFilterMode;

            // Role: Level 5-6 (Coach/Coachee) - Show personal training records
            if (UserRoles.IsCoachingRole(userLevel))
            {
                var personalRecords = await GetPersonalTrainingRecords(user?.Id ?? "");
                return View("Records", personalRecords);
            }
            
            // Supervisor view: 
            List<WorkerTrainingStatus> workers;

            if (isInitialState)
            {
                // Return empty list on initial load
                workers = new List<WorkerTrainingStatus>();
            }
            else
            {
                // Fetch filtered data
                workers = await GetWorkersInSection(section, null, category, search, statusFilter);
            }

            return View("RecordsWorkerList", workers);
        }

        // WORKER DETAIL PAGE - Show individual worker's training records
        public async Task<IActionResult> WorkerDetail(string workerId, string name)
        {
            // Get current user info
            var user = await _userManager.GetUserAsync(User);
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var userRole = userRoles.FirstOrDefault();
            int userLevel = user?.RoleLevel ?? 6;

            ViewBag.UserRole = userRole;
            ViewBag.UserLevel = userLevel;
            ViewBag.WorkerId = workerId;
            ViewBag.WorkerName = name;

            // Get worker's training records
            var trainingRecords = await GetPersonalTrainingRecords(workerId);

            return View("WorkerDetail", trainingRecords);
        }
        
        // Helper method: Get personal training records for Coach/Coachee
        private async Task<List<TrainingRecord>> GetPersonalTrainingRecords(string userId)
        {
            // ✅ QUERY FROM DATABASE instead of hardcoded data
            var trainingRecords = await _context.TrainingRecords
                .Where(tr => tr.UserId == userId)
                .OrderByDescending(tr => tr.Tanggal)
                .ToListAsync();

            return trainingRecords;
        }

        // Helper method: Get all workers in a section (with optional filters)
        private async Task<List<WorkerTrainingStatus>> GetWorkersInSection(string? section, string? unitFilter = null, string? category = null, string? search = null, string? statusFilter = null)
        {
            // Mock data - All workers in GAST section
            var allWorkers = new List<WorkerTrainingStatus>
            {
                // Alkylation Unit (065)
                new WorkerTrainingStatus
                {
                    WorkerId = "user-rustam",
                    WorkerName = "Rustam Santiko",
                    NIP = "123456",
                    Position = "Coach",
                    Section = "GAST",
                    Unit = "Alkylation (065)",
                    TotalTrainings = 10,
                    CompletedTrainings = 8,
                    PendingTrainings = 1,
                    ExpiringSoonTrainings = 1
                },
                new WorkerTrainingStatus
                {
                    WorkerId = "user-iwan",
                    WorkerName = "Iwan",
                    NIP = "789012",
                    Position = "Operator",
                    Section = "GAST",
                    Unit = "Alkylation (065)",
                    TotalTrainings = 10,
                    CompletedTrainings = 6,
                    PendingTrainings = 2,
                    ExpiringSoonTrainings = 2
                },
                new WorkerTrainingStatus
                {
                    WorkerId = "user-budi",
                    WorkerName = "Budi Santoso",
                    NIP = "345678",
                    Position = "Operator",
                    Section = "GAST",
                    Unit = "Alkylation (065)",
                    TotalTrainings = 10,
                    CompletedTrainings = 9,
                    PendingTrainings = 1,
                    ExpiringSoonTrainings = 0
                },
                // SWS RFCC & Non RFCC (067 & 167)
                new WorkerTrainingStatus
                {
                    WorkerId = "user-ahmad",
                    WorkerName = "Ahmad Fauzi",
                    NIP = "234567",
                    Position = "Operator",
                    Section = "GAST",
                    Unit = "SWS RFCC & Non RFCC (067 & 167)",
                    TotalTrainings = 10,
                    CompletedTrainings = 7,
                    PendingTrainings = 2,
                    ExpiringSoonTrainings = 1
                },
                new WorkerTrainingStatus
                {
                    WorkerId = "user-dedi",
                    WorkerName = "Dedi Kurniawan",
                    NIP = "456789",
                    Position = "Sr Operator",
                    Section = "GAST",
                    Unit = "SWS RFCC & Non RFCC (067 & 167)",
                    TotalTrainings = 10,
                    CompletedTrainings = 10,
                    PendingTrainings = 0,
                    ExpiringSoonTrainings = 0
                },
                
                // === RFCC Section ===
                new WorkerTrainingStatus
                {
                    WorkerId = "rfcc-1",
                    WorkerName = "Doni Setiawan",
                    NIP = "556112",
                    Position = "Operator",
                    Section = "RFCC",
                    Unit = "RFCC LPG Treating Unit (062)",
                    TotalTrainings = 10,
                    CompletedTrainings = 10,
                    PendingTrainings = 0,
                    ExpiringSoonTrainings = 0
                },
                new WorkerTrainingStatus
                {
                    WorkerId = "rfcc-2",
                    WorkerName = "Eko Prasetyo",
                    NIP = "556113",
                    Position = "Sr Operator",
                    Section = "RFCC",
                    Unit = "RFCC LPG Treating Unit (062)",
                    TotalTrainings = 10,
                    CompletedTrainings = 5,
                    PendingTrainings = 5,
                    ExpiringSoonTrainings = 0
                },
                new WorkerTrainingStatus
                {
                    WorkerId = "rfcc-3",
                    WorkerName = "Fajar Nugraha",
                    NIP = "556114",
                    Position = "Coach",
                    Section = "RFCC",
                    Unit = "Propylene Recovery Unit (063)",
                    TotalTrainings = 12,
                    CompletedTrainings = 12,
                    PendingTrainings = 0,
                    ExpiringSoonTrainings = 0
                },

                // === NGP Section ===
                new WorkerTrainingStatus
                {
                    WorkerId = "ngp-1",
                    WorkerName = "Gilang Ramadhan",
                    NIP = "667223",
                    Position = "Operator",
                    Section = "NGP",
                    Unit = "Saturated Gas Concentration Unit (060)",
                    TotalTrainings = 8,
                    CompletedTrainings = 6,
                    PendingTrainings = 2,
                    ExpiringSoonTrainings = 0
                },
                new WorkerTrainingStatus
                {
                    WorkerId = "ngp-2",
                    WorkerName = "Hadi Kurniawan",
                    NIP = "667224",
                    Position = "Panel",
                    Section = "NGP",
                    Unit = "Saturated LPG Treating Unit (064)",
                    TotalTrainings = 8,
                    CompletedTrainings = 8,
                    PendingTrainings = 0,
                    ExpiringSoonTrainings = 0
                },
                 new WorkerTrainingStatus
                {
                    WorkerId = "ngp-3",
                    WorkerName = "Indra Gunawan",
                    NIP = "667225",
                    Position = "Sr Operator",
                    Section = "NGP",
                    Unit = "Isomerization Unit (082)",
                    TotalTrainings = 10,
                    CompletedTrainings = 2,
                    PendingTrainings = 8,
                    ExpiringSoonTrainings = 0
                },

                // === DHT / HMU Section ===
                new WorkerTrainingStatus
                {
                    WorkerId = "dht-1",
                    WorkerName = "Joko Susilo",
                    NIP = "778334",
                    Position = "Operator",
                    Section = "DHT / HMU",
                    Unit = "Diesel Hydrotreating Unit I & II (054 & 083)",
                    TotalTrainings = 10,
                    CompletedTrainings = 10,
                    PendingTrainings = 0,
                    ExpiringSoonTrainings = 0
                },
                new WorkerTrainingStatus
                {
                    WorkerId = "dht-2",
                    WorkerName = "Kiki Amalia",
                    NIP = "778335",
                    Position = "Operator",
                    Section = "DHT / HMU",
                    Unit = "Hydrogen Manufacturing Unit (068)",
                    TotalTrainings = 10,
                    CompletedTrainings = 9,
                    PendingTrainings = 1,
                    ExpiringSoonTrainings = 0
                },
                new WorkerTrainingStatus
                {
                    WorkerId = "dht-3",
                    WorkerName = "Lukman Hakim",
                    NIP = "778336",
                    Position = "Coach",
                    Section = "DHT / HMU",
                    Unit = "Common DHT H2 Compressor (085)",
                    TotalTrainings = 15,
                    CompletedTrainings = 10,
                    PendingTrainings = 5,
                    ExpiringSoonTrainings = 0
                }
            };

            // 0. FILTER BY SECTION
            // If section is provided, filter by it. If empty/null ("Semua Bagian"), include ALL workers.
            if (!string.IsNullOrEmpty(section))
            {
                allWorkers = allWorkers.Where(w => w.Section == section).ToList();
            }

            // 1. FILTER BY SEARCH (Name or NIP)
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                allWorkers = allWorkers.Where(w => 
                    w.WorkerName.ToLower().Contains(search) || 
                    (w.NIP != null && w.NIP.Contains(search))
                ).ToList();
            }
            
            // 2. CALCULATE STATUS "SUDAH/BELUM"
            foreach (var worker in allWorkers)
            {
                // Only calculate dynamic status if a Category is selected
                if (!string.IsNullOrEmpty(category))
                {
                    // Load training records
                    worker.TrainingRecords = await GetPersonalTrainingRecords(worker.WorkerId);
                    
                    bool isCompleted = false;

                    // Check if they have ANY passed/valid record in this Category
                    isCompleted = worker.TrainingRecords.Any(r => 
                        !string.IsNullOrEmpty(r.Kategori) && 
                        r.Kategori.Contains(category, StringComparison.OrdinalIgnoreCase) &&
                        (r.Status == "Passed" || r.Status == "Valid" || r.Status == "Permanent")
                    );
                    
                    // Update CompletionPercentage to reflect this binary status for the View (100 = SUDAH, 0 = BELUM)
                    worker.CompletionPercentage = isCompleted ? 100 : 0;
                }
                // If no Category selected, we keep the Mock Data's default CompletionPercentage
            }

            // 4. FILTER BY STATUS (Sudah/Belum) - Applied AFTER status calculation
            if (!string.IsNullOrEmpty(statusFilter) && !string.IsNullOrEmpty(category))
            {
                if (statusFilter == "Sudah")
                {
                    allWorkers = allWorkers.Where(w => w.CompletionPercentage == 100).ToList();
                }
                else if (statusFilter == "Belum")
                {
                    allWorkers = allWorkers.Where(w => w.CompletionPercentage != 100).ToList();
                }
            }
            
            return allWorkers;
        }
        

    }
}