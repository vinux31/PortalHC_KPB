# 🤝 Strategi Kolaborasi 2 Developer — PortalHC_KPB

## Pembagian Peran

```
┌──────────────────────────────────────┐  ┌──────────────────────────────────────┐
│    DEV 1 — CODER + AI ASSISTANT      │  │    DEV 2 — QA / TESTER               │
│──────────────────────────────────────│  │──────────────────────────────────────│
│ 🔧 Coding fitur baru                │  │ 🔍 Testing tampilan & fitur          │
│ 🤖 Pakai AI (Gemini Code Assist)    │  │ 👤 Cek tampilan per role             │
│ 🐛 Fix bug yang dilaporkan Dev 2    │  │ 🐛 Temukan & laporkan bug            │
│ 📝 Buat migration database          │  │ ✅ Verifikasi fix sudah benar        │
│ 🏗️  Refactor & improve code          │  │ 📋 Isi TESTING_CHECKLIST.md          │
│                                      │  │ 📝 Isi BUG_REPORTS.md               │
│ Tools: VS Code + AI + Terminal       │  │ Tools: Browser saja                  │
└──────────────────────────────────────┘  └──────────────────────────────────────┘
```

> [!IMPORTANT]
> Karena **hanya Dev 1 yang coding**, tidak ada risiko konflik Git sama sekali!

---

## 🔄 Alur Kerja Harian

| Waktu | Dev 1 (Coder) | Dev 2 (Tester) |
|-------|---------------|----------------|
| **Pagi** | `git pull` → mulai coding | `git pull` → `dotnet run` → mulai testing |
| **Siang** | Push perubahan | Pull update → test fitur baru |
| **Sore** | Fix bug dari laporan Dev 2 | Buat bug report untuk Dev 1 |
| **Pulang** | Push SEMUA perubahan | Update status checklist |

---

## 🛠️ Perintah Harian Dev 1 (Coder)

```bash
# PAGI — sebelum mulai
git pull origin main

# SETELAH SELESAI
git status
git add .
git commit -m "Deskripsi perubahan yang dibuat"
git push origin main
```

## 🖥️ Perintah Harian Dev 2 (Tester)

```bash
# PAGI — ambil kode terbaru dari Dev 1
cd C:\Users\[username]\Desktop\PortalHC_KPB
git pull origin main

# Jalankan website
dotnet run

# Buka browser → https://localhost:5001
# Mulai testing menggunakan TESTING_CHECKLIST.md
# Catat bug di BUG_REPORTS.md
```

---

## 📁 File Kolaborasi

| File | Dikelola oleh | Isi |
|------|--------------|-----|
| `STRATEGI_KOLABORASI.md` | Dev 1 & Dev 2 | Panduan ini |
| `TESTING_CHECKLIST.md` | Dev 2 | Checklist testing per modul & per role |
| `BUG_REPORTS.md` | Dev 2 (tulis) + Dev 1 (fix) | Laporan bug yang ditemukan |

---

## 📞 Komunikasi

| Media | Kapan |
|-------|-------|
| **WhatsApp/Telegram** | Koordinasi cepat: *"Sudah push fitur X, tolong test"* |
| **BUG_REPORTS.md** | Laporan bug formal |
| **Video call** | Demo fitur baru atau diskusi bug kompleks |

> [!TIP]
> **Kunci sukses:** Dev 1 push secara rutin → Dev 2 langsung bisa test versi terbaru.
> Komunikasi setiap kali ada fitur baru yang perlu ditest!
