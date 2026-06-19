# Phase 398: Test + UAT "seakan online" (INJ-13) - Research

**Researched:** 2026-06-18
**Domain:** Validation / E2E testing (Playwright + xUnit) — milestone-closing verification phase v32.2 inject
**Confidence:** HIGH (semua surface code + test infra di-grep langsung dari codebase; 0 dependensi eksternal baru)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** E2E otomatis SAJA — Phase 398 menghasilkan Playwright spec konsolidasi yang repeatable. **TIDAK ada human UAT browser terpisah** untuk 398; bukti "mata manusia" sudah terpenuhi oleh per-phase UAT 394 (7/8), 395 (live), 396 (5/5), 397 (9/9). 398 = automation + regression + audit milestone.
- **D-02:** Verifikasi **4 surface downstream WAJIB** (semua di-assert di E2E):
  - (a) `/CMP/Records` (+ `RecordsWorkerDetail`) menampilkan baris sesi inject berlabel **"Assessment Online"** (otomatis via `WorkerDataService.GetUnifiedRecords` yang tak filter `IsManualEntry` — spec §2.2).
  - (b) `/CMP/Results` menampilkan **rincian jawaban per-soal benar/salah** (butuh `ShuffledQuestionIds` + `PackageUserResponses` + anchor paket — spec §2.3).
  - (c) **Breakdown elemen teknis** tampil di Results (`SessionElemenTeknisScore` / `ElemenTeknisScores`).
  - (d) **Sertifikat PDF dapat diunduh** (untuk skenario cert auto/manual).
- **D-03:** **Side-by-side parity** — assert sesi inject **TAK bisa dibedakan** dari sesi online asli di view yang sama. Dalam ≥1 skenario, sandingkan 1 sesi inject + 1 sesi online asli (struktur baris Records + render Results identik). Bukti load-bearing INJ-13 ("bagi pekerja tak bisa dibedakan", spec §1).
- **D-04:** Cakupan **representatif** (bukan full cartesian): tiap mode isi-jawaban diuji **1x tembus** inject→Records→Results→cert: **Form** (395), **Auto-generate** (395), **Excel** (396); **WAJIB sertakan soal Essay** (risiko §13: `Status=Completed` bukan "Menunggu Penilaian", + rincian per-soal essay tampil); **+1 skenario Pre/Post linked** (silang inject↔online — 397). Target ~4-5 skenario E2E. Soal MC/MA tercakup di dalam skenario tsb.
- **D-05:** Regresi = **(i)** full suite (`dotnet test` unit + integration) hijau **+ (ii)** **live online-path E2E** (create assessment online → pekerja ambil → grade → cert) sebagai bukti jalur online asli tetap utuh berdampingan dengan inject. Reuse spec online existing bila memungkinkan.
- **D-06:** **Phase 398 menjalankan `/gsd-audit-milestone`** (traceability INJ-01..INJ-13 = 13/13) sebagai bagian penutup phase — bukan ditunda ke command terpisah.

### Claude's Discretion
- Struktur file: spec baru (mis. `tests/e2e/inject-seakan-online-398.spec.ts`) vs perluas existing — putuskan. Reuse helper `accounts.ts`/`dbSnapshot.ts`.
- Cara implement side-by-side parity (D-03): SQL fixture seed sesi online pembanding vs query struktur render — discretion.
- Urutan & granularitas skenario; pemilihan spec online mana yang dipakai untuk regresi jalur online (D-05).
- **0 migration** dipertahankan. Konfirmasi via `dotnet ef migrations add _verify` → 0 diff atau `git diff Migrations/` kosong.
- Semua skenario yang menulis DB WAJIB snapshot→restore per CLAUDE.md Seed Workflow; catat di `docs/SEED_JOURNAL.md` + tandai CLEANED. Playwright dari MAIN tree, AD-off (`Authentication__UseActiveDirectory=false`), `--workers=1`.

### Deferred Ideas (OUT OF SCOPE)
- Tidak ada scope creep — semua tetap di domain test/verifikasi INJ-13.
- One-time cleanup data test/audit lokal pasca-367 — REVIEWED, **tidak di-fold** (cleanup data sisa 367, di luar scope INJ-13; 398 self-clean via snapshot/restore).
- Fitur baru, edit-massal sesi inject, perubahan engine grading/authoring (final di 393-397).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| INJ-13 | Hasil inject **terverifikasi identik online** end-to-end — muncul di `/CMP/Records` (label "Assessment Online"), rincian jawaban per-soal benar/salah + elemen teknis di `/CMP/Results`, sertifikat dapat di-download — dikunci E2E + regression test + audit milestone. | Semua 4 surface ter-grep + dikonfirmasi otomatis-via-data (Records §2.2, Results §2.3, ElemenTeknis CMPController.cs:2318-2345, Cert CMPController.cs:1835/1943). Test infra (Playwright + dbSnapshot helper + exceljs) sudah lengkap dari 395/396/397. GAP yang 398 isi = **downstream surfaces + side-by-side parity + consolidated milestone proof**, BUKAN re-test commit (sudah di 395/396/397). [VERIFIED: codebase grep] |

