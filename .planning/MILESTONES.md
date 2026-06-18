# Milestones

## v32.1 Perbaikan Teks & Desain (Shipped: 2026-06-18)

**Phases completed:** 4 phases (388-390 + 390.1), 7 plans — **0 migration, 0 backend** (pure UI/teks 3 surface). Audit PASSED 7/7 REQ.

**Key accomplishments:**

- LBL-03: label "Batas Nilai Kelulusan" (bukan "Nilai Kelulusan") di kartu ringkasan `Views/CMP/Results.cshtml`; nilai persen tak berubah.
- DSN-01/02/03 (Phase 389, risk tertinggi): redesign `CoachCoacheeMapping` → accordion card per coach (avatar inisial + nama + section + badge beban warna-ikut-threshold) + toolbar diseragamkan + hapus dead-`onclick` "Tambah Mapping". Behavior parity (modal/AJAX/collapse) terjaga.
- DSN-04/05 (Phase 388): `CoachWorkload` polish — filter bar + section "Saran Penyeimbangan" dibungkus card konsisten + hapus inline magic-number font-size + spacing.
- DSN-06 (Phase 390): Test & UAT behavior parity penutup — semua aksi existing (assign/edit/nonaktif/graduated/hapus/reactivate/import/export) lolos Playwright + UAT browser.
- DSN-07 (Phase 390.1): import-button auto-enable via DOMContentLoaded fix (V-18 PASS runtime, verify 4/4, SECURED 2/2).
- Verifikasi: gsd-verifier 389-VERIFICATION 5/5, audit milestone PASSED (7/7 REQ + DSN-07 bonus, integration 12/12 wired, nyquist all-compliant). Known deferred at close: 44 artifact (43 quick-task lama selesai + 1 todo cleanup DB lokal) — lihat STATE.md Deferred Items.

---

## v31.0 Hotfix Pra-Ujian Lisensor (Shipped: 2026-06-15)

**Phases completed:** 6 phases, 12 plans, 28 tasks

**Key accomplishments:**

- `_QuestionImage.cshtml` render `<img src>` + lightbox `data-img-src` via `Url.Content("~"+path)` → gambar soal/opsi tidak lagi 404 di sub-path `/KPB-PortalHC` (Dev/Prod), e2e terbukti load 200 + naturalWidth>0.
- `StartExam.cshtml` flush jawaban essay (invoke SaveTextAnswer + await) sebelum submit/changePage + save-on-blur + timeout best-effort → keystroke ~2 detik terakhir tidak hilang dan peserta sudah-ketik tidak ditolak "belum dijawab"; e2e terbukti TextAnswer tersimpan utuh (97/97 char) di DB sebelum gate.
- 6 RED/skip-gated test scaffolds (xUnit + Playwright) that lock PXF-02 option validation, PXF-04 essay-empty finalize count-parity, and PXF-05 PDF MA answer-cell contracts before any production code — project builds RED only on the two Wave-1 helper symbols.
- Two pure EF-free helpers — `QuestionOptionValidator.ValidateQuestionOptions` (PXF-02 option-presence) and `AssessmentScoreAggregator.BuildAnswerCell` (PXF-05 answer-cell with ', ' MA join) — that turn the Wave-0 RED unit tests GREEN (24/24) without touching any controller, view, or scoring logic.
- Wired the Wave-1 pure helper `QuestionOptionValidator.ValidateQuestionOptions` into BOTH `CreateQuestion` and `EditQuestion(POST)` in `AssessmentAdminController.cs` — an admin/HC can no longer save a Single/Multiple Answer question with <2 text options nor with a checked-correct option that has no text, closing the F-DEV-01 exam-freeze class at config-time. Single shared rule across Create + Edit (no drift); correctCount gate, copy-path, and importer untouched; 0 migration.
- Closes F-04 (essay-empty dead-end) by applying the single pending predicate `!string.IsNullOrWhiteSpace(TextAnswer) && EssayScore == null` byte-identical at all 4 count surfaces, plus making `SubmitEssayScore` a defensive upsert guarded by a mandatory `Status == PendingGrading` check — with a Rule-1 correction moving whitespace evaluation in-memory at the 2 EF sites so SQL Server's tab/newline blind spot can't re-introduce count divergence.
- Both official-evidence export surfaces — the per-peserta PDF (`GeneratePerPesertaPdf`, F-17) and the Excel "Detail Per Soal" sheet (`AddDetailPerSoalSheet`, F-DEV-02) — now route their "Jawaban" cell through `BuildAnswerCell` and their Benar/Salah label through `IsQuestionCorrect`, so Multiple Answer is labeled all-or-nothing (SetEquals) and lists every selected option on BOTH surfaces identically; scoring engine untouched, 0 migration.
- The two Wave-0 e2e specs (PXF-02 option-validation reject, PXF-04 essay-empty finalize round-trip) were un-gated and greened against the running app with self-contained SQL seeds, the full dotnet test suite passed 474/474 with a 0-error build, and a live browser UAT APPROVED the byte-rendered official per-peserta PDF + Excel "Detail Jawaban" Multiple-Answer all-or-nothing labels (F-17 / F-DEV-02 proof), the PXF-02 server reject banner, and the PXF-04 EssayGrading finalize surface — closing Phase 386 (PXF-02 + PXF-04 + PXF-05) with 0 migration.
- SubmitEssayScore type+ownership hardening (WR-01/WR-02), cert-number retry 3x + certError surface (PXF-08), monitor workerSubmitted broadcast (PXF-10), dan Excel "Detail Jawaban" essay cell tampil jawaban+skor nyata (PXF-09) — semua in-place edit ke AssessmentAdminController.cs, 0 migration.
- Two server-authoritative write guards: SubmitExam no longer nulls a SignalR-saved MC answer absent from the form (PXF-12), and Hub.SaveTextAnswer rejects + logs post-timer essay writes (PXF-13), mirroring SaveMultipleAnswer.
- Option images on Results.cshtml and ExamSummary.cshtml now expose per-letter aria context (opsi A / opsi B / opsi C / opsi D) by converting each plain foreach into an indexed for loop, mirroring the verbatim letters[oi] derivation from StartExam.cshtml.
- Proportional verification (D-09) untuk Phase 387: 8 xUnit Integration facts (disposable real-SQL) untuk PXF-06/09/12 + 1 Playwright a11y spec PXF-11 (2 surface) + manual browser/SignalR/DB sign-off PXF-08/10/13 — semua PASS, fast suite 347/347 GREEN, build 0 error, 0 migration.

---

## v30.0 Essay Grading Correctness + Monitoring UI Refactor (Shipped: 2026-06-15)

**Phases completed:** 2 phases, 8 plans, 15 tasks

**Key accomplishments:**

- Helper murni `AssessmentScoreAggregator.IsQuestionCorrect(q, responsesForQ) -> bool?` sebagai single source of truth correctness per-soal (MC/MA byte-for-byte DISPLAY-path + cabang Essay baru `EssayScore>0`), dikunci 11 unit test pure tanpa DB — fondasi kill-drift bug "Nilai Anda 100% tapi 4/6 benar".
- CMPController.Results count/Elemen-Teknis/Tinjauan-badge + Results.cshtml kini essay-aware lewat helper IsQuestionCorrect — soal essay yang dinilai HC dihitung Benar di seluruh surface web Results (count 6/6, bukan 4/6), plus teks jawaban essay worker tampil di baris Tinjauan.
- 1. [Rule 1 - Bug] Fixed test cross-session question leak (shared fixture DB)
- SQL seed fixture session essay-pending package-aware + spec Playwright FLOW 384 (4 test RED/fixme) dengan harness snapshot→seed→restore — kontrak e2e dikunci sebelum UI dibangun.
- GET /Admin/EssayGrading + view per-worker (clone kartu essay byte-for-byte) + essay-grading.js dengan finalize D-09 in-place + D-10 read-only — reuse backend POST tanpa ubah, authz Admin/HC.
- Blok essay inline (stacked cards per-worker) diganti tabel worker-list ringkas 4-kolom dengan badge status 3-state + tombol "Tinjau Essay" → page per-worker; handler AJAX essay dead dihapus.
- e2e FLOW 384 (UIG-04) dijalankan HIJAU 4/4 runtime (round-trip list→Tinjau Essay→Simpan Skor→Selesaikan→Selesai in-place + read-only persisted) + UAT manual browser approved; DB di-restore, journal cleaned.

---

## v29.0 Assessment E2E Worker-Success Fix (Shipped Local: 2026-06-15, Audited: 2026-06-15)

**Phases completed:** 3 phase (380-382), 8 plan
**Status:** SHIPPED LOCAL + PUSHED `origin/ITHandoff` 2026-06-15 (tag `v29.0`). **0 migration**.
**Audit:** `v29.0-MILESTONE-AUDIT.md` — status **passed** (11/11 REQ WSE-01..11, 3/3 phase, integration 11/11 seam WIRED [build 0 err, 58/58 cross-seam unit, no clobber CMPController seri A→B→C], 10/10 E2E flow). Security threats_open:0 (380 7/7, 382 12/12). Full suite 415/415.

**Delivered:** Worker bisa ujian + lulus end-to-end untuk assessment Normal + PrePost soal single-answer (NON-Proton). Audit-driven (`docs/assessment-audit/2026-06-14-E2E-worker-success-FOCUS.md`, verdict awal CONDITIONAL). 3 phase SEQUENTIAL merge A→B→C (semua sentuh `CMPController.cs`).

**Key accomplishments:**

1. **Admin/Engine Integrity (Phase 380, WSE-01,02,03)** — WSE-01 SHF-01 `ShuffleEngine` ON-path empty-package filter + StartExam all-empty guard (paket kosong tak menzerokan worker / no 0% Fail palsu); WSE-02 TOK-01 `AccessTokenMatches` both-sides ToUpperInvariant heal + EditAssessment uppercase-write 3 sites (token edit-admin tetap dipakai worker); WSE-03 RST-01/04 `AddExtraTime` `[Authorize(Roles="Admin,HC")]` + cap atomic. xUnit 384/384, SECURED 7/7, UAT 5/5 (live e2e Flow L+M 10/10).
2. **Worker Entry / StartExam integrity (Phase 381, WSE-04,05)** — WSE-04 NEW-same-day-PrePost shared type-aware sibling helper (`SiblingSessionQuery`, filter AssessmentType/LinkedGroupId) + rewire StartExam/ReshufflePackage/ReshuffleAll (Pre/Post tak tercampur); WSE-05 OPS-01/TOK-03 write-on-GET impersonation guard 3 write-site + in-memory preview (admin impersonate = read-only, tak memulai/membakar waktu/mengunci shuffle). 381-VERIFICATION + e2e #4/#7.
3. **Grading / Lifecycle / Cert (Phase 382, WSE-06..11)** — WSE-06 SAVE-01 dedupe read-final (GradingService `finalByQuestion` ORDER BY SubmittedAt desc + SubmitExam GroupBy mirror); WSE-07 STAT-01 anti-resurrection guard kedua branch + const Abandoned; WSE-08 STAT-02 AbandonExam atomic ExecuteUpdate (ownership-in-WHERE, anti-race+anti-spoof); WSE-09 TMR-01 blocklist-invert + TMR-02 server-timer authority + TMR-03 token-on-success; WSE-10 TOK-02 StartedAt-gate SaveAnswer+SubmitExam; WSE-11 CERT-01 `DeriveCertificateStatus` null→Aktif single-source. xUnit 415/415, e2e #8-12 acceptance, SECURED 12/12, nyquist-compliant.

**Migration:** 0 (D-01-IMPACT — SAVE-01 dedupe last-write-wins, BUKAN filtered-index; `dotnet ef migrations add _verify_382` → 0 model diff). **Known deferred (non-blocker):** CERT-01 konfirmasi visual human (DB-coherence sudah otomatis); 1 finding LOW I-1 (WSE-01 pre-check non-type-aware → redirect aman); RES-02/GRD-02 backlog. **Notify IT:** v29.0 = 0 migration baru (carry lama 360/372 masih pending IT). Plain-language summary: `docs/milestone-v29.0/index.html`.

---

## v28.0 Assessment & Records Bug Fixes (Shipped Local: 2026-06-14, Audited: 2026-06-14)

**Phases completed:** 4 phase (376-379), 16 plan
**Status:** SHIPPED LOCAL, NOT PUSHED — branch `ITHandoff` (bundle v24-v28). 0 migration.
**Audit:** `v28.0-MILESTONE-AUDIT.md` — status **passed** (6/6 REQ GRADE/IMP/CMPRT/E2E, 4/4 phase, integration PASS 0-orphan/0-broken, 4/4 nyquist compliant, 3/3 E2E flow).

**Delivered:** Fix 4 bug fungsional outstanding domain assessment & records (promote backlog 999.8/999.6/999.10/999.7): grading essay-only Score aggregation, impersonasi identity lintas worker-data surfaces, routing CMP orphan 500, migrasi net e2e exam-taking ke wizard.

**Key accomplishments:**

