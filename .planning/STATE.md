---
gsd_state_version: 1.0
milestone: v28.0
milestone_name: Assessment & Records Bug Fixes
status: between_milestones
last_updated: "2026-06-14T09:53:33Z"
last_activity: 2026-06-14
progress:
  total_phases: 16
  completed_phases: 4
  total_plans: 16
  completed_plans: 16
  percent: 100
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Between milestones — v28.0 CLOSED 2026-06-14. Planning next cycle / push IT.

## Current Position

**BETWEEN MILESTONES.** v28.0 SHIPPED LOCAL + audited PASSED + closed (manual append-only) 2026-06-14. Archive `milestones/v28.0-{ROADMAP,REQUIREMENTS}.md`, audit `v28.0-MILESTONE-AUDIT.md` (root), tag `v28.0` (lokal). REQUIREMENTS.md dihapus dari root (fresh saat `/gsd-new-milestone`).

**Next:** `/gsd-new-milestone` (mulai siklus berikut) ATAU **Push IT** (bundle v24-v28 ke ITHandoff). Phase dir 352-379 tetap di `phases/` aktif (pola v24; cleanup via `/gsd-cleanup` nanti).

Predecessor: v25.0 + v26.0 + v27.0 + v28.0 SHIPPED LOCAL + audited PASSED + closed 2026-06-14 (v25/26/27 joint safe-close; v28.0 manual append-only).

| Milestone | Phases | REQ | Audit | Archive |
|-----------|--------|-----|-------|---------|
| v25.0 Proton Kelulusan & Bypass | 358-368 | 20/20 PCOMP/PBYP | PASSED | milestones/v25.0-ROADMAP.md |
| v26.0 Urgent Search & Records Visibility | 369-371 | 3/3 URG | PASSED | milestones/v26.0-ROADMAP.md |
| v27.0 Shuffle Toggle | 372-375 | 16/16 SHUF | PASSED | milestones/v27.0-ROADMAP.md |
| v28.0 Assessment & Records Bug Fixes | 376-379 | 6/6 GRADE/IMP/CMPRT/E2E | PASSED | milestones/v28.0-ROADMAP.md |

Predecessor: v24.0 ✅ SHIPPED LOCAL + closed 2026-06-09 (352-357, 25/25 REQ).

## Next Action

1. **Push IT** (prioritas handoff) — bundle v24-v28 belum push (branch ITHandoff, ahead origin/ITHandoff). **3 migration** wajib flag IT: `Origin` (358), `PendingProtonBypass`+filtered-index (360), `AddShuffleTogglesToAssessmentSession` (372). v28.0 = 0 migration. Catatan: migration 358 sudah di origin/ITHandoff; 360+372 di delta unpushed.
2. **`/gsd-new-milestone`** — mulai milestone berikut (recreate REQUIREMENTS.md). Kandidat backlog tersisa: 999.9 label kosmetik (LOW). (999.8/999.6/999.10/999.7 SUDAH ditutup di v28.0.)
3. **`/gsd-cleanup`** — arsipkan phase dir 352-379 dari `phases/` aktif (opsional, kapan saja).

## Tag Git (lokal, belum push)

- `v25.0`, `v26.0`, `v27.0` — dibuat saat joint-close 2026-06-14. Push bareng bundle ke IT.
- `v28.0` — dibuat saat manual close 2026-06-14. Push bareng bundle ke IT.

## Deferred Items

### v15.0 Deferred (carry-over)

| REQ | Item | Status | Due |
|-----|------|--------|-----|
| EPRV-01 | Preview Essay rubrik/jawaban — Jalur A (label) vs Jalur B (field baru) | menunggu user verifikasi save/load Rubrik | 2026-05-12 |

### Carry-over dari v14.0 close (2026-04-24)

| Category | Item | Status | Source |
|----------|------|--------|--------|
| UAT | Phase 303 Plan 02 Task 3 — Coach Workload 12-langkah human verification | paused-at-checkpoint | HANDOFF.json (2026-04-10) |
| UAT | Phase 235 — 5 items butuh human verification via browser | pending | STATE.md (prior) |
| UAT | Phase 247 approval chain — 2 TODO (HC review + resubmit notification) | pending | STATE.md (prior) |
| Research gap | Phase 297 Pre-Post Renewal behavior — keputusan 2 sesi baru otomatis | undecided | v14.0 planning |
| Research gap | Phase 298 essay max character limit — nvarchar(max) vs nvarchar(2000) | undecided | v14.0 planning |
| Blocker | Phase 293 `GetSectionUnitsDictAsync` Level 2+ support | undecided | v13.0 carry-over |
| v11.2 paused | Phase 281 (System Settings) + Phase 285 (Dedicated Impersonation Page) | paused | MILESTONES.md v11.2 |

### Backlog aktif (belum dipromote)

| Item | Reason |
|------|--------|
| 999.9 label residu "Backfill/Restore" di UI BulkBackfill | kosmetik (LOW) |

> ✅ Ditutup di v28.0 (2026-06-14): 999.8 essay→376 (GRADE), 999.6 impersonate→377 (IMP), 999.10 route→378 (CMPRT), 999.7 e2e→379 (E2E).

### Push pending IT

| Item | Status |
|------|--------|
| Push bundle v24-v28 (3 migration: Origin, PendingProtonBypass+index, ShuffleToggles; v28.0=0 migration) | pending IT availability; branch ITHandoff NOT PUSHED |

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
- Phase 293 `GetSectionUnitsDictAsync` — hardcoded 2-level (keputusan tertunda).

## Session Continuity

Last activity: 2026-06-14

Next action: **Push IT** (bundle v24-v28, 3 migration) ATAU `/gsd-new-milestone` untuk siklus berikut. JANGAN edit DB/kode Dev/Prod (CLAUDE.md).
