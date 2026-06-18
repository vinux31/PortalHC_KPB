---
gsd_state_version: 1.0
milestone: v22.0
milestone_name: CMP-06 Residual Fix + CMP/Records + ManageAssessment/Monitoring Audit
status: executing
stopped_at: Completed 399-02-PLAN.md
last_updated: "2026-06-18T05:27:14.054Z"
last_activity: 2026-06-18
progress:
  total_phases: 30
  completed_phases: 0
  total_plans: 4
  completed_plans: 2
  percent: 50
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 399 — Foundation — Junction UserUnits + Primary-Mirror + Multi-Select UI + Display

## Current Position

Phase: 399 (Foundation — Junction UserUnits + Primary-Mirror + Multi-Select UI + Display) — EXECUTING
Plan: 3 of 4
Status: Plan 02 SHIPPED (write-through SyncUserUnitsAsync di Create/Edit/Import + MU-07 guard + validasi + import/export multi-unit + 6 test logic GREEN) — ready execute plan 03
Last activity: 2026-06-18 -- 399-02 complete (write-through primary-mirror + MU-07 + import/export; suite 366/366, round-trip SQL MATCH)

## Next Action

Execute **399-03** (multi-select widget Create/Edit Worker — `initSectionUnitMultiCascade` checkbox-list + primary radio di `shared-cascade.js`; DOM widget + MU-07 modal Razor; Playwright spec runtime round-trip 2-unit + MU-07 modal/block). Konsumsi ViewModel `Units`/`PrimaryUnit` + `ViewBag.SectionUnitsJson` dari Plan 02. **migration=TRUE notify IT** saat milestone push (fc015f4d, Plan 01). Plan 02 = 0 migration.

**Critical path:** `399 → 401 → 402 → 404`.

Urutan + paralelisme eksekusi v32.3 (spec §6):

- **Wave 0 — Phase 399 (solo, migration=TRUE):** Model `UserUnit` + migration `AddUserUnitsTable` (filtered-unique index primary) + backfill 1 primary-row/pekerja + kontrak write-through primary-mirror (Worker Create/Edit/Import) + UI Bagian-single/Unit-multi-select + display semua unit (Profil/WorkerDetail/Settings/ManageWorkers/Excel/Home/`_PSign`) + Import multi-unit + validasi `Unit ∈ unit-Bagian` + audit set-diff + guard hapus-unit. REQ MU-01/02/03/04/05/07.
- **Wave 1 — Phase 400 + 401 + 403 PARALEL** (cluster file disjoint, depends 399; eksekusi via git worktree terpisah, merge tiap selesai):
  - **400** (MU-06) — listing set-aware `WorkerDataService`/`WorkerController`/CMP-view + rollup dedup. 0 migration.
  - **401** (PSU-01/02/03/04/05/07) — resolusi PROTON `AssignmentUnit` eksplisit (drop fallback `User.Unit`) + filter axis + validasi ∈UserUnits + no-clobber + skip+audit-warn + reactivation guard. File `CoachMapping`/`CDP`/`ProtonData`/`Bypass`/`AssessmentAdmin`. 0 migration.
  - **403** (ORG-01/02) — `OrganizationController` cascade/guard UserUnits-aware + reparent cross-Bagian hard-block + PreviewEditCascade. Terisolasi. 0 migration.
- **Wave 2 — Phase 402 SERIAL setelah 401** (CXU-01..05) — coaching cross-unit: eligible set-aware + server guard ⊆Bagian + AssignmentUnit per-coachee + relax JS lock + self-scope multi-unit coach. Berat di `CoachMapping`+`CDP` (shared dgn 401) + butuh aturan AssignmentUnit dari 401. 0 migration.
- **Wave 3 — Phase 404 setelah semua** (QA-01..04) — test SQL riil (SQLEXPRESS, fixture {X,Y} + coach cross-unit + PROTON T1@X→T2@Y) + invariant single-active + invariant `AssignmentUnit ∈ UserUnits` + B-06 anti-dobel + UAT + docs D1=b. 0 migration.