1. **Fix Essay-Only Score Aggregation (Phase 376, GRADE-01,02)** — diagnose-first: bug TAK reproduce (fixed incidental v27.0 Phase 373). Reframe Option 1 → helper pure `AssessmentScoreAggregator.Compute` (kill-drift, D-02/D-04) wired ke `FinalizeEssayGrading` forward + endpoint `RecomputeEssayScores` (prod-repair historis, Admin idempotent, D-03 Score+IsPassed only). 361/361 + integration 3/3 + e2e FLOW L Score=80 ×3.
2. **Impersonation Identity Across Surfaces (Phase 377, IMP-01,02)** — akar bug 999.6 (impersonate X → worker-data tampil admin) fixed. Resolver single-source `ImpersonationService.GetEffectiveUserAsync` (D-05) + middleware D-04 fail-closed; CMP/CDP/Home konsumsi; D-03 mode-role hint + StartExam write-on-GET guard. 377-AUDIT.md (11 in-scope call-site). xUnit 372/372 + e2e 13/13 + UAT browser 7/7 live (Records Iwan 6 record). SECURED 25/25 + VALIDATED nyquist 0-gap.
3. **Fix CMP CertificationManagement Route 500 (Phase 378, CMPRT-01)** — `CMPController.CertificationManagement` redirect 302 → CDP canonical (was View→500 orphan). Hapus 6 method dead + 2 builder orphan (KEEP `BuildSertifikatRowsAsync`: caller `ExportSertifikatDetailExcel`). build exit 0 + e2e Y0 redirect→CDP 200. VERIFICATION 6/6.
4. **Migrate exam-taking e2e to wizard (Phase 379, E2E-01)** — 10 create flow (A-J) flat-form→wizard 4-langkah + layer PACKAGE; 10 `test.fixme`→0; +Flow K BARU essay DB-assert Score===80 (cover sinergi GRADE-01). Suite 75 passed / 7 skipped / 0 failed (`--workers=1`). Helper extension additive (D-04), test-infra only (0 prod code).

**Migration:** 0. **Known deferred (non-blocker):** 377 live exam-transition impersonation guard (butuh fixture Upcoming-due); 378 2 item Info code-review; `CMPController.DokumenKkj` section filter pre-existing out-scope. **NOT PUSHED** — bundle v24-v28 pending IT (3 migration carry dari milestone lama: Origin/358, PendingProtonBypass+index/360, ShuffleToggles/372).

---

## v27.0 Shuffle Toggle (Acak Soal & Acak Pilihan) (Shipped Local: 2026-06-14, Audited: 2026-06-14)

**Phases completed:** 4 phase (372-375), 12 plan
**Status:** SHIPPED LOCAL, NOT PUSHED — branch `ITHandoff` (bundle v24-v27). 1 migration `AddShuffleTogglesToAssessmentSession` (defaultValue:true) — applied DB lokal, flag IT.
**Audit:** `v27.0-MILESTONE-AUDIT.md` — status **passed** (16/16 REQ SHUF-01..16, 4/4 phase, integration 5/5 wired, 4/4 threats_open:0, 4/4 nyquist, build 352/352 + shuffle 46/46 live).

**Delivered:** HC bisa ON/OFF dua sistem pengacakan independen (Acak Soal + Acak Pilihan) per-assessment via ManagePackages. Default ON dua-duanya (data lama tak berubah).

**Key accomplishments:**

1. **Data Foundation + Propagasi (Phase 372, SHUF-01..03)** — 2 kolom `ShuffleQuestions`/`ShuffleOptions` di `AssessmentSession` + migration defaultValue:true + set eksplisit 3 loop CreateAssessment (hindari EF bool-false trap) + propagate sibling EditAssessment + toggle wizard Step 3.
2. **Shuffle Engine read-logic + reshuffle (Phase 373, SHUF-04..09,15)** — pure `Helpers/ShuffleEngine.cs` (ON canonical / OFF q.Order / OFF≥2 round-robin `workerIndex%count` index-stabil + guard paket kosong / opsi dict-or-"{}") wired ke `CMPController.StartExam` + fix bug SHUF-09 (`ReshufflePackage`/`ReshuffleAll` hard-code "{}" tereliminasi) + cleanup komentar stale.
3. **UI ManagePackages + Lock + Pre/Post (Phase 374, SHUF-10..14)** — 2 toggle header + endpoint POST `UpdateShuffleSettings` (`[Authorize(Admin,HC)]`+AntiForgery+audit+propagate) + lock saat peserta mulai + warning ukuran-paket-beda + reminder Pre/Post + hide Proton Th3/Manual. Helper `ShuffleToggleRules`. VERIFICATION 9/9 + UAT browser 7/7.
4. **Test & UAT (Phase 375, SHUF-16)** — `ShuffleModeMatrixTests` + `ShuffleEngineTests` (19 shuffle xUnit, suite 352/352) + `shuffle.spec.ts` 5/5 ManagePackages + exam-diff manual 3/3 live (B1 soal beda, B2 opsi beda, B3 OFF round-robin). Checkpoint di-approve via verifikasi otomatis (dotnet test + 7-skeptik adversarial).

**Migration:** 1 (Phase 372). **Known deferred:** exam-effect VISUAL order-diff manual-only by design (D-03); NOT PUSHED.

---

## v26.0 Urgent — Search & Records Visibility (Shipped Local: 2026-06-12, Audited: 2026-06-12)

**Phases completed:** 3 phase (369-371), 3 plan
**Status:** SHIPPED LOCAL, NOT PUSHED — branch `ITHandoff` (bundle v24-v27). 0 migration.
**Audit:** `v26.0-MILESTONE-AUDIT.md` — status **passed** (3/3 REQ URG-01..03, 3/3 phase, integration 3/3, security 3/3, nyquist 3/3).

**Delivered:** Fix urgent search & records visibility — sinkron H1 search-drop main→ITHandoff + hapus window 7-hari (tampilan default tanpa batas) + sesi online tampil di Tab Input Records (visibility-only).

**Key accomplishments:**

1. **Sync H1 Search-Drop Fix (Phase 369, URG-01)** — port `GetWorkersInSection` treat searchScope null/kosong sebagai "Nama" (commit `14e7adc5` main) ke ITHandoff — search nama Tab Input Records tak lagi diabaikan diam-diam.
2. **Hapus Window 7-Hari (Phase 370, URG-02)** — tampilan default tanpa batas window 7-hari (Post Test OJT >7 hari kini tampil).
3. **Sesi Online di Tab Input Records (Phase 371, URG-03)** — view-only `_TrainingRecordsTab.cshtml` online tampil + badge.

**Migration:** 0. **Known deferred:** NOT PUSHED.

---

## v25.0 Proton Kelulusan & Bypass (Shipped Local: 2026-06-13, Audited: 2026-06-13, re-audited 2026-06-14)

**Phases completed:** 11 phase (358-368), ~38 plan
**Status:** SHIPPED LOCAL, NOT PUSHED — branch `ITHandoff` (bundle v24-v27). 2 migration (`Origin` 358 + `PendingProtonBypass`+filtered-index 360) — applied DB lokal, flag IT (Origin sudah di origin/ITHandoff; PendingProtonBypass+index di delta unpushed).
**Audit:** `v25.0-MILESTONE-AUDIT.md` — status **passed** (20/20 REQ PCOMP/PBYP, 10/11 full-artifact [362 no-dir, shipped per ROADMAP], integration 5/5 wired, 0 blocker). Re-audit 2026-06-14: 0 drift.

**Delivered:** Logic kelulusan Proton konsisten (exam Tahun 1/2 terbit penanda "Lulus" + gate berurutan dipaksa) + fitur Bypass Tahun, plus delete-records cascade overhaul + polish + test-hardening.

**Key accomplishments:**

1. **Penanda Kelulusan fondasi A (Phase 358, PCOMP-01..05)** — kolom `Origin` + `ProtonCompletionService` (helper bersama exam/interview/bypass) + wire GradingService (exam lulus + re-grade flip) + backfill. Fix bug exam Tahun 1/2 tak pernah "Lulus".
2. **Gate Berurutan + Cleanup A (Phase 359, PCOMP-06..10)** — `ProtonYearGate` + gate eligibility server-side CreateAssessment + gate antar-tahun + graduation gate + matikan tampilan level.
3. **Bypass Backend B (Phase 360, PBYP-01..07)** — migration `PendingProtonBypass` + 4 closure mode (CL-A/B(a)/B(b)/C) + notif `PROTON_BYPASS_READY` + coach handling + 6 endpoint.
4. **Bypass UI B (Phase 361, PBYP-08..10)** — Tab2 wizard 3-langkah + panel pending + notif deep-link + e2e UAT 6/6.
5. **Phase 362-368** — 362 PROTON CDP Polish (6 gap, no GSD dir) + 363 Audit Fix Alur PROTON T1-T10 + 364 restore e2e baseline + 365 test-harden Coach×Coachee AF-3 + 366 cascade image cleanup + 367 delete-records cascade overhaul (27 temuan) + 368 delete-records hygiene lanjutan.

**Migration:** 2 (Origin 358 + PendingProtonBypass 360). **Known deferred:** 362 no formal GSD verification (shipped per ROADMAP claim); 999.8 essay-grading suspected prod bug → backlog; NOT PUSHED.

---

## v24.0 Gambar di Soal Assessment (Shipped Local: 2026-06-09, Audited: 2026-06-09)

**Phases completed:** 6 phases (352–357), 22 plans, ~23 tasks
**Status:** SHIPPED LOCAL, NOT PUSHED — branch `ITHandoff` (ahead of origin/main baseline v23.0 `650cfeb4`; sync 5 origin/main commits before merge). v19–v23 already delivered to IT 2026-06-06.
**Audit:** `milestones/v24.0-MILESTONE-AUDIT.md` — status **passed** (25/25 REQ, 6/6 phases, integration 17/17 WIRED 0 broken, 4/4 E2E flows). Stale IMG-04 checkbox corrected during audit.

**Delivered:** Admin dapat melampirkan gambar pada soal assessment + tiap opsi jawaban (image-only ≤5MB magic-byte, alt text), tampil konsisten di 6 layar, dengan integritas file (sync Pre→Post shared-file + hapus atomic). Plus 2 addon off-theme: audit-fix Assign Coach×Coachee + standarisasi istilah tipe soal.

**Key accomplishments:**

1. **Data Foundation + Image-Only Upload (Phase 352)** — entity + migration `AddImageToPackageQuestionAndOption` (4 kolom ImagePath/ImageAlt), `FileUploadHelper.ValidateImageFile` magic-byte image-only (tolak non-gambar, ≤5MB), AssessmentConstants. IMG-04.
2. **Admin Backend Gambar — CRUD + Sync + Atomic Delete (Phase 353)** — upload/alt/replace/remove gambar soal+opsi di ManagePackageQuestions (prefill thumbnail) + sync Pre→Post shared-file + hapus file atomic ref-count (pola Phase 333, no orphan). IMG-01/02/03/05/06/07, RND-04, SYN-01/02.
3. **Render Gambar di 6 Layar (Phase 354)** — gambar tampil di StartExam/ExamSummary/Results (peserta) + _PreviewQuestion/AssessmentMonitoringDetail/EditPesertaAnswers (admin), responsive img-fluid+lazy + lightbox via `_QuestionImage`/`_ImageLightboxModal`. RND-01/02/03/05/06/07. (2 bug Playwright-caught: RuntimeBinderException @model dynamic + label-toggle radio.)
4. **Test & UAT (Phase 355)** — xUnit (FileUploadHelper, replace-deletes-old, sync) + Playwright e2e image-in-assessment upload→render end-to-end. TST-01/02.
5. **Audit Fix Assign Coach×Coachee (Phase 356, addon off-theme)** — 6 fix CoachMappingController: AF-1 (HIGH) eligibility per-unit coachee (track multi-unit), AF-3 graduate per-unit (IsActive=false+cascade), AF-2 UI guard 1-unit/batch, AF-5 notif reassign, AF-6 pesan duplikat spesifik, AF-7 batch query. Helper `CoacheeEligibilityCalculator` + xUnit. AF-4 deferred → backlog 999.5. Code review caught WR-01 (cross-unit eligibility false-negative) FIXED.
6. **Standarisasi Istilah Tipe Soal (Phase 357, addon off-theme)** — re-label "Single Answer / Multiple Answer / Essay" (override Phase 305) di semua surface; `QuestionTypeLabels.cs` single-source penuh + hapus dead code TrueFalse. DB enum tetap (no-migration). LBL-02.

**Migration:** 1 (Phase 352 `AddImageToPackageQuestionAndOption`). Phases 353-357 = 0 migration.

**Known deferred (non-blocking, tracked):** AF-4 reactivate-window → 999.5; nyquist VALIDATION frontmatter not flipped 353/356/357 (tests green); RND-02 ExamSummary live-verified (354) not in 355 Playwright; backlog 999.3/999.4/999.5. PDF panduan regen = manual user.

---

## v23.0 CMP/Records Search & Filter Consistency Audit (Shipped Local: 2026-06-06, Audited: 2026-06-06)

**Phases completed:** 2 phase (350, 351), 7 plan (350:3, 351:4)
**Status:** SHIPPED LOCAL, push pending IT availability (bundled v19.0+v20.0+v21.0+v22.0+v23.0; v23.0 leg `6c3638b6`..HEAD ~52 commit; **0 migration** — pure fix/view/test)
**Audit:** `milestones/v23.0-MILESTONE-AUDIT.md` — status **passed** (7/7 REQ SF-01..07, 2/2 phase, integration 7/7 WIRED 0 broken, 2 E2E flow utuh). Nyquist partial (artifact-only: VALIDATION.md draft, Wave 0 spec hijau). Origin: backlog 999.2 (bug "ojt v14.2"→0 worker Team View).

