---
gsd_state_version: 1.0
milestone: v30.0
milestone_name: Essay Grading Correctness + Monitoring UI Refactor
status: v30.0 milestone complete
stopped_at: Completed 383-04-PLAN.md (ECG-06 regression lock; 5 test, 440/440 full suite, 0 migration)
last_updated: "2026-06-15T04:37:55.839Z"
last_activity: 2026-06-15
progress:
  total_phases: 21
  completed_phases: 2
  total_plans: 8
  completed_plans: 8
  percent: 100
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 384 — monitoring-essay-grading-ui-refactor-fase-2

## Current Position

Phase: 384
Plan: 1 of 4

- Plan 01 ✅ DONE — helper `IsQuestionCorrect` + 11 unit test, RED→GREEN, commits 32e49942/adf247d5.
- Plan 02 ✅ DONE — `CMPController.Results` 4 site rewire ke `IsQuestionCorrect` + IsEssayPending D-06 broaden + D-07 essay UserAnswer=TextAnswer (commit f6f4ed43); blok Razor render teks essay + regression `ResultsEssayCorrectnessTests` (commit 7f5d560a). **Task 3 UAT APPROVED via browser** (commit 83d30dfa — CMP/Results/166 tampil 6/6, essay Soal 5/6 hijau Benar + teks jawaban + ET 6/6).
- Plan 03 ✅ DONE (wave 2) — PDF export essay correctness `GeneratePerPesertaPdf` di-unify ke `IsQuestionCorrect` (essay >0, null pending); threshold lama `>= ScoreValue/2` dihapus (ECG-05/D-03). commit 145f08fe.
- Plan 04 ✅ DONE (wave 3) — ECG-06 regression lock: `SubmitEssayScore` (persist + range guard) + `FinalizeEssayGrading` (recompute essay-aware + idempotent no-op Completed) via mirror-data-level + authz `[Authorize(Roles=Admin,HC)]` reflection-assert. **NO production code change (D-05** — controller hash identik baseline). 5 test baru di `EssayFinalizeRecomputeTests.cs` (real-SQL fixture, SQLEXPRESS tersedia). **full suite 440/440 incl Integration.** Migration guard: `dotnet ef add _verify_383` = 0 model diff (D-04 no-migration). commits 24e44cb4/158a9f03.

**MILESTONE v30.0 STARTED.** Essay Grading Correctness + Monitoring UI Refactor (phases 383-384, 10 REQ ECG-01..06 + UIG-01..04). Driven by user bug report 2026-06-15: `CMP/Results` shows "Nilai Anda 100%" but "(4/6 benar)" — essays graded fully correct are counted wrong in the X/Y count, Elemen Teknis, Tinjauan Jawaban badge, and PDF export. Root cause (workflow-verified multi-agent): two divergent paths in `CMPController.Results()` — score% is essay-aware (Path A via `AssessmentScoreAggregator`), but count/ET/Tinjauan recompute inline with option-matching only (Path B, no Essay branch). Closes deferred backlog **RES-02** + **GRD-02**.

**Plan:** Not started

**Spec:** `docs/superpowers/specs/2026-06-15-essay-grading-correctness-design.md` (brainstorming-approved 2026-06-15).

**Next:** `/gsd-plan-phase 383` (after roadmap committed) — fix bug first (isolated hotfix), then phase 384.

Predecessor: v25.0 + v26.0 + v27.0 + v28.0 + v29.0 SHIPPED LOCAL + audited PASSED + closed (v25/26/27 joint safe-close 2026-06-14; v28.0 manual append-only 2026-06-14; v29.0 manual append-only 2026-06-15).

