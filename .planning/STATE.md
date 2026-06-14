---
gsd_state_version: 1.0
milestone: v22.0
milestone_name: CMP-06 Residual Fix + CMP/Records + ManageAssessment/Monitoring Audit
status: planning
last_updated: "2026-06-14T13:42:26.901Z"
last_activity: 2026-06-14 — v29.0 ROADMAP.md written (3 phase 380-382 SEQUENTIAL A→B→C, 11 REQ WSE-01..11, 2-5 success criteria/phase grounded di E2E scenario 1-12). Coverage 11/11 mapped, 0 orphan. Audit-driven from `docs/assessment-audit/2026-06-14-E2E-worker-success-FOCUS.md`.
progress:
  total_phases: 24
  completed_phases: 0
  total_plans: 0
  completed_plans: 0
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** v29.0 Assessment E2E Worker-Success Fix — roadmap created (Phases 380-382), ready to plan (started 2026-06-14).

## Current Position

Phase: Not started (roadmap complete — awaiting plan)
Plan: —
Status: Roadmap created → ready to plan Phase 380
Last activity: 2026-06-14 — v29.0 ROADMAP.md written (3 phase 380-382 SEQUENTIAL A→B→C, 11 REQ WSE-01..11, 2-5 success criteria/phase grounded di E2E scenario 1-12). Coverage 11/11 mapped, 0 orphan. Audit-driven from `docs/assessment-audit/2026-06-14-E2E-worker-success-FOCUS.md`.

**v29.0 scope:** worker bisa ujian+lulus E2E (Normal+PrePost single-answer NON-Proton). 11 REQ WSE-01..11. Phase 380(A) admin/engine integrity · 381(B) worker entry · 382(C) grading/lifecycle/cert (+1 migration). Eksekusi SERI. Defer backlog RES-02/GRD-02. OUT: Proton/essay/multi-answer/admin-data-gov.

**Next:** `/gsd-plan-phase 380` (roadmap sudah di-tulis; phases LOCKED). Eksekusi SERI 380→381→382 (semua sentuh CMPController.cs, no paralel).

**v28.0 carry (push IT):** ✅ Push v24-v28 ke `origin/ITHandoff` DONE (HEAD `bb8c04ed`). Sisa: notify IT 2 migration (PendingProtonBypass+index/360, ShuffleToggles/372) → IT promosi Dev/Prod. v29.0 akan +1 migration (filtered-index PackageUserResponse/382).

Predecessor: v25.0 + v26.0 + v27.0 + v28.0 SHIPPED LOCAL + audited PASSED + closed 2026-06-14 (v25/26/27 joint safe-close; v28.0 manual append-only).

| Milestone | Phases | REQ | Audit | Archive |
|-----------|--------|-----|-------|---------|
| v25.0 Proton Kelulusan & Bypass | 358-368 | 20/20 PCOMP/PBYP | PASSED | milestones/v25.0-ROADMAP.md |
| v26.0 Urgent Search & Records Visibility | 369-371 | 3/3 URG | PASSED | milestones/v26.0-ROADMAP.md |
| v27.0 Shuffle Toggle | 372-375 | 16/16 SHUF | PASSED | milestones/v27.0-ROADMAP.md |
| v28.0 Assessment & Records Bug Fixes | 376-379 | 6/6 GRADE/IMP/CMPRT/E2E | PASSED | milestones/v28.0-ROADMAP.md |

Predecessor: v24.0 ✅ SHIPPED LOCAL + closed 2026-06-09 (352-357, 25/25 REQ).

## Next Action

1. ✅ **Push IT — DONE 2026-06-14.** Branch `ITHandoff` (454 commit) + tag `v24-v28.0` pushed ke `origin/ITHandoff` (remote=local, synced). HEAD `bb8c04ed`. **Sisa = NOTIFY IT**: 2 migration baru wajib flag — `PendingProtonBypass`+filtered-index (360) + `AddShuffleTogglesToAssessmentSession` (372). `Origin` (358) sudah lama di remote. v28.0 = 0 migration. IT lalu apply migration di DB Dev + promosi server Dev (10.55.3.3)/Prod (tanggung jawab IT, bukan dev).
2. **`/gsd-new-milestone`** — mulai milestone berikut (recreate REQUIREMENTS.md). Kandidat backlog tersisa: 999.9 label kosmetik (LOW). (999.8/999.6/999.10/999.7 SUDAH ditutup di v28.0.)
3. ✅ **Cleanup — DONE 2026-06-14.** Phase dir 352-379 (27 dir, v24-v28) sudah dipindah ke `milestones/vXX.0-phases/`. Sisa di `phases/`: cuma backlog 999.6/999.9/999.10.

## Tag Git

- `v24.0`, `v25.0`, `v26.0`, `v27.0`, `v28.0` — ✅ PUSHED ke `origin/ITHandoff` 2026-06-14.

## Deferred Items

> ✅ **ACCEPTED OK 2026-06-14** (keputusan user): semua carry-over v11.2/v13/v14/v15 di bawah = **phase lama, dianggap OK / non-blocking** (kode sudah ship + jalan; tak ada bug report di milestone v16-v28). Bukan pekerjaan tertunda aktif. Tetap dicatat sebagai histori, bukan TODO. Buka lagi hanya bila muncul bug/kebutuhan nyata.

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

### Backlog aktif (belum dipromote)

| Item | Reason |
|------|--------|
| 999.9 label residu "Backfill/Restore" di UI BulkBackfill | kosmetik (LOW) |

> ✅ Ditutup di v28.0 (2026-06-14): 999.8 essay→376 (GRADE), 999.6 impersonate→377 (IMP), 999.10 route→378 (CMPRT), 999.7 e2e→379 (E2E).

### Push IT — ✅ DONE 2026-06-14

| Item | Status |
|------|--------|
| Push bundle v24-v28 ke `origin/ITHandoff` (branch + 5 tag) | ✅ PUSHED 2026-06-14, HEAD `bb8c04ed`, remote synced |
| Notify IT — 2 migration baru (`PendingProtonBypass`+index/360, `ShuffleToggles`/372) | ⏳ PENDING — kasih commit hash + flag ke IT |
| IT apply migration DB Dev + promosi server Dev (10.55.3.3)/Prod | ⏳ tanggung jawab IT (bukan dev) |

## Accumulated Context

### Decisions (persist across milestones)

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
- [push] 3 migration (Origin, PendingProtonBypass+index, ShuffleToggles) — notify IT flag migration saat push; 360+372 di delta unpushed. (v28.0 = 0 migration.)
- ✅ Phase 293 `GetSectionUnitsDictAsync` hardcoded 2-level — accepted-OK (user 2026-06-14; org 2-level cukup, buka bila butuh >2 level).

## Session Continuity

Last activity: 2026-06-14

Next action: ✅ Push IT DONE (v24-v28 di `origin/ITHandoff`, HEAD `bb8c04ed`). Sisa: notify IT 2 migration (360+372) → IT promosi Dev/Prod. Lalu `/gsd-new-milestone` untuk siklus berikut. JANGAN edit DB/kode Dev/Prod (CLAUDE.md).
