# Seed Data Workflow — HcPortal (KPB)

Dokumen ini adalah **sumber kebenaran utama** untuk pekerjaan yang berhubungan dengan seed data. Wajib diikuti baik oleh developer manusia maupun AI assistant (Claude/Copilot). Komplemen dari [`docs/DEV_WORKFLOW.md`](DEV_WORKFLOW.md).

---

## 1. Tujuan

Aturan ini mencegah dua masalah konkret di workflow lokal:

1. **Seed test bocor jadi seed permanen** — data dummy yang seharusnya sementara malah ikut ter-commit ke `Data/SeedData.cs`.
2. **Sisa data dummy mengotori DB lokal** — cleanup tidak lengkap (cascade orphan dari Sessions, Answers, dst.).

Aturan ini **hanya berlaku untuk lingkungan lokal**. DB Dev (`10.55.3.3`) dan Prod tidak boleh disentuh developer (lihat `DEV_WORKFLOW.md` §2).

**DB lokal yang aktif:** SQL Server Express (`localhost\SQLEXPRESS`, database `HcPortalDB_Dev`). Sumber: `appsettings.Development.json`.

---

## 2. Aturan Utama (Golden Rules)

- ❌ **JANGAN** bikin seed tanpa klasifikasi dulu.
- ❌ **JANGAN** biarkan seed `temporary` nempel di DB lokal lewat session kerja.
- ❌ **JANGAN** promosikan seed temporary jadi permanent tanpa review (pindah ke `Data/SeedData.cs` dulu, baru commit).
- ✅ **SELALU** snapshot DB sebelum insert seed temporary.
- ✅ **SELALU** restore DB setelah test (sukses atau gagal).
- ✅ **SELALU** catat entry di `SEED_JOURNAL.md` sebelum mulai.

---

## 3. Klasifikasi Seed (Wajib Sebelum Mulai)

Setiap seed harus diklasifikasi dengan format `<lifecycle> + <scope>`:

| Lifecycle | Scope | Use case | Tempat tinggal |
|---|---|---|---|
| `permanent` | `prod-required` | Tambah unit organisasi, role baru, master data wajib | `Data/SeedData.cs` (idempotent, lihat pola eksisting) |
| `temporary` | `local-only` | Reproduce bug, fixture Playwright, eksplorasi fitur, demo lokal | DB lokal saja, **wajib dihapus** |

**Hanya dua kombinasi di atas yang valid.** Kalau ragu antara `permanent` vs `temporary`, diskusikan dulu — salah klasifikasi berarti DB lokal/Prod kotor.

---

## 4. Flow 5 Langkah (untuk `temporary + local-only`)

### Step 1 — Klasifikasi
Tetapkan tag (`temporary + local-only`), tujuan (terkait phase/bug apa), dan dampak yang diperkirakan (entitas mana saja yang akan tersentuh: Users, Assessments, Sessions, Answers, dll.).

### Step 2 — Snapshot DB
Stop Kestrel dulu, lalu backup DB ke file `.bak` (lihat §5.1).

### Step 3 — Catat di Journal
Tambah satu baris di [`SEED_JOURNAL.md`](SEED_JOURNAL.md) dengan status `active`.

### Step 4 — Insert Seed & Jalankan Test
Apa pun hasilnya (sukses atau gagal), lanjut ke Step 5. **Jangan biarkan seed nempel** lewat session kerja.

### Step 5 — Restore + Tutup Journal
- Restore DB dari `.bak` (lihat §5.2)
- Ubah status journal jadi `cleaned`
- Hapus file `.bak` (atau biarkan, tapi sudah masuk `.gitignore`)

### Untuk `permanent + prod-required`
Flow berbeda: edit `Data/SeedData.cs` mengikuti pola idempotent yang sudah ada (cek dulu sebelum insert), commit, kemudian notifikasi IT untuk apply ke Dev. **Tidak butuh snapshot/journal**.

---

## 5. Command Snapshot & Restore (SQL Server Express)

### 5.1 Snapshot (Sebelum Seed)

