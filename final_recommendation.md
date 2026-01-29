# 🎯 HC Portal - Rekomendasi Final Deployment

> **Berdasarkan jawaban Anda, berikut rekomendasi spesifik untuk deployment HC Portal.**

---

## 📋 Ringkasan Kebutuhan Anda

| Parameter | Nilai |
|-----------|-------|
| **Jumlah User** | 400-600 pekerja |
| **Akses** | Perlu dari luar jaringan (Internet) |
| **Authentication** | SSO via @pertamina.com (M365 E3) |
| **Server** | Belum diputuskan (internal/external) |
| **Lisensi Windows/SQL** | Belum diketahui |

---

## ⭐ REKOMENDASI: Azure Cloud (Opsi A)

Berdasarkan kebutuhan Anda, **Azure Cloud adalah pilihan terbaik** karena:

| Alasan | Penjelasan |
|--------|------------|
| ✅ **Akses Internet** | Azure langsung accessible dari mana saja |
| ✅ **M365 Integration** | Seamless SSO dengan Entra ID |
| ✅ **No Licensing Hassle** | Tidak perlu cek lisensi Windows/SQL |
| ✅ **Scalable** | Mudah scale up jika user bertambah |
| ✅ **Managed Service** | Less IT overhead |

---

## 💰 Estimasi Biaya untuk 400-600 Users

### Tier: Production-Ready (Recommended)

| Komponen | Azure Service | Spesifikasi | Biaya/Bulan |
|----------|---------------|-------------|-------------|
| **Web App** | App Service P1v3 | 2 vCPU, 8GB RAM | $145 |
| **Database** | Azure SQL S2 | 50 DTU, 250GB | $75 |
| **Storage** | Blob Storage | 100GB | $10 |
| **Backup** | Geo-redundant | Daily backup | $15 |
| **Monitoring** | Application Insights | Basic | $0 |
| **SSL** | Managed Certificate | Auto-renew | $0 |
| | | **TOTAL/BULAN** | **~$245** |
| | | **TOTAL/TAHUN** | **~$2,940** |
| | | **IDR/BULAN*** | **~Rp 3.9 juta** |
| | | **IDR/TAHUN*** | **~Rp 47 juta** |

*Kurs: 1 USD = Rp 16.000

### Tier Alternatif (Budget-Friendly)

| Tier | Web + DB | Biaya/Bulan | Cocok Untuk |
|------|----------|-------------|-------------|
| **Basic** | B2 + S1 | ~$100 (Rp 1.6 juta) | Testing, <100 users aktif |
| **Standard** | P1v3 + S2 | ~$245 (Rp 3.9 juta) | Production 400-600 users |
| **Premium** | P2v3 + S4 | ~$450 (Rp 7.2 juta) | High traffic, 1000+ users |

---

## 🔐 Setup SSO dengan @pertamina.com

### Langkah 1: Register App di Azure Portal
```
1. Login ke https://portal.azure.com dengan akun admin IT
2. Azure Active Directory → App registrations → New
3. Name: "HC Portal KPB"
4. Redirect URI: https://hcportal.azurewebsites.net/signin-oidc
5. Catat: Client ID & Tenant ID
```

### Langkah 2: Konfigurasi HC Portal
```json
// appsettings.json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "pertamina.com",
    "TenantId": "[PERTAMINA_TENANT_ID]",
    "ClientId": "[APP_CLIENT_ID]",
    "CallbackPath": "/signin-oidc"
  }
}
```

### Langkah 3: User Experience
```
User buka HC Portal → Redirect ke login Microsoft → 
Login dengan akun @pertamina.com → Otomatis masuk ke HC Portal
```

> [!TIP]
> User tidak perlu membuat akun baru atau mengingat password terpisah!

---

## 📅 Timeline Implementasi (Revisi)

| Phase | Durasi | Fokus |
|-------|--------|-------|
| **Phase 1** | Week 1-2 | Azure Setup + Database + SSO |
| **Phase 2** | Week 3-4 | CRUD Operations + File Upload |
| **Phase 3** | Week 5 | Approval Workflow |
| **Phase 4** | Week 6 | Testing + UAT + Go-Live |

**Total: ~6 minggu untuk Production-Ready**

---

## ❓ Pertanyaan Lanjutan (untuk IT Pertamina)

| No | Pertanyaan | Tujuan |
|----|------------|--------|
| 1 | Siapa **admin Azure/M365** yang bisa register app? | Setup SSO |
| 2 | Apa **Tenant ID** Microsoft tenant Pertamina? | Konfigurasi SSO |
| 3 | Apakah ada **Azure subscription** Pertamina yang bisa dipakai? | Billing |
| 4 | Apakah perlu **approval formal** untuk pakai Azure? | Procurement |

---

## 🔄 Jika Tetap Mau Internal Server

Jika keputusan akhir adalah **server internal**, berikut yang perlu disiapkan:

| Item | Requirement |
|------|-------------|
| Server | Min 8 vCPU, 16GB RAM, 500GB SSD |
| OS | Windows Server 2022 |
| Database | SQL Server 2022 Standard |
| Network | Port 443 open, SSL certificate |
| Security | VPN atau reverse proxy untuk akses luar |

> [!WARNING]
> Akses dari luar jaringan via internal server membutuhkan konfigurasi VPN atau DMZ yang lebih kompleks.

---

## ✅ Next Steps

1. **Diskusi dengan IT Pertamina** tentang Azure subscription
2. **Dapatkan Tenant ID** dari admin M365
3. **Tentukan budget** yang disetujui
4. **Mulai Phase 1** setelah keputusan final

---

*Dokumen dibuat: 29 Januari 2026*