**Delivered:** Konsistensi search & filter lintas 3 surface CMP/Records (My Records, Team View, Worker Detail). Team View search kini mencakup judul assessment (fix bug 999.2 di level predikat, badge per-worker D-07 utuh) + dropdown "Lingkup" jujur + export WYSIWYG. Worker Detail dapat feedback 0-match (counter aria-live + empty-state) + filter Kategori dari record aktual (bukan master, tampung free-text/legacy). My Records dapat paritas filter Kategori+Tipe + back-nav "Back to Team View" mengaktifkan tab Team View dengan restore filter.

**Key accomplishments:**

1. **Team View Server-Side Search Scope + Export Parity (Phase 350, SF-01/02/06)** — `WorkerDataService.GetWorkersInSection` tambah predikat `AssessmentSessions.Any(a => a.Title.Contains(search))` di-OR ke path Training/Keduanya (fix 999.2, badge count per-worker D-07 invariant terjaga). Dropdown "Lingkup" relabel "Judul Kegiatan" + placeholder jujur sebut assessment. Export Assessment WYSIWYG (Category-narrow + archived-drop, non-empty saat search judul assessment). 4 xUnit Fact + Playwright cmp-records-350 2/2.

2. **Worker Detail + Cross-Surface Filter Consistency (Phase 351, SF-03/04/05/07)** — SF-03: `#wdRecordCounter` aria-live "Menampilkan X dari Y" + `#workerDetailEmptyState` ("Tidak ada hasil untuk filter ini.") saat 0-match. SF-04: helper `BuildActualCategories` (distinct-actual unifiedRecords.Kategori) + `ViewBag.ActualCategoriesJson` ganti master di Worker Detail + My Records. SF-05: My Records filter Kategori+Tipe parity (id `myCategoryFilter`/`myTypeFilter` hindari duplicate-id Team View) + `data-category`. SF-07: hash→tab activator (`#team` → `getOrCreateInstance(tab-team).show()`) + sessionStorage restore 9 filter. 3 xUnit + Playwright cmp-records-351 5/5 + regression 346/350 hijau.

**Known deferred (non-blocking, tech debt):**

- Phase 350 VERIFICATION `human_needed`: XLSX export content (archived vs current per-Category) belum di-eyeball lokal:5277; kode + Playwright href/counter SF-06 verified.
- Phase 351 code review 3 INFO opsional (data-type konvensi 2 surface, deserialize null-coalesce defensif, comparer culture).
- Nyquist artifact-only partial: VALIDATION.md 350/351 frontmatter draft tak di-update post-exec (Wave 0 Playwright spec sudah hijau).
- 19 debug session historis (pre-v23.0, cross-milestone) belum di-resolve.

---

## v22.0 CMP-06 + Assessment/Monitoring Audit Fixes (Shipped Local: 2026-06-05, Audited: 2026-06-05)

**Phases completed:** 5 phase (345, 346, 347, 348, 349), 24 plan (345:4, 346:6, 347:4, 348:5, 349:5)
**Status:** SHIPPED LOCAL, push pending IT availability (bundled v19.0+v20.0+v21.0+v22.0; v22.0 leg `47eb8828`..HEAD ~127 commit; **0 migration** — pure fix/polish)
**Audit:** `milestones/v22.0-MILESTONE-AUDIT.md` — status **passed** (60/60 REQ, 5/5 phase, integration 9/9 E2E flow 0 broken, label "Menunggu Penilaian" konsisten lintas 5 phase via konstanta). CMP06R `human_needed` di-resolve 2026-06-05 (seed `pending345` Completed+IsPassed=NULL + Playwright MCP 2 surface badge amber `bg-warning` rgb(255,193,7); PDF CMP06R-03 env-blocked lokal QuestPDF 204 [Phase 327 known] tapi code-verified — bukan defect).

**Delivered:** Tampilan jujur "Menunggu Penilaian" (essay pending-grade) di seluruh permukaan rekam-jejak assessment (CMP/Records, RecordsWorkerDetail, UserAssessmentHistory, Excel, PDF, Monitoring, ManageAssessment) menggantikan "Failed" merah / "Completed" abu / sel kosong palsu — dengan passRate & averageScore mengeluarkan sesi pending dari denominator. Plus 2-audit sweep ManageAssessment + Assessment Monitoring (Pre-Post LinkedGroupId correctness, Tab2 pagination/empty-state, i18n + a11y chevron + 7-kartu summary + exclude-Cancelled progress + search-by-category).

**Key accomplishments:**

1. **CMP-06 Residual Fix (Phase 345, CMP06R-01..05)** — Sesi `Completed+IsPassed=NULL` (essay submit belum dinilai HC) kini tampil badge amber "Menunggu Penilaian" di RecordsWorkerDetail + Records + UserAssessmentHistory + Excel + PDF (Orange.Darken2), ganti "Failed" merah / "Completed" abu / sel kosong palsu. `ComputeHistoryStats` static helper: passRate + averageScore exclude pending (graded-only denominator), all-pending → "Belum ada penilaian", indikator "Menunggu Penilaian: N". 7 [Fact] xUnit + Playwright 3 surface + seed SEED_WORKFLOW snapshot/restore.

2. **CMP/Records Detail + Search Logic (Phase 346, REC-01..09)** — Team View search scope (Nama / Training / Keduanya) post-load filter (badge count per-worker utuh, REC-06 D-07), training-detail modal, un-gated "Lihat Hasil", `IsResultsAuthorized` static auth helper (Results/Certificate/CertificatePdf, REC-04), REC-07 Tab3 History pending badge via `GetUnifiedRecords`/`GetAllWorkersHistory` WHERE +PendingGrading, inverted date-range hint, header "Assessment Lulus".

3. **CMP/Records i18n + a11y Polish (Phase 347, POL-01..10)** — Bahasa Indonesia (Lulus/Tidak Lulus/Nilai/Jabatan/Semua*) + a11y (modal `role="dialog"`+aria-labelledby+btn-close "Tutup", filter `<label for=>`, grid `col-sm-6`, pagination `aria-current`, reset `type="button"`) + `records.css` DRY union via `_Layout RenderSectionAsync Styles` + `@section`. 76/76 test.

4. **ManageAssessment + Monitoring MED Fix (Phase 348, MAM-01..13)** — Pre-Post RegenerateToken/Export/PDF/badge sadar LinkedGroupId (PostTest beda-tanggal tak ke-miss); essay-pending "Menunggu Penilaian" jujur di Monitoring Detail + tak inflate passRate (root cause `ExecuteUpdateAsync` bypass change-tracker → reload status); Tab2 empty-state + HTMX pagination + hx-post delete preserve filter; badge GroupStatus + dropdown data-driven (Proton phantom hilang) + tooltip jujur + reshuffle scoped. `DeriveUserStatus`/`IsTrainingInitialState` static TDD. 98/98 xUnit + Playwright 9 + human-verify APPROVED.

5. **ManageAssessment + Monitoring LOW Polish (Phase 349, MAP-01..23)** — i18n Monitoring Detail/NIP, a11y chevron-rotate CSS `[aria-expanded]` + aria-label + drop ARIA nested-interactive, empty-state filter-aware + "Reset Semua Filter" + Tab3 0-match aria-live + counter "Menampilkan X dari Y" + skeleton kolom-match, **7-kartu summary Monitoring Detail (invariant Total = jumlah 6 kartu, +Abandoned +Menunggu Penilaian)**, TotalCount exclude Cancelled (progress bisa 100%), Pre-Post Regenerate dropdown, search-by-Category, drop dead-var/param. 105/105 xUnit + Playwright UAT 5 SC + human-verify APPROVED.

**Patterns established / reused (cross-phase):**

- **`AssessmentConstants.AssessmentStatus.PendingGrading` ("Menunggu Penilaian") sebagai single source of truth** lintas 11+ surface (service→controller→view→PDF→Excel) — integration check konfirmasi konsisten, 3 literal-drift LOW non-breaking.
- **Exclude-pending denominator** konsisten di 3 jalur (CMP/Records passRate, group PassedCount via `IsPassed ?? false`, Detail MenungguPenilaianCount).
- **Seed-workflow PDF/visual verify** (345 + closure): seed temporary prefix + snapshot + Playwright + DELETE-cleanup + SEED_JOURNAL untuk surface yang tak bisa di-assert otomatis.

**Tech debt / deferred at close (acknowledged):**

- Push batch v22.0 leg ~127 commit (full bundle v19+v20+v21+v22) pending IT availability + verifikasi lokal lengkap.
- **CMP06R-03 PDF** env-blocked lokal (QuestPDF/SkiaSharp return 204, Phase 327 known) — code-verified, perlu render-confirm di env QuestPDF normal (Dev/Prod).
- **348/349 tanpa VERIFICATION.md** (diverifikasi human-verify checkpoint + UAT + APPROVED, bukan gsd-verifier) — substantif satisfied.
- **Nyquist VALIDATION.md** 346/347/348 missing (pure polish, logic-bearing minim) — accepted.
- Backlog: **Phase 999.2** CMP/Records search extend ke Assessment title (bug user UAT, REC-06 D-07 scope) + 999.1 Realtime SignalR.
- 3 literal-string drift LOW (`DeriveUserStatus` L2682, `IsMenungguPenilaian` L2771, sub-row CancelledCount) — refactor opsional ke konstanta.

---

## v21.0 ManageOrganization Overhaul + Level Label CRUD (Shipped Local: 2026-06-04, Audited: 2026-06-04)

**Phases completed:** 5 phase (340, 341, 342, 343, 344), 16 plan (340:3, 341:3, 342:3, 343:4, 344:3)
**Status:** SHIPPED LOCAL, push pending IT availability (bundled v19.0+v20.0+v21.0 + audit/remediation = ~74 commit batch v21.0 leg, e31db3c5..c820f49d; 1 migration `20260603012335_AddOrganizationLevelLabel`)
**Audit:** `milestones/v21.0-MILESTONE-AUDIT.md` — status passed (26/26 REQ, 5/5 phase, integration 5/5 cross-phase links wired, 0 broken/orphaned). Initial `gaps_found` (Phase 340 missing VERIFICATION.md + empty SUMMARY frontmatter — artifact-only, 0 feature gaps) remediated 2026-06-04.

**Delivered:** Configurable organization tier labels — HC/Admin can rename "Bagian"/"Unit"/"Sub-unit" tiers app-wide via a CRUD page, with the new label propagating to the org tree + 26 integrated views in real time. Plus a hardened ManageOrganization tree (pre-order DFS dropdown, per-parent dup-name, cascade-impact preview, dynamic modal titles) and a full test/UAT layer.

**Key accomplishments:**

1. **Foundation (Phase 340, ORG-LABEL-01/02/03/07)** — `OrganizationLevelLabel` model + EF migration `20260603012335` + idempotent `SeedData.SeedOrganizationLevelLabelsAsync` (0=Bagian/1=Unit/2=Sub-unit, permanent+prod-required). `IOrgLabelService`/`OrgLabelService` (Singleton IMemoryCache no-TTL, manual invalidate, fallback `"Level {N}"`, auto-detect max level+buffer) + `GET /Admin/GetLevelLabels` JSON endpoint. 20/20 tests.

2. **Label CRUD Page (Phase 341, ORG-LABEL-04/05/06)** — `/Admin/ManageOrgLevelLabels` (`[Authorize(Roles="Admin, HC")]`) + 4 actions CRUD with server validation (required/trim/≤50/unique), audit log per mutation, delete-highest-only guard. 38 tests + UAT 10/10 + Coach 403.

3. **ManageOrganization Tree Fixes (Phase 342, ORG-TREE-01..10)** — pre-order DFS dropdown, per-parent dup-name (ORG-TREE-02), inactive-visible "(nonaktif)", data-name escape, color palette L3-5, path breadcrumb, `PreviewEditCascade` impact modal (ORG-TREE-07, 6 [Fact] preview==actual), legend + dynamic modal title + tier badge from `OrgLabelService`. 44 tests + Playwright 10/10.

4. **App-wide Integration (Phase 343, ORG-INTEG-01/02)** — global `@inject IOrgLabelService OrgLabels` in `_ViewImports.cshtml` + 110 `@OrgLabels.GetLabel(N)` calls across 26 views (CMP/CDP/ProtonData/Admin/Worker), replacing hardcoded tier strings. ORG-INTEG-02 = documented audit-only SKIP (controller display strings = Excel headers/audit-log, by design). SC2 Playwright live-verified (rename propagates 3 pages).

5. **Test + UAT (Phase 344, TEST-01..06 + ORG-INTEG-03)** — xUnit 52/52 (incl. disposable real-SQL-Server `OrgLabelMigrationIntegrationTests` TEST-05, `OrgTreePreOrder` DFS helper TEST-03, 5 reflection permission [Fact] TEST-02) + Playwright `manage-org-label.spec.ts` 7 scenarios + manual UAT 5/5 (cascade count SQL-cross-checked). Verifier PASS 5/5. **TDD caught a real null-key bug in the planned DFS helper; adversarial reviewer caught 6 critical the standard checker missed.**

**Patterns established (cross-phase reuse):**

- Configurable display labels via cached service + global `@inject` (340→343): one source of truth, real-time propagation via cache-invalidate-on-mutation.
- Client-side label hydration (342): tree JS consumes `GET /Admin/GetLevelLabels` rather than server-injecting, keeping the endpoint as the single label API.
- Adversarial plan review (344): independent skeptic agent catches false-green/silent-pass traps the structural checker misses (helper-vs-source fidelity, tautological fixtures, matrix-setup coupling, wave races).

**Tech debt at close (acknowledged, defer):**

