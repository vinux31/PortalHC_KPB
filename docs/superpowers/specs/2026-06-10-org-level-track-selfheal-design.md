# Self-Heal Seed: Org Level Normalization (F1) + ProtonTrack Re-Seed (F2)

**Tanggal:** 2026-06-10
**Branch:** `main` (jalur hotfix, decoupled dari milestone v24/v25 di `ITHandoff`)
**Jenis:** Data-integrity hotfix — startup self-heal di `Data/SeedData.cs`
**Migration:** TIDAK ADA (data-only, no schema change)
**Asal:** Dev check post-deploy `14e7adc5` (lihat memory `project_dev_deploy_14e7adc5_findings`)

---

## 1. Problem

Dua temuan saat verifikasi Dev (`10.55.3.3/KPB-PortalHC`), dua-duanya **data**, bukan bug logika:

### F1 — Org Level split-brain (HIGH)
`OrganizationUnits.Level` di Dev nyampur konvensi:
- LAB / OM / UTL II / HC = Level 0 (benar — ditambah via UI v21, 0-based)
- RFCC / DHT/HMU / NGP / GAST = Level **1** (salah, harusnya 0) + 18 unit anaknya = Level **2** (salah, harusnya 1)

Akar: org Dev di-seed versi LAMA (1-based) pre-v21. Normalisasi handoff Step 5 (`IF NOT EXISTS(Level=0) UPDATE Level=Level-1`) **ke-skip** karena 4 Bagian baru sudah Level 0 → guard FALSE → UPDATE tak jalan. Seed `SeedOrganizationUnitsAsync` idempotent all-or-nothing (skip kalau tabel ada isi) → tak pernah benerin baris existing.

Dampak: tampilan tree masih benar (label dari depth), TAPI `OrganizationController` pakai kolom `Level` secara fungsional — child baru = `parent.Level+1` (tambah unit di RFCC jadi Level-2 "Sub-unit" bukan "Unit"); cascade-move cek `Level>=1`; `OrgLabelController:189` proteksi-hapus-label cek `u.Level`. Integritas data latent.

### F2 — ProtonData tab Status kosong (MEDIUM)
`/ProtonData/StatusData` → `[]`. Tabel Status = matriks (Unit × Track) dengan kolom centang Silabus/Guidance. Track = penghasil baris. Di Dev `ProtonTracks` = **0 baris** → loop `section→unit→track` tak pernah masuk inner → 0 baris → matriks kosong → HC tak bisa lihat gap silabus/guidance.

Akar: 6 Track (Panelman/Operator × Tahun 1-3) = master data resmi, di-seed **lewat migration** `CreateProtonTrackTable` (23 Feb 2026, Step 5 MERGE). Migration jalan **sekali**. Di Dev baris track hilang/kehapus entah kapan (cause tak dipastikan — out of scope). Tak ada UI buat track, tak ada self-heal → kosong permanen. Lokal masih punya 6 (terverifikasi `sqlcmd`).

ITHandoff **tidak** menyentuh ini (diff `SeedData.cs` & `ProtonDataController.StatusData` kosong; kerja Proton di sana = Phase 358 penanda exam, beda urusan).

---

## 2. Goals / Non-Goals

### Goals
- Org Level selalu konsisten 0-based-by-depth di SEMUA env, otomatis, tahan-banting.
- 6 ProtonTrack selalu ada di semua env, otomatis re-seed kalau hilang.
- Nol migration, nol perubahan UI, nol konflik dengan branch `ITHandoff`.
- Idempotent + aman jalan tiap startup (no-op kalau data sudah benar).

### Non-Goals
- SQL one-off manual ke DB Dev (ditolak — self-heal nutup Dev saat next deploy).
- Forensik "kenapa track kehapus di Dev" (di luar scope; self-heal bikin irrelevant).
- UI CRUD untuk ProtonTrack (track tetap master data fixed).
- Ubah view label-mapping (sudah depth-based, tidak terdampak).
- Deploy ke Dev/Prod (tanggung jawab IT; spec hasilkan handoff terpisah).
- Ubah seeding org units yang existing (`SeedOrganizationUnitsAsync` tetap apa adanya).
- **F2 tidak memperbaiki referensi `ProtonTrackId` lama yang dangling.** Re-seed hanya buat ulang baris master track (Id baru). Baris lain yang masih nunjuk Id track lama (`AssessmentSession.ProtonTrackId` nullable tanpa FK enforce — `AssessmentSession.cs:96`; silabus/assignment) TIDAK di-relink. Itu kondisi pre-existing (sudah dangling sejak track hilang) — fix tak memperburuk, tapi juga tak memperbaiki.

---

## 3. Design

