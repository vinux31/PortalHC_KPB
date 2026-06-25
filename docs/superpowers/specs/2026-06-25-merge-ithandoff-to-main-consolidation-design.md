# Rencana Detail — Konsolidasi `ITHandoff` → `main` (bundle v32.x)

**Tanggal:** 2026-06-25
**Penulis:** Rino (dibantu Claude)
**Status:** Rancangan disetujui (Keputusan 1 = Opsi A / integration branch). Keputusan 2 (rak kanonik IT) ditunda ke FASE 6.
**Tujuan:** Menggabungkan seluruh pekerjaan yang sudah selesai dari branch `ITHandoff` ke `main` sehingga kode yang dikirim ke IT **lengkap dan berfungsi**, tanpa ada yang terlewat atau error.

---

## 0. Ringkasan situasi (hasil analisa)

- **Bukan fast-forward.** Dua branch divergen dari base v31.0 (`64456bd5`). `main` +515 commit, `ITHandoff` +543 commit.
- **Kedua branch SIAP secara kerjaan:** semua milestone CLOSED + audited PASSED + tagged lokal, build **0-error** dua-duanya.
  - `main` (`10d3952b`): v32.0, v32.2, v32.5, v32.6, v32.9
  - `ITHandoff` (`89147e7d`): v32.1, v32.3, v32.4, v32.7, v32.8
- **Migration AMAN** (EF Core 8): 6 migration divergen, target disjoint, apply bersih urut timestamp di fresh DB. Snapshot auto-merge = union persis (terbukti). **Tidak perlu reconcile-migration.**
- **Effort nyata = resolusi konflik:** 6 file kode konflik (3 HIGH) + 8 file auto-merge-diam yang wajib dicek manual + 7 file bookkeeping.

### Daftar 6 migration (flag IT = migration=TRUE)

| Urut | Timestamp | Migration | Branch | Sentuh |
|---|---|---|---|---|
| 1 | 0618 045427 | `AddUserUnitsTable` (399) | ITHandoff | tabel baru `UserUnits` + backfill data |
| 2 | 0621 011101 | `AddParticipantRemovalColumns` (409) | main | `AssessmentSessions` +3 kolom |
| 3 | 0621 065918 | `AddRetakeColumnsAndArchive` (405) | ITHandoff | `AssessmentSessions` +3 kolom + tabel `AssessmentAttemptResponseArchives` |
| 4 | 0622 124217 | `AddAssessmentPackageSection` (415) | main | `PackageQuestions.SectionId` + tabel `AssessmentPackageSections` |
| 5 | 0623 103224 | `AddPackageNumberUniqueIndex` (422) | ITHandoff | renumber data + **unique index** `AssessmentPackages(SessionId,PackageNumber)` |
| 6 | 0624 133656 | `AddTokenVerifiedAt` (427) | ITHandoff | `AssessmentSessions.TokenVerifiedAt` |

Carry lama (sudah ada di base, tetap ingatkan IT): 360 PendingProtonBypass, 372 ShuffleToggles.
Dev DB baseline saat ini = `AddShuffleTogglesToAssessmentSession` (v31.0). **Semua 6 migration net-new buat IT.**

---

## FASE 0 — Persiapan & safety (sebelum sentuh apa pun)

```bash
# Jalankan dari worktree main: ".../Desktop/PortalHC_KPB"

# 0.1 Pastikan working tree bersih DUA worktree
git status                                   # main worktree: harus clean
git -C "../PortalHC_KPB-ITHandoff" status     # ITHandoff worktree: cek

# 0.2 Tag safety (jaring pengaman — kalau apa pun kacau, balik ke sini)
git tag backup/main-preV32merge main
git tag backup/ithandoff-preV32merge ITHandoff

# 0.3 Bersihin worktree nyasar (sisa sesi agent lama)
git worktree remove ".claude/worktrees/agent-acbf6f4ad8d22ccc0" --force
git worktree remove ".claude/worktrees/pensive-saha-4b1351" --force
git worktree prune
git worktree list                            # sisakan: main + PortalHC_KPB-ITHandoff

# 0.4 Bersihin untracked di ITHandoff (2 jpeg, 1 xlsx, dir docs/akun-multirole)
#     -> putuskan: gitignore atau hapus. JANGAN commit materi exam (xlsx) ke repo.
git -C "../PortalHC_KPB-ITHandoff" status --untracked-files=all
```