> **Catatan:** init `phase-op 398` mengembalikan requirement null; INJ-13 di-map ke Phase 398 di `.planning/REQUIREMENTS.md:79` + ROADMAP.md:220 — diperlakukan sebagai requirement phase ini.
</phase_requirements>

## Summary

Phase 398 adalah fase **verifikasi penutup milestone v32.2**, bukan fase fitur. Pekerjaannya adalah **membuktikan** (lewat Playwright + full xUnit suite + audit milestone) bahwa sesi inject yang dibangun di 393-397 tampil **identik dengan assessment online** di 4 surface downstream (Records / Results per-soal / breakdown elemen teknis / sertifikat PDF), bahwa jalur online tidak ter-regresi, dan bahwa seluruh requirement INJ-01..13 ter-trace 13/13.

Temuan utama: **semua kode surface sudah ada dan sudah benar** — visibility Records gratis karena `WorkerDataService.GetUnifiedRecords` (Services/WorkerDataService.cs:28-62) tak memfilter `IsManualEntry` dan melabel semua sesi `Status IN (Completed, PendingGrading)` sebagai `"Assessment Online"`; render per-soal + elemen teknis di `CMPController.Results` (Controllers/CMPController.cs:2184-2366) di-gate hanya pada `UserPackageAssignment` + `PackageUserResponses` + `AllowAnswerReview`, yang ketiganya diproduksi inject (Phase 393); cert PDF lewat endpoint yang sama (`Certificate`:1835 / `CertificatePdf`:1943). Test infra **lengkap**: helper `tests/helpers/accounts.ts` + `tests/helpers/dbSnapshot.ts` (backup/restore/queryScalar/queryString/execScript), `exceljs` devDep, pola `test.describe.configure({mode:'serial'})` + snapshot-beforeAll/restore-afterAll sudah dipakai di 4 spec inject (394/395/396/397).

**GAP yang 398 isi (jangan duplikasi):** spec 395/396/397 sudah membuktikan **commit** (inject→DB score benar, anti silent-grade-0). Yang BELUM diuji end-to-end di satu spec konsolidasi adalah **navigasi pasca-commit ke surface pekerja**: render aktual `/CMP/Records` (baris + label), render aktual `/CMP/Results/{id}` (per-soal benar/salah + breakdown elemen teknis), download cert PDF nyata, dan **side-by-side parity** (D-03) sesi inject vs sesi online asli. Plus regresi online-path live (D-05) + audit milestone (D-06).

**Primary recommendation:** Buat 1 spec baru `tests/e2e/inject-seakan-online-398.spec.ts` (`mode:'serial'`, snapshot/restore), reuse helper existing, ~4-5 skenario per D-04, masing-masing navigasi pasca-commit ke 4 surface + 1 skenario side-by-side parity. Untuk D-05, **jalankan kembali** (bukan tulis ulang) spec online existing yang menggerakkan create→take→grade→cert: **`tests/e2e/exam-types.spec.ts` FLOW K/L/M** (paling lengkap: MA/Essay/Mixed full-cycle) + opsional `exam-taking.spec.ts` Flow A (MC + cert + monitoring). Tutup fase dengan `/gsd-audit-milestone v32.2`.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Drive inject wizard (commit) | E2E (Playwright UI) | API/Backend (InjectAssessmentService) | UI integration test; service sudah unit-tested |
| Render baris Records "Assessment Online" | API/Backend (WorkerDataService) | View (Views/CMP/Records.cshtml) | Label di-set server-side di GetUnifiedRecords; E2E assert DOM |
| Render per-soal + elemen teknis Results | API/Backend (CMPController.Results) | View (Views/CMP/Results.cshtml) | Gating logic server-side; E2E assert rendered review items |
| Cert PDF download | API/Backend (CMPController.CertificatePdf) | — | QuestPDF generation server-side; E2E assert download event + bytes |
| Side-by-side parity (D-03) | E2E (assertion) | DB fixture (seed online sibling) | Test-only concern; compare 2 sessions in same view |
| Online-path regression (D-05) | E2E (rerun existing specs) | xUnit (full suite) | No new code; reuse + run |
| 0-migration gate | Build/CI (git diff / ef) | — | Verification gate, no runtime tier |
| Audit milestone (D-06) | GSD tooling (/gsd-audit-milestone) | Docs (MILESTONE-AUDIT.md) | Aggregates VERIFICATION.md across phases |

## Standard Stack

### Core (semua sudah terpasang — 0 instalasi baru)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| @playwright/test | ^1.58.2 | E2E browser automation | Stack e2e proyek (tests/package.json) [VERIFIED: tests/package.json] |
| exceljs | ^4.4.0 | Bangun file .xlsx upload FRESH untuk skenario Excel | devDep, dipakai inject-excel-396.spec.ts (readFile incompat ClosedXML → tulis fresh) [VERIFIED: tests/package.json + inject-excel-396.spec.ts:22] |
| xUnit (HcPortal.Tests) | — | Full regression suite (501 `[Fact]`/`[Theory]`) | Test project proyek (HcPortal.Tests.csproj) [VERIFIED: grep count 501] |
| sqlcmd | (system) | BACKUP/RESTORE/query via dbSnapshot helper | `-S localhost\SQLEXPRESS -E -C -I -b` [VERIFIED: tests/helpers/dbSnapshot.ts] |

