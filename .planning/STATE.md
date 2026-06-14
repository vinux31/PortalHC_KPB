---
gsd_state_version: 1.0
milestone: v28.0
milestone_name: Assessment & Records Bug Fixes
status: ready_to_plan
last_updated: "2026-06-14T00:00:00.000Z"
last_activity: 2026-06-14
progress:
  total_phases: 4
  completed_phases: 0
  total_plans: 0
  completed_plans: 0
  percent: 0
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** v28.0 Assessment & Records Bug Fixes — roadmap ready, next plan Phase 376 (started 2026-06-14)

## Current Position

**Milestone v28.0** — roadmap created (4 phase 376-379, 6 REQ mapped). Ready to plan. Phase: 376 (not started). 4 bug promote backlog: 999.8 essay→376 (GRADE), 999.6 impersonate→377 (IMP), 999.10 route→378 (CMPRT), 999.7 e2e→379 (E2E). 0 migration. No research (bug-fix).

**Next:** `/gsd-plan-phase 376` (atau `/gsd-discuss-phase 376` — 376 & 377 butuh diagnose/audit dulu). Phase 376/377/378 independent; 379 depends 376.

Predecessor: v25.0 + v26.0 + v27.0 SHIPPED LOCAL + audited PASSED + closed (joint safe-close) 2026-06-14 (archive `milestones/v2{5,6,7}.0-*`).

| Milestone | Phases | REQ | Audit | Archive |
|-----------|--------|-----|-------|---------|
| v25.0 Proton Kelulusan & Bypass | 358-368 | 20/20 PCOMP/PBYP | PASSED | milestones/v25.0-ROADMAP.md |
| v26.0 Urgent Search & Records Visibility | 369-371 | 3/3 URG | PASSED | milestones/v26.0-ROADMAP.md |
| v27.0 Shuffle Toggle | 372-375 | 16/16 SHUF | PASSED | milestones/v27.0-ROADMAP.md |

REQUIREMENTS.md di-split ke 3 arsip + dihapus dari root (fresh saat `/gsd-new-milestone`). Phase dir 358-375 tetap di `phases/` aktif (pola v24; cleanup via `/gsd-cleanup` nanti).

Predecessor: v24.0 ✅ SHIPPED LOCAL + closed 2026-06-09 (352-357, 25/25 REQ).

## Next Action

1. **Push IT** (prioritas handoff) — bundle v24-v27 belum push (branch ITHandoff, ahead origin/ITHandoff). **3 migration** wajib flag IT: `Origin` (358), `PendingProtonBypass`+filtered-index (360), `AddShuffleTogglesToAssessmentSession` (372). Catatan: migration 358 sudah di origin/ITHandoff; 360+372 di delta unpushed.
2. **`/gsd-new-milestone`** — mulai milestone berikut (recreate REQUIREMENTS.md). Kandidat backlog: 999.8 essay-grading prod bug (HIGH), 999.10 CMP route 500, 999.6 impersonate, 999.9 label kosmetik.
3. **`/gsd-cleanup`** — arsipkan phase dir 358-375 dari `phases/` aktif (opsional, kapan saja).

## Tag Git (lokal, belum push)

- `v25.0`, `v26.0`, `v27.0` — dibuat saat joint-close 2026-06-14. Push bareng bundle ke IT.

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
| 999.8 essay-grading prod bug (Score=0 walau grade+finalize) | SUSPECTED PRODUCTION BUG, domain assessment — prioritas audit/fix |
| 999.10 /CMP/CertificationManagement 500 view-not-found | pre-existing, route orphan |
| 999.6 impersonate identity tak dipakai query worker surfaces | dari brainstorm delete-records batch-1 |
| 999.9 label residu "Backfill/Restore" di UI BulkBackfill | kosmetik |
| 999.7 e2e exam-taking migrasi 10 flow ke wizard | flat-form usang |

### Push pending IT

| Item | Status |
|------|--------|
| Push bundle v24-v27 (3 migration: Origin, PendingProtonBypass+index, ShuffleToggles) | pending IT availability; branch ITHandoff NOT PUSHED |

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

- **999.8 SUSPECTED PRODUCTION BUG** (essay-grading): essay-only finalize biarkan `AssessmentSessions.Score=0` walau grade(80)+finalize. Belum didiagnosis (produksi tak disentuh). Sinyal domain assessment — prioritaskan.
- [push] 3 migration (Origin, PendingProtonBypass+index, ShuffleToggles) — notify IT flag migration saat push; 360+372 di delta unpushed.
- Phase 293 `GetSectionUnitsDictAsync` — hardcoded 2-level (keputusan tertunda).

## Session Continuity

Last activity: 2026-06-14 — Joint safe-close v25.0+v26.0+v27.0 (semua audited PASSED + hygiene flip). 6 arsip dibuat, ROADMAP collapse 3 one-liner, REQUIREMENTS split+hapus, 3 tag git lokal. Phase 375 (v27 terakhir) closed+secured+validated sesi ini.

Next action: **Push IT** (bundle v24-v27, 3 migration) ATAU `/gsd-new-milestone` untuk siklus berikut. JANGAN edit DB/kode Dev/Prod (CLAUDE.md).