Tambah **2 method self-heal** ke `Data/SeedData.cs`, dipanggil dari `InitializeAsync`. Pola sama dengan safety-net seed yang sudah ada. Tidak ada file/kelas baru.

### 3.1 Wiring — `InitializeAsync`

Urutan baru (yang **bold** = tambahan):

```
1. CreateRolesAsync
2. CreateAdminUserAsync
3. SeedOrganizationUnitsAsync            (existing)
4. NormalizeOrganizationLevelsAsync      ← F1 BARU (setelah org units pasti ada)
5. SeedOrganizationLevelLabelsAsync      (existing)
6. SeedProtonTracksAsync                 ← F2 BARU
```

Tiap method baru **self-contained**: bungkus `try/catch`, on error → `_logger.LogError` + `return` (TIDAK throw). Jadi kegagalan satu rutin tak nge-block rutin lain maupun startup. Ini selaras decision A (rapikan sebisanya + log, app tetap jalan) dan perilaku try/catch existing di `Program.cs:148-152`.

> **Logger (final):** `SeedData` static tanpa logger field. `InitializeAsync` ambil `ILogger<SeedData>` via `serviceProvider.GetRequiredService<ILogger<SeedData>>()`, lalu **lewatkan sebagai parameter** ke `NormalizeOrganizationLevelsAsync` dan `SeedProtonTracksAsync`. Bukan `Console.WriteLine` (biar log masuk pipeline standar + kelihatan di Prod).

> **Lingkup env (final):** seed jalan di **SEMUA environment termasuk Production** (`Program.cs:137`, tanpa `IsDevelopment()` guard) — **ini disengaja**, karena Prod punya data org legacy 1-based yang sama. **Tidak** ditambah env-guard (itu malah ngalahin tujuan self-heal). Mitigasi visibilitas: tiap mutasi di-log (jumlah baris diubah) supaya perubahan data Prod ketahuan di log startup.

### 3.2 F1 — `NormalizeOrganizationLevelsAsync(context, logger)`

Recompute `Level` dari **kedalaman tree** (jarak dari root), lalu UPDATE hanya baris yang beda.

Algoritma:
1. Load semua `OrganizationUnits` (Id, ParentId, Level) — **semua baris**, termasuk inactive (level = properti struktural, lepas dari IsActive).
2. Build map `parentId → children`.
3. BFS dari root (`ParentId == null`), set depth: root=0, anak=parent+1. Pakai `visited` set.
4. **Orphan/cycle handling (decision A):** baris yang tak terjangkau dari root manapun (ParentId nunjuk baris hilang, atau ada cycle) → **lewati, log `LogWarning`** dengan **count + daftar Id**. Jangan crash, jangan ubah. **Konsekuensi (eksplisit):** ini = *normalisasi parsial* — subtree yang terjangkau dirapikan, baris orphan dibiarkan apa adanya. Wajar (org orphan nyaris mustahil krn FK self-ref), tapi kalau log nunjuk orphan → flag di handoff IT buat remediasi manual.
5. Kumpulkan baris yang `computedDepth != Level`. Kalau kosong → no-op, return (skip transaction).
6. **Bungkus transaction:** `BeginTransactionAsync` → set Level in-memory baris-baris itu → `SaveChangesAsync` → `CommitAsync`. `catch (DbUpdateException)` → `RollbackAsync` + log. **Hanya ubah kolom Level. Tidak tambah/hapus baris.** (Atomik: kalau gagal di tengah, rollback — bukan setengah-normalisasi.)
7. Log `LogInformation`: jumlah baris dinormalisasi + jumlah orphan di-skip.

Idempotensi: run kedua → semua sudah match depth → 0 update → no-op (tak buka transaction). **Tak thrash tiap startup.**

**Target pasca-jalan = struktural, bukan angka tetap:** semua root → Level 0, semua anak → Level induk+1; dengan struktur 2-tingkat saat ini → tak ada Level ≥ 2. Distribusi akhir **tergantung jumlah data per-env**:
- **Dev:** `{0:8, 1:18}` (8 Bagian + 18 Unit yang ada di Dev).
- **Lokal:** `{0:4, 1:17}` (baseline seed — **sudah** 0-based → self-heal **no-op**, terverifikasi `sqlcmd`).
- **Prod:** angka beda lagi sesuai data Prod; aturan tetap (root 0, anak 1).

### 3.3 F2 — `SeedProtonTracksAsync(context, logger)`

Pastikan 6 track ada; insert yang hilang; jangan sentuh yang sudah ada.

Expected set (cocok persis migration `CreateProtonTrackTable` Step 5):

