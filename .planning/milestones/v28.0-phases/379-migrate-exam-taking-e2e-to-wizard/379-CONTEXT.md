# Phase 379: Migrate exam-taking e2e to wizard - Context

**Gathered:** 2026-06-14
**Status:** Ready for planning

<domain>
## Phase Boundary

Migrasi 10 create-flow (A-J, `test.fixme(true, ...)`) di `tests/e2e/exam-taking.spec.ts` dari flat-form `/Admin/CreateAssessment` usang → **wizard CreateAssessment 4-langkah + layer PACKAGE** (wizard create → create package → add questions ke package). Suite hijau `--workers=1`. PLUS **tambah 1 flow essay baru** untuk cover sinergi GRADE-01 (depends 376).

**Depends on:** 376 (GRADE essay-score fix — Flow essay baru memvalidasi fix-nya e2e).
**REQ:** E2E-01. **Migration=false.** **Scope = test-infra only** (tak ubah kode produksi/controller/view; bila migrasi mengungkap bug produksi, surface ke backlog, jangan fix inline di fase test).

**Perubahan struktural yang memaksa migrasi:** assessment kini WAJIB lewat PACKAGE. Pola lama (questions langsung ke assessment via `/Admin/ManageQuestions?id=`) usang — diganti `createDefaultPackage` + `addQuestionViaForm(packageId)`.

**OUT of scope:**
- Test debt e2e lain (di luar exam-taking.spec.ts).
- Fix kode produksi (fase ini murni migrasi test).
- Rewrite full harness / refactor signature helper existing.

</domain>

<decisions>
## Implementation Decisions

### Coverage Essay (sinergi GRADE-01, depends 376)
- **D-01:** **Tambah flow essay BARU** (mis. Flow K) — bukan delegasi ke e2e 376, bukan sekadar nempel ke flow existing. Flow K full e2e: wizard create assessment essay-only/mixed → worker answer essay (`fillEssayAnswer`) → HC grade + finalize (`gradeSingleEssaySession`) → **ASSERT `AssessmentSessions.Score` teragregasi (bukan 0)**. = validasi e2e end-to-end fix GRADE-01 Phase 376 di dalam suite exam-taking. Net regression terkuat untuk jalur essay.

### Flow E Proton T3 Interview (double-drift)
- **D-02:** **Migrasi PENUH + re-check Proton form.** Migrasi wizard-create E1 + re-verifikasi/perbaiki step interview (E2 badge "Interview Dijadwalkan"/no-Start, E3 `form[action*="SubmitInterviewResults"]`) terhadap perubahan Proton v25.0 (Phase 358-363). E hijau penuh seperti flow lain. SC1 "10 flow" terpenuhi utuh (E tidak di-defer / tidak best-effort-skip).

### Definisi "suite hijau" (DoD)
- **D-03:** **Full green run lokal WAJIB.** Semua flow (A-J + K essay) benar-benar DIJALANKAN hijau lokal via `npx playwright test tests/e2e/exam-taking.spec.ts --workers=1` (env: SQLBrowser + `lpc:` shared-mem + `Authentication__UseActiveDirectory=false`); bukti run dilampirkan. Flow time-dependent (G timer-1min, H real-time) di-handle **deterministik** sebisa mungkin (assertion timer-state / SignalR event, bukan wait wall-clock 60 detik) untuk hindari flaky. Sesuai mandat verify lokal CLAUDE.md.

### Strategi Helper
- **D-04:** **Reuse + extend `tests/e2e/helpers/examTypes.ts`** sebagai jalur kanonik untuk SEMUA create/question step. Pakai existing: `createAssessmentViaWizard`, `createDefaultPackage`, `addQuestionViaForm`, `fillEssayAnswer`, `gradeSingleEssaySession`, `submitExamTwoStep`, `checkMAOptionsForQuestion`. **Extend ADDITIVE** untuk gap: token-required (Flow B — `isTokenRequired`/`accessToken` di `wizardSelectors`), interview/Proton type (Flow E), paste-import (Flow D3), durasi 1-menit (Flow G). **JANGAN refactor signature helper existing** (preserve blame; pola "extend additive" wizardSelectors.ts). Konsisten dgn `exam-types`/`shuffle`/`proton-bypass` yang sudah pakai helper ini.

### Claude's Discretion
- **Penempatan Flow K:** append ke `exam-taking.spec.ts` (suite sama) vs file terpisah — planner pilih (default: append, satu suite).
- **Cleanup package-layer:** step delete tiap flow kini harus tangani package (DeleteAssessment Phase 353 atomic cascade kemungkinan sudah hapus package — verifikasi; bila tidak, tambah cleanup package). Diskresi planner.
- **Bentuk extend helper** (signature param token/interview/paste/duration) — planner desain, tetap additive.
- **Handling deterministik Flow G/H** (timer/real-time) — planner pilih mekanisme (assertion state vs short-wait).

### Folded Todos
*(none)*

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase scope & requirements
- `.planning/ROADMAP.md` — Phase 379 entry + SC1-SC3 (SC1: 10 flow A-J pakai wizard, flat-form `.fixme` dihapus; SC2: suite hijau `--workers=1`; SC3: flow essay ter-cover, sinergi GRADE-01).
- `.planning/REQUIREMENTS.md` §E2E — **E2E-01** (10 create flow A-J migrasi flat-form → wizard 4-langkah; spec hijau; regression net termasuk essay GRADE-01).

