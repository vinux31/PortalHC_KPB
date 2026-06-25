# Project Retrospective: Portal HC KPB

*A living document updated after each milestone. Lessons feed forward into future planning.*

---

## Milestone: v32.9 — EditQuestion Option-Edit Data Integrity (Identity-Based)

**Shipped:** 2026-06-25 (local, NOT pushed) | **Phases:** 1 (420) | **Plans:** 3 | **migration:** FALSE

### What Was Built
Mengganti mekanisme upsert opsi jawaban di `EditQuestion` POST dari **posisional** (`existing[i]` OrderBy Id) menjadi **identity-based** (match `PackageOption` by stable `Id`). Carrier `OptionInput.Id` (hidden per baris form) + anti-tamper fail-closed pre-mutation + set-difference guard `existingIds.Except(keptIds)` + UPDATE/REMOVE/ADD by Id. Menutup backlog 999.15: hapus opsi tengah pada soal terjawab tak lagi me-relabel jawaban peserta secara senyap; guard answered-option menyala untuk delete posisi MANAPUN; regression-lock 999.14 (FK-Restrict 500) tetap.

### What Worked
- **Bug-driven milestone, scope-tight.** Sumber = 1 backlog item terverifikasi reproduces-on-main (7-agen verify workflow), bukan spec spekulatif. 1 fase / 3 plan / 6 REQ — tidak over-scoped.
- **Integration-checker pada milestone 1-fase difokuskan ke DoD nyata** (integritas lintas-surface), bukan wiring antar-fase yang degenerate. Membuktikan semua surface scoring/display JOIN by `PackageOptionId` (bukan posisi) → fix UPDATE-by-Id otomatis aman.
- **Locked-context dihormati:** in-code note `AAC:8027-8035` (upsert posisional di-LOCK D-418-02) → fix ubah MEKANISME, bukan perketat threshold guard. Discuss-phase menangkap ini sebelum plan.
- **Playwright real-browser UAT menutup gap yang controller-test tak bisa jangkau** (lesson 354): hidden-Id survive JS reletter, clone-reset gotcha §2c. 3/3 PASS DB-verified.

### What Was Inefficient
- **VERIFICATION.md ditulis sebelum UAT** → status `human_needed` (5/6) padahal kerja selesai; harus di-resolve manual saat close (UAT 3/3 sudah ada). Verifier idealnya re-run setelah Plan 03 UAT, atau status verifikasi nunggu UAT.
- **audit-open false-positive** "UAT gap" untuk fase yang sudah passed/0-pending — noise saat close gate.

### Patterns Established
- **Identity-based upsert carrier pattern:** hidden `Id` per baris authoring form + anti-tamper server-side (`submittedId ∈ existingIds`, reject duplikat) + set-difference guard SEBELUM mutasi + clone-reset hapus Id (cegah baris baru warisi Id). Template untuk authoring form lain yang punya child-record editable.
- **Code-reviewer CR menangkap test-yang-lolos-secara-accidental:** CR-01 = test null-Id yang lolos via Count/text tapi diam-diam mass-recreate. Tambah Id-stability assert mengunci UPDATE-in-place. Test hijau ≠ kontrak terkunci.

### Key Lessons
- Saat mengubah mekanisme penyimpanan yang di-LOCK spec, ubah MEKANISME-nya (identity match), jangan tambal gejala (perketat guard threshold) — gejala bocor di permukaan lain (delete tengah vs ekor).
- `PackageUserResponse` tanpa text-snapshot aman SELAMA edit UPDATE-by-Id (bukan relabel). Audit lintas-surface wajib saat menyentuh authoring opsi.

### Cost Observations
- Single phase, full gate-set (discuss→plan→execute→review+fix→secure→validate→UAT). Suite 702/702, 3 plan commits + docs. Efficient: bug terverifikasi dulu (no wasted exploration).

---

## Milestone: v32.6 — Section + Scoped Shuffle + Pagination + Opsi Dinamis

**Shipped:** 2026-06-24 (local, NOT pushed) | **Phases:** 6 (415-419 + 415.1) | **Plans:** 20 | **Tasks:** 41

### What Was Built
Soal di-kelompokkan ke **Section** per-paket (entity `AssessmentPackageSection` + `PackageQuestion.SectionId` nullable, migration Phase 415). Di atasnya: **scoped shuffle** (acak hanya DALAM section, kunci komposit `(SectionNumber, ET)`), **pagination per-section** (`StartNewPage` + auto-split per-10 + resume), **opsi jawaban dinamis 2–6** (A–F, ganti kunci A–D), **import Excel dual-format** (13-kolom universal + kompatibel-mundur 9-kolom), dan **export label Section** (Excel band-header + PDF heading). 4 helper pure single-source: `SectionStructureComparer`, `ShuffleEngine.BuildSectionQuestionAssignment`, `SectionPaginator`, `SectionExportLayout`. +415.1 hotfix off-theme (guard essay cross-package). 100% kompatibel-mundur (no-Section = perilaku legacy byte-identik).

### What Worked
- **Pure-helper single-source kill-drift** — 4 seam Section dipakai konsisten lintas authoring→exam→export→import; integration-checker konfirmasi 0 divergent copy. Two-sentinel discipline (Comparer `int.MinValue` vs ExportLayout `int.MaxValue`) eksplisit + terdokumentasi.
- **Pitfall-1 (.Include Section) sebagai checklist** — setiap surface section-aware wajib eager-load; tertangkap di review + integration sweep, dibuktikan live (export band-header @5277).
- **De-tautology test** — `RepeatedEtInSibling_Fires` (ET-warning) + per-peserta heading test gagal di logika lama, lulus di baru — menangkap false-negative nyata.
- **Cross-milestone e2e** — 4 spec (lifecycle/inject/linkprepost/add-remove × Section) membuktikan fitur lama (v32.2/397/v32.5) tetap koheren dengan Section.

### What Was Inefficient
- **Gate-doc debt terkonsentrasi di fase paling berisiko** — 415 (keystone) + 415.1 (auth-relaksasi) justru yang TANPA SECURITY.md sampai pre-close audit menangkapnya. Mitigasi substansial sudah ada+verified; hanya artefak gate yang telat. Pelajaran: jalankan secure-phase SEGERA setelah fase berisiko-tinggi, jangan tunda ke akhir.
- **VALIDATION.md ditinggal `draft`** (415.1, 419) walau test hijau — perlu validate-phase eksplisit untuk finalize. Pelajaran: finalize nyquist gate saat fase selesai, bukan numpuk.
- **419 phase-complete tak flip PAG-04 checkbox** — REQUIREMENTS PAG-04 tetap `[ ]`/Pending, tertangkap re-audit. Tool gap (gsd-tools phase complete lewatkan checkbox tertentu).
- **exceljs SAX-load rapuh di Playwright runner** — pivot ke JSZip sharedStrings scan untuk assert band-header.

