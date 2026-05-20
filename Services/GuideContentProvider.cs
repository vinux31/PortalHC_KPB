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
            Title: "Cara Melihat Library KKJ (Kebutuhan Kompetensi Jabatan)",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Akses CMP", "Klik menu <b>CMP</b> di navbar atas."),
                new GuideStep(2, "Buka Tab Library", "Klik tab <b>Library KKJ</b>. Anda akan melihat daftar kompetensi khusus untuk posisi Anda.")
            },
            Keywords: new[] { "kkj", "library", "kebutuhan kompetensi jabatan" }
        ),
        new GuideItem(
            Id: "cmp-mapping-kkj-cpdp",
            Module: GuideModule.Cmp,
            Title: "Cara Melihat Mapping KKJ — CPDP",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Akses CMP", "Klik menu <b>CMP</b> di navbar atas."),
                new GuideStep(2, "Buka Tab Mapping", "Klik tab <b>CPDP Mapping</b> untuk melihat pemetaan pengembangan kompetensi Anda.")
            },
            Keywords: new[] { "mapping", "cpdp", "kkj" }
        ),
        new GuideItem(
            Id: "cmp-training-records",
            Module: GuideModule.Cmp,
            Title: "Cara Melihat Riwayat Training (Capability Building Records)",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Buka Tab Training Records", "Di menu CMP, klik tab <b>Training Records</b> (Riwayat Pelatihan)."),
                new GuideStep(2, "Review Riwayat", "Semua pelatihan baik internal maupun eksternal yang di-input oleh HC akan tampil di tabel riwayat beserta jam pelatihannya.")
            },
            Keywords: new[] { "training", "riwayat", "records", "capability building" }
        ),
        new GuideItem(
            Id: "cmp-monitoring-records-tim",
            Module: GuideModule.Cmp,
            Title: "Cara Monitoring Records Tim",
            Roles: new[] { RoleGroup.AdminHC },
            Steps: new[]
            {
                new GuideStep(1, "Tab Records Team", "Di menu CMP, klik tab <b>Records Team</b>."),
                new GuideStep(2, "List Anggota Tim", "Anda akan melihat semua anggota tim di bawah supervisi Anda beserta persentase kepatuhan (compliance) dan jam pelatihannya."),
                new GuideStep(3, "Detail Pekerja", "Klik nama pekerja untuk melihat lebih detail riwayat assessment dan training-nya.")
            },
            Keywords: new[] { "monitoring", "records team", "compliance" }
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
            Roles: new[] { RoleGroup.AdminHC },
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
            Roles: new[] { RoleGroup.AdminHC },
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
            Id: "account-profile-view-edit",
            Module: GuideModule.Account,
            Title: "Cara Melihat & Edit Profil (Nama, Posisi)",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Buka Profil", "Klik avatar profil Anda di pojok kanan atas layar, lalu pilih <b>My Profile</b>."),
                new GuideStep(2, "Edit Data Personal", "Klik tombol pensil/Edit di profil. Edit informasi yang diperlukan, lalu tekan Simpan.")
            },
            Keywords: new[] { "profil", "edit", "nama", "posisi", "my profile" }
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
            Id: "account-logout-role-system",
            Module: GuideModule.Account,
            Title: "Cara Logout & Memahami Role System",
            Roles: new[] { RoleGroup.All },
            Steps: new[]
            {
                new GuideStep(1, "Logout dari Portal", "Klik avatar profil di pojok kanan atas, lalu pilih <b>Logout</b> untuk keluar dari sistem dengan aman."),
                new GuideStep(2, "Memahami Role Badge", "Role Anda ditampilkan di navbar (badge: HC, Admin, Coachee). Role menentukan akses menu. Hubungi Admin jika role tidak sesuai.")
            },
            Keywords: new[] { "logout", "role", "badge", "akses" }
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
            AnswerHtml: "Pre-Post Test adalah tipe assessment khusus untuk mengukur <strong>peningkatan kompetensi</strong> sebelum dan sesudah program pelatihan. Peserta mengerjakan <strong>Pre-Test</strong> sebelum pelatihan dan <strong>Post-Test</strong> setelah pelatihan, dengan jadwal yang ditentukan oleh Admin. Hasil kedua test dibandingkan untuk mengukur <strong>Gain Score</strong> (selisih peningkatan nilai).",
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
            AnswerHtml: "Deliverable melewati <strong>rantai approval bertingkat</strong>: setelah coach upload evidence, deliverable di-review oleh <strong>Sr. Supervisor</strong> → <strong>Section Head</strong> → <strong>HC Review</strong>. Jika salah satu reviewer menolak, seluruh rantai approval di-reset dan coach harus upload ulang evidence. Mapping coach-coachee dikelola oleh Admin/HC.",
            Roles: new[] { RoleGroup.All },
            Keywords: new[] { "approve", "approval", "deliverable", "siapa", "coach", "atasan", "sr supervisor", "section head", "hc" }
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
            Keywords: new[] { "role", "saya", "apa", "posisi", "admin", "hc", "coachee" }
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
            Module: GuideModule.Data,
            Title: "Panduan Buat Assessment & Input Soal",
            Description: "Tutorial lengkap membuat assessment, mengelola paket soal, dan mengimpor soal dari Excel — untuk Admin & HC.",
            FilePath: "~/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html",
            CardCssClass: "guide-tutorial-card--data",
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

    public static GuidePdfLink? GetPdf(GuideModule module, string userRole)
        => Pdfs.FirstOrDefault(p => p.Module == module && GuideRoleAccess.CanSee(userRole, p.Roles));

    public static IReadOnlyList<GuideModuleCardVm> GetModuleCards(string userRole)
    {
        var allModules = new[]
        {
            (Module: GuideModule.Cmp,     Title: "CMP — Competency Management Platform", Icon: "bi-journal-bookmark-fill", Card: "card-cmp",     Roles: new[] { RoleGroup.All },
             Keywords: new[] { "cmp", "competency", "assessment", "kkj", "cpdp", "mapping", "sertifikat", "training", "records", "library", "ujian", "soal" }),
            (Module: GuideModule.Cdp,     Title: "CDP — Competency Development Platform", Icon: "bi-graph-up-arrow",        Card: "card-cdp",     Roles: new[] { RoleGroup.All },
             Keywords: new[] { "cdp", "career", "development", "idp", "silabus", "coaching", "proton", "deliverable", "evidence", "upload", "approval", "plan" }),
            (Module: GuideModule.Account, Title: "Akun & Profil",                         Icon: "bi-person-circle",         Card: "card-account", Roles: new[] { RoleGroup.All },
             Keywords: new[] { "akun", "profil", "password", "ganti", "settings", "login", "logout", "edit", "nama" }),
            (Module: GuideModule.Data,    Title: "Kelola Data",                           Icon: "bi-database-fill",         Card: "card-data",    Roles: new[] { RoleGroup.AdminHC },
             Keywords: new[] { "kelola", "data", "proton", "import", "sinkronisasi", "override", "sync" }),
            (Module: GuideModule.Admin,   Title: "Admin Panel",                           Icon: "bi-gear-wide-connected",   Card: "card-admin",   Roles: new[] { RoleGroup.AdminHC },
             Keywords: new[] { "admin", "panel", "kelola", "pekerja", "kkj", "cpdp", "upload", "mapping", "coach", "coachee", "assessment", "monitoring", "audit", "log" }),
        };

        return allModules
            .Where(m => GuideRoleAccess.CanSee(userRole, m.Roles))
            .Select(m => new GuideModuleCardVm(
                Module: m.Module,
                Title: m.Title,
                IconCssClass: m.Icon,
                CardCssClass: m.Card,
                ItemCount: GetItems(m.Module, userRole).Count + (GetPdf(m.Module, userRole) != null ? 1 : 0),
                Roles: m.Roles,
                Keywords: m.Keywords
            ))
            .ToList();
    }
}