### Supporting (helper test existing — reuse, jangan tulis ulang)
| Helper | Path | Purpose | When to Use |
|--------|------|---------|-------------|
| accounts | `tests/helpers/accounts.ts` | Kredensial admin/hc/coachee (admin@pertamina.com / 123456) | Semua skenario login [VERIFIED] |
| dbSnapshot | `tests/helpers/dbSnapshot.ts` | `backup(path)` / `restore(path)` / `queryScalar(sql)` / `queryString(sql)` / `execScript(sqlPath)` | beforeAll snapshot + afterAll restore + assert DB scalar [VERIFIED] |
| wizardSelectors | `tests/helpers/wizardSelectors.ts` | Selector wizard CreateAssessment (online path) | Reuse bila tulis skenario online manual (lebih baik: rerun spec existing) [VERIFIED: ls] |
| examTypes | `tests/helpers/examTypes.ts` | Helper flow exam online (39KB) | Dipakai exam-types.spec.ts FLOW K/L/M [VERIFIED: ls] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Spec baru 398 konsolidasi | Perluas inject-395/396/397 | Spec existing sudah fokus "commit"; menambah surface-nav membuat mereka gemuk + redundan. Spec 398 baru = pembacaan jelas "downstream parity" (REKOMENDASI). |
| Rerun exam-types.spec.ts (D-05) | Tulis spec online baru | Spec existing sudah teruji + lengkap (K/L/M MA/Essay/Mixed). Tulis baru = duplikasi + risiko drift. **Rerun yang ada.** |
| SQL fixture seed online sibling (D-03) | Drive online exam via UI penuh untuk pembanding | UI penuh lambat + flaky. Pola seed-online-via-execScript sudah dipakai inject-397 (`seedOnlinePostRoom`) — REKOMENDASI untuk D-03. |

**Installation:** Tidak ada. Semua dependency terpasang.

**Version verification:** `@playwright/test ^1.58.2`, `exceljs ^4.4.0`, `typescript ^5.9.3` — dari `tests/package.json` (di-Read langsung). Tidak ada instalasi npm/nuget di fase ini. [VERIFIED: tests/package.json]

## Architecture Patterns

### System Diagram — Alur Verifikasi 398

```
                          ┌──────────────────────────────────────────────┐
                          │  Pre-req runtime (MANUAL, bukan webServer)     │
                          │  dotnet run dari MAIN tree                      │
                          │  Authentication__UseActiveDirectory=false      │
                          │  localhost:5277  +  SQLEXPRESS reachable        │
                          └──────────────────────────────────────────────┘
                                          │
   beforeAll: db.backup() ───────────────┤  (snapshot DB — CLAUDE.md Seed Workflow)
                                          ▼
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │  SPEC inject-seakan-online-398.spec.ts  (mode: serial, --workers=1)          │
  │                                                                               │
  │  Per skenario (Form / Auto-gen / Excel / Pre-Post-linked):                    │
  │    login admin → /Admin/InjectAssessment → wizard 6 langkah → #btnInject      │
  │         │                                                                     │
  │         ▼ commit (jalur sama online, byte-identik — sudah unit+e2e tested)     │
  │    ┌──────────────┬──────────────┬───────────────┬───────────────────┐       │
  │    ▼              ▼              ▼               ▼                              │
  │  (a) /CMP/Records  (b) /CMP/Results/{id}  (c) ElemenTeknis  (d) CertificatePdf │
  │   label             per-soal Benar/Salah   breakdown %        download .pdf    │
  │  "Assessment        + AllowAnswerReview     (jika ElemenTeknis  (jika cert)     │
  │   Online"            gated render            di-author)                          │
  │         │                                                                       │
  │         ▼  D-03 side-by-side: seed online sibling (execScript) → bandingkan     │
  │            struktur baris Records + render Results inject vs online = identik    │
  └─────────────────────────────────────────────────────────────────────────────┘
                                          │
   afterAll: db.restore() ───────────────┤  (kembalikan baseline)
                                          ▼
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │  D-05 REGRESI:                                                                 │
  │   (i) dotnet test HcPortal.Tests   → 501 Fact/Theory hijau (tak regresi)       │
  │   (ii) RERUN exam-types.spec.ts (FLOW K/L/M) + exam-taking.spec.ts (Flow A)    │
  │        → jalur online create→ambil→grade→cert tetap utuh                       │
  └─────────────────────────────────────────────────────────────────────────────┘
                                          │
                                          ▼
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │  D-06: /gsd-audit-milestone v32.2  → aggregate VERIFICATION.md 393-398         │
  │        → INJ-01..13 = 13/13 + integration wired → v32.2-MILESTONE-AUDIT.md     │
  └─────────────────────────────────────────────────────────────────────────────┘
```

