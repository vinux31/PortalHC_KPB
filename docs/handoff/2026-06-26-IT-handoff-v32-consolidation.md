# IT Handoff — Konsolidasi v32 (ITHandoff → main)

**Tanggal:** 2026-06-26
**Dari:** Rino (developer)
**Untuk:** Team IT (deploy Dev → Prod)
**Status kode:** sudah di-push ke GitHub origin.

---

## Pembaruan 2026-06-30 (pasca-konsolidasi, **code-only, migration = FALSE**)

Setelah handoff konsolidasi ini, ada **3 commit tambahan code-only** di atas `bcea1664` (HEAD sekarang `0c9a0d51`). **TIDAK ada migration baru** — daftar 6 migration di §2 TETAP, prosedur deploy IDENTIK.

| Commit | Isi | Sifat |
|---|---|---|
| `1fe9e69a` | **Fix bug produk:** buat soal *Multiple Answer* lewat form gagal ("butuh min. 2 jawaban benar" walau sudah dicentang ≥2). Akar: `applyQTypeSwitch` tak panggil `reletterRows()` → `name` tetap `correctIndex` (harusnya `options[i].IsCorrect`). Fix di `Views/Admin/ManagePackageQuestions.cshtml`. Pre-existing Phase 418 (v32.6), bukan regresi merge. | **Bug produk** — perlu deploy |
| `7f867619` | Perbaikan e2e test-debt (selector/teks) | Test saja |
| `0c9a0d51` | Tuntaskan 6 e2e deferred (semua verified hijau) + quarantine blok stale Phase 307 panel (test.fixme) | Test saja |

**Yang IT perlu tahu:** tarik `main` HEAD `0c9a0d51`. Karena nol migration baru, cukup **build + deploy kode** (tidak perlu `dotnet ef database update` ulang jika 6 migration konsolidasi sudah ter-apply). Bug pembuatan soal Multiple Answer hilang setelah deploy.

---

## 1. Perubahan rak pull (PENTING)

Mulai handoff ini, **tarik kode dari branch `main`**, bukan `ITHandoff` lagi.

- `main` sekarang = superset terkonsolidasi. Berisi semua milestone v32:
  - dari main: v32.0 / v32.2 / v32.5 / v32.6 / v32.9
  - di-merge dari ITHandoff: v32.1 / v32.3 / v32.4 / v32.7 / v32.8
- Branch `ITHandoff` jadi **arsip** (jangan dipakai lagi untuk deploy).

| Item | Nilai |
|---|---|
| Branch | `main` |
| Commit HEAD | `0c9a0d51` (3 commit code-only di atas `bcea1664` — lihat **Pembaruan 2026-06-30**; migration tetap 6, nol baru) |
| Tag rilis | `v32-consolidated` (di `9fa729f4`; HEAD lebih baru, lihat catatan) |
| Repo | `https://github.com/vinux31/PortalHC_KPB.git` |

Verifikasi setelah pull: `git rev-parse HEAD` harus `0c9a0d51...` (lihat Pembaruan 2026-06-30).

> **Catatan:** setelah merge, audit multi-agent atas 19 seam hasil-resolve-tangan menemukan **8 bug integrasi lintas-fitur merge-only** (kombinasi v32.1/3/4/7/8 × v32.0/2/5/6/9 yang tak teruji per-branch). Semua sudah di-fix (commit `bcea1664`), terverifikasi build + xUnit 1004/0/2 + UAT live @5277. **Code-only — TIDAK ada migration baru.** Daftar migration di bawah TETAP 6.

---

## 2. Migration: **migration = TRUE**

Ada **6 migration baru** sejak v31.0. Apply pakai EF, **JANGAN jalankan SQL manual**:

```bash
dotnet ef database update
```

EF meng-apply otomatis berurutan berdasarkan timestamp (bukan nomor fase). Daftar (urutan apply sesuai timestamp):

