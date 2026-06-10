---
gsd_state_version: 1.0
milestone: v25.0
milestone_name: Proton Kelulusan & Bypass
status: executing
last_updated: "2026-06-10T04:05:47.397Z"
last_activity: 2026-06-10
progress:
  total_phases: 16
  completed_phases: 1
  total_plans: 8
  completed_plans: 6
  percent: 75
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-09)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 359 — gate-berurutan-cleanup-a

## Current Position

Milestone: v25.0 — Proton Kelulusan & Bypass (4 phase 358-361)
Phase: 359 (gate-berurutan-cleanup-a) — EXECUTING
Plan: 3 of 4
Status: Ready to execute
Last activity: 2026-06-10

Predecessor: v24.0 ✅ SHIPPED LOCAL + audited + closed 2026-06-09 (phases 352-357, 25/25 REQ, archive milestones/v24.0-*). Bundle v19-v23 sudah ke IT; v24.0 belum push (branch ITHandoff).

## v25.0 Phase Map

| Phase | Goal | REQ | Migration | Depends on | UI |
|-------|------|-----|-----------|-----------|-----|
| **358** Penanda Kelulusan (fondasi A) | Origin + helper `ProtonCompletionService` + wire GradingService (exam lulus + re-grade flip) + refactor SubmitInterviewResults + backfill (cek 100%) → exam Tahun 1/2 lulus tercatat "Lulus" | PCOMP-01..05 | **true** (`Origin`) | — | no |
| **359** Gate Berurutan + Cleanup (A) | ProtonYearGate + gate eligibility server-side (CreateAssessment) + gate antar-tahun + Tahun 3 data-driven + graduation gate + matikan tampilan level | PCOMP-06..10 | false | 358 | yes |
| **360** Bypass Backend (B) | migration `PendingProtonBypass` + closure CL-A/B(a)/B(b)/C + notif `PROTON_BYPASS_READY` (GradingService hook) + coach handling (E15) + bootstrap-by-unit + 6 endpoint | PBYP-01..07 | **true** (`PendingProtonBypass`) | 358 (helper+Origin), 359 (gate-exempt logical) | partial |
| **361** Bypass UI (B) | Tab2 redesign + wizard 3-langkah + panel pending + notif deep-link + e2e UAT | PBYP-08..10 | false | 360 | yes |

**Roadmap shaping notes:**

- Dua spec final: A (`...-proton-completion-logic-design.md`) + B (`...-proton-bypass-tahun-design.md`). B depends A → implement+verify A dulu.
- **Split A → 2 fase** sesuai pola granular proyek: 358 penanda (fix bug "Tahun 1/2 gak pernah Lulus", shippable sendiri) + 359 gate (urutan dipaksa + UI bersih).
- **Split B → 2 fase**: 360 backend (migration+logic+endpoint) + 361 frontend (Tab2 wizard). SEMUA 6 endpoint di 360; 361 murni UI.
- **File-overlap sequencing:** GradingService disentuh 358 (penanda) + 360 (notif hook) → 360 setelah 358. AssessmentAdminController disentuh 358 (SubmitInterviewResults) + 359 (CreateAssessment gate) → beda method, sekuensial. Maka urutan strict 358→359→360→361.
- **2 migration:** `Origin` (358) + `PendingProtonBypass` (360). Notify IT keduanya (DEV_WORKFLOW). Snapshot DB lokal sebelum apply (SEED_WORKFLOW).
- **Verifikasi lokal wajib (CLAUDE.md):** tiap phase `dotnet build` + `dotnet run` localhost:5277 + Playwright (UI) sebelum commit. ❌ tidak ada edit di Dev/Prod. AD lokal: `Authentication__UseActiveDirectory=false dotnet run`.
- **Out of scope:** audit Tab1 + undo-executed → backlog 999.x.

## Next Action

1. **`/gsd-plan-phase 358`** — Penanda Kelulusan. Input: spec A §4.1/4.7/4.8 + plan `docs/superpowers/plans/2026-06-09-proton-completion-logic.md` (Task 1,3,4,5,10). **Migration** (`Origin`) — snapshot DB lokal sebelum apply. Verifikasi build + ef update + xUnit.
2. **Carry-over IT promo** — v24.0 (352-357) belum push (branch ITHandoff). v25.0 nambah 2 migration ke batch.

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

### Backlog (v25.0 deferred)

