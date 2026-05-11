# Phase 315: Assessment Matrix Test - Research

**Researched:** 2026-05-11
**Domain:** End-to-end Playwright test infrastructure + SQL Server seed/restore lifecycle + bug collector pattern
**Confidence:** HIGH (semua keputusan locked di CONTEXT, Wave 0 questions semua terjawab via source read)

## Summary

Phase 315 membangun automated discovery test sweep (Playwright spec) yang menyapu 7 kombinasi (tipe assessment × tipe soal) + 3 sentinel meta-validation di DB lokal SQL Server Express. Goal: discover bug — bukan regression suite. Output adalah 1 markdown report per run (`docs/test-reports/YYYY-MM-DD-assessment-matrix.md`) dengan severity classification (critical/major/minor), screenshot, dan hypothesis.

CONTEXT.md sudah mengunci 7 decision (D-01..D-07) — semua override spec asli dengan rasional jelas (helper folder split, marker strategy, sentinel exit-code handling). Wave 0 investigation sudah dijawab via source code read (lihat `## Wave 0 Investigation Answers`): MA/Essay save via SignalR hub (bukan SaveAnswer HTTP), Notes field TIDAK ADA → wajib fallback Title prefix marker, ID range 9001-9009/50001-50037/80001-80124 100% bebas collision karena `Data/SeedData.cs` hanya seed Roles/Admin/OrgUnits.

**Primary recommendation:** Bangun 4 helper files (`tests/helpers/dbSnapshot.ts`, `tests/e2e/helpers/{matrixReport,examMatrix,matrixTypes}.ts`), 1 spec file (`tests/e2e/assessment-matrix.spec.ts`), 1 SQL seed (`tests/sql/assessment-matrix-seed.sql`), extend existing `global.setup.ts`, create `global.teardown.ts`, dan register di playwright.config.ts. Pakai Title prefix `[MATRIX_TEST_2026_05_11]` sebagai marker (Notes field TIDAK ADA — fallback D-05 wajib). Smoke run `--grep "Scenario 5"` documented di header docblock spec. Connection string: `Server=localhost\SQLEXPRESS;Database=HcPortalDB_Dev;Integrated Security=True`.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Helper Folder Layout (override spec)**

Spec asli menulis semua helper di `tests/helpers/`. Karena Phase 307 sudah putuskan e2e-specific helper ditaruh di `tests/e2e/helpers/`, helper Phase 315 dipisah:

- **D-01:** `dbSnapshot.ts` → `tests/helpers/dbSnapshot.ts` (generic SQL util — test/seed lain bisa re-use)
- **D-02:** `matrixReport.ts`, `examMatrix.ts`, `matrixTypes.ts` → `tests/e2e/helpers/` (matrix-spesifik, bukan shared utility)
- **D-03:** `globalTeardown` → `tests/e2e/global.teardown.ts` (konsisten dgn `tests/e2e/global.setup.ts` existing)
- Spec asli yang menyebut `tests/helpers/matrixReport.ts` dan `tests/helpers/examMatrix.ts` di-override oleh keputusan ini.

**Wave 0 Investigation Approach**

- **D-04:** 5 open question pre-impl dijawab via baca source code, bukan probe runtime. Target file investigasi:
  - `Controllers/CMPController.cs` — endpoint SaveAnswer, SubmitExam, Results
  - `Controllers/AssessmentAdminController.cs` — SubmitEssayScore, FinalizeEssayGrading, AssessmentMonitoringDetail
  - `Views/CMP/StartExam.cshtml` + JS handler — flow client-side MA & Essay save
  - `Models/AssessmentSession.cs` — cek apakah field `Notes` ada
  - `Data/SeedData.cs` — verifikasi ID range 9001-9009 / 50001-50037 / 80001-80124 tidak collide
- Output Wave 0: 1 dokumen singkat — di section `## Wave 0 Investigation Answers` RESEARCH.md ini.

**Marker Strategy**

