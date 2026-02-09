using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using HcPortal.Models;
using HcPortal.Data;
using System.Collections.Generic;

namespace HcPortal.Controllers
{
    [Authorize]
    public class BPController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public BPController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Index()
        {
            return RedirectToAction("TalentProfile");
        }

        // 1. Talent Profile & Career Historical
        public async Task<IActionResult> TalentProfile()
        {
            // Get current user from database
            var user = await _userManager.GetUserAsync(User);
            
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Build profile from user data
            var model = new TalentProfileViewModel
            {
                Name = user.FullName ?? "Unknown",
                NIO = user.Id.Substring(0, 6).ToUpper(), // Generate NIO from user ID
                Position = user.Position ?? "Staff",
                Unit = user.Unit ?? user.Section ?? "General",
                Directorate = "Technical",
                Age = 30, // TODO: Calculate from birthdate if available
                Tenure = "N/A", // TODO: Calculate from hire date if available
                TalentClassification = "Future Leader",
                PerformanceHistory = new List<PerformanceRecord>
                {
                    new PerformanceRecord { Year = 2024, Grade = "A", Description = "Outstanding" },
                    new PerformanceRecord { Year = 2023, Grade = "B+", Description = "Exceeds Expectations" },
                    new PerformanceRecord { Year = 2022, Grade = "B", Description = "Meets Expectations" }
                },
                CareerHistory = new List<CareerHistory>
                {
                    new CareerHistory { 
                        Tahun = 2024, 
                        Jabatan = user.Position ?? "Staff", 
                        Unit = user.Unit ?? user.Section ?? "General", 
                        Tipe = "Current", 
                        NoSK = "SK-2024/HR/001", 
                        Keterangan = "Posisi saat ini" 
                    }
                }
            };

            return View(model);
        }

        // 2.1 Point System Program
        public IActionResult PointSystem()
        {
            // Dummy Data for Points
            var model = new PointSystemViewModel
            {
                TotalPoints = 1250,
                Level = "Gold",
                NextLevelThreshold = 2000,
                Activities = new List<PointActivity>
                {
                    new PointActivity { Date = "25 Jan 2025", Description = "Project Leadership: Turnaround 2024", Points = 500, Status = "Verified" },
                    new PointActivity { Date = "10 Dec 2024", Description = "Internal Instructor: Safety Awareness", Points = 150, Status = "Verified" },
                    new PointActivity { Date = "15 Nov 2024", Description = "Innovation Award: 2nd Place", Points = 300, Status = "Verified" },
                    new PointActivity { Date = "01 Nov 2024", Description = "Full Attendance Q3", Points = 300, Status = "Verified" }
                }
            };
            return View(model);
        }

        // 2.2 Eligibility Validator (Akselerasi Promosi)
        public IActionResult EligibilityValidator()
        {
            // Dummy Data for Validator
            var model = new EligibilityViewModel
            {
                CurrentPosition = "Section Head",
                TargetPosition = "Department Manager",
                OverallStatus = "Eligible with Note",
                Criteria = new List<EligibilityCriteria>
                {
                    new EligibilityCriteria { Name = "Tenure in Current Position", Required = "Min 2 Years", Actual = "3.5 Years", IsMet = true },
                    new EligibilityCriteria { Name = "Performance Rating (Avg 2 Years)", Required = "Min B+", Actual = "A-", IsMet = true },
                    new EligibilityCriteria { Name = "Assessment Score", Required = "Min 85", Actual = "92", IsMet = true },
                    new EligibilityCriteria { Name = "English Score (TOEIC)", Required = "Min 600", Actual = "550", IsMet = false }, // Not met example
                    new EligibilityCriteria { Name = "Leadership Training", Required = "Completed", Actual = "Completed", IsMet = true }
                }
            };
            return View(model);
        }
    }

    // View Models (Inline for simplicity as requested)
    // View Models (Inline for simplicity as requested)
    public class TalentProfileViewModel
    {
        public string? Name { get; set; }
        public string? NIO { get; set; }
        public string? Position { get; set; }
        public string? Unit { get; set; }
        public string? Directorate { get; set; }
        public int Age { get; set; }
        public string? Tenure { get; set; }
        public string? TalentClassification { get; set; }
        public List<PerformanceRecord>? PerformanceHistory { get; set; }
        public List<CareerHistory>? CareerHistory { get; set; }
    }

    public class PerformanceRecord
    {
        public int Year { get; set; }
        public string? Grade { get; set; }
        public string? Description { get; set; }
    }

    // Re-model CareerHistory if it was separate, but assuming it matches what we need or redefining here for safety if separate file missing props
    public class CareerHistory
    {
        public int Tahun { get; set; }
        public string? Jabatan { get; set; }
        public string? Unit { get; set; }
        public string? Tipe { get; set; }
        public string? NoSK { get; set; }
        public string? Keterangan { get; set; }
    }

    public class PointSystemViewModel
    {
        public int TotalPoints { get; set; }
        public string? Level { get; set; }
        public int NextLevelThreshold { get; set; }
        public List<PointActivity>? Activities { get; set; }
    }

    public class PointActivity
    {
        public string? Date { get; set; }
        public string? Description { get; set; }
        public int Points { get; set; }
        public string? Status { get; set; }
    }

    public class EligibilityViewModel
    {
        public string? CurrentPosition { get; set; }
        public string? TargetPosition { get; set; }
        public string? OverallStatus { get; set; }
        public List<EligibilityCriteria>? Criteria { get; set; }
    }

    public class EligibilityCriteria
    {
        public string? Name { get; set; }
        public string? Required { get; set; }
        public string? Actual { get; set; }
        public bool IsMet { get; set; }
    }
}