**Pra-kondisi build (sudah diverifikasi):** main build 0-err (28 warn pre-existing), ITHandoff build 0-err (25 warn pre-existing).

**Gerbang FASE 0:** dua working tree bersih + 2 tag safety ada + worktree nyasar bersih.

---

## FASE 1 — Bikin integration branch + mulai merge

```bash
# 1.1 Buat fotokopi (integration branch) dari main — main asli tetap aman
git switch -c integrate/v32-consolidation main

# 1.2 Mulai merge ITHandoff (akan KONFLIK — itu diharapkan)
git merge --no-ff ITHandoff
# -> git berhenti dengan daftar file CONFLICT. Jangan panik, lanjut FASE 2.

# Cek daftar konflik kapan saja:
git status --short | grep -E '^(UU|AA|U|A)'
```

> Kalau di tengah jalan mau batal total: `git merge --abort` → integration branch balik bersih, `main` asli **tidak tersentuh sama sekali**.

---

## FASE 2 — Resolusi konflik (KEEP BOTH — jangan pilih satu sisi)

Prinsip umum: kedua branch nambah FITUR berbeda di permukaan yang sama. Hampir selalu jawabannya **pertahankan dua-duanya**, bukan `ours`/`theirs`. `ours` = main, `theirs` = ITHandoff.

### 2A. `Controllers/AssessmentAdminController.cs` — HIGH (file paling berat)

1. **Helper sync Pre→Post duplikat** — `main` punya `SyncToPostIfSamePackageAsync`, `ITHandoff` punya `SyncToLinkedPostIfSamePackageAsync`. **Buang versi main, pakai versi ITHandoff** (kanonik v32.7, sekalian tutup bocor Import). Rewire SEMUA call-site (di akhir `Create/Edit/DeleteQuestion`) ke helper yang bertahan, pakai argumen session-id (`parentPkg.AssessmentSessionId`).
2. **Awal `CreateQuestion`/`EditQuestion`** — pasang BERURUTAN: guard lock-reject `SessionEditLockRules` (ITHandoff) **dan** guard Section-IDOR belongs-to-package (main).
3. **Pertahankan dua-duanya:** region Section CRUD + blok ET-warning + `ResolveCorrectness` (main) **dan** endpoint `UpdateRetakeSettings` + `ToggleSamePackage` (ITHandoff). Mereka method terpisah → taruh berdampingan.
4. **Inheritance add-participant:** pakai blok `savedAssessment.*` (ITHandoff, bawa kolom retake) **tapi** tambahin balik safety `AssessmentType ?? "Standard"` (main, anti NOT-NULL).
5. Setelah file ini beres: `dotnet build` + jalankan xUnit suite.

### 2B. `Controllers/CMPController.cs` — MED (StartExam status-gate)

- Pertahankan guard `IsParticipantRemoved` (main, security ortogonal).
- Adopsi logika status idempotent display-only (ITHandoff): variabel `nowWib` + **buang** blok persisted `Upcoming→Open` `SaveChanges` (main).
- Urutan gate (per analisa): cek Completed → **GRDF-01 Pre→Post gate** → token-gate → status display.
- Pastikan kode setelah marker (time-gate yang pakai `nowWib`) compile dengan variabel terdefinisi.

### 2C. `Views/Admin/AssessmentMonitoringDetail.cshtml` — LOW