### Recommended File Structure
```
tests/e2e/
├── inject-seakan-online-398.spec.ts   # BARU — 4-5 skenario downstream parity (D-02/03/04)
└── (rerun existing untuk D-05):
    ├── exam-types.spec.ts             # FLOW K (MA) / L (Essay+grade) / M (Mixed) full cycle
    ├── exam-taking.spec.ts            # Flow A (MC full lifecycle + cert + monitoring + export)
    └── cmp-records-346/350/351.spec.ts # Records surface online (opsional cross-check)
docs/SEED_JOURNAL.md                    # entri Phase 398 (per skenario) → CLEANED
.planning/v32.2-MILESTONE-AUDIT.md      # artifact D-06 (dibuat oleh /gsd-audit-milestone)
```

### Pattern 1: Snapshot/Restore wrapper (CLAUDE.md Seed Workflow — WAJIB)
**What:** beforeAll backup ke default backup dir (C:\Temp blocked oleh service account), afterAll restore.
**When to use:** SEMUA skenario yang menulis DB (commit inject = tulis DB + cert + audit).
**Example:**
```typescript
// Source: tests/e2e/inject-assessment-395.spec.ts:94-115 (pola identik dipakai 396/397)
test.beforeAll(async () => {
  const dirRaw = await db.queryString(
    `SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))`
  );
  const dir = dirRaw.replace(/\\+$/, '').replace(/\\/g, '/');
  snapshotPath = `${dir}/HcPortalDB_Dev-pre398-${new Date().toISOString().replace(/[:.]/g, '-')}.bak`;
  await db.backup(snapshotPath);
});
test.afterAll(async () => {
  if (!snapshotPath) return;
  await db.restore(snapshotPath);   // restore SUKSES atau GAGAL — selalu kembalikan baseline
});
```

### Pattern 2: Drive wizard → commit → assert DB (pola "seakan online")
**What:** login admin → /Admin/InjectAssessment → isi wizard → #btnInject → assert flash + DB.
**Example:**
```typescript
// Source: tests/e2e/inject-assessment-395.spec.ts:118-191 + inject-excel-396.spec.ts:209-267
await page.click('#btnNext5');
await expect(page.locator('#step-6')).toBeVisible();
await page.click('#btnInject');
await page.waitForLoadState('load');
await expect(page.locator('.alert-success').filter({ hasText: /Inject berhasil/i }).first())
  .toBeVisible({ timeout: 10_000 });
// → kemudian (398 BARU): navigasi ke surface downstream
const sessionId = await db.queryScalar(
  `SELECT TOP 1 Id FROM AssessmentSessions WHERE Title='${titleSql}' ORDER BY Id DESC`);
await page.goto(`/CMP/Results/${sessionId}`);   // assert per-soal + elemen teknis
```

### Pattern 3: Seed sesi online pembanding via execScript (D-03 side-by-side)
**What:** INSERT 1 AssessmentSession online (`IsManualEntry=0`) langsung via SQL untuk dibandingkan.
**When to use:** D-03 — butuh sesi online asli untuk worker yang sama agar dibandingkan dengan inject.
**Example:**
```typescript
// Source: tests/e2e/inject-assessment-397.spec.ts:48-65 (seedOnlinePostRoom)
// CATATAN: untuk parity Results per-soal, sesi online pembanding juga butuh
//   UserPackageAssignment + PackageUserResponses + PackageQuestion/Option agar Results me-render.
//   Opsi lebih bersih: drive 1 exam online RINGAN via UI (1 MC) untuk pembanding — discretion.
```

### Pattern 4: Excel skenario — bangun .xlsx FRESH (jangan round-trip ClosedXML)
```typescript
// Source: tests/e2e/inject-excel-396.spec.ts:104-135 (buildUploadXlsx)
const wb = new ExcelJS.Workbook();
const ws = wb.addWorksheet('Jawaban');   // parser baca by name + posisi kolom
ws.getRow(2).getCell(1).value = nip;     // col1=NIP, col2=Nama, col3+=jawaban
// download template HANYA untuk exercise tombol; jangan parse balik (incompat exceljs.readFile)
```

### Anti-Patterns to Avoid
- **Duplikasi assertion commit:** spec 395/396/397 SUDAH membuktikan score commit == preview (anti silent-grade-0). 398 fokus **surface downstream + parity**, jangan ulang grading assertions. [berdasarkan inject-395/396/397.spec.ts]
- **Run dari worktree sibling:** Razor embedded saat build (`AddControllersWithViews` tanpa RuntimeCompilation). E2E view-change WAJIB dari MAIN tree. [VERIFIED: STATE.md:57, ada worktree `.claude/worktrees/pensive-saha-4b1351/`]
- **Lupa restore on failure:** afterAll restore harus jalan walau test gagal (try/finally pola di helper). Tinggalkan DB kotor = langgar CLAUDE.md.
- **`--workers` > 1:** WAJIB `--workers=1` (shared SQLEXPRESS + serial state). [VERIFIED: CONTEXT.md D-discretion + playwright.config fullyParallel:false]
- **Lupa Authentication__UseActiveDirectory=false:** login lokal gagal tanpa AD-off. [VERIFIED: header tiap spec inject]

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| DB snapshot/restore | sqlcmd manual inline | `tests/helpers/dbSnapshot.ts` (backup/restore/query*) | Sudah ada localhost-guard + SINGLE_USER ROLLBACK + default backup dir resolve [VERIFIED] |
| Login flow | re-implement | `loginAdmin()` pola (copy dari 395) + `accounts.admin` | Konsisten + kredensial terpusat [VERIFIED] |
| Online-path regression spec | tulis baru create→take→grade→cert | RERUN `exam-types.spec.ts` FLOW K/L/M + `exam-taking.spec.ts` Flow A | Spec teruji lengkap; tulis baru = drift risk [VERIFIED: grep describe blocks] |
| Excel upload file | edit template hasil download | `buildUploadXlsx()` pola (exceljs fresh, sheet "Jawaban") | exceljs.readFile incompat ClosedXML output [VERIFIED: inject-excel-396.spec.ts:107] |
| Worker NIP lookup | hardcode | `db.queryString("SELECT TOP 1 NIP FROM Users WHERE Email=...")` | Tabel Identity = **Users** (BUKAN AspNetUsers) di project ini [VERIFIED: inject-excel-396.spec.ts:96-102] |
| Milestone audit | manual checklist | `/gsd-audit-milestone v32.2` | Aggregates VERIFICATION.md 3-source cross-ref [VERIFIED: audit-milestone.md] |

