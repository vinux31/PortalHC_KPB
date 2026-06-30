---
gsd_state_version: 1.0
milestone: v32.9
milestone_name: EditQuestion Option-Edit Data Integrity (SHIPPED) — no active milestone
status: complete
stopped_at: Phase 999.17 CLOSED (3/3 plans, all gates green, UAT-approved, ROADMAP marked ✅)
last_updated: "2026-06-30T10:00:00.000Z"
last_activity: 2026-06-30
progress:
  total_phases: 1
  completed_phases: 1
  total_plans: 3
  completed_plans: 3
  percent: 100
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Tidak ada phase aktif — semua milestone v32.x CLOSED (terakhir v32.9 shipped 2026-06-25). Backlog Phase 999.17 closed 2026-06-30. Next: `/gsd-new-milestone` atau `/gsd-review-backlog`.

## Current Position

Phase: 999.17 (excel-zero-config-template-dropdown-data-validation-plus-imp) — ✅ CLOSED 2026-06-30
Plan: 3 of 3 complete
Status: Phase 999.17 CLOSED — semua gerbang hijau (verify 7/7, code-review clean 0C/0W/3-info, secure 8/8 threats_open:0, full-suite 1027/0/2), UAT manusia APPROVED (VRF-03), migration=FALSE. ROADMAP ditandai ✅.
Last activity: 2026-06-30

**Milestone v32.9 (Phase 420, EditQuestion identity-based option-edit) SUDAH SHIPPED 2026-06-25** (lihat MILESTONES.md — 1 phase/3 plan, audit 6/6, migration=FALSE, `cf8595e3` ada di origin/main). 999.17 = backlog phase dikerjakan SETELAH v32.9 close, kini juga CLOSED. **Tidak ada phase aktif berikutnya** — next = `/gsd-new-milestone` (scope baru) atau `/gsd-review-backlog` (promote 999.x lain).

## Milestone Source (historis)

> v32.9 (Phase 420 — fix relabel-senyap identity-based option-edit) **SUDAH SHIPPED 2026-06-25** + pushed `cf8595e3` + tag `v32.9`. Detail root-cause/akomplismen ada di `MILESTONES.md` §v32.9. Working-notes dipindah ke sana — tidak ada kerja v32.9 yang tersisa.

## Backlog Housekeeping (lakukan saat milestone setup / close)

| Item | Verdict (verify 2026-06-24) | Aksi |
|------|------|------|
| 999.14 EditQuestion delete-answered FK-Restrict 500 | DROP — sudah ditutup guard D-418-02 (418) | hapus dari ROADMAP Backlog (regression-lock di v32.9) |
| 999.9 label Backfill/Restore | DROP — no-op, view BulkBackfill hard-removed Phase 396 (`74f266bf`), 0 source match | hapus dari ROADMAP Backlog |
| 999.5 test Coach×Coachee AF-3/AF-6 | DROP — stale, Phase 365 sudah ship `MarkMappingCompletedTests.cs` (AF-3 graduate + AF-6 race) | hapus dari ROADMAP Backlog |
| 999.13 Section re-guard resume path | DEFER (permanent) — benign; soal di-pin `ShuffledQuestionIds`, Section cuma display/pagination null-safe; worst case re-pagination kosmetik | tandai permanent-defer |
| 999.16 LinkPrePost section guard | DEFER (permanent) — no-op; `InjectQuestionSpec` tak punya `SectionId` → inject all-Lainnya → guard tak akan nyala. Promote hanya bila muncul surface LinkPrePost non-inject | tandai permanent-defer |
| 999.15 relabel senyap | ✅ SHIPPED via v32.9 Phase 420 (identity-based option-edit, 2026-06-25) | DONE |
| 999.17 Excel zero-config + import Skor | ✅ CLOSED 2026-06-30 (gerbang hijau, UAT-approved) | DONE |

## Push IT

**⏳ PENDING (perlu aksi — koordinasi IT):**

| Item | Status |
|------|--------|
| Notify IT — 2 migration carry lama (`PendingProtonBypass`+index/360, `ShuffleToggles`/372) | ⏳ kasih commit hash + flag ke IT |
| Push bundle ke `origin/main` (v32.2 + v32.5 + v32.6 + **999.17**) | ⏳ koordinasi IT — **migration=TRUE:** Phase 409 `AddParticipantRemovalColumns` (v32.5) + Phase 415 `AddAssessmentPackageSection` (v32.6). 999.17 = 10 commit unpushed (`ade2425c`..`7720fffa`), migration=FALSE |

**✅ DONE (tidak perlu aksi — jejak audit):**

