---
gsd_state_version: 1.0
milestone: v32.9
milestone_name: EditQuestion Option-Edit Data Integrity (Identity-Based)
status: ready_to_plan
stopped_at: v32.9 roadmap created (1 fase, Phase 420) — ready to plan
last_updated: "2026-06-24T13:30:00.000Z"
last_activity: 2026-06-24
progress:
  total_phases: 1
  completed_phases: 0
  total_plans: 0
  completed_plans: 0
  percent: 0
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** v32.9 — EditQuestion Option-Edit Data Integrity (mendefinisikan requirements + roadmap)

## Current Position

Phase: 420 — EditQuestion Identity-Based Option Editing (not started)
Plan: —
Status: Ready to plan
Last activity: 2026-06-24 — Milestone v32.9 roadmap created (1 fase)

Milestone **v32.9 EditQuestion Option-Edit Data Integrity (Identity-Based)** — hapus/edit opsi jawaban pada soal yang SUDAH dijawab peserta tidak lagi me-relabel jawaban peserta secara senyap. Ganti upsert opsi POSISIONAL di `AssessmentAdminController.cs` EditQuestion POST menjadi **IDENTITY-based** (match baris input ke `PackageOption` existing by stable `Id`, bukan posisi) → hapus opsi tengah membuang record yang BENAR + guard answered-option (D-418-02) menyala untuk delete posisi MANAPUN. **migration=FALSE**. Branch main. Fase mulai **420** (lanjut dari 419).

## Milestone Source (verified)

- **Scope = backlog 999.15** (review Phase 418 WR-01), diverifikasi reproduces on main 2026-06-24 via 7-agen verify workflow.
- **Root cause:** loop upsert `EditQuestion` (`AssessmentAdminController.cs:8121-8160`) map input ke `existing[i]` (OrderBy Id) BY POSISI → pertahankan `PackageOption.Id` by-posisi. Form JS `ManagePackageQuestions.cshtml` (`removeOptionRow:866-878` + `reletterRows:769-826`) compact daftar saat hapus opsi tengah (A,B,C,D → hapus B → kirim A,C,D, no empty slot). Guard edit-shrink D-418-02 (`:8036-8075`) hanya flag slot EKOR (`i>=keep`) → Id opsi-B selamat, guard TAK menyala walau B sudah dijawab. Upsert lalu UPDATE record B (Id tetap) jadi teks "C". `PackageUserResponse` simpan `PackageOptionId` saja (no text snapshot, `Models/PackageUserResponse.cs:19-21`) → jawaban peserta yang dulu "B" kini menunjuk teks "C" → makna berubah senyap di review/grading/PDF.
- **Fix arah (LOCKED context):** ⚠️ in-code note `AAC:8027-8035` peringatkan upsert posisional di-LOCK spec D-418-02 → fix ubah **MEKANISME** jadi identity-based (hidden `OptionId` per baris form + match-by-Id di controller), BUKAN sekadar perketat threshold guard.
- **Regression-lock 999.14** — konversi MC/MA→Essay + penyusutan-EKOR pada soal terjawab SUDAH ditutup guard D-418-02 (`drop-noop`, jangan re-build); pertahankan tertutup (no FK-Restrict 500).
- **FK relevan:** `PackageUserResponse → PackageOption` = Restrict (`ApplicationDbContext.cs:561-564`).

## Backlog Housekeeping (lakukan saat milestone setup / close)

| Item | Verdict (verify 2026-06-24) | Aksi |
|------|------|------|
| 999.14 EditQuestion delete-answered FK-Restrict 500 | DROP — sudah ditutup guard D-418-02 (418) | hapus dari ROADMAP Backlog (regression-lock di v32.9) |
| 999.9 label Backfill/Restore | DROP — no-op, view BulkBackfill hard-removed Phase 396 (`74f266bf`), 0 source match | hapus dari ROADMAP Backlog |
| 999.5 test Coach×Coachee AF-3/AF-6 | DROP — stale, Phase 365 sudah ship `MarkMappingCompletedTests.cs` (AF-3 graduate + AF-6 race) | hapus dari ROADMAP Backlog |
| 999.13 Section re-guard resume path | DEFER (permanent) — benign; soal di-pin `ShuffledQuestionIds`, Section cuma display/pagination null-safe; worst case re-pagination kosmetik | tandai permanent-defer |
| 999.16 LinkPrePost section guard | DEFER (permanent) — no-op; `InjectQuestionSpec` tak punya `SectionId` → inject all-Lainnya → guard tak akan nyala. Promote hanya bila muncul surface LinkPrePost non-inject | tandai permanent-defer |
| 999.15 relabel senyap | PROMOTE → v32.9 | (milestone ini) |

## Push IT

| Item | Status |
|------|--------|
| Push v29.0 + v30.0 ke `origin/ITHandoff` (branch + tag) | ✅ PUSHED 2026-06-15, HEAD `fe8c5ffe` |
| Notify IT — 2 migration carry lama (`PendingProtonBypass`+index/360, `ShuffleToggles`/372) | ⏳ PENDING — kasih commit hash + flag ke IT |
| Push v32.2 + v32.5 + v32.6 ke `origin/main` (bundle deploy) | ⏳ pending koordinasi IT — **migration=TRUE:** Phase 409 `AddParticipantRemovalColumns` (v32.5) + Phase 415 `AddAssessmentPackageSection` (v32.6) |
| v32.9 | migration=FALSE (rencana) |

