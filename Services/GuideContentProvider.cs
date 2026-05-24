using HcPortal.Models.Guide;

namespace HcPortal.Services;

public static class GuideContentProvider
{
    public static readonly IReadOnlyList<GuideItem> Items = new List<GuideItem>
    {
        // ═══════════════════ CMP ═══════════════════
        new GuideItem(
            Id: "cmp-library-kkj",
            Module: GuideModule.Cmp,
            Title: "Cara Akses Library KKJ (Kebutuhan Kompetensi Jabatan)",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Akses Dokumen KKJ",
                    "Buka menu <b>CMP</b> di navbar → klik card <b>Dokumen KKJ & Alignment KKJ/IDP</b>. Halaman muncul dengan 2 tab: <b>KKJ</b> (default) + <b>Alignment KKJ/IDP</b>."),
                new GuideStep(2, "Pahami Filter Role-Based",
                    "Sistem otomatis filter daftar bagian sesuai role Anda: <b>L1-L4</b> (Admin/HC/Manager/Atasan) lihat semua bagian; <b>L5-L6</b> (Coach/Coachee) lihat bagian sendiri saja (sesuai field <code>Section</code> di profil Anda)."),
                new GuideStep(3, "Browse Per Bagian",
                    "Daftar bagian terurut berdasarkan <code>DisplayOrder</code>. Klik baris bagian → expand → muncul daftar file KKJ yang tersedia untuk bagian tersebut."),
                new GuideStep(4, "Download File KKJ",
                    "Klik tombol <b>Download</b> di samping file → file ter-download (endpoint <code>/CMP/KkjFileDownload/:id</code>). Nama file menyimpan informasi versi."),
                new GuideStep(5, "Cari Versi Lama (Archived)",
                    "File yang sudah di-archive oleh AdminHC tidak tampil di list. Kalau perlu versi historis tertentu, koordinasi langsung dengan AdminHC.")
            },
            Keywords: new[] { "kkj", "library", "kebutuhan kompetensi jabatan", "download", "dokumen", "bagian", "section" }
        ),
        new GuideItem(
            Id: "cmp-mapping-kkj-cpdp",
            Module: GuideModule.Cmp,
            Title: "Cara Akses Alignment KKJ ↔ IDP (Mapping Pengembangan)",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Akses Tab Alignment",
                    "Dari halaman <b>Dokumen KKJ & Alignment KKJ/IDP</b>, klik tab <b>Alignment KKJ/IDP</b> (di samping tab KKJ). URL bisa langsung: <code>/CMP/DokumenKkj?tab=alignment</code>."),
                new GuideStep(2, "Tujuan Alignment",
                    "Dokumen ini menyelaraskan <b>KKJ</b> (Kebutuhan Kompetensi Jabatan) dengan <b>IDP</b> (Individual Development Plan). Tujuannya: rencana pelatihan setiap pekerja sesuai kompetensi yang dibutuhkan jabatannya."),
                new GuideStep(3, "Browse Per Bagian",
                    "Pattern sama dengan tab KKJ — daftar bagian terurut. Klik bagian → expand → tampil daftar file CPDP (Competency Personal Development Plan) per bagian."),
                new GuideStep(4, "Download File CPDP",
                    "Klik tombol <b>Download</b> per file → endpoint <code>/CMP/CpdpFileDownload/:id</code>."),
                new GuideStep(5, "Refresh Frekuensi",
                    "Koordinasi AdminHC bila ada update KKJ — dokumen CPDP harus mengikuti supaya mapping tetap akurat. Versi lama akan di-archive otomatis.")
            },
            Keywords: new[] { "alignment", "mapping", "cpdp", "kkj", "idp", "individual development plan", "pengembangan" }
        ),
        new GuideItem(
            Id: "cmp-training-records",
            Module: GuideModule.Cmp,
            Title: "Cara Akses Training Records (Capability Building Records)",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Akses Records",
                    "Buka menu <b>CMP</b> → klik card <b>Riwayat Pelatihan</b> → masuk halaman <b>Capability Building Records</b>."),
                new GuideStep(2, "Tab My Records (Default)",
                    "Tab pertama yang aktif. Menampilkan semua record peserta yang sedang login. Badge angka di tab = total record (Assessment Online + Training Manual)."),
                new GuideStep(3, "Pahami 2 Tipe Record",
                    "<b>Assessment Online</b>: record otomatis dari ujian sistem yang Anda kerjakan. <b>Training Manual</b>: record yang di-input oleh HC dari pelatihan eksternal/sertifikasi vendor (tidak melalui ujian online)."),
                new GuideStep(4, "Filter & Search",
                    "Tersedia filter: <b>Section</b>, <b>Unit</b>, <b>Kategori</b>, <b>Status</b>. Kolom search untuk cari berdasarkan judul atau nama penyelenggara."),
                new GuideStep(5, "Export Personal",
                    "Klik tombol <b>Export Excel</b> di sudut kanan → file Excel 2 sheet (Assessment + Training) ter-download untuk dokumentasi personal Anda.")
            },
            Keywords: new[] { "training records", "riwayat pelatihan", "capability building", "assessment online", "training manual", "export excel" }
        ),
        new GuideItem(
            Id: "cmp-monitoring-records-tim",
            Module: GuideModule.Cmp,
            Title: "Cara Monitoring Records Tim (Team View)",
            Roles: new[] { RoleGroup.Atasan, RoleGroup.Manager, RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Akses Team View",
                    "Buka halaman <b>Records</b> (CMP → Riwayat Pelatihan). Klik tab <b>Team View</b> di samping tab My Records. Tab ini hanya muncul untuk role level ≤4 (Atasan, Manager, AdminHC)."),
                new GuideStep(2, "Cakupan Tim",
                    "Tab menampilkan bawahan langsung Anda. Kalau Anda <b>Manager+</b>, sistem juga menampilkan bawahan tidak langsung mengikuti struktur organisasi (rekursif sesuai hierarki bagian)."),
                new GuideStep(3, "Cascade Filter + Date Range",
                    "Filter cascade: pilih <b>Bagian</b> → <b>Unit</b> terkait muncul; pilih <b>Kategori</b> → <b>Sub-Kategori</b> muncul. Plus filter <b>Status</b> dan rentang tanggal <b>Date From</b> / <b>Date To</b>. Partial reload tabel via <code>RecordsTeamPartial</code> tanpa full page refresh."),
                new GuideStep(4, "Detail Worker",
                    "Klik tombol <b>Detail</b> di samping nama pekerja → halaman <code>/CMP/RecordsWorkerDetail?workerId=...</code>. Tampil riwayat lengkap (assessment + training) per individu."),
                new GuideStep(5, "Export Excel Team",
                    "Dua tombol export terpisah: <b>Export Assessment Team</b> (Excel khusus assessment) dan <b>Export Training Team</b> (Excel khusus training manual). Filter yang aktif di view ikut diteruskan ke export.")
            },
            Keywords: new[] { "monitoring", "team view", "records tim", "atasan", "manager", "cascade filter", "export team", "bawahan" }
        ),
        new GuideItem(
            Id: "cmp-pre-post-test",
            Module: GuideModule.Cmp,
            Title: "Cara Mengerjakan Pre-Post Test",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Akses Assessment",
                    "Buka <b>CMP</b> → klik card <b>Assessment Saya</b>. Tampil daftar semua ujian yang ditujukan untuk Anda."),
                new GuideStep(2, "Pahami Pairing Pre-Post",
                    "Sistem otomatis memasangkan <b>Pre-Test</b> + <b>Post-Test</b> berdampingan dalam 1 card visual. Badge <b>Pre-Test</b> (warna info) dan badge <b>Post-Test</b> (warna primary) untuk identifikasi cepat."),
                new GuideStep(3, "Kerjakan Pre-Test",
                    "Klik tombol <b>Mulai Pre-Test</b> (kalau belum pernah mulai) atau <b>Lanjutkan Pre-Test</b> (kalau ada saved progress dari sesi sebelumnya). Wizard ujian akan terbuka dengan timer + soal."),
                new GuideStep(4, "Ikuti Pelatihan",
                    "Setelah Pre-Test selesai, ikuti program pelatihan sesuai jadwal HC. Post-Test belum aktif sampai pelatihan dianggap selesai oleh sistem."),
                new GuideStep(5, "Kerjakan Post-Test",
                    "Setelah pelatihan, tombol <b>Mulai Post-Test</b> muncul di card yang sama. Klik → ujian Post-Test. Sistem otomatis menghitung <b>Gain Score</b> = nilai Post − nilai Pre untuk lihat seberapa besar peningkatan Anda."),
                new GuideStep(6, "Lihat Hasil",
                    "Setelah Post-Test selesai, klik tombol <b>Lihat Hasil</b> → halaman <code>/CMP/Results?id=...</code>. Tampil breakdown nilai per soal + analisa per elemen teknis (skor per kompetensi).")
            },
            Keywords: new[] { "pre-test", "post-test", "pretest", "posttest", "gain score", "pairing", "ujian pasangan", "improvement" }
        ),
        new GuideItem(
            Id: "cmp-monitoring-manager",
            Module: GuideModule.Cmp,
            Title: "Monitoring Compliance via Analytics Dashboard",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Akses & Pahami Summary",
                    "Buka <b>CMP</b> → klik card <b>Dasbor Analitik</b> (hanya visible untuk Admin/HC). Halaman menampilkan 4 KPI cards atas: <b>Total Sessions</b>, <b>Pass Rate</b>, <b>Expiring</b> (sertifikat akan expire), <b>Avg Gain Score</b>."),
                new GuideStep(2, "Tab Fail Rate",
                    "Tab pertama. Visualisasi persentase peserta yang <b>fail</b> per kategori/bagian. Klik bar/segment chart → drill-down ke daftar pekerja yang gagal di kategori tersebut."),
                new GuideStep(3, "Tab Trend",
                    "Chart line menampilkan jumlah <b>Pass</b> / <b>Fail</b> per bulan, ditambah sub-chart <b>rata-rata Gain Score per bulan</b>. Filter periode tersedia — berguna untuk lihat dampak training program pada periode tertentu."),
                new GuideStep(4, "Tab Skor Elemen Teknis",
                    "Breakdown skor rata-rata per <b>elemen kompetensi teknis</b>. Identify area kompetensi yang lemah cross-unit — input untuk perencanaan training kolektif."),
                new GuideStep(5, "Tab Sertifikat Expired",
                    "Daftar pekerja yang sertifikatnya akan expire dalam rentang tertentu. Sort by tanggal expiry — data untuk plan perpanjangan sertifikat (renewal chain di Bagian Assessment Admin)."),
                new GuideStep(6, "Tab Item Analysis + Gain Score Report",
                    "Dua tab terpisah untuk analisa lanjutan Pre-Post pair. <b>Tab Item Analysis</b>: pilih <b>Assessment</b> dari dropdown → per-soal analytics (persentase peserta jawab benar/salah, identify soal terlalu sulit/mudah). <b>Tab Gain Score Report</b>: per-peserta improvement Pre→Post. Masing-masing punya tombol <b>Export Excel</b>.")
            },
            Keywords: new[] { "analytics dashboard", "monitoring", "compliance", "fail rate", "trend", "elemen teknis", "sertifikat expired", "item analysis", "gain score report" }
        ),
        new GuideItem(
            Id: "cmp-budget-training",
            Module: GuideModule.Cmp,
            Title: "Cara Kelola Budget Training & Assessment",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Akses Budget Training",
                    "Buka <b>CMP</b> → klik card <b>Budget Training</b> (hanya visible untuk Admin/HC). Halaman dengan 2 tab: <b>Data Budget</b> (default) + <b>Ringkasan</b>."),
                new GuideStep(2, "Tab Data Budget",
                    "Tampil tabel semua budget item dengan filter: <b>Tahun</b>, <b>Type</b>, <b>Kategori</b>, dan <b>Search</b> by judul. Kolom sortable + pagination."),
                new GuideStep(3, "Tambah Budget Item",
                    "Klik tombol <b>Tambah</b> → form wizard 3 card: <b>Card 1</b> (Tahun Anggaran + Judul), <b>Card 2</b> (Kategori + SubKategori cascade + Jumlah Peserta), <b>Card 3</b> (Anggaran/<code>EstimasiBiayaTotal</code> + Realisasi Biaya + Vendor)."),
                new GuideStep(4, "Quick Update Realisasi",
                    "Inline edit nilai realisasi langsung di tabel tanpa buka form penuh. Endpoint <code>BudgetTrainingQuickUpdate</code> — perubahan tersimpan otomatis saat blur input."),
                new GuideStep(5, "Import Excel",
                    "Klik tombol <b>Import</b> → halaman import. <b>Download Template</b> Excel dulu → isi data → upload via <b>Choose File</b>. Sistem validate per baris dan tampilkan hasil <b>Success / Skip / Error</b> per row dengan pesan detail."),
                new GuideStep(6, "Export Excel & Tab Ringkasan",
                    "Tombol <b>Export Excel</b> di header → download filtered data ke Excel. Tab <b>Ringkasan</b> menampilkan grafik total anggaran vs realisasi per kategori/tahun untuk monitoring progress.")
            },
            Keywords: new[] { "budget training", "anggaran", "realisasi", "import excel", "export", "vendor", "kategori biaya", "estimasi biaya" }
        ),
        new GuideItem(
            Id: "cmp-tipe-assessment",
            Module: GuideModule.Cmp,
            Title: "Tipe-tipe Assessment (Pre-Test, Post-Test, Regular)",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Pre-Test",
                    "<b>Pre-Test</b> adalah ujian awal yang dikerjakan <i>sebelum</i> pelatihan atau coaching dimulai. Tujuannya untuk mengukur tingkat pemahaman awal Anda (baseline knowledge) sebelum mendapat materi. Hasil Pre-Test biasanya tidak menentukan kelulusan — fungsi utamanya membandingkan progres."),
                new GuideStep(2, "Post-Test",
                    "<b>Post-Test</b> dikerjakan <i>setelah</i> pelatihan selesai. Sistem otomatis menghitung selisih nilai vs Pre-Test (<b>Gain Score</b>) untuk lihat seberapa besar peningkatan pemahaman Anda. Paket soal Post-Test bisa <i>sama</i> dengan Pre-Test (sengaja untuk perbandingan langsung) atau berbeda — tergantung setting HC."),
                new GuideStep(3, "Regular Assessment",
                    "<b>Regular Assessment</b> adalah ujian mandiri yang tidak dipasangkan dengan ujian lain. Cakupannya: sertifikasi kompetensi, On-the-Job assessment, Mandatory HSSE Training, dan lain-lain. Tidak ada hitungan Gain Score karena tidak ada pasangan Pre/Post.")
            },
            Keywords: new[] { "tipe", "pre-test", "post-test", "regular", "pretest", "posttest", "jenis assessment", "gain score" }
        ),
        new GuideItem(
            Id: "cmp-tipe-package-question",
            Module: GuideModule.Cmp,
            Title: "Tipe Soal: Multiple Choice, Multiple Answer, Essay",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Multiple Choice (MC)",
                    "Soal dengan <b>satu</b> jawaban benar dari beberapa opsi (radio button). Default semua soal lama. Auto-grading instant saat peserta submit."),
                new GuideStep(2, "Multiple Answer (MA)",
                    "Soal dengan <b>lebih dari satu</b> jawaban benar (checkbox). Peserta harus pilih <i>semua</i> opsi benar untuk dianggap betul — kurang satu = salah. Auto-grading instant. (Feature Phase 296)"),
                new GuideStep(3, "Essay",
                    "Soal jawaban teks bebas. Field tambahan saat input soal: <b>Rubrik</b> (kunci jawaban referensi internal HC — tidak ditampilkan ke peserta) dan <b>MaxCharacters</b> (default 2000). <b>Grading manual</b> oleh HC — hasil tidak instan, peserta harus tunggu HC nilai."),
                new GuideStep(4, "Kapan pakai yang mana",
                    "<b>MC</b> untuk pengetahuan faktual (definisi, prosedur). <b>MA</b> untuk konsep multi-aspek (mis: \"Sebutkan APD yang wajib dipakai\"). <b>Essay</b> untuk analisis, penjelasan, atau studi kasus.")
            },
            Keywords: new[] { "tipe soal", "multiple choice", "multiple answer", "essay", "rubrik", "package question", "mc", "ma" }
        ),
        new GuideItem(
            Id: "cmp-cara-buat-assessment",
            Module: GuideModule.Cmp,
            Title: "Cara Buat Assessment (Admin)",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Akses Admin Assessment",
                    "Login sebagai Admin/HC. Buka menu <b>Kelola Data → Manage Assessment & Training</b>. Klik tombol <b>Buat Assessment</b> di kanan atas."),
                new GuideStep(2, "Pilih Kategori",
                    "Pilih kategori dari dropdown (OJT, IHT, Mandatory HSSE, dll). Kalau kategori punya sub-kategori, dropdown sub-kategori muncul otomatis. Kalau kategori belum ada, buat dulu via <b>Manage Categories</b> (lihat accordion \"Cara Manage Kategori Assessment\")."),
                new GuideStep(3, "Isi Detail Sesi",
                    "Isi: judul, deskripsi, tanggal mulai/akhir, durasi (menit), tipe assessment (Pre-Test / Post-Test / Regular), pass percentage, token akses (opsional). Set <b>Extra Time</b> kalau ada peserta perlu tambahan waktu (aksesibilitas)."),
                new GuideStep(4, "Link Pre-Post (Opsional)",
                    "Kalau buat Post-Test, ada field <b>Linked Pre-Test Session</b> untuk pilih sesi Pre yang dipasangkan. Centang <b>SamePackage</b> kalau ingin paket soal Post-Test <i>identik</i> dengan Pre — biar perbandingan apple-to-apple. Kosongkan kalau paket berbeda."),
                new GuideStep(5, "Tambah Peserta",
                    "Pilih peserta dari daftar pekerja (filter unit, pencarian nama/email, Select All/Deselect All). Token akses <i>shared</i> per batch — semua peserta sesi pakai token yang sama."),
                new GuideStep(6, "Publish & Monitor",
                    "Klik <b>Buat Assessment</b>. Setelah publish, peserta bisa akses ujian. Monitor real-time via <b>Admin Monitoring</b> (SignalR live update — lihat siapa yang sudah masuk, soal ke berapa, sisa waktu).")
            },
            Keywords: new[] { "buat assessment", "create", "wizard", "admin", "settings", "extra time", "linked session", "samepackage" }
        ),
        new GuideItem(
            Id: "cmp-cara-upload-package",
            Module: GuideModule.Cmp,
            Title: "Cara Upload Package Question (Paket Soal)",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Buka Manage Package",
                    "Dari sesi assessment, klik menu <b>Manage Questions</b>. Bisa buat <b>multi-package</b> per sesi (Paket A, Paket B, Paket C — <code>PackageNumber</code>). Berguna untuk anti-contek: peserta dapat paket berbeda."),
                new GuideStep(2, "Pilih Metode Input",
                    "Tiga cara input soal: <b>1.</b> Upload file Excel (download template dulu), <b>2.</b> Paste dari clipboard Excel, <b>3.</b> Manual entry per-soal di form."),
                new GuideStep(3, "Format Excel",
                    "Kolom wajib: <b>Question</b>, <b>OptionA</b>, <b>OptionB</b>, <b>OptionC</b>, <b>OptionD</b> (E opsional), <b>CorrectAnswer</b> (huruf opsi benar atau pisah koma kalau MA), <b>QuestionType</b> (MC/MA/Essay). Untuk Essay: tambah kolom <b>Rubrik</b> dan <b>MaxCharacters</b>."),
                new GuideStep(4, "Preview & Verifikasi",
                    "Setelah import, cek <b>semua</b> soal di tab Preview: pertanyaan, opsi, kunci jawaban. Edit per-soal kalau ada salah ketik atau kunci jawaban keliru. Wajib verifikasi sebelum publish — soal yang sudah dikerjakan peserta sulit di-edit."),
                new GuideStep(5, "Shuffle & Publish",
                    "Aktifkan <b>Shuffle Per-User</b> kalau ingin tiap peserta lihat urutan opsi A/B/C/D berbeda (anti-contek). Display saja — grading tetap pakai <code>PackageOption.Id</code> di backend. Bisa <b>reshuffle</b> per-sesi atau bulk kalau perlu ulang acak.")
            },
            Keywords: new[] { "upload", "package", "paket soal", "excel", "import", "shuffle", "preview", "multi-package" }
        ),
        new GuideItem(
            Id: "cmp-cara-manage-kategori",
            Module: GuideModule.Cmp,
            Title: "Cara Manage Kategori Assessment (Admin)",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Buka Manage Categories",
                    "Login sebagai Admin/HC. Buka menu <b>Admin → Manage Kategori Assessment</b>. Halaman ini punya CRUD penuh untuk kategori assessment (tambah, edit, hapus)."),
                new GuideStep(2, "Buat Parent Category",
                    "Klik <b>Tambah Kategori</b>. Isi nama (contoh: \"Assessment OJ\", \"Mandatory HSSE Training\"). Tambah <b>Signatory User</b> (pekerja yang akan TTD sertifikat untuk kategori ini — biasanya manager unit terkait)."),
                new GuideStep(3, "Tambah Sub-Kategori",
                    "Untuk hierarki, buat kategori baru dan pilih <b>Parent Category</b> dari dropdown. Sub-kategori bisa <i>inherit signatory parent</i> (kosongkan signatory) atau set signatory sendiri (override). Mendukung 2 level (parent + child)."),
                new GuideStep(4, "Edit / Hapus",
                    "Edit kapan saja (nama, signatory). <b>Hapus</b> hanya bisa kalau tidak ada sesi aktif yang masih pakai kategori ini — kalau ada, sistem block dan tampilkan daftar sesi yang menghalangi.")
            },
            Keywords: new[] { "kategori", "manage", "categories", "signatory", "parent", "sub-kategori", "ttd", "sertifikat" }
        ),
        new GuideItem(
            Id: "cmp-fitur-khusus-admin",
            Module: GuideModule.Cmp,
            Title: "Fitur Khusus Admin (Manual Entry, Extra Time, Reshuffle, Edit Jawaban, Renewal)",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Manual Entry Sertifikat",
                    "Untuk peserta yang sudah punya sertifikat dari lembaga luar (tidak ikut ujian online), gunakan <b>Manual Entry</b>. Aktifkan toggle saat buat sesi → muncul field: <b>Penyelenggara</b>, <b>Kota</b>, <b>SubKategori</b>, <b>CertificateType</b> (Kompetensi/Profesi/Pelatihan). Sistem set <code>IsManualEntry=true</code> — sertifikat tetap masuk records peserta."),
                new GuideStep(2, "Set Extra Time",
                    "Per-sesi atau per-peserta, set <b>ExtraTimeMinutes</b> (tambahan waktu menit) untuk aksesibilitas. Saat peserta mulai ujian, timer otomatis dapat tambahan — peserta tidak perlu lapor. (Feature Phase 302)"),
                new GuideStep(3, "Reshuffle Package",
                    "Kalau perlu ulang acak urutan opsi A/B/C/D (mis: ada bocor jawaban), klik <b>Reshuffle</b> per-sesi atau bulk (semua sesi sekaligus). Display random, grading tetap konsisten via <code>PackageOption.Id</code>."),
                new GuideStep(4, "Akhiri Ujian",
                    "<b>AkhiriUjian</b> (1 peserta) atau <b>AkhiriSemuaUjian</b> (bulk semua peserta sesi) — paksa submit semua jawaban yang sudah ada. Berguna kalau waktu habis tapi peserta lupa submit, atau ada keadaan darurat."),
                new GuideStep(5, "Edit Jawaban Peserta",
                    "Setelah ujian selesai, Admin bisa <b>override jawaban</b> peserta lewat halaman Detail Sesi (mis: koreksi typo sistem, atau adjudikasi soal yang ambigu). Setiap edit di-log otomatis ke <b>AssessmentEditLog</b> dengan timestamp + user yang edit — audit trail lengkap."),
                new GuideStep(6, "Renewal Chain",
                    "Untuk sertifikat yang perlu diperbarui (mis: lisensi tahunan), buat sesi baru lalu link ke sertifikat lama via <b>RenewsSessionId</b> atau <b>RenewsTrainingId</b>. Sistem chain sertifikat lama → baru untuk tracking history perpanjangan. (Feature Phase 200)")
            },
            Keywords: new[] { "manual entry", "extra time", "reshuffle", "akhiri ujian", "edit jawaban", "renewal", "fitur khusus", "audit log", "aksesibilitas" }
        ),

        // ═══════════════════ CDP ═══════════════════
        new GuideItem(
            Id: "cdp-plan-idp-silabus",
            Module: GuideModule.Cdp,
            Title: "Cara Melihat Plan IDP / Silabus",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Masuk ke CDP", "Pilih menu <b>CDP</b> di navbar."),
                new GuideStep(2, "Buka Plan IDP", "Pada bagian sidebar menu CDP, klik <b>Plan IDP / Silabus</b>. Sistem akan menampilkan daftar silabus sesuai jabatan dan level Anda.")
            },
            Keywords: new[] { "idp", "silabus", "plan", "individual development plan" }
        ),
        new GuideItem(
            Id: "cdp-approve-deliverable",
            Module: GuideModule.Cdp,
            Title: "Cara Approve / Reject Deliverable (Coaching Guidance)",
            Roles: new[] { RoleGroup.Atasan, RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Cek Request Masuk", "Buka CDP dan cek notifikasi approval yang muncul saat coachee submit evidence."),
                new GuideStep(2, "Review Bukti", "Download evidence yang disertakan coachee tersebut. Review kelengkapannya."),
                new GuideStep(3, "Aksi Approve/Reject", "Klik tombol <b>Approve</b> hijau jika sesuai, atau <b>Reject</b> (beri catatan perbaikan) jika evidence belum memenuhi standar.")
            },
            Keywords: new[] { "approve", "reject", "deliverable", "coaching guidance" }
        ),
        new GuideItem(
            Id: "cdp-coaching-proton-dashboard",
            Module: GuideModule.Cdp,
            Title: "Cara Melihat Coaching Proton Dashboard",
            Roles: new[] { RoleGroup.Manager, RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Akses Dashboard", "Di halaman CDP, klik kartu <b>Coaching Proton Dashboard</b>."),
                new GuideStep(2, "Monitoring Keseluruhan", "Lihat chart progress dan statistik coaching secara global (untuk HC) atau level Unit Kerja."),
                new GuideStep(3, "Filter Data", "Gunakan filter Section, Unit, Kategori, dan Track untuk mempersempit data yang ditampilkan.")
            },
            Keywords: new[] { "dashboard", "coaching", "proton", "monitoring" }
        ),
        new GuideItem(
            Id: "cdp-daftar-deliverable",
            Module: GuideModule.Cdp,
            Title: "Cara Melihat Daftar Deliverable & Status Progress",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Buka Halaman Deliverable", "Masuk ke menu <b>CDP</b>, klik <b>Deliverable</b> di panel navigasi kiri."),
                new GuideStep(2, "Pantau Status Progress", "Lihat daftar deliverable yang ditugaskan. Status: <i>Not Started, In Progress, Pending Approval, Approved, Rejected</i>. Filter berdasarkan status atau periode."),
                new GuideStep(3, "Detail Deliverable", "Klik judul deliverable untuk melihat deskripsi lengkap, target tanggal selesai, dan riwayat coaching session.")
            },
            Keywords: new[] { "deliverable", "status", "progress", "daftar" }
        ),
        new GuideItem(
            Id: "cdp-historik-proton-export",
            Module: GuideModule.Cdp,
            Title: "Cara Melihat Historik Proton & Export Laporan",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Lihat Historik Proton", "Di halaman CDP, klik <b>Historik Proton</b> untuk melihat riwayat coaching per periode. Data ditampilkan berdasarkan periode Proton yang telah berjalan."),
                new GuideStep(2, "Export Laporan", "Gunakan tombol <b>Export Excel</b> atau <b>Export PDF</b> untuk mengunduh laporan progress coaching dan deliverable."),
                new GuideStep(3, "Laporan Admin/HC", "Untuk Admin dan HC: akses <b>Bottleneck Report</b> untuk melihat deliverable yang terhambat dan <b>Workload Summary</b> untuk memantau beban kerja coach.")
            },
            Keywords: new[] { "historik", "proton", "export", "laporan", "bottleneck", "workload" }
        ),
        new GuideItem(
            Id: "cdp-upload-evidence",
            Module: GuideModule.Cdp,
            Title: "Cara Upload Evidence Deliverable",
            Roles: new[] { RoleGroup.Coachee },
            Steps: new[]
            {
                new GuideStep(1, "Buka Deliverable Saya", "Di menu CDP, klik <b>Deliverable</b>. Pilih deliverable yang sedang dikerjakan."),
                new GuideStep(2, "Siapkan File Evidence", "Format yang didukung: PDF, DOCX, XLSX, JPG, PNG. Maksimal ukuran file sesuai konfigurasi (umumnya 10 MB)."),
                new GuideStep(3, "Klik Upload Evidence", "Klik tombol <b>Upload Evidence</b>, pilih file, tambah catatan singkat (opsional)."),
                new GuideStep(4, "Submit untuk Review", "Klik <b>Submit</b>. Status berubah jadi <i>Pending Approval</i>. Notifikasi akan dikirim ke reviewer chain (Sr Supervisor → Section Head → HC).")
            },
            Keywords: new[] { "upload", "evidence", "deliverable", "coachee", "submit" }
        ),
        new GuideItem(
            Id: "cdp-coaching-session",
            Module: GuideModule.Cdp,
            Title: "Cara Catat Coaching Session",
            Roles: new[] { RoleGroup.Coach, RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Buka Daftar Coachee", "Di menu CDP, buka <b>My Coachee</b>. Pilih coachee yang sudah sesi coaching."),
                new GuideStep(2, "Tambah Coaching Session", "Klik <b>+ Tambah Session</b>. Isi tanggal, durasi, dan topik yang dibahas."),
                new GuideStep(3, "Catat Outcome", "Isi outcome diskusi: insight yang dipelajari, action item, dan link ke deliverable yang dibahas (kalau ada)."),
                new GuideStep(4, "Save", "Klik <b>Save</b>. Histori session ini akan muncul di Histori Proton coachee.")
            },
            Keywords: new[] { "coaching", "session", "catat", "log", "coach" }
        ),
        new GuideItem(
            Id: "cdp-reviewer-chain",
            Module: GuideModule.Cdp,
            Title: "Memahami Alur Reviewer (Sr Supervisor → Section Head → HC)",
            Roles: new[] { RoleGroup.Atasan, RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Notifikasi Evidence Masuk", "Saat coachee submit evidence, notifikasi muncul di bell icon. Pertama-tama review oleh <b>Sr Supervisor</b>."),
                new GuideStep(2, "Sr Supervisor Review", "Sr Supervisor buka deliverable → cek evidence → klik <b>Approve</b> atau <b>Reject</b> dengan catatan."),
                new GuideStep(3, "Section Head Review", "Setelah Sr Supervisor approve, naik ke <b>Section Head</b>. Section Head review final dari sisi unit."),
                new GuideStep(4, "HC Review", "Terakhir review oleh <b>HC</b>. HC verifikasi kelengkapan dan compliance program."),
                new GuideStep(5, "Reject Reset Chain", "Kalau salah satu reviewer reject, seluruh chain di-reset. Coachee harus upload evidence baru, mulai dari Sr Supervisor lagi.")
            },
            Keywords: new[] { "reviewer", "chain", "approval", "sr supervisor", "section head", "hc", "alur" }
        ),
        new GuideItem(
            Id: "cdp-final-assessment",
            Module: GuideModule.Cdp,
            Title: "Cara Submit Final Assessment Proton",
            Roles: new[] { RoleGroup.Coachee, RoleGroup.Coach },
            Steps: new[]
            {
                new GuideStep(1, "Pastikan Semua Deliverable Approved", "Final Assessment hanya bisa di-submit kalau semua deliverable di periode Proton sudah <i>Approved</i>."),
                new GuideStep(2, "Buka Final Assessment", "Di menu CDP, klik <b>Final Assessment</b>. Daftar kompetensi akan muncul beserta target level."),
                new GuideStep(3, "Isi Self-Assessment (Coachee)", "Coachee isi nilai self-assessment per kompetensi. Tambah refleksi pembelajaran selama periode."),
                new GuideStep(4, "Validasi Coach", "Coach validasi nilai coachee, koreksi kalau perlu, tambah komentar."),
                new GuideStep(5, "Submit Final", "Coach klik <b>Submit Final</b>. Hasil masuk ke Histori Proton dan tidak bisa diedit lagi.")
            },
            Keywords: new[] { "final", "assessment", "proton", "submit", "coachee", "coach" }
        ),
        new GuideItem(
            Id: "cdp-bottleneck-report",
            Module: GuideModule.Cdp,
            Title: "Cara Lihat Bottleneck Report",
            Roles: new[] { RoleGroup.Manager, RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Buka Coaching Proton Dashboard", "Di menu CDP, klik <b>Coaching Proton Dashboard</b>."),
                new GuideStep(2, "Pilih Tab Bottleneck", "Klik tab <b>Bottleneck Report</b>. Tabel menampilkan deliverable yang stuck (lama tidak progress) per coachee/unit."),
                new GuideStep(3, "Filter & Drill Down", "Filter berdasarkan section, status (Pending Approval > X hari, Rejected, dll), atau coach. Klik baris untuk lihat detail histori deliverable."),
                new GuideStep(4, "Eskalasi", "Identifikasi akar bottleneck (reviewer lambat, coachee pasif, scope unclear) → eskalasi ke pihak terkait via internal comms.")
            },
            Keywords: new[] { "bottleneck", "report", "stuck", "delay", "deliverable", "manager" }
        ),

        // ═══════════════════ Account ═══════════════════
        new GuideItem(
            Id: "account-login",
            Module: GuideModule.Account,
            Title: "Cara Login ke HC Portal",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Buka Portal", "Akses URL HC Portal melalui browser."),
                new GuideStep(2, "Masukkan Kredensial", "Ketik <b>email</b> dan <b>password</b> yang sudah diaktifkan. Klik Log In.")
            },
            Keywords: new[] { "login", "masuk", "kredensial", "akun" }
        ),
        new GuideItem(
            Id: "account-view-profile",
            Module: GuideModule.Account,
            Title: "Cara Melihat Profil",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Buka Profil", "Klik avatar profil Anda di pojok kanan atas layar, lalu pilih <b>My Profile</b>."),
                new GuideStep(2, "Review Data Personal", "Halaman profil menampilkan informasi: nama, NIP, email, jabatan, unit kerja, dan role. Pastikan semua data sudah benar.")
            },
            Keywords: new[] { "profil", "lihat", "my profile", "view" }
        ),
        new GuideItem(
            Id: "account-edit-profile",
            Module: GuideModule.Account,
            Title: "Cara Edit Profil",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Masuk Halaman Profil", "Klik avatar profil → pilih <b>My Profile</b>."),
                new GuideStep(2, "Klik Tombol Edit", "Klik tombol pensil/Edit di halaman profil."),
                new GuideStep(3, "Simpan Perubahan", "Edit informasi yang diperlukan, lalu tekan <b>Simpan</b>. Beberapa field (mis: NIP, role) tidak bisa diedit user — hubungi Admin/HC kalau perlu update.")
            },
            Keywords: new[] { "edit", "ubah", "profil", "nama" }
        ),
        new GuideItem(
            Id: "account-change-password",
            Module: GuideModule.Account,
            Title: "Cara Mengganti Password",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Settings", "Klik avatar profil Anda, pilih <b>Settings</b>."),
                new GuideStep(2, "Change Password", "Pilih tab Change Password. Masukkan password lama Anda dan ketikkan password baru. Konfirmasi dan Simpan.")
            },
            Keywords: new[] { "password", "ganti", "ubah", "change", "settings" }
        ),
        new GuideItem(
            Id: "account-logout",
            Module: GuideModule.Account,
            Title: "Cara Logout",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Buka Menu Profil", "Klik avatar profil Anda di pojok kanan atas navbar."),
                new GuideStep(2, "Pilih Logout", "Pilih opsi <b>Logout</b> untuk keluar dari sistem dengan aman. Anda akan diarahkan kembali ke halaman login.")
            },
            Keywords: new[] { "logout", "keluar", "sign out" }
        ),
        new GuideItem(
            Id: "account-role-system",
            Module: GuideModule.Account,
            Title: "Memahami Role System",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Lihat Role di Navbar", "Role Anda ditampilkan di navbar kanan atas sebagai badge (mis: <b>HC</b>, <b>Coachee</b>, <b>Coach</b>)."),
                new GuideStep(2, "Pahami 6 Level Akses", "Sistem punya 6 level: <b>1</b> Admin, <b>2</b> HC, <b>3</b> Direktur/VP/Manager, <b>4</b> Section Head/Sr Supervisor, <b>5</b> Coach/Supervisor, <b>6</b> Coachee. Setiap level punya akses menu berbeda."),
                new GuideStep(3, "Minta Update Role", "Kalau role tidak sesuai jabatan Anda, hubungi <b>Admin atau HC</b> untuk update via Admin Panel → Kelola Pekerja.")
            },
            Keywords: new[] { "role", "level", "akses", "section head", "supervisor", "manager", "vp", "direktur" }
        ),

        // ═══════════════════ Data (AdminHC) ═══════════════════
        new GuideItem(
            Id: "data-silabus",
            Module: GuideModule.Data,
            Title: "Cara Kelola Data Silabus",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Masuk Kelola Data", "Klik navbar <b>Kelola Data</b>. Bagian default adalah konfigurasi Silabus."),
                new GuideStep(2, "Tambah/Edit Silabus", "Gunakan panel inline editing untuk menambah kompetensi silabus baru, update judulnya, atau hapus dan nonaktifkan (Deactivate) jika silabus tsb sudah obsolete.")
            },
            Keywords: new[] { "silabus", "kelola data", "inline editing", "deactivate" }
        ),
        new GuideItem(
            Id: "data-guidance-files",
            Module: GuideModule.Data,
            Title: "Cara Upload dan Kelola Guidance File",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Buka Guidance List", "Di menu Kelola Data, pilih <b>Guidance Files</b>."),
                new GuideStep(2, "Upload File Petunjuk", "Klik tombol <b>Upload Guidance</b>, pilih target Unit/Bagian, lalu lampirkan file yang akan menjadi rujukan/guidance resmi bagi coachee.")
            },
            Keywords: new[] { "guidance", "upload", "petunjuk", "file" }
        ),
        new GuideItem(
            Id: "data-override-proton",
            Module: GuideModule.Data,
            Title: "Cara Override Data Pekerja (Proton Data Sync)",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Buka Override Data", "Di menu Kelola Data, pilih tab <b>Override</b>. Ini digunakan mengatur data mentah Proton yang gagal tersinkron."),
                new GuideStep(2, "Save Override", "Isi paksa value mapping posisi yang valid (misal mapping KKJ jika Proton tidak mendeteksi ID Jabatan otomatis), lalu klik Override / Save. Data yang masuk akan digenerate otomatis.")
            },
            Keywords: new[] { "override", "proton", "sync", "data pekerja" }
        ),

        // ═══════════════════ Admin Panel (AdminHC) ═══════════════════
        new GuideItem(
            Id: "admin-kelola-pekerja",
            Module: GuideModule.Admin,
            Title: "Cara Kelola Pekerja (Tambah, Edit, Import Excel & Export)",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Buka Kelola Pekerja", "Pilih <b>Admin Panel → Kelola Pekerja (Manage Workers)</b>."),
                new GuideStep(2, "CRUD Pekerja", "Anda bisa Tambah baru, edit jabatan pekerja yang ada, atur active/deactive status, atau hapus pekerja."),
                new GuideStep(3, "Mass Import / Export", "Gunakan tombol <b>Import Workers</b> beserta template Excel yang tersedia untuk input data besar. Untuk backup, klik <b>Export Excel</b>.")
            },
            Keywords: new[] { "pekerja", "tambah", "edit", "import", "export", "excel" }
        ),
        new GuideItem(
            Id: "admin-upload-kkj-cpdp",
            Module: GuideModule.Admin,
            Title: "Cara Upload File KKJ Matrix & CPDP Files",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Upload File KKJ / CPDP", "Pilih <b>Admin Panel → KKJ Matrix</b> atau <b>Admin Panel → CPDP Files</b>."),
                new GuideStep(2, "Histori Matrix", "Pilih Bagian divisi tujuan, lalu Upload dokumen PDF terkait KKJ / mapping matriksnya agar tampil di module CMP bagi seluruh pekerja. Catatan historis file sebelumnya bisa dilihat di submenu History.")
            },
            Keywords: new[] { "upload", "kkj", "cpdp", "matrix", "histori" }
        ),
        new GuideItem(
            Id: "admin-mapping-coach-coachee",
            Module: GuideModule.Admin,
            Title: "Cara Mapping Coach-Coachee",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Buka Setting Mapping", "Pilih <b>Admin Panel → Coach Coachee Mapping</b>. Di sini tersimpan seluruh relasi siapa membimbing siapa."),
                new GuideStep(2, "Assign / Cabut Akses", "Anda dapat assign Coach baru (Assign Mapping), mengubah coach untuk periode tertentu (Edit), atau mencabut hak apabila transfer staff / resign (Deactivate).")
            },
            Keywords: new[] { "mapping", "coach", "coachee", "assign", "deactivate" }
        ),
        new GuideItem(
            Id: "admin-bank-soal",
            Module: GuideModule.Admin,
            Title: "Cara Kelola Bank Soal (Paket Ujian)",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Buka Kelola Paket Soal", "Di <b>Admin Panel → Kelola Paket Soal</b>, klik tombol <b>Buat Paket Soal Baru</b> untuk membuat kategori paket ujian."),
                new GuideStep(2, "Import Soal dari Template", "Download template Excel yang tersedia, isi soal dan kunci jawaban, lalu upload kembali untuk mass import soal."),
                new GuideStep(3, "Define Kunci Jawaban", "Tentukan jawaban benar untuk setiap soal. Anda juga dapat memberikan pembahasan atau feedback untuk setiap opsi jawaban."),
                new GuideStep(4, "Kategorikan Paket Soal", "Tag paket soal dengan kategori kompetensi, level jabatan, atau unit kerja untuk memudahkan filtering saat membuat jadwal assessment.")
            },
            Keywords: new[] { "bank soal", "paket", "ujian", "import", "template" }
        ),
        new GuideItem(
            Id: "admin-create-assessment",
            Module: GuideModule.Admin,
            Title: "Cara Membuat Jadwal Assessment",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Buka Kelola Assessment", "Pilih <b>Admin Panel → Kelola Assessment</b>, lalu klik tombol <b>Create New Assessment</b>."),
                new GuideStep(2, "Set Durasi dan Tanggal", "Masukkan nama ujian, durasi (menit), batas tanggal mulai dan selesai, serta passing grade minimum."),
                new GuideStep(3, "Pilih Paket Soal", "Pilih satu atau beberapa paket soal yang sudah dibuat sebelumnya dari Bank Soal."),
                new GuideStep(4, "Assign Peserta", "Pilih peserta individu atau assign berdasarkan group/unit kerja, lalu klik <b>Save</b> untuk mempublish jadwal assessment.")
            },
            Keywords: new[] { "create", "assessment", "jadwal", "durasi", "passing grade" }
        ),
        new GuideItem(
            Id: "admin-assessment-monitoring",
            Module: GuideModule.Admin,
            Title: "Monitoring Ujian Berjalan",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Buka Assessment Monitoring", "Pilih <b>Admin Panel → Assessment Monitoring</b> untuk melihat semua ujian yang sedang berlangsung."),
                new GuideStep(2, "Real-time Monitoring", "Pantau progres user secara realtime: status login, waktu tersisa, jumlah soal terjawab, dan persentase penyelesaian."),
                new GuideStep(3, "Force-Close User", "Jika ada kendala teknis (browser freeze, dll), Anda dapat melakukan <i>Force-Close</i> untuk mengakhiri sesi user. Sistem akan otomatis submit skor sesuai progres yang sudah dicapai."),
                new GuideStep(4, "View Progress User", "Klik nama user untuk melihat detail progress: jawaban per soal, waktu pengerjaan, dan status penyelesaian.")
            },
            Keywords: new[] { "monitoring", "ujian", "realtime", "force close", "progress" }
        ),
        new GuideItem(
            Id: "admin-add-training",
            Module: GuideModule.Admin,
            Title: "Cara Menambahkan Training Record",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Buka Add Training", "Pilih <b>Add Training</b> dari panel utama Admin Panel untuk membuka formulir input training."),
                new GuideStep(2, "Input Data Pelatihan", "Isi data karyawan yang mengikuti pelatihan, nama training, penyelenggara, tanggal pelaksanaan, dan durasi (jam pelatihan)."),
                new GuideStep(3, "Set Kategori", "Tentukan kategori training: <b>Mandatory</b> (wajib, terkait kompetensi jabatan) atau <b>Opsional</b> (pengembangan tambahan)."),
                new GuideStep(4, "Upload Sertifikat", "Jika ada, lampirkan file sertifikat sebagai bukti kelulusan training. Data akan muncul di tab Training Records di CMP.")
            },
            Keywords: new[] { "training", "record", "add", "mandatory", "sertifikat" }
        ),
        new GuideItem(
            Id: "admin-audit-log",
            Module: GuideModule.Admin,
            Title: "Cara Melihat Audit Log",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Buka Audit Log", "Pilih <b>Admin Panel → Audit Log</b> untuk mengakses catatan aktivitas sistem."),
                new GuideStep(2, "Filter Data", "Filter berdasarkan user, jenis aksi, atau rentang waktu untuk menemukan aktivitas spesifik."),
                new GuideStep(3, "Lihat Detail Aktivitas", "Review IP address, waktu akses, dan aktivitas sensitif seperti Force-Close, Override, Delete data, atau perubahan role."),
                new GuideStep(4, "Investigasi", "Gunakan Audit Log untuk melacak siapa yang melakukan perubahan, mengidentifikasi aktivitas mencurigakan, atau menginvestigasi kendala sistem.")
            },
            Keywords: new[] { "audit", "log", "aktivitas", "investigasi", "filter" }
        ),
        new GuideItem(
            Id: "admin-kelola-units",
            Module: GuideModule.Admin,
            Title: "Cara Kelola Units (Bagian/Divisi)",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Buka Kelola Units", "Pilih <b>Admin Panel → Kelola Units</b> untuk mengelola struktur organisasi (Bagian/Divisi)."),
                new GuideStep(2, "Tambah Unit Baru", "Klik <b>Tambah Unit</b>, masukkan nama unit, kode unit, dan deskripsi. Tentukan unit parent jika ada hierarki."),
                new GuideStep(3, "Edit atau Hapus Unit", "Edit informasi unit yang sudah ada atau nonaktifkan unit yang tidak digunakan. Pastikan tidak ada pekerja terassign sebelum menghapus."),
                new GuideStep(4, "Assign Pekerja ke Unit", "Di halaman <b>Kelola Pekerja</b>, edit data pekerja dan set unit assignment sesuai struktur organisasi.")
            },
            Keywords: new[] { "units", "bagian", "divisi", "struktur", "organisasi" }
        ),
        new GuideItem(
            Id: "admin-notifikasi-system",
            Module: GuideModule.Admin,
            Title: "Memahami Sistem Notifikasi",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Notifikasi Otomatis", "Sistem mengirim notifikasi secara otomatis untuk event penting: <b>Assessment Assigned</b> (saat Anda ditugaskan assessment baru), <b>Evidence Submitted</b> (saat coachee submit evidence), <b>Evidence Approved/Rejected</b> (saat reviewer memproses evidence)."),
                new GuideStep(2, "Coaching Notifications", "Notifikasi juga dikirim untuk event coaching: <b>Coaching Session Completed</b>, <b>Deliverable Status Changed</b>, dan <b>Approval Chain Updated</b>."),
                new GuideStep(3, "Cek Notifikasi", "Semua notifikasi muncul di <b>ikon lonceng (bell icon)</b> di navbar kanan atas. Klik untuk melihat daftar notifikasi terbaru dan klik item untuk navigasi langsung ke halaman terkait.")
            },
            Keywords: new[] { "notifikasi", "bell", "alert", "system", "event" }
        ),
        new GuideItem(
            Id: "admin-maintenance-impersonate",
            Module: GuideModule.Admin,
            Title: "Maintenance Mode & Impersonate User",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Aktifkan Maintenance Mode", "Buka <b>Admin Panel → Maintenance Mode</b> untuk mengaktifkan mode pemeliharaan. Pilih scope: <b>All</b> (seluruh sistem) atau modul tertentu (CMP, CDP, dll)."),
                new GuideStep(2, "Impersonate User", "Buka <b>Admin Panel → Impersonate</b> untuk login sebagai user lain. Fitur ini berguna untuk debugging dan testing — Anda dapat melihat tampilan sistem dari sudut pandang user tertentu tanpa mengetahui password mereka."),
                new GuideStep(3, "Kembali ke Akun Asli", "Setelah selesai impersonate, klik banner kuning di bagian atas halaman atau klik <b>Stop Impersonating</b> untuk kembali ke akun Admin Anda.")
            },
            Keywords: new[] { "maintenance", "impersonate", "debug", "testing" }
        ),
        new GuideItem(
            Id: "admin-renewal-management",
            Module: GuideModule.Admin,
            Title: "Cara Kelola Renewal Sertifikat",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Buka Renewal Management", "Pilih <b>Admin Panel → Renewal Management</b>. Halaman menampilkan daftar sertifikat assessment yang mendekati masa kadaluarsa."),
                new GuideStep(2, "Filter Sertifikat", "Filter berdasarkan rentang tanggal expired (mis: 30 / 60 / 90 hari ke depan), kategori assessment, atau unit."),
                new GuideStep(3, "Buat Jadwal Renewal", "Pilih sertifikat → klik <b>Schedule Renewal</b>. Sistem otomatis create assessment baru dengan paket soal sama, assign ke pekerja terkait."),
                new GuideStep(4, "Notifikasi Pekerja", "Pekerja akan dapat notifikasi assessment renewal. Setelah lulus, sertifikat baru terbit dan masa berlaku otomatis di-update.")
            },
            Keywords: new[] { "renewal", "sertifikat", "expired", "kadaluarsa", "perpanjangan" }
        ),
    };

    public static readonly IReadOnlyList<GuideFaqItem> Faqs = new List<GuideFaqItem>
    {
        // ═══════════════════ Akun & Login ═══════════════════
        new GuideFaqItem(
            Id: "faq-akun-login",
            Category: FaqCategory.Akun,
            Question: "Bagaimana cara login ke HC Portal?",
            AnswerHtml: "Buka halaman login di browser Anda, masukkan <strong>email</strong> dan <strong>password</strong> yang sudah terdaftar di sistem, lalu klik <strong>Login</strong>. Pastikan akun Anda sudah aktif dan diverifikasi oleh admin.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "login", "cara", "masuk", "akun" }
        ),
        new GuideFaqItem(
            Id: "faq-akun-lupa-password",
            Category: FaqCategory.Akun,
            Question: "Bagaimana jika lupa password?",
            AnswerHtml: "Hubungi <strong>Admin atau tim HC</strong> melalui jalur komunikasi internal untuk meminta reset password. Saat ini belum tersedia fitur self-service lupa password.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "lupa", "password", "reset" }
        ),
        new GuideFaqItem(
            Id: "faq-akun-daftar-baru",
            Category: FaqCategory.Akun,
            Question: "Siapa yang bisa mendaftarkan akun baru?",
            AnswerHtml: "Hanya <strong>Admin dan HC</strong> yang dapat menambahkan pekerja baru ke sistem melalui menu <strong>Admin Panel → Kelola Pekerja</strong>. Pekerja tidak dapat mendaftar sendiri.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "daftar", "akun baru", "registrasi", "pekerja" }
        ),
        new GuideFaqItem(
            Id: "faq-akun-ganti-password",
            Category: FaqCategory.Akun,
            Question: "Bagaimana cara mengganti password?",
            AnswerHtml: "Klik <strong>avatar / nama Anda</strong> di kanan atas navbar → pilih <strong>Settings</strong> → masukkan password lama dan password baru → klik <strong>Simpan</strong>.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "ganti", "ubah", "password", "settings" }
        ),
        new GuideFaqItem(
            Id: "faq-akun-akses-menu",
            Category: FaqCategory.Akun,
            Question: "Kenapa saya tidak bisa mengakses menu tertentu?",
            AnswerHtml: "Beberapa menu hanya tersedia untuk role tertentu. Menu <strong>Kelola Data</strong> dan <strong>Admin Panel</strong> hanya tampil untuk role <strong>Admin</strong> dan <strong>HC</strong>. Jika Anda merasa perlu akses lebih, hubungi Admin sistem.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "akses", "menu", "tersembunyi", "role", "hak" }
        ),

        // ═══════════════════ Assessment ═══════════════════
        new GuideFaqItem(
            Id: "faq-assessment-apa-itu",
            Category: FaqCategory.Assessment,
            Question: "Apa itu assessment?",
            AnswerHtml: "Assessment adalah <strong>ujian kompetensi</strong> yang digunakan untuk mengukur tingkat pengetahuan dan kemampuan sesuai KKJ (Kebutuhan Kompetensi Jabatan) pada posisi Anda.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "assessment", "apa", "ujian", "kompetensi" }
        ),
        new GuideFaqItem(
            Id: "faq-assessment-batas-waktu",
            Category: FaqCategory.Assessment,
            Question: "Apakah ada batas waktu mengerjakan assessment?",
            AnswerHtml: "Ya, setiap assessment memiliki <strong>timer</strong> yang terlihat di bagian atas halaman ujian. Jika waktu habis, jawaban yang sudah diisi akan <strong>otomatis tersubmit</strong>. Jangan menutup browser saat ujian berlangsung.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "batas waktu", "timer", "assessment", "ujian" }
        ),
        new GuideFaqItem(
            Id: "faq-assessment-ulang",
            Category: FaqCategory.Assessment,
            Question: "Bisakah saya mengerjakan ulang assessment?",
            AnswerHtml: "Tergantung pada <strong>konfigurasi yang ditetapkan Admin</strong>. Beberapa assessment hanya boleh diambil sekali, sementara yang lain dapat diulang. Hubungi Admin atau HC jika Anda memerlukan izin untuk mengulang.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "ulang", "kembali", "assessment", "retake" }
        ),
        new GuideFaqItem(
            Id: "faq-assessment-kategori",
            Category: FaqCategory.Assessment,
            Question: "Apa saja kategori assessment yang tersedia?",
            AnswerHtml: "Tersedia 6 kategori assessment: <strong>Assessment OJ</strong> (On the Job), <strong>IHT</strong> (In House Training), <strong>Licencor</strong> (Training Licencor/Sertifikasi), <strong>OTS</strong> (On The Spot), <strong>Mandatory HSSE Training</strong>, dan <strong>Assessment Proton</strong> (terkait program Coaching Proton). Selain itu, tersedia juga tipe <strong>Pre-Post Test</strong> untuk mengukur peningkatan kompetensi sebelum dan sesudah program pelatihan.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "kategori", "jenis", "assessment", "ojt", "iht", "licencor", "ots", "mandatory", "hsse", "proton" }
        ),
        new GuideFaqItem(
            Id: "faq-assessment-pre-post",
            Category: FaqCategory.Assessment,
            Question: "Apa itu assessment Pre-Post Test?",
            AnswerHtml: "Pre-Post Test adalah tipe assessment khusus untuk mengukur <strong>peningkatan kompetensi</strong> sebelum dan sesudah program pelatihan. Peserta mengerjakan <strong>Pre-Test</strong> sebelum pelatihan dan <strong>Post-Test</strong> setelah pelatihan, dengan jadwal yang ditentukan oleh Admin. Hasil kedua test dibandingkan untuk mengukur <strong>Gain Score</strong> (selisih peningkatan nilai). Lihat panduan step-by-step di <a href=\"/Home/GuideDetail?module=cmp#collapse-cmp-pre-post-test\"><strong>CMP → Cara Mengerjakan Pre-Post Test</strong></a>.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "pre-post", "test", "pretest", "posttest", "gain score", "peningkatan", "kompetensi" }
        ),
        new GuideFaqItem(
            Id: "faq-assessment-sertifikat-expired",
            Category: FaqCategory.Assessment,
            Question: "Apakah sertifikat assessment memiliki masa berlaku?",
            AnswerHtml: "Ya, beberapa sertifikat memiliki <strong>masa berlaku</strong> yang ditentukan Admin. Sertifikat yang mendekati masa kadaluarsa akan ditandai dengan status <strong>Akan Expired</strong> di halaman <strong>Analytics Dashboard</strong> (CMP) dan <strong>Renewal Management</strong> (Admin Panel). Periksa secara berkala dan hubungi HC untuk proses renewal assessment.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "sertifikat", "kadaluarsa", "expired", "masa berlaku", "renewal" }
        ),

        // ═══════════════════ CDP & Coaching ═══════════════════
        new GuideFaqItem(
            Id: "faq-cdp-cmp-vs-cdp",
            Category: FaqCategory.CdpCoaching,
            Question: "Apa perbedaan CMP dan CDP?",
            AnswerHtml: "<strong>CMP (Competency Management Platform)</strong> berfokus pada pengelolaan kompetensi: KKJ, assessment, dan training records.<br><br><strong>CDP (Competency Development Platform)</strong> berfokus pada pengembangan karier: IDP, coaching terstruktur, dan deliverable pengembangan diri.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "perbedaan", "cmp", "cdp", "beda", "apa" }
        ),
        new GuideFaqItem(
            Id: "faq-cdp-idp",
            Category: FaqCategory.CdpCoaching,
            Question: "Apa itu IDP (Individual Development Plan)?",
            AnswerHtml: "IDP adalah <strong>rencana pengembangan kompetensi personal</strong> yang disusun untuk setiap pekerja. Berisi daftar aktivitas pengembangan (pelatihan, sertifikasi, dll.) yang perlu diselesaikan dalam periode tertentu.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "idp", "individual development plan", "apa" }
        ),
        new GuideFaqItem(
            Id: "faq-cdp-coaching-proton",
            Category: FaqCategory.CdpCoaching,
            Question: "Apa itu Coaching Proton?",
            AnswerHtml: "Coaching Proton adalah <strong>program coaching terstruktur</strong> di mana setiap coachee (pekerja) memiliki deliverable yang harus diselesaikan, dibuktikan dengan evidence, dan disetujui oleh coach atau atasan yang ditentukan.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "coaching", "proton", "apa itu" }
        ),
        new GuideFaqItem(
            Id: "faq-cdp-approve-deliverable",
            Category: FaqCategory.CdpCoaching,
            Question: "Siapa yang bisa menyetujui (approve) deliverable?",
            AnswerHtml: "Deliverable melewati <strong>rantai approval bertingkat</strong>: setelah coachee upload evidence, deliverable di-review oleh <strong>Sr. Supervisor</strong> → <strong>Section Head</strong> → <strong>HC Review</strong>. Jika salah satu reviewer menolak, <em>seluruh rantai approval di-reset</em> dan coachee harus upload ulang evidence. Mapping coach-coachee dikelola oleh Admin/HC. Lihat panduan lengkap di <a href=\"/Home/GuideDetail?module=cdp#collapse-cdp-reviewer-chain\"><strong>CDP → Memahami Alur Reviewer</strong></a>.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "approve", "approval", "deliverable", "siapa", "coach", "atasan", "sr supervisor", "section head", "hc" }
        ),
        new GuideFaqItem(
            Id: "faq-coach-coachee-self",
            Category: FaqCategory.CdpCoaching,
            Question: "Bagaimana saya tahu siapa coach saya?",
            AnswerHtml: "Buka menu <strong>CDP</strong> → halaman utama menampilkan <strong>Mapping Coach-Coachee</strong> Anda di bagian atas. Nama coach dan unit terlihat jelas. Kalau belum ada mapping, hubungi Admin/HC untuk assignment.",
            Roles: new[] { RoleGroup.Coachee },
            Keywords: new[] { "coach", "saya", "siapa", "mapping" }
        ),
        new GuideFaqItem(
            Id: "faq-supervisor-vs-coach",
            Category: FaqCategory.CdpCoaching,
            Question: "Apa beda Coach dan Supervisor?",
            AnswerHtml: "<strong>Coach</strong> dan <strong>Supervisor</strong> punya level akses sama (Level 5). Bedanya: <strong>Coach</strong> punya mapping ke coachee tertentu (bertanggung jawab langsung untuk coaching session + validasi final assessment). <strong>Supervisor</strong> punya akses sistem yang sama tapi tidak punya mapping coachee — biasanya untuk role pengawas yang tidak terlibat langsung coaching.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "coach", "supervisor", "beda", "perbedaan", "level" }
        ),

        // ═══════════════════ Umum ═══════════════════
        new GuideFaqItem(
            Id: "faq-umum-browser",
            Category: FaqCategory.Umum,
            Question: "Browser apa yang direkomendasikan?",
            AnswerHtml: "Disarankan menggunakan <strong>Google Chrome</strong>, <strong>Microsoft Edge</strong>, atau <strong>Mozilla Firefox</strong> versi terbaru untuk pengalaman terbaik. Hindari menggunakan Internet Explorer atau browser lawas.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "browser", "chrome", "edge", "firefox", "rekomendasi" }
        ),
        new GuideFaqItem(
            Id: "faq-umum-hp-mobile",
            Category: FaqCategory.Umum,
            Question: "Apakah HC Portal bisa diakses dari HP?",
            AnswerHtml: "Ya, HC Portal dirancang <strong>responsif (mobile-friendly)</strong> dan dapat diakses melalui browser di perangkat smartphone atau tablet. Namun untuk kemudahan pengisian data yang kompleks, disarankan menggunakan komputer/laptop.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "hp", "mobile", "handphone", "akses", "ponsel" }
        ),
        new GuideFaqItem(
            Id: "faq-umum-kendala-hubungi",
            Category: FaqCategory.Umum,
            Question: "Siapa yang harus dihubungi jika ada kendala?",
            AnswerHtml: "Hubungi <strong>tim HC atau Admin sistem</strong> melalui jalur komunikasi internal yang berlaku di organisasi Anda (email, grup pesan, atau datang langsung).",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "kendala", "masalah", "error", "hubungi", "kontak", "bantuan" }
        ),
        new GuideFaqItem(
            Id: "faq-umum-role-system",
            Category: FaqCategory.Umum,
            Question: "Bagaimana cara mengetahui role saya di sistem?",
            AnswerHtml: "Sistem HC Portal memiliki <strong>10 role</strong> dengan 6 level akses:<br><br><strong>Level 1 — Admin</strong>: Akses penuh ke seluruh fitur dan konfigurasi sistem<br><strong>Level 2 — HC</strong>: Akses penuh untuk pengelolaan SDM dan coaching<br><strong>Level 3 — Direktur, VP, Manager</strong>: Akses manajemen untuk monitoring seluruh section/unit<br><strong>Level 4 — Section Head, Sr Supervisor</strong>: Akses monitoring pada level section masing-masing serta review deliverable<br><strong>Level 5 — Coach, Supervisor</strong>: Akses coaching dan pengelolaan coachee yang di-assign<br><strong>Level 6 — Coachee</strong>: Akses untuk mengerjakan assessment, melihat silabus, dan mengelola deliverable pribadi<br><br>Role Anda ditampilkan di <strong>navbar kanan atas</strong> sebagai badge di bawah nama Anda.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "role", "saya", "apa", "posisi", "admin", "hc", "coachee",
                              "section head", "sr supervisor", "supervisor",
                              "manager", "vp", "direktur" }
        ),
        new GuideFaqItem(
            Id: "faq-umum-data-aman",
            Category: FaqCategory.Umum,
            Question: "Apakah data saya aman di HC Portal?",
            AnswerHtml: "Ya. HC Portal menggunakan sistem <strong>autentikasi dan otorisasi berbasis role</strong> yang ketat. Setiap user hanya dapat mengakses data sesuai hak aksesnya. Data penting diproteksi oleh mekanisme keamanan aplikasi.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "aman", "data", "keamanan", "privasi" }
        ),

        // ═══════════════════ KKJ & CPDP ═══════════════════
        new GuideFaqItem(
            Id: "faq-kkj-matrix-apa",
            Category: FaqCategory.KkjCpdp,
            Question: "Apa itu KKJ Matrix?",
            AnswerHtml: "KKJ Matrix (Kebutuhan Kompetensi Jabatan) adalah <strong>dokumen standar kompetensi</strong> yang mendefinisikan kompetensi apa saja yang diperlukan untuk setiap jabatan/posisi dalam organisasi.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "kkj", "matrix", "apa", "kebutuhan kompetensi jabatan" }
        ),
        new GuideFaqItem(
            Id: "faq-kkj-lihat-saya",
            Category: FaqCategory.KkjCpdp,
            Question: "Di mana saya bisa melihat KKJ saya?",
            AnswerHtml: "Masuk ke halaman <strong>CMP</strong> → klik tab <strong>Library KKJ</strong>. KKJ yang ditampilkan sudah disesuaikan dengan posisi/jabatan Anda.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "kkj", "mana", "lihat", "saya", "dimana" }
        ),
        new GuideFaqItem(
            Id: "faq-cpdp-apa-itu",
            Category: FaqCategory.KkjCpdp,
            Question: "Apa itu CPDP?",
            AnswerHtml: "CPDP (Competency and Proficiency Development Program) adalah <strong>peta program pengembangan kompetensi</strong> yang menghubungkan gap kompetensi pekerja dengan program pelatihan atau kegiatan yang relevan.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "cpdp", "apa", "competency", "proficiency", "development", "program" }
        ),
        new GuideFaqItem(
            Id: "faq-kkj-upload-file",
            Category: FaqCategory.KkjCpdp,
            Question: "Bagaimana cara upload file KKJ atau CPDP?",
            AnswerHtml: "Fitur ini hanya tersedia untuk <strong>Admin dan HC</strong>. Masuk ke <strong>Admin Panel</strong> → pilih <strong>KKJ Matrix Files</strong> atau <strong>CPDP Files</strong> → klik <strong>Upload File</strong> → pilih file → Simpan.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "upload", "kkj", "cpdp", "file", "admin", "hc" }
        ),

        // ═══════════════════ Admin & Kelola Data (AdminHC) ═══════════════════
        new GuideFaqItem(
            Id: "faq-admin-tambah-pekerja",
            Category: FaqCategory.AdminData,
            Question: "Bagaimana cara menambahkan pekerja baru?",
            AnswerHtml: "Masuk ke <strong>Admin Panel → Kelola Pekerja</strong> → klik tombol <strong>Tambah Pekerja</strong> → isi formulir data pekerja → simpan. Untuk import massal, gunakan fitur <strong>Import Workers</strong> sesuai template Excel yang tersedia.",
            Roles: new[] { RoleGroup.AdminHC },
            Keywords: new[] { "tambah", "pekerja", "baru", "cara", "admin" }
        ),
        new GuideFaqItem(
            Id: "faq-admin-import-pekerja",
            Category: FaqCategory.AdminData,
            Question: "Bagaimana cara import data pekerja secara massal?",
            AnswerHtml: "Masuk ke <strong>Admin Panel → Kelola Pekerja</strong> → klik <strong>Import Workers</strong> → download template Excel yang tersedia → isi data pekerja sesuai format → upload kembali file tersebut.",
            Roles: new[] { RoleGroup.AdminHC },
            Keywords: new[] { "import", "massal", "pekerja", "excel", "template" }
        ),
        new GuideFaqItem(
            Id: "faq-admin-mapping-coach",
            Category: FaqCategory.AdminData,
            Question: "Bagaimana cara mapping Coach-Coachee?",
            AnswerHtml: "Masuk ke <strong>Admin Panel → Coach-Coachee Mapping</strong> → klik <strong>Tambah Mapping</strong> → pilih Coach dan Coachee → tentukan periode mapping → simpan. Mapping yang sudah tidak aktif dapat dinonaktifkan menggunakan tombol <strong>Nonaktifkan</strong>.",
            Roles: new[] { RoleGroup.AdminHC },
            Keywords: new[] { "mapping", "coach", "coachee", "cara" }
        ),
        new GuideFaqItem(
            Id: "faq-admin-audit-log",
            Category: FaqCategory.AdminData,
            Question: "Apa itu Audit Log?",
            AnswerHtml: "Audit Log adalah <strong>catatan otomatis</strong> yang merekam semua aktivitas penting yang dilakukan user di sistem (login, upload file, perubahan data, dll.). Berguna untuk tracking, investigasi, dan keamanan sistem.",
            Roles: new[] { RoleGroup.AdminHC },
            Keywords: new[] { "audit", "log", "apa", "aktivitas" }
        ),
        new GuideFaqItem(
            Id: "faq-admin-silabus-proton",
            Category: FaqCategory.AdminData,
            Question: "Bagaimana cara mengelola data Silabus Proton?",
            AnswerHtml: "Masuk ke menu <strong>Kelola Data</strong> di navbar → di halaman Admin Panel, pilih section <strong>Kelola Data Proton</strong>. Di sini Anda bisa mengelola <strong>Silabus</strong> (import/export via Excel, tambah/edit/hapus kompetensi), upload <strong>Guidance Document</strong>, dan membuat <strong>Override</strong> jika diperlukan penyesuaian silabus.",
            Roles: new[] { RoleGroup.AdminHC },
            Keywords: new[] { "silabus", "proton", "import", "data", "kelola", "override", "guidance" }
        ),
        new GuideFaqItem(
            Id: "faq-admin-analytics",
            Category: FaqCategory.AdminData,
            Question: "Di mana saya bisa melihat statistik dan laporan assessment?",
            AnswerHtml: "Buka menu <strong>CMP</strong> → klik <strong>Analytics Dashboard</strong>. Halaman ini menampilkan grafik dan statistik assessment berdasarkan unit, kategori, distribusi hasil, serta daftar sertifikat yang akan segera kadaluarsa.",
            Roles: new[] { RoleGroup.AdminHC },
            Keywords: new[] { "analytics", "dashboard", "laporan", "statistik", "assessment" }
        ),
        new GuideFaqItem(
            Id: "faq-admin-monitoring-ujian",
            Category: FaqCategory.AdminData,
            Question: "Bagaimana cara memonitor ujian yang sedang berlangsung?",
            AnswerHtml: "Masuk ke <strong>Admin Panel → Assessment Monitoring</strong>. Anda bisa memantau sesi ujian <strong>secara real-time</strong>: melihat progress peserta, skor sementara, dan jika diperlukan dapat <strong>mengakhiri sesi ujian</strong> atau <strong>me-reset assessment</strong> peserta tertentu.",
            Roles: new[] { RoleGroup.AdminHC },
            Keywords: new[] { "monitoring", "ujian", "realtime", "pantau", "assessment" }
        ),
        new GuideFaqItem(
            Id: "faq-admin-export-laporan",
            Category: FaqCategory.AdminData,
            Question: "Laporan apa saja yang bisa di-export?",
            AnswerHtml: "Tersedia berbagai laporan yang bisa di-download: <strong>Records Assessment</strong> (Excel), <strong>Progress Deliverable</strong> (Excel/PDF), <strong>Coaching Tracking</strong>, <strong>Bottleneck Report</strong> (deliverable yang terhambat), <strong>Workload Summary</strong> (beban kerja coach), <strong>Sertifikat</strong>, <strong>Histori Proton</strong>, dan <strong>Audit Log</strong>.",
            Roles: new[] { RoleGroup.AdminHC },
            Keywords: new[] { "export", "laporan", "excel", "pdf", "coaching", "bottleneck", "workload" }
        ),
    };

    public static readonly IReadOnlyList<GuidePdfLink> Pdfs = new List<GuidePdfLink>
    {
        new GuidePdfLink(
            Module: GuideModule.Cmp,
            Title: "Panduan Lengkap Assessment",
            Description: "Tutorial end-to-end mengerjakan assessment: dari mulai ujian, mengerjakan soal, submit, melihat hasil, hingga download sertifikat.",
            FilePath: "~/documents/guides/Panduan-Lengkap-Assessment.html",
            CardCssClass: "guide-tutorial-card--cmp",
            BtnColorClass: "btn-primary",
            Roles: new[] { RoleGroup.All }
        ),
        new GuidePdfLink(
            Module: GuideModule.Cdp,
            Title: "Panduan Lengkap Coaching Proton",
            Description: "Tutorial end-to-end untuk Coachee, Coach, dan Atasan: dari melihat silabus, upload evidence, approval, hingga Final Assessment.",
            FilePath: "~/documents/guides/Panduan-Lengkap-Coaching-Proton.html",
            CardCssClass: "guide-tutorial-card--cdp",
            BtnColorClass: "btn-success",
            Roles: new[] { RoleGroup.All }
        ),
        new GuidePdfLink(
            Module: GuideModule.Cmp,
            Title: "Panduan Buat Assessment & Input Soal",
            Description: "Tutorial lengkap membuat assessment, mengelola paket soal, dan mengimpor soal dari Excel — untuk Admin & HC.",
            FilePath: "~/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html",
            CardCssClass: "guide-tutorial-card--cmp",
            BtnColorClass: "btn-primary",
            Roles: new[] { RoleGroup.AdminHC }
        ),
        new GuidePdfLink(
            Module: GuideModule.Admin,
            Title: "Panduan Konfigurasi Active Directory",
            Description: "Tutorial lengkap konfigurasi Active Directory untuk integrasi autentikasi HC Portal.",
            FilePath: "~/documents/guides/ActiveDirectory-Guide.html",
            CardCssClass: "guide-tutorial-card--admin",
            BtnColorClass: "btn-danger",
            Roles: new[] { RoleGroup.AdminHC }
        ),
        new GuidePdfLink(
            Module: GuideModule.Account,
            Title: "Panduan Penggunaan Website HC Portal KPB",
            Description: "Tutorial umum penggunaan HC Portal: navigasi, fitur dasar, dan tips & trik untuk semua role.",
            FilePath: "~/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html",
            CardCssClass: "guide-tutorial-card--account",
            BtnColorClass: "btn-info",
            Roles: new[] { RoleGroup.All }
        ),
    };

    public static IReadOnlyList<GuideItem> GetItems(GuideModule module, string userRole)
        => Items
            .Where(i => i.Module == module && GuideRoleAccess.CanSee(userRole, i.Roles))
            .ToList();

    public static IReadOnlyDictionary<FaqCategory, IReadOnlyList<GuideFaqItem>> GetFaqsByCategory(string userRole)
        => Faqs
            .Where(f => GuideRoleAccess.CanSee(userRole, f.Roles))
            .GroupBy(f => f.Category)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<GuideFaqItem>)g.ToList()
            );

    public static IReadOnlyList<GuidePdfLink> GetPdfs(GuideModule module, string userRole)
        => Pdfs.Where(p => p.Module == module && GuideRoleAccess.CanSee(userRole, p.Roles)).ToList();

    public static IReadOnlyList<GuideModuleCardVm> GetModuleCards(string userRole)
    {
        var allModules = new[]
        {
            (Module: GuideModule.Cmp,     Title: "CMP — Competency Management Platform", Short: "CMP",           Icon: "bi-journal-bookmark-fill", Card: "card-cmp",     AosDelay: 100, Roles: new[] { RoleGroup.All },
             Keywords: new[] { "cmp", "competency", "assessment", "kkj", "cpdp", "mapping", "sertifikat", "training", "records", "library", "ujian", "soal" }),
            (Module: GuideModule.Cdp,     Title: "CDP — Competency Development Platform", Short: "CDP",           Icon: "bi-graph-up-arrow",        Card: "card-cdp",     AosDelay: 150, Roles: new[] { RoleGroup.All },
             Keywords: new[] { "cdp", "career", "development", "idp", "silabus", "coaching", "proton", "deliverable", "evidence", "upload", "approval", "plan" }),
            (Module: GuideModule.Account, Title: "Akun & Profil",                         Short: "Akun & Profil", Icon: "bi-person-circle",         Card: "card-account", AosDelay: 200, Roles: new[] { RoleGroup.All },
             Keywords: new[] { "akun", "profil", "password", "ganti", "settings", "login", "logout", "edit", "nama" }),
            (Module: GuideModule.Data,    Title: "Kelola Data",                           Short: "Kelola Data",   Icon: "bi-database-fill",         Card: "card-data",    AosDelay: 250, Roles: new[] { RoleGroup.AdminHC },
             Keywords: new[] { "kelola", "data", "proton", "import", "sinkronisasi", "override", "sync" }),
            (Module: GuideModule.Admin,   Title: "Admin Panel",                           Short: "Admin Panel",   Icon: "bi-gear-wide-connected",   Card: "card-admin",   AosDelay: 300, Roles: new[] { RoleGroup.AdminHC },
             Keywords: new[] { "admin", "panel", "kelola", "pekerja", "kkj", "cpdp", "upload", "mapping", "coach", "coachee", "assessment", "monitoring", "audit", "log" }),
        };

        return allModules
            .Where(m => GuideRoleAccess.CanSee(userRole, m.Roles))
            .Select(m => new GuideModuleCardVm(
                Module: m.Module,
                Title: m.Title,
                ShortLabel: m.Short,
                IconCssClass: m.Icon,
                CardCssClass: m.Card,
                ItemCount: GetItems(m.Module, userRole).Count + GetPdfs(m.Module, userRole).Count,
                AosDelay: m.AosDelay,
                Roles: m.Roles,
                Keywords: m.Keywords
            ))
            .ToList();
    }

    // Phase 4: auto-keyword builder. Gabung title + module + role label + manual keywords
    // + step titles (untuk GuideItem). Eliminasi miss search untuk role names tanpa
    // perlu manual tag tiap item.
    public static string BuildKeywords(GuideItem item)
    {
        var parts = new System.Collections.Generic.List<string>
        {
            item.Title.ToLowerInvariant(),
            item.Module.ToString().ToLowerInvariant(),
            GuideRoleAccess.BadgeLabel(item.Roles).ToLowerInvariant()
        };
        parts.AddRange(item.Steps.Select(s => s.Title.ToLowerInvariant()));
        parts.AddRange(item.Keywords);
        return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    public static string BuildKeywords(GuideFaqItem faq)
    {
        var parts = new System.Collections.Generic.List<string>
        {
            faq.Question.ToLowerInvariant(),
            faq.Category.ToString().ToLowerInvariant(),
            GuideRoleAccess.BadgeLabel(faq.Roles).ToLowerInvariant()
        };
        parts.AddRange(faq.Keywords);
        return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    public static string BuildKeywords(GuideModuleCardVm card)
    {
        var parts = new System.Collections.Generic.List<string>
        {
            card.Title.ToLowerInvariant(),
            card.Module.ToString().ToLowerInvariant(),
            GuideRoleAccess.BadgeLabel(card.Roles).ToLowerInvariant()
        };
        parts.AddRange(card.Keywords);
        return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
