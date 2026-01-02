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
                new IdpItem { 
                    Id = 1, 
                    Kompetensi = "SRU Process Safety", 
                    Aktivitas = "Mempelajari HAZOP Report Unit 22", 
                    Metode = "Self Learning", 
                    DueDate = new DateTime(2025, 12, 31), 
                    Status = "In Progress",
                    Evidence = "-"
                },
                new IdpItem { 
                    Id = 2, 
                    Kompetensi = "Rotating Equipment", 
                    Aktivitas = "Training Pompa Sentrifugal", 
                    Metode = "Classroom", 
                    DueDate = new DateTime(2025, 10, 15), 
                    Status = "Done",
                    Evidence = "Sertifikat.pdf"
                },
                new IdpItem { 
                    Id = 3, 
                    Kompetensi = "Digital Mindset", 
                    Aktivitas = "Belajar C# Basic", 
                    Metode = "Mentoring", 
                    DueDate = new DateTime(2025, 08, 20), 
                    Status = "Open",
                    Evidence = "-"
                }
            };

            // 2. KIRIM DATA KE VIEW
            return View(myIdpList);
        }

        public IActionResult Dashboard()
        {
            return View();
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