## Accumulated Context

### Decisions (persist across milestones)

- [v32.6 / 418 opsi dinamis]: CreateQuestion/EditQuestion POST pakai binding `List<OptionInput>` (≤6) + `correctIndex` (MC single-select). Guard H3 (`q.Options.Count>4`) DIHAPUS → soal 5-6 opsi editable. Guard edit-shrink **D-418-02** (`OptionShrinkGuard.FindBlockedOptionIds`, query-existence pre-SaveChanges) tutup hazard FK-Restrict 500 untuk shrink-EKOR + convert→Essay. ⚠️ Upsert opsi POSISIONAL (`existing[i]` by Id) di-LOCK spec D-418-02 → **lubang 999.15** (relabel opsi tengah senyap) = target v32.9.
- [v32.6 / Section]: entity `AssessmentPackageSection` per-paket + `PackageQuestion.SectionId int?` nullable (FK Question→Section SetNull, Section→Package Restrict — Cascade picu SQL 1785 multi-path). Section opsional → kosong = perilaku global lama (kompatibel-mundur). `ShuffleEngine` partisi by `(SectionNumber, ET)`. `SectionStructureComparer.MismatchedSections` + re-guard StartExam (SEC-04).
- [v32.5 / soft-remove]: 3 kolom `RemovedAt/RemovedBy/RemovalReason` (migration=TRUE `AddParticipantRemovalColumns`). Invarian: soft-removed ⇔ `RemovedAt != null` (BUKAN via Status). Seam `CMPController.IsParticipantRemoved`.
- [v29.0 / 382 / D-01-IMPACT]: SAVE-01 dedupe last-write-wins in-memory, NO migration.
- [v27.0 / SHUF]: Shuffle Toggle 2 sistem independen default ON; engine pure `Helpers/ShuffleEngine.cs`.
- [v25.0 / A-4]: Penanda kelulusan Proton lewat 1 helper bersama `ProtonCompletionService` (Origin).
- [v24.0 / spec §9]: Hapus file gambar pola Phase 333/335 — kumpul path SEBELUM tx, File.Delete SETELAH commit, warn-only.
- [v22.0 cross-milestone]: `AssessmentConstants.AssessmentStatus.PendingGrading` = single source of truth label lintas 11+ surface.
- [v12.0]: AdminController dipecah jadi 8 controller per domain; URL tetap via [Route].

### Open Blockers/Concerns

- [push] Carry migration lama (Origin, PendingProtonBypass+index/360, ShuffleToggles/372) — notify IT flag. v28→v32.2 = 0 migration baru; **v32.5 Phase 409 + v32.6 Phase 415 = migration=TRUE** (di range unpushed sama).
- [branch] ⚠️ JANGAN tarik ITHandoff→main tanpa cherry-pick guard 391/398.1 (ITHandoff kehilangan guard tsb). v32.3/v32.4/v32.8 hidup di ITHandoff (terpisah dari main).
- [v32.9] Fix MEKANISME (identity-based), bukan perketat threshold guard (in-code note `AAC:8035` LOCK). UAT real-browser WAJIB (lesson 354 — Razor/JS authoring form). Skenario kritis: hapus opsi tengah pada soal SUDAH dijawab → harus diblokir, bukan relabel.

## Session Continuity

Last activity: 2026-06-24

Stopped at: **v32.9 roadmap CREATED** via /gsd-new-milestone. PROJECT.md + REQUIREMENTS.md (OPTEDIT-01..05 + VRF-01) + ROADMAP.md (1 fase, Phase 420) + STATE.md siap. v32.6 phase dirs diarsip ke `milestones/v32.6-phases/`. Backlog di-triage (999.15 PROMOTED→420; 999.14/999.9/999.5 DROPPED; 999.13/999.16 PERMANENT-DEFER). Scope terverifikasi = backlog 999.15 (7-agen verify workflow).

Next action: **`/gsd-plan-phase 420`** (atau `/gsd-discuss-phase 420` dulu — fix mengubah mekanisme spec-locked D-418-02, layak discuss). migration=FALSE.

## Deferred Items

Items acknowledged + deferred at v32.6 milestone close (2026-06-24) — pre-existing cross-project debt + GSD UAT-convention status-staleness, NOT blockers:

| Category | Count | Note |
|----------|-------|------|
| debug sessions | 14 | [diagnosed] — OLD/other features (KKJ matrix, paste-excel, monitoring, team-view) — predate v32.6 |
| quick tasks | 45 | project-wide backlog (KKJ/CPDP/dll) |
| backlog 999.x | 5 | 999.13/14/15/16 + 999.9/999.5 — verified+triaged 2026-06-24 (lihat Backlog Housekeeping di atas) |

Full audit: `gsd-tools audit-open`. None block ship.