**Key insight:** Fase ini ~100% reuse infra. Risiko utama bukan menulis kode baru, melainkan **environment correctness** (server jalan dari main tree, AD-off, SQL reachable) + **kelengkapan assertion surface** (jangan berhenti di commit; navigasi ke 4 surface).

## Runtime State Inventory

> 398 = test phase, bukan rename/refactor/migration. Tidak ada perubahan runtime state produksi.
> Satu-satunya state yang disentuh = DB lokal test (dibungkus snapshot/restore → 0 residu).

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | DB lokal `HcPortalDB_Dev` ditulis oleh commit inject (sessions+responses+cert+audit) + seed online D-03 | snapshot beforeAll → restore afterAll (sudah pola 395/396/397) + SEED_JOURNAL entri CLEANED |
| Live service config | None — tak ada layanan eksternal. SignalR out-of-scope inject (REQUIREMENTS.md:57). | none |
| OS-registered state | None — verified, tak ada scheduled task/pm2/service baru. | none |
| Secrets/env vars | `Authentication__UseActiveDirectory=false` (env var saat `dotnet run` lokal, BUKAN file commit) | set saat run server; tak masuk git |
| Build artifacts | None baru. ⚠️ ada worktree `.claude/worktrees/pensive-saha-4b1351/` (pre-Plan tree) — JANGAN run e2e dari sana | run dari main tree saja |

## Common Pitfalls

### Pitfall 1: Berhenti di commit, lupa navigasi surface (gagal penuhi D-02)
**What goes wrong:** Spec hanya assert `AssessmentSessions.Score` di DB (seperti 395/396/397) lalu dianggap selesai.
**Why it happens:** Pola 395/396/397 berhenti di DB-assert (fokus mereka = commit). 398 fokus berbeda.
**How to avoid:** Setiap skenario WAJIB `page.goto('/CMP/Records')` (assert baris + label "Assessment Online") + `page.goto('/CMP/Results/{id}')` (assert per-soal Benar/Salah + elemen teknis) + download cert.
**Warning signs:** Tidak ada `page.goto('/CMP/...')` di spec; hanya `db.queryScalar`.

### Pitfall 2: Results render kosong "Tinjauan jawaban tidak tersedia"
**What goes wrong:** `/CMP/Results/{id}` tampil empty-state alih-alih per-soal.
**Why it happens:** Render per-soal di-gate `packageAssignment != null` + `AllowAnswerReview == true` (CMPController.cs:2217+2243). Jika skenario set `AllowAnswerReview=false` atau tak ada `UserPackageAssignment`, kosong.
**How to avoid:** Pastikan wizard inject set `AllowAnswerReview=true` (default true, spec §14). Untuk seed online pembanding (D-03), seed JUGA harus punya UserPackageAssignment+responses+package agar render — atau drive online RINGAN via UI.
**Warning signs:** `Views/CMP/Results.cshtml:416` "Tinjauan jawaban tidak tersedia" muncul. [VERIFIED: grep]

### Pitfall 3: Essay sesi tertinggal "Menunggu Penilaian" (risiko §13)
**What goes wrong:** Skenario essay → `Status='Menunggu Penilaian'` (PendingGrading) bukan `Completed`.
**Why it happens:** `FinalizeEssayGrading` tak terpanggil benar. (Sudah ditangani 393/395, tapi 398 HARUS membuktikan ulang end-to-end.)
**How to avoid:** Skenario essay (D-04 WAJIB) assert `Status='Completed'` di DB + render per-soal essay di Results (skor + teks, bukan badge pending). Records label tetap "Assessment Online" (GetUnifiedRecords meng-include PendingGrading juga — WorkerDataService.cs:33).
**Warning signs:** `IsEssayPending=true` di Results, badge "Menunggu Penilaian". [VERIFIED: CMPController.cs:2301]

