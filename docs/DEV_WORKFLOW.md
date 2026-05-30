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

## Pre-Deploy Backup SOP (Phase 338 REST-05/06/07)

**RULE:** Setiap deploy ke Dev (10.55.3.3) atau Prod yang melibatkan migration WAJIB include backup pre-migration. Cilacap PreTest 30 Mar 2026 hilang karena IT redeploy tanpa backup (root cause `336-ROOT_CAUSE.md`). Phase 338 W5 close gap ini.

### Developer steps (sebelum push)

1. Commit code + migration files lokal. Verify build PASS + tests green.
2. Generate handoff doc dari template:
   ```bash
   cp docs/templates/DB_HANDOFF_IT.template.md docs/DB_HANDOFF_IT_$(date +%Y-%m-%d).md
   # Edit fill placeholder {DATE}, {COMMIT_HASH}, {BRANCH}, {MIGRATION_LIST},
   # {AFFECTED_TABLES}, {DEVELOPER_NAME}, {DEVELOPER_EMAIL}, {WINDOW_START}, dst
   ```
3. Optional: render Markdown→HTML pakai pandoc atau VS Code preview. Send ke IT
   sebagai `.md` atau `.html`.
4. Push commit + attach handoff doc ke IT email/WhatsApp notification.
5. **Wait** IT confirm backup completed sebelum migration apply.

### IT steps (di server)

1. Receive handoff doc dari developer (Markdown atau HTML).
2. Run backup script (Phase 338 REST-05):
   ```powershell
   .\scripts\backup-dev-pre-migration.ps1 `
       -Server "10.55.3.3" `
       -Database "HcPortalDB_Dev" `
       -OutputPath "C:\Backup\HcPortalDB_Dev_pre_$(Get-Date -Format yyyyMMdd_HHmmss).bak"
   ```
3. Confirm output backup file size (script auto-print di akhir). Verify > 100 MB
   (sesuaikan baseline DB size).
4. Reply developer: "Backup OK, file at `{path}` size `{size}MB`, proceeding migration."
5. Apply deployment per Section 4 handoff doc:
   - Stop service → Pull commit → `dotnet ef database update` → Start service → Smoke test.
6. Bila gagal: rollback via `RESTORE DATABASE` (Section 5 handoff doc) lalu notify developer.

### Automated guardrail (Phase 338 REST-06)

`CreateAssessment` admin form sekarang auto-detect counterpart Pre/Post group via
title pattern `{Pre|Post}Test {Track} {Lokasi}` (336-NAMING-CONVENTION-SPEC).
TempData info notify admin saat LinkedGroupId auto-paired — manual override
allowed via direct field set. Orphan Pre/Post detection awal mencegah pair
hilang seperti incident Cilacap.

### Reference Files

- Template: [`docs/templates/DB_HANDOFF_IT.template.md`](templates/DB_HANDOFF_IT.template.md)
- Script: [`scripts/backup-dev-pre-migration.ps1`](../scripts/backup-dev-pre-migration.ps1)
- Naming spec: [`336-NAMING-CONVENTION-SPEC.md`](../.planning/phases/336-investigate-pretest-loss-cilacap-restore-strategy/336-NAMING-CONVENTION-SPEC.md)
- Incident root cause: [`336-ROOT_CAUSE.md`](../.planning/phases/336-investigate-pretest-loss-cilacap-restore-strategy/336-ROOT_CAUSE.md)
- Existing handoff doc precedents: `docs/DB_HANDOFF_IT_2026-05-13.html`, `docs/DB_HANDOFF_IT_2026-05-26.html`

### Lesson Learned (Phase 336 + 338)

Cilacap PreTest 30 Mar 2026 hilang akibat IT pull code GitHub + sync DB tanpa
backup Dev existing → data Dev (PreTest dibuat langsung di Dev, BUKAN bagian
package sync) ke-overwrite. Root cause = OPERATIONAL, BUKAN aplikasi bug.

Phase 338 close gap dengan 3 layer guard:
1. **REST-05**: Template + script systematize backup workflow — IT punya artifact
   versioned di repo, tidak bergantung disiplin tulis doc manual tiap deploy.
2. **REST-06**: LinkedGroupId auto-pair di CreateAssessment — detect orphan Pre/Post
   group AWAL supaya kalau salah satu hilang, sistem indicate inconsistency.
3. **REST-07**: SOP doc onboarding developer baru — proses tertulis, traceable.

---

*Last updated: 2026-05-30 — Phase 338 REST-05/06/07 backup SOP + LinkedGroupId auto-pair.*
*Initial version: 2026-05-07.*
