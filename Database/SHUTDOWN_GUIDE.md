# 🛑 Cara Stop Semua Services - End of Day

## 1️⃣ Stop Aplikasi HC Portal

### Di Terminal/PowerShell:
Tekan **Ctrl+C** di terminal yang menjalankan `dotnet run`

### Atau via Task Manager:
1. Tekan **Ctrl+Shift+Esc** (buka Task Manager)
2. Cari proses **HcPortal.exe**
3. Klik kanan → **End Task**

---

## 2️⃣ Stop SQL Server Management Studio (SSMS)

1. **Tutup semua query windows** yang terbuka
2. **Close SSMS** (File → Exit atau klik X)

> **Note:** SSMS hanya aplikasi client, menutupnya tidak akan stop SQL Server service.

---

## 3️⃣ Stop SQL Server Service (OPSIONAL)

> ⚠️ **Catatan:** SQL Server service bisa dibiarkan running. Tidak akan menggunakan banyak resource jika tidak ada koneksi aktif.

### Jika Ingin Stop SQL Server:

**Cara 1: Via Services**
1. Tekan **Win+R** → ketik `services.msc` → Enter
2. Cari **SQL Server (SQLEXPRESS)**
3. Klik kanan → **Stop**

**Cara 2: Via PowerShell (Admin)**
```powershell
Stop-Service -Name "MSSQL$SQLEXPRESS"
```

---

## 4️⃣ Checklist Sebelum Shutdown

- [ ] Terminal `dotnet run` sudah di-stop (Ctrl+C)
- [ ] SSMS sudah ditutup
- [ ] Browser tabs (localhost:5277) sudah ditutup
- [ ] (Optional) SQL Server service di-stop

---

## 🔄 Cara Start Lagi Besok

### 1. Start SQL Server (jika di-stop)
**Via Services:**
1. Win+R → `services.msc`
2. Cari **SQL Server (SQLEXPRESS)**
3. Klik kanan → **Start**

**Via PowerShell:**
```powershell
Start-Service -Name "MSSQL$SQLEXPRESS"
```

### 2. Jalankan Aplikasi
```powershell
cd c:\Users\rinoa\Desktop\PortalHC_KPB
dotnet run
```

### 3. Buka Browser
http://localhost:5277

---

## 💡 Tips

**SQL Server bisa dibiarkan running:**
- Tidak akan menggunakan banyak resource saat idle
- Lebih cepat saat start aplikasi besok
- Otomatis start saat Windows restart (jika diset Automatic)

**Jika mau hemat resource:**
- Stop SQL Server service saat tidak digunakan
- Start lagi saat mau development

---

## 📊 Status Hari Ini

✅ **Yang Sudah Selesai:**
- SQL Server Express installed
- Database `HcPortalDB_Dev` created
- Connection string configured (Windows Authentication)
- Migrations applied
- Seed data (9 users) created
- Authorization fixed (login redirect works)
- Website running successfully

✅ **Ready untuk besok:**
- Lanjut Phase 2: CRUD Operations
- Implementasi service layer
- Migrasi mock data ke database

---

*Selamat istirahat! 😊*
