# Phase 315: Assessment Matrix Test - Context

**Gathered:** 2026-05-11
**Status:** Ready for planning

<domain>
## Phase Boundary

Bangun automated Playwright spec `tests/e2e/assessment-matrix.spec.ts` yang menyapu kombinasi (tipe assessment × tipe soal) end-to-end di lokal:

- 7 skenario discovery (4 mixed per tipe assessment + 3 single-type Online per tipe soal)
- 3 sentinel meta-validation skenario (`[META-AllCorrect]`, `[META-AllWrong]`, `[META-CollectorCheck]`)
- Setiap skenario: peserta1 + peserta2 (coachee + coachee2) → exam → submit → grading manual essay (jika ada) → verify score di result page
- Continue-on-fail behavior dengan severity classification (critical/major/minor)
- DB seed temporary via `tests/sql/assessment-matrix-seed.sql` + BACKUP/RESTORE cleanup di globalTeardown
- Output: 1 markdown report `docs/test-reports/2026-05-11-assessment-matrix.md`
- 4-layer meta-validasi (setup correctness, helper correctness, collector correctness, cleanup correctness)
- Smoke run protocol: 1 skenario via `--grep "Scenario 5"` sebelum trust full run

**Sumber kebenaran detail:** `docs/superpowers/specs/2026-05-11-assessment-matrix-test-design.md` (commit `94bacecf`). CONTEXT.md ini melengkapi spec dengan keputusan yang baru terkunci di discuss.

</domain>

<decisions>
## Implementation Decisions

### Helper Folder Layout (override spec)

Spec asli menulis semua helper di `tests/helpers/`. Karena Phase 307 sudah putuskan e2e-specific helper ditaruh di `tests/e2e/helpers/`, helper Phase 315 dipisah:

- **D-01:** `dbSnapshot.ts` → `tests/helpers/dbSnapshot.ts` (generic SQL util — test/seed lain bisa re-use)
- **D-02:** `matrixReport.ts`, `examMatrix.ts`, `matrixTypes.ts` → `tests/e2e/helpers/` (matrix-spesifik, bukan shared utility)
- **D-03:** `globalTeardown` → `tests/e2e/global.teardown.ts` (konsisten dgn `tests/e2e/global.setup.ts` existing)
- Spec asli yang menyebut `tests/helpers/matrixReport.ts` dan `tests/helpers/examMatrix.ts` di-override oleh keputusan ini.

### Wave 0 Investigation Approach

- **D-04:** 5 open question pre-impl dijawab via baca source code, bukan probe runtime. Target file investigasi:
  - `Controllers/CMPController.cs` — endpoint SaveAnswer, SubmitExam, Results
  - `Controllers/AssessmentAdminController.cs` — SubmitEssayScore, FinalizeEssayGrading, AssessmentMonitoringDetail
  - `Views/CMP/StartExam.cshtml` + JS handler di `wwwroot/js/` — flow client-side MA & Essay save
  - `Models/AssessmentSession.cs` — cek apakah field `Notes` ada
  - `Data/SeedData.cs` — verifikasi ID range 9001-9009 / 50001-50037 / 80001-80124 tidak collide
- Output Wave 0: 1 dokumen singkat (e.g. `315-INVESTIGATION.md` atau bagian di RESEARCH.md) yang mencatat jawaban 5 open question + path source yang dirujuk.
- 5 question yang harus dijawab (dari spec § Open Questions):
  1. MA save flow — bagaimana multi-optionId disimpan? (loop SaveAnswer, endpoint terpisah, atau staging client-side)
  2. Essay save flow — via SaveAnswer (text), endpoint terpisah, atau submit-time only
  3. `AssessmentSession.Notes` field — ada atau tidak
  4. ID range collision — confirm 9001-9009 / 50001-50037 / 80001-80124 free terhadap `SeedData.cs`
  5. URL encoding — `Admin/AssessmentMonitoringDetail?title=&category=&scheduleDate=` aman buat Indonesian chars + spasi

### Marker Strategy

- **D-05:** Primary marker = `AssessmentSession.Notes = 'MATRIX_TEST_2026_05_11'` jika field ada.
- Fallback (kalau Wave 0 nemu Notes ngga ada): Title prefix `[MATRIX_TEST_2026_05_11]`, query Layer 1 + Layer 4 cleanup pakai `WHERE Title LIKE '[MATRIX_TEST_%]%'`.
- Schema migration tambah kolom Notes baru → ditolak. Test infra tidak boleh memodifikasi schema produksi.

### Sentinel Exit Code Handling