### Pitfall 4: Side-by-side parity (D-03) tak benar-benar membandingkan
**What goes wrong:** Skenario "parity" hanya assert sesi inject ada, tak membandingkan dengan online.
**Why it happens:** D-03 ambigu — butuh ≥2 sesi (1 inject + 1 online asli) di view yang sama.
**How to avoid:** Seed/buat 1 sesi online asli (`IsManualEntry=0`) untuk worker yang sama → di Records keduanya berlabel "Assessment Online" (tak ada penanda visual beda), di Results keduanya render struktur per-soal identik. Assert: tak ada teks/badge/atribut yang membedakan inject dari online di DOM pekerja.
**Warning signs:** Hanya 1 `page.goto` Results, tak ada perbandingan struktur.

### Pitfall 5: DB tidak restore → audit milestone baca data kotor
**What goes wrong:** Sesi test ZZ-prefix nyangkut → audit/Records produksi tercemar.
**How to avoid:** afterAll restore WAJIB (try/finally). SEED_JOURNAL tandai CLEANED hanya setelah restore terverifikasi (`db.queryScalar` count sessions == baseline).

## Code Examples

### Assert baris Records + label "Assessment Online" (D-02a)
```typescript
// Source pattern: cmp-records-346.spec.ts + WorkerDataService.cs:47 (RecordType="Assessment Online")
await page.goto('/CMP/Records');   // atau /CMP/RecordsWorkerDetail untuk worker spesifik
const row = page.locator('table tr', { hasText: title });
await expect(row).toBeVisible();
await expect(row).toContainText('Assessment Online');   // label otomatis, no IsManualEntry filter
```

### Assert per-soal Benar/Salah + elemen teknis (D-02b/c)
```typescript
// Source: CMPController.cs:2243-2345 (QuestionReviews + ElemenTeknisScores)
await page.goto(`/CMP/Results/${sessionId}`);
await expect(page.locator('text=Tinjauan jawaban tidak tersedia')).toHaveCount(0);  // NOT empty-state
// per-soal verdict (Benar/Salah) ter-render; breakdown elemen teknis tampil jika di-author
```

### Download cert PDF nyata (D-02d)
```typescript
// Source: CMPController.cs:1943 CertificatePdf (HttpGet) — endpoint sama online; QuestPDF
const [dl] = await Promise.all([
  page.waitForEvent('download', { timeout: 15_000 }),
  page.goto(`/CMP/CertificatePdf/${sessionId}`),   // atau klik tombol di Results/Certificate
]);
const dest = path.join(tmpDir, 'cert.pdf');
await dl.saveAs(dest);
expect(fs.readFileSync(dest).slice(0,4).toString('latin1')).toBe('%PDF');
```

### 0-migration gate (Claude's Discretion — D dipilih)
```bash
# Opsi A (cepat): git diff harus kosong sepanjang fase
git diff --stat HEAD -- Migrations/ Data/   # → kosong = 0 migration
# Opsi B (defensif): ef snapshot diff 0
dotnet ef migrations add _verify --project HcPortal.csproj && git status Migrations/  # → 0 perubahan model → hapus _verify
```
> Baseline saat ini: `git diff HEAD -- Migrations/ Data/` = KOSONG. [VERIFIED: git diff dijalankan]

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| BulkBackfill (skor agregat saja, Admin-only) | Inject page full-fidelity (per-soal+ET+cert, Admin+HC) | v32.2 (393-396) | BulkBackfill di-retire (route 404, Phase 396 INJ-11) — 398 boleh assert route gone (sudah di 396 Scenario 6, jangan duplikasi) |
| Human UAT per phase | E2E otomatis konsolidasi (D-01) | Phase 398 | Repeatable; human UAT sudah tercakup 394-397 |

**Deprecated/outdated:**
- BulkBackfill route (`/Admin/BulkBackfill`, `/Admin/BulkBackfillAssessment`): 404 — sudah diuji di inject-excel-396.spec.ts:348-366. **Jangan ulang di 398.**

## Validation Architecture

> 398 ADALAH fase test — "deliverable" = spec/assertion itu sendiri. Validation Architecture di sini = assertion/spec apa yang menyusun verifikasi fase + Wave 0 test requirements.

### Test Framework
| Property | Value |
|----------|-------|
| Framework (E2E) | @playwright/test ^1.58.2 |
| Framework (unit/regression) | xUnit (HcPortal.Tests, 501 Fact/Theory) |
| Config file | `tests/playwright.config.ts` (testDir ./e2e, globalTeardown, fullyParallel:false) |
| Quick run command | `cd tests && npx playwright test e2e/inject-seakan-online-398.spec.ts --workers=1` |
| Full suite command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` + `cd tests && npx playwright test --workers=1` |
| Pre-req runtime | `dotnet run` dari MAIN tree, `Authentication__UseActiveDirectory=false`, localhost:5277, SQLEXPRESS reachable |

### Phase Requirements → Test Map
| Req | Behavior | Test Type | Automated Command | File Exists? |
|-----|----------|-----------|-------------------|-------------|
| INJ-13 (D-02a) | Records baris inject berlabel "Assessment Online" | e2e | `npx playwright test e2e/inject-seakan-online-398.spec.ts --workers=1` | ❌ Wave 0 (spec baru) |
| INJ-13 (D-02b) | Results per-soal Benar/Salah (4 mode) | e2e | (same spec) | ❌ Wave 0 |
| INJ-13 (D-02c) | Results breakdown elemen teknis | e2e | (same spec) | ❌ Wave 0 |
| INJ-13 (D-02d) | Cert PDF download nyata (%PDF) | e2e | (same spec) | ❌ Wave 0 |
| INJ-13 (D-03) | Side-by-side inject vs online indistinguishable | e2e | (same spec) | ❌ Wave 0 |
| INJ-13 (D-04 essay §13) | Essay sesi Status=Completed + per-soal tampil | e2e | (same spec, skenario essay) | ❌ Wave 0 |
| INJ-13 (D-05 i) | Full xUnit suite hijau (no regression) | unit/integration | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` | ✅ ada (501 Fact) |
| INJ-13 (D-05 ii) | Online-path live create→take→grade→cert | e2e | `npx playwright test e2e/exam-types.spec.ts e2e/exam-taking.spec.ts --workers=1` | ✅ ada (rerun) |
| INJ-13 (gate) | 0 migration | build | `git diff --stat HEAD -- Migrations/ Data/` (kosong) | ✅ verified kosong |
| INJ-13 (D-06) | Audit milestone 13/13 | tooling | `/gsd-audit-milestone v32.2` → v32.2-MILESTONE-AUDIT.md | ❌ artifact dibuat fase ini |

