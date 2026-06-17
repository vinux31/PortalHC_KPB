---
gsd_state_version: 1.0
milestone: v32.1
milestone_name: Perbaikan Teks & Desain
status: executing
stopped_at: Phase 389 context gathered
last_updated: "2026-06-17T03:59:46.791Z"
last_activity: 2026-06-17 -- Phase 388 execution started
progress:
  total_phases: 24
  completed_phases: 0
  total_plans: 2
  completed_plans: 0
  percent: 0
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 388 — Label Hasil + CoachWorkload Polish

## Current Position

Phase: 388 (Label Hasil + CoachWorkload Polish) — EXECUTING
Plan: 1 of 2
Status: Executing Phase 388
Last activity: 2026-06-17 -- Phase 388 execution started

## Next Action

`/gsd-plan-phase 388` (Label Hasil + CoachWorkload Polish — LBL-03 + DSN-04 + DSN-05).

Urutan fase v32.1:

- **Phase 388** — LBL-03 (`Results.cshtml` label "Batas Nilai Kelulusan") + DSN-04/05 (CoachWorkload polish: filter+heading→card, hapus inline font-size, spacing). File DISJOINT, low-risk.
- **Phase 389** — DSN-01/02/03 (CoachCoacheeMapping → accordion card per coach + toolbar seragam + hapus dead-onclick). RISK TERTINGGI (behavior regression modal/AJAX/collapse). File terisolasi → boleh paralel dgn 388.
- **Phase 390** — DSN-06 (Test & UAT behavior parity penutup; depends 388 + 389).

Constraint global: behavior parity WAJIB, 0 backend, 0 migration, 0 controller. Verifikasi tiap phase: dotnet build + dotnet run (localhost:5277) + Playwright + UAT browser. Arah desain terkunci (brainstorm + visual-companion). 1 deploy IT di akhir milestone (migration=FALSE).

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
| **v32.1** — semua LBL/DSN = 0 migration, 0 backend; target 1 push → IT re-deploy Dev | ⏳ pending (roadmap dibuat, belum di-plan/execute) |

## Accumulated Context

### Decisions (persist across milestones)

- [v32.1 roadmap / phases 388-390]: 3 phase derived by file-overlap + risk — 388 (LBL-03 Results + DSN-04/05 CoachWorkload polish, DISJOINT low-risk), 389 (DSN-01/02/03 CoachCoacheeMapping accordion card opsi "B", RISK TERTINGGI), 390 (DSN-06 Test & UAT parity penutup). 7/7 REQ mapped, 0 orphan/duplicate, 0 migration. Arah desain terkunci: accordion card per coach (header avatar inisial+nama+section+badge beban warna-ikut-threshold existing >=8 merah / >=5 kuning / else info) + CoachWorkload polish-only. Risiko utama = behavior regression modal/AJAX/collapse.
- [v31.0 Hotfix Pra-Ujian Lisensor / phases 385-387]: 14/14 PXF closed, 0 migration. Pattern kunci: shared display helper `AssessmentScoreAggregator.IsQuestionCorrect` + `BuildAnswerCell` (MA all-or-nothing SetEquals, essay Benar=`>0`) dipakai 1× lintas web Results + PDF + Excel (kill-drift); essay PathBase-aware sub-path `/KPB-PortalHC`; predikat pending essay TUNGGAL `!IsNullOrWhiteSpace(TextAnswer) && EssayScore==null` byte-identik 4 surface; aria opsi huruf A/B/C/D (lesson: a11y Razor dinamis WAJIB Playwright runtime assert, grep+build INSUFFICIENT — Phase 354). MERGED→main 7ea6c81e, UAT Dev full-lifecycle PASS 2026-06-16.
- [v30.0 / ECG]: helper terpusat `AssessmentScoreAggregator.IsQuestionCorrect` (essay `>0`=Benar, `=0`=Salah, `null`=pending) di `CMPController.Results` 4 site + PDF export (kill-drift). MA non-empty guard display-path beda dari scoring `Compute`.
- [v29.0 / 382]: SAVE-01 dedupe last-write-wins in-memory, NO migration.
- [v27.0 / SHUF]: Shuffle Toggle 2 sistem independen default ON; engine pure `Helpers/ShuffleEngine.cs`.
- [v25.0 / A-4]: Penanda kelulusan Proton lewat 1 helper bersama `ProtonCompletionService` (Origin).
- [v24.0 / spec §9]: Hapus file gambar pola Phase 333/335 — kumpul path SEBELUM tx, File.Delete SETELAH commit, warn-only.
- [v22.0 cross-milestone]: `AssessmentConstants.AssessmentStatus.PendingGrading` = single source of truth label lintas 11+ surface.
- [v21.0 / org labels]: tier label org (Bagian/Unit/Sub-unit) configurable via `IOrgLabelService` + global `@inject OrgLabels` (110 calls / 26 views) — **relevan v32.1** (CoachCoacheeMapping pakai `@OrgLabels.GetLabel(0/1)` di header kolom coachee).
- [v12.0]: AdminController dipecah jadi 8 controller per domain; URL tetap via [Route]. (CoachCoacheeMapping/CoachWorkload actions di `CoachMapping`/`Admin` controller — view-only edit v32.1.)

