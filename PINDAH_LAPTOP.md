# Panduan Memindahkan Project PortalHC_KPB ke Laptop Baru

## Analisis Tech Stack

**Framework**: ASP.NET Core 8.0 MVC (C#)
**Database**: SQL Server Express (via Entity Framework Core)
**IDE**: Visual Studio 2022
**Version Control**: Git

---

## Software yang Harus Diinstall di Laptop Baru

### 1. .NET 8 SDK (WAJIB)
- Download di: https://dotnet.microsoft.com/download/dotnet/8.0
- Pilih: `.NET 8.0 SDK` (bukan Runtime saja)
- Verifikasi setelah install: `dotnet --version` → harus tampil `8.x.x`

### 2. SQL Server Express (WAJIB)
- Project menggunakan SQL Server Express (`Server=localhost\SQLEXPRESS`)
- Download **SQL Server 2022 Express** (gratis) di: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
- Pilih edisi **Express**
- Pastikan instance name saat install adalah `SQLEXPRESS`

### 3. Visual Studio 2022 (atau VS Code)
- **Visual Studio 2022 Community** (gratis): https://visualstudio.microsoft.com/
  - Saat install, centang workload: **"ASP.NET and web development"**
  - SQL Server Express bisa ikut terinstall di sini (tidak perlu install terpisah)
- **Atau VS Code** + ekstensi C# Dev Kit (lebih ringan)

### 4. Git (WAJIB)
- Download: https://git-scm.com/

### 5. Claude Code (Opsional - untuk lanjutkan pakai AI)
- Install via npm: `npm install -g @anthropic-ai/claude-code`

---

## Cara Memindahkan Project

### Cara A: Via USB/Flash Drive (Copy Folder)

Copy folder `PortalHC_KPB` ke laptop baru, **kecuali folder-folder ini** (hapus dulu untuk hemat ukuran):

```
PortalHC_KPB\
  ├── ❌ bin\          ← HAPUS (hasil build, bisa regenerate)
  ├── ❌ obj\          ← HAPUS (hasil build, bisa regenerate)
  ├── ❌ .vs\          ← HAPUS (cache Visual Studio)
  ├── ✅ Controllers\  ← COPY
  ├── ✅ Data\         ← COPY
  ├── ✅ Helpers\      ← COPY
  ├── ✅ Migrations\   ← COPY (penting!)
  ├── ✅ Models\       ← COPY
  ├── ✅ Views\        ← COPY
  ├── ✅ wwwroot\      ← COPY (termasuk PDF dokumen)
  ├── ✅ .claude\      ← COPY (settings Claude Code)
  ├── ✅ .planning\    ← COPY (roadmap GSD)
  ├── ✅ .gitignore    ← COPY
  ├── ✅ appsettings.json            ← COPY
  ├── ✅ appsettings.Development.json ← COPY
  ├── ✅ appsettings.Production.json  ← COPY
  ├── ✅ HcPortal.csproj             ← COPY
  └── ✅ Program.cs                  ← COPY
```

### Cara B: Via Git/GitHub (Lebih Rapi)

```bash
# Di laptop lama — push ke GitHub
git remote add origin https://github.com/username/PortalHC_KPB.git
git push -u origin main

# Di laptop baru — clone dari GitHub
git clone https://github.com/username/PortalHC_KPB.git
```

---

## Langkah Setup di Laptop Baru

Setelah semua software terinstall dan folder sudah dicopy:

```bash
# 1. Masuk ke folder project
cd C:\Users\NamaUser\Desktop\PortalHC_KPB

# 2. Restore NuGet packages (auto download dari internet)
dotnet restore

# 3. Jalankan aplikasi
dotnet run
```

**Database akan dibuat otomatis** — saat pertama kali `dotnet run`, aplikasi akan:
1. Membuat database `HcPortalDB_Dev` di SQL Server Express
2. Menjalankan semua migrations
3. Mengisi data awal (seed data) secara otomatis

---

## Ringkasan Kebutuhan Software

| Software | Keterangan | Ukuran Perkiraan |
|---|---|---|
| .NET 8 SDK | Runtime + build tools | ~300 MB |
| SQL Server Express | Database engine | ~300 MB |
| Visual Studio 2022 | IDE lengkap | ~5-10 GB |
| Git | Version control | ~50 MB |

> **Tips**: SQL Server Express bisa diinstall bersama Visual Studio 2022.
> Saat install VS 2022, centang workload **"ASP.NET and web development"**
> dan SQL Server Express akan ikut otomatis — tidak perlu install terpisah.