- Push batch ~74 commit lokal v21.0 leg (full bundle v19+v20+v21) pending IT availability.
- Phase 341 Nyquist `*-VALIDATION.md` `nyquist_compliant: false` (PARTIAL) — phase has 38 tests + UAT 10/10; optional `/gsd-validate-phase 341` to flip green (non-blocking).
- STATE.md frontmatter `milestone: v16.0` stale (parallel-session drift); body reflects v22.0 active.
- v22.0 (CMP-06 Residual Fix, Phases 345-347) already started in a parallel session — NOT part of v21.0.

## v20.0 CMP Records Overhaul + Cilacap UX/Restore (Shipped Local: 2026-06-02, Audited: 2026-06-02)

**Phases completed:** 4 phase (336, 337, 338, 339), 10 plan (Phase 337 has 3 wave plans, Phase 338 has 5 wave plans, Phase 336 + 339 single plan each)
**Status:** SHIPPED LOCAL, push pending IT availability (bundled v19.0+v20.0 = ~155 commit batch + 0 migration di Phase 339)
**Audit:** `milestones/v20.0-MILESTONE-AUDIT.md` — status passed (39/39 REQ + 4/4 phase + integration COHERENT post-re-audit setelah Phase 339 closed 3 partial)

**Delivered:** CMP/Records overhaul (Approach C: filter silent-fail fix + data integrity + race-safe AJAX + a11y ARIA + ViewModel refactor + SQL push-down + pagination) + 6 Cilacap admin UX gap closure + PreTest OJT GAST Cilacap data loss investigation (root cause IT operational redeploy, BUKAN bug) + restore strategy A locked + Excel BulkBackfill endpoint + guardrail backup hook + DEV_WORKFLOW.md SOP + Phase 339 gap closure (3 partial REQ tertutup via UI wiring + regex validator).

**Key accomplishments:**

1. **CMP Records Full Overhaul (Phase 337, CMP-01..26)** — 26 REQ in 3 wave: Wave 1 filter + data integrity (CMP-01..11) hapus guard `WorkerDataService.cs:391` + ganti `.Contains()` → `string.Equals OrdinalIgnoreCase` + Sertifikat column rendered + AttemptNumber null-safe. Wave 2 UX + quality (CMP-12..23) AbortController AJAX + tab parity + dead `data-*` removal + keyboard-navigable rows + ARIA tab roles + `CMPRecordsViewModel.cs` refactor (single roleLevel source). Wave 3 arch (CMP-24..26) SQL push-down `GetAllWorkersHistory` 5 optional params + `PaginationHelper.Calculate` Team View.

2. **Cilacap UX 5 Gap Closure (Phase 338 W1-3, CIL-01..05)** — Filter default badge counter Closed (CIL-01) + search aggregation include Closed (CIL-02) + history row drill-down ke `/CMP/Results/{sessionId}` (CIL-03) + admin/HC banner di `/CMP/Assessment` role-gated (CIL-04) + **CIL-05 HIGH PRIORITY**: `ExcelExportHelper.AddDetailPerSoalSheet` + `AddElemenTeknisSheet` di-call `ExportAssessmentResults` L4296-4297.

3. **PreTest Cilacap Investigation (Phase 336, REST-01..03)** — Investigation-only, ZERO source code drift. Schema Evolution Timeline 7 commit ADD-ONLY + Migration Candidate Analysis 13/13 NO CULPRIT. Root cause: IT operational redeploy code+DB tanpa backup (path F-variant), BUKAN bug aplikasi. Strategy A locked (re-import via Excel BulkBackfill). Naming convention spec `{Stage} Test {Track} {Lokasi}` final.

4. **REST-04 Restore Execute + CIL-06 BulkExportPdf (Phase 338-04, REST-04 + CIL-06)** — `BulkExportPdf` endpoint L4499 + `GeneratePerPesertaPdf` L4558 QuestPDF helper. `BulkBackfillAssessment` L733 atomic transaction + AuditLog per row. _Execute Cilacap PreTest 30 Mar 2026 data restore di Dev DB pending IT promo + admin trigger_ (code production-ready).

5. **Guardrail Backup + Naming (Phase 338-05, REST-05..07)** — `scripts/backup-dev-pre-migration.ps1` SQL Server `.bak` hook untuk `AssessmentSessions` + `AssessmentAttemptHistory` + `PackageUserResponses` (REST-05). `docs/templates/DB_HANDOFF_IT.template.md` template komunikasi IT. `TryAutoDetectCounterpartGroup` L6599 auto-pair LinkedGroupId Pre/Post (REST-06). `docs/DEV_WORKFLOW.md` L142+ Pre-Deploy Backup SOP section (REST-07).

6. **Phase 339 Gap Closure** — `/gsd-audit-milestone v20.0` (2026-06-02 morning) identified 3 partial REQ. Phase 339 surgical fix: T1 `_AssessmentGroupsTab.cshtml:283-291` dropdown-item BulkExportPdf + divider + BulkBackfill (CIL-06 + REST-04 dropdown variant). T2 `Views/Admin/Index.cshtml:274-289` Admin-only card BulkBackfill di Section D System (REST-04 primary nav). T3 `AssessmentAdminController.cs:847-855` conditional regex validator + `CreateAssessment.cshtml:193` `<span asp-validation-for="Title">` (REST-06). **D-03 entity safety:** `Models/AssessmentSession.cs:13` UNTOUCHED. Playwright MCP UAT 5/6 PASS + 1 N/A (HC role no-creds, code-proof).

**Patterns established (cross-phase reuse):**

- Conditional validator guard parity (Phase 338-05 auto-pair → Phase 339 regex validator: same `AssessmentTypeInput != "PrePostTest"` guard)
- Entity-immutable Validation (Phase 339 D-03: server-side controller validation, NOT entity data annotation, untuk feature scoped ke subset usage)
- Admin-only nav gate match endpoint (Phase 339 D-02: `@if (User.IsInRole("Admin"))` standalone gate match `[Authorize(Roles="Admin")]` — DISTINCT dari `|| HC` variant)
- Wave 4 endpoint + Wave 5 wiring split (Phase 338 → 339 reorganization: endpoint logic shipped early, UI wiring afterward via dedicated gap closure phase)

**Tech debt at close (acknowledged, defer):**

- Push batch ~155 commit lokal v19.0+v20.0 pending IT availability
- v16.0+v17.0+v18.0 MILESTONES.md entries belum ditambah (pre-existing tech debt — backlog housekeeping non-blocker)
- Nyquist `*-VALIDATION.md` MISSING semua 4 phase v20.0 (defer batch `/gsd-validate-phase N`)
- Phase 337 6 item live UAT 6-pillar `/CMP/Records` + Phase 338 7 item live UAT termasuk REST-04 KRITIS Cilacap data restore execute — pending Dev environment post-IT promo
- Pre-existing carry-over 8 backlog: EPRV-01 + Phase 235/247/281/285/293/297/298/303

---

## v19.0 Portal HC Bug Fixes (Cascade Hardening) (Shipped Local: 2026-05-28, Audited: 2026-05-29)

**Phases completed:** 11 phases (325-335), 11 plans (multi-plan: Phase 325 has 5, Phase 327 has 8; others 1)
**Status:** SHIPPED LOCAL, push pending IT availability (bundled v18.0 Phase 324 + v19.0 = ~78 commit batch + 1 migration `ChangeValidUntilToDateOnly`)
**Audit:** `milestones/v19.0-MILESTONE-AUDIT.md` — status passed (16/16 REQ + 88/88 must-haves verified + integration 7 patterns COHERENT)

**Delivered:** Sertifikat ecosystem audit closure — 6 spec-driven security/validator/timezone fixes (Phase 325-327) + 7 audit-driven cascade hardening fixes (Phase 329-335) following Phase 328 cascade audit sweep recommendation.

**Key accomplishments:**

1. **Security hardening (Phase 325, SEC-01..03 + FOUNDATION + CLOSURE)** — Path traversal sanitize via `Path.GetFileName` (SEC-01), magic byte validation `MatchesMagicByte` 4-key Dictionary + enforce di Add/Edit Training endpoint (SEC-02), DeleteAssessment renewal pre-check pre-tx (SEC-03 gold standard pattern), plus xUnit foundation `HcPortal.Tests/` 10 baseline tests.
2. **Validator hardening (Phase 326, VAL-01/02)** — DAG cycle prevention validator (RenewsSessionId tidak boleh introduce cycle) + Permanent+ValidUntil contradiction reject validator. EditTrainingRecordViewModel L67-69 separate fields + Razor ValidUntil span.
3. **Timezone DateOnly refactor (Phase 327, TZ-01)** — `ValidUntil DateTime` → `DateOnly` migration `ChangeValidUntilToDateOnly` 8-plan refactor (entity, migration, ViewModel, Razor, status derivation, EF query, tests, smoke). Permanent tz drift elimination. +8 CertificateStatus tests = 18 baseline carry-forward.
4. **Cascade audit sweep (Phase 328, CSCD-AUDIT)** — Audit-only phase, zero source code. 14 mutator + 5 preview endpoint diaudit. Classification: 8 HIGH + 5 MED + 0 LOW + 1 NONE (DeleteAssessment gold standard). 7 next-phase fix proposal yang jadi Phase 329-335.
5. **Cascade hardening 7 endpoint (Phase 329-335, CSCD-01..07)** — Renewal pre-check pattern + DbUpdateException catch + file-capture-before-tx + tx wrap + D6 info-leak fix (NO `+ ex.Message`) di: DeleteAssessmentGroup+PrePostGroup (329), MED Bundle DeleteCategory/Package/Question/OrgUnit/NotifService (330), DeleteTraining+ManualAssessment atomicity (331), DeleteBagian file atomicity (332), DeleteCoachingSession file atomicity (333), DeleteKompetensi orphan evidence + D6 info-leak (334), **DeleteWorker FINAL HIGH triple-fix D2+D5+D7** (335 — MILESTONE CLOSE).

**Patterns established (cross-phase reuse, integration verified COHERENT):**

- Renewal pre-check pre-tx (Phase 325 P05 → 329/331/335)
- DbUpdateException catch + friendly TempData (Phase 329 → 330-335)
- File capture-before-tx + delete post-commit warn-only (Phase 331 → 332-335)
- D6 info-leak fix NO `+ ex.Message` (Phase 334 CRITICAL → 335)
- xUnit baseline 10→18 carry-forward (Phase 325 Plan 01 + Phase 327 = consistent 18/18 PASS test count seluruh phase 326-335)

**Tech debt at close:**

- Push batch ~78 commit pending IT availability (`docs/IT_NOTIFY.md` ready deliver)
- v16.0 MILESTONES.md entry belum ditambah (backlog housekeeping non-blocker)
- Nyquist `*-VALIDATION.md` missing semua phase (nyquist_validation likely disabled by design)
- SUMMARY.md frontmatter `requirements_completed` field tidak ada di 11/11 SUMMARY (workflow §5c expectation — non-blocker, REQ matrix di REQUIREMENTS + VERIFICATION sudah cover)

**Known deferred (carry-over ke v20.0):** 8 carry-over (v13-v15) + Phase 281/285 (v11.2 paused) + 2 todo baru 2026-05-29 (`001-gap-ux-assessment-monitoring`, `002-restore-pretest-ojt-gast-cilacap`).

---

## v18.0 Cascade Delete Hardening + Duplicate TR Fix (Shipped: 2026-05-29)

**Phases completed:** 2 phases (323-324), 5 plans, ~15 tasks
**Status:** Phase 323 PUSHED tag `v18.0-p323-complete`; Phase 324 SHIPPED LOCAL (bundled push dengan v19.0 batch pending IT availability)
**Audit:** `milestones/v18.0-MILESTONE-AUDIT.md` — status passed (6/7 shipped + 1 deferred-superseded DUPL-02b)

**Delivered:** Cascade hardening + duplicate TR fix — `AssessmentEditLogs` cascade di 3 endpoint Delete (Phase 323) + hapus auto-create `TrainingRecord` di 3 lokasi production code (Phase 324), plus SQL cleanup script + IT handoff HTML untuk legacy data purge.

**Key accomplishments:**

1. **CASCADE-01 (Phase 323)** — `DeleteAssessment` / `DeleteAssessmentGroup` / `DeletePrePostGroup` di `Controllers/AssessmentAdminController.cs` tambah `RemoveRange(AssessmentEditLogs)` sebelum cascade existing. Session dengan edit log history bisa dihapus tanpa FK Restrict 500. Tag `v18.0-p323-complete` PUSHED.
2. **DUPL-01 (Phase 324)** — Block auto-create `TrainingRecord` dihapus di 3 lokasi: `GradingService.GradeAndCompleteAsync` L255-285, `AssessmentAdminController.FinalizeEssayGrading` L3404-3421, `GradingService.RegradeAfterEditAsync` L483-567. `AssessmentSession` jadi sole source-of-truth row "Assessment Online" di `/CMP/Records`.
3. **DUPL-02a (Phase 324)** — Playwright E2E spec `tests/e2e/Phase324_NoDuplicateTrainingRecord.spec.ts` 7-scenario skeleton (S1+S2 static-green, S3-S7 skip). Live runtime S1+S2 override → browser MCP equivalent UAT proof.
4. **DUPL-03 (Phase 324)** — SQL cleanup script `docs/sql/cleanup-2026-05-26-trainingrecord-duplicates.sql` idempotent dengan XACT_ABORT + safety cap 5000. Pre-count 18 → post-count 0 di DB lokal. SEED_JOURNAL.md entry status `cleaned`.
5. **DUPL-04 (Phase 324)** — IT handoff HTML `docs/DB_HANDOFF_IT_2026-05-26.html` (fork dari template 2026-05-13) — embedded SQL + step ordering (deploy code dulu → cleanup data) + rollback plan.
6. **DUPL-05 (Phase 324)** — Pre/post-fix screenshots `docs/screenshots/phase324/before-fix.png` + `after-fix.png` + cross-grep audit 0 hit `TrainingRecords.(Add|AddAsync|AddRange)` di scope file.

