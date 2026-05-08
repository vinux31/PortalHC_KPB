# Seed Workflow — Design Spec

**Tanggal:** 2026-05-08
**Status:** Draft for review
**Scope:** Aturan dan flow untuk pekerjaan yang berhubungan dengan seed data di HcPortal KPB

---

## 1. Tujuan

Aturan ini mencegah dua masalah konkret di workflow lokal:

1. **Seed test bocor jadi seed permanen** karena dev lupa kategorinya — mis. data dummy yang seharusnya sementara malah ikut ter-commit ke `Data/SeedData.cs`.
2. **Sisa data dummy mengotori DB lokal** karena cleanup tidak lengkap (cascade orphan dari Sessions, Answers, dst.).

Aturan ini **hanya berlaku untuk lingkungan lokal**. DB Dev (`10.55.3.3`) dan Prod tidak boleh disentuh developer — promosi data master baru ke Dev/Prod adalah tanggung jawab Team IT (lihat `docs/DEV_WORKFLOW.md` §6).

**DB lokal yang aktif:** SQL Server Express (`localhost\SQLEXPRESS`, database `HcPortalDB_Dev`). Lihat `appsettings.Development.json`.

---

## 2. Klasifikasi seed (wajib sebelum mulai)

Setiap seed yang akan dibuat **harus diklasifikasi dulu** dengan format `<lifecycle> + <scope>`:

| Lifecycle | Scope | Use case | Tempat tinggal |
|---|---|---|---|
| `permanent` | `prod-required` | Tambah unit organisasi, role baru, master data wajib | `Data/SeedData.cs` (idempotent, lihat pola eksisting) |
| `temporary` | `local-only` | Reproduce bug, fixture Playwright, eksplorasi fitur, demo lokal | DB lokal saja, **wajib dihapus** setelah selesai |

**Hanya dua kombinasi di atas yang valid.** Kombinasi `permanent + local-only` tidak masuk akal (kalau permanent, harus ada di prod). Kombinasi `temporary + prod-required` juga tidak boleh (kalau prod butuh, dia permanent).

Kalau ragu antara `permanent` vs `temporary` — diskusikan dulu sebelum lanjut. Salah klasifikasi berarti DB lokal/Prod jadi kotor.

---

## 3. Flow 5 langkah (untuk `temporary + local-only`)

Flow ini wajib diikuti tiap kali butuh seed temporary:

1. **Klasifikasi.** Tetapkan tag (`temporary + local-only`), tujuan (terkait phase/bug apa), dan dampak yang diperkirakan (entitas mana saja yang akan tersentuh: Users, Assessments, Sessions, Answers, dll.).

2. **Snapshot DB.** Backup `HcPortalDB_Dev` ke file `.bak` pakai `sqlcmd` (lihat §5). Stop Kestrel dulu sebelum snapshot supaya state konsisten.

3. **Catat di journal.** Tambah satu baris di `docs/SEED_JOURNAL.md` dengan status `active` (lihat §4 untuk format).

4. **Insert seed & jalankan test.** Apa pun hasilnya (sukses atau gagal), lanjut ke step 5. Jangan biarkan seed nempel di DB lewat session kerja.

5. **Restore + tutup journal.** Restore DB dari `.bak`, ubah status journal jadi `cleaned`, hapus file `.bak` (atau biarkan kalau mau, tapi pastikan masuk `.gitignore`).

**Untuk `permanent + prod-required`:** flow berbeda — edit `Data/SeedData.cs` mengikuti pola idempotent yang sudah ada (cek dulu sebelum insert), commit, kemudian notifikasi IT untuk apply ke Dev. Tidak butuh snapshot/journal.

---

## 4. Format journal — `docs/SEED_JOURNAL.md`

File append-only. Header dibuat sekali, lalu satu baris per seed:

```markdown
# Seed Journal

Audit trail untuk seed `temporary + local-only`. Lihat `docs/SEED_WORKFLOW.md` untuk aturan.

| Tanggal | Phase | Klasifikasi | Tujuan | Dampak (entitas tersentuh) | Snapshot file | Status |
|---------|-------|-------------|--------|----------------------------|---------------|--------|
| 2026-05-08 | 314 | temporary + local-only | Reproduce regenerate token untuk status upcoming | Users(1), Assessments(2), Sessions(2) | HcPortalDB_Dev.20260508-1430.bak | cleaned |
```

**Aturan journal:**
- Status `active` saat seed masih ada di DB → `cleaned` setelah restore
- Entry tidak pernah dihapus (audit trail)
- Kolom "Dampak" diisi *sebelum* test mulai (perkiraan), boleh di-update kalau berbeda dari kenyataan
- Kalau ada beberapa seed dalam satu session, satu baris per seed (jangan digabung)

---

## 5. Command snapshot & restore (SQL Server Express)

### 5.1 Snapshot (sebelum seed)

```bash
# 1. Stop Kestrel kalau lagi jalan (Ctrl+C di terminal dotnet run)
# 2. Backup DB ke .bak file
sqlcmd -S "localhost\SQLEXPRESS" -E -Q "BACKUP DATABASE HcPortalDB_Dev TO DISK='C:\Temp\HcPortalDB_Dev.20260508-1430.bak' WITH INIT"
```

