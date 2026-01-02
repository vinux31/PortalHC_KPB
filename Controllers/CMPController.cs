using Microsoft.AspNetCore.Mvc;
using HcPortal.Models;

namespace HcPortal.Controllers
{
    public class CMPController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // --- HALAMAN 1: SUSUNAN KKJ (MATRIX VIEW) ---
        public IActionResult Kkj()
        {
            var matrixData = new List<KkjMatrixItem>
            {
                // Baris 12
                new KkjMatrixItem { No=12, SkillGroup="Engineering", SubSkillGroup="Production & Processing", Indeks="6.2.2", Kompetensi="Gas Processing Operations", 
                    Target_SectionHead="2", Target_SrSpv_GSH="2", Target_ShiftSpv_GSH="-", Target_Panelman_GSH="2", Target_Operator_GSH="-", 
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU="2", Target_Operator_ARU="2", Target_SrSpv_Facility="2", Target_JrAnalyst="1", Target_HSE="2" },

                // Baris 15
                new KkjMatrixItem { No=15, SkillGroup="Engineering", SubSkillGroup="Production & Processing", Indeks="6.2.5", Kompetensi="Material & Chemical Blending", 
                    Target_SectionHead="2", Target_SrSpv_GSH="2", Target_ShiftSpv_GSH="-", Target_Panelman_GSH="1", Target_Operator_GSH="-", 
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU="2", Target_Operator_ARU="-", Target_SrSpv_Facility="1", Target_JrAnalyst="-", Target_HSE="-" },

                // Baris 16
                new KkjMatrixItem { No=16, SkillGroup="Engineering", SubSkillGroup="Production & Processing", Indeks="6.2.6", Kompetensi="Oil Processing Operations", 
                    Target_SectionHead="3", Target_SrSpv_GSH="2", Target_ShiftSpv_GSH="2", Target_Panelman_GSH="2", Target_Operator_GSH="2", 
                    Target_ShiftSpv_ARU="1", Target_Panelman_ARU="2", Target_Operator_ARU="2", Target_SrSpv_Facility="-", Target_JrAnalyst="2", Target_HSE="-" },

                // Baris 17
                new KkjMatrixItem { No=17, SkillGroup="Engineering", SubSkillGroup="Production & Processing", Indeks="6.2.7", Kompetensi="Refinery Process Operations", 
                    Target_SectionHead="3", Target_SrSpv_GSH="3", Target_ShiftSpv_GSH="3", Target_Panelman_GSH="2", Target_Operator_GSH="3", 
                    Target_ShiftSpv_ARU="2", Target_Panelman_ARU="2", Target_Operator_ARU="3", Target_SrSpv_Facility="2", Target_JrAnalyst="3", Target_HSE="2" },

                // Baris 26
                new KkjMatrixItem { No=26, SkillGroup="Engineering", SubSkillGroup="Process Engineering", Indeks="7.1.1", Kompetensi="Catalyst & Chemical Management", 
                    Target_SectionHead="2", Target_SrSpv_GSH="2", Target_ShiftSpv_GSH="1", Target_Panelman_GSH="1", Target_Operator_GSH="1", 
                    Target_ShiftSpv_ARU="1", Target_Panelman_ARU="1", Target_Operator_ARU="1", Target_SrSpv_Facility="1", Target_JrAnalyst="1", Target_HSE="1" },

                // Baris 27
                new KkjMatrixItem { No=27, SkillGroup="Engineering", SubSkillGroup="Process Engineering", Indeks="7.1.2", Kompetensi="Energy Management", 
                    Target_SectionHead="3", Target_SrSpv_GSH="3", Target_ShiftSpv_GSH="3", Target_Panelman_GSH="2", Target_Operator_GSH="3", 
                    Target_ShiftSpv_ARU="2", Target_Panelman_ARU="2", Target_Operator_ARU="1", Target_SrSpv_Facility="2", Target_JrAnalyst="3", Target_HSE="2" },

                // Baris 50
                new KkjMatrixItem { No=50, SkillGroup="HSSE", SubSkillGroup="Project Engineering", Indeks="7.7.1", Kompetensi="Commissioning & Operational Readiness", 
                    Target_SectionHead="3", Target_SrSpv_GSH="3", Target_ShiftSpv_GSH="2", Target_Panelman_GSH="2", Target_Operator_GSH="2", 
                    Target_ShiftSpv_ARU="2", Target_Panelman_ARU="2", Target_Operator_ARU="2", Target_SrSpv_Facility="2", Target_JrAnalyst="2", Target_HSE="2" },

                // Baris 64 (Safety)
                new KkjMatrixItem { No=64, SkillGroup="O & M", SubSkillGroup="Safety", Indeks="12.2.14", Kompetensi="Safe Work Practice & Lifesaving Rules", 
                    Target_SectionHead="3", Target_SrSpv_GSH="3", Target_ShiftSpv_GSH="3", Target_Panelman_GSH="2", Target_Operator_GSH="2", 
                    Target_ShiftSpv_ARU="2", Target_Panelman_ARU="2", Target_Operator_ARU="3", Target_SrSpv_Facility="2", Target_JrAnalyst="2", Target_HSE="2" }
            };

            return View(matrixData);
        }