**Known deferred:** DUPL-02b S3-S7 implementation **deferred-superseded** — Phase 325 pivoted ke v19.0 security hardening, tidak pick up. Skeleton `test.skip(true, "...Phase 325...")` jadi historical placeholder. Re-promote ke v20.0 jika perlu full coverage.

**Tech debt at close:** Phase 324 push pending IT availability (bundled dengan v19.0 batch ~78 commit + 1 migration `ChangeValidUntilToDateOnly` Phase 327).

---

## v17.0 Assessment Admin Power Tools (Shipped: 2026-05-22)

**Phases completed:** 3 phases (320 + 321 + 322), 11 plans, 84 commits

**Delivered:** 3 power tool admin assessment — Excel export per-peserta dengan spider chart PNG (SkiaSharp), edit jawaban peserta dengan auto-regrade + cert cascade + audit dual-write + SignalR real-time, dan rollback filter scope ke per-tab native (eliminate 3 bug Phase 311 shared shell filter).

**Key accomplishments:**

1. **Phase 320 — Excel Export Per-Peserta** (EXP-01..08): Refactor `ExportAssessmentResults` → 1 sheet "Summary" (rename dari "Results") + N sheet per peserta dengan spider chart PNG 500×500 (SkiaSharp), `Helpers/SheetNameSanitizer.cs` format `{NIP}_{FullName}`, paralel `Task.WhenAll`. Variant A Online + Variant B Manual Entry. Status filter Completed + Abandoned only. HC permission full sama Admin. Performance < 30s untuk 50 peserta.
2. **Phase 321 — Edit Jawaban Peserta** (EDIT-01..13): Halaman dedicated `/AssessmentAdmin/EditPesertaAnswers/{sessionId}` dengan form MC/MA per soal + reason dropdown 5 preset + concurrency token + transaction scope + dry-run `PreviewEditScore`. Auto-cascade Pass↔Fail flip (cabut/generate NomorSertifikat retry 3x + TrainingRecord status). Audit dual-write `AuditLog` + tabel baru `AssessmentEditLog`. SignalR signal real-time `workerAnswerEdited`. Activity Log tab "Edit History". Dropdown ⋮ hybrid layout.
3. **Phase 322 — Filter Scope Per Tab** (FILTER-01..03): Rollback Phase 311 Plan 02 shared filter shell ke per-tab native filter. Eliminate Bug 1 (double filter Tab 1) + Bug 2 (cross-tab contamination semantic mismatch via D-21 Strategy D Hybrid URL query string) + Bug 3 bonus (pagination preserve filter via hx-include pattern). Sub-tab Riwayat Training filter NEW.

**Critical bugs discovered + fixed during execution:**

- **`6ecb7a50` Phase 322-06:** ViewBag string null → JSON null → URL-encoded `"null"` literal di filter param.
- **`773c970c` Phase 322-05 CRITICAL:** HTMX `hx-vals` attribute inheritance gotcha (wrapper ancestor hx-vals override descendant form data) → migrate ke URL query string per-wrapper. Key learning didokumentasikan di ROADMAP closure.

**Post-shipping discovery (2026-05-22) + 3 follow-up fix commit (2026-05-23):**

- Browser visual verification reveal filter Tab 2 (Input Records) + Tab 3 (History) **invisible** meski element ada di DOM. Root cause: CSS dead-code Phase 311.1 (commit `b17292f7`, 2026-05-07) tidak dihapus saat Phase 322 rollback shell shared filter.
- `b0b4049b fix(manage-assessment)`: hoist `_HistoryTab.cshtml` filter row di luar `@if/@else` empty-state.
- `3cdccfb4 fix(css)`: hapus dead Phase 311.1 hide-rules `wwwroot/css/site.css:93-122`.
- `13046757 docs(phase-322)`: UAT.md amend — Step 4+7 false-positive flag + Post-Verification Discovery section.
- Spec gap exposed: `tests/e2e/manage-assessment-filter.spec.ts:118,181` assert `state: 'attached'` bukan `toBeVisible()`. Added `FUTURE-SPEC-01` ke deferred backlog.

**Tags:** `v17.0-p320-complete` + `v17.0-p322-complete` + `v17.0` (milestone close, this archive)

**Verification artifacts (per phase):**

- Phase 320: `320-UAT.md` (8-step manual UAT + Playwright 4-test PASS) + `320-RESEARCH.md`
- Phase 321: `321-VALIDATION.md` (Nyquist) + `321-UI-SPEC.md` + `321-SECURITY.md`
- Phase 322: `322-UAT.md` (12-step manual UAT — 11 PASS + 1 N/A) + `322-VALIDATION.md` (Nyquist 8/8) + `322-SECURITY.md` (threats:0) + post-discovery section

**Known Deferred (carry-over to v18.0+):**

- EPRV-01 (Preview Essay rubrik/jawaban) — v15.0 carry-over, menunggu user verifikasi save/load Rubrik
- FUTURE-KUNCI-01, FUTURE-NOTIFY-01, FUTURE-APPROVAL-01, FUTURE-BULK-01, FUTURE-UNDO-01 — v17.0 deferred per spec
- FUTURE-SPEC-01 — strengthen Playwright assertion `toBeVisible()` di filter spec
- v14.0/v15.0 carry-over: Phase 303 UAT, Phase 235 UAT, Phase 247 UAT 2 TODO, research gaps Phase 297/298, blocker Phase 293, v11.2 paused (Phase 281 + 285)
- v16.0 housekeeping — milestone shipped 2026-05-12 tapi belum ada entry di MILESTONES.md log

**Tag:** `v17.0` (created 2026-05-23)

---

## v15.0 Audit Findings 27 April 2026 (Shipped: 2026-05-11)

**Phases completed:** 12 phases (304-314 + 313.1), 28 plans, 53 tasks

**Delivered:** Tindak lanjut 11 temuan audit 27 April + 4 temuan audit 29 April pada flow assessment & login PortalHC_KPB — bug-fix + UX enhancements + 1 perf improvement (HTMX lazy load + DB index opportunistic). Coverage 100% (15/15 temuan).

**Key accomplishments:**

1. **Phase 304 — UI Label Polish (Login + WIB)** (AUTH-01, WIZ-02, WIZ-03): Eye-icon toggle password Login + label "(WIB)" konsisten Step 3 + suffix "WIB" Step 4 wizard.
2. **Phase 305 — Question Type Naming Clarity** (LBL-01): Helper `QuestionTypeLabels` + 5 view edit + 8 file dokumentasi context-aware. UI rename Single Choice / Multiple Answers tanpa ubah enum DB.
3. **Phase 306 — Score Editable per Question Type** (QSCR-01): Score input MC/MA/Essay enabled (range 1-100), modal warning edit score yang mempengaruhi sesi Completed, AuditLog `EditQuestion-ScoreChange` + `CreateQuestion-CustomScore`. UAT 10/10 PASS.
4. **Phase 307 — Selected Participants Inline View** (WIZ-01): Panel "Peserta Terpilih" real-time Step 2 + helper `renderSelectedParticipants` reused Step 4. Performance 50+ peserta <200ms via DocumentFragment + debounce.
5. **Phase 308 — PrePost Wizard Validation Fix** (WIZ-04): JS Status='Upcoming' + server `ModelState.Remove("Status")` conditional — wizard PrePost tidak reset Step 1. UAT 4-step PASS.
6. **Phase 309 — Worker Cert Defensive + Submitted Status** (WCRT-01, SUB-01): Try-catch + structured log + null-safe `CMPController.Certificate` + helper `IsAssessmentSubmitted` (Completed OR `Menunggu Penilaian`) di 3 lokasi. GradingService `PendingGrading` constant refactor opportunistic.
7. **Phase 310 — Essay Finalize Idempotency** (ESCG-01): `FinalizeEssayGrading` rowsAffected branching D-03/D-04 BI message + UI gate "Create Sertifikasi" disabled saat finalized + dedup `NotifyIfGroupCompleted`. Path A Playwright MCP walkthrough PASS.
8. **Phase 311 — ManageAssessment Performance** (PERF-01): REFRAMED — backend bukan bottleneck, proxy wifi kantor adalah. HTMX lazy load (initial doc <14 KB, ≥50% reduction baseline ~1.4 menit) + AsNoTracking + 2 EF migration index + IMemoryCache Categories TTL 5min + 3 invalidation hooks.
9. **Phase 312 — Admin Full-Delete Assessment Room** (DEL-01): Role tier guard — Admin override status guard, HC blocked Completed/with-response. UI conditional render + AuditLog dengan Status & ResponseCount. Smoke 5 skenario PASS.
10. **Phase 313 + 313.1 — Block Manual Submit Saat Waktu Habis** (TMR-01, F-313-UAT-01): LIFE-03 jadi 2-tier branching (manual reject tanpa grace, auto reject setelah 2-min grace) + ExamSummary 3-branch button + Phase 313.1 SQL seed extend self-contained + Playwright FLOW 313 helper `exam313.ts` 7 tests PASS dalam 28.3s.
11. **Phase 314 — Fix Regenerate Token Upcoming** (TKN-01): Investigative bug fix — repro → root cause → patch backend defensive + frontend error propagation server JSON ke `alert()` (line 396-419 + 981-1009). Smoke 3 skenario PASS.

**Known Deferred:**

- **EPRV-01** (Preview Essay rubrik/jawaban) — DEFERRED, butuh user verifikasi save/load Rubrik. Due 2026-05-12.
- Carry-over dari v14.0: Phase 235 UAT (5 items), Phase 247 approval chain UAT (2 TODO), Phase 303 Coach Workload 12-langkah, research gaps Phase 297/298, blocker Phase 293.

**Tag:** v15.0 (created 2026-05-11)

---

## v14.0 Assessment Enhancement (Shipped: 2026-04-24)

**Phases completed:** 8 phases (296-303), 23 plans, 35 tasks
**Files modified:** 218 | **Insertions:** 51,734 | **Deletions:** 1,456
**Timeline:** 2026-04-06 → 2026-04-10 (implementasi inti) + iterasi polish s/d 2026-04-24
**Git range:** `c506cb13` → `3fa4049f` (206 commits)

**Delivered:** Sistem assessment berkembang penuh dari hanya Multiple Choice menjadi platform evaluasi kompetensi end-to-end — mendukung Pre-Post Test dengan gain score, 4 tipe soal baru (TF/MA/Essay/FiB) dengan grading otomatis dan manual, UI ujian responsif mobile, reporting statistik (item analysis + discrimination), akomodasi aksesibilitas (keyboard nav + extra time), dan monitoring beban coach-coachee dengan saran reassign.

**Key accomplishments:**

1. **Data Foundation + GradingService Extraction (Phase 296)** — Migrasi DB backward-compatible (QuestionType, AssessmentType, AssessmentPhase, LinkedGroupId, LinkedSessionId, HasManualGrading) dan GradingService sebagai single source of truth untuk semua grading path (SubmitExam, AkhiriUjian, AkhiriSemuaUjian). GradeFromSavedAnswers dihapus.
2. **Admin Pre-Post Test (Phase 297)** — HC dapat membuat assessment Pre-Post Test dengan jadwal & paket soal Pre/Post terpisah, copy paket same-package, monitoring grup expandable, delete group cascade, reset Pre→Post cascade, sertifikat hanya dari Post.
3. **Question Types (Phase 298)** — 4 tipe soal baru (True/False, Multiple Answer, Essay, Fill-in-the-Blank) dengan admin form, Excel import multi-tipe, worker UI sesuai tipe (radio/checkbox/textarea/text input), grading all-or-nothing MA, exact-match FiB, Essay manual grading via AssessmentMonitoringDetail.
4. **Worker Pre-Post + Comparison (Phase 299)** — Card pair Pre/Post dengan guard Post-disabled sebelum Pre completed, halaman perbandingan side-by-side, gain score `(Post-Pre)/(100-Pre)×100` dengan edge-case Pre=100.
5. **Mobile Optimization (Phase 300)** — Offcanvas drawer navigasi soal, sticky footer Prev/Next/Submit, touch target ≥48dp, timer tetap visible saat scroll, landscape mode, kompatibel dengan anti-copy Phase 280 (swipe dihapus per D-10).
6. **Advanced Reporting (Phase 301)** — Item Analysis per soal (p-value difficulty), discrimination index Kelley 27% upper/lower dengan warning n<30, distractor analysis, Gain Score Report per pekerja/elemen, Excel export (ClosedXML), Gain Score Trend chart.
7. **Accessibility WCAG Quick Wins (Phase 302)** — Skip link, keyboard navigation (arrow keys opsi, Tab antar soal), auto-focus soal pertama, ExtraTimeMinutes per assessment via SignalR real-time. A11Y-03 (screen reader) & A11Y-04 (font size) di-drop per D-18/D-19.
8. **Rasio Coach-Coachee + Balanced Mapping (Phase 303)** — Halaman Coach Workload dengan Chart.js horizontal bar (threshold coloring), 4 summary cards, tabel detail, saran reassign approve/skip AJAX, Set Threshold modal, auto-suggest coach beban terendah di assign modal, entity CoachWorkloadThreshold + 5 controller actions.