### Patterns Established
- **Re-audit pra-close multi-angle** (req-coverage + gate-completeness + integration + ship-readiness) sebelum complete-milestone — menangkap PAG-04 checkbox + 2 SECURITY gap + nyquist draft yang phase-level verify lewatkan.
- **Shuffle-robust pagination assert** — saat ShuffleEnabled, jangan asumsi posisi qids[0]; assert blok (semua Section-B page > semua Section-A page).
- **Hard-delete vs soft-remove di e2e** — peserta Not-started/no-data = hard-delete (Mode='hard', tak ke #tbodyRemoved); assert sesuai state, bukan asumsi soft.

### Key Lessons
- Peserta Not-started + no-response → `RemoveParticipantCoreAsync` hard-delete (Mode='hard'); soft-remove+Restore khusus peserta berdata.
- Baris JS-injected (SignalR/fallback) bisa belum ter-wire Bootstrap penuh → reload sebelum aksi dropdown di e2e.
- Re-login `page` yang sudah-auth → `/Account/Login` redirect Home → input hilang → timeout. Cek auth state dulu.
- `human_needed` VERIFICATION = konvensi UAT-item GSD, bukan gap — verifikasi via UAT.md + ROADMAP complete.

### Cost Observations
- Heavy multi-agent: code-review (10 angle) + 3 verify-workflow + 4 e2e blueprint + audit (4 angle) + 2 secure + integration-checker. Workflow fan-out untuk review/audit, single-writer untuk edit coupled.
- Live UAT @5277 (curl + Playwright) menangkap yang unit tak bisa (controller Include, render runtime).

---

## Milestone: v32.5 — Flexible Add/Remove Participant

**Shipped:** 2026-06-22 (local, NOT pushed) | **Phases:** 6 (409-414) | **Plans:** 13 | **Tasks:** 27

### What Was Built
Add/remove/restore peserta assessment **live** dari Monitoring Detail (AJAX+SignalR), kapan saja (batch belum-progres maupun InProgress). Hapus **hybrid by-state**: belum-mulai→hard-delete cascade; ada-data→soft-remove+arsip (reversibel, sertifikat aman) via 3 kolom `RemovedAt/RemovedBy/RemovalReason` (migration Phase 409). Guard re-entry server-authoritative (StartExam/SubmitExam/Hub) + exclude-removed dari count aktif + force-kick worker via SignalR `examRemoved` + panel "Peserta Dikeluarkan" + Restore. **+Phase 414 off-theme**: decouple gate "Tinjauan Jawaban" admin/HC dari `AllowAnswerReview` (pure helper `CanReviewAnswers`).

### What Worked
- **Pola pure-static-helper testable** (`IsParticipantRemoved`, `CanReviewAnswers`) — keputusan authorize/gate di-unit-test tanpa DB (hindari replica tautologis 999.12). Konsisten + cepat.
- **Milestone-autopilot** menjalankan 414 end-to-end (discuss→plan→execute→review→secure→validate→Playwright UAT→commit) dalam satu invoke; gate berurutan menangkap masalah lebih awal.
- **Playwright UAT 2-persona via kepemilikan sesi** (1 login admin menguji owner & non-owner) — efisien, tak perlu kredensial worker.

### What Was Inefficient
- **Phase 412 `monFlashRow`/`flashRow` bug** lolos runtime-smoke 412 (cek render simbol), baru ketangkap Playwright e2e 413 — handler-attach mati di browser. Re-confirm lesson 354: runtime-smoke ≠ real-browser UAT untuk Razor/JS/SignalR.
- **Audit doc angka stale** (605→609) + heading dobel — perlu adversarial re-check untuk nangkep.

### Patterns Established
- **Owner-vs-non-owner gate** via single var computed-once post-auth, used-twice (gate-build + VM-flag) — anti-desync (pelajaran dari monFlashRow milestone yang sama).
- **Off-theme bugfix bundling**: Phase desimal/lanjutan off-theme (414) di-track terpisah, 0 REQ, audit-milestone re-run konfirmasi disjoint.

### Key Lessons
- Real-browser Playwright UAT WAJIB untuk Razor/JS/SignalR — runtime-smoke tak nangkep ReferenceError yang abort handler-attach.
- Gate-build + VM-flag WAJIB pakai variabel efektif yang SAMA; desync → null-data/hide tak konsisten.
- Adversarial re-check (multi-finder + build/test live) murah & nangkep doc-staleness + sibling-surface miss yang gate biasa lewatkan.

### Cost Observations
- Model mix: mayoritas opus (executor/planner/researcher/verifier-spawn), sonnet (checker/auditor).
- Notable: 1 milestone-autopilot run menutup Phase 414 penuh; workflow adversarial re-check (5 agen paralel) konfirmasi clear-to-close.

---

## Milestone: v32.2 — Inject Hasil Assessment Manual (Seakan Online)

**Shipped:** 2026-06-19 (local, not pushed)
**Phases:** 7 (393-398 + 398.1) | **Plans:** 26 | **Migration:** 0

### What Was Built
Page `/Admin/InjectAssessment` (Section C, Admin+HC) untuk inject hasil assessment manual identik online — wizard 6-langkah, `InjectBatchAsync` reuse GradingService/Aggregator/CertNumberHelper (nol duplikasi), mode jawaban input-asli/auto-gen, import Excel matrix (+retire BulkBackfill), link Pre/Post silang inject↔online. 398.1 tech-debt cleanup (8 FIX/2 DROP).

### What Worked
- **Reuse-bukan-paralel** sebagai tesis arsitektur: satu grading entry-point + satu read projection + satu renderer → inject tak bisa dibedakan dari online by-construction (integration checker 7/7 WIRED konfirmasi struktural).
- **Verify-first (D-01) di tech-debt phase**: 2 dari 10 temuan ternyata false-positive/by-design (essay decimal int-only, unlink cross-batch) — di-DROP dengan bukti, bukan blind-fix. Cegah refactor sia-sia + regresi.
- **Interactive execute (398.1)**: runtime verification menangkap fix yang salah — 999.13 "rely product flush + page.fill" + "direct invoke" GAGAL Flow K, percobaan-3 (mirror essay-flush-385: evaluate + product flush) baru PASS. Tanpa e2e runtime, fix salah lolos grep+build.

### What Was Inefficient
- **v32.0 tak diarsip sebelum v32.2 mulai** → ROADMAP/REQUIREMENTS/phases intermingled; CLI milestone-complete over-count (9 phase) perlu koreksi manual. Lesson: archive milestone SEBELUM mulai berikut.
- 999.13 makan 3 percobaan (root cause: parsed qcard-id ≠ DOM dataset.questionId di shuffled exam) — diagnosis butuh baca produk flushEssay.

### Patterns Established
- Tech-debt phase desimal (398.1) sebagai penutup milestone: verify-first + drop-with-evidence + behavior-lock test untuk yang tak-fixable in-scope.
- e2e essay helper: andalkan product flush (baca DOM live), JANGAN re-implement save dengan id hasil-parse.

### Key Lessons
- Archive milestone N sebelum milestone N+1 dimulai (hindari intermingling).
- Runtime e2e WAJIB untuk fix test-helper/UI — grep+build tak cukup (ulang lesson 354/392).

### Cost Observations
- Model mix: opus (plan/execute/pattern), sonnet (verify/checker/integration).
- Sessions: 1 panjang (audit→insert 398.1→discuss→plan→execute interactive→re-audit→close).

## Milestone: v31.0 — Hotfix Pra-Ujian Lisensor

**Shipped:** 2026-06-16 (local; NOT pushed)
**Phases:** 3 (385, 386, 387) | **Plans:** 12 | **REQ:** 14/14 PXF-01..14 | **0 migration**

### What Was Built
Pra-ujian-lisensor hardening lintas alur assessment SA+MA+Essay+soal-bergambar: (385) gambar soal/opsi PathBase-aware di sub-path `/KPB-PortalHC` + essay flush sebelum submit/blur/timeout; (386) validasi opsi soal (tolak SA/MA tanpa opsi ber-teks) + essay-kosong tetap bisa di-finalize + PDF/Excel MA all-or-nothing (SetEquals) via shared `IsQuestionCorrect`+`BuildAnswerCell`; (387 pasca-acara) SubmitEssayScore guards (type/ownership/status), cert nomor retry+log+surface, Excel essay cell, monitor broadcast, aria opsi huruf, SubmitExam MC no-null-overwrite, SaveTextAnswer timer guard.

### What Worked
- File-overlap phasing (PXF-02/04/05 + 07/14 digabung 386 krn semua `AssessmentAdminController.cs`) → nol konflik write.
- Shared display helpers (`IsQuestionCorrect`/`BuildAnswerCell`) dipakai PDF + Excel + web → kill-drift label MA/essay lintas surface resmi.
- Proportional verification (D-09): unit untuk logika, Playwright untuk a11y render runtime, manual untuk SignalR/cert/timer.

### What Was Inefficient
- Phase 385 (hotfix) ship tanpa VERIFICATION.md (pakai UAT.md) dan tanpa VALIDATION.md → di-backfill saat auto-close.
- PXF-07/PXF-14 di-fold ke 386 tapi tak ditandai di SUMMARY `requirements-completed` frontmatter → traceability doc-gap.
- PXF-08 `certError` dikirim controller tapi consumer JS (`essay-grading.js`) tak membacanya → gap baru ketahuan saat integration-check audit (di-fix inline `3005733d`).

### Key Lessons
- Auto-close lintas-sesi: tunggu signature repo benar-benar diam (HEAD + tracked + untracked) sebelum ambil alih — VERIFICATION/REVIEW bisa muncul untracked setelah commit terakhir (verifier/reviewer agent jalan tanpa commit).
- Integration check menangkap contract-drift yang phase-verify lewatkan: controller mengirim field, client tak konsumsi.

### Cost Observations
- v31.0 close dijalankan full-auto (1 sesi) sesudah execute 387 di sesi lain; secure+validate+verify+audit+complete+cleanup+HTML berantai.

---

## Milestone: v30.0 — Essay Grading Correctness + Monitoring UI Refactor

**Shipped:** 2026-06-15 (local; NOT pushed)
**Phases:** 2 (383-384) | **Plans:** 8 | **REQ:** 10/10 (ECG-01..06 + UIG-01..04) | **Migration:** 0

### What Was Built
- **Fase 1 (383):** Helper pure `AssessmentScoreAggregator.IsQuestionCorrect(q, responses) → bool?` (single source of truth correctness per-soal; essay Benar=`EssayScore>0`, null=pending) di 3 titik `CMPController.Results` (count/Elemen Teknis/Tinjauan) + PDF export. Fix bug user "Nilai Anda 100% tapi 4/6 benar" (essay dinilai benar dihitung salah). Regression lock Submit/Finalize EssayScore tanpa ubah kode. Read/display-path only.
- **Fase 2 (384):** Refactor UI Monitoring penilaian essay — blok inline numpuk → tabel worker-list ringkas (badge 3-state) + page per-worker `/Admin/EssayGrading` ("Tinjau Essay") reuse endpoint POST existing (backend 0 ubah). D-09 finalize in-place (no reload) + D-10 read-only finalized.

### What Worked
- **Helper terpusat kill-drift** (pola Phase 363/365/376) — satu `IsQuestionCorrect` jadi single source; web Results + PDF tak bisa divergen lagi. MC/MA mirror display-path byte-for-byte (no behavior change), cabang Essay baru.
- **Test-infra-first (Wave 0)** di 384 — spec Playwright RED/fixme + seed dikunci SEBELUM UI dibangun → executor punya target perilaku jelas (selector/route/flow).
- **Adversarial verification workflow** (ultracode) menangkap regression HIGH yang regression-gate unit suite + verifier tunggal LEWATKAN: 2 helper e2e lama (`examMatrix.ts`/`examTypes.ts`) masih target page lama setelah markup dipindah. Di-fix sebelum close.
- **UAT human-verify checkpoint** (384-04) — no self-approve; user approve 8-langkah browser.

### What Was Inefficient
- **UIG-04 test design bug** — versi awal mengulang save+finalize, tapi serial shared-seed sudah finalize di test sebelumnya → input disabled → fill timeout. Fix: fold D-09 ke UIG-03, UIG-04 jadi verifikasi read-only persisted. Pelajaran: serial e2e dengan seed-session tunggal → state carry antar-test, desain assertion sesuai urutan.
- **Collateral test-helper drift** — memindahkan markup UI (384-03) memutus helper e2e di luar phase. Pelajaran: grep cross-suite selector usage SEBELUM hapus/pindah markup bersama.

### Patterns Established
- e2e essay round-trip: seed PendingGrading + `GenerateCertificate=1` → finalize terbitkan cert → state finalized read-only testable dalam 1 fixture.
- D-09 in-place finalize: `finalizeInPlace()` (disable input/tombol) ganti `location.reload()` → URL stabil, test-friendly.

### Key Lessons
- Bug "display vs scoring drift" → fix di SATU helper display-path, jangan patch tiap call-site (akar drift = 2 jalur recompute terpisah).
- Adversarial review (correctness + security + skeptic-verify) menangkap collateral regression yang gate deterministik (build/unit) tak lihat — worth the spend untuk milestone close.

### Cost Observations
- Model mix: ~100% opus (interactive execute-phase + audit + verification workflow).
- Workflow: 1 verification workflow (4 agents, ~314k subagent tokens) + 1 integration-checker agent.
- Notable: interactive mode (no per-plan subagent) untuk 01-03 hemat token vs full subagent spawn; workflow dipakai hanya untuk verifikasi adversarial.

---

## Milestone: v24.0 — Gambar di Soal Assessment (Manage Package)

**Shipped:** 2026-06-09 (local) | **Phases:** 6 (352–357) | **Plans:** 22

### What Was Built
Gambar pada soal + opsi assessment: upload image-only (≤5MB magic-byte) + alt text, CRUD admin di ManagePackageQuestions, render konsisten 6 layar (responsive + lightbox), sync Pre→Post shared-file, hapus file atomic (pola Phase 333, no orphan). Plus 2 addon off-theme: Phase 356 audit-fix Assign Coach×Coachee (6 fix AF-1..7, eligibility per-unit HIGH) + Phase 357 standarisasi istilah tipe soal "Single/Multiple Answer" (single-source helper + hapus dead TrueFalse).

### What Worked
- **Spec-driven discuss** (352-355 dari satu design spec; 356/357 dari audit spec file:line) → planning mulus, checker PASS iterasi awal.
- **Code review menangkap bug yang UAT lewatkan** (356 WR-01: eligibility cross-unit false-negative dari interaksi AF-1+AF-3; data UAT single-unit tak ekspos). Review = safety net nyata.
- **Single-source helper** (QuestionTypeLabels 357, _QuestionImage partial 354) → 1 ubah propagate banyak surface; UAT 2-surface cukup buktikan rendering.
- **Playwright runtime UAT** wajib untuk Razor dynamic (354 RuntimeBinderException + label-toggle bug — build+grep tak deteksi).

### What Was Inefficient
- **Dev-server lock** (`dotnet run` lokal pegang HcPortal.dll/.exe) berulang blok `dotnet build` (MSB3027/3021, bukan CS error) — harus stop dev server tiap build gate. Lesson: hentikan dev server sebelum sesi execute.
- **CLI milestone-complete mis-count + garbage accomplishments** (recurring): phases 7 vs 6, accomplishments = deviation tags ("[Rule 1 - Bug]"/"[Discretion]"). Manual fix MILESTONES.md tiap close. Pola sama v21/v23.
- **Stale traceability checkbox** (IMG-04 `[ ]` Pending padahal satisfied) — audit yang nangkap.

### Patterns Established
- Off-theme addon di dalam milestone image (356/357) — independen jalur file, paralel-able, di-audit terpisah tapi 1 milestone.
- UAT proportional: surface helper-driven cukup 2 browser-verified + sisanya code-verified (single-source).
- sqlcmd butuh `-C` (trust ODBC18 cert) + `-I` (QUOTED_IDENTIFIER untuk UPDATE tabel filtered-index).

### Key Lessons
- Code review SETELAH UAT tetap wajib — UAT data lokal sering tak ekspos edge (cross-unit, race).
- Razor `@model dynamic`/binding WAJIB Playwright runtime; grep+build tak cukup.
- CLI milestone-complete output JANGAN dipercaya mentah — verify count + rewrite accomplishments manual.

### Cost Observations
- Model mix: mayoritas opus (orchestrator + planner + executor inline), sonnet (checker/reviewer/verifier/integration).
- Eksekusi --interactive inline (bukan subagent) untuk 356+357 → lower overhead, kontrol per-task.

---

## Milestone: v23.0 — CMP/Records Search & Filter Consistency Audit

**Shipped:** 2026-06-06 (local) | **Phases:** 2 (350-351) | **Plans:** 7 | **REQ:** 7/7 SF-01..07 | **0 migration**

### What Was Built
Konsistensi search & filter lintas 3 surface CMP/Records. Team View search cakup judul assessment (fix bug 999.2, `GetWorkersInSection` predicate, badge per-worker D-07 utuh) + dropdown "Lingkup" jujur + export WYSIWYG (SF-01/02/06). Worker Detail 0-match feedback (counter aria-live + empty-state) + filter Kategori dari record aktual via `BuildActualCategories` (SF-03/04). My Records filter Kategori+Tipe parity + back-nav `#team` tab activator (SF-05/07).

### What Worked
- **Wave 0 RED Playwright spec sebagai kontrak verifikasi** — spec ditulis dulu menargetkan selector final, hijau setelah view/backend. Menangkap regresi duplicate-id `#categoryFilter` (Team View partial vs My Records) saat wave-gate — strict-mode violation = sinyal jelas, bukan silent bug produksi.
- **`--interactive` per-plan checkpoint** (konsisten dgn v22.0) — deviation id-rename tertangkap + diperbaiki inline, zero rework. Per-task build+grep gate fast feedback.
- **Audit 3-source coverage cross-ref** (VERIFICATION + SUMMARY + traceability) menangkap checkbox REQUIREMENTS stale `[ ]` meski VERIFICATION passed → di-sync saat audit.
- **Pre-close re-check (user-prompted "ada miss?")** menangkap SEED_JOURNAL.md uncommitted (sisa Playwright matrix global-setup) sebelum close.

### What Was Inefficient
- **`milestone complete` CLI mis-count BERULANG** (sama dgn v22.0 retro) — auto-extract accomplishments ambil dari phase 323/324 lama un-archived, hasilkan "6 plans/0 tasks" + garbage ("Wave 1 SHIP READY", "Conflict:"). Manual fix MILESTONES entry lagi. **Akar belum ditangani: phase dir tak di-arsip per milestone.** Aksi v24.0: `/gsd-cleanup` arsip phase dir v18-v23 sebelum close berikutnya.
- **`audit-open` CLI crash** (`ReferenceError: output is not defined`) — pre-close artifact audit gagal, fallback manual enumerate deferred. Tooling bug.
- **Razor runtime-compile tak refresh** view yang diedit pada app running → rename id `#myCategoryFilter` tak ter-render sampai restart `dotnet run`. Wave-gate fail-positif sampai restart.

### Patterns Established
- **Namespace id per surface** saat partial digabung 1 DOM (`myCategoryFilter` vs Team View `categoryFilter`) — hindari `getElementById` collision. Plan T-trap harus cek duplicate-id lintas partial.
- **Distinct-actual option source** — opsi dropdown dari record aktual (`unifiedRecords.Kategori`) bukan master; sumber opsi = sumber data-attr → compare exact-equals aman.

### Key Lessons
- Wave 0 RED contract spec = jaring regresi cross-surface paling efektif (duplicate-id ketangkap otomatis).
- CLI `milestone complete` TIDAK bisa dipercaya untuk count/accomplishments selama phase dir flat lintas milestone — selalu verify + manual-fix MILESTONES post-CLI.

### Cost Observations
- Model: opus (executor + planner; verifier sonnet)
- Sessions: 1 (resume dari pause "/gsd-execute-phase 351 besok")
- Notable: `--interactive` + Wave 0 spec = zero rework meski 1 deviation signifikan (duplicate-id).

---

## Milestone: v22.0 — CMP-06 + Assessment/Monitoring Audit Fixes

**Shipped:** 2026-06-05 (local) | **Phases:** 5 (345-349) | **Plans:** 24 | **REQ:** 60/60 | **0 migration**

### What Was Built
Tampilan jujur "Menunggu Penilaian" (essay pending-grade) lintas 6+ surface assessment-records (CMP/Records, RecordsWorkerDetail, UserAssessmentHistory, Excel, PDF, Monitoring) + passRate/average exclude-pending; 2-audit sweep ManageAssessment+Monitoring (Pre-Post LinkedGroupId, Tab2 pagination/empty-state, i18n + a11y chevron + 7-kartu summary invariant + exclude-Cancelled progress + search Category).

### What Worked
- **Konstanta sebagai single source of truth** (`AssessmentStatus.PendingGrading`) — integration check konfirmasi label konsisten lintas 5 phase tanpa drift bermakna. Audit-driven (6×5-lens + 7-lens) menghasilkan REQ yang tepat-sasaran.
- **Interactive `/gsd-execute-phase --interactive` konfirmasi-tiap-plan** (348+349) — user kontrol per-plan, deviation tertangkap awal, zero rework. Per-task `dotnet build` + grep-acceptance gate = fast feedback.
- **Seed-workflow untuk verify yang tak bisa di-assert otomatis** — snapshot + seed prefix + Playwright + DELETE-cleanup + SEED_JOURNAL menutup CMP06R "human_needed" tanpa kotori DB.

### What Was Inefficient
- **`milestone complete` CLI mis-count phases** — menghitung SEMUA dir di `.planning/phases/` (323/324/340+ un-archived) sebagai v22.0 → "13 phases/46 plans" + accomplishment garbage. Akar: ROADMAP/phases dir tak di-reorganize lintas milestone (v15-v21 flat). Perlu manual fix MILESTONES entry. **Pelajaran: `/gsd-cleanup` arsip phase dir per milestone sebelum close**, atau CLI harus filter by milestone REQ mapping.
- **PDF (QuestPDF/SkiaSharp) return 204 di env lokal** (Phase 327 known) — blokir verify visual PDF; harus pivot ke 2 surface lain + code-review. Environmental, bukan defect.
- **VERIFICATION.md absen di 348/349** (interactive mode tak spawn gsd-verifier) → audit flag artifact-gap; human-verify+UAT substantif menutupi.

### Patterns Established
- **Exclude-pending denominator** konsisten 3 jalur (ComputeHistoryStats passRate + group `IsPassed ?? false` + MAP-10 MenungguPenilaianCount).
- **7-kartu summary invariant** (Total = jumlah N status bucket) sebagai kontrak UI testable browser.
- **Drop-gate > else-branch** untuk render kondisional identik (MAP-17 DRY).

### Key Lessons
- Audit milestone CLI butuh phase-archival rapi; un-archived dir lintas milestone bikin stats salah. Arsip phase dir saat close atau /gsd-cleanup rutin.
- Verify-via-seed valid untuk surface non-otomatable; tapi cek environmental blocker (QuestPDF 204) dulu sebelum janji full visual verify.

### Cost Observations
- Model mix: mostly opus (planning/execute) + sonnet (checker/integration/verifier).
- Sessions: ~beberapa (plan→execute 5 phase→audit→close dalam 1 sesi panjang).
- Notable: interactive per-plan execution + per-task build gate = zero rework across 24 plans.

---

## Milestone: v14.0 — Assessment Enhancement

**Shipped:** 2026-04-24
**Phases:** 8 (296-303) | **Plans:** 23 | **Tasks:** 35 | **Commits:** 206

### What Was Built
- **Data Foundation + GradingService** — Migrasi DB backward-compatible (QuestionType, AssessmentType, AssessmentPhase, LinkedGroupId, LinkedSessionId, HasManualGrading); GradingService single source of truth; `GradeFromSavedAnswers` dihapus
- **Admin Pre-Post Test** — Create/Edit/Monitor dengan paket Pre/Post terpisah atau same-package copy, delete group cascade, reset Pre→Post cascade, sertifikat hanya dari Post
- **4 Question Types baru** — True/False, Multiple Answer (all-or-nothing), Essay (manual grading), Fill-in-the-Blank (exact match case-insensitive); Excel import multi-tipe; worker UI sesuai tipe
- **Worker Pre-Post + Comparison** — Card pair dengan guard Post-disabled sampai Pre completed, comparison side-by-side, gain score `(Post-Pre)/(100-Pre)×100` dengan edge-case Pre=100
- **Mobile Optimization** — Offcanvas drawer navigasi soal, sticky footer Prev/Next/Submit, touch target ≥48dp, kompatibel dengan anti-copy Phase 280 (swipe dihapus per D-10)
- **Advanced Reporting** — Item analysis (p-value difficulty), discrimination Kelley 27% dengan warning n<30, distractor analysis, Gain Score Report per pekerja/elemen, Excel export (ClosedXML), Gain Score Trend chart
- **WCAG Quick Wins** — Skip link, keyboard navigation (arrow keys opsi, Tab antar soal), auto-focus soal pertama, ExtraTimeMinutes per assessment via SignalR real-time
- **Coach Workload Dashboard (Phase 303)** — Halaman dengan Chart.js horizontal bar threshold coloring, 4 summary cards, tabel detail, saran reassign approve/skip AJAX, modal Set Threshold, auto-suggest coach beban terendah di assign modal

### What Worked
- **Foundation-first pattern**: Phase 296 (data model + service extraction) sebagai landasan — 7 phase selanjutnya tidak perlu rework migrasi atau grading logic
- **Single source of truth refactor**: Menghapus `GradeFromSavedAnswers` dan memusatkan ke GradingService menghilangkan bug divergence antar grading path (SubmitExam, AkhiriUjian, AkhiriSemuaUjian) — prinsip "extract service before expanding features"
- **Backward-compatible migration**: Semua kolom nullable + default value "MultipleChoice" pada `QuestionType` → data lama auto-upgrade tanpa backfill script
- **UI-spec-driven phases**: Phase 297-302 & 303 semua dengan UI-SPEC sebelum planning — mengurangi rework view/razor
- **Mobile compat Phase 280 preserved**: Phase 300 respect keputusan anti-copy → swipe dihapus (D-10) daripada break fitur anti-cheat

### What Was Inefficient
- **Phase 303 UAT checkpoint terlupa**: Code shipped 2026-04-10, tapi human verification 12 langkah belum diapprove formal sampai milestone close. 2 minggu berjalan tanpa reminder — tooling perlu auto-surface paused checkpoints saat dormant >7 hari
- **Research gaps dibiarkan ke planning**: Essay char limit, Renewal behavior, Item Analysis n-threshold UX masuk `research gap` di STATE.md tapi tidak pernah diresolve sebelum implementation → implementation ambil default tanpa traceability ke keputusan eksplisit
- **Quick tasks mengalir paralel**: 8 quick tasks + side features (AddManualAssessment, Import Training E2E, performance analysis, sosialisasi docs) dijalankan sambil v14.0 berlangsung → membuat timeline v14.0 tampak 2 minggu+ padahal implementasi inti 4 hari
- **HANDOFF.json jadi stale**: HANDOFF masih `status: paused` untuk Phase 303 meskipun STATE.md menunjukkan `progress: 100%` — ada inkonsistensi state yang tidak auto-resolve

### Patterns Established
- **"Paused at checkpoint" harus diacknowledge atau ditutup eksplisit sebelum milestone close** — defer ke MILESTONES.md Known Gaps dengan explicit rationale
- **Foundation phase = phase 1 dari milestone berorientasi fitur** — migrasi DB + service extraction dulu, fitur belakangan
- **Multi-type discriminator column dengan default "primary type"** — pola backward-compatible untuk menambah varian entity tanpa breaking existing rows
- **Chart.js v4 horizontal bar via `indexAxis:'y'`** — bukan v2 `horizontalBar` (deprecated) — pattern untuk visualisasi workload/distribution di seluruh codebase

### Key Lessons
- Checkpoint tugas manual (human-verify) **harus** punya expiry atau reminder — 2 minggu dormant = risk bug production yang tidak ter-UAT
- Research gap di STATE.md `### Pending Todos` memerlukan **due date eksplisit** terkait phase — kalau tidak akan tertimbun saat planning berpindah
- Sertifikat/training record hanya dari Post-Test (bukan Pre) — pattern "secondary phase adalah source of truth untuk downstream artifact" penting untuk fitur multi-phase assessment di masa depan
- `data-*` attribute untuk auto-suggest UI menghindari AJAX round-trip — pertimbangkan untuk fitur "smart defaults" lain

### Cost Observations
- 206 commits dalam periode 18 hari kalendar (4 hari inti implementasi + 14 hari polish/iterasi)
- 218 files changed, +51,734 / -1,456 LOC — milestone terbesar sejak v2.5 (+12,297 LOC)
- Notable: Rasio +35 insertion per 1 deletion — tipikal feature-build milestone (bukan refactor); bandingkan v7.6 refactor dengan rasio ~2.7 insertion per deletion

---

## Milestone: v8.2 — Proton Coaching Ecosystem Audit

**Shipped:** 2026-03-23
**Phases:** 6 (233-238) | **Plans:** 16

### What Was Built
- Dokumen riset HTML perbandingan 3 platform enterprise (360Learning, BetterUp, CoachHub) vs Portal KPB — 20 rekomendasi 3-tier
- Setup audit: silabus delete safety, guidance file management, coach-coachee mapping atomic transaction, import all-or-nothing two-pass, progression warning override
- Execution audit: EvidencePathHistory resubmit traceability, approval race guard first-write-wins, notification completeness, PlanIdp coaching guidance scoped
- Completion audit: unique constraint ProtonFinalAssessment, coaching session Edit/Delete CRUD, HistoriProton completion criteria, MarkMappingCompleted graduated flow
- Monitoring audit: filter cascade fix, override transition validation, 3 export baru
- Differentiator: workload indicator badge warna, batch HC approval, bottleneck horizontal bar chart

### What Worked
- **Research-first pattern (v8.1 proven)**: Riset Phase 233 memberikan lens konkret untuk audit — gap analysis vs platform enterprise menghasilkan 37 findings (20 dari riset + 24 tambahan dari codebase, overlap)
- **6-phase sequential dependency chain**: Setup→Execution→Completion→Monitoring→Gap Closure — setiap phase membangun di atas perbaikan sebelumnya
- **Gap closure phase pattern**: Phase 238 muncul dari milestone audit internal — menutup 5 partial requirements yang backend sudah siap tapi UI belum wired
- **2-day execution**: 6 phases + 16 plans dalam 2 hari — audit milestones memang lebih cepat karena scope jelas

### What Was Inefficient
- **Phase 235 4 plans instead of 3**: Plan 235-04 gap closure diperlukan karena SubmitEvidenceWithCoaching batch endpoint terlewat di 235-02
- **v8.0 audit file used for v8.2**: Milestone audit file masih bernama v8.0-MILESTONE-AUDIT.md — naming mismatch karena v8.0 audit dilakukan sebelum v8.1/v8.2 split

### Patterns Established
- **Coaching audit checklist**: Setup integrity → Execution safety → Completion consistency → Monitoring accuracy — reusable sequence untuk audit ekosistem apapun
- **Differentiator phase terakhir**: Enhancement (workload indicator, batch approval, bottleneck) di phase terakhir setelah semua bug fix — fondasi bersih dulu
- **Warning-only progression**: Server mengirim warning tapi tidak block — user bisa override dengan confirm dialog

### Key Lessons
1. Research-first pattern terus terbukti — riset menghasilkan findings yang tidak terlihat dari codebase review saja
2. Gap closure phases (238) are natural — milestone self-audit menemukan UI yang belum wired ke backend yang sudah siap
3. Audit milestones semakin cepat seiring maturity codebase — v8.2 menemukan lebih sedikit critical bug dibanding v4.0

### Cost Observations
- Model mix: opus (orchestrator), sonnet (executor, verifier)
- Sessions: 2 (across 2 days)
- Notable: 24 requirements, 88 commits, 17K+ LOC dalam 2 hari

---

## Milestone: v7.12 — Struktur Organisasi CRUD

**Shipped:** 2026-03-21
**Phases:** 4 (219-222) | **Plans:** 7

### What Was Built
- Entity OrganizationUnit (adjacency list) dengan migrasi 4 Bagian + 19 Unit dari static class + konsolidasi KkjBagian
- CRUD page Struktur Organisasi di Kelola Data: indented table, tambah/edit/pindah/hapus/reorder, anti-circular reference, soft-delete guard
- Integrasi 15+ dropdown/filter Bagian/Unit di 4 controller ke database OrganizationUnits
- Cleanup final: hapus OrganizationStructure.cs, seed data idempotent, ImportWorkers validasi terhadap DB

### What Worked
- **4-phase dependency chain**: DB→CRUD→Integrasi→Cleanup memastikan setiap step bisa diverifikasi independen
- **Single-day execution**: Semua 4 phases + 7 plans selesai dalam satu sesi
- **Clean separation of concerns**: Model/migration terpisah dari UI terpisah dari integrasi — tidak ada partial-broken state
- **Idempotent seed pattern**: SeedOrganizationUnitsAsync dengan AnyAsync() guard — aman dijalankan berulang

### What Was Inefficient
- **Phase 216 deferred dari v7.11**: Export Fixes masih belum dieksekusi (carried over dari milestone sebelumnya)
- **STATE.md drift**: Performance Metrics section tidak auto-update saat plans selesai — manual entries outdated

### Patterns Established
- **Static-to-DB migration pattern**: Buat entity + migrasi data → CRUD page → integrasi codebase → hapus static class
- **Adjacency list untuk hierarchical data**: Self-referential ParentId dengan Level dan DisplayOrder
- **Cascade dropdown dari DB**: GetSectionsAsync() + GetUnitsBySectionAsync() pattern menggantikan hardcoded lists

### Key Lessons
- Pisahkan DB migration dan CRUD UI ke phase terpisah — verifikasi data integrity sebelum build UI di atasnya
- Integrasi seluruh codebase sekaligus (bukan incremental per controller) menghindari partial-broken state

---

## Milestone: v7.10 — RenewalCertificate Bug Fixes & Enhancement

**Shipped:** 2026-03-21
**Phases:** 3 (210-212) | **Plans:** 5

### What Was Built
- Critical renewal chain fixes: bulk renew per-user FK via JSON dictionary hidden input, badge count sync via BuildRenewalRowsAsync
- Data & display fixes: DeriveCertificateStatus ValidUntil=null separation, MapKategori consistency, grouping OrdinalIgnoreCase, URL-safe karakter khusus
- Tipe filter dropdown (Assessment/Training/Semua) with query param routing
- Renewal method modal for single and bulk renew (Assessment vs Training choice)
- AddTraining renewal mode with prefill Judul/Kategori/Peserta, banner, hidden FK inputs, bulk multi-user

### What Worked
- **3-phase dependency chain**: 210→211→212 sequential ordering ensured each fix built on stable foundation
- **Audit-first milestone**: v7.10 audit passed 14/14 requirements before archival — no gaps to close
- **Per-user FK map via JSON**: Hidden input dictionary pattern solved bulk renew FK assignment without model binding changes
- **Single-day execution**: All 3 phases + 5 plans completed in one session

### What Was Inefficient
- **Plan 210-02 gap closure**: Original 210-01 missed per-user FK mapping — required a gap closure plan (210-02) after UAT revealed bulk renew FK mismatch
- **Nyquist validation missing**: Phases 210 and 211 missing VALIDATION.md — pattern continues for bug-fix phases

### Patterns Established
- **Per-user FK map pattern**: JSON dictionary in hidden input (`renewFkMap`) for bulk operations where each row needs different FK values
- **Modal method selection**: When a record can be renewed via multiple paths, show a modal with explicit method buttons instead of auto-routing
- **Renewal mode pattern**: GET accepts renewal params → prefill form → banner indicator → hidden FK inputs → POST persists FK chain

### Key Lessons
1. Gap closure plans (210-02) are a natural part of bug-fix milestones — initial fix often reveals edge cases only visible during UAT
2. Mixed-type bulk operations need explicit user choice — auto-routing to one path loses the other type's items
3. AddTraining renewal mode reused CreateAssessment renewal patterns — proven patterns accelerate development

### Cost Observations
- Model mix: opus (orchestrator), sonnet (executor, verifier)
- Sessions: 1
- Notable: 14 requirements (10 bug fixes + 4 enhancements) shipped in a single day

---

## Milestone: v5.0 — Guide Page Overhaul

**Shipped:** 2026-03-16
**Phases:** 2 (171-172) | **Plans:** 4

### What Was Built
- GuideDetail accordion simplification: CMP 7→4 items, CDP 7→3 items, redundant step-by-step guides removed (covered by PDF tutorials)
- Tutorial card CSS refactor: inline styles → CSS variant modifier classes, AD guide tutorial card added
- Dynamic role-conditional guide card counts via Razor int variables
- FAQ expand/collapse all toggle, category reorder, redundant FAQ items removed
- Unified .guide-role-badge class, .step-variant-blue, shared accordion base styling
- Back-to-top floating button on both Guide pages, breadcrumb navigation on GuideDetail

### What Worked
- **PDF-first content strategy**: Removing accordion items covered by PDF tutorials was clean — content deduplication reduced maintenance burden
- **CSS variant modifier pattern**: guide-tutorial-card--cmp/cdp/admin is extensible and eliminates inline style drift
- **Browser UAT via Playwright**: All 5 UAT tests verified programmatically — faster and more reliable than manual browser testing
- **Small milestone scope**: 2 phases, 4 plans — entire milestone (plan → execute → verify → UAT → audit → archive) completed in a single session

### What Was Inefficient
- **SUMMARY frontmatter**: 171 SUMMARYs missing one_liner field, 172 SUMMARYs had it — inconsistent template adherence across executor runs
- **Nyquist validation still missing**: Both phases lack VALIDATION.md — pattern continues to be skipped for non-feature-building work

### Patterns Established
- **guide-role-badge**: Canonical class for role indicator badges across Guide system — replaces divergent .role-badge / .guide-step-badge-role
- **Back-to-top pattern**: Fixed-position button with scroll threshold toggle (.visible class at 300px), smooth scroll to top, hidden on print
- **Breadcrumb with module switch**: Razor switch block maps module param to friendly name, reuses existing .guide-breadcrumb CSS

### Key Lessons
1. UI polish milestones execute fast — mostly CSS/Razor changes with minimal controller logic
2. Playwright-based UAT is a reliable replacement for manual browser verification on CSS/JS features
3. Deduplication work (removing accordion items covered by PDFs) is low-risk, high-clarity improvement

### Cost Observations
- Model mix: sonnet (executor, verifier, integration checker), opus (orchestrator)
- Sessions: 1
- Notable: Entire milestone lifecycle completed in one session (~2 hours total)

---

## Milestone: v4.0 — E2E Use-Case Audit

**Shipped:** 2026-03-12
**Phases:** 6 (153-158) | **Plans:** 16

### What Was Built
- Assessment flow audit: Fixed FK crash, open redirect, certificate access control, TrainingRecord auto-creation
- Coaching Proton audit: Fixed mapping reactivation cascade, ProtonFinalAssessment creation on interview pass
- Admin Kelola Data audit: Fixed DeleteWorker cascade order, CPDP MIME type, missing audit log entries
- CDP Dashboard audit: Fixed URL manipulation, duplicate key crash on multiple assignments
- Auth audit: Full 7-controller authorization matrix verified
- Navigation audit: All links verified, GuideDetail case-sensitivity fix

### What Worked
- **Use-case flow audit format**: Organizing by flow (not by page/role) caught cross-cutting bugs (e.g., cascade order in DeleteWorker affecting coaching data)
- **Hybrid code+browser UAT**: Code review found bugs that browser testing alone would miss (security issues, edge cases); browser UAT confirmed fixes worked
- **Independent phases**: All 6 audit phases were independent — could execute in any order without blocking
- **Budget model profile**: Sonnet executor handled audit-style work (read → analyze → fix → verify) efficiently

### What Was Inefficient
- **SUMMARY frontmatter gaps**: All SUMMARY files have empty `requirements_completed` arrays — systematic gap in audit-style summary writing
- **Nyquist validation skipped**: All 6 phases missing VALIDATION.md — audit phases don't fit the Nyquist pattern well (they're verification, not feature-building)

