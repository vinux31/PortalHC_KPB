# 🗄️ Setup SQL Server Lokal - Step by Step Guide

## 📋 Informasi SQL Server Anda

Dari screenshot yang Anda berikan:
- **Instance Name:** `SQLEXPRESS`
- **Connection String:** `Server=localhost\SQLEXPRESS;Database=master;Trusted_Connection=true`
- **SQL Administrator:** `VINUX\rinoa`
- **Version:** SQL Server 2022 Express (16.0.1000.6 RTM)

---

## 🎯 Langkah-Langkah Setup

### Step 1: Buka SQL Server Management Studio (SSMS)

1. Buka SSMS dari Start Menu
2. Di dialog "Connect to Server":
   - **Server Name:** `localhost\SQLEXPRESS` (atau hanya `.` atau `(local)`)
   - **Authentication:** Windows Authentication
   - **User Name:** VINUX\rinoa (otomatis terisi)
3. Klik **Connect**

---

### Step 2: Jalankan Script Setup Database

1. Di SSMS, klik **New Query** (atau tekan Ctrl+N)
2. Buka file script yang sudah saya buat: [`Database/01_CreateDatabase.sql`](file:///c:/Users/rinoa/Desktop/PortalHC_KPB/Database/01_CreateDatabase.sql)
3. Copy semua isi file tersebut
4. Paste ke Query window di SSMS
5. Klik **Execute** (atau tekan F5)

**Expected Output:**
```
✅ Database HcPortalDB_Dev berhasil dibuat
✅ Login hcportal_dev berhasil dibuat
✅ User hcportal_dev berhasil dibuat dengan permission db_owner
✅ Setup Database Selesai!
```

---

### Step 3: Verifikasi Database Sudah Dibuat

Di SSMS, di panel **Object Explorer** (kiri):
1. Expand **Databases**
2. Anda akan melihat **HcPortalDB_Dev** (database baru)
3. Expand **Security** → **Logins**
4. Anda akan melihat **hcportal_dev** (login baru)

---

### Step 4: Test Connection dari Aplikasi

Kembali ke terminal/PowerShell di folder project:

```powershell
# Test build
dotnet build

# Jalankan aplikasi
dotnet run
```

**Expected Result:**
- Aplikasi akan otomatis membuat tabel-tabel di database
- Seed data akan dijalankan
- Website jalan di http://localhost:5xxx

---

## 🔍 Troubleshooting

### Problem: "Cannot open database HcPortalDB_Dev"
**Solution:** Pastikan script Step 2 sudah dijalankan dengan sukses

### Problem: "Login failed for user 'hcportal_dev'"
**Solution:** 
1. Buka SSMS
2. Expand **Security** → **Logins**
3. Klik kanan **hcportal_dev** → **Properties**
4. Pastikan password: `Dev123456!`

### Problem: "A network-related error occurred"
**Solution:** 
1. Pastikan SQL Server service running
2. Buka **SQL Server Configuration Manager**
3. Pastikan **SQL Server (SQLEXPRESS)** status = Running

---

## ✅ Checklist Setup

- [ ] SSMS terbuka dan terkoneksi
- [ ] Script `01_CreateDatabase.sql` dijalankan
- [ ] Database `HcPortalDB_Dev` terlihat di Object Explorer
- [ ] Login `hcportal_dev` terlihat di Security
- [ ] `appsettings.Development.json` sudah diupdate
- [ ] `dotnet build` berhasil
- [ ] `dotnet run` berhasil dan website jalan

---

## 📝 File yang Sudah Dibuat/Diupdate

| File | Status | Keterangan |
|------|--------|------------|
| `Database/01_CreateDatabase.sql` | ✨ New | Script setup database |
| `appsettings.Development.json` | ✏️ Updated | Connection string SQL Server |

---

## 🔜 Next Step Setelah Setup Berhasil

Setelah semua checklist di atas ✅, kita akan:
1. **Jalankan EF Core Migrations** untuk membuat tabel-tabel
2. **Seed data awal** untuk testing
3. **Mulai Phase 2:** Implementasi CRUD Operations

---

*Silakan jalankan Step 1-4 di atas, lalu kabari saya hasilnya!*