| TrackType | TahunKe | DisplayName | Urutan |
|---|---|---|---|
| Panelman | Tahun 1 | Panelman - Tahun 1 | 1 |
| Panelman | Tahun 2 | Panelman - Tahun 2 | 2 |
| Panelman | Tahun 3 | Panelman - Tahun 3 | 3 |
| Operator | Tahun 1 | Operator - Tahun 1 | 4 |
| Operator | Tahun 2 | Operator - Tahun 2 | 5 |
| Operator | Tahun 3 | Operator - Tahun 3 | 6 |

Algoritma:
1. Load existing keys `(TrackType, TahunKe)` dari `ProtonTracks`.
2. **Pre-check orphan (log saja):** hitung baris anak yang `ProtonTrackId` tak match track manapun (mis. `ProtonTrackId = 0` sisa default migration Step 9, atau nunjuk Id track yang sudah hilang) di `ProtonKompetensiList` + `ProtonTrackAssignment`. Kalau ada → `LogWarning` count (data orphan pre-existing — **tidak diperbaiki di sini**, lihat Non-Goals).
3. Untuk tiap expected yang **belum ada** key-nya → `Add`. Kalau kosong (6 sudah ada) → no-op, return.
4. **Bungkus transaction:** `BeginTransactionAsync` → `AddRange` track yang hilang → `SaveChangesAsync` → `CommitAsync`. `catch (DbUpdateException)` → `RollbackAsync` + log.
5. Log `LogInformation` jumlah track di-insert (0 = sudah lengkap).

Insert-if-missing (bukan overwrite) → preserve baris existing apa adanya. Cek key di app-level (langkah 1) = penjaga utama anti-duplikat; unique constraint `AK_ProtonTracks_TrackType_TahunKe` (migration L28 + `ApplicationDbContext.cs:339` `.IsUnique()`) = backstop. Identity `Id` di-generate DB → track yang di-reseed dapat **Id baru** (lihat Non-Goals: ref lama tak di-relink).

### 3.4 Error handling (decision A)

- Tiap method: `try { … } catch (Exception ex) { logger.LogError(ex, "…self-heal gagal, dilewati"); }` → return normal.
- App **selalu** lanjut start, walau self-heal gagal.
- Outer `Program.cs:148-152` tetap jadi jaring terakhir.

---

## 4. Testing (TDD — test dulu)

Pakai `HcPortal.Tests` (xUnit), DbContext in-memory/SQLite disposable seperti test existing (`OrgLabelMigrationIntegrationTests` pakai real-SQL disposable — ikuti pola itu untuk F1 karena butuh tree query).

### F1 — `NormalizeOrganizationLevelsTests`
1. **Split-brain → fixed:** seed 1 root Level 1 + 1 child Level 2 → run → assert root Level 0, child Level 1.
2. **Sudah benar → no-op:** seed root 0 / child 1 → run → tak berubah (assert sama).
3. **Mixed (skenario Dev):** 4 root L0 (no child) + 4 root L1 (each w/ child L2) → run → assert semua root 0, semua child 1.
4. **Idempotent:** run dua kali → hasil identik, run kedua 0 update.
5. **Orphan (decision A):** baris dengan ParentId nunjuk Id tak-ada → run → baris orphan tak diubah, tak throw, app lanjut (assert no exception + warning logged).
6. **Deep tree (>2 level):** root→child→grandchild → assert 0/1/2.
7. **Dev split-brain + double-run (anti-thrash):** seed skenario Dev (test 3) → run F1 → assert `{0:8,1:18}` → run F1 **lagi** → assert run kedua = **0 update** (no-op). Bukti self-heal tak nulis tiap startup di Prod.

### F2 — `SeedProtonTracksTests`
1. **Kosong → 6 ter-seed:** tracks kosong → run → assert 6 baris, DisplayName/Urutan sesuai tabel.
2. **Idempotent:** run lagi → tetap 6 (no dup, assert unique (TrackType,TahunKe)).
3. **Partial → lengkapi:** seed 2 dari 6 → run → assert 6 (4 ditambah, 2 existing tak diduplikat).
4. **Preserve existing:** existing row dgn DisplayName custom → run → DisplayName custom TIDAK ditimpa (insert-missing-only).
5. **Orphan tak korup:** seed child row (`ProtonKompetensiList`/`ProtonTrackAssignment`) dgn `ProtonTrackId` nunjuk Id tak-ada + tracks kosong → run → assert 6 track ter-seed, child orphan **tetap apa adanya** (tak crash, tak diubah), orphan ke-log warning.

Gate: `dotnet build` 0 error + `dotnet test` semua pass (existing + baru).

---

## 5. Verification (manual, lokal @5277)

- DB lokal sudah benar (org 0-based, 6 track) → self-heal harus **no-op** (cek log: "0 normalized, 0 inserted"). Konfirmasi tak ada perubahan data tak diinginkan.
- Skenario rusak buatan (SEED_WORKFLOW: snapshot → inject root Level 1 + delete tracks → restart app → cek Level kembali 0-based & 6 track balik → restore). Buktikan self-heal kerja end-to-end di runtime, bukan cuma unit test.
- ProtonData/Index tab Status: pasca track ada, matriks render (baris muncul, awal merah).