### Patterns Established
- **Audit phase pattern**: Code review → findings document → targeted fixes → browser UAT → VERIFICATION.md
- **Authorization matrix**: Full controller-level authorization audit as a reusable verification approach

### Key Lessons
1. Audit milestones are faster than build milestones — 6 phases in 2 days vs typical 3-4 days
2. The v3.0 known gaps (Phase 89 PlanIDP, ASSESS-04 PositionTargetHelper) were both resolved by this audit
3. Tech debt documentation (10 items) provides a clear backlog for future work without blocking the milestone

### Cost Observations
- Model mix: 100% sonnet (executor)
- Sessions: ~4 sessions across 2 days
- Notable: Highest requirements-per-day ratio (33 requirements in 2 days)

---

## Milestone: v3.21 — Account Profile & Settings Cleanup

**Shipped:** 2026-03-11
**Phases:** 1 (Phase 152) | **Plans:** 1 | **Tasks:** 2

### What Was Built
- AccountController authorization hardened with class-level `[Authorize]` + `[AllowAnonymous]` on Login/AccessDenied
- ProfileViewModel decoupling Profile view from ApplicationUser entity
- Client-side validation on Settings page, international phone regex support
- Profile page UI polish (button label, row spacing)

### What Worked
- Single-phase milestone for small cleanup tasks — fast turnaround, no dependency overhead
- Budget model profile (sonnet executor) handled the straightforward changes efficiently

