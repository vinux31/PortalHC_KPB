using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using HcPortal.Models;

namespace HcPortal.Controllers
{
    public class CMPController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public CMPController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        // --- HALAMAN 1: SUSUNAN KKJ (MATRIX VIEW) ---
        public async Task<IActionResult> Kkj(string? section)
        {
            // Get current user role
            var user = await _userManager.GetUserAsync(User);
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var userRole = userRoles.FirstOrDefault();
            
            ViewBag.UserRole = userRole;
            ViewBag.SelectedSection = section;
            
            // If HC or Section Head and no section selected, show selection page
            if ((userRole == "HC" || userRole == "Section Head") && string.IsNullOrEmpty(section))
            {
                return View("KkjSectionSelect");
            }

            var matrixData = new List<KkjMatrixItem>
            {
                // 1. Gas Processing Operations
                // Data: - 2 2 - 2 - - 2 2 2 1 2 2 - 2
                new KkjMatrixItem { No=1, SkillGroup="Engineering", SubSkillGroup="Production & Processing Operations and Maintenance", Indeks="6.2.2", Kompetensi="Gas Processing Operations", 
                    Target_SectionHead="-", Target_SrSpv_GSH="2", Target_ShiftSpv_GSH="2", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="2", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="2", Target_Panelman_ARU_12_13="2", Target_Panelman_ARU_14="2", Target_Operator_ARU_8_11="1", Target_Operator_ARU_12_13="2", 
                    Target_SrSpv_Facility="2", Target_JrAnalyst="-", Target_HSE="2" },

                // 2. Material & Chemical Blending
                // Data: - 2 2 - 1 - - 2 - 1 - - 2 - 1
                new KkjMatrixItem { No=2, SkillGroup="Engineering", SubSkillGroup="Production & Processing Operations and Maintenance", Indeks="6.2.5", Kompetensi="Material & Chemical Blending", 
                    Target_SectionHead="-", Target_SrSpv_GSH="2", Target_ShiftSpv_GSH="2", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="1", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="2", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="1", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="2", Target_JrAnalyst="-", Target_HSE="1" },

                // 3. Oil Processing Operations
                // Data: 3 2 2 2 2 1 2 2 - 2 - - 2 - 2
                new KkjMatrixItem { No=3, SkillGroup="Engineering", SubSkillGroup="Production & Processing Operations and Maintenance", Indeks="6.2.6", Kompetensi="Oil Processing Operations", 
                    Target_SectionHead="3", Target_SrSpv_GSH="2", Target_ShiftSpv_GSH="2", 
                    Target_Panelman_GSH_12_13="2", Target_Panelman_GSH_14="2", Target_Operator_GSH_8_11="1", Target_Operator_GSH_12_13="2",
                    Target_ShiftSpv_ARU="2", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="2", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="2", Target_JrAnalyst="-", Target_HSE="2" },

                // 4. Refinery Process Operations
                // Data: 3 3 3 2 3 2 2 3 2 3 2 2 3 2 3
                new KkjMatrixItem { No=4, SkillGroup="Engineering", SubSkillGroup="Production & Processing Operations and Maintenance", Indeks="6.2.7", Kompetensi="Refinery Process Operations", 
                    Target_SectionHead="3", Target_SrSpv_GSH="3", Target_ShiftSpv_GSH="3", 
                    Target_Panelman_GSH_12_13="2", Target_Panelman_GSH_14="3", Target_Operator_GSH_8_11="2", Target_Operator_GSH_12_13="2",
                    Target_ShiftSpv_ARU="3", Target_Panelman_ARU_12_13="2", Target_Panelman_ARU_14="3", Target_Operator_ARU_8_11="2", Target_Operator_ARU_12_13="2", 
                    Target_SrSpv_Facility="3", Target_JrAnalyst="2", Target_HSE="3" },

                // 5. Catalyst & Chemical Management
                // Data: - 2 2 1 1 1 1 1 1 1 1 2 1 -
                // Wait user text row 5: "Process Engineering 7.1.1 ... 2 2 1 1 1 1 1 1 1 1 1 2 1 -"
                // Let's re-verify row 5 columns
                // 1(SH):-, 2(SrG):2, 3(ShG):2, 4(Pn12):1, 5(Pn14):1, 6(Op8):1, 7(Op12):1, 8(ShARU):1, 9(PnA12):1, 10(PnA14):1, 11(OpA8):1, 12(OpA12):2, 13(Fac):1, 14(Jr):- ... wait user text length
                // User text: 2 2 1 1 1 1 1 1 1 1 1 2 1 - (14 values)
                // My columns: 15. The first column SH is empty. User text starts with empty field.
                // So: SH:-, SrG:2, ShG:2, P12:1, P14:1, O8:1, O12:1, ShA:1, PA12:1, PA14:1, OA8:1, OA12:2, Fac:1, Jr:-, HSE:-?
                // Text end: ... 1 2 1 - ...
                // SrSpvFac:1, Jr:-, HSE:-? NO.
                // Let's map backwards from end. HSE: - . Jr: -. Fac: 1.
                // OpA12: 2. OpA8: 1. PA14: 1. PA12: 1. ShA: 1. ...
                // Correct.
                new KkjMatrixItem { No=5, SkillGroup="Engineering", SubSkillGroup="Process Engineering", Indeks="7.1.1", Kompetensi="Catalyst & Chemical Management", 
                    Target_SectionHead="-", Target_SrSpv_GSH="2", Target_ShiftSpv_GSH="2", 
                    Target_Panelman_GSH_12_13="1", Target_Panelman_GSH_14="1", Target_Operator_GSH_8_11="1", Target_Operator_GSH_12_13="1",
                    Target_ShiftSpv_ARU="1", Target_Panelman_ARU_12_13="1", Target_Panelman_ARU_14="1", Target_Operator_ARU_8_11="1", Target_Operator_ARU_12_13="2", 
                    Target_SrSpv_Facility="1", Target_JrAnalyst="-", Target_HSE="-" },

                // 6. Energy Management
                // Data: 3 3 3 2 3 2 2 1 2 3 2 2 - 2 -
                new KkjMatrixItem { No=6, SkillGroup="Engineering", SubSkillGroup="Process Engineering", Indeks="7.1.2", Kompetensi="Energy Management", 
                    Target_SectionHead="3", Target_SrSpv_GSH="3", Target_ShiftSpv_GSH="3", 
                    Target_Panelman_GSH_12_13="2", Target_Panelman_GSH_14="3", Target_Operator_GSH_8_11="2", Target_Operator_GSH_12_13="2",
                    Target_ShiftSpv_ARU="1", Target_Panelman_ARU_12_13="2", Target_Panelman_ARU_14="3", Target_Operator_ARU_8_11="2", Target_Operator_ARU_12_13="2", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="2", Target_HSE="-" },

                // 7. Process Control
                // Data: - 2 1 2 1 1 2 1 2 1 1 - - -
                // 1(SH):-, 2(Sr):-, 3(Sv):2, 4(P12):1, 5(P14):2, 6(O8):1, 7(O12):1, 8(SA):2, 9(PA12):1, 10(PA14):2, 11(OA8):1, 12(OA12):1, 13:- 14:- 15:-
                new KkjMatrixItem { No=7, SkillGroup="Engineering", SubSkillGroup="Process Engineering", Indeks="7.1.4", Kompetensi="Process Control", 
                    Target_SectionHead="-", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="2", 
                    Target_Panelman_GSH_12_13="1", Target_Panelman_GSH_14="2", Target_Operator_GSH_8_11="1", Target_Operator_GSH_12_13="1",
                    Target_ShiftSpv_ARU="2", Target_Panelman_ARU_12_13="1", Target_Panelman_ARU_14="2", Target_Operator_ARU_8_11="1", Target_Operator_ARU_12_13="1", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="-", Target_HSE="-" },

                // 8. Commissioning & Operational Readiness
                // Data: 3 3 2 2 2 2 2 2 2 2 2 2 3 2 2
                new KkjMatrixItem { No=8, SkillGroup="HSSE", SubSkillGroup="Project Engineering", Indeks="7.7.1", Kompetensi="Commissioning & Operational Readiness", 
                    Target_SectionHead="3", Target_SrSpv_GSH="3", Target_ShiftSpv_GSH="2", 
                    Target_Panelman_GSH_12_13="2", Target_Panelman_GSH_14="2", Target_Operator_GSH_8_11="2", Target_Operator_GSH_12_13="2",
                    Target_ShiftSpv_ARU="2", Target_Panelman_ARU_12_13="2", Target_Panelman_ARU_14="2", Target_Operator_ARU_8_11="2", Target_Operator_ARU_12_13="2", 
                    Target_SrSpv_Facility="3", Target_JrAnalyst="2", Target_HSE="2" },
                
                // 9. Cost Engineering
                // Data: 2 - - - - - - - - - - - 2 1 -
                new KkjMatrixItem { No=9, SkillGroup="HSSE", SubSkillGroup="Project Engineering", Indeks="7.7.2", Kompetensi="Cost Engineering", 
                    Target_SectionHead="2", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="2", Target_JrAnalyst="1", Target_HSE="-" },

                // 10. Sourcing Procurement
                // Data: 2 - - - - - - - - - - - - - -
                new KkjMatrixItem { No=10, SkillGroup="Operation & Maintenance", SubSkillGroup="Procurement", Indeks="9.2.3", Kompetensi="Sourcing Procurement", 
                    Target_SectionHead="2", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="-", Target_HSE="-" },

                // 11. Process Hazard Analysis
                // Data: 2 - - - - - - - - - - - - - 2
                new KkjMatrixItem { No=11, SkillGroup="Operation & Maintenance", SubSkillGroup="Safety", Indeks="12.2.12", Kompetensi="Process Hazard Analysis", 
                    Target_SectionHead="2", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="-", Target_HSE="2" },

                // 12. Process Safety Management
                // Data: - - - - - - - - - - - - - - 1
                new KkjMatrixItem { No=12, SkillGroup="Operation & Maintenance", SubSkillGroup="Safety", Indeks="12.2.13", Kompetensi="Process Safety Management", 
                    Target_SectionHead="-", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="-", Target_HSE="1" },

                // 13. Safe Work Practice
                // Data: 3 3 3 2 2 2 2 3 2 2 2 2 3 2 3
                new KkjMatrixItem { No=13, SkillGroup="Operation & Maintenance", SubSkillGroup="Safety", Indeks="12.2.14", Kompetensi="Safe Work Practice & Lifesaving Rules", 
                    Target_SectionHead="3", Target_SrSpv_GSH="3", Target_ShiftSpv_GSH="3", 
                    Target_Panelman_GSH_12_13="2", Target_Panelman_GSH_14="2", Target_Operator_GSH_8_11="2", Target_Operator_GSH_12_13="2",
                    Target_ShiftSpv_ARU="3", Target_Panelman_ARU_12_13="2", Target_Panelman_ARU_14="2", Target_Operator_ARU_8_11="2", Target_Operator_ARU_12_13="2", 
                    Target_SrSpv_Facility="3", Target_JrAnalyst="2", Target_HSE="3" },

                 // 14. Incident Investigation
                 // Data: - - - - - - - - - - - - - - 1
                new KkjMatrixItem { No=14, SkillGroup="Operation & Maintenance", SubSkillGroup="Safety", Indeks="12.2.9", Kompetensi="Incident Investigation", 
                    Target_SectionHead="-", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="-", Target_HSE="1" },

                // 15. Individual Performance Management
                // Data: 2 - - - - - - - - - - - - - -
                new KkjMatrixItem { No=15, SkillGroup="Operation & Maintenance", SubSkillGroup="People Management", Indeks="13.2.6", Kompetensi="Individual Performance Management", 
                    Target_SectionHead="2", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="-", Target_HSE="-" },

                // 16. Manpower Planning
                // Data: 2 2 - - - - - - - - - - - - -
                new KkjMatrixItem { No=16, SkillGroup="Operation & Maintenance", SubSkillGroup="People Management", Indeks="13.2.8", Kompetensi="Manpower Planning", 
                    Target_SectionHead="2", Target_SrSpv_GSH="2", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="-", Target_JrAnalyst="-", Target_HSE="-" },

                // 17. Project Risk Management
                // Data: 2 - - - - - - - - - - - - 2 1 -
                new KkjMatrixItem { No=17, SkillGroup="Operation & Maintenance", SubSkillGroup="Risk Management", Indeks="15.1.3", Kompetensi="Project Risk Management", 
                    Target_SectionHead="2", Target_SrSpv_GSH="-", Target_ShiftSpv_GSH="-", 
                    Target_Panelman_GSH_12_13="-", Target_Panelman_GSH_14="-", Target_Operator_GSH_8_11="-", Target_Operator_GSH_12_13="-",
                    Target_ShiftSpv_ARU="-", Target_Panelman_ARU_12_13="-", Target_Panelman_ARU_14="-", Target_Operator_ARU_8_11="-", Target_Operator_ARU_12_13="-", 
                    Target_SrSpv_Facility="2", Target_JrAnalyst="1", Target_HSE="-" }
            };

            return View(matrixData);
        }

