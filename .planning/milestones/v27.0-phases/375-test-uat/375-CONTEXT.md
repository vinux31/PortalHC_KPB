# Phase 375: Test & UAT - Context

**Gathered:** 2026-06-13
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase **terakhir v27.0 Shuffle Toggle** — finalisasi test + UAT (REQ **SHUF-16**, + fold **SHUF-15**). Migration: **false** (kolom live sejak Phase 372).

Phase ini HANYA:
1. **xUnit consolidation** — 1 test sweep mode-matrix sbg single-source-of-truth (di ATAS 27 test existing yang sudah cover semua mode) + konfirmasi suite hijau (SC#1).
2. **Automated Playwright `shuffle.spec.ts`** (sisi ManagePackages) — 5 skenario render + save-PRG.
3. **Manual browser UAT exam-taking effect** (SC#2) — toggle ON/OFF berefek di urutan soal & opsi (2 peserta), checkpoint human-verify pola Phase 374.
4. **Fold SHUF-15** — cleanup komentar stale shuffle di `CMPController.cs` (kemungkinan SUDAH bersih dari Phase 373 — verify + close box).

**Temuan kunci scout (2026-06-13):** Mode-matrix xUnit **SUDAH 100% covered** oleh 372/373/374 — 27 test / 8 file, suite **347/347 hijau**, semua 11 dimensi (ON/OFF ×1/≥2 paket, round-robin determinisme, guard paket kosong, opsi ON/OFF, migration default, propagate, lock, reshuffle). ManagePackages-side UAT **sudah human-verified 7/7** di sesi browser Phase 374. Jadi kerja inti 375 = **Playwright UAT** (terutama exam-taking effect) + consolidation test + SHUF-15.

**BUKAN bagian phase ini:**
- Engine impl (Phase 373), UI/endpoint/lock/warning/reminder/hide impl (Phase 374) — sudah selesai.
- **Fix baseline e2e Phase 364** (`exam-taking.spec.ts` Flow A `.fixme` flat-form usang; `exam-types.spec.ts` W0 title-validation broken) — **out of scope**, tetap scope Phase 364 / backlog 999.7.
- Full automated exam-taking order-diff e2e — ditolak (pakai manual browser).

</domain>

<decisions>
## Implementation Decisions

### xUnit treatment (gray area dibahas)
- **D-01:** **Tambah 1 consolidation test** mode-matrix sweep (`[Theory]`/`[InlineData]`) yang gabung semua mode (ShuffleQuestions ON/OFF × 1/≥2 paket × ShuffleOptions ON/OFF + guard paket kosong) jadi SATU titik kebenaran eksplisit — di ATAS 27 test existing (BUKAN ganti). Plus jalankan full suite konfirmasi hijau (baseline 347→348+). SC#1 = suite hijau termasuk sweep.
- **D-01a:** JANGAN duplikasi assertion detail yang sudah di-cover `ShuffleEngineTests` (12 test) — sweep = high-level invariant per-mode (mis. ON→shuffled & seed-stable, OFF 1pkg→urut asli, OFF ≥2pkg→paket utuh round-robin index-stabil, opsi ON→dict non-empty/OFF→empty, all-empty→no DivideByZero). Reuse pola `new Random(42)` seed-stability.

### Playwright UAT — automation level (gray area dibahas)
- **D-02:** **Hybrid.** Automated `tests/e2e/shuffle.spec.ts` untuk **sisi ManagePackages** (stabil, pakai `createAssessmentViaWizard`) + **manual browser** untuk exam-taking effect (SC#2 urutan soal/opsi).
- **D-02a:** Scenario set automated = **5 render + save-PRG**: (1) card render + toggle saved-state + klik Simpan → success PRG (assert `TempData` success saja), (2) lock disabled + banner (assessment dgn peserta started), (3) reminder Pre/Post (Pre OFF + Post ON → alert di Post), (4) warning §9 live-JS flip (multi-paket ukuran beda → flip Acak Soal OFF muncul, ON hilang), (5) hide Proton-Th3/Manual (card tidak dirender). **Propagate-detail TIDAK di-assert ulang di e2e** (sudah unit test `ShufflePropagationTests`/`ShuffleUpdateEndpointTests` — no dobel).

### Manual exam-taking effect (gray area dibahas)
- **D-03:** **Manual browser 2-peserta diff** — Claude jalankan app, 1 assessment multi-paket, 2 peserta StartExam, banding urutan soal/opsi ON vs OFF di browser + screenshot. Engine determinism unit test = proxy pendukung (bukan pengganti — wiring engine→view perlu bukti e2e visual).
- **D-03a:** **Acceptance SC#2 (pass-bar exact):** PASS bila — ShuffleQuestions **ON** → urutan soal **beda** antar 2 peserta; ShuffleOptions **ON** → urutan opsi **beda**; ShuffleQuestions **OFF + ≥2 paket** → tiap worker dapat **1 paket UTUH urutan asli** (round-robin index, paket beda per worker). Bukti = visual browser + screenshot.

### Test data (gray area dibahas)
- **D-04:** **Seed temporary + snapshot/restore** ikut `docs/SEED_WORKFLOW.md`: snapshot DB lokal → seed assessment multi-paket + 2 peserta (klasifikasi `temporary + local-only`) → manual exam-diff → **restore DB** + tandai `docs/SEED_JOURNAL.md` cleaned. JANGAN biarkan seed nempel.

### Eksekusi & checkpoint (gray area dibahas)
- **D-05:** **Checkpoint UAT akhir** pola Phase 374 — automated `shuffle.spec.ts` jalan di wave test; lalu 1 checkpoint `human-verify` di akhir: Claude jalankan manual exam-diff + lapor, user approve. 1 stop point.

### Blocker handling (gray area dibahas)
- **D-06:** **Hindari flat-form total, JANGAN fix Phase 364.** Semua setup assessment di 375 pakai `createAssessmentViaWizard` (Phase 355 proven, modern 4-step wizard). JANGAN sentuh `exam-taking.spec.ts` `.fixme` / `exam-types.spec.ts` W0 / title-validation REST-06 — itu scope Phase 364 (out of scope 375). `shuffle.spec.ts` berdiri sendiri.
- **D-06a:** Manual exam-diff (D-03) pakai StartExam flow langsung (`/CMP/StartExam?sessionId=...`) — bukan flat-form create. Setup via wizard, assignment via assign-coachee/peserta existing.

### SHUF-15 fold (gray area dibahas)
- **D-07:** **Fold ke 375.** Cleanup komentar stale shuffle di `CMPController.cs`. **Re-grep 2026-06-13 menunjukkan komentar stale "option shuffle removed" SUDAH TIDAK ADA** — line 989 sekarang sudah benar (`// Option shuffle gated on ShuffleOptions`). Jadi SHUF-15 = **verify clean + close box** (kemungkinan sudah ter-cleanup di Phase 373 engine rewrite). Bila saat execute ditemukan komentar stale lain (re-grep `option shuffle removed`/`shuffle removed` di CMPController), bersihkan. Tandai SHUF-15 done.

### Dokumentasi UAT + DoD (gray area dibahas)
- **D-08:** **`375-HUMAN-UAT.md`** (pola 374, status `partial`) — catat hasil manual exam-diff + 5 skenario ManagePackages e2e. **Checkpoint manual-approve memenuhi SC#2.** Verifier terima manual-UAT sbg bukti SC#2 (render-conditional + exam-effect memang Razor/JS-runtime, tak layak unit; sudah dijustifikasi 374-VALIDATION manual-only).

### Claude's Discretion
- Isi exact `[Theory]/[InlineData]` consolidation sweep (D-01) — selama high-level per-mode invariant, no dobel detail.
- Markup/selector exact `shuffle.spec.ts` (reuse `wizardSelectors.ts` + pola `image-in-assessment.spec.ts`).
- Struktur/urutan wave plan (test vs e2e vs manual checkpoint) — diserahkan planner; manual exam-diff = checkpoint terakhir.
- Detail seed SQL multi-paket + 2 peserta (D-04).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Scope contract (keputusan terkunci)
- `.planning/ROADMAP.md:167-174` — Phase 375 goal + 2 Success Criteria (+ `:110` checkbox).
- `.planning/REQUIREMENTS.md:74` — SHUF-16 (test + UAT); `.planning/REQUIREMENTS.md:73` — SHUF-15 (cleanup komentar stale).
- `docs/superpowers/specs/2026-06-13-shuffle-toggle-design.md` **§11 Testing** (daftar test wajib: ON 1/≥2, OFF 1/≥2 round-robin, guard kosong, opsi ON/OFF, migration default, propagate, lock, reshuffle, Playwright UAT) + **§13** grading by `PackageOption.Id`.

### Engine & coverage existing (JANGAN duplikasi)
- `Helpers/ShuffleEngine.cs` — engine under test. API: `BuildQuestionAssignment(packages, shuffleQuestions, workerIndex, rng)` (ON=cross-package sampling K-min; OFF 1pkg=OrderBy(q.Order); OFF ≥2pkg=`packagesWithQuestions[workerIndex % count]` setelah filter empty), `BuildOptionShuffle(questions, shuffleOptions, rng)` (ON=dict Fisher-Yates / OFF=empty), `Shuffle<T>`. **Determinisme = parameter `Random rng` (seed); test pakai `new Random(42)`.**
- 8 file test existing (27 test, semua mode COVERED — sweep D-01 di atas ini, JANGAN ulang detail): `HcPortal.Tests/{ShuffleEngineTests(12), ShuffleCreatePersistenceTests(3), ShuffleToggleRulesTests(3 Theory), ShuffleMigrationTests(2), ShufflePropagationTests(2), ShuffleReshuffleTests(2), ShuffleLockGuardTests(3), ShuffleUpdateEndpointTests(1)}.cs`.

### UI contract yang di-UAT (Phase 374)
- `.planning/phases/374-ui-managepackages-lock-pre-post/374-UI-SPEC.md` — copy + behavior card Pengacakan (toggle/lock/reminder/warning/hide) yang harus tampil benar di UAT.
- `.planning/phases/374-ui-managepackages-lock-pre-post/374-VALIDATION.md` — Manual-Only rows (4 render-conditional di-defer ke 375 Playwright) = peta skenario UAT.
- `.planning/phases/374-ui-managepackages-lock-pre-post/374-SUMMARY.md`-03 — endpoint `UpdateShuffleSettings`, route `/Admin/ManagePackages?assessmentId=X`.

### Playwright e2e infra (reuse — JANGAN reinvent)
- `tests/playwright.config.ts` — `testDir ./e2e`, baseURL `localhost:5277`, `fullyParallel:false` (serial/workers=1), global setup/teardown (Phase 315 BACKUP→seed→RESTORE+Layer4).
- `tests/e2e/helpers/examTypes.ts` — **`createAssessmentViaWizard(page, opts)`** (modern 4-step, D-06 wajib pakai ini); `addQuestionViaForm`.
- `tests/e2e/helpers/wizardSelectors.ts` — selector terpusat (JANGAN hardcode DOM id).
- `tests/helpers/{auth.ts (login(page,'admin'|'hc')), accounts.ts (admin@pertamina.com/123456 dst), dbSnapshot.ts (backup/restore/execScript), utils.ts}`.
- `tests/e2e/image-in-assessment.spec.ts` — **template pola UAT proven** (Phase 355: wizard create + setInputFiles + DB backup/restore beforeAll/afterAll). Tiru struktur untuk `shuffle.spec.ts`.

### Workflow lokal (CLAUDE.md)
- `docs/DEV_WORKFLOW.md` — verifikasi lokal wajib (`dotnet build`+`dotnet run`@5277+DB lokal); jangan edit DB Dev/Prod; jangan push tanpa verify.
- `docs/SEED_WORKFLOW.md` + `docs/SEED_JOURNAL.md` — D-04 seed temporary + snapshot/restore + journal.
- **Env Playwright lokal:** `Authentication__UseActiveDirectory=false` + (bila login 500) `ConnectionStrings__DefaultConnection='Server=lpc:localhost\SQLEXPRESS;Database=HcPortalDB_Dev;...'` shared-memory override (NTLM loopback fix, Phase 364-01).

### SHUF-15 target
- `Controllers/CMPController.cs` — re-grep `option shuffle removed`/`shuffle removed` (line 1054 sudah drift; line 989 sudah benar "gated on ShuffleOptions"). Verify clean / cleanup bila ada.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`createAssessmentViaWizard` + `wizardSelectors`** (tests/e2e/helpers) — setup assessment e2e tanpa flat-form (D-06).
- **`login(page, accountKey)`** + seed accounts (admin/hc/coachee `123456`) — tests/helpers/auth.ts + accounts.ts.
- **`dbSnapshot` backup/restore/execScript** + global.setup/teardown (Phase 315) — DB isolation lifecycle (D-04).
- **`image-in-assessment.spec.ts`** — template struktur spec (wizard create + DB snapshot beforeAll/afterAll + render assert).
- **Text-based option selectors (shuffle-safe)** — `label:has-text("${optionText}")`, BUKAN posisi (penting buat assert urutan opsi acak).
- **`ShuffleEngine` API + seed determinism** — pola `new Random(42)` untuk consolidation sweep (D-01).

### Established Patterns
- Playwright serial (`fullyParallel:false`, workers=1) — DB isolation; combined run wajib serial (memory: `--workers=1`).
- ManagePackages route `/Admin/ManagePackages?assessmentId=X` (controller `[Route("Admin/[action]")]`).
- StartExam peserta: `/CMP/StartExam?sessionId=...` → answer (text-match) → submit `Kumpulkan Ujian`.
- 27 shuffle test existing = mode-matrix sudah covered; 375 consolidation = high-level invariant, no dobel.

### Integration Points
- `tests/e2e/shuffle.spec.ts` (BARU) — masuk suite e2e existing, global setup/teardown otomatis.
- `HcPortal.Tests/ShuffleEngineTests.cs` atau file baru — consolidation sweep `[Theory]` (D-01).
- `375-HUMAN-UAT.md` (BARU) — hasil manual exam-diff + e2e (D-08), status partial.
- `Controllers/CMPController.cs` — SHUF-15 verify/cleanup (D-07).

### ⚠️ Constraint Koordinasi
- **STATE.md sengaja pinned v25.0** (roadmap v27.0 append-only). Phase dir `375-test-uat` dibuat manual. JANGAN `/gsd-new-milestone`/`complete-milestone` vanilla.
- Sequential strict v27.0: 372 ✅→373 ✅→374 ✅→**375** (terakhir).
- Suite baseline **347/347** — sweep D-01 tambah jadi 348+; e2e shuffle.spec.ts terpisah (npx playwright).

</code_context>

<specifics>
## Specific Ideas

- **xUnit half praktis sudah selesai** — 375 SC#1 = jalankan + consolidation sweep + dokumentasi peta covered, bukan nulis 20 test baru.
- **ManagePackages UAT sudah manual-verified 7/7** (Phase 374 browser) — 375 meng-automate-kan-nya jadi `shuffle.spec.ts` (regresi-proof) + tambah yang BELUM diverifikasi: **exam-taking effect** (urutan soal/opsi yang dilihat peserta).
- **Bukti SC#2 = visual + screenshot** 2 peserta (ON beda, OFF round-robin paket utuh).
- **JANGAN scope-creep ke Phase 364** (exam-taking baseline broken) — `shuffle.spec.ts` wizard-based standalone.

</specifics>

<deferred>
## Deferred Ideas

- **Fix baseline e2e Phase 364** (`exam-taking.spec.ts` Flow A `.fixme` flat-form; `exam-types.spec.ts` W0 title-validation REST-06) → tetap Phase 364 / backlog 999.7. BUKAN 375.
- **Full automated exam-taking order-diff e2e** (2 worker assert urutan beda otomatis) → ditolak by design; pakai manual browser (D-03). Bisa jadi enhancement future bila exam-taking infra distabilkan.
- **Pre-existing bug SURF-317-A** (MultipleAnswer results 500, "do-not-fix" Phase 364-02) → JANGAN sentuh di 375.

### Reviewed Todos (not folded)
None — diskusi tetap dalam scope phase.

</deferred>

---

*Phase: 375-test-uat*
*Context gathered: 2026-06-13*
