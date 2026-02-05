# Analisis Website PortalHC_KPB

Dokumen ini berisi analisis detail mengenai struktur, properti, dan fitur dari proyek **PortalHC_KPB**.

## 1. Identitas & Teknologi (Tech Stack)

*   **Framework**: ASP.NET Core 8.0 (MVC Architecture)
*   **Bahasa Pemrograman**: C#
*   **Database**: SQLite (`HcPortal.db`)
    *   *Penggunaan*: Saat ini database operasional utamanya digunakan untuk fitur **Identity** (User, Role, Login).
    *   *Catatan*: Sebagian besar data bisnis (Training Records, Matriks Kompetensi, IDP) saat ini diimplementasikan menggunakan **Mock Data** (Hardcoded di Controller) untuk keperluan rapid prototyping.
*   **ORM**: Entity Framework Core
*   **Frontend**: Razor Views (`.cshtml`) dengan Bootstrap 5 (Responsive UI).
*   **Authentication**: ASP.NET Core Identity.

## 2. Struktur Project

Struktur folder mengikuti standar **ASP.NET Core MVC**:

| Direktori | Deskripsi |
| :--- | :--- |
| `Controllers/` | Berisi logika alur aplikasi (Brain). Controller utama: `CMPController`, `CDPController`, `BPController`. |
| `Views/` | Berisi tampilan antarmuka (UI). Dikelompokkan berdasarkan folder Controller (`CMP`, `CDP`, `BP`). |
| `Models/` | Berisi struktur data (Class). Contoh: `KkjMatrixItem`, `TrainingRecord`, `ApplicationUser`. |
| `Data/` | Berisi `ApplicationDbContext` untuk konfigurasi koneksi database. |
| `wwwroot/` | Berisi file statis seperti CSS, JS, Gambar, dan file PDF sertifikat. |

## 3. Fitur & Modul Utama (Functional Modules)

Website ini terdiri dari 3 pilar modul utama:

### A. CMP (Competency Management Platform)
*Controller: `CMPController`*
Fokus pada manajemen standar kompetensi dan data pelatihan operasional.

1.  **KKJ (Kamus & Matriks Kompetensi)**
    *   Menampilkan matriks target level skill untuk setiap posisi (Operator, Panelman, Shift Spv, dll).
    *   Menggunakan layout tabel kompleks untuk memetakan skill vs target role.
2.  **Mapping (KKJ - CPDP)**
    *   Pemetaan detail antara Kompetensi teknis dengan Indikator Perilaku dan Silabus pembelajaran.
3.  **Assessment Lobby**
    *   Halaman dashboard untuk ujian/assessment (OJT, IHT, OTS, Mandatory Training).
    *   Menampilkan status: *Open, Upcoming, Completed*.
4.  **Records (Capability Building Records)**
    *   **Fitur Vital**: Monitoring status training karyawan.
    *   **Logic Role-Based**:
        *   View untuk Supervisor/HC: Melihat list tim, filter by Section/Unit, status completion.
        *   View untuk Coach/Coachee: Melihat history training pribadi dan unduh sertifikat.

### B. CDP (Competency Development Platform)
*Controller: `CDPController`*
Fokus pada pengembangan karir jangka panjang dan individual plan.

1.  **Dashboard Executive**
    *   Menampilkan statistik visual: Completion Rate, Budget Usage, dan Compliance per Unit.
2.  **Tracking Progress**
    *   Tabel flat untuk monitoring item IDP (Individual Development Plan).
    *   Fitur approval berjenjang: *Sr Supervisor -> Section Head -> HC*.
3.  **Coaching Log**
    *   Pecatatan sejarah sesi coaching antara atasan dan bawahan.

### C. BP (Business Partner / People Management)
*Controller: `BPController`*
Fokus pada profil talent, promosi, dan gamifikasi.

1.  **Talent Profile**: CV digital karyawan lengkap dengan history karir dan performa tahunan (PA).
2.  **Point System**: Gamifikasi reward untuk keaktifan (Innovation, Leadership, Attendance).
3.  **Eligibility Validator**: Pengecekan otomatis syarat promosi/mutasi (Tenure, TOEIC, PA Rating).

## 4. Keamanan & Akses (Security)

*   **Role-Based Access Control (RBAC)**
    *   Login sistem membedakan akses antara: `HC`, `Section Head`, `Sr Supervisor`, `Operator`, dan `Coach`.
    *   Tampilan menu dan data menyesuaikan role. Contoh: Supervisor Unit A tidak bisa melihat data Unit B (logika sudah dipersiapkan di controller).
*   **MIME Type Protection**
    *   Terdapat konfigurasi khusus di `Program.cs` untuk memastikan file PDF ditampilkan secara aman di browser (inline) dan mencegah *sniffing*.
    *   `X-Content-Type-Options: nosniff`.

## 5. Status Pengembangan

*   **Status**: **High-Fidelity Prototype**.
    *   Secara visual (UI/UX) sudah sangat matang dan siap presentasi ("Premium Look").
    *   Alur navigasi sudah berfungsi penuh.
*   **Data Source**: Hybrid.
    *   User Management -> **Real Database** (SQLite).
    *   Business Data (Training, Matrix) -> **Mock Data** (Perlu migrasi ke SQL Server/PostgreSQL untuk fase Production).