```bash
# 1. Stop Kestrel kalau lagi jalan (Ctrl+C di terminal dotnet run)
# 2. Backup DB ke .bak file
sqlcmd -S "localhost\SQLEXPRESS" -E -Q "BACKUP DATABASE HcPortalDB_Dev TO DISK='C:\Temp\HcPortalDB_Dev.20260508-1430.bak' WITH INIT"
```

**Catatan:**
- `-E` = Windows Integrated Security (sesuai connection string `Integrated Security=True`).
- `WITH INIT` = overwrite kalau file sudah ada.
- Lokasi `C:\Temp\` harus bisa ditulis oleh service `MSSQL$SQLEXPRESS`. Kalau permission denied, pakai folder default instance (mis. `C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\`).

### 5.2 Restore (Setelah Test)

```bash
# Pastikan tidak ada koneksi aktif: Kestrel mati, SSMS/Azure Data Studio tutup koneksi ke HcPortalDB_Dev
sqlcmd -S "localhost\SQLEXPRESS" -E -Q "USE master; ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE HcPortalDB_Dev FROM DISK='C:\Temp\HcPortalDB_Dev.20260508-1430.bak' WITH REPLACE; ALTER DATABASE HcPortalDB_Dev SET MULTI_USER;"
```

**Penjelasan steps:**
- `SINGLE_USER WITH ROLLBACK IMMEDIATE` — evict semua koneksi aktif, rollback transaction berjalan.
- `RESTORE ... WITH REPLACE` — overwrite DB existing dengan isi backup.
- `MULTI_USER` — kembalikan ke mode normal supaya Kestrel bisa connect lagi.

### 5.3 Troubleshooting

**Error "exclusive access could not be obtained":** ada koneksi yang masih hidup. Cek dulu:

```bash
sqlcmd -S "localhost\SQLEXPRESS" -E -Q "SELECT session_id FROM sys.dm_exec_sessions WHERE database_id = DB_ID('HcPortalDB_Dev')"
```

Kill session yang muncul (atau tutup SSMS/Azure Data Studio), lalu retry restore.

### 5.4 Cleanup File Backup

Setelah journal di-mark `cleaned`:

```bash
rm "C:\Temp\HcPortalDB_Dev.20260508-1430.bak"
```

Opsional, tapi disarankan supaya `C:\Temp\` tidak penuh.

---

## 6. Format Journal — `SEED_JOURNAL.md`

File [`SEED_JOURNAL.md`](SEED_JOURNAL.md) adalah audit trail append-only. Aturan pengisian:

- **Status `active`** saat seed masih ada di DB.
- **Status `cleaned`** setelah restore selesai.
- **Entry tidak pernah dihapus** — audit trail berarti history utuh.
- **Kolom Dampak** diisi *sebelum* test mulai (perkiraan), boleh di-update kalau berbeda dari kenyataan.
- **Satu baris per seed** — kalau ada beberapa seed dalam satu session, satu baris masing-masing.

Contoh entry:

```markdown
| 2026-05-08 | 314 | temporary + local-only | Reproduce regenerate token untuk status upcoming | Users(1), Assessments(2), Sessions(2) | HcPortalDB_Dev.20260508-1430.bak | cleaned |
```

---

## 7. Hal yang Sengaja TIDAK Di-cover

- **Strategi seed untuk Dev/Prod** — tidak relevan, dipegang IT.
- **Cleanup script per-seed (SQL DELETE manual)** — over-engineered, snapshot/restore lebih reliable.
- **Transactional seed** — tidak compatible dengan Playwright E2E.
- **Otomasi via npm script atau dotnet tool** — bisa ditambah nanti kalau flow ini terbukti dipakai konsisten.

---

## 8. Referensi

- [`CLAUDE.md`](../CLAUDE.md) — entry point ringkas
- [`docs/DEV_WORKFLOW.md`](DEV_WORKFLOW.md) — workflow code & promosi Dev/Prod
- [`docs/SEED_JOURNAL.md`](SEED_JOURNAL.md) — audit trail aktif
- [`Data/SeedData.cs`](../Data/SeedData.cs) — pola seed `permanent + prod-required` (idempotent)
- [`appsettings.Development.json`](../appsettings.Development.json) — connection string lokal

---

*Last updated: 2026-05-08 — initial version.*
