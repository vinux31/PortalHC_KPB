# Seed Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tetapkan aturan dan flow untuk pekerjaan yang berhubungan dengan seed data (klasifikasi, snapshot/restore, journal) lewat 3 dokumentasi baru/update + `.gitignore` patch.

**Architecture:** Pure dokumentasi + `.gitignore` change. Tidak ada perubahan code. Pola mengikuti `DEV_WORKFLOW.md` yang sudah ada — `CLAUDE.md` jadi entry point ringkas, `docs/SEED_WORKFLOW.md` jadi detail, `docs/SEED_JOURNAL.md` jadi audit trail kosong yang siap diisi.

**Tech Stack:** Markdown, Git, `.gitignore` patterns. Tidak ada testing framework — verification dilakukan dengan cara baca file balik dan validasi link.

**Spec source:** `docs/superpowers/specs/2026-05-08-seed-workflow-design.md`

---

## File Structure

| File | Aksi | Tanggung jawab |
|---|---|---|
| `.gitignore` | Modify (line ~501-507) | Tambah pattern `*.bak` supaya snapshot file SQL Server tidak ke-commit |
| `docs/SEED_JOURNAL.md` | Create | Audit trail append-only untuk seed `temporary + local-only` |
| `docs/SEED_WORKFLOW.md` | Create | Detail lengkap flow + klasifikasi + command snapshot/restore |
| `CLAUDE.md` | Modify (insert after line 19) | Seksi ringkas `## Seed Data Workflow (Lokal)` + link ke detail |

**Urutan implementasi:** dari yang paling tidak punya dependency (`.gitignore` → `SEED_JOURNAL.md` → `SEED_WORKFLOW.md` → `CLAUDE.md`). Ini supaya kalau dev mau test flow di tengah jalan (snapshot beneran), `.gitignore` sudah aman duluan.

---

## Task 1: Update `.gitignore` — tambah `*.bak`

**Files:**
- Modify: `.gitignore:501-507` (seksi "Temp/debug artifacts")

**Konteks:** Saat ini `.gitignore` punya `*.rptproj.bak` (line 274) yang spesifik Reporting Services. Tidak ada pattern umum `*.bak`. Snapshot SQL Server akan menghasilkan file `HcPortalDB_Dev.<timestamp>.bak` — meskipun lokasinya `C:\Temp\` (di luar repo), defensive: tambah `*.bak` ke `.gitignore` supaya kalau ada dev yang nyimpen di dalam repo karena permission issue, tidak ikut ke-commit.

- [ ] **Step 1: Baca seksi target di `.gitignore`**

Run: lihat lines 499-510 untuk konfirmasi context yang akan diubah.

Expected output (current):
```
# Git worktrees
.worktrees/

# Temp/debug artifacts
*.png
!wwwroot/**/*.png
*.db
*.db-shm
*.db-wal
.playwright-mcp/
```

- [ ] **Step 2: Tambahkan `*.bak` setelah baris `*.db-wal`**

Edit `.gitignore`, ubah seksi "Temp/debug artifacts":

```
# Temp/debug artifacts
*.png
!wwwroot/**/*.png
*.db
*.db-shm
*.db-wal
*.bak
.playwright-mcp/
```

- [ ] **Step 3: Verifikasi pattern `*.bak` aktif**

Buat dummy file dan cek `git status`:

```bash
touch test-snapshot.bak
git status --porcelain | grep test-snapshot.bak
```

Expected: tidak ada output (file di-ignore).

Lalu cleanup:
```bash
rm test-snapshot.bak
```

- [ ] **Step 4: Commit**

```bash
git add .gitignore
git commit -m "chore(gitignore): ignore *.bak files for seed snapshot workflow"
```

---

## Task 2: Create `docs/SEED_JOURNAL.md`

**Files:**
- Create: `docs/SEED_JOURNAL.md`

**Konteks:** Journal append-only. Header dibuat sekali, lalu setiap dev yang bikin seed temporary nambah satu baris. File harus ada *sebelum* `SEED_WORKFLOW.md` di-publish supaya link dari workflow tidak broken.

- [ ] **Step 1: Buat file dengan header + tabel kosong**

Tulis ke `docs/SEED_JOURNAL.md`:

```markdown
# Seed Journal