### UI/Design baseline (relevan v32.1)

- Design language app = Bootstrap 5 + Bootstrap Icons (`bi-*`); card idiom `card border-0 shadow-sm`; badge warna status; modal Bootstrap; AJAX fetch + `RequestVerificationToken`.
- `CoachWorkload.cshtml` sudah pakai card idiom dgn benar (summary cards + chart card + table card) → polish-only baseline. Yang BELUM ber-card: filter bar (`<form>` telanjang L114) + heading "Saran Penyeimbangan" (`<h5>` telanjang L229). Inline magic-number: `style="font-size:11px"` (L93), `style="font-size:12px"` (L104), + style inline lain (legend chart, chevron transition).
- `CoachCoacheeMapping.cshtml` = tabel grouped telanjang (thead `table-dark` + baris coach `table-primary` collapse) → target redesign accordion card. Badge beban existing: `ActiveCount>=8 bg-danger / >=5 bg-warning / else bg-info` (L262). Dead-code `onclick` di tombol "Tambah Mapping" (L58). Kolom coachee existing 10: Nama/NIP/@OrgLabels.GetLabel(0)/@OrgLabels.GetLabel(1)/Jabatan/ProtonTrack/Status/Mulai/Coachee Aktif/Aksi.
- Aksi/JS existing CoachCoacheeMapping (WAJIB parity): `openEditModal`, `confirmDeactivate`, `reactivateMapping`, `confirmDelete`, form `MarkMappingCompleted` (Graduated), modal `#assignModal`/`#editModal`/`#importMappingModal`, filter `resetPageAndSubmit`, pagination.
- Helper JS app: `appUrl(path)` / `window.basePath` untuk PathBase-aware URL (WAJIB pakai di fetch, jangan hardcode `/Admin/...` / `/CoachMapping/...`).

### Open Blockers/Concerns

- [push] Carry migration lama (Origin, PendingProtonBypass+index/360, ShuffleToggles/372) — notify IT flag. v28-v32 = 0 migration baru.
- v32.1 = pure view/JS edit → risiko utama = **behavior regression** (modal/AJAX/collapse wiring CoachCoacheeMapping — Phase 389 RISK TERTINGGI). Verifikasi: build + Playwright + UAT browser semua aksi existing (assign/edit/nonaktif/graduated/hapus/reactivate/import/export + threshold/saran). DSN-06 = phase Test & UAT parity penutup (Phase 390).

## Session Continuity

Last activity: 2026-06-17

Stopped at: Phase 389 context gathered

Next action: `/gsd-plan-phase 388` (Label Hasil + CoachWorkload Polish). Lalu 389 (CoachCoacheeMapping redesign), 390 (Test & UAT parity). Approve roadmap dulu bila perlu review user.
