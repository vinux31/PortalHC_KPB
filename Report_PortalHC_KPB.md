# REPORT — Portal HC KPB

**Tanggal:** 6 Maret 2026
**Versi Aplikasi:** v3.5 (User Guide Milestone)
**Platform:** ASP.NET Core 8.0 MVC

---

## Daftar Isi

1. [Struktur Organisasi](#1-struktur-organisasi)
2. [Arsitektur Website](#2-arsitektur-website)
3. [Navigasi Menu](#3-navigasi-menu)
4. [Assessment Online](#4-assessment-online)
5. [Training Offline (Manual)](#5-training-offline-manual)
6. [Coaching Proton](#6-coaching-proton)
7. [History Assessment & Training](#7-history-assessment--training)
8. [Rekomendasi Improvement](#8-rekomendasi-improvement)

---

## 1. Struktur Organisasi

### 1.1 Hierarki Bagian & Unit

Portal HC KPB mengelola 4 Bagian (Section) dengan total 19 Unit kerja:

```
┌──────────────────────────────────────────────────────────────────┐
│                        PORTAL HC KPB                             │
│                    (HC & Admin — Lintas Bagian)                   │
└───────────┬──────────┬───────────────┬───────────────┬───────────┘
            │          │               │               │
     ┌──────┴──┐  ┌────┴────┐   ┌──────┴──────┐  ┌────┴────────┐
     │  RFCC   │  │  GAST   │   │     NGP     │  │  DHT / HMU  │
     └────┬────┘  └────┬────┘   └──────┬──────┘  └──────┬──────┘
          │            │               │                 │
  ┌───────┴───────┐    │       ┌───────┴───────┐  ┌──────┴──────┐
  │ RFCC LPG      │    │       │ Saturated Gas │  │ DHT I & II  │
  │ Treating (062)│    │       │ Conc. (060)   │  │ (054 & 083) │
  ├───────────────┤    │       ├───────────────┤  ├─────────────┤
  │ Propylene     │    │       │ Saturated LPG │  │ Hydrogen    │
  │ Recovery (063)│    │       │ Treating (064)│  │ Mfg (068)   │
  └───────────────┘    │       ├───────────────┤  ├─────────────┤
                       │       │ Isomerization │  │ Common DHT  │
               ┌───────┴───┐   │ Unit (082)    │  │ H2 Comp(085)│
               │ RFCC NHT  │   ├───────────────┤  └─────────────┘
               │ (053)     │   │ Common Fac.   │
               ├───────────┤   │ NLP (160)     │
               │Alkylation │   ├───────────────┤
               │(065)      │   │ Naphtha HT    │
               ├───────────┤   │ II (084)      │
               │ Wet Gas   │   └───────────────┘
               │ SAU (066) │
               ├───────────┤
               │ SWS RFCC &│
               │ Non (067/ │
               │ 167)      │
               ├───────────┤
               │ Amine Reg │
               │ I&II (069/│
               │ 079)      │
               ├───────────┤
               │ Flare     │
               │ Sys (319) │
               ├───────────┤
               │ Sulfur    │
               │ Rec (169) │
               └───────────┘
```

### 1.2 Hierarki Role & Level Akses

Sistem menggunakan 10 role dalam 6 level hierarki:

```
Level 1 ─── Admin ──────────────── Akses penuh seluruh sistem
                                    (Section/Unit = null)

Level 2 ─── HC (Human Capital) ─── Kelola data pekerja, assessment,
                                    coaching, laporan
                                    (Section/Unit = null)

Level 3 ─── Direktur ──────────┐
            VP ─────────────────┤── Monitoring & read access
            Manager ────────────┤   seluruh bagian
            Section Head ───────┘   (Section Head = 1 bagian)

Level 4 ─── Sr. Supervisor ─────── Approval deliverable,
                                    monitoring unit sendiri

Level 5 ─── Coach ─────────────┐── Coaching coachee yang di-mapping,
            Supervisor ─────────┘   upload evidence

Level 6 ─── Coachee ────────────── Ikut assessment, lihat progress
                                    sendiri, akses data personal
```

**Ringkasan Akses per Role:**

| Role | CMP (Assessment) | CDP (Coaching) | Kelola Data | Scope Data |
|------|:-:|:-:|:-:|---|
| Admin | ✅ Kelola | ✅ Semua | ✅ Penuh | Seluruh sistem |
| HC | ✅ Kelola | ✅ Semua | ✅ Penuh | Seluruh sistem |
| Direktur/VP/Manager | ✅ Lihat | ✅ Lihat | ❌ | Semua bagian |
| Section Head | ✅ Lihat | ✅ Approve | ❌ | Bagian sendiri |
| Sr. Supervisor | ✅ Lihat | ✅ Approve | ❌ | Unit sendiri |
| Coach/Supervisor | ✅ Ikut ujian | ✅ Upload evidence | ❌ | Coachee mapping |
| Coachee | ✅ Ikut ujian | ✅ Lihat progress | ❌ | Data personal |

---

## 2. Arsitektur Website

### 2.1 Technology Stack

| Layer | Teknologi | Versi |
|-------|-----------|-------|
| **Framework** | ASP.NET Core MVC | 8.0 |
| **Bahasa** | C# | .NET 8 |
| **ORM** | Entity Framework Core | 8.0.0 |
| **Database** | SQL Server (prod) / SQLite (dev fallback) | SQLEXPRESS |
| **Auth** | ASP.NET Core Identity + LDAP | — |
| **Frontend** | Bootstrap 5.3, jQuery 3.7, Chart.js | — |
| **Excel** | ClosedXML | 0.105.0 |
| **PDF** | QuestPDF | 2026.2.2 |
| **LDAP** | System.DirectoryServices | 10.0.0 |
| **Animasi** | AOS (Animate On Scroll) | 2.3.1 |

### 2.2 Diagram Arsitektur

```
┌─────────────────────────────────────────────────────────────┐
│                      BROWSER (Client)                        │
│  Bootstrap 5.3 │ jQuery 3.7 │ Chart.js │ AOS │ Custom JS    │
└────────────────────────────┬────────────────────────────────┘
                             │ HTTP/HTTPS
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                    ASP.NET Core 8 MVC                         │
│                                                               │
│  ┌─────────────┐  ┌──────────────┐  ┌────────────────────┐  │
│  │ Controllers  │  │    Views     │  │     Services       │  │
│  │             │  │  (Razor)     │  │                    │  │
│  │ • Account   │  │ • 54+ views  │  │ • LocalAuthService │  │
│  │ • Home      │  │ • 3 partials │  │ • LdapAuthService  │  │
│  │ • Admin     │  │ • _Layout    │  │ • HybridAuthService│  │
│  │ • CMP       │  │              │  │ • AuditLogService  │  │
│  │ • CDP       │  │              │  │ • NotificationSvc  │  │
│  │ • ProtonData│  │              │  │                    │  │
│  └──────┬──────┘  └──────────────┘  └────────────────────┘  │
│         │                                                     │
│  ┌──────┴──────────────────────────────────────────────────┐ │
│  │              ASP.NET Core Identity                       │ │
│  │      Cookie Auth (8h) │ Session (30min) │ RBAC          │ │
│  └──────┬──────────────────────────────────────────────────┘ │
│         │                                                     │
│  ┌──────┴──────────────────────────────────────────────────┐ │
│  │           Entity Framework Core 8.0                      │ │
│  │              73 DbSets │ 44+ Migrations                  │ │
│  └──────┬──────────────────────────────────────────────────┘ │
└─────────┼───────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────┐     ┌─────────────────────┐
│   SQL Server         │     │   File System        │
│   (SQLEXPRESS)       │     │   /wwwroot/uploads/  │
│                      │     │   • evidence/        │
│   73+ tabel          │     │   • guidance/        │
│   Identity tables    │     │   • kkj/             │
│   Audit logs         │     │   • cpdp/            │
└──────────────────────┘     └─────────────────────┘
```

### 2.3 Mode Autentikasi

```
                    ┌──────────────────┐
                    │   Login Request   │
                    └────────┬─────────┘
                             │
                    ┌────────▼─────────┐
                    │ UseActiveDirectory│
                    │  = true?          │
                    └───┬──────────┬───┘
                   Ya   │          │  Tidak
                        ▼          ▼
              ┌─────────────┐  ┌──────────────┐
              │   Hybrid    │  │    Local     │
              │ AuthService │  │  AuthService │
              └──┬──────────┘  │ (Identity    │
                 │             │  password)   │
          ┌──────┴──────┐     └──────────────┘
          ▼             ▼
   ┌───────────┐  ┌──────────┐
   │   LDAP    │  │  Local   │
   │ (Pertamina│  │ Fallback │
   │  AD)      │  │ (admin@) │
   └───────────┘  └──────────┘
```

- **Development:** Local Auth (password hash via Identity)
- **Production:** Hybrid (LDAP → Active Directory Pertamina, fallback local untuk admin)

### 2.4 Controller Inventory

| Controller | LOC | Methods | Tanggung Jawab | Auth |
|------------|----:|--------:|----------------|------|
| AccountController | 275 | 8 | Login, Logout, Profile, Settings | Per-method |
| HomeController | 292 | 7 | Dashboard, User Guide | [Authorize] |
| AdminController | 5,828 | 91 | KKJ, CPDP, Assessment, Worker, Training, Coaching | [Authorize] + Roles |
| CMPController | 1,885 | 33 | Assessment, Exam Engine, Training Records | [Authorize] |
| CDPController | 2,227 | 28 | Dashboard, PlanIdp, Deliverable, Coaching Proton | [Authorize] |
| ProtonDataController | 792 | 25 | Silabus, Guidance, Override | [Authorize(Roles=Admin,HC)] |
| **Total** | **~11,300** | **~192** | | |

---

## 3. Navigasi Menu

### 3.1 Navbar Structure

```
┌───────────────────────────────────────────────────────────────────┐
│  🏢 HC Portal     CMP     CDP     Panduan     Kelola Data   [👤]  │
└───────────────────────────────────────────────────────────────────┘
       │              │       │         │            │           │
       │              │       │         │            │           │
   Home/Index    CMP/Index  CDP/    Home/Guide   Admin/    User Menu
   (Dashboard)            Index                  Index     ├─ Profile
                                                          ├─ Settings
                                                          └─ Logout
```

### 3.2 Visibilitas Menu per Role

| Menu | Route | Semua User | Admin/HC Only |
|------|-------|:----------:|:-------------:|
| HC Portal (Home) | `/Home/Index` | ✅ | — |
| CMP | `/CMP/Index` | ✅ | — |
| CDP | `/CDP/Index` | ✅ | — |
| Panduan | `/Home/Guide` | ✅ | — |
| **Kelola Data** | `/Admin/Index` | ❌ | ✅ |
| Profile | `/Account/Profile` | ✅ | — |
| Settings | `/Account/Settings` | ✅ | — |

### 3.3 Halaman dalam Tiap Modul

**CMP (Competency Management Portal):**
- Hub CMP → KKJ Matrix (per Bagian) | Mapping | Assessment
- Assessment → Lobby → Start Exam → Summary → Submit → Results
- Training Records → Personal | Team View (Admin/HC)

**CDP (Career Development Portal):**
- Hub CDP → Plan IDP (Silabus + Guidance) | Dashboard | Coaching Proton
- Deliverable Detail → Upload Evidence → Approval Chain
- Export: Excel & PDF progress report

**Kelola Data (Admin Hub):**
- Section A: Manajemen Pekerja (CRUD, Import/Export Excel)
- Section B: KKJ Matrix, CPDP Files
- Section C: Assessment (Create, Monitor, Packages, Questions)
- Section D: Coach-Coachee Mapping
- Section E: Proton Data (Silabus, Guidance, Override)
- Section F: Audit Log

---

## 4. Assessment Online

### 4.1 Ringkasan

Assessment Online adalah fitur ujian berbasis web dengan timer, auto-save, dan grading otomatis. HC/Admin membuat sesi assessment untuk satu atau lebih pekerja, melampirkan paket soal, dan memantau progres secara real-time.

### 4.2 Flowchart Assessment Online

```
                    ┌──────────────────────┐
                    │    HC / Admin        │
                    │  Buat Assessment     │
                    │  (max 50 user/batch) │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │  Isi Form:           │
                    │  • Judul             │
                    │  • Kategori          │
                    │  • Jadwal & Durasi   │
                    │  • Pass Percentage   │
                    │  • Token (opsional)  │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │  Buat Paket Soal     │
                    │  (Import via Excel)  │
                    │  • Soal + 4 opsi     │
                    │  • Jawaban benar     │
                    └──────────┬───────────┘
                               │
            ╔══════════════════╧══════════════════╗
            ║        MONITORING (HC/Admin)         ║
            ║  • Status per worker                 ║
            ║  • Force Close / Reset               ║
            ║  • Export hasil                       ║
            ╚══════════════════╤══════════════════╝
                               │
        ═══════════════════════╪═══════════════════════
                               │
                    ┌──────────▼───────────┐
                    │    WORKER / COACHEE   │
                    │  Lobby Assessment     │
                    │  (daftar ujian)       │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │  Status = "Upcoming"? │──── Ya ──→ Tunggu jadwal
                    └──────────┬───────────┘
                          Tidak│ (sudah "Open")
                               │
                    ┌──────────▼───────────┐
                    │  Token Required?      │
                    └───┬──────────────┬───┘
                   Ya   │              │ Tidak
                        ▼              │
               ┌─────────────┐         │
               │ Input Token  │         │
               │ (validasi)   │         │
               └──────┬──────┘         │
                      │                 │
                      └────────┬────────┘
                               │
                    ┌──────────▼───────────┐
                    │    START EXAM         │
                    │  • Soal di-shuffle    │
                    │  • Opsi di-shuffle    │
                    │  • Timer mulai        │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │  MENGERJAKAN UJIAN    │
                    │                       │
                    │  Auto-save tiap klik  │──→ SaveAnswer (per soal)
                    │  Progress tiap 30s    │──→ UpdateSessionProgress
                    │  Status check 10s     │──→ CheckExamStatus
                    │  Resume jika putus    │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │    EXAM SUMMARY       │
                    │  Review jawaban       │
                    │  Lihat belum dijawab  │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │    SUBMIT EXAM        │
                    │  • Validasi timer     │
                    │  • Grading otomatis   │
                    │  • Hitung skor (%)    │
                    │  • Pass/Fail          │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │      HASIL            │
                    │  • Skor & Status      │
                    │  • Review jawaban     │
                    │    (jika diizinkan)   │
                    │  • Sertifikat PDF     │
                    │    (jika lulus)        │
                    └──────────────────────┘
```

### 4.3 Kategori Assessment

| Kategori | Tipe Ujian | Keterangan |
|----------|-----------|------------|
| Assessment OJ | Online (pilihan ganda) | On-the-Job assessment |
| IHT (In-House Training) | Online (pilihan ganda) | Training internal |
| Licencor | Online (pilihan ganda) | Sertifikasi licensor |
| OTS | Online (pilihan ganda) | Operator Training Simulator |
| Mandatory HSSE Training | Online (pilihan ganda) | Keselamatan wajib |
| Assessment Proton (Tahun 1-2) | Online (pilihan ganda) | Ujian program Proton |
| Assessment Proton (Tahun 3) | Interview (offline) | HC input hasil interview manual |

### 4.4 Fitur Exam Engine

- **Shuffle:** Soal dan opsi jawaban diacak per peserta (mencegah kecurangan)
- **Auto-save:** Jawaban tersimpan otomatis setiap klik radio button
- **Timer:** Countdown durasi ujian, grace period 2 menit
- **Resume:** Jika koneksi putus, peserta bisa lanjut dari halaman terakhir
- **Force Close:** HC bisa menutup ujian paksa kapan saja
- **Reset:** HC bisa reset ujian agar peserta mengulang dari awal

---

## 5. Training Offline (Manual)

### 5.1 Ringkasan

Training Offline adalah pencatatan manual untuk pelatihan yang dilakukan di luar sistem (tatap muka, kunjungan lapangan, sertifikasi eksternal). HC/Admin menginput data training per pekerja beserta informasi sertifikat.

### 5.2 Flowchart Training Offline

```
    ┌──────────────────────────┐
    │      HC / Admin          │
    │  Buka Worker Detail      │
    │  (Admin > ManageWorkers) │
    └────────────┬─────────────┘
                 │
    ┌────────────▼─────────────┐
    │    Tambah Training        │
    │                           │
    │  • Judul Training         │
    │  • Kategori:              │
    │    - PROTON               │
    │    - OJT                  │
    │    - MANDATORY            │
    │  • Tanggal Mulai/Selesai  │
    │  • Penyelenggara:         │
    │    - Internal             │
    │    - Licensor             │
    │    - NSO                  │
    │  • Kota pelaksanaan       │
    │  • Status:                │
    │    - Passed               │
    │    - Wait Certificate     │
    │    - Valid                 │
    │  • Nomor Sertifikat       │
    │  • Tipe Sertifikat:       │
    │    - Permanent            │
    │    - Annual               │
    │    - 3-Year               │
    │  • Valid Until            │
    │  • URL Sertifikat         │
    └────────────┬─────────────┘
                 │
    ┌────────────▼─────────────┐
    │   Simpan ke Database     │
    │   (TrainingRecord table) │
    └────────────┬─────────────┘
                 │
         ┌───────┴───────┐
         │               │
         ▼               ▼
  ┌──────────────┐  ┌──────────────────┐
  │ Tampil di    │  │  Expiry Tracking  │
  │ Records      │  │                   │
  │ (Personal &  │  │  IsExpiringSoon:  │
  │  Team View)  │  │  < 30 hari        │
  │              │  │                   │
  │              │  │  IsExpired:        │
  │              │  │  sudah lewat       │
  └──────────────┘  └──────────────────┘
```

### 5.3 Perbedaan Assessment Online vs Training Offline

| Aspek | Assessment Online | Training Offline |
|-------|:-:|:-:|
| **Pelaksanaan** | Di dalam portal (exam engine) | Di luar portal (tatap muka) |
| **Input data** | Otomatis (sistem) | Manual (HC/Admin) |
| **Grading** | Otomatis (skor %) | Manual (status: Passed/Valid) |
| **Sertifikat** | PDF otomatis (jika lulus) | URL sertifikat eksternal |
| **Expiry tracking** | ❌ Tidak ada | ✅ ValidUntil + alert |
| **RecordType** | "Assessment Online" | "Training Manual" |
| **Tampil di Records** | ✅ Ya (unified) | ✅ Ya (unified) |

---

## 6. Coaching Proton

### 6.1 Ringkasan

Coaching Proton adalah program pengembangan kompetensi operator dan panelman melalui pendampingan (coaching). Setiap coachee di-assign ke track tertentu (Panelman/Operator, Tahun 1-3) dan harus menyelesaikan deliverable yang dibuktikan dengan evidence file.

### 6.2 Struktur Data Proton

```
ProtonTrack (6 track)
├── Panelman - Tahun 1
├── Panelman - Tahun 2
├── Panelman - Tahun 3
├── Operator - Tahun 1
├── Operator - Tahun 2
└── Operator - Tahun 3
        │
        ▼
ProtonKompetensi (per Bagian/Unit/Track)
├── Nama Kompetensi (contoh: "Pengetahuan Teknis")
│   │
│   ▼
│   ProtonSubKompetensi
│   ├── Nama Sub-Kompetensi (contoh: "Dasar Prinsip Operasi")
│   │   │
│   │   ▼
│   │   ProtonDeliverable (leaf node)
│   │   ├── "Menjelaskan pressure drop"
│   │   ├── "Mengidentifikasi jenis katalis"
│   │   └── ...
│   │
│   └── Sub-Kompetensi lainnya...
│
└── Kompetensi lainnya...
```

### 6.3 Flowchart Coaching Proton

```
    ┌──────────────────────────────┐
    │         HC / Admin           │
    │                              │
    │  1. Assign Coach ↔ Coachee   │
    │     (CoachCoacheeMapping)    │
    │                              │
    │  2. Assign Proton Track      │
    │     (ProtonTrackAssignment)  │
    └──────────────┬───────────────┘
                   │
    ┌──────────────▼───────────────┐
    │         COACHEE              │
    │  Lihat Silabus & Guidance    │
    │  (CDP > Plan IDP)            │
    │  • Daftar deliverable        │
    │  • Download file guidance    │
    └──────────────┬───────────────┘
                   │
    ┌──────────────▼───────────────┐
    │          COACH               │
    │  Upload Evidence             │
    │  (PDF / JPG / PNG)           │
    │                              │
    │  Status: Pending → Submitted │
    └──────────────┬───────────────┘
                   │
    ╔══════════════╧══════════════════════════════════╗
    ║          3-LEVEL APPROVAL CHAIN                  ║
    ║                                                  ║
    ║  Level 1: Sr. Supervisor                         ║
    ║  ├── ✅ Approve → SrSpvApprovalStatus="Approved" ║
    ║  └── ❌ Reject  → Status="Rejected"              ║
    ║                                                  ║
    ║  Level 2: Section Head                           ║
    ║  ├── ✅ Approve → ShApprovalStatus="Approved"    ║
    ║  └── ❌ Reject  → Status="Rejected"              ║
    ║                                                  ║
    ║  (Level 1 & 2 bersifat independen,               ║
    ║   tidak harus berurutan)                         ║
    ║                                                  ║
    ║  Level 3: HC Review                              ║
    ║  └── ✅ Review → HCApprovalStatus="Reviewed"     ║
    ╚══════════════╤══════════════════════════════════╝
                   │
    ┌──────────────▼───────────────┐
    │  Semua Deliverable Approved? │
    └───────┬──────────────┬──────┘
       Ya   │              │ Belum
            ▼              ▼
    ┌───────────────┐   Lanjut
    │ Notifikasi    │   coaching...
    │ ke semua HC   │
    │               │
    │ "Coachee X    │
    │  telah selesai│
    │  semua        │
    │  deliverable" │
    └───────────────┘
```

### 6.4 Alur Reject & Perbaikan

```
    ┌─────────────────┐
    │ SrSpv/SH Reject │
    │ (+ alasan)      │
    └────────┬────────┘
             │
    ┌────────▼────────┐
    │ Status = Rejected│
    │ Evidence tetap   │
    └────────┬────────┘
             │
    ┌────────▼────────┐
    │  Coach upload    │
    │  evidence baru   │
    │  → Status =      │
    │    Submitted      │
    └────────┬────────┘
             │
             ▼
      (Kembali ke
       approval chain)
```

### 6.5 Scoping Data per Role (Coaching Proton)

| Role | Lihat Data | Filter |
|------|-----------|--------|
| Admin / HC | Semua coachee | Bagian + Unit |
| Direktur / VP / Manager | Semua coachee | Bagian + Unit |
| Section Head | Bagian sendiri | Unit (dalam bagian) |
| Sr. Supervisor | Bagian sendiri | Unit (dalam bagian) |
| Coach | Coachee mapping sendiri | Track |
| Coachee | Data personal saja | — |

---

## 7. History Assessment & Training

### 7.1 Unified Records View

Portal menggabungkan Assessment Online dan Training Offline dalam satu tampilan tabel terpadu menggunakan `UnifiedTrainingRecord`:

```
┌─────────────────────────────────────────────────────────────────┐
│                    CMP > Training Records                        │
├──────────┬─────────┬──────────┬───────┬────────┬────────────────┤
│ Tanggal  │  Tipe   │  Judul   │ Skor  │ Status │ Sertifikat     │
├──────────┼─────────┼──────────┼───────┼────────┼────────────────┤
│ 05 Mar   │🔵Online │ Assess.  │ 85%   │ Passed │ [Lihat Hasil]  │
│ 03 Mar   │🟢Manual │ HSSE     │  —    │ Valid  │ [Download]     │
│ 01 Mar   │🔵Online │ OTS Exam │ 60%   │ Failed │ [Lihat Hasil]  │
│ 28 Feb   │🟢Manual │ Licensor │  —    │ Passed │ [Download]     │
└──────────┴─────────┴──────────┴───────┴────────┴────────────────┘
```

**Dua mode tampilan:**
- **Personal** (`CMP/Records`) — Pekerja melihat riwayat sendiri
- **Team View** (`CMP/RecordsTeam`) — Admin/HC melihat semua pekerja

### 7.2 Assessment Attempt History

Setiap percobaan ujian dicatat dalam `AssessmentAttemptHistory`:

```
┌──────────────────────────────────────────────────────────────┐
│              Riwayat Percobaan Ujian                           │
├─────────┬──────────────┬───────┬────────┬────────┬───────────┤
│ Attempt │ Mulai        │ Skor  │ Status │ Selesai│ Keterangan│
├─────────┼──────────────┼───────┼────────┼────────┼───────────┤
│ #1      │ 01 Mar 09:00 │ 45%   │ Failed │ 09:45  │ Submitted │
│ #2      │ 02 Mar 10:00 │  —    │  —     │  —     │ Abandoned │
│ #3      │ 03 Mar 09:00 │ 85%   │ Passed │ 09:40  │ Submitted │
└─────────┴──────────────┴───────┴────────┴────────┴───────────┘
```

**Status Percobaan:**
- **Submitted** — Ujian diselesaikan dan di-submit
- **Abandoned** — Pekerja meninggalkan ujian (bisa di-reset oleh HC)
- **Force Closed** — HC menutup ujian paksa

### 7.3 Sertifikat & Expiry Tracking

```
    ┌─────────────────────────┐
    │   Training Record       │
    │   CertificateType:      │
    │   • Permanent (∞)       │
    │   • Annual (1 tahun)    │
    │   • 3-Year (3 tahun)    │
    └───────────┬─────────────┘
                │
    ┌───────────▼─────────────┐
    │      ValidUntil set?    │
    └────┬──────────────┬─────┘
    Ya   │              │ Tidak
         ▼              ▼
  ┌──────────────┐  ┌──────────┐
  │ Hitung sisa  │  │ Permanent│
  │ hari         │  │ (no exp) │
  └──────┬───────┘  └──────────┘
         │
    ┌────┴─────────────┐
    │                  │
    ▼                  ▼
 ≤30 hari          > 30 hari
 ┌──────────┐     ┌──────────┐
 │⚠️ Expiring│     │ ✅ Valid  │
 │  Soon     │     │          │
 └──────────┘     └──────────┘
    │
    ▼ (jika sudah lewat)
 ┌──────────┐
 │❌ Expired │
 └──────────┘
```

Dashboard menampilkan alert untuk sertifikat yang akan habis masa berlakunya.

---

## 8. Rekomendasi Improvement

### 8.1 Arsitektur & Code Quality

| # | Rekomendasi | Prioritas | Alasan |
|---|-------------|:---------:|--------|
| 1 | **Split AdminController** menjadi beberapa controller (AssessmentAdminController, WorkerAdminController, dll.) | 🔴 Tinggi | 5,828 LOC dalam 1 file sulit di-maintain. Bisa pakai partial class atau controller terpisah |
| 2 | **Tambah Service Layer** antara Controller dan DbContext | 🟡 Sedang | Business logic saat ini langsung di controller. Service layer meningkatkan testability dan reusability |
| 3 | **Tambah API Endpoints** (REST/JSON) | 🟡 Sedang | Saat ini semua MVC views. API memungkinkan integrasi mobile app atau SPA di masa depan |
| 4 | **Distributed Cache** (Redis/SQL Server cache) | 🟢 Rendah | Saat ini in-memory cache, cukup untuk single instance. Perlu jika scale ke multiple server |

### 8.2 Fitur & User Experience

| # | Rekomendasi | Prioritas | Alasan |
|---|-------------|:---------:|--------|
| 5 | **Dashboard Analytics** yang lebih detail | 🔴 Tinggi | Tambah grafik tren assessment per bagian, completion rate coaching, dan benchmark antar unit |
| 6 | **Bulk Assessment Results Export** ke Excel per bagian/unit | 🟡 Sedang | Memudahkan reporting ke manajemen tanpa harus export satu-satu |
| 7 | **Email/WhatsApp Notification** untuk assessment dan coaching | 🟡 Sedang | Saat ini hanya in-app notification. Integrasi email/WA meningkatkan responsiveness |
| 8 | **Assessment Bank Soal** terpusat | 🟡 Sedang | Saat ini soal per paket. Bank soal memungkinkan reuse dan randomisasi lintas assessment |
| 9 | **Training Calendar View** | 🟢 Rendah | Tampilan kalender untuk jadwal training upcoming dan expiring certificates |
| 10 | **Coaching Progress Report** otomatis per periode | 🟢 Rendah | Laporan bulanan/triwulan progress coaching per bagian yang auto-generate |

### 8.3 Keamanan & Infrastruktur

| # | Rekomendasi | Prioritas | Alasan |
|---|-------------|:---------:|--------|
| 11 | **Rate Limiting** pada login dan exam endpoints | 🔴 Tinggi | Mencegah brute force password dan exam abuse |
| 12 | **File Upload Virus Scanning** | 🟡 Sedang | Evidence dan guidance file upload sebaiknya di-scan sebelum disimpan |
| 13 | **Database Backup Automation** | 🔴 Tinggi | Automated daily backup SQL Server dengan retention policy |
| 14 | **Logging ke External Service** (Seq, ELK, Application Insights) | 🟢 Rendah | Centralized logging untuk monitoring dan troubleshooting di production |

### 8.4 Improvement Khusus per Modul

**Assessment Online:**
- Tambah tipe soal selain pilihan ganda (essay, matching, true/false)
- Tambah gambar/media pada soal
- Anti-cheat: deteksi tab switch, fullscreen mode
- Statistik soal: tingkat kesulitan, daya beda

**Training Offline:**
- Upload sertifikat langsung (file PDF) selain URL
- Auto-reminder email sebelum sertifikat expired
- Integrasi dengan sistem HR untuk validasi data training

**Coaching Proton:**
- Progress bar visual per coachee di dashboard coach
- Coaching session notes/log terintegrasi
- Target timeline per deliverable (bukan hanya status)
- Perbandingan progress antar coachee dalam satu track

---

## Lampiran

### A. Database Statistics

| Metrik | Jumlah |
|--------|-------:|
| Total DbSets | 73 |
| Total Migrations | 44+ |
| Total Controllers | 6 |
| Total Controller Methods | ~192 |
| Total Views | 54+ |
| Total Models | 40+ |
| Total Lines of Code (Controllers) | ~11,300 |

### B. File Reference

| File | Keterangan |
|------|-----------|
| `Controllers/AdminController.cs` | Admin hub — 91 methods |
| `Controllers/CMPController.cs` | Assessment & exam engine — 33 methods |
| `Controllers/CDPController.cs` | Coaching & deliverable — 28 methods |
| `Controllers/ProtonDataController.cs` | Silabus & guidance — 25 methods |
| `Models/OrganizationStructure.cs` | Definisi 4 bagian & 19 unit |
| `Models/ApplicationUser.cs` | User model (6 role levels) |
| `Models/ProtonModels.cs` | Proton track, kompetensi, deliverable |
| `Data/ApplicationDbContext.cs` | 73 DbSets, FK, indexes |
| `Program.cs` | DI, auth config, middleware pipeline |
| `Views/Shared/_Layout.cshtml` | Navbar & role-based menu |

---

*Report ini di-generate berdasarkan analisa codebase Portal HC KPB per 6 Maret 2026.*
