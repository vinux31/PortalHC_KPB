using Microsoft.AspNetCore.Mvc;
using HcPortal.Models;
using System.Collections.Generic;

namespace HcPortal.Controllers
{
    public class BPController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("TalentProfile");
        }

        // 1. Talent Profile & Career Historical
        public IActionResult TalentProfile()
        {
            // Dummy Data for Profile & History
            var model = new TalentProfileViewModel
            {
                Name = "Budi Santoso",
                NIO = "759921",
                Position = "Section Head",
                Unit = "SRU",
                Directorate = "Technical",
                Age = 34,
                Tenure = "8 Years",
                TalentClassification = "Future Leader",
                PerformanceHistory = new List<PerformanceRecord>
                {
                    new PerformanceRecord { Year = 2024, Grade = "A", Description = "Outstanding" },
                    new PerformanceRecord { Year = 2023, Grade = "B+", Description = "Exceeds Expectations" },
                    new PerformanceRecord { Year = 2022, Grade = "B", Description = "Meets Expectations" }
                },
                CareerHistory = new List<CareerHistory>
                {
                    new CareerHistory { Tahun = 2024, Jabatan = "Section Head SRU", Unit = "SRU Process", Tipe = "Promosi", NoSK = "SK-2024/HR/005", Keterangan = "Promosi jabatan setelah assessment Q3." },
                    new CareerHistory { Tahun = 2021, Jabatan = "Senior Supervisor", Unit = "SRU Process", Tipe = "Mutasi", NoSK = "SK-2021/HR/112", Keterangan = "Rotasi internal." },
                    new CareerHistory { Tahun = 2019, Jabatan = "Junior Engineer", Unit = "Utilities", Tipe = "Mutasi", NoSK = "SK-2019/HR/088", Keterangan = "Pindah unit." },
                    new CareerHistory { Tahun = 2018, Jabatan = "Management Trainee", Unit = "RFCC", Tipe = "New Hire", NoSK = "SK-2018/REC/001", Keterangan = "Lulus program BPS." }
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