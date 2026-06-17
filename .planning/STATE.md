---
gsd_state_version: 1.0
milestone: v32.1
milestone_name: Perbaikan Teks & Desain
status: Defining requirements
stopped_at: "Milestone v32.1 started — PROJECT.md updated, defining requirements"
last_updated: "2026-06-17T00:00:00.000Z"
last_activity: 2026-06-17
progress:
  total_phases: 0
  completed_phases: 0
  total_plans: 0
  completed_plans: 0
  percent: 0
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** v32.1 Perbaikan Teks & Desain STARTED 2026-06-17 (branch ITHandoff; main pegang v32.0). Pure UI/teks polish 3 surface — LBL-03 + DSN-01..05, 0 backend, 0 migration.

## Current Position

Phase: Not started (defining requirements)
Plan: —
Status: Defining requirements
Last activity: 2026-06-17 — Milestone v32.1 started

## Next Action

Selesaikan REQUIREMENTS.md (LBL-03 + DSN-01..05) → roadmapper (mulai Phase 388) → `/gsd-plan-phase 388`.

Scope terkunci (brainstorm + visual-companion):
- **LBL-03** label "Nilai Kelulusan" → "Batas Nilai Kelulusan" (`Views/CMP/Results.cshtml:60`)
- **DSN-01/02/03** CoachCoacheeMapping → accordion card per coach (B) + toolbar rapi + hapus dead-onclick
- **DSN-04/05** CoachWorkload polish-only (filter+heading ke card, bersihkan inline style)
- Constraint: behavior parity, 0 backend, 0 migration.

## Tag Git

- `v24.0`..`v31.0` — ✅ tag dibuat. v29/v30 PUSHED `origin/ITHandoff`; v31.0 MERGED→main + PUSHED `origin/main` 2026-06-16 (merge `7ea6c81e`).

## Deferred Items

> ✅ **ACCEPTED OK 2026-06-14** (keputusan user): semua carry-over v11.2/v13/v14/v15 di bawah = phase lama, dianggap OK / non-blocking (kode ship + jalan; tak ada bug report v16-v31). Histori, bukan TODO. Buka lagi hanya bila muncul bug/kebutuhan nyata.

### Carry-over ACCEPTED OK (ringkas)

| Item | Status |
|------|--------|
| EPRV-01 Preview Essay rubrik (v15.0) | accepted-OK |
| Phase 303 Coach Workload 12-langkah UAT | accepted-OK (kode ship+jalan) |
| Phase 235 (5 items) + Phase 247 (2 TODO) UAT | accepted-OK |
| Phase 297 Pre-Post Renewal behavior + Phase 298 essay char limit | accepted-OK (undecided, non-blocking) |
| Phase 293 org Level 2+ support | accepted-OK (org 2-level cukup) |
| v11.2 Phase 281 (System Settings) + 285 (Impersonation Page) | accepted-OK (closed-early) |

### Backlog aktif (belum dipromote)

| Item | Reason |
|------|--------|
| 999.9 label residu "Backfill/Restore" di UI BulkBackfill | kosmetik (LOW) |
| 999.6 impersonate identity (dir tersisa) | ditutup fungsional v28.0/377; dir backlog tinggal |
| 999.10 route CMP (dir tersisa) | ditutup v28.0/378; dir backlog tinggal |
| 43 quick-task todo (audit-open, status `[missing]`) | acknowledged deferred (artifact lama hilang) |
| v31.0 Future pasca-acara (F-02/F-03/F-01/F-06/F-11/F-13/F-19/F-20/F-22) | mostly closed Phase 387 (PXF-06..14); sisa F-02/F-18 deferred |

### Push IT

| Item | Status |
|------|--------|
| Notify IT — 2 migration carry lama (`PendingProtonBypass`+index/360, `ShuffleToggles`/372) | ⏳ PENDING (carry lama; v28-v32 = 0 migration baru) |
| v31.0 deploy Dev + UAT full-lifecycle | ✅ DONE 2026-06-16 (IT deploy + browser UAT PASS; PXF-01 gambar sub-path CLOSED) |
| **v32.1** — semua LBL/DSN = 0 migration, 0 backend; target 1 push → IT re-deploy Dev | ⏳ pending (milestone aktif, belum di-plan) |

