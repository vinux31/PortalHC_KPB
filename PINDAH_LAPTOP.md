# Panduan Lengkap Memindahkan Project PortalHC_KPB ke Laptop Baru

> **Terakhir diupdate**: 25 Februari 2026
> **Project**: PortalHC_KPB — Portal Human Capital KPB
> **GitHub**: https://github.com/vinux31/PortalHC_KPB

---

## 📋 Analisis Tech Stack

| Komponen | Detail |
|---|---|
| **Framework** | ASP.NET Core 8.0 MVC (C#) |
| **Database** | SQL Server Express (via Entity Framework Core) |
| **Auth** | ASP.NET Core Identity |
| **NuGet Packages** | ClosedXML (Excel), EF Core SqlServer, EF Core Sqlite, EF Core Tools/Design |
| **Version Control** | Git → GitHub (`vinux31/PortalHC_KPB`) |
| **AI Assistant** | Claude Code (via terminal, butuh Node.js + npm) |
| **IDE** | Visual Studio Code (dengan Gemini Code Assist) |

---

## 🔧 Software yang Harus Diinstall (Urutan Install)

### 1. Git (WAJIB — Install Pertama)
- **Download**: https://git-scm.com/downloads/win
- Verifikasi: `git --version`
- **Konfigurasi setelah install**:
  ```bash
  git config --global user.name "Nama Kamu"
  git config --global user.email "email@kamu.com"
  ```

### 2. .NET 8 SDK (WAJIB)
- **Download**: https://dotnet.microsoft.com/download/dotnet/8.0
- Pilih: **`.NET 8.0 SDK`** (bukan Runtime saja)
- Verifikasi: `dotnet --version` → harus tampil `8.x.x`

### 3. SQL Server Express (WAJIB)
- **Download**: https://www.microsoft.com/en-us/sql-server/sql-server-downloads → pilih **Express**
- ⚠️ **PENTING**: Saat install, pastikan instance name = **`SQLEXPRESS`**
- Connection string project: `Server=localhost\SQLEXPRESS;Database=HcPortalDB_Dev`

#### Setelah Install SQL Server — Aktifkan TCP/IP:
1. Buka **SQL Server Configuration Manager**
2. Navigasi ke: `SQL Server Network Configuration` → `Protocols for SQLEXPRESS`
3. Klik kanan **TCP/IP** → **Enable**
4. Restart SQL Server service

> **Catatan**: SQL Server Express juga bisa terinstall otomatis saat install Visual Studio 2022 (lihat poin 4).

### 4. IDE — Pilih Salah Satu

#### Opsi A: Visual Studio Code (Lebih Ringan) ✅ *Rekomendasi — ini yang dipakai sekarang*
- **Download**: https://code.visualstudio.com/
- **Extensions yang diperlukan**:
  - **C# Dev Kit** (Microsoft) — untuk IntelliSense C#
  - **Gemini Code Assist** (Google) — AI assistant di editor
- Ukuran: ~300 MB

#### Opsi B: Visual Studio 2022 Community (Lebih Lengkap)
- **Download**: https://visualstudio.microsoft.com/
- Saat install, centang workload: **"ASP.NET and web development"**
- SQL Server Express bisa ikut terinstall dari sini
- Ukuran: ~5-10 GB

### 5. Node.js (WAJIB — untuk Claude Code)
- **Download**: https://nodejs.org/ → pilih **LTS version**
- Verifikasi:
  ```bash
  node --version   # harus tampil v20+ atau v22+
  npm --version    # harus tampil 10+
  ```

#### ⚠️ Fix PowerShell Execution Policy (Windows):
Jika `npm` error "running scripts is disabled", jalankan PowerShell **sebagai Administrator**:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```
Kemudian restart terminal.

### 6. Claude Code (untuk AI di Terminal)
- **Install via npm** (setelah Node.js terinstall):
  ```bash
  npm install -g @anthropic-ai/claude-code
  ```
- **Setup API Key**: Kamu perlu Anthropic API key
  1. Buka https://console.anthropic.com/
  2. Buat API key baru
  3. Saat pertama kali jalankan `claude` di terminal, masukkan API key tersebut
- Verifikasi: `claude --version`

---

## 📦 Cara Memindahkan Project

### ✅ Cara Terbaik: Via Git/GitHub (Rekomendasi)

Project sudah ada di GitHub, jadi cara paling rapi:

```bash
# Di laptop baru — clone dari GitHub
git clone https://github.com/vinux31/PortalHC_KPB.git

# Masuk ke folder project
cd PortalHC_KPB
```

#### ⚠️ File/Folder yang TIDAK Ada di GitHub (perlu copy manual):

| File/Folder | Alasan | Cara Copy |
|---|---|---|
| `.claude/` | Di-gitignore (settings Claude Code lokal) | Copy via USB/cloud |
| `.planning/` | GSD roadmap & phases (development planning) | Copy via USB/cloud |
| `HcPortal.db` | Database SQLite lokal (jika ada) | Opsional — bisa regenerate |
| `.vs/` | Cache Visual Studio | Tidak perlu — auto-generate |

**.claude/settings.local.json** berisi permission rules untuk Claude Code (izin git, dotnet build, dll). **Copy file ini agar tidak perlu setup ulang permissions.**

### Cara Alternatif: Via USB/Flash Drive

Copy folder `PortalHC_KPB` ke laptop baru. **Hapus folder-folder ini dulu** untuk hemat ukuran:

```
❌ HAPUS (auto-generate):     ✅ COPY (penting):
├── bin\                      ├── Controllers\
├── obj\                      ├── Data\
├── .vs\                      ├── Helpers\
                              ├── Migrations\ (PENTING!)
                              ├── Models\
                              ├── Views\
                              ├── wwwroot\
                              ├── Services\
                              ├── .claude\ (settings AI)
                              ├── .planning\ (roadmap GSD)
                              ├── .gitignore
                              ├── appsettings.*.json
                              ├── HcPortal.csproj
                              ├── HcPortal.sln
                              └── Program.cs
```

---

## 🚀 Langkah Setup di Laptop Baru

Setelah semua software terinstall dan project sudah di-clone/copy:

### Step 1: Restore NuGet Packages
```bash
cd C:\Users\NamaUser\Desktop\PortalHC_KPB
dotnet restore
```
Ini akan download semua NuGet packages dari internet:
- ClosedXML 0.105.0
- Microsoft.AspNetCore.Identity.EntityFrameworkCore 8.0.0
- Microsoft.EntityFrameworkCore.SqlServer 8.0.0
- Microsoft.EntityFrameworkCore.Sqlite 8.0.0
- Microsoft.EntityFrameworkCore.Tools 8.0.0
- Microsoft.EntityFrameworkCore.Design 8.0.0

### Step 2: Build Project
```bash
dotnet build
```
Pastikan tidak ada error. Jika ada error, periksa apakah .NET 8 SDK sudah terinstall.

### Step 3: Jalankan Aplikasi
```bash
dotnet run
```

**Database akan dibuat otomatis** — saat pertama kali `dotnet run`, aplikasi akan:
1. ✅ Membuat database `HcPortalDB_Dev` di SQL Server Express
2. ✅ Menjalankan semua migrations (51 migration files)
3. ✅ Seed data: Roles & Users (`SeedData`)
4. ✅ Seed data: KKJ Matrix (`SeedMasterData`)
5. ✅ Seed data: CPDP Items (`SeedMasterData`)
6. ✅ Seed data: Training Records (`SeedMasterData`)
7. ✅ Seed data: Competency Mappings (`SeedCompetencyMappings`)
8. ✅ Seed data: Proton/IDP Data (`SeedProtonData`)

### Step 4: Akses Website
- Buka browser: `http://localhost:5000` (atau port yang ditampilkan di terminal)
- Login dengan akun seed data yang sudah di-setup

### Step 5: Setup Claude Code (Opsional)
```bash
# Install Claude Code
npm install -g @anthropic-ai/claude-code

# Pastikan folder .claude sudah ada di project (copy dari laptop lama)
# Jalankan Claude di folder project
cd C:\Users\NamaUser\Desktop\PortalHC_KPB
claude
```

---

## 🔍 Troubleshooting

### ❌ Error: "Cannot connect to SQL Server"
1. Pastikan SQL Server Express sudah running:
   - Buka **Services** (ketik `services.msc` di Start Menu)
   - Cari **SQL Server (SQLEXPRESS)** → pastikan status **Running**
2. Pastikan instance name benar (`SQLEXPRESS`)
3. Pastikan TCP/IP sudah di-enable (lihat langkah di atas)

### ❌ Error: "npm is not recognized" atau "scripts disabled"
```powershell
# Jalankan di PowerShell sebagai Administrator:
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### ❌ Error: "dotnet is not recognized"
- Restart terminal setelah install .NET SDK
- Atau install ulang .NET 8 SDK

### ❌ Error saat dotnet build
```bash
# Coba clean & restore ulang
dotnet clean
dotnet restore
dotnet build
```

### ❌ Database migration error
```bash
# Jika perlu reset database (HAPUS data lama):
dotnet ef database drop --force
dotnet run   # akan recreate & seed ulang
```

---

## ✅ Checklist Verifikasi Setup Berhasil

- [ ] `git --version` → menampilkan versi
- [ ] `dotnet --version` → menampilkan `8.x.x`
- [ ] `node --version` → menampilkan `v20+` atau `v22+`
- [ ] `npm --version` → menampilkan `10+`
- [ ] SQL Server Express running (`services.msc`)
- [ ] `git clone` atau copy project berhasil
- [ ] `dotnet restore` sukses tanpa error
- [ ] `dotnet build` sukses tanpa error
- [ ] `dotnet run` sukses — website bisa dibuka di browser
- [ ] Login berhasil dengan akun seed data
- [ ] `claude --version` berfungsi (jika pakai Claude Code)
- [ ] Folder `.claude/` sudah di-copy (permissions Claude Code)
- [ ] Folder `.planning/` sudah di-copy (GSD roadmap)

---

## 📊 Ringkasan Kebutuhan Software

| Software | Keterangan | Ukuran ± | Wajib? |
|---|---|---|---|
| Git | Version control | ~50 MB | ✅ Wajib |
| .NET 8 SDK | Runtime + build tools | ~300 MB | ✅ Wajib |
| SQL Server Express | Database engine | ~300 MB | ✅ Wajib |
| VS Code | IDE ringan + extensions | ~300 MB | ✅ Wajib (pilih 1) |
| VS 2022 Community | IDE lengkap | ~5-10 GB | Alternatif |
| Node.js LTS | Runtime untuk npm/Claude | ~100 MB | Untuk Claude |
| Claude Code | AI assistant di terminal | ~50 MB | Opsional |

> **Total minimum**: ~1 GB (Git + .NET 8 + SQL Express + VS Code)
> **Dengan Claude Code**: ~1.1 GB tambahan Node.js + Claude