- Concat dua blok `<script>` (independen): blok live-monitoring expose (`window.monBuildActionsHtml` dll, main) + handler `btn-riwayat-percobaan` modal riwayat (ITHandoff). Taruh berurutan dalam section script yang sama.

### 2D. `Views/Admin/CreateWorker.cshtml` — MED

- Ambil sisi **ITHandoff** untuk dua hunk (container multi-unit `#unitMultiContainer` + script `initSectionUnitMultiCascade`).
- **Sebelum finalisasi:** pastikan `CreateWorkerViewModel` punya `Units`/`PrimaryUnit`, controller GET set `ViewBag.SectionUnitsJson`, POST bind `Units`/`PrimaryUnit` (datang dari controller/model auto-merge — verifikasi ikut masuk).

### 2E. `Views/Admin/ManagePackageQuestions.cshtml` — HIGH

- **Hunk 1 (additive):** render banner lock (ITHandoff) **dan** panel "Kelola Section" (main) berurutan di atas. Pertahankan deklarasi `bool isLocked = ViewBag.IsSamePackageLocked == true;`.
- **Hunk 2 (hand-weave):** mulai dari struktur baris ter-group-Section (main), lalu **graft** logika per-baris `isLocked` (ITHandoff) ke tombol aksi tiap baris: disable Edit/Delete + judul "terkunci" saat `isLocked`, Preview tetap aktif. Bukan pilih-sisi — anyam markup baris.

### 2F. `Views/CMP/Results.cshtml` — HIGH

- **Basis = `@switch (Model.RetakeMode)`** (ITHandoff, tiering leak-safe: ShowFullReview/ShowWrongFlagsOnly/ShowScoreOnly).
- **Lipat** nota admin-bypass Phase 414 (main) ke tier `ShowFullReview` — render saat `CanReviewAnswers && !AllowAnswerReview`.
- **Hunk kedua:** rekonsiliasi cabang `else if (!Model.CanReviewAnswers)` (main) vs `else if (Model.CanRetake)` (ITHandoff) jadi satu rantai yang nangani dua kasus (no-review DAN retake-available).

### 2G. File bookkeeping (7) — trivial

`.planning/MILESTONES.md`, `PROJECT.md`, `RETROSPECTIVE.md`, `ROADMAP.md`, `STATE.md`, `todos/completed/2026-06-11-...md`, `docs/SEED_JOURNAL.md`.
→ Bukan kode. Reconcile jadi superset (union entri kedua sisi). Untuk `STATE.md`/`PROJECT.md` ambil sisi `main` (lebih baru) lalu set manual ke state pasca-merge. Untuk `SEED_JOURNAL.md` **simpan entri dua sisi** biar audit-trail seed utuh.

**Gerbang FASE 2:** `git status` tidak ada lagi marker konflik; semua `<<<<<<<`/`=======`/`>>>>>>>` hilang.

---

## FASE 3 — Verifikasi migration & snapshot (kritis)

8 file auto-merge-diam berikut **tidak** di-flag konflik oleh git — wajib dicek tangan:

| File | Cek manual |
|---|---|
| `Migrations/ApplicationDbContextModelSnapshot.cs` | **Terima auto-merge, JANGAN edit tangan.** Dibuktikan via `has-pending-model-changes` (3.x). |
| `Data/ApplicationDbContext.cs` | 3 DbSet baru ada (`AssessmentPackageSections`, `UserUnits`, `AssessmentAttemptResponseArchives`). Grep `OnModelCreating`: blok `modelBuilder.Entity<>()` tiap sisi ada **tepat sekali** (unique idx Section, config UserUnit, filtered idx PackageNumber). |
| `Models/AssessmentSession.cs` | Tepat **SATU** properti `Status` (no CS0102). 7 properti baru masing-masing sekali. |
| `Models/AssessmentResultsViewModel.cs` | `Results.cshtml` pakai `CanReviewAnswers` (main) **dan** `RetakeMode`/`CanRetake` (ITHandoff); controller set dua-duanya. |
| `Program.cs` | `AddScoped<InjectAssessmentService>` (main) + `AddScoped<RetakeService>` (ITHandoff) ada masing-masing sekali; app start (DI resolve). |
| `Controllers/TrainingAdminController.cs` | `BulkBackfill` hilang (tak ada orphan view/route) + helper cert ada (`CertIssuanceRules.ResemblesAutoCertFormat`, `ManualEntryRules.PassStatusMismatch`, `CertNumberHelper.IsDuplicateKeyException`). |
| `Views/Admin/EditAssessment.cshtml` | Render: toggle retake buka MaxAttempts/Cooldown; checkbox shuffle bind; form hapus-peserta jalan. |
| `tests/e2e/helpers/wizardSelectors.ts` | Low-risk. Jalankan Playwright wizard/option spec; `#creationMode` resolve. |

```bash
# 3.1 Build (bukti entity + snapshot compile bareng)
dotnet build                                  # WAJIB 0 error

# 3.2 BUKTI snapshot = model (no-op test, EF8)
dotnet ef migrations has-pending-model-changes
#  -> WAJIB "No changes...". Kalau muncul ops AddColumn/CreateTable = ada yg ke-drop saat merge:
#     regen via `dotnet ef migrations add MergeReconcile` lalu pastikan Up() KOSONG sebelum commit.

# 3.3 Urutan apply (harus 6 migration urut timestamp)
dotnet ef migrations list

# 3.4 Bukti apply bersih di scratch DB (BUKAN Dev/Prod)
dotnet ef database update --connection "Server=(localdb)\MSSQLLocalDB;Database=MergeVerify;Trusted_Connection=True;TrustServerCertificate=True"
#  -> selesai tanpa error; tiap migration ter-log urut 0618 -> 0624

# 3.5 (opsional, terkuat) generate skrip idempotent buat di-review mata
dotnet ef migrations script --idempotent -o ./merge_verify.sql

# 3.6 buang scratch DB kalau sudah
```

**Gerbang FASE 3:** build 0-err + `has-pending-model-changes` kosong + `database update` scratch sukses.

---

## FASE 4 — Test + UAT (verifikasi lokal per DEV_WORKFLOW)

```bash
# 4.1 Full xUnit suite — WAJIB hijau
dotnet test

# 4.2 Playwright e2e (lifecycle assessment + wizard)
#     butuh AD-off:
Authentication__UseActiveDirectory=false dotnet run --urls http://localhost:5277
#     lalu jalankan suite e2e di terminal lain
```

**4.3 UAT manual @ http://localhost:5277** — golden-path lintas-fitur (titik tabrakan dua stream):
- Ambil ujian → submit → grade → cert (lifecycle dasar).
- Retake: peserta gagal → ujian ulang → tier review leak-safe.
- Section: paket ber-section + pagination + shuffle scoped.
- Multi-unit: buat worker multi-unit + cascade section.
- SamePackage Pre→Post: edit soal Pre tersinkron ke Post + lock Post-Test.
- Exam security: token-verified + StartExam idempotent (refresh GET tak ubah status).

**Gerbang FASE 4:** xUnit hijau + e2e hijau + UAT manual semua skenario lolos.

---

## FASE 5 — Promosi ke `main` + tag

```bash
# Hanya kalau SEMUA gerbang FASE 2-4 hijau
git switch main
git merge --ff-only integrate/v32-consolidation     # main maju ke hasil terverifikasi
git tag v32-consolidated                            # penanda bundle
git branch -d integrate/v32-consolidation           # fotokopi selesai, hapus
```

> `--ff-only` memastikan `main` cuma maju ke commit yang sudah lo verifikasi — kalau gagal ff (mestinya nggak, karena integration dibuat dari main), berarti ada yang aneh, stop & cek.