### Test assets (the canonical migration tooling)
- `tests/e2e/helpers/examTypes.ts` — **helper wizard kanonik**: `createAssessmentViaWizard` (L51), `createDefaultPackage` (L121), `addQuestionViaForm` (L168), `submitExamTwoStep` (L255), `checkMAOptionsForQuestion` (L287), `fillEssayAnswer` (L320), `gradeSingleEssaySession` (L386), `createPrePostAssessmentViaWizard` (L538). Extend di sini (D-04).
- `tests/e2e/helpers/wizardSelectors.ts` — selector wizard 4-step (`wizardSelectors` L36: step/pill/btnNext, Step1-3 fields incl `isTokenRequired`/`accessToken` L67-68; `questionFormSelectors` L103; `prePostWizardSelectors` L92). Extend additive (preserve blame).
- `tests/e2e/exam-taking.spec.ts` — target migrasi (1736 baris, 10 describe Flow A-J, serial mode L6, `test.fixme(true,...)` di tiap describe; belum import examTypes.ts).
- `tests/e2e/exam-types.spec.ts`, `tests/e2e/shuffle.spec.ts`, `tests/e2e/proton-bypass.spec.ts` — **contoh spec yang SUDAH pakai wizard helper** (pola referensi migrasi).

### Cross-phase
- Phase 376 (depends) — fix GRADE-01 (`AssessmentScoreAggregator` + `RecomputeEssayScores`); Flow K (D-01) memvalidasi fix ini e2e. Lihat `.planning/phases/376-*/` artefak.

### Project workflow / env
- `CLAUDE.md` — Develop Workflow (verify lokal wajib; jangan edit Dev/Prod).
- Local e2e SQL env (STATE.md / memory `reference_local_e2e_sql_env_fix`): start SQLBrowser + `lpc:` shared-memory conn override + combined Playwright `--workers=1` + AD lokal `Authentication__UseActiveDirectory=false dotnet run`.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets (jalur migrasi)
- `examTypes.ts` helper suite (lihat canonical_refs) — semua building block create/package/question/answer/grade SUDAH ada & proven. Migrasi = ganti step flat-form dgn panggilan helper.
- Discriminated union `QuestionInput` (`examTypes.ts:18`) — MC/MA/Essay typed; `CreateAssessmentOpts` (L23) + `CreatePrePostOpts` (L517).

### Pola LAMA yang diganti (per flow, contoh Flow A)
- A1 flat-form: `page.goto('/Admin/CreateAssessment')` + isi `#Title`/`#Category`/`#ScheduleDate`/`#ScheduleTime`/`#DurationMinutes`/`#PassPercentage`/`#AllowAnswerReview`/`#GenerateCertificate` + `#submitBtn` (1 halaman, no wizard, no package) → ganti `createAssessmentViaWizard(...)` + `createDefaultPackage(...)`.
- A2/A3: `/Admin/ManageQuestions?id={assessmentId}` + `textarea[name="question_text"]`/`input[name="options"]`/`input[name="correct_option_index"]`/"Tambah Soal" (questions langsung ke assessment) → ganti `addQuestionViaForm(packageId, q)` ke ManagePackageQuestions.

### Flow-specific gap (extend helper, D-04)
- Flow B token: wizard `#IsTokenRequired`/`#AccessToken` (selector ada, helper opts belum) — extend `CreateAssessmentOpts`.
- Flow D package+paste: D2 sudah create package, D3 paste-import (`textarea[name="pasteText"]` di Import Questions) — D sebagian sudah package-aware; D1 create masih flat-form.
- Flow E Proton interview: E1 create (Proton T3 type) + E2 badge + E3 `SubmitInterviewResults` form — re-check vs Proton 358-363 (D-02).
- Flow G timer: assessment durasi 1-menit — extend duration handling deterministik (D-03).

### Established Patterns
- `test.describe.configure({ mode: 'serial' })` — flow stateful (title/assessmentId/packageId share antar test dalam describe).
- Akun nyata: `rino.prasetyo@pertamina.com` (worker), hc/coachee via `helpers/auth login`.
- successModal `data-bs-backdrop="static"` — WAJIB dismiss via `modalManageBtn` sebelum nav (helper sudah handle).
- Combined run WAJIB `--workers=1` (DB isolation).

### Integration Points
- `examTypes.ts` + `wizardSelectors.ts` (extend additive).
- `exam-taking.spec.ts` (import helper baru + rewrite create/question steps semua flow + Flow K baru).
- DeleteAssessment cascade (Phase 353 atomic) — cleanup package-layer (verifikasi).

</code_context>

<specifics>
## Specific Ideas

- Flow K essay = bukti hidup fix 376 (GRADE-01) di e2e — assert `Score` teragregasi > 0 setelah grade+finalize, BUKAN hanya badge "Sudah Dinilai".
- Mirror pola create di `exam-types.spec.ts`/`shuffle.spec.ts` (sudah wizard) untuk konsistensi.

</specifics>

<deferred>
## Deferred Ideas

- **Full e2e harness rewrite** / refactor signature helper existing — DITOLAK (extend additive saja, D-04).
- **Fix bug produksi** yang mungkin terungkap saat migrasi — surface ke backlog, jangan fix inline di fase test.
- **Test debt e2e di luar exam-taking.spec.ts** — out of scope.

### Reviewed Todos (not folded)
*(tidak ada todo match relevan untuk fase ini)*

</deferred>

---

*Phase: 379-migrate-exam-taking-e2e-to-wizard*
*Context gathered: 2026-06-14*