### What Was Inefficient
- Nothing significant — simple milestone executed cleanly

### Patterns Established
- Read-only profile pages should use a dedicated ViewModel, not the entity model directly

### Key Lessons
- Small cleanup milestones (6 requirements, 1 phase) can ship in a single session with minimal overhead

### Cost Observations
- Model mix: 100% sonnet (executor + verifier)
- Sessions: 1
- Notable: Entire milestone (plan → execute → verify → UAT → archive) completed in one session

---

## Milestone: v2.2 — Attempt History

**Shipped:** 2026-02-26
**Phases:** 1 (Phase 46) | **Plans:** 2 | **Tasks:** 4

### What Was Built
- AssessmentAttemptHistory table — archive row written at Reset time, preserving Score, IsPassed, AttemptNumber, timestamps
- Archival logic in ResetAssessment — Completed sessions only; archive + reset share one SaveChangesAsync
- Unified history query — merged archived + current Completed sessions with batch Attempt # computation (GroupBy avoids N+1)
- Dual sub-tab History tab — Riwayat Assessment + Riwayat Training with client-side filters (worker search + title dropdown)

### What Worked
- Archive-before-clear pattern: inserting the archive block before UserResponse deletion meant session field values were still available — no extra query needed to capture them
- Batch count pattern: computing archived AttemptNumber via one GroupBy query + ToDictionary lookup eliminated N+1 for all current session rows
- Tuple return from helper: returning `(assessment, training)` from GetAllWorkersHistory() kept the two sorted/shaped lists cleanly separated without a discriminator flag