**Catatan:**
- `-E` = Windows Integrated Security (sesuai connection string `Integrated Security=True`)
- `WITH INIT` = overwrite kalau file sudah ada
- Lokasi `C:\Temp\` harus bisa ditulis oleh service `MSSQL$SQLEXPRESS`. Kalau permission denied, pakai folder default instance (mis. `C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\`)

### 5.2 Restore (setelah test)

```bash
# Pastikan tidak ada koneksi aktif: Kestrel mati, SSMS/Azure Data Studio tutup koneksi ke HcPortalDB_Dev
sqlcmd -S "localhost\SQLEXPRESS" -E -Q "USE master; ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE HcPortalDB_Dev FROM DISK='C:\Temp\HcPortalDB_Dev.20260508-1430.bak' WITH REPLACE; ALTER DATABASE HcPortalDB_Dev SET MULTI_USER;"
```

**Penjelasan steps:**
- `SINGLE_USER WITH ROLLBACK IMMEDIATE` — evict semua koneksi aktif, rollback transaction yang berjalan (ini kenapa Kestrel/SSMS harus tutup dulu, atau ROLLBACK IMMEDIATE akan kill koneksi mereka secara paksa)
- `RESTORE ... WITH REPLACE` — overwrite DB existing dengan isi backup
- `MULTI_USER` — kembalikan ke mode normal supaya Kestrel bisa connect lagi

**Kalau restore gagal** dengan error "exclusive access could not be obtained": ada koneksi yang masih hidup. Jalankan dulu:
```bash
sqlcmd -S "localhost\SQLEXPRESS" -E -Q "SELECT session_id FROM sys.dm_exec_sessions WHERE database_id = DB_ID('HcPortalDB_Dev')"
```
Kill session yang muncul, lalu retry restore.

### 5.3 Cleanup file backup

Setelah journal di-mark `cleaned`, hapus `.bak` file (opsional, tapi disarankan supaya `C:\Temp\` tidak penuh):

```bash
rm "C:\Temp\HcPortalDB_Dev.20260508-1430.bak"
```

---

## 6. Lokasi & struktur file

| File | Isi | Status |
|---|---|---|
| `CLAUDE.md` | Seksi pendek `## Seed Data Workflow (Lokal)` (4-5 baris ringkasan + link) | Update existing |
| `docs/SEED_WORKFLOW.md` | Detail lengkap (§1-§5 dari spec ini) | New file |
| `docs/SEED_JOURNAL.md` | Header + tabel kosong (siap diisi entry pertama) | New file |
| `.gitignore` | Tambah pattern `*.bak` supaya backup tidak ke-commit | Update existing |

---

## 7. Draft kalimat untuk `CLAUDE.md`

Disisipkan setelah seksi "Develop Workflow", sebelum baris terakhir. Style: numbered bullets + ❌, mengikuti pola Develop Workflow yang sudah ada.

```markdown
## Seed Data Workflow (Lokal)

Aturan ringkas saat butuh seed data untuk testing/development:

1. **Klasifikasi dulu** sebelum membuat seed — `temporary + local-only` (untuk test/reproduce) atau `permanent + prod-required` (masuk `Data/SeedData.cs`). Kalau ragu, diskusikan.
2. **Snapshot DB** lokal (`sqlcmd ... BACKUP DATABASE`) sebelum insert seed temporary.
3. **Catat di `docs/SEED_JOURNAL.md`** — tujuan, klasifikasi, dampak entitas tersentuh.
4. **Restore DB** setelah test selesai (sukses *atau* gagal), lalu tandai journal `cleaned`.

❌ Jangan biarkan seed temporary nempel di DB lokal lewat session kerja. ❌ Jangan promosikan seed temporary jadi permanent tanpa review (pindah ke `Data/SeedData.cs` dulu, baru commit).

Detail lengkap (klasifikasi, format journal, command SQL Server BACKUP/RESTORE): lihat [`docs/SEED_WORKFLOW.md`](docs/SEED_WORKFLOW.md).
```

---

## 8. Hal yang sengaja TIDAK di-cover

YAGNI list — supaya scope tetap fokus:

- **Strategi seed untuk Dev/Prod.** Tidak relevan — Dev/Prod dipegang IT.
- **Cleanup script per-seed (SQL DELETE manual).** Over-engineered. Snapshot/restore lebih reliable.
- **Transactional seed.** Tidak compatible dengan Playwright E2E (request-per-transaction).
- **Otomasi via npm script atau dotnet tool.** Bisa ditambah nanti kalau flow ini terbukti dipakai konsisten. Untuk awal: command manual cukup.
- **Multi-developer collaboration.** Saat ini single dev (Rino). Kalau nanti tim bertambah, journal jadi shared state — bisa di-revisit.

---

## 9. Verifikasi spec ini terhadap codebase

Klaim teknis di spec ini sudah di-verifikasi:

| Klaim | Sumber verifikasi |
|---|---|
| Lokal pakai SQL Server Express, bukan SQLite | `appsettings.Development.json:10` |
| `Data/SeedData.cs` adalah pola idempotent + production-safe | `Data/SeedData.cs:7-10` (komentar header) |
| WAL mode hanya aktif untuk SQLite (tidak relevan SQLEXPRESS) | `Program.cs:137` (kondisi `ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"`) |
| Style CLAUDE.md = numbered bullets + ❌, link ke detail | `CLAUDE.md:9-19` |
| Naming convention file = UPPERCASE_UNDERSCORE | `docs/DEV_WORKFLOW.md` |
| Test infrastructure pakai Playwright | `tests/playwright.config.ts`, `tests/package.json` |
| Promosi ke Dev = tanggung jawab IT | `docs/DEV_WORKFLOW.md` §2, §6 |