Audit trail untuk seed `temporary + local-only`. Lihat [`docs/SEED_WORKFLOW.md`](SEED_WORKFLOW.md) untuk aturan klasifikasi & flow.

**Cara isi:** Tambah satu baris per seed temporary (jangan digabung). Status `active` saat seed masih ada di DB → `cleaned` setelah restore. Entry tidak pernah dihapus (audit trail).

| Tanggal | Phase | Klasifikasi | Tujuan | Dampak (entitas tersentuh) | Snapshot file | Status |
|---------|-------|-------------|--------|----------------------------|---------------|--------|
| _(belum ada entry)_ | | | | | | |
```

- [ ] **Step 2: Verifikasi file ada dan markdown valid**

```bash
ls docs/SEED_JOURNAL.md
head -5 docs/SEED_JOURNAL.md
```

Expected: file ada, header `# Seed Journal` muncul di line 1.

- [ ] **Step 3: Commit**

```bash
git add docs/SEED_JOURNAL.md
git commit -m "docs: add SEED_JOURNAL.md for seed audit trail"
```

---

## Task 3: Create `docs/SEED_WORKFLOW.md`

**Files:**
- Create: `docs/SEED_WORKFLOW.md`

**Konteks:** File detail lengkap. Sumber konten = spec §1-§5 + §8. Style harus konsisten dengan `DEV_WORKFLOW.md`: numbered sections, tabel environment, golden rules pakai ❌/✅, command block dengan bahasa shell (`bash`).

- [ ] **Step 1: Tulis isi file**

Tulis ke `docs/SEED_WORKFLOW.md`:

````markdown
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
````

- [ ] **Step 2: Verifikasi file ada dan link relatif valid**

```bash
ls docs/SEED_WORKFLOW.md
grep -n "](SEED_JOURNAL.md)" docs/SEED_WORKFLOW.md
grep -n "](DEV_WORKFLOW.md)" docs/SEED_WORKFLOW.md
grep -n "../CLAUDE.md" docs/SEED_WORKFLOW.md
```

Expected: file ada, semua link relatif muncul (link ke SEED_JOURNAL.md, DEV_WORKFLOW.md, CLAUDE.md).

- [ ] **Step 3: Verifikasi referenced files semua eksis**

```bash
ls docs/SEED_JOURNAL.md docs/DEV_WORKFLOW.md CLAUDE.md Data/SeedData.cs appsettings.Development.json
```

Expected: semua file listed (tidak ada "No such file"). Kalau ada yang missing, link broken.

- [ ] **Step 4: Commit**

```bash
git add docs/SEED_WORKFLOW.md
git commit -m "docs: add SEED_WORKFLOW.md with classification & SQL Server snapshot/restore flow"
```

---

## Task 4: Update `CLAUDE.md` — Tambah Seksi Seed Workflow

**Files:**
- Modify: `CLAUDE.md` (insert section between line 19 dan akhir file)

**Konteks:** `CLAUDE.md` saat ini punya seksi "Develop Workflow (Wajib Dibaca)" (lines 5-19). Pola: heading → narasi singkat → numbered list → ❌ rules → link ke detail. Seksi seed harus mengikuti pola yang sama.

- [ ] **Step 1: Baca CLAUDE.md untuk konfirmasi insertion point**

Run: cek line 19 (akhir seksi "Develop Workflow") dan pastikan tidak ada konten lain setelahnya.

Expected: file berakhir di line 19 (`Detail lengkap... lihat docs/DEV_WORKFLOW.md`) dengan trailing newline.

- [ ] **Step 2: Append seksi baru ke akhir CLAUDE.md**

Tambah konten ini setelah line 19:

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

Pastikan ada blank line **sebelum** heading `## Seed Data Workflow (Lokal)` (sesuai CommonMark — heading butuh blank line di atasnya).

- [ ] **Step 3: Verifikasi struktur file**

```bash
grep -n "^## " CLAUDE.md
```

Expected (2 headings):
```
5:## Develop Workflow (Wajib Dibaca)
21:## Seed Data Workflow (Lokal)
```

(line number untuk seksi baru bisa beda tergantung blank line, tapi harus muncul).

- [ ] **Step 4: Verifikasi link ke SEED_WORKFLOW.md valid**