| Milestone | Phases | REQ | Audit | Archive |
|-----------|--------|-----|-------|---------|
| v25.0 Proton Kelulusan & Bypass | 358-368 | 20/20 PCOMP/PBYP | PASSED | milestones/v25.0-ROADMAP.md |
| v26.0 Urgent Search & Records Visibility | 369-371 | 3/3 URG | PASSED | milestones/v26.0-ROADMAP.md |
| v27.0 Shuffle Toggle | 372-375 | 16/16 SHUF | PASSED | milestones/v27.0-ROADMAP.md |
| v28.0 Assessment & Records Bug Fixes | 376-379 | 6/6 GRADE/IMP/CMPRT/E2E | PASSED | milestones/v28.0-ROADMAP.md |
| v29.0 Assessment E2E Worker-Success Fix | 380-382 | 11/11 WSE | PASSED | milestones/v29.0-ROADMAP.md |

Predecessor: v24.0 ✅ SHIPPED LOCAL + closed 2026-06-09 (352-357, 25/25 REQ).

## Next Action

1. ✅ **v30.0 CLOSED 2026-06-15** — milestone audit PASSED (10/10 REQ, 2/2 phases, integration 3/3 flows), archived + tagged `v30.0`. 0 migration kedua phase.
2. ✅ **Push v30.0 PUSHED 2026-06-15** — branch `ITHandoff` (`1a29865e..fe8c5ffe`) + tag `v30.0` → `origin/ITHandoff`. **Sisa NOTIFY IT:** flag **migration=FALSE** v30.0 (HEAD `fe8c5ffe`); carry-over lama 360/372 masih pending.
3. **`/gsd-new-milestone`** untuk mulai milestone berikut (questioning → research → requirements → roadmap).

## Tag Git

- `v24.0`, `v25.0`, `v26.0`, `v27.0`, `v28.0` — ✅ PUSHED ke `origin/ITHandoff` 2026-06-14.
- `v29.0` — ✅ dibuat saat manual close 2026-06-15 + PUSHED `origin/ITHandoff`. Annotated (Assessment E2E Worker-Success Fix, 380-382, 11/11 REQ, 0 migration).
- `v30.0` — ✅ dibuat + PUSHED `origin/ITHandoff` 2026-06-15. Annotated (Essay Grading Correctness + Monitoring UI Refactor, 383-384, 10/10 REQ, 0 migration).

## Deferred Items

> ✅ **ACCEPTED OK 2026-06-14** (keputusan user): semua carry-over v11.2/v13/v14/v15 di bawah = **phase lama, dianggap OK / non-blocking** (kode sudah ship + jalan; tak ada bug report di milestone v16-v29). Bukan pekerjaan tertunda aktif. Tetap dicatat sebagai histori, bukan TODO. Buka lagi hanya bila muncul bug/kebutuhan nyata.

### v15.0 Deferred (carry-over) — ACCEPTED OK

| REQ | Item | Status | Due |
|-----|------|--------|-----|
| EPRV-01 | Preview Essay rubrik/jawaban — Jalur A (label) vs Jalur B (field baru) | accepted-OK (user 2026-06-14; buka bila perlu field baru) | 2026-05-12 |

### Carry-over dari v14.0 close (2026-04-24) — ACCEPTED OK

| Category | Item | Status | Source |
|----------|------|--------|--------|
| UAT | Phase 303 Plan 02 Task 3 — Coach Workload 12-langkah human verification | accepted-OK (kode ship+jalan; approval formal di-waive) | STATE.md (prior) |
| UAT | Phase 235 — 5 items butuh human verification via browser | accepted-OK | STATE.md (prior) |
| UAT | Phase 247 approval chain — 2 TODO (HC review + resubmit notification) | accepted-OK | STATE.md (prior) |
| Research gap | Phase 297 Pre-Post Renewal behavior — keputusan 2 sesi baru otomatis | accepted-OK (undecided, non-blocking) | v14.0 planning |
| Research gap | Phase 298 essay max character limit — nvarchar(max) vs nvarchar(2000) | accepted-OK (undecided, non-blocking) | v14.0 planning |
| Blocker | Phase 293 `GetSectionUnitsDictAsync` Level 2+ support | accepted-OK (org 2-level cukup; buka bila butuh >2 level) | v13.0 carry-over |
| v11.2 paused | Phase 281 (System Settings) + Phase 285 (Dedicated Impersonation Page) | accepted-OK (closed-early, non-blocking) | MILESTONES.md v11.2 |

