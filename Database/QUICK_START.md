# 📋 Quick Start - SQL Server Setup

## 🚀 Langkah Cepat (5 Menit)

### 1️⃣ Buka SSMS dan Connect
- Server Name: `localhost\SQLEXPRESS`
- Authentication: Windows Authentication
- Klik **Connect**

### 2️⃣ Jalankan Script Setup
1. Klik **New Query** (Ctrl+N)
2. Buka file: [`01_CreateDatabase.sql`](file:///c:/Users/rinoa/Desktop/PortalHC_KPB/Database/01_CreateDatabase.sql)
3. Copy semua isi → Paste ke SSMS
4. Klik **Execute** (F5)

### 3️⃣ Verifikasi
Di Object Explorer (panel kiri):
- ✅ Databases → **HcPortalDB_Dev** (harus ada)
- ✅ Security → Logins → **hcportal_dev** (harus ada)

### 4️⃣ Test dari Aplikasi
```powershell
cd c:\Users\rinoa\Desktop\PortalHC_KPB
dotnet build
dotnet run
```

## ✅ Sukses Jika:
- Build tanpa error
- Website jalan di http://localhost:xxxx
- Di SSMS, database `HcPortalDB_Dev` ada tabel-tabel baru

---

**Setelah berhasil, kabari saya untuk lanjut ke Phase 2!** 🎯