### What Was Inefficient
- Plan spec said "3 plans" but 2 plans covered all requirements cleanly — the spec was slightly over-estimated; quick review before planning could have reduced to 2 upfront

### Patterns Established
- **Archive-before-clear**: When resetting stateful records, archive the current row *before* deletions/resets so field values are still available in memory. Share the downstream SaveChangesAsync.
- **Batch count for sequence numbers**: Compute `AttemptNumber` as `existingRows.Count + 1` using a single `GroupBy` across all (UserId, Title) pairs, then dictionary lookup per row — no sequence column needed.
- **Nested Bootstrap sub-tabs**: `ul.nav.nav-tabs` inside an existing `div.tab-pane` works cleanly for two-level navigation; default active sub-tab set via `active show` classes.
- **Client-side `data-*` filter**: `data-worker` + `data-title` attributes on `<tr>` elements; JS filterAssessmentRows() reads both inputs and sets `row.style.display` — no round-trip, works with static server render.

### Key Lessons
1. EF migrations require `--configuration Release` when the Debug build exe is locked by a running process — standard environment constraint for this project.
2. `GetAllWorkersHistory()` returning a tuple is appropriate when two result sets have fundamentally different shapes (sort order, columns) — don't force them into a single typed list.
3. For sequential numbering without a DB sequence: count existing rows for the same (UserId, title) key, add 1. Consistent at both archive time (Plan 01) and query time (Plan 02).

