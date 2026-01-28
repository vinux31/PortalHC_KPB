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
                new CpdpItem { No="1", NamaKompetensi="Safe Work Practice & Lifesaving Rules", IndikatorPerilaku="2 (Intermediate)", DetailIndikator="Memahami dan mampu menerapkan cara kerja yang aman (safe work practice & lifesaving rules) sesuai dengan risiko keselamatan terkait aktivitasnya", Silabus="1.1. Safe Work Practice\n1.2. Lifesaving Rules", Status="Aligned" },
                new CpdpItem { No="1", NamaKompetensi="Safe Work Practice & Lifesaving Rules", IndikatorPerilaku="2 (Intermediate)", DetailIndikator="Memahami dan mampu menerapkan mitigasi yang harus dilaksanakan sesuai aturan, standar, dan instruksi keselamatan yang berlaku", Silabus="1.1. Safe Work Practice\n1.2. Lifesaving Rules", Status="Aligned" },
                new CpdpItem { No="1", NamaKompetensi="Safe Work Practice & Lifesaving Rules", IndikatorPerilaku="2 (Intermediate)", DetailIndikator="Memahami dan mampu menerapkan penanggulangan apabila terjadi kondisi darurat (pelaporan insiden, pertolongan pertama, penanganan lanjutan terhadap korban dan penanggulangan kebakaran atau spill yang terjadi, dll.)", Silabus="1.3. Emergency Response", Status="Aligned" },

                // 2. Energy Management
                new CpdpItem { No="2", NamaKompetensi="Energy Management", IndikatorPerilaku="2 (Intermediate)", DetailIndikator="Memahami karakteristik energi yang digunakan (listrik, steam, fuel oil, fuel gas, dll.)", Silabus="2.1. Karakteristik Energi", Status="Aligned" },
                new CpdpItem { No="2", NamaKompetensi="Energy Management", IndikatorPerilaku="2 (Intermediate)", DetailIndikator="Memahami prinsip - prinsip dasar equipment yang menggunakan energi", Silabus="2.2. Prinsip – Prinsip Dasar Peralatan yang Menggunakan Energi", Status="Aligned" },
                new CpdpItem { No="2", NamaKompetensi="Energy Management", IndikatorPerilaku="2 (Intermediate)", DetailIndikator="Mampu mengumpulkan data - data yang diperlukan untuk evaluasi efisiensi penggunaan energi", Silabus="2.3. Data Collecting for Energy Consumption Evaluation", Status="Aligned" },

                // 3. Catalyst & Chemical Management
                new CpdpItem { No="3", NamaKompetensi="Catalyst & Chemical Management", IndikatorPerilaku="1 (Basic)", DetailIndikator="Memahami jenis-jenis dan fungsi catalyst dan chemical", Silabus="3.1. Jenis & Fungsi Catalyst & Chemical", Status="Aligned" },
                new CpdpItem { No="3", NamaKompetensi="Catalyst & Chemical Management", IndikatorPerilaku="1 (Basic)", DetailIndikator="Memahami karakteristik/ performance catalyst dan chemical", Silabus="3.2. Karakteristik Catalyst & Chemical", Status="Aligned" },
                new CpdpItem { No="3", NamaKompetensi="Catalyst & Chemical Management", IndikatorPerilaku="1 (Basic)", DetailIndikator="Memahami impurities di feed yang dapat berpengaruh terhadap performance catalyst dan chemical", Silabus="3.3. Pengaruh Impurities pada Catalyst & Chemical Performance", Status="Aligned" },
                new CpdpItem { No="3", NamaKompetensi="Catalyst & Chemical Management", IndikatorPerilaku="1 (Basic)", DetailIndikator="Mampu melakukan perhitungan atas make up pemakaian catalyst sesuai kapasitas pengolahan atau kandungan impurities", Silabus="3.4. Catalyst Make Up Consumption", Status="Aligned" },
                new CpdpItem { No="3", NamaKompetensi="Catalyst & Chemical Management", IndikatorPerilaku="1 (Basic)", DetailIndikator="Mampu melakukan perhitungan kebutuhan chemical", Silabus="3.5. Chemical Make Up Consumption", Status="Aligned" },

                // 4. Process Control & Computer Operations
                new CpdpItem { No="4", NamaKompetensi="Process Control & Computer Operations", IndikatorPerilaku="1 (Basic)", DetailIndikator="Memahami prinsip-prinsip dasar pengendalian dan pengukuran variabel-variabel operasi seperti pengendalian tekanan, pengendalian temperatur, dll.", Silabus="4.1. Prinsip Dasar Pengendalian & Pengukuran Variabel Operasi", Status="Aligned" },
                new CpdpItem { No="4", NamaKompetensi="Process Control & Computer Operations", IndikatorPerilaku="1 (Basic)", DetailIndikator="Memahami prinsip-prinsip kerja dari peralatan pengendalian proses, seperti Field Instrument, Control loop, PLC, DCS, dll.", Silabus="4.2. Prinsip Kerja Peralatan Pengendalian Proses", Status="Aligned" },
                new CpdpItem { No="4", NamaKompetensi="Process Control & Computer Operations", IndikatorPerilaku="1 (Basic)", DetailIndikator="-", Silabus="4.3. Computer Operations (Sub Kompetensi ini penambahan oleh tim SME GAST)", Status="Aligned" },

                // 5.1 Refinery Process Operations
                new CpdpItem { No="5.1", NamaKompetensi="Refinery Process Operations", IndikatorPerilaku="2 (Intermediate)", DetailIndikator="Memahami prinsip-prinsip dasar dan mampu menjalankan prosedur pengoperasian fasilitas refinery process sesuai standar K3L dengan bimbingan", Silabus="5.5. Prinsip Dasar & Pengoperasian Fasilitas Kilang", Status="Aligned" },
                new CpdpItem { No="5.1", NamaKompetensi="Refinery Process Operations", IndikatorPerilaku="2 (Intermediate)", DetailIndikator="Memahami perkembangan informasi terkait pengoperasian fasilitas refinery process", Silabus="5.9. Equipment Operations", Status="Aligned" },
                new CpdpItem { No="5.1", NamaKompetensi="Refinery Process Operations", IndikatorPerilaku="2 (Intermediate)", DetailIndikator="Mampu melakukan BOC/BEC dengan pengawasan ketat", Silabus="5.1. BOC / BEC", Status="Aligned" },
                new CpdpItem { No="5.1", NamaKompetensi="Refinery Process Operations", IndikatorPerilaku="2 (Intermediate)", DetailIndikator="Mampu melakukan identifikasi bahaya dari permasalahan pada operasi fasilitas refinery process", Silabus="5.6. Identifikasi Bahaya pada Pengoperasian Fasilitas Kilang", Status="Aligned" },
                new CpdpItem { No="5.1", NamaKompetensi="Refinery Process Operations", IndikatorPerilaku="2 (Intermediate)", DetailIndikator="-", Silabus="5.2. Feed & Product Specification", Status="Aligned" },
                new CpdpItem { No="5.1", NamaKompetensi="Refinery Process Operations", IndikatorPerilaku="2 (Intermediate)", DetailIndikator="-", Silabus="5.3. P&ID, Line Up & Lay Out", Status="Aligned" },
                new CpdpItem { No="5.1", NamaKompetensi="Refinery Process Operations", IndikatorPerilaku="2 (Intermediate)", DetailIndikator="-", Silabus="5.4. Start Up, Shutdown & Emergency Unit", Status="Aligned" },

                // 5.2 Oil Processing Operations
                new CpdpItem { No="5.2", NamaKompetensi="Oil Processing Operations", IndikatorPerilaku="1 (Basic)", DetailIndikator="Memahami prinsip-prinsip dasar dan mampu menjalankan prosedur pengoperasian fasilitas oil processing secara aman, andal, dan optimal sesuai standar K3L dengan bimbingan", Silabus="5.7. Prinsip Dasar & Pengoperasian Peralatan", Status="Aligned" },
                new CpdpItem { No="5.2", NamaKompetensi="Oil Processing Operations", IndikatorPerilaku="1 (Basic)", DetailIndikator="Memahami perkembangan informasi terkait pengoperasian fasilitas oil processing", Silabus="5.9. Equipment Operations", Status="Aligned" },
                new CpdpItem { No="5.2", NamaKompetensi="Oil Processing Operations", IndikatorPerilaku="1 (Basic)", DetailIndikator="Mampu melakukan pengoperasian sub proses/area oil processing tertentu", Silabus="5.8. Pengoperasian Sub Proses", Status="Aligned" },
                new CpdpItem { No="5.2", NamaKompetensi="Oil Processing Operations", IndikatorPerilaku="1 (Basic)", DetailIndikator="Mampu melakukan identifikasi bahaya dari permasalahan pada operasi fasilitas oil processing", Silabus="5.6. Identifikasi Bahaya pada Pengoperasian Fasilitas Kilang", Status="Aligned" },

                // 5.3 Process Optimization
                new CpdpItem { No="5.3", NamaKompetensi="Process Optimization", IndikatorPerilaku="1 (Basic)", DetailIndikator="Memahami prinsip dasar parameter proses operasi yang harus dimonitor dan batasan design (limitasi/ operating windows)", Silabus="5.13. Operating Windows", Status="Aligned" },
                new CpdpItem { No="5.3", NamaKompetensi="Process Optimization", IndikatorPerilaku="1 (Basic)", DetailIndikator="Mampu memahami karakteristik unit operasi", Silabus="5.10. Karakteristik Unit Operasi", Status="Aligned" },
                new CpdpItem { No="5.3", NamaKompetensi="Process Optimization", IndikatorPerilaku="1 (Basic)", DetailIndikator="Mampu mengumpulkan data-data yang diperlukan untuk optimasi proses", Silabus="5.12. Data Collecting for Process Optimization", Status="Aligned" },
                new CpdpItem { No="5.3", NamaKompetensi="Process Optimization", IndikatorPerilaku="1 (Basic)", DetailIndikator="Mampu melakukan day to day monitoring berdasarkan hasil pengumpulan data dengan supervisi/ instruksi", Silabus="5.11. Day to Day Monitoring", Status="Aligned" }
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

        // (Biarkan fungsi Records/Capability yang lama tetap ada di bawah sini)
        public IActionResult Records()
        {
             // ... kodingan capability records yg lama ...
             return View(new List<TrainingRecord>()); // Placeholder biar tidak error
        }
    }
}