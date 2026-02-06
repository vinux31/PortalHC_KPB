# 🔧 Fix SSL Certificate Error

## ❌ Error yang Muncul:
```
Encryption was enabled on this connection, review your SSL and certificate configuration
The certificate chain was issued by an authority that is not trusted
```

## ✅ Solusi: Trust Server Certificate

### Cara 1: Via SSMS Connection Dialog (RECOMMENDED)

1. **Buka SSMS**
2. **Di dialog "Connect to Server":**
   - Server Name: `localhost\SQLEXPRESS` (atau hanya `.`)
   - Authentication: **Windows Authentication**
3. **Klik tombol "Options >>"** (di bawah)
4. **Pilih tab "Connection Properties"**
5. **Centang ✅ "Trust server certificate"**
6. **Klik "Connect"**

### Cara 2: Via Connection String Tab

1. **Buka SSMS**
2. **Di dialog "Connect to Server":**
3. **Klik tab "Connection String"** (di atas)
4. **Paste connection string ini:**
   ```
   Server=localhost\SQLEXPRESS;Integrated Security=true;TrustServerCertificate=true
   ```
5. **Klik "Connect"**

### Cara 3: Disable Encryption (Untuk Development Lokal)

1. **Buka SQL Server Configuration Manager**
2. **Expand "SQL Server Network Configuration"**
3. **Klik "Protocols for SQLEXPRESS"**
4. **Klik kanan "TCP/IP" → Properties**
5. **Tab "Flags"**
6. **Set "Force Encryption" = No**
7. **Restart SQL Server service**

---

## 🎯 Setelah Berhasil Connect:

1. **Jalankan script** `01_CreateDatabase.sql`
2. **Lanjutkan ke step berikutnya**

---

## 💡 Kenapa Error Ini Muncul?

SQL Server 2022 secara default **mengaktifkan encryption** untuk semua koneksi. Untuk development lokal, kita bisa safely trust certificate dengan menambahkan `TrustServerCertificate=true`.

**Ini aman untuk development lokal!** ✅

Untuk production nanti, Tim Server Admin akan setup proper SSL certificate.