        // --- HALAMAN 2: MAPPING KKJ - CPDP ---
        public IActionResult Mapping()
        {
            var cpdpData = new List<CpdpItem>
            {
                // 1. SAFE WORK PRACTICE (Tahun Pertama)
                new CpdpItem { Kompetensi="Safe Work Practice & Lifesaving Rules", Implementasi="Tahun Pertama", KodeSub="1.1", SubOps="Safe Work Practice Regulation", SubPanel="Safe Work Practice Regulation" },
                new CpdpItem { Kompetensi="Safe Work Practice & Lifesaving Rules", Implementasi="Tahun Pertama", KodeSub="1.2", SubOps="Supervision of Safe Work Practice", SubPanel="Supervision of Safe Work Practice" },
                new CpdpItem { Kompetensi="Safe Work Practice & Lifesaving Rules", Implementasi="Tahun Pertama", KodeSub="1.3", SubOps="Monitoring Safety Equipment Readiness", SubPanel="Monitoring Safety Equipment Readiness" },
                new CpdpItem { Kompetensi="Safe Work Practice & Lifesaving Rules", Implementasi="Tahun Pertama", KodeSub="1.4", SubOps="Intervention & Safety Awareness", SubPanel="Intervention & Safety Awareness" },

                // 2. ENERGY MANAGEMENT (Tahun Kedua)
                new CpdpItem { Kompetensi="Energy Management", Implementasi="Tahun Kedua", KodeSub="2.1", SubOps="Karakteristik Energi", SubPanel="Integrasi & Konversi Energi" },
                new CpdpItem { Kompetensi="Energy Management", Implementasi="Tahun Kedua", KodeSub="2.2", SubOps="Prinsip Dasar Peralatan Energi", SubPanel="Teknik – Teknik Konservasi Energi" },
                new CpdpItem { Kompetensi="Energy Management", Implementasi="Tahun Kedua", KodeSub="2.3", SubOps="Data Collecting for Energy Evaluation", SubPanel="Monitoring & Evaluasi Pemakaian Energi" },
                new CpdpItem { Kompetensi="Energy Management", Implementasi="Tahun Kedua", KodeSub="2.4", SubOps="Boiler & Furnace Optimization", SubPanel="-" },

                // 3. CATALYST & CHEMICAL (Tahun Kedua)
                new CpdpItem { Kompetensi="Catalyst & Chemical Management", Implementasi="Tahun Kedua", KodeSub="3.1", SubOps="Jenis & Fungsi Catalyst & Chemical", SubPanel="Jenis & Fungsi Catalyst & Chemical" },
                new CpdpItem { Kompetensi="Catalyst & Chemical Management", Implementasi="Tahun Kedua", KodeSub="3.2", SubOps="Karakteristik Catalyst & Chemical", SubPanel="Karakteristik Catalyst & Chemical" },
                new CpdpItem { Kompetensi="Catalyst & Chemical Management", Implementasi="Tahun Kedua", KodeSub="3.3", SubOps="Pengaruh Impurities", SubPanel="Pengaruh Impurities" },
                new CpdpItem { Kompetensi="Catalyst & Chemical Management", Implementasi="Tahun Kedua", KodeSub="3.4", SubOps="Catalyst Make Up Consumption", SubPanel="Catalyst Make Up Consumption" },
                new CpdpItem { Kompetensi="Catalyst & Chemical Management", Implementasi="Tahun Kedua", KodeSub="3.5", SubOps="Chemical Make Up Consumption", SubPanel="Chemical Make Up Consumption" },

                // 4. PROCESS CONTROL (Tahun Ketiga)
                new CpdpItem { Kompetensi="Process Control & Computer Ops", Implementasi="Tahun Ketiga", KodeSub="4.1", SubOps="Prinsip Dasar Pengendalian Variabel", SubPanel="Tunning Controller" },
                new CpdpItem { Kompetensi="Process Control & Computer Ops", Implementasi="Tahun Ketiga", KodeSub="4.2", SubOps="Prinsip Kerja Peralatan Pengendalian", SubPanel="Process Control Analysis" },
                new CpdpItem { Kompetensi="Process Control & Computer Ops", Implementasi="Tahun Ketiga", KodeSub="4.3", SubOps="Computer Operations", SubPanel="Computer Operations" },

                // 5. REFINERY PROCESS (Tahun Pertama)
                new CpdpItem { Kompetensi="Refinery Process Ops & Optimization", Implementasi="Tahun Pertama", KodeSub="5.1", SubOps="BOC / BEC", SubPanel="Routine Activities for Operations" },
                new CpdpItem { Kompetensi="Refinery Process Ops & Optimization", Implementasi="Tahun Pertama", KodeSub="5.2", SubOps="Feed & Product Specification", SubPanel="Pengoperasian HMI" },
                new CpdpItem { Kompetensi="Refinery Process Ops & Optimization", Implementasi="Tahun Pertama", KodeSub="5.3", SubOps="P&ID, Line Up & Lay Out", SubPanel="Operating Windows" },
                new CpdpItem { Kompetensi="Refinery Process Ops & Optimization", Implementasi="Tahun Pertama", KodeSub="5.4", SubOps="Start up, Shutdown & Emergency", SubPanel="Refinery Operations Analysis" },
                new CpdpItem { Kompetensi="Refinery Process Ops & Optimization", Implementasi="Tahun Pertama", KodeSub="5.5", SubOps="Prinsip Dasar Fasilitas Kilang", SubPanel="Troubleshooting & Problem Solving" },
                new CpdpItem { Kompetensi="Refinery Process Ops & Optimization", Implementasi="Tahun Pertama", KodeSub="5.6", SubOps="Identifikasi Bahaya Fasilitas Kilang", SubPanel="Operational Risk Identification" },

                // 5. REFINERY PROCESS (Tahun Kedua)
                new CpdpItem { Kompetensi="Refinery Process Ops & Optimization", Implementasi="Tahun Kedua", KodeSub="5.7", SubOps="Prinsip Dasar & Pengoperasian Alat", SubPanel="Pengoperasian & Interaksi Sub Proses" },
                new CpdpItem { Kompetensi="Refinery Process Ops & Optimization", Implementasi="Tahun Kedua", KodeSub="5.8", SubOps="Pengoperasian Sub Proses", SubPanel="Start up, Shutdown & Emergency Unit" },
                new CpdpItem { Kompetensi="Refinery Process Ops & Optimization", Implementasi="Tahun Kedua", KodeSub="5.9", SubOps="Equipment Operations", SubPanel="Process Optimization" },
                
                // 5. REFINERY PROCESS (Tahun Ketiga)
                new CpdpItem { Kompetensi="Refinery Process Ops & Optimization", Implementasi="Tahun Ketiga", KodeSub="5.10", SubOps="Karakteristik Unit Operasi", SubPanel="-" },
                new CpdpItem { Kompetensi="Refinery Process Ops & Optimization", Implementasi="Tahun Ketiga", KodeSub="5.11", SubOps="Day to Day Monitoring", SubPanel="-" },
                new CpdpItem { Kompetensi="Refinery Process Ops & Optimization", Implementasi="Tahun Ketiga", KodeSub="5.12", SubOps="Data Collecting for Optimization", SubPanel="-" },
                new CpdpItem { Kompetensi="Refinery Process Ops & Optimization", Implementasi="Tahun Ketiga", KodeSub="5.13", SubOps="Operating Windows", SubPanel="-" },
            };

            return View(cpdpData);
        }