### Sampling Rate
- **Per task commit:** `npx playwright test e2e/inject-seakan-online-398.spec.ts --workers=1` (spec 398 saja, cepat)
- **Per wave merge:** `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` (full xUnit)
- **Phase gate:** spec 398 + online-path rerun + full xUnit semua hijau + 0-migration + audit 13/13 sebelum `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `tests/e2e/inject-seakan-online-398.spec.ts` — spec konsolidasi 4-5 skenario (D-02/03/04), covers INJ-13 surface
- [ ] (D-03) mekanisme seed/buat sesi online pembanding untuk worker yang sama (execScript fixture ATAU drive online ringan via UI — discretion)
- [ ] `docs/SEED_JOURNAL.md` — entri Phase 398 per skenario (snapshot → CLEANED)
- [ ] `.planning/v32.2-MILESTONE-AUDIT.md` — dibuat oleh `/gsd-audit-milestone v32.2` (D-06)
- [ ] Tidak perlu framework install (Playwright + xUnit + exceljs sudah ada)

## Security Domain

> `security_enforcement` tidak diset eksplisit false di config; namun 398 = test phase 0-kode-produksi. RBAC + transparansi inject sudah di-secure di per-phase (393 SECURED 13/13, 394 17/17, 395 15/15, 396 12/12, 397 18/18 dari MEMORY.md). 398 tidak memperkenalkan surface baru.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V4 Access Control | yes (assert, bukan implement) | `/CMP/Results` authz (CMPController.IsResultsAuthorized:2503) — 398 BOLEH assert L4 cross-section Forbid sebagai parity-online (sudah di cmp-records-346.spec.ts:104) |
| V5 Input Validation | no (no input baru) | sudah di 395/396 (Excel atomic, NIP valid) |
| V6 Cryptography | no | n/a |

### Known Threat Patterns for stack
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Test menulis DB produksi-lokal tak ter-restore | Tampering | snapshot/restore wajib (dbSnapshot helper) + localhost-guard |
| sqlcmd target non-localhost | Tampering | `runSqlcmd` reject non-localhost (dbSnapshot.ts) [VERIFIED] |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Sesi online pembanding (D-03) untuk parity Results butuh UserPackageAssignment+responses+package agar me-render per-soal — seed SQL minimal (a la inject-397 seedOnlinePostRoom) TIDAK cukup untuk Results-render; butuh paket. | Pattern 3 + Pitfall 2 | Jika salah, seed minimal cukup → planner over-engineer D-03. Mitigasi: planner verifikasi dengan 1 trial run, atau drive online ringan via UI (lebih pasti). [ASSUMED — berdasarkan CMPController.cs:2212-2217 gating, belum di-run] |
| A2 | `/gsd-audit-milestone v32.2` akan menulis `.planning/v32.2-MILESTONE-AUDIT.md` (pola milestone sebelumnya v29/v30/v31). | D-06 / Validation | Path artifact bisa beda format (workflow line 165 sebut `v{version}-v{version}-MILESTONE-AUDIT.md` typo vs aktual `v{version}-MILESTONE-AUDIT.md`). Low risk; artifact tetap dibuat. [ASSUMED — berdasarkan ls milestones + audit-milestone.md] |
| A3 | exam-types.spec.ts FLOW K/L/M + exam-taking.spec.ts Flow A masih hijau di env saat ini (D-05 rerun) — belum di-run dalam sesi ini. | D-05 / Stack | Jika ada spec online yang sudah flaky, regresi gagal bukan karena inject. Mitigasi: jalankan online-path baseline DULU sebelum simpulkan regresi. [ASSUMED] |

## Open Questions

1. **D-03 implementasi seed online pembanding**
   - What we know: inject-397 seed sesi online via execScript (`seedOnlinePostRoom`) — tapi itu sesi tanpa paket (cukup untuk grouping, tak cukup untuk Results-render per-soal).
   - What's unclear: untuk membandingkan render Results per-soal (bukan cuma Records baris), sesi online pembanding butuh paket+assignment+responses. Seed SQL untuk itu kompleks.
   - Recommendation: untuk D-03 yang menyentuh **Records** (baris + label) → seed SQL minimal cukup. Untuk D-03 yang menyentuh **Results** (per-soal) → drive 1 exam online RINGAN via UI (1 MC, ambil, submit) sebagai pembanding, ATAU batasi parity-Results pada assertion "tak ada penanda visual beda" di sesi inject saja vs known online baseline. Serahkan ke planner; tandai sebagai keputusan struktur.

2. **Granularitas skenario (D-04 ~4-5)**
   - What we know: Form / Auto-gen / Excel / Pre-Post-linked + essay wajib.
   - What's unclear: apakah essay = skenario terpisah atau di-bundle ke salah satu mode (mis. Form+essay).
   - Recommendation: bundle essay ke skenario Form (Form input-asli + 1 essay) untuk hemat — essay sudah punya jalur Form di 395. Excel essay-skor-tanpa-teks sudah di 396. Pre-Post linked = skenario ke-4. Target 4 skenario inject + 1 side-by-side = 5. Discretion planner.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| @playwright/test | E2E spec 398 + rerun online | ✓ | ^1.58.2 | — |
| exceljs | Skenario Excel | ✓ | ^4.4.0 | — |
| xUnit / dotnet test | D-05 full suite | ✓ | HcPortal.Tests (501 Fact) | — |
| sqlcmd | dbSnapshot backup/restore/query | ✓ (asumsi sama env 395/396/397 baru jalan) | system | — |
| SQLEXPRESS localhost | DB write + snapshot | ✓ (env sama dipakai inject-395..397) | MSSQL17 | — |
| dotnet run (main tree, AD-off) | server localhost:5277 | ✓ (manual start, BUKAN webServer config) | — | — |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None. Semua infra sudah dipakai phase 394-397.

> ⚠️ Catatan env (dari `reference_local_e2e_sql_env_fix` + STATE.md): bila login 500 / SQL conn fail, start **SQLBrowser** service + gunakan `lpc:` shared-memory override (NTLM loopback fail). Playwright combined run WAJIB `--workers=1`.

## Sources

### Primary (HIGH confidence)
- `Services/WorkerDataService.cs:28-62` — GetUnifiedRecords, RecordType="Assessment Online", no IsManualEntry filter (D-02a)
- `Controllers/CMPController.cs:2184-2366` — Results action: per-soal gating (2217 packageAssignment, 2243 AllowAnswerReview), ElemenTeknisScores (2318-2345)
- `Controllers/CMPController.cs:1835` (Certificate), `:1943` (CertificatePdf) — endpoint cert sama online (D-02d)
- `Views/CMP/Results.cshtml:416` — empty-state "Tinjauan jawaban tidak tersedia" (negative assertion)
- `tests/e2e/inject-assessment-395.spec.ts` / `inject-excel-396.spec.ts` / `inject-assessment-397.spec.ts` — pola snapshot/restore, drive wizard, commit, exceljs, seed online
- `tests/helpers/dbSnapshot.ts` + `tests/helpers/accounts.ts` — helper test reuse
- `tests/playwright.config.ts` — fullyParallel:false, globalTeardown, baseURL localhost:5277
- `tests/e2e/exam-types.spec.ts` (FLOW K/L/M) + `exam-taking.spec.ts` (Flow A) — online-path regression (D-05)
- `.planning/REQUIREMENTS.md:42,79` + `.planning/ROADMAP.md:68,166-174,220` — INJ-13 + 13/13 mapping
- `docs/superpowers/specs/2026-06-17-inject-assessment-manual-design.md` §1/§2.2/§2.3/§13/Fase 6 — sumber kebenaran INJ-13
- `~/.claude/get-shit-done/workflows/audit-milestone.md` — proses /gsd-audit-milestone (D-06)
- `CLAUDE.md` — Develop + Seed Workflow (snapshot/restore wajib)

### Secondary (MEDIUM confidence)
- MEMORY.md — bukti per-phase secure/validate/UAT 393-397 (mendasari D-01 tak ulang human UAT)
- `.planning/STATE.md:57` — Razor embedded → run dari main tree (anti-pattern worktree)

### Tertiary (LOW confidence)
- A1/A2/A3 di Assumptions Log — belum di-run dalam sesi ini (D-03 seed kompleksitas, path artifact audit, online-spec flakiness)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua dependency di-Read dari tests/package.json + HcPortal.Tests; 0 instalasi baru
- Architecture/surfaces: HIGH — 4 surface code path di-grep + di-Read langsung (WorkerDataService, CMPController.Results, cert endpoints)
- Test infra: HIGH — helper + 4 spec inject existing di-Read; pola identik
- D-03 side-by-side mekanik: MEDIUM — render-parity Results butuh paket di sesi pembanding (A1, belum di-trial)
- Pitfalls: HIGH — diturunkan langsung dari gating code + lesson STATE.md/MEMORY.md

**Research date:** 2026-06-18
**Valid until:** 2026-07-18 (stack stabil; codebase aktif — re-cek bila 393-397 berubah sebelum 398 di-execute)