---

## 6. Rollout & Handoff

- Land di `main` (atomic commits per fix + test).
- Hasilkan handoff IT (`docs/DB_HANDOFF_IT_<tgl>.html` gaya existing): "deploy commit X ke Dev → **backup DB** → restart → self-heal auto-jalan saat startup". **Tanpa SQL manual** — cukup deploy + restart.
- **Query verifikasi post-restart** (IT jalankan, read-only):
  ```sql
  -- F1: harus mulai Level 0, tak ada Level >= 2 (Dev: {0:8, 1:18})
  SELECT Level, COUNT(*) FROM OrganizationUnits GROUP BY Level ORDER BY Level;
  -- F2: harus 6
  SELECT COUNT(*) AS Tracks FROM ProtonTracks;
  ```
  Plus cek UI: tab Status ProtonData render baris (awal merah).
- **Rollback:** kalau self-heal bikin efek tak diinginkan → **restore DB dari .bak** (backup pre-deploy). **Tidak ada migration buat di-downgrade** (fix murni kode seed). Revert code = checkout commit sebelumnya + restart. Eskalasi ke Rino.
- `ITHandoff` telan otomatis saat sync `main` (SeedData.cs tak disentuh ITHandoff → no konflik).

---

## 7. Risiko & Mitigasi

| Risiko | Mitigasi |
|---|---|
| F1 mutate Level tiap boot | Hanya UPDATE `WHERE Level != depth` → no-op saat bersih; hanya kolom Level; no add/delete; log jumlah |
| Self-heal gagal blokir startup | Tiap method try/catch + return; app selalu lanjut (decision A) |
| Orphan/cycle bikin loop tak henti | `visited` set di BFS; orphan di-skip + log |
| F2 duplikat track | Insert-if-missing by unique (TrackType,TahunKe); unique constraint DB sebagai backstop |
| Salah benerin data prod yang sengaja beda | Org: depth = sumber kebenaran struktural (tak ada kasus sah Level≠depth). Track: insert-missing-only, tak overwrite existing |
| F1/F2 gagal di tengah → DB inkonsisten | `BeginTransactionAsync`+Commit; gagal → `RollbackAsync` (atomik, bukan setengah jalan) |
| Anak track orphan (`ProtonTrackId=0`/Id hilang) sisa hapus track | Pre-check log (langkah F2-2); FK silabus=Cascade & assignment=Restrict → via EF tak bisa orphan, orphan cuma dari restore/raw SQL. F2 re-create master saja, ref lama tak di-relink (non-goal, sudah dangling sblm fix) |
| Mutasi data Prod tanpa approval eksplisit | Sengaja jalan semua env (Prod legacy 1-based sama); no-op kalau bersih; tiap mutasi di-log buat visibilitas. Backup pre-deploy = jaring rollback |
| Track hilang lagi pasca-seed | Tak ada code path hapus `ProtonTracks` (terverifikasi grep — no `.Remove`); penyebab kemungkinan restore DB. Self-heal jalan tiap boot → recurrence auto-koreksi saat restart berikut |

---

## 8. Open Questions

Tidak ada yang blocking. Semua decision sudah locked:
- F1 = self-heal kode (bukan SQL manual). ✓
- F2 = self-heal kode (pindah seed migration→SeedData). ✓
- Error policy = A (rapikan sebisanya + log, app lanjut). ✓

---

## 9. Adversarial Verification (5-lens workflow, 2026-06-10)

48 finding (33 CONFIRMED_OK, 9 MISS, 6 RISK). **Kunci: lens F1 Level-dependency = 12 cek, 0 miss, 0 risk** → normalisasi `Level` tak merusak behavior existing manapun (tree label depth-based; tak ada view yang "kebetulan benar" gara-gara Level salah).

**Diterima → masuk spec ini:** target struktural (bukan `{0:8,1:18}` tetap) §3.2; transaction wrapping F1/F2 §3.2/§3.3; logger final §3.1; orphan child-FK pre-check + non-goal ref-relink §2/§3.3; all-env Prod eksplisit §3.1; partial-norm clarification §3.2; test Dev-split-brain double-run + orphan §4; handoff queries+rollback §6; risk rows §7.

**Ditolak (over-engineering / tak relevan):** env-guard Prod (ngalahin tujuan self-heal); retry/backoff/health-endpoint (restart idempotent = recovery alami); pre-flight verify unique constraint (cek app-level cukup); full audit-log mechanism orphan (WARN log + flag handoff cukup).
