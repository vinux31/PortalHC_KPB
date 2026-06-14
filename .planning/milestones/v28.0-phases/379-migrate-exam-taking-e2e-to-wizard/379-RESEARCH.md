# Phase 379: Migrate exam-taking e2e to wizard - Research

**Researched:** 2026-06-14
**Domain:** Playwright TypeScript e2e migration (flat-form → wizard) + essay regression coverage (GRADE-01)
**Confidence:** HIGH (semua temuan diverifikasi langsung dari source codebase di sesi ini)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Tambah flow essay BARU (Flow K) — bukan delegasi ke e2e 376, bukan nempel ke flow existing. Flow K full e2e: wizard create assessment essay-only/mixed → worker answer essay (`fillEssayAnswer`) → HC grade + finalize (`gradeSingleEssaySession`) → **ASSERT `AssessmentSessions.Score` teragregasi (bukan 0)**. = validasi e2e end-to-end fix GRADE-01 Phase 376.
- **D-02:** Flow E (Proton T3 interview) migrasi PENUH + re-check Proton form. Migrasi wizard-create E1 + re-verifikasi/perbaiki step interview (E2 badge "Interview Dijadwalkan"/no-Start, E3 `form[action*="SubmitInterviewResults"]`) terhadap perubahan Proton v25.0 (Phase 358-363). E hijau penuh seperti flow lain (TIDAK di-defer / TIDAK best-effort-skip).
- **D-03:** Full green run lokal WAJIB. Semua flow (A-J + K) DIJALANKAN hijau lokal via `npx playwright test tests/e2e/exam-taking.spec.ts --workers=1` (env: SQLBrowser + `lpc:` shared-mem + `Authentication__UseActiveDirectory=false`); bukti run dilampirkan. Flow time-dependent (G timer-1min, H real-time) di-handle deterministik (assertion timer-state / SignalR event / DB-state, bukan wait wall-clock 60 detik).
- **D-04:** Reuse + extend `tests/e2e/helpers/examTypes.ts` sebagai jalur kanonik untuk SEMUA create/question step. Pakai existing: `createAssessmentViaWizard`, `createDefaultPackage`, `addQuestionViaForm`, `fillEssayAnswer`, `gradeSingleEssaySession`, `submitExamTwoStep`, `checkMAOptionsForQuestion`. Extend ADDITIVE untuk gap: token-required (B), interview/Proton type (E), paste-import (D3), durasi 1-menit (G). JANGAN refactor signature helper existing (preserve blame).

### Claude's Discretion
- **Penempatan Flow K:** append ke `exam-taking.spec.ts` (suite sama) vs file terpisah — planner pilih (default: append, satu suite).
- **Cleanup package-layer:** step delete tiap flow kini harus tangani package (DeleteAssessment Phase 353 atomic cascade kemungkinan sudah hapus package — verifikasi; bila tidak, tambah cleanup package). Diskresi planner.
- **Bentuk extend helper** (signature param token/interview/paste/duration) — planner desain, tetap additive.
- **Handling deterministik Flow G/H** (timer/real-time) — planner pilih mekanisme (assertion state vs short-wait).

