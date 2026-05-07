# Develop Workflow — HcPortal (KPB)

Dokumen ini adalah **sumber kebenaran utama** untuk alur kerja perbaikan/development proyek HcPortal. Wajib diikuti baik oleh developer manusia maupun AI assistant (Claude/Copilot).

---

## 1. Environment Map

Status website terdiri dari **3 environment** dengan pemegang yang berbeda:

| Aspek | Lokal | Development | Production |
|---|---|---|---|
| **URL** | `http://localhost:5277` | `http://10.55.3.3/KPB-PortalHC` | `appkpb.pertamina.com/KPB-PortalHC` *(perkiraan, belum live)* |
| **Connection String** | `appsettings.Development.json` (SQLEXPRESS / SQLite) | dikelola Team IT | `appsettings.Production.json` (template) |
| **DB** | `HcPortal.db` (SQLite) atau `HcPortalDB_Dev` lokal (SQLEXPRESS) | `HcPortalDB_Dev` di server IT | `HcPortalDB` di server IT |
| **Yang pegang** | Developer (Rino) | **Team IT** | **Team IT** |
| **Cara update** | edit langsung | request ke IT | request ke IT |

> **Sumber URL lokal:** `Properties/launchSettings.json` (profile `HcPortal`).
> **Sumber connection string:** `appsettings.Development.json` & `appsettings.Production.json` — **jangan commit credential** ke repo.

---

## 2. Aturan Utama (Golden Rules)

- ❌ **JANGAN** edit kode atau DB langsung di server Dev atau Prod.
- ❌ **JANGAN** push ke `main` tanpa verifikasi lokal.
- ❌ **JANGAN** ALTER tabel manual — selalu pakai EF Core migration.
- ✅ **SELALU** reproduce bug di lokal sebelum fix.
- ✅ **SELALU** verifikasi fix di lokal (build + run + DB) sebelum commit.
- ✅ Promosi ke Dev/Prod **hanya** Team IT (deploy code & apply migration ke DB).

---

## 3. Standard Fix Workflow (Step-by-Step)

### Step 1 — Identifikasi bug di Dev
- Buka `http://10.55.3.3/KPB-PortalHC`
- Catat:
  - URL halaman yang bermasalah
  - Langkah reproduce (klik apa, isi apa)
  - Pesan error / screenshot
  - Akun yang dipakai (role)

### Step 2 — Reproduce di lokal
- `dotnet run` dari root project
- Buka `http://localhost:5277`
- Pastikan bug muncul juga di lokal.
- Kalau **tidak muncul** di lokal: periksa apakah masalah datang dari data spesifik di DB Dev atau perbedaan config — investigasi lebih dulu sebelum lanjut.

### Step 3 — Fix kode / DB
- Edit file yang relevan (Controller / View / Model / Service / dll).
- Kalau ubah model atau schema → buat migration:
  ```bash
  dotnet ef migrations add <NamaMigrasi> --context ApplicationDbContext
  ```
- Test apply migration di lokal:
  ```bash
  dotnet ef database update --context ApplicationDbContext
  ```

### Step 4 — Verifikasi lokal (WAJIB sebelum commit)
- [ ] `dotnet build` → tanpa error & warning baru
- [ ] `dotnet run` → halaman fix bekerja, tes **golden path** + **edge case** manual via browser
- [ ] DB lokal: migration apply sukses & data konsisten
- [ ] (Disarankan) Playwright tests:
  ```bash
  cd tests
  npx playwright test
  ```

### Step 5 — Commit & push
- Commit message jelas (ikut konvensi `.github/copilot-instructions.md`).
- Push ke branch sesuai aturan repo (default: `main`).
- File migration **wajib** ikut ter-commit.

### Step 6 — Handoff ke Team IT
- Notifikasi Team IT dengan info:
  - Commit hash
  - Deskripsi singkat fix
  - **Flag eksplisit** kalau ada migration baru: *"Ada migration baru: `<NamaMigrasi>`, perlu `dotnet ef database update` di DB Dev."*
- Team IT akan:
  - `git pull` di server Dev
  - Deploy code ke server (`10.55.3.3`)
  - Apply migration ke `HcPortalDB_Dev`
- Setelah IT selesai deploy → **verifikasi ulang di Dev** untuk memastikan fix sukses di environment Dev.

---

## 4. SOP Migration DB

Karena DB Dev dipegang Team IT, perubahan schema harus dirapikan dulu di lokal:

- **Selalu** pakai EF Core migration. **Jangan** ALTER tabel manual.
- Test **apply** migration di lokal sebelum commit.
- Test **rollback** kalau memungkinkan:
  ```bash
  dotnet ef database update <NamaMigrationSebelumnya> --context ApplicationDbContext
  ```
- File `Migrations/*.cs` (Up/Down + ModelSnapshot) **wajib** ter-commit.
- Kalau migration mengandung **data migration** (mis. UPDATE data lama, bukan hanya schema), kasih catatan tambahan ke IT supaya mereka backup DB Dev sebelum apply.
- Hindari migration yang **destructive** (DROP COLUMN dengan data) tanpa diskusi dulu — koordinasi dengan IT.

---

## 5. Pre-Commit Checklist (Quick Reference)

Salin ke deskripsi commit / PR:

```
- [ ] dotnet build pass (tanpa warning baru)
- [ ] dotnet run + manual verify di http://localhost:5277
- [ ] Golden path & edge case dicek manual
- [ ] DB lokal: migration apply & data OK
- [ ] (Optional) Playwright tests pass
- [ ] Migration file di-commit (jika ada)
- [ ] Team IT di-notify (commit hash + flag migration)
```

---

## 6. Tanggung Jawab Team IT

- Deploy code dari `main` ke server Dev (`10.55.3.3`).
- Apply EF migration ke `HcPortalDB_Dev`.
- Promosi Dev → Prod setelah validasi.
- Manage credential server (connection string production, dll) — **tidak boleh** di-commit ke repo.
- Backup DB sebelum apply migration destructive.

---

## 7. Referensi

- [`.github/copilot-instructions.md`](../.github/copilot-instructions.md) — instruksi teknis (build/run/EF) & konvensi kode
- [`Database/SETUP_GUIDE.md`](../Database/SETUP_GUIDE.md) — setup DB lokal pertama kali
- [`Database/QUICK_START.md`](../Database/QUICK_START.md) — quick reference setup DB
- [`docs/checklist-deploy.html`](checklist-deploy.html) — checklist deploy Production (untuk Team IT)
- `Properties/launchSettings.json` — config URL & ASPNETCORE_ENVIRONMENT lokal

---

*Last updated: 2026-05-07 — initial version.*
