# Phase 364: Restore Baseline Regresi e2e Exam - Research

**Researched:** 2026-06-11
**Domain:** Playwright e2e test restoration (TypeScript), .NET MVC server-side title validator (REST-06)
**Confidence:** HIGH (semua temuan diverifikasi langsung dari kode di working tree session ini)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions (copy verbatim — planner MUST honor)

**Pola judul baru**
- **D-01:** Prefix seragam **"Pre Test"** untuk semua flow standard. Zero risiko auto-pair — counterpart "Post Test" dengan remainder sama tak pernah ada.
- **D-02:** Marker flow dipertahankan **setelah prefix**: `Pre Test [317-K] MA Exam {ts}`. Marker tetap grep-able, regex lolos.
- **D-03:** Implementasi **edit per-call** di 2 file spec. Helper `tests/helpers/utils.ts` TIDAK disentuh — dipakai spec lain yang sudah jalan.
- **D-04:** Comply level = **lolos regex saja**. Nama lama dipertahankan sebagai suffix. Tidak perlu konvensi penuh `{Stage} Test {Track} {Lokasi}` — server hanya validasi regex.

**Definisi selesai (SC#4)**
- **D-05:** Failure NON-judul (selector drift, fitur berubah v18-v25) **di-fix di test-code sampai kedua spec hijau**. Spirit fase = baseline hidup lagi. Tetap zero kode produksi.
- **D-06:** Kalau ketemu **bug produksi nyata**: catat sebagai temuan → backlog/fase lain, JANGAN fix di 364. Flow yang kena dapat `test.fixme('alasan + ref backlog')`.
- **D-07:** Dokumentasi failure tersisa: **deviasi di SUMMARY + entry backlog 999.x** kalau actionable. Tidak perlu file FINDINGS terpisah.
- **D-08:** "PASS penuh @5277" = **1x full run hijau** live, retry policy default Playwright config.
- **D-09:** Run yang mengandung `fixme` = **BUKAN "PASS penuh"** — gate jatuh ke jalur SC#4 alternatif "failure terdokumentasi bukan-karena-judul". Jujur ke verifier; jangan hitung skip sebagai hijau.

**Guard auto-pair + exempt (SC#2/SC#3)**
- **D-10:** **Run baseline diagnosa DULU** sebelum edit: kedua spec as-is @5277, catat failure per-flow (judul vs non-judul).
- **D-11:** Guard auto-pair SC#3 = struktural (prefix seragam "Pre Test" + timestamp `uniqueTitle`) **+ 1 asersi DB** `LinkedGroupId IS NULL` di salah satu flow standard. exam-types sudah punya pola query DB — reuse.
- **D-12:** FLOW P [318-P]: **ikut full run saja**, tanpa asersi exempt khusus. PASS = bukti exempt benar; kalau patah non-judul → ikuti D-05/D-06.

**Eksekusi & cleanup**
- **D-13:** Pola eksekusi 355 **keep as-is**: `Authentication__UseActiveDirectory=false dotnet run` (env override, tanpa edit file) + SEED snapshot DB sebelum run + restore & cek residue setelah + catat `docs/SEED_JOURNAL.md`.
- **D-14:** Cleanup data test = **snapshot/restore saja**. Tidak ada teardown delete tambahan di spec.
- **D-15:** Gate akhir = **2 spec target PASS @5277 + `dotnet test` suite hijau**. Tanpa full e2e suite.

**Baseline ke depan + asersi**
- **D-16:** Status baseline pasca-restore: **on-demand**. Catat di SUMMARY sebagai baseline tersedia.
- **D-17:** Asersi sensitif judul panjang: **partial-match boleh** — substring unik (marker + timestamp) kalau UI truncate.

### Claude's Discretion
- Urutan run spec, workers/serial config Playwright, timeout tuning.
- Bentuk pencatatan hasil run baseline diagnosa (cukup di SUMMARY plan).
- Pemilihan flow mana yang dapat asersi DB LinkedGroupId (D-11) — pilih yang paling murah.
- Detail teknis fix selector drift per-flow.

### Deferred Ideas (OUT OF SCOPE)
- Bug produksi apa pun yang ketemu saat restore → temuan ke backlog 999.x / fase lain (D-06), bukan di-fix di 364.
- Menjadikan 2 spec ini gate wajib tiap fase UAT → ditolak (D-16: on-demand).
</user_constraints>

<phase_requirements>
## Phase Requirements

Test-only phase — tidak ada product requirement ID. Acuan = 4 Success Criteria roadmap.

| SC | Deskripsi | Research Support |
|----|-----------|------------------|
| SC#1 | Judul lolos validator REST-06 (`AssessmentAdminController.cs:876`, regex `^(Pre|Post)\s*Test\s+.+$`, case-sensitive) | Validator dibaca verbatim. Pola `Pre Test [marker] {nama} {ts}` lolos (lihat §Validator Mechanics). |
| SC#2 | Fix per-flow, bukan ganti buta. FLOW P (PrePostTest) exempt | Exempt path diverifikasi: `AssessmentTypeInput != "PrePostTest"` gate di :861/:874. FLOW T (Manual) jalur controller berbeda total. Lihat §Flow Inventory. |
| SC#3 | Auto-pair Phase 338 tidak salah-pasang LinkedGroupId | `TryAutoDetectCounterpartGroup` (:7132) butuh counterpart "Post Test" dengan remainder PERSIS sama. Timestamp `uniqueTitle` bikin remainder unik → counterpart selalu absen → aman. Lihat §Auto-Pair Mechanics. |
| SC#4 | Kedua spec PASS penuh @5277 atau failure terdokumentasi bukan-karena-judul | Selector-drift candidates dipetakan per-flow (§Selector Drift). Lifecycle global setup/teardown otomatis snapshot/restore. |
</phase_requirements>

## Summary

Phase 364 menghidupkan 2 spec e2e (`exam-taking.spec.ts`, `exam-types.spec.ts`) yang patah sejak v20.0. Penyebab tunggal yang sudah dikonfirmasi: validator naming **REST-06** di `AssessmentAdminController.cs:874-881` menolak judul tanpa prefix `Pre Test`/`Post Test` (regex `^(Pre|Post)\s*Test\s+.+$`, **case-sensitive**) di langkah `POST /Admin/CreateAssessment`. Spec ditulis era v16-v17 (Phase 307-319) — judul lama seperti `"Legacy Exam {ts}"` ditolak ModelState, assessment tak terbuat, dan semua sub-test serial setelahnya kolaps.

**Temuan kritis yang mengoreksi scope roadmap/CONTEXT.md:** Roadmap & CONTEXT.md menyebut **9 flow patah** (3 di exam-taking, 6 di exam-types). Audit kode session ini menemukan **24 call site `uniqueTitle()`** di kedua spec — bukan 9. exam-taking punya **flow A sampai J** (bukan hanya Legacy/Token/ForceClose); exam-types punya **W0/K/L/M/N/O** plus P/Q/R/S/T/U/V/W/X/Y. Setiap flow yang membuat assessment standard via `#Title` fill atau `createAssessmentViaWizard` kena validator. Jumlah judul yang HARUS diedit jauh lebih dari 9 (lihat §Flow Inventory untuk daftar lengkap + klasifikasi kena/exempt/N-A). Roadmap "9 flow" hanya menghitung sebagian; rencana harus pakai inventory lengkap di research ini.

**Primary recommendation:** Jalankan baseline diagnosa dulu (D-10), lalu edit SEMUA judul standard-create yang kena validator (bukan hanya 9) dengan pola `uniqueTitle('Pre Test ' + <marker lama>)` per-call. Pelaksanaan run otomatis dibungkus global setup/teardown Playwright (BACKUP → matrix seed → RESTORE) — D-13/D-14 sudah ter-enforce di lifecycle, bukan langkah manual terpisah. Waspadai selector-drift sekunder (v18-v25) hanya setelah judul comply.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Title validation (REST-06) | API/Backend (`AssessmentAdminController.cs`) | — | Server-side ModelState validation pada POST CreateAssessment; bukan client-side. |
| Auto-pair LinkedGroupId | API/Backend (`TryAutoDetectCounterpartGroup`) | Database (AssessmentSessions query) | Server query counterpart pada POST; depend pada title pattern + LinkedGroupId existing. |
| Title fix | Test code (TypeScript spec args) | — | Edit argumen `uniqueTitle()` per-call; zero produksi. |
| DB assertion (LinkedGroupId IS NULL) | Test code (`dbSnapshot.ts` sqlcmd) | Database | Direct sqlcmd query; pola sudah ada di exam-types FLOW P/Q/R. |
| Snapshot/restore lifecycle | Test infra (global.setup/teardown) | Database (BACKUP/RESTORE) | Otomatis per Playwright run via project dependency + globalTeardown. |
| App auth (login peserta lokal) | App runtime (env override) | — | `Authentication__UseActiveDirectory=false dotnet run` (D-13). |

## Validator Mechanics (read-only — JANGAN ubah produksi)

**Source: `Controllers/AssessmentAdminController.cs:873-881` [VERIFIED: dibaca session ini]**

```csharp
// Phase 339 REST-06: Validate Title pattern for standard Pre/Post tests
if (AssessmentTypeInput != "PrePostTest"
    && !string.IsNullOrEmpty(model.Title)
    && !System.Text.RegularExpressions.Regex.IsMatch(model.Title, @"^(Pre|Post)\s*Test\s+.+$"))
{
    ModelState.AddModelError("Title",
        "Title harus pola '{Stage} Test {Track} {Lokasi}' ...");
}
```

**Fakta penting (mengoreksi line ref roadmap):**
- Regex aktual di **baris 876** (roadmap/CONTEXT.md sebut :872/:874 — angka bergeser; pakai 876). Validator block :873-881. [VERIFIED]
- Regex **case-sensitive** (`RegexOptions` tidak di-set). `^(Pre|Post)\s*Test\s+.+$` → "Pre Test" valid, "pre test"/"PRE TEST" DITOLAK. Pola `Pre Test ...` (huruf besar P+T persis) wajib. [VERIFIED]
- `\s*` antara Pre/Post dan Test → "PreTest" (tanpa spasi) juga lolos, tapi CONTEXT D-01/D-02 lock pakai "Pre Test" (dengan spasi). Patuhi spasi. [VERIFIED]
- `\s+.+$` → wajib ada minimal 1 spasi + ≥1 char setelah "Test". Pola `Pre Test [317-K] MA Exam {ts}` memenuhi (`[317-K]...` = remainder). [VERIFIED]
- Validator **hanya aktif saat `AssessmentTypeInput != "PrePostTest"`** → mode standard. PrePostTest exempt. [VERIFIED]

**Contoh pola final (mengikuti D-01/D-02):**
- `uniqueTitle('Legacy Exam')` → `uniqueTitle('Pre Test Legacy Exam')` → `Pre Test Legacy Exam 1718064000000` ✅
- `uniqueTitle('[317-K] MA Exam')` → `uniqueTitle('Pre Test [317-K] MA Exam')` → `Pre Test [317-K] MA Exam 1718064000000` ✅ (marker `[317-K]` jadi bagian remainder, tetap grep-able)

## Auto-Pair Mechanics (SC#3 — read-only)

**Source: `Controllers/AssessmentAdminController.cs:859-871` + `:7132-7158` [VERIFIED]**

Flow POST CreateAssessment (mode standard):
1. Jika `AssessmentTypeInput != "PrePostTest"` && `model.LinkedGroupId == null` && title non-empty → panggil `TryAutoDetectCounterpartGroup(title, category)` (:865).
2. Helper (:7132) regex-match title `^(Pre|Post)\s*Test\s+(?<rest>.+)$` **IgnoreCase**, ekstrak `stage` + `rest`.
3. Cari counterpart: stage berlawanan ("Post" untuk title "Pre Test...") + **rest PERSIS sama** + `LinkedGroupId != null`:
   ```csharp
   var counterpartTitleA = $"{oppositeStage}Test {rest}";   // "PostTest <rest>"
   var counterpartTitleB = $"{oppositeStage} Test {rest}";  // "Post Test <rest>"
   .Where(s => s.Title == counterpartTitleA || s.Title == counterpartTitleB)
   ```
4. Jika ada → set `model.LinkedGroupId = counterpartId`.

**Kenapa guard D-01/D-11 aman [VERIFIED via logika kode]:**
- Semua judul standard pakai prefix "Pre Test" (D-01) → `oppositeStage` = "Post". Counterpart yang dicari = `Post Test <rest>`.
- `<rest>` = `[marker] {nama lama} {timestamp}`. Timestamp `Date.now()` (epoch ms) **unik per call** (`utils.ts:23`). Tidak ada judul "Post Test <marker> {nama} {timestamp-sama}" yang pernah ada di DB.
- Query `s.Title == "Post Test <rest>"` → **selalu 0 baris** → `FirstOrDefaultAsync()` return null → `counterpartId.HasValue == false` → `LinkedGroupId` tetap NULL. Aman.

**Asersi DB D-11 (rekomendasi flow termurah):** Pakai flow yang sudah punya `assessmentId` ter-extract + sudah import `dbSnapshot`. Di **exam-types** FLOW K cocok (sudah `import * as db`, sudah ekstrak `assessmentId` di K1, sudah pakai `db.queryScalar`). Tambah 1 asersi di K1 atau K5:
```ts
const linkedNull = await db.queryScalar(
  `SELECT COUNT(*) FROM AssessmentSessions WHERE Id = ${assessmentId} AND LinkedGroupId IS NULL`
);
expect(linkedNull).toBe(1);
```
Di **exam-taking** tidak ada `import dbSnapshot` saat ini — kalau asersi DB diletakkan di exam-taking, perlu tambah import (zero produksi, OK). Lebih murah taruh di exam-types FLOW K. (Discretion CONTEXT — pilih K.)

## Flow Inventory — Daftar Lengkap 24 Call Site `uniqueTitle` (mengoreksi "9 flow")

**Source: `Grep uniqueTitle\(` session ini [VERIFIED]**

### exam-taking.spec.ts (11 call site)
| Line | Title arg lama | Create path | Kena validator? | Edit jadi |
|------|----------------|-------------|-----------------|-----------|
| :35 | `Legacy Exam` | `#Title` fill + standard submit | YES | `Pre Test Legacy Exam` |
| :316 | `Token Exam` | `#Title` fill + standard | YES | `Pre Test Token Exam` |
| :402 | `ForceClose Exam` | `#Title` fill + standard | YES | `Pre Test ForceClose Exam` |
| :548 | `Package Exam` | `#Title` fill + standard | YES | `Pre Test Package Exam` |
| :702 | `Proton T3 Interview` | `#Title` fill, Category='Assessment Proton' | YES (AssessmentTypeInput≠PrePostTest) | `Pre Test Proton T3 Interview` |
| :833 | `Multi Worker` | `#Title` fill + standard | YES | `Pre Test Multi Worker` |
| :963 | `Timer Expired` | `#Title` fill + standard | YES | `Pre Test Timer Expired` |
| :1057 | `RealTime Mon` | `#Title` fill + standard | YES | `Pre Test RealTime Mon` |
| :1291 | `EditTest` | `#Title` fill + standard | YES | `Pre Test EditTest` |
| :1404 | `Abandon Test` | `#Title` fill + standard | YES | `Pre Test Abandon Test` |

> CATATAN exam-taking: roadmap hanya sebut 3 (Legacy/Token/ForceClose). Realitanya **10 flow standard** semua kena. Flow A juga punya sub-test `A8`/`I3` yang assert/edit judul (lihat §Selector Drift). Blok "Phase 313" di akhir (:1608+) pakai fixture SQL via `clickResumeForFixture` — TIDAK create via UI, judul fixture hardcoded di seed SQL, TIDAK kena validator (judul fixture mengandung "Fixture", bukan dibuat lewat CreateAssessment). Verifikasi saat baseline run.

### exam-types.spec.ts (14 call site, 13 via uniqueTitle + grep)
| Line | Title arg lama | Create path | Kena validator? | Edit jadi |
|------|----------------|-------------|-----------------|-----------|
| :37 | `[317-SMOKE-W0] Order Verify` | `createAssessmentViaWizard` (standard) | YES | `Pre Test [317-SMOKE-W0] Order Verify` |
| :191 | `[317-K] MA Exam` | wizard standard | YES | `Pre Test [317-K] MA Exam` |
| :310 | `[317-L] Essay Exam` | wizard standard | YES | `Pre Test [317-L] Essay Exam` |
| :435 | `[317-M] Mixed Exam` | wizard standard | YES | `Pre Test [317-M] Mixed Exam` |
| :588 | `[317-N] NoReview Exam` | wizard standard | YES | `Pre Test [317-N] NoReview Exam` |
| :690 | `[317-O] ExtraTime Exam` | wizard standard | YES | `Pre Test [317-O] ExtraTime Exam` |
| :860 | `[318-P] PrePost Exam` | `createPrePostAssessmentViaWizard` (PrePostTest) | **NO — exempt** | TIDAK diubah (D-12) |
| :1065 | `[318-Q] EWCD Past Exam` | wizard standard | YES | `Pre Test [318-Q] EWCD Past Exam` |
| :1151 | `[318-R] Cert Exam` | wizard standard | YES | `Pre Test [318-R] Cert Exam` |
| :1278 | `[318-S-TRUE] Review Exam` | wizard standard | YES | `Pre Test [318-S-TRUE] Review Exam` |
| :1364 | `[318-S-FALSE] NoReview Exam` | wizard standard | YES | `Pre Test [318-S-FALSE] NoReview Exam` |
| :1457 | `[319-T] Manual CRUD` | `POST /Admin/AddManualAssessment` (TrainingAdminController) | **NO — jalur controller berbeda** | TIDAK perlu diubah, tapi lihat catatan |

> CATATAN exam-types:
> - **FLOW P exempt** [VERIFIED]: `createPrePostAssessmentViaWizard` set `assessmentType='PrePostTest'` (examTypes.ts:548) → validator skip. D-12 = ikut full run, jangan diedit.
> - **FLOW T (Manual) jalur berbeda** [VERIFIED]: `AddManualAssessment` di `TrainingAdminController.cs:653` — TIDAK ada regex REST-06 (cuma validasi WorkerCerts + file). Judul `[319-T] Manual CRUD` TIDAK ditolak. SECARA TEKNIS tidak wajib diedit. **Rekomendasi: biarkan apa adanya** (bukan create standard, tidak kena validator). Kalau baseline run nunjukin T hijau, jangan sentuh.
> - **FLOW U/V/W/X/Y**: tidak create assessment via title (U=ManageCategories, V=Export Excel, W=Analytics, X=CertMgmt, Y=gap smoke). `[319-U]` pakai `Date.now()` template literal (bukan uniqueTitle), category name bukan assessment title → tidak kena validator. Smoke block W0.T0/V0/W0/X0 juga tidak create standard assessment.
> - **6 flow standard wizard "roadmap"** = W0/K/L/M/N/O. Tambahan yang roadmap LEWATKAN: Q, R, S (TRUE+FALSE = 2 judul). Jadi exam-types punya **11 judul standard kena validator**, bukan 6.

**Total judul HARUS diedit: ~21 (10 exam-taking + 11 exam-types).** FLOW P exempt, FLOW T jalur berbeda. **Ini bukan 9.** Planner WAJIB pakai daftar ini, bukan angka roadmap.

## Create-Wizard Path & Manifestasi Penolakan Validator

**Dua pola create berbeda [VERIFIED]:**

1. **exam-taking** — direct form fill di `/Admin/CreateAssessment` (single-page, bukan wizard helper):
   ```ts
   await page.fill('#Title', title);
   await page.selectOption('#Category', 'OJT');
   // ... fill ScheduleDate/Time/Duration/PassPercentage ...
   await page.click('#submitBtn');
   await page.waitForTimeout(3_000);
   const success = await page.locator('#successModal')...
   const alert = await page.locator('.alert-success').isVisible()...
   expect(success || alert).toBeTruthy();   // ← GAGAL di sini saat validator reject
   ```
   Saat validator reject: server return View dengan ModelState error → `#successModal` tak muncul, `.alert-success` absen → `expect(success || alert).toBeTruthy()` **FAIL**. Sub-test serial berikutnya (A2+) lalu gagal karena `title`/`assessmentId` invalid.

2. **exam-types** — `createAssessmentViaWizard` (examTypes.ts:51), 4-step Bootstrap wizard:
   - STEP 1 fill `#Category` + `#Title`, **`AssessmentTypeInput` dibiarkan default (Standard)** → validator aktif.
   - STEP 4 klik `#btnSubmit` lalu **`await page.locator('#successModal.show').waitFor({state:'visible', timeout:15_000})`** (examTypes.ts:103).
   - Saat validator reject: `#successModal` tak pernah `.show` → `waitFor` **timeout 15s** → helper throw → sub-test K1/L1/dst FAIL.

**Implikasi planning:** Penolakan validator memanifestasi sebagai (a) `expect(success||alert)` false di exam-taking, (b) waitFor timeout 15s di exam-types wizard helper. Setelah judul comply, kegagalan ini hilang; sisa kegagalan = selector/feature drift (D-05).

## DB Assertion Pattern (D-11)

**Helper: `tests/helpers/dbSnapshot.ts` [VERIFIED]** — sqlcmd subprocess wrapper, localhost-only guard. Fungsi siap pakai:
- `queryScalar(sql): Promise<number>` — return integer first numeric line. Pakai untuk `COUNT(*)` / `LinkedGroupId IS NULL`.
- `queryString(sql): Promise<string>` — return first non-empty line. Pakai untuk Status/varchar.

**Pola query DB sudah hidup di exam-types** (bukan helper terpisah — langsung `db.queryScalar`/`db.queryString` inline):
- FLOW K K5 (:287-295): `SELECT ISNULL(Score,-1) FROM AssessmentSessions WHERE Id = ${sessionId}`.
- FLOW P P1 (:903-909): `SELECT COUNT(*) ... LinkedSessionId IS NOT NULL` → **pola persis untuk D-11** (tinggal ganti `LinkedSessionId IS NOT NULL` → `LinkedGroupId IS NULL`, ganti session-pair filter → `Id = ${assessmentId}`).
- FLOW R/S/T: banyak contoh `db.queryScalar`/`db.queryString`.

**Koneksi:** hardcoded `localhost\SQLEXPRESS` / `HcPortalDB_Dev` / Integrated Security (dbSnapshot.ts:22-29). Sama dengan DB lokal yang dipakai `dotnet run`. Tidak perlu config tambahan.

## Playwright Run Config & Lifecycle

**Source: `tests/playwright.config.ts` + `global.setup.ts` + `global.teardown.ts` [VERIFIED]**

| Property | Value |
|----------|-------|
| testDir | `./e2e` (relatif `tests/`) |
| baseURL | `http://localhost:5277` |
| timeout (per test) | 60_000 ms |
| expect timeout | 10_000 ms |
| actionTimeout | 10_000 ms (Phase 316 bound) |
| fullyParallel | **false** |
| retries | **0** (D-08 "default" = 0 retry; run sekali) |
| reporter | html (no auto-open) + list |
| projects | `setup` (testMatch global.setup.ts) → `chromium` (dependencies:['setup']) |
| globalTeardown | `./e2e/global.teardown.ts` |
| screenshot | `on` |

**KRITIS — lifecycle otomatis bungkus setiap run [VERIFIED, temuan baru]:**
- Project `setup` (global.setup.ts) adalah **dependency** project `chromium`. Run spec apa pun via `npx playwright test <file>` → `setup` jalan dulu: cek app `/Account/Login` 200 → resolve backup dir → **pre-check collision Id 9001-9018 == 0** → **BACKUP DB** → seed matrix SQL (`tests/sql/assessment-matrix-seed.sql`) → Layer 1 validation (expect 18 sessions / 10 packages / 30 questions / 80 options / UPA 0) → tulis `.matrix-state.json` → append SEED_JOURNAL `active`.
- **globalTeardown** (selalu jalan di akhir): flush report → **RESTORE DB** dari snapshot → Layer 4 (expect 0 matrix rows) → SEED_JOURNAL `active→cleaned` → hapus state+`.bak`.
- **Konsekuensi untuk D-13/D-14:** snapshot/restore SUDAH ter-enforce di lifecycle Playwright. Setiap run 2 spec target otomatis BACKUP sebelum + RESTORE sesudah. Data test (judul `Pre Test ...`) yang dibuat 2 spec ikut ter-revert oleh RESTORE global. **D-13/D-14 terpenuhi otomatis** — tidak perlu snapshot manual terpisah, TAPI tetap catat journal entry sesuai D-13 (lifecycle sudah append/clean entry matrix; entry manual 364 opsional untuk dokumentasi). Verifier harus paham: residue check = Layer 4 teardown + cek tidak ada `Pre Test%{ts}` nyangkut.

**Cara run hanya 2 spec target (Claude's Discretion — urutan/run):**
```bash
# dari direktori tests/ (node_modules sudah ada, Playwright 1.58.2)
cd tests
npx playwright test exam-taking.spec.ts exam-types.spec.ts
# atau satu per satu untuk baseline diagnosa per-file (D-10):
npx playwright test exam-taking.spec.ts
npx playwright test exam-types.spec.ts
```
`setup` + `teardown` tetap jalan untuk subset (project dependency). Mode serial (`test.describe.configure({mode:'serial'})` di kedua file) → sub-test dalam 1 describe stop di kegagalan pertama.

**Auth:** `login(page, role)` (auth.ts:4) → POST `/Account/Login` → `waitForURL('**/Home/**')`. Akun: `hc` = `meylisa.tjiang@pertamina.com`, `coachee` = `rino.prasetyo@pertamina.com`, `coachee2` = `iwan3@pertamina.com` (accounts.ts) — password `123456`. **Tidak pakai storageState** — tiap test login fresh. App WAJIB jalan dengan AD off (D-13) supaya login peserta sukses.

## Selector Drift — Kandidat Kegagalan Non-Judul (Static Prediction)

Spec ditulis Phase 307-319 (era v16-v17, git log terakhir `773301f0` FLOW Y). App berevolusi v18-v25. Setelah judul comply, kandidat drift berikut perlu dicek saat baseline run (D-10) — **prediksi statis, verifikasi via run**:

| Flow | Sub-test | Risiko drift | Evidence |
|------|----------|--------------|----------|
| exam-taking A8 | assert "Your Score"/"100%"/"PASSED" di Results | Results page MC-only OK; tapi teks badge bisa berubah | exam-taking.spec.ts:205-207 hardcoded string EN |
| exam-taking A1 | `#successModal` classList `show` ATAU `.alert-success` | Pola sukses create bisa berubah; saat ini dual-fallback toleran | :55-57 |
| exam-types FLOW K K5 | **SURF-317-A** Results MA page 500 (production bug) | Sudah di-workaround via DB query; kalau bug masih ada, K5 lewat DB OK | exam-types.spec.ts:278-296 — bug terdokumentasi, BUKAN judul. **Jangan fix (D-06)** |
| exam-types FLOW M M5 | Total score 100 via DB | DB-based, robust | :552-575 |
| exam-types FLOW O | SignalR multi-context + AddExtraTime modal | SignalR hub state `Connected` gate; `assessmentHub` global var | :747-754 — fragile kalau hub JS berubah |
| exam-taking H4/H7 | polling `#count-*` + `/Admin/GetMonitoringProgress` JSON | Monitoring page markup + endpoint shape | :1136-1262 — drift risk MEDIUM (monitoring sering disentuh) |
| exam-taking E (Proton T3) | Category='Assessment Proton' + `#protonTrackSelect` + interview form | **Proton overhaul v25.0 (Phase 358-363) banyak sentuh area ini** | :701-805 — drift risk HIGH; mungkin perlu `test.fixme` kalau perilaku Proton berubah total |
| exam-types FLOW Q | EWCD past reject + `Ujian sudah ditutup` | Window-close guard CMPController | :1110-1135 |
| exam-types FLOW R | Certificate PDF download + NomorSertifikat | Cert generation; v24.0 image phase tidak sentuh cert | :1236-1258 |

**Catatan paling penting:** FLOW E (Proton T3 Interview, exam-taking) berisiko drift tertinggi karena Phase 358-363 (PROTON overhaul) banyak mengubah alur Proton/interview/graduation. Kalau E patah karena perubahan perilaku produksi (bukan judul) → kandidat kuat `test.fixme` + temuan backlog (D-05/D-06/D-09).

**Yang BUKAN scope drift:** `tests/e2e/assessment.spec.ts:196` (`uniqueTitle('Phase 308 Standard')`) — file BERBEDA, bukan target Phase 364. Jangan disentuh. Hanya 2 file target.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Generate judul unik | Manual timestamp string | `uniqueTitle(prefix)` existing (utils.ts:23) — ubah ARG saja (D-03) | Timestamp guard sudah handle keunikan; mengubah helper merusak spec lain |
| Snapshot/restore DB | sqlcmd manual ad-hoc | global.setup/teardown lifecycle (otomatis) | Sudah ter-wire, sudah ada Layer 1/4 validation + journal |
| Query DB assertion | Tambah lib SQL baru | `dbSnapshot.queryScalar/queryString` | Sudah ada, localhost-guarded, dipakai exam-types |
| Login | Manual cookie injection | `login(page, role)` helper | Sudah ada, role-mapped |
| Auth bypass lokal | Edit appsettings.json | `Authentication__UseActiveDirectory=false dotnet run` env override (D-13) | appsettings HEAD = AD on (siap handoff); jangan commit perubahan |

**Key insight:** Hampir semua infrastruktur sudah ada. Phase 364 = edit argumen string + cek drift, BUKAN bangun apa pun baru. Zero produksi, zero helper baru (kecuali mungkin 1 import `dbSnapshot` di exam-taking kalau asersi DB ditaruh di sana — tapi rekomendasi taruh di exam-types yang sudah import).

## Common Pitfalls

### Pitfall 1: Menghitung "9 flow" dari roadmap, melewatkan ~12 judul lain
**What goes wrong:** Plan hanya edit 9 judul (Legacy/Token/ForceClose + K/L/M/N/O), lalu exam-taking Package/Proton/Multi/Timer/RealTime/Edit/Abandon + exam-types Q/R/S masih patah → SC#4 tak tercapai.
**Why:** Roadmap & CONTEXT.md under-count. Audit kode = 24 call site, ~21 perlu edit.
**How to avoid:** Pakai §Flow Inventory lengkap. Edit SEMUA judul standard-create, bukan 9.
**Warning signs:** Baseline run (D-10) nunjukin >9 flow gagal di langkah create.

### Pitfall 2: Mengedit judul FLOW P atau FLOW T (yang exempt/N-A)
**What goes wrong:** Edit `[318-P] PrePost Exam` → `Pre Test [318-P]...` bisa MENGGANGGU asersi PrePost (P1 query `AssessmentType IN ('PreTest','PostTest')` + LinkedSessionId). FLOW T (Manual) tidak perlu diubah.
**Why:** P exempt (PrePostTest), T jalur controller beda.
**How to avoid:** D-12 — P ikut full run apa adanya. T biarkan. Jangan tambah prefix di keduanya.

### Pitfall 3: Lupa app harus run dengan AD off
**What goes wrong:** `coachee`/`coachee2` login gagal ("Tidak dapat menghubungi server autentikasi") → semua flow worker-side gagal.
**Why:** appsettings.json HEAD `Authentication:UseActiveDirectory=true` (siap-handoff); lokal tak ada AD.
**How to avoid:** D-13 — `Authentication__UseActiveDirectory=false dotnet run` (env override, JANGAN edit file).
**Warning signs:** login() timeout di `waitForURL('**/Home/**')`.

### Pitfall 4: Global setup collision pre-check gagal (Id 9001-9018 sudah terpakai)
**What goes wrong:** Setup throw "ID range 9001-9018 sudah dipakai" → seluruh run abort sebelum spec jalan.
**Why:** Run sebelumnya tidak teardown bersih (matrix rows nyangkut).
**How to avoid:** Cek SEED_JOURNAL entry matrix terakhir = `cleaned` (saat ini: 2026-06-11 cleaned ✅). Kalau `active`, restore manual snapshot dulu. (Verified journal session ini: bersih.)
**Warning signs:** Setup error di Step 3 collision check.

### Pitfall 5: SURF-317-A (Results MA page 500) disangka regresi judul
**What goes wrong:** FLOW K/M assert via DB karena Results page 500 untuk MA questions — ini production bug LAMA terdokumentasi, bukan akibat judul.
**Why:** `CMPController.Results:2190 ToDictionary` throws untuk MA (multiple PackageUserResponse per question).
**How to avoid:** D-06 — production bug, jangan fix di 364. Spec sudah workaround via DB. Catat sebagai temuan existing kalau muncul.

### Pitfall 6: Menghitung run ber-`fixme` sebagai "PASS penuh"
**What goes wrong:** Pakai `test.fixme` untuk flow yang drift, lalu klaim SC#4 hijau penuh.
**Why:** `fixme` = skip, bukan pass.
**How to avoid:** D-09 — run ber-fixme = jalur SC#4 alternatif "failure terdokumentasi bukan-karena-judul". Jujur ke verifier.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Judul bebas `"Legacy Exam"` | Wajib `^(Pre|Post)\s*Test\s+.+$` | v20.0 (Phase 339, REST-06) | Spec v16-v17 patah di create |
| Auto-pair manual LinkedGroupId | `TryAutoDetectCounterpartGroup` by title | v20.0 (Phase 338) | Title pattern jadi semantik — timestamp jaga isolasi |
| Results MA render UI | DB-based score verify (SURF-317-A workaround) | Phase 317 | exam-types K/M assert via DB, bukan UI |
| Question render = creation order | Anti-cheat shuffle per-session | (CMPController:1188) | Spec pakai DOM-text marker matching, bukan `.nth(N)` |

**Deprecated/outdated dalam spec target:**
- Beberapa assert teks EN hardcoded ("Your Score", "PASSED") — cek apakah UI masih EN atau sudah ID.
- FLOW E Proton interview — alur Proton berubah signifikan v25.0 (Phase 358-363).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | DB lokal saat ini bersih dari Id 9001-9018 (journal matrix terakhir `cleaned`) | Pitfall 4 | LOW — kalau salah, setup abort dengan pesan jelas; restore manual |
| A2 | Selector drift v18-v25 hanya muncul di flow tertentu (E/H tertinggi) | Selector Drift | MEDIUM — drift aktual hanya ketahuan saat baseline run (D-10). Ini sebabnya D-10 wajib |
| A3 | FLOW T (Manual) hijau apa adanya (jalur AddManualAssessment tak kena REST-06) | Flow Inventory | LOW — diverifikasi controller; tapi T bisa patah karena drift lain (TomSelect/ManageAssessment), bukan judul |
| A4 | Blok "Phase 313" exam-taking pakai fixture SQL, tidak kena validator | Flow Inventory | LOW — judul fixture hardcoded di seed, bukan via CreateAssessment. Verifikasi saat run |
| A5 | "Proton T3 Interview" (Category Proton) tetap kena validator standard | Flow Inventory | LOW — `AssessmentTypeInput` tetap ≠ PrePostTest untuk Proton; diverifikasi gate kode |

## Open Questions

1. **Berapa flow yang akan butuh `test.fixme` karena drift v18-v25?**
   - What we know: judul fix menyelesaikan kegagalan create. Drift sekunder ada (E/H/O berisiko).
   - What's unclear: jumlah & flow mana yang benar-benar drift — hanya ketahuan dari baseline run.
   - Recommendation: D-10 baseline run DULU; klasifikasi judul vs non-judul per-flow sebelum estimasi plan size.

2. **Apakah FLOW E (Proton T3) masih relevan setelah overhaul Proton v25.0?**
   - What we know: Phase 358-363 banyak ubah alur Proton/interview/graduation.
   - What's unclear: apakah `#protonTrackSelect`/interview form/Tahun 3 track masih sama.
   - Recommendation: kalau E patah karena perilaku produksi berubah → `test.fixme` + temuan backlog (D-06), bukan fix di 364.

3. **Asersi DB LinkedGroupId (D-11) di flow mana?**
   - What we know: exam-types FLOW K paling murah (sudah import db + ekstrak assessmentId).
   - Recommendation: taruh di K1 atau K5. (Discretion — sudah dipilih K di research.)

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Node + Playwright | run spec | ✓ | @playwright/test 1.58.2 (tests/node_modules ada) | — |
| sqlcmd | dbSnapshot + setup/teardown | ✓ (dipakai phase sebelumnya) | SQL Server Express lokal | — |
| .NET SDK (`dotnet run`) | app @5277 | ✓ | net (project existing) | — |
| SQL Server Express `localhost\SQLEXPRESS` / `HcPortalDB_Dev` | DB lokal | ✓ | MSSQL17 | — |
| App running @localhost:5277 (AD off) | semua flow | harus di-start manual | — | `Authentication__UseActiveDirectory=false dotnet run` |

**Missing dependencies with no fallback:** None — semua infra hadir; app perlu di-start manual dengan AD off saat run.

## Validation Architecture

> nyquist_validation = true (config.json) — section disertakan.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright @playwright/test 1.58.2 (e2e) + xUnit (HcPortal.Tests, .NET) untuk gate suite |
| Config file | `tests/playwright.config.ts` |
| Quick run command | `cd tests && npx playwright test exam-taking.spec.ts` (atau exam-types.spec.ts) |
| Full suite command (gate D-15) | `cd tests && npx playwright test exam-taking.spec.ts exam-types.spec.ts` + `dotnet test HcPortal.Tests` |

### Phase Requirements → Test Map
| SC | Behavior | Test Type | Automated Command | Exists? |
|----|----------|-----------|-------------------|---------|
| SC#1 | Judul lolos validator (create sukses) | e2e | `npx playwright test exam-taking.spec.ts exam-types.spec.ts` — semua create step lolos | ✅ (post-edit judul) |
| SC#2 | FLOW P exempt, fix per-flow | e2e | full run; FLOW P hijau tanpa diedit | ✅ FLOW P :858-1052 |
| SC#3 | LinkedGroupId IS NULL | e2e + DB | asersi `db.queryScalar(...LinkedGroupId IS NULL)` di exam-types FLOW K | ❌ Wave 0 — tambah 1 asersi |
| SC#4 | Kedua spec PASS penuh / failure terdokumentasi | e2e | full run hijau (atau fixme terdokumentasi D-09) | ✅ (post-edit + drift fix) |
| Gate | dotnet test suite hijau | unit | `dotnet test HcPortal.Tests` | ✅ existing suite |

### Sampling Rate
- **Per task (edit judul batch):** `npx playwright test <file>` per-file untuk validasi incremental.
- **Baseline diagnosa (D-10):** run as-is SEBELUM edit apa pun, catat failure per-flow.
- **Phase gate (D-15):** 2 spec target PASS @5277 + `dotnet test` hijau.

### Wave 0 Gaps
- [ ] Asersi DB `LinkedGroupId IS NULL` di exam-types FLOW K (SC#3 / D-11) — 1 `expect` + reuse `db.queryScalar`.
- [ ] (Opsional) Baseline diagnosa run output dicatat di SUMMARY (D-10) — bukan file test, tapi prasyarat plan.
- *Tidak ada framework install gap — Playwright + node_modules + xUnit sudah ada.*

## Security Domain

> security_enforcement tidak di-set di config (default enabled), TAPI fase ini **test-only, zero produksi, zero network baru, zero input user baru**. ASVS minimal applicable.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V5 Input Validation | indirect | REST-06 title regex SUDAH ada di produksi (read-only); fase ini comply, bukan ubah |
| V6 Cryptography | no | — |
| Lainnya | no | Test-only, tidak ada surface baru |

### Known Threat Patterns
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| SQL injection via DB assertion | Tampering | Query D-11 pakai integer `assessmentId` (parameter-free literal int, bukan string user). exam-types existing pattern aman. Hindari interpolasi string mentah ke SQL kecuali int. |
| Snapshot/restore non-localhost | Tampering | `dbSnapshot.runSqlcmd` REJECT non-localhost `-S` (guard T-315-01) — sudah ter-enforce |

## Sources

### Primary (HIGH confidence — dibaca verbatim session ini)
- `Controllers/AssessmentAdminController.cs:820-909, 7095-7158` — validator REST-06 + auto-pair + helper.
- `Controllers/TrainingAdminController.cs:642-712` — AddManualAssessment (no REST-06).
- `tests/e2e/exam-taking.spec.ts` (1717 baris penuh) — flow A-J + Phase 313 block.
- `tests/e2e/exam-types.spec.ts` (2153 baris penuh) — W0/K/L/M/N/O/P/Q/R/S/T/U/V/W/X/Y.
- `tests/helpers/utils.ts:23` — uniqueTitle. `tests/helpers/dbSnapshot.ts` — query helpers. `tests/helpers/auth.ts` + `accounts.ts` — login.
- `tests/e2e/helpers/examTypes.ts:51-104, 538-608` — wizard helpers. `tests/e2e/helpers/wizardSelectors.ts` — selectors.
- `tests/playwright.config.ts` + `tests/e2e/global.setup.ts` + `global.teardown.ts` — lifecycle.
- `.planning/config.json`, `docs/SEED_JOURNAL.md` (tail) — flags + DB clean-state.
- `Grep uniqueTitle\(` — 24 call site enumerasi.

### Secondary (MEDIUM)
- `.planning/phases/355-test-uat/355-03-SUMMARY.md` — pola fix judul terbukti (Deviasi 1/2/3).

### Tertiary (LOW)
- Memory index (Phase 358-363 Proton overhaul) — basis prediksi drift FLOW E (perlu verifikasi via run).

## Metadata

**Confidence breakdown:**
- Validator/auto-pair mechanics: HIGH — kode dibaca verbatim, logika di-trace.
- Flow inventory (24 site, ~21 edit): HIGH — grep + baca setiap call site.
- Lifecycle snapshot/restore: HIGH — setup/teardown dibaca penuh.
- Selector drift prediction: MEDIUM — prediksi statis; aktual hanya dari baseline run (D-10).
- FLOW T/Phase-313 exempt: MEDIUM-HIGH — controller diverifikasi, perilaku run belum.

**Research date:** 2026-06-11
**Valid until:** 2026-07-11 (stable; kode produksi read-only, spec target jarang berubah). Re-verify drift kalau ada fase yang sentuh area exam/Proton sebelum eksekusi.