### Deferred Ideas (OUT OF SCOPE)
- Full e2e harness rewrite / refactor signature helper existing — DITOLAK (extend additive saja, D-04).
- Fix kode produksi yang mungkin terungkap saat migrasi — surface ke backlog, jangan fix inline.
- Test debt e2e di luar `exam-taking.spec.ts` — out of scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| E2E-01 | 10 create flow di `tests/e2e/exam-taking.spec.ts` (A-J `.fixme`) dimigrasi dari flat-form usang ke wizard CreateAssessment 4-langkah; spec hijau (regression net termasuk essay GRADE-01) | Per-flow migration map (§ Architecture Patterns), helper kanonik sudah lengkap (§ Standard Stack / Don't Hand-Roll), pola FLOW L `exam-types.spec.ts` = blueprint Flow K (§ Code Examples), Validation Architecture = full green run sbg deliverable [VERIFIED: tests/e2e/exam-types.spec.ts, examTypes.ts, exam-taking.spec.ts] |
</phase_requirements>

## Summary

Fase ini adalah migrasi TEST-INFRA murni: 10 describe-block (Flow A–J) di `tests/e2e/exam-taking.spec.ts` masing-masing punya `test.fixme(true, ...)` karena step create-nya pakai flat-form `/Admin/CreateAssessment` yang usang (sekarang wizard 4-langkah + layer PACKAGE wajib). Kabar baiknya: **seluruh tooling migrasi sudah ada dan proven** di `tests/e2e/helpers/examTypes.ts` + `tests/e2e/helpers/wizardSelectors.ts`, dipakai oleh `exam-types.spec.ts`, `shuffle.spec.ts`, dan `proton-bypass.spec.ts` yang sudah hijau. Migrasi = ganti ~2 step per flow (create + add-question) dengan panggilan helper; mayoritas step worker-side (start/answer/submit/results/monitoring/cleanup) TIDAK berubah karena selector CMP/StartExam/Results/Monitoring tidak ter-drift. [VERIFIED: codebase grep + read]

Untuk Flow K (D-01), tidak perlu menemukan ulang apa pun: **FLOW L di `exam-types.spec.ts:305-428` sudah persis blueprint-nya** — wizard create essay → `createDefaultPackage` → `addQuestionViaForm({type:'Essay'})` → worker `fillEssayAnswer` + `submitExamTwoStep` → HC `gradeSingleEssaySession(score:80)` → assert `db.queryScalar('SELECT ISNULL(Score,-1) FROM AssessmentSessions WHERE Id={sessionId}')` === 80. FLOW L bahkan sudah di-un-fixme oleh Phase 376 (komentar GRADE-01 ada di L6). Flow K = port pola ini ke suite exam-taking (default: append). [VERIFIED: tests/e2e/exam-types.spec.ts:412-427]

Tiga drift nyata yang perlu ditangani: (1) **token** — spec lama B1 pakai `#tokenInputContainer` + tombol `:has-text("Generate")`, markup current pakai `#tokenSection` + `onclick="generateToken()"`; `wizardSelectors` sudah punya `isTokenRequired`/`accessToken` tapi `CreateAssessmentOpts` belum punya field-nya → extend additive. (2) **Proton T3 interview** (Flow E) — create Proton ada di Step 1 (`#protonTrackSelect` name=`ProtonTrackId`, opsi `data-tahun`), form interview di monitoring detail muncul saat `Category=="Assessment Proton" && TahunKe=="Tahun 3"` dengan field `judges`/`aspect_*`/`notes`/`isPassed`/`@Html.AntiForgeryToken()` — field name cocok dengan E3 lama, tapi flat-form create-nya hilang. (3) **paste-import** (Flow D3) — `textarea[name="pasteText"]` masih ada tapi sekarang harus terhubung ke package (D2 sudah package-aware). Flow G timer & Flow H real-time perlu di-deterministik-kan (DB-state assertion alih-alih `waitForTimeout(70_000)`). [VERIFIED: Views/Admin/CreateAssessment.cshtml, Views/Admin/AssessmentMonitoringDetail.cshtml, Controllers/AssessmentAdminController.cs:3669]

**Primary recommendation:** Import helper dari `./helpers/examTypes.ts`, tambah ~4 field optional ke `CreateAssessmentOpts` (`isTokenRequired`, `accessToken`, `protonTrackId`/`protonTrackTahun`, `shuffle*` jika perlu) + helper baru kecil untuk paste-import, lalu rewrite step create/add-question tiap flow mengikuti pola FLOW L/O exam-types. Flow K = port FLOW L. Verifikasi via full green run `--workers=1`.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Create assessment (wizard 4-step) | API/Backend (AssessmentAdminController.Add) via Browser wizard | — | Flat-form usang; jalur kanonik = wizard markup `CreateAssessment.cshtml` |
| Create package + add questions | API/Backend (CreatePackage/ManagePackageQuestions) via Browser | — | Assessment kini WAJIB lewat PACKAGE; questions ke package, bukan assessment |
| Worker take exam (start/answer/submit) | Browser (CMP/StartExam) + SignalR (AssessmentHub) | — | Tidak ter-drift; helper `submitExamTwoStep`/`fillEssayAnswer`/`checkMAOptionsForQuestion` survive |
| HC grade essay + finalize | API/Backend (SubmitEssayScore/FinalizeEssayGrading) via Browser | DB (Score aggregation, fix 376) | `gradeSingleEssaySession` helper; assertion = DB read `AssessmentSessions.Score` |
| Score aggregation correctness (GRADE-01) | API/Backend (AssessmentScoreAggregator + FinalizeEssayGrading, Phase 376) | DB | Flow K assert nilai teragregasi via DB scalar; bukan tier test |
| Proton T3 interview submit | API/Backend (SubmitInterviewResults) via Browser | DB | Form post standar HTML + antiforgery; create via Step 1 ProtonTrackId |
| Real-time monitoring state | API/Backend polling (GetMonitoringProgress) + Browser | DB | Deterministik via DB-state/JSON-endpoint assert, bukan wall-clock |
| Test data lifecycle (snapshot/restore) | Test harness (global.setup/teardown) + DB (sqlcmd) | — | BACKUP penuh pre-suite, RESTORE penuh post-suite → cleanup per-flow non-kritis untuk integritas |

## Standard Stack

### Core (semua SUDAH ada — fase ini reuse, tidak menambah dependency)
| Library/Asset | Versi/Lokasi | Purpose | Why Standard |
|---------|-------------|---------|--------------|
| `@playwright/test` | `tests/playwright.config.ts` | e2e runner | Sudah dipakai seluruh suite [VERIFIED] |
| `tests/e2e/helpers/examTypes.ts` | flat-export functions | wizard create/package/question/answer/grade | Jalur kanonik D-04; dipakai exam-types/shuffle/proton-bypass [VERIFIED] |
| `tests/e2e/helpers/wizardSelectors.ts` | `wizardSelectors`/`questionFormSelectors`/`prePostWizardSelectors`/`extraTimeSelectors` | selector single-source | Sudah punya `isTokenRequired`/`accessToken` (L67-68) [VERIFIED: wizardSelectors.ts:67-68] |
| `tests/helpers/dbSnapshot.ts` | `queryScalar`/`queryString`/`backup`/`restore`/`execScript` | DB assert + snapshot via sqlcmd localhost-only | DB-based verify pola SURF-317-A; localhost guard CLAUDE.md [VERIFIED: dbSnapshot.ts:116-128] |
| `tests/helpers/auth.ts` + `accounts.ts` | `login(page, 'hc'\|'coachee'\|'coachee2')` | login helper | `coachee`=rino.prasetyo, `coachee2`=iwan3, `hc`=meylisa.tjiang [VERIFIED: accounts.ts:3-5] |
| `tests/helpers/utils.ts` | `uniqueTitle`/`today`/`autoConfirm`/`yesterday` | util test | Sudah dipakai exam-taking [VERIFIED: utils.ts] |

### Supporting (helper existing yang relevan untuk flow downstream)
| Helper | Signature ringkas | Kapan dipakai |
|--------|---------|-------------|
| `createAssessmentViaWizard(page, CreateAssessmentOpts)` | examTypes.ts:51 | semua create standard (A,C,D,F,G,H,I,J) |
| `createDefaultPackage(page, packageName='Paket A') → packageId` | examTypes.ts:121 | semua flow yang add question |
| `addQuestionViaForm(page, packageId, QuestionInput, images?)` | examTypes.ts:168 | add MC/MA/Essay ke package |
| `submitExamTwoStep(page)` | examTypes.ts:255 | StartExam → ExamSummary → Results |
| `fillEssayAnswer(page, qCard, answer)` | examTypes.ts:320 | Flow K worker essay (direct SignalR invoke) |
| `gradeSingleEssaySession(pageHc, {title,category,scheduleDate,sessionId,score})` | examTypes.ts:386 | Flow K HC grade + finalize |
| `checkMAOptionsForQuestion(page, qCard, optionTexts)` | examTypes.ts:287 | bila ada MA question (opsional) |
| `createPrePostAssessmentViaWizard(page, CreatePrePostOpts)` | examTypes.ts:538 | TIDAK dipakai (tak ada PrePost flow di exam-taking) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| DB-scalar assert `Score` (Flow K) | UI assert badge "Sudah Dinilai" pada Results | UI badge TIDAK membuktikan agregasi numerik (D-01 minta `Score != 0`). FLOW L sudah pilih DB-assert via SURF-317-A workaround (Results page essay-only kadang tidak surface skor numerik). DB-assert = pilihan benar [VERIFIED: exam-types.spec.ts:412-421] |
| Extend `CreateAssessmentOpts` (additive) | Buat helper `createTokenAssessmentViaWizard` baru | Field optional lebih hemat & DRY; helper baru duplikasi 4-step. D-04 izinkan dua-duanya; additive field lebih kecil blast-radius |
| `waitForTimeout(70_000)` (Flow G) | DB/JS-state assertion timer-expired | Wall-clock = flaky + lambat (D-03 larang). Deterministik wajib |

**Installation:** Tidak ada install. Semua dependency sudah terpasang. Verifikasi:
```bash
# (opsional) konfirmasi tooling sudah ada
npx playwright --version
```

**Version verification:** N/A — fase ini tidak menambah package. Semua helper internal repo.

## Architecture Patterns

### System Architecture Diagram (alur data 1 flow tipikal pasca-migrasi)

```
[HC login] ──> createAssessmentViaWizard(opts)
                 │  POST /Admin/CreateAssessment (4-step wizard)
                 │  → successModal.show
                 ▼
            extract assessmentId dari #modal-manage-btn href
                 │  (regex /(?:\/|assessmentId=)(\d+)/)
                 ▼
[HC] ──> /Admin/ManagePackages?assessmentId=N ──> createDefaultPackage() → packageId
                 │  POST /Admin/CreatePackage
                 ▼
[HC] ──> addQuestionViaForm(packageId, q) ×N
                 │  POST /Admin/ManagePackageQuestions?packageId=N
                 ▼
[Worker login] ──> /CMP/Assessment ──> .btn-start-standard / Resume
                 │  (dialog accept) → /CMP/StartExam/{sessionId}
                 │  SignalR assessmentHub Connected
                 ▼
   answer (exam-radio / checkMAOptionsForQuestion / fillEssayAnswer)
                 │
                 ▼  submitExamTwoStep → /CMP/ExamSummary → /CMP/Results/{sessionId}
                 │
   ┌─────────────┴───────────────┐
   ▼ (essay path, Flow K)         ▼ (objective path)
[HC] gradeSingleEssaySession      [HC] AssessmentMonitoring(Detail) verify
   │ SubmitEssayScore + Finalize     │ status/score/result
   ▼ (fix 376 aggregates)            ▼
[DB assert] queryScalar Score=80  [cleanup] DeleteAssessment (Hapus Grup)
                                         │  atomic cascade Phase 353 → package+question ikut
                                         ▼
                              [teardown] db.restore(snapshot) — full DB reset
```

### Recommended File Touch Map
```
tests/e2e/
├── exam-taking.spec.ts        # REWRITE create/add-question tiap flow A-J + APPEND Flow K + hapus 10 .fixme
├── helpers/
│   ├── examTypes.ts           # EXTEND additive: CreateAssessmentOpts +token/+proton; helper paste-import baru
│   └── wizardSelectors.ts     # tambah selector tokenSection/protonTrackSelect kalau belum cukup (additive)
└── (global.setup.ts / global.teardown.ts — TIDAK disentuh; snapshot/restore otomatis)
```

### Pattern 1: Wizard create + extract assessmentId (pola kanonik)
**What:** Ganti flat-form create dengan helper, ambil assessmentId dari modal.
**When to use:** Semua flow create standard.
```typescript
// Source: tests/e2e/exam-types.spec.ts:314-333 (FLOW L) — VERIFIED proven
await login(page, 'hc');
await createAssessmentViaWizard(page, {
  title, category: 'OJT', scheduleDate: today(), scheduleTime: '00:01',
  durationMinutes: 30, passPercentage: 60,
  allowAnswerReview: true, generateCertificate: false,
  participantEmails: ['rino.prasetyo@pertamina.com'],
});
const href = await page.locator('#modal-manage-btn').getAttribute('href');
assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
```

### Pattern 2: Package + question (ganti `/Admin/ManageQuestions?id=` flat form)
```typescript
// Source: tests/e2e/exam-types.spec.ts:336-355 — VERIFIED
await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
await page.waitForLoadState('networkidle');
packageId = await createDefaultPackage(page);
await addQuestionViaForm(page, packageId, {
  type: 'MultipleChoice', text: 'Apa kepanjangan OJT?',
  options: ['On the Job Training','Online Job Test','Operation Job Task','Operational Job Training'],
  correctIndex: 0, score: 100,
});
```

### Pattern 3: Worker answer by option-text (anti-shuffle robust)
**What:** Cross-package shuffle + per-question option shuffle aktif (anti-cheat). Positional `.nth()` mapping correctIndex = SALAH. Cari label berdasarkan teks.
```typescript
// Source: tests/e2e/exam-taking.spec.ts:1653-1659 (313.1) + examTypes.ts:287-303 — VERIFIED
const qCard = page.locator('[id^="qcard_"]').nth(i);
await qCard.locator('label[id^="lbl_"]').filter({ hasText: 'Pilihan A' }).first().click();
// MA: checkMAOptionsForQuestion(page, qCard, ['opt text 1','opt text 2'])
```
> Catatan: flow lama A6/D6/F3 pakai `.exam-radio.nth(correctIdx)` positional dengan heuristik teks `qText.includes('durasi')`. Itu fragile tapi survive sebagai downstream step. Migrasi BOLEH biarkan jika hijau; bila flaky, ganti ke pattern label-by-text. (Diskresi planner — minimal change D-03.)

### Anti-Patterns to Avoid
- **Refactor signature helper existing** — D-04 melarang. Tambah field optional / helper baru, JANGAN ubah param order yang ada.
- **`waitForTimeout(70_000)` untuk timer/poll** — flaky & lambat (D-03). Pakai DB/JS-state assert.
- **Assert essay score via UI badge saja** — tidak membuktikan agregasi numerik (D-01). Wajib DB-scalar.
- **Positional `.nth(correctIndex)` untuk jawaban benar** — shuffle aktif; pakai option-text match.
- **Lupa dismiss successModal (`data-bs-backdrop="static"`)** — helper `createAssessmentViaWizard` TIDAK auto-dismiss; caller harus klik `#modal-manage-btn` sebelum nav (lihat FLOW L L1 ambil href dulu, lalu L2 goto ManagePackages langsung — pola valid). [VERIFIED: examTypes.ts:102-103]

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Wizard 4-step navigation | Manual step-click di tiap flow | `createAssessmentViaWizard` | Sudah handle Step1-4, peserta fallback selector, successModal wait [VERIFIED] |
| Package create + extract packageId | Manual form fill + regex href | `createDefaultPackage` | Handle alert-success + packageId regex [VERIFIED] |
| Add question (MC/MA/Essay + image) | Manual QuestionType switch + JS wait | `addQuestionViaForm` | Handle Pitfall-2 applyQTypeSwitch wait + post-submit verify [VERIFIED] |
| Essay answer save (SignalR debounce) | `fill` + `waitForTimeout` | `fillEssayAnswer` | Direct hub `invoke('SaveTextAnswer')` + await (bypass fire-and-forget race) [VERIFIED: examTypes.ts:336-361] |
| Grade essay + finalize | Manual score input + finalize click | `gradeSingleEssaySession` | Handle badge wait + confirm dialog + reload [VERIFIED] |
| Read AssessmentSessions.Score | Custom SQL conn | `db.queryScalar(...)` | localhost-only guard, `-b` exit-code, `-I` quoted-identifier [VERIFIED: dbSnapshot.ts] |
| Cleanup test data | Per-flow manual delete loop | global.teardown `db.restore()` | BACKUP/RESTORE penuh reset DB ke baseline pasca-suite [VERIFIED: global.teardown.ts:64-66] |

**Key insight:** Migrasi ini hampir seluruhnya = "panggil helper yang sudah ada". Risiko utama bukan logika baru, melainkan (a) drift selector/markup pada step yang TIDAK ter-cover helper (token, proton, paste, timer) dan (b) flakiness pada flow time-dependent. Hampir semua building block sudah proven hijau di `exam-types.spec.ts`.

## Per-Flow Migration Map (A–J + K)

Legend: **CREATE** = ganti flat-form `/Admin/CreateAssessment` → `createAssessmentViaWizard`. **QADD** = ganti `/Admin/ManageQuestions?id=` flat → `createDefaultPackage` + `addQuestionViaForm`. **SURVIVE** = step worker/monitoring/cleanup tidak berubah (selector tervalidasi). **EXTEND** = butuh helper extension additive.

| Flow | Deskripsi | Step di-migrasi | Step SURVIVE | Helper extension (D-04) | Catatan / Risiko |
|------|-----------|-----------------|--------------|--------------------------|-------------------|
| **A** | Legacy full lifecycle | A1 CREATE; A2/A3 QADD (3 MC) | A4-A14 (start/answer/results/review/cert/monitoring/reset); A15 cleanup | — | A9 answer-review + A10 cert butuh `allowAnswerReview:true`+`generateCertificate:true` di opts. A6 positional answer fragile (lihat Pattern 3). |
| **B** | Token-protected exam | B1 CREATE + token | B2 (badge "Token Required" + `.btn-start-token`), B3 (token modal), B4 (monitoring token), B5 cleanup | **token**: `isTokenRequired`+`accessToken` ke `CreateAssessmentOpts` | DRIFT: spec lama pakai `#tokenInputContainer`+`:has-text("Generate")`; markup current `#tokenSection`+`onclick=generateToken()`. Token 6-char alfanumerik. B punya NO question (tidak QADD). |
| **C** | Force Close & Close Early | C1 CREATE (2 worker); C2 QADD (2 MC) | C3 (worker start), C4 ForceClose, C5 CloseEarly modal, C6 ForceCloseAll, C7 cleanup | — | 2 peserta: `participantEmails: ['rino.prasetyo@...','iwan3@...']`. |
| **D** | Package-based + reshuffle + paste-import | D1 CREATE; D3 paste-import | D2 (sudah package-aware: createPackage), D4 reshuffle, D5 worker start, D6 answer+submit, D7 cleanup | **paste-import** helper baru (opsional) untuk `textarea[name="pasteText"]` | D2 sudah pakai `input[name="packageName"]`+Create Package (sama dgn `createDefaultPackage`). D3 paste 6-kolom TSV (Q\toptA..D\tcorrect). Bisa pakai `createDefaultPackage` + helper paste, atau panggil flat paste-import (selector masih ada). |
| **E** | Proton T3 Interview (offline) | E1 CREATE Proton T3 | E2 (badge "Interview Dijadwalkan" no-Start), E3 (`form[action*="SubmitInterviewResults"]`), E4 cleanup | **proton**: `protonTrackId`/select-by-Tahun ke opts (Step 1 `#protonTrackSelect`) | **D-02 double-drift.** Create Proton ada di STEP 1 (Category='Assessment Proton' → `#protonFieldsSection` show → pilih track Tahun 3 via `data-tahun`). Controller set `TahunKe="Tahun 3"`. E3 form fields `judges`/`aspect_*`(5)/`notes`/`isPassed`+antiforgery COCOK dgn spec lama. RISIKO: butuh ProtonTrack Tahun 3 ter-seed di DB lokal (spec lama punya `test.skip` jika tak ada). Lihat Open Q1. |
| **F** | Multiple workers same assessment | F1 CREATE (2 worker); F2 QADD (1 MC) | F3 (coachee), F4 (coachee2), F5 monitoring, F6 cleanup | — | F4 pakai `login(page,'coachee2')`. |
| **G** | Timer expired (1-min) | G1 CREATE (durationMinutes:1) + QADD (1 MC) | G3 cleanup | **duration** sudah param `durationMinutes` (no extension); **deterministik** timer-expiry | DRIFT DETERMINISTIK (D-03): G2 lama `waitForTimeout(70_000)`. Ganti: assert via DB-state (Status auto-flip) atau JS `window.timerStartRemaining`/expired-modal. Lihat § Deterministic Time-Dependent Flows. |
| **H** | Real-time monitoring | H1 CREATE + QADD (1 MC) | H2-H4 (count cards, badge, polling), H5 submit, H6 completed, H7 `GetMonitoringProgress` JSON, H8 cleanup | — | H6/H7 sudah pakai polling endpoint + count assert (cukup deterministik). H6 ada `waitForTimeout(12_000)` untuk poll-hide → bisa diganti assert state, tapi minimal-change OK. Lihat § Deterministic. |
| **I** | Edit assessment | I1 CREATE | I2 (Edit form `#Title`/`#DurationMinutes`/`#PassPercentage`), I3 edit, I4 verify persist, I5 cleanup | — | EditAssessment.cshtml masih flat (`#Title` dll) — I2-I4 SURVIVE (verifikasi cepat selector edit page). |
| **J** | Abandon & Reset recovery | J1 CREATE + QADD (1 MC) | J2 start, J3 abandon, J4 cannot-restart, J5 monitoring Abandoned, J6 reset, J7 retake, J8 cleanup | — | J3 abandon via `#abandonForm`/Keluar btn — downstream, SURVIVE. |
| **K (BARU)** | Essay full cycle + Score aggregation (GRADE-01) | K1 CREATE essay; K2 createDefaultPackage; K3 addQuestion Essay | (semua baru, port FLOW L) | — (semua helper sudah ada) | **D-01.** Port `exam-types.spec.ts` FLOW L verbatim: worker `fillEssayAnswer`+`submitExamTwoStep` → HC `gradeSingleEssaySession(score:80)` → **assert `db.queryScalar('SELECT ISNULL(Score,-1) ... Id={sessionId}')` === 80** + Status='Completed'. Net regression GRADE-01. |

**Total `.fixme` to remove:** 10 (A,B,C,D,E,F,G,H,I,J — line 32,316,403,551,708,840,972,1068,1304,1419). [VERIFIED: grep exam-taking.spec.ts]

## Helper Extension Gaps (D-04 Additive — JANGAN ubah signature existing)

Proposal bentuk additive (planner finalize). Semua field OPTIONAL → flow existing tetap kompatibel.

### 1. Token (Flow B) — extend `CreateAssessmentOpts`
```typescript
// examTypes.ts:23 CreateAssessmentOpts — TAMBAH field optional (tidak ubah yang ada):
export interface CreateAssessmentOpts {
  // ... existing fields verbatim ...
  isTokenRequired?: boolean;   // default false
  accessToken?: string;        // 6-char alfanumerik; jika kosong+isTokenRequired → klik Generate
}
// Di createAssessmentViaWizard STEP 3 (setelah passPercentage), tambah blok kondisional:
//   if (opts.isTokenRequired) {
//     await page.locator(wizardSelectors.isTokenRequired).check();
//     await page.locator('#tokenSection').waitFor({ state: 'visible' });   // markup current
//     if (opts.accessToken) await page.fill(wizardSelectors.accessToken, opts.accessToken);
//     else await page.locator('button:has-text("Generate"), button[onclick*="generateToken"]').click();
//   }
```
> Selector tersedia: `wizardSelectors.isTokenRequired='#IsTokenRequired'`, `wizardSelectors.accessToken='#AccessToken'` (sudah ada L67-68). `#tokenSection` belum di wizardSelectors → tambah additive. [VERIFIED: CreateAssessment 506-518, JS toggle 1367-1385]

### 2. Proton T3 (Flow E) — extend `CreateAssessmentOpts` (Step 1)
```typescript
  protonTrackId?: number;       // value option #protonTrackSelect
  protonTrackTahun?: 'Tahun 1'|'Tahun 2'|'Tahun 3';  // pilih opsi by data-tahun (lebih robust)
// Di STEP 1 (setelah category select), bila category === 'Assessment Proton':
//   await page.locator('#protonFieldsSection').waitFor({ state: 'visible' });
//   pilih option #protonTrackSelect by data-tahun==='Tahun 3' (loop options, cek attr)
//   ATAU selectOption by value protonTrackId.
// Tahun 3 → controller set TahunKe='Tahun 3' + DurationMinutes boleh 0 (offline interview).
```
> Selector ada: `#protonTrackSelect` name=`ProtonTrackId`, opsi punya `data-tahun="@track.TahunKe"`. Section `#protonFieldsSection` (d-none → show via JS saat Category='Assessment Proton'). [VERIFIED: CreateAssessment 210-235, 1322-1356; controller 1168-1191,1460-1463]

### 3. Paste-import (Flow D3) — helper baru kecil
```typescript
// helper baru (additive), TIDAK ubah addQuestionViaForm:
export async function importQuestionsViaPaste(page: Page, packageId: number, tsvRows: string): Promise<void> {
  // navigate ke Import Questions untuk package, fill textarea[name="pasteText"], klik Import, verify success
}
```
> `textarea[name="pasteText"]` + tombol Import + 6-kolom TSV masih dipakai di D3 lama. Verifikasi route Import current (link `a:has-text("Import Questions")` dari ManagePackages). [VERIFIED: exam-taking.spec.ts:599-621]

### 4. Duration 1-menit (Flow G) — TANPA extension
`durationMinutes` SUDAH param `CreateAssessmentOpts` (number). Cukup `durationMinutes: 1`. Yang perlu = deterministik timer-expiry (lihat bawah), bukan helper baru.

## Deterministic Time-Dependent Flows (D-03)

### Flow G (timer 1-menit)
**Masalah:** G2 lama `await page.waitForTimeout(70_000)` (70 detik wall-clock) — lambat + flaky.
**Rekomendasi (pilih satu, minimal-change):**
- **Opsi A (DB-state, paling deterministik):** Worker start (status→InProgress, `StartedAt` set). Lalu BUKAN tunggu 70s; assert via DB bahwa timer-expiry logic dapat dipicu — namun ini memerlukan elapsed nyata. Karena `DurationMinutes=1`, alternatif: gunakan seed fixture pola Phase 313 (`StartedAt=NOW-X menit`) untuk timer expired tanpa wait. TAPI itu menambah seed SQL (scope creep).
- **Opsi B (state assertion + short bounded wait, direkomendasikan untuk minimal-change):** Setelah start, assert `#examTimer` visible + `window.timerStartRemaining` ≈ 60. Untuk expiry, kurangi durasi efektif: tunggu bounded (mis. `waitForFunction` polling `timerStartRemaining <= 0` ATAU `#examExpiredModal` visible ATAU URL berubah ke Results, timeout 75s). Ini tetap event-driven (resolve segera saat expired), bukan fixed sleep — hilangkan `waitForTimeout(70_000)` buta. Assert outcome: expired-modal ATAU auto-submit ke Results ATAU status DB 'Completed'/'Abandoned'.
- **Opsi C (paling cepat, butuh konfirmasi planner):** Reuse pola fixture seed Phase 313 (timer fixtures `StartedAt` di masa lalu) untuk simulasi expired instan. Trade-off: menambah seed → diskusikan; D-03 prioritaskan reliabilitas + bukti run, kecepatan sekunder.

**Default usul:** Opsi B (event-driven `waitForFunction`, bounded 75s, assert state) — paling kecil blast-radius, hilangkan flakiness sleep-buta, tanpa seed baru. [ASSUMED: perlu konfirmasi expired-modal id `#examExpiredModal` masih ada — verify saat Wave 0]

### Flow H (real-time monitoring)
**Status:** Sebagian besar SUDAH deterministik — H4/H6 assert `#count-completed`/`#count-inprogress` counts + H7 `GetMonitoringProgress` JSON. Ada 2 `waitForTimeout` (H4 2s, H6 2s+12s) untuk poll-cycle.
**Rekomendasi:** Pertahankan struktur; ganti `waitForTimeout(12_000)` (H6 poll-hide) dengan `expect(...).toHaveText(...)`/`waitForFunction` pada count cards (poll auto-refresh akan update DOM → assertion retry sampai match dalam expect timeout). Untuk H6 "Submit Assessment hidden", assert via `expect(closeBtn).toBeHidden()` (auto-retry) alih-alih sleep. Minimal change, hilangkan sleep-buta terbesar. JSON endpoint H7 sudah deterministik (langsung fetch). [VERIFIED: exam-taking.spec.ts:1147-1278]

## Package Cleanup (Diskresi D-04)

**Temuan:** global.teardown.ts melakukan `db.restore(snapshot)` penuh pasca-suite → seluruh data test (assessment+package+question+session) ter-reset ke baseline. **Integritas DB lintas-run TIDAK bergantung pada cleanup per-flow.** [VERIFIED: global.teardown.ts:64-95]

**Untuk DeleteAssessment cascade (Phase 353 atomic):** Memory project mencatat DeleteAssessment = atomic cascade yang menghapus child (Phase 353 + 366 cascade image cleanup). Flow cleanup lama (A15,B5,...) pakai "Hapus Grup" dari ManageAssessment dropdown. Karena teardown restore penuh, cleanup per-flow hanya untuk **isolasi antar-flow dalam run yang sama** (mencegah title-collision saat search). Karena tiap flow pakai `uniqueTitle()` (timestamp), collision minim.

**Rekomendasi:** Pertahankan step cleanup existing (SURVIVE) — tidak perlu menambah package-delete terpisah; jika DeleteAssessment Phase 353 cascade menghapus package, cleanup cukup. Verifikasi cepat saat Wave 0 (cek tak ada orphan package error). Bila planner mau hemat, cleanup step boleh disederhanakan tapi BUKAN prioritas (teardown sudah jadi safety net). [ASSUMED: Phase 353 DeleteAssessment cascade hapus package — perlu grep konfirmasi saat plan; memory mendukung]

## Code Examples (verified, dari source)

### Flow K (BARU) — blueprint port dari FLOW L
```typescript
// Source: tests/e2e/exam-types.spec.ts:305-427 (FLOW L) — VERIFIED, sudah un-fixme oleh Phase 376
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm,
         fillEssayAnswer, submitExamTwoStep, gradeSingleEssaySession } from './helpers/examTypes';
import * as db from '../helpers/dbSnapshot';

test.describe('Flow K: Essay Full Cycle + Score Aggregation (GRADE-01)', () => {
  let title: string, category = 'IHT', scheduleDate: string, assessmentId: number, packageId: number, sessionId: number;
  const Q_MARKER = '[379-K] Essay GRADE-01 regression';

  test('K1 — HC creates Essay assessment via wizard', async ({ page }) => {
    title = uniqueTitle('Pre Test [379-K] Essay'); scheduleDate = today();
    await login(page, 'hc');
    await createAssessmentViaWizard(page, { title, category, scheduleDate, scheduleTime: '00:01',
      durationMinutes: 60, passPercentage: 70, allowAnswerReview: true,
      participantEmails: ['rino.prasetyo@pertamina.com'] });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
  });
  // K2 createDefaultPackage; K3 addQuestionViaForm({type:'Essay', score:100, rubrik, maxCharacters})
  // K4 worker: start → fillEssayAnswer(page, qCard, '...') → submitExamTwoStep(page); capture sessionId
  // K5 HC: gradeSingleEssaySession(page, {title, category, scheduleDate, sessionId, score: 80})
  test('K6 — DB assert Score aggregated (GRADE-01: NOT 0)', async () => {
    const score = await db.queryScalar(`SELECT ISNULL(Score, -1) FROM AssessmentSessions WHERE Id = ${sessionId}`);
    expect(score).toBe(80);   // bukan 0 → fix 376 terbukti e2e
    const completed = await db.queryScalar(`SELECT COUNT(*) FROM AssessmentSessions WHERE Id = ${sessionId} AND Status = 'Completed'`);
    expect(completed).toBe(1);
  });
});
```

### Flow E — Proton interview submit (E3, field verified)
```typescript
// Source: Views/Admin/AssessmentMonitoringDetail.cshtml:657-733 + Controller:3669-3705 — VERIFIED
const form = page.locator('form[action*="SubmitInterviewResults"]').first();
await form.locator('input[name="judges"]').fill('Dr. Andi, Ir. Budi');
const aspects = form.locator('select[name^="aspect_"]');           // 5 aspek
for (let i = 0; i < await aspects.count(); i++) await aspects.nth(i).selectOption('4');
await form.locator('textarea[name="notes"]').fill('E2E — kompetensi baik.');
await form.locator('input[name="isPassed"]').check();
await form.locator('button[type="submit"]').click();
await page.waitForLoadState('networkidle');
await expect(page.locator('body')).toContainText(/Lulus|berhasil disimpan/i);
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `/Admin/CreateAssessment` flat-form 1-halaman | Wizard 4-step (`#step-1..4`, `#btnNext1..3`, `#btnSubmit`) | Phase 317/319 (v17) | Semua create di exam-taking usang → `createAssessmentViaWizard` |
| Questions langsung ke assessment (`/Admin/ManageQuestions?id=`) | PACKAGE wajib: `/Admin/ManagePackages` → `/Admin/ManagePackageQuestions?packageId=` | Package era | `createDefaultPackage` + `addQuestionViaForm` |
| Token `#tokenInputContainer` + `:has-text("Generate")` | `#tokenSection` + `onclick="generateToken()"` | refactor wizard | Selector token drift (Flow B) |
| Essay score via UI badge | DB-scalar `AssessmentSessions.Score` (SURF-317-A) | Phase 317 + fix 376 | Flow K assert DB |
| Timer test `waitForTimeout(70_000)` | event-driven `waitForFunction`/DB-state | (this phase) | Flow G deterministik (D-03) |

**Deprecated/outdated di spec lama:**
- `#tokenInputContainer` (B1) → ganti `#tokenSection`.
- `input[name="options"]`/`input[name="correct_option_index"]`/"Tambah Soal" (A3/C2/F2/...) → `addQuestionViaForm`.
- `#submitBtn` flat (semua create) → wizard `#btnSubmit`.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | ProtonTrack Tahun 3 ter-seed di DB lokal HcPortalDB_Dev (prereq Flow E create + interview form) | Flow E map / Open Q1 | Flow E create gagal pilih track → tidak hijau (D-02 minta E penuh hijau). Mitigasi: cek `SELECT COUNT(*) FROM ProtonTracks WHERE TahunKe='Tahun 3'`; jika 0, seed minimal (klasifikasi per SEED_WORKFLOW) atau gunakan track existing. |
| A2 | DeleteAssessment (Phase 353 atomic) cascade menghapus child package+question → cleanup per-flow cukup tanpa package-delete terpisah | Package Cleanup | Bila tidak cascade, orphan package menumpuk dalam run (tapi teardown restore tetap reset). Risiko rendah. Verify grep saat plan. |
| A3 | `#examExpiredModal` masih ada di StartExam.cshtml untuk assert Flow G expiry | Deterministic G (Opsi B) | Assertion expiry perlu fallback ke DB-state/URL-Results. Verify Wave 0. |
| A4 | Field interview `judges`/`aspect_*`/`notes`/`isPassed` cocok 100% (controller 3669-3705) — TIDAK ter-drift Phase 358-363 | Flow E / Code Examples | LOW — sudah diverifikasi controller+view match aspek list identik. |
| A5 | Worker-side selector CMP (`.btn-start-standard`, `[id^="qcard_"]`, `#reviewSubmitBtn`, Results) tetap valid (SURVIVE) | Per-flow map | LOW — dipakai aktif & hijau di exam-types/shuffle/313-flows. |

## Open Questions

1. **ProtonTrack Tahun 3 availability di DB lokal (Flow E).**
   - What we know: create Proton butuh opsi di `#protonTrackSelect` (dari `ViewBag.ProtonTracks`); spec lama punya `test.skip(true,'No Tahun 3 ProtonTrack available')`. D-02 melarang skip — E harus hijau penuh.
   - What's unclear: apakah DB lokal saat ini punya ProtonTrack TahunKe='Tahun 3' aktif.
   - Recommendation: Wave 0 cek `db.queryScalar("SELECT COUNT(*) FROM ProtonTracks WHERE TahunKe='Tahun 3'")`. Jika ≥1, pilih track itu (by `data-tahun`). Jika 0, planner putuskan: seed minimal (temporary, SEED_WORKFLOW + journal) atau pakai SeedProtonTracksAsync existing (idempotent, lihat memory). Karena teardown restore penuh, seed temporary aman.

2. **Paste-import vs addQuestionViaForm untuk Flow D3.**
   - What we know: D3 spec lama paste 6-kolom TSV via `textarea[name="pasteText"]`. Helper `addQuestionViaForm` per-question, bukan batch-paste.
   - What's unclear: apakah paste-import route masih identik post-package-refactor.
   - Recommendation: Pertahankan paste-import (buat helper kecil `importQuestionsViaPaste`) untuk PRESERVE coverage paste-import (itu nilai unik Flow D). Jika route paste drift, fallback ke `addQuestionViaForm` ×3. Verify saat plan.

3. **Apakah migrasi mengungkap bug produksi?**
   - Per CONTEXT: jika migrasi reveal bug prod (mis. token/proton/timer behavior), SURFACE ke backlog, JANGAN fix inline (scope test-infra only). Catat di SUMMARY + STATE.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| App lokal (`dotnet run`) di `http://localhost:5277` | semua flow (baseURL) | wajib running | — | tidak ada — harus start; AD=false (`Authentication__UseActiveDirectory=false`) |
| SQL Server `localhost\SQLEXPRESS` + `HcPortalDB_Dev` | DB assert + snapshot/restore | wajib | SQLEXPRESS lokal | tidak ada |
| SQLBrowser service | login (NTLM loopback fix) | wajib start | — | `lpc:` shared-memory conn override |
| `sqlcmd` | dbSnapshot (queryScalar/backup/restore) | wajib (sudah dipakai harness) | — | tidak ada |
| Playwright chromium | runner | terpasang | — | — |
| ProtonTrack Tahun 3 (data) | Flow E | UNKNOWN (Open Q1) | — | seed minimal / SeedProtonTracksAsync |

**Missing dependencies with no fallback:** App lokal + SQL + SQLBrowser harus aktif sebelum run (mandat D-03). Jalankan: `Authentication__UseActiveDirectory=false dotnet run` (lokal, AD off), start SQLBrowser, gunakan `lpc:` jika login 500 error 53.

**Missing dependencies with fallback:** ProtonTrack Tahun 3 — seed jika tidak ada (Open Q1).

## Validation Architecture

> nyquist_validation = true (config.json). Section ini WAJIB. Untuk fase ini, **the test IS the deliverable** — validasi = suite berjalan HIJAU. SC2 (suite green `--workers=1`) adalah gate primer.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright `@playwright/test` (config `tests/playwright.config.ts`, projects: setup→chromium) |
| Config file | `tests/playwright.config.ts` (globalTeardown = `e2e/global.teardown.ts`; baseURL `http://localhost:5277`; `fullyParallel:false`, `retries:0`) |
| Quick run command | `npx playwright test tests/e2e/exam-taking.spec.ts -g "Flow K" --workers=1` (1 flow saat dev) |
| Full suite command | `npx playwright test tests/e2e/exam-taking.spec.ts --workers=1` |
| DB assert | `tests/helpers/dbSnapshot.ts` `queryScalar` (localhost-only guard) |
| Data lifecycle | global.setup BACKUP pre-suite → global.teardown RESTORE post-suite (full reset) |

### Phase Requirements → Test Map (SC1/SC2/SC3)
| SC | Behavior | Test Type | Automated Command | File Exists? |
|----|----------|-----------|-------------------|-------------|
| SC1 | 10 flow A-J pakai wizard; 10 `.fixme` dihapus | structural + run | `grep -c "test.fixme" tests/e2e/exam-taking.spec.ts` → 0; `npx playwright test ... --workers=1` (semua A-J run, bukan skip) | ✅ exam-taking.spec.ts (rewrite) |
| SC2 | Suite hijau `--workers=1` | e2e full | `npx playwright test tests/e2e/exam-taking.spec.ts --workers=1` (all pass) | ✅ |
| SC3 | Flow essay ter-cover (GRADE-01) | e2e + DB | Flow K K6 assert `Score===80` (bukan 0) + Status='Completed' via `db.queryScalar` | ❌ Flow K BARU (Wave 0/plan) |
| E2E-01 | (= SC1+SC2+SC3) | e2e | full green run, bukti dilampirkan | ✅/❌ |

### Sampling Rate
- **Per task/commit (dev):** jalankan flow yang baru disentuh `-g "Flow X" --workers=1` (cepat, isolasi).
- **Per wave merge:** jalankan beberapa flow terkait `--workers=1`.
- **Phase gate (D-03):** FULL `npx playwright test tests/e2e/exam-taking.spec.ts --workers=1` HIJAU; lampirkan output run sebagai bukti (DoD).

### Wave 0 Gaps
- [ ] Konfirmasi app lokal running (`localhost:5277`, AD=false) + SQL + SQLBrowser sebelum run apa pun.
- [ ] Verify ProtonTrack Tahun 3 ada (`SELECT COUNT(*) FROM ProtonTracks WHERE TahunKe='Tahun 3'`) — Open Q1; seed bila 0.
- [ ] Verify `#tokenSection` (bukan `#tokenInputContainer`) + tombol Generate selector (Flow B).
- [ ] Verify `#examExpiredModal` / outcome timer-expiry (Flow G Opsi B) — A3.
- [ ] Verify paste-import route current (Flow D3) — Open Q2.
- [ ] Smoke 1 flow sederhana (mis. A atau K) end-to-end HIJAU sebagai bukti tooling + env sebelum migrasi massal.

*(Tidak ada framework install gap — Playwright + helper sudah lengkap.)*

## Security Domain

> security_enforcement absent di config → default enabled. Fase ini = TEST-INFRA only, NOL perubahan kode produksi (controller/view/service). Permukaan serangan baru: tidak ada.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V5 Input Validation | no (test data only) | — (test pakai uniqueTitle + literal strings) |
| V6 Cryptography | no | — |
| Test-infra safety | yes | `dbSnapshot.runSqlcmd` REJECT non-localhost (`Refusing to target non-localhost SQL Server`) [VERIFIED: dbSnapshot.ts:39-44]; CLAUDE.md jangan sentuh DB Dev/Prod |
| Antiforgery (Flow E) | yes (test exercise) | Form SubmitInterviewResults pakai `@Html.AntiForgeryToken()` — test submit via real form (token auto-included) [VERIFIED: AssessmentMonitoringDetail.cshtml:658] |

### Known Threat Patterns (test-infra context)
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Test menyentuh DB non-localhost (Dev/Prod) | Tampering | `dbSnapshot` localhost guard + CLAUDE.md compliance; snapshot/restore lokal saja |
| Seed temporary nempel pasca-run | Tampering | global.teardown RESTORE penuh + SEED_WORKFLOW journal (jika seed ProtonTrack) |
| Migrasi diam-diam ubah kode produksi | Tampering/Scope | CONTEXT scope = test-infra only; bug prod → backlog, bukan fix inline |

## Sources

### Primary (HIGH confidence — read langsung sesi ini)
- `tests/e2e/exam-taking.spec.ts` (1737 baris) — target migrasi, 10 flow A-J + 313 flows
- `tests/e2e/helpers/examTypes.ts` — helper kanonik (createAssessmentViaWizard L51, createDefaultPackage L121, addQuestionViaForm L168, fillEssayAnswer L320, gradeSingleEssaySession L386, dst)
- `tests/e2e/helpers/wizardSelectors.ts` — wizardSelectors/questionFormSelectors/prePostWizardSelectors (token L67-68)
- `tests/e2e/exam-types.spec.ts` — FLOW L (305-428, blueprint Flow K), FLOW O (multi-context), import pattern
- `tests/helpers/dbSnapshot.ts` — queryScalar/backup/restore/localhost guard
- `tests/helpers/auth.ts`, `accounts.ts`, `utils.ts` — login/akun/util
- `tests/e2e/global.setup.ts` + `global.teardown.ts` — snapshot/restore lifecycle
- `tests/playwright.config.ts` — config (setup→chromium, globalTeardown, baseURL)
- `Views/Admin/CreateAssessment.cshtml` — wizard markup (proton 210-235, token 500-518, JS toggle 1322-1385)
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — interview form (26-31, 619-738)
- `Views/CMP/Assessment.cshtml` — worker badge Proton T3 (417-446)
- `Controllers/AssessmentAdminController.cs` — SubmitInterviewResults (3669-3784), proton create (1165-1463)
- `.planning/phases/376-*/376-01-SUMMARY.md` + `376-03-SUMMARY.md` — fix 376 (AssessmentScoreAggregator + RecomputeEssayScores)

### Secondary (project context)
- `.planning/REQUIREMENTS.md` (E2E-01), `.planning/ROADMAP.md` (Phase 379 SC1-3), `.planning/STATE.md`, `.planning/config.json`
- `CLAUDE.md` (Develop Workflow, Seed Workflow)

### Tertiary (LOW confidence — flagged)
- ProtonTrack Tahun 3 availability di DB lokal — UNKNOWN, verify Wave 0 (Open Q1).

## Metadata

**Confidence breakdown:**
- Standard stack / helpers: HIGH — semua helper dibaca langsung, proven hijau di exam-types/shuffle.
- Per-flow migration map: HIGH — tiap flow dibaca penuh dari source; step create/QADD/SURVIVE teridentifikasi.
- Flow K blueprint: HIGH — FLOW L exam-types persis pola, sudah un-fixme oleh 376.
- Flow E (Proton) drift: MEDIUM-HIGH — markup+controller diverifikasi cocok; risiko data (Tahun 3 seed) MEDIUM (Open Q1).
- Deterministic G/H: MEDIUM — rekomendasi event-driven solid, tapi `#examExpiredModal` outcome perlu Wave 0 confirm (A3).
- Token/paste drift: MEDIUM-HIGH — token markup diverifikasi (`#tokenSection`); paste route perlu verify (Open Q2).

**Research date:** 2026-06-14
**Valid until:** ~2026-07-14 (stabil; codebase internal, tak ada dependency eksternal fast-moving)