### Cost Observations
- Model profile: budget
- 1-day milestone (one sitting)
- Fast execution: 2 plans × ~10 min average = ~20 min total active work

---

## Milestone: v2.3 — Admin Portal

**Shipped:** 2026-03-01
**Phases:** 8 (47-53, 59) | **Plans:** 29 | **Tasks:** 33

### What Was Built
- AdminController with 12-card hub page — centralized admin tool access with role-gated navigation
- KKJ Matrix & CPDP Items spreadsheet editors — inline editing, bulk-save, multi-cell clipboard, Excel export
- Assessment Management migration — all manage actions (Create/Edit/Delete/Reset/ForceClose/Export/Monitoring/History) from CMP to Admin
- Coach-Coachee Mapping manager — grouped-by-coach view with bulk assign, soft-delete, section filter
- Proton Silabus & Coaching Guidance — two-tab /Admin/ProtonData replacing ProtonCatalog with CRUD + file management
- DeliverableProgress Override — third ProtonData tab for HC to fix stuck records; sequential lock removed entirely
- Final Assessment Manager — Assessment Proton exam category with eligibility gates and Tahun 3 interview workflow
- ProtonCatalog cleanup — dead controller/views removed after full migration

### What Worked
- **Admin hub pattern**: Single AdminController with card-based Index page scales well — each phase added 1-2 new tool pages without architectural changes
- **Spreadsheet-style editing**: Bulk-save pattern (collect all rows as JSON, POST once) is faster and more reliable than per-row AJAX
- **Phased migration**: Moving Assessment Management in 5 plans (scaffold → CRUD → monitoring → cleanup → gap fixes) prevented breaking changes mid-milestone
- **SUMMARY.md extraction**: Phase summaries provided quick context restoration across sessions

### What Was Inefficient
- **GAP plans**: Phases 47-49 each needed UAT gap plans (47-03/04/05, 48-04, 49-05) — initial plans didn't fully capture UI requirements, requiring correction passes
- **Phase removal churn**: 4 phases were removed/superseded during execution (56, 57, 58, 60) — upfront requirements were over-scoped
- **5 requirements left incomplete**: OPER-05, CRUD-01 through CRUD-04 were planned but phases were removed before execution

### Patterns Established
- **Admin hub card pattern**: Each admin tool gets a card on Admin/Index with icon, description, and link; cards grouped by domain (Data Management, Proton, Assessment & Training)
- **Spreadsheet editor pattern**: Read-mode table → Edit mode toggle → JSON bulk-save POST → DOM re-render; multi-cell clipboard via getTableCells() 2D array
- **AuditLog on write**: All admin write operations log to AuditLog via AuditLogService.LogAsync — consistent across all admin tools
- **ProtonData tab pattern**: Multiple admin tools sharing one route (/Admin/ProtonData) via Bootstrap nav-tabs — reduces hub card count

### Key Lessons
1. Scope v2.3 requirements more tightly — 12 requirements was too many; 7 shipped, 5 deferred. Better to ship smaller milestones with 100% completion.
2. GAP plans are a sign that initial discuss-phase didn't capture enough detail. Future phases should include more concrete UI mockup questions.
3. Migration phases (49, 59) are clean and predictable — the pattern of "move, verify references, delete originals" works reliably.
4. Sequential lock removal (Phase 52) was the right call — Active-on-assignment is simpler to understand and maintain.

### Cost Observations
- Model profile: budget
- 4-day milestone across multiple sessions
- 29 plans is the largest milestone yet — previous max was v1.7 with 14 plans

---

## Milestone: v2.4 — CDP Progress

**Shipped:** 2026-03-01
**Phases:** 4 (61-64) | **Plans:** 9

### What Was Built
- ProtonProgress page with data from ProtonDeliverableProgress + ProtonTrackAssignment — replaced IdpItems data source
- 5 filter parameters (Bagian/Unit, Coachee, Track, Tahun) wired to EF Core Where composition with role-scope-first pattern
- Per-role approval workflow: SrSpv/SectionHead/HC each with independent approval columns; data migration backfills from existing records
- Combined coaching report + evidence submission modal; CoachingSession FK linked to deliverable progress
- Excel (ClosedXML) + PDF (QuestPDF) export from Progress page
- Group-boundary server-side pagination (20 rows/page) with 3 empty state scenarios

### What Worked
- **Role-scope-first pattern**: Deriving scopedCoacheeIds from the logged-in user's role before applying any URL parameters ensures security by default — filters only narrow within already-authorized scope
- **EF Where composition**: Chaining `.Where()` calls on IQueryable and calling single `ToListAsync()` at the end keeps all filtering server-side with clean code structure
- **Per-role approval**: Independent columns for SrSpv/SH/HC approvals allowed each role to act independently without blocking others; data migration backfilled cleanly

### What Was Inefficient
- Phase renumbering (originally 63-66, renamed to 61-64 after phase removal) created git commit number mismatch — commits reference old numbers while SUMMARY files use new numbers

### Patterns Established
- **Role-scope-first filtering**: Always scope data by user role (CoachCoacheeMapping for coach, section for SrSpv, etc.) before applying user-selected filters
- **Per-role approval columns**: Independent approval per authorization level; any rejection overrides overall status; individual approvals don't cascade
- **Group-boundary pagination**: Group rows by logical unit (coachee + kompetensi + sub), slice groups into pages without splitting — better UX than arbitrary row cuts

### Key Lessons
1. ProtonProgress was a complete rewrite — the existing page was stub/mock data. Starting fresh was faster than patching.
2. QuestPDF was added for PDF export alongside ClosedXML for Excel — two export libraries now coexist cleanly.
3. Per-role approval migration needed a data fix (Locked→Pending) that was combined with schema migration — efficient single migration.

### Cost Observations
- Model profile: budget
- 2-day milestone (Feb 27-28)
- Executed in parallel with v2.5 phases

---

## Milestone: v2.5 — User Infrastructure & AD Readiness

**Shipped:** 2026-03-01
**Phases:** 8 (65-72) | **Plans:** 14

### What Was Built
- Dynamic Profile page bound to @model ApplicationUser; null-safe fallback; avatar initials from FullName
- Functional Settings page with ChangePassword, EditProfile (FullName/Position), and disabled placeholder items
- ManageWorkers migration: 11 actions from CMPController → AdminController with HC access; clean break (no redirects)
- Kelola Data hub: Admin/Index restructured into 3 domain sections; HC nav access extended
- Dual auth infrastructure: IAuthService + LocalAuthService + LdapAuthService; config toggle; System.DirectoryServices NuGet
- Login flow: IAuthService-based auth; AD hint; profile sync (FullName/Email); unregistered user rejection
- User structure: UserRoles.GetDefaultView() helper; SeedData modernization; AuthSource lifecycle (added then removed)
- Hybrid auth: HybridAuthService wraps AD-first + local fallback for admin user