**Invariant global WAJIB dijaga (spec §7):** (1) Section scalar 1 Bagian/akun (semua `UserUnits.Unit` anak Bagian); (2) PROTON single-active (1 `ProtonTrackAssignment` aktif, index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` dipertahankan); (3) primary mirror `ApplicationUser.Unit`=baris `IsPrimary` (write-through); (4) `AssignmentUnit ∈ coachee.UserUnits` (pasca-401); (5) `ProtonKompetensi.Unit` 1:1 per deliverable. **D1=b** = cert/analytics atribusi primary (no kolom unit-at-issue). **De-risk:** authz Section (`IsResultsAuthorized`+SectionHead L4) 100% scalar → 0 perubahan.

**Verifikasi tiap fase (CLAUDE.md Develop Workflow):** `dotnet build` + `dotnet run` (localhost:5277) + cek DB lokal + Playwright bila ada UI. ❌ tidak ada edit di Dev/Prod. Semua → 1 push → notify IT re-deploy Dev (**migration=TRUE** Phase 399, commit hash).

## Tag Git

- `v24.0`..`v31.0` — ✅ tag dibuat. v29/v30 PUSHED `origin/ITHandoff`; v31.0 MERGED→main + PUSHED `origin/main` 2026-06-16 (merge `7ea6c81e`). v32.1 CLOSED (archive-only, tag lokal, NOT pushed — deploy bareng v32.3).

## Deferred Items

> ✅ **ACCEPTED OK 2026-06-14** (keputusan user): semua carry-over v11.2/v13/v14/v15 di bawah = phase lama, dianggap OK / non-blocking (kode ship + jalan; tak ada bug report v16-v32). Histori, bukan TODO. Buka lagi hanya bila muncul bug/kebutuhan nyata.

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
| 999.11 WR-01 PendingGrading guard (parked dari v32.0) | tech-debt parked |
| 999.12 391 test WebApplicationFactory (parked dari v32.0) | tech-debt parked |
| 43 quick-task todo (audit-open, status `[missing]`) | acknowledged deferred (artifact lama hilang) |

### v2 / future (dari REQUIREMENTS v32.3)

| Item | Reason |
|------|--------|
| Cert/analytics atribusi per-unit akurat (kolom unit-at-issue + backfill) | deferred — D1=b primary; buka bila compliance per-unit butuh (migration ke-2) |
| PROTON paralel (2 track aktif konkuren) | deferred — sekuensial dikonfirmasi; perlu relax unique index + kolom Unit `ProtonTrackAssignment` + re-key ~21 site |

### Push IT

| Item | Status |
|------|--------|
| Notify IT — 2 migration carry lama (`PendingProtonBypass`+index/360, `ShuffleToggles`/372) | ⏳ PENDING (carry lama) |
| **v32.1** — 0 migration, 0 backend; deploy ditunda (close bareng v32.3) | ⏳ pending (bundle dgn v32.3) |
| **v32.3** — **migration=TRUE** (`AddUserUnitsTable` Phase 399 + backfill); 1 push → notify IT migration=TRUE (commit hash) | ⏳ pending (roadmap dibuat, belum di-plan/execute) |

## Accumulated Context

### Decisions (persist across milestones)

- [399-02 / write-through SyncUserUnitsAsync SHIPPED]: kontrak write-through primary-mirror ter-implement di `WorkerController` sebagai PUBLIC STATIC helper testable (`SyncUserUnitsAsync(ctx,user,units,primary)` + `ParseUnitCell` + `ValidateUnitsInSection` + `EvaluateRemoveUnitGuardAsync` + `WorkerUnitsView` record) — pola `AdminBaseController.FindTitleDuplicatesAsync` (testable seam, no InternalsVisibleTo). Single-source junction `UserUnits` + mirror `ApplicationUser.Unit` (replace-set DELETE-before-INSERT, no window 2-primary) + audit set-diff (D-12, hapus anti-pattern `if user.Unit != model.Unit`). Wire Create/Edit/Import; **EditWorker 1-tx atomic** (`BeginTransactionAsync`: UpdateAsync + UserUnits + deactivate, **Open Q3**). **MU-07 asimetris**: PTA aktif → HARD-BLOCK (`protonUnit = AssignmentUnit ?? oldPrimary`, **Open Q1** kedua cabang + **Open Q2** kosong-semua-unit ter-cover) (D-11); coach-mapping aktif tanpa PTA → confirm (NeedConfirm re-prompt) → auto-deactivate IsActive/EndDate 1 tx (D-10). Validasi `Unit∈Bagian` + `primary∈set` tiap junction-write (MU-05, mass-assignment guard). Import **pipe** "UnitA|UnitB" first=primary dedup backward-compat (MU-04) + per-unit validasi; Export kolom 7 primary-first comma-join (D-08); template help-text + contoh pipe (D-06); ManageWorkers `ViewBag.UserUnitsDict` (untuk display Plan 04, unitFilter TETAP scalar = Phase 400 MU-06). **6 test logic Wave 0 GREEN (19 fakta)**; suite **366/366** (347+19, 0 skip, no regresi); build 0 error; app boot localhost:5277 HTTP 200; **round-trip 2-unit SQL lokal MATCH** (2 baris/1 IsPrimary/mirror==primary-row) + filtered-unique tolak 2nd primary + RESTORE baseline (SEED_JOURNAL cleaned). authz `[Authorize(Admin,HC)]` 12 + `[ValidateAntiForgeryToken]` 6 utuh; bind hanya Units/PrimaryUnit. Commits 862003b7/facc0df6/23fb5033/dadca0cc. **Plan 02 = 0 migration.** Interface siap Plan 03 (widget konsumsi Units/PrimaryUnit/SectionUnitsJson) + Plan 04 (display konsumsi UserUnitsDict/VM/PSign).
- [399-01 / junction UserUnits APPLIED]: tabel `UserUnits` ada di DB lokal (`HcPortalDB_Dev`) — filtered-unique `IX_UserUnits_UserId_PrimaryUnique` WHERE [IsPrimary]=1 (enforce invariant #3) + unique `IX_UserUnits_UserId_Unit_Unique` + backfill idempotent (6 baris IsPrimary=1 == 6 Users Unit-non-null; Unit-null=0 baris; re-run backfill=0 dobel). Migration scaffold via **global `dotnet ef` CLI v10.0.3** (backward-compat OK dengan project EF 8.0.0; snapshot tetap ProductVersion 8.0.0) — fallback pin-tool/hand-author tak perlu. Index UserId polos dibuang (EF dedup ke filtered-unique; has-pending-model-changes=none). `Users.Unit` scalar DIPERTAHANKAN (mirror). **migration=TRUE commit `fc015f4d`** (`AddUserUnitsTable`) — SATU-SATUNYA migration milestone v32.3, notify IT saat push. Interface siap untuk plan 02 (SyncUserUnitsAsync write-through) + plan 03/04 (UI/display). 7 test scaffold Wave 0 RED skip-with-reason (suite 347 pass/0 fail/16 skip).
- [v32.3 roadmap / phases 399-404]: 6 fase derived dari spec §5/§6 (mapping fase→REQ + dependency TERKUNCI, bukan re-derive) — **399** Foundation junction `UserUnits`+mirror+multi-select UI (MU-01/02/03/04/05/07, **migration=TRUE**), **400** listing set-aware+dedup (MU-06), **401** PROTON unit-resolution hardening (PSU-01/02/03/04/05/07), **402** coaching cross-unit (CXU-01..05), **403** Org cascade/guard UserUnits-aware (ORG-01/02), **404** test SQL riil+UAT+docs (QA-01..04). 24/24 REQ mapped, 0 orphan/duplicate. Dependency: 400/401/403→399; 402→401; 404→semua. Wave 1 {400,401,403} PARALEL (cluster file disjoint), 402 serial setelah 401. Critical path 399→401→402→404. Phase numbering mulai 399 (391-398 reserved di branch main: v32.0=391-392, v32.2=393-398).
- [v32.3 invariant (spec §7)]: (1) Section scalar 1 Bagian/akun; (2) PROTON single-active (index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` + E8 dipertahankan); (3) primary mirror `ApplicationUser.Unit`=baris `UserUnits.IsPrimary` write-through; (4) `AssignmentUnit ∈ coachee.UserUnits` (pasca-401); (5) `ProtonKompetensi.Unit` 1:1 per deliverable. Junction `UserUnits.Unit` = NAME-string (konsisten `AssignmentUnit`/`ProtonKompetensi.Unit`), validasi via `GetUnitsForSectionAsync(user.Section)`. D1=b cert/analytics atribusi primary (no kolom unit-at-issue, no migration ke-2). De-risk: authz Section 100% scalar → 0 perubahan.
- [v32.1 / 389-01 spec parity]: `tests/e2e/coachcoacheemapping-389.spec.ts` 14-test (V-01..V-14) test-first (Nyquist safeguard, Phase 354 lesson). Closed PASSED 7/7.
- [v31.0 Hotfix Pra-Ujian Lisensor / phases 385-387]: 14/14 PXF closed, 0 migration. Pattern: shared display helper `AssessmentScoreAggregator.IsQuestionCorrect`+`BuildAnswerCell` (kill-drift); essay PathBase-aware sub-path. MERGED→main 7ea6c81e, UAT Dev full-lifecycle PASS.
- [v30.0 / ECG]: helper terpusat `AssessmentScoreAggregator.IsQuestionCorrect` (essay `>0`=Benar) `CMPController.Results` 4 site + PDF (kill-drift).
- [v29.0 / 382]: SAVE-01 dedupe last-write-wins in-memory, NO migration.
- [v25.0 / A-4]: Penanda kelulusan Proton lewat 1 helper bersama `ProtonCompletionService` (Origin).
- [v24.0 / spec §9]: Hapus file gambar pola Phase 333/335 — kumpul path SEBELUM tx, File.Delete SETELAH commit, warn-only.
- [v24.0 / AF-1..7]: eligibility coachee per-unit (CoacheeEligibilityCalculator) — **relevan v32.3** (multi-unit memperluas eligibility ke lintas-unit dalam Bagian, fase 402).
- [v22.0 cross-milestone]: `AssessmentConstants.AssessmentStatus.PendingGrading` single source of truth label.
- [v21.0 / org labels]: tier label org (Bagian/Unit/Sub-unit) configurable via `IOrgLabelService` + global `@inject OrgLabels` (110 calls / 26 views) — **relevan v32.3** (display unit + multi-select Bagian/Unit pakai `@OrgLabels.GetLabel(0/1)`).
- [v13.0]: org tree `OrganizationUnit` self-FK `ParentId` (Level0=Bagian, Level1=Unit), user nyambung via Name-string bukan Id — **fondasi v32.3** (`UserUnits.Unit` NAME-string anak Bagian; `GetSectionUnitsDictAsync`/`GetUnitsForSectionAsync` primitif siap dipakai multi-select + validasi).
- [v12.0]: AdminController dipecah jadi 8 controller per domain; URL tetap via [Route]. (Worker/CoachMapping/CDP/Organization/AssessmentAdmin = controller terpisah → fondasi cluster file disjoint paralelisme Wave 1 v32.3.)