- **D-06:** Layer 3 sentinel `[META-CollectorCheck]` (sengaja gagal buat verifikasi collector) pakai Playwright `test.fail()` annotation. Run exit code = 0 saat sentinel gagal sesuai harapan, exit non-zero kalau sentinel mendadak pass (collector rusak).
- `matrixReport` filter: finding dari `[META-*]` skenario tidak ikut summary statistik discovery, tapi tetap tercatat di section terpisah `## Meta-validation results`.

### Smoke Run Gating

- **D-07:** Smoke run protocol (`--grep "Scenario 5"` dulu sebelum full 7) hanya didokumentasikan, tidak di-enforce di code. Lokasi dokumentasi: README di `tests/` atau header docblock di `assessment-matrix.spec.ts`.
- Tidak bikin marker file `.smoke-passed` atau npm script gating — terlalu banyak moving part untuk discovery-focused test.

### Claude's Discretion

- Snapshot file path (`C:/temp/PortalHC_Local-matrix-{timestamp}.bak`) — Claude bisa pilih directory sesuai disk space (fallback ke `tests/sql/` kalau `C:/temp/` tidak ada), gitignore-kan `.bak` files.
- Sabotage strategy peserta2 (skenario MA-bearing sengaja salah 1 MA) — deterministic: salah option pertama dari `correctOptionIds[]` saja, biar reproducible.
- Report file naming convention — `docs/test-reports/YYYY-MM-DD-assessment-matrix.md` (date saja, bukan timestamp). Run baru di hari yang sama overwrite report.
- Console error filtering di Layer "Tidak ada error di console Playwright runtime" — list whitelist console error yang aman di-ignore (e.g. favicon 404) ditulis di `matrixReport` config.
- Exact severity threshold per finding type (critical/major/minor) — Claude tentukan saat implementasi, sejalan dgn tabel klasifikasi spec § Error handling.

### Folded Todos

Tidak ada todo lama yang difold ke Phase 315 scope. Phase ini greenfield infra.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & Requirements
- `docs/superpowers/specs/2026-05-11-assessment-matrix-test-design.md` — Design spec utama (commit `94bacecf`). 7+3 scenario config, endpoint contract, file contracts, error handling, 4-layer meta-validation, open questions. **Sumber kebenaran utama**.
- `.planning/REQUIREMENTS.md` — QA-01 acceptance criteria
- `.planning/ROADMAP.md` § Phase 315 — Success criteria 1-7

### Workflow & Conventions
- `docs/SEED_WORKFLOW.md` — SOP BACKUP/RESTORE + journal entry untuk seed temporary lokal
- `docs/SEED_JOURNAL.md` — Format entry existing; append entry baru saat matrix test run
- `docs/DEV_WORKFLOW.md` — Aturan lokal-only (jangan run ke 10.55.3.3 atau prod)
- `CLAUDE.md` — Klasifikasi seed temporary vs permanent

### Reused Code
- `tests/playwright.config.ts` — Existing config; harus extend `globalSetup` ref + tambah `globalTeardown` register
- `tests/e2e/global.setup.ts` — Extend dgn BACKUP + seed execution
- `tests/helpers/auth.ts` — Re-use `login(page, accountKey)`
- `tests/helpers/accounts.ts` — Fixture user existing (admin/hc/coachee/coachee2 sudah ada)
- `tests/e2e/helpers/exam313.ts` — Pattern POM-style helper untuk exam flow (Phase 313.1 reference)

### Source Files untuk Wave 0 Investigation
- `Controllers/CMPController.cs` — SaveAnswer, SubmitExam, Results endpoint
- `Controllers/AssessmentAdminController.cs` — Grading endpoint
- `Views/CMP/StartExam.cshtml` — Client-side exam UI
- `wwwroot/js/` (exam handler JS) — Client-side save flow MA/Essay
- `Models/AssessmentSession.cs` — Cek field Notes
- `Data/SeedData.cs` — Cek ID range collision

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- **`tests/helpers/auth.ts`** — `login(page, accountKey)` siap pakai untuk login peserta1, peserta2, hc.
- **`tests/helpers/accounts.ts`** — `coachee`, `coachee2`, `hc`, `admin` semua tersedia. Email `admin@pertamina.com` confirmed di project memory.
- **`tests/e2e/helpers/exam313.ts`** — Pola helper exam flow yang sama (POM-style flat function exports per keputusan Phase 313.1). examMatrix.ts ikuti pola ini.
- **`tests/e2e/helpers/wizardSelectors.ts`** — Pola selector centralization buat halaman wizard admin. Kalau matrix test butuh selector spesifik halaman admin, naruh di sini.
- **`tests/playwright.config.ts`** — `fullyParallel: false`, `retries: 0`, `screenshot: 'on'` cocok dgn matrix test discovery (sequential, no retry biar tau real failure, screenshot otomatis untuk report).