```bash
grep -n "docs/SEED_WORKFLOW.md" CLAUDE.md
ls docs/SEED_WORKFLOW.md
```

Expected: link muncul di CLAUDE.md, file target eksis.

- [ ] **Step 5: Commit**

```bash
git add CLAUDE.md
git commit -m "docs(claude): add Seed Data Workflow section linking to SEED_WORKFLOW.md"
```

---

## Task 5: End-to-End Verification

**Files:**
- Read-only verification (no changes)

**Konteks:** Pastikan semua 4 file (`.gitignore`, `SEED_JOURNAL.md`, `SEED_WORKFLOW.md`, `CLAUDE.md`) konsisten satu sama lain. Cross-reference link harus valid dua arah.

- [ ] **Step 1: Verifikasi semua file commit-able**

```bash
git status --porcelain
```

Expected: clean working tree (semua sudah committed di Task 1-4).

- [ ] **Step 2: Verifikasi semua file ada di lokasi yang benar**

```bash
ls .gitignore docs/SEED_JOURNAL.md docs/SEED_WORKFLOW.md CLAUDE.md
```

Expected: semua 4 file listed.

- [ ] **Step 3: Cross-reference link bidirectional**

CLAUDE.md → SEED_WORKFLOW.md:
```bash
grep "docs/SEED_WORKFLOW.md" CLAUDE.md
```
Expected: muncul.

SEED_WORKFLOW.md → SEED_JOURNAL.md:
```bash
grep "SEED_JOURNAL.md" docs/SEED_WORKFLOW.md
```
Expected: muncul (multiple kali — header reference, body, contoh).

SEED_JOURNAL.md → SEED_WORKFLOW.md:
```bash
grep "SEED_WORKFLOW.md" docs/SEED_JOURNAL.md
```
Expected: muncul (header reference).

SEED_WORKFLOW.md → DEV_WORKFLOW.md:
```bash
grep "DEV_WORKFLOW.md" docs/SEED_WORKFLOW.md
```
Expected: muncul (komplemen reference + golden rules reference).

- [ ] **Step 4: Verifikasi `*.bak` ignore pattern aktif**

```bash
touch test-final.bak
git status --porcelain | grep "test-final.bak" || echo "PROPERLY IGNORED"
rm test-final.bak
```

Expected: `PROPERLY IGNORED` muncul.

- [ ] **Step 5: Konfirmasi git log**

```bash
git log --oneline -5
```

Expected: 4 commit terakhir adalah Task 1-4 (gitignore, SEED_JOURNAL, SEED_WORKFLOW, CLAUDE.md update). Urutan boleh bervariasi tapi semua 4 ada.

- [ ] **Step 6: Final report**

Tidak ada commit di step ini — verifikasi saja. Kalau semua expected match → implementasi selesai.

---

## Self-Review

**Spec coverage:**
- ✓ §1 Tujuan → tercakup di SEED_WORKFLOW.md §1 (Task 3)
- ✓ §2 Klasifikasi → SEED_WORKFLOW.md §3 (Task 3)
- ✓ §3 Flow 5 langkah → SEED_WORKFLOW.md §4 (Task 3)
- ✓ §4 Format journal → SEED_JOURNAL.md (Task 2) + SEED_WORKFLOW.md §6 (Task 3)
- ✓ §5 Command snapshot/restore → SEED_WORKFLOW.md §5 (Task 3)
- ✓ §6 Lokasi & struktur → semua 4 file dibikin (Task 1-4)
- ✓ §7 Draft CLAUDE.md → CLAUDE.md update (Task 4)
- ✓ §8 YAGNI list → SEED_WORKFLOW.md §7 (Task 3)
- ✓ Spec line "tambah pattern *.bak ke .gitignore" → Task 1

**Placeholder scan:** tidak ada TBD/TODO. Semua command konkret, semua content full.

**Type/path consistency:**
- File path `docs/SEED_WORKFLOW.md` & `docs/SEED_JOURNAL.md` konsisten di Task 2, 3, 4, 5
- Database name `HcPortalDB_Dev` konsisten di seluruh dokumen
- SQL Server instance `localhost\SQLEXPRESS` konsisten
- Backup filename pattern `HcPortalDB_Dev.20260508-1430.bak` konsisten di Task 3 §5.1, §5.2, §5.4, §6