**Gerbang FASE 5:** `main` = superset terverifikasi, tag dibuat.

---

## FASE 6 — Push + notify IT (Keputusan 2 di sini)

> **Keputusan 2 (belum dijawab):** IT selama ini narik dari rak `main` atau `ITHandoff`?
> - (a) IT ambil `main` → `git push origin main` + push tag. Rak `ITHandoff` jadi arsip.
> - (b) IT ambil `ITHandoff` → sinkron dua rak: `git branch -f ITHandoff main` lalu push `origin main` + `origin ITHandoff` biar isinya identik.
>
> **Push & promosi Dev/Prod = tanggung jawab Team IT** (CLAUDE.md). Developer cuma push branch repo + notify.

**Notifikasi IT wajib memuat:**
- Commit hash final (`v32-consolidated`).
- **migration=TRUE, 6 migration** (apply urut via `dotnet ef database update`, jangan SQL manual): `AddUserUnitsTable` (399), `AddRetakeColumnsAndArchive` (405), `AddParticipantRemovalColumns` (409), `AddAssessmentPackageSection` (415), `AddPackageNumberUniqueIndex` (422), `AddTokenVerifiedAt` (427). + carry 360/372.
- **DB-handoff doc** dari `docs/templates/DB_HANDOFF_IT.template.md`.
- **IT WAJIB backup pre-migration** (`scripts/backup-dev-pre-migration.ps1`) — SOP Pre-Deploy Backup.
- ⚠️ **Warning 422 (`AddPackageNumberUniqueIndex` = unique index):** cek duplikat di Dev SEBELUM apply:
  ```sql
  SELECT AssessmentSessionId, PackageNumber, COUNT(*) c
  FROM AssessmentPackages GROUP BY AssessmentSessionId, PackageNumber HAVING COUNT(*) > 1;
  ```
  Migration auto-renumber duplikat, tapi backup dulu biar aman.
- 2 migration data-mutating (`AddUserUnitsTable` backfill, `AddPackageNumberUniqueIndex` renumber) — cek `SELECT COUNT(*) FROM UserUnits` ≈ jumlah user ber-Unit pasca-apply.

---

## Rencana rollback

| Titik gagal | Aksi |
|---|---|
| Saat merge (FASE 1-2) | `git merge --abort` → integration branch bersih, `main` tak tersentuh |
| Setelah resolve, sebelum ff (FASE 3-4 gagal) | `git switch main` + `git branch -D integrate/v32-consolidation` → buang fotokopi |
| Setelah ff ke main (FASE 5), sebelum push | `git reset --hard backup/main-preV32merge` → main balik |
| Setelah push (FASE 6) | revert commit merge + koordinasi IT (hindari — verifikasi dulu) |

Tag `backup/main-preV32merge` + `backup/ithandoff-preV32merge` = jaring pengaman utama.

---

## Checklist ringkas

- [ ] FASE 0: tree bersih ×2, tag safety ×2, worktree nyasar bersih
- [ ] FASE 1: integration branch dibuat, `git merge --no-ff ITHandoff` jalan
- [ ] FASE 2: 6 file kode resolve (KEEP BOTH) + 7 bookkeeping
- [ ] FASE 3: 8 file auto-merge dicek + build 0-err + `has-pending-model-changes` kosong + scratch DB apply
- [ ] FASE 4: xUnit hijau + e2e hijau + UAT manual 6 skenario
- [ ] FASE 5: ff `main` + tag `v32-consolidated`
- [ ] FASE 6: (Keputusan 2) push + notify IT (6 migration + warning 422)

**Estimasi effort:** sesi kerja serius (bukan instan). 3 file HIGH (`AssessmentAdminController`, `ManagePackageQuestions` hunk2, `Results.cshtml`) butuh anyam tangan + verifikasi. Risiko teknis terbesar (migration) sudah terbukti aman.