### Established Patterns

- **Setup project di playwright config** — `setup` project (test pattern `global.setup.ts`) dipakai sebagai `dependencies` untuk project utama (`chromium`). Pola sama dipakai untuk register globalTeardown via `globalTeardown` field di defineConfig.
- **Test data via SQL seed (bukan API)** — Project sudah pakai `Data/SeedData.cs` untuk permanent seed. Matrix test mengikuti pola tapi temporary + tagged.
- **Helper organization** — Phase 307 decision: e2e-spesifik → `tests/e2e/helpers/`, shared → `tests/helpers/`. Diikuti Phase 315.
- **Account fixtures hardcoded** — Email/password literal di `accounts.ts`, bukan env var. Konsisten untuk lokal-only test.

### Integration Points

- **`tests/playwright.config.ts`** — Edit: register `globalTeardown` (path: `./e2e/global.teardown.ts` relatif testDir).
- **`tests/e2e/global.setup.ts`** — Extend: tambah BACKUP step + run seed SQL + tulis `.matrix-state.json` + Layer 1 validation. Existing assertion (app running check) tetap dipertahankan.
- **`docs/SEED_JOURNAL.md`** — Append entry temporary `MATRIX_TEST_2026_05_11` saat setup, mark `cleaned` saat teardown sukses.
- **`tests/.gitignore`** (atau create) — Tambah `.matrix-state.json`, `*.bak`, dan path snapshot.
- **`docs/test-reports/`** — Direktori output report; create kalau belum ada.

</code_context>

<specifics>
## Specific Ideas

- Endpoint contract di spec sudah ter-verified terhadap `Controllers/CMPController.cs` dan `Controllers/AssessmentAdminController.cs` saat spec ditulis (2026-05-11). Wave 0 hanya isi gap untuk MA save flow, Essay save flow, dan URL encoding.
- ID range 9001-9009 (was 9001-9007 di draft awal spec) dipilih supaya jelas keluar dari range normal (1-1000 typical) → mudah identifikasi visual saat debug DB lokal.
- Sentinel `[META-CollectorCheck]` mencegah scenario "happy path semua skenario discovery PASS, collector ternyata rusak silently" — tanpa sentinel ini, false confidence besar.
- Tag string `MATRIX_TEST_2026_05_11` memuat date supaya kalau ada test run di hari berbeda di masa depan, marker bisa dibedakan per run.
- Helper `examMatrix.ts` pakai `softAssert()` wrapper sehingga critical failure throw `SkipScenarioError` (skip sisa step skenario) sedangkan major/minor cuma record finding + lanjut.

</specifics>

<deferred>
## Deferred Ideas

- **QA-02 CI integration** — Tunggu matrix test proven stable di lokal. Saat itu baru pikir GitHub Actions / Azure DevOps integration. (Requirements doc § Future Requirements)
- **QA-03 Regression subset conversion** — Convert subset matrix test jadi smoke regression. Setelah matrix test berjalan beberapa kali.
- **QA-04 Visual regression** — Percy/Chromatic tooling. Out of scope v16.0.
- **QA-05 Multi-environment** — Extend ke staging/prod. Hanya kalau infra test bisa sentuh DB Dev/Prod dengan aman.
- **QA-06 Concurrency stress test** — Banyak peserta paralel di session sama. Bukan discovery focus.
- **QA-07 Coverage expansion ke flow lain** — CDP, IDP, Coaching, Worker management. Foundation Phase 315 stabil dulu.
- **Schema migration tambah kolom `Notes` ke AssessmentSession** — Ditolak untuk Phase 315 (test infra tidak boleh memodifikasi schema produksi). Kalau Notes ternyata berguna untuk audit log non-test, evaluasi terpisah sebagai feature work.
- **Smoke run gating via marker file `.smoke-passed`** — Terlalu banyak moving part. Cukup dokumentasi.
- **mssql node driver** — Spec menolak karena cukup `sqlcmd` via spawn. Kalau nanti perlu query result kompleks dari TypeScript, baru evaluasi.

### Reviewed Todos (not folded)

Tidak ada todo lain yang ter-match scope Phase 315.

</deferred>

---

*Phase: 315-assessment-matrix-test*
*Context gathered: 2026-05-11*
