---
gsd_state_version: 1.0
milestone: v29.0
milestone_name: Assessment E2E Worker-Success Fix
status: "between_milestones — v29.0 (380-382) CLOSED 2026-06-15 (audited PASSED 11/11 WSE) + PUSHED origin/ITHandoff. Planning next cycle."
last_updated: "2026-06-15T00:00:00.000Z"
last_activity: 2026-06-15
progress:
  total_phases: 3
  completed_phases: 3
  total_plans: 8
  completed_plans: 8
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Between milestones — v29.0 CLOSED 2026-06-15 (shipped local + audited PASSED + PUSHED origin/ITHandoff). Planning next cycle.

## Current Position

**BETWEEN MILESTONES.** v29.0 "Assessment E2E Worker-Success Fix" (phases 380-382, 11 REQ WSE-01..11) SHIPPED LOCAL + audited PASSED (2026-06-15) + manual append-only close + PUSHED origin/ITHandoff (tag `v29.0`). Archive: `milestones/v29.0-ROADMAP.md` + `milestones/v29.0-REQUIREMENTS.md`. Audit: `v29.0-MILESTONE-AUDIT.md` (REQ 11/11, phases 3/3, integration 11/11 WIRED, flows 10/10). Plain-language summary: `docs/milestone-v29.0/index.html`. **0 migration** (SAVE-01 dedupe last-write-wins, BUKAN filtered-index). Phase dirs 380/381/382 diarsip ke `milestones/v29.0-phases/`.

**1 item PENDING (non-blocker):** CERT-01 konfirmasi visual human (dashboard cert ValidUntil=null tampil "Aktif" — DB-coherence sudah otomatis via CertAlertConsistencyTests + e2e #12). 1 finding LOW I-1 (WSE-01 pre-check non-type-aware → kasus salah-konfig Pre-isi/Post-kosong → redirect aman, bukan Fail palsu) — follow-up opsional.

**Next:** `/gsd-new-milestone` untuk siklus berikut, ATAU promote backlog 999.9 (label kosmetik LOW). 999.6/999.10 masih di `phases/` (backlog).

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

1. ✅ **v29.0 PUSHED — 2026-06-15.** Branch `ITHandoff` + tag `v29.0` pushed ke `origin/ITHandoff`. **v29.0 = 0 migration baru** (SAVE-01 dedupe, BUKAN filtered-index — tak perlu flag migration baru ke IT). **Sisa NOTIFY IT (carry-over lama):** 2 migration `PendingProtonBypass`+filtered-index (360) + `AddShuffleTogglesToAssessmentSession` (372) → IT apply DB Dev + promosi server Dev (10.55.3.3)/Prod.
2. **`/gsd-new-milestone`** — mulai milestone berikut (recreate REQUIREMENTS.md). Kandidat backlog tersisa: 999.9 label kosmetik (LOW). (999.8/999.6/999.10/999.7 ditutup v28.0.)
3. **Opsional v29.0 follow-up:** CERT-01 konfirmasi visual browser (1 UAT pixel) + I-1 type-aware pre-check (1-line). Non-blocker.

## Tag Git

- `v24.0`, `v25.0`, `v26.0`, `v27.0`, `v28.0` — ✅ PUSHED ke `origin/ITHandoff` 2026-06-14.
- `v29.0` — ✅ dibuat saat manual close 2026-06-15 + PUSHED `origin/ITHandoff`. Annotated (Assessment E2E Worker-Success Fix, 380-382, 11/11 REQ, 0 migration).

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
| RES-02 (display-drift X/Y vs Score%) · GRD-02 (empty-MA SetEquals LOW) | backlog (deferred saat planning v29.0) |

### Backlog aktif (belum dipromote)

| Item | Reason |
|------|--------|
| 999.9 label residu "Backfill/Restore" di UI BulkBackfill | kosmetik (LOW) |
| 999.6 impersonate identity (dir tersisa) | sudah ditutup fungsional v28.0/377; dir backlog tinggal |
| 999.10 route CMP (dir tersisa) | sudah ditutup v28.0/378; dir backlog tinggal |

> ✅ Ditutup di v28.0 (2026-06-14): 999.8 essay→376 (GRADE), 999.6 impersonate→377 (IMP), 999.10 route→378 (CMPRT), 999.7 e2e→379 (E2E).

### Push IT

| Item | Status |
|------|--------|
| Push bundle v24-v28 ke `origin/ITHandoff` (branch + 5 tag) | ✅ PUSHED 2026-06-14, HEAD `bb8c04ed` |
| Push v29.0 (branch + tag `v29.0`) ke `origin/ITHandoff` | ✅ PUSHED 2026-06-15 |
| Notify IT — 2 migration carry (`PendingProtonBypass`+index/360, `ShuffleToggles`/372). **v29.0 = 0 migration baru.** | ⏳ PENDING — kasih commit hash + flag ke IT |
| IT apply migration DB Dev + promosi server Dev (10.55.3.3)/Prod | ⏳ tanggung jawab IT (bukan dev) |

## Accumulated Context

### Decisions (persist across milestones)

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

Last activity: 2026-06-15 — v29.0 manual append-only close + PUSHED origin/ITHandoff (auto-chain pasca-382: secure 12/12 → validate 415/415 → verify [pakai hasil sesi lain, 1 PENDING] → audit PASSED 11/11 → HTML awam → close → push).

Next action: **Notify IT** (carry migration 360/372; v29.0 = 0 migration baru) ATAU `/gsd-new-milestone` untuk siklus berikut. Opsional v29.0 follow-up: CERT-01 visual UAT + I-1 type-aware pre-check. JANGAN edit DB/kode Dev/Prod (CLAUDE.md).
