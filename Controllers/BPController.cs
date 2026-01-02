using Microsoft.AspNetCore.Mvc;
using HcPortal.Models;

namespace HcPortal.Controllers
{
    public class BPController : Controller
    {
        public IActionResult Index()
        {
            // Redirect Index ke Simulation (biar defaultnya langsung ke sini)
            return RedirectToAction("Simulation");
        }

        // --- HALAMAN CAREER SIMULATION ---
        public IActionResult Simulation()
        {
            var talentPool = new List<CareerCandidate>
            {
                new CareerCandidate { 
                    Id = 1, Nama = "Ahmad Fikri", JabatanSaatIni = "Senior Engineer", 
                    TargetPosisi = "Section Head", Unit = "SRU", PRL = "12", 
                    Readiness = "Ready Now", AssessmentScore = 92 
                },
                new CareerCandidate { 
                    Id = 2, Nama = "Dewi Sartika", JabatanSaatIni = "Panel Operator", 
                    TargetPosisi = "Junior Engineer", Unit = "RFCC", PRL = "08", 
                    Readiness = "Ready 1-2 Year", AssessmentScore = 78 
                },
                new CareerCandidate { 
                    Id = 3, Nama = "John Doe", JabatanSaatIni = "Field Operator", 
                    TargetPosisi = "Panel Operator", Unit = "Utilities", PRL = "06", 
                    Readiness = "Retain / Not Ready", AssessmentScore = 60 
                },
                new CareerCandidate { 
                    Id = 4, Nama = "Budi Santoso", JabatanSaatIni = "Section Head", 
                    TargetPosisi = "Manager", Unit = "SRU", PRL = "15", 
                    Readiness = "Ready Future", AssessmentScore = 85 
                }
            };

            return View(talentPool);
        }

        // --- HALAMAN 2: CAREER HISTORICAL (TIMELINE) ---
        public IActionResult Historical()
        {
            var history = new List<CareerHistory>
            {
                new CareerHistory { 
                    Tahun = 2024, Jabatan = "Section Head SRU", Unit = "SRU Process", 
                    Tipe = "Promosi", NoSK = "SK-2024/HR/005", 
                    Keterangan = "Promosi jabatan setelah assessment Q3." 
                },
                new CareerHistory { 
                    Tahun = 2021, Jabatan = "Senior Supervisor", Unit = "SRU Process", 
                    Tipe = "Mutasi", NoSK = "SK-2021/HR/112", 
                    Keterangan = "Rotasi internal untuk penguatan tim operasional." 
                },
                new CareerHistory { 
                    Tahun = 2019, Jabatan = "Junior Engineer", Unit = "Utilities", 
                    Tipe = "Mutasi", NoSK = "SK-2019/HR/088", 
                    Keterangan = "Pindah unit dari RFCC ke Utilities." 
                },
                new CareerHistory { 
                    Tahun = 2018, Jabatan = "Management Trainee (OJT)", Unit = "RFCC", 
                    Tipe = "New Hire", NoSK = "SK-2018/REC/001", 
                    Keterangan = "Lulus program Bimbingan Profesi Sarjana (BPS)." 
                }
            };

            return View(history);
        }

    }
}