### Open Blockers/Concerns

- [push] Carry migration lama (`PendingProtonBypass`+index/360, `ShuffleToggles`/372) — notify IT flag. **v32.3 = migration BARU TRUE** (`AddUserUnitsTable` Phase 399 + backfill).
- [v32.3 risiko utama (spec §10)]: (a) `CleanupCoachCoacheeMappingOrg` reset `AssignmentUnit`→primary = data-loss multi-unit → Fase 401 jadikan UserUnits-aware/gated SEBELUM data multi-unit produksi; (b) reparent unit lintas-Bagian = split-brain Section → Fase 403 hard-block; (c) primary-mirror desync → kontrak write-through terpusat + test; (d) EF-InMemory tak enforce filtered-unique-index → test palsu hijau → Fase 404 WAJIB SQL riil SQLEXPRESS; (e) atribusi cert primary bikin cert unit-Y muncul di laporan unit-X → diterima (D1=b), didokumentasikan.
- [v32.3 / v32.0 close DEFERRED]: ⚠️ JANGAN `/gsd-complete-milestone v32.0` standar (REQUIREMENTS/STATE/PROJECT kini live v32.3 → step5 destruktif). Safe close v32.0 NANTI manual (post-v32.3). Lihat MEMORY `project_v32_0_close_deferred`.

## Session Continuity

Last activity: 2026-06-18

Stopped at: Completed 399-02-PLAN.md

Next action: `/gsd-plan-phase 399` (Foundation — Wave 0 solo, migration=TRUE). Lalu Wave 1 {400, 401, 403} paralel, Wave 2 = 402 (setelah 401), Wave 3 = 404. `/clear` dulu (fresh context).