**Known Gaps / Deferred Items:**

- Phase 303 UAT 12-langkah Coach Workload — kode di-commit, human verification formal belum diapprove (paused 2026-04-10). Diacknowledge pada milestone close 2026-04-24.
- Phase 235 pending UAT: 5 items butuh human verification via browser
- Phase 247 approval chain UAT: 2 TODO (HC review + resubmit notification)
- Research gap Phase 297 Pre-Post Renewal behavior — keputusan teknis tertunda
- Research gap Phase 298 essay max character limit — belum diputuskan (nvarchar(max) vs 2000)
- Keputusan Phase 293 `GetSectionUnitsDictAsync` Level 2+ — masih hardcoded 2-level

**Key Decisions:**

- GradeFromSavedAnswers dihapus — GradingService satu-satunya source of truth
- Chart.js v4 dengan `indexAxis:'y'` untuk horizontal bar (bukan v2 horizontalBar)
- Auto-suggest coach via `data-section` attribute, tanpa server round-trip
- `coachWorkloads` dictionary di-serialize ke JS saat page load — tidak butuh AJAX endpoint terpisah
- Export endpoints re-query database independen (tidak share state dengan API endpoints) — Phase 301

---

## v11.2 Admin Platform Enhancement (Shipped: 2026-04-02)

**Phases completed:** 2 of 4 phases (282-283), 4 plans
**Timeline:** 2026-04-01 → 2026-04-02

**Delivered:** Maintenance mode dan user impersonation untuk admin portal.

**Key accomplishments:**

1. Maintenance Mode — Admin dapat mengaktifkan maintenance mode dari System Settings, non-admin diarahkan ke halaman maintenance informatif, Admin/HC tetap bisa akses
2. User Impersonation — Admin dapat melihat aplikasi sebagai role lain (HC/User) atau user spesifik, read-only mode, audit trail, auto-expire 30 menit

**Known Gaps:**

- Phase 281 (System Settings) belum dimulai — SETT-01..07 pending
- Phase 285 (Dedicated Impersonation Page) sedang executing — IMP-UI-01..03 pending
- Phase 284 (Backup & Restore) removed from milestone — BKP-01..08 deferred
- Milestone closed early by user decision untuk prioritas refactoring

---

## v9.1 UAT Coaching Proton End-to-End (Shipped: 2026-03-25)

**Phases completed:** 1 of 5 phases (257), 2 plans
**Timeline:** 2026-03-25 (1 day)

**Delivered:** Code review dan bug fix untuk coach-coachee mapping flow (MAP-01..08). Fix progression warning yang tidak trigger untuk 0 progress records.

**Key accomplishments:**

1. Code review 8 mapping requirements — list/pagination, assign, import Excel, template download, track assignment, deactivate cascade, reactivate reuse, progression warning
2. Bug fix: progression warning `prevProgressCount > 0` check — coachee tanpa progress Tahun 1 tidak lagi bisa di-assign Tahun 2 tanpa warning

**Known Gaps:**

- Phase 258-261 skipped (SIL-01..06, EVI-01..05, APR-01..07, DSH-01..06 not executed)
- Milestone closed early by user decision

---

## v8.6 Codebase Audit & Hardening (Shipped: 2026-03-24)

**Phases completed:** 5 phases, 7 plans, 5 tasks

**Key accomplishments:**

- (none recorded)

---

## v8.2 Proton Coaching Ecosystem Audit (Shipped: 2026-03-23)

**Phases completed:** 6 phases (233-238), 16 plans, 30 tasks
**Timeline:** 2026-03-22 → 2026-03-23 (2 days)
**Code changes:** 86 files changed, +17,252 / -297 lines
**Commits:** 88

**Delivered:** End-to-end audit ekosistem Proton coaching — riset 3 platform enterprise (360Learning, BetterUp, CoachHub), audit setup/execution/completion/monitoring flow, fix 24+ bug, plus differentiator enhancement (workload indicator, batch approval, bottleneck analysis).

**Key accomplishments:**

1. Riset coaching platform — Dokumen HTML perbandingan 3 platform enterprise vs Portal KPB, 20 rekomendasi 3-tier, gap analysis per 4 area Proton
2. Audit Setup Flow — Silabus delete safety (hard delete blocked jika progress aktif), guidance file management, coach-coachee mapping atomic transaction, import all-or-nothing two-pass, progression warning override
3. Audit Execution Flow — Evidence resubmit traceability (EvidencePathHistory), approval race guard first-write-wins, notification completeness (resubmit ke coach), PlanIdp coaching guidance scoped to coach's mapped coachees
4. Audit Completion — Unique constraint ProtonFinalAssessment, coaching session Edit/Delete CRUD, HistoriProton completion criteria (assessment + all deliverables), MarkMappingCompleted graduated flow
5. Audit Monitoring & Differentiator — Filter cascade bug fix, override transition validation, workload indicator badge warna, batch HC approval, bottleneck horizontal bar chart, 3 export baru
6. Gap Closure — UI wiring progression warning confirm dialog, session Edit/Delete buttons, 3 export link buttons

**Known Gaps (accepted as tech debt):**

- v8.0 audit: AINT-02/03 deferred (tab-switch detection), 10 orphaned requirements from removed phases (COMP-01-03, NOTF-01-04, QBNK-01-03)
- v8.0 audit: ANLT-04 partial (30-day only, not 30/60/90), 5 Chart.js visual checks pending human verification

---

## v7.12 Struktur Organisasi CRUD (Shipped: 2026-03-21)

**Phases completed:** 4 phases (219-222), 7 plans
**Timeline:** 2026-03-21 (single day)
**Code changes:** 28 files changed, +3,961 / -380 lines

**Delivered:** Migrasi penuh dari static class OrganizationStructure ke database-driven CRUD — entity OrganizationUnit dengan adjacency list, halaman admin Struktur Organisasi dengan indented table + full CRUD, integrasi seluruh dropdown/filter/validasi portal ke database, dan cleanup final.

**Key accomplishments:**

1. DB Model & Migration — Entity OrganizationUnit (self-referential adjacency list), migrasi 4 Bagian + 19 Unit dari static class, konsolidasi KkjBagian ke OrganizationUnit (KkjFile/CpdpFile FK remapped)
2. CRUD Page Kelola Data — Halaman Struktur Organisasi di Kelola Data Section A: indented table view, tambah/edit/pindah/hapus/reorder node, validasi anti-circular reference, soft-delete dengan guard children/user assignment
3. Integrasi Codebase — 15+ dropdown filter Bagian/Unit di 4 controller (Admin, CMP, CDP, ProtonData) dan views diganti ke database OrganizationUnits, cascade filter tetap berfungsi, role-based section locking L4/L5 dipertahankan
4. Cleanup & Finalisasi — OrganizationStructure.cs dihapus, SeedOrganizationUnitsAsync ditambahkan sebagai safety net deployment, ImportWorkers memvalidasi Section/Unit terhadap database

---

## v7.10 RenewalCertificate Bug Fixes & Enhancement (Shipped: 2026-03-21)

**Phases completed:** 3 phases, 5 plans, 5 tasks

**Timeline:** 2026-03-21 (single day)
**Commits:** 25+ feat/fix/docs commits

**Delivered:** Perbaikan total renewal certificate — bulk renew FK chain, badge count sync, data/display fixes (ValidUntil null, category prefill, grouping), filter tipe Assessment/Training, renewal method modal, dan AddTraining renewal mode.

**Key accomplishments:**

1. Critical renewal chain fixes — Bulk renew sekarang assign RenewsSessionId/RenewsTrainingId per-user via JSON dictionary hidden input, badge count Admin/Index menggunakan BuildRenewalRowsAsync sebagai single source of truth
2. Data & display fixes — DeriveCertificateStatus pisahkan cek Permanent dan ValidUntil=null, MapKategori konsisten dengan AssessmentCategories, grouping case-insensitive (OrdinalIgnoreCase), URL-safe karakter khusus
3. Tipe filter — Dropdown filter Assessment/Training/Semua pada halaman RenewalCertificate dengan query param routing
4. Renewal method modal — Single renew dan bulk renew menampilkan popup pilihan metode (via Assessment atau via Training Record baru), mixed-type bulk validation
5. AddTraining renewal mode — GET menerima renewTrainingId/renewSessionId params, prefill Judul/Kategori/Peserta, banner Mode Renewal, hidden FK inputs, bulk multi-user support

---

## v7.8 Dokumen KKJ & Alignment KKJ/IDP — Combine Menu (Shipped: 2026-03-20)

**Phases completed:** 3 phases (205-207), 3 plans
**Timeline:** 2026-03-20 (single day)
**Commits:** 9 feat/fix commits across 23 files

**Delivered:** Gabung 2 menu terpisah (KKJ + Alignment KKJ/IDP) di CMP Index menjadi 1 halaman dengan 2 tab stacked sections, role-based filtering, dan visual polish.

**Key accomplishments:**

1. Halaman gabungan DokumenKkj — 2 tab ("Kebutuhan Kompetensi Jabatan (KKJ)" dan "Alignment KKJ & IDP") dengan stacked sections per bagian, server-side role filtering via query param deep-link
2. CMP Hub update — 2 card digabung jadi 1 card primary "Dokumen KKJ & Alignment KKJ/IDP", action Kkj/Mapping dan view lama dihapus
3. Visual polish — pemisah bagian dengan border-top, kolom Tipe rata tengah, rename tab KKJ, hapus kolom Tanggal Upload, compact empty state

---

## v7.7 Renewal Certificate & Certificate History (Shipped: 2026-03-19)

**Phases completed:** 5 phases (200-204), 9 plans
**Timeline:** 2026-03-19 (single day)
**Commits:** 15 feat commits across 16 files

**Delivered:** Full certificate renewal lifecycle — renewal chain data model (FK tracking), CreateAssessment pre-fill from expired certs, dedicated Renewal Certificate admin page with bulk renew, certificate history modal with Union-Find chain grouping, and CDP table enhancement hiding renewed certs.

**Key accomplishments:**

1. Renewal chain foundation — RenewsSessionId/RenewsTrainingId nullable FK columns on AssessmentSession and TrainingRecord, 4 batch queries with HashSet-based IsRenewed flag computation
2. CreateAssessment renewal pre-fill — GET accepts renewSessionId/renewTrainingId params, auto-fills Title/Category/peserta, Mode Renewal banner, ValidUntil required +1yr validation
3. Renewal Certificate page — Dedicated admin page at /Admin/RenewalCertificate with expired/akan-expired filter, single Renew button + checkbox bulk Renew Selected (category-locked), cascade Bagian/Unit/Kategori filter, badge count card in Kelola Data Section C
4. Certificate history modal — Shared endpoint CertificateHistory with Union-Find renewal chain grouping, _CertificateHistoryModalContent partial view, dual mode (renewal with Renew buttons, readonly for CDP)
5. CDP Certification Management enhancement — Renewed certs hidden by default with toggle "Tampilkan Riwayat Renewal" (opacity 50%), Expired/AkanExpired card counts exclude renewed certificates

---

## v7.6 Code Deduplication & Shared Services (Shipped: 2026-03-18)

**Phases completed:** 4 phases (196-199), 6 plans
**Timeline:** 2026-03-18 (single day)
**Code changes:** 38 files changed, +3,311 / -1,206 lines

**Delivered:** Pure refactoring milestone — extracted shared services, consolidated CRUD entry points, and unified code patterns across controllers. No new UI, no DB migrations. Net reduction of ~700+ lines of duplicated code.

**Key accomplishments:**

1. IWorkerDataService shared service — 4 helper methods (GetUnifiedRecords, GetAllWorkersHistory, GetWorkersInSection, NotifyIfGroupCompleted) extracted from Admin+CMPController, removing 561 lines of duplicated code
2. ExcelExportHelper — Static helper eliminates ~170 lines of ClosedXML boilerplate across 15 export actions in 4 controllers
3. Training CRUD consolidated — CMP orphan edit/delete/import actions removed, ImportTraining moved to AdminController with link from ManageAssessment
4. FileUploadHelper + PaginationHelper — 6 inline patterns replaced across 3 controllers with reusable static helpers
5. CMPController role-scoping helper — GetCurrentUserRoleLevelAsync extracts repeated role-checking from 5 action methods

---

## v7.1 Export & Import Data (Shipped: 2026-03-16)

**Phases completed:** 5 phases, 5 plans, 0 tasks

**Key accomplishments:**

- (none recorded)

---

## v6.0 Deployment Preparation (Closed: 2026-03-16)

**Phases defined:** 2 phases (173-174), 0 plans executed
**Timeline:** 2026-03-16 (defined and closed same day)
**Status:** Closed without execution

**Intent:** Prepare production-ready release package and deployment documentation for IT team (IIS + AD + SSMS database).

**Known Gaps (all 4 requirements unstarted):**

- REL-01: Release folder creation
- REL-02: Production config (AD enabled)
- DOC-01: SSMS database export guide
- DOC-02: IIS deployment guide

---

## v5.0 Guide Page Overhaul (Shipped: 2026-03-16)

**Phases completed:** 2 phases (171-172), 4 plans, 8 tasks
**Timeline:** 2026-03-16 (1 day)
**Code changes:** 17 files changed, +1,709 / -385 lines
**Commits:** 18

**Delivered:** Guide & FAQ system cleanup and UI polish — redundant accordion guides removed (covered by PDF tutorials), dynamic role-based card counts, FAQ expand/collapse toggle, unified badge and button styling, back-to-top navigation, and breadcrumb navigation on GuideDetail pages.