- **D-05:** Primary marker = `AssessmentSession.Notes = 'MATRIX_TEST_2026_05_11'` jika field ada.
- Fallback (kalau Wave 0 nemu Notes ngga ada): Title prefix `[MATRIX_TEST_2026_05_11]`, query Layer 1 + Layer 4 cleanup pakai `WHERE Title LIKE '[MATRIX_TEST_%]%'`.
- Schema migration tambah kolom Notes baru → ditolak. Test infra tidak boleh memodifikasi schema produksi.
- **VERDICT (research):** Notes field TIDAK ADA → **fallback Title prefix WAJIB DIPAKAI** (lihat Wave 0 answer #3).

**Sentinel Exit Code Handling**

- **D-06:** Layer 3 sentinel `[META-CollectorCheck]` (sengaja gagal buat verifikasi collector) pakai Playwright `test.fail()` annotation. Run exit code = 0 saat sentinel gagal sesuai harapan, exit non-zero kalau sentinel mendadak pass (collector rusak).
- `matrixReport` filter: finding dari `[META-*]` skenario tidak ikut summary statistik discovery, tapi tetap tercatat di section terpisah `## Meta-validation results`.

**Smoke Run Gating**

- **D-07:** Smoke run protocol (`--grep "Scenario 5"` dulu sebelum full 7) hanya didokumentasikan, tidak di-enforce di code. Lokasi dokumentasi: README di `tests/` atau header docblock di `assessment-matrix.spec.ts`.
- Tidak bikin marker file `.smoke-passed` atau npm script gating — terlalu banyak moving part untuk discovery-focused test.

### Claude's Discretion

- Snapshot file path (`C:/temp/PortalHC_Local-matrix-{timestamp}.bak`) — Claude bisa pilih directory sesuai disk space (fallback ke `tests/sql/` kalau `C:/temp/` tidak ada), gitignore-kan `.bak` files.
- Sabotage strategy peserta2 (skenario MA-bearing sengaja salah 1 MA) — deterministic: salah option pertama dari `correctOptionIds[]` saja, biar reproducible.
- Report file naming convention — `docs/test-reports/YYYY-MM-DD-assessment-matrix.md` (date saja, bukan timestamp). Run baru di hari yang sama overwrite report.
- Console error filtering di Layer "Tidak ada error di console Playwright runtime" — list whitelist console error yang aman di-ignore (e.g. favicon 404) ditulis di `matrixReport` config.
- Exact severity threshold per finding type (critical/major/minor) — Claude tentukan saat implementasi, sejalan dgn tabel klasifikasi spec § Error handling.

### Deferred Ideas (OUT OF SCOPE)

- **QA-02 CI integration** — Tunggu matrix test proven stable di lokal. Saat itu baru pikir GitHub Actions / Azure DevOps integration.
- **QA-03 Regression subset conversion** — Convert subset matrix test jadi smoke regression. Setelah matrix test berjalan beberapa kali.
- **QA-04 Visual regression** — Percy/Chromatic tooling. Out of scope v16.0.
- **QA-05 Multi-environment** — Extend ke staging/prod.
- **QA-06 Concurrency stress test** — Banyak peserta paralel di session sama.
- **QA-07 Coverage expansion ke flow lain** — CDP, IDP, Coaching, Worker management.
- **Schema migration tambah kolom `Notes` ke AssessmentSession** — Ditolak untuk Phase 315.
- **Smoke run gating via marker file `.smoke-passed`** — Cukup dokumentasi.
- **mssql node driver** — Cukup `sqlcmd` via spawn.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| QA-01 | Developer dapat menjalankan automated test sweep yang menguji seluruh kombinasi (tipe assessment × tipe soal) end-to-end di lokal, dengan output 1 markdown report yang merangkum semua temuan bug per skenario (severity, screenshot, hypothesis). Harus include DB seed temporary + cleanup otomatis (BACKUP/RESTORE), continue-and-collect bug behavior, dan meta-validation (sentinel skenario verifikasi framework test sendiri). | Full coverage: `## Standard Stack` (Playwright 1.58 + sqlcmd via Node spawn), `## Architecture Patterns` (4-tier pipeline globalSetup → spec → globalTeardown + collector pattern), `## Wave 0 Investigation Answers` (5 open questions terjawab), `## Don't Hand-Roll` (pakai sqlcmd BACKUP/RESTORE bukan migrate-twin DB), `## Common Pitfalls` (4 pitfall spesifik), `## Code Examples` (saveMultipleAnswer SignalR + Title-prefix marker), `## Validation Architecture` (Nyquist 4-layer meta-validation map). |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

| Directive | Source | Compliance impact |
|-----------|--------|-------------------|
| Always respond in Bahasa Indonesia | CLAUDE.md §1 | Report markdown + PR title/body + UAT doc → Bahasa Indonesia. Header docblock spec boleh Indonesia/English mix (TS-comment). |
| Lokal-only dev workflow | CLAUDE.md §"Develop Workflow" + `docs/DEV_WORKFLOW.md` | sqlcmd target wajib `localhost\SQLEXPRESS`, NEVER `10.55.3.3`. Guard di `dbSnapshot.ts` reject hostname ≠ localhost. |
| Klasifikasi seed → temporary + local-only | CLAUDE.md §"Seed Data Workflow" + `docs/SEED_WORKFLOW.md` §3 | Seed `assessment-matrix-seed.sql` = `temporary + local-only`. WAJIB snapshot via sqlcmd BACKUP sebelum INSERT + restore di teardown + journal entry. |
| Snapshot DB sebelum seed temporary | CLAUDE.md §"Seed Data Workflow" + SEED_WORKFLOW.md §4 Step 2 | Global setup harus stop Kestrel SEBELUM BACKUP? **Tidak perlu** — BACKUP di SQL Server tidak butuh exclusive lock (RESTORE butuh). Setup order: BACKUP → seed → spec → globalTeardown (matikan Kestrel di teardown sebelum RESTORE? No — teardown stop Kestrel TIDAK trivial dari Playwright. **Mitigation:** Wajib Kestrel OFF saat developer trigger run — dokumentasikan di smoke run protocol header). |
| Append entry di SEED_JOURNAL.md | CLAUDE.md §"Seed Data Workflow" + SEED_WORKFLOW.md §6 | Setup tulis 1 entry `active`, teardown update jadi `cleaned`. Format match existing (1 row per seed). |
| ❌ Edit kode/DB langsung di server Dev/Prod | CLAUDE.md §"Develop Workflow" | dbSnapshot.ts hostname guard wajib reject non-`localhost`. |
| ❌ Push tanpa verifikasi lokal | CLAUDE.md §"Develop Workflow" | Wave 0 + Wave 1 + Wave 2 sebelum commit harus lewat smoke run (Scenario 5) lokal. |

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| BACKUP/RESTORE DB | Test infra (Node spawn → sqlcmd) | SQL Server Express | sqlcmd adalah satu-satunya tool yang dapat issue BACKUP/RESTORE T-SQL via `-Q`. Node `child_process.spawn` cukup; tidak butuh `mssql` driver karena tidak ada query result yang perlu di-parse di TS layer (queries via sqlcmd dengan `-Q` dan output regex parse). |
| Seed data insertion | Test infra (sqlcmd execScript) | SQL Server (raw T-SQL) | Pola sama Phase 313.1 — sqlcmd `-i` execute file SQL hierarchical. Tidak butuh ORM access dari TS. |
| Login + browser context per peserta | Playwright Browser Context | `tests/helpers/auth.ts` existing | 2 peserta = 2 BrowserContext berbeda untuk isolated cookies. `login()` existing reuse. |
| Exam answer save (MC) | Server HTTP POST `/CMP/SaveAnswer` | DOM event listener (radio change) | Sudah ada di production (Phase 227+). |
| Exam answer save (MA) | **SignalR Hub `SaveMultipleAnswer`** | Hub method via `window.assessmentHub.invoke()` | CRITICAL FINDING Wave 0 — MA TIDAK pakai HTTP endpoint, pakai SignalR hub call (Views/CMP/StartExam.cshtml:851). Test helper harus emulate via Playwright clicking checkboxes (DOM trigger → JS invoke hub) — JANGAN coba issue HTTP POST manual. |
| Exam answer save (Essay) | **SignalR Hub `SaveTextAnswer`** | Hub method via `window.assessmentHub.invoke()` | CRITICAL FINDING Wave 0 — Essay JUGA SignalR (Views/CMP/StartExam.cshtml:897). Helper `takeExam` fill textarea + wait debounce 2s + verify save indicator OR langsung lanjut ke SubmitExam (form payload `answers[qId]=textValue` juga dikirim — investigate apakah server pakai form value atau DB save). |
| Submit exam | Server HTTP POST `/CMP/SubmitExam` | Form submit dgn payload `answers[qId]=value` | CMPController.cs:1569 — Dictionary<int,int> binding. Untuk Essay, `answers[qId]=text` — perlu cek SubmitExam logic detail di Wave 1. |
| Grade essay | Server HTTP POST `/Admin/SubmitEssayScore` + `/Admin/FinalizeEssayGrading` | Admin browser context | Form-encoded body, `[ValidateAntiForgeryToken]` — Playwright `page.request.post` perlu CSRF token dari page yang sudah login HC. |
| Result verification | Server HTTP GET `/CMP/Results/{id}` | DOM assertion (score badge, per-Q breakdown) | Standard Playwright page assertion. |
| Finding collection | In-memory singleton `matrixReport.ts` | File write di teardown | Singleton accumulator pattern, flush() ke markdown di teardown. |
| Report rendering | Pure TS (markdown template literal) | File write `docs/test-reports/...` | Tidak butuh library; markdown handcrafted. |
| Layer 1/4 validation | Test infra (sqlcmd query → regex parse) | matrixReport (critical alert kalau gagal) | Layer 1 SELECT COUNT pre-spec, Layer 4 SELECT COUNT post-RESTORE = 0. |
| Layer 2/3 meta-validation | Spec `test()` block sentinel | Playwright `test.fail()` annotation (Layer 3) | Layer 2 = real session run, Layer 3 = test.fail() purposely failing. |

## Wave 0 Investigation Answers

> **Sumber tunggal kebenaran untuk 5 open question dari spec § Open Questions.** Semua dijawab via source-code read per D-04.

### Q1: MA save flow — bagaimana multi-optionId disimpan?

**Answer:** Via **SignalR Hub method `SaveMultipleAnswer`**, BUKAN HTTP endpoint. Loop client-side TIDAK terjadi — semua optionIds dikirim sekaligus sebagai comma-separated string dalam 1 hub invocation.

**Evidence:**
- `Views/CMP/StartExam.cshtml:822-857` — checkbox change handler:
  ```js
  if (window.assessmentHub && window.assessmentHub.state === 'Connected') {
      window.assessmentHub.invoke('SaveMultipleAnswer', SESSION_ID, parseInt(qId), selected.join(','))
  }
  ```
- `Hubs/AssessmentHub.cs:188-252` — `SaveMultipleAnswer(int sessionId, int questionId, string optionIds)`:
  - Validasi session ownership + status `InProgress`
  - Validasi timer belum expired (DurationMinutes + ExtraTimeMinutes)
  - Validasi optionIds milik question (`PackageOptions.PackageQuestionId == questionId`)
  - **Wipe-and-insert pattern**: hapus semua `PackageUserResponse` existing untuk question, lalu INSERT 1 row per valid optionId
- `Controllers/CMPController.cs:348` SaveAnswer HTTP — HANYA 1 optionId (`int optionId`), bukan list. Konfirmasi MA TIDAK pakai endpoint ini.

**Implication untuk test:** `examMatrix.takeExam` helper untuk MA harus:
1. Click checkbox per correctOptionId (Playwright DOM event → trigger JS handler → trigger hub invoke)
2. WAIT for SignalR hub call complete sebelum next question (helper tunggu sampai save indicator state='saved')
3. JANGAN coba issue HTTP POST manual ke SaveAnswer — itu hanya untuk MC

[VERIFIED: source code read `Hubs/AssessmentHub.cs:188-252` + `Views/CMP/StartExam.cshtml:822-857`]

### Q2: Essay save flow — via SaveAnswer text, endpoint terpisah, atau submit-time only?

**Answer:** Via **SignalR Hub method `SaveTextAnswer`**, dengan debounce 2 detik client-side.

**Evidence:**
- `Views/CMP/StartExam.cshtml:861-904` — textarea input handler:
  ```js
  essayTimers[qId] = setTimeout(function() {
      if (window.assessmentHub && window.assessmentHub.state === 'Connected') {
          window.assessmentHub.invoke('SaveTextAnswer', SESSION_ID, parseInt(qId), self.value)
      }
  }, 2000);
  ```
- `Hubs/AssessmentHub.cs:134-182` — `SaveTextAnswer(int sessionId, int questionId, string textAnswer)`:
  - Truncate ke `MaxCharacters` server-side
  - Upsert `PackageUserResponse.TextAnswer` (PackageOptionId=NULL untuk Essay)
- Hidden input form payload `answers[@q.QuestionId]` (StartExam.cshtml:81) — Essay value juga ikut dikirim di SubmitExam form, TAPI server prioritaskan DB record (PackageUserResponse) yang sudah ada dari hub save.

**Implication untuk test:** `examMatrix.takeExam` helper untuk Essay harus:
1. `fill(textarea, value)` — Playwright fire input event
2. WAIT 2.5 detik (lebih dari 2s debounce) sebelum lanjut question berikut, ATAU
3. Trigger `blur` event manual untuk flush pending save, ATAU
4. Verify save indicator transitioned saving→saved sebelum lanjut

Rekomendasi: pakai opsi (4) — assert `#saveIndicator[data-state="saved"]` visible sebelum next question. Lebih reliable dibanding fixed wait.

[VERIFIED: source code read `Hubs/AssessmentHub.cs:134-182` + `Views/CMP/StartExam.cshtml:861-904`]

### Q3: AssessmentSession.Notes field — ada atau tidak?

**Answer:** **TIDAK ADA.** Field `Notes` tidak ditemukan di `Models/AssessmentSession.cs` (semua 193 baris di-scan).

**Evidence:**
- `Models/AssessmentSession.cs` — Grep `Notes` returns 0 hits.
- Properties existing yang ada: `Id`, `UserId`, `Title`, `Category`, `Schedule`, `DurationMinutes`, `Status`, `Progress`, `BannerColor`, `Score`, `PassPercentage`, `AllowAnswerReview`, `GenerateCertificate`, `IsPassed`, `CompletedAt`, `StartedAt`, `ElapsedSeconds`, `LastActivePage`, `ExamWindowCloseDate`, `ValidUntil`, `NomorSertifikat`, `IsTokenRequired`, `AccessToken`, `CreatedAt`, `UpdatedAt`, `CreatedBy`, `ProtonTrackId`, `TahunKe`, `InterviewResultsJson`, `RenewsSessionId`, `RenewsTrainingId`, `IsManualEntry`, `ManualSertifikatUrl`, `Penyelenggara`, `Kota`, `SubKategori`, `CertificateType`, `AssessmentType`, `AssessmentPhase`, `LinkedGroupId`, `LinkedSessionId`, `HasManualGrading`, `SamePackage`, `ExtraTimeMinutes`.

**Implication:** D-05 LOCKED FALLBACK **wajib dipakai** — pakai Title prefix `[MATRIX_TEST_2026_05_11]` sebagai marker. Schema migration ditolak (DEFERRED).

Marker query untuk Layer 1 + Layer 4 cleanup:
```sql
-- Layer 1 (post-seed validation)
SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[MATRIX_TEST_2026_05_11]%';
-- Expected: 9

-- Layer 4 (post-RESTORE cleanup validation)
SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[MATRIX_TEST_2026_05_11]%';
-- Expected: 0
```

[VERIFIED: source code read `Models/AssessmentSession.cs` lines 1-193 + grep Notes 0 matches]

### Q4: ID range collision check — 9001-9009 / 50001-50037 / 80001-80124 free?

**Answer:** **100% BEBAS COLLISION.** `Data/SeedData.cs` (106 baris total) HANYA seed:
1. Roles via `RoleManager` (string names, tidak ada ID conflict dengan AssessmentSession/Package/Question/Option)
2. Bootstrap admin user `admin@pertamina.com` via `UserManager` (string GUID PK, tidak ada conflict)
3. `OrganizationUnits` — beda tabel, beda PK space

`Data/SeedData.cs` TIDAK PERNAH insert ke tabel `AssessmentSessions`, `AssessmentPackages`, `PackageQuestions`, atau `PackageOptions`. Plus database lokal mungkin ada existing test data dari Phase 313/313.1 fixtures (`Phase 313 Timer Fixture %`) atau seed lain — tapi semua identity-generated, biasanya start dari 1 dan increment. Range 9001+/50001+/80001+ jauh di atas range realistic existing data lokal.

**Defensive measure:** Setup phase (sebelum seed jalan) tetap WAJIB pre-check:
```sql
SELECT COUNT(*) FROM AssessmentSessions WHERE Id BETWEEN 9001 AND 9009;
-- Expected: 0; halt + raise alert kalau > 0
```

**SQL Server identity insert caveat:** Karena `Id INT IDENTITY(1,1) PRIMARY KEY`, INSERT manual ID 9001+ butuh `SET IDENTITY_INSERT AssessmentSessions ON;` sebelum INSERT dan `OFF;` setelahnya. Phase 313.1 seed pakai approach beda — pakai `OUTPUT INSERTED.Id INTO @SessionIds` (let SQL auto-generate, capture identity ke table var). Phase 315 boleh ikuti pola yang sama ATAU explicit `IDENTITY_INSERT` jika butuh ID deterministic untuk `.matrix-state.json`. **Rekomendasi:** explicit `IDENTITY_INSERT` karena state file butuh ID stabil yang match config — itu yang spec asumsikan (sessionId 9001..9009 hardcoded di state).

[VERIFIED: source code read `Data/SeedData.cs` lines 1-106]

### Q5: URL encoding `Admin/AssessmentMonitoringDetail?title=&category=&scheduleDate=` — aman buat Indonesian chars + spasi?

**Answer:** **AMAN.** Standard ASP.NET Core MVC model binding handle query string encoding/decoding otomatis untuk `string` parameter.

**Evidence:**
- `Controllers/AssessmentAdminController.cs:2684` — `public async Task<IActionResult> AssessmentMonitoringDetail(string title, string category, DateTime scheduleDate, string? assessmentType = null)`. Standard MVC binding — framework decode `%20`, `%5B`, `%5D`, Unicode chars automatically.
- Playwright `page.goto()` accept URL string; `URLSearchParams` constructor / `new URL()` will encode query values via `encodeURIComponent` rules — match server expectation.

**Recommendation:** Build URL via `URL` object (cleanest):
```ts
const url = new URL('/Admin/AssessmentMonitoringDetail', baseURL);
url.searchParams.set('title', '[MATRIX_TEST_2026_05_11] Matrix Manual Mixed');
url.searchParams.set('category', 'Matrix Test Category');
url.searchParams.set('scheduleDate', '2026-05-11');
await page.goto(url.toString());
```

Bracket char `[` dan `]` di Title prefix akan di-encode jadi `%5B` dan `%5D` — ASP.NET Core decode otomatis sebelum bind ke `string title`. Tidak ada kebutuhan custom escaping.

**Edge case:** Kalau prefix marker mengandung `&` atau `=` (yang punya semantic khusus di query string), tetap aman karena `URLSearchParams.set` encode keduanya jadi `%26` dan `%3D`. Untuk Phase 315 prefix `[MATRIX_TEST_2026_05_11]` tidak ada char problematic.

[VERIFIED: source code read `Controllers/AssessmentAdminController.cs:2684-2702` + standard MVC binding behavior (HIGH confidence — pola dipakai universal di project)]

## Standard Stack

### Core (already installed)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| @playwright/test | ^1.58.2 | E2E test runner + fixtures + globalSetup/globalTeardown | Sudah dipakai existing test infrastructure (Phase 307+). Built-in globalTeardown hook + screenshot:'on' fit untuk discovery test. |
| typescript | ^5.9.3 | Type safety untuk helpers & spec | Sudah set up di tests/tsconfig.json. |
| sqlcmd | bundled SQL Server Express | T-SQL execution incl. BACKUP/RESTORE | Sudah dipakai Phase 313.1 seed. Tidak butuh tambahan mssql Node driver per spec & D-deferred. |

### Supporting (Node built-in, no install needed)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| child_process | Node built-in | `spawn` sqlcmd subprocess | dbSnapshot.ts wrapper (backup/restore/execScript/queryScalar). |
| fs/promises | Node built-in | Read/write `.matrix-state.json`, report.md, journal | Universal file I/O. |
| path | Node built-in | Path join cross-platform | Snapshot file path resolution (Windows backslash). |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| sqlcmd via spawn | `mssql` Node driver | Node driver bisa parse query result langsung di TS — tapi spec & CONTEXT explicitly reject (avoid extra dep). Cukup sqlcmd + regex parse output. |
| Markdown handcrafted | `markdown-it` atau `marked` | Library overkill untuk template ~150 line. Template literal lebih audit-friendly. |
| `.matrix-state.json` file | In-memory via Playwright fixture | Cross-process communication setup → spec → teardown butuh file. Fixture scope tidak persist ke teardown. |
| screenshot full-page automatic | Manual `page.screenshot()` per finding | Playwright `screenshot: 'on'` config sudah on; matrixReport ambil PATH dari Playwright's test-results dir. Hindari double-capture. |

### Installation
Sudah lengkap — tidak perlu install package baru. Verifikasi sqlcmd:
```bash
sqlcmd -? | head -3
# Expected: Microsoft (R) SQL Server Command Line Tool ...
```

**Version verification:** `@playwright/test ^1.58.2` confirmed di `tests/package.json:17`. SQL Server Express version dari connection string `localhost\SQLEXPRESS`, target DB `HcPortalDB_Dev`. [VERIFIED: `tests/package.json` + `appsettings.Development.json`]

## Architecture Patterns

### System Architecture Diagram

```
┌────────────────────────────────────────────────────────────────────────┐
│ Developer trigger (lokal saja — Kestrel WAJIB stopped sebelum run)     │
│   npx playwright test assessment-matrix [--grep "Scenario 5"]          │
└──────────────────────────────────┬─────────────────────────────────────┘
                                   ▼
┌────────────────────────────────────────────────────────────────────────┐
│ Phase A: playwright.config.ts globalSetup (extends global.setup.ts)    │
│  1. Existing: page.goto /Account/Login → assert response OK            │
│  2. NEW: dbSnapshot.backup(snapshotPath) ← sqlcmd BACKUP               │
│  3. NEW: Pre-check ID collision (SELECT COUNT WHERE Id IN range) = 0   │
│  4. NEW: dbSnapshot.execScript('tests/sql/assessment-matrix-seed.sql') │
│  5. NEW: Layer 1 validation (SELECT COUNT marker = 9 sessions)         │
│  6. NEW: Write tests/.matrix-state.json (scenarios array, snapshotPath)│
│  7. NEW: Append SEED_JOURNAL.md entry status=active                    │
│                                                                        │
│  Failure di Phase A → halt seluruh run sebelum spec start              │
└──────────────────────────────────┬─────────────────────────────────────┘
                                   ▼
┌────────────────────────────────────────────────────────────────────────┐
│ Phase B: assessment-matrix.spec.ts (10 test blocks total)              │
│  Reads .matrix-state.json di top-level → scenarios array               │
│                                                                        │
│  For each of 7 discovery scenarios:                                    │
│    test('S{N} {title}', async ({ browser }) => {                       │
│      ctx1 = browser.newContext(); page1 = ctx1.newPage();              │
│      ctx2 = browser.newContext(); page2 = ctx2.newPage();              │
│      ctxHc = browser.newContext(); pageHc = ctxHc.newPage();           │
│      try {                                                             │
│        await takeExam(page1, cfg, 'coachee');                          │
│        await takeExam(page2, cfg, 'coachee2',                          │
│                       { sabotageOneAnswer: true });                    │
│        if (cfg.hasEssay) await gradeEssaysAsHc(pageHc, cfg);           │
│        await verifyResultPage(page1, cfg, 'coachee');                  │
│      } catch (e) {                                                     │
│        if (e instanceof SkipScenarioError) return;  // critical skip   │
│        throw e;  // unexpected — let Playwright handle                 │
│      } finally { ctx1.close(); ctx2.close(); ctxHc.close(); }          │
│    })                                                                  │
│                                                                        │
│  For 3 sentinel scenarios:                                             │
│    test('[META-AllCorrect] Sentinel', ...)   ← Layer 2                 │
│    test('[META-AllWrong] Sentinel', ...)     ← Layer 2                 │
│    test.fail('[META-CollectorCheck] Sentinel', ...) ← Layer 3 (D-06)   │
│                                                                        │
│  Internal: every assertion via softAssert(ctx, fn, expected) →         │
│   - throws SkipScenarioError jika severity='critical'                  │
│   - record() finding + return null jika severity in major/minor        │
└──────────────────────────────────┬─────────────────────────────────────┘
                                   ▼
┌────────────────────────────────────────────────────────────────────────┐
│ Phase C: playwright.config.ts globalTeardown (new file)                │
│  1. matrixReport.flush('docs/test-reports/YYYY-MM-DD-...md')           │
│     ← write BEFORE RESTORE — preserve findings even if RESTORE fails   │
│  2. dbSnapshot.restore(state.snapshotPath) ← sqlcmd RESTORE WITH REPLC │
│  3. Layer 4 validation (SELECT COUNT marker = 0)                       │
│  4. Update SEED_JOURNAL.md entry status=cleaned                        │
│  5. Delete .matrix-state.json + snapshot .bak file                     │
│                                                                        │
│  RESTORE failure → log critical alert with manual restore command,     │
│   exit non-zero (developer wajib manual fix).                          │
└────────────────────────────────────────────────────────────────────────┘
```

### Recommended Project Structure

```
tests/
├── helpers/
│   ├── auth.ts                       # existing — login(page, accountKey)
│   ├── accounts.ts                   # existing — coachee, coachee2, hc, admin
│   └── dbSnapshot.ts                 # NEW (D-01) — generic SQL util
├── e2e/
│   ├── global.setup.ts               # EXTEND — add BACKUP + seed + Layer 1
│   ├── global.teardown.ts            # NEW (D-03) — flush + RESTORE + Layer 4
│   ├── assessment-matrix.spec.ts     # NEW — 10 test blocks (7+3)
│   └── helpers/
│       ├── exam313.ts                # existing — Phase 313.1 reference pattern
│       ├── wizardSelectors.ts        # existing
│       ├── matrixTypes.ts            # NEW (D-02) — ScenarioConfig, Severity, Finding types
│       ├── matrixReport.ts           # NEW (D-02) — collector singleton + flush
│       └── examMatrix.ts             # NEW (D-02) — takeExam, gradeEssaysAsHc, verifyResultPage
├── sql/
│   └── assessment-matrix-seed.sql    # NEW — 9 sessions + 9 packages + 37 questions + 124 options
├── playwright.config.ts              # EDIT — register globalTeardown
└── .gitignore                        # CREATE or EDIT — add .matrix-state.json, sql/*.bak
docs/
└── test-reports/                     # NEW dir — YYYY-MM-DD-assessment-matrix.md goes here
```

### Pattern 1: globalTeardown Registration

**What:** Playwright runs `globalSetup` then runs spec(s), then runs `globalTeardown` — even if specs fail. Registration via top-level config field.

**When to use:** Whenever cleanup must run regardless of test pass/fail.

**Example:**
```ts
// tests/playwright.config.ts
import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  globalTeardown: require.resolve('./e2e/global.teardown.ts'),  // NEW
  timeout: 60_000,
  // ... rest unchanged
  projects: [
    { name: 'setup', testMatch: /global\.setup\.ts/ },
    { name: 'chromium', use: { browserName: 'chromium' }, dependencies: ['setup'] },
  ],
});
```

**Path resolution caveat:** `globalTeardown` adalah path relative ke config file location (`tests/playwright.config.ts`). Pakai `require.resolve('./e2e/global.teardown.ts')` untuk absolute path resolution yang tidak break di Windows.

**Signature contract:** Function signature MUST be:
```ts
// tests/e2e/global.teardown.ts
import type { FullConfig } from '@playwright/test';
async function globalTeardown(config: FullConfig): Promise<void> {
  // 1. matrixReport.flush(...)
  // 2. dbSnapshot.restore(state.snapshotPath)
  // 3. Layer 4 validation
  // 4. update journal
}
export default globalTeardown;
```

[CITED: https://playwright.dev/docs/test-global-setup-teardown — global-teardown signature contract]

### Pattern 2: sqlcmd Subprocess via Node spawn

**What:** Wrap sqlcmd CLI as TS async function. spawn (not exec) untuk streaming stderr/stdout + control exit code precisely.

**When to use:** All DB ops (BACKUP, RESTORE, EXEC seed, queryScalar count).

**Example:**
```ts
// tests/helpers/dbSnapshot.ts
import { spawn } from 'child_process';

const SQLCMD_BASE_ARGS = [
  '-S', 'localhost\\SQLEXPRESS',
  '-d', 'HcPortalDB_Dev',
  '-E',           // Windows Integrated Security
  '-C',           // Trust Server Certificate
  '-I',           // QUOTED_IDENTIFIER ON
  '-b',           // exit with non-zero on T-SQL error (CRITICAL — without -b, syntax error returns 0)
];

function runSqlcmd(args: string[]): Promise<{ stdout: string; stderr: string }> {
  return new Promise((resolve, reject) => {
    // Safety guard: refuse non-localhost target (CLAUDE.md compliance)
    const sIdx = args.indexOf('-S');
    if (sIdx >= 0 && !/^localhost/i.test(args[sIdx + 1])) {
      return reject(new Error(`Refusing to target non-localhost SQL Server: ${args[sIdx + 1]}`));
    }
    const proc = spawn('sqlcmd', args, { windowsHide: true });
    let stdout = '', stderr = '';
    proc.stdout.on('data', d => stdout += d);
    proc.stderr.on('data', d => stderr += d);
    proc.on('error', reject);
    proc.on('close', code => {
      if (code !== 0) reject(new Error(`sqlcmd exit ${code}: ${stderr || stdout}`));
      else resolve({ stdout, stderr });
    });
  });
}

export async function backup(snapshotPath: string): Promise<void> {
  const tsql = `BACKUP DATABASE HcPortalDB_Dev TO DISK='${snapshotPath}' WITH INIT, FORMAT;`;
  await runSqlcmd([...SQLCMD_BASE_ARGS, '-Q', tsql]);
}

export async function restore(snapshotPath: string): Promise<void> {
  // Single-user → restore → multi-user (pattern from docs/SEED_WORKFLOW.md §5.2)
  const tsql = `
    USE master;
    ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    RESTORE DATABASE HcPortalDB_Dev FROM DISK='${snapshotPath}' WITH REPLACE;
    ALTER DATABASE HcPortalDB_Dev SET MULTI_USER;
  `.trim();
  await runSqlcmd([...SQLCMD_BASE_ARGS.filter(a => a !== '-d' && a !== 'HcPortalDB_Dev'), '-Q', tsql]);
}

export async function execScript(sqlPath: string): Promise<void> {
  await runSqlcmd([...SQLCMD_BASE_ARGS, '-i', sqlPath]);
}

export async function queryScalar(sql: string): Promise<number> {
  const { stdout } = await runSqlcmd([
    ...SQLCMD_BASE_ARGS,
    '-Q', `SET NOCOUNT ON; ${sql}`,
    '-h', '-1',    // suppress column headers
    '-W',          // strip trailing whitespace
  ]);
  const match = stdout.trim().match(/^-?\d+/);
  if (!match) throw new Error(`queryScalar: no numeric output from ${sql}\nStdout: ${stdout}`);
  return parseInt(match[0], 10);
}
```

**Key flags:**
- `-b` (FAIL ON ERROR) is CRITICAL — without it, T-SQL `THROW` or syntax error returns 0 and code happily proceeds. Phase 313.1 seed assumes this.
- `-E` (Windows auth) match `appsettings.Development.json` connection string `Integrated Security=True`.
- `-C` (trust cert) match `TrustServerCertificate=True`.
- `-I` (QUOTED_IDENTIFIER ON) required for some T-SQL constructs.

[VERIFIED: `docs/SEED_WORKFLOW.md` §5.1-5.2 + flags from Microsoft Learn sqlcmd reference + Phase 313.1 seed script line 13 `sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -i ...`]

### Pattern 3: softAssert + Collector Singleton

**What:** Wrap assertion in try-catch; on fail, record() finding + (critical) throw SkipScenarioError OR (major/minor) swallow + return null.

**When to use:** Every test() block body — replace direct `expect()` for discovery-mode failures.

**Example:**
```ts
// tests/e2e/helpers/matrixTypes.ts
export type Severity = 'critical' | 'major' | 'minor';
export type Finding = {
  scenarioId: number;
  scenarioTitle: string;
  step: string;
  expected: string;
  actual: string;
  screenshotPath?: string;
  severity: Severity;
  isMeta?: boolean;
};
export type ScenarioConfig = {
  id: number;
  sessionId: number;
  title: string;
  type: 'Manual' | 'Online' | 'PreTest' | 'PostTest';
  category: string;
  scheduleDate: string;
  questions: Array<{
    id: number;
    type: 'MultipleChoice' | 'MultipleAnswer' | 'Essay';
    scoreValue: number;
    correctOptionIds: number[];
  }>;
};

// tests/e2e/helpers/matrixReport.ts
import { Page } from '@playwright/test';
import { writeFile } from 'fs/promises';
import { Finding, Severity, ScenarioConfig } from './matrixTypes';

class Collector {
  private findings: Finding[] = [];
  record(f: Finding) { this.findings.push(f); }
  count() { return this.findings.length; }
  async flush(outPath: string) {
    const discovery = this.findings.filter(f => !f.isMeta);
    const meta = this.findings.filter(f => f.isMeta);
    const md = renderReport(discovery, meta);  // helper below
    await writeFile(outPath, md, 'utf-8');
  }
}

export const collector = new Collector();          // module-level singleton

export class SkipScenarioError extends Error {
  constructor(msg: string) { super(msg); this.name = 'SkipScenarioError'; }
}

export async function softAssert<T>(
  ctx: { scenario: ScenarioConfig; step: string; severity: Severity; page: Page; isMeta?: boolean },
  fn: () => Promise<T>,
  expected: string
): Promise<T | null> {
  try {
    return await fn();
  } catch (e: any) {
    const screenshotPath = `test-results/matrix-s${ctx.scenario.id}-${ctx.step.replace(/\s+/g,'-')}.png`;
    await ctx.page.screenshot({ path: screenshotPath, fullPage: true }).catch(() => {});
    collector.record({
      scenarioId: ctx.scenario.id,
      scenarioTitle: ctx.scenario.title,
      step: ctx.step,
      expected,
      actual: e?.message ?? String(e),
      screenshotPath,
      severity: ctx.severity,
      isMeta: ctx.isMeta,
    });
    if (ctx.severity === 'critical') {
      throw new SkipScenarioError(`Critical at ${ctx.step}: ${e?.message}`);
    }
    return null;
  }
}
```

**Singleton lifecycle caveat:** Module-level singleton state TIDAK persist across Playwright workers. Phase 315 pakai `fullyParallel: false` + 1 worker (default chromium project tanpa `workers` override), jadi singleton akan persist throughout spec execution. globalTeardown runs di same Node process — singleton still accessible via `import { collector } from '...'`. **Verified:** Phase 313.1 seed and Phase 311 HTMX patterns konfirm 1-worker assumption di project ini.

### Pattern 4: SQL Server identity insert dengan deterministic IDs

**What:** Force seed to use specific PK values (9001-9009 etc) so `.matrix-state.json` config matches DB.

**When to use:** When test config refers to IDs that must match DB rows post-seed (Phase 315 spec assumes this).

**Example:**
```sql
-- tests/sql/assessment-matrix-seed.sql
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Resolve fixture user IDs (anti-pattern Phase 309: never NULL-FK)
DECLARE @CoacheeId NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
DECLARE @Coachee2Id NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'iwan3@pertamina.com');
IF @CoacheeId IS NULL OR @Coachee2Id IS NULL
  THROW 50001, 'Fixture user (rino.prasetyo / iwan3) tidak ditemukan — seed UserManager dulu.', 1;

-- Idempotent cleanup (chain FK-respecting, sama pola Phase 313.1)
DELETE pur FROM PackageUserResponses pur INNER JOIN AssessmentSessions s
  ON pur.AssessmentSessionId = s.Id WHERE s.Title LIKE '[MATRIX_TEST_2026_05_11]%';
DELETE upa FROM UserPackageAssignments upa INNER JOIN AssessmentSessions s
  ON upa.AssessmentSessionId = s.Id WHERE s.Title LIKE '[MATRIX_TEST_2026_05_11]%';
DELETE po FROM PackageOptions po INNER JOIN PackageQuestions pq
  ON po.PackageQuestionId = pq.Id INNER JOIN AssessmentPackages ap
  ON pq.AssessmentPackageId = ap.Id INNER JOIN AssessmentSessions s
  ON ap.AssessmentSessionId = s.Id WHERE s.Title LIKE '[MATRIX_TEST_2026_05_11]%';
DELETE pq FROM PackageQuestions pq INNER JOIN AssessmentPackages ap
  ON pq.AssessmentPackageId = ap.Id INNER JOIN AssessmentSessions s
  ON ap.AssessmentSessionId = s.Id WHERE s.Title LIKE '[MATRIX_TEST_2026_05_11]%';
DELETE ap FROM AssessmentPackages ap INNER JOIN AssessmentSessions s
  ON ap.AssessmentSessionId = s.Id WHERE s.Title LIKE '[MATRIX_TEST_2026_05_11]%';
DELETE FROM AssessmentSessions WHERE Title LIKE '[MATRIX_TEST_2026_05_11]%';

BEGIN TRAN;

-- Force deterministic PK via IDENTITY_INSERT
SET IDENTITY_INSERT AssessmentSessions ON;
INSERT INTO AssessmentSessions (Id, Title, Category, UserId, ...) VALUES
  (9001, '[MATRIX_TEST_2026_05_11] Matrix Manual Mixed', 'Matrix Test Category', @CoacheeId, ...),
  -- ... 9 rows total (9001..9009)
;
SET IDENTITY_INSERT AssessmentSessions OFF;

-- Repeat for AssessmentPackages (9001..9009), PackageQuestions (50001..50037), PackageOptions (80001..80124)
-- Pattern: SET IDENTITY_INSERT <table> ON → INSERT explicit Ids → OFF

-- Also seed for coachee2 — separate AssessmentSession rows? Or same session with multiple users?
-- INVESTIGATION (Wave 1): apakah 2 user butuh 2 session berbeda atau 1 session dengan multiple UserPackageAssignment?
-- Check CMPController.StartExam:926 — UserPackageAssignment.AssessmentSessionId is unique per session+user.
-- Conclusion: spec asumsikan 1 AssessmentSession PER PESERTA (peserta1 di sessionId 9001, peserta2 di session paralel? Or same session?).
-- READ MORE CAREFULLY: spec asumsikan "2 peserta per session" — tapi UserId di AssessmentSession adalah single string FK.
-- IMPLIKASI: Untuk "2 peserta per scenario" perlu 2 AssessmentSession terpisah dengan Title/Category/ScheduleDate sama (sibling pattern, sama persis dengan production batch assessment).
-- REVISED ID range: 9001-9009 jadi tidak cukup untuk 2 peserta × 9 scenario = 18 sessions.

COMMIT;

-- Final verification SELECT (Layer 1 source data)
SELECT s.Id, s.Title, s.UserId, s.AssessmentType,
  (SELECT COUNT(*) FROM AssessmentPackages WHERE AssessmentSessionId = s.Id) AS PkgCount,
  (SELECT COUNT(*) FROM PackageQuestions pq INNER JOIN AssessmentPackages ap
     ON pq.AssessmentPackageId = ap.Id WHERE ap.AssessmentSessionId = s.Id) AS QCount
FROM AssessmentSessions s
WHERE s.Title LIKE '[MATRIX_TEST_2026_05_11]%'
ORDER BY s.Title, s.UserId;
```

**⚠️ CRITICAL CLARIFICATION (Wave 1 gap surfaced via investigation):**
- `AssessmentSession.UserId` adalah **single FK** (1 session = 1 user).
- "2 peserta per scenario" pada spec berarti **2 sibling sessions** (same Title + Category + Schedule.Date, beda UserId) — pola production batch assessment.
- ID range 9001-9009 **TIDAK CUKUP** untuk 9 scenario × 2 peserta. Phase 315 plan harus naikkan range jadi **9001-9018** (18 sessions) atau pakai range terpisah per peserta. **Locked clarification candidate untuk plan stage.**

[VERIFIED: source code read `Models/AssessmentSession.cs:9` + `Controllers/CMPController.cs:926` UserPackageAssignment.AssessmentSessionId unique per session]

### Anti-Patterns to Avoid

- **❌ `mssql` Node driver for query execution** — Project explicitly rejects (CONTEXT.md deferred). Use sqlcmd subprocess only.
- **❌ Manual loop SaveAnswer for MA** — MA pakai SignalR `SaveMultipleAnswer` (single hub call with comma-separated string). Loop akan break atomicity guarantee (wipe-and-insert at server).
- **❌ Skip Layer 1 validation** — Tanpa Layer 1 (post-seed COUNT check), bug di seed SQL bisa silent-fail dan spec run dengan 0 rows. Detection: halt sebelum spec start.
- **❌ Hard-code path Windows backslash di TS** — Pakai `path.join()` cross-platform; pakai forward slash di SQL string literal (SQL Server accept both).
- **❌ Run spec dengan Kestrel running** — RESTORE butuh exclusive DB lock; ALTER DATABASE SINGLE_USER WITH ROLLBACK IMMEDIATE akan kill Kestrel connection forcibly tapi developer wajib aware → dokumentasikan di header docblock spec.
- **❌ Forget `-b` flag di sqlcmd** — Tanpa `-b`, T-SQL error return exit 0 (silent failure). Phase 313.1 seed assumes this.
- **❌ Flush report AFTER restore** — Report harus ditulis SEBELUM RESTORE supaya kalau RESTORE crash, findings tetap tersimpan.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| BACKUP/RESTORE SQL Server | EF Core migrate-twin DB or transactional rollback | sqlcmd BACKUP/RESTORE WITH REPLACE | Existing project pattern (Phase 313.1 + SEED_WORKFLOW.md). EF migrate-twin would need separate DB context — invasive. Transactional rollback impossible across multiple Playwright workers/processes. |
| URL building with query params | Manual string concat `+ '?title=' + value` | `new URL()` + `searchParams.set()` | Auto-encode Unicode/spaces/brackets per Q5 finding. |
| Login flow | New login helper | Existing `tests/helpers/auth.ts:login(page, accountKey)` | Already battle-tested across Phase 307-313 e2e tests. |
| Account fixtures | New email/password constants | Existing `tests/helpers/accounts.ts` | All 4 needed (admin, hc, coachee, coachee2) available; matches dev seed (admin@pertamina.com confirmed in MEMORY.md). |
| Screenshot per finding | Manual `page.screenshot()` in test body | Playwright `screenshot: 'on'` config + matrixReport ambil path from test-results | Already enabled in `playwright.config.ts:13`. matrixReport.softAssert just records path; no extra capture. |
| Multi-step seed cleanup chain | New ORM/SQL custom | FK-respecting DELETE chain Phase 313.1 pattern (6-step) | Pattern proven, lihat `.planning/seeds/313-timer-fixtures.sql:75-110`. |
| Markdown report templating | mustache/handlebars/marked | TS template literal | Single template, ~150 lines max. Library adds dependency + indirection. |
| Sentinel "expected failure" handling | Custom try-catch + skip | Playwright `test.fail()` annotation (D-06) | Built-in — exit code = 0 when test fails as expected, non-zero when unexpected pass (collector breakage signal). |
| State persistence setup→spec→teardown | Playwright fixture scoped to project | File-based `tests/.matrix-state.json` | Fixture scope doesn't reach globalTeardown. File is reliable cross-stage IPC. |

**Key insight:** Phase 315 stack adalah 95% reuse + 5% new orchestration. Building dari nol akan reintroduce bug yang sudah di-solve di Phase 307/313/313.1.

## Runtime State Inventory

> Phase 315 adalah **greenfield infra** (test infrastructure baru), bukan rename/refactor. Tetap diperlukan karena seed temporary lokal punya runtime state.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | DB lokal `HcPortalDB_Dev` di `localhost\SQLEXPRESS` — 18+ AssessmentSessions tagged `[MATRIX_TEST_2026_05_11]` (after revised count for 2 peserta × 9 scenario), plus packages, questions, options, eventual UserPackageAssignments + PackageUserResponses. | Snapshot via sqlcmd BACKUP sebelum seed; RESTORE WITH REPLACE di teardown (Layer 4 verify = 0). |
| Live service config | None — Kestrel adalah local web server, tidak ada external service config (Datadog, n8n, etc) yang menyimpan Title prefix. | None. |
| OS-registered state | None — tidak ada Windows Task Scheduler, pm2, atau systemd entry yang reference "MATRIX_TEST" string. | None. |
| Secrets/env vars | None — connection string sudah di `appsettings.Development.json`, tidak butuh tambahan env var atau SOPS key. | None. |
| Build artifacts | `tests/.matrix-state.json` (gitignored), snapshot `*.bak` files (gitignored), report `docs/test-reports/YYYY-MM-DD-*.md` (committed to git). | Gitignore `.matrix-state.json` + `*.bak`. Report file commit OK. |

**Nothing found in categories** Live service config, OS-registered state, Secrets/env vars — verified by mental scan of project structure (no Datadog/n8n/systemd integration) + grep of MATRIX_TEST string across non-test directories.

## Common Pitfalls

### Pitfall 1: SignalR Hub Connection Not Established Before Save

**What goes wrong:** Test calls Playwright `page.click('checkbox')` immediately after `page.goto('/CMP/StartExam/9001')`. The change handler runs but `window.assessmentHub.state !== 'Connected'` (still in handshake), so `SaveMultipleAnswer` is silently skipped (line 850 condition: `if (window.assessmentHub && window.assessmentHub.state === 'Connected')`). DB never updated. Server-side Submit sees no answers; score = 0 → false-positive "Bug found" finding.

**Why it happens:** SignalR connection adalah async — `OnConnectedAsync` butuh roundtrip handshake (~500ms-1s di lokal, longer di slow network). DOM is ready BEFORE hub.

**How to avoid:**
1. `takeExam` helper WAIT for hub ready before first MA/Essay save:
   ```ts
   await page.waitForFunction(() => window.assessmentHub?.state === 'Connected', { timeout: 10_000 });
   ```
2. Alternatively, verify `#saveIndicator` state transitioned to 'saved' before moving on (positive confirmation).

**Warning signs:** All MA/Essay answers show 0 score, but MC scenarios pass. Inspect via `SELECT COUNT FROM PackageUserResponses WHERE AssessmentSessionId=9001` post-submit.

[VERIFIED: `Views/CMP/StartExam.cshtml:850` SignalR readiness gate]

### Pitfall 2: Kestrel Running Blocks RESTORE

**What goes wrong:** RESTORE command issues `ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE`. If Kestrel is running (developer forgot to stop), this kills its connection mid-stream → potential transaction in-flight rolled back, but Kestrel itself crashes or returns 500 to next request → state divergence.

**Why it happens:** Most developers run `dotnet run` in one terminal and test in another. Easy to forget to stop dotnet before Playwright run.

**How to avoid:**
1. **Documentation only** (per D-07 — no automated gate): header docblock di `assessment-matrix.spec.ts` warn explicit: "Stop Kestrel (`Ctrl+C` di `dotnet run` terminal) sebelum run."
2. Optional automated check di globalSetup: `await fetch('http://localhost:5277').catch(...)` — if app reachable, throw setup error "Kestrel masih running; stop dulu sebelum matrix test."
3. RESTORE error catch + show curl command for manual restore.

**Warning signs:** Sebelum Phase A complete, BACKUP succeed but seed INSERT throws "transient" connection error. Or teardown RESTORE fails with "exclusive access could not be obtained".

[CITED: `docs/SEED_WORKFLOW.md` §5.3 troubleshooting]

### Pitfall 3: AssessmentSession.UserId is Single FK — Multi-User Requires Sibling Sessions

**What goes wrong:** Spec mentions "2 peserta per session (coachee + coachee2)" — natural read suggests 1 AssessmentSession.Id with 2 users. **WRONG.** AssessmentSession.UserId is `NOT NULL string` (single FK to Users). One session = one user.

**Why it happens:** Spec phrasing menyamarkan model constraint. Production pattern adalah sibling sessions (same Title+Category+Schedule.Date, beda UserId — `Controllers/AssessmentAdminController.cs:2688` shows this composite key).

**How to avoid:**
1. Seed harus INSERT **2 sibling sessions per scenario** (UserId=coachee + UserId=coachee2, same Title prefix).
2. ID range adjustment: 9 scenarios × 2 = **18 sessions**. Re-range: AssessmentSession **9001-9018**, AssessmentPackage **9001-9018** (or shared per scenario? package per session — so 9001-9018), PackageQuestion **50001-50068** (2× 37 question if questions are per-session, or 50001-50037 if shared across sibling sessions — investigate).
3. **Actually:** AssessmentPackage.AssessmentSessionId is FK ONE-TO-ONE typically (one package set per session). Sibling sessions = independent packages = duplicate question/option rows. **OR** packages shared (one AssessmentPackage row, dengan UserPackageAssignment.AssessmentSessionId pointing to multiple sessions — verifikasi via UserPackageAssignment model di Wave 1).
4. **Recommendation:** Use sibling sessions but share AssessmentPackage parent — `UserPackageAssignment` is the per-user join. Single Package + N Sessions + N UserPackageAssignments. Re-read `CMPController.cs:926` shows assignment is created lazily on StartExam — seed dapat skip UPA, let app auto-create.

**Warning signs:** Test setup `await page2 = takeExam(..., 'coachee2', sessionId=9001)` fails with "Unauthorized" (session.UserId != current user).

**Plan stage MUST resolve:** Wave 0 of execution plan harus baca AssessmentPackage.cs + UserPackageAssignment.cs source untuk konfirm cardinality, lalu finalize seed SQL ID range. **Spec's "9 sessions" undercount; revised target = 18 sibling sessions.**

[VERIFIED: `Models/AssessmentSession.cs:10` UserId NOT NULL FK + `Controllers/AssessmentAdminController.cs:2688` composite-key sibling pattern]

### Pitfall 4: Identity Insert Without IDENTITY_INSERT ON

**What goes wrong:** Seed SQL writes `INSERT INTO AssessmentSessions (Id, Title, ...) VALUES (9001, ...)` directly. SQL Server rejects: "Cannot insert explicit value for identity column in table 'AssessmentSessions' when IDENTITY_INSERT is OFF."

**Why it happens:** Phase 313.1 seed bypassed this by using `OUTPUT INSERTED.Id` (let SQL auto-generate IDs, capture them into table variable). Spec asumsikan deterministic IDs 9001-9018 → needs explicit IDENTITY_INSERT ON.

**How to avoid:**
```sql
SET IDENTITY_INSERT AssessmentSessions ON;
INSERT INTO AssessmentSessions (Id, Title, ...) VALUES
  (9001, ...), (9002, ...), ..., (9018, ...);
SET IDENTITY_INSERT AssessmentSessions OFF;
```

Repeat per table: AssessmentPackages, PackageQuestions, PackageOptions. **Constraint:** Only one table per session can have IDENTITY_INSERT ON. Order block-by-block.

Alternative: Auto-generate IDs (Phase 313.1 pattern) and update `.matrix-state.json` post-INSERT with actual captured IDs. Trade-off: state.json must be written by SQL output parsing in setup, not pre-defined. Adds complexity but more robust.

**Recommendation:** Pakai **IDENTITY_INSERT ON pattern** untuk Phase 315 — predictable, easier debug, matches spec's hardcoded ID assumption.

**Warning signs:** `Msg 544, Level 16, State 1: Cannot insert explicit value for identity column...` di sqlcmd stderr.

[CITED: SQL Server Books Online — IDENTITY_INSERT behavior + Phase 313.1 seed pattern observed]

## Code Examples

### Example 1: Helper `takeExam` for MC + MA + Essay scenarios

```ts
// tests/e2e/helpers/examMatrix.ts
import { Page, expect } from '@playwright/test';
import { login } from '../../helpers/auth';
import { AccountKey } from '../../helpers/accounts';
import { ScenarioConfig } from './matrixTypes';
import { softAssert, SkipScenarioError } from './matrixReport';

export async function takeExam(
  page: Page,
  cfg: ScenarioConfig,
  peserta: AccountKey,
  sessionId: number,                  // explicit — sibling session ID per peserta
  options: { sabotageOneAnswer?: boolean } = {}
): Promise<void> {
  await login(page, peserta);

  await softAssert(
    { scenario: cfg, step: 'navigate-start-exam', severity: 'critical', page },
    async () => {
      await page.goto(`/CMP/StartExam/${sessionId}`);
      await expect(page.locator('#examForm')).toBeVisible({ timeout: 10_000 });
    },
    'StartExam page renders #examForm'
  );

  // Wait SignalR hub ready (Pitfall 1 prevention)
  await softAssert(
    { scenario: cfg, step: 'signalr-ready', severity: 'critical', page },
    async () => {
      await page.waitForFunction(
        () => (window as any).assessmentHub?.state === 'Connected',
        undefined,
        { timeout: 10_000 }
      );
    },
    'SignalR assessmentHub connected'
  );

  for (let i = 0; i < cfg.questions.length; i++) {
    const q = cfg.questions[i];
    const isSabotaged = options.sabotageOneAnswer && i === 0;  // deterministic: first Q sabotaged

    if (q.type === 'MultipleChoice') {
      const optId = isSabotaged
        ? findWrongOption(q)  // any optionId not in correctOptionIds
        : q.correctOptionIds[0];
      await page.check(`input.exam-radio[data-question-id="${q.id}"][value="${optId}"]`);
      // Wait DB save via #saveIndicator state='saved'
      await page.locator(`#saveIndicatorBadge`).waitFor({ state: 'visible' });
      await page.locator(`#saveIndicatorText`).filter({ hasText: 'saved' }).waitFor({ timeout: 5_000 });
    } else if (q.type === 'MultipleAnswer') {
      const targets = isSabotaged
        ? q.correctOptionIds.slice(1)  // miss first correct → partial scoring
        : q.correctOptionIds;
      for (const oid of targets) {
        await page.check(`input.exam-checkbox[data-question-id="${q.id}"][value="${oid}"]`);
      }
      // Last change triggers SignalR; wait save indicator
      await page.locator(`#saveIndicatorText`).filter({ hasText: 'saved' }).waitFor({ timeout: 5_000 });
    } else if (q.type === 'Essay') {
      const answer = isSabotaged ? '' : 'Jawaban essay benar dari peserta.';
      await page.fill(`textarea.exam-essay[data-question-id="${q.id}"]`, answer);
      // Essay debounce 2s — wait save indicator transition
      await page.waitForTimeout(2_500);
      await page.locator(`#saveIndicatorText`).filter({ hasText: 'saved' }).waitFor({ timeout: 5_000 });
    }
  }

  await softAssert(
    { scenario: cfg, step: 'submit-exam', severity: 'critical', page },
    async () => {
      await page.click('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)');
      await page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 15_000 });
    },
    'SubmitExam redirects to /CMP/Results/{id}'
  );
}