| # | Timestamp / Migration | Fase | Isi singkat |
|---|---|---|---|
| 1 | `20260618045427_AddUserUnitsTable` | 399 | Tabel `UserUnits` (akun multi-unit dalam 1 bagian) |
| 2 | `20260621011101_AddParticipantRemovalColumns` | 409 | Kolom soft-remove peserta (add/remove fleksibel) |
| 3 | `20260621065918_AddRetakeColumnsAndArchive` | 405 | Kolom retake/attempt + arsip riwayat ujian ulang |
| 4 | `20260622124217_AddAssessmentPackageSection` | 415 | Kolom Section pada paket assessment |
| 5 | `20260623103224_AddPackageNumberUniqueIndex` | 422 | **Lihat §3 — unique index + auto-dedup data** |
| 6 | `20260624133656_AddTokenVerifiedAt` | 427 | Kolom `TokenVerifiedAt` (hardening keamanan ujian) |

---

## 3. ⚠️ Khusus migration #5 (`AddPackageNumberUniqueIndex`, fase 422)

Migration ini **mengubah data existing**, bukan cuma skema:

- **STEP 1** — auto-renumber `PackageNumber` per session via `ROW_NUMBER()` (gap-free 1..N), untuk membersihkan duplikat lama (bug penomoran count-based + delete tak renumber). **Idempotent** (re-run = hasil sama).
- **STEP 2** — bikin UNIQUE index `IX_AssessmentPackages_SessionId_PackageNumber_Unique`.

Implikasi:

- ✅ **Tidak perlu cek/perbaiki duplikat manual** — STEP 1 sudah menanganinya. Migration tidak akan gagal karena duplikat.
- ⚠️ Renumber **permanen** (`Down` tidak me-revert renumber). Nomor paket yang tampil pada session lama bisa berubah.
- ⚠️ **WAJIB backup DB sebelum apply** (standar, plus karena ada mutasi data permanen).

Backup SQL Server sebelum migrate:

```sql
BACKUP DATABASE [HcPortalDB_Dev]
TO DISK = N'D:\backup\HcPortalDB_Dev_preV32_20260626.bak'
WITH FORMAT, INIT, NAME = N'pre-v32-consolidation';
```

---

## 4. Urutan deploy (tanggung jawab IT)

1. Backup DB Dev (§3).
2. Pull `main` (`9fa729f4`) ke server Dev.
3. `dotnet ef database update` di Dev.
4. Build + deploy aplikasi ke Dev (`http://10.55.3.3/KPB-PortalHC`).
5. UAT/smoke di Dev.
6. Setelah Dev OK → ulang ke Prod (backup → migrate → deploy).

❌ Jangan edit kode/DB langsung di server. Semua via branch `main` + EF migration.

---

## 5. Verifikasi pasca-deploy (saran)

- Login + buka Kelola Data (assessment list) — render normal.
- Cek fitur baru: inject assessment, retake, add/remove peserta, Section pada paket, multi-unit akun.
- Cek tidak ada error migration di log saat startup.

---

## 6. Ringkasan kualitas (sudah diverifikasi lokal sebelum push)

- `dotnet build` 0 error.
- `dotnet ef migrations has-pending-model-changes` → no-op (snapshot = union 6 migration, nol drift).
- Scratch-DB apply bersih berurutan.
- xUnit 1004 pass / 0 fail / 2 skip.
- E2E 55-spec: nol regresi produk (44 clean + 11 test-debt non-blocker).
- Audit 4-reviewer PASSED.
- 2 bug integrasi lintas-fitur (inject-backdate clobber, double-sync Import) ditemukan + di-fix saat merge.

Detail merge: `docs/superpowers/specs/2026-06-25-merge-ithandoff-to-main-consolidation-design.md`.

---

**Rollback (kalau perlu):** tag `backup/main-preV32merge` (kode) + restore `.bak` (DB).