        // --- HALAMAN 3: ASSESSMENT LOBBY ---
        public IActionResult Assessment()
        {
            // 1. KITA BUAT LIST DATA (Pastikan variabel ini ada)
            var exams = new List<AssessmentSession>
            {
                new AssessmentSession { 
                    Id = 101, Title = "PROTON Simulation: Amine Unit", Category = "PROTON", 
                    Schedule = DateTime.Now, DurationMinutes = 90, Status = "Open", IsTokenRequired = true 
                },
                new AssessmentSession { 
                    Id = 102, Title = "Technical GTK Level 2", Category = "Technical", 
                    Schedule = DateTime.Now.AddDays(5), DurationMinutes = 60, Status = "Upcoming", IsTokenRequired = false 
                },
                new AssessmentSession { 
                    Id = 90, Title = "Basic Safety Awareness", Category = "HSE", 
                    Schedule = DateTime.Now.AddMonths(-2), DurationMinutes = 45, Status = "Completed", Score = 88, IsTokenRequired = false 
                }
            };

            // 2. KRUSIAL: Data 'exams' HARUS dimasukkan ke dalam kurung View()
            // KALAU INI KOSONG -> HALAMAN AKAN BLANK/ERROR
            return View(exams); 
        }

        // (Biarkan fungsi Records/Capability yang lama tetap ada di bawah sini)
        public IActionResult Records()
        {
             // ... kodingan capability records yg lama ...
             return View(new List<TrainingRecord>()); // Placeholder biar tidak error
        }
    }
}