**Key accomplishments:**

1. GuideDetail accordion simplification — CMP reduced from 7 to 4 items (5 for Admin/HC), CDP reduced from 7 to 3 items (5 for Admin/HC), redundant step-by-step items removed as covered by PDF tutorials
2. Tutorial card CSS refactor — Inline styles replaced with CSS variant modifier classes (guide-tutorial-card--cmp/cdp/admin), AD guide tutorial card added for admin module
3. Dynamic guide card counts — All 5 module cards show role-conditional counts via Razor int variables, matching actual GuideDetail accordion item counts
4. FAQ improvements — Expand/collapse all toggle button, categories reordered by priority, redundant step-by-step FAQ items removed
5. UI consistency — Unified .guide-role-badge class across Guide and GuideDetail, .step-variant-blue replacing pink, shared accordion base styling
6. Navigation — Floating back-to-top button on both Guide pages, GuideDetail breadcrumb (Beranda > Panduan > Module Name) replacing back button

**Tech Debt (1 item, non-blocking):**

- Legacy CSS alias .guide-step-badge-role kept in guide.css (no view references it)

---

## v4.3 Bug Finder (Shipped: 2026-03-13)

**Phases completed:** 3 phases (168-170), 8 plans
**Timeline:** 2026-03-13 (1 day)
**Code changes:** 49 files changed, +2,319 / -325 lines
**Commits:** 17

**Delivered:** Comprehensive codebase, file system, database, and security audit. Removed dead code, temp files, and unused imports. Fixed CSRF gap, XSS patterns, and file upload validation. Portal is clean, secure, and free of technical debt.

**Key accomplishments:**

1. Dead code removed — 2 unreachable controller actions (CleanupDuplicateAssignments, SearchUsers), 3 unused imports cleaned
2. Logic bugs fixed — 2 silent catch blocks now log at Warning level, all null dereference risks verified
3. File system cleaned — 40+ temp screenshots/artifacts removed, .gitignore hardened with 5 new patterns
4. Database verified — All 35 DbSets active, FK integrity confirmed, seed data properly gated
5. CSRF gap closed — NotificationController's [IgnoreAntiforgeryToken] removed, JS updated to send token header
6. XSS patterns fixed — 4 unsafe Html.Raw(x.Replace()) replaced with Json.Serialize, all 8 upload endpoints secured

**Tech Debt (5 items, all non-blocking):**

- Pre-existing bare catch at AdminController:1072 (intentional audit-log pattern)
- 1 null-forgiving operator deferred ([Authorize] guarantee)
- 3 orphaned KkjMatrixItemId columns (documented from Phase 90)
- 5 near-duplicate code pairs (below extraction threshold)
- SUMMARY prose counting error (27 vs 35 DbSets, non-blocking)

---

## v4.0 E2E Use-Case Audit (Shipped: 2026-03-12)

**Phases completed:** 6 phases (153-158), 16 plans
**Timeline:** 2026-03-11 → 2026-03-12 (2 days)
**Code changes:** 18 files changed, +2,737 / -66 lines
**Commits:** 72

**Delivered:** Comprehensive end-to-end audit of the entire portal organized by 6 use-case flows — code review + browser UAT per flow. All 33 requirements verified, 10+ bugs fixed, 10 tech debt items documented.

**Key accomplishments:**

1. Assessment flow hardened — Fixed DeleteQuestion FK crash, open redirect in Results, certificate access control (IsPassed guard), TrainingRecord auto-creation on exam submission
2. Coaching Proton bugs fixed — CoachCoacheeMappingReactivate cascades to restore ProtonTrackAssignments; SubmitInterviewResults creates ProtonFinalAssessment on pass
3. Admin data management audited — Fixed ProtonFinalAssessment cascade order in DeleteWorker, CPDP download MIME type, added missing audit log entries
4. CDP Dashboard scoping fixed — Coachee URL manipulation prevented (server-side override), duplicate key crash on multiple assignments resolved
5. Auth & authorization verified — Full controller authorization matrix confirmed across all 7 controllers, AccessDenied flow validated
6. Navigation integrity confirmed — All navbar links, guide pages, and hub cards verified; GuideDetail case-sensitivity bug fixed

**Tech Debt (10 items):**

- 2 deferred browser tests (assessment validation, certificate negative test)
- 3 coaching edge cases (ExportProgressExcel role attr, evidence storage, download auth)
- 2 admin edge cases (silabus delete warning, override status validation)
- 1 pre-existing (Chart.js rendering)
- 2 silent catch blocks (AD sync)

---

## v3.21 Account Profile & Settings Cleanup (Shipped: 2026-03-11)

**Phases completed:** 1 phase (152), 1 plan, 2 tasks
**Timeline:** 2026-03-11
**Files modified:** 5 (4 modified, 1 created)

**Delivered:** Account Profile & Settings page cleanup — authorization pattern, client-side validation, phone regex, ViewModel refactor, button label fix, and UI spacing consistency.

**Key accomplishments:**

1. Class-level `[Authorize]` on AccountController with `[AllowAnonymous]` on Login/AccessDenied
2. New ProfileViewModel replacing ViewBag for role display on Profile page
3. Client-side validation on Settings page via `_ValidationScriptsPartial`
4. Phone regex updated to accept international formats (+62 812-3456-7890)
5. Profile button label corrected to "Pengaturan", all rows unified to mb-3

---

## v3.8 CoachingProton UI Redesign (Shipped: 2026-03-07)

**Phases completed:** 1 phase (112), 1 plan, 2 tasks
**Timeline:** 2026-03-07
**Files modified:** 1 (Views/CDP/CoachingProton.cshtml)

**Delivered:** Complete visual redesign of CoachingProton page — clickable badges converted to proper buttons, status badges given bold+border treatment for resolved states, JS innerHTML synchronized with server-rendered styling, and Export PDF recolored for consistency.

**Key accomplishments:**

1. Converted 4 Pending badge spans to proper `btn-outline-warning` Tinjau buttons with preserved modal triggers
2. Added `fw-bold` + colored border to Approved/Rejected/Reviewed status badges via Razor helpers
3. Updated 6 JS innerHTML locations to match new badge styling after AJAX operations
4. Changed Export PDF button from red to green outline, matching Excel export
5. Unified Evidence column: Sudah Upload = bold green+border, Belum Upload = plain gray

---

## v3.6 Histori Proton (Shipped: 2026-03-06)

**Phases completed:** 2 phases (107-108), 4 plans
**Timeline:** 2026-03-06

**Delivered:** Proton History feature in CDP menu — role-scoped worker list with search/filter and vertical timeline detail page showing each worker's Proton journey (Tahun 1-3) with expandable nodes, status badges, and responsive styling.

**Key accomplishments:**

1. **CDP Histori Proton menu** — New navbar item with role-scoped access (Coachee self-redirect, Coach/SrSpv/SH section-scoped, HC/Admin all workers)
2. **Worker list page** — Table with search by nama/NIP, filter by unit/section, step indicator showing Tahun progress, status badges
3. **Timeline detail page** — Vertical left-aligned timeline with colored circles (green=Lulus, yellow=Dalam Proses), expandable Bootstrap Collapse cards per Proton year
4. **Per-node detail** — Each node shows Tahun, Unit, Coach name, Status, Competency Level (if lulus), Start/End dates
5. **Responsive design** — Bootstrap 5 consistent styling, mobile-friendly layout

**Files Modified:** Models (2 ViewModels), Controllers/CDPController.cs, Views/CDP (3 views)

---

## v3.2 Bug Hunting & Quality Audit (Shipped: 2026-03-05)

**Phases completed:** 7 phases (92-98, 99), 31 plans, 95 tasks

**Delivered:** Comprehensive audit of all portal sections — Homepage, CMP, CDP, Admin Portal, Account pages, Authentication/Authorization, and Data Integrity. Fixed 20+ bugs across UI, navigation, localization, authorization, soft-delete cascades, and audit logging.

**Key accomplishments:**

1. **Homepage Audit** — Fixed 5 bugs: deadline links, pluralization, localization, query consistency, negative days display
2. **CMP Section Audit** — Fixed 6 bugs: localization (Indonesian dates), validation errors, navigation flow
3. **CDP Section Audit** — Fixed 8 bugs: auth issues, navigation gaps, ProtonGuidance access, edge cases
4. **Admin Portal Audit** — Fixed 4 bugs: role gates, UI issues, ManageWorkers validation
5. **Account Pages Audit** — Profile/settings verified working, avatar display fixes
6. **Auth & Authorization Audit** — Verified login flow (local/AD), inactive user block, AccessDenied page, role-based navigation, return URL security
7. **Data Integrity Audit** — Fixed 7 bugs: 3 orphan leaks (parent.IsActive filters), 4 missing AuditLog calls
8. **CDP Cleanup** — Removed broken Deliverable card from CDP Index

**Bug Summary:**

- UI/Localization: 9 bugs fixed
- Navigation: 5 bugs fixed
- Authorization/Security: 4 bugs fixed
- Data Integrity: 7 bugs fixed
- Validation: 3 bugs fixed

**Files Modified:** 15+ controllers, 20+ views

---

## v3.1 CPDP Mapping File-Based Rewrite (Shipped: 2026-03-03)

**Phases completed:** 1 phase (88), 6 plans, 17 tasks

**Delivered:** Full rewrite of KKJ Matrix from fixed 15-column spreadsheet model to dynamic key-value relational model with document-based file management system.

**Key accomplishments:**

1. **Dynamic Schema** — KkjColumn and KkjTargetValue tables replace fixed columns; administrators can add/edit/delete competency columns dynamically
2. **Document-Based File Management** — KkjFile and CpdpFile models with upload/download/archive functionality; versioned file tracking with AuditLog
3. **File Management UI** — Silabus tab and Coaching Guidance tab with full file CRUD operations, archive status filtering, and role-based access control
4. **Migration** — Existing KKJ Matrix data migrated from fixed columns to key-value model

**Files Modified:** Models (KkjColumn, KkjTargetValue, KkjFile, CpdpFile), AdminController, ProtonDataController, Views

---

## v3.0 Full QA & Feature Completion (Shipped: 2026-03-05)

**Phases completed:** 10 phases (82-91, 86 superseded), 34 plans
**Timeline:** 2026-03-02 to 2026-03-05 (4 days)

**Delivered:** Comprehensive end-to-end QA of all portal features organized by use-case flows, code cleanup removing orphaned/duplicate pages, UI rename "Proton Progress" → "Coaching Proton" throughout portal, KKJ Matrix full rewrite to document-based file management, and PlanIDP 2-tab redesign. All major user flows verified working.

**Key accomplishments:**

1. Cleanup & Rename — "Proton Progress" renamed consistently, 3 orphaned CMP pages removed, AuditLog card added to Kelola Data hub
2. Master Data QA — All Kelola Data CRUD verified, Worker/Silabus soft delete infrastructure with IsActive filters fully implemented
3. Assessment Flow QA — DownloadQuestionTemplate action created, full assessment lifecycle verified across 10 requirements
4. Coaching Proton QA — Full coaching workflow verified with browser testing (8 requirements, all flows pass)
5. Dashboard & Navigation QA — SeedDashboardTestData action created, all dashboards show correct role-scoped data, login flow secure with inactive user block
6. KKJ Matrix Full Rewrite — Document-based file management system (KkjFile/CpdpFile) replacing spreadsheet editor, 3 plans complete
7. PlanIDP 2-Tab Redesign — Unified Silabus + Coaching Guidance tabs for all roles, 3 plans complete with read-only consumer view
8. Admin Assessment Pages Audit — ManageAssessment + AssessmentMonitoring all 11 flows verified, RegenerateToken multi-sibling fix, IsActive filters added
9. CMP Assessment Pages Audit — Assessment + Records pages verified, CSRF fixes applied, Records redesigned with 2-tab layout

**Known Gaps:**

- Phase 89 PlanIDP: No VERIFICATION.md file (5 requirements unverified: PLANIDP-01 through PLANIDP-05)
- ASSESS-04: Assessment Results competency display may be broken (PositionTargetHelper missing from codebase)
- Phase 88: KKJ Matrix verification claims don't match actual implementation (discrepancy between claimed relational model and actual file-based approach)

---

## v2.7 Assessment Monitoring (Shipped: 2026-03-01)

**Phases completed:** 3 phases (79-81), 4 plans
**Files modified:** 7 | **Insertions:** 697 | **Deletions:** 9
**Timeline:** 2026-03-01

**Delivered:** Dedicated Assessment Monitoring page extracted from ManageAssessment dropdown into a first-class Kelola Data hub entry with group list, per-participant detail, full HC action suite, and Admin ManageQuestions feature — plus hub cleanup removing redundant cards.

**Key accomplishments:**

1. Assessment Monitoring group list — Dedicated page at /Admin/AssessmentMonitoring with real-time stats (participant count, completed, passed, status badge), search/filter bar, and Regenerate Token per group
2. Per-participant monitoring detail — Drill-down view showing each participant's live progress, status, score, countdown timer; token card with copy and inline regenerate
3. Full HC action suite on monitoring page — Reset, Force Close, Bulk Close, Close Early, Regenerate Token all available from the dedicated monitoring detail page
4. Admin ManageQuestions — New Admin-context question management page (ManageQuestions GET, AddQuestion POST, DeleteQuestion POST) accessible from ManageAssessment dropdown
5. Hub cleanup — Monitoring dropdown removed from ManageAssessment (CLN-01), Training Records hub card removed from Kelola Data Section C (CLN-02), AssessmentMonitoring table full-height styling