| Item | Status |
|------|--------|
| Push v29.0 + v30.0 ke `origin/ITHandoff` (branch + tag) | ✅ PUSHED 2026-06-15, HEAD `fe8c5ffe` |
| v32.9 (Phase 420) | ✅ SELESAI — di `origin/main` (`cf8595e3`) + tag `v32.9`, migration=FALSE |

## Accumulated Context

### Decisions (persist across milestones)

- [999.17 / 01 Excel zero-config download]: Template Excel soal "zero-config" sisi DOWNLOAD via seam pure `Helpers/QuestionTemplateBuilder.Build(type)->XLWorkbook` (ekstrak dari `DownloadQuestionTemplate`, analog `InjectExcelHelper`). Dropdown DataValidation `QuestionType` (List 3-nilai, inCellDropdown, baris 2-1000) di SEMUA template: Universal kolom L(12) + legacy MC/MA/Essay kolom H(8). Kolom `Skor` baru di Universal posisi 14(N) APPEND setelah Rubrik (marker dual-format 6/7/9/10 TAK bergeser) + numeric DV WholeNumber 1-100; legacy TANPA Skor (D-07). DV = hint anti-typo non-otoritatif (ErrorStyle.Warning); server otoritatif menyusul Plan 02. DataValidation greenfield ClosedXML 0.105.0 (`git grep CreateDataValidation` dulu 0 kini ada). migration=FALSE. (Plan 02 sudah ganti hardcode 10 → ScoreValue dari kolom Skor Excel.)
- [999.17 / 02 Import Skor]: `ImportPackageQuestions` baca kolom Skor (N/14) di branch `isNewFormat` via **list paralel `rawScores`** (lockstep ke `rows`, BUKAN perlebar tuple 13-field → hindari cascade 3 signature helper RowIsValid/CorrectLettersMapToFilledOptions/rowForCheck + risiko regresi dual-format; PATTERNS menyanksi alternatif ini). `ScoreValue` ganti hardcode 10. Validasi server `int.TryParse(NumberStyles.None, InvariantCulture)` + range 1-100 (tolak desimal/negatif/>100/non-angka, D-09/D-10). Tolak-keras-atomic (D-12): ≥1 invalid → `TempData["ScoreErrors"]` + RedirectToAction + **0 write** (pola SectionMismatch, pass mandiri sebelum persist). Legacy 9-kolom + paste → rawScore null → default 10 (D-07/LEGACY-SAFE). View render ScoreErrors (alert-danger) + dok kolom Skor. **GradingService 0 diff (D-11)** — grade-lock dikunci `WeightedScoreImportTests` (import bobot non-uniform 30/10 → finalPercentage 75). suite Skor 15/15 + fast 653/0/2 + grading-integ 14/14. migration=FALSE. Commits test `5e4719cf` → feat `92a69a86`. Plan 03 UAT-approved (VRF-03) — phase CLOSED.
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

## Session Continuity

Last activity: 2026-06-30

Stopped at: Phase 999.17 CLOSED — bookkeeping done (ROADMAP ✅ + STATE + MEMORY updated). 3/3 plan, gerbang hijau, UAT-approved, migration=FALSE.

Next action: Tidak ada phase aktif. v32.9 (Phase 420) shipped 2026-06-25; 999.17 closed 2026-06-30. Opsi: (1) push 9 commit 999.17 (`ade2425c`..`1907621b`) bareng bundle migration-TRUE (v32.5/409 + v32.6/415) ke origin/main saat koordinasi IT; (2) `/gsd-new-milestone` scope baru; (3) `/gsd-review-backlog` promote 999.x lain (999.13/999.16 = permanent-defer; 999.9/999.5 = dropped-stale).

## Deferred Items

Items acknowledged + deferred at **v32.9 milestone close (2026-06-25)** — same pre-existing cross-project debt carried from v32.6 close, NOT blockers (Phase 420-specific verification staleness was RESOLVED before close, not deferred):

| Category | Count | Note |
|----------|-------|------|
| debug sessions | 14 | [diagnosed] — OLD/other features (KKJ matrix, paste-excel, monitoring, team-view, user-assessment-history-404, delete-single-assessment) — predate v32.9 |
| quick tasks | 45 | project-wide backlog (KKJ/CPDP/records/dll) |
| pending todos | 1 | 2026-06-11 one-time-cleanup data test lokal (database) |
| backlog 999.x | 4 | 999.13/999.16 permanent-defer (no-op/benign) + 999.9/999.5 dropped-stale; **999.14 = regression-lock di 420, 999.15 = SHIPPED via 420** |

Full audit: `gsd-tools audit-open`. None block ship.