| Item | Reason |
|------|--------|
| Audit/improve Tab1 Override Deliverable (DeliverableStatusHistory, warning un-approve penanda-Lulus, RejectedById) | spec B §13 out of scope; audit lebih akurat setelah A jalan |
| Undo bypass executed (tombol) | spec B §8.2 Opsi C — koreksi via bypass lagi; butuh PreviousStatus kalau dibangun |
| 999.3 cascade-image-cleanup, 999.4 e2e-baseline, 999.5 coach-test-hardening | v24.0 carry-over backlog |

### Push pending IT

| Item | Status |
|------|--------|
| Push bundle v24.0 (352-357) + nanti v25.0 (~2 migration) | pending IT availability; branch ITHandoff |

## Quick Tasks Completed

| Date | Slug | Description |
|------|------|-------------|
| 2026-05-26 | cdp-portal-platform-rename | Rename CDP label "Competency Development Portal" → "Platform" (parity dgn CMP). |

## Accumulated Context

### Decisions (persist across milestones)

- [v25.0 / A-2]: Approve deliverable Proton cuma L4 (Sr SPV **atau** SH; 1 approver cukup, co-sign opsional). HC = final review, BUKAN approver deliverable. Logic approval TIDAK diubah.
- [v25.0 / A-3]: `CompetencyLevelGranted` dimatikan — `ProtonFinalAssessment` = penanda "Lulus/Selesai" murni. Kolom dormant (tidak di-drop).
- [v25.0 / A-4]: Penanda kelulusan Proton dibuat lewat 1 helper bersama (`ProtonCompletionService`) — 3 jalur exam/interview/bypass, dibedakan kolom `Origin`.
- [v24.0 / spec §8 Gap 1]: Sinkron Pre→Post gambar = shared-file (string path copy), BUKAN file fisik digandakan.
- [v24.0 / spec §9]: Hapus file gambar pakai pola Phase 333/335 — kumpul path SEBELUM tx, File.Delete SETELAH commit, inner try/catch warn-only.
- [v23.0 / Phase 350]: REC-06 D-07 invariant LOCKED — search assessment-title filter di level worker (post-load), badge/count per-worker utuh.
- [v22.0 cross-milestone]: `AssessmentConstants.AssessmentStatus.PendingGrading` = single source of truth label lintas 11+ surface; exclude-pending denominator konsisten.
- [v14.0 / Phase 296]: GradeFromSavedAnswers dihapus — GradingService satu-satunya source of truth grading.
- [v13.0]: SortableJS 1.15.7 via CDN; drag-drop sibling-only; orgTree.js single orchestrator.
- [v12.0]: AdminController dipecah jadi 8 controller per domain; URL tetap via [Route].
- [v21.0]: Configurable display labels via cached `IOrgLabelService` + global `@inject`.

### Open Blockers/Concerns

- [v25.0] 2 migration (`Origin` 358, `PendingProtonBypass` 360) — snapshot DB lokal sebelum apply; notify IT flag migration saat shipped.
- [v25.0] Tahun 3 deliverable: gate jalan otomatis HANYA kalau silabus Tahun 3 diisi deliverable (tugas data/admin, di luar kode).
- Phase 293 `GetSectionUnitsDictAsync` — hardcoded 2-level (keputusan tertunda).

### Roadmap Evolution

- v25.0 added (2026-06-09): Proton Kelulusan & Bypass — 4 phase 358-361 dari 2 spec brainstorm (A completion-logic + B bypass-tahun). Diskusi A nemu gap: `ProtonFinalAssessment` cuma terbit interview Tahun 3 → Tahun 1/2 gak pernah "Lulus" (BUG). B depends A. Split A→2 (penanda/gate), B→2 (backend/UI) sesuai pola granular.
- v24.0 shipped+closed (2026-06-09): Gambar di Soal Assessment, phases 352-357.

## Session Continuity

Last activity: 2026-06-09 — Milestone v25.0 started (PROJECT.md + STATE.md + REQUIREMENTS.md + ROADMAP.md). 4 phase 358-361, 20 REQ (PCOMP-01..10 + PBYP-01..10). 2 spec final (A completion-logic, B bypass) + plan A (`docs/superpowers/plans/2026-06-09-proton-completion-logic.md`, 11 task, split per fase 358/359 — ProtonYearGate pindah ke 359).

Next action: `/gsd-plan-phase 358` — Penanda Kelulusan. Implement + verify A (358+359) DULU, baru B (360+361). Migration `Origin` di 358 (snapshot DB dulu). Verifikasi `dotnet build` + `dotnet ef database update` + `dotnet run` localhost:5277 + xUnit sebelum commit (CLAUDE.md Develop Workflow).