---

## v1.0 CMP Assessment Completion (Shipped: 2026-02-17)

**Phases completed:** 3 phases, 10 plans, 6 tasks

**Key accomplishments:**

1. Assessment Results Workflow — Users can view their assessment results immediately after completion with score, pass/fail status, and conditional answer review (if enabled by HC)
2. HC Configuration Controls — HC staff can configure pass thresholds (0-100%) and toggle answer review visibility per assessment with category-based defaults
3. Reports Dashboard & Analytics — HC can view, filter, and analyze assessment results across all users with Chart.js visualizations showing pass rates by category and score distributions
4. Excel Export & User History — HC can export assessment data to Excel format and drill down into individual user assessment history with complete performance tracking
5. Auto-Competency Tracking — Assessment completion automatically updates user competency levels via AssessmentCompetencyMap with monotonic progression ensuring levels only increase
6. CPDP Integration & Gap Analysis — Full integration loop connecting assessments → KKJ competencies → CPDP framework → IDP suggestions with radar chart visualization and evidence-based tracking

---

## v1.1 CDP Coaching Management (Shipped: 2026-02-18)

**Phases completed:** 4 phases (4-7), 11 plans, plus Phase 8 post-fix

**Key accomplishments:**

1. Coaching Sessions — Coaches can log sessions with domain-specific fields (Kompetensi, SubKompetensi, Deliverable, CatatanCoach) and action items with due dates against a stable data model
2. Proton Deliverable Tracking — Structured Kompetensi hierarchy with sequential lock enforcing ordered progression; coaches upload and revise evidence files per deliverable
3. Approval Workflow & Completion — Full SrSpv → SectionHead approval chain with rejection reasons; HC final approval triggers Proton Assessment that auto-updates competency levels
4. Development Dashboard — Role-scoped monitoring for Spv/HC with team competency progress, deliverable status, pending approvals, and Chart.js trend charts
5. Admin Role Switcher Fix — Admin can simulate all 5 role views (HC, Atasan, Coach, Coachee, Admin) with correct access gates and scoped data per simulated role

---

## v1.2 UX Consolidation (Shipped: 2026-02-19)

**Phases completed:** 4 phases (9-12), 8 plans, 11 requirements shipped

**Key accomplishments:**

1. Gap Analysis Removed — CMP Index card, CPDP Progress cross-link, controller action, view, and ViewModel deleted atomically with zero dead routes remaining
2. Unified Training Records — Personal assessment sessions and manual training records merged into single chronological table with type-differentiated columns; HC worker list extended with combined completion rate
3. Assessment Page Role-Filtered — Workers see Open/Upcoming only at DB level; HC/Admin get restructured Management + Monitoring tab layout with callout directing workers to Training Records
4. CDP Dashboard Consolidated — CDPDashboardViewModel with three nullable role-branched sub-models; Proton Progress tab (all roles, role-scoped) and Assessment Analytics tab (HC/Admin only) replace three standalone pages
5. Standalone Pages Retired — DevDashboard and HC Reports pages fully deleted; Chart.js moved to _Layout.cshtml globally; universal Dashboard nav entry added for all authenticated roles

---

## v1.3 Assessment Management UX (Shipped: 2026-02-19)

**Phases completed:** 15 phases, 34 plans, 6 tasks

**Key accomplishments:**

- (none recorded)

---

## v1.6 Training Records Management (Shipped: 2026-02-20)

**Phases completed:** 20 phases, 47 plans, 6 tasks

**Key accomplishments:**

- (none recorded)

---

## v1.7 Assessment System Integrity (Shipped: 2026-02-21)

**Phases completed:** 6 phases (21-26), 14 plans
**Files modified:** 83 | **Insertions:** 17,854 | **Deletions:** 222
**Timeline:** 2026-02-20 → 2026-02-21

**Key accomplishments:**

1. Exam state tracking — Workers marked InProgress with timestamp on first exam load; idempotent guard prevents double-writes; visible as yellow badge in MonitoringDetail
2. Full exam lifecycle — Abandon flow (Keluar Ujian), HC force-close/reset, server-side timer enforcement (+2min grace), configurable exam window close dates with lockout
3. Package answer persistence & review — PackageUserResponse table; answer review works for package exams; token enforcement blocks direct URL bypass via TempData guard
4. HC audit log — All 7+ HC assessment management actions logged with actor NIP/name, timestamp; paginated read-only AuditLog page (HC/Admin only)
5. Worker UX — Riwayat Ujian history table on Assessment page; Kompetensi Diperoleh card on Results page showing earned competencies after passing
6. Data integrity safeguards — DeletePackage shows assignment count in confirm dialog with cascade cleanup; EditAssessment warns on schedule change when packages attached

---

## v1.9 Proton Catalog Management (Shipped: 2026-02-24)

**Phases completed:** 40 phases, 86 plans, 9 tasks

**Key accomplishments:**

- (none recorded)

---

## v2.0 Assessment Management & Training History (Shipped: 2026-02-24)

**Phases completed:** 40 phases, 86 plans, 9 tasks

**Key accomplishments:**

- (none recorded)

---

## v2.1 Assessment Resilience & Real-Time Monitoring (Shipped: 2026-02-25)

**Phases completed:** 5 phases (41-45), 13 plans
**Files modified:** 52 | **Insertions:** 12,184 | **Deletions:** 255
**Timeline:** 2026-02-24 → 2026-02-25

**Delivered:** Workers never lose exam progress (auto-save + session resume), HC can monitor live during assessments, and cross-package shuffle gives each worker a unique question mix from multiple packages.

**Key accomplishments:**

1. Auto-save — Worker answers saved per-click via AJAX with atomic upsert (ExecuteUpdateAsync + UNIQUE constraint); legacy exam path also covered via SaveLegacyAnswer
2. Session resume — ElapsedSeconds + LastActivePage persisted; workers resume from exact page with accurate remaining time; pre-populated answers on reconnect
3. Worker polling — 10s poll interval with IMemoryCache (5s TTL, ~99% DB load reduction); auto-redirects worker to Results when HC closes session early
4. Real-time monitoring — HC sees live progress (answered/total), status, score, time remaining per worker; 10s auto-refresh + 1s countdown; JS-rendered Reset/ForceClose action buttons
5. Cross-package per-position shuffle — Each question slot independently picks which package's question to show; even distribution across packages; import validation enforces equal counts; all 5 consumers (StartExam, SubmitExam, ExamSummary, Results, CloseEarly) updated

---

## v2.2 Attempt History (Shipped: 2026-02-26)

**Phases completed:** 1 phase (46), 2 plans, 4 tasks
**Files modified:** 15 | **Insertions:** 2,851 | **Deletions:** 82
**Timeline:** 2026-02-26

**Delivered:** HC and Admin can view a full chronological record of every assessment attempt per worker — including attempts previously erased by Reset — with sequential Attempt # numbering and dual Riwayat Assessment / Riwayat Training sub-tabs at /CMP/Records.

**Key accomplishments:**

1. AssessmentAttemptHistory model + EF Core migration — new SQL Server table preserving SessionId, Score, IsPassed, AttemptNumber, StartedAt, CompletedAt at archive time
2. Archive-before-clear in ResetAssessment — Completed sessions archived with AttemptNumber = existing row count + 1 before wipe; unstarted sessions produce no history row
3. Unified assessment history query — GetAllWorkersHistory() returns (assessment, training) tuple; batch GroupBy/ToDictionary computes Attempt # for current sessions without N+1
4. Riwayat Assessment + Riwayat Training dual sub-tabs — Bootstrap nested nav-tabs; client-side worker/NIP text + title dropdown filter with no round-trip

---

## v2.3 Admin Portal (Shipped: 2026-03-01)

**Phases completed:** 8 phases (47-53, 59), 29 plans
**Files modified:** 274 | **Insertions:** 82,601 | **Deletions:** 8,074
**Timeline:** 2026-02-26 → 2026-03-01 (4 days)

**Delivered:** Admin has full CRUD control over master data (KKJ Matrix, CPDP Items), operational records (Coach-Coachee Mapping, DeliverableProgress Override, Final Assessment), and assessment management — all consolidated under /Admin with role-gated access.

**Key accomplishments:**

1. Admin Portal infrastructure — AdminController with 12-card hub page, role-gated navigation, and class-level authorization
2. KKJ Matrix & CPDP Items managers — Spreadsheet-style inline editing with bulk-save, multi-cell clipboard, and Excel export for master data
3. Assessment Management migration — All manage actions (Create, Edit, Delete, Reset, Force Close, Export, Monitoring, History) moved from CMP to Admin with AuditLog
4. Coach-Coachee Mapping manager — Grouped-by-coach view with bulk assign, soft-delete, section filter, and Excel export
5. Proton Silabus & Coaching Guidance — Two-tab /Admin/ProtonData replacing ProtonCatalog with full silabus CRUD and guidance file management
6. DeliverableProgress Override — Third ProtonData tab for HC to override stuck statuses; sequential lock removed (all deliverables Active on assignment)
7. Final Assessment Manager — Assessment Proton exam category with eligibility-gated coachee picker, Tahun 3 interview workflow; legacy HCApprovals removed
8. ProtonCatalog cleanup — Redirect-only controller and views deleted after full migration to /Admin/ProtonData

### Known Gaps

- **OPER-05**: CoachingSession & ActionItem admin override — phase never planned
- **CRUD-01**: AssessmentQuestion inline edit — phase never planned
- **CRUD-02**: PackageQuestion edit/delete — REMOVED (Phase 56)
- **CRUD-03**: ProtonTrack edit/delete — REMOVED (covered by Phase 59 ProtonData migration)
- **CRUD-04**: Password Reset standalone — superseded by v2.5 Phase 67 ManageWorkers migration

---

## v2.4 CDP Progress (Shipped: 2026-03-01)

**Phases completed:** 4 phases (61-64), 9 plans
**Files modified:** 49 | **Insertions:** 20,101 | **Deletions:** 6,105
**Timeline:** 2026-02-27 → 2026-02-28

**Delivered:** CDP/Progress page rebuilt from scratch — data source corrected to ProtonDeliverableProgress, all filters wired to real queries with role-scoping, per-role approval workflow (SrSpv/SH/HC) with coaching report + evidence, Excel/PDF export via QuestPDF, and server-side group-boundary pagination with empty states.

**Key accomplishments:**

1. Data source fix — ProtonProgress action queries ProtonDeliverableProgress + ProtonTrackAssignment (not IdpItems), real coachee list from CoachCoacheeMapping, correct summary stats
2. Role-scoped filtering — 5 filter parameters (Bagian/Unit, Coachee, Track, Tahun) wired to EF Core Where composition with role-scope-first pattern; client-side search box
3. Per-role approval workflow — SrSpv/SectionHead/HC each have independent approval columns; per-role migration backfills from existing Approved records; rejection takes overall precedence
4. Coaching report + evidence — SubmitEvidenceWithCoaching combined modal; CoachingSession FK linked; Deliverable detail page shows coaching report
5. Export — Excel export via ClosedXML and PDF export via QuestPDF from ProtonProgress page
6. UI polish — Group-boundary server-side pagination (20 rows/page), 3 empty state scenarios, "Menampilkan X dari Y" counter

---

## v2.5 User Infrastructure & AD Readiness (Shipped: 2026-03-01)

**Phases completed:** 8 phases (65-72), 14 plans
**Files modified:** 41 | **Insertions:** 12,297 | **Deletions:** 1,055
**Timeline:** 2026-02-27 → 2026-02-28

**Delivered:** Full user system overhaul — dynamic profile/settings pages, ManageWorkers migrated to AdminController with HC access, Kelola Data hub reorganized, dual authentication (Active Directory + local) via IAuthService abstraction, hybrid auth with AD-first + local fallback for admin, and role structure additions (Supervisor level 5).

**Key accomplishments:**

1. Dynamic profile page — Profile bound to @model ApplicationUser; real user data (Nama, NIP, Email, Position, Section, Unit, Role); null-safe em dash fallback; avatar initials from FullName
2. Functional settings page — Change password via ChangePasswordAsync; edit FullName/Position; non-functional items (2FA, Notifications, Language) removed or disabled
3. ManageWorkers migration — 11 actions (CRUD, import, export, detail) moved from CMPController to AdminController with [Authorize(Roles = "Admin, HC")]; standalone navbar button removed; 5 view files copied and updated
4. Kelola Data hub — Admin/Index.cshtml restructured into 3 domain sections (Manajemen Pekerja, Kelola Assessment, Data Proton); stale "Segera" items cleaned up; HC nav access extended
5. LDAP auth infrastructure — IAuthService interface + LocalAuthService + LdapAuthService (DirectoryEntry LDAP bind); config toggle UseActiveDirectory; System.DirectoryServices NuGet
6. Dual auth login flow — AccountController.Login POST uses IAuthService; AD hint on login page; profile sync (FullName/Email only); unregistered users rejected with message
7. Hybrid auth — HybridAuthService wraps AD-first + local fallback for admin@pertamina.com; Supervisor role (level 5) added; SectionHead demoted to level 3
8. User structure polish — UserRoles.GetDefaultView() single source of truth; SeedData modernized; AuthSource field added then removed (global config routing replaces per-user)

---

## v2.6 Codebase Cleanup (Shipped: 2026-03-01)

**Phases completed:** 46 phases, 98 plans, 13 tasks

**Key accomplishments:**

- (none recorded)

---