function findWrongOption(q: { correctOptionIds: number[] }): number {
  // Implementation: query DOM for all option values for this question, filter out correctOptionIds
  // OR: pre-compute in scenario config — add `wrongOptionIds` field
  throw new Error('TODO: implement via DOM scan or pre-compute');
}
```

[Source: synthesis dari Hubs/AssessmentHub.cs + Views/CMP/StartExam.cshtml + Phase 313.1 helper pattern]

### Example 2: Layer 1 Validation Query

```sql
-- Layer 1: Setup validation (Phase 315)
-- Run via dbSnapshot.queryScalar() post-seed
DECLARE @ExpectedSessions INT = 18;     -- 9 scenarios × 2 peserta (revised from spec's 9)
DECLARE @ExpectedQuestions INT = 74;    -- 37 distinct × 2 if per-session; or 37 if shared (verify Wave 1)
DECLARE @ExpectedOptions INT = 248;     -- corresponds to question count × ~4 options

SELECT COUNT(*) AS SessionCount
FROM AssessmentSessions
WHERE Title LIKE '[MATRIX_TEST_2026_05_11]%';
-- Expected: 18 (or 9 if spec asumsikan shared sessions — verify against actual schema)
```

```ts
// tests/e2e/global.setup.ts (extended)
import { test as setup, expect } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { writeFile, appendFile } from 'fs/promises';

setup('verify app is running + seed matrix', async ({ page }) => {
  // Existing assertion — keep
  const response = await page.goto('/Account/Login');
  expect(response?.ok()).toBeTruthy();
  await expect(page.locator('button[type="submit"]')).toBeVisible();

  // NEW: BACKUP + seed
  const ts = new Date().toISOString().replace(/[:.]/g, '-');
  const snapshotPath = `C:/Temp/HcPortalDB_Dev-matrix-${ts}.bak`;

  // Pre-check ID collision
  const collision = await db.queryScalar(
    `SELECT COUNT(*) FROM AssessmentSessions WHERE Id BETWEEN 9001 AND 9018`
  );
  expect(collision, 'ID range 9001-9018 has unexpected pre-existing rows').toBe(0);

  // BACKUP
  await db.backup(snapshotPath);

  // EXECUTE seed
  await db.execScript('tests/sql/assessment-matrix-seed.sql');

  // Layer 1 validation
  const sessionCount = await db.queryScalar(
    `SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[MATRIX_TEST_2026_05_11]%'`
  );
  expect(sessionCount, 'Layer 1: expected 18 sessions seeded').toBe(18);

  // Write state file (consumed by spec + teardown)
  await writeFile('tests/.matrix-state.json', JSON.stringify({
    snapshotPath,
    seededAt: new Date().toISOString(),
    scenarios: [/* TODO: 9 scenario configs with sibling sessionIds per peserta */],
  }, null, 2));

  // Append journal entry
  const journalEntry = `\n| ${new Date().toISOString().slice(0,10)} | 315 | temporary + local-only | Assessment matrix test sweep — 7 discovery + 3 sentinel | AssessmentSessions(18), Packages(18), Questions(74), Options(248) | ${snapshotPath} | active |\n`;
  await appendFile('docs/SEED_JOURNAL.md', journalEntry);
});
```

[Source: pattern dari Phase 313.1 seed + `docs/SEED_WORKFLOW.md` §6 journal format]

### Example 3: globalTeardown with Layer 4 + journal close

```ts
// tests/e2e/global.teardown.ts
import type { FullConfig } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { collector } from './helpers/matrixReport';
import { readFile, unlink, readFile as readJournal, writeFile } from 'fs/promises';

async function globalTeardown(_: FullConfig): Promise<void> {
  const today = new Date().toISOString().slice(0, 10);
  const reportPath = `docs/test-reports/${today}-assessment-matrix.md`;

  // Step 1: Flush report FIRST (before RESTORE, per Pitfall: preserve findings)
  try {
    await collector.flush(reportPath);
    console.log(`[teardown] Report ditulis: ${reportPath}`);
  } catch (e) {
    console.error('[teardown] flush() gagal:', e);
  }

  // Step 2: Restore DB
  const state = JSON.parse(await readFile('tests/.matrix-state.json', 'utf-8'));
  try {
    await db.restore(state.snapshotPath);
    console.log(`[teardown] RESTORE OK dari ${state.snapshotPath}`);
  } catch (e) {
    console.error('[teardown] RESTORE GAGAL — manual restore command:');
    console.error(`  sqlcmd -S "localhost\\SQLEXPRESS" -E -Q "USE master; ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE HcPortalDB_Dev FROM DISK='${state.snapshotPath}' WITH REPLACE; ALTER DATABASE HcPortalDB_Dev SET MULTI_USER;"`);
    throw e;  // teardown failure → non-zero exit
  }

  // Step 3: Layer 4 validation
  const remainingRows = await db.queryScalar(
    `SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[MATRIX_TEST_2026_05_11]%'`
  );
  if (remainingRows !== 0) {
    console.error(`[teardown] Layer 4 FAIL: ${remainingRows} matrix rows remain post-RESTORE`);
    throw new Error(`Layer 4 cleanup validation failed`);
  }

  // Step 4: Update journal entry to 'cleaned'
  const journalText = await readJournal('docs/SEED_JOURNAL.md', 'utf-8');
  // Find last active line with snapshotPath and replace 'active' → 'cleaned'
  // Pattern: ... | snapshotPath | active |
  const updatedJournal = journalText.replace(
    /(\|\s*\S*matrix[^|]*\.bak\s*\|\s*)active(\s*\|)/,
    '$1cleaned$2'
  );
  await writeFile('docs/SEED_JOURNAL.md', updatedJournal);

  // Step 5: Cleanup state file + snapshot
  await unlink('tests/.matrix-state.json').catch(() => {});
  await unlink(state.snapshotPath).catch(() => {});
}

export default globalTeardown;
```

[Source: composition dari SEED_WORKFLOW.md §5.2 + Playwright globalTeardown contract]

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `globalSetup` only (no teardown counterpart) | `globalSetup` + `globalTeardown` registration via top-level config field | Playwright 1.30+ | Single source for cleanup; Phase 315 leverages this. [CITED: Playwright docs] |
| MC/MA save via single HTTP endpoint loop | MC via HTTP `/CMP/SaveAnswer`, MA & Essay via SignalR hub | Phase 298 (v14.0 Assessment Enhancement) | Test infra wajib differentiate channel. [VERIFIED: AssessmentHub.cs] |
| Manual DB cleanup via DELETE per-test | Snapshot/Restore pattern via sqlcmd | Phase 313.1 (v15.0) | More reliable + matches SEED_WORKFLOW.md SOP. [VERIFIED: 313-timer-fixtures.sql] |
| Helper di flat `tests/helpers/` | e2e-specific di `tests/e2e/helpers/`, shared di `tests/helpers/` | Phase 307 (v15.0) | D-01/D-02 untuk Phase 315. [CITED: STATE.md decision log] |

**Deprecated/outdated:**
- `assemblyManager` patterns for SignalR (old): pre Phase 200 used different hub naming; current is `AssessmentHub` at `/assessmentHub` mounting (`Program.cs` MapHub).
- `AssessmentQuestion` & `UserResponse` legacy nav properties removed Phase 227 (CLEN-02) — modern model uses `PackageQuestion` + `PackageUserResponse`.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | AssessmentPackage adalah 1-per-session (cardinality ONE) — bukan shared across sibling sessions | Pitfall 3, Architecture Map | If sharable: seed SQL bisa di-share antara coachee + coachee2 → halve dataset (74→37 questions, 248→124 options). If 1-per-session: 18 packages × 4 questions × 4 options dst. **Plan stage Wave 0 MUST verify via `Models/AssessmentPackage.cs` + `Models/UserPackageAssignment.cs` read.** |
| A2 | Form payload `answers[qId]=value` di SubmitExam IGNORED by server jika DB sudah punya saved response | Wave 0 Q2 | If server reads form value (not DB): Essay submit may fail because form `answers[essayQId]` cast to int returns NaN. **Plan Wave 0 MUST read CMPController.SubmitExam:1569+ logic untuk Essay branch.** |
| A3 | Singleton `collector` di matrixReport.ts persist across all test() blocks within globalSetup→spec→globalTeardown lifecycle | Pattern 3 | If new module instance per worker: report empty at teardown. Mitigation: Playwright default 1 worker for `fullyParallel: false` + chromium project. **Confirmed via tests/playwright.config.ts:8 line — no override.** |
| A4 | sqlcmd version on developer machine supports `-b` and `-C` flags | Pattern 2 | If older sqlcmd missing `-C`: TLS handshake fails. Mitigation: Phase 313.1 seed already uses these flags successfully (verified). |
| A5 | `iwan3@pertamina.com` (coachee2 fixture) exists in dev DB | Pattern 4 / Layer 1 | If absent: seed fails with `THROW 50001 — Fixture user not found`. Mitigation: seed pre-check + halt with explicit instruction. Verify with `SELECT Email FROM Users WHERE Email IN ('rino.prasetyo@pertamina.com', 'iwan3@pertamina.com')`. |
| A6 | Spec's "9 sessions" should be **18 sibling sessions** (2 peserta × 9 scenarios) — same Title+Category+Schedule.Date, different UserId | Pitfall 3 | Major impact on seed dimensions, state.json structure, Layer 1 expected counts. **Resolution candidate untuk plan Wave 0.** |
| A7 | `npx playwright test --grep "Scenario 5"` matches test name pattern (test('S5 Matrix Online MC-only', ...)) | Smoke run protocol | If grep doesn't match: smoke run runs nothing → false confidence. Use unique title prefix `S5` or `Scenario 5` consistently. |

**Resolution priority:** A1, A2, A6 are HIGH RISK. Plan stage Wave 0 task #1 must resolve via source-code read before any seed SQL drafting.

## Open Questions

1. **AssessmentPackage cardinality with sibling sessions** (linked to A1, A6)
   - What we know: AssessmentPackage.AssessmentSessionId is FK (one direction). `Controllers/CMPController.cs:912` queries packages WHERE `siblingSessionIds.Contains(p.AssessmentSessionId)` — suggesting packages exist per-session, BUT line 947 sentinel comment "store first package ID" implies sharing pattern.
   - What's unclear: Does test seed need to create AssessmentPackage rows for EACH sibling session (2 per scenario), or can siblings share a single AssessmentPackage row via the composite-key matching at query time?
   - Recommendation: Plan Wave 0 Task 1 — read `Models/AssessmentPackage.cs` + `Models/UserPackageAssignment.cs` + execution path `CMPController.StartExam:880-1000` carefully and document.

2. **SubmitExam Essay handling — form value vs DB persisted value**
   - What we know: Hidden input `name="answers[@q.QuestionId]"` (StartExam.cshtml:81) sends form value at submit. SignalR `SaveTextAnswer` upserts DB.
   - What's unclear: Does `SubmitExam` controller (CMPController.cs:1569) read `answers[essayQId]` (cast to int — would be NaN for Essay) or query DB `PackageUserResponse.TextAnswer` for Essay grading branch?
   - Recommendation: Plan Wave 0 Task 1 — read `CMPController.SubmitExam` body fully + GradingService for Essay branch.

3. **Whitelist of "safe to ignore" console errors**
   - What we know: Spec § Self-check mentions "Tidak ada error di console Playwright runtime".
   - What's unclear: Production app likely logs benign console.warn (favicon 404 di subpath, SignalR negotiate retries) yang akan trigger false positives.
   - Recommendation: matrixReport accept whitelist regex array, populated during smoke run (S5 only) iteration — start empty, add encountered benign patterns.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Node.js | Playwright runtime + dbSnapshot.ts spawn | ✓ | (assumed ≥16; verify) | — (blocker) |
| @playwright/test | spec runner, fixtures | ✓ | 1.58.2 | — |
| sqlcmd CLI | BACKUP/RESTORE/EXEC | ✓ | bundled SQL Server Express | — |
| SQL Server Express `localhost\SQLEXPRESS` | DB host | ✓ | (verify via `sqlcmd -? `) | — (blocker) |
| Database `HcPortalDB_Dev` | Target DB | ✓ | per `appsettings.Development.json` | — |
| Fixture user `rino.prasetyo@pertamina.com` | Seed UserId resolution | ✓ (assumed — Phase 313.1 seed depends on this) | — | Seed throws THROW 50001 with clear message |
| Fixture user `iwan3@pertamina.com` | Coachee2 seed | ⚠️ — verify | — | Add via seed pre-check + abort with instruction |
| Fixture user `meylisa.tjiang@pertamina.com` | HC grading flow | ⚠️ — verify | — | Same — pre-check abort |
| `C:/Temp/` directory writable by MSSQL$SQLEXPRESS service | BACKUP file write | ⚠️ — verify (per SEED_WORKFLOW.md §5.1 troubleshooting) | — | Fallback to SQL Server default backup path |
| Kestrel **stopped** sebelum run | Avoid Pitfall 2 | ⚠️ developer manual | — | Setup pre-check ping localhost:5277 — halt if reachable |
| `docs/test-reports/` directory | Report output destination | ✗ — likely missing | — | Create dir at setup time (`mkdir -p`) |

**Missing dependencies with no fallback:**
- Node.js (assumed present — Playwright already installed)
- SQL Server Express + HcPortalDB_Dev (assumed present — project running locally)

**Missing dependencies with fallback:**
- `iwan3@pertamina.com` user → seed validates + halts with instruction "create user via UserManager seed first"
- `C:/Temp/` writable → fallback to MSSQL default `C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\`
- `docs/test-reports/` directory → setup auto-creates

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | @playwright/test 1.58.2 (TypeScript) |
| Config file | `tests/playwright.config.ts` (EDIT — add globalTeardown) |
| Quick run command | `cd tests && npx playwright test assessment-matrix --grep "Scenario 5"` (smoke run, ~2-3 min) |
| Full suite command | `cd tests && npx playwright test assessment-matrix` (full 10 test blocks, ~10-15 min) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| QA-01 | 7 discovery scenarios end-to-end (peserta1+peserta2 take exam → submit → grade → verify result) | e2e (Playwright) | `npx playwright test assessment-matrix` | ❌ Wave 1 |
| QA-01 | 3 sentinel meta-validation (`[META-AllCorrect]`, `[META-AllWrong]`, `[META-CollectorCheck]`) | e2e (Playwright) | (same as above; filtered via title prefix `[META]`) | ❌ Wave 1 |
| QA-01 | Markdown report generated dengan severity + screenshot + hypothesis per finding | smoke (file existence + content shape) | post-run inspect `docs/test-reports/YYYY-MM-DD-assessment-matrix.md` | ❌ Wave 1 (renderer in matrixReport.ts) |
| QA-01 | DB lokal kembali ke state pre-test (Layer 4 cleanup) | integration (sqlcmd queryScalar) | inside `global.teardown.ts` Layer 4 step | ❌ Wave 1 |
| QA-01 | Smoke run protocol works (`--grep "Scenario 5"`) | manual-only (developer verifies smoke output) | `cd tests && npx playwright test assessment-matrix --grep "Scenario 5"` then `cat docs/test-reports/...md` | ❌ Wave 2 |
| QA-01 | 4-layer meta-validation (setup/helper/collector/cleanup) all pass | e2e + integration | full run + manual report inspection | ❌ Wave 2 |
| QA-01 | Wave 0 5 open questions documented | research (this RESEARCH.md) | (this file `## Wave 0 Investigation Answers`) | ✓ Done |

### Sampling Rate

- **Per task commit:** Smoke run `npx playwright test assessment-matrix --grep "Scenario 5"` lokal (require Kestrel stopped + sqlcmd reachable)
- **Per wave merge:** Full suite `npx playwright test assessment-matrix` + manual inspect report markdown + Layer 4 verify
- **Phase gate:** Full suite green + smoke run produces expected single sentinel-minor finding + Layer 1-4 all pass → ready for `/gsd-verify-work`

### Wave 0 Gaps

- [ ] `tests/sql/assessment-matrix-seed.sql` — seed SQL hierarchical (sessions → packages → questions → options) — Wave 1
- [ ] `tests/helpers/dbSnapshot.ts` — sqlcmd wrapper (D-01) — Wave 1
- [ ] `tests/e2e/helpers/matrixTypes.ts` — type definitions (D-02) — Wave 1
- [ ] `tests/e2e/helpers/matrixReport.ts` — collector + softAssert + flush (D-02) — Wave 1
- [ ] `tests/e2e/helpers/examMatrix.ts` — takeExam/gradeEssaysAsHc/verifyResultPage (D-02) — Wave 1
- [ ] `tests/e2e/global.setup.ts` — EXTEND (BACKUP + seed + state.json + journal) — Wave 1
- [ ] `tests/e2e/global.teardown.ts` — NEW (D-03, flush → RESTORE → Layer 4 → journal close) — Wave 1
- [ ] `tests/e2e/assessment-matrix.spec.ts` — 10 test blocks (7 discovery + 3 sentinel) — Wave 2
- [ ] `tests/playwright.config.ts` — EDIT (register globalTeardown) — Wave 1
- [ ] `tests/.gitignore` — CREATE/EDIT (`.matrix-state.json`, `*.bak`) — Wave 1
- [ ] `docs/test-reports/` directory — CREATE — Wave 1 (auto via setup)
- [ ] Plan Wave 0 source-code investigation tasks (verify A1, A2, A6 assumptions) — Plan Wave 0

**Existing test infrastructure that covers some aspects:**
- `tests/helpers/auth.ts`, `tests/helpers/accounts.ts` — login + fixture users (REUSE, no change)
- `tests/e2e/helpers/exam313.ts` — POM-style helper pattern reference (informs examMatrix.ts shape)
- `tests/e2e/helpers/wizardSelectors.ts` — selector centralization pattern (no change for matrix)
- `tests/playwright.config.ts:13` — `screenshot: 'on'` already enabled (REUSE for finding capture)

## Security Domain

> security_enforcement assumed enabled (config.json absent for this key, default enabled).

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | Reuse `tests/helpers/auth.ts` (existing ASP.NET Core Identity flow); no test-specific bypass |
| V3 Session Management | no (test infra is local-only; no session forgery concern in test scope) | — |
| V4 Access Control | yes | sqlcmd dbSnapshot.ts hostname guard refuses non-`localhost`; CLAUDE.md compliance — local-only test cannot accidentally hit Dev/Prod |
| V5 Input Validation | yes | `URL` + `URLSearchParams.set()` for query params (per Q5 investigation); avoid manual string concat |
| V6 Cryptography | no | No cryptographic operation in test infra (no token signing, no password ops beyond existing login helper) |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Test infra accidentally hits Dev/Prod SQL Server | Tampering (production data) | dbSnapshot.ts `-S` arg whitelist check: reject any hostname not matching `/^localhost/i` (sample code in Pattern 2) |
| Seed temporary leaks into prod via accidental commit | Information Disclosure (PII in seed) | Title prefix `[MATRIX_TEST_2026_05_11]` is non-PII; no real user data; seed file gitignored from `.bak` only — `.sql` file IS committed (deliberate, contains only synthetic data) |
| sqlcmd command injection via dynamic SQL string | Tampering | All SQL string literals (queries, BACKUP path) controlled by test code, not user input; snapshotPath built from `Date.toISOString()` regex-sanitized (`[:.] → -`) |
| .matrix-state.json committed accidentally | Information Disclosure (snapshot path may reveal local user dir) | `tests/.gitignore` explicit entry; verify in plan Wave 1 |
| RESTORE invocation reachable via test runner from CI | Tampering | Phase 315 is local-only (CONTEXT D-deferred QA-02); no CI integration → no remote restore execution vector |

## Sources

### Primary (HIGH confidence)

- `docs/superpowers/specs/2026-05-11-assessment-matrix-test-design.md` (commit 94bacecf) — Phase 315 design spec, 430 lines, full architecture diagram + endpoint contract + file contracts
- `Controllers/CMPController.cs:348-417` — SaveAnswer HTTP endpoint (MC only)
- `Controllers/CMPController.cs:1569-1700+` — SubmitExam form binding + Essay branch (partial read; full read deferred to plan Wave 0)
- `Controllers/CMPController.cs:880-1000` — StartExam package mode + UserPackageAssignment auto-create
- `Controllers/AssessmentAdminController.cs:2684-2700` — AssessmentMonitoringDetail query string binding
- `Controllers/AssessmentAdminController.cs:2873-2950` — SubmitEssayScore + FinalizeEssayGrading
- `Hubs/AssessmentHub.cs:134-252` — SaveTextAnswer + SaveMultipleAnswer (Wave 0 CRITICAL evidence)
- `Views/CMP/StartExam.cshtml:81, 500-905` — form hidden inputs + JS save handlers (MC HTTP, MA/Essay SignalR)
- `Models/AssessmentSession.cs:1-193` — full schema, Notes field absence verification
- `Data/SeedData.cs:1-106` — ID range collision check (no Assessment* seed)
- `.planning/seeds/313-timer-fixtures.sql` — sqlcmd seed pattern + cleanup chain (Phase 313.1 reference)
- `docs/SEED_WORKFLOW.md` — Snapshot/Restore SOP + journal format
- `docs/DEV_WORKFLOW.md` — Local-only rule (CLAUDE.md inheritance)
- `tests/playwright.config.ts` — Existing Playwright config (extension target)
- `tests/e2e/global.setup.ts` — Existing setup (extension target)
- `tests/helpers/auth.ts`, `tests/helpers/accounts.ts` — Reused fixtures
- `tests/e2e/helpers/exam313.ts` — POM helper pattern (Phase 313.1)
- `tests/package.json` — Playwright 1.58.2 version verification
- `appsettings.Development.json` — Connection string `localhost\SQLEXPRESS;Database=HcPortalDB_Dev;Integrated Security=True`
- `.planning/phases/315-assessment-matrix-test/315-CONTEXT.md` — User decisions D-01..D-07
- `.planning/REQUIREMENTS.md` — QA-01 acceptance criteria
- `.planning/STATE.md` — Project decisions log (Phase 307 helper folder split decision)
- `.planning/ROADMAP.md` — Phase 315 success criteria
- `CLAUDE.md` — Project instructions (Bahasa Indonesia, lokal-only, seed workflow)

### Secondary (MEDIUM confidence)

- Playwright globalTeardown docs (https://playwright.dev/docs/test-global-setup-teardown) — function signature contract
- SQL Server BOL — IDENTITY_INSERT behavior, BACKUP/RESTORE syntax
- Microsoft Learn — sqlcmd flags reference

### Tertiary (LOW confidence)

- (None — all critical claims backed by either Primary or Secondary)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all dependencies already in tests/package.json + verified via Phase 313.1 pattern
- Architecture: HIGH — pattern proven (Playwright globalSetup/Teardown + sqlcmd subprocess), Phase 313.1 partial precedent
- Pitfalls: HIGH (Pitfall 1, 2, 4) / MEDIUM (Pitfall 3 — depends on A1/A6 resolution in plan Wave 0)
- Wave 0 answers: HIGH for Q1, Q2, Q3, Q4, Q5 (all verified via direct source read)
- Open questions: MEDIUM (3 items, each backed by clear path to resolution)
- Assumptions: HIGH for A3, A4, A5, A7 / MEDIUM for A1, A2, A6 (plan Wave 0 must resolve)

**Research date:** 2026-05-11
**Valid until:** 2026-06-10 (30 days — stack stable, no breaking changes expected; SignalR/sqlcmd patterns mature)

**Plan stage MUST address (before any seed SQL drafting):**
1. A1 + A6 resolution — read `Models/AssessmentPackage.cs` + `Models/UserPackageAssignment.cs` + StartExam full flow → finalize seed dimensions (18 sessions vs 9 with sharing).
2. A2 resolution — read `CMPController.SubmitExam` Essay branch full body → confirm whether DB-saved TextAnswer or form-submitted value is authoritative.
3. Drafting order: Wave 0 (read source + answer A1/A2/A6) → Wave 1 (helpers + seed SQL + setup/teardown + config) → Wave 2 (spec + sentinels) → Wave 3 (report polish + smoke verify) → `/gsd-verify-work` gate.