### What Worked
- **IAuthService abstraction**: Clean separation of auth concerns behind interface — switching from local to AD requires only config toggle, no code changes
- **Clean break migration**: Removing old CMP ManageWorkers entirely (no redirects) eliminated dead code and confusion about canonical URLs
- **Kelola Data hub 3-section layout**: Grouping admin tools by domain made navigation intuitive; HC users can now access worker management directly

### What Was Inefficient
- **AuthSource field lifecycle**: Added in Phase 69 (per-user auth source), removed in Phase 72 (global config routing). The discuss-phase for Phase 69 should have concluded global routing earlier.
- **Phase 71 SeedData cleanup needed Phase 72 follow-up**: Modernizing SeedData in Phase 71 revealed the Admin KPB user needed hybrid auth fallback, requiring an entire additional phase (72)

### Patterns Established
- **IAuthService for dual auth**: Interface with Task<AuthResult> pattern; AuthenticationConfig POCO for LDAP settings; DI factory delegate for config-based registration
- **HybridAuthService composite**: Wraps two concrete services; email-based routing for fallback; silent failure semantics (same error UX regardless of which path failed)
- **GetDefaultView() single source of truth**: Role → SelectedView mapping extracted to static helper; SeedData and runtime both use same function

### Key Lessons
1. Global config routing (UseActiveDirectory toggle) is simpler than per-user AuthSource — one config flag instead of per-row DB field
2. Hybrid auth pattern solves the "admin needs local login in AD mode" problem elegantly — HybridAuthService tries AD first, falls back to local for specific email
3. The Supervisor role (level 5) addition was necessary for production role hierarchy — discovered during implementation, not during requirements

### Cost Observations
- Model profile: budget
- 2-day milestone (Feb 27-28), executed in parallel with v2.4
- 8 phases is second-largest (v2.3 had 8 phases too) but 14 plans is more manageable than v2.3's 29

---

## Milestone: v2.7 — Assessment Monitoring

**Shipped:** 2026-03-01
**Phases:** 3 (79-81) | **Plans:** 4

### What Was Built
- Assessment Monitoring group list page (/Admin/AssessmentMonitoring) with real-time stats, search/filter, status badges, Regenerate Token
- Per-participant monitoring detail with live progress, countdown timer, token card with inline copy/regenerate
- Full HC action suite on dedicated monitoring page (Reset, Force Close, Bulk Close, Close Early, Regenerate Token)
- Admin ManageQuestions page (ManageQuestions GET, AddQuestion POST, DeleteQuestion POST) accessible from ManageAssessment dropdown
- Hub cleanup — Monitoring dropdown removed from ManageAssessment, Training Records card removed from Section C, table min-height styling

### What Worked
- **Focused extraction pattern**: Moving existing monitoring functionality from a dropdown to a dedicated page was clean — controller actions already existed, just needed new views and navigation wiring
- **discuss-phase context capture**: CONTEXT.md for Phase 81 captured 4 distinct items (2 removals, 1 addition, 1 styling fix) which let the planner create well-scoped plans
- **Budget profile efficiency**: Sonnet planner/executor with haiku checker/verifier delivered all 4 plans without iteration — checker passed on first try for all phases
- **Single-day milestone**: All 3 phases planned and executed in one session with no blockers

### What Was Inefficient
- **Plan-index wave mismatch**: Plan 81-02 frontmatter specified wave 2 with depends_on 81-01, but the plan-index tool returned both as wave 1 — had to manually verify and enforce correct wave ordering
- **Summary one-liner extraction**: summary-extract returned null for all one_liner fields — summaries may not have the expected field format

### Patterns Established
- **Monitoring extraction pattern**: When a feature outgrows a dropdown action, create dedicated page with group list → detail drill-down → actions; remove old entry point last
- **Admin controller mirroring**: Copying CMP controller actions to AdminController with only redirect-target changes provides Admin-context equivalent pages without shared code complexity

### Key Lessons
1. Small focused milestones (3 phases, 4 plans) execute cleanly in a single session — ideal scope for extraction/cleanup work
2. Phase 81's discuss-phase captured a bonus feature (ManageQuestions) that wasn't in original requirements — discuss-phase is the right place to expand scope
3. CLN-01/CLN-02 cleanup phases should always be last — ensures new functionality is verified before removing old entry points

### Cost Observations
- Model profile: budget (sonnet planner/executor, haiku checker/verifier/researcher)
- 1-session milestone (~2 hours total)
- 4 plans is the smallest non-trivial milestone

---

## Milestone: v3.0 — Full QA & Feature Completion

**Shipped:** 2026-03-05
**Phases:** 10 (82-91, 86 superseded) | **Plans:** 34

### What Was Built
- Cleanup & Rename: "Proton Progress" → "Coaching Proton" throughout, orphaned CMP pages removed, AuditLog card added
- Master Data QA: Worker/Silabus soft delete infrastructure with IsActive filters across all queries
- Assessment Flow QA: Full lifecycle verified (create, assign, exam, auto-save, results, certificate)
- Coaching Proton QA: Full coaching workflow verified (mapping, evidence, multi-level approval, exports)
- Dashboard & Navigation QA: All dashboards role-scoped, login flow secure, nav visibility enforced
- KKJ Matrix Full Rewrite: Document-based file management (KkjFile/CpdpFile) replacing spreadsheet
- PlanIDP 2-Tab Redesign: Unified Silabus + Coaching Guidance tabs for all roles
- Admin & CMP Assessment Audit: 20 assessment flows verified with CSRF fixes and Records redesign

### What Worked
- **Use-case flow QA**: Testing by flow (not by page) caught cross-page integration issues that page-level testing would miss
- **Soft delete infrastructure**: Adding IsActive to ApplicationUser and ProtonKompetensi once, then filtering everywhere, was cleaner than scattered delete logic
- **Browser verification pattern**: Claude analyzes code → user verifies in browser → Claude fixes bugs. Efficient division of labor.
- **Supersession**: Phase 86 → 89 pivot was clean — superseding a phase and creating a better-scoped replacement avoids sunk cost

### What Was Inefficient
- **Phase 89 missing VERIFICATION.md**: Phase shipped without verification file — gap discovered only during milestone audit
- **Phase 88 verification mismatch**: VERIFICATION.md claims don't match actual codebase state — verification was written without re-reading the final code
- **PositionTargetHelper gap**: Component exists only in worktree, not main codebase — incomplete merge or abandoned branch work

### Patterns Established
- **Use-case flow QA**: Organize QA by user flows (assessment lifecycle, coaching workflow) not by page or role
- **Seed data actions**: Idempotent SeedXxxTestData actions for browser verification — quick setup for manual testing
- **Soft delete with IsActive**: Add bool IsActive to entity, filter in all queries, deactivate instead of delete

### Key Lessons
1. Always create VERIFICATION.md immediately after phase execution — don't defer to later sessions
2. Verification files should be written by re-reading actual code, not from memory of what was built
3. Use-case flow QA is superior to page-by-page QA for catching integration issues
4. 10 phases in 4 days is sustainable but verification quality suffers at speed

### Cost Observations
- Model profile: budget (sonnet planner/executor, haiku checker)
- 4-day milestone (2026-03-02 → 2026-03-05)
- QA phases are faster than build phases — mostly reading + verifying, less code generation

---

## Milestone: v3.6 — Histori Proton

**Shipped:** 2026-03-06
**Phases:** 2 (107-108) | **Plans:** 4

### What Was Built
- CDP "Histori Proton" navbar menu with role-scoped access (Coachee self-redirect, Coach/SrSpv/SH section, HC/Admin all)
- Worker list page with search by nama/NIP, filter by unit/section, step indicator, status badges
- Vertical timeline detail page with left-aligned line, colored circles (green=Lulus, yellow=Dalam Proses), expandable Bootstrap Collapse cards
- HistoriProtonDetailViewModel with ProtonTimelineNode — queries ProtonTrackAssignment + ProtonFinalAssessment + CoachCoacheeMapping

### What Worked
- **Cloning CoachingProton role-scoping**: HistoriProton role access copied CoachingProton's RoleLevel branching — proven pattern, zero auth bugs
- **Small focused milestone**: 2 phases, 4 plans — planned and executed in a single session with no blockers
- **Backend-first waves**: Plan 01 (ViewModel + controller) in wave 1, Plan 02 (view) in wave 2 — clean separation, view could reference compiled model

### What Was Inefficient
- **Plan-index wave mismatch (again)**: Plan 02 frontmatter had wave 2 + depends_on 01, but plan-index tool returned both as wave 1 — same bug as v2.7
- **Summary one-liner extraction still null**: summary-extract returns null for one_liner — format issue persists across milestones

### Patterns Established
- **Timeline CSS inline pattern**: All timeline CSS in a `<style>` block within the Razor view — appropriate for single-page custom styling
- **Left-aligned vertical timeline**: `.timeline` with `::before` pseudo-element for line, `.timeline-node::before` for circles — reusable pattern for any stepped history

### Key Lessons
1. Small milestones (2 phases) execute cleanly in one session — ideal for focused feature additions
2. Role-scoping patterns are now mature enough to clone without modification — CoachingProton is the reference implementation
3. Coach data best sourced from CoachCoacheeMapping (not ProtonTrackAssignment which lacks CoachId)

### Cost Observations
- Model profile: budget (sonnet planner/executor, haiku checker/researcher)
- 1-session milestone (~1.5 hours)
- 4 plans is efficient for a complete new feature (list + detail pages)

---

## Milestone: v3.8 — CoachingProton UI Redesign

**Shipped:** 2026-03-07
**Phases:** 1 (112) | **Plans:** 1

