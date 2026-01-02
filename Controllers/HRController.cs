using Microsoft.AspNetCore.Mvc;
using HcPortal.Models;

namespace HcPortal.Controllers
{
    public class HRController : Controller
    {
        public IActionResult Index()
        {
            // Redirect Index ke Absensi agar tidak kosong
            return RedirectToAction("Absensi");
        }

        public IActionResult Absensi()
        {
            var logs = new List<AttendanceLog>
            {
                new AttendanceLog { 
                    Id = 1, Tanggal = new DateTime(2025, 12, 01), 
                    JamMasuk = "07:15", JamPulang = "16:00", 
                    Status = "Present", OvertimeHours = 0 
                },
                new AttendanceLog { 
                    Id = 2, Tanggal = new DateTime(2025, 12, 02), 
                    JamMasuk = "07:20", JamPulang = "19:00", 
                    Status = "Present", OvertimeHours = 3, Keterangan = "Meeting Project SRU" 
                },
                new AttendanceLog { 
                    Id = 3, Tanggal = new DateTime(2025, 12, 03), 
                    JamMasuk = "-", JamPulang = "-", 
                    Status = "Sick", OvertimeHours = 0, Keterangan = "Demam (Surat Dokter)" 
                },
                new AttendanceLog { 
                    Id = 4, Tanggal = new DateTime(2025, 12, 04), 
                    JamMasuk = "08:15", JamPulang = "16:15", 
                    Status = "Late", OvertimeHours = 0, Keterangan = "Ban Bocor" 
                }
            };
            
            // Calculate Summaries
            var viewModel = new AttendanceViewModel
            {
                Logs = logs,
                CurrentPeriod = "December 2025",
                AvailablePeriods = new List<string> { "January 2026", "December 2025", "November 2025", "October 2025" },
                TotalDays = logs.Count(x => x.Status == "Present" || x.Status == "Late"),
                TotalOvertimeHours = logs.Sum(x => x.OvertimeHours)
            };

            return View(viewModel);
        }

        // --- HALAMAN 3: PAYROLL (SLIP GAJI) ---
        public IActionResult Payroll()
        {
            var salaryHistory = new List<PayrollItem>
            {
                new PayrollItem { 
                    Id = 101, Periode = "Desember 2025", 
                    TanggalTransfer = new DateTime(2025, 12, 25), 
                    GajiPokok = 15000000, Tunjangan = 5000000, Potongan = 500000, 
                    Status = "Paid" 
                },
                new PayrollItem { 
                    Id = 100, Periode = "November 2025", 
                    TanggalTransfer = new DateTime(2025, 11, 25), 
                    GajiPokok = 15000000, Tunjangan = 4500000, Potongan = 500000, 
                    Status = "Paid" 
                },
                new PayrollItem { 
                    Id = 99, Periode = "Oktober 2025", 
                    TanggalTransfer = new DateTime(2025, 10, 25), 
                    GajiPokok = 14500000, Tunjangan = 4500000, Potongan = 500000, 
                    Status = "Paid" 
                }
            };

            return View(salaryHistory);
        }

        // --- HALAMAN 4: EMPLOYEE SELF SERVICE (MENU GRID) ---
        public IActionResult Service()
        {
            // Tidak perlu kirim model data, cukup tampilkan View saja
            return View();
        }
    }
}