## Accumulated Context

### Decisions (persist across milestones)

- [v31.0 Hotfix Pra-Ujian Lisensor / phases 385-387]: 14/14 PXF closed, 0 migration. Pattern kunci: shared display helper `AssessmentScoreAggregator.IsQuestionCorrect` + `BuildAnswerCell` (MA all-or-nothing SetEquals, essay Benar=`>0`) dipakai 1× lintas web Results + PDF + Excel (kill-drift); essay PathBase-aware sub-path `/KPB-PortalHC`; predikat pending essay TUNGGAL `!IsNullOrWhiteSpace(TextAnswer) && EssayScore==null` byte-identik 4 surface; aria opsi huruf A/B/C/D (lesson: a11y Razor dinamis WAJIB Playwright runtime assert, grep+build INSUFFICIENT — Phase 354). MERGED→main 7ea6c81e, UAT Dev full-lifecycle PASS 2026-06-16.
- [v30.0 / ECG]: helper terpusat `AssessmentScoreAggregator.IsQuestionCorrect` (essay `>0`=Benar, `=0`=Salah, `null`=pending) di `CMPController.Results` 4 site + PDF export (kill-drift). MA non-empty guard display-path beda dari scoring `Compute`.
- [v29.0 / 382]: SAVE-01 dedupe last-write-wins in-memory, NO migration.
- [v27.0 / SHUF]: Shuffle Toggle 2 sistem independen default ON; engine pure `Helpers/ShuffleEngine.cs`.
- [v25.0 / A-4]: Penanda kelulusan Proton lewat 1 helper bersama `ProtonCompletionService` (Origin).
- [v24.0 / spec §9]: Hapus file gambar pola Phase 333/335 — kumpul path SEBELUM tx, File.Delete SETELAH commit, warn-only.
- [v22.0 cross-milestone]: `AssessmentConstants.AssessmentStatus.PendingGrading` = single source of truth label lintas 11+ surface.
- [v21.0 / org labels]: tier label org (Bagian/Unit/Sub-unit) configurable via `IOrgLabelService` + global `@inject OrgLabels` (110 calls / 26 views) — **relevan v32.1** (CoachCoacheeMapping pakai `@OrgLabels.GetLabel(0/1)`).
- [v12.0]: AdminController dipecah jadi 8 controller per domain; URL tetap via [Route]. (CoachCoacheeMapping/CoachWorkload actions di `CoachMapping`/`Admin` controller — view-only edit v32.1.)

### UI/Design baseline (relevan v32.1)

- Design language app = Bootstrap 5 + Bootstrap Icons (`bi-*`); card idiom `card border-0 shadow-sm`; badge warna status; modal Bootstrap; AJAX fetch + `RequestVerificationToken`.
- `CoachWorkload.cshtml` sudah pakai card idiom dgn benar (summary cards + chart card + table card) → polish-only baseline.
- `CoachCoacheeMapping.cshtml` = tabel grouped telanjang (thead `table-dark` + baris coach `table-primary`) → target redesign accordion card.
- Helper JS app: `appUrl(path)` / `window.basePath` untuk PathBase-aware URL (WAJIB pakai di fetch, jangan hardcode `/Admin/...`).

### Open Blockers/Concerns

- [push] Carry migration lama (Origin, PendingProtonBypass+index/360, ShuffleToggles/372) — notify IT flag. v28-v32 = 0 migration baru.
- v32.1 = pure view/JS edit → risiko utama = **behavior regression** (modal/AJAX/collapse wiring CoachCoacheeMapping). Verifikasi: build + Playwright + UAT browser semua aksi existing (assign/edit/nonaktif/graduated/hapus/reactivate/import/export + threshold/saran).

## Session Continuity

Last activity: 2026-06-17

Stopped at: Milestone v32.1 started — PROJECT.md updated (Current Milestone v32.1, v31.0 demoted ke Previous), STATE.md reset. Defining requirements (LBL-03 + DSN-01..05).

Next action: Tulis REQUIREMENTS.md → spawn roadmapper (Phase 388 dst) → approve roadmap → `/gsd-plan-phase 388`.