### v29.0 Deferred / Pending (non-blocker)

| Item | Status |
|------|--------|
| CERT-01 konfirmasi visual human (dashboard cert null tampil "Aktif") | PENDING — DB-coherence sudah otomatis (CertAlertConsistencyTests + e2e #12); cuma pixel check |
| I-1 WSE-01 pre-check non-type-aware (kasus salah-konfig Pre-isi/Post-kosong) | follow-up opsional — redirect aman, bukan Fail palsu |
| RES-02 (display-drift X/Y vs Score%) · GRD-02 (empty-MA SetEquals LOW) | ✅ CLOSED v30.0 — RES-02→ECG-02, GRD-02→ECG-01 MA non-empty guard |

### Backlog aktif (belum dipromote)

| Item | Reason |
|------|--------|
| 999.9 label residu "Backfill/Restore" di UI BulkBackfill | kosmetik (LOW) |
| 999.6 impersonate identity (dir tersisa) | sudah ditutup fungsional v28.0/377; dir backlog tinggal |
| 999.10 route CMP (dir tersisa) | sudah ditutup v28.0/378; dir backlog tinggal |
| 43 quick-task todo (audit-open, semua status `[missing]`) | acknowledged deferred saat v30.0 close 2026-06-15 — backlog project-wide lama (todo file ada, artifact hilang), bukan deliverable v30.0; pola sama close v25-v29 |

> ✅ Ditutup di v28.0 (2026-06-14): 999.8 essay→376 (GRADE), 999.6 impersonate→377 (IMP), 999.10 route→378 (CMPRT), 999.7 e2e→379 (E2E).

### Push IT

| Item | Status |
|------|--------|
| Push bundle v24-v28 ke `origin/ITHandoff` (branch + 5 tag) | ✅ PUSHED 2026-06-14, HEAD `bb8c04ed` |
| Push v29.0 (branch + tag `v29.0`) ke `origin/ITHandoff` | ✅ PUSHED 2026-06-15 |
| Push v30.0 (branch `ITHandoff` + tag `v30.0`) ke `origin/ITHandoff` | ✅ PUSHED 2026-06-15, HEAD `fe8c5ffe` |
| Notify IT — 2 migration carry (`PendingProtonBypass`+index/360, `ShuffleToggles`/372). **v29.0 = 0 migration baru.** | ⏳ PENDING — kasih commit hash + flag ke IT |
| IT apply migration DB Dev + promosi server Dev (10.55.3.3)/Prod | ⏳ tanggung jawab IT (bukan dev) |

## Accumulated Context

### Decisions (persist across milestones)

- [v30.0 / ECG-06 (383-04)]: Regression lock poin 2 (Simpan/Selesaikan essay) **tanpa ubah kode produksi (D-05)** — `Controllers/AssessmentAdminController.cs` hash identik baseline pasca-plan. 5 test baru di `HcPortal.Tests/EssayFinalizeRecomputeTests.cs` via **mirror-data-level** (precedent file ini — hindari ctor 12-dep controller): (1) `SubmitEssayScore` persist `EssayScore` + range guard `score<0||>ScoreValue` (mirror L3460-3477, T-298-13/V5); (2) `FinalizeEssayGrading` recompute **essay-aware** via `Compute` (Score=80 bukan 0) + **idempotent** no-op saat `Status==Completed` (mirror WHERE-guard `ExecuteUpdateAsync` L3593-3599 → re-run 0 baris, no double-count, D-03/T-383-09); (3) **authz `[Authorize(Roles="Admin, HC")]` kedua action dikunci via reflection-assert** (class pure `EssaySubmitFinalizeAuthzTests`, no DB — `GetMethods().First()` hindari overload-ambiguity; T-383-07/V4 — **BUKAN known gap**, RESEARCH OQ#3 resolved). Helper `QuestionOfSessionAsync` ([Rule 1 fix] scope soal ke session — `FirstAsync` global ambil soal milik test lain di fixture shared-DB). real-SQL disposable fixture (`Category=Integration`); SQLEXPRESS tersedia → integration jalan penuh. **full suite 440/440** (incl Integration). Migration guard: `dotnet ef migrations add _verify_383 --no-build` = empty Up/Down (0 model diff) → **D-04 0 migration baru** (plan test-only, zero model/DbContext change); orphan files dihapus, tree clean. commits 24e44cb4 (Submit) / 158a9f03 (Finalize+authz).
- [v30.0 / ECG-02/03/04 (383-02)]: `CMPController.Results` 4 call-site di-unify ke `AssessmentScoreAggregator.IsQuestionCorrect` (kill-drift): review-on correctness (verdict==true→correctCount++), `IsEssayPending` broadened (D-06: essay && verdict==null, **independen status sesi** — graded essay di sesi Completed render Benar/Salah, ungraded selalu "Menunggu Penilaian"), review-off count, Elemen Teknis predicate. **Guard `selectedIds.Count == 0` (yang men-skip essay tanpa PackageOptionId) DIHAPUS** di review-off + ET — itu akar bug "(4/6 benar)". **D-07:** essay `UserAnswer = TextAnswer` worker + `CorrectAnswer = "Dinilai manual"` + blok Razor BARU di `Results.cshtml` me-render `question.UserAnswer` (Pitfall 1: view sebelumnya tak pernah merujuk UserAnswer → set controller saja tak tampil). Regression pure-unit `ResultsEssayCorrectnessTests` (4 fact: count==6, ET counts essay, zero=Salah, null=pending). commits f6f4ed43 (controller) / 7f5d560a (view+test); build 0 error, non-Integration 318/318. **Task 3 = checkpoint human-verify UAT runtime PENDING** (sesi 166 lokal ada: Completed/Score100/AllowReview=1/2-essay-graded EssayScore=10 + TextAnswer "Refinery"/"Alkylation").
- [v30.0 / ECG-01 (383-01)]: Helper `AssessmentScoreAggregator.IsQuestionCorrect(q, responsesForQ) -> bool?` = single source correctness per-soal (true=Benar/false=Salah/null=essay pending). MC/MA mirror DISPLAY-path inline `CMPController.Results` (L2259-2324) byte-for-byte; cabang Essay baru `EssayScore.HasValue ? Value>0 : null` (D-02). **MA non-empty guard `selected.Count > 0 && SetEquals` sengaja BEDA dari `Compute` (scoring-path tanpa guard)** — display vs scoring concern terpisah (RESEARCH Pitfall 5); closes GRD-02. Pure/static/EF-free, 11 unit test no-DB hijau. `Compute` D-04 formula TIDAK diubah. Fondasi: consumer (3 site CMPController + PDF + View D-07) di Plan 02/03/04.
- [v30.0 / ECG-05 / D-03 (383-03)]: PDF export essay correctness di `AssessmentAdminController.GeneratePerPesertaPdf` (L5018) di-unify ke `AssessmentScoreAggregator.IsQuestionCorrect(q, sessionResponses.Where(r => r.PackageQuestionId == q.Id))` (essay `>0` → Benar, `==0` → Salah, null → Pending). **Threshold lama `EssayScore >= ScoreValue/2` DIHAPUS** — perubahan behavior PDF DISENGAJA agar PDF & web Results pakai satu aturan (kill-drift, tak bisa divergen lagi). `statusColor`/`statusText` + truncate 300 char dipertahankan; `SubmitEssayScore`/`FinalizeEssayGrading` TAK disentuh (D-05 lock-only, Plan 04). Render QuestPDF/SkiaSharp bisa env-blocked lokal (Phase 327) → code-verify cukup (aturan dikunci 11 unit test Plan 01). commit 145f08fe; build 0 error, suite non-Integration 314/314.
- [v29.0 / CERT-01 (382-03)]: `DeriveCertificateStatus(null ValidUntil, null/non-Permanent)` → **Aktif** (BUKAN Expired) — cert lulus tanpa kedaluwarsa = Aktif/Permanen. Single-source helper; consumer (AdminBase worklist L200, Renewal+CDP tally) ikut otomatis via Status enum — TIDAK diedit (Pattern 7). HomeController badge/notif sudah filter `ValidUntil.HasValue` (null sudah excluded, tak drift). Test lama `_ReturnsExpired` di-REWRITE → `_ReturnsAktif`; +`CertAlertConsistencyTests` (lock null-cert tak masuk tally Expired/AkanExpired).
- [v29.0 / 382 / D-01-IMPACT]: SAVE-01 = dedupe last-write-wins in-memory (ORDER BY SubmittedAt desc), **NO migration** (PackageUserResponse tak punya diskriminator QuestionType → filtered unique index tak feasible). Diverifikasi `dotnet ef migrations add _verify_382` → empty Up/Down (0 model diff), lalu dihapus. **v29.0 = 0 migration baru. Tidak perlu notify IT migration untuk milestone ini.**
- [v29.0 / e2e date helper (382-03)]: [Rule 3 fix] `tests/helpers/utils.ts` today/tomorrow/yesterday — UTC `toISOString()` → komponen tanggal LOKAL. Server validasi `Schedule < DateTime.Today` (waktu LOKAL); di TZ UTC+8 dini hari, tanggal UTC = kemarin-lokal → create assessment ditolak "Schedule date cannot be in the past". Shared helper, semua e2e flow ikut benar.
- [v29.0 / SAVE-01 (382-01)]: GradingService MC scoring baca jawaban FINAL per soal via `finalByQuestion` (last-write-wins by `SubmittedAt`, in-memory pada list yg sudah ToListAsync); `MultipleAnswer` TIDAK ter-dedupe (multi-row by design).
- [v29.0 / STAT-01 (382-01)]: `GradeAndCompleteAsync` guard NOT IN (Completed,Abandoned,Cancelled,PendingGrading) di KEDUA branch (non-essay+essay), rowsAffected==0→false; const `AssessmentStatus.Abandoned` ditambah (single-source). Test grading pakai real-SQL disposable fixture (`Category=Integration`) — `ExecuteUpdateAsync` tak didukung EF8 InMemory.
- [v29.0 / STAT-02 (382-02)]: `AbandonExam` jadi single atomic guarded `ExecuteUpdateAsync` WHERE (Id && UserId==owner && (InProgress||Open)) + rowsAffected==0 reject — TOCTOU dihapus, ownership di WHERE (anti-race + anti-spoof). SubmitExam SAVE-01 GroupBy `OrderByDescending(SubmittedAt).First()` (push==stored Score) + STAT-01 early guard terminal-set + audit.
- [v29.0 / TMR+TOK (382-02)]: timer "Standard" di-enforce (skip hanya Manual/null via pure `ShouldEnforceSubmitTimer`); token auto-submit di-peek (`TempData.Keep`) di guard, di-consume (`TempData.Remove`) HANYA di SubmitExam success path pasca-grading (retry-safe, TMR-03); incomplete-gate pakai `serverTimerExpired` sebagai otoritas (TMR-02). TOK-02 gate `IsTokenRequired && StartedAt==null` di SaveAnswer(Json)+SubmitExam(redirect). 4 keputusan di-extract ke pure static helper CMPController (uji via helper, ctor 14-dep infeasible — pola Phase 380).
- [v27.0 / SHUF]: Shuffle Toggle 2 sistem independen (Acak Soal + Acak Pilihan) per-assessment, default ON dua-duanya (data lama tak berubah); engine pure `Helpers/ShuffleEngine.cs` (ON canonical / OFF q.Order / OFF≥2 round-robin `workerIndex%count` guard); exam-effect manual-only by design (D-03, anti-brittle).
- [v25.0 / A-2]: Approve deliverable Proton cuma L4 (Sr SPV **atau** SH; 1 approver cukup). HC = final review, BUKAN approver deliverable.
- [v25.0 / A-3]: `CompetencyLevelGranted` dimatikan — `ProtonFinalAssessment` = penanda "Lulus/Selesai" murni. Kolom dormant (tidak di-drop).
- [v25.0 / A-4]: Penanda kelulusan Proton lewat 1 helper bersama (`ProtonCompletionService`) — 3 jalur exam/interview/bypass, dibedakan kolom `Origin`.
- [v24.0 / spec §8 Gap 1]: Sinkron Pre→Post gambar = shared-file (string path copy), BUKAN file fisik digandakan.
- [v24.0 / spec §9]: Hapus file gambar pakai pola Phase 333/335 — kumpul path SEBELUM tx, File.Delete SETELAH commit, inner try/catch warn-only.
- [v23.0 / Phase 350]: REC-06 D-07 invariant LOCKED — search assessment-title filter di level worker (post-load), badge/count per-worker utuh.
- [v22.0 cross-milestone]: `AssessmentConstants.AssessmentStatus.PendingGrading` = single source of truth label lintas 11+ surface.
- [v14.0 / Phase 296]: GradeFromSavedAnswers dihapus — GradingService satu-satunya source of truth grading.
- [v13.0]: SortableJS 1.15.7 via CDN; drag-drop sibling-only; orgTree.js single orchestrator.
- [v12.0]: AdminController dipecah jadi 8 controller per domain; URL tetap via [Route].
- [v21.0]: Configurable display labels via cached `IOrgLabelService` + global `@inject`.

### Open Blockers/Concerns

- ✅ **999.8 essay-grading** (RESOLVED v28.0/Phase 376): bug TAK reproduce di code current (fixed incidental v27.0 Phase 373). Hardening: helper `AssessmentScoreAggregator` + endpoint `RecomputeEssayScores` (prod-repair historis pasca-deploy bila ada baris Score=0 lama).
- [push] Carry migration (Origin, PendingProtonBypass+index/360, ShuffleToggles/372) — notify IT flag migration; 360+372 di delta yang sudah di remote. **v28.0 = 0 migration; v29.0 = 0 migration** (D-01-IMPACT dedupe, tak ada migration baru untuk flag).
- ✅ Phase 293 `GetSectionUnitsDictAsync` hardcoded 2-level — accepted-OK (user 2026-06-14; org 2-level cukup, buka bila butuh >2 level).

## Session Continuity

Last activity: 2026-06-15

Stopped at: Completed 383-04-PLAN.md (ECG-06 regression lock; 5 test, 440/440 full suite, 0 migration)

Next action: **Phase 383 SELESAI (4/4 plan DONE)** — `/gsd-verify-work 383` (verifikasi ECG-01..06 closure) lalu lanjut Phase 384 (Fase 2 Monitoring essay UI, UIG-01..04; plan 384-01/02 sudah ada). Plan 04 ECG-06: 5 test regression lock di `EssayFinalizeRecomputeTests.cs` (Submit persist+range, Finalize recompute-essay-aware+idempotent, authz reflection) — full suite 440/440 incl Integration, NO production change (D-05), 0 migration (D-04 guard PASSED). Pending non-blocker: notify IT carry migration 360/372 (v30.0 = 0 migration baru). JANGAN edit DB/kode Dev/Prod (CLAUDE.md).