### What Was Built
- Converted 4 Pending badge spans to proper `btn-outline-warning` Tinjau buttons with modal triggers
- Added `fw-bold` + colored border to resolved status badges (Approved/Rejected/Reviewed) via Razor helpers
- Synchronized 6 JS innerHTML locations with new badge styling for AJAX consistency
- Unified Export PDF button to green outline matching Excel export
- Styled Evidence badges: Sudah Upload = bold green+border, Belum Upload = plain gray

### What Worked
- **Single-file scope**: Entire milestone touched only `CoachingProton.cshtml` — no cross-file coordination needed
- **Razor helper leverage**: Updating `GetApprovalBadge` and `GetApprovalBadgeWithTooltip` fixed all server-rendered badges at once
- **CONTEXT.md locked decisions**: discuss-phase captured exact class names and color mappings upfront — zero ambiguity during execution
- **Research line-number mapping**: Researcher identified exact line numbers for all 6 JS innerHTML locations — executor hit them all on first pass

### What Was Inefficient
- Nothing notable — single-plan milestone executed cleanly in one session

### Patterns Established
- **btn vs badge convention**: Interactive elements use `btn btn-outline-*` classes; read-only status indicators use `badge` with `fw-bold border` for resolved states
- **JS innerHTML sync rule**: After changing server-rendered badge/button styling, grep all `innerHTML` assignments in the same view — they MUST match

### Key Lessons
1. Pure CSS/HTML redesigns are ideal single-plan milestones — low risk, high visual impact
2. The discuss-phase "locked decisions" pattern eliminates design ambiguity at execution time
3. JS innerHTML is the #1 risk area for styling drift — always audit after Razor template changes

### Cost Observations
- Model profile: budget (sonnet planner/executor, haiku checker/researcher)
- 1-session milestone (~30 min total)
- Smallest possible milestone: 1 phase, 1 plan, 2 tasks

---

## Milestone: v4.3 — Bug Finder

**Shipped:** 2026-03-13
**Phases:** 3 (168-170) | **Plans:** 8

### What Was Built
- Code audit: 2 dead actions removed, 2 silent catches fixed, 3 unused imports cleaned
- File & database audit: 40+ temp files removed, .gitignore hardened, all 35 DbSets verified active
- Security review: NotificationController CSRF gap closed, 4 XSS patterns fixed, 2 import endpoints secured

### What Worked
- **Audit milestone structure**: 3 orthogonal phases (code/files/security) with zero cross-phase dependencies — all 3 ran in Wave 1 in parallel
- **Pre-scan in planner**: Planner agent pre-scanned codebase during planning, identified specific gaps (e.g., NotificationController CSRF) — executor had precise targets
- **Budget model profile**: All 8 plans executed with sonnet — audit/fix work doesn't need opus-level reasoning

### What Was Inefficient
- **SUMMARY frontmatter still empty**: `requirements_completed` arrays still null in all 8 SUMMARYs — same gap as v4.0
- **Nyquist validation skipped again**: Audit phases don't produce VALIDATION.md — pattern doesn't fit audit work

### Patterns Established
- **Security audit template**: Auth → CSRF → XSS/SQLi → uploads is a clean 4-step security checklist
- **Json.Serialize() for JS contexts**: Replaces unsafe Html.Raw(x.Replace()) pattern — established as canonical approach

### Key Lessons
- Audit milestones complete fast (1 day for 3 phases) because scope is well-defined and findings are binary (gap exists or doesn't)
- File cleanup should happen early in project lifecycle — 40+ screenshots accumulated over 167 phases

### Cost Observations
- Model mix: 100% sonnet (executor + verifier + checker + integration)
- Sessions: 1
- Notable: Entire milestone (plan + execute + verify + audit) completed in a single session

---

## Milestone: v7.6 — Code Deduplication & Shared Services

**Shipped:** 2026-03-18
**Phases:** 4 (196-199) | **Plans:** 6

### What Was Built
- IWorkerDataService shared service replacing 4 duplicate helpers in Admin+CMP (561 lines removed)
- ExcelExportHelper static class eliminating ~170 lines of ClosedXML boilerplate across 15 export actions
- Training CRUD consolidated — CMP orphan actions removed, ImportTraining moved to Admin with ManageAssessment link
- FileUploadHelper + PaginationHelper static classes replacing 6 inline patterns across 3 controllers
- CMPController GetCurrentUserRoleLevelAsync replacing 5 repeated role-checking blocks

### What Worked
- **Single-day refactoring milestone**: 4 phases planned and executed in one session — refactoring scope is well-bounded
- **Wave dependency ordering**: Phase 199 wave 2 (role-scoping) depended on wave 1 (file upload + pagination) — CMPController changes were sequential, avoiding merge conflicts
- **UAT for refactoring**: Regression tests (file upload, pagination, role-scoping) confirmed zero behavior change

### What Was Inefficient
- **SUMMARY frontmatter gaps continue**: Phase 198 SUMMARY missing `requirements-completed` field — same pattern as v4.0/v4.3
- **Nyquist validation not done**: All 4 phases missing VALIDATION.md — refactoring phases don't produce testable features

### Patterns Established
- **Static helper extraction pattern**: FileUploadHelper.SaveFileAsync and PaginationHelper.Calculate as reusable static helpers — appropriate for stateless utility functions
- **DI service for stateful helpers**: IWorkerDataService registered as scoped — appropriate when helper needs DbContext and UserManager
- **Template download exception**: Excel template downloads with colored headers/sample data use inline ClosedXML + ToFileResult (not CreateSheet) — documented design decision

### Key Lessons
1. Refactoring milestones execute fast (single day) because scope is clear and testable via build + regression
2. Integration checker confirmed all wiring in one pass — refactoring doesn't introduce new integration surfaces
3. CRUD consolidation (Phase 198) was the most impactful phase — removing orphan actions prevents confusion about canonical entry points

### Cost Observations
- Model mix: opus (executor), sonnet (verifier, integration checker)
- Sessions: 1
- Notable: Entire milestone lifecycle (plan → execute → UAT → audit → archive) in one session

---

## Milestone: v8.6 — Codebase Audit & Hardening

**Shipped:** 2026-03-24
**Phases:** 5 (248-252) | **Plans:** 7

### What Was Built
- CSS `.bg-purple` + data annotations (`[MaxLength]`, `[Range]`) pada model TrainingRecord dan ProtonModels
- Defensive null guards + `TryParse` + safe `ToDictionary` mencegah crash di 5+ halaman CMP
- Hapus 4 `console.log` sensitif, tutup XSS server-side via `HtmlEncode`, throttle notification 1x/jam via IMemoryCache
- UTC normalization (`DateTime.UtcNow`), composite unique index migration, thread-safe `_lastScopeLabel` refactor
- Client-side XSS closure via `escHtml()` vanilla JS di 3 AJAX handler CoachingProton

### What Worked
- Gap closure phase (252) dari audit — audit menemukan celah AJAX XSS, langsung ditutup sebagai phase tambahan
- Semua fix atomic — 1 bug = 1 commit, mudah di-revert jika ada regresi
- Milestone audit sebelum completion menangkap gap SEC-02 AJAX yang terlewat

### What Was Inefficient
- Phase 252 bisa digabung ke Phase 250 sejak awal jika audit XSS mencakup jalur AJAX juga
- Beberapa UAT test di-skip karena tidak ada test data (deliverable belum ada evidence) — perlu seed data untuk testing approval flow

### Patterns Established
- `escHtml()` vanilla JS helper sebagai defense-in-depth pattern untuk semua innerHTML interpolation
- Milestone audit → gap closure phase sebagai quality gate sebelum milestone completion

### Key Lessons
- Selalu audit jalur server-side DAN client-side bersamaan untuk kerentanan XSS
- Programmatic verification via Playwright bisa menggantikan manual UAT saat test data tidak tersedia

### Cost Observations
- Model mix: opus (planner), sonnet (executor, researcher, verifier, checker)
- Sessions: 1
- Notable: 5 phases + gap closure + milestone completion dalam 1 session, ~2 jam total

---

## Cross-Milestone Trends

| Milestone | Phases | Plans | Days | Avg plans/day |
|-----------|--------|-------|------|---------------|
| v1.0 | 3 | 10 | 1 | 10 |
| v1.1 | 5 | 13 | 2 | 6.5 |
| v1.2 | 4 | 7 | 1 | 7 |
| v1.3 | 3 | 3 | 1 | 3 |
| v1.4 | 1 | 3 | 1 | 3 |
| v1.5 | 1 | 7 | 1 | 7 |
| v1.6 | 3 | 3 | 1 | 3 |
| v1.7 | 6 | 14 | 2 | 7 |
| v1.8 | 6 | 10 | 2 | 5 |
| v1.9 | 5 | 8 | 2 | 4 |
| v2.0 | 3 | 5 | 1 | 5 |
| v2.1 | 5 | 13 | 2 | 6.5 |
| v2.2 | 1 | 2 | 1 | 2 |
| v2.3 | 8 | 29 | 4 | 7.25 |
| v2.4 | 4 | 9 | 2 | 4.5 |
| v2.5 | 8 | 14 | 2 | 7 |
| v2.6 | 6 | 12 | 1 | 12 |
| v2.7 | 3 | 4 | 1 | 4 |
| v3.0 | 10 | 34 | 4 | 8.5 |
| v3.6 | 2 | 4 | 1 | 4 |
| v3.8 | 1 | 1 | 1 | 1 |
| v3.21 | 1 | 1 | 1 | 1 |
| v4.0 | 6 | 16 | 2 | 8 |
| v4.3 | 3 | 8 | 1 | 8 |

| v5.0 | 2 | 4 | 1 | 4 |
| v7.6 | 4 | 6 | 1 | 6 |
| v7.10 | 3 | 5 | 1 | 5 |
| v7.12 | 4 | 7 | 1 | 7 |
| v8.2 | 6 | 16 | 2 | 8 |
| v8.6 | 5 | 7 | 1 | 7 |

**Running total:** 122 phases, ~275 plans, 35 days