        // --- HALAMAN 2: MAPPING KKJ - CPDP ---
        public IActionResult Mapping()
        {
            var cpdpData = new List<CpdpItem>
            {
                // 1. Safe Work Practice & Lifesaving Rules
                new CpdpItem { No="1", NamaKompetensi="Safe Work Practice & Lifesaving Rules", IndikatorPerilaku="2 (Intermediate)", 
                    DetailIndikator="Memahami dan mampu menerapkan cara kerja yang aman (safe work practice & lifesaving rules) sesuai dengan risiko keselamatan terkait aktivitasnya", 
                    Silabus="1.1. Safe Work Practice", 
                    TargetDeliverable="Target 1.1\n1. Mampu memahami 5 Tingkatan Budaya HSSE.\n2. Mampu memahami Pengertian Bahaya menurut standar ISO & OSHA.\n3. Mampu memahami Lessons Learned.\n4. Mampu memahami 9 Perilaku Wajib.\n5. Mampu memahami Grafik Flammable Range, sumber dan jenis-jenis Rambatan Panas.\n6. Mampu memahami Pengamatan Keselamatan Kerja (PEKA)\n7. Mampu memahami Skill Champion SLP (Safety Leadership Program)\na. Skill 1 : Memberikan Perintah Kerja yang Aman\nb. Skill 2 : Memberikan Penghargaan terhadap Perilaku Aman\nc. Skill 3 : Membimbing Perilaku yang Kurang Aman\nd. Skill 4 : Menghentikan Pekerjaan yang Tidak Aman" },
                new CpdpItem { No="1", NamaKompetensi="Safe Work Practice & Lifesaving Rules", IndikatorPerilaku="2 (Intermediate)", 
                    DetailIndikator="Memahami dan mampu menerapkan cara kerja yang aman (safe work practice & lifesaving rules) sesuai dengan risiko keselamatan terkait aktivitasnya", 
                    Silabus="1.2. Lifesaving Rules", 
                    TargetDeliverable="Target 1.2\n1. Mengetahui Regulasi Pemerintah tentang HSSE:\na. Aturan Perundang-undangan tentang Keselamatan Kerja\nb. Aturan Perundang-undangan tentang Perlindungan dan Pengelolaan Lingkungan Hidup\nc. Peraturan Pemerintah tentang Keselamatan Kerja Pada Pemurnian dan Pengolahan Minyak dan Gas Bumi\nd. Peraturan Pemerintah tentang Pedoman Penerapan SMK3\n2. Mampu memahami HSSE Golden Rules\n3. Mampu memahami Safety Data Sheet\n4. Mampu memahami 10 Corporate Life Saving Rules (CLSR) 2024\n5. Mampu memahami Pembuatan Job Safety Analysis (JSA)\n6. Mampu memahami Prosedur & Pembuatan Surat Ijin Kerja Aman (SIKA)\n7. Mengetahui dan memahami hasil pengujian alat gas tester dalam SIKA panas & Ruang Terbatas (Hot Work Permit & Confined Space)\n8. Mampu memahami Prosedur Pengukuran Gas Mudah Terbakar\n9. Mampu memahami Fire & Gas Detector System (FGDS)" },
                new CpdpItem { No="1", NamaKompetensi="Safe Work Practice & Lifesaving Rules", IndikatorPerilaku="2 (Intermediate)", 
                    DetailIndikator="Memahami dan mampu menerapkan mitigasi yang harus dilaksanakan sesuai aturan, standar, dan instruksi keselamatan yang berlaku", 
                    Silabus="1.1. Safe Work Practice", 
                    TargetDeliverable="Target 1.1\n1. Mampu memahami 5 Tingkatan Budaya HSSE.\n2. Mampu memahami Pengertian Bahaya menurut standar ISO & OSHA.\n3. Mampu memahami Lessons Learned.\n4. Mampu memahami 9 Perilaku Wajib.\n5. Mampu memahami Grafik Flammable Range, sumber dan jenis-jenis Rambatan Panas.\n6. Mampu memahami Pengamatan Keselamatan Kerja (PEKA)\n7. Mampu memahami Skill Champion SLP (Safety Leadership Program)\na. Skill 1 : Memberikan Perintah Kerja yang Aman\nb. Skill 2 : Memberikan Penghargaan terhadap Perilaku Aman\nc. Skill 3 : Membimbing Perilaku yang Kurang Aman\nd. Skill 4 : Menghentikan Pekerjaan yang Tidak Aman" },
                new CpdpItem { No="1", NamaKompetensi="Safe Work Practice & Lifesaving Rules", IndikatorPerilaku="2 (Intermediate)", 
                    DetailIndikator="Memahami dan mampu menerapkan mitigasi yang harus dilaksanakan sesuai aturan, standar, dan instruksi keselamatan yang berlaku", 
                    Silabus="1.2. Lifesaving Rules", 
                    TargetDeliverable="Target 1.2\n1. Mengetahui Regulasi Pemerintah tentang HSSE:\na. Aturan Perundang-undangan tentang Keselamatan Kerja\nb. Aturan Perundang-undangan tentang Perlindungan dan Pengelolaan Lingkungan Hidup\nc. Peraturan Pemerintah tentang Keselamatan Kerja Pada Pemurnian dan Pengolahan Minyak dan Gas Bumi\nd. Peraturan Pemerintah tentang Pedoman Penerapan SMK3\n2. Mampu memahami HSSE Golden Rules\n3. Mampu memahami Safety Data Sheet\n4. Mampu memahami 10 Corporate Life Saving Rules (CLSR) 2024\n5. Mampu memahami Pembuatan Job Safety Analysis (JSA)\n6. Mampu memahami Prosedur & Pembuatan Surat Ijin Kerja Aman (SIKA)\n7. Mengetahui dan memahami hasil pengujian alat gas tester dalam SIKA panas & Ruang Terbatas (Hot Work Permit & Confined Space)\n8. Mampu memahami Prosedur Pengukuran Gas Mudah Terbakar\n9. Mampu memahami Fire & Gas Detector System (FGDS)" },
                new CpdpItem { No="1", NamaKompetensi="Safe Work Practice & Lifesaving Rules", IndikatorPerilaku="2 (Intermediate)", 
                    DetailIndikator="Memahami dan mampu menerapkan penanggulangan apabila terjadi kondisi darurat (pelaporan insiden, pertolongan pertama, penanganan lanjutan terhadap korban dan penanggulangan kebakaran atau spill yang terjadi, dll.)", 
                    Silabus="1.3. Emergency Response", 
                    TargetDeliverable="Target 1.3\n1. Mampu memahami Metode Pemadaman Kebakaran\n2. Mampu memahami Media Pemadam Kebakaran & lokasi alat pemadam kebakaran yang tersedia di masing-masing unit GAST\n3. Mampu memahami Prosedur Keadaan Darurat\n4. Mampu memahami Jenis-jenis Peralatan Proteksi Kebakaran" },
                
                // 2. Energy Management
                new CpdpItem { No="2", NamaKompetensi="Energy Management", IndikatorPerilaku="2 (Intermediate)", 
                    DetailIndikator="Memahami karakteristik energi yang digunakan (listrik, steam, fuel oil, fuel gas, dll.)", 
                    Silabus="2.1. Karakteristik Energi", 
                    TargetDeliverable="Target 2.1\n1. Mengetahui sumber fuel gas dan spesifikasinya.\n2. Mengetahui parameter kondisi operasi fuel gas.\n3. Mengetahui parameter kondisi operasi HDS Heater." },
                new CpdpItem { No="2", NamaKompetensi="Energy Management", IndikatorPerilaku="2 (Intermediate)", 
                    DetailIndikator="Memahami prinsip - prinsip dasar equipment yang menggunakan energi", 
                    Silabus="2.2. Prinsip – Prinsip Dasar Peralatan yang Menggunakan Energi", 
                    TargetDeliverable="Target 2.2\n1. Mampu memahami fungsi dan prinsip kerja dari HDS Heater (F-053-01) & Splitter Reboiler (E-053-05) sebagai peralatan yang menggunakan energi.\n2. Mampu memahami sistem pengaman (Interlock, Alarm, dan Permissive) pada HDS Heater dan reboiler sistem." },
                new CpdpItem { No="2", NamaKompetensi="Energy Management", IndikatorPerilaku="2 (Intermediate)", 
                    DetailIndikator="Mampu mengumpulkan data - data yang diperlukan untuk evaluasi efisiensi penggunaan energi", 
                    Silabus="2.3. Data Collecting for Energy Consumption Evaluation", 
                    TargetDeliverable="Target 2.3\n1. Mengetahui tag number dan instrumentasi untuk kebutuhan evaluasi penggunaan energi di Unit RFCC NHT" },

                // 3. Catalyst & Chemical Management
                new CpdpItem { No="3", NamaKompetensi="Catalyst & Chemical Management", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="Memahami jenis-jenis dan fungsi catalyst dan chemical", 
                    Silabus="3.1. Jenis & Fungsi Catalyst & Chemical", 
                    TargetDeliverable="Target 3.1\n1. Memahami fungsi catalyst dan chemical pada unit RFCC NHT" },
                new CpdpItem { No="3", NamaKompetensi="Catalyst & Chemical Management", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="Memahami karakteristik/ performance catalyst dan chemical", 
                    Silabus="3.2. Karakteristik Catalyst & Chemical", 
                    TargetDeliverable="Target 3.2\n1. Mampu menjelaskan karakteristik catalyst dan chemical yang digunakan di unit RFCC NHT\n2. Mampu menjelaskan parameter teknis yang mempengaruhi performance catalyst pada unit RFCC NHT\n3. Mampu membaca, memahami, dan menggunakan data karakteristik chemical dari dokumen teknis seperti datasheet, MSDS / operating manual" },
                new CpdpItem { No="3", NamaKompetensi="Catalyst & Chemical Management", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="Memahami impurities di feed yang dapat berpengaruh terhadap performance catalyst dan chemical", 
                    Silabus="3.3. Pengaruh Impurities pada Catalyst & Chemical Performance", 
                    TargetDeliverable="Target 3.3\n1. Mampu mengidentifikasi jenis-jenis impurities yang dapat memengaruhi performance pada catalyst dan chemical yang digunakan\n2. Mampu menjelaskan dampak impurities terhadap performance katalis dan chemical" },
                new CpdpItem { No="3", NamaKompetensi="Catalyst & Chemical Management", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="Mampu melakukan perhitungan atas make up pemakaian catalyst sesuai kapasitas pengolahan atau kandungan impurities", 
                    Silabus="3.4. Catalyst Make Up Consumption", 
                    TargetDeliverable="Target 3.4\n1. Menjelaskan proses loading/unloading catalyst pada unit RFCC NHT.\n2. Mampu menjelaskan proses skimming pada catalyst.\n3. Mampu menjelaskan proses sulfiding pada catalyst SHU,Main HDS dan Finishing HDS reaktor." },
                new CpdpItem { No="3", NamaKompetensi="Catalyst & Chemical Management", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="Mampu melakukan perhitungan kebutuhan chemical", 
                    Silabus="3.5. Chemical Make Up Consumption", 
                    TargetDeliverable="Target 3.5\n1. Mampu menjelaskan Lokasi SC (Sampling Connection) untuk Chemical.\n2. Mampu menjelaskan langkah - langkah make up chemical." },

                // 4. Process Control & Computer Operations
                new CpdpItem { No="4", NamaKompetensi="Process Control & Computer Operations", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="Memahami prinsip-prinsip dasar pengendalian dan pengukuran variabel-variabel operasi seperti pengendalian tekanan, pengendalian temperatur, dll.", 
                    Silabus="4.1. Prinsip Dasar Pengendalian & Pengukuran Variabel Operasi", 
                    TargetDeliverable="Target 4.1\n1. Mampu memahami Basic Process Control, Cascade Control, opposite range, Split Range Control dan ratio control\n2. Mampu memahami field instrument pada unit proses\n3. Mampu memahami istilah-istilah umum yang ada di HMI DCS\n4. Mampu membaca dan memahami proses flow suatu unit melalui display yang ada di HMI DCS\n5. Mampu mengoperasikan alat ukur bantu parameter operasi di GAST" },
                new CpdpItem { No="4", NamaKompetensi="Process Control & Computer Operations", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="Memahami prinsip-prinsip kerja dari peralatan pengendalian proses, seperti Field Instrument, Control loop, PLC, DCS, dll.", 
                    Silabus="4.2. Prinsip Kerja Peralatan Pengendalian Proses", 
                    TargetDeliverable="Target 4.2\n1. Mampu memahami konsep dasar loop instrument.\n2. Mampu memahami instrumen di lapangan.\n3. Mampu memahami prinsip kerja dan fungsi control valve." },
                new CpdpItem { No="4", NamaKompetensi="Process Control & Computer Operations", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="-", 
                    Silabus="4.3. Computer Operations (Sub Kompetensi ini penambahan oleh tim SME GAST)", 
                    TargetDeliverable="Target 4.3\n1. Mampu mengoperasikan aplikasi - aplikasi penunjang proses yang disediakan perusahaan, seperti Web STK dan Virtual Intranet Access.\n2. Mampu mengoperasikan Autodesk Naviswork.\n3. Mampu mengoperasikan Microsoft Office/Microsoft 365 secara umum, seperti: Word, Excel, PowerPoint, Visio, Outlook, OneDrive, Teams." },

                // 5.1 Refinery Process Operations
                new CpdpItem { No="5.1", NamaKompetensi="Refinery Process Operations", IndikatorPerilaku="2 (Intermediate)", 
                    DetailIndikator="Memahami prinsip-prinsip dasar dan mampu menjalankan prosedur pengoperasian fasilitas refinery process sesuai standar K3L dengan bimbingan", 
                    Silabus="5.5. Prinsip Dasar & Pengoperasian Fasilitas Kilang", 
                    TargetDeliverable="Target 5.5\n1. Memahami distribusi dan fungsi utilitas yang digunakan untuk menunjang proses unit RFCC NHT.\n2. Mampu membuat block diagram dan process flow diagram unit RFCC NHT.\n3. Mampu memahami deskripsi proses RFCC NHT dari feed hingga menghasilkan produk akhir dengan PFD yang telah dibuat pada point No. 2." },
                new CpdpItem { No="5.1", NamaKompetensi="Refinery Process Operations", IndikatorPerilaku="2 (Intermediate)", 
                    DetailIndikator="Memahami perkembangan informasi terkait pengoperasian fasilitas refinery process", 
                    Silabus="5.9. Equipment Operations", 
                    TargetDeliverable="Target 5.9\n1. Mampu membaca drawing equipment pada Process Data Sheet.\n2. Mampu memahami prinsip kerja dan fungsi equipment pada unit RFCC NHT.\n3. Mampu memahami persiapan yang dilakukan sebelum mengoperasikan equipment.\n4. Mampu memahami dampak yang terjadi jika terdapat kegagalan beroperasi pada equipment di RFCC NHT.\n5. Mampu memahami prosedur shutdown critical equipment di RFCC NHT." },
                new CpdpItem { No="5.1", NamaKompetensi="Refinery Process Operations", IndikatorPerilaku="2 (Intermediate)", 
                    DetailIndikator="Mampu melakukan BOC/BEC dengan pengawasan ketat", 
                    Silabus="5.1. BOC / BEC", 
                    TargetDeliverable="Target 5.1\n1. Mampu memahami filosofi dari kegiatan plant monitoring (BOC+).\n2. Mampu menyebutkan batas-batas normal kondisi operasi pada critical equipment saat normal operasi mengacu pada plant monitoring (BOC+)." },
                new CpdpItem { No="5.1", NamaKompetensi="Refinery Process Operations", IndikatorPerilaku="2 (Intermediate)", 
                    DetailIndikator="Mampu melakukan identifikasi bahaya dari permasalahan pada operasi fasilitas refinery process", 
                    Silabus="5.6. Identifikasi Bahaya pada Pengoperasian Fasilitas Kilang", 
                    TargetDeliverable="Target 5.6\n1. Mampu mengidentifikasi potensi bahaya operasional pada masing-masing section dan peralatan di unit RFCC NHT.\n2. Mampu menjelaskan safeguard system, permissive to start dan pengoperasian serta berpengalaman mengoperasikan equipment di RFCC NHT\n3. Mampu memahami decontamination philosophy dan contohnya." },
                new CpdpItem { No="5.1", NamaKompetensi="Refinery Process Operations", IndikatorPerilaku="2 (Intermediate)", 
                    DetailIndikator="-", 
                    Silabus="5.2. Feed & Product Specification", 
                    TargetDeliverable="Target 5.2\n1. Mampu memahami karakteristik feed RFCC NHT (Hot Naphta ex. 052 RFCC).\n2. Mampu memahami root cause dan corrective action jika terdapat produk yang off spec.\n3. Mampu menyebutkan spesifikasi dari setiap produk unit RFCC NHT." },
                new CpdpItem { No="5.1", NamaKompetensi="Refinery Process Operations", IndikatorPerilaku="2 (Intermediate)", 
                    DetailIndikator="-", 
                    Silabus="5.3. P&ID, Line Up & Lay Out", 
                    TargetDeliverable="Target 5.3\n1. Mampu memahami dan membaca simbol-simbol P&ID.\n2. Mampu menghafal lokasi unit RFCC NHT pada Dokumen Overall Site Plan.\n3. Mampu menghafal Plot Plant area unit RFCC NHT untuk mengetahui lokasi equipment prosesnya.\n4. Mampu menghafal tag number dan nama yang digunakan untuk penamaan plant dan equipment di unit RFCC NHT.\n5. Mampu melakukan line up unit RFCC NHT dengan menggunakan 3D Navisworks." },
                new CpdpItem { No="5.1", NamaKompetensi="Refinery Process Operations", IndikatorPerilaku="2 (Intermediate)", 
                    DetailIndikator="-", 
                    Silabus="5.4. Start Up, Shutdown & Emergency Unit", 
                    TargetDeliverable="Target 5.4\n1. Mampu memahami tahapan kegiatan PSSR (Pre-Startup Safety Review) dan tanggung jawab PSSR sesuai dengan scope kegiatan Tim Operasi.\n2. Mampu menjelaskan tahapan startup RFCC NHT.\n3. Mampu menjelaskan safeguard system dan permissive to start critical equipment RFCC NHT.\n4. Mampu menjelaskan tahapan kegiatan shutdown RFCC NHT\n5. Mampu menjelaskan prosedur shutdown critical equipment di RFCC NHT\n6. Mampu menyebutkan dan menjelaskan kegagalan utilities yang dapat menyebabkan terjadinya emergency shutdown dan melakukan correction action untuk mengamankan unit\n7. Mampu menjelaskan dampak yang terjadi jika terjadi kegagalan beroperasi pada equipment di RFCC NHT dan tindakan yang dilakukan untuk mengatasi permasalahan" },

                // 5.2 Oil Processing Operations
                new CpdpItem { No="5.2", NamaKompetensi="Oil Processing Operations", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="Memahami prinsip-prinsip dasar dan mampu menjalankan prosedur pengoperasian fasilitas oil processing secara aman, andal, dan optimal sesuai standar K3L dengan bimbingan", 
                    Silabus="5.7. Prinsip Dasar & Pengoperasian Peralatan", 
                    TargetDeliverable="Target 5.7\n1. Mampu menjelaskan prinsip dasar kerja masing-masing peralatan pada unit RFCC NHT.\n2. Mampu menjelaskan troubleshooting equipment – equipment critical di unit RFCC NHT ketika terjadi trouble." },
                new CpdpItem { No="5.2", NamaKompetensi="Oil Processing Operations", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="Memahami perkembangan informasi terkait pengoperasian fasilitas oil processing", 
                    Silabus="5.9. Equipment Operations", 
                    TargetDeliverable="Target 5.9\n1. Mampu membaca drawing equipment pada Process Data Sheet.\n2. Mampu memahami prinsip kerja dan fungsi equipment pada unit RFCC NHT.\n3. Mampu memahami persiapan yang dilakukan sebelum mengoperasikan equipment.\n4. Mampu memahami dampak yang terjadi jika terdapat kegagalan beroperasi pada equipment di RFCC NHT.\n5. Mampu memahami prosedur shutdown critical equipment di RFCC NHT." },
                new CpdpItem { No="5.2", NamaKompetensi="Oil Processing Operations", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="Mampu melakukan pengoperasian sub proses/area oil processing tertentu", 
                    Silabus="5.8. Pengoperasian Sub Proses", 
                    TargetDeliverable="Target 5.8\n1. Mampu memahami langkah-langkah pengoperasian pada seksi SHU Reactor.\n2. Mampu memahami langkah-langkah pengoperasian pada seksi Splitter.\n3. Mampu memahami langkah-langkah pengoperasian pada seksi HDS Reaction.\n4. Mampu memahami langkah-langkah pengoperasian pada seksi HDS Separation.\n5. Mampu memahami langkah-langkah pengoperasian pada seksi Stabilizer." },
                new CpdpItem { No="5.2", NamaKompetensi="Oil Processing Operations", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="Mampu melakukan identifikasi bahaya dari permasalahan pada operasi fasilitas oil processing", 
                    Silabus="5.6. Identifikasi Bahaya pada Pengoperasian Fasilitas Kilang", 
                    TargetDeliverable="Target 5.6\n1. Mampu mengidentifikasi potensi bahaya operasional pada masing-masing section dan peralatan di unit RFCC NHT.\n2. Mampu menjelaskan safeguard system, permissive to start dan pengoperasian serta berpengalaman mengoperasikan equipment di RFCC NHT\n3. Mampu memahami decontamination philosophy dan contohnya." },

                // 5.3 Process Optimization
                new CpdpItem { No="5.3", NamaKompetensi="Process Optimization", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="Memahami prinsip dasar parameter proses operasi yang harus dimonitor dan batasan design (limitasi/ operating windows)", 
                    Silabus="5.13. Operating Windows", 
                    TargetDeliverable="Target 5.13\n1. Memahami langkah-langkah pengoperasian indicating controller.\n2. Mengidentifikasi dan menjelaskan batas atas/bawah dari variabel proses operasi." },
                new CpdpItem { No="5.3", NamaKompetensi="Process Optimization", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="Mampu memahami karakteristik unit operasi", 
                    Silabus="5.10. Karakteristik Unit Operasi", 
                    TargetDeliverable="Target 5.10\n1. Mampu memahami karakteristik operasi di unit RFCC NHT." },
                new CpdpItem { No="5.3", NamaKompetensi="Process Optimization", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="Mampu mengumpulkan data-data yang diperlukan untuk optimasi proses", 
                    Silabus="5.12. Data Collecting for Process Optimization", 
                    TargetDeliverable="Target 5.12\n1. Mampu mengoperasikan dan mengelola data dari PI." },
                new CpdpItem { No="5.3", NamaKompetensi="Process Optimization", IndikatorPerilaku="1 (Basic)", 
                    DetailIndikator="Mampu melakukan day to day monitoring berdasarkan hasil pengumpulan data dengan supervisi/ instruksi", 
                    Silabus="5.11. Day to Day Monitoring", 
                    TargetDeliverable="Target 5.11\n1. Mampu menjelaskan tata cara pengambilan sample di unit RFCC NHT serta lokasi sampling connection unit RFCC NHT\n2. Mengetahui bagian-bagian equipment yang menjadi visual daily check pada saat plant patrol untuk mengetahui lebih awal ketika terjadi bocoran/leak." }
            };

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
        public async Task<IActionResult> Records(string? section, string? category, string? subCategory, string? search, string? statusFilter, string? isFiltered)
        {
            // Get current user and role
            var user = await _userManager.GetUserAsync(User);
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var userRole = userRoles.FirstOrDefault();
            
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
            ViewBag.SelectedSection = section; // Can be null/empty for "Semua Bagian"
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedSubCategory = subCategory;
            ViewBag.SearchTerm = search;
            ViewBag.SelectedStatus = statusFilter;
            ViewBag.IsInitialState = isInitialState;
            
            // Determine if we should show Status column (Filter Mode) or Stats columns (Default Mode)
            bool isFilterMode = !string.IsNullOrEmpty(category);
            ViewBag.IsFilterMode = isFilterMode;

            // Role: Coach/Coachee - Show personal training records (existing behavior)
            if (userRole == UserRoles.Coach || userRole == UserRoles.Coachee)
            {
                var personalRecords = GetPersonalTrainingRecords(user?.Id ?? "");
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
                workers = GetWorkersInSection(section, null, category, subCategory, search, statusFilter);
            }

            return View("RecordsWorkerList", workers);
        }
        
        // Helper method: Get personal training records for Coach/Coachee
        private List<TrainingRecord> GetPersonalTrainingRecords(string userId)
        {
            // Mock data - same as before
            return new List<TrainingRecord>
            {
                // === PROTON (2 items) ===
                new TrainingRecord 
                { 
                    Id = 1, 
                    Judul = "PROTON Assessment: Distillation Unit Operations", 
                    Kategori = "Proton", // Fixed casing to match dropdown
                    Tanggal = new DateTime(2024, 11, 15),
                    Penyelenggara = "NSO",
                    Status = "Passed",
                    CertificateType = "Permanent",
                    SertifikatUrl = "/certificates/proton-distillation-2024.pdf"
                },
                new TrainingRecord 
                { 
                    Id = 2, 
                    Judul = "PROTON Assessment: Heat Exchanger Systems", 
                    Kategori = "Proton", 
                    Tanggal = new DateTime(2025, 1, 10),
                    Penyelenggara = "NSO",
                    Status = "Wait Certificate",
                    CertificateType = "Permanent",
                    SertifikatUrl = null
                },

                // === OTS (2 items) ===
                new TrainingRecord 
                { 
                    Id = 3, 
                    Judul = "OTS Simulation: Emergency Shutdown Procedures", 
                    Kategori = "OTS", 
                    Tanggal = new DateTime(2024, 10, 5),
                    Penyelenggara = "Internal",
                    Status = "Passed",
                    CertificateType = "Permanent",
                    SertifikatUrl = "/certificates/ots-emergency-2024.pdf"
                },
                new TrainingRecord 
                { 
                    Id = 4, 
                    Judul = "OTS Simulation: Blackout Recovery", 
                    Kategori = "OTS", 
                    Tanggal = new DateTime(2024, 12, 20),
                    Penyelenggara = "Internal",
                    Status = "Passed",
                    CertificateType = "Permanent",
                    SertifikatUrl = "/certificates/ots-blackout-2024.pdf"
                },

                // === OJT (2 items) ===
                new TrainingRecord 
                { 
                    Id = 5, 
                    Judul = "On Job Training: Panel Operator Competency", 
                    Kategori = "OJT", 
                    Tanggal = new DateTime(2024, 9, 12),
                    Penyelenggara = "Internal",
                    Status = "Passed",
                    CertificateType = "Permanent",
                    SertifikatUrl = "/certificates/ojt-panel-2024.pdf"
                },
                new TrainingRecord 
                { 
                    Id = 6, 
                    Judul = "On Job Training: Field Operator Assessment", 
                    Kategori = "OJT", 
                    Tanggal = new DateTime(2024, 8, 25),
                    Penyelenggara = "Internal",
                    Status = "Passed",
                    CertificateType = "Permanent",
                    SertifikatUrl = "/certificates/ojt-field-2024.pdf"
                },

                // === IHT (2 items) ===
                new TrainingRecord 
                { 
                    Id = 7, 
                    Judul = "Internal Training: Pump Maintenance & Troubleshooting", 
                    Kategori = "IHT", 
                    Tanggal = new DateTime(2024, 7, 18),
                    Penyelenggara = "Internal",
                    Status = "Passed",
                    CertificateType = "Permanent",
                    SertifikatUrl = "/certificates/iht-pump-2024.pdf"
                },
                new TrainingRecord 
                { 
                    Id = 8, 
                    Judul = "Internal Training: Process Control Systems", 
                    Kategori = "IHT", 
                    Tanggal = new DateTime(2024, 6, 30),
                    Penyelenggara = "Internal",
                    Status = "Passed",
                    CertificateType = "Permanent",
                    SertifikatUrl = "/certificates/iht-process-2024.pdf"
                },

                // === MANDATORY HSSE (2 items with expiry dates) ===
                new TrainingRecord 
                { 
                    Id = 9, 
                    Judul = "Basic Fire Fighting & Emergency Response", 
                    Kategori = "MANDATORY", 
                    Tanggal = new DateTime(2024, 2, 10),
                    Penyelenggara = "External - HSSE Provider",
                    Status = "Valid",
                    CertificateType = "Annual",
                    ValidUntil = new DateTime(2025, 2, 10), // Expires in ~13 days (WARNING!)
                    SertifikatUrl = "/certificates/hsse-fire-2024.pdf"
                },
                new TrainingRecord 
                { 
                    Id = 10, 
                    Judul = "Working at Height Certification", 
                    Kategori = "MANDATORY", 
                    Tanggal = new DateTime(2023, 5, 15),
                    Penyelenggara = "External - HSSE Provider",
                    Status = "Valid",
                    CertificateType = "3-Year",
                    ValidUntil = new DateTime(2026, 5, 15), // Still valid for ~1.3 years
                    SertifikatUrl = "/certificates/hsse-height-2023.pdf"
                },
                
                 // === ADDITIONAL MOCK FOR HSSE SPECIFIC FILTER ===
                new TrainingRecord 
                { 
                    Id = 11, 
                    Judul = "Gas Tester", 
                    Kategori = "MANDATORY", 
                    Tanggal = new DateTime(2024, 1, 20),
                    Penyelenggara = "Internal",
                    Status = "Valid",
                    CertificateType = "2-Year",
                    ValidUntil = new DateTime(2026, 1, 20),
                    SertifikatUrl = "#"
                }
            };
        }
        
        // Helper method: Get all workers in a section (with optional filters)
        private List<WorkerTrainingStatus> GetWorkersInSection(string? section, string? unitFilter = null, string? category = null, string? subCategory = null, string? search = null, string? statusFilter = null)
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
                    w.NIP.Contains(search)
                ).ToList();
            }
            
            // 2. CALCULATE STATUS "SUDAH/BELUM"
            foreach (var worker in allWorkers)
            {
                // Only calculate dynamic status if a Category is selected
                if (!string.IsNullOrEmpty(category))
                {
                    // Load training records
                    worker.TrainingRecords = GetPersonalTrainingRecords(worker.WorkerId);
                    
                    bool isCompleted = false;

                    // Check if they have ANY passed/valid record in this Category
                    isCompleted = worker.TrainingRecords.Any(